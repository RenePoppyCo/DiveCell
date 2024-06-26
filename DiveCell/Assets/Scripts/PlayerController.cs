using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Horizontal Movement Settings:")]
    [SerializeField] private float walkSpeed = 1;       

    [Header("Vertical Movement Settings:")]
    [SerializeField] private float jumpForce = 45f;
    private int jumpBufferCounter = 0;
    [SerializeField] private int jumpBufferFrames;   
    private float coyoteTimeCounter = 0;
    [SerializeField] private float coyoteTime; // how long it will be
    private int airJumpCounter = 0; // keep track of how long the player is in the air
    [SerializeField] private int maxAirJumps;

    [Header("Ground Check Settings")]    
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckY = 0.2f;
    [SerializeField] private float groundCheckX = 0.5f;
    [SerializeField] private LayerMask ground;   

    [Header("Dash Settings")]  
    [SerializeField] private float dashSpeed; // control how fast dash is
    [SerializeField] private float dashTime; 
    [SerializeField] private float dashCooldown;

    [Header("Attack Settings")]
    bool attack = false;
    float timeBetweenAttack, timeSinceAttacked;
    [SerializeField] Transform SideAttackTransform, UpAttackTransform, DownAttackTransform;
    [SerializeField] Vector2 SideAttackArea, UpAttackArea, DownAttackArea;
    [SerializeField] LayerMask attackableLayer;
    [SerializeField] float damage;

    [Header("Recoil Settings")]
    [SerializeField] int recoilXSteps = 5;
    [SerializeField] int recoilYSteps = 5;
    [SerializeField] float recoilXSpeed = 100;
    [SerializeField] float recoilYSpeed = 100;
    int stepsXRecoiled, stepsYRecoiled;

    [Header("Health Settings")]
    public int health;
    public int maxHealth;

    [HideInInspector] public PlayerStateList pState;
    private Rigidbody2D rb;
    private float Xaxis, yAxis;        
    Animator anim;
    private bool canDash = true;
    private bool dashed;
    private float gravity;

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
        health = maxHealth;
    }

    // Start is called before the first frame update
    void Start()
    {
        pState = GetComponent<PlayerStateList>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        gravity = rb.gravityScale;
    }

    // we'll call this to clear the scene for attack areas
    private void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(SideAttackTransform.position, SideAttackArea);
        Gizmos.DrawWireCube(UpAttackTransform.position, UpAttackArea);
        Gizmos.DrawWireCube(DownAttackTransform.position, DownAttackArea);
    }

    // Update is called once per frame
    void Update()
    {
        GetInputs();
        UpdateJumpVariables();
        if(pState.dashing) return;
        Flip();
        Move();
        Jump();     
        StartDash();    
        Attack();        
    }

    private void FixedUpdate(){
        if(pState.dashing) return;
        Recoil();
    }

    public void TakeDamage(float _damage){
        UnityEngine.Debug.Log(_damage);
        health -= Mathf.RoundToInt(_damage);

        StartCoroutine(StopTakeingDamage());
    }

    IEnumerator StopTakeingDamage(){
        pState.invincible = true;
        anim.SetTrigger("takeDamage");
        ClampHealth();
        yield return new WaitForSeconds(1f);
        pState.invincible = false;
    }
    
    // set min/max on health
    void ClampHealth(){
        health = Mathf.Clamp(health, 0, maxHealth);
    }

    void GetInputs(){
        Xaxis = Input.GetAxisRaw("Horizontal");
        yAxis = Input.GetAxisRaw("Vertical");
        attack = Input.GetMouseButtonDown(0);
    }

    // flip player when walking in other direction
    void Flip(){
        if(Xaxis < 0){
            transform.localScale = new Vector2(-1, transform.localScale.y);
            pState.lookingRight = false;
        }
        else if(Xaxis > 0){
            transform.localScale = new Vector2(1, transform.localScale.y);
            pState.lookingRight = true;
        }
    }

    private void Move(){
        rb.velocity = new Vector2(walkSpeed * Xaxis, rb.velocity.y);
        anim.SetBool("Running", rb.velocity.x != 0 && Grounded());
    }

    void StartDash(){
        if(Input.GetButtonDown("Dash") && canDash && !dashed){
            StartCoroutine(Dash());
            dashed = true; // won't allow player to dash again once in the air
        }

        if(Grounded()){
            dashed = false;
        }
    }

    // coroutine for dashing
    IEnumerator Dash(){
        canDash = false;
        pState.dashing = true;
        anim.SetTrigger("Dashing");
        rb.gravityScale = 0;
        rb.velocity = new Vector2(transform.localScale.x * dashSpeed, 0);
        yield return new WaitForSeconds(dashTime);
        rb.gravityScale = gravity;
        pState.dashing = false;
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    void Attack(){        
        timeSinceAttacked += Time.deltaTime;
        if(attack && timeSinceAttacked >= timeBetweenAttack){
            timeSinceAttacked = 0;
            anim.SetTrigger("Attacking");
            
            //UnityEngine.Debug.Log("this is called at leasttasdashdhasdashjlksadh");

            // set up side attack (when the player is not holding down w or s, or holding down s, but grounded)
            if(yAxis == 0 || yAxis < 0 && Grounded()){
                Hit(SideAttackTransform, SideAttackArea, ref pState.recoilingX, recoilXSpeed);
            }
            else if(yAxis > 0){
                Hit(UpAttackTransform, UpAttackArea, ref pState.recoilingY, recoilYSpeed); // when player attacks up
            }
            else if(yAxis < 0 && !Grounded()){
                Hit(DownAttackTransform, DownAttackArea, ref pState.recoilingY, recoilYSpeed);
            }
        }
    }

    private void Hit(Transform _attackTransform, Vector2 _attackArea, ref bool _recoilDir, float _recoilStrength){
        // declairing what player is able to hit or not hit
        Collider2D[] objectsToHit = Physics2D.OverlapBoxAll(_attackTransform.position, _attackArea, 0, attackableLayer);

        if(objectsToHit.Length > 0){
            //UnityEngine.Debug.Log("Hit");
            _recoilDir = true;
        }
        // look for things to hit within the area
        for(int i=0; i < objectsToHit.Length; i++){
            if(objectsToHit[i].GetComponent<Enemy>() != null){
                objectsToHit[i].GetComponent<Enemy>().EnemyHit(damage, (transform.position - objectsToHit[i].transform.position).normalized, _recoilStrength);
            }
        }
    }

    void Recoil(){
        if(pState.recoilingX){
            if(pState.lookingRight){
                rb.velocity = new Vector2(-recoilXSpeed, 0);
            }
            else{
                rb.velocity = new Vector2(recoilXSpeed, 0);
            }
        }

        if(pState.recoilingY){
            rb.gravityScale = 0; // ensure gravity does not affect recoil

            if(yAxis < 0){      // if attacking downward           
                rb.velocity = new Vector2(rb.velocity.x, recoilYSpeed);
            }
            else{
                rb.velocity = new Vector2(rb.velocity.x, -recoilYSpeed);
            }
            airJumpCounter = 0;
        }
        else{
            rb.gravityScale = gravity;
        }

        if(pState.recoilingX && stepsXRecoiled < recoilXSteps){
            stepsXRecoiled++;
        }
        else{
            StopRecoilX();
        }
        
        if(pState.recoilingY && stepsYRecoiled < recoilYSteps){
            stepsYRecoiled++;
        }
        else{
            StopRecoilY();
        }
        if(Grounded()){
            StopRecoilY();
        }
    }

    void StopRecoilX(){
        stepsXRecoiled = 0;
        pState.recoilingX = false;
    }
        void StopRecoilY(){
        stepsYRecoiled = 0;
        pState.recoilingY = false;
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
            if(jumpBufferCounter > 0 && coyoteTimeCounter > 0){
                rb.velocity = new Vector3(rb.velocity.x, jumpForce);
                pState.jumping = true;
            }
            else if(!Grounded() && airJumpCounter < maxAirJumps && Input.GetButtonDown("Jump")){
                pState.jumping = true;
                airJumpCounter++;
                rb.velocity = new Vector3(rb.velocity.x, jumpForce);
            }
        }

        anim.SetBool("Jumping", !Grounded());
    }

    // set jumping bool to false when grounded
    void UpdateJumpVariables(){
        if(Grounded()){
            pState.jumping = false;
            coyoteTimeCounter = coyoteTime;
            airJumpCounter = 0;
        }
        else{
            coyoteTimeCounter -= Time.deltaTime; // decrease when player isn't grounded
        }

        if(Input.GetButtonDown("Jump")){
            jumpBufferCounter = jumpBufferFrames;
        }
        else{
            jumpBufferCounter--;
        }    
    }
}
