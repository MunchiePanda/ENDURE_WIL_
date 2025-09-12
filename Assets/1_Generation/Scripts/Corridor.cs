using System.Collections;
using UnityEngine;
using System.Collections.Generic;

namespace ENDURE
{
	// This class represents a corridor that connects two rooms in our dungeon!
	// It's like a hallway that goes from one room to another.
	public class Corridor : MonoBehaviour
	{
		private GameObject _tilesObject; // The container for all our corridor floor tiles
		private GameObject _wallsObject; // The container for all our corridor walls
		private GameObject _roofObject; // The container for all our corridor roof tiles
		public Tile TilePrefab; // The floor tile we use to build the corridor
		public GameObject WallPrefab; // The wall piece we use around the corridor
		public GameObject RoofPrefab; // The roof piece we use above the corridor

		public Room[] Rooms = new Room[2]; // The two rooms this corridor connects
		public List<Triangle> Triangles = new List<Triangle>(); // Used for Delaunay triangulation

		public float Length; // How long this corridor is
		public IntVector2 Coordinates; // Where this corridor is positioned on the map

		private Map _map; // Reference to the main map that owns this corridor
		private List<Tile> _tiles; // All the floor tiles that make up this corridor

		// This sets up the corridor with a reference to the main map
		public void Init(Map map)
		{
			_map = map;
		}

		// This builds the corridor by placing floor tiles between the two rooms
		public IEnumerator Generate()
		{
			transform.localPosition *= RoomMapManager.TileSize;
			_tilesObject = new GameObject("Tiles");
			_tilesObject.transform.parent = transform;
			_tilesObject.transform.localPosition = Vector3.zero;

			// Make sure the corridor doesn't overlap with the rooms
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

		// This draws debug lines to show how the corridor connects the rooms
		public void Show()
		{
			Debug.DrawLine(Rooms[0].transform.localPosition, transform.localPosition, Color.white, 3.5f);
			Debug.DrawLine(transform.localPosition, Rooms[1].transform.localPosition, Color.white, 3.5f);
		}

		// This creates a floor tile at the specified coordinates
		private Tile CreateTile(IntVector2 coordinates)
		{
			if (_map.GetTileType(coordinates) == TileType.Empty)
			{
				_map.SetTileType(coordinates, TileType.Corridor);
			}
			else
			{
				return null; // Can't place tile here, it's already occupied
			}
			Tile newTile = Instantiate(TilePrefab);
			newTile.Coordinates = coordinates;
			newTile.name = "Tile " + coordinates.x + ", " + coordinates.z;
			newTile.transform.parent = _tilesObject.transform;
			newTile.transform.localPosition = RoomMapManager.TileSize * new Vector3(coordinates.x - Coordinates.x + 0.5f, 0, coordinates.z - Coordinates.z + 0.5f);
			return newTile;
		}

		// This adjusts the corridor position to avoid overlapping with rooms
		private void MoveStickedCorridor()
		{
			IntVector2 correction = new IntVector2(0, 0);

			// Check if corridor is too close to the first room and adjust
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

			// Check if corridor is too close to the second room and adjust
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

			// Apply the correction to both coordinates and position
			Coordinates += correction;
			transform.localPosition += RoomMapManager.TileSize * new Vector3(correction.x, 0f, correction.z);
		}

		// This creates walls around the corridor where needed
		public IEnumerator CreateWalls()
		{
			_wallsObject = new GameObject("Walls");
			_wallsObject.transform.parent = transform;
			_wallsObject.transform.localPosition = Vector3.zero;

			// For each tile in the corridor, check all directions for walls
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
					}
				}
			}
			yield return null;
		}

		// This creates a roof above the corridor
		public IEnumerator CreateRoof()
		{
			if (RoofPrefab == null) yield break;

			_roofObject = new GameObject("Roof");
			_roofObject.transform.parent = transform;
			_roofObject.transform.localPosition = Vector3.zero;

			// Create roof tiles above each floor tile
			foreach (Tile tile in _tiles)
			{
				GameObject newRoof = Instantiate(RoofPrefab);
				newRoof.name = "Roof (" + tile.Coordinates.x + ", " + tile.Coordinates.z + ")";
				newRoof.transform.parent = _roofObject.transform;
				newRoof.transform.localPosition = tile.transform.localPosition + new Vector3(0, RoomMapManager.TileSize, 0);
				newRoof.transform.localScale *= RoomMapManager.TileSize;
			}
			yield return null;
		}
	}
}