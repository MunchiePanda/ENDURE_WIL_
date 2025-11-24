using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using ENDURE;

namespace ENDURE
{
  public class Corridor : MonoBehaviour
	{
		private GameObject _tilesObject;
		private GameObject _wallsObject;
		public Tile TilePrefab;
		public GameObject WallPrefab;

		public Room[] Rooms = new Room[2];
		public List<Triangle> Triangles = new List<Triangle>();

		public float Length;
		public IntVector2 Coordinates; // Rooms[1].x , Rooms[0].z

		private Map _map;
		private List<Tile> _tiles;
		private GameObject _itemsObject;
		private GameObject _plantPropsObject;

		public void Init(Map map)
		{
			_map = map;
		}

		public IEnumerator Generate()
		{
			transform.localPosition *= RoomMapManager.TileSize;
			_tilesObject = new GameObject("Tiles");
			_tilesObject.transform.parent = transform;
			_tilesObject.transform.localPosition = Vector3.zero;

			// Seperate Corridor to room
			MoveStickedCorridor();

			_tiles = new List<Tile>();
			int start = Rooms[0].Coordinates.x + Rooms[0].Size.x / 2;
			int end = Coordinates.x;
			if (start > end)
			{
				int temp = start;
				start = end;
				end = temp;
			}
			for (int i = start; i <= end; i++)
			{
				Tile newTile = CreateTile(new IntVector2(i, Coordinates.z));
				if (newTile)
				{
					_tiles.Add(newTile);
				}
			}
			start = Rooms[1].Coordinates.z + Rooms[1].Size.z / 2;
			end = Coordinates.z;
			if (start > end)
			{
				int temp = start;
				start = end;
				end = temp;
			}
			for (int i = start; i <= end; i++)
			{
				Tile newTile = CreateTile(new IntVector2(Coordinates.x, i));
				if (newTile)
				{
					_tiles.Add(newTile);
				}
			}
			yield return null;
		}

		public void Show()
		{
			Debug.DrawLine(Rooms[0].transform.localPosition, transform.localPosition, Color.white, 3.5f);
			Debug.DrawLine(transform.localPosition, Rooms[1].transform.localPosition, Color.white, 3.5f);
		}

		private Tile CreateTile(IntVector2 coordinates)
		{
			if (_map.GetTileType(coordinates) == TileType.Empty)
			{
				_map.SetTileType(coordinates, TileType.Corridor);
			}
			else
			{
				return null;
			}
			Tile newTile = Instantiate(TilePrefab);
			newTile.Coordinates = coordinates;
			newTile.name = "Tile " + coordinates.x + ", " + coordinates.z;
			newTile.transform.parent = _tilesObject.transform;
    newTile.transform.localPosition = RoomMapManager.TileSize * new Vector3(coordinates.x - Coordinates.x + 0.5f, 0, coordinates.z - Coordinates.z + 0.5f);

    // Apply the floor material from the first room's settings - create instance to avoid sharing
    if (Rooms[0] != null && Rooms[0].Setting != null && Rooms[0].Setting.floor != null)
    {
        // Create a material instance so each tile has its own material
        Material tileMaterial = new Material(Rooms[0].Setting.floor);
        newTile.transform.GetChild(0).GetComponent<Renderer>().material = tileMaterial;
    }
    else
    {
        Debug.LogError("Floor material not set!");
    }

    return newTile;
		}

		private void MoveStickedCorridor()
		{
			IntVector2 correction = new IntVector2(0, 0);

			if (Rooms[0].Coordinates.x == Coordinates.x + 1)
			{
				// left 2
				correction.x = 2;
			}
			else if (Rooms[0].Coordinates.x + Rooms[0].Size.x == Coordinates.x)
			{
				// right 2
				correction.x = -2;
			}
			else if (Rooms[0].Coordinates.x == Coordinates.x)
			{
				// left
				correction.x = 1;
			}
			else if (Rooms[0].Coordinates.x + Rooms[0].Size.x == Coordinates.x + 1)
			{
				// right
				correction.x = -1;
			}

			if (Rooms[1].Coordinates.z == Coordinates.z + 1)
			{
				// Bottom 2
				correction.z = 2;
			}
			else if (Rooms[1].Coordinates.z + Rooms[1].Size.z == Coordinates.z)
			{
				// Top 2
				correction.z = -2;
			}
			else if (Rooms[1].Coordinates.z == Coordinates.z)
			{
				// Bottom
				correction.z = 1;
			}
			else if (Rooms[1].Coordinates.z + Rooms[1].Size.z == Coordinates.z + 1)
			{
				// Top
				correction.z = -1;
			}

			Coordinates += correction;
			transform.localPosition += RoomMapManager.TileSize * new Vector3(correction.x, 0f, correction.z);
		}

