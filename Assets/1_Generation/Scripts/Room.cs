using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using ENDURE;

namespace ENDURE
{
    public class Room : MonoBehaviour
    {
        public Corridor CorridorPrefab;
        public IntVector2 Size;
        public IntVector2 Coordinates;
        public int Num;

        private GameObject _tilesObject;
        private GameObject _wallsObject;
        private GameObject _enemiesObject;
        private GameObject _itemsObject;
        private GameObject _plantPropsObject;
        private GameObject _patrolPointsObject;
        private GameObject _sceneTransitionObject;
        public Tile TilePrefab;
        private Tile[,] _tiles;
        public GameObject WallPrefab;
        public RoomSetting Setting;

        public Dictionary<Room, Corridor> RoomCorridor = new Dictionary<Room, Corridor>();

        private Map _map;

        public GameObject PlayerPrefab;

        [Header("Scene Exit")]
        [Tooltip("Prefab for the scene transition interactable that returns the player to the hub/village.")]
        public GameObject sceneTransitionPrefab;
        [Tooltip("Scene name to load when using the exit. Leave empty to use prefab defaults.")]
        public string sceneTransitionTargetSceneName;
        [Tooltip("Build index override if scene name is empty. Set to -1 to ignore.")]
        public int sceneTransitionTargetSceneBuildIndex = -1;
        [Tooltip("Distance away from the wall when placing the exit interactable.")]
        public float sceneTransitionWallOffset = 1.5f;

        [Header("Enemy Spawning")]
        public GameObject[] enemyPrefabs;
        public int minEnemiesPerRoom = 0;
        public int maxEnemiesPerRoom = 2;

        [Header("Item Spawning")]
        public GameObject[] itemPrefabs;
        public int minItemsPerRoom = 1;
        public int maxItemsPerRoom = 3;

        [Header("Plant Props Spawning")]
        [Tooltip("Decorative plant props to spawn on floor tiles (no colliders)")]
        public GameObject[] plantPropPrefabs;
        [Tooltip("Minimum number of plant props per room")]
        public int minPlantPropsPerRoom = 2;
        [Tooltip("Maximum number of plant props per room")]
        public int maxPlantPropsPerRoom = 6;
        [Tooltip("Foliage density multiplier (0 = no plants, 1 = full density, 2 = double density)")]
        [Range(0f, 2f)]
        public float foliageDensity = 1f;

        [Header("Patrol Points")]
        public int minPatrolPointsPerRoom = 2;
        public int maxPatrolPointsPerRoom = 5;

        public void Init(Map map)
        {
            _map = map;
        }

        public IEnumerator Generate()
        {
            _tilesObject = new GameObject("Tiles");
            _tilesObject.transform.parent = transform;
            _tilesObject.transform.localPosition = Vector3.zero;

            _tiles = new Tile[Size.x, Size.z];
            for (int x = 0; x < Size.x; x++)
            {
                for (int z = 0; z < Size.z; z++)
                {
                    _tiles[x, z] = CreateTile(new IntVector2((Coordinates.x + x), Coordinates.z + z));
                }
            }
            yield return null;
        }

        private Tile CreateTile(IntVector2 coordinates)
        {
            if (_map.GetTileType(coordinates) == TileType.Empty)
            {
                _map.SetTileType(coordinates, TileType.Room);
            }
            else
            {
                Debug.LogError("Tile Conflict!");
            }
            Tile newTile = Instantiate(TilePrefab);
            newTile.Coordinates = coordinates;
            newTile.name = "Tile " + coordinates.x + ", " + coordinates.z;
            newTile.transform.parent = _tilesObject.transform;

            Debug.Log($"Created individual tile: {newTile.name} at position {newTile.transform.position}");
            newTile.transform.localPosition = RoomMapManager.TileSize * new Vector3(coordinates.x - Coordinates.x - Size.x * 0.5f + 0.5f, 0f, coordinates.z - Coordinates.z - Size.z * 0.5f + 0.5f);

            if (Setting != null && Setting.floor != null)
            {
                Material tileMaterial = new Material(Setting.floor);
                Renderer tileRenderer = newTile.transform.GetChild(0).GetComponent<Renderer>();
                if (tileRenderer != null)
                {
                    tileRenderer.material = tileMaterial;
                    Debug.Log($"Applied material {tileMaterial.name} to tile {coordinates} renderer");
                }
                else
                {
                    Debug.LogError($"No renderer found on tile {coordinates} child object!");
                }
            }
            else
            {
                Debug.LogError("Floor material not set!");
            }

            return newTile;
        }

