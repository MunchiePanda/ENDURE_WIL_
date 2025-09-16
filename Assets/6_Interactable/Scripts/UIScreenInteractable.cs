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

    public override void Interact(Interactor interactor)
    {
        if (onInteractAnimation != null) onInteractAnimation.Play();

        if (screenToOpen == null)
        {
            Debug.LogWarning($"UIScreenInteractable Interact: {nameof(UIScreenInteractable)} on {gameObject.name} has no screen assigned.");
            return;
        }

        if (toggle)
        {
            screenToOpen.SetActive(!screenToOpen.activeSelf);
        }
        else
        {
            screenToOpen.SetActive(true);
        }
    }
}


