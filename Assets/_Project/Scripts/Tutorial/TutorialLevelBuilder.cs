using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ENDURE
{
    /// <summary>
    /// Builds a handcrafted tutorial experience using the existing runtime systems
    /// (tiles, plant props, items, enemies, etc). Attach this to an empty GameObject
    /// inside the tutorial scene and click the context menu to spawn all sections.
    /// </summary>
    public class TutorialLevelBuilder : MonoBehaviour
    {
        [System.Serializable]
        public class TutorialZone
        {
            public string name = "Zone";
            public Vector3 worldOrigin;
            public Vector2Int gridSize = new Vector2Int(6, 6);
            public Vector2Int gridCoordinateOffset = Vector2Int.zero;
            public bool generateWalls = true;
            public bool useDegradingTiles;
            public float walkDegrade = 0.1f;
            public float runDegrade = 0.25f;
            public int plantProps = 0;
            public int itemCount = 0;
            [Tooltip("Optional enemy prefab to spawn inside this zone.")]
            public GameObject enemyPrefab;
            public Vector3 enemyLocalOffset = Vector3.zero;
            [TextArea]
            public string instructionText;
            public Vector3 instructionOffset = new Vector3(0, 2f, 0);
        }

        [Header("Core Prefabs & Settings")]
        public Tile tilePrefab;
        public GameObject wallPrefab;
        public GameObject[] plantPrefabs;
        public GameObject[] itemPrefabs;
        public float tileSpacing = 1.0f;
        public float propHeightOffset = 0.45f;
        public bool autoBuildOnStart = false;

        [Header("Tutorial Layout")]
        public List<TutorialZone> zones = new List<TutorialZone>();

        private readonly List<GameObject> spawnedObjects = new List<GameObject>();

        private void Start()
        {
            if (autoBuildOnStart)
            {
                BuildTutorialLevel();
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Build Tutorial Level")]
        private void ContextBuild()
        {
            if (!Application.isPlaying)
            {
                BuildTutorialLevel();
            }
            else
            {
                Debug.LogWarning("Stop play mode before using the context build tool.");
            }
        }
#endif

        public void BuildTutorialLevel()
        {
            ClearSpawnedObjects();
            if (tilePrefab == null)
            {
                Debug.LogError("TutorialLevelBuilder: TilePrefab is not assigned.");
                return;
            }

            // Ensure tile size matches the tutorial grid spacing so Tile distance checks work.
            RoomMapManager.TileSize = Mathf.RoundToInt(Mathf.Max(0.5f, tilePrefab.transform.localScale.x));

            int zoneIndex = 0;
            foreach (var zone in zones)
            {
                BuildZone(zone, zoneIndex);
                zoneIndex++;
#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
            }

            Debug.Log($"TutorialLevelBuilder: Spawned {zones.Count} zones.");
        }

        private void BuildZone(TutorialZone zone, int zoneIndex)
        {
            Transform zoneRoot = new GameObject($"TutorialZone_{zoneIndex:00}_{zone.name}").transform;
            zoneRoot.SetParent(transform);
            zoneRoot.position = zone.worldOrigin;
            spawnedObjects.Add(zoneRoot.gameObject);

            // Build floor tiles
            for (int x = 0; x < zone.gridSize.x; x++)
            {
                for (int z = 0; z < zone.gridSize.y; z++)
                {
                    Vector3 localPos = new Vector3(x * tileSpacing, 0f, z * tileSpacing);
                    Tile tile = Instantiate(tilePrefab, zoneRoot);
                    tile.transform.localPosition = localPos;
                    tile.transform.localRotation = Quaternion.identity;
                    tile.transform.localScale = tilePrefab.transform.localScale;

                    int coordX = zone.gridCoordinateOffset.x + x;
                    int coordZ = zone.gridCoordinateOffset.y + z;
                    tile.Coordinates = new IntVector2(coordX, coordZ);
                    tile.walkDegradationAmount = zone.useDegradingTiles ? Mathf.Max(0f, zone.walkDegrade) : 0f;
                    tile.runDegradationAmount = zone.useDegradingTiles ? Mathf.Max(zone.walkDegrade, zone.runDegrade) : 0f;
                }
            }

            // Optional walls around the perimeter
            if (wallPrefab != null && zone.generateWalls)
            {
                SpawnWalls(zone, zoneRoot);
            }

            // Spawn plant props
            if (zone.plantProps > 0 && plantPrefabs != null && plantPrefabs.Length > 0)
            {
                SpawnProps(zone, zoneRoot, plantPrefabs, zone.plantProps, "PlantProp");
            }

            // Spawn items
            if (zone.itemCount > 0 && itemPrefabs != null && itemPrefabs.Length > 0)
            {
                SpawnProps(zone, zoneRoot, itemPrefabs, zone.itemCount, "Item");
            }

            // Spawn optional enemy
            if (zone.enemyPrefab != null)
            {
                GameObject enemy = Instantiate(zone.enemyPrefab, zoneRoot);
                enemy.name = $"Enemy_{zone.name}";
                Vector3 enemyPos = zone.worldOrigin + zone.enemyLocalOffset;
                enemy.transform.position = enemyPos;
                spawnedObjects.Add(enemy);
            }

            // Spawn instruction text (simple TextMesh)
            if (!string.IsNullOrEmpty(zone.instructionText))
            {
                GameObject textGO = new GameObject($"Instruction_{zone.name}");
                textGO.transform.SetParent(zoneRoot);
                textGO.transform.localPosition = zone.instructionOffset;
                var textMesh = textGO.AddComponent<TextMesh>();
                textMesh.text = zone.instructionText;
                        textMesh.fontSize = 32;
                textMesh.anchor = TextAnchor.MiddleCenter;
                textMesh.color = Color.white;
                spawnedObjects.Add(textGO);
            }
        }

        private void SpawnWalls(TutorialZone zone, Transform zoneRoot)
        {
            float width = zone.gridSize.x * tileSpacing;
            float depth = zone.gridSize.y * tileSpacing;
            float halfHeight = wallPrefab.transform.localScale.y * 0.5f;

            Vector3 origin = zoneRoot.position;

            // Create simple rectangular boundary using supplied wall prefab
            for (int i = 0; i <= zone.gridSize.x; i++)
            {
                float x = origin.x + i * tileSpacing;
                float zMin = origin.z;
                float zMax = origin.z + depth;

                SpawnWall(new Vector3(x, origin.y + halfHeight, zMin - tileSpacing * 0.5f), Vector3.one, zoneRoot);
                SpawnWall(new Vector3(x, origin.y + halfHeight, zMax - tileSpacing * 0.5f), Vector3.one, zoneRoot);
            }

            for (int j = 0; j <= zone.gridSize.y; j++)
            {
                float z = origin.z + j * tileSpacing;
                float xMin = origin.x;
                float xMax = origin.x + width;

                SpawnWall(new Vector3(xMin - tileSpacing * 0.5f, origin.y + halfHeight, z), Vector3.one, zoneRoot);
                SpawnWall(new Vector3(xMax - tileSpacing * 0.5f, origin.y + halfHeight, z), Vector3.one, zoneRoot);
            }
        }

        private void SpawnWall(Vector3 worldPosition, Vector3 scale, Transform parent)
        {
            if (wallPrefab == null) return;

            GameObject wall = Instantiate(wallPrefab, parent);
            wall.transform.position = worldPosition;
            wall.transform.localScale = new Vector3(scale.x, scale.y, scale.z);
            spawnedObjects.Add(wall);
        }

        private void SpawnProps(TutorialZone zone, Transform zoneRoot, GameObject[] prefabPool, int count, string prefix)
        {
            if (prefabPool == null || prefabPool.Length == 0) return;

            for (int i = 0; i < count; i++)
            {
                GameObject prefab = prefabPool[Random.Range(0, prefabPool.Length)];
                GameObject instance = Instantiate(prefab, zoneRoot);
                instance.name = $"{prefix}_{zone.name}_{i + 1}";

                float x = Random.Range(0f, zone.gridSize.x * tileSpacing);
                float z = Random.Range(0f, zone.gridSize.y * tileSpacing);

                instance.transform.localPosition = new Vector3(x, propHeightOffset, z);
                instance.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                float randomScale = Random.Range(0.9f, 1.4f);
                instance.transform.localScale = prefab.transform.localScale * randomScale;

                // Remove colliders from plant props if they shouldn't block the player.
                foreach (var col in instance.GetComponentsInChildren<Collider>())
                {
                    DestroyImmediate(col);
                }

                spawnedObjects.Add(instance);
            }
        }

        private void ClearSpawnedObjects()
        {
            for (int i = spawnedObjects.Count - 1; i >= 0; i--)
            {
                if (spawnedObjects[i] != null)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        DestroyImmediate(spawnedObjects[i]);
                    }
                    else
                    {
                        Destroy(spawnedObjects[i]);
                    }
#else
                    Destroy(spawnedObjects[i]);
#endif
                }
            }

            spawnedObjects.Clear();

            // Clean up existing children under the builder root (in case level was hand-built before).
            var toRemove = new List<GameObject>();
            foreach (Transform child in transform)
            {
                toRemove.Add(child.gameObject);
            }

            foreach (var child in toRemove)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(child);
                }
                else
                {
                    Destroy(child);
                }
#else
                Destroy(child);
#endif
            }
        }
    }
}

