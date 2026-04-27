using System;
using System.Collections;
using System.Collections.Generic;
using ArtUnbound.Data;
using UnityEngine;

namespace ArtUnbound.Gameplay
{
    /// <summary>
    /// Main puzzle board controller for piece placement and validation.
    /// </summary>
    public class PuzzleBoard : MonoBehaviour
    {
        public event Action<PuzzlePiece> OnPieceSnappedRaw;
        public event Action<int, int> OnPieceSnapped;
        public event Action OnPuzzleComplete;
        public event Action OnCompleted;
        public event Action<PuzzlePiece> OnPlacementError;
        public event Action<PuzzlePiece> OnPlacementSuccess;
        public event Action OnBoardStateChanged; // Event for auto-save
        public event System.Action<ArtUnbound.UI.MilestoneType, int> OnMilestoneAchieved; // type, edgeCount (for Edges)

        [SerializeField] private Transform slotRoot;
        [SerializeField] private PuzzlePiece piecePrefab;
        [SerializeField] private ArtUnbound.UI.PieceScrollController scrollController;

        [Tooltip("Assign the PieceTrayPanel GameObject that has PieceTrayGridController. " +
                 "Position it in the scene wherever you want the 2-D thumbnail grid to appear.")]
        [SerializeField] private ArtUnbound.UI.PieceTrayGridController pieceTrayController;

        [Tooltip("New 3D tray controller. When assigned, replaces PieceScrollController and PieceTrayGridController.")]
        [SerializeField] private ArtUnbound.UI.PieceTray3DController pieceTray3DController;

        [SerializeField] private ArtUnbound.Input.HandTrackingInputController inputController;
        [SerializeField] private PuzzleConfig puzzleConfig;
        [SerializeField] private bool helpModeEnabled = true;
        [SerializeField] private HelpModeController helpModeController;
        [SerializeField] private Color errorGlowColor = new Color(1f, 0.2f, 0.2f, 0.8f);

        [Header("Board backing (behind pieces)")]
        [Tooltip("Optional fabric or surface material (e.g. Yughues M_YFFaM_*). Cloned at runtime. When set, overrides Board Backing Texture.")]
        [SerializeField] private Material boardBackingMaterial;
        [Tooltip("Optional albedo/base map when no backing material is assigned. Applied to URP Lit (or fallback) behind the puzzle grid.")]
        [SerializeField] private Texture2D boardBackingTexture;

        [Header("Frame (for completed image reveal)")]
        [SerializeField] private FrameConfigSet frameConfigSet;
        [Tooltip("Fallback if FrameConfigSet has no config for tier")]
        [SerializeField] private Material frameBronceMaterial;
        [SerializeField] private Material framePlataMaterial;
        [SerializeField] private Material frameOroMaterial;

        private readonly List<PuzzlePiece> activePieces = new List<PuzzlePiece>();
        
        // Slot highlighting
        private GameObject slotHighlight;
        private int currentHighlightedSlot = -1;

        private void Start()
        {
            if (inputController == null)
            {
                inputController = FindFirstObjectByType<ArtUnbound.Input.HandTrackingInputController>();
                if (inputController == null)
                {
                    Debug.LogWarning("[PuzzleBoard] HandTrackingInputController not found in scene. Creating one.");
                    GameObject inputObj = new GameObject("HandTrackingInputController");
                    inputController = inputObj.AddComponent<ArtUnbound.Input.HandTrackingInputController>();
                }
            }

            if (inputController != null)
            {
                inputController.OnSwipeHorizontal += OnSwipeInput;
            }
            else
            {
                Debug.LogError("[PuzzleBoard] Failed to initialize HandTrackingInputController!");
            }
        }

        private void OnDestroy()
        {
            if (inputController != null)
            {
                inputController.OnSwipeHorizontal -= OnSwipeInput;
            }
        }

        private void OnSwipeInput(float delta)
        {
            if (scrollController != null)
            {
                scrollController.OnSwipe(delta);
            }
        }

        private readonly List<PuzzleSlot> slots = new List<PuzzleSlot>();
        private readonly Dictionary<int, PieceMorphology> morphologyByPieceId = new Dictionary<int, PieceMorphology>();
        private readonly Dictionary<int, PuzzlePiece> placedBySlot = new Dictionary<int, PuzzlePiece>();
        private int snappedCount;
        private int totalPieces;

        // Milestone tracking: rows, cols, edges already celebrated (avoid duplicates)
        private readonly HashSet<int> completedRows = new HashSet<int>();
        private readonly HashSet<int> completedCols = new HashSet<int>();
        private readonly HashSet<int> completedEdges = new HashSet<int>(); // 0=top, 1=bottom, 2=left, 3=right
        private bool allEdgesCelebrated;
        private Texture2D currentTexture;

        // Edge-state arrays for varied piece morphology.
        // hEdges[row * edgeCols + col]  = bottom edge of piece (col, row);  size = cols*(rows-1)
        // vEdges[row * (edgeCols-1) + col] = right edge of piece (col, row); size = (cols-1)*rows
        // true → Positive (tab out), false → Negative (tab in)
        private bool[] hEdges;
        private bool[] vEdges;
        private int edgeCols; // stored so GenerateMorphology can index the arrays
        private Vector3 lastPos;
        private float currentPieceWidth = 0.05f;  // Current piece width  (calculated dynamically per puzzle)
        private float currentPieceHeight = 0.05f; // Current piece height (calculated dynamically per puzzle)
        private float boardWidthM;
        private float boardHeightM;
        private int   gridCols;       // set in CreateSlotsFromCount / CreateSlots
        private int   gridRows;
        private GameObject fullImageReveal;
        private GameObject fullImageRevealFrame;

        // Public getters for progress tracking
        public int SnappedCount => snappedCount;
        public int TotalPieces => totalPieces;
        public int PlacedCount => placedBySlot.Count;
        public int IncorrectCount => placedBySlot.Count - snappedCount;

        /// <summary>Returns all pieces that are placed in the wrong slot.</summary>
        public List<PuzzlePiece> GetIncorrectlyPlacedPieces()
        {
            var result = new List<PuzzlePiece>();
            foreach (var kvp in placedBySlot)
            {
                int slotIndex = kvp.Key;
                PuzzlePiece piece = kvp.Value;
                if (piece != null && slotIndex < slots.Count && slots[slotIndex].pieceId != piece.PieceId)
                    result.Add(piece);
            }
            return result;
        }

        private void Update()
        {
            if (Vector3.Distance(transform.position, lastPos) > 0.01f)
                lastPos = transform.position;
        }

        /// <summary>
        /// Initializes the puzzle board with piece count and artwork texture.
        /// </summary>
        public void Initialize(int difficultyIndex, Texture2D artworkTexture)
        {
            snappedCount = 0;
            totalPieces = 0; // Computed by BuildSlotsAndPieces from grid
            currentTexture = artworkTexture;

            if (fullImageReveal != null)
            {
                Destroy(fullImageReveal);
                fullImageReveal = null;
            }
            if (fullImageRevealFrame != null)
            {
                Destroy(fullImageRevealFrame);
                fullImageRevealFrame = null;
            }

            foreach (var piece in activePieces)
            {
                if (piece != null) DestroyImmediate(piece.gameObject);
            }
            activePieces.Clear();

            slots.Clear();
            morphologyByPieceId.Clear();
            placedBySlot.Clear();
            completedRows.Clear();
            completedCols.Clear();
            completedEdges.Clear();
            allEdgesCelebrated = false;

            if (slotRoot == null)
            {
                Debug.LogError($"[PuzzleBoard] Initialize failed. SlotRoot is null.");
                return;
            }

            slotRoot.localPosition = Vector3.zero;
            slotRoot.localRotation = Quaternion.identity;

            CreateSlotsFromCount(difficultyIndex);
        }

        /// <summary>
        /// Initializes the puzzle board with artwork definition.
        /// </summary>
        public void Initialize(ArtworkDefinition definition, int difficultyIndex)
        {
            snappedCount = 0;
            var textureToUse = definition?.puzzleTexture != null ? definition.puzzleTexture : definition?.fullImage?.texture;
            currentTexture = textureToUse;
            
            // Ensure slotRoot is active and at its default local transform.
            // Controller-mode hanging moves slotRoot (completedFrame) to wall position;
            // resetting here ensures the new puzzle/restore appears in front of the user.
            if (slotRoot != null)
            {
                if (!slotRoot.gameObject.activeSelf)
                    slotRoot.gameObject.SetActive(true);
                slotRoot.localPosition = Vector3.zero;
                slotRoot.localRotation = Quaternion.identity;
            }

            if (fullImageReveal != null)
            {
                Destroy(fullImageReveal);
                fullImageReveal = null;
            }
            if (fullImageRevealFrame != null)
            {
                Destroy(fullImageRevealFrame);
                fullImageRevealFrame = null;
            }

            foreach (var piece in activePieces)
            {
                if (piece != null) DestroyImmediate(piece.gameObject);
            }
            activePieces.Clear();

            slots.Clear();
            morphologyByPieceId.Clear();
            placedBySlot.Clear();
            completedRows.Clear();
            completedCols.Clear();
            completedEdges.Clear();
            allEdgesCelebrated = false;

            if (definition == null || slotRoot == null)
            {
                return;
            }

            CreateSlots(definition, difficultyIndex);
        }

        public void SetHelpMode(bool enabled)
        {
            helpModeEnabled = enabled;
        }

