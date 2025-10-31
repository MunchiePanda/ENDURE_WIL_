using System;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBehaviour : MonoBehaviour     ///PLEASE DO NOT ADD MORE COMMENTS, IF STRUGGLE -> ASK
{
    [Header("Enemy Hearing")]
    [SerializeField] bool canHear = true;               //Default to True
    [SerializeField] float hearingRange = 10f;
    public float hearingSensitivity = 1f;
    private Vector3 lastheardPos;

    /* Enemy sight will have a Raycast to "see" in front of them
     * Enemy will also have a Field of View so That its not Only in the Line Of the Raycast
     */

    [Header("Enemy Sight")]
    [SerializeField] bool canSee = true;               //default to True
    [SerializeField] Transform eyes;
    [Tooltip("The Length of the Raycast that Shoots out the Eyes.")]
    public float sightRange;
    [Tooltip("The Range of the enemies View in Degrees")]
    public float fieldOfView = 60f;  // degrees
    [Tooltip("Obstacle Layer that blocks Player Line of Sight.")]
    public LayerMask obstacleLayer;                     //Obstacle Layer that blocks Player Line of Sight.

    [Header("Enemy Behaviour (Regardless of Sight Or Sound")]
    public Transform player;
    private NavMeshAgent agent;
    public enum EnemyState {Patrolling, Chasing, Attacking}
    public EnemyState currentState = EnemyState.Patrolling;

    public Transform[] patrolPoints;
    public float attackRange = 2f;
    public float patrolWaitTime = 2f;

    private int currentPatrolIndex;
    private float waitTimer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        currentState = EnemyState.Patrolling;       //Det Default State For Enemy                

    }

    // Update is called once per frame
    void Update()
    {
        //Rudementary State Machine
        switch (currentState)
        {
            case EnemyState.Patrolling:
                Patrol();
                break;
            case EnemyState.Attacking:
                Attack();
                break;
            case EnemyState.Chasing: 
                Chase();
                break;

        }

        StateTransitions();
       
    }


    //Will Update this so that it works without CanSee (if using only hearning)
    void StateTransitions()         //Called in Update ( Controls the Transitions between each state.
    {
        bool canSeePlayer = CanSeePlayer();
        float distance = Vector3.Distance(transform.position, player.position);



        if(currentState == EnemyState.Patrolling && canSeePlayer)           //IF Patrolling
        {
            currentState = EnemyState.Chasing;
        }
        else if(currentState == EnemyState.Chasing)                         //If Chasing
        {
            if (!canSeePlayer)                  //Stop Chasing when the Enemy Can No Longer see the player.
            {
                currentState = EnemyState.Patrolling;
                GoToNextPatrolPoint();
            }
            else if(distance < attackRange)                 
            {
                currentState = EnemyState.Attacking; 
            }
        }
        else if(currentState == EnemyState.Attacking)                           //If Attacking
        {
            if(distance > attackRange)
            {
                currentState = EnemyState.Chasing; 
            }

        }
    }

    public void HearNoise(Vector3 noisePos, float noiseVol)                 //Method is Called if Player Makes a Noise
    {
        float dist = Vector3.Distance(transform.position, noisePos);

        if(dist <= hearingRange * noiseVol * hearingSensitivity)
        {
            Debug.Log($"{gameObject.name} heard something!");
            lastheardPos = noisePos;
        }
    }

    private bool CanSeePlayer()
    {
        if(canSee == false) return false;
      
        Vector3 directionToPlayer = (player.position - eyes.position).normalized;       //From Eyes Not General TRansform
        float angle = Vector3.Angle(eyes.forward, directionToPlayer);

        if (angle < fieldOfView * 0.5f)      //Checks if Player is in Field Of View
        {
            //Checks if in Line Of Sight. and Not behind an Obstacle and Not too Far away
            if (Physics.Raycast(eyes.position, directionToPlayer, out RaycastHit hit, sightRange, ~obstacleLayer))      //(~) means, Everything But This Obstacle Layer //This makes it so players can "hide" behind Obstacles
            {
                Debug.Log(hit.transform.gameObject.tag);

                if (hit.transform.CompareTag("Player"))
                {
                    Debug.Log("Raycast hit Player");
                    return true;
                }
            }
        }

        return false;
    }

    private void Chase()
    {
        agent.SetDestination(player.position);
    }

    private void Attack()
    {
        agent.ResetPath();
        transform.LookAt(player.position);      //Look At Player Before Attack

        //Attack Logic
    }

    private void Patrol()
    {
        if(!agent.pathPending && agent.remainingDistance < 2f)        //0.5f being its distnace to the Patrol Point
        {
            waitTimer += Time.deltaTime;

            if(waitTimer > patrolWaitTime)
            {
                GoToNextPatrolPoint();
                waitTimer = 0f;
            }
        }
    }

    private void GoToNextPatrolPoint()
    {
        if(patrolPoints.Length == 0) return;        //No Go, Is Empty
        currentPatrolIndex += 1;

        if(currentPatrolIndex >= patrolPoints.Length)       //Keep within Confines of the Transform Array
        {
            currentPatrolIndex = 0;
        }

        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
    }

    private void OnDrawGizmosSelected()
    {
        if(eyes != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(eyes.position, eyes.forward * sightRange);
        }
    }

}
