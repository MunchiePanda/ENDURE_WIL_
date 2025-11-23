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
		
		[Header("Regeneration Settings")]
		[Tooltip("How fast the tile regenerates per second (0.1 = 10% per second)")]
		public float regenerationRate = 0.1f;
		
		[Tooltip("How long to wait after last step before regeneration starts (in seconds)")]
		public float regenerationDelay = 2f;
		
		[Header("Current State")]
		public float degradationLevel = 0f;
		public bool isBroken = false;
		public TileState currentState = TileState.Intact;
		
		private MeshRenderer meshRenderer;
		private Collider tileCollider;
		private Material originalMaterial;
		private float lastDegradationTime = 0f;
		private bool isRegenerating = false;

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
			
			lastDegradationTime = Time.time;
			isRegenerating = false;
			
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
			else
			{
				SetState(TileState.Intact);
			}
		}
		
		void Update()
		{
			// Don't regenerate if broken
			if (isBroken) return;
			
			// Check if enough time has passed since last degradation to start regenerating
			if (degradationLevel > 0f && Time.time - lastDegradationTime >= regenerationDelay)
			{
				if (!isRegenerating)
				{
					isRegenerating = true;
					Debug.Log($"Tile {Coordinates} started regenerating");
				}
				
				// Regenerate over time
				degradationLevel -= regenerationRate * Time.deltaTime;
				degradationLevel = Mathf.Clamp01(degradationLevel);
				
				// Update visual state as it regenerates
				UpdateVisualState();
				
				// If fully regenerated, return to intact state
				if (degradationLevel <= 0f)
				{
					degradationLevel = 0f;
					SetState(TileState.Intact);
					isRegenerating = false;
					Debug.Log($"Tile {Coordinates} fully regenerated");
				}
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