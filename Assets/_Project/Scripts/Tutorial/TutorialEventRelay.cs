using UnityEngine;

namespace ENDURE.Tutorial
{
    /// <summary>
    /// Simple helper to notify the current TutorialManager from other scripts/events.
    /// Attach to an object and call Trigger() (e.g., via UnityEvent) when an action is completed.
    /// </summary>
    public class TutorialEventRelay : MonoBehaviour
    {
        public TutorialManager manager;
        public string eventId = "Event";

        private void Awake()
        {
            if (manager == null)
            {
                manager = FindObjectOfType<TutorialManager>();
            }
        }

        public void Trigger()
        {
            manager?.NotifyActionCompleted(eventId);
        }
    }
}

