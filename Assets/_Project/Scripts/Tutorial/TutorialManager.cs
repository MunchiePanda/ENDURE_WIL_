using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ENDURE.Tutorial
{
    public enum TutorialCondition
    {
        None,
        MoveWASD,
        Jump,
        ToggleTorch,
        OpenInventory,
        Interact,
        CustomEvent,
        Trigger
    }

    [Serializable]
    public class TutorialStep
    {
        [Tooltip("Internal identifier (optional) for reference in code or analytics.")]
        public string stepId;

        [TextArea]
        public string message;

        public TutorialCondition condition;

        [Tooltip("Optional ID used for CustomEvent / Trigger steps.")]
        public string targetId;

        [Tooltip("Optional sprite shown in the dialogue panel for this step.")]
        public Sprite displaySprite;

        [Tooltip("If false, the Continue button is enabled immediately and no condition is checked.")]
        public bool requiresCondition = true;

        [Tooltip("Invoked when the step becomes active (use to enable props, doors, etc.).")]
        public UnityEvent onStepStart;
    }

    /// <summary>
    /// Manages a sequence of tutorial steps and displays prompts to the player.
    /// Supports basic input-driven steps plus custom triggers via TutorialTrigger/TutorialEventRelay.
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        [Header("UI")]
        [Tooltip("Text component that shows the current tutorial message.")]
        public TMPro.TextMeshProUGUI promptText;
        [Tooltip("Optional dialogue panel UI that shows images and a continue button.")]
        public TutorialDialoguePanel dialoguePanel;
        [Tooltip("Reference to the player controller so we can toggle cursor/input when the dialogue is open.")]
        public ENDURE.PlayerController playerController;
        [Tooltip("Persistent objective text shown while the player works on a step. If null, promptText is used.")]
        public TMPro.TextMeshProUGUI objectiveText;

        [Header("Steps")]
        public List<TutorialStep> steps = new List<TutorialStep>();

        [Header("Input Settings")]
        public bool autoStartOnAwake = true;
        public KeyCode torchKey = KeyCode.F;
        public KeyCode inventoryKey = KeyCode.I;
        public KeyCode interactKey = KeyCode.E;

        [Header("Completion")]
        [Tooltip("Invoked when all tutorial steps have been completed.")]
        public UnityEvent onTutorialCompleted;
        [Tooltip("If true, automatically load the specified scene after the final step.")]
        public bool loadNextSceneOnComplete = false;
        [Tooltip("Scene name to load when the tutorial finishes.")]
        public string nextSceneName;

        private int currentStepIndex = -1;
        private TutorialStep activeStep;
        private bool tutorialRunning;
        private bool stepReadyToContinue;
        private bool dialogueOpen;

        // Tracking for move step
        private readonly HashSet<KeyCode> moveKeysPressed = new HashSet<KeyCode>();

        private void Awake()
        {
            if (playerController == null)
            {
                playerController = FindObjectOfType<ENDURE.PlayerController>();
            }

            dialoguePanel?.Hide();

            if (autoStartOnAwake)
            {
                StartTutorial();
            }
        }
        public void StartTutorial()
        {
            currentStepIndex = -1;
            moveKeysPressed.Clear();
            stepReadyToContinue = false;
            dialogueOpen = false;
            CloseDialogueInternal();
            AdvanceStep();
        }

        public void SkipToStep(int stepIndex)
        {
            currentStepIndex = Mathf.Clamp(stepIndex - 1, -1, steps.Count - 1);
            moveKeysPressed.Clear();
            AdvanceStep();
        }

        public void StopTutorial()
        {
            tutorialRunning = false;
            activeStep = null;
            currentStepIndex = -1;
            stepReadyToContinue = false;
            dialogueOpen = false;
            SetObjectiveText(string.Empty);
            CloseDialogueInternal();
        }

        private void AdvanceStep()
        {
            currentStepIndex++;
            moveKeysPressed.Clear();
            stepReadyToContinue = false;

            if (currentStepIndex >= steps.Count)
            {
                HandleTutorialComplete();
                return;
            }

            activeStep = steps[currentStepIndex];
            tutorialRunning = true;
            CloseDialogueInternal();
            SetObjectiveText(activeStep.message);
            stepReadyToContinue = !activeStep.requiresCondition;
            UpdateHintVisibility();

            activeStep.onStepStart?.Invoke();
        }

        private void Update()
        {
            if (!tutorialRunning || activeStep == null)
            {
                return;
            }

            switch (activeStep.condition)
            {
                case TutorialCondition.None:
                    CompleteCurrentStep();
                    break;

                case TutorialCondition.MoveWASD:
                    MonitorMovementInput();
                    break;

                case TutorialCondition.Jump:
                    if (Input.GetButtonDown("Jump"))
                    {
                        CompleteCurrentStep();
                    }
                    break;

                case TutorialCondition.ToggleTorch:
                    if (Input.GetKeyDown(torchKey))
                    {
                        CompleteCurrentStep();
                    }
                    break;

                case TutorialCondition.OpenInventory:
                    if (Input.GetKeyDown(inventoryKey))
                    {
                        CompleteCurrentStep();
                    }
                    break;

                case TutorialCondition.Interact:
                    if (Input.GetKeyDown(interactKey))
                    {
                        CompleteCurrentStep();
                    }
                    break;

                case TutorialCondition.CustomEvent:
                case TutorialCondition.Trigger:
                    // Wait for NotifyActionCompleted to be called externally.
                    break;
            }

            if (dialogueOpen && stepReadyToContinue && Input.GetKeyDown(KeyCode.Return))
            {
                ContinueCurrentStep();
            }
        }

        private void MonitorMovementInput()
        {
            if (Input.GetKeyDown(KeyCode.W))
            {
                moveKeysPressed.Add(KeyCode.W);
            }
            if (Input.GetKeyDown(KeyCode.S))
            {
                moveKeysPressed.Add(KeyCode.S);
            }
            if (Input.GetKeyDown(KeyCode.A))
            {
                moveKeysPressed.Add(KeyCode.A);
            }
            if (Input.GetKeyDown(KeyCode.D))
            {
                moveKeysPressed.Add(KeyCode.D);
            }

            if (moveKeysPressed.Contains(KeyCode.W) &&
                moveKeysPressed.Contains(KeyCode.A) &&
                moveKeysPressed.Contains(KeyCode.S) &&
                moveKeysPressed.Contains(KeyCode.D))
            {
                CompleteCurrentStep();
            }
        }

        private void CompleteCurrentStep()
        {
            if (!tutorialRunning || activeStep == null)
            {
                return;
            }

            stepReadyToContinue = true;
            SetObjectiveText(string.Empty);
            UpdateHintVisibility();

            if (!activeStep.requiresCondition)
            {
                // Informational steps that already allowed continue shouldn't auto-advance.
                return;
            }

            // Wait for player to press continue button.
        }

        private void HandleTutorialComplete()
        {
            tutorialRunning = false;
            activeStep = null;
            SetObjectiveText(string.Empty);
            CloseDialogueInternal();

            onTutorialCompleted?.Invoke();

            if (loadNextSceneOnComplete && !string.IsNullOrEmpty(nextSceneName))
            {
                TryLoadNextScene();
            }
        }

        /// <summary>
        /// Call this from TutorialTrigger or TutorialEventRelay when external actions happen.
        /// </summary>
        /// <param name="eventId">Identifier that matches the current step's targetId.</param>
        public void NotifyActionCompleted(string eventId)
        {
            if (!tutorialRunning || activeStep == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(activeStep.targetId))
            {
                return;
            }

            if (string.Equals(activeStep.targetId, eventId, StringComparison.OrdinalIgnoreCase))
            {
                CompleteCurrentStep();
            }
        }

        public void ContinueCurrentStep()
        {
            if (!tutorialRunning || activeStep == null)
            {
                return;
            }

            if (!stepReadyToContinue)
            {
                return;
            }

            tutorialRunning = false;
            CloseDialogueInternal();
            AdvanceStep();
        }

        public void CloseDialoguePanel()
        {
            CloseDialogueInternal();
        }

        private void TryLoadNextScene()
        {
            SceneLoader loader = SceneLoader.Instance;
            if (loader == null)
            {
                loader = FindObjectOfType<SceneLoader>(true);
            }

            if (loader != null)
            {
                loader.LoadScene(nextSceneName);
                return;
            }

            try
            {
                SceneManager.LoadScene(nextSceneName);
            }
            catch (Exception ex)
            {
                Debug.LogError($"TutorialManager: Failed to load scene '{nextSceneName}'. Exception: {ex.Message}");
            }
        }
        
        public void OnDialogueInteract()
        {
            if (!tutorialRunning || activeStep == null || dialoguePanel == null)
            {
                return;
            }

            if (dialogueOpen)
            {
                CloseDialogueInternal();
            }
            else
            {
                OpenDialogueInternal();
            }
        }

        private void OpenDialogueInternal()
        {
            if (dialoguePanel == null || activeStep == null)
            {
                return;
            }

            dialogueOpen = true;
            dialoguePanel.ShowStep(activeStep.message, activeStep.displaySprite);
            UpdateHintVisibility();
            playerController?.SetState(ENDURE.PlayerController.PlayerState.UI);
        }

        private void CloseDialogueInternal()
        {
            bool wasOpen = dialogueOpen;
            dialogueOpen = false;
            dialoguePanel?.Hide();
            playerController?.SetState(ENDURE.PlayerController.PlayerState.Playing);

            if (wasOpen)
            {
                UpdateHintVisibility();
            }
        }

        private void SetObjectiveText(string text)
        {
            string content = text ?? string.Empty;

            if (objectiveText != null)
            {
                objectiveText.text = content;
            }

            if (promptText != null)
            {
                promptText.text = content;
            }
        }

        private void UpdateHintVisibility()
        {
            dialoguePanel?.SetContinueHintVisible(dialogueOpen && stepReadyToContinue);
        }
        

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (steps == null)
            {
                steps = new List<TutorialStep>();
            }
        }

        [ContextMenu("Auto-Focus Prompt Text")]
        private void FindPromptText()
        {
            if (promptText == null)
            {
                promptText = GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (promptText == null)
                {
                    Debug.LogWarning("TutorialManager: No TextMeshProUGUI found in children.");
                }
            }
        }
#endif
    }
}


