using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using Unity.VisualScripting.ReorderableList.Element_Adder_Menu;
using UnityEditor;
using UnityEngine;

// WILL SERVE AS A BASE CLASS FOR ALL ENEMIES

public class Enemy : MonoBehaviour
{

    [SerializeField] protected float health;
    [SerializeField] protected float recoilLength;
    [SerializeField] protected float recoilFactor;
    [SerializeField] protected bool isRecoiling = false;

    [SerializeField] protected PlayerController player;
    [SerializeField] protected float speed;

    [SerializeField] protected float damage;

    protected float recoilTimer;

    protected Rigidbody2D rb;
    protected SpriteRenderer sr;

    protected enum EnemyStates{
        // mushroom
        Mushroom_Idel,
        Mushroom_Flip,

        // bat
        Bat_Idle,
        Bat_Chase,
        Bat_Stunned,
        Bat_Death,
    }
    protected EnemyStates currentEnemyState;

    // Start is called before the first frame update
    protected virtual void Start(){
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        player = PlayerController.Instance;
    }

    // Update is called once per frame
    protected virtual void Update()
    {        
        if (health <= 0){
            Destroy(gameObject);
        }

        if(isRecoiling){
            if(recoilTimer < recoilLength){
                recoilTimer += Time.deltaTime;
            }
            else{
                isRecoiling = false;
                recoilTimer = 0;
            }
        }
        else{
            UpdateEnemyStates();
        }
    }

    public virtual void EnemyGetsHit(float _damageDone, Vector2 _hitDirection, float _hitForce){
        health -= _damageDone;

        if(!isRecoiling){
            rb.velocity = _hitForce * recoilFactor * _hitDirection;
        }
    }

    protected void OnCollisionStay2D(Collision2D _other) {
        if(_other.gameObject.CompareTag("Player") && !PlayerController.Instance.pState.invincible && !PlayerController.Instance.pState.invincible && health > 0){
            Attack();
            PlayerController.Instance.HitStopTime(0,5,0.5f);
        }
    }

    protected virtual void Death(float _destroyTime){
        Destroy(gameObject, _destroyTime);
    }

    protected virtual void UpdateEnemyStates(){}    

    protected void ChangeState(EnemyStates _newState){
        currentEnemyState = _newState;
    }

    protected virtual void Attack(){
        //UnityEngine.Debug.Log("attack");
        PlayerController.Instance.TakeDamage(damage);
    }
}
