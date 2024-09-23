using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LSL;
using LSL4Unity.Utils;

// PlayerController는 플레이어 캐릭터로서 Player 게임 오브젝트를 제어한다.
public class PlayerController : MonoBehaviour {
   public AudioClip deathClip; // 사망시 재생할 오디오 클립
   public float jumpForce = 700f; // 점프 힘

   private int jumpCount = 0; // 누적 점프 횟수
   private bool isGrounded = false; // 바닥에 닿았는지 나타냄
   private bool isDead = false; // 사망 상태

   private Rigidbody2D playerRigidbody; // 사용할 리지드바디 컴포넌트
   private Animator animator; // 사용할 애니메이터 컴포넌트
   private AudioSource playerAudio; // 사용할 오디오 소스 컴포넌트

   #region LSL4Unity_inlet
    public string StreamName; // must be same with the OpenViBE streamname
    ContinuousResolver resolver;
    double max_chunk_duration = 0.5; // epoch interval 2.0sec
    private StreamInlet inlet;

    private float[,] data_buffer;
    private double[] timestamp_buffer;
    float EEGpow;
    bool isSatisfied = false;
    #endregion

   private void Start() {
       // 초기화
       playerRigidbody = GetComponent<Rigidbody2D>();
       animator = GetComponent<Animator>();
       playerAudio = GetComponent<AudioSource>();
   }

   private void Update() {
       // 사용자 입력을 감지하고 점프하는 처리
       if(isDead){
        return;
       }

        #region LSL_inlet_update
        if(inlet!=null){
            int samples_returned = inlet.pull_chunk(data_buffer, timestamp_buffer);
            if(samples_returned > 0){
                float x = data_buffer[samples_returned - 1, 0];

                Debug.Log(x);
                EEGpow = x;

                if(EEGpow < 2000){
                    return;
                }
                else{
                    if(Input.GetMouseButtonDown(0) || jumpCount < 2){
                        jumpCount++;
                        playerRigidbody.velocity = Vector2.zero;
                        playerRigidbody.AddForce(new Vector2(0, jumpForce));
                        playerAudio.Play();
                    }
                    else if(Input.GetMouseButtonUp(0) || playerRigidbody.velocity.y > 0){
                        playerRigidbody.velocity = playerRigidbody.velocity * 0.5f;
                    }
                }
            }
        }
        #endregion
        
       

       animator.SetBool("Grounded", isGrounded);
   }

   private void Die() {
       // 사망 처리
       animator.SetTrigger("Die");
       playerAudio.clip = deathClip;
       playerAudio.Play();

       playerRigidbody.velocity = Vector2.zero;
       isDead = true;

       GameManager.instance.OnPlayerDead();
   }

   private void OnTriggerEnter2D(Collider2D other) {
       // 트리거 콜라이더를 가진 장애물과의 충돌을 감지
       if(other.tag == "Dead" && !isDead){
            Die();
       }
   }

   private void OnCollisionEnter2D(Collision2D collision) {
       // 바닥에 닿았음을 감지하는 처리
       if(collision.contacts[0].normal.y > 0.7f){
        isGrounded = true;
        jumpCount = 0;
       }
   }

   private void OnCollisionExit2D(Collision2D collision) {
       // 바닥에서 벗어났음을 감지하는 처리
       isGrounded = false;
   }
}