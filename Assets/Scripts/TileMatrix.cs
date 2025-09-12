using System.Collections.Generic;
using UnityEngine;

// This struct holds information about a room we make
public struct RoomData
{
    public List<Vector2Int> OccupiedTiles; // These are all the little squares this room takes up
}

// This struct helps us remember where to put things and how far away they are
public struct SpawnPointData
{
    public Vector3 Location; // Where to put it in the game world
    public float Distance;   // How far from the center it is
}

// This class is like a big brain that figures out how to draw our dungeon map!
public class TileMatrix
{
    public int rowsNum { get; private set; } // How many rows of squares our map has
    public int colsNum { get; private set; } // How many columns of squares our map has
    public int minRoomSize { get; private set; } // The smallest size a room can be
    public int maxRoomSize { get; private set; } // The biggest size a room can be
    public int maxRandomAttemptsPerRoom { get; private set; } // How many times we try to place a room before giving up

    private List<bool> TileMap; // This is our map! True means a square is used, false means it's empty
    private List<RoomData> GeneratedRooms; // All the rooms we've made so far
    private List<Vector2Int> HubTiles; // Special squares that are part of the main starting area

    // When we first create our map brain, we set up some basic rules
    public TileMatrix()
    {
        rowsNum = 80;
        colsNum = 80;
        minRoomSize = 6;
        maxRoomSize = 12;
        maxRandomAttemptsPerRoom = 300;
        GeneratedRooms = new List<RoomData>();
        HubTiles = new List<Vector2Int>();
    }

    // We tell the map brain how big our map should be
    public void InitializeTileMap(int rows, int cols)
    {
        if (rows <= 0 || cols <= 0)
        {
            Debug.LogError($"Invalid tile map dimensions: Rows={rows}, Cols={cols}");
            return;
        }

        rowsNum = rows;
        colsNum = cols;
        TileMap = new List<bool>(rowsNum * colsNum);
        for (int i = 0; i < rowsNum * colsNum; i++)
        {
            TileMap.Add(false); // Make all squares empty at first
        }
        GeneratedRooms.Clear();
        HubTiles.Clear();

        // We make a special starting area called the "hub" in the middle of our map
        int CenterRow = 5; // A special row for the hub
        int CenterCol = 40; // A special column for the hub
        int HalfSize = 5; // How big half of the hub is
        for (int r = CenterRow - HalfSize; r <= CenterRow + HalfSize - 1; r++)
        {
            for (int c = CenterCol - HalfSize; c <= CenterCol + HalfSize - 1; c++)
            {
                Vector2Int tile = new Vector2Int(r, c);
                if (IsTileInMap(tile))
                {
                    TileMap[r * colsNum + c] = true; // Mark these squares as used
                    HubTiles.Add(tile); // Remember them as hub tiles
                }
            }
        }
    }

    // We tell the map brain how big rooms should be (smallest and biggest)
    public void SetRoomSize(int newMinRoomSize, int newMaxRoomSize)
    {
        minRoomSize = newMinRoomSize;
        maxRoomSize = newMaxRoomSize;
    }

    // This checks if a square is inside our map boundaries
    public bool IsTileInMap(Vector2Int tile)
    {
        return tile.x >= 0 && tile.x < rowsNum && tile.y >= 0 && tile.y < colsNum;
    }

    // This checks if a square is already being used by a room or hallway
    public bool IsTileOccupied(Vector2Int tile)
    {
        if (!IsTileInMap(tile)) return false;
        return TileMap[tile.x * colsNum + tile.y];
    }

    // This checks if a square is part of our special starting hub area
    public bool IsTileInHub(Vector2Int tile)
    {
        return HubTiles.Contains(tile);
    }

    // This checks if a square is used by anything (room, hallway, or hub)
    public bool IsTileOccupiedPublic(Vector2Int tile)
    {
        return IsTileOccupied(tile);
    }

    // This checks if a bunch of squares are all empty and inside the map
    public bool AreTilesValid(List<Vector2Int> tiles)
    {
        foreach (Vector2Int tile in tiles)
        {
            if (!IsTileInMap(tile) || IsTileOccupied(tile))
                return false;
        }
        return true;
    }

    // This marks a square as 'used' on our map
    public void OccupyTile(Vector2Int tile)
    {
        if (IsTileInMap(tile) && !IsTileInHub(tile))
        {
            TileMap[tile.x * colsNum + tile.y] = true;
        }
    }

