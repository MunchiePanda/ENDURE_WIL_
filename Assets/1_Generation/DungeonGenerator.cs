using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is like a master builder for our dungeon!
// It uses the TileMatrix brain to figure out the map, then builds it with actual game pieces.
public class DungeonGenerator : MonoBehaviour
{
    [Header("Map Settings - How big our map is and how rooms are made!")]
    [SerializeField] private int rows = 80; // How many squares tall our map is
    [SerializeField] private int cols = 80; // How many squares wide our map is
    [SerializeField] private int minRoomSize = 10; // The smallest room we can make
    [SerializeField] private int maxRoomSize = 16; // The biggest room we can make
    [SerializeField] private int roomCount = 40;   // How many rooms we want to try and make
    [SerializeField] private float tileSize = 10.0f; // How big each square is in the game world (e.g., 10 units wide)

    [Header("Dungeon Pieces - What our dungeon is made of!")]
    [SerializeField] private GameObject floorPrefab;          // The piece we use for the floor
    [SerializeField] private GameObject wallPrefab;           // The piece we use for the walls
    [SerializeField] private GameObject roofPrefab;           // The piece we use for the roof
    [SerializeField] private float wallHeight = 10.0f; // How tall our walls are in the game world

    private TileMatrix tileMatrix; // This is the map-making brain we talked about earlier!
    private List<GameObject> spawnedObjects = new List<GameObject>(); // We'll keep track of all the pieces we put in the dungeon
    private HashSet<string> spawnedWallKeys = new HashSet<string>(); // To keep track of unique walls and prevent duplicates
    private Vector2Int activeExitPoint = new Vector2Int(-1, -1); // Where the player came from to get here
    private bool isGenerating = false; // A switch to know if we are busy building the dungeon

    // This happens when the game first starts, like getting our tools ready!
    void Awake()
    {
        tileMatrix = new TileMatrix(); // We get our map-making brain ready
    }

    // This is like a button that automatically builds the dungeon when the game starts!
    void Start()
        {
        GenerateDungeon();
    }

    // This is like pressing the "Build Dungeon!" button!
    public void GenerateDungeon()
    {
        if (isGenerating) return; // If we're already building, let's not start over!

        ClearDungeon(); // First, clear away any old dungeon pieces

        // Clear the wall keys at the start of a new generation
        spawnedWallKeys.Clear();

        // Make sure our map is big enough for the starting area
        if (rows < 20 || cols < 20)
        {
            Debug.LogWarning("Grid must be at least 20x20 for hub!");
            return;
        }

        tileMatrix.InitializeTileMap(rows, cols); // Tell the map brain how big the map is
        tileMatrix.SetRoomSize(minRoomSize, maxRoomSize); // Tell the map brain how big rooms should be

        // Create the rooms on our map!
        // We'll use a default exit point for now, maybe the center of our hub.
        tileMatrix.CreateRooms(roomCount, new Vector2Int(5, 40)); // Example exit point, you can change this!

        SpawnDungeonObjects(); // Now, use the map to put actual pieces in the game!
        tileMatrix.PrintDebugTileMap(); // Show us a little picture of our map in the console
    }

    // This is like building a new part of the dungeon when the player goes through a special door!
    public void GenerateDungeonFromDoor(Vector2Int exitPoint)
    {
        if (isGenerating) // If we're busy, we wait
        {
            Debug.LogWarning("Generation already in progress!");
            return;
        }

        isGenerating = true; // We start building!
        activeExitPoint = exitPoint; // Remember where the player came from
        
        StartCoroutine(BeginAsyncGeneration(exitPoint)); // Start building in a slow, careful way
    }

    // This helps us build the dungeon without making the game freeze for a moment!
    private IEnumerator BeginAsyncGeneration(Vector2Int exitPoint)
    {
        ClearDungeon(); // Clear away old dungeon pieces first
        
        if (rows < 20 || cols < 20)
        {
            Debug.LogWarning("Grid must be at least 20x20 for hub!");
            isGenerating = false;
            yield break; // Stop here if map is too small
        }

        tileMatrix.InitializeTileMap(rows, cols); // Tell the map brain how big the map is
        tileMatrix.SetRoomSize(minRoomSize, maxRoomSize); // Tell the map brain how big rooms should be
        tileMatrix.CreateRooms(roomCount, exitPoint); // Create rooms, starting from the exit door!
        tileMatrix.PrintDebugTileMap(); // Show us our new map!

        yield return null; // Take a short break so the game doesn't freeze

        SpawnDungeonObjects(); // Put all the dungeon pieces in the game!

        isGenerating = false; // We're done building!
        // Maybe tell other scripts that the dungeon is ready!
    }

