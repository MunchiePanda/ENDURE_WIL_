using UnityEngine;

/// <summary>
///    This is the base class for all interactable objects.
///    It implements the IInteractable interface and provides shared interaction setup.
/// </summary>

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    [Header("IInteractable Requirements")]
    public string uiPrompt;

    [SerializeField] public bool isInteractable = true;        //whether the object can be interacted with
    [SerializeField] private Collider interactableCollider;     //the collider that is used for interaction detection (raycast)

    [Header("Layer Settings")]
    [SerializeField] private string requiredLayerName = "InteractableObjects";
    [SerializeField] private bool enforceLayerInEditor = true;

    // IInteractable required members
    public string prompt => uiPrompt;
    public bool bIsInteractable => isInteractable;
    public Collider interactCollider => interactableCollider;

    protected virtual void Awake()
    {
        interactableCollider = TryGetComponent<Collider>(out var col) ? col : gameObject.AddComponent<SphereCollider>();
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        // Refresh collider reference in the editor
        if (interactableCollider == null)
        {
            TryGetComponent<Collider>(out interactableCollider);
        }

        // Enforce layer to match Interactor's LayerMask
        if (enforceLayerInEditor && !string.IsNullOrEmpty(requiredLayerName))
        {
            int layer = LayerMask.NameToLayer(requiredLayerName);
            if (layer == -1)
            {
                Debug.LogWarning($"InteractableBase: Layer '{requiredLayerName}' not found. Create it under Project Settings > Tags and Layers.", this);
            }
            else if (gameObject.layer != layer)
            {
                gameObject.layer = layer;
            }
        }

        // Ensure required serialized fields are set up
        AddOrCreateSerilizedFields();
    }
#endif

    private void AddOrCreateSerilizedFields()
    {
        // Ensure Collider exists
        if (interactableCollider == null)
        {
            interactableCollider = TryGetComponent<Collider>(out var col) ? col : gameObject.AddComponent<SphereCollider>();
        }
    }

    void Start()
    {
        isInteractable = true;  //always default to true
    }

    public virtual void OnHoverEnter(Interactor interactor)
    {
    }

    public virtual void OnHoverExit(Interactor interactor)
    {
    }

    public abstract void Interact(Interactor interactor);
}


