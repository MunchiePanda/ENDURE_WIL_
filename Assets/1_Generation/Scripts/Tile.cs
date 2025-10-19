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
		
		[Header("Current State")]
		public float degradationLevel = 0f;
		public bool isBroken = false;
		public TileState currentState = TileState.Intact;
		
		private MeshRenderer meshRenderer;
		private Collider tileCollider;
		private Material originalMaterial;

		public void Start()
		{
			transform.localScale *= RoomMapManager.TileSize;
			
			// Get required components - look in children since they're on the Quad child object
			meshRenderer = GetComponentInChildren<MeshRenderer>();
			tileCollider = GetComponentInChildren<Collider>();
			
			if (meshRenderer == null) Debug.LogError("Tile missing MeshRenderer!");
			if (tileCollider == null) Debug.LogError("Tile missing Collider!");
			
			// Disable static batching to prevent material sharing
			gameObject.isStatic = false;
			if (meshRenderer != null)
			{
				meshRenderer.gameObject.isStatic = false;
				
				// Create a unique material instance for THIS tile only
				originalMaterial = new Material(meshRenderer.sharedMaterial);
				meshRenderer.material = originalMaterial;
				
				Debug.Log($"Tile {Coordinates} Start() - Created unique material instance ID: {originalMaterial.GetInstanceID()}");
			}
		}
		
		public void DegradeTile(float amount)
		{
			if (isBroken) return; // Can't degrade further if already broken
			
			degradationLevel += amount;
			degradationLevel = Mathf.Clamp01(degradationLevel);
			
			Debug.Log($"Tile {Coordinates} degraded by {amount}. Level: {degradationLevel:F2}");
			
			// Update visual appearance based on degradation level
			UpdateVisualState();
			
			// Update state machine
			if (degradationLevel >= 1f)
			{
				Debug.Log($"*** TILE {Coordinates} BREAKING! ***");
				SetState(TileState.Broken);
				BreakTile();
			}
			else if (degradationLevel > 0f)
			{
				SetState(TileState.Degrading);
			}
		}
		
		private void BreakTile()
		{
			Debug.Log($"=== BREAKING TILE {Coordinates} ===");
			Debug.Log($"About to destroy: {gameObject.name} (InstanceID: {gameObject.GetInstanceID()})");
			
			isBroken = true;
			
			if (tileCollider != null)
			{
				tileCollider.enabled = false;
			}
			
			StartCoroutine(DestroyTileAfterDelay());
		}
		
		private IEnumerator DestroyTileAfterDelay()
		{
			yield return new WaitForSeconds(0.1f);
			
			Debug.Log($"DESTROYING TILE {Coordinates} NOW - GameObject: {gameObject.name}");
			Destroy(this.gameObject);
		}
		
		private void OnDestroy()
		{
			Debug.Log($"Tile {Coordinates} OnDestroy called");
		}
		
		private void SetState(TileState newState)
		{
			if (currentState != newState)
			{
				TileState previousState = currentState;
				currentState = newState;
				Debug.Log($"Tile {Coordinates} state changed from {previousState} to {newState}");
			}
		}
		
		private void UpdateVisualState()
		{
			if (meshRenderer == null || originalMaterial == null) return;
			
			// ALWAYS create a new material instance to ensure no sharing
			Material currentMaterial = new Material(originalMaterial);
			meshRenderer.material = currentMaterial;
			
			// Adjust color based on degradation level
			Color baseColor = originalMaterial.color;
			Color degradedColor = Color.Lerp(baseColor, Color.gray, degradationLevel);
			currentMaterial.color = degradedColor;
			
			Debug.Log($"Tile {Coordinates} - NEW material instance created, color: {degradedColor} (degradation: {degradationLevel:F2})");
		}
		
	}
}