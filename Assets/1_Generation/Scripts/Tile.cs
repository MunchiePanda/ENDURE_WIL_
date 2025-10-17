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
		public bool enableRegeneration = false; // Temporarily disable regeneration to test
		
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
			
			// Debug: Log each tile's unique identity
			Debug.Log($"TILE CREATED: {name} at {Coordinates} - Instance ID: {GetInstanceID()}");
			Debug.Log($"MeshRenderer Instance ID: {(meshRenderer != null ? meshRenderer.GetInstanceID() : -1)}");
			Debug.Log($"Collider Instance ID: {(tileCollider != null ? tileCollider.GetInstanceID() : -1)}");
		}
		
		public void DegradeTile(float amount)
		{
			if (isBroken) return; // Can't degrade further if already broken
			
			Debug.Log($"=== DEGRADING TILE {Coordinates} by {amount} ===");
			Debug.Log($"Tile Instance ID: {GetInstanceID()}");
			Debug.Log($"Previous degradation level: {degradationLevel}");
			
			degradationLevel += amount;
			degradationLevel = Mathf.Clamp01(degradationLevel);
			
			Debug.Log($"Tile {Coordinates} (ID: {GetInstanceID()}) degraded by {amount}. Current level: {degradationLevel}");
			
			if (degradationLevel >= 1f)
			{
				Debug.Log($"*** TILE {Coordinates} (ID: {GetInstanceID()}) REACHED BREAKING POINT! BREAKING NOW... ***");
				BreakTile();
			}
			else
			{
				Debug.Log($"Tile {Coordinates} still intact. Degradation: {degradationLevel}/1.0");
			}
		}
		
		private void BreakTile()
		{
			isBroken = true;
			currentState = TileState.Broken;
			
			Debug.Log($"=== BREAKING TILE {Coordinates} ===");
			Debug.Log($"Tile Instance ID: {GetInstanceID()}");
			Debug.Log($"GameObject Name: {gameObject.name}");
			Debug.Log($"Collider before: {(tileCollider != null ? tileCollider.enabled.ToString() : "NULL")}");
			Debug.Log($"Renderer before: {(meshRenderer != null ? meshRenderer.enabled.ToString() : "NULL")}");
			Debug.Log($"Renderer GameObject: {(meshRenderer != null ? meshRenderer.gameObject.name : "NULL")}");
			Debug.Log($"Renderer Material: {(meshRenderer != null ? meshRenderer.material.name : "NULL")}");
			
			// TEMPORARILY DISABLE ONLY THE COLLIDER TO TEST
			if (tileCollider != null) 
			{
				tileCollider.enabled = false;
				Debug.Log($"Collider disabled for tile {Coordinates} (ID: {GetInstanceID()})");
			}
			
			// DON'T DISABLE THE RENDERER YET - TEST IF COLLIDER ALONE CAUSES THE ISSUE
			// if (meshRenderer != null) 
			// {
			//     meshRenderer.enabled = false;
			//     Debug.Log($"Renderer disabled for tile {Coordinates} (ID: {GetInstanceID()})");
			// }
			
			Debug.Log($"Tile {Coordinates} (ID: {GetInstanceID()}) BROKEN! (Renderer still enabled for testing)");
			
			// Start regeneration if enabled
			if (enableRegeneration)
			{
				if (regenerationCoroutine != null)
					StopCoroutine(regenerationCoroutine);
				regenerationCoroutine = StartCoroutine(RegenerateCoroutine());
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