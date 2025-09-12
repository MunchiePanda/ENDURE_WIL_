using UnityEngine;
using System.Collections;

namespace ENDURE
{
	// This is the main manager that controls the entire dungeon generation process!
	// It sets up the map parameters and starts the generation.
	public class RoomMapManager : MonoBehaviour
	{
		public Map mapPrefap; // The map prefab we use to create dungeons
		private Map mapInstance; // The actual map instance we're working with

		public int MapSizeX; // How wide the map should be
		public int MapSizeZ; // How tall the map should be
		public int MaxRooms; // Maximum number of rooms to create
		public int MinRoomSize; // Smallest size a room can be
		public int MaxRoomSize; // Largest size a room can be

		public int TileSizeFactor = 1; // How big each tile should be
		public static int TileSize; // Static reference to tile size (used by other classes)

		// Start the dungeon generation when the game begins
		void Start()
		{
			TileSize = TileSizeFactor;
			BeginGame();
		}

		// Check for input to restart the dungeon
		void Update()
		{
			if (Input.GetKeyDown(KeyCode.Space))
			{
				RestartGame();
			}
		}

		// Create a new map and start generating the dungeon
		private void BeginGame()
		{
			mapInstance = Instantiate(mapPrefap);
			mapInstance.RoomCount = MaxRooms;
			mapInstance.MapSize = new IntVector2(MapSizeX, MapSizeZ);
			mapInstance.RoomSize.Min = MinRoomSize;
			mapInstance.RoomSize.Max = MaxRoomSize;
			TileSize = TileSizeFactor;

			StartCoroutine(mapInstance.Generate());
		}

		// Destroy the current map and create a new one
		private void RestartGame()
		{
			StopAllCoroutines();
			Destroy(mapInstance.gameObject);
			BeginGame();
		}
	}
}