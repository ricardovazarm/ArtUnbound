using System;
using UnityEngine;
using ArtUnbound.Data;
using ArtUnbound.Input;
using ArtUnbound.Gameplay;

namespace ArtUnbound.MR
{
    /// <summary>
    /// Main controller for the artwork hanging feature.
    /// Manages the state flow: Idle -> Grabbed -> Previewing -> Placing -> Placed.
    /// </summary>
    public class ArtworkHangingController : MonoBehaviour
    {
        public enum HangingState
        {
            Idle,       // Waiting for user to grab the frame
            Grabbed,    // Frame attached to hand
            Previewing, // Showing ghost preview on wall
            Placing,    // Animating frame to wall
            Placed      // Anchored to wall
        }

        [Header("References")]
        [SerializeField] private HandTrackingInputController handInput;
        [SerializeField] private HandAttachmentController handAttachment;
        [SerializeField] private WallPlacementDetector placementDetector;
        [SerializeField] private WallAnchorManager anchorManager;
        [SerializeField] private PuzzleBoard puzzleBoard;

        [Header("Settings")]
        [SerializeField] private float grabDetectionRadius = 0.1f; // 10cm
        [SerializeField] private float placementAnimationDuration = 0.5f;

        private HangingState currentState = HangingState.Idle;
        private Transform completedFrame;
        private string currentArtworkId;
        private FrameTier currentFrameTier;
        private bool isAnimating;
        private float animationTime;
        private Vector3 animationStartPos;
        private Quaternion animationStartRot;
        private Vector3 animationTargetPos;
        private Quaternion animationTargetRot;

        public event Action OnFrameGrabbed;
        public event Action OnFramePlaced;
        public event Action OnPlacementCancelled;

        public HangingState CurrentState => currentState;

        private void Awake()
        {
            if (handInput == null)
                handInput = FindFirstObjectByType<HandTrackingInputController>();
            
            if (handAttachment == null)
                handAttachment = GetComponent<HandAttachmentController>();
            
            if (placementDetector == null)
                placementDetector = GetComponent<WallPlacementDetector>();
            
            if (anchorManager == null)
                anchorManager = FindFirstObjectByType<WallAnchorManager>();
            
            if (puzzleBoard == null)
                puzzleBoard = FindFirstObjectByType<PuzzleBoard>();
        }

        /// <summary>
        /// Enables frame grabbing for the completed puzzle.
        /// </summary>
        public void EnableFrameGrab(string artworkId, FrameTier frameTier)
        {
            if (puzzleBoard == null)
            {
                Debug.LogError("[ArtworkHanging] Cannot enable grab: PuzzleBoard is null");
                return;
            }

            currentArtworkId = artworkId;
            currentFrameTier = frameTier;
            currentState = HangingState.Idle;

            // Get the completed frame from the puzzle board
            completedFrame = GetCompletedFrameTransform();
            
            if (completedFrame == null)
            {
                Debug.LogWarning("[ArtworkHanging] No completed frame found on puzzle board");
                return;
            }

            // Add collider if not present (for pinch detection)
            if (completedFrame.GetComponent<BoxCollider>() == null)
            {
                var collider = completedFrame.gameObject.AddComponent<BoxCollider>();
                collider.size = new Vector3(0.5f, 0.5f, 0.05f); // Approximate frame size
            }

            // Subscribe to pinch events
            if (handInput != null)
            {
                handInput.OnPinchStart += OnPinchStart;
                handInput.OnPinchEnd += OnPinchEnd;
            }

            Debug.Log($"[ArtworkHanging] Enabled frame grab for {artworkId} (Tier: {frameTier})");
        }

        /// <summary>
        /// Disables frame grabbing.
        /// </summary>
        public void DisableFrameGrab()
        {
            if (handInput != null)
            {
                handInput.OnPinchStart -= OnPinchStart;
                handInput.OnPinchEnd -= OnPinchEnd;
            }

            currentState = HangingState.Idle;
            completedFrame = null;
            
            Debug.Log("[ArtworkHanging] Disabled frame grab");
        }

        private void OnPinchStart(Vector3 pinchPosition, Quaternion pinchRotation)
        {
            if (currentState == HangingState.Idle)
            {
                // Check if pinch is near the frame
                if (completedFrame != null && Vector3.Distance(pinchPosition, completedFrame.position) < grabDetectionRadius)
                {
                    GrabFrame();
                }
            }
        }

        private void OnPinchEnd(Vector3 pinchPosition, Quaternion pinchRotation)
        {
            if (currentState == HangingState.Grabbed || currentState == HangingState.Previewing)
            {
                ReleaseFrame();
            }
        }

