using UnityEngine;

/// <summary>
/// Interactable that opens a UI screen/panel when interacted with.
/// Assign a scene UI GameObject to screenToOpen.
/// </summary>
public class UIScreenInteractable : InteractableBase
{
    [Header("Interaction - UI Screen")]
    public Animation onInteractAnimation;

    [Tooltip("UI GameObject in the scene to activate (e.g., inventory panel)")]
    public GameObject screenToOpen;

    [Tooltip("If true, toggles the screen on/off. If false, only opens it.")]
    public bool toggle = false;

    [Tooltip("If true and the screen isn't found under mainUI/interactor, instantiate the prefab under mainUI (or interactor Canvas).")]
    public bool instantiateIfMissing = false;

    public override void Interact(Interactor interactor)
    {
        if (onInteractAnimation != null) onInteractAnimation.Play();

        if (screenToOpen == null)
        {
            Debug.LogWarning($"UIScreenInteractable Interact: {nameof(UIScreenInteractable)} on {gameObject.name} has no screen assigned.");
            return;
        }

        // Delegate to the player's UI manager for screen handling
        var uiManager = interactor.GetComponentInChildren<UIManager>(true);
        if (uiManager == null)
        {
            Debug.LogWarning("UIScreenInteractable Interact: UIManager not found under Interactor.", this);
            return;
        }

        var instance = uiManager.OpenScreen(screenToOpen, toggle, instantiateIfMissing);
        if (instance == null)
        {
            Debug.LogWarning($"UIScreenInteractable Interact: Could not open screen '{screenToOpen.name}'.", this);
        }
    }
}