        public bool TrySnapPiece(PuzzlePiece piece)
        {
            if (piece == null)
            {
                Debug.LogWarning("[PuzzleBoard] TrySnapPiece called with NULL piece!");
                return false;
            }

            // NEW APPROACH: Check distance to board plane (not individual slots)
            // The board plane is defined by the board's transform
            Vector3 boardNormal = transform.forward; // Board faces forward (towards user when rotated)
            Vector3 boardPosition = transform.position; // Center of board
            
            // Calculate distance from piece to board plane
            Vector3 pieceToBoard = piece.transform.position - boardPosition;
            float distanceToPlane = Mathf.Abs(Vector3.Dot(pieceToBoard, boardNormal));
            
            // STEP 1: Check if piece is close enough to the board plane
            float maxDepthToPlane = (puzzleConfig != null ? puzzleConfig.maxDepthToPlaneCm : 4f) * 0.01f;
            if (distanceToPlane > maxDepthToPlane)
            {
                Debug.LogWarning($"[PuzzleBoard] ✗ Piece too far from board plane: {distanceToPlane:F3}m > {maxDepthToPlane:F3}m");
                return false;
            }

            if (IsDefaultMorphology(piece.Morphology))
            {
                piece.ApplyMorphology(GetMorphologyForPieceId(piece.PieceId));
            }

            // STEP 2: Project piece position onto board plane
            Vector3 projectedPiecePos = piece.transform.position - boardNormal * Vector3.Dot(pieceToBoard, boardNormal);
            
            // STEP 3: Find closest slot by distance on the board plane (NO distance limit)
            int bestIndex = -1;
            float bestPlanarDist = float.MaxValue;

            for (int i = 0; i < slots.Count; i++)
            {
                if (placedBySlot.ContainsKey(i)) continue;

                // Calculate 3D distance directly (simpler and more accurate)
                float dist3D = Vector3.Distance(piece.transform.position, slots[i].position);

                if (dist3D < bestPlanarDist)
                {
                    bestPlanarDist = dist3D;
                    bestIndex = i;
                }
            }

            if (bestIndex < 0)
            {
                Debug.LogWarning("[PuzzleBoard] ✗ No valid slot found (all occupied)!");
                return false;
            }

            // STEP 4: Snap to the closest slot (no planar distance check)
            
            if (puzzleConfig == null || puzzleConfig.useGridSnapping)
            {
                piece.SetSnapped(slots[bestIndex].position, slots[bestIndex].rotation);
            }

            // Reparent to slotRoot so piece stays with board, not tray
            if (slotRoot != null) piece.transform.SetParent(slotRoot, true);

            piece.SetSlotIndex(bestIndex);
            placedBySlot[bestIndex] = piece;

            bool isCorrectSlot = slots[bestIndex].pieceId == piece.PieceId;
            bool morphologyMatches = !puzzleConfig || !puzzleConfig.useTriangularMorphology || CheckMorphologyMatches(bestIndex, piece);

            if (helpModeEnabled)
            {
                if (!morphologyMatches)
                {
                    OnPlacementError?.Invoke(piece);
                    if (helpModeController != null)
                    {
                        helpModeController.PlayErrorFeedback(piece.transform.position, errorGlowColor);
                    }
                }
                else if (isCorrectSlot)
                {
                    OnPlacementSuccess?.Invoke(piece);
                    if (helpModeController != null)
                    {
                        helpModeController.PlayHelpFeedback(piece.transform.position);
                    }
                }
            }

            if (isCorrectSlot)
            {
                snappedCount++;
            }

            OnPieceSnappedRaw?.Invoke(piece);
            OnPieceSnapped?.Invoke(slots[bestIndex].col, slots[bestIndex].row);

            // Milestone feedback only when piece is in correct position
            if (isCorrectSlot)
                TryPlayMilestoneFeedback(slots[bestIndex].col, slots[bestIndex].row, piece.transform.position);

            if (snappedCount >= slots.Count)
            {
                // Log detailed piece placement before completion
                LogPiecePlacementStatus();

                OnCompleted?.Invoke();
                OnPuzzleComplete?.Invoke();
            }


            // Clear highlight after successful snap
            ClearSlotHighlight();

            return true;
        }

        /// <summary>
        /// Updates the visual highlight to show which slot would be selected for the given piece position.
        /// Call this while dragging a piece to show preview.
        /// Returns the slot index being highlighted, or -1 if none.
        /// </summary>
        public int UpdateSlotHighlight(Vector3 piecePosition)
        {
            // Check if piece is close enough to the board plane
            Vector3 boardNormal = transform.forward;
            Vector3 boardPosition = transform.position;
            Vector3 pieceToBoard = piecePosition - boardPosition;
            float distanceToPlane = Mathf.Abs(Vector3.Dot(pieceToBoard, boardNormal));

            float maxDepthToPlane = (puzzleConfig != null ? puzzleConfig.maxDepthToPlaneCm : 4f) * 0.01f;
            if (distanceToPlane > maxDepthToPlane)
            {
                ClearSlotHighlight();
                return -1;
            }

            // Find closest slot, but only highlight if within snap distance
            int bestIndex = -1;
            float bestDist = float.MaxValue;
            float snapDistM = puzzleConfig != null ? puzzleConfig.snapDistanceCm * 0.01f : 0.03f;

            for (int i = 0; i < slots.Count; i++)
            {
                if (placedBySlot.ContainsKey(i)) continue;

                float dist = Vector3.Distance(piecePosition, slots[i].position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestIndex = i;
                }
            }

            // Only highlight if piece is actually within snap range (prevents highlight when picking from tray)
            if (bestIndex >= 0 && bestDist > snapDistM)
            {
                bestIndex = -1;
                ClearSlotHighlight();
            }
            else if (bestIndex >= 0 && bestIndex != currentHighlightedSlot)
            {
                HighlightSlot(bestIndex);
                Debug.Log($"[HIGHLIGHT] Slot {bestIndex} lit: piece {distanceToPlane * 100:F1}cm from board plane, {bestDist * 100:F1}cm from slot (within {snapDistM * 100:F0}cm)");
            }

            return bestIndex;
        }

        /// <summary>
        /// Returns the world position of a slot (for debug/logging).
        /// </summary>
        public Vector3 GetSlotPosition(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count) return Vector3.zero;
            return slots[slotIndex].position;
        }

        /// <summary>
        /// Returns true if the piece is close enough to the slot to snap. Prevents accidental placement when releasing far from board.
        /// </summary>
        public bool IsWithinSnapDistance(Vector3 piecePosition, int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count) return false;

            Vector3 boardNormal = transform.forward;
            Vector3 pieceToBoard = piecePosition - transform.position;
            float distanceToPlane = Mathf.Abs(Vector3.Dot(pieceToBoard, boardNormal));
            float maxDepthToPlane = (puzzleConfig != null ? puzzleConfig.maxDepthToPlaneCm : 4f) * 0.01f;
            if (distanceToPlane > maxDepthToPlane) return false;

