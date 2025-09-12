using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ENDURE
{
	// This class represents a room in our dungeon!
	// It's like a big square area where players can walk around.
	public class Room : MonoBehaviour
	{
		public Corridor CorridorPrefab; // The corridor prefab we use to connect rooms
		public IntVector2 Size; // How big this room is (width and height)
		public IntVector2 Coordinates; // Where this room is positioned on the map
		public int Num; // A number to identify this room

		private GameObject _tilesObject; // Container for all the floor tiles in this room
		private GameObject _wallsObject; // Container for all the walls around this room
		private GameObject _roofObject; // Container for all the roof tiles above this room
		public Tile TilePrefab; // The floor tile we use to build the room
		private Tile[,] _tiles; // A 2D array to keep track of all tiles in this room
		public GameObject WallPrefab; // The wall piece we use around the room
		public GameObject RoofPrefab; // The roof piece we use above the room
		public RoomSetting Setting; // The visual settings for this room (colors, materials, etc.)

		public Dictionary<Room, Corridor> RoomCorridor = new Dictionary<Room, Corridor>(); // Keeps track of which corridors connect to which rooms

		private Map _map; // Reference to the main map that owns this room

		public GameObject PlayerPrefab; // The player prefab to spawn in the first room

		// This sets up the room with a reference to the main map
		public void Init(Map map)
		{
			_map = map;
		}

		// This builds the room by placing floor tiles in a grid pattern
		public IEnumerator Generate()
		{
			// Create parent object to hold all the floor tiles
			_tilesObject = new GameObject("Tiles");
			_tilesObject.transform.parent = transform;
			_tilesObject.transform.localPosition = Vector3.zero;

			// Create a 2D array to store all the tiles
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

		// This creates a floor tile at the specified coordinates
		private Tile CreateTile(IntVector2 coordinates)
		{
			if (_map.GetTileType(coordinates) == TileType.Empty)
			{
				_map.SetTileType(coordinates, TileType.Room);
			}
			else
			{
				Debug.LogError("Tile Conflict! Two rooms are trying to use the same space!");
			}
			Tile newTile = Instantiate(TilePrefab);
			newTile.Coordinates = coordinates;
			newTile.name = "Tile " + coordinates.x + ", " + coordinates.z;
			newTile.transform.parent = _tilesObject.transform;
			newTile.transform.localPosition = RoomMapManager.TileSize * new Vector3(coordinates.x - Coordinates.x - Size.x * 0.5f + 0.5f, 0f, coordinates.z - Coordinates.z - Size.z * 0.5f + 0.5f);
			newTile.transform.GetChild(0).GetComponent<Renderer>().material = Setting.floor;
			return newTile;
		}

		// This creates a corridor connecting this room to another room
		public Corridor CreateCorridor(Room otherRoom)
		{
			// Don't create if already connected
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

		// This creates walls around the room where needed
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
					// If it's center or corner or not wall
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
					newWall.transform.GetChild(0).GetComponent<Renderer>().material = Setting.wall;
				}
			}
			yield return null;
		}

		// This creates a roof above the room
public IEnumerator CreateRoof()
{
    if (RoofPrefab == null) yield break;

    _roofObject = new GameObject("Roof");
    _roofObject.transform.parent = transform;
    _roofObject.transform.localPosition = Vector3.zero;

    // Create roof tiles above each floor tile
    for (int x = 0; x < Size.x; x++)
    {
        for (int z = 0; z < Size.z; z++)
        {
            GameObject newRoof = Instantiate(RoofPrefab);
            newRoof.name = "Roof (" + (Coordinates.x + x) + ", " + (Coordinates.z + z) + ")";
            newRoof.transform.parent = _roofObject.transform;
            newRoof.transform.localPosition = RoomMapManager.TileSize * new Vector3(x - Size.x * 0.5f + 0.5f, 1f, z - Size.z * 0.5f + 0.5f);
            newRoof.transform.localScale *= RoomMapManager.TileSize;

            // Assign the floor material to the roof
            Renderer roofRenderer = newRoof.transform.GetChild(0).GetComponent<Renderer>();
            if (roofRenderer != null)
            {
                roofRenderer.material = new Material(Setting.floor);
                roofRenderer.material.renderQueue = 3000; // Ensure it renders on top
                roofRenderer.material.SetInt("_Cull", 0); // Disable culling to make it double-sided
            }
            else
            {
                Debug.LogError("Roof prefab child does not have a Renderer component!");
            }
        }
    }
    yield return null;
}

		// This creates the player in the first room
public IEnumerator CreatePlayer()
{
    // Add a delay before spawning the player
    yield return new WaitForSeconds(0.5f);

    GameObject player = Instantiate(PlayerPrefab);
    player.name = "Player";
    player.transform.parent = transform.parent;
    player.transform.localPosition = transform.localPosition;
}
	}
}