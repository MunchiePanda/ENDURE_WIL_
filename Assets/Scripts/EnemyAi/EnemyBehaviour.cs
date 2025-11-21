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

    [Header("Integration: Player Priority")]
    [Tooltip("How long the enemy keeps chasing the player after losing sight.")]
    [SerializeField] private float chaseMemoryDuration = 3f;
    [Tooltip("Rotation speed while moving.")]
    [SerializeField] private float rotationSpeed = 360f;

    private int currentPatrolIndex;
    private float waitTimer;
    private Vector3 lastKnownPlayerPos;
    private bool hasLastKnownPlayerPos;
    private float timeSinceLastSeen;
    private bool playerCurrentlyVisible;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        currentState = EnemyState.Patrolling;       //Det Default State For Enemy                

        //added stub to find the player as the player is only spawned after the game starts
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                Debug.Log($"{gameObject.name} found player: {player.name}");
                lastKnownPlayerPos = player.position;
                hasLastKnownPlayerPos = true;
            }
            else
            {
                Debug.LogWarning($"{gameObject.name} could not find player! Make sure player has 'Player' tag.");
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (player == null) //No Player Assigned
        {
            return;
        }

        playerCurrentlyVisible = CanSeePlayer();
        UpdateSight(playerCurrentlyVisible);

        // INTEGRATION: Bulleted state execution stays the same, but now visibility is tracked before actions.
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

        HandleRotation();

        StateTransitions(playerCurrentlyVisible);
    }


    //Will Update this so that it works without CanSee (if using only hearning)
    void StateTransitions(bool canSeePlayer)         //Called in Update ( Controls the Transitions between each state.
    {
        if (player == null) return;
        float distance = Vector3.Distance(transform.position, player.position);
        bool playerRecentlySeen = canSeePlayer || (hasLastKnownPlayerPos && timeSinceLastSeen <= chaseMemoryDuration);

        // INTEGRATION: Keep chasing/attacking until we lose the player for chaseMemoryDuration.
        // Previous behaviour would immediately return to patrol as soon as sight was lost and any attack state
        // simply dropped back to chasing without memory.
        if (currentState == EnemyState.Patrolling && canSeePlayer)           //IF Patrolling
        {
            currentState = EnemyState.Chasing;
        }
        else if (currentState == EnemyState.Chasing)                         //If Chasing
        {
            if (distance < attackRange)
            {
                currentState = EnemyState.Attacking;
            }
            else if (!playerRecentlySeen)
            {
                currentState = EnemyState.Patrolling;
                GoToNextPatrolPoint();
            }
        }
        else if (currentState == EnemyState.Attacking)                           //If Attacking
        {
            if (distance > attackRange)
            {
                if (playerRecentlySeen)
                {
                    currentState = EnemyState.Chasing;
                }
                else
                {
                    currentState = EnemyState.Patrolling;
                    GoToNextPatrolPoint();
                }
            }
        }
    }

    private void UpdateSight(bool canSeePlayer)
    {
        if (canSeePlayer)
        {
            lastKnownPlayerPos = player.position;
            hasLastKnownPlayerPos = true;
            timeSinceLastSeen = 0f;
        }
        else
        {
            timeSinceLastSeen += Time.deltaTime;
        }
    }

    private void HandleRotation()
    {
        if (agent == null) return;

        Vector3 direction = agent.velocity;
        if (direction.sqrMagnitude < 0.01f && playerCurrentlyVisible)
        {
            direction = (player.position - transform.position);
        }
        else if (direction.sqrMagnitude < 0.01f && hasLastKnownPlayerPos)
        {
            direction = (lastKnownPlayerPos - transform.position);
        }

        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
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
        Vector3 targetPosition = playerCurrentlyVisible
            ? player.position
            : hasLastKnownPlayerPos ? lastKnownPlayerPos : player.position;

        agent.SetDestination(targetPosition);
        // INTEGRATION: Prefer last known player location when the player is temporarily lost.
        // Previous behaviour only ever chased `player.position`.
        // agent.SetDestination(player.position);
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