        public Corridor CreateCorridor(Room otherRoom)
        {
            if (RoomCorridor.ContainsKey(otherRoom))
            {
                return RoomCorridor[otherRoom];
            }

            Corridor newCorridor = Instantiate(CorridorPrefab);
            newCorridor.name = "Corridor (" + otherRoom.Num + ", " + Num + ")";
            newCorridor.transform.parent = transform.parent;
            newCorridor.Coordinates = new IntVector2(Coordinates.x + Size.x / 2, otherRoom.Coordinates.z + otherRoom.Size.z / 2);
            newCorridor.transform.localPosition = new Vector3(newCorridor.Coordinates.x - _map.MapSize.x / 2, 0, newCorridor.Coordinates.z - _map.MapSize.z / 2);
            newCorridor.Rooms[0] = otherRoom;
            newCorridor.Rooms[1] = this;
            newCorridor.Length = Vector3.Distance(otherRoom.transform.localPosition, transform.localPosition);
            newCorridor.Init(_map);
            otherRoom.RoomCorridor.Add(this, newCorridor);
            RoomCorridor.Add(otherRoom, newCorridor);

            return newCorridor;
        }

        public IEnumerator CreateWalls()
        {
            _wallsObject = new GameObject("Walls");
            _wallsObject.transform.parent = transform;
            _wallsObject.transform.localPosition = Vector3.zero;

            IntVector2 leftBottom = new IntVector2(Coordinates.x - 1, Coordinates.z - 1);
            IntVector2 rightTop = new IntVector2(Coordinates.x + Size.x, Coordinates.z + Size.z);
            for (int x = leftBottom.x; x <= rightTop.x; x++)
            {
                for (int z = leftBottom.z; z <= rightTop.z; z++)
                {
                    if ((x != leftBottom.x && x != rightTop.x && z != leftBottom.z && z != rightTop.z) ||
                        ((x == leftBottom.x || x == rightTop.x) && (z == leftBottom.z || z == rightTop.z)) ||
                        (_map.GetTileType(new IntVector2(x, z)) != TileType.Wall))
                    {
                        continue;
                    }
                    Quaternion rotation = Quaternion.identity;
                    if (x == leftBottom.x)
                    {
                        rotation = MapDirection.West.ToRotation();
                    }
                    else if (x == rightTop.x)
                    {
                        rotation = MapDirection.East.ToRotation();
                    }
                    else if (z == leftBottom.z)
                    {
                        rotation = MapDirection.South.ToRotation();
                    }
                    else if (z == rightTop.z)
                    {
                        rotation = MapDirection.North.ToRotation();
                    }
                    else
                    {
                        Debug.LogError("Wall is not on appropriate location!!");
                    }

                    GameObject newWall = Instantiate(WallPrefab);
                    newWall.name = "Wall (" + x + ", " + z + ")";
                    newWall.transform.parent = _wallsObject.transform;
                    newWall.transform.localPosition = RoomMapManager.TileSize * new Vector3(x - Coordinates.x - Size.x * 0.5f + 0.5f, 0f, z - Coordinates.z - Size.z * 0.5f + 0.5f);
                    newWall.transform.localRotation = rotation;
                    newWall.transform.localScale *= RoomMapManager.TileSize;

                    if (Setting != null && Setting.wall != null)
                    {
                        newWall.transform.GetChild(0).GetComponent<Renderer>().material = Setting.wall;
                    }
                    else
                    {
                        Debug.LogError("Wall material not set!");
                    }
                }
            }
            yield return null;
        }

