using UnityEngine;

public interface IInteractable
{
    string prompt { get; }                     // Text shown for interaction
    bool bIsInteractable { get; }         // Whether this object can currently be interacted with
    Collider interactCollider { get; }      // The collider used for interaction detection

    // Called when the interactor starts looking at this object
    void OnHoverEnter(Interactor interactor);

    // Called when the interactor stops looking at this object
    void OnHoverExit(Interactor interactor);

    // Called when the interactor presses the interact key
    void Interact(Interactor interactor);
}
