using UnityEngine;

namespace ENDURE
{
    /// <summary>
    /// Spawns the player at a specific position in the scene.
    /// Useful for village/dungeon transitions where player needs to spawn at a fixed location.
    /// </summary>
    public class PlayerSpawner : MonoBehaviour
    {
        [Header("Player Prefab")]
        [Tooltip("The player prefab to spawn. If null, will try to find existing player in scene.")]
        public GameObject playerPrefab;

        [Header("Spawn Position")]
        [Tooltip("World position where the player should spawn.")]
        public Vector3 spawnPosition = new Vector3(223.7003f, 1.11f, 218.2149f);

        [Header("Spawn Rotation")]
        [Tooltip("Rotation of the player when spawned.")]
        public Vector3 spawnRotation = Vector3.zero;

        [Header("Spawn Settings")]
        [Tooltip("Spawn player automatically on Start?")]
        public bool spawnOnStart = true;

        [Tooltip("Destroy existing player in scene before spawning?")]
        public bool destroyExistingPlayer = true;

        private void Start()
        {
            if (spawnOnStart)
            {
                SpawnPlayer();
            }
        }

        /// <summary>
        /// Spawns the player at the configured spawn position.
        /// </summary>
        public void SpawnPlayer()
        {
            // Check if player already exists
            GameObject existingPlayer = GameObject.FindGameObjectWithTag("Player");
            if (existingPlayer != null)
            {
                if (destroyExistingPlayer)
                {
                    Debug.Log("PlayerSpawner: Destroying existing player before spawning new one.");
                    Destroy(existingPlayer);
                }
                else
                {
                    Debug.Log("PlayerSpawner: Player already exists, moving to spawn position.");
                    existingPlayer.transform.position = spawnPosition;
                    existingPlayer.transform.rotation = Quaternion.Euler(spawnRotation);
                    return;
                }
            }

            // Spawn new player if prefab is assigned
            if (playerPrefab != null)
            {
                GameObject player = Instantiate(playerPrefab, spawnPosition, Quaternion.Euler(spawnRotation));
                player.name = "Player";
                Debug.Log($"PlayerSpawner: Spawned player at position {spawnPosition}");
            }
            else
            {
                Debug.LogWarning("PlayerSpawner: No player prefab assigned! Cannot spawn player.");
            }
        }

        /// <summary>
        /// Sets the spawn position programmatically.
        /// </summary>
        public void SetSpawnPosition(Vector3 position)
        {
            spawnPosition = position;
        }

        /// <summary>
        /// Sets the spawn rotation programmatically.
        /// </summary>
        public void SetSpawnRotation(Vector3 rotation)
        {
            spawnRotation = rotation;
        }

        // Draw spawn position in editor
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(spawnPosition, 0.5f);
            Gizmos.DrawLine(spawnPosition, spawnPosition + Vector3.up * 2f);
        }
    }
}

