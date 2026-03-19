using UnityEngine;

namespace ArtUnbound.MR
{
    /// <summary>
    /// Makes a completed puzzle frame follow the user's hand smoothly.
    /// Used when the player grabs the frame to hang it on a wall.
    /// </summary>
    public class HandAttachmentController : MonoBehaviour
    {
        [Header("Attachment Settings")]
        [SerializeField] private float followSpeed = 12f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private Vector3 positionOffset = new Vector3(0f, 0f, 0.15f); // 15cm in front of hand
        [SerializeField] private Vector3 rotationOffset = new Vector3(0f, 0f, 0f);
        [SerializeField] private float attachedScale = 0.8f; // Shrink to 80% when held

        private Transform targetHand;
        private Transform attachedFrame;
        private bool isAttached;
        private Vector3 originalScale;

        /// <summary>
        /// Attaches the frame to the hand.
        /// </summary>
        public void Attach(Transform frame, Transform hand)
        {
            if (frame == null || hand == null)
            {
                Debug.LogWarning("[HandAttachment] Cannot attach: frame or hand is null");
                return;
            }

            attachedFrame = frame;
            targetHand = hand;
            originalScale = frame.localScale;
            isAttached = true;

            Debug.Log($"[HandAttachment] Attached frame to hand at {hand.position}");
        }

        /// <summary>
        /// Detaches the frame from the hand.
        /// </summary>
        public void Detach()
        {
            if (attachedFrame != null)
            {
                // Restore original scale
                attachedFrame.localScale = originalScale;
            }

            isAttached = false;
            attachedFrame = null;
            targetHand = null;

            Debug.Log("[HandAttachment] Detached frame from hand");
        }

        /// <summary>
        /// Returns true if a frame is currently attached.
        /// </summary>
        public bool IsAttached => isAttached;

        private void Update()
        {
            if (!isAttached || attachedFrame == null || targetHand == null)
                return;

            // Calculate target position with offset
            Vector3 targetPosition = targetHand.position + targetHand.TransformDirection(positionOffset);
            
            // Calculate target rotation (frame faces forward, not toward hand)
            Quaternion targetRotation = targetHand.rotation * Quaternion.Euler(rotationOffset);

            // Smooth follow
            attachedFrame.position = Vector3.Lerp(attachedFrame.position, targetPosition, followSpeed * Time.deltaTime);
            attachedFrame.rotation = Quaternion.Slerp(attachedFrame.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // Apply attached scale (slightly smaller for easier handling)
            Vector3 targetScale = originalScale * attachedScale;
            attachedFrame.localScale = Vector3.Lerp(attachedFrame.localScale, targetScale, followSpeed * Time.deltaTime);
        }
    }
}
