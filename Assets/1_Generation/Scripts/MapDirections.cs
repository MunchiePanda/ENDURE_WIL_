using UnityEngine;
using System.Collections;

namespace ENDURE
{
	// This enum defines the four cardinal directions we use in our dungeon
	public enum MapDirection
	{
		North, // Up
		East,  // Right
		South, // Down
		West   // Left
	}

	// This static class provides helper functions for working with directions
	public static class MapDirections
	{
		public const int Count = 4; // There are 4 directions

		// Array of all directions for easy iteration
		public static readonly MapDirection[] Directions =
		{
			MapDirection.North,
			MapDirection.East,
			MapDirection.South,
			MapDirection.West
		};

		// Convert directions to movement vectors (x, z coordinates)
		private static readonly IntVector2[] Vectors =
		{
			new IntVector2(0, 1),  // North: move up in Z
			new IntVector2(1, 0),  // East: move right in X
			new IntVector2(0, -1), // South: move down in Z
			new IntVector2(-1, 0), // West: move left in X
		};

		// Convert a direction to a movement vector
		public static IntVector2 ToIntVector2(this MapDirection direction)
		{
			return Vectors[(int) direction];
		}

		// Array of opposite directions
		private static readonly MapDirection[] Opposites =
		{
			MapDirection.South, // North's opposite
			MapDirection.West,  // East's opposite
			MapDirection.North, // South's opposite
			MapDirection.East   // West's opposite
		};

		// Get the opposite direction
		public static MapDirection GetOpposite(this MapDirection direction)
		{
			return Opposites[(int) direction];
		}

		// Convert directions to rotation quaternions for wall placement
		private static readonly Quaternion[] Rotations =
		{
			Quaternion.identity,           // North: no rotation
			Quaternion.Euler(0f, 90f, 0f),  // East: 90 degrees
			Quaternion.Euler(0f, 180f, 0f), // South: 180 degrees
			Quaternion.Euler(0f, 270f, 0f)  // West: 270 degrees
		};

		// Convert a direction to a rotation for wall placement
		public static Quaternion ToRotation(this MapDirection direction)
		{
			return Rotations[(int) direction];
		}
	}
}