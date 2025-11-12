using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using System.Collections;

namespace ENDURE
{
    public class DungeonNavMeshManager : MonoBehaviour
    {
        [Header("NavMesh Configuration")]
        [Tooltip("Automatically create NavMeshSurface if not present")]
        public bool autoSetup = true;

        [Tooltip("Delay after dungeon generation before baking")]
        public float bakeDelay = 1f;

        [Header("NavMesh Agent Settings")]
        [Tooltip("Agent radius - should match enemy collider size")]
        public float agentRadius = 0.5f;

        [Tooltip("Agent height - should match enemy height")]
        public float agentHeight = 2f;

        [Tooltip("Max slope the agent can walk on")]
        public float maxSlope = 45f;

        [Tooltip("Max vertical step height the agent can climb")]
        public float stepHeight = 0.4f;

        [Header("Dungeon Size")]
        [Tooltip("Should match your map size X (100 for your dungeon)")]
        public int dungeonSizeX = 100;

        [Tooltip("Should match your map size Z (100 for your dungeon)")]
        public int dungeonSizeZ = 100;

        [Tooltip("Extra padding around dungeon bounds")]
        public float boundsPadding = 10f;

        [Header("Layer Settings")]
        [Tooltip("Layers to include in NavMesh (typically Default for floors)")]
        public LayerMask walkableLayers = ~0;

        [Header("References")]
        public NavMeshSurface navMeshSurface;
        public RoomMapManager roomMapManager;

        private bool isNavMeshBaked = false;

        void Awake()
        {
            if (autoSetup)
            {
                SetupNavMeshSurface();
            }
        }

        void Start()
        {
            if (roomMapManager == null)
            {
                roomMapManager = GetComponent<RoomMapManager>();
            }

            StartCoroutine(WaitForGenerationAndBake());
        }

        private void SetupNavMeshSurface()
        {
            if (navMeshSurface == null)
            {
                navMeshSurface = GetComponent<NavMeshSurface>();

                if (navMeshSurface == null)
                {
                    navMeshSurface = gameObject.AddComponent<NavMeshSurface>();
                    Debug.Log("DungeonNavMeshManager: Created NavMeshSurface component");
                }
            }

            ConfigureNavMeshSurface();
        }

        private void ConfigureNavMeshSurface()
        {
            if (navMeshSurface == null) return;

            navMeshSurface.collectObjects = CollectObjects.Volume;

            float sizeX = dungeonSizeX * RoomMapManager.TileSize + boundsPadding * 2;
            float sizeZ = dungeonSizeZ * RoomMapManager.TileSize + boundsPadding * 2;

            navMeshSurface.size = new Vector3(sizeX, 20f, sizeZ);
            navMeshSurface.center = Vector3.zero;

            navMeshSurface.layerMask = walkableLayers;

            navMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;

            Debug.Log($"NavMeshSurface configured for {dungeonSizeX}x{dungeonSizeZ} dungeon");
            Debug.Log($"NavMesh bounds: {navMeshSurface.size} at center {navMeshSurface.center}");
        }

        private IEnumerator WaitForGenerationAndBake()
        {
            Debug.Log("Waiting for dungeon generation to complete...");

            yield return new WaitForSeconds(bakeDelay);

            BakeNavMesh();
        }

        public void BakeNavMesh()
        {
            if (navMeshSurface == null)
            {
                Debug.LogError("Cannot bake NavMesh: NavMeshSurface is null!");
                return;
            }

            Debug.Log("Baking NavMesh for 100x100 dungeon...");

            navMeshSurface.BuildNavMesh();

            isNavMeshBaked = true;

            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            Debug.Log($"NavMesh baked successfully! Vertices: {triangulation.vertices.Length}, Triangles: {triangulation.indices.Length / 3}");
        }

        public void ClearNavMesh()
        {
            if (navMeshSurface != null)
            {
                navMeshSurface.RemoveData();
                isNavMeshBaked = false;
                Debug.Log("NavMesh cleared");
            }
        }

        public bool IsNavMeshReady()
        {
            return isNavMeshBaked && navMeshSurface != null && navMeshSurface.navMeshData != null;
        }

        void OnDrawGizmosSelected()
        {
            if (navMeshSurface != null)
            {
                Gizmos.color = new Color(0, 1, 0, 0.3f);
                Gizmos.DrawWireCube(navMeshSurface.center, navMeshSurface.size);

                Gizmos.color = Color.green;
                Vector3 labelPos = navMeshSurface.center + Vector3.up * (navMeshSurface.size.y * 0.5f + 2f);

#if UNITY_EDITOR
                UnityEditor.Handles.Label(labelPos, $"NavMesh Volume\n{navMeshSurface.size.x}x{navMeshSurface.size.z}");
#endif
            }
        }
    }
}
