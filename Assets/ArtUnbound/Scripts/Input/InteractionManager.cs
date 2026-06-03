using ArtUnbound.Data;
using ArtUnbound.Gameplay;
using ArtUnbound.MR;
using ArtUnbound.VR;
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

        /// <summary>True while any piece or frame is actively being held.</summary>
        public bool IsDragging => currentDraggedPiece != null || currentDraggedFrame != null;

        /// <summary>The piece currently being dragged, or null if none.</summary>
        public PuzzlePiece CurrentDraggedPiece => currentDraggedPiece;

        private PuzzlePiece currentDraggedPiece;
        private GrabbableFrame currentDraggedFrame; // For hanging artwork
        private bool isRepositioningWallArtwork = false; // True when dragging a placed wall artwork
        private string repositioningArtworkId = null;
        private Vector3 dragOffset;
        private int targetSlotIndex = -1; // Store the slot shown in the highlight
        private bool pieceWasFromBoard = false; // Track if the piece was grabbed from the board
        private bool pieceWasFromThumbnail = false; // True when grabbed from the 2D thumbnail panel
        private float _externalDragStartTime = -10f; // Time.time when BeginExternalDrag was called
        private const float MinThumbnailHoldSec = 0.25f; // Ignore Pinch END this soon after thumbnail grab
        private int _pinchStartsSinceExternalDrag = 0; // counts NEW Pinch STARTs after BeginExternalDrag
        private bool _ignoreNextPinchStart = false;    // skips the grab gesture's own START if it fires after BeginExternalDrag

        // ── Controller select→aim→place state ────────────────────────────────────
        private PuzzlePiece _ctrlSelectedPiece      = null;
        private bool        _ctrlIsScrollingTray    = false;
        private bool        _ctrlRayHitTrayOnDown   = false; // ray hit tray at trigger-down
        private float       _ctrlTriggerDownTime    = -1f;
        private float       _ctrlLastRayHitY        = 0f;
        private bool        _ctrlPieceWasFromBoard  = false;
        private const float ClickMaxSec             = 0.15f; // <= 0.15s = click, > = hold/scroll
        private ArtUnbound.UI.PieceTray3DController _tray3DCached;
        private PuzzleBoard _boardCached;
        private ArtUnbound.MR.WallPlacementDetector _wallDetectorCached;
        private VRModeController _vrModeControllerCached;
        private VRWallHangingController _vrWallHangingCached;

        // Controller frame-hanging state
        private ArtUnbound.MR.GrabbableFrame _ctrlSelectedFrame = null;
        private bool        _ctrlIsRepositioningWallArtwork = false;
        private string      _ctrlRepositioningArtworkId     = null;
        private GameObject  _ctrlRepositioningArtworkGO     = null;
        // Time until which frame SELECT is blocked — set by BlockFrameSelectFor() when
        // EnableFrameGrab is called, preventing the navigation trigger from accidentally
        // selecting the newly-enabled frame in the same press.
        private static float _frameSelectBlockedUntil = -1f;

        /// <summary>
        /// Blocks all InteractionManager instances from selecting a GrabbableFrame for
        /// <paramref name="seconds"/>. Call this immediately after EnableFrameGrab().
        /// </summary>
        public static void BlockFrameSelectFor(float seconds)
        {
            _frameSelectBlockedUntil = Time.time + seconds;
        }

        private bool IsControllerMode => inputController != null && inputController.useControllers;

        private ArtUnbound.UI.PieceTray3DController GetTray3D()
        {
            if (_tray3DCached == null)
                _tray3DCached = FindFirstObjectByType<ArtUnbound.UI.PieceTray3DController>();
            return _tray3DCached;
        }

        private PuzzleBoard GetBoard()
        {
            if (_boardCached == null)
                _boardCached = FindFirstObjectByType<PuzzleBoard>();
            return _boardCached;
        }

        private ArtUnbound.MR.WallPlacementDetector GetWallDetector()
        {
            if (_wallDetectorCached == null)
                _wallDetectorCached = FindFirstObjectByType<ArtUnbound.MR.WallPlacementDetector>();
            return _wallDetectorCached;
        }

        private VRModeController GetVRModeController()
        {
            if (_vrModeControllerCached == null)
                _vrModeControllerCached = FindFirstObjectByType<VRModeController>();
            return _vrModeControllerCached;
        }

        private VRWallHangingController GetVRWallHangingController()
        {
            if (_vrWallHangingCached == null)
                _vrWallHangingCached = FindFirstObjectByType<VRWallHangingController>(FindObjectsInactive.Include);
            return _vrWallHangingCached;
        }

        private void SetFrameHighlight(ArtUnbound.MR.GrabbableFrame frame, bool selected)
        {
            if (frame == null) return;
            foreach (var rend in frame.GetComponentsInChildren<Renderer>())
            {
                if (selected)
                {
                    rend.material.EnableKeyword("_EMISSION");
                    rend.material.SetColor("_EmissionColor", Color.yellow * 0.3f);
                }
                else
                {
                    rend.material.DisableKeyword("_EMISSION");
                }
            }
        }

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
            if (!IsControllerMode) return;
            if (!inputController.GetPointerPose(out Vector3 origin, out Quaternion rot)) return;
            Ray ray = new Ray(origin, rot * Vector3.forward);

            // Piece selected: highlight target board slot
            if (_ctrlSelectedPiece != null)
            {
                var board = GetBoard();
                if (board != null)
                {
                    if (board.RayHitsBoard(ray))
                    {
                        int slotIdx = board.GetSlotAtRaycast(ray);
                        if (slotIdx >= 0) board.HighlightSlot(slotIdx);
                        else              board.ClearSlotHighlight();
                    }
                    else board.ClearSlotHighlight();
                }
                return;
            }

            // Frame selected: show wall ghost preview
            if (_ctrlSelectedFrame != null)
            {
                var wallDetector = GetWallDetector();
                if (wallDetector != null)
                {
                    if (wallDetector.RaycastToWall(ray, out Vector3 wallPos, out Quaternion wallRot))
                        wallDetector.ShowControllerGhost(wallPos, wallRot, true);
                    else
                        wallDetector.HideControllerGhost();
                }
            }
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
            if (IsControllerMode) { HandleControllerTriggerDown(); return; }

            // Count genuinely NEW gestures that start while a thumbnail-grabbed piece is held.
            // Skip the first one: the grab gesture's own START can fire after BeginExternalDrag
            // due to UIInputModule/HTPC execution order, so it must not disable the block guard.
            if (pieceWasFromThumbnail && currentDraggedPiece != null)
            {
                if (_ignoreNextPinchStart)
                    _ignoreNextPinchStart = false;
                else
                    _pinchStartsSinceExternalDrag++;
            }

            // GUARD: Don't grab a new object if we're already holding one (this instance or any other).
            if (currentDraggedPiece != null || currentDraggedFrame != null) return;

            // If another IM already holds a piece (GlobalGrabbedPiece != null), respect that lock.
            // The sphere-overlap filter (CurrentState != Grabbed) already prevents grabbing the same piece,
            // and ForceReleaseGlobalLock would destroy a legitimate grab held by the other IM.
            if (PuzzlePiece.GlobalGrabbedPiece != null) return;

            // currentDragDistance = 0f; // Removed - not used
            dragOffset = Vector3.zero;
            pieceWasFromThumbnail = false;

            // METHOD 1: Proximity/Sphere Overlap (Primary for Hand Tracking)
            float grabRadius = 0.04f; // 4cm radius - easier to grab pieces
            Collider[] colliders = Physics.OverlapSphere(position, grabRadius, interactableLayer);

            PuzzlePiece bestPiece = null;
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
                    }

                    if (frame != null && frame.IsGrabbable && !frame.IsBeingDragged)
                    {
                        grabbableFrame = frame;
                        break; // Frame takes priority
                    }
                    
                    // Check for puzzle pieces on the board (thumbnails are handled via Button/IPointerDownHandler)
                    var p = col.GetComponentInParent<PuzzlePiece>();
                    if (p != null && p.CurrentState != PieceState.Grabbed)
                    {
                        float d = Vector3.Distance(position, p.transform.position);
                        if (d < bestDist)
                        {
                            bestDist  = d;
                            bestPiece = p;
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
                return;
            }

            // METHOD 2: Raycast Fallback (for Controllers) - only for board pieces
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
                Debug.Log($"[GRAB-Physics] piece={bestPiece.PieceId} state={bestPiece.CurrentState} dist={bestDist*100:F1}cm");

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

                // If SetDragged was blocked (another piece has the global lock), discard ownership.
                if (bestPiece.CurrentState != PieceState.Grabbed)
                {
                    currentDraggedPiece = null;
                    bestPiece.ShowThumbnailMode(); // ensure thumbnail is visible if it was ghosted
                    return;
                }
            }
        }

        private void HandlePinchHold(Vector3 position, Quaternion rotation)
        {
            if (IsControllerMode) { HandleControllerTriggerHeld(); return; }

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
            if (IsControllerMode) { HandleControllerTriggerUp(); return; }

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

            // Block the Pinch END that belongs to the same gesture that triggered the thumbnail grab.
            // Two conditions must BOTH be met to allow release:
            //   1. At least one new Pinch START was seen after BeginExternalDrag (new gesture)
            //   2. OR the minimum hold time has elapsed (safety net for edge cases)
            if (pieceWasFromThumbnail &&
                _pinchStartsSinceExternalDrag == 0 &&
                Time.time - _externalDragStartTime < MinThumbnailHoldSec)
            {
                Debug.Log($"[RELEASE-Blocked] piece={currentDraggedPiece.PieceId} noNewPinch dt={(Time.time-_externalDragStartTime)*1000:F0}ms");
                return;
            }

            Debug.Log($"[RELEASE] piece={currentDraggedPiece.PieceId} fromThumbnail={pieceWasFromThumbnail} fromBoard={pieceWasFromBoard}");
            
            // Store references before clearing
            var piece            = currentDraggedPiece;
            var slotIndex        = targetSlotIndex;
            var wasFromBoard     = pieceWasFromBoard;
            var wasFromThumbnail = pieceWasFromThumbnail;

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
                    if (wasFromBoard)
                    {
                        // Piece was grabbed from the board → goes to END of tray
                        board.ReturnPieceToTray(piece);
                    }
                    else
                    {
                        // Piece came from the tray → animate back to its original grid position
                        var tray3D = FindFirstObjectByType<ArtUnbound.UI.PieceTray3DController>();
                        if (tray3D != null)
                        {
                            tray3D.ReturnPieceToGrid(piece);
                        }
                        else if (wasFromThumbnail)
                        {
                            // 2D thumbnail fallback
                            piece.SetState(PieceState.InPool);
                            piece.ShowThumbnailMode();
                        }
                        else
                        {
                            board.ReturnPieceToTray(piece);
                        }
                    }
                }

                // Clear highlight after snap attempt
                board.ClearSlotHighlight();
            }
            else
            {
                // Board not found — force the piece back to a clean state so _globalGrabbedPiece
                // doesn't remain locked forever, which would block all future grabs.
                Debug.LogWarning("[InteractionManager] Board not found on release — force returning piece to pool.");
                piece.SetState(PieceState.InPool);
                piece.ShowThumbnailMode();
            }
        }

        /// <summary>
        /// Called by PieceThumbnailItem.OnPointerDown to begin dragging a piece
        /// from the thumbnail panel. Works for near pinch (poke), remote pinch (ray),
        /// and controller input — all routed through the Meta XR UI event system.
        ///
        /// HandlePinchHold will start moving the piece to the hand on the next frame.
        /// HandlePinchEnd will attempt the snap when the pinch is released.
        /// </summary>
        public void BeginExternalDrag(PuzzlePiece piece)
        {
            if (piece == null) return;
            if (currentDraggedPiece != null || currentDraggedFrame != null) return;

            var board = FindFirstObjectByType<PuzzleBoard>();
            if (board != null)
                piece.transform.rotation = board.transform.rotation;

            // Place the piece at the live hand position. GetPointerPose queries the
            // XRHandSubsystem directly so it is never stale, unlike TrackedObject which
            // is written by ProcessHand one Update() tick before UIInputModule fires.
            if (inputController != null &&
                inputController.GetPointerPose(out Vector3 handPos, out Quaternion _))
            {
                piece.transform.position = handPos;
            }
            else if (inputController != null && inputController.TrackedObject != null)
            {
                piece.transform.position = inputController.TrackedObject.position;
            }

            currentDraggedPiece              = piece;
            dragOffset                       = Vector3.zero;
            pieceWasFromBoard                = false;
            pieceWasFromThumbnail            = true;
            _externalDragStartTime           = Time.time;
            _pinchStartsSinceExternalDrag    = 0;
            _ignoreNextPinchStart            = true;  // the grab gesture's START may fire after us

            Debug.Log($"[GRAB-Thumbnail] piece={piece.PieceId} state={piece.CurrentState}");

            piece.SetDragged(true); // → SetState(Grabbed) → ShowPieceMode (mesh on, thumbnail ghosted)

            // Verify the grab actually succeeded — SetState(Grabbed) can be blocked by the global lock.
            if (piece.CurrentState != PieceState.Grabbed)
            {
                Debug.LogWarning($"[GRAB-Thumbnail] Grab blocked by global lock on piece {piece.PieceId} — cleaning up.");
                currentDraggedPiece   = null;
                pieceWasFromThumbnail = false;
                piece.ShowThumbnailMode(); // un-ghost thumbnail in case OnPointerDown already ghosted it
                return;
            }

            if (ArtUnbound.Feedback.AudioManager.Instance != null)
                ArtUnbound.Feedback.AudioManager.Instance.PlayPieceGrab();

        }

        // ── Controller select→aim→place handlers ─────────────────────────────────

        private void HandleControllerTriggerDown()
        {
            _ctrlTriggerDownTime  = Time.time;
            _ctrlRayHitTrayOnDown = false;

            if (_ctrlSelectedPiece != null) return; // piece selected — wait for TriggerUp to place
            if (_ctrlSelectedFrame != null) return;  // frame selected — wait for TriggerUp to hang

            if (!inputController.GetPointerPose(out Vector3 origin, out Quaternion rot)) return;
            Ray ray = new Ray(origin, rot * Vector3.forward);

            var tray = GetTray3D();
            if (tray != null && tray.RayHitsTray(ray, out float hitY))
            {
                // Don't start scrolling immediately — record intent; scroll activates after ClickMaxSec hold
                _ctrlRayHitTrayOnDown = true;
                _ctrlLastRayHitY     = hitY;
                Debug.Log($"[CTRL-Down] ray hit tray — deferring scroll until hold > {ClickMaxSec}s");
            }
            else
            {
                Debug.Log("[CTRL-Down] ray did NOT hit tray");
            }
        }

        private void HandleControllerTriggerHeld()
        {
            if (!inputController.GetPointerPose(out Vector3 origin, out Quaternion rot)) return;
            Ray ray = new Ray(origin, rot * Vector3.forward);

            // Promote deferred tray-hit to active scroll once the hold crosses ClickMaxSec
            if (!_ctrlIsScrollingTray && _ctrlRayHitTrayOnDown
                && (Time.time - _ctrlTriggerDownTime) >= ClickMaxSec)
            {
                var tray0 = GetTray3D();
                if (tray0 != null && tray0.RayHitsTray(ray, out float initY))
                {
                    _ctrlIsScrollingTray = true;
                    _ctrlLastRayHitY    = initY;
                    Debug.Log("[CTRL-Held] scroll mode activated");
                }
            }

            if (_ctrlIsScrollingTray)
            {
                var tray = GetTray3D();
                if (tray != null && tray.RayHitsTray(ray, out float hitY))
                {
                    tray.ScrollBy(hitY - _ctrlLastRayHitY);
                    _ctrlLastRayHitY = hitY;
                }
                return;
            }

            if (_ctrlSelectedPiece != null)
            {
                var board = FindFirstObjectByType<PuzzleBoard>();
                if (board != null)
                {
                    int slotIdx = board.GetSlotAtRaycast(ray);
                    if (slotIdx >= 0) board.HighlightSlot(slotIdx);
                    else              board.ClearSlotHighlight();
                }
            }
        }

        private void HandleControllerTriggerUp()
        {
            bool wasClick = (Time.time - _ctrlTriggerDownTime) < ClickMaxSec;
            bool rayHitTray = _ctrlRayHitTrayOnDown;
            _ctrlRayHitTrayOnDown = false;

            Debug.Log($"[CTRL-Up] wasClick={wasClick} isScrolling={_ctrlIsScrollingTray} rayHitTray={rayHitTray} selectedPiece={_ctrlSelectedPiece?.PieceId.ToString() ?? "none"}");

            if (_ctrlIsScrollingTray)
            {
                _ctrlIsScrollingTray = false;
                return;
            }

            // Quick click while ray was on tray and no piece was already selected → attempt piece select via raycast
            // (falls through to SELECT phase below)

            if (!inputController.GetPointerPose(out Vector3 origin, out Quaternion rot)) return;
            Ray ray = new Ray(origin, rot * Vector3.forward);
            var board = GetBoard();

            // ── HANG phase: frame already selected ───────────────────────────────
            // No wasClick guard: any trigger-up while frame is selected attempts hanging.
            // 0.15s click threshold is too tight for VR controller use.
            if (_ctrlSelectedFrame != null)
            {
                var wallDetector = GetWallDetector();
                bool placed = false;

                bool wasRepositioning = _ctrlIsRepositioningWallArtwork;
                if (wasRepositioning)
                {
                    // Placed wall artwork: reposition on wall or remove
                    var hanging = FindFirstObjectByType<ArtUnbound.MR.ArtworkHangingController>();
                    Vector3    wallPos2 = Vector3.zero;
                    Quaternion wallRot2 = Quaternion.identity;
                    bool foundWall = wallDetector != null &&
                                     wallDetector.RaycastToWall(ray, out wallPos2, out wallRot2);
                    if (foundWall)
                    {
                        hanging?.ControllerRepositionWallArtwork(
                            _ctrlRepositioningArtworkId, _ctrlRepositioningArtworkGO,
                            true, wallPos2, wallRot2);
                        placed = true;
                    }
                    else
                    {
                        hanging?.ControllerRepositionWallArtwork(
                            _ctrlRepositioningArtworkId, _ctrlRepositioningArtworkGO,
                            false, Vector3.zero, Quaternion.identity);
                    }

                    _ctrlIsRepositioningWallArtwork = false;
                    _ctrlRepositioningArtworkId     = null;
                    _ctrlRepositioningArtworkGO     = null;
                }
                else
                {
                    // Completed puzzle frame: place clone on wall
                    var vrMode = GetVRModeController();
                    Debug.Log($"[CTRL-Hang-VR] vrMode={vrMode != null} IsVRMode={vrMode?.IsVRMode}  ray.origin={ray.origin:F2}  ray.dir={ray.direction:F2}");
                    if (vrMode != null && vrMode.IsVRMode)
                    {
                        // VR path: raycast against VRWall layer (with fallback to normal-based detection)
                        var vrHanging = GetVRWallHangingController();
                        Debug.Log($"[CTRL-Hang-VR] vrHanging={vrHanging != null}");
                        if (vrHanging != null && vrHanging.RaycastToWall(ray, out Vector3 vrWallPos, out Quaternion vrWallRot))
                            placed = vrHanging.PlaceFrameAtWall(vrWallPos, vrWallRot);
                    }
                    else if (wallDetector != null && wallDetector.RaycastToWall(ray, out Vector3 wallPos, out Quaternion wallRot))
                    {
                        var hanging = FindFirstObjectByType<ArtUnbound.MR.ArtworkHangingController>();
                        if (hanging != null)
                            placed = hanging.PlaceFrameAtWall(wallPos, wallRot);
                    }
                }

                // If no wall found, keep frame selected so user can aim at a different wall
                if (!placed && !wasRepositioning)
                {
                    Debug.Log("[CTRL-Hang] No wall found — frame stays selected for retry");
                    return;
                }

                wallDetector?.HideControllerGhost();
                SetFrameHighlight(_ctrlSelectedFrame, false);
                _ctrlSelectedFrame = null;
                Debug.Log($"[CTRL-Hang] placed={placed} wasRepositioning={wasRepositioning}");
                return;
            }

            // ── PLACE phase: piece already selected ──────────────────────────────
            if (_ctrlSelectedPiece != null && wasClick)
            {
                board?.ClearSlotHighlight();

                if (board != null && board.RayHitsBoard(ray))
                {
                    int slotIdx = board.GetSlotAtRaycast(ray);
                    if (slotIdx >= 0)
                    {
                        bool snapped = board.SnapPieceToSlot(_ctrlSelectedPiece, slotIdx);
                        if (snapped && !_ctrlPieceWasFromBoard)
                            GetTray3D()?.RemovePieceFromTray(_ctrlSelectedPiece);
                    }
                }
                else if (_ctrlPieceWasFromBoard && board != null)
                {
                    // Click outside board: piece was from board → return to end of tray
                    board.ReturnPieceToTray(_ctrlSelectedPiece);
                }
                // else: click outside board, piece from tray → just deselect (no move)

                _ctrlSelectedPiece.SetSelected(false);
                _ctrlSelectedPiece     = null;
                _ctrlPieceWasFromBoard = false;
                return;
            }

            // ── SELECT phase: no piece selected yet ──────────────────────────────
            if (_ctrlSelectedPiece == null && wasClick)
            {
                if (board == null) board = FindFirstObjectByType<PuzzleBoard>();
                PuzzlePiece piece = null;

                // 1st: board geometry pick — finds placed pieces without relying on Physics/colliders
                if (board != null && board.RayHitsBoard(ray))
                {
                    piece = board.GetPlacedPieceAtRaycast(ray);
                    Debug.Log($"[CTRL-Select] board geo → {(piece != null ? piece.PieceId.ToString() : "null")}");
                }

                // 2nd: tray geometry pick — tray pieces have colliders OFF
                if (piece == null)
                {
                    var tray = GetTray3D();
                    if (tray != null && tray.RayHitsTray(ray, out float _))
                    {
                        piece = tray.GetPieceAtRaycast(ray);
                        Debug.Log($"[CTRL-Select] tray grid → {(piece != null ? piece.PieceId.ToString() : "null")}");
                    }
                }

                if (piece != null && piece.CurrentState != PieceState.Grabbed)
                {
                    if (piece.CurrentState == PieceState.Placed)
                    {
                        if (board == null || !board.RemovePieceFromSlot(piece)) return; // Locked
                        _ctrlPieceWasFromBoard = true;
                    }
                    _ctrlSelectedPiece = piece;
                    piece.SetSelected(true);
                    Debug.Log($"[CTRL-Select] selected piece {piece.PieceId}");
                    if (ArtUnbound.Feedback.AudioManager.Instance != null)
                        ArtUnbound.Feedback.AudioManager.Instance.PlayPieceGrab();
                    return;
                }

                // No puzzle piece found — check for a grabbable completed frame or placed wall artwork.
                // Skip if frame selection is blocked (cooldown after EnableFrameGrab prevents
                // the navigation trigger from immediately selecting the newly-active frame).
                float blockRemaining = _frameSelectBlockedUntil - Time.time;
                Debug.Log($"[CTRL-FrameSelect] blocked={blockRemaining > 0} ({blockRemaining:F2}s left)  wasClick={wasClick}  piece=null");
                if (Time.time < _frameSelectBlockedUntil) return;

                RaycastHit[] allHits = Physics.RaycastAll(ray, rayLength);
                Debug.Log($"[CTRL-FrameSelect] RaycastAll hits={allHits.Length}  rayLen={rayLength}");
                foreach (var h in allHits)
                {
                    // Placed wall artwork takes priority over the puzzle-complete frame.
                    // Ignore if further than 40 cm from the controller — prevents accidentally
                    // selecting paintings on a distant back wall while interacting with the board.
                    if (h.collider.CompareTag("PlacedArtwork") && h.distance <= 0.4f)
                    {
                        string artId = null;
                        var placedId = h.collider.GetComponent<ArtUnbound.MR.PlacedArtworkIdentifier>();
                        if (placedId != null && !string.IsNullOrEmpty(placedId.artworkId))
                            artId = placedId.artworkId;
                        else
                        {
                            string goName = h.collider.gameObject.name;
                            const string prefix = "PlacedArtwork_";
                            if (goName.StartsWith(prefix))
                                artId = goName.Substring(prefix.Length);
                        }

                        // Reuse or add GrabbableFrame only for highlight purposes
                        var gfPlaced = h.collider.GetComponentInParent<ArtUnbound.MR.GrabbableFrame>();
                        if (gfPlaced == null)
                        {
                            gfPlaced = h.collider.gameObject.AddComponent<ArtUnbound.MR.GrabbableFrame>();
                            gfPlaced.IsGrabbable = true;
                        }

                        _ctrlSelectedFrame              = gfPlaced;
                        _ctrlIsRepositioningWallArtwork = true;
                        _ctrlRepositioningArtworkId     = artId;
                        _ctrlRepositioningArtworkGO     = h.collider.gameObject;
                        SetFrameHighlight(gfPlaced, true);
                        Debug.Log($"[CTRL-Select] selected placed wall artwork '{artId}' for repositioning");
                        if (ArtUnbound.Feedback.AudioManager.Instance != null)
                            ArtUnbound.Feedback.AudioManager.Instance.PlayPieceGrab();
                        break;
                    }

                    var gf = h.collider.GetComponentInParent<ArtUnbound.MR.GrabbableFrame>();
                    Debug.Log($"[CTRL-FrameSelect] hit: {h.collider.gameObject.name} layer={LayerMask.LayerToName(h.collider.gameObject.layer)} tag={h.collider.tag} dist={h.distance:F2}  gf={gf != null}  grabbable={gf?.IsGrabbable}");
                    if (gf != null && gf.IsGrabbable && !gf.IsBeingDragged)
                    {
                        _ctrlSelectedFrame = gf;
                        SetFrameHighlight(gf, true);
                        Debug.Log("[CTRL-Select] selected frame for hanging");
                        if (ArtUnbound.Feedback.AudioManager.Instance != null)
                            ArtUnbound.Feedback.AudioManager.Instance.PlayPieceGrab();
                        break;
                    }
                }
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