        private void GrabFrame()
        {
            if (completedFrame == null || handInput == null) return;

            // Get the hand transform
            Transform handTransform = handInput.TrackedObject;
            if (handTransform == null)
            {
                Debug.LogWarning("[ArtworkHanging] Cannot grab frame: hand transform is null");
                return;
            }

            // Attach frame to hand
            if (handAttachment != null)
            {
                handAttachment.Attach(completedFrame, handTransform);
            }

            // Start placement detection
            if (placementDetector != null)
            {
                placementDetector.StartDetection();
            }

            currentState = HangingState.Grabbed;
            
            OnFrameGrabbed?.Invoke();
            
            Debug.Log("[ArtworkHanging] Frame grabbed");
        }

        private void ReleaseFrame()
        {
            if (placementDetector != null && placementDetector.HasValidPlacement)
            {
                // Valid placement - start placement animation
                StartPlacementAnimation();
            }
            else
            {
                // Invalid placement - cancel and return to idle
                CancelPlacement();
            }
        }

        private void StartPlacementAnimation()
        {
            if (completedFrame == null) return;

            // Detach from hand
            if (handAttachment != null)
            {
                handAttachment.Detach();
            }

            // Stop detection
            if (placementDetector != null)
            {
                placementDetector.StopDetection();
            }

            // Setup animation
            animationStartPos = completedFrame.position;
            animationStartRot = completedFrame.rotation;
            animationTargetPos = placementDetector.ValidPosition;
            animationTargetRot = placementDetector.ValidRotation;
            animationTime = 0f;
            isAnimating = true;

            currentState = HangingState.Placing;

            Debug.Log($"[ArtworkHanging] Starting placement animation to {animationTargetPos}");
        }

        private void CancelPlacement()
        {
            // Detach from hand
            if (handAttachment != null)
            {
                handAttachment.Detach();
            }

            // Stop detection
            if (placementDetector != null)
            {
                placementDetector.StopDetection();
            }

            // Return to idle
            currentState = HangingState.Idle;
            
            OnPlacementCancelled?.Invoke();

            Debug.Log("[ArtworkHanging] Placement cancelled - no valid wall found");
        }

        private void Update()
        {
            if (isAnimating && currentState == HangingState.Placing)
            {
                UpdatePlacementAnimation();
            }

            // Update placement detector transform to match frame
            if (currentState == HangingState.Grabbed && completedFrame != null && placementDetector != null)
            {
                placementDetector.transform.position = completedFrame.position;
                placementDetector.transform.rotation = completedFrame.rotation;
            }
        }

        private void UpdatePlacementAnimation()
        {
            if (completedFrame == null) return;

            animationTime += Time.deltaTime;
            float t = Mathf.Clamp01(animationTime / placementAnimationDuration);
            
            // Ease out cubic
            t = 1f - Mathf.Pow(1f - t, 3f);

            completedFrame.position = Vector3.Lerp(animationStartPos, animationTargetPos, t);
            completedFrame.rotation = Quaternion.Slerp(animationStartRot, animationTargetRot, t);
            completedFrame.localScale = Vector3.Lerp(completedFrame.localScale, Vector3.one, t); // Restore scale

            if (t >= 1f)
            {
                // Animation complete - create anchor
                FinalizePlacement();
            }
        }

        private void FinalizePlacement()
        {
            isAnimating = false;

            if (anchorManager != null && completedFrame != null)
            {
                bool success = anchorManager.CreateAnchor(
                    currentArtworkId,
                    completedFrame.position,
                    completedFrame.rotation,
                    currentFrameTier
                );

                if (success)
                {
                    currentState = HangingState.Placed;
                    OnFramePlaced?.Invoke();
                    
                    Debug.Log($"[ArtworkHanging] Artwork {currentArtworkId} successfully placed on wall");
                }
                else
                {
                    Debug.LogError("[ArtworkHanging] Failed to create anchor for artwork");
                    CancelPlacement();
                }
            }
            else
            {
                Debug.LogError("[ArtworkHanging] Cannot finalize placement: missing anchor manager or frame");
                CancelPlacement();
            }
        }

        /// <summary>
        /// Gets the completed frame transform from the puzzle board.
        /// </summary>
        private Transform GetCompletedFrameTransform()
        {
            if (puzzleBoard == null) return null;

            // Look for the FullImageRevealFrame (created by ShowFullImageReveal)
            Transform slotRoot = puzzleBoard.transform.Find("SlotRoot");
            if (slotRoot == null)
            {
                Debug.LogWarning("[ArtworkHanging] SlotRoot not found");
                return slotRoot;
            }

            Transform frameTransform = slotRoot.Find("FullImageRevealFrame");
            if (frameTransform == null)
            {
                Debug.LogWarning("[ArtworkHanging] FullImageRevealFrame not found");
                // Fallback: return SlotRoot itself (it contains the full image reveal)
                return slotRoot;
            }

            return frameTransform;
        }

        private void OnDestroy()
        {
            DisableFrameGrab();
        }
    }
}
