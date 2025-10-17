using UnityEngine;
using System.Collections;
using ENDURE;

namespace ENDURE
{
	public enum TileState
	{
		Intact,
		Degrading,
		Broken
	}

	public class Tile : MonoBehaviour
	{
		public IntVector2 Coordinates;
		
		[Header("Degradation Settings")]
		public float walkDegradationAmount = 0.2f;
		public float runDegradationAmount = 0.5f;
		public float regenerationDelay = 5f;
		public float regenerationSpeed = 0.1f;
		public bool enableRegeneration = true;
		
		[Header("Current State")]
		public float degradationLevel = 0f;
		public bool isBroken = false;
		public TileState currentState = TileState.Intact;
		
		private MeshRenderer meshRenderer;
		private Collider tileCollider;
		private Coroutine regenerationCoroutine;

		public void Start()
		{
			transform.localScale *= RoomMapManager.TileSize;
			
			// Get required components - look in children since they're on the Quad child object
			meshRenderer = GetComponentInChildren<MeshRenderer>();
			tileCollider = GetComponentInChildren<Collider>();
			
			if (meshRenderer == null) Debug.LogError("Tile missing MeshRenderer!");
			if (tileCollider == null) Debug.LogError("Tile missing Collider!");
		}
		
		public void DegradeTile(float amount)
		{
			if (isBroken) return; // Can't degrade further if already broken
			
			degradationLevel += amount;
			degradationLevel = Mathf.Clamp01(degradationLevel);
			
			Debug.Log($"Tile {Coordinates} degraded by {amount}. Level: {degradationLevel:F2}");
			
			if (degradationLevel >= 1f)
			{
				Debug.Log($"*** TILE {Coordinates} BREAKING! ***");
				BreakTile();
			}
		}
		
		private void BreakTile()
		{
			Debug.Log($"=== BREAKING TILE {Coordinates} - CHECKING PARENT HIERARCHY ===");
			
			// Debug the parent hierarchy
			Debug.Log($"Tile {Coordinates} parent: {(transform.parent != null ? transform.parent.name : "NULL")}");
			Debug.Log($"Tile {Coordinates} parent parent: {(transform.parent != null && transform.parent.parent != null ? transform.parent.parent.name : "NULL")}");
			Debug.Log($"Tile {Coordinates} parent parent parent: {(transform.parent != null && transform.parent.parent != null && transform.parent.parent.parent != null ? transform.parent.parent.parent.name : "NULL")}");
			
			// Check if parent has any components that might be shared
			if (transform.parent != null)
			{
				Debug.Log($"Parent '{transform.parent.name}' has {transform.parent.GetComponents<Component>().Length} components");
				foreach (Component comp in transform.parent.GetComponents<Component>())
				{
					Debug.Log($"  - {comp.GetType().Name}");
				}
			}
			
			// Test: Only disable the renderer
			if (meshRenderer != null) 
			{
				meshRenderer.enabled = false;
				Debug.Log($"Renderer disabled for tile {Coordinates}");
			}
		}
		
		private IEnumerator RegenerateCoroutine()
		{
			Debug.Log($"Tile {Coordinates} starting regeneration in {regenerationDelay} seconds...");
			yield return new WaitForSeconds(regenerationDelay);
			
			Debug.Log($"Tile {Coordinates} regenerating...");
			
			// Gradually restore the tile
			while (degradationLevel > 0f)
			{
				degradationLevel -= regenerationSpeed * Time.deltaTime;
				degradationLevel = Mathf.Clamp01(degradationLevel);
				yield return null;
			}
			
			// Fully restore the tile
			degradationLevel = 0f;
			isBroken = false;
			currentState = TileState.Intact;
			
			// Re-enable components
			if (tileCollider != null) tileCollider.enabled = true;
			if (meshRenderer != null) meshRenderer.enabled = true;
			
			Debug.Log($"Tile {Coordinates} fully regenerated!");
		}
	}
}