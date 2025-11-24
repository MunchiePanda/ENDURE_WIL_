using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
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

        [Header("Steps")]
        public List<TutorialStep> steps = new List<TutorialStep>();

        [Header("Input Settings")]
        public bool autoStartOnAwake = true;
        public KeyCode torchKey = KeyCode.F;
        public KeyCode inventoryKey = KeyCode.I;
        public KeyCode interactKey = KeyCode.E;

        private int currentStepIndex = -1;
        private TutorialStep activeStep;
        private bool tutorialRunning;

        // Tracking for move step
        private readonly HashSet<KeyCode> moveKeysPressed = new HashSet<KeyCode>();

        private void Awake()
        {
            if (autoStartOnAwake)
            {
                StartTutorial();
            }
        }

        public void StartTutorial()
        {
            currentStepIndex = -1;
            moveKeysPressed.Clear();
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
            if (promptText != null)
            {
                promptText.text = string.Empty;
            }
        }

        private void AdvanceStep()
        {
            currentStepIndex++;
            moveKeysPressed.Clear();

            if (currentStepIndex >= steps.Count)
            {
                tutorialRunning = false;
                activeStep = null;
                if (promptText != null)
                {
                    promptText.text = string.Empty;
                }
                return;
            }

            activeStep = steps[currentStepIndex];
            tutorialRunning = true;
            if (promptText != null)
            {
                promptText.text = activeStep.message;
            }

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
            tutorialRunning = false;
            AdvanceStep();
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

