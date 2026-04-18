using ArtUnbound.Data;
using ArtUnbound.Gameplay;
using ArtUnbound.MR;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ArtUnbound.Input
{
    /// <summary>
    /// Bridges HandTracking input with Gameplay objects (PuzzlePieces and GrabbableFrame).
    /// Handles Raycasting and Dragging logic.
    /// </summary>
    public class InteractionManager : MonoBehaviour
    {
        [SerializeField] private HandTrackingInputController inputController;
        [SerializeField] private LayerMask interactableLayer;
        [SerializeField] private float rayLength = 2.0f;
        [SerializeField] private bool useRaycast = true; // Enabled for Controllers
        [SerializeField] private LineRenderer rayVisualizer;

        private PuzzlePiece currentDraggedPiece;
        private GrabbableFrame currentDraggedFrame; // For hanging artwork
        private bool isRepositioningWallArtwork = false; // True when dragging a placed wall artwork
        private string repositioningArtworkId = null;
        private Vector3 dragOffset;
        private int targetSlotIndex = -1; // Store the slot shown in the highlight
        private bool pieceWasFromBoard = false; // Track if the piece was grabbed from the board
        private bool pieceWasFromThumbnail = false; // True when grabbed from the 2D thumbnail panel

        private void OnEnable()
        {
            if (inputController != null)
            {
                inputController.OnPinchStart += HandlePinchStart;
                inputController.OnPinchHold += HandlePinchHold;
                inputController.OnPinchEnd += HandlePinchEnd;
            }
        }

        private void Start()
        {
            if (rayVisualizer != null)
            {
                rayVisualizer.enabled = false;
                rayVisualizer.useWorldSpace = true;
                rayVisualizer.positionCount = 2;
                rayVisualizer.startWidth = 0.02f;
                rayVisualizer.endWidth = 0.02f;

                if (rayVisualizer.sharedMaterial == null)
                {
                    Shader s = Shader.Find("Sprites/Default");
                    if (s == null) s = Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply");
                    if (s == null) s = Shader.Find("UI/Default");

                    if (s != null)
                    {
                        rayVisualizer.material = new Material(s);
                        rayVisualizer.material.color = Color.red;
                    }
                }

                rayVisualizer.startColor = Color.red;
                rayVisualizer.endColor = Color.red;
            }
        }

        private void Update()
        {
            // El Ray Visualizer manual ha sido eliminado para evitar conflictos.
            // Si necesitas un láser visual para la UI, te recomiendo usar el 
            // XRRayInteractor o Near-Far Interactor nativo de Unity en los prefabs de las manos.
        }

        private void OnDestroy()
        {
            if (inputController != null)
            {
                inputController.OnPinchStart -= HandlePinchStart;
                inputController.OnPinchHold -= HandlePinchHold;
                inputController.OnPinchEnd -= HandlePinchEnd;
            }
        }

        private void HandlePinchStart(Vector3 position, Quaternion rotation)
        {
            // GUARD: Don't grab a new object if we're already holding one
            if (currentDraggedPiece != null || currentDraggedFrame != null)
            {
                return;
            }

            // currentDragDistance = 0f; // Removed - not used
            dragOffset = Vector3.zero;
            pieceWasFromThumbnail = false;

            // METHOD 1: Proximity/Sphere Overlap (Primary for Hand Tracking)
            float grabRadius = 0.04f; // 4cm radius - easier to grab pieces
            Collider[] colliders = Physics.OverlapSphere(position, grabRadius, interactableLayer);

            PuzzlePiece bestPiece = null;
            ArtUnbound.UI.PieceThumbnailItem bestThumbnail = null;
            GrabbableFrame grabbableFrame = null;
            float bestDist = float.MaxValue;

            if (colliders.Length > 0)
            {
                foreach (var col in colliders)
                {
                    // Check for GrabbableFrame first (priority when hanging artwork)
                    var frame = col.GetComponent<GrabbableFrame>();

                    // If no GrabbableFrame component, check if this is a placed wall artwork
                    if (frame == null && col.CompareTag("PlacedArtwork"))
                    {
                        // Try PlacedArtworkIdentifier first, then fall back to name parsing
                        string artId = null;
                        var placedId = col.GetComponent<ArtUnbound.MR.PlacedArtworkIdentifier>();
                        if (placedId != null && !string.IsNullOrEmpty(placedId.artworkId))
                        {
                            artId = placedId.artworkId;
                        }
                        else
                        {
                            // Fallback: "PlacedArtwork_<artworkId>" naming from WallAnchorManager
                            string goName = col.gameObject.name;
                            const string prefix = "PlacedArtwork_";
                            if (goName.StartsWith(prefix))
                                artId = goName.Substring(prefix.Length);
                        }

                        frame = col.gameObject.AddComponent<GrabbableFrame>();
                        frame.IsGrabbable = true;
                        isRepositioningWallArtwork = true;
                        repositioningArtworkId = artId;
                        Debug.Log($"[InteractionManager] Grabbing placed artwork for repositioning: {artId ?? "unknown"}");
                    }
                    // If GrabbableFrame already exists from a previous grab, still check if
                    // this is a placed artwork — the component is left over from the first grab
                    // and the CompareTag branch above was skipped.
                    else if (frame != null && col.CompareTag("PlacedArtwork"))
                    {
                        string artId = null;
                        var placedId = col.GetComponent<ArtUnbound.MR.PlacedArtworkIdentifier>();
                        if (placedId != null && !string.IsNullOrEmpty(placedId.artworkId))
                            artId = placedId.artworkId;
                        else
                        {
                            string goName = col.gameObject.name;
                            const string prefix = "PlacedArtwork_";
                            if (goName.StartsWith(prefix))
                                artId = goName.Substring(prefix.Length);
                        }
                        isRepositioningWallArtwork = true;
                        repositioningArtworkId = artId;
                        Debug.Log($"[InteractionManager] Re-grabbing placed artwork for repositioning: {artId ?? "unknown"}");
                    }

                    if (frame != null && frame.IsGrabbable && !frame.IsBeingDragged)
                    {
                        grabbableFrame = frame;
                        Debug.Log($"[InteractionManager] Found grabbable frame at {frame.transform.position}");
                        break; // Frame takes priority
                    }
                    
                    // Check for 2D thumbnail (lives in panel, NOT a child of PuzzlePiece)
                    var thumb = col.GetComponent<ArtUnbound.UI.PieceThumbnailItem>();
                    if (thumb != null)
                    {
                        if (thumb.LinkedPiece != null
                            && thumb.LinkedPiece.CurrentState != PieceState.Grabbed
                            && thumb.LinkedPiece.CurrentState != PieceState.Placed)
                        {
                            float d = Vector3.Distance(position, col.bounds.center);
                            if (d <= grabRadius && d < bestDist)
                            {
                                bestDist      = d;
                                bestThumbnail = thumb;
                                bestPiece     = null; // thumbnail supersedes any 3D piece at same spot
                            }
                        }
                        continue; // don't also check as a regular piece
                    }

                    // Otherwise check for puzzle pieces
                    var p = col.GetComponentInParent<PuzzlePiece>();
                    if (p != null && p.CurrentState != PieceState.Grabbed)
                    {
                        float d = Vector3.Distance(position, p.transform.position);
                        if (d < bestDist)
                        {
                            bestDist  = d;
                            bestPiece = p;
                            bestThumbnail = null; // 3D piece supersedes thumbnail if closer
                        }
                    }
                }
                
                // CRITICAL FIX: Only grab if object is ACTUALLY within the grab radius
                if (bestPiece != null && bestDist > grabRadius)
                {
                    bestPiece = null; // Reject pieces outside the threshold
                }
            }

            // Handle frame grab
            if (grabbableFrame != null)
            {
                grabbableFrame.StartDrag(position);
                currentDraggedFrame = grabbableFrame;
                Debug.Log("[InteractionManager] Started dragging frame");
                return;
            }

            // Handle thumbnail grab — spawn the linked 3D piece at hand position
            if (bestThumbnail != null)
            {
                var piece = bestThumbnail.LinkedPiece;

                // Align piece rotation to board so placement looks natural
                var board0 = FindFirstObjectByType<PuzzleBoard>();
                if (board0 != null)
                    piece.transform.rotation = board0.transform.rotation;

                // Move piece to hand immediately (it was somewhere invisible in PieceScrollController)
                piece.transform.position = position;

                currentDraggedPiece   = piece;
                dragOffset            = Vector3.zero;   // piece is already at hand
                pieceWasFromBoard     = false;
                pieceWasFromThumbnail = true;

                piece.SetDragged(true); // → SetState(Grabbed) → ShowPieceMode (mesh on, thumb hidden)

                if (ArtUnbound.Feedback.AudioManager.Instance != null)
                    ArtUnbound.Feedback.AudioManager.Instance.PlayPieceGrab();

                Debug.Log($"[InteractionManager] Grabbed piece {piece.PieceId} from thumbnail panel");
                return;
            }

            // METHOD 2: Raycast Fallback (for Controllers) - only for pieces
            if (useRaycast && bestPiece == null)
            {
                Ray ray = new Ray(position, rotation * Vector3.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, rayLength, interactableLayer))
                {
                    var piece = hit.collider.GetComponentInParent<PuzzlePiece>();
                    if (piece != null && piece.CurrentState != PieceState.Placed && piece.CurrentState != PieceState.Grabbed)
                    {
                        // CRITICAL: Validate distance for raycast too
                        float rayDist = Vector3.Distance(position, piece.transform.position);
                        if (rayDist <= grabRadius)
                        {
                            bestPiece = piece;
                        }
                    }
                }
            }

            if (bestPiece != null)
            {
                // Track if the piece was grabbed from the board
                pieceWasFromBoard = (bestPiece.CurrentState == PieceState.Placed);
                
                // If the piece was placed on the board, try to remove it
                if (pieceWasFromBoard)
                {
                    var board = FindFirstObjectByType<PuzzleBoard>();
                    if (board != null)
                    {
                        // Try to remove it - will return false if correctly placed (locked)
                        bool wasRemoved = board.RemovePieceFromSlot(bestPiece);
                        
                        if (!wasRemoved)
                        {
                            // Piece is correctly placed and locked - cannot grab it
                            return;
                        }
                    }
                }
                
                // Play grab sound
                if (ArtUnbound.Feedback.AudioManager.Instance != null)
                {
                    ArtUnbound.Feedback.AudioManager.Instance.PlayPieceGrab();
                }
                
                // When grabbing from tray (rotated), align piece to board so placement looks natural
                if (!pieceWasFromBoard)
                {
                    var board = FindFirstObjectByType<PuzzleBoard>();
                    if (board != null)
                        bestPiece.transform.rotation = board.transform.rotation;
                }
                
                currentDraggedPiece = bestPiece;
                dragOffset = bestPiece.transform.position - position;
                bestPiece.SetDragged(true);
            }
        }

        private void HandlePinchHold(Vector3 position, Quaternion rotation)
        {
            // Handle frame dragging
            if (currentDraggedFrame != null)
            {
                currentDraggedFrame.UpdateDrag(position);
                
                // Update frame rotation to face away from nearest wall
                UpdateFrameRotationToNearestWall(currentDraggedFrame.ClonedFrame);
                
                return;
            }
            
            // Handle piece dragging
            if (currentDraggedPiece != null)
            {
                // Move piece to follow hand position directly with offset
                Vector3 targetPoint = position + dragOffset;

                // Direct follow (no lerp) for better responsiveness
                // The piece should feel "glued" to the fingers
                currentDraggedPiece.transform.position = targetPoint;

                // Keep original rotation (don't rotate the piece)
                // The piece maintains board orientation for proper snapping
                
                // Update slot highlight using the ACTUAL piece position (not calculated position)
                // This ensures the highlight is always based on where the piece really is
                var board = FindFirstObjectByType<PuzzleBoard>();
                if (board != null)
                {
                    targetSlotIndex = board.UpdateSlotHighlight(currentDraggedPiece.transform.position);
                }
            }
        }

        private void HandlePinchEnd(Vector3 position, Quaternion rotation)
        {
            if (rayVisualizer != null)
            {
                rayVisualizer.enabled = false;
            }

            // Handle frame release
            if (currentDraggedFrame != null)
            {
                Transform clonedFrame = currentDraggedFrame.ClonedFrame;

                if (clonedFrame == null)
                {
                    Debug.LogError("[InteractionManager] ClonedFrame is null! Cannot place on wall.");
                    currentDraggedFrame = null;
                    isRepositioningWallArtwork = false;
                    repositioningArtworkId = null;
                    return;
                }

                Vector3 releasePosition = clonedFrame.position;
                currentDraggedFrame.EndDrag();

                var hangingController = FindFirstObjectByType<ArtUnbound.MR.ArtworkHangingController>();

                if (isRepositioningWallArtwork)
                {
                    // Wall-artwork grab: reposition on wall or remove if away from wall
                    if (hangingController != null)
                    {
                        hangingController.TryRepositionWallArtwork(repositioningArtworkId, currentDraggedFrame, clonedFrame, releasePosition);
                    }
                    else
                    {
                        // Fallback: just destroy clone and restore original
                        Destroy(clonedFrame.gameObject);
                        currentDraggedFrame.RestoreOriginal();
                    }

                    isRepositioningWallArtwork = false;
                    repositioningArtworkId = null;
                }
                else
                {
                    // In-puzzle completed frame: place on wall or restore (keep grabbable)
                    bool placedOnWall = false;
                    if (hangingController != null)
                    {
                        Debug.Log($"[InteractionManager] Passing clone {clonedFrame.name} to TryPlaceOnWall");
                        placedOnWall = hangingController.TryPlaceOnWall(clonedFrame, releasePosition);
                    }
                    else
                    {
                        Debug.LogError("[InteractionManager] ArtworkHangingController not found!");
                    }

                    if (!placedOnWall)
                    {
                        // Not near a wall — destroy clone, restore original so it can be grabbed again
                        Destroy(clonedFrame.gameObject);
                        currentDraggedFrame.RestoreOriginal();
                    }

                    Debug.Log($"[InteractionManager] Released in-puzzle frame (placed on wall: {placedOnWall})");
                }

                currentDraggedFrame = null;
                return;
            }

            // Guard against processing the same pinch end twice
            if (currentDraggedPiece == null)
            {
                targetSlotIndex = -1;
                return;
            }
            
            // Store references before clearing
            var piece      = currentDraggedPiece;
            var slotIndex  = targetSlotIndex;
            var wasFromBoard = pieceWasFromBoard;

            // Clear IMMEDIATELY to prevent double processing
            piece.SetDragged(false);
            currentDraggedPiece   = null;
            targetSlotIndex       = -1;
            pieceWasFromBoard     = false;
            pieceWasFromThumbnail = false;
            
            // Try to snap the piece to the board using the slot we stored from the highlight
            var board = FindFirstObjectByType<PuzzleBoard>();
            if (board != null)
            {
                bool snapped = false;

                if (slotIndex >= 0)
                {
                    // Only snap if piece is actually within snap distance on release (prevents accidental placement)
                    if (board.IsWithinSnapDistance(piece.transform.position, slotIndex))
                    {
                        snapped = board.SnapPieceToSlot(piece, slotIndex);
                        if (snapped)
                            Debug.Log($"[SNAP] Piece snapped to slot {slotIndex}");
                    }
                    else
                    {
                        float dist = Vector3.Distance(piece.transform.position, board.GetSlotPosition(slotIndex));
                        Debug.Log($"[SNAP] Skipped: piece {dist * 100:F1}cm from slot {slotIndex} (beyond snap distance)");
                    }
                }

                if (!snapped)
                {
                    // Failed to snap - return based on origin
                    if (wasFromBoard)
                    {
                        board.ReturnPieceToTray(piece);
                    }
                    else
                    {
                        piece.ReturnToPool(piece.GetPoolPosition());
                    }
                }
                
                // Clear highlight after snap attempt
                board.ClearSlotHighlight();
            }
        }

        private bool TryClickUI(Vector3 origin, Vector3 direction)
        {
            Ray ray = new Ray(origin, direction);
            float closestDist = rayLength;
            Selectable hitSelectable = null;

            // Find all active selectables (Buttons, Toggles, etc.)
            Selectable[] selectables = FindObjectsByType<Selectable>(FindObjectsSortMode.None);
            foreach (var sel in selectables)
            {
                if (sel.interactable && sel.gameObject.activeInHierarchy)
                {
                    RectTransform rect = sel.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        // Raycast against the front plane of the RectTransform
                        Plane plane = new Plane(-rect.forward, rect.position);
                        if (!plane.Raycast(ray, out float enter))
                        {
                            // Try the back plane just in case
                            plane = new Plane(rect.forward, rect.position);
                            plane.Raycast(ray, out enter);
                        }

                        if (enter > 0 && enter < closestDist)
                        {
                            Vector3 hitPoint = ray.GetPoint(enter);
                            Vector3 localHit = rect.InverseTransformPoint(hitPoint);

                            if (rect.rect.Contains(localHit))
                            {
                                closestDist = enter;
                                hitSelectable = sel;
                            }
                        }
                    }
                }
            }

            if (hitSelectable != null)
            {
                PointerEventData pointerData = new PointerEventData(EventSystem.current);
                ExecuteEvents.Execute(hitSelectable.gameObject, pointerData, ExecuteEvents.pointerClickHandler);
                ExecuteEvents.Execute(hitSelectable.gameObject, pointerData, ExecuteEvents.submitHandler);
                
                Button btn = hitSelectable as Button;
                if (btn != null && btn.onClick != null)
                {
                    btn.onClick.Invoke();
                }

                return true;
            }

            return false;
        }
        
        /// <summary>
        /// Updates the frame rotation to face away from the nearest wall during dragging.
        /// This provides visual feedback and makes placement more intuitive.
        /// </summary>
        private void UpdateFrameRotationToNearestWall(Transform frameClone)
        {
            if (frameClone == null) return;
            
            // Find the nearest wall using WallPlacementDetector logic
            var detector = FindFirstObjectByType<ArtUnbound.MR.WallPlacementDetector>();
            if (detector == null) return;
            
            // Check for nearby wall
            if (detector.CheckNearbyWall(frameClone.position, out Vector3 wallPosition, out Quaternion wallRotation))
            {
                // Smoothly rotate the frame to face outward from the wall
                frameClone.rotation = Quaternion.Slerp(frameClone.rotation, wallRotation, Time.deltaTime * 10f);
            }
        }
    }
}
