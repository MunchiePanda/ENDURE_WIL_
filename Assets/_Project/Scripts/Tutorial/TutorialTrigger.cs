using UnityEngine;

namespace ENDURE.Tutorial
{
    /// <summary>
    /// Simple trigger volume that notifies the TutorialManager when the player enters/exits.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TutorialTrigger : MonoBehaviour
    {
        public TutorialManager manager;
        [Tooltip("ID that must match the step's targetId to complete.")]
        public string triggerId = "Trigger";
        public bool triggerOnEnter = true;
        public bool triggerOnExit;
        public bool singleUse = true;

        private bool hasTriggered;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void Awake()
        {
            if (manager == null)
            {
                manager = FindObjectOfType<TutorialManager>();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!triggerOnEnter || hasTriggered && singleUse) return;
            if (!other.CompareTag("Player")) return;

            FireTrigger();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!triggerOnExit || hasTriggered && singleUse) return;
            if (!other.CompareTag("Player")) return;

            FireTrigger();
        }

        private void FireTrigger()
        {
            if (singleUse)
            {
                hasTriggered = true;
            }
            manager?.NotifyActionCompleted(triggerId);
        }
    }
}

