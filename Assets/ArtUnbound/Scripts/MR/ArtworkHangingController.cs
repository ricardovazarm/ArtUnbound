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
        /// Handles both new placement and repositioning of existing artworks.
        /// </summary>
        public bool TryPlaceOnWall(Transform frameClone, Vector3 position, bool isRepositioning = false)
        {
            if (frameClone == null)
            {
                Debug.LogError("[ArtworkHanging] frameClone is null!");
                OnPlacementCancelled?.Invoke();
                return false;
            }
            
            Debug.Log($"[ArtworkHanging] Attempting to place frame: {frameClone.name} at {position}, repositioning: {isRepositioning}");
            
            // Use WallPlacementDetector to check if near a wall
            if (placementDetector == null)
            {
                Debug.LogWarning("[ArtworkHanging] No WallPlacementDetector available");
                OnPlacementCancelled?.Invoke();
                return false;
            }
            
            // Check if there's a valid wall nearby (tight radius — must actually be close to a wall)
            bool foundWall = placementDetector.CheckNearbyWall(position, out Vector3 wallPosition, out Quaternion wallRotation, 0.20f);
            
            if (!foundWall)
            {
                Debug.Log("[ArtworkHanging] No wall found nearby - cancelling placement");
                OnPlacementCancelled?.Invoke();
                return false;
            }
            
            // Position frame slightly away from wall (5mm) to avoid z-fighting
            Vector3 offsetPosition = wallPosition + wallRotation * Vector3.forward * 0.005f;
            
            // Snap frame to wall with the wall's rotation (perpendicular to wall surface)
            frameClone.position = offsetPosition;
            frameClone.rotation = wallRotation;
            
            Debug.Log($"[ArtworkHanging] Placing frame at {offsetPosition}");
            Debug.Log($"[ArtworkHanging] Frame rotation set to: {wallRotation.eulerAngles}");
            
            // Handle repositioning vs new placement
            if (isRepositioning && anchorManager != null)
            {
                // Update existing anchor position
                bool success = anchorManager.UpdateAnchorPosition(currentArtworkId, offsetPosition, wallRotation);
                
                if (success)
                {
                    currentState = HangingState.Placed;
                    OnFramePlaced?.Invoke();
                    Debug.Log($"[ArtworkHanging] Frame repositioned successfully for {currentArtworkId}");
                    return true;
                }
                else
                {
                    Debug.LogError("[ArtworkHanging] Failed to update anchor position");
                    OnPlacementCancelled?.Invoke();
                    return false;
                }
            }
            else if (anchorManager != null)
            {
                // Get board dimensions for persistence/reconstruction
                float boardW = 0.5f, boardH = 0.5f;
                if (puzzleBoard != null)
                    puzzleBoard.GetBoardDimensions(out boardW, out boardH);

                // Create new anchor
                bool success = anchorManager.CreateAnchor(
                    currentArtworkId,
                    offsetPosition,
                    wallRotation,
                    currentFrameTier,
                    frameClone,
                    boardW,
                    boardH
                );
                
                if (success)
                {
                    currentState = HangingState.Placed;
                    OnFramePlaced?.Invoke();
                    Debug.Log($"[ArtworkHanging] Frame successfully placed on wall and anchored for {currentArtworkId}");
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

        /// <summary>
        /// Handles grab/release of an already-placed wall artwork (not an in-puzzle frame).
        /// If near a wall: repositions the anchor. If away from wall: removes the artwork entirely.
        /// </summary>
        public void TryRepositionWallArtwork(string artworkId, GrabbableFrame sourceFrame, Transform clonedFrame, Vector3 releasePosition)
        {
            if (string.IsNullOrEmpty(artworkId))
            {
                Debug.LogWarning("[ArtworkHanging] TryRepositionWallArtwork: no artworkId");
                Destroy(clonedFrame?.gameObject);
                return;
            }

            Vector3 wallPos = Vector3.zero;
            Quaternion wallRot = Quaternion.identity;
            // Use a tight proximity radius (0.35m) so artwork released in the middle of the room
            // is correctly treated as "away from wall" — the full raycastDistance (2m) would
            // always find a wall somewhere in the room and never trigger removal.
            bool nearWall = placementDetector != null &&
                            placementDetector.CheckNearbyWall(releasePosition, out wallPos, out wallRot, 0.20f);

            if (nearWall && clonedFrame != null)
            {
                Vector3 offsetPos = wallPos + wallRot * Vector3.forward * 0.005f;

                // Move the SPECIFIC grabbed artwork (sourceFrame.gameObject) to the new position.
                // We cannot rely on anchorManager.UpdateAnchorPosition because spawnedArtworks[artworkId]
                // may point to a different instance when duplicates exist.
                if (sourceFrame != null)
                {
                    sourceFrame.gameObject.transform.SetParent(null, worldPositionStays: true);
                    sourceFrame.gameObject.transform.SetPositionAndRotation(offsetPos, wallRot);
                    sourceFrame.RestoreOriginal(); // make it visible at new position
                }

                if (anchorManager != null && !string.IsNullOrEmpty(artworkId))
                    anchorManager.UpdateAnchorForSpecificArtwork(artworkId, sourceFrame?.gameObject.transform, offsetPos, wallRot);

                Destroy(clonedFrame.gameObject);
                Debug.Log($"[ArtworkHanging] Wall artwork {artworkId} repositioned");
            }
            else
            {
                // Released away from any wall — destroy the SPECIFIC grabbed object directly.
                Destroy(clonedFrame?.gameObject);
                Destroy(sourceFrame?.gameObject);

                // Clean up one storage entry (anchor erasure) without touching other instances.
                if (anchorManager != null && !string.IsNullOrEmpty(artworkId))
                    anchorManager.EraseArtworkStorageEntry(artworkId);

                Debug.Log($"[ArtworkHanging] Wall artwork {artworkId} removed from wall");
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
                    // Destroy the component so EnableFrameGrab creates it fresh next time
                    // (avoids stale BoxCollider reference issues across puzzle sessions)
                    Destroy(grabbableFrame);
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
                float boardW = 0.5f, boardH = 0.5f;
                if (puzzleBoard != null)
                    puzzleBoard.GetBoardDimensions(out boardW, out boardH);

                bool success = anchorManager.CreateAnchor(
                    currentArtworkId,
                    completedFrame.position,
                    completedFrame.rotation,
                    currentFrameTier,
                    completedFrame,
                    boardW,
                    boardH
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
