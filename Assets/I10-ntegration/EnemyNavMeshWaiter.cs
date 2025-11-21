using UnityEngine;
using UnityEngine.AI;

public class EnemyNavMeshWaiter : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Max time to wait for NavMesh before giving up")]
    public float maxWaitTime = 5f;

    [Tooltip("How often to check if NavMesh is ready")]
    public float checkInterval = 0.2f;

    private NavMeshAgent agent;
    private EnemyBehaviour enemyBehaviour;
    private float waitTimer = 0f;
    private bool isActivated = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        enemyBehaviour = GetComponent<EnemyBehaviour>();

        if (agent != null)
        {
            agent.enabled = false;
        }

        if (enemyBehaviour != null)
        {
            enemyBehaviour.enabled = false;
        }

        InvokeRepeating(nameof(CheckNavMesh), 0.5f, checkInterval);
    }

    void CheckNavMesh()
    {
        waitTimer += checkInterval;

        if (waitTimer > maxWaitTime)
        {
            Debug.LogWarning($"{gameObject.name}: Timed out waiting for NavMesh!");
            CancelInvoke(nameof(CheckNavMesh));
            ActivateEnemy();
            return;
        }

        if (IsOnNavMesh())
        {
            CancelInvoke(nameof(CheckNavMesh));
            ActivateEnemy();
        }
    }

    bool IsOnNavMesh()
    {
        if (agent == null) return false;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 2f, NavMesh.AllAreas))
        {
            return true;
        }

        return false;
    }

    void ActivateEnemy()
    {
        if (isActivated) return;

        if (agent != null)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 5f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
            }

            agent.enabled = true;
        }

        if (enemyBehaviour != null)
        {
            enemyBehaviour.enabled = true;
        }

        isActivated = true;
        Debug.Log($"{gameObject.name}: Activated on NavMesh!");
    }
}
