using UnityEngine;

namespace ENDURE.Tutorial
{
    /// <summary>
    /// Quick helper that spawns the tutorial giver, optional pickup, and wires up camera cues.
    /// Intended as a temporary auto-setup while the level generator is still in flux.
    /// </summary>
    public class TutorialAutoSetup : MonoBehaviour
    {
        [Header("References")]
        public TutorialManager tutorialManager;
        public TutorialCameraCue cameraCue;

        [Header("Tutorial NPC")]
        public GameObject tutorialGiverPrefab;
        public Transform tutorialGiverSpawn;

        [Header("Pickup")]
        public GameObject pickupPrefab;
        public Transform pickupSpawn;

        [Tooltip("Optional override for where the camera should look when highlighting the pickup.")]
        public Transform pickupCameraTarget;

        private GameObject _tutorialGiverInstance;
        private GameObject _pickupInstance;

        private void Start()
        {
            AutoFindReferences();
            SpawnTutorialGiver();
            SpawnPickup();
            RegisterCameraTargets();
        }

        private void AutoFindReferences()
        {
            if (tutorialManager == null)
            {
                tutorialManager = FindObjectOfType<TutorialManager>(true);
                if (tutorialManager == null)
                {
                    Debug.LogWarning("TutorialAutoSetup: No TutorialManager found in scene.");
                }
            }

            if (cameraCue == null)
            {
                cameraCue = FindObjectOfType<TutorialCameraCue>(true);
                if (cameraCue == null)
                {
                    Debug.LogWarning("TutorialAutoSetup: No TutorialCameraCue found. Camera highlights will not play.");
                }
            }
        }

        private void SpawnTutorialGiver()
        {
            if (tutorialGiverPrefab == null || tutorialGiverSpawn == null)
            {
                return;
            }

            if (_tutorialGiverInstance != null) return;

            _tutorialGiverInstance = Instantiate(
                tutorialGiverPrefab,
                tutorialGiverSpawn.position,
                tutorialGiverSpawn.rotation);

            if (tutorialManager != null)
            {
                var giver = _tutorialGiverInstance.GetComponentInChildren<TutorialGiverInteractable>();
                if (giver != null)
                {
                    giver.tutorialManager = tutorialManager;
                }
            }
        }

        private void SpawnPickup()
        {
            if (pickupPrefab == null || pickupSpawn == null)
            {
                return;
            }

            if (_pickupInstance != null) return;

            _pickupInstance = Instantiate(
                pickupPrefab,
                pickupSpawn.position,
                pickupSpawn.rotation);
        }

        private void RegisterCameraTargets()
        {
            if (cameraCue == null) return;

            if (pickupCameraTarget != null)
            {
                cameraCue.pickupTarget = pickupCameraTarget;
            }
            else if (_pickupInstance != null)
            {
                cameraCue.pickupTarget = _pickupInstance.transform;
            }
        }
    }
}