        private List<Transform> CreatePatrolPoints()
        {
            _patrolPointsObject = new GameObject("PatrolPoints");
            _patrolPointsObject.transform.parent = transform;
            _patrolPointsObject.transform.localPosition = Vector3.zero;

            int patrolPointCount = Random.Range(minPatrolPointsPerRoom, maxPatrolPointsPerRoom + 1);
            List<Transform> patrolPoints = new List<Transform>();

            float minDistanceBetweenPoints = Mathf.Max(Size.x, Size.z) * 0.25f;
            int maxAttempts = 30;

            for (int i = 0; i < patrolPointCount; i++)
            {
                Vector3 randomLocalPosition = Vector3.zero;
                bool validPositionFound = false;
                int attempts = 0;

                while (!validPositionFound && attempts < maxAttempts)
                {
                    randomLocalPosition = new Vector3(
                        Random.Range(-Size.x * 0.75f, Size.x * 0.75f),
                        0f,
                        Random.Range(-Size.z * 0.75f, Size.z * 0.75f)
                    );

                    validPositionFound = true;

                    foreach (Transform existingPoint in patrolPoints)
                    {
                        float distance = Vector3.Distance(randomLocalPosition, existingPoint.localPosition);
                        if (distance < minDistanceBetweenPoints)
                        {
                            validPositionFound = false;
                            break;
                        }
                    }

                    attempts++;
                }

                if (!validPositionFound && attempts >= maxAttempts)
                {
                    Debug.LogWarning($"Could not find spread position for patrol point {i + 1}, using random position");
                }

                GameObject patrolPoint = new GameObject($"PatrolPoint {i + 1}");
                patrolPoint.transform.parent = _patrolPointsObject.transform;
                patrolPoint.transform.localPosition = randomLocalPosition;

                PatrolPoint patrolComponent = patrolPoint.AddComponent<PatrolPoint>();

                NavMeshHit hit;
                if (NavMesh.SamplePosition(patrolPoint.transform.position, out hit, 15f, NavMesh.AllAreas))
                {
                    patrolPoint.transform.position = hit.position;
                }
                else
                {
                    Debug.LogWarning($"Patrol point {i + 1} could not find NavMesh, keeping at {patrolPoint.transform.position}");
                }

                patrolPoints.Add(patrolPoint.transform);
            }

            Debug.Log($"Created {patrolPointCount} patrol points in room {Num} with spacing ~{minDistanceBetweenPoints:F1} units");
            return patrolPoints;
        }


        public IEnumerator CreateMonsters()
        {
            List<Transform> availablePatrolPoints = CreatePatrolPoints();

            if (enemyPrefabs != null && enemyPrefabs.Length > 0)
            {
                _enemiesObject = new GameObject("Enemies");
                _enemiesObject.transform.parent = transform;
                _enemiesObject.transform.localPosition = Vector3.zero;

                int enemyCount = Random.Range(minEnemiesPerRoom, maxEnemiesPerRoom + 1);

                for (int i = 0; i < enemyCount; i++)
                {
                    GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

                    Vector3 randomLocalPosition = new Vector3(
                        Random.Range(-Size.x * 0.4f, Size.x * 0.4f),
                        0f,
                        Random.Range(-Size.z * 0.4f, Size.z * 0.4f)
                    );

                    GameObject newEnemy = Instantiate(enemyPrefab);
                    newEnemy.name = $"Enemy {i + 1}";
                    newEnemy.transform.parent = _enemiesObject.transform;
                    newEnemy.transform.localPosition = randomLocalPosition;

                    NavMeshAgent agent = newEnemy.GetComponent<NavMeshAgent>();
                    if (agent != null)
                    {
                        agent.enabled = false;

                        Vector3 worldPosition = newEnemy.transform.position;

                        NavMeshHit hit;
                        if (NavMesh.SamplePosition(worldPosition, out hit, 15f, NavMesh.AllAreas))
                        {
                            newEnemy.transform.position = hit.position;
                            Debug.Log($"{newEnemy.name} snapped to NavMesh at {hit.position}");
                        }
                        else
                        {
                            Debug.LogWarning($"{newEnemy.name} could not find nearby NavMesh! Trying center of room...");

                            if (NavMesh.SamplePosition(transform.position, out hit, 20f, NavMesh.AllAreas))
                            {
                                newEnemy.transform.position = hit.position;
                                Debug.Log($"{newEnemy.name} placed at room center NavMesh position");
                            }
                            else
                            {
                                Debug.LogError($"{newEnemy.name} NO NAVMESH FOUND IN ENTIRE ROOM!");
                            }
                        }

                        agent.enabled = true;
                    }

                    EnemyBehaviour enemyBehaviour = newEnemy.GetComponent<EnemyBehaviour>();
                    if (enemyBehaviour != null && availablePatrolPoints.Count > 0)
                    {
                        int pointsToAssign = Mathf.Min(Random.Range(2, 5), availablePatrolPoints.Count);
                        Transform[] assignedPoints = new Transform[pointsToAssign];

                        List<Transform> tempPoints = new List<Transform>(availablePatrolPoints);
                        for (int j = 0; j < pointsToAssign; j++)
                        {
                            int randomIndex = Random.Range(0, tempPoints.Count);
                            assignedPoints[j] = tempPoints[randomIndex];

                            PatrolPoint patrolComp = tempPoints[randomIndex].GetComponent<PatrolPoint>();
                            if (patrolComp != null)
                            {
                                patrolComp.isAssigned = true;
                            }

                            tempPoints.RemoveAt(randomIndex);
                        }

                        enemyBehaviour.patrolPoints = assignedPoints;
                        Debug.Log($"{newEnemy.name} assigned {pointsToAssign} patrol points");
                    }

                    yield return null;
                }
            }

            yield return CreateItems();
            yield return CreatePlantProps();
        }

