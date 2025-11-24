using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Lightweight bridge that reads EnemyBehaviour state/velocity
/// and drives an Animator without modifying the EnemyBehaviour script.
/// </summary>
[RequireComponent(typeof(Animator))]
public class EnemyAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyBehaviour enemyBehaviour;
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent navMeshAgent;

    [Header("Animator Parameters")]
    [Tooltip("Float parameter used to blend idle/walk/run (value: movement speed).")]
    [SerializeField] private string locomotionParam = "Locomotion";
    [Tooltip("Bool parameter set true while the enemy is in the attack state.")]
    [SerializeField] private string attackingBoolParam = "IsAttacking";
    [Tooltip("Trigger fired once when entering the attack state (optional).")]
    [SerializeField] private string attackTriggerParam = "Attack";
    [Tooltip("Bool parameter set true when the enemy is moving (walk/run).")]
    [SerializeField] private string movingBoolParam = "IsMoving";

    [Header("Speed Thresholds")]
    [SerializeField] private float movingThreshold = 0.1f;
    [SerializeField] private float maxSpeedForBlend = 4f;

    private EnemyBehaviour.EnemyState previousState;

    private void Reset()
    {
        enemyBehaviour = GetComponent<EnemyBehaviour>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    private void Awake()
    {
        if (enemyBehaviour == null)
        {
            enemyBehaviour = GetComponent<EnemyBehaviour>();
        }

        if (navMeshAgent == null)
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        previousState = enemyBehaviour != null ? enemyBehaviour.currentState : EnemyBehaviour.EnemyState.Patrolling;
    }

    private void Update()
    {
        if (enemyBehaviour == null || animator == null)
        {
            return;
        }

        float speed = navMeshAgent != null ? navMeshAgent.velocity.magnitude : 0f;
        float normalizedSpeed = maxSpeedForBlend > 0.001f ? Mathf.Clamp01(speed / maxSpeedForBlend) : speed;

        if (!string.IsNullOrEmpty(locomotionParam))
        {
            animator.SetFloat(locomotionParam, normalizedSpeed);
        }

        if (!string.IsNullOrEmpty(movingBoolParam))
        {
            animator.SetBool(movingBoolParam, speed > movingThreshold);
        }

        bool isAttacking = enemyBehaviour.currentState == EnemyBehaviour.EnemyState.Attacking;
        if (!string.IsNullOrEmpty(attackingBoolParam))
        {
            animator.SetBool(attackingBoolParam, isAttacking);
        }

        if (isAttacking && previousState != EnemyBehaviour.EnemyState.Attacking && !string.IsNullOrEmpty(attackTriggerParam))
        {
            animator.SetTrigger(attackTriggerParam);
        }

        previousState = enemyBehaviour.currentState;
    }
}