            float distToSlot = Vector3.Distance(piecePosition, slots[slotIndex].position);
            float snapDistM = puzzleConfig != null ? puzzleConfig.snapDistanceCm * 0.01f : 0.03f;
            return distToSlot <= snapDistM;
        }

        /// <summary>
        /// Snaps a piece directly to a specific slot without recalculating which slot to use.
        /// This is used when the highlight already determined the target slot.
        /// </summary>
        public bool SnapPieceToSlot(PuzzlePiece piece, int slotIndex)
        {
            if (piece == null)
            {
                Debug.LogWarning("[PuzzleBoard] SnapPieceToSlot: piece is null");
                return false;
            }

            if (slotIndex < 0 || slotIndex >= slots.Count)
            {
                Debug.LogWarning($"[PuzzleBoard] SnapPieceToSlot: invalid slot index {slotIndex}");
                return false;
            }

            if (placedBySlot.ContainsKey(slotIndex))
            {
                Debug.LogWarning($"[PuzzleBoard] SnapPieceToSlot: slot {slotIndex} is already occupied");
                return false;
            }

            // Apply default morphology if needed
            if (IsDefaultMorphology(piece.Morphology))
            {
                piece.ApplyMorphology(GetMorphologyForPieceId(piece.PieceId));
            }

            // Snap to the slot
            if (puzzleConfig == null || puzzleConfig.useGridSnapping)
            {
                piece.SetSnapped(slots[slotIndex].position, slots[slotIndex].rotation);
            }

            piece.SetSlotIndex(slotIndex);
            placedBySlot[slotIndex] = piece;

            bool isCorrectSlot = slots[slotIndex].pieceId == piece.PieceId;
            bool morphologyMatches = !puzzleConfig || !puzzleConfig.useTriangularMorphology || CheckMorphologyMatches(slotIndex, piece);

            // Play sound based ONLY on correct slot placement (ignore morphology for audio)
            if (isCorrectSlot)
            {
                // Correct placement - special sound and particles
                if (ArtUnbound.Feedback.AudioManager.Instance != null)
                {
                    ArtUnbound.Feedback.AudioManager.Instance.PlayPieceSnap();
                }
                
                if (ArtUnbound.Feedback.PieceEffectsManager.Instance != null)
                {
                    ArtUnbound.Feedback.PieceEffectsManager.Instance.PlayCorrectPlacementEffect(piece.transform.position);
                }
            }
            else
            {
                // Incorrect placement - normal sound
                if (ArtUnbound.Feedback.AudioManager.Instance != null)
                {
                    ArtUnbound.Feedback.AudioManager.Instance.PlayPieceIncorrect();
                }
            }

            if (helpModeEnabled)
            {
                if (!isCorrectSlot)
                {
                    // Incorrect slot - show error feedback
                    OnPlacementError?.Invoke(piece);
                    if (helpModeController != null)
                    {
                        helpModeController.PlayErrorFeedback(piece.transform.position, errorGlowColor);
                    }
                }
                else
                {
                    // Correct slot - show success feedback
                    OnPlacementSuccess?.Invoke(piece);
                    if (helpModeController != null)
                    {
                        helpModeController.PlayHelpFeedback(piece.transform.position);
                    }
                }
            }

            if (isCorrectSlot)
            {
                snappedCount++;
            }

            OnPieceSnappedRaw?.Invoke(piece);
            OnPieceSnapped?.Invoke(slots[slotIndex].col, slots[slotIndex].row);

            // Milestone feedback only when piece is in correct position
            if (isCorrectSlot)
                TryPlayMilestoneFeedback(slots[slotIndex].col, slots[slotIndex].row, piece.transform.position);

            // Remove thumbnail permanently — piece is now on the board (no-op in 3D system)
            pieceTrayController?.RemoveThumbnail(piece.PieceId);

            // Reparent to slotRoot so piece moves with board, not tray
            if (slotRoot != null) piece.transform.SetParent(slotRoot, true);

            // 3D tray: remove piece and compact remaining pieces to fill the gap
            pieceTray3DController?.RemovePieceFromTray(piece);

            if (snappedCount >= slots.Count)
            {
                // Puzzle complete - play completion sound
                if (ArtUnbound.Feedback.AudioManager.Instance != null)
                {
                    ArtUnbound.Feedback.AudioManager.Instance.PlayPuzzleComplete();
                }

                OnCompleted?.Invoke();
                OnPuzzleComplete?.Invoke();
            }

            string correctnessMsg = isCorrectSlot ? "✅ CORRECT PLACEMENT" : "❌ INCORRECT PLACEMENT";

            // Notify that board state changed (triggers auto-save)
            NotifyBoardStateChanged();
            
            return true;
        }

        /// <summary>
        /// Removes a piece from its current slot on the board.
        /// Used when the player picks up a placed piece to move it.
        /// </summary>
        /// <returns>True if the piece was removed, false if it's locked (correctly placed)</returns>
        public bool RemovePieceFromSlot(PuzzlePiece piece)
        {
            if (piece == null) return false;

            int slotIndex = piece.CurrentSlotIndex;
            if (slotIndex < 0 || slotIndex >= slots.Count)
            {
                Debug.LogWarning($"[PuzzleBoard] Cannot remove piece {piece.name} - invalid slot index {slotIndex}");
                return false;
            }

            // PREVENT REMOVING CORRECTLY PLACED PIECES
            if (slots[slotIndex].pieceId == piece.PieceId)
            {
                return false; // Piece is locked, cannot remove
            }


            // Remove from the slot
            if (placedBySlot.ContainsKey(slotIndex))
            {
                placedBySlot.Remove(slotIndex);
                
                // If it was correctly placed, decrement snapped count (this shouldn't happen now due to check above)
                if (slots[slotIndex].pieceId == piece.PieceId)
                {
                    snappedCount--;
                }
            }

            piece.SetSlotIndex(-1); // Clear slot reference
            
            // Notify that board state changed (triggers auto-save)
            NotifyBoardStateChanged();
            
            return true; // Successfully removed
        }

        /// <summary>
        /// Returns a board piece to the tray (wrong slot or intentional removal).
        /// Moves the thumbnail to the end of the 2D grid so it appears as newly available.
        /// For pieces grabbed directly from the tray, InteractionManager calls
        /// piece.SetState(InPool) instead to avoid the MoveToEnd grid shift.
        /// </summary>
        public void ReturnPieceToTray(PuzzlePiece piece)
        {
            if (piece == null) return;

            // 3D SYSTEM: add piece to the end of the tray grid
            if (pieceTray3DController != null)
            {
                pieceTray3DController.AddPieceAtEnd(piece);
                NotifyBoardStateChanged();
                return;
            }

            // OLD SYSTEM (2D thumbnails + PieceScrollController)
            pieceTrayController?.MoveToEnd(piece.PieceId);
            if (pieceTrayController != null)
            {
                piece.SetState(PieceState.InPool);
            }
            else if (scrollController != null)
            {
                scrollController.AddPieceAtEnd(piece);
            }
            else
            {
                Debug.LogWarning("[PuzzleBoard] Cannot return piece to tray - no tray controller available");
                return;
            }

            NotifyBoardStateChanged();
        }

        public void HighlightSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count) return;

            
            currentHighlightedSlot = slotIndex;

            // Create highlight object if it doesn't exist
            if (slotHighlight == null)
            {
                slotHighlight = GameObject.CreatePrimitive(PrimitiveType.Quad);
                slotHighlight.name = "SlotHighlight";
                Destroy(slotHighlight.GetComponent<Collider>()); // Remove collider

                // Create bright, more visible yellow-green material
                Shader highlightShader = Shader.Find("Unlit/Color");
                if (highlightShader == null) highlightShader = Shader.Find("Sprites/Default");
                if (highlightShader == null) highlightShader = Shader.Find("UI/Default");
                
                Material highlightMat = new Material(highlightShader);
                highlightMat.color = new Color(0.5f, 1f, 0f, 0.8f); // Bright yellow-green, more opaque
                slotHighlight.GetComponent<Renderer>().material = highlightMat;
                
            }

            // Position and scale the highlight in SLOT ROOT local space
            PuzzleSlot slot = slots[slotIndex];
            float pieceW = currentPieceWidth  > 0f ? currentPieceWidth  : (puzzleConfig != null ? puzzleConfig.maxPieceSizeM : 0.05f);
            float pieceH = currentPieceHeight > 0f ? currentPieceHeight : (puzzleConfig != null ? puzzleConfig.maxPieceSizeM : 0.05f);

            // Convert slot world position to local position relative to slotRoot
            Vector3 localPos = slotRoot.InverseTransformPoint(slot.position);
            localPos.z = 0.017f; // Slightly in front of grid and numbers for visibility

            slotHighlight.transform.SetParent(slotRoot, false);
            slotHighlight.transform.localPosition = localPos;
            slotHighlight.transform.localRotation = Quaternion.Euler(0, 180, 0); // Rotate 180° to face user
            // Make it slightly larger than the piece for better visibility
            slotHighlight.transform.localScale = new Vector3(pieceW * 1.1f, pieceH * 1.1f, 1f);
            slotHighlight.SetActive(true);
            
        }

        public void ClearSlotHighlight()
        {
            if (slotHighlight != null)
            {
                slotHighlight.SetActive(false);
            }
            currentHighlightedSlot = -1;
        }

        /// <summary>Returns true if the ray intersects the board plane within the board's bounds.</summary>
        public bool RayHitsBoard(Ray ray)
        {
            Plane p = new Plane(-transform.forward, transform.position);
            if (!p.Raycast(ray, out float dist) || dist <= 0f) return false;
            Vector3 local = transform.InverseTransformPoint(ray.GetPoint(dist));
            return Mathf.Abs(local.x) <= boardWidthM  * 0.5f + 0.05f
                && Mathf.Abs(local.y) <= boardHeightM * 0.5f + 0.05f;
        }

        /// <summary>
        /// Returns the placed piece whose slot is closest to where the ray intersects the board plane.
        /// Returns null if the ray misses the board or no pieces are placed near the hit point.
        /// </summary>
        public PuzzlePiece GetPlacedPieceAtRaycast(Ray ray)
        {
            Plane p = new Plane(-transform.forward, transform.position);
            if (!p.Raycast(ray, out float dist) || dist <= 0f) return null;
            Vector3 hit   = ray.GetPoint(dist);
            Vector3 local = transform.InverseTransformPoint(hit);
            if (Mathf.Abs(local.x) > boardWidthM  * 0.5f + 0.05f) return null;
            if (Mathf.Abs(local.y) > boardHeightM * 0.5f + 0.05f) return null;

            PuzzlePiece best = null;
            float bestDist = float.MaxValue;
            foreach (var kvp in placedBySlot)
            {
                // Skip hidden pieces (e.g. during PostGame full-image reveal)
                if (kvp.Value == null || !kvp.Value.gameObject.activeSelf) continue;
                float d = Vector3.Distance(hit, slots[kvp.Key].position);
                if (d < bestDist) { bestDist = d; best = kvp.Value; }
            }
            // Only return if within ~70% of the smaller piece dimension (avoids grabbing distant pieces).
            float minPieceDim = Mathf.Min(currentPieceWidth, currentPieceHeight);
            return bestDist <= minPieceDim * 0.7f ? best : null;
        }

        /// <summary>
        /// Returns the index of the closest unoccupied slot to where the ray intersects the board.
        /// Returns -1 if the ray misses the board or all slots are occupied.
        /// </summary>
        public int GetSlotAtRaycast(Ray ray)
        {
            Plane p = new Plane(-transform.forward, transform.position);
            if (!p.Raycast(ray, out float dist) || dist <= 0f) return -1;
            Vector3 hit   = ray.GetPoint(dist);
            Vector3 local = transform.InverseTransformPoint(hit);
            if (Mathf.Abs(local.x) > boardWidthM  * 0.5f + 0.05f) return -1;
            if (Mathf.Abs(local.y) > boardHeightM * 0.5f + 0.05f) return -1;

            int best = -1; float bestDist = float.MaxValue;
            for (int i = 0; i < slots.Count; i++)
            {
                if (placedBySlot.ContainsKey(i)) continue;
                float d = Vector3.Distance(hit, slots[i].position);
                if (d < bestDist) { bestDist = d; best = i; }
            }
            return best;
        }

        private void CreateSlotsFromCount(int difficultyIndex)
        {
            if (currentTexture == null)
            {
                Debug.LogError("[PuzzleBoard] No texture assigned for puzzle generation. Check that ArtworkDefinition.fullImage has a valid Sprite. Pieces will NOT appear.");
                return;
            }

            if (piecePrefab == null)
            {
                Debug.LogError("[PuzzleBoard] piecePrefab is not assigned in the Inspector. Pieces will NOT appear.");
                return;
            }

            CalculateGridAndPieces(difficultyIndex, currentTexture.width, currentTexture.height,
                out int cols, out int rows, out float pieceWidth, out float pieceHeight,
                out float boardW, out float boardH);

            BuildSlotsAndPieces(cols, rows, pieceWidth, pieceHeight, boardW, boardH,
                seed: difficultyIndex ^ (currentTexture.width * 31) ^ (currentTexture.height * 17),
                texture: currentTexture);
        }

        private void CreateSlots(ArtworkDefinition definition, int difficultyIndex)
        {
            var textureToUse = definition.puzzleTexture != null ? definition.puzzleTexture : definition.fullImage?.texture;
            if (textureToUse == null)
            {
                Debug.LogError($"[PuzzleBoard] Artwork '{definition?.artworkId}' has no texture (puzzleTexture and fullImage.texture are null). Pieces will NOT appear.");
                return;
            }

            CalculateGridAndPieces(difficultyIndex, textureToUse.width, textureToUse.height,
                out int cols, out int rows, out float pieceWidth, out float pieceHeight,
                out float boardW, out float boardH);

            BuildSlotsAndPieces(cols, rows, pieceWidth, pieceHeight, boardW, boardH,
                seed: definition.artworkId?.GetHashCode() ?? 0,
                texture: textureToUse);
        }

        private void BuildSlotsAndPieces(int cols, int rows, float pieceWidth, float pieceHeight,
            float boardW, float boardH, int seed, Texture2D texture)
        {
            gridCols = cols;
            gridRows = rows;
            currentPieceWidth = pieceWidth;
            currentPieceHeight = pieceHeight;

            GenerateEdgeStates(cols, rows, seed);

            int id = 0;
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    Vector3 localPos = new Vector3(
                        (-boardW * 0.5f) + pieceWidth  * 0.5f + pieceWidth  * x,
                        ( boardH * 0.5f) - pieceHeight * 0.5f - pieceHeight * y,
                        0.015f
                    );

                    // Slot (col,row) expects the piece that shows texture for that position.
                    // UVs are flipped horizontally (board 180° Y), so piece (cols-1-x, y) shows texture at (x,y).
                    int expectedPieceId = y * cols + (cols - 1 - x);
                    int slotCol = cols - 1 - x;
                    PieceMorphology morphology = GenerateMorphology(slotCol, y, cols, rows);

                    PuzzleSlot slot = new PuzzleSlot
                    {
                        pieceId = expectedPieceId,
                        row = y,
                        col = x,
                        position = slotRoot.TransformPoint(localPos),
                        rotation = slotRoot.rotation,
                        morphology = morphology
                    };

                    slots.Add(slot);
                    morphologyByPieceId[id] = morphology;

                    CreatePiece(id, x, y, cols, rows, morphology, pieceWidth, pieceHeight, texture);
                    id++;
                }
            }

            totalPieces = slots.Count;
            Debug.Log($"[PIECE] BuildSlotsAndPieces: cols={cols}, rows={rows}, " +
                      $"pieceW={pieceWidth*100f:F2}cm, pieceH={pieceHeight*100f:F2}cm, " +
                      $"board={boardW*100f:F1}x{boardH*100f:F1}cm, total={totalPieces}");

            CreateBoardVisual(boardW, boardH);
            InitializeScroll();
        }

        private void CreateBoardVisual(float width, float height)
        {
            if (slotRoot == null) return;

            boardWidthM = width;
            boardHeightM = height;

            // Look for existing board visual and destroy it IMMEDIATELY
            Transform existing = slotRoot.Find("BoardVisual");
            if (existing != null)
            {
                DestroyImmediate(existing.gameObject);
            }

            GameObject boardViz = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boardViz.name = "BoardVisual";
            boardViz.transform.SetParent(slotRoot, false);

            // Set scale (slightly larger than puzzle)
            float margin = 0f; // No margin per user feedback ("I expect to place it on the edge")
            float thickness = 0.01f; // 1cm thickness
            boardViz.transform.localScale = new Vector3(width + margin * 2, height + margin * 2, thickness);

            // Position behind pieces (Z+ is forward, if pieces are at 0, board should be at +thickness/2 or -thickness/2? 
            // If user looks at -Z, pieces are at Z=0. Board should be further away (more negative? or positive?)
            // If pieces face -Z (towards user), board should be at Z > 0 (behind pieces).
            // Actually, let's just put it at Z = 0.01f (behind pieces if camera is at -Z)
            boardViz.transform.localPosition = new Vector3(0, 0, 0.01f);

            // Backing surface: optional fabric material/texture, else white canvas
            var renderer = boardViz.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (boardBackingMaterial != null)
                {
                    renderer.material = new Material(boardBackingMaterial);
                }
                else
                {
                    Shader boardShader = Shader.Find("Universal Render Pipeline/Lit");
                    if (boardShader == null) boardShader = Shader.Find("Standard");
                    if (boardShader == null) boardShader = Shader.Find("Diffuse");
                    if (boardShader == null) boardShader = Shader.Find("Mobile/Diffuse");

                    Material boardMat = new Material(boardShader);
                    boardMat.color = Color.white;
                    if (boardBackingTexture != null)
                    {
                        if (boardMat.HasProperty("_BaseMap")) boardMat.SetTexture("_BaseMap", boardBackingTexture);
                        if (boardMat.HasProperty("_MainTex")) boardMat.SetTexture("_MainTex", boardBackingTexture);
                    }
                    renderer.material = boardMat;
                }
            }

            // Remove collider if it interferes with raycast (though we might want it for placement later)
            // For now, remove to avoid blocking piece interaction if it's in front
            Destroy(boardViz.GetComponent<Collider>());

            // Create grid lines
            CreateGridLines(slotRoot, width, height);

            // Create slot number labels (COMMENTED OUT - may be needed for debugging later)
            // CreateSlotLabels();
        }

        private void CreateGridLines(Transform parent, float width, float height)
        {
            // Look for existing grid and destroy it IMMEDIATELY
            Transform existing = parent.Find("GridLines");
            if (existing != null)
            {
                DestroyImmediate(existing.gameObject);
            }

            GameObject gridContainer = new GameObject("GridLines");
            gridContainer.transform.SetParent(parent, false);
            gridContainer.transform.localPosition = new Vector3(0, 0, 0.015f);

            // Get grid dimensions from slots
            if (slots.Count == 0) return;

            int cols = 0;
            int rows = 0;
            foreach (var slot in slots)
            {
                if (slot.col + 1 > cols) cols = slot.col + 1;
                if (slot.row + 1 > rows) rows = slot.row + 1;
            }

            float pieceWidth = width / cols;
            float pieceHeight = height / rows;

            // Create material for lines - Dark grey on white board
            Shader lineShader = Shader.Find("Unlit/Color");
            if (lineShader == null) lineShader = Shader.Find("Sprites/Default");
            if (lineShader == null) lineShader = Shader.Find("UI/Default");
            
            Material lineMaterial = new Material(lineShader);
            lineMaterial.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            // Create vertical lines
            for (int i = 0; i <= cols; i++)
            {
                GameObject line = new GameObject($"VLine_{i}");
                line.transform.SetParent(gridContainer.transform, false);
                
                LineRenderer lr = line.AddComponent<LineRenderer>();
                lr.material = lineMaterial;
                lr.startWidth = 0.0005f; // 0.5mm width - very thin lines
                lr.endWidth = 0.0005f;
                lr.positionCount = 2;
                lr.useWorldSpace = false;
                
                float x = -width / 2f + i * pieceWidth;
                lr.SetPosition(0, new Vector3(x, -height / 2f, 0));
                lr.SetPosition(1, new Vector3(x, height / 2f, 0));
            }

            // Create horizontal lines
            for (int i = 0; i <= rows; i++)
            {
                GameObject line = new GameObject($"HLine_{i}");
                line.transform.SetParent(gridContainer.transform, false);
                
                LineRenderer lr = line.AddComponent<LineRenderer>();
                lr.material = lineMaterial;
                lr.startWidth = 0.0005f; // 0.5mm width - very thin lines
                lr.endWidth = 0.0005f;
                lr.positionCount = 2;
                lr.useWorldSpace = false;
                
                float y = -height / 2f + i * pieceHeight;
                lr.SetPosition(0, new Vector3(-width / 2f, y, 0));
                lr.SetPosition(1, new Vector3(width / 2f, y, 0));
            }
        }

        private void CreateSlotLabels()
        {
            // Look for existing labels
            Transform existing = slotRoot.Find("SlotLabels");
            if (existing != null) Destroy(existing.gameObject);

            GameObject labelContainer = new GameObject("SlotLabels");
            labelContainer.transform.SetParent(slotRoot, false);
            labelContainer.transform.localPosition = new Vector3(0, 0, 0.016f); // In front of grid

            float pieceSize = currentPieceWidth > 0f ? Mathf.Min(currentPieceWidth, currentPieceHeight)
                                                    : (puzzleConfig != null ? puzzleConfig.maxPieceSizeM : 0.05f);

            for (int i = 0; i < slots.Count; i++)
            {
                PuzzleSlot slot = slots[i];
                
                // Create a 3D Text Mesh for the label
                GameObject labelObj = new GameObject($"Label_{i}");
                labelObj.transform.SetParent(labelContainer.transform, false);
                
                // Convert to local space
                Vector3 localPos = slotRoot.InverseTransformPoint(slot.position);
                localPos.z = 0.016f; // In front of grid
                
                labelObj.transform.localPosition = localPos;
                // Rotate 180 degrees around Y axis to face the user correctly
                labelObj.transform.localRotation = Quaternion.Euler(0, 180, 0);
                
                TextMesh textMesh = labelObj.AddComponent<TextMesh>();
                textMesh.text = i.ToString();
                textMesh.characterSize = 0.003f; // 3mm characters (smaller)
                textMesh.fontSize = 50;
                textMesh.anchor = TextAnchor.MiddleCenter;
                textMesh.alignment = TextAlignment.Center;
                textMesh.color = Color.black;
                
                // Optional: Add a renderer material if default doesn't work
                var renderer = labelObj.GetComponent<MeshRenderer>();
                if (renderer != null && renderer.sharedMaterial == null)
                {
                    Material textMat = new Material(Shader.Find("GUI/Text Shader"));
                    if (textMat.shader == null) textMat = new Material(Shader.Find("Unlit/Color"));
                    renderer.material = textMat;
                }
            }

        }

        /// <summary>
        /// Calculates optimal grid dimensions and piece size based on fixed board width.
        /// NEW SYSTEM: Board width is fixed (50cm), piece size is dynamic.
        /// Computes board dimensions and grid for the given difficulty and texture.
        /// Approach:
        ///   1. Fit the texture's aspect ratio inside a square bounding box of side
        ///      <c>puzzleConfig.boardMaxSizeM</c>. Landscape images get the full side as width;
        ///      portrait images get the full side as height. This keeps the actual board
        ///      ratio identical to the painting's — no rounding distortion.
        ///   2. For each axis, pick a grid count that keeps the resulting piece dimension
        ///      close to <c>puzzleConfig.GetTargetPieceSize(difficultyIndex)</c>:
        ///        - Easy   → round UP   (cols/rows go up so pieces shrink toward target ≤ max)
        ///        - Hard   → round DOWN (cols/rows go down so pieces grow toward target ≥ min)
        ///        - Normal → round CLOSEST to the target midpoint
        ///   3. Pieces are rectangular: pieceWidth and pieceHeight are independent. The
        ///      "rectangularness" is small in practice (typically &lt;10%) because both axes
        ///      target the same piece size — only the rounding direction differs.
        /// </summary>
        private void CalculateGridAndPieces(int difficultyIndex, int texWidth, int texHeight,
            out int cols, out int rows, out float pieceWidth, out float pieceHeight,
            out float boardWidth, out float boardHeight)
        {
            float boundingBox = puzzleConfig != null ? puzzleConfig.boardMaxSizeM : 0.6f;
            float targetPieceSize = puzzleConfig != null
                ? puzzleConfig.GetTargetPieceSize(difficultyIndex)
                : 0.05f;

            float aspectRatio = texHeight > 0 ? (float)texWidth / texHeight : 1f;
            if (aspectRatio >= 1f)
            {
                boardWidth = boundingBox;
                boardHeight = boundingBox / aspectRatio;
            }
            else
            {
                boardHeight = boundingBox;
                boardWidth = boundingBox * aspectRatio;
            }

            cols = ChooseGridCount(boardWidth, targetPieceSize, difficultyIndex);
            rows = ChooseGridCount(boardHeight, targetPieceSize, difficultyIndex);

            pieceWidth = boardWidth / cols;
            pieceHeight = boardHeight / rows;
        }

        /// <summary>
        /// Picks an integer grid count along one axis such that the resulting piece dimension
        /// satisfies the difficulty's bound (Easy ≤ max, Hard ≥ min, Normal closest to midpoint).
        /// </summary>
        private static int ChooseGridCount(float boardSpan, float targetPieceSize, int difficultyIndex)
        {
            if (targetPieceSize <= 0f || boardSpan <= 0f) return 2;
            float approx = boardSpan / targetPieceSize;
            int floor = Mathf.Max(2, Mathf.FloorToInt(approx));
            int ceil = Mathf.Max(2, Mathf.CeilToInt(approx));
            if (floor == ceil) return floor;

            return difficultyIndex switch
            {
                0 => ceil,  // Easy: keep piece ≤ max → more cells
                2 => floor, // Hard: keep piece ≥ min → fewer cells
                _ => Mathf.Abs(boardSpan / floor - targetPieceSize) <= Mathf.Abs(boardSpan / ceil - targetPieceSize) ? floor : ceil
            };
        }

        /// <summary>
        /// DEPRECATED: Old method kept for reference. Use CalculateGridAndPieceSize instead.
        /// </summary>
        private void CalculateGridDimensions(int targetCount, int texWidth, int texHeight, out int cols, out int rows)
        {
            float ratio = (float)texWidth / texHeight;
            // rows = sqrt(target / ratio)
            rows = Mathf.RoundToInt(Mathf.Sqrt(targetCount / ratio));
            if (rows < 2) rows = 2;
            cols = Mathf.RoundToInt(rows * ratio);
            if (cols < 2) cols = 2;
            
            // Log actual piece count (may differ from target due to aspect ratio)
            int actualCount = cols * rows;
        }

        /// <summary>
        /// Builds the hEdges / vEdges arrays that drive piece morphology variety.
        /// Each internal shared edge is randomly Positive or Negative (seeded by artworkId
        /// so the same puzzle always produces the same shapes).
        /// </summary>
        private void GenerateEdgeStates(int cols, int rows, int seed)
        {
            edgeCols = cols;

            // Save and restore Unity's global random state so we don't affect anything else.
            UnityEngine.Random.State savedState = UnityEngine.Random.state;
            UnityEngine.Random.InitState(seed);

            // Horizontal shared edges: bottom of piece (col, row) for row 0..rows-2
            hEdges = new bool[cols * (rows - 1)];
            for (int i = 0; i < hEdges.Length; i++)
                hEdges[i] = UnityEngine.Random.value > 0.5f;

            // Vertical shared edges: right of piece (col, row) for col 0..cols-2
            vEdges = new bool[(cols - 1) * rows];
            for (int i = 0; i < vEdges.Length; i++)
                vEdges[i] = UnityEngine.Random.value > 0.5f;

            UnityEngine.Random.state = savedState;
        }

        private PieceMorphology GenerateMorphology(int col, int row, int numCols, int numRows)
        {
            // Each shared edge state was assigned in GenerateEdgeStates.
            // A piece sees its neighbor's shared edge as the OPPOSITE state → always fits.
            PieceEdgeState H(int c, int r) => hEdges[r * numCols + c] ? PieceEdgeState.Positive : PieceEdgeState.Negative;
            PieceEdgeState V(int c, int r) => vEdges[r * (numCols - 1) + c] ? PieceEdgeState.Positive : PieceEdgeState.Negative;
            PieceEdgeState OppH(int c, int r) => hEdges[r * numCols + c] ? PieceEdgeState.Negative : PieceEdgeState.Positive;
            PieceEdgeState OppV(int c, int r) => vEdges[r * (numCols - 1) + c] ? PieceEdgeState.Negative : PieceEdgeState.Positive;

            return new PieceMorphology
            {
                // top:    opposite of the bottom edge of the piece above us
                top    = row == 0           ? PieceEdgeState.Flat : OppH(col, row - 1),
                // bottom: our own bottom edge
                bottom = row == numRows - 1 ? PieceEdgeState.Flat : H(col, row),
                // right:  our own right edge
                right  = col == numCols - 1 ? PieceEdgeState.Flat : V(col, row),
                // left:   opposite of the right edge of the piece to our left
                left   = col == 0           ? PieceEdgeState.Flat : OppV(col - 1, row)
            };
        }

        private bool CheckMorphologyMatches(int slotIndex, PuzzlePiece piece)
        {
            PuzzleSlot slot = slots[slotIndex];

            int topIndex = GetSlotIndex(slot.row - 1, slot.col);
            int rightIndex = GetSlotIndex(slot.row, slot.col + 1);
            int bottomIndex = GetSlotIndex(slot.row + 1, slot.col);
            int leftIndex = GetSlotIndex(slot.row, slot.col - 1);

            if (topIndex >= 0 && placedBySlot.TryGetValue(topIndex, out PuzzlePiece topPiece))
            {
                if (!EdgesComplement(piece.Morphology.top, topPiece.Morphology.bottom))
                {
                    return false;
                }
            }

            if (rightIndex >= 0 && placedBySlot.TryGetValue(rightIndex, out PuzzlePiece rightPiece))
            {
                if (!EdgesComplement(piece.Morphology.right, rightPiece.Morphology.left))
                {
                    return false;
                }
            }

            if (bottomIndex >= 0 && placedBySlot.TryGetValue(bottomIndex, out PuzzlePiece bottomPiece))
            {
                if (!EdgesComplement(piece.Morphology.bottom, bottomPiece.Morphology.top))
                {
                    return false;
                }
            }

            if (leftIndex >= 0 && placedBySlot.TryGetValue(leftIndex, out PuzzlePiece leftPiece))
            {
                if (!EdgesComplement(piece.Morphology.left, leftPiece.Morphology.right))
                {
                    return false;
                }
            }

            return true;
        }

        private PieceMorphology GetMorphologyForPieceId(int pieceId)
        {
            if (morphologyByPieceId.TryGetValue(pieceId, out PieceMorphology morphology))
            {
                return morphology;
            }

            return new PieceMorphology();
        }

        private bool IsDefaultMorphology(PieceMorphology morphology)
        {
            return morphology.top == PieceEdgeState.Flat
                && morphology.right == PieceEdgeState.Flat
                && morphology.bottom == PieceEdgeState.Flat
                && morphology.left == PieceEdgeState.Flat;
        }

        private int GetSlotIndex(int row, int col)
        {
            if (row < 0 || col < 0)
            {
                return -1;
            }

            int gridSize = Mathf.RoundToInt(Mathf.Sqrt(slots.Count));
            if (col >= gridSize || row >= gridSize)
            {
                return -1;
            }

            return row * gridSize + col;
        }

        private void CreatePiece(int id, int col, int row, int gridCols, int gridRows, PieceMorphology morphology, float pieceWidth, float pieceHeight, Texture2D texture)
        {
            if (piecePrefab == null) return;

            // Instantiate parented to slotRoot so they move with the board
            PuzzlePiece piece = Instantiate(piecePrefab, slotRoot);
            piece.transform.position = slotRoot.position; // Initially at root, scroll will move them
            piece.transform.rotation = Quaternion.identity;

            piece.Initialize(id, GetSlotIndex(row, col), morphology);
            piece.name = $"Piece_{id}_{col}_{row}";

            // Generate Mesh
            Mesh mesh = PieceMeshGenerator.GeneratePieceMesh(morphology, pieceWidth, pieceHeight, col, row, gridCols, gridRows);

            // Assign Mesh
            var meshFilter = piece.GetComponentInChildren<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.mesh = mesh;
                // Debug.Log($"[PuzzleBoard] Piece {id} Mesh Vertices: {mesh.vertexCount}, Triangles: {mesh.triangles.Length}");
            }
            var meshRenderer = piece.GetComponentInChildren<MeshRenderer>();
            if (meshRenderer != null && texture != null)
            {
                // FORCE SAFE MATERIAL: Create a new material to bypass any Prefab shader issues
                // Try to find URP Lit first, then Standard, then Legacy Diffuse
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Toon/Lit Input"); // Try different URP
                if (shader == null) shader = Shader.Find("Standard");
                if (shader == null) shader = Shader.Find("Mobile/Diffuse");

                if (shader != null)
                {
                    Material safeMat = new Material(shader);
                    // Set texture on both common property names just in case
                    if (safeMat.HasProperty("_BaseMap")) safeMat.SetTexture("_BaseMap", texture);
                    if (safeMat.HasProperty("_MainTex")) safeMat.SetTexture("_MainTex", texture);
                    safeMat.color = Color.white; // Ensure not black

                    meshRenderer.material = safeMat;
                }
                else
                {
                    Debug.LogError("[PuzzleBoard] Could not find any valid shader! Pieces may be invisible.");
                }
            }

            // Store UV region for editor comparison with thumbnail
            {
                float uL  = (float)col / gridCols;
                float uR  = (float)(col + 1) / gridCols;
                float vBt = 1f - (float)(row + 1) / gridRows;
                float vTp = 1f - (float)row / gridRows;
                piece.SetMeshUvRegion(new Rect(uL, vBt, uR - uL, vTp - vBt));
            }

            // Add collider for interaction
            var collider = piece.GetComponentInChildren<MeshCollider>();
            if (collider != null)
            {
                collider.sharedMesh = mesh;
            }

            activePieces.Add(piece);
        }

        private Vector3 TrayLocalPosition()
        {
            float x = puzzleConfig != null ? puzzleConfig.trayOffsetX : 0.45f;
            float y = puzzleConfig != null ? puzzleConfig.trayOffsetY : 0f;
            float z = puzzleConfig != null ? puzzleConfig.trayOffsetZ : 0f;
            return new Vector3(-x, y, z);
        }

        private void InitializeScroll()
        {
            // ── 3D TRAY SYSTEM ─────────────────────────────────────────────────────
            if (pieceTray3DController != null)
            {
                float rotY = puzzleConfig != null ? puzzleConfig.trayRotationY : -35f;
                pieceTray3DController.transform.localPosition = TrayLocalPosition();
                pieceTray3DController.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
                pieceTray3DController.transform.localScale    = Vector3.one;

                var shuffled3D = PieceShuffler.GetShuffledCopy(activePieces);
                // Tray cells are square; use the larger piece dimension so rectangular thumbnails fit without clipping.
                float trayCellSize = Mathf.Max(currentPieceWidth, currentPieceHeight);
                pieceTray3DController.Initialize(shuffled3D, trayCellSize);
                return;
            }
            // ── OLD SYSTEM (fallback) ──────────────────────────────────────────────

            if (scrollController == null)
            {
                scrollController = GetComponentInChildren<ArtUnbound.UI.PieceScrollController>();
            }

            if (scrollController == null)
            {
                // Try to find an existing "PieceTray" object (common name in prefabs)
                Transform tray = transform.Find("PieceTray");
                if (tray != null)
                {
                    scrollController = tray.GetComponent<ArtUnbound.UI.PieceScrollController>();
                    if (scrollController == null)
                    {
                        scrollController = tray.gameObject.AddComponent<ArtUnbound.UI.PieceScrollController>();
                    }

                    // Force reset position to ensure it's visible below board
                    tray.localPosition = new Vector3(0, -0.4f, 0);
                    tray.localRotation = Quaternion.identity;
                    tray.localScale = Vector3.one;
                }
            }

            if (scrollController == null)
            {
                Debug.LogWarning("[PuzzleBoard] PieceScrollController missing. Creating default Scroll Container.");
                GameObject scrollObj = new GameObject("PieceScrollContainer");
                scrollObj.transform.SetParent(transform, false);
                // Position below the board (approx 30cm down)
                scrollObj.transform.localPosition = new Vector3(0, -0.3f, 0);

                scrollController = scrollObj.AddComponent<ArtUnbound.UI.PieceScrollController>();
            }

            if (scrollController == null)
            {
                Debug.LogError("[PuzzleBoard] Failed to create or find PieceScrollController!");
                return;
            }

            // VERTICAL TRAY: Position to the RIGHT of the board at same height
            // INVERTED: Canvas is rotated 180°, so player's right = negative X
            // Distance: 0.45m (45cm to player's right) to accommodate 5-column grid
            scrollController.transform.localPosition = new Vector3(-0.45f, 0f, 0f);
            scrollController.transform.localRotation = Quaternion.identity;
            scrollController.transform.localScale = Vector3.one;

            // Remove any interfering ScrollRect component that shouldn't be here
            var sr = scrollController.GetComponent<UnityEngine.UI.ScrollRect>();
            if (sr != null)
            {
                Debug.LogWarning("[PuzzleBoard] Removing extraneous ScrollRect component from PieceTray.");
                Destroy(sr);
            }

            // Create visual background for the tray
            // REMOVED: User requested to remove the visual tray.
            // CreateTrayVisual(scrollController.transform);

            // Shuffle active pieces for initial display
            var shuffledPieces = PieceShuffler.GetShuffledCopy(activePieces);

            List<Transform> pieceTransforms = new List<Transform>();
            foreach (var p in shuffledPieces)
            {
                pieceTransforms.Add(p.transform);
                // SetState(InPool) is a no-op for newly created pieces (default state is already InPool).
                // Call ShowThumbnailMode directly so MeshRenderers are hidden immediately.
                p.SetState(PieceState.InPool);
                p.ShowThumbnailMode();
            }

            Debug.Log($"[PIECE] InitializeScroll: passing {pieceTransforms.Count} pieces to tray (slots={slots.Count})");
            float legacyTrayCellSize = Mathf.Max(currentPieceWidth, currentPieceHeight);
            scrollController.Initialize(pieceTransforms, legacyTrayCellSize);

            // Initialize the 2D thumbnail panel (positioned wherever the designer placed it in the scene)
            if (pieceTrayController != null)
            {
                pieceTrayController.Initialize(shuffledPieces, currentTexture, gridCols, gridRows, legacyTrayCellSize);

                // Move the 3D tray far below so its pieces don't overlap with the 2D panel.
                // Pieces are hidden (MeshRenderer disabled) but the 3D tray was positioned at the
                // same world-space location as the canvas panel, causing depth-buffer artifacts.
                scrollController.transform.localPosition = new Vector3(0f, -5f, 0f);
            }
        }

        private void CreateTrayVisual(Transform tray)
        {
            // First, check if the tray ITSELF has an Image component (like the screenshot showed)
            var existingImage = tray.GetComponent<UnityEngine.UI.Image>();
            if (existingImage != null)
            {
                // Make it semi-transparent red
                existingImage.color = new Color(1f, 0f, 0f, 0.3f);
                // Also ensure it is large enough
                var rect = tray.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.sizeDelta = new Vector2(3000, 400); // 3m x 0.4m (assuming 1 unit = 1mm? No, UI is weird. Let's just trust color for now)
                                                             // Actually, if it's world space, we should rely on scale.
                }
                return;
            }

            // Check if child visual already exists
            if (tray.Find("TrayVisual") != null) return;

            // Create a simple Cube visual (Standard 3D Object)
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "TrayVisual";
            visual.transform.SetParent(tray, false);

            // Remove collider preventing interaction
            Destroy(visual.GetComponent<Collider>());

            // Set Material (Transparent Red)
            var renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.SetFloat("_Mode", 3); // Transparent
                renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                renderer.material.SetInt("_ZWrite", 0);
                renderer.material.DisableKeyword("_ALPHATEST_ON");
                renderer.material.EnableKeyword("_ALPHABLEND_ON");
                renderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                renderer.material.renderQueue = 3000;
                renderer.material.color = new Color(1f, 0f, 0f, 0.3f);
            }

            // Set Size: 0.5m width (matched to PieceScrollController), 0.1m height, 0.001m thickness
            // This relies on the parent (PieceTray) having scale (1,1,1), which we enforce.
            visual.transform.localScale = new Vector3(0.5f, 0.15f, 0.001f); // Increased height slightly to 0.15 for easier grabbing
        }

        private bool EdgesComplement(PieceEdgeState a, PieceEdgeState b)
        {
            if (a == PieceEdgeState.Flat && b == PieceEdgeState.Flat)
            {
                return true;
            }

            return (a == PieceEdgeState.Positive && b == PieceEdgeState.Negative)
                || (a == PieceEdgeState.Negative && b == PieceEdgeState.Positive);
        }

        #region Save/Load State

        /// <summary>
        /// Captures the current state of the board for saving.
        /// Returns a list of all placed pieces with their positions.
        /// </summary>
        public List<PlacedPieceData> GetCurrentBoardState()
        {
            var state = new List<PlacedPieceData>();
            
            foreach (var kvp in placedBySlot)
            {
                int slotIndex = kvp.Key;
                PuzzlePiece piece = kvp.Value;
                
                if (piece != null)
                {
                    state.Add(new PlacedPieceData(piece.PieceId, slotIndex));
                }
            }
            
            Debug.Log($"[PIECE] GetCurrentBoardState: saving {state.Count} placed (slots={slots.Count}, totalPieces={totalPieces})");
            return state;
        }

        /// <summary>
        /// Restores the board state from saved data.
        /// Places pieces directly on the board without animation.
        /// </summary>
        public void RestoreBoardState(List<PlacedPieceData> savedState)
        {
            if (savedState == null || savedState.Count == 0)
            {
                return;
            }
            
            Debug.Log($"[PIECE] RestoreBoardState: restoring {savedState.Count} pieces (activePieces={activePieces.Count}, slots={slots.Count})");
            
            int restoredCount = 0;
            
            foreach (var data in savedState)
            {
                // Validate indices
                if (data.slotIndex < 0 || data.slotIndex >= slots.Count)
                {
                    Debug.LogWarning($"[PuzzleBoard] Invalid slot index {data.slotIndex}, skipping");
                    continue;
                }
                
                // Find the piece by ID
                var piece = activePieces.Find(p => p.PieceId == data.pieceId);
                if (piece == null)
                {
                    Debug.LogWarning($"[PuzzleBoard] Piece with ID {data.pieceId} not found, skipping");
                    continue;
                }
                
                var slot = slots[data.slotIndex];
                
                // Place piece directly without animation
                piece.transform.position = slot.position;
                piece.transform.rotation = transform.rotation;
                piece.SetSlotIndex(data.slotIndex); // Required so RemovePieceFromSlot works for incorrectly placed pieces
                piece.SetState(PieceState.Placed);
                
                // Make sure piece is active and visible
                piece.gameObject.SetActive(true);
                
                // Register in the board
                placedBySlot[data.slotIndex] = piece;
                
                // Only increment snappedCount if piece is in the CORRECT slot
                bool isCorrectSlot = (slot.pieceId == piece.PieceId);
                if (isCorrectSlot)
                {
                    snappedCount++;
                }
                
                restoredCount++;
                
                // Reparent to slotRoot so the piece stays with the board
                if (slotRoot != null) piece.transform.SetParent(slotRoot, true);

                // Remove from tray list (both systems)
                pieceTray3DController?.RemovePieceFromTray(piece);
                if (pieceTray3DController == null && scrollController != null)
                {
                    scrollController.RemovePieceFromTray(piece);
                    piece.gameObject.SetActive(true);
                }
            }
            
            int trayInPool = 0;
            if (scrollController != null)
            {
                foreach (var p in activePieces)
                    if (p.CurrentState == PieceState.InPool || p.CurrentState == PieceState.Returning) trayInPool++;
            }
            Debug.Log($"[PIECE] RestoreBoardState done: restored={restoredCount}/{savedState.Count}, correctOnBoard={snappedCount}, trayInPool={trayInPool}, totalPieces={totalPieces}");

            // Ensure remaining tray pieces are visible and correctly positioned
            if (pieceTray3DController != null)
                pieceTray3DController.RefreshAfterRestore();
            else if (scrollController != null)
                scrollController.RefreshTrayAfterRestore();
            
            // Update HUD if needed
            OnPieceSnapped?.Invoke(snappedCount, totalPieces);

            // Mark already-completed rows/cols/edges so we don't celebrate them on next placement
            RefreshCompletedMilestones();
        }

        /// <summary>
        /// Scans current board state and marks completed rows/cols/edges as celebrated.
        /// Call after RestoreBoardState so we don't fire milestones for pre-existing completions.
        /// </summary>
        private void RefreshCompletedMilestones()
        {
            if (slots.Count == 0) return;

            int maxRow = 0, maxCol = 0;
            foreach (var s in slots)
            {
                if (s.row > maxRow) maxRow = s.row;
                if (s.col > maxCol) maxCol = s.col;
            }

            for (int r = 0; r <= maxRow; r++)
            {
                if (IsRowComplete(r, maxCol)) completedRows.Add(r);
            }
            for (int c = 0; c <= maxCol; c++)
            {
                if (IsColComplete(c, maxRow)) completedCols.Add(c);
            }
            if (IsEdgeComplete(0, maxCol, true)) completedEdges.Add(0);   // top
            if (IsEdgeComplete(maxRow, maxCol, true)) completedEdges.Add(1); // bottom
            if (IsEdgeComplete(0, maxRow, false)) completedEdges.Add(2);    // left
            if (IsEdgeComplete(maxCol, maxRow, false)) completedEdges.Add(3); // right
            if (completedEdges.Count >= 4) allEdgesCelebrated = true;
        }

        /// <summary>
        /// Checks if a milestone was just completed by this placement. Returns (type, edgeCount) or null.
        /// Deduplicates: row+col = one event, multiple edges = one event.
        /// </summary>
        private (ArtUnbound.UI.MilestoneType type, int edgeCount)? TryGetMilestoneForPlacement(int col, int row, Vector3 piecePosition)
        {
            if (slots.Count == 0) return null;

            int maxRow = 0, maxCol = 0;
            foreach (var s in slots)
            {
                if (s.row > maxRow) maxRow = s.row;
                if (s.col > maxCol) maxCol = s.col;
            }

            var newEdges = new List<int>();
            if (!completedEdges.Contains(0) && IsEdgeComplete(0, maxCol, true)) newEdges.Add(0);
            if (!completedEdges.Contains(1) && IsEdgeComplete(maxRow, maxCol, true)) newEdges.Add(1);
            if (!completedEdges.Contains(2) && IsEdgeComplete(0, maxRow, false)) newEdges.Add(2);
            if (!completedEdges.Contains(3) && IsEdgeComplete(maxCol, maxRow, false)) newEdges.Add(3);

            // Edge takes priority: if any edge completes, show "Edge complete" and stop.
            // Corner piece (2 edges) = still just "Edge complete".
            if (newEdges.Count > 0)
            {
                foreach (int e in newEdges) completedEdges.Add(e);
                foreach (int e in newEdges)
                {
                    if (e == 0) completedRows.Add(0);
                    else if (e == 1) completedRows.Add(maxRow);
                    else if (e == 2) completedCols.Add(0);
                    else if (e == 3) completedCols.Add(maxCol);
                }
                bool allEdgesJustCompleted = !allEdgesCelebrated && completedEdges.Count >= 4;
                if (allEdgesJustCompleted) { allEdgesCelebrated = true; return (ArtUnbound.UI.MilestoneType.AllEdges, 0); }
                return (ArtUnbound.UI.MilestoneType.Edge, 0);
            }

            // No edges: check row/column only (interior rows/cols, not edges).
            bool rowJustCompleted = !completedRows.Contains(row) && IsRowComplete(row, maxCol);
            bool colJustCompleted = !completedCols.Contains(col) && IsColComplete(col, maxRow);

            if (rowJustCompleted) completedRows.Add(row);
            if (colJustCompleted) completedCols.Add(col);

            if (rowJustCompleted && colJustCompleted) return (ArtUnbound.UI.MilestoneType.RowAndColumn, 0);
            if (rowJustCompleted) return (ArtUnbound.UI.MilestoneType.Row, 0);
            if (colJustCompleted) return (ArtUnbound.UI.MilestoneType.Column, 0);
            return null;
        }

        private bool IsRowComplete(int row, int maxCol)
        {
            for (int c = 0; c <= maxCol; c++)
            {
                bool filled = false;
                for (int i = 0; i < slots.Count; i++)
                {
                    if (slots[i].row == row && slots[i].col == c && placedBySlot.ContainsKey(i)) { filled = true; break; }
                }
                if (!filled) return false;
            }
            return true;
        }

        private bool IsColComplete(int col, int maxRow)
        {
            for (int r = 0; r <= maxRow; r++)
            {
                bool filled = false;
                for (int i = 0; i < slots.Count; i++)
                {
                    if (slots[i].col == col && slots[i].row == r && placedBySlot.ContainsKey(i)) { filled = true; break; }
                }
                if (!filled) return false;
            }
            return true;
        }

        /// <param name="fixedVal">row index if isRowEdge, else col index</param>
        /// <param name="maxOther">max col if isRowEdge, else max row</param>
        /// <param name="isRowEdge">true = top/bottom edge (fixed row), false = left/right (fixed col)</param>
        private bool IsEdgeComplete(int fixedVal, int maxOther, bool isRowEdge)
        {
            for (int k = 0; k <= maxOther; k++)
            {
                int r = isRowEdge ? fixedVal : k;
                int c = isRowEdge ? k : fixedVal;
                bool filled = false;
                for (int i = 0; i < slots.Count; i++)
                {
                    if (slots[i].row == r && slots[i].col == c && placedBySlot.ContainsKey(i)) { filled = true; break; }
                }
                if (!filled) return false;
            }
            return true;
        }

        /// <summary>
        /// Plays milestone feedback (particles, sound, floating text) if row/column/edge was just completed.
        /// </summary>
        private void TryPlayMilestoneFeedback(int col, int row, Vector3 piecePosition)
        {
            var milestone = TryGetMilestoneForPlacement(col, row, piecePosition);
            if (!milestone.HasValue) return;

            var (type, edgeCount) = milestone.Value;
            if (ArtUnbound.Feedback.PieceEffectsManager.Instance != null)
            {
                ArtUnbound.Feedback.PieceEffectsManager.Instance.PlayMilestoneEffect(piecePosition, "");
            }
            if (ArtUnbound.Feedback.AudioManager.Instance != null)
            {
                ArtUnbound.Feedback.AudioManager.Instance.PlayMilestone();
            }
            OnMilestoneAchieved?.Invoke(type, edgeCount);
        }

        /// <summary>
        /// Notifies listeners that the board state has changed.
        /// Used to trigger auto-save.
        /// </summary>
        private void NotifyBoardStateChanged()
        {
            OnBoardStateChanged?.Invoke();
        }

        /// <summary>
        /// Returns the board dimensions in meters. Valid after a puzzle is initialized.
        /// </summary>
        public void GetBoardDimensions(out float width, out float height)
        {
            width = boardWidthM;
            height = boardHeightM;
        }

        /// <summary>
        /// Hides the scroll buttons (called when puzzle is complete)
        /// </summary>
        public void HideScrollButtons()
        {
            if (pieceTray3DController != null)
            {
                pieceTray3DController.HideScrollButtons();
                return;
            }
            if (scrollController != null)
            {
                scrollController.HideScrollButtons();
            }
        }

        /// <summary>
        /// Tiling/offset for the <b>completed-puzzle</b> full-image quad only (<see cref="ShowFullImageReveal"/>).
        /// Quest/Android standalone often needs a different V term than Editor/Meta Link; do not use for tray pieces or other UI.
        /// </summary>
        private static void ApplyCompletedFullImageTextureMapping(Material mat)
        {
            if (mat == null) return;

            // The FullImageReveal quad uses localRotation (0, 180, 0).
            // slotRoot world Y ≈ 180°, so the combined world rotation is ~0° (Identity).
            // The user therefore sees the FRONT face of the quad — no UV mirroring occurs.
            // Use standard tiling (1,1) with no offset; no X flip needed.
            if (mat.HasProperty("_BaseMap_ST"))
                mat.SetVector("_BaseMap_ST", new Vector4(1f, 1f, 0f, 0f));
            if (mat.HasProperty("_MainTex_ST"))
                mat.SetVector("_MainTex_ST", new Vector4(1f, 1f, 0f, 0f));
            if (!mat.HasProperty("_BaseMap_ST") && !mat.HasProperty("_MainTex_ST"))
            {
                mat.mainTextureScale = new Vector2(1f, 1f);
                mat.mainTextureOffset = new Vector2(0f, 0f);
            }
        }

        /// <summary>
        /// Replaces the assembled pieces with the full artwork image and plays a reveal animation.
        /// Creates a 3D frame around the image with the correct material for the earned tier.
        /// </summary>
        /// <param name="frameTier">The frame tier earned (Bronce, Plata, Oro).</param>
        public void ShowFullImageReveal(FrameTier frameTier = FrameTier.Bronce)
        {
            if (slotRoot == null || currentTexture == null)
            {
                Debug.LogWarning($"[PuzzleBoard] ShowFullImageReveal skipped: slotRoot={slotRoot != null}, texture={currentTexture != null}");
                return;
            }

            // Clear any lingering slot-highlight so it doesn't render over the revealed image.
            ClearSlotHighlight();

            // Remove previous reveal if any
            if (fullImageReveal != null)
            {
                Destroy(fullImageReveal);
                fullImageReveal = null;
            }
            if (fullImageRevealFrame != null)
            {
                Destroy(fullImageRevealFrame);
                fullImageRevealFrame = null;
            }

            // Hide all pieces and grid lines
            foreach (var piece in activePieces)
            {
                if (piece != null) piece.gameObject.SetActive(false);
            }
            Transform gridLines = slotRoot.Find("GridLines");
            if (gridLines != null) gridLines.gameObject.SetActive(false);

            // Create full-image quad. Z=0.0155: in front of pieces towards user.
            // Rotation 180° Y to face user; 180° Z to correct upside-down texture.
            fullImageReveal = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fullImageReveal.name = "FullImageReveal";
            Destroy(fullImageReveal.GetComponent<Collider>());

            fullImageReveal.transform.SetParent(slotRoot, false);
            fullImageReveal.transform.localPosition = new Vector3(0, 0, 0.0155f);
            fullImageReveal.transform.localRotation = Quaternion.Euler(0, 180, 0);
            fullImageReveal.transform.localScale = new Vector3(boardWidthM, boardHeightM, 1f);

            var renderer = fullImageReveal.GetComponent<Renderer>();
            if (renderer != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Texture");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    Material mat = new Material(shader);
                    if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", currentTexture);
                    if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", currentTexture);
                    ApplyCompletedFullImageTextureMapping(mat);
                    renderer.material = mat;
                }
            }

            // Create 3D frame around the image
            CreateFrameAroundImage(frameTier);

            StartCoroutine(AnimateFullImageReveal());
        }

        /// <summary>
        /// Creates a 3D frame (4 extruded bars) around the full image with the correct material.
        /// Each bar is a cuboid with depth for a real picture-frame look.
        /// </summary>
        private void CreateFrameAroundImage(FrameTier frameTier)
        {
            Material frameMat = GetFrameMaterial(frameTier);
            if (frameMat == null) return;

            const float thickness = 0.02f; // 2cm frame thickness (visible width)
            const float depth = 0.02f;      // 2cm frame depth (extrusion)
            float w = boardWidthM;
            float h = boardHeightM;

            fullImageRevealFrame = new GameObject("FullImageRevealFrame");
            fullImageRevealFrame.transform.SetParent(slotRoot, false);
            fullImageRevealFrame.transform.localPosition = new Vector3(0, 0, 0.015f); // Align with image plane
            fullImageRevealFrame.transform.localRotation = Quaternion.Euler(0, 180, 0);
            fullImageRevealFrame.transform.localScale = Vector3.one;

            // Top, Bottom, Left, Right bars - each is an elongated cube (cuboid)
            CreateFrameBar("FrameTop", fullImageRevealFrame.transform, w + thickness * 2f, thickness, depth, 0f, h / 2f + thickness / 2f, frameMat);
            CreateFrameBar("FrameBottom", fullImageRevealFrame.transform, w + thickness * 2f, thickness, depth, 0f, -h / 2f - thickness / 2f, frameMat);
            CreateFrameBar("FrameLeft", fullImageRevealFrame.transform, thickness, h, depth, -w / 2f - thickness / 2f, 0f, frameMat);
            CreateFrameBar("FrameRight", fullImageRevealFrame.transform, thickness, h, depth, w / 2f + thickness / 2f, 0f, frameMat);
        }

        private void CreateFrameBar(string name, Transform parent, float width, float height, float depth, float localX, float localY, Material mat)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            Destroy(cube.GetComponent<Collider>());
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = new Vector3(localX, localY, 0f);
            cube.transform.localRotation = Quaternion.identity;
            cube.transform.localScale = new Vector3(width, height, depth);
            var r = cube.GetComponent<Renderer>();
            if (r != null && mat != null)
                r.sharedMaterial = mat;
        }

        private Material GetFrameMaterial(FrameTier tier)
        {
            var config = frameConfigSet?.GetConfig(tier);
            if (config?.frameMaterial != null)
                return config.frameMaterial;

            Material mat = tier switch
            {
                FrameTier.Bronce => frameBronceMaterial,
                FrameTier.Plata  => framePlataMaterial,
                FrameTier.Oro    => frameOroMaterial,
                _                => frameBronceMaterial
            };

            if (mat != null) return mat;

            // Fallback: procedural material so the frame always renders even if Inspector slots are empty.
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) return null;

            var fallback = new Material(shader);
            fallback.color = tier switch
            {
                FrameTier.Bronce => new Color(0.55f, 0.33f, 0.12f), // bronze brown
                FrameTier.Plata  => new Color(0.75f, 0.75f, 0.78f), // silver
                FrameTier.Oro    => new Color(0.85f, 0.70f, 0.10f), // gold
                _                => new Color(0.55f, 0.33f, 0.12f)
            };
            return fallback;
        }

        private IEnumerator AnimateFullImageReveal()
        {
            if (fullImageReveal == null) yield break;

            float duration = 0.5f;
            float elapsed = 0f;
            Vector3 startScale = new Vector3(boardWidthM * 0.92f, boardHeightM * 0.92f, 1f);
            Vector3 endScale = new Vector3(boardWidthM, boardHeightM, 1f);

            fullImageReveal.transform.localScale = startScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = 1f - (1f - t) * (1f - t); // Ease out quad
                fullImageReveal.transform.localScale = Vector3.Lerp(startScale, endScale, t);
                yield return null;
            }

            fullImageReveal.transform.localScale = endScale;
        }

        /// <summary>
        /// Public method to check current puzzle status (can be called from Inspector or other scripts)
        /// </summary>
        [ContextMenu("Debug: Log Piece Placement Status")]
        public void DebugLogPiecePlacement()
        {
            LogPiecePlacementStatus();
        }

        /// <summary>
        /// Logs detailed information about piece placement for debugging
        /// </summary>
        private void LogPiecePlacementStatus()
        {
            
            int totalPlaced = 0;
            int correctlyPlaced = 0;
            int incorrectlyPlaced = 0;
            
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (placedBySlot.TryGetValue(i, out PuzzlePiece piece))
                {
                    totalPlaced++;
                    bool isCorrect = piece.PieceId == slot.pieceId;
                    
                    if (isCorrect)
                    {
                        correctlyPlaced++;
                    }
                    else
                    {
                        incorrectlyPlaced++;
                        Debug.LogWarning($"❌ Slot {i} (Row {slot.row}, Col {slot.col}): Has Piece {piece.PieceId} but expects Piece {slot.pieceId}");
                    }
                }
                else
                {
                    Debug.LogWarning($"⚠️ Slot {i} (Row {slot.row}, Col {slot.col}): EMPTY");
                }
            }
            
        }

        #endregion

        #region Artwork Hanging Support

        /// <summary>
        /// Gets the transform of the completed frame (for hanging on walls).
        /// Returns the SlotRoot which contains both the FullImageReveal and FullImageRevealFrame.
        /// </summary>
        public Transform GetCompletedFrameTransform()
        {
            // Return SlotRoot which contains the entire completed artwork
            // (FullImageReveal + FullImageRevealFrame + frame bars)
            return slotRoot;
        }

        /// <summary>
        /// Enables or disables frame interaction (makes it grabbable).
        /// </summary>
        public void EnableFrameInteraction(bool enable)
        {
            Transform frame = GetCompletedFrameTransform();
            if (frame == null) return;

            // Add or remove collider for pinch detection
            var collider = frame.GetComponent<BoxCollider>();
            if (enable && collider == null)
            {
                collider = frame.gameObject.AddComponent<BoxCollider>();
                // Approximate frame size based on board dimensions
                collider.size = new Vector3(boardWidthM, boardHeightM, 0.05f);
                Debug.Log($"[PuzzleBoard] Added BoxCollider to frame: size={collider.size}");
            }
            else if (!enable && collider != null)
            {
                Destroy(collider);
            }

            if (enable)
            {
                Debug.Log("[PuzzleBoard] Frame interaction enabled - frame is now grabbable");
            }
        }

        #endregion
    }
}