		public IEnumerator CreateWalls()
		{
			_wallsObject = new GameObject("Walls");
			_wallsObject.transform.parent = transform;
			_wallsObject.transform.localPosition = Vector3.zero;

			foreach (Tile tile in _tiles)
			{
				foreach (MapDirection direction in MapDirections.Directions)
				{
					IntVector2 coordinates = tile.Coordinates + direction.ToIntVector2();
					if (_map.GetTileType(coordinates) == TileType.Wall)
					{
						GameObject newWall = Instantiate(WallPrefab);
						newWall.name = "Wall (" + coordinates.x + ", " + coordinates.z + ")";
						newWall.transform.parent = _wallsObject.transform;
						newWall.transform.localPosition = RoomMapManager.TileSize * _map.CoordinatesToPosition(coordinates) - transform.localPosition;
						newWall.transform.localRotation = direction.ToRotation();
    newWall.transform.localScale *= RoomMapManager.TileSize;

    // Apply the wall material from the first room's settings
    if (Rooms[0] != null && Rooms[0].Setting != null && Rooms[0].Setting.wall != null)
    {
        newWall.transform.GetChild(0).GetComponent<Renderer>().material = Rooms[0].Setting.wall;
    }
    else
    {
        Debug.LogError("Wall material not set!");
    }
					}
				}
			}
			yield return null;
		}

		public IEnumerator CreateItems()
		{
			// Use item prefabs from the first room
			if (Rooms[0] != null && Rooms[0].itemPrefabs != null && Rooms[0].itemPrefabs.Length > 0)
			{
				_itemsObject = new GameObject("Items");
				_itemsObject.transform.parent = transform;
				_itemsObject.transform.localPosition = Vector3.zero;

				int itemCount = Random.Range(Rooms[0].minItemsPerRoom, Rooms[0].maxItemsPerRoom + 1);
				// Corridors are usually smaller, so reduce item count
				itemCount = Mathf.Max(0, itemCount / 2);

				List<Vector3> usedPositions = new List<Vector3>();
				float minDistanceBetweenItems = 1.5f; // Minimum distance between items
				int maxAttempts = 50; // Maximum attempts to find a valid position

				for (int i = 0; i < itemCount; i++)
				{
					if (_tiles == null || _tiles.Count == 0) break;

					GameObject itemPrefab = Rooms[0].itemPrefabs[Random.Range(0, Rooms[0].itemPrefabs.Length)];

					Vector3 localPosition = Vector3.zero;
					bool validPositionFound = false;
					int attempts = 0;

					while (!validPositionFound && attempts < maxAttempts)
					{
						// Pick a random tile from the corridor
						Tile randomTile = _tiles[Random.Range(0, _tiles.Count)];

						if (randomTile != null)
						{
							// Get tile's local position and add random offset
							Vector3 tileLocalPos = randomTile.transform.localPosition;
							localPosition = new Vector3(
								tileLocalPos.x + Random.Range(-0.4f, 0.4f),
								tileLocalPos.y, // On the floor
								tileLocalPos.z + Random.Range(-0.4f, 0.4f)
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
				}
			}

			yield return null;
		}

		public IEnumerator CreatePlantProps()
		{
			// Use plant prop prefabs from the first room
			if (Rooms[0] != null && Rooms[0].plantPropPrefabs != null && Rooms[0].plantPropPrefabs.Length > 0)
			{
				_plantPropsObject = new GameObject("PlantProps");
				_plantPropsObject.transform.parent = transform;
				_plantPropsObject.transform.localPosition = Vector3.zero;

				// Apply foliage density from first room
				int basePlantCount = Random.Range(Rooms[0].minPlantPropsPerRoom, Rooms[0].maxPlantPropsPerRoom + 1);
				// Corridors are usually smaller, so reduce plant count, then apply density
				int plantCount = Mathf.RoundToInt((basePlantCount / 2) * Rooms[0].foliageDensity);

				for (int i = 0; i < plantCount; i++)
				{
					if (_tiles == null || _tiles.Count == 0) break;

					GameObject plantPrefab = Rooms[0].plantPropPrefabs[Random.Range(0, Rooms[0].plantPropPrefabs.Length)];

					// Wild, unkept positioning - spread across corridor area, not tied to specific tiles
					// Calculate corridor bounds from tile positions
					float minX = float.MaxValue, maxX = float.MinValue;
					float minZ = float.MaxValue, maxZ = float.MinValue;
					
					foreach (Tile tile in _tiles)
					{
						Vector3 tilePos = tile.transform.localPosition;
						if (tilePos.x < minX) minX = tilePos.x;
						if (tilePos.x > maxX) maxX = tilePos.x;
						if (tilePos.z < minZ) minZ = tilePos.z;
						if (tilePos.z > maxZ) maxZ = tilePos.z;
					}
					
					// Spread plants across the corridor area with more randomness to break linearity
					float corridorWidth = maxX - minX;
					float corridorDepth = maxZ - minZ;
					
					// Add more randomness - don't just follow the corridor line
					// Use wider spread perpendicular to corridor direction to break linear pattern
					float widthSpread = Mathf.Max(corridorWidth, corridorDepth) * 0.6f; // Wider spread
					float depthSpread = Mathf.Max(corridorWidth, corridorDepth) * 0.6f;
					
					// Randomly offset from center to break linearity
					float centerX = (minX + maxX) * 0.5f;
					float centerZ = (minZ + maxZ) * 0.5f;
					
					Vector3 localPosition = new Vector3(
						centerX + Random.Range(-widthSpread, widthSpread),
						0f, // On the floor
						centerZ + Random.Range(-depthSpread, depthSpread)
					);

					GameObject newPlant = Instantiate(plantPrefab);
					newPlant.name = $"PlantProp {i + 1}";
					newPlant.transform.parent = _plantPropsObject.transform;
					newPlant.transform.localPosition = localPosition;

					// Remove all colliders from plant props
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
	}
}
