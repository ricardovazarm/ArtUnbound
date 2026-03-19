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
            Debug.Log($"[ArtworkHanging] EnableFrameGrab called for {artworkId}, tier: {frameTier}");
            
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
                Debug.LogError("[ArtworkHanging] No completed frame found on puzzle board!");
                return;
            }

            Debug.Log($"[ArtworkHanging] Found completed frame: {completedFrame.name}");

            // Add GrabbableFrame component to make it interactive
            var grabbableFrame = completedFrame.GetComponent<GrabbableFrame>();
            if (grabbableFrame == null)
            {
                grabbableFrame = completedFrame.gameObject.AddComponent<GrabbableFrame>();
                Debug.Log("[ArtworkHanging] Added GrabbableFrame component");
            }
            
            // Subscribe to grab event to forward it
            grabbableFrame.OnFrameGrabbed += HandleFrameGrabbed;
            
            grabbableFrame.IsGrabbable = true;
            Debug.Log("[ArtworkHanging] Frame is now grabbable");

            Debug.Log($"[ArtworkHanging] Enabled frame grab for {artworkId} (Tier: {frameTier}), State: {currentState}");
        }
        
        /// <summary>
        /// Tries to place the frame on a nearby wall. Returns true if placed, false otherwise.
        /// </summary>
        public bool TryPlaceOnWall(Transform frameClone, Vector3 position)
        {
            // Use WallPlacementDetector to check if near a wall
            if (placementDetector == null)
            {
                Debug.LogWarning("[ArtworkHanging] No WallPlacementDetector available");
                OnPlacementCancelled?.Invoke();
                return false;
            }
            
            // Check if there's a valid wall nearby
            bool foundWall = placementDetector.CheckNearbyWall(position, out Vector3 wallPosition, out Quaternion wallRotation);
            
            if (!foundWall)
            {
                Debug.Log("[ArtworkHanging] No wall found nearby - cancelling placement");
                OnPlacementCancelled?.Invoke();
                return false;
            }
            
            // Snap frame to wall
            frameClone.position = wallPosition;
            frameClone.rotation = wallRotation;
            
            // Create persistent anchor
            if (anchorManager != null)
            {
                bool success = anchorManager.CreateAnchor(
                    currentArtworkId,
                    wallPosition,
                    wallRotation,
                    currentFrameTier,
                    frameClone
                );
                
                if (success)
                {
                    currentState = HangingState.Placed;
                    OnFramePlaced?.Invoke();
                    Debug.Log($"[ArtworkHanging] Frame successfully placed on wall and anchored");
                    return true;
                }
                else
                {
                    Debug.LogError("[ArtworkHanging] Failed to create anchor");
                    OnPlacementCancelled?.Invoke();
                    return false;
                }
            }
            else
            {
                Debug.LogError("[ArtworkHanging] No WallAnchorManager available");
                OnPlacementCancelled?.Invoke();
                return false;
            }
        }

        private void HandleFrameGrabbed()
        {
            Debug.Log("[ArtworkHanging] Frame was grabbed by user");
            currentState = HangingState.Grabbed;
            
            // Forward the event
            OnFrameGrabbed?.Invoke();
        }

        /// <summary>
        /// Disables frame grabbing.
        /// </summary>
        public void DisableFrameGrab()
        {
            if (completedFrame != null)
            {
                var grabbableFrame = completedFrame.GetComponent<GrabbableFrame>();
                if (grabbableFrame != null)
                {
                    grabbableFrame.OnFrameGrabbed -= HandleFrameGrabbed;
                    grabbableFrame.IsGrabbable = false;
                }
            }

            currentState = HangingState.Idle;
            completedFrame = null;
            
            Debug.Log("[ArtworkHanging] Disabled frame grab");
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
                    currentFrameTier,
                    completedFrame  // Pass the existing frame transform
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
        /// Returns SlotRoot which contains both the image and the frame.
        /// </summary>
        private Transform GetCompletedFrameTransform()
        {
            if (puzzleBoard == null) return null;

            // The SlotRoot contains the FullImageReveal and FullImageRevealFrame
            // We need to return SlotRoot so we can move the entire completed artwork
            Transform slotRoot = puzzleBoard.transform.Find("SlotRoot");
            
            if (slotRoot == null)
            {
                Debug.LogWarning("[ArtworkHanging] SlotRoot not found in PuzzleBoard");
                return null;
            }

            // Verify that the frame was created
            Transform frameTransform = slotRoot.Find("FullImageRevealFrame");
            Transform imageTransform = slotRoot.Find("FullImageReveal");
            
            if (frameTransform == null || imageTransform == null)
            {
                Debug.LogWarning($"[ArtworkHanging] Completed artwork not fully created. Frame: {frameTransform != null}, Image: {imageTransform != null}");
            }

            return slotRoot;
        }

        private void OnDestroy()
        {
            DisableFrameGrab();
        }
    }
}