        private IEnumerator CreateItems()
        {
            if (itemPrefabs != null && itemPrefabs.Length > 0)
            {
                _itemsObject = new GameObject("Items");
                _itemsObject.transform.parent = transform;
                _itemsObject.transform.localPosition = Vector3.zero;

                int itemCount = Random.Range(minItemsPerRoom, maxItemsPerRoom + 1);
                List<Vector3> usedPositions = new List<Vector3>();
                float minDistanceBetweenItems = 1.5f; // Minimum distance between items
                int maxAttempts = 50; // Maximum attempts to find a valid position

                for (int i = 0; i < itemCount; i++)
                {
                    GameObject itemPrefab = itemPrefabs[Random.Range(0, itemPrefabs.Length)];

                    // Position on floor tiles - use actual tile positions for better placement
                    Vector3 localPosition = Vector3.zero;
                    bool validPositionFound = false;
                    int attempts = 0;

                    while (!validPositionFound && attempts < maxAttempts)
                    {
                        if (_tiles != null && Size.x > 0 && Size.z > 0)
                        {
                            // Pick a random tile to place the item on
                            int randomX = Random.Range(0, Size.x);
                            int randomZ = Random.Range(0, Size.z);
                            Tile randomTile = _tiles[randomX, randomZ];
                            
                            if (randomTile != null)
                            {
                                // Get tile's local position relative to room and add random offset
                                Vector3 tileLocalPos = randomTile.transform.localPosition;
                                localPosition = new Vector3(
                                    tileLocalPos.x + Random.Range(-0.4f, 0.4f),
                                    tileLocalPos.y, // On the floor
                                    tileLocalPos.z + Random.Range(-0.4f, 0.4f)
                                );
                            }
                            else
                            {
                                // Fallback to random position in room
                                localPosition = new Vector3(
                                    Random.Range(-Size.x * 0.4f, Size.x * 0.4f),
                                    0f,
                                    Random.Range(-Size.z * 0.4f, Size.z * 0.4f)
                                );
                            }
                        }
                        else
                        {
                            // Fallback if tiles aren't available
                            localPosition = new Vector3(
                        Random.Range(-Size.x * 0.4f, Size.x * 0.4f),
                                0f,
                        Random.Range(-Size.z * 0.4f, Size.z * 0.4f)
                    );
                        }

                        // Check if this position is far enough from other items
                        validPositionFound = true;
                        foreach (Vector3 usedPos in usedPositions)
                        {
                            if (Vector3.Distance(localPosition, usedPos) < minDistanceBetweenItems)
                            {
                                validPositionFound = false;
                                break;
                            }
                        }

                        attempts++;
                    }

                    // Only spawn if we found a valid position
                    if (validPositionFound)
                    {
                    GameObject newItem = Instantiate(itemPrefab);
                    newItem.name = $"Item {i + 1}";
                    newItem.transform.parent = _itemsObject.transform;
                        newItem.transform.localPosition = localPosition;
                        float randomScale = Random.Range(1f, 2f);
                        newItem.transform.localScale = Vector3.one * randomScale;
                        usedPositions.Add(localPosition);
                    }
                    else
                    {
                        Debug.LogWarning($"Could not find valid position for item {i + 1} after {maxAttempts} attempts, skipping.");
                    }
                }
            }

            yield return null;
        }

