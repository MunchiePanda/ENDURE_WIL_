using UnityEngine;
using System;

namespace ENDURE
{
	// This class holds the visual settings for a room!
	// It defines what materials to use for floors and walls.
	[Serializable]
	public class RoomSetting
	{
		public Material floor, wall; // The materials to use for floor and wall tiles
	}
}