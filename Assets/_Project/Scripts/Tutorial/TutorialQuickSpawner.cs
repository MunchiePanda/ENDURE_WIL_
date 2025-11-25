using System.Collections.Generic;
using UnityEngine;

namespace ENDURE.Tutorial
{
    /// <summary>
    /// Quick utility to scatter test enemies and items inside the current room while keeping distance from the player.
    /// Attach this to an empty GameObject in the tutorial scene.
    /// </summary
    public class TutorialQuickSpawner : MonoBehaviour
    {
        [Header("References")]
        public RoomMapManager roomManager;
        public Transform player;
        public TutorialManager tutorialManager;

        [Header("Prefabs")]
        public GameObject tutorialGiverPrefab;
        public List<GameObject> enemyPrefabs = new List<GameObject>();
        public List<GameObject> itemPrefabs = new List<GameObject>();
        public List<GameObject> craftingPrefabs = new List<GameObject>();

        [Header("Counts")]
        public int enemyCount = 3;
        public int itemCount = 5;
        public int craftingCount = 1;

        [Header("Placement Rules")]
        public float minimumDistanceFromPlayer = 5f;
        public LayerMask groundMask = Physics.DefaultRaycastLayers;
        public float groundRayHeight = 20f;

        [Header("Randomization")]
        public int maxPlacementAttempts = 25;

        private float halfWidth;
        private float halfLength;
        private Vector3 areaCenter;

        private void Awake()
        {
            if (roomManager == null)
            {
                roomManager = FindObjectOfType<RoomMapManager>();
            }

            if (player == null)
            {
                var controller = FindObjectOfType<PlayerController>();
                if (controller != null)
                {
                    player = controller.transform;
                }
            }

            if (tutorialManager == null)
            {
                tutorialManager = FindObjectOfType<TutorialManager>();
            }

            if (roomManager != null)
            {
                float tileSize = Mathf.Max(1, RoomMapManager.TileSize);
                halfWidth = roomManager.MapSizeX * 0.5f * tileSize;
                halfLength = roomManager.MapSizeZ * 0.5f * tileSize;
                areaCenter = roomManager.transform.position;
            }
            else
            {
                halfWidth = halfLength = 10f;
                areaCenter = transform.position;
            }
        }

        private void Start()
        {
            SpawnTutorialGiverNearPlayer();

            // Always ensure at least one of each category is spawned when prefabs exist.
            SpawnPrefabs(itemPrefabs, Mathf.Max(itemCount, 1), "Item");
            SpawnPrefabs(craftingPrefabs, Mathf.Max(craftingCount, 1), "Crafting");
            SpawnPrefabs(enemyPrefabs, Mathf.Max(enemyCount, 1), "Enemy");
        }

        private void SpawnTutorialGiverNearPlayer()
        {
            if (tutorialGiverPrefab == null || player == null) return;

            Vector3 playerPos = player.position;
            Vector3 spawnOffset = player.forward * 3f + player.right * 1.5f;
            Vector3 spawnPoint = playerPos + spawnOffset;

            Ray ray = new Ray(spawnPoint + Vector3.up * groundRayHeight, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, groundRayHeight * 2f, groundMask, QueryTriggerInteraction.Ignore))
            {
                spawnPoint = hit.point;
            }

            var giver = Instantiate(tutorialGiverPrefab, spawnPoint, Quaternion.LookRotation(player.forward), transform);

            var interactable = giver.GetComponentInChildren<TutorialGiverInteractable>();
            var manager = tutorialManager != null ? tutorialManager : FindObjectOfType<TutorialManager>();

            if (interactable != null && manager != null)
            {
                interactable.tutorialManager = manager;
            }
        }

        private void SpawnPrefabs(IList<GameObject> list, int count, string label)
        {
            if (list == null || list.Count == 0 || count <= 0)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                Vector3 spawnPosition;
                if (!TryGetSpawnPosition(out spawnPosition))
                {
                    Debug.LogWarning($"TutorialQuickSpawner: Could not find valid {label} spawn after {maxPlacementAttempts} tries.");
                    continue;
                }

                GameObject prefab = list[Random.Range(0, list.Count)];
                if (prefab == null)
                {
                    continue;
                }

                Instantiate(prefab, spawnPosition, Quaternion.identity, transform);
            }
        }

        private bool TryGetSpawnPosition(out Vector3 position)
        {
            position = Vector3.zero;
            for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
            {
                float x = Random.Range(-halfWidth, halfWidth);
                float z = Random.Range(-halfLength, halfLength);

                Vector3 candidate = areaCenter + new Vector3(x, groundRayHeight, z);

                if (player != null && Vector3.Distance(candidate, player.position) < minimumDistanceFromPlayer)
                {
                    continue;
                }

                Ray ray = new Ray(candidate, Vector3.down);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, groundRayHeight * 2f, groundMask, QueryTriggerInteraction.Ignore))
                {
                    position = hit.point;
                    return true;
                }
            }

            return false;
        }
    }
}

