using UnityEngine;

/// <summary>
/// Interactable that transitions to a different scene when held for the required duration.
/// Works with existing Interactor system without modifications.
/// Can be used for any scene transition scenario (dungeon entrances, doors, portals, etc.)
/// </summary>
[RequireComponent(typeof(Collider))]
public class SceneTransitionInteractable : InteractableBase
{
    [Header("Scene Transition")]
    [Tooltip("Name of the scene to load (must be in Build Settings). Takes priority over build index.")]
    public string targetSceneName = "";

    [Tooltip("Build index of the scene to load (alternative to scene name). Used if scene name is empty.")]
    public int targetSceneBuildIndex = -1;

    [Header("Hold Interaction")]
    [Tooltip("How long the player must hold E to enter (in seconds)")]
    [SerializeField] private float holdDuration = 2f;

    [Header("Visual Feedback")]
    [Tooltip("Optional: Animation to play when interaction starts")]
    public Animation onInteractAnimation;

    [Tooltip("Optional: Progress bar fill image (for hold progress)")]
    public UnityEngine.UI.Image progressBarFill;

    [Tooltip("Optional: Container for progress bar UI")]
    public GameObject progressBarContainer;

    [Tooltip("Optional: Text to show progress percentage")]
    public TMPro.TextMeshProUGUI progressText;

    [Header("Player Control")]
    [Tooltip("Disable player movement while holding?")]
    public bool disablePlayerMovement = true;

    private HoldInteractionHandler holdHandler;
    private bool isTransitioning = false;

    protected override void Awake()
    {
        base.Awake();

        // Add or get hold interaction handler
        holdHandler = GetComponent<HoldInteractionHandler>();
        if (holdHandler == null)
        {
            holdHandler = gameObject.AddComponent<HoldInteractionHandler>();
        }

        // Configure hold handler
        holdHandler.holdDuration = holdDuration;
        holdHandler.progressBarFill = progressBarFill;
        holdHandler.progressBarContainer = progressBarContainer;
        holdHandler.progressText = progressText;
        holdHandler.disablePlayerMovement = disablePlayerMovement;
    }

    public override void Interact(Interactor interactor)
    {
        if (isTransitioning)
        {
            return;
        }

        // Start hold interaction
        holdHandler.StartHold(
            onComplete: () => OnHoldComplete(interactor),
            onCancel: () => OnHoldCancel(interactor)
        );

        // Play animation if available
        if (onInteractAnimation != null)
        {
            onInteractAnimation.Play();
        }
    }

    private void OnHoldComplete(Interactor interactor)
    {
        if (isTransitioning)
        {
            return;
        }

        isTransitioning = true;

        // Ensure SceneLoader exists
        SceneLoader loader = SceneLoader.Instance;
        if (loader == null)
        {
            GameObject loaderObj = new GameObject("SceneLoader");
            loader = loaderObj.AddComponent<SceneLoader>();
        }

        // Load the target scene
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            loader.LoadScene(targetSceneName);
        }
        else if (targetSceneBuildIndex >= 0)
        {
            loader.LoadScene(targetSceneBuildIndex);
        }
        else
        {
            Debug.LogError($"SceneTransitionInteractable: No valid scene target specified on {gameObject.name}. " +
                          "Please set either targetSceneName or targetSceneBuildIndex.");
            isTransitioning = false;
        }
    }

    private void OnHoldCancel(Interactor interactor)
    {
        // Stop animation if playing
        if (onInteractAnimation != null && onInteractAnimation.isPlaying)
        {
            onInteractAnimation.Stop();
        }
    }

    public override void OnHoverExit(Interactor interactor)
    {
        base.OnHoverExit(interactor);
        
        // Cancel hold if player looks away
        if (holdHandler != null && holdHandler.IsHolding)
        {
            holdHandler.CancelHold();
        }
    }

    private void OnValidate()
    {
        // Set default prompt if not set
        if (string.IsNullOrEmpty(uiPrompt))
        {
            uiPrompt = "Hold E to Enter";
        }
    }

    private void OnDestroy()
    {
        // Clean up hold handler
        if (holdHandler != null)
        {
            holdHandler.CancelHold();
        }
    }
}

