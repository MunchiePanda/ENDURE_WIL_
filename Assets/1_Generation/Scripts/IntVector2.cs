using UnityEngine;

namespace ENDURE
{
	// This is a simple 2D integer vector that we use for grid coordinates!
	// It's like Vector2 but with integers instead of floats, perfect for tile-based maps.
	[System.Serializable]
	public struct IntVector2
	{
		public int x, z; // The x and z coordinates (we use z instead of y for 2D maps)

		// Constructor to create a new IntVector2
		public IntVector2(int x, int z)
		{
			this.x = x;
			this.z = z;
		}

		// Add two IntVector2s together
		public static IntVector2 operator +(IntVector2 a, IntVector2 b)
		{
			a.x += b.x;
			a.z += b.z;
			return a;
		}

		// Subtract one IntVector2 from another
		public static IntVector2 operator -(IntVector2 a, IntVector2 b)
		{
			a.x -= b.x;
			a.z -= b.z;
			return a;
		}

		// Add an IntVector2 to a Vector3 (useful for positioning in 3D space)
		public static Vector3 operator +(Vector3 a, IntVector2 b)
		{
			a.x += b.x;
			a.z += b.z;
			return a;
		}
	}
}