using UnityEngine;
using System.Collections;

namespace ENDURE
{
	// This class represents a single floor tile in our dungeon!
	// It's a simple component that just holds position information.
	public class Tile : MonoBehaviour
	{
		public IntVector2 Coordinates; // Where this tile is positioned on the map

		// This scales the tile to the correct size when it's created
		public void Start()
		{
			transform.localScale *= RoomMapManager.TileSize;
		}
	}
}