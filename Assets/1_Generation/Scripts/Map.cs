using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace ENDURE
{
	/// <summary>
	/// This struct holds minimum and maximum values, typically used for room sizes.
	/// </summary>
	[System.Serializable]
	public struct MinMax
	{
		/// <summary>
		/// The minimum value.
		/// </summary>
		public int Min;

		/// <summary>
		/// The maximum value.
		/// </summary>
		public int Max;
	}

	/// <summary>
	/// Represents the type of a tile in the dungeon.
	/// </summary>
	public enum TileType
	{
		/// <summary>
		/// Empty tile, no structure.
		/// </summary>
		Empty,

		/// <summary>
		/// Tile that is part of a room.
		/// </summary>
		Room,

		/// <summary>
		/// Tile that is part of a corridor.
		/// </summary>
		Corridor,

		/// <summary>
		/// Tile that is a wall.
		/// </summary>
		Wall
	}

	/// <summary>
	/// The main class for dungeon generation. This class handles the creation of rooms, corridors, walls, and roofs.
	/// It uses a combination of procedural generation techniques:
	///   - Random room placement with collision checks.
	///   - Delaunay Triangulation (Bowyer-Watson algorithm) to connect rooms.
	///   - Minimal Spanning Tree (Prim's algorithm) to ensure all rooms are connected with minimal corridors.
	///   - Wall generation around rooms and corridors.
	///   - Roof generation above floor tiles.
		/// </summary>
		public class Map : MonoBehaviour
		{
			/// <summary>
			/// Prefab for rooms in the dungeon.
			/// </summary>
			public Room RoomPrefab;

			/// <summary>
			/// The number of rooms to generate in the dungeon.
			/// </summary>
			[HideInInspector] public int RoomCount;

			/// <summary>
			/// Settings for rooms, including materials for floors and walls.
			/// </summary>
			public RoomSetting[] RoomSettings;

			/// <summary>
			/// The size of the dungeon map.
			/// </summary>
			[HideInInspector] public IntVector2 MapSize;

			/// <summary>
			/// The minimum and maximum size of rooms.
			/// </summary>
			[HideInInspector] public MinMax RoomSize;

			/// <summary>
			/// Delay between generation steps for visualization.
			/// </summary>
			public float GenerationStepDelay;

			/// <summary>
			/// List of rooms in the dungeon.
			/// </summary>
			private List<Room> _rooms;

			/// <summary>
			/// List of corridors in the dungeon.
			/// </summary>
			private List<Corridor> _corridors;

			/// <summary>
			/// 2D array representing the type of each tile in the dungeon.
			/// </summary>
			private TileType[,] _tilesTypes;

			/// <summary>
			/// Flag to check if the player has been spawned.
			/// </summary>
			private bool _hasPlayer = false;

			private bool _sceneExitSpawned = false;

			/// <summary>
			/// Reference to the first room where the player should spawn.
			/// </summary>
			private Room _firstRoom = null;

			/// <summary>
			/// Sets the type of a tile at the specified coordinates.
			/// </summary>
			/// <param name="coordinates">The coordinates of the tile.</param>
			/// <param name="tileType">The type of the tile.</param>
			public void SetTileType(IntVector2 coordinates, TileType tileType)
			{
				_tilesTypes[coordinates.x, coordinates.z] = tileType;
			}

			/// <summary>
			/// Gets the type of a tile at the specified coordinates.
			/// </summary>
			/// <param name="coordinates">The coordinates of the tile.</param>
			/// <returns>The type of the tile.</returns>
			public TileType GetTileType(IntVector2 coordinates)
			{
				return _tilesTypes[coordinates.x, coordinates.z];
			}


        // Generate Rooms and Corridors
        public IEnumerator Generate()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            _tilesTypes = new TileType[MapSize.x, MapSize.z];
            _rooms = new List<Room>();

            // Generate Rooms (WITHOUT enemies/items yet)
            for (int i = 0; i < RoomCount; i++)
            {
                Room roomInstance = CreateRoom();
                if (roomInstance == null)
                {
                    RoomCount = _rooms.Count;
                    Debug.Log("Cannot make more rooms!");
                    Debug.Log("Created Rooms : " + RoomCount);
                    break;
                }
                roomInstance.Setting = RoomSettings[Random.Range(0, RoomSettings.Length)];
                StartCoroutine(roomInstance.Generate());

                // Store reference to first room for player spawning later
                if (_firstRoom == null)
                {
                    _firstRoom = roomInstance;
                }

                yield return null;
            }
            Debug.Log("Every rooms are generated");

            // Delaunay Triangulation
            yield return BowyerWatson();

            // Minimal Spanning Tree
            yield return PrimMST();
            Debug.Log("Every rooms are minimally connected");

            // Generate Corridors
            foreach (Corridor corridor in _corridors)
            {
                StartCoroutine(corridor.Generate());
                yield return null;
            }
            Debug.Log("Every corridors are generated");

            // Generate Walls
            yield return WallCheck();
            foreach (Room room in _rooms)
            {
                yield return room.CreateWalls();
            }
            foreach (Corridor corridor in _corridors)
            {
                yield return corridor.CreateWalls();
            }
            Debug.Log("Every walls are generated");

            // Generate roof
            yield return GenerateRoof();
            Debug.Log("Roof generation complete");

            // Spawn player now that dungeon structure is complete
            if (!_hasPlayer && _firstRoom != null)
            {
                yield return _firstRoom.CreatePlayer();
                _hasPlayer = true;
                Debug.Log("Player spawned after dungeon generation complete");
            }

            if (!_sceneExitSpawned && _firstRoom != null)
            {
                yield return _firstRoom.CreateSceneExit();
                _sceneExitSpawned = true;
                Debug.Log("Scene exit spawned in starting room");
            }

            Debug.Log("Dungeon structure complete - ready for NavMesh baking");

            // WAIT for NavMesh to be baked
            yield return new WaitForSeconds(2f);

            // Verify NavMesh is ready
            int waitAttempts = 0;
            while (!DungeonNavMeshManager.IsNavMeshReady && waitAttempts < 20)
            {
                Debug.Log($"Waiting for NavMesh... (attempt {waitAttempts + 1})");
                yield return new WaitForSeconds(0.5f);
                waitAttempts++;
            }

            if (DungeonNavMeshManager.IsNavMeshReady)
            {
                Debug.Log("NavMesh confirmed ready - spawning enemies and items...");

                // NOW spawn enemies and items in all rooms
                foreach (Room room in _rooms)
                {
                    if (room == _firstRoom)
                    {
                        Debug.Log($"Skipping enemy spawn in starting room '{room.name}' to keep player spawn safe.");
                        continue;
                    }
                    yield return room.CreateMonsters();
                }

                // Spawn items and plants in corridors
                foreach (Corridor corridor in _corridors)
                {
                    yield return corridor.CreateItems();
                    yield return corridor.CreatePlantProps();
                }
                Debug.Log("All enemies, items, and plants spawned!");
            }
            else
            {
                Debug.LogError("NavMesh failed to bake! Skipping enemy spawn.");
            }

        }



        public IEnumerator GenerateRoof()
		{
			// Create a parent object for all roof tiles
			GameObject roofParent = new GameObject("Roof");
			roofParent.transform.parent = transform;
			roofParent.transform.localPosition = Vector3.zero;

			// Find all Tile objects in the scene to match roof positions to floor positions
			Tile[] allTiles = FindObjectsOfType<Tile>();

			// Iterate through all tiles in the map
			for (int x = 0; x < MapSize.x; x++)
			{
				for (int z = 0; z < MapSize.z; z++)
				{
					IntVector2 coordinates = new IntVector2(x, z);
					TileType tileType = _tilesTypes[x, z];

					// Place a roof tile above floor tiles (rooms and corridors)
					if (tileType == TileType.Room || tileType == TileType.Corridor)
					{
						// Find the corresponding floor tile
						Tile floorTile = null;
						foreach (Tile tile in allTiles)
						{
							if (tile.Coordinates.x == coordinates.x && tile.Coordinates.z == coordinates.z)
							{
								floorTile = tile;
								break;
							}
						}

						GameObject roofTile = GameObject.CreatePrimitive(PrimitiveType.Cube);
						roofTile.name = $"Roof ({coordinates.x}, {coordinates.z})";
						roofTile.transform.parent = roofParent.transform;

        // Position the roof tile directly above the floor tile
						if (floorTile != null)
						{
							// Use the floor tile's world position and place roof above it
							Vector3 floorWorldPos = floorTile.transform.position;
							roofTile.transform.position = new Vector3(
								floorWorldPos.x,
								floorWorldPos.y + 14.0f, // Height above floor
								floorWorldPos.z
							);
							// Convert to local position relative to roof parent
							roofTile.transform.localPosition = roofParent.transform.InverseTransformPoint(roofTile.transform.position);
						}
						else
						{
							// Fallback: use coordinate-based positioning
        Vector3 floorPosition = CoordinatesToPosition(coordinates);
        roofTile.transform.localPosition = new Vector3(
            floorPosition.x * RoomMapManager.TileSize,
            14.0f, // Height of the roof above the floor
            floorPosition.z * RoomMapManager.TileSize
        );
						}

        // Scale the roof tile to match the floor tile size
        roofTile.transform.localScale = new Vector3(RoomMapManager.TileSize, 0.2f, RoomMapManager.TileSize);

        // Apply the roof material from the first room's settings
        if (_rooms.Count > 0 && _rooms[0].Setting != null && _rooms[0].Setting.roof != null)
        {
            roofTile.GetComponent<Renderer>().material = _rooms[0].Setting.roof;
        }
        else
        {
							// Fallback: use default material or create a simple one
							// Don't use Shader.Find as it may not work in URP/HDRP
							Renderer renderer = roofTile.GetComponent<Renderer>();
							if (renderer != null && renderer.sharedMaterial == null)
							{
								// Use default material if available, otherwise leave as is
								Debug.LogWarning($"GenerateRoof: No roof material set for room settings. Using default material.");
							}
						}

						// Yield every 10 tiles to prevent frame drops on large dungeons
						if ((x * MapSize.z + z) % 10 == 0)
						{
							yield return null;
        }
					}
				}
			}
			
			Debug.Log("Roof generation complete");
			yield return null;
		}
	

		private IEnumerator WallCheck()
		{
			for (int x = 0; x < MapSize.x; x++)
			{
				for (int z = 0; z < MapSize.z; z++)
				{
					if (_tilesTypes[x, z] == TileType.Empty && IsWall(x, z))
					{
						_tilesTypes[x, z] = TileType.Wall;
					}
				}
			}
			yield return null;
		}

		private bool IsWall(int x, int z)
		{
			for (int i = x - 1; i <= x + 1; i++)
			{
				if (i < 0 || i >= MapSize.x)
				{
					continue;
				}
				for (int j = z - 1; j <= z + 1; j++)
				{
					if (j < 0 || j >= MapSize.z || (i == x && j == z))
					{
						continue;
					}
					if (_tilesTypes[i, j] == TileType.Room || _tilesTypes[i, j] == TileType.Corridor)
					{
						return true;
					}
				}
			}

			return false;
		}

		private Room CreateRoom()
		{
			Room newRoom = null;

			// Try as many as we can.
			for (int i = 0; i < RoomCount * RoomCount; i++)
			{
				IntVector2 size = new IntVector2(Random.Range(RoomSize.Min, RoomSize.Max + 1), Random.Range(RoomSize.Min, RoomSize.Max + 1));
				IntVector2 coordinates = new IntVector2(Random.Range(1, MapSize.x - size.x), Random.Range(1, MapSize.z - size.z));
				if (!IsOverlapped(size, coordinates))
				{
					newRoom = Instantiate(RoomPrefab);
					_rooms.Add(newRoom);
					newRoom.Num = _rooms.Count;
					newRoom.name = "Room " + newRoom.Num + " (" + coordinates.x + ", " + coordinates.z + ")";
					newRoom.Size = size;
					newRoom.Coordinates = coordinates;
					newRoom.transform.parent = transform;
					Vector3 position = CoordinatesToPosition(coordinates);
					position.x += size.x * 0.5f - 0.5f;
					position.z += size.z * 0.5f - 0.5f;
					position *= RoomMapManager.TileSize;
					newRoom.transform.localPosition = position;
					newRoom.Init(this);
					break;
				}
			}

			if (newRoom == null)
			{
				Debug.LogError("Too many rooms in map!! : " + _rooms.Count);
			}

			return newRoom;
		}

		public IntVector2 RandomCoordinates
		{
			get { return new IntVector2(Random.Range(0, MapSize.x), Random.Range(0, MapSize.z)); }
		}

		private bool IsOverlapped(IntVector2 size, IntVector2 coordinates)
		{
			foreach (Room room in _rooms)
			{
				// Give a little space between two rooms
				if (Mathf.Abs(room.Coordinates.x - coordinates.x + (room.Size.x - size.x) * 0.5f) < (room.Size.x + size.x) * 0.7f &&
					Mathf.Abs(room.Coordinates.z - coordinates.z + (room.Size.z - size.z) * 0.5f) < (room.Size.z + size.z) * 0.7f)
				{
					return true;
				}
			}
			return false;
		}

		// Big enough to cover the map
		private Triangle LootTriangle
		{
			get
			{
				Vector3[] vertexs = new Vector3[]
				{
					RoomMapManager.TileSize * new Vector3(MapSize.x * 2, 0, MapSize.z),
					RoomMapManager.TileSize * new Vector3(-MapSize.x * 2, 0, MapSize.z),
					RoomMapManager.TileSize * new Vector3(0, 0, -2 * MapSize.z)
				};

				Room[] tempRooms = new Room[3];
				for (int i = 0; i < 3; i++)
				{
					tempRooms[i] = Instantiate(RoomPrefab);
					tempRooms[i].transform.localPosition = vertexs[i];
					tempRooms[i].name = "Loot Room " + i;
					tempRooms[i].Init(this);
				}

				return new Triangle(tempRooms[0], tempRooms[1], tempRooms[2]);
			}
		}

		private IEnumerator BowyerWatson()
		{
			List<Triangle> triangulation = new List<Triangle>();

			Triangle loot = LootTriangle;
			triangulation.Add(loot);

			foreach (Room room in _rooms)
			{
				List<Triangle> badTriangles = new List<Triangle>();

				foreach (Triangle triangle in triangulation)
				{
					if (triangle.IsContaining(room))
					{
						badTriangles.Add(triangle);
					}
				}

				List<Corridor> polygon = new List<Corridor>();
				foreach (Triangle badTriangle in badTriangles)
				{
					foreach (Corridor corridor in badTriangle.Corridors)
					{
						if (corridor.Triangles.Count == 1)
						{
							polygon.Add(corridor);
							corridor.Triangles.Remove(badTriangle);
							continue;
						}

						foreach (Triangle triangle in corridor.Triangles)
						{
							if (triangle == badTriangle)
							{
								continue;
							}

							// Delete Corridor which is between two bad triangles.
							if (badTriangles.Contains(triangle))
							{
								corridor.Rooms[0].RoomCorridor.Remove(corridor.Rooms[1]);
								corridor.Rooms[1].RoomCorridor.Remove(corridor.Rooms[0]);
								Destroy(corridor.gameObject);
							}
							else
							{
								polygon.Add(corridor);
							}
							break;
						}
					}
				}

				// Delete Bad Triangles
				for (int index = badTriangles.Count - 1; index >= 0; --index)
				{
					Triangle triangle = badTriangles[index];
					badTriangles.RemoveAt(index);
					triangulation.Remove(triangle);
					foreach (Corridor corridor in triangle.Corridors)
					{
						corridor.Triangles.Remove(triangle);
					}
				}

				foreach (Corridor corridor in polygon)
				{
					// TODO: Edge sync
					Triangle newTriangle = new Triangle(corridor.Rooms[0], corridor.Rooms[1], room);
					triangulation.Add(newTriangle);
				}
			}
			yield return null;

			for (int index = triangulation.Count - 1; index >= 0; index--)
			{
				if (triangulation[index].Rooms.Contains(loot.Rooms[0]) || triangulation[index].Rooms.Contains(loot.Rooms[1]) ||
					triangulation[index].Rooms.Contains(loot.Rooms[2]))
				{
					triangulation.RemoveAt(index);
				}
			}

			foreach (Room room in loot.Rooms)
			{
				List<Corridor> deleteList = new List<Corridor>();
				foreach (KeyValuePair<Room, Corridor> pair in room.RoomCorridor)
				{
					deleteList.Add(pair.Value);
				}
				for (int index = deleteList.Count - 1; index >= 0; index--)
				{
					Corridor corridor = deleteList[index];
					corridor.Rooms[0].RoomCorridor.Remove(corridor.Rooms[1]);
					corridor.Rooms[1].RoomCorridor.Remove(corridor.Rooms[0]);
					Destroy(corridor.gameObject);
				}
				Destroy(room.gameObject);
			}
		}

		private IEnumerator PrimMST()
		{
			List<Room> connectedRooms = new List<Room>();
			_corridors = new List<Corridor>();

			connectedRooms.Add(_rooms[0]);

			while (connectedRooms.Count < _rooms.Count)
			{
				KeyValuePair<Room, Corridor> minLength = new KeyValuePair<Room, Corridor>();
				List<Corridor> deleteList = new List<Corridor>();

				foreach (Room room in connectedRooms)
				{
					foreach (KeyValuePair<Room, Corridor> pair in room.RoomCorridor)
					{
						if (connectedRooms.Contains(pair.Key))
						{
							continue;
						}
						if (minLength.Value == null || minLength.Value.Length > pair.Value.Length)
						{
							minLength = pair;
						}
					}
				}

				// Check Unnecessary Corridors.
				foreach (KeyValuePair<Room, Corridor> pair in minLength.Key.RoomCorridor)
				{
					if (connectedRooms.Contains(pair.Key) && (minLength.Value != pair.Value))
					{
						deleteList.Add(pair.Value);
					}
				}

				// Delete corridors
				for (int index = deleteList.Count - 1; index >= 0; index--)
				{
					Corridor corridor = deleteList[index];
					corridor.Rooms[0].RoomCorridor.Remove(corridor.Rooms[1]);
					corridor.Rooms[1].RoomCorridor.Remove(corridor.Rooms[0]);
					deleteList.RemoveAt(index);
					Destroy(corridor.gameObject);
				}

				connectedRooms.Add(minLength.Key);
				_corridors.Add(minLength.Value);
			}
			yield return null;
		}

		public Vector3 CoordinatesToPosition(IntVector2 coordinates)
		{
			return new Vector3(coordinates.x - MapSize.x * 0.5f + 0.5f, 0f, coordinates.z - MapSize.z * 0.5f + 0.5f);
		}
	}
}