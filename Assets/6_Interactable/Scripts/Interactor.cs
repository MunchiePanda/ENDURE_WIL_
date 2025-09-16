using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Casts a ray from the camera center to find the closest object implementing IInteractable.
/// Updates the prompt text and invokes hover/interaction events. (D6)
/// </summary>
public class Interactor : MonoBehaviour
{
    // Placed on Player/Main Camera
    [Header("Casting")]
    private Camera cam;
    [SerializeField, Min(0.1f)] float maxDist = 3f;
    [SerializeField] LayerMask interactMask;                 // The Layer that all interactable objects will be on
    [SerializeField] private string requiredLayerName = "InteractableObjects"; // Name of the required layer

    [Header("UI")]
    public TextMeshProUGUI promptText;       // Displays current interact prompt

    // Internal state
    private readonly RaycastHit[] hits = new RaycastHit[5];     // Reads 5 closest colliders
    private IInteractable interactable;                         // Current interact target

    private void Start()
    {
        // Cache camera reference once (WHY: avoid repeated Camera.main lookups every frame)
        cam = Camera.main;
    }

#if UNITY_EDITOR
    private void OnValidate()   // Added for editor validation so that mistakes are less likely ~S
    {
        // Ensure the raycast LayerMask targets the configured layer (WHY: consistency with interactables)
        if (!string.IsNullOrEmpty(requiredLayerName))
        {
            int layer = LayerMask.NameToLayer(requiredLayerName);
            if (layer == -1)
            {
                Debug.LogWarning($"Interactor: Layer '{requiredLayerName}' not found. Create it under Project Settings > Tags and Layers.", this);
            }
            else
            {
                // Force mask to exactly this single layer
                interactMask = (1 << layer);
            }
        }
    }
#endif

    private void Update()
    {
        // Raycast from screen center (WHY: simulate crosshair interaction)
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        // Non-alloc raycast for GC-friendly queries (using raycast ray, saving to hits array, looking only on interactMask)
        int count = Physics.RaycastNonAlloc(ray, hits, maxDist, interactMask, QueryTriggerInteraction.Ignore);
        
        // Find the closest IInteractable among hits
        IInteractable found = null;     //Found is the closest IInteractable found by the raycast
        float best = float.MaxValue;    //Best is the distance to found

        for (int i = 0; i < count; i++) //for every hit, check if it is an IInteractable
        {
            var hit = hits[i];
            if (!hit.collider) continue; // If no collider, skip the rest of the loop (WHY: invalid hit)

            var candidate = hit.collider.GetComponentInParent<IInteractable>();
            if (candidate == null) continue;            // If not interactable, skip this one
            if (!candidate.bIsInteractable) continue;   // If not currently interactable, skip this one

            // Choose the nearest interactable (WHY: prefer closest target when multiple are in view)
            if (hit.distance < best)
            {
                best = hit.distance;
                found = candidate;
            }

            // Clear used entry (optional hygiene)
            hits[i] = default;
        }

        // If the target changed, notify previous and new targets and update prompt
        if (!ReferenceEquals(interactable, found))
        {
            if (interactable != null)
            {
                // Tell old target we are no longer looking at it (WHY: allow target to revert hover state)
                interactable.OnHoverExit(this);
                if (promptText != null) promptText.text = string.Empty;
            }

            // Set new current target
            interactable = found;

            if (interactable != null)
            {
                // Tell new target we are looking at it (WHY: allow target to show hover state)
                interactable.OnHoverEnter(this);
                if (promptText != null) promptText.text = interactable.prompt;
            }
        }

        // If the interact key was pressed this frame, attempt interaction
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) TryInteract();
    }

    /// <summary>
    /// Attempts to invoke Interact on the current target, or logs a helpful message. (D6)
    /// </summary>
    private void TryInteract()
    {
        if (interactable != null)   //If the interactable is not null, call the Interact method on the interactable
        {
            interactable.Interact(this);
        }
        else
        {
            Debug.LogWarning("Interactor TryInteract(): Not looking at anything interactable (D6)");
        }
    }
}

/* MOVED TO ITS OWN SCRIPT ~S
public interface IInteractable
{
    public string prompt {  get;}     //text that pops up on HUD/Canvas that shows the player how to interact with the object

    void OnHoverEnter(Interactor interactor);       //when raycast hits but no message has been made.
    void OnHoverExit(Interactor interactor);        //when raycast leaves unprompted

    void Interact(Interactor interactor);
}
*/

