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
    [SerializeField] private GameObject treasureChestPrefab;  // A cool chest to find!
    [SerializeField] private GameObject torchPrefab;          // Light up the dungeon!
    [SerializeField] private GameObject techScrapPrefab;      // Shiny bits to collect
    [SerializeField] private GameObject carvingPrefab;        // Fancy decorations on walls
    [SerializeField] private GameObject healthPickupPrefab;   // For when our player needs a little boost!
    [SerializeField] private GameObject exitPortalPrefab;     // The way out of the dungeon!
    [SerializeField] private GameObject loadingIndicator;     // Something to show when the dungeon is being built

    private TileMatrix tileMatrix; // This is the map-making brain we talked about earlier!
    private List<GameObject> spawnedObjects = new List<GameObject>(); // We'll keep track of all the pieces we put in the dungeon
    private Vector2Int activeExitPoint = new Vector2Int(-1, -1); // Where the player came from to get here
    private bool isGenerating = false; // A switch to know if we are busy building the dungeon

    // This happens even before the game starts, like getting our tools ready!
    void Awake()
    {
        tileMatrix = new TileMatrix(); // We get our map-making brain ready
        if (loadingIndicator != null)
        {
            loadingIndicator.SetActive(false); // Hide the loading sign at first
        }
    }

    // This is like pressing the "Build Dungeon!" button!
    public void GenerateDungeon()
    {
        if (isGenerating) return; // If we're already building, let's not start over!

        ClearDungeon(); // First, clear away any old dungeon pieces

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
        if (loadingIndicator != null) loadingIndicator.SetActive(true); // Show the loading sign!
        
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
            if (loadingIndicator != null) loadingIndicator.SetActive(false);
            yield break; // Stop here if map is too small
        }

        tileMatrix.InitializeTileMap(rows, cols); // Tell the map brain how big the map is
        tileMatrix.SetRoomSize(minRoomSize, maxRoomSize); // Tell the map brain how big rooms should be
        tileMatrix.CreateRooms(roomCount, exitPoint); // Create rooms, starting from the exit door!
        tileMatrix.PrintDebugTileMap(); // Show us our new map!

        yield return null; // Take a short break so the game doesn't freeze

        SpawnDungeonObjects(); // Put all the dungeon pieces in the game!

        isGenerating = false; // We're done building!
        if (loadingIndicator != null) loadingIndicator.SetActive(false); // Hide the loading sign
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

        activeExitPoint = new Vector2Int(-1, -1); // Forget the old exit point
        isGenerating = false; // We are not building anymore
        if (loadingIndicator != null) loadingIndicator.SetActive(false); // Hide the loading sign
    }

    // This is where we actually put the game pieces in the world!
    private void SpawnDungeonObjects()
    {
        // Put down all the floor tiles!
        List<Vector2Int> floorTiles = tileMatrix.GetFloorTilePositions();
        foreach (Vector2Int tilePos in floorTiles)
        {
            Vector3 worldPos = TileToWorldPosition(tilePos);
            InstantiateAndAdd(floorPrefab, worldPos);
        }

        // Put up all the wall tiles!
        List<Vector2Int> wallTiles = tileMatrix.GetWallTilePositions();
        foreach (Vector2Int tilePos in wallTiles)
        {
            Vector3 worldPos = TileToWorldPosition(tilePos);
            // Walls need to be placed carefully, depending on which side of the tile they are
            // This simple example just places a wall at the center of the tile, you'll need more logic here
            InstantiateAndAdd(wallPrefab, worldPos + new Vector3(0,0,tileSize * 0.5f)); // Lift walls a bit
        }

        // Put cool stuff in the rooms!
        List<SpawnPointData> roomSpawnPoints = tileMatrix.GetRoomSpawnPoints(tileSize, true); // Don't put stuff in the hub
        foreach (SpawnPointData spawnPoint in roomSpawnPoints)
        {
            // Example: Put a treasure chest in some rooms far away
            if (Random.value < spawnPoint.Distance / (rows * tileSize / 2) && treasureChestPrefab != null)
            {
                InstantiateAndAdd(treasureChestPrefab, spawnPoint.Location);
            }
            // Example: Put some tech scrap in most rooms
            if (techScrapPrefab != null)
            {
                int numScrap = Random.Range(1, Mathf.CeilToInt(3 * (spawnPoint.Distance / (rows * tileSize / 2))));
                for (int i = 0; i < numScrap; i++)
                {
                    Vector3 offset = new Vector3(Random.Range(-tileSize * 0.3f, tileSize * 0.3f), Random.Range(-tileSize * 0.3f, tileSize * 0.3f), 0);
                    InstantiateAndAdd(techScrapPrefab, spawnPoint.Location + offset);
                }
            }
            // Example: Add carvings to some far-away rooms
            if (carvingPrefab != null && Random.value > 0.7f && spawnPoint.Distance > (rows * tileSize / 4))
            {
                InstantiateAndAdd(carvingPrefab, spawnPoint.Location + new Vector3(0, 0, tileSize * 0.5f));
            }
        }

        // Put health pickups in corridors!
        List<Vector2Int> corridorTiles = tileMatrix.GetCorridorTilePositions();
        foreach (Vector2Int tilePos in corridorTiles)
        {
            if (healthPickupPrefab != null && Random.value < 0.2f) // Small chance to spawn a health pickup
            {
                Vector3 worldPos = TileToWorldPosition(tilePos);
                InstantiateAndAdd(healthPickupPrefab, worldPos);
            }
        }

        // Put the exit portal at a random edge tile!
        List<Vector2Int> borderTiles = new List<Vector2Int>();
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Vector2Int tile = new Vector2Int(r, c);
                // Check if it's on the border and occupied by the dungeon (not hub)
                if (tileMatrix.IsTileOccupiedPublic(tile) && !tileMatrix.IsTileInHub(tile) && 
                    (r == 0 || r == rows - 1 || c == 0 || c == cols - 1))
                {
                    borderTiles.Add(tile);
                }
            }
        }
        if (exitPortalPrefab != null && borderTiles.Count > 0)
        {
            Vector2Int portalTile = borderTiles[Random.Range(0, borderTiles.Count)];
            Vector3 portalWorldPos = TileToWorldPosition(portalTile) + new Vector3(0, 0, tileSize * 0.5f); // Lift it a bit
            InstantiateAndAdd(exitPortalPrefab, portalWorldPos);
        }

        // Example: Spawning central room objects (like a special treasure chest or torches)
        SpawnCentralRoomObjects();
    }

    // This puts special things in the starting hub area
    private void SpawnCentralRoomObjects()
    {
        // We'll calculate the center of the hub in world coordinates
        float centerX = 40 * tileSize; // Column 40
        float centerY = 5 * tileSize;  // Row 5
        float Z = 10.0f; // A small height

        // If we have a treasure chest for the hub
        if (treasureChestPrefab != null)
        {
            InstantiateAndAdd(treasureChestPrefab, new Vector3(centerX, centerY, Z));
        }

        // If we have torches for the hub
        if (torchPrefab != null)
        {
            List<Vector3> torchOffsets = new List<Vector3>
            {
                new Vector3(-4.5f * tileSize, -4.5f * tileSize, 0),
                new Vector3(-4.5f * tileSize,  4.5f * tileSize, 0),
                new Vector3(4.5f * tileSize, -4.5f * tileSize, 0),
                new Vector3(4.5f * tileSize,  4.5f * tileSize, 0)
            };

            foreach (Vector3 offset in torchOffsets)
            {
                InstantiateAndAdd(torchPrefab, new Vector3(centerX, centerY, Z) + offset);
            }
        }
    }

    // This helps us turn our map squares (like 1, 2) into actual places in the game (like 10, 20)
    private Vector3 TileToWorldPosition(Vector2Int tile)
    {
        // Unity's Y-axis is often 'up', so we'll use X for columns and Z for rows
        // Remember to multiply by tileSize to get actual world units
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
