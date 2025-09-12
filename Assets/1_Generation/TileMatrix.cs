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

// This helps us know which way a wall should face
public enum WallDirection
{
    North, // Wall goes above the tile
    South, // Wall goes below the tile
    West,  // Wall goes to the left of the tile
    East   // Wall goes to the right of the tile
}

// This struct tells us where to put a wall and which way it should face
public struct WallSpawnData
{
    public Vector2Int TilePosition; // Which square on the map this wall is for
    public WallDirection Direction;   // Which side of the tile the wall should be on
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

    // Tuning
    public int corridorRadius = 1; // Corridor half-width (1 => 3x3, 0 => 1x1)
    public bool ensureConnectivity = true; // After generation, connect isolated regions to hub

    // BSP settings (used by alternative generator)
    public int bspMinLeafSize = 10;
    public int bspMaxLeafSize = 24;

    // MST-rooms settings (rectangle rooms, Prim connectivity)
    public int rectRoomPadding = 1; // gap between rooms
    public int rectMinSize = 4;
    public int rectMaxSize = 10;
    public float extraConnectionChance = 0.15f; // chance to add non-MST edge for loops

    // When we first create our map brain, we set up some basic rules
    public TileMatrix()
    {
        rowsNum = 80;
        colsNum = 80;
        minRoomSize = 1; // Even smaller rooms for more organic, cave-like shapes
        maxRoomSize = 3; // Even smaller rooms for more organic, cave-like shapes
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
            if (tiles.Count == 0)
            {
                continue;
            }
            if (AreTilesValid(tiles))
            {
                foreach (Vector2Int tile in tiles)
                {
                    OccupyTile(tile);
                }
                RoomData newRoom = new RoomData { OccupiedTiles = tiles };
                if (newRoom.OccupiedTiles.Count > 0)
                {
                    GeneratedRooms.Add(newRoom);
                    ConnectRooms(parentRoom, newRoom, edgeTile); // Connect to the specific edge tile
                }
                return true;
            }
        }
        return false;
    }

    // This makes a pathway (corridor) between two rooms
    public void ConnectRooms(RoomData parentRoom, RoomData newRoom, Vector2Int edgeTile)
    {
        if (newRoom.OccupiedTiles == null || newRoom.OccupiedTiles.Count == 0)
        {
            return;
        }
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
            // Corridor thickness controlled by corridorRadius
            int radius = Mathf.Max(0, corridorRadius);
            for (int dR = -radius; dR <= radius; dR++)
            {
                for (int dC = -radius; dC <= radius; dC++)
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

        // Determine the first row immediately above the hub at the exitPoint's column.
        // The hub occupies rows 0-9 for its given column range. So, the first row outside is row 10.
        int firstAvailableRowAboveHub = 0;
        foreach (Vector2Int hubTile in HubTiles)
        {
            if (hubTile.x > firstAvailableRowAboveHub) firstAvailableRowAboveHub = hubTile.x;
        }
        firstAvailableRowAboveHub++; // Move one row up from the maximum hub row

        // First, we make a special hallway that goes upwards from our exit point
        int size = Random.Range(minRoomSize, maxRoomSize);
        List<Vector2Int> tiles = new List<Vector2Int>();
        int startRowForHallway = firstAvailableRowAboveHub; // Start above the hub
        int startColForHallway = exitPoint.y; // Keep the same column as the exit point

        // Make sure the hallway fits on the map vertically
        if (startRowForHallway + size > rowsNum)
        {
             size = Mathf.Max(minRoomSize, rowsNum - startRowForHallway); // Adjust size to fit within bounds
        }

        // Form the tiles for the initial hallway. It's a square room for now.
        for (int r = startRowForHallway; r < startRowForHallway + size; r++)
        {
            for (int c = startColForHallway - (size / 2); c < startColForHallway + (size / 2); c++)
            {
                if (c >= 0 && c < colsNum)
                {
                    tiles.Add(new Vector2Int(r, c));
                }
            }
        }

        if (tiles.Count > 0 && AreTilesValid(tiles))
        {
            foreach (Vector2Int tile in tiles)
            {
                OccupyTile(tile);
            }
            RoomData newRoom = new RoomData { OccupiedTiles = tiles };
            if (newRoom.OccupiedTiles.Count > 0)
            {
                GeneratedRooms.Add(newRoom);
                // Connect this new room to the original exitPoint (which is in the hub)
                ConnectRooms(hubRoom, newRoom, exitPoint);
            }
        }
        else
        {
            Debug.LogWarning($"Failed to place initial hallway at safe point: {new Vector2Int(startRowForHallway, startColForHallway)}");
            // Fallback: try with minimum size at the same safe start.
            size = minRoomSize;
            tiles.Clear();
            // Re-form tiles for minimum size hallway
            for (int r = startRowForHallway; r < startRowForHallway + size; r++)
            {
                for (int c = startColForHallway - (size / 2); c < startColForHallway + (size / 2); c++)
                {
                    if (c >= 0 && c < colsNum)
                    {
                        tiles.Add(new Vector2Int(r, c));
                    }
                }
            }
            if (tiles.Count > 0 && AreTilesValid(tiles))
            {
                foreach (Vector2Int tile in tiles)
                {
                    OccupyTile(tile);
                }
                RoomData newRoom = new RoomData { OccupiedTiles = tiles };
                if (newRoom.OccupiedTiles.Count > 0)
                {
                    GeneratedRooms.Add(newRoom);
                    ConnectRooms(hubRoom, newRoom, exitPoint);
                }
            }
            else
            {
                Debug.LogError($"Failed to place fallback hallway at safe point: {new Vector2Int(startRowForHallway, startColForHallway)}");
            }
        }

        // Generate remaining rooms
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
                if (TryPlaceRoomFromEdge(edgeTile, roomSize, parentRoom))
                {
                    break; // Yay, we made a room!
                }
                attempts++;
            }
        }

        if (ensureConnectivity)
        {
            EnsureConnectivityToHub();
        }
    }

    // Alternative generator: BSP partitioning with guaranteed connectivity
    public void CreateRoomsBsp()
    {
        // Clear and keep existing hub from InitializeTileMap()
        // Build BSP tree
        List<RectInt> leaves = BuildBspLeaves(new RectInt(0, 0, rowsNum, colsNum));

        // Carve random room in each leaf
        List<RectInt> rooms = new List<RectInt>();
        foreach (var leaf in leaves)
        {
            int roomW = Random.Range(Mathf.Max(3, bspMinLeafSize / 2), Mathf.Max(4, leaf.width - 1));
            int roomH = Random.Range(Mathf.Max(3, bspMinLeafSize / 2), Mathf.Max(4, leaf.height - 1));
            roomW = Mathf.Min(roomW, leaf.width - 2);
            roomH = Mathf.Min(roomH, leaf.height - 2);
            if (roomW <= 0 || roomH <= 0) continue;
            int rx = Random.Range(leaf.xMin + 1, leaf.xMax - roomW);
            int ry = Random.Range(leaf.yMin + 1, leaf.yMax - roomH);
            RectInt room = new RectInt(rx, ry, roomW, roomH);
            rooms.Add(room);
            CarveRoom(room);
        }

        // Connect rooms by linking leaf centers via a simple spanning approach: nearest-neighbor chain
        if (rooms.Count == 0) return;
        List<Vector2Int> centers = new List<Vector2Int>();
        foreach (var r in rooms) centers.Add(Vector2Int.RoundToInt(r.center));
        HashSet<int> connected = new HashSet<int> { 0 };
        while (connected.Count < centers.Count)
        {
            int bestA = -1, bestB = -1; float best = float.MaxValue;
            foreach (int a in connected)
            {
                for (int b = 0; b < centers.Count; b++)
                {
                    if (connected.Contains(b)) continue;
                    float d = ManhattanDistance(centers[a], centers[b]);
                    if (d < best)
                    {
                        best = d; bestA = a; bestB = b;
                    }
                }
            }
            if (bestA == -1 || bestB == -1) break;
            CarveCorridor(centers[bestA], centers[bestB]);
            connected.Add(bestB);
        }
    }

    // Alternative method inspired by Room Dungeon Generator: place non-overlapping rectangles, connect via MST
    public void CreateRoomsMstLike(int roomCount)
    {
        List<RectInt> rooms = new List<RectInt>();
        int attempts = 0;
        int maxAttempts = roomCount * roomCount;
        while (rooms.Count < roomCount && attempts < maxAttempts)
        {
            attempts++;
            int w = Random.Range(rectMinSize, rectMaxSize + 1);
            int h = Random.Range(rectMinSize, rectMaxSize + 1);
            int x = Random.Range(1, rowsNum - w - 1);
            int y = Random.Range(1, colsNum - h - 1);
            RectInt cand = new RectInt(x, y, w, h);
            if (!OverlapsAny(cand, rooms, rectRoomPadding))
            {
                rooms.Add(cand);
                CarveRoom(cand);
            }
        }

        // Compute centers
        List<Vector2Int> centers = new List<Vector2Int>();
        foreach (var r in rooms) centers.Add(Vector2Int.RoundToInt(r.center));

        // Prim MST
        List<int> connected = new List<int>();
        if (centers.Count == 0) return;
        connected.Add(0);
        while (connected.Count < centers.Count)
        {
            int bestA = -1, bestB = -1; float best = float.MaxValue;
            foreach (int a in connected)
            {
                for (int b = 0; b < centers.Count; b++)
                {
                    if (connected.Contains(b)) continue;
                    float d = ManhattanDistance(centers[a], centers[b]);
                    if (d < best)
                    {
                        best = d; bestA = a; bestB = b;
                    }
                }
            }
            if (bestA == -1 || bestB == -1) break;
            CarveCorridor(centers[bestA], centers[bestB]);
            connected.Add(bestB);
        }

        if (ensureConnectivity) EnsureConnectivityToHub();
    }

    bool OverlapsAny(RectInt a, List<RectInt> list, int padding)
    {
        foreach (var b in list)
        {
            RectInt expanded = new RectInt(b.xMin - padding, b.yMin - padding, b.width + padding * 2, b.height + padding * 2);
            if (a.Overlaps(expanded)) return true;
        }
        return false;
    }

    // Delaunay + MST corridors inspired by Room Dungeon Generator (rooms already carved)
    public void ConnectRoomsDelaunayMst(List<RectInt> rooms)
    {
        if (rooms == null || rooms.Count == 0) return;
        List<Vector2> pts = new List<Vector2>();
        foreach (var r in rooms) pts.Add(r.center);

        // Bowyer–Watson to get Delaunay triangulation
        List<(int a,int b,int c)> triangles = DelaunayBowyerWatson(pts);
        // Build unique edges from triangles
        HashSet<(int,int)> edges = new HashSet<(int,int)>();
        foreach (var t in triangles)
        {
            AddEdge(edges, t.Item1, t.Item2);
            AddEdge(edges, t.Item2, t.Item3);
            AddEdge(edges, t.Item3, t.Item1);
        }

        // Prim MST over Delaunay edges
        List<(int,int)> mst = new List<(int,int)>();
        HashSet<int> inTree = new HashSet<int> { 0 };
        while (inTree.Count < pts.Count)
        {
            float best = float.MaxValue; (int,int) bestEdge = (-1,-1);
            foreach (var e in edges)
            {
                bool u = inTree.Contains(e.Item1);
                bool v = inTree.Contains(e.Item2);
                if (u == v) continue;
                float d = Vector2.Distance(pts[e.Item1], pts[e.Item2]);
                if (d < best) { best = d; bestEdge = e; }
            }
            if (bestEdge.Item1 == -1) break;
            mst.Add(bestEdge);
            inTree.Add(bestEdge.Item1);
            inTree.Add(bestEdge.Item2);
        }

        // Optionally add some extra edges for loops
        foreach (var e in edges)
        {
            if (Random.value < extraConnectionChance) mst.Add(e);
        }

        // Carve corridors along chosen edges
        foreach (var e in mst)
        {
            Vector2Int a = Vector2Int.RoundToInt(pts[e.Item1]);
            Vector2Int b = Vector2Int.RoundToInt(pts[e.Item2]);
            CarveCorridor(a, b);
        }
    }

    void AddEdge(HashSet<(int,int)> set, int i, int j)
    {
        if (i == j) return;
        int a = Mathf.Min(i, j), b = Mathf.Max(i, j);
        set.Add((a, b));
    }

    List<(int a,int b,int c)> DelaunayBowyerWatson(List<Vector2> pts)
    {
        List<(int,int,int)> tris = new List<(int,int,int)>();
        // Super-triangle big enough
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        foreach (var p in pts) { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
        float dx = max.x - min.x; float dy = max.y - min.y; float delta = Mathf.Max(dx, dy) * 10f + 10f;
        Vector2 p0 = new Vector2(min.x - delta, min.y - 1);
        Vector2 p1 = new Vector2(min.x + dx * 0.5f, max.y + delta);
        Vector2 p2 = new Vector2(max.x + delta, min.y - 1);
        int i0 = pts.Count; int i1 = pts.Count + 1; int i2 = pts.Count + 2;
        List<Vector2> all = new List<Vector2>(pts); all.Add(p0); all.Add(p1); all.Add(p2);
        tris.Add((i0, i1, i2));

        for (int pi = 0; pi < pts.Count; pi++)
        {
            Vector2 p = pts[pi];
            List<(int,int,int)> bad = new List<(int,int,int)>();
            foreach (var t in tris)
            {
                if (PointInCircumcircle(p, all[t.Item1], all[t.Item2], all[t.Item3])) bad.Add(t);
            }
            // Find polygon boundary (unique edges of bad tris)
            HashSet<(int,int)> poly = new HashSet<(int,int)>();
            foreach (var t in bad)
            {
                ToggleEdge(poly, t.Item1, t.Item2);
                ToggleEdge(poly, t.Item2, t.Item3);
                ToggleEdge(poly, t.Item3, t.Item1);
            }
            // Remove bad tris
            for (int bi = tris.Count - 1; bi >= 0; bi--)
            {
                if (bad.Contains(tris[bi])) tris.RemoveAt(bi);
            }
            // Re-triangulate polygon with point p
            foreach (var e in poly)
            {
                tris.Add((e.Item1, e.Item2, pi));
            }
        }

        // Remove tris using super-verts
        tris.RemoveAll(t => t.Item1 >= pts.Count || t.Item2 >= pts.Count || t.Item3 >= pts.Count);
        return tris;
    }

    void ToggleEdge(HashSet<(int,int)> set, int i, int j)
    {
        int a = Mathf.Min(i, j), b = Mathf.Max(i, j);
        if (!set.Remove((a, b))) set.Add((a, b));
    }

    bool PointInCircumcircle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float ax = a.x - p.x, ay = a.y - p.y;
        float bx = b.x - p.x, by = b.y - p.y;
        float cx = c.x - p.x, cy = c.y - p.y;
        float det = (ax * ax + ay * ay) * (bx * cy - cx * by)
                  - (bx * bx + by * by) * (ax * cy - cx * ay)
                  + (cx * cx + cy * cy) * (ax * by - bx * ay);
        return det > 0f;
    }

    List<RectInt> BuildBspLeaves(RectInt root)
    {
        Queue<RectInt> queue = new Queue<RectInt>();
        List<RectInt> leaves = new List<RectInt>();
        queue.Enqueue(root);
        while (queue.Count > 0)
        {
            RectInt cur = queue.Dequeue();
            bool canSplitH = cur.height >= bspMaxLeafSize;
            bool canSplitV = cur.width >= bspMaxLeafSize;
            if (canSplitH || canSplitV)
            {
                bool splitHoriz = canSplitH && (!canSplitV || Random.value < 0.5f);
                if (splitHoriz)
                {
                    int split = Random.Range(cur.yMin + bspMinLeafSize, cur.yMax - bspMinLeafSize);
                    RectInt a = new RectInt(cur.xMin, cur.yMin, cur.width, split - cur.yMin);
                    RectInt b = new RectInt(cur.xMin, split, cur.width, cur.yMax - split);
                    queue.Enqueue(a); queue.Enqueue(b);
                }
                else
                {
                    int split = Random.Range(cur.xMin + bspMinLeafSize, cur.xMax - bspMinLeafSize);
                    RectInt a = new RectInt(cur.xMin, cur.yMin, split - cur.xMin, cur.height);
                    RectInt b = new RectInt(split, cur.yMin, cur.xMax - split, cur.height);
                    queue.Enqueue(a); queue.Enqueue(b);
                }
            }
            else
            {
                leaves.Add(cur);
            }
        }
        return leaves;
    }

    void CarveRoom(RectInt room)
    {
        for (int r = room.xMin; r < room.xMax; r++)
        {
            for (int c = room.yMin; c < room.yMax; c++)
            {
                Vector2Int t = new Vector2Int(r, c);
                if (IsTileInMap(t) && !IsTileInHub(t))
                {
                    TileMap[r * colsNum + c] = true;
                }
            }
        }
        GeneratedRooms.Add(new RoomData { OccupiedTiles = RectToTiles(room) });
    }

    List<Vector2Int> RectToTiles(RectInt room)
    {
        List<Vector2Int> list = new List<Vector2Int>();
        for (int r = room.xMin; r < room.xMax; r++)
        {
            for (int c = room.yMin; c < room.yMax; c++)
            {
                list.Add(new Vector2Int(r, c));
            }
        }
        return list;
    }

    void CarveCorridor(Vector2Int a, Vector2Int b)
    {
        Vector2Int cur = a;
        while (cur.x != b.x)
        {
            cur.x += cur.x < b.x ? 1 : -1;
            FillAt(cur);
        }
        while (cur.y != b.y)
        {
            cur.y += cur.y < b.y ? 1 : -1;
            FillAt(cur);
        }
    }

    void FillAt(Vector2Int center)
    {
        int radius = Mathf.Max(0, corridorRadius);
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                Vector2Int n = new Vector2Int(center.x + dx, center.y + dy);
                if (IsTileInMap(n) && !IsTileInHub(n))
                {
                    TileMap[n.x * colsNum + n.y] = true;
                }
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
                if (TileMap[r * colsNum + c])
                {
                    floorLocations.Add(tile);
                }
            }
        }
        return floorLocations;
    }

    // After rooms/corridors placed, connect any isolated occupied regions back to the hub region
    private void EnsureConnectivityToHub()
    {
        // Build visited map via BFS from hub tiles
        bool[,] visited = new bool[rowsNum, colsNum];
        Queue<Vector2Int> q = new Queue<Vector2Int>();
        foreach (var hub in HubTiles)
        {
            if (IsTileInMap(hub))
            {
                visited[hub.x, hub.y] = true;
                q.Enqueue(hub);
            }
        }

        Vector2Int[] dirs = new Vector2Int[]
        {
            new Vector2Int(-1,0), new Vector2Int(1,0), new Vector2Int(0,-1), new Vector2Int(0,1)
        };

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            foreach (var d in dirs)
            {
                var n = new Vector2Int(cur.x + d.x, cur.y + d.y);
                if (!IsTileInMap(n)) continue;
                if (visited[n.x, n.y]) continue;
                if (!IsTileOccupied(n)) continue;
                visited[n.x, n.y] = true;
                q.Enqueue(n);
            }
        }

        // Collect frontier of main region and list of disconnected tiles
        List<Vector2Int> mainFrontier = new List<Vector2Int>();
        List<Vector2Int> disconnected = new List<Vector2Int>();
        for (int r = 0; r < rowsNum; r++)
        {
            for (int c = 0; c < colsNum; c++)
            {
                if (!IsTileOccupied(new Vector2Int(r, c))) continue;
                if (visited[r, c])
                {
                    // consider as frontier if adjacent to empty
                    foreach (var d in dirs)
                    {
                        var n = new Vector2Int(r + d.x, c + d.y);
                        if (IsTileInMap(n) && !IsTileOccupied(n))
                        {
                            mainFrontier.Add(new Vector2Int(r, c));
                            break;
                        }
                    }
                }
                else
                {
                    disconnected.Add(new Vector2Int(r, c));
                }
            }
        }

        if (disconnected.Count == 0 || mainFrontier.Count == 0) return;

        // For each disconnected tile, carve a corridor to nearest main frontier tile
        foreach (var iso in disconnected)
        {
            Vector2Int nearest = iso;
            float best = float.MaxValue;
            foreach (var f in mainFrontier)
            {
                float d = ManhattanDistance(iso, f);
                if (d < best)
                {
                    best = d;
                    nearest = f;
                }
            }

            // Carve corridor from iso to nearest
            Vector2Int cur = iso;
            while (cur.x != nearest.x || cur.y != nearest.y)
            {
                if (cur.x != nearest.x) cur.x += cur.x < nearest.x ? 1 : -1;
                if (cur.y != nearest.y) cur.y += cur.y < nearest.y ? 1 : -1;

                int radius = Mathf.Max(0, corridorRadius);
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        Vector2Int n = new Vector2Int(cur.x + dx, cur.y + dy);
                        if (IsTileInMap(n) && !IsTileInHub(n))
                        {
                            OccupyTile(n);
                        }
                    }
                }
            }
        }
    }

    // This tells us where to put the wall pieces and how to turn them
    public List<WallSpawnData> GetWallSpawnPoints()
    {
        List<WallSpawnData> wallSpawnPoints = new List<WallSpawnData>();
        for (int r = 0; r < rowsNum; r++)
        {
            for (int c = 0; c < colsNum; c++)
            {
                Vector2Int currentTile = new Vector2Int(r, c);
                // We only care about tiles that are part of the dungeon and not the hub
                if (!TileMap[r * colsNum + c] || IsTileInHub(currentTile)) continue;

                // Define all four possible neighbors and their corresponding wall directions
                Vector2Int[] neighbors = 
                {
                    new Vector2Int(r - 1, c), // North neighbor
                    new Vector2Int(r + 1, c), // South neighbor
                    new Vector2Int(r, c - 1), // West neighbor
                    new Vector2Int(r, c + 1)  // East neighbor
                };
                WallDirection[] directions = { WallDirection.North, WallDirection.South, WallDirection.West, WallDirection.East };

                for (int i = 0; i < neighbors.Length; i++)
                {
                    Vector2Int neighbor = neighbors[i];
                    WallDirection direction = directions[i];

                    // If a neighbor is outside the map or is an empty space (not occupied)
                    // We will add the wall, and let DungeonGenerator handle duplicates.
                    if (!IsTileInMap(neighbor) || !IsTileOccupied(neighbor))
                    {
                        wallSpawnPoints.Add(new WallSpawnData { TilePosition = currentTile, Direction = direction });
                    }
                }
            }
        }
        return wallSpawnPoints;
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
        // Unity's Console truncates very long single log entries.
        // To ensure the whole map is visible, we print it in chunks of rows.
        const int rowsPerChunk = 32; // Tune as needed
        int printed = 0;
        while (printed < rowsNum)
        {
            int endRow = Mathf.Min(printed + rowsPerChunk, rowsNum);
            string output = "";
            for (int r = printed; r < endRow; r++)
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
            printed = endRow;
        }
    }
}