    // This measures the distance between two squares like a city block, not a straight line
    public float ManhattanDistance(Vector2Int A, Vector2Int B)
    {
        return Mathf.Abs(A.x - B.x) + Mathf.Abs(A.y - B.y);
    }

    // This finds the squares on the edge of a room where we can try to connect new rooms
    public List<Vector2Int> GetEdgeTiles(RoomData room)
    {
        List<Vector2Int> edgeTiles = new List<Vector2Int>();
        foreach (Vector2Int tile in room.OccupiedTiles)
        {
            List<Vector2Int> neighbors = new List<Vector2Int>
            {
                new Vector2Int(tile.x - 1, tile.y), // Up
                new Vector2Int(tile.x + 1, tile.y), // Down
                new Vector2Int(tile.x, tile.y - 1), // Left
                new Vector2Int(tile.x, tile.y + 1)  // Right
            };
            foreach (Vector2Int neighbor in neighbors)
            {
                if (IsTileInMap(neighbor) && !room.OccupiedTiles.Contains(neighbor) && !IsTileInHub(neighbor))
                {
                    if (!edgeTiles.Contains(tile)) // Only add the original tile if its neighbor is an edge
                    {
                        edgeTiles.Add(tile);
                    }
                }
            }
        }
        return edgeTiles;
    }

    // This tries to place a new room starting from an edge of an existing room
    public bool TryPlaceRoomFromEdge(Vector2Int edgeTile, int size, RoomData parentRoom)
    {
        List<Vector2Int> directions = new List<Vector2Int>
        {
            new Vector2Int(-1, 0), // Up
            new Vector2Int(1, 0),  // Down
            new Vector2Int(0, -1), // Left
            new Vector2Int(0, 1)   // Right
        };
        // Shuffle directions randomly
        for (int i = 0; i < directions.Count; i++)
        {
            Vector2Int temp = directions[i];
            int randomIndex = Random.Range(i, directions.Count);
            directions[i] = directions[randomIndex];
            directions[randomIndex] = temp;
        }

        foreach (Vector2Int dir in directions)
        {
            List<Vector2Int> tiles = new List<Vector2Int>();
            int startRow = edgeTile.x + dir.x * size; // Adjusted to move the start of the room
            int startCol = edgeTile.y + dir.y * size; // Adjusted to move the start of the room

            // Make sure the room doesn't overlap the edgeTile itself by moving the room outward
            if (dir.x != 0) startRow = edgeTile.x + (dir.x > 0 ? 1 : -size);
            if (dir.y != 0) startCol = edgeTile.y + (dir.y > 0 ? 1 : -size);


            for (int r = startRow; r < startRow + size; r++)
            {
                for (int c = startCol; c < startCol + size; c++)
                {
                    tiles.Add(new Vector2Int(r, c));
                }
            }
            if (AreTilesValid(tiles))
            {
                foreach (Vector2Int tile in tiles)
                {
                    OccupyTile(tile);
                }
                RoomData newRoom = new RoomData { OccupiedTiles = tiles };
                GeneratedRooms.Add(newRoom);
                ConnectRooms(parentRoom, newRoom, edgeTile); // Connect to the specific edge tile
                return true;
            }
        }
        return false;
    }

    // This makes a pathway (corridor) between two rooms
    public void ConnectRooms(RoomData parentRoom, RoomData newRoom, Vector2Int edgeTile)
    {
        Vector2Int newRoomTile = newRoom.OccupiedTiles[0]; // Start with the first tile in the new room
        float minDist = ManhattanDistance(edgeTile, newRoomTile);

        // Find the closest tile in the new room to the edge tile of the parent room
        foreach (Vector2Int tile in newRoom.OccupiedTiles)
        {
            float dist = ManhattanDistance(edgeTile, tile);
            if (dist < minDist)
            {
                minDist = dist;
                newRoomTile = tile;
            }
        }

        Vector2Int current = edgeTile;
        // Keep moving from the edge tile towards the closest tile in the new room
        while (current.x != newRoomTile.x || current.y != newRoomTile.y)
        {
            if (current.x != newRoomTile.x)
            {
                current.x += current.x < newRoomTile.x ? 1 : -1; // Move up or down
            }
            if (current.y != newRoomTile.y)
            {
                current.y += current.y < newRoomTile.y ? 1 : -1; // Move left or right
            }
            // Occupy a 3x3 area for a wider hallway
            for (int dR = -1; dR <= 1; dR++)
            {
                for (int dC = -1; dC <= 1; dC++)
                {
                    Vector2Int neighbor = new Vector2Int(current.x + dR, current.y + dC);
                    if (IsTileInMap(neighbor) && !IsTileInHub(neighbor))
                    {
                        OccupyTile(neighbor);
                    }
                }
            }
        }
    }

