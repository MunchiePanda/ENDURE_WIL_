using UnityEngine;

namespace ENDURE.Tutorial
{
    /// <summary>
    /// Simple interactable that forwards player interactions to the TutorialManager.
    /// </summary>
    public class TutorialGiverInteractable : InteractableBase
    {
        public TutorialManager tutorialManager;

        protected override void Awake()
        {
            base.Awake();
            if (tutorialManager == null)
            {
                tutorialManager = FindObjectOfType<TutorialManager>(true);
            }
        }

        public override void Interact(Interactor interactor)
        {
            if (tutorialManager == null) return;
            tutorialManager.OnDialogueInteract();
        }
    }
}

