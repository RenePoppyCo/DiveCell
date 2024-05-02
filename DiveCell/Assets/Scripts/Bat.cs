using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Unity.VisualScripting;
using UnityEngine;

public class Bat : Enemy
{
    [SerializeField] private float chaseDistance;
    [SerializeField] private float stunDuration;

    float timer;

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        ChangeState(EnemyStates.Bat_Idle);
    }

    protected override void UpdateEnemyStates()
    {
        float _dist = Vector2.Distance(transform.position, PlayerController.Instance.transform.position);

        switch(currentEnemyState)
        {
            case EnemyStates.Bat_Idle:
                if(_dist < chaseDistance){
                    ChangeState(EnemyStates.Bat_Chase);
                }
                break;
            
            case EnemyStates.Bat_Chase:
                rb.MovePosition(Vector2.MoveTowards(transform.position, PlayerController.Instance.transform.position, Time.deltaTime * speed));

                FlipBat();
                break;
            
            case EnemyStates.Bat_Stunned:
                timer += Time.deltaTime;

                if(timer > stunDuration){
                    ChangeState(EnemyStates.Bat_Idle);
                    timer = 0;
                }
                break;
            
            case EnemyStates.Bat_Death:
                Death(Random.Range(5, 10));
                break;
        }
    }

    protected override void Death(float _destroyTime){
        rb.gravityScale = 12;
        base.Death(_destroyTime);
    }

    public override void EnemyGetsHit(float _damageDone, Vector2 _hitDirection, float _hitForce)
    {
        base.EnemyGetsHit(_damageDone, _hitDirection, _hitForce);

        if(health > 0){
            ChangeState(EnemyStates.Bat_Stunned);
        }
        else{
            ChangeState(EnemyStates.Bat_Death);
        }
    }

    void FlipBat(){
        if(PlayerController.Instance.transform.position.x < transform.position.x){
            sr.flipX = true;
        }
        else{
            sr.flipX = false;
        }
    }

}
