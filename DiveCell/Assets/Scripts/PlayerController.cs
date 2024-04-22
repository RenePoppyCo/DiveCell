using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Horizontal Movement Settings:")]
    [SerializeField] private float walkSpeed = 1;                
    [SerializeField] private float jumpForce = 45f;
    private int jumpBufferCounter = 0;
    [SerializeField] private int jumpBufferFrames;    

    [Header("Ground Check Settings")]    
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckY = 0.2f;
    [SerializeField] private float groundCheckX = 0.5f;
    [SerializeField] private LayerMask ground;   

    PlayerStateList pState;
    private Rigidbody2D rb;
    private float Xaxis;        
    Animator anim;

    // singleton so that we can reference the player script outside of this script 
    public static PlayerController Instance;

    // destory any dupes
    private void Awake() {
        if(Instance != null && Instance != this) {
            Destroy(gameObject);
        }
        else {
            Instance = this;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        pState = GetComponent<PlayerStateList>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        GetInputs();
        UpdateJumpVariables();
        Flip();
        Move();
        Jump();        
    }

    void GetInputs(){
        Xaxis = Input.GetAxisRaw("Horizontal");
    }

    // flip player when walking in other direction
    void Flip(){
        if(Xaxis < 0){
            transform.localScale = new Vector2(-1, transform.localScale.y);
        }
        else if(Xaxis > 0){
            transform.localScale = new Vector2(1, transform.localScale.y);
        }
    }

    private void Move(){
        rb.velocity = new Vector2(walkSpeed * Xaxis, rb.velocity.y);
        anim.SetBool("Running", rb.velocity.x != 0 && Grounded());
    }

    // ensures the player is on the floor before it can take another jump
    public bool Grounded(){
        if(Physics2D.Raycast(groundCheckPoint.position, Vector2.down, groundCheckY, ground) 
            || Physics2D.Raycast(groundCheckPoint.position + new Vector3(groundCheckX, 0, 0), Vector2.down, groundCheckY, ground)
            || Physics2D.Raycast(groundCheckPoint.position + new Vector3(-groundCheckX, 0, 0), Vector2.down, groundCheckY, ground))
        {
            return true;
        }
        else{
            return false;
        }
    }

    void Jump(){
        if(Input.GetButtonUp("Jump") && rb.velocity.y > 0){
            rb.velocity = new Vector2(rb.velocity.x, 0);
            pState.jumping = false;
        }

        if(!pState.jumping){           
            if(jumpBufferCounter > 0 && Grounded()){
                rb.velocity = new Vector3(rb.velocity.x, jumpForce);
                pState.jumping = true;
            }
        }

        anim.SetBool("Jumping", !Grounded());
    }

    // set jumping bool to false when grounded
    void UpdateJumpVariables(){
        if(Grounded()){
            pState.jumping = false;
        }

        if(Input.GetButtonDown("Jump")){
            jumpBufferCounter = jumpBufferFrames;
        }
        else{
            jumpBufferCounter--;
        }    
    }
}
