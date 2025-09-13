using UnityEngine;
using System.Collections;
using ENDURE;

namespace ENDURE
{
	public class Tile : MonoBehaviour
	{
		public IntVector2 Coordinates;

		public void Start()
		{
			transform.localScale *= RoomMapManager.TileSize;
		}
	}
}