    // This is the main function to make all the rooms!
    public void CreateRooms(int roomCount, Vector2Int exitPoint)
    {
        // We create a pretend room for the hub so we can connect to it
        RoomData hubRoom = new RoomData { OccupiedTiles = HubTiles };
        GeneratedRooms.Add(hubRoom);

        // First, we make a special hallway that goes upwards from our exit point
        int size = Random.Range(minRoomSize, maxRoomSize);
        List<Vector2Int> tiles = new List<Vector2Int>();
        int startRow = exitPoint.x + size; // This makes it go upward from the exit point
        int startCol = exitPoint.y; // Starts at the same column as the exit point

        // Make sure the hallway fits on the map
        if (startRow + size > rowsNum) // This check is probably not right for 'startRow' being a target, should be about room size
        {
             size = Mathf.Max(minRoomSize, rowsNum - exitPoint.x - 1); // Adjust size to fit within bounds
        }
        startRow = exitPoint.x + 1; // Start just above ExitPoint
        for (int r = startRow; r < startRow + size; r++)
        {
            for (int c = startCol - (size / 2); c < startCol + (size / 2); c++)
            {
                if (c >= 0 && c < colsNum)
                {
                    tiles.Add(new Vector2Int(r, c));
                }
            }
        }

        if (AreTilesValid(tiles))
        {
            foreach (Vector2Int tile in tiles)
            {
                OccupyTile(tile);
            }
            RoomData newRoom = new RoomData { OccupiedTiles = tiles };
            GeneratedRooms.Add(newRoom);
            ConnectRooms(hubRoom, newRoom, exitPoint);
        }
        else
        {
            Debug.LogWarning($"Failed to place initial hallway at ExitPoint: {exitPoint}");
            // If the first try fails, we try with the smallest room size
            size = minRoomSize;
            tiles.Clear();
            startRow = exitPoint.x + 1; // Start just above ExitPoint
            for (int r = startRow; r < startRow + size; r++)
            {
                for (int c = startCol - (size / 2); c < startCol + (size / 2); c++)
                {
                    if (c >= 0 && c < colsNum)
                    {
                        tiles.Add(new Vector2Int(r, c));
                    }
                }
            }
            if (AreTilesValid(tiles))
            {
                foreach (Vector2Int tile in tiles)
                {
                    OccupyTile(tile);
                }
                RoomData newRoom = new RoomData { OccupiedTiles = tiles };
                GeneratedRooms.Add(newRoom);
                ConnectRooms(hubRoom, newRoom, exitPoint);
            }
            else
            {
                Debug.LogError($"Failed to place fallback hallway at ExitPoint: {exitPoint}");
            }
        }

        // Now we make the rest of the rooms, but only above the exit point
        for (int i = GeneratedRooms.Count - 1; i < roomCount; i++)
        {
            int attempts = 0;
            while (attempts < maxRandomAttemptsPerRoom)
            {
                int roomIndex = Random.Range(0, GeneratedRooms.Count); // Pick a random room we already made
                RoomData parentRoom = GeneratedRooms[roomIndex];
                List<Vector2Int> edgeTiles = GetEdgeTiles(parentRoom); // Find its edges
                if (edgeTiles.Count == 0)
                {
                    attempts++;
                    continue;
                }
                Vector2Int edgeTile = edgeTiles[Random.Range(0, edgeTiles.Count)]; // Pick a random edge
                int roomSize = Random.Range(minRoomSize, maxRoomSize); // Pick a random size for the new room
                if (edgeTile.x <= exitPoint.x) // Only make rooms above the exit point
                {
                    attempts++;
                    continue;
                }
                if (TryPlaceRoomFromEdge(edgeTile, roomSize, parentRoom))
                {
                    break; // Yay, we made a room!
                }
                attempts++;
            }
        }
    }

    // This tells us where to put the floor pieces
    public List<Vector2Int> GetFloorTilePositions()
    {
        List<Vector2Int> floorLocations = new List<Vector2Int>();
        for (int r = 0; r < rowsNum; r++)
        {
            for (int c = 0; c < colsNum; c++)
            {
                Vector2Int tile = new Vector2Int(r, c);
                if (TileMap[r * colsNum + c] && !IsTileInHub(tile))
                {
                    floorLocations.Add(tile);
                }
            }
        }
        return floorLocations;
    }

