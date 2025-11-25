using System.Collections;
using UnityEngine;

namespace ENDURE.Tutorial
{
    /// <summary>
    /// Handles switching between the player's camera and a highlight camera to show objects of interest.
    /// Place the highlight camera in the scene (disabled) and assign it here.
    /// </summary>
    public class TutorialCameraCue : MonoBehaviour
    {
        [Header("Camera References")]
        public Camera playerCamera;
        public Camera highlightCamera;

        [Header("Timing")]
        public float focusDuration = 2.5f;
        public AnimationCurve blendCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [Header("Offsets")]
        public Vector3 orbitOffset = new Vector3(0, 2f, -3f);
        public float minDistance = 1.5f;

        [Header("Quick Targets")]
        public Transform pickupTarget;
        public Transform enemyTarget;

        private Coroutine _focusRoutine;

        private void Awake()
        {
            if (highlightCamera != null)
            {
                highlightCamera.gameObject.SetActive(false);
                highlightCamera.enabled = false;
            }
        }

        public void HighlightPickup()
        {
            if (pickupTarget != null)
            {
                FocusOnTarget(pickupTarget);
            }
        }

        public void HighlightEnemy()
        {
            if (enemyTarget != null)
            {
                FocusOnTarget(enemyTarget);
            }
        }

        public void FocusOnTarget(Transform target)
        {
            if (target == null || playerCamera == null || highlightCamera == null)
            {
                Debug.LogWarning("TutorialCameraCue: Missing camera or target reference.");
                return;
            }

            if (_focusRoutine != null)
            {
                StopCoroutine(_focusRoutine);
            }

            _focusRoutine = StartCoroutine(FocusRoutine(target));
        }

        private IEnumerator FocusRoutine(Transform target)
        {
            highlightCamera.gameObject.SetActive(true);
            highlightCamera.enabled = true;
            playerCamera.enabled = false;

            PositionHighlightCamera(target);

            float elapsed = 0f;
            while (elapsed < focusDuration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            highlightCamera.enabled = false;
            highlightCamera.gameObject.SetActive(false);
            playerCamera.enabled = true;
            _focusRoutine = null;
        }

        private void PositionHighlightCamera(Transform target)
        {
            Vector3 desiredPos = target.position + orbitOffset;
            if ((desiredPos - target.position).sqrMagnitude < minDistance * minDistance)
            {
                desiredPos = target.position + target.forward * -minDistance + Vector3.up * minDistance;
            }

            highlightCamera.transform.position = desiredPos;
            highlightCamera.transform.LookAt(target.position);
        }
    }
}

