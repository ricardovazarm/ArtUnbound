using System;
using UnityEngine;

namespace ArtUnbound.MR
{
    /// <summary>
    /// Controls the positioning of the canvas in Comfort Mode (floating in front of user).
    /// </summary>
    public class ComfortModeController : MonoBehaviour
    {
        public event Action OnPositionLocked;
        public event Action OnPositionUnlocked;

        [Header("Configuration")]
        [SerializeField] private float distanceFromHead = 0.8f;
        [SerializeField] private float tiltAngle = 15f;
        [SerializeField] private float heightOffset = -0.1f;

        [Header("References")]
        [SerializeField] private Transform headTransform;
        [SerializeField] private GameObject previewPrefab;
        [SerializeField] private CanvasFrameController canvasFrameController;

        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = false;

        public bool IsLocked => isLocked;
        public Vector3 CurrentPosition => currentPosition;
        public Quaternion CurrentRotation => currentRotation;

        private bool isLocked = false;
        private Vector3 currentPosition;
        private Quaternion currentRotation;
        private GameObject previewInstance;

        private void Awake()
        {
            if (headTransform == null)
            {
                // Try to find the main camera as fallback
                var mainCam = Camera.main;
                if (mainCam != null)
                {
                    headTransform = mainCam.transform;
                }
            }
        }

        private void Update()
        {
            if (!isLocked && headTransform != null)
            {
                CalculateErgonomicPosition();
                UpdatePreview();
            }
        }

        /// <summary>
        /// Starts the positioning mode, showing a preview of where the canvas will be placed.
        /// </summary>
        public void StartPositioning()
        {
            isLocked = false;

            if (previewPrefab != null && previewInstance == null)
            {
                previewInstance = Instantiate(previewPrefab);
            }

            CalculateErgonomicPosition();
            UpdatePreview();
        }

        /// <summary>
        /// Calculates the ergonomic position based on the user's head position and orientation.
        /// </summary>
        public void CalculateErgonomicPosition()
        {
            if (headTransform == null) return;

            // Get forward direction, keeping it horizontal
            Vector3 forward = headTransform.forward;
            forward.y = 0f;
            forward.Normalize();

            // Calculate position
            currentPosition = headTransform.position + forward * distanceFromHead;
            currentPosition.y += heightOffset;

            // Calculate rotation (facing the user with tilt)
            currentRotation = Quaternion.LookRotation(-forward) * Quaternion.Euler(tiltAngle, 0f, 0f);
        }

        /// <summary>
        /// Updates the preview object position and rotation.
        /// </summary>
        private void UpdatePreview()
        {
            if (previewInstance != null)
            {
                previewInstance.transform.position = currentPosition;
                previewInstance.transform.rotation = currentRotation;
            }
        }

        /// <summary>
        /// Locks the canvas position at the current location.
        /// </summary>
        public void LockPosition()
        {
            isLocked = true;

            // Destroy preview
            if (previewInstance != null)
            {
                Destroy(previewInstance);
                previewInstance = null;
            }

            // Position the actual canvas
            if (canvasFrameController != null)
            {
                canvasFrameController.transform.position = currentPosition;
                canvasFrameController.transform.rotation = currentRotation;
            }

            OnPositionLocked?.Invoke();
        }

        /// <summary>
        /// Unlocks the canvas position, allowing repositioning.
        /// </summary>
        public void UnlockPosition()
        {
            isLocked = false;

            if (previewPrefab != null && previewInstance == null)
            {
                previewInstance = Instantiate(previewPrefab);
            }

            OnPositionUnlocked?.Invoke();
        }

        /// <summary>
        /// Gets the calculated ergonomic position.
        /// </summary>
        public Vector3 GetErgonomicPosition()
        {
            CalculateErgonomicPosition();
            return currentPosition;
        }

        /// <summary>
        /// Gets the calculated ergonomic rotation.
        /// </summary>
        public Quaternion GetErgonomicRotation()
        {
            CalculateErgonomicPosition();
            return currentRotation;
        }

        /// <summary>
        /// Sets the distance from the user's head.
        /// </summary>
        public void SetDistance(float distance)
        {
            distanceFromHead = Mathf.Clamp(distance, 0.3f, 2f);
        }

        /// <summary>
        /// Sets the tilt angle.
        /// </summary>
        public void SetTiltAngle(float angle)
        {
            tiltAngle = Mathf.Clamp(angle, 0f, 45f);
        }

        /// <summary>
        /// Manually adjusts the position offset.
        /// </summary>
        public void AdjustPosition(Vector3 offset)
        {
            if (!isLocked)
            {
                currentPosition += offset;
            }
        }

        private void OnDestroy()
        {
            if (previewInstance != null)
            {
                Destroy(previewInstance);
            }
        }

        private void OnDrawGizmos()
        {
            if (!showDebugGizmos || headTransform == null) return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(currentPosition, 0.1f);
            Gizmos.DrawLine(headTransform.position, currentPosition);

            // Draw canvas orientation
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(currentPosition, currentRotation * Vector3.forward * 0.2f);
            Gizmos.color = Color.green;
            Gizmos.DrawRay(currentPosition, currentRotation * Vector3.up * 0.2f);
        }
    }
}