    // This tells us where to put the wall pieces and how to turn them
    public List<Vector2Int> GetWallTilePositions()
    {
        List<Vector2Int> wallTiles = new List<Vector2Int>();
        for (int r = 0; r < rowsNum; r++)
        {
            for (int c = 0; c < colsNum; c++)
            {
                Vector2Int currentTile = new Vector2Int(r, c);
                if (!TileMap[r * colsNum + c] || IsTileInHub(currentTile)) continue; // Only check occupied non-hub tiles

                List<Vector2Int> neighbors = new List<Vector2Int>
                {
                    new Vector2Int(r - 1, c), // North
                    new Vector2Int(r + 1, c), // South
                    new Vector2Int(r, c - 1), // West
                    new Vector2Int(r, c + 1)  // East
                };

                foreach (Vector2Int neighbor in neighbors)
                {
                    // If a neighbor is outside the map or empty, then currentTile needs a wall facing that neighbor
                    if (!IsTileInMap(neighbor) || !IsTileOccupied(neighbor))
                    {
                        if (!wallTiles.Contains(currentTile))
                        {
                            wallTiles.Add(currentTile); // We add the tile that *needs* a wall
                        }
                    }
                }
            }
        }
        return wallTiles;
    }

    // This tells us where the rooms are, so we can put things inside them
    public List<SpawnPointData> GetRoomSpawnPoints(float tileSize, bool excludeCentralRoom)
    {
        List<SpawnPointData> spawnPoints = new List<SpawnPointData>();
        Vector2Int center = new Vector2Int(5, 40); // The center of our hub
        for (int roomIndex = excludeCentralRoom ? 1 : 0; roomIndex < GeneratedRooms.Count; roomIndex++)
        {
            RoomData room = GeneratedRooms[roomIndex];
            float avgX = 0, avgY = 0;
            foreach (Vector2Int tile in room.OccupiedTiles)
            {
                avgX += tile.y; // X in world corresponds to column (y in Vector2Int)
                avgY += tile.x; // Y in world corresponds to row (x in Vector2Int)
            }
            avgX /= room.OccupiedTiles.Count;
            avgY /= room.OccupiedTiles.Count;
            
            float worldX = avgX * tileSize;
            float worldY = avgY * tileSize;
            float Z = 10.0f; // A small height above the floor
            Vector3 location = new Vector3(worldX, worldY, Z + 5.0f); // Adjust Z slightly for spawning objects
            
            float distance = Vector2.Distance(new Vector2(avgY, avgX), new Vector2(center.x, center.y)); // Distance from hub
            SpawnPointData spawnPoint = new SpawnPointData { Location = location, Distance = distance };
            spawnPoints.Add(spawnPoint);
        }
        return spawnPoints;
    }

    // This tells us where hallways are, so we can put things there too
    public List<Vector2Int> GetCorridorTilePositions()
    {
        List<Vector2Int> spawnPoints = new List<Vector2Int>();
        for (int r = 0; r < rowsNum; r++)
        {
            for (int c = 0; c < colsNum; c++)
            {
                Vector2Int tile = new Vector2Int(r, c);
                if (!TileMap[r * colsNum + c] || IsTileInHub(tile)) continue;
                
                bool isInRoom = false;
                foreach (RoomData room in GeneratedRooms)
                {
                    if (room.OccupiedTiles.Contains(tile))
                    {
                        isInRoom = true;
                        break;
                    }
                }
                // If it's occupied, not a hub tile, and not part of a defined room, it's a corridor
                if (!isInRoom)
                {
                    spawnPoints.Add(tile);
                }
            }
        }
        return spawnPoints;
    }

    // This helps us see our map in the debug console, like a drawing!
    public void PrintDebugTileMap()
    {
        string output = "";
        for (int r = 0; r < rowsNum; r++)
        {
            for (int c = 0; c < colsNum; c++)
            {
                Vector2Int tile = new Vector2Int(r, c);
                if (IsTileInHub(tile))
                    output += "H"; // 'H' for Hub
                else
                    output += TileMap[r * colsNum + c] ? "█" : "."; // '█' for used, '.' for empty
            }
            output += "\n";
        }
        Debug.Log(output);
    }
}
