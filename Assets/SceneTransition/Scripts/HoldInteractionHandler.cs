using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Helper component that handles hold-to-interact logic with visual feedback.
/// Can be attached to any interactable that needs hold functionality.
/// Works independently without modifying existing systems.
/// </summary>
public class HoldInteractionHandler : MonoBehaviour
{
    [Header("Hold Settings")]
    [Tooltip("How long the player must hold the interact key (in seconds)")]
    [SerializeField] public float holdDuration = 2f;

    [Header("Visual Feedback")]
    [Tooltip("Optional: Progress bar to show hold progress")]
    public Image progressBarFill;
    
    [Tooltip("Optional: Container GameObject for progress bar (will be shown/hidden)")]
    public GameObject progressBarContainer;
    
    [Tooltip("Optional: Text to update with progress")]
    public TextMeshProUGUI progressText;

    [Header("Player Control")]
    [Tooltip("Disable player movement while holding?")]
    public bool disablePlayerMovement = true;

    private Coroutine holdCoroutine;
    private bool isHolding = false;
    private float currentHoldProgress = 0f;

    public float HoldDuration => holdDuration;
    public bool IsHolding => isHolding;
    public float CurrentProgress => currentHoldProgress;

    /// <summary>
    /// Start the hold interaction. Call this from Interact() method.
    /// </summary>
    public void StartHold(System.Action onComplete, System.Action onCancel = null)
    {
        if (isHolding)
        {
            return; // Already holding
        }

        if (holdCoroutine != null)
        {
            StopCoroutine(holdCoroutine);
        }

        holdCoroutine = StartCoroutine(HoldCoroutine(onComplete, onCancel));
    }

    /// <summary>
    /// Cancel the current hold interaction
    /// </summary>
    public void CancelHold()
    {
        if (holdCoroutine != null)
        {
            StopCoroutine(holdCoroutine);
            holdCoroutine = null;
        }

        ResetHoldState();
    }

    private IEnumerator HoldCoroutine(System.Action onComplete, System.Action onCancel)
    {
        isHolding = true;
        currentHoldProgress = 0f;

        // Disable player movement if requested
        var playerController = FindObjectOfType<ENDURE.PlayerController>();
        bool playerWasMovable = true;
        if (disablePlayerMovement && playerController != null)
        {
            playerWasMovable = playerController.canMove;
            playerController.canMove = false;
        }

        // Show progress bar
        if (progressBarContainer != null)
        {
            progressBarContainer.SetActive(true);
        }

        // Hold loop
        while (currentHoldProgress < 1f)
        {
            // Check if E key is still being held
            if (Keyboard.current == null || !Keyboard.current.eKey.isPressed)
            {
                // Key released - cancel
                if (onCancel != null)
                {
                    onCancel.Invoke();
                }

                // Restore player movement
                if (disablePlayerMovement && playerController != null)
                {
                    playerController.canMove = playerWasMovable;
                }

                ResetHoldState();
                yield break;
            }

            // Update progress
            currentHoldProgress += Time.deltaTime / holdDuration;
            currentHoldProgress = Mathf.Clamp01(currentHoldProgress);

            // Update UI
            UpdateProgressUI();

            yield return null;
        }

        // Hold complete!
        if (onComplete != null)
        {
            onComplete.Invoke();
        }

        // Restore player movement
        if (disablePlayerMovement && playerController != null)
        {
            playerController.canMove = playerWasMovable;
        }

        ResetHoldState();
    }

    private void UpdateProgressUI()
    {
        if (progressBarFill != null)
        {
            progressBarFill.fillAmount = currentHoldProgress;
        }

        if (progressText != null)
        {
            progressText.text = $"{Mathf.RoundToInt(currentHoldProgress * 100)}%";
        }
    }

    private void ResetHoldState()
    {
        isHolding = false;
        currentHoldProgress = 0f;
        holdCoroutine = null;

        if (progressBarContainer != null)
        {
            progressBarContainer.SetActive(false);
        }

        if (progressBarFill != null)
        {
            progressBarFill.fillAmount = 0f;
        }

        if (progressText != null)
        {
            progressText.text = "";
        }
    }

    private void OnDisable()
    {
        CancelHold();
    }
}


