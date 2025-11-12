using UnityEngine;

public class EnemyAttackHandler : MonoBehaviour
{
    [Header("Combat Settings")]
    public float attackDamage = 10f;
    public float attackCooldown = 1.5f;

    private EnemyBehaviour enemyBehaviour;
    private Transform player;
    private ENDURE.CharacterManager playerCharacterManager;
    private float lastAttackTime;
    private EnemyBehaviour.EnemyState lastState;

    void Start()
    {
        enemyBehaviour = GetComponent<EnemyBehaviour>();

        if (enemyBehaviour == null)
        {
            Debug.LogError($"{gameObject.name} has EnemyAttackHandler but no EnemyBehaviour!");
            enabled = false;
            return;
        }

        if (enemyBehaviour.player == null)
        {
            var playerController = FindFirstObjectByType<ENDURE.PlayerController>();
            if (playerController != null)
            {
                enemyBehaviour.player = playerController.transform;
                Debug.Log($"{gameObject.name} auto-found player");
            }
        }

        if (enemyBehaviour.player != null)
        {
            player = enemyBehaviour.player;
            playerCharacterManager = player.GetComponent<ENDURE.CharacterManager>();
        }
    }

    void Update()
    {
        if (enemyBehaviour == null || player == null) return;

        if (enemyBehaviour.currentState == EnemyBehaviour.EnemyState.Attacking)
        {
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                PerformAttack();
                lastAttackTime = Time.time;
            }
        }

        lastState = enemyBehaviour.currentState;
    }

    private void PerformAttack()
    {
        if (playerCharacterManager != null)
        {
            playerCharacterManager.TakeDamage(attackDamage);
            Debug.Log($"{gameObject.name} attacked player for {attackDamage} damage!");
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} tried to attack but player has no CharacterManager!");
        }
    }
}
