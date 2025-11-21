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

        [Tooltip("Delay after dungeon structure completes before baking")]
        public float bakeDelay = 1f;

        [Header("Layer Settings")]
        [Tooltip("Layers to include in NavMesh (typically Default for floors)")]
        public LayerMask walkableLayers = ~0;

        [Header("Debug")]
        public bool showDebugInfo = true;

        [Header("References")]
        public NavMeshSurface navMeshSurface;
        public RoomMapManager roomMapManager;

        public static bool IsNavMeshReady { get; private set; }

        void Awake()
        {
            IsNavMeshReady = false;

            if (autoSetup && navMeshSurface == null)
            {
                navMeshSurface = GetComponent<NavMeshSurface>();

                if (navMeshSurface == null)
                {
                    navMeshSurface = gameObject.AddComponent<NavMeshSurface>();
                    Debug.Log("DungeonNavMeshManager: Created NavMeshSurface component");
                }
            }
        }

        void Start()
        {
            if (roomMapManager == null)
            {
                roomMapManager = GetComponent<RoomMapManager>();
            }

            if (autoSetup)
            {
                ConfigureNavMeshSurface();
            }

            StartCoroutine(WaitForDungeonAndBake());
        }

        private void ConfigureNavMeshSurface()
        {
            if (navMeshSurface == null) return;

            navMeshSurface.collectObjects = CollectObjects.All;

            navMeshSurface.layerMask = walkableLayers;

            navMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;

            Debug.Log("NavMeshSurface configured to bake entire dungeon");
            Debug.Log($"Collect: PhysicsColliders, Layers: {walkableLayers.value}");
        }

        private IEnumerator WaitForDungeonAndBake()
        {
            Debug.Log("Waiting for dungeon structure to complete...");

            yield return new WaitForSeconds(bakeDelay);

            // CRITICAL: Wait for physics to update so colliders are registered
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            if (showDebugInfo)
            {
                CheckTileColliders();
            }

            BakeNavMesh();
        }

        private void CheckTileColliders()
        {
            Collider[] colliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);
            int meshColliderCount = 0;
            int boxColliderCount = 0;

            foreach (Collider col in colliders)
            {
                if (col is MeshCollider) meshColliderCount++;
                if (col is BoxCollider) boxColliderCount++;
            }

            Debug.Log($"Physics check: Found {colliders.Length} colliders ({meshColliderCount} MeshColliders, {boxColliderCount} BoxColliders)");

            if (colliders.Length == 0)
            {
                Debug.LogError("NO COLLIDERS FOUND! NavMesh will be empty!");
            }
        }

        public void BakeNavMesh()
        {
            if (navMeshSurface == null)
            {
                Debug.LogError("Cannot bake NavMesh: NavMeshSurface is null!");
                return;
            }

            Debug.Log("Baking NavMesh for entire dungeon...");

            navMeshSurface.BuildNavMesh();

            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();

            if (triangulation.vertices.Length > 0)
            {
                IsNavMeshReady = true;

                Debug.Log($"? NavMesh baked successfully! Vertices: {triangulation.vertices.Length}, Triangles: {triangulation.indices.Length / 3}");

                Bounds bounds = new Bounds(triangulation.vertices[0], Vector3.zero);
                foreach (Vector3 vertex in triangulation.vertices)
                {
                    bounds.Encapsulate(vertex);
                }
                Debug.Log($"? NavMesh coverage: {bounds.size.x:F1} x {bounds.size.z:F1} units at center {bounds.center}");
                Debug.Log("? NavMesh is ready - enemies can now spawn!");
            }
            else
            {
                Debug.LogError("? NavMesh baked but has NO vertices! Check:");
                Debug.LogError("  1. Tiles have colliders");
                Debug.LogError("  2. Tiles are on correct layer");
                Debug.LogError("  3. NavMeshSurface layer mask includes tile layer");
            }
        }

        public void ClearNavMesh()
        {
            if (navMeshSurface != null)
            {
                navMeshSurface.RemoveData();
                IsNavMeshReady = false;
                Debug.Log("NavMesh cleared");
            }
        }
    }
}