        private IEnumerator CreatePlantProps()
        {
            if (plantPropPrefabs != null && plantPropPrefabs.Length > 0)
            {
                _plantPropsObject = new GameObject("PlantProps");
                _plantPropsObject.transform.parent = transform;
                _plantPropsObject.transform.localPosition = Vector3.zero;

                // Apply foliage density multiplier - increase base count for denser, wilder look
                int basePlantCount = Random.Range(minPlantPropsPerRoom, maxPlantPropsPerRoom + 1);
                // Multiply by room area to make larger rooms denser, then apply density setting
                float roomArea = Size.x * Size.z;
                float areaMultiplier = Mathf.Clamp(roomArea / 25f, 0.5f, 2f); // Scale based on room size
                int plantCount = Mathf.RoundToInt(basePlantCount * areaMultiplier * foliageDensity);

                for (int i = 0; i < plantCount; i++)
                {
                    GameObject plantPrefab = plantPropPrefabs[Random.Range(0, plantPropPrefabs.Length)];

                    // Wild, unkept positioning - spread across entire room area with more randomness
                    // Use room size to spread plants across the full area
                    float roomWidth = Size.x * RoomMapManager.TileSize;
                    float roomDepth = Size.z * RoomMapManager.TileSize;
                    
                    // Spread plants across 90% of room area with more variation for wild look
                    // Add some clustering tendency for natural distribution
                    float spreadFactor = Random.value < 0.3f ? 0.3f : 0.45f; // 30% chance to cluster more
                    Vector3 localPosition = new Vector3(
                        Random.Range(-roomWidth * spreadFactor, roomWidth * spreadFactor),
                        0f, // On the floor
                        Random.Range(-roomDepth * spreadFactor, roomDepth * spreadFactor)
                    );

                    GameObject newPlant = Instantiate(plantPrefab);
                    newPlant.name = $"PlantProp {i + 1}";
                    newPlant.transform.parent = _plantPropsObject.transform;
                    newPlant.transform.localPosition = localPosition;

                    // Remove all colliders from plant props (allows items to spawn on same tiles)
                    Collider[] colliders = newPlant.GetComponentsInChildren<Collider>();
                    foreach (Collider col in colliders)
                    {
                        Destroy(col);
                    }

                    // Wild, unkept rotation - include tilt for overgrown look
                    float randomYaw = Random.Range(0f, 360f);
                    float randomPitch = Random.Range(-20f, 20f); // Tilt forward/back for wild look
                    float randomRoll = Random.Range(-15f, 15f); // Tilt side to side
                    newPlant.transform.rotation = Quaternion.Euler(randomPitch, randomYaw, randomRoll);

                    // Wild scale variation - much more variation for unkept look, then scale by 3
                    float scaleVariation = Random.Range(0.5f, 1.8f); // Much wider range for wild appearance
                    Vector3 originalScale = plantPrefab.transform.localScale;
                    newPlant.transform.localScale = originalScale * scaleVariation * 3f;
                }
            }

            yield return null;
        }

        public IEnumerator CreatePlayer()
        {
            GameObject player = Instantiate((PlayerPrefab));
            player.name = "Player";
            player.transform.parent = transform.parent;
            player.transform.localPosition = transform.localPosition;

            // Disable player movement/gravity until properly positioned
            var playerController = player.GetComponent<ENDURE.PlayerController>();
            if (playerController != null)
            {
                playerController.canMove = false;
            }

            // Disable CharacterController temporarily to prevent falling
            var characterController = player.GetComponent<CharacterController>();
            if (characterController != null)
            {
                characterController.enabled = false;
            }

            // Wait a frame to ensure player is positioned
            yield return null;

            // Re-enable CharacterController
            if (characterController != null)
            {
                characterController.enabled = true;
            }

            // Wait a bit more to ensure player lands on floor
            yield return new WaitForSeconds(0.1f);

            // Re-enable player movement
            if (playerController != null)
            {
                playerController.canMove = true;
            }

            yield return null;
        }

        public IEnumerator CreateSceneExit()
        {
            if (sceneTransitionPrefab == null)
            {
                Debug.LogWarning($"Room {Num} CreateSceneExit(): sceneTransitionPrefab not assigned.");
                yield break;
            }

            if (_sceneTransitionObject != null)
            {
                yield break;
            }

            _sceneTransitionObject = Instantiate(sceneTransitionPrefab);
            _sceneTransitionObject.name = "SceneExit";
            _sceneTransitionObject.transform.parent = transform;

            float halfWidth = Size.x * RoomMapManager.TileSize * 0.5f;
            float localX = -halfWidth + Mathf.Max(0.5f, sceneTransitionWallOffset);
            Vector3 localPosition = new Vector3(localX, 0f, 0f);
            _sceneTransitionObject.transform.localPosition = localPosition;

            // Snap to NavMesh when available
            NavMeshHit hit;
            if (NavMesh.SamplePosition(_sceneTransitionObject.transform.position, out hit, 5f, NavMesh.AllAreas))
            {
                _sceneTransitionObject.transform.position = hit.position;
                _sceneTransitionObject.transform.localPosition = transform.InverseTransformPoint(hit.position);
            }

            SceneTransitionInteractable interactable = _sceneTransitionObject.GetComponent<SceneTransitionInteractable>();
            if (interactable != null)
            {
                if (!string.IsNullOrEmpty(sceneTransitionTargetSceneName))
                {
                    interactable.targetSceneName = sceneTransitionTargetSceneName;
                }

                if (sceneTransitionTargetSceneBuildIndex >= 0)
                {
                    interactable.targetSceneBuildIndex = sceneTransitionTargetSceneBuildIndex;
                }
            }

            yield return null;
        }
    }
}