    // This cleans up all the old dungeon pieces, like tidying up our toys!
    public void ClearDungeon()
    {
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null) Destroy(obj); // Make each piece disappear
        }
        spawnedObjects.Clear(); // Empty our list of pieces

        spawnedWallKeys.Clear(); // Also clear the unique wall tracker

        activeExitPoint = new Vector2Int(-1, -1); // Forget the old exit point
        isGenerating = false; // We are not building anymore
    }

    // This is where we actually put the game pieces in the world!
    private void SpawnDungeonObjects()
    {
        // Put down all the floor tiles!
        List<Vector2Int> floorTiles = tileMatrix.GetFloorTilePositions();
        foreach (Vector2Int tilePos in floorTiles)
        {
            Vector3 worldPos = TileToWorldPosition(tilePos);
            InstantiateAndAdd(floorPrefab, worldPos); // Place the floor

            // Now, let's put a roof above each floor piece!
            // The roof should be positioned at `floor.y + wallHeight + (roofPrefab.localScale.y * 0.5f)`
            // Assuming floorPrefab.transform.localScale.y is the height of the floor
            float floorHeightOffset = floorPrefab.transform.localScale.y * 0.5f; // Assuming pivot is center for floor
            float roofOffset = wallHeight + (roofPrefab.transform.localScale.y * 0.5f); // Half roof height to center it
            InstantiateAndAdd(roofPrefab, worldPos + new Vector3(0, roofOffset + floorHeightOffset, 0));
        }

        // Put up all the wall tiles!
        List<WallSpawnData> wallSpawnPoints = tileMatrix.GetWallSpawnPoints();
        foreach (WallSpawnData wallData in wallSpawnPoints)
        {
            Vector3 worldPos = TileToWorldPosition(wallData.TilePosition);
            Quaternion rotation = Quaternion.identity;
            Vector3 offset = Vector3.zero;

            // For a wall prefab with scale (1,1,10) that is 1 unit thick and 10 units long (along its local Z-axis),
            // and a tileSize of 10.0f, the wall needs to be shifted by half a tileSize in the correct direction.
            // The wall's pivot is usually at its center.
            float wallThicknessOffset = 0.5f; // Assuming wall prefab is 1 unit thick, and pivot is center

            switch (wallData.Direction)
            {
                case WallDirection.North:
                    // Wall needs to be along the X-axis (its local Z is world Z), shifted by 0.5 * tileSize in positive Z
                    offset = new Vector3(0, wallHeight * 0.5f, tileSize * 0.5f); // Centered vertically, at Z-edge
                    rotation = Quaternion.Euler(0, 0, 0); // No rotation (default for Z-axis wall, assuming prefab long axis is Z)
                    break;
                case WallDirection.South:
                    // Wall needs to be along the X-axis (its local Z is world Z), shifted by 0.5 * tileSize in negative Z
                    offset = new Vector3(0, wallHeight * 0.5f, -tileSize * 0.5f); // Centered vertically, at Z-edge
                    rotation = Quaternion.Euler(0, 0, 0); // No rotation (default for Z-axis wall)
                    break;
                case WallDirection.West:
                    // Wall needs to be along the Z-axis (its local Z is world X), shifted by 0.5 * tileSize in negative X
                    offset = new Vector3(-tileSize * 0.5f, wallHeight * 0.5f, 0); // Centered vertically, at X-edge
                    rotation = Quaternion.Euler(0, 90, 0); // Rotate 90 degrees for X-axis wall
                    break;
                case WallDirection.East:
                    // Wall needs to be along the Z-axis (its local Z is world X), shifted by 0.5 * tileSize in positive X
                    offset = new Vector3(tileSize * 0.5f, wallHeight * 0.5f, 0); // Centered vertically, at X-edge
                    rotation = Quaternion.Euler(0, 90, 0); // Rotate 90 degrees for X-axis wall
                    break;
            }
            
            Vector3 finalWallPosition = worldPos + offset;
            // Use integer tile coordinate plus direction to ensure stable uniqueness
            string wallKey = wallData.TilePosition.x + "," + wallData.TilePosition.y + "," + (int)wallData.Direction;

            if (!spawnedWallKeys.Contains(wallKey))
            {
                InstantiateAndAdd(wallPrefab, finalWallPosition, rotation);
                spawnedWallKeys.Add(wallKey);
            }
        }
    }

    // This helps us turn our map squares (like 1, 2) into actual places in the game (like 10, 20)
    private Vector3 TileToWorldPosition(Vector2Int tile)
    {
        // Unity's Y-axis is often 'up', so we'll use X for columns and Z for rows
        // Remember to multiply by tileSize to get actual world units
        // We also offset by half a tile size to center the floor prefabs if they are 1x1 unit and tileSize is their extent.
        // If floor prefab is already 10x1x10 (with scale 10,1,10), it's centered on integer world coords.
        return new Vector3(tile.y * tileSize, 0, tile.x * tileSize);
    }

    // This helps us create new game objects and remember them for cleaning up later
    private GameObject InstantiateAndAdd(GameObject prefab, Vector3 position, Quaternion rotation = default)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"Prefab is null at position: {position}");
            return null;
        }
        GameObject obj = Instantiate(prefab, position, rotation);
        spawnedObjects.Add(obj);
        return obj;
    }
}
