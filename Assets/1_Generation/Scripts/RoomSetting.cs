using UnityEngine;
using System;

namespace ENDURE
{
	/// <summary>
	/// Represents the visual settings for a room, including materials for floors, walls, and roofs.
	/// </summary>
	[Serializable]
	public class RoomSetting
	{
		/// <summary>
		/// Material for the floor of the room.
		/// </summary>
		public Material floor;

		/// <summary>
		/// Material for the walls of the room.
		/// </summary>
		public Material wall;

		/// <summary>
		/// Material for the roof of the room.
		/// </summary>
		public Material roof;
	}
}
