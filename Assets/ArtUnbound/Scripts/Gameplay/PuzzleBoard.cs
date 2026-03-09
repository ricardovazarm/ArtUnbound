using System;
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

        [SerializeField] private Transform slotRoot;
        [SerializeField] private PuzzlePiece piecePrefab;
        [SerializeField] private ArtUnbound.UI.PieceScrollController scrollController;
        [SerializeField] private ArtUnbound.Input.HandTrackingInputController inputController;
        [SerializeField] private PuzzleConfig puzzleConfig;
        [SerializeField] private bool helpModeEnabled = true;
        [SerializeField] private HelpModeController helpModeController;
        [SerializeField] private Color errorGlowColor = new Color(1f, 0.2f, 0.2f, 0.8f);

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
                else
                {
                    Debug.Log("[PuzzleBoard] HandTrackingInputController found via FindFirstObjectByType.");
                }
            }
            else
            {
                Debug.Log("[PuzzleBoard] HandTrackingInputController assigned via Inspector.");
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
        private Texture2D currentTexture;
        private Vector3 lastPos;

        private void OnEnable()
        {
            Debug.Log("[PuzzleBoard] OnEnable called. GameObject is active.");
        }

        private void Update()
        {
            if (Vector3.Distance(transform.position, lastPos) > 0.01f)
            {
                Debug.Log($"[PuzzleBoard] Moved from {lastPos} to {transform.position}");
                lastPos = transform.position;
            }

            // VERTICAL TRAY: Force PieceTray position to the RIGHT of the board (player's right)
            // INVERTED: Canvas is rotated 180°, so player's right = negative X
            // Distance: 0.45m (45cm to player's right) to accommodate 5-column grid
            if (scrollController != null)
            {
                Vector3 targetPos = new Vector3(-0.45f, 0f, 0f); // Player's right side, same height as board center
                if (Vector3.Distance(scrollController.transform.localPosition, targetPos) > 0.01f)
                {
                    if (Time.frameCount % 60 == 0) // Log once per second approx
                    {
                        Debug.LogWarning($"[PuzzleBoard] PieceTray drifted to {scrollController.transform.localPosition}. Forcing back to (-0.45, 0, 0).");
                    }
                    scrollController.transform.localPosition = targetPos;
                    scrollController.transform.localRotation = Quaternion.identity;
                    scrollController.transform.localScale = Vector3.one;
                }
            }
        }

        /// <summary>
        /// Initializes the puzzle board with piece count and artwork texture.
        /// </summary>
        public void Initialize(int pieceCount, Texture2D artworkTexture)
        {
            snappedCount = 0;
            totalPieces = pieceCount;
            currentTexture = artworkTexture;

            // Clear existing pieces
            foreach (var piece in activePieces)
            {
                if (piece != null) Destroy(piece.gameObject);
            }
            activePieces.Clear();

            slots.Clear();
            morphologyByPieceId.Clear();
            placedBySlot.Clear();

            if (slotRoot == null || pieceCount <= 0)
            {
                Debug.LogError($"[PuzzleBoard] Initialize failed. SlotRoot: {slotRoot}, PieceCount: {pieceCount}");
                return;
            }

            Debug.Log($"[PuzzleBoard] Initializing with PieceCount: {pieceCount}, Texture: {artworkTexture?.name} ({artworkTexture?.width}x{artworkTexture?.height})");
            CreateSlotsFromCount(pieceCount);
        }

        /// <summary>
        /// Initializes the puzzle board with artwork definition.
        /// </summary>
        public void Initialize(ArtworkDefinition definition, int pieceCount)
        {
            snappedCount = 0;

            // Clear existing pieces
            foreach (var piece in activePieces)
            {
                if (piece != null) Destroy(piece.gameObject);
            }
            activePieces.Clear();

            slots.Clear();
            morphologyByPieceId.Clear();
            placedBySlot.Clear();

            if (definition == null || slotRoot == null || pieceCount <= 0)
            {
                return;
            }

            CreateSlots(definition, pieceCount);
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

            Debug.Log($"[PuzzleBoard] TrySnapPiece called for piece {piece.name} at position {piece.transform.position}");

            // NEW APPROACH: Check distance to board plane (not individual slots)
            // The board plane is defined by the board's transform
            Vector3 boardNormal = transform.forward; // Board faces forward (towards user when rotated)
            Vector3 boardPosition = transform.position; // Center of board
            
            // Calculate distance from piece to board plane
            Vector3 pieceToBoard = piece.transform.position - boardPosition;
            float distanceToPlane = Mathf.Abs(Vector3.Dot(pieceToBoard, boardNormal));
            
            Debug.Log($"[PuzzleBoard] Piece distance to board plane: {distanceToPlane:F3}m (Board pos: {boardPosition}, Normal: {boardNormal})");
            Debug.Log($"[PuzzleBoard] Total slots: {slots.Count}, Placed pieces: {placedBySlot.Count}");

            // STEP 1: Check if piece is close enough to the board plane (within 2cm)
            // This is the ONLY requirement to trigger snap
            float maxDepthToPlane = 0.02f; // 2cm perpendicular distance to board plane
            if (distanceToPlane > maxDepthToPlane)
            {
                Debug.LogWarning($"[PuzzleBoard] ✗ Piece too far from board plane: {distanceToPlane:F3}m > {maxDepthToPlane:F3}m");
                return false;
            }

            Debug.Log($"[PuzzleBoard] ✓ Piece is within snap depth ({distanceToPlane:F3}m <= {maxDepthToPlane:F3}m). Finding closest slot...");

            if (IsDefaultMorphology(piece.Morphology))
            {
                piece.ApplyMorphology(GetMorphologyForPieceId(piece.PieceId));
            }

            // STEP 2: Project piece position onto board plane
            Vector3 projectedPiecePos = piece.transform.position - boardNormal * Vector3.Dot(pieceToBoard, boardNormal);
            
            Debug.Log($"[PuzzleBoard] Piece projected onto board plane: {projectedPiecePos}");

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

            Debug.Log($"[PuzzleBoard] TrySnapPiece: piecePos={piece.transform.position}, bestSlot={bestIndex}, dist={bestPlanarDist:F3}m");

            if (bestIndex < 0)
            {
                Debug.LogWarning("[PuzzleBoard] ✗ No valid slot found (all occupied)!");
                return false;
            }

            // STEP 4: Snap to the closest slot (no planar distance check)
            Debug.Log($"[PuzzleBoard] SUCCESS! Snapping piece to slot {bestIndex}");
            
            if (puzzleConfig == null || puzzleConfig.useGridSnapping)
            {
                piece.SetSnapped(slots[bestIndex].position, slots[bestIndex].rotation);
            }

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

            if (snappedCount >= slots.Count)
            {
                OnCompleted?.Invoke();
                OnPuzzleComplete?.Invoke();
            }

            Debug.Log($"[PuzzleBoard] ✓ SNAP SUCCESS! Piece {piece.name} -> Slot {bestIndex} (Planar: {bestPlanarDist:F3}m, Depth to plane: {distanceToPlane:F3}m, Snapped: {snappedCount}/{slots.Count})");
            
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

            float maxDepthToPlane = 0.02f; // 2cm
            if (distanceToPlane > maxDepthToPlane)
            {
                ClearSlotHighlight();
                return -1;
            }

            // Find closest slot and track top 5 for debugging
            int bestIndex = -1;
            float bestDist = float.MaxValue;
            var nearbySlots = new System.Collections.Generic.List<(int index, float dist)>();

            for (int i = 0; i < slots.Count; i++)
            {
                if (placedBySlot.ContainsKey(i)) continue;

                float dist = Vector3.Distance(piecePosition, slots[i].position);
                nearbySlots.Add((i, dist));
                
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestIndex = i;
                }
            }

            if (bestIndex >= 0 && bestIndex != currentHighlightedSlot)
            {
                HighlightSlot(bestIndex);
            }
            
            return bestIndex;
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

            Debug.Log($"[PuzzleBoard] SnapPieceToSlot: Snapping piece {piece.name} to slot {slotIndex}");

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

            Debug.Log($"🔊 AUDIO DEBUG | PieceId={piece.PieceId} | SlotPieceId={slots[slotIndex].pieceId} | isCorrect={isCorrectSlot} | morphologyMatches={morphologyMatches}");

            // Play sound based ONLY on correct slot placement (ignore morphology for audio)
            if (isCorrectSlot)
            {
                Debug.Log("🎵 Playing CORRECT sound (PlayPieceSnap)");
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
                Debug.Log("🎵 Playing INCORRECT sound (PlayPieceIncorrect)");
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
            Debug.Log($"[PuzzleBoard] {correctnessMsg} | Piece {piece.name} (ID:{piece.PieceId}) -> Slot {slotIndex} (Expected:{slots[slotIndex].pieceId}) | Progress: {snappedCount}/{slots.Count} correct");
            
            return true;
        }

        /// <summary>
        /// Removes a piece from its current slot on the board.
        /// Used when the player picks up a placed piece to move it.
        /// </summary>
        public void RemovePieceFromSlot(PuzzlePiece piece)
        {
            if (piece == null) return;

            int slotIndex = piece.CurrentSlotIndex;
            if (slotIndex < 0 || slotIndex >= slots.Count)
            {
                Debug.LogWarning($"[PuzzleBoard] Cannot remove piece {piece.name} - invalid slot index {slotIndex}");
                return;
            }

            Debug.Log($"[PuzzleBoard] Removing piece {piece.name} from slot {slotIndex}");

            // Remove from the slot
            if (placedBySlot.ContainsKey(slotIndex))
            {
                placedBySlot.Remove(slotIndex);
                
                // If it was correctly placed, decrement snapped count
                if (slots[slotIndex].pieceId == piece.PieceId)
                {
                    snappedCount--;
                    Debug.Log($"[PuzzleBoard] Decremented snapped count to {snappedCount}/{slots.Count}");
                }
            }

            piece.SetSlotIndex(-1); // Clear slot reference
        }

        /// <summary>
        /// Returns a piece to the beginning of the tray.
        /// Used when a piece is released without placing it on the board.
        /// </summary>
        public void ReturnPieceToTray(PuzzlePiece piece)
        {
            if (piece == null) return;
            if (scrollController == null)
            {
                Debug.LogWarning("[PuzzleBoard] Cannot return piece to tray - scrollController is null");
                return;
            }

            Debug.Log($"[PuzzleBoard] Returning piece {piece.name} to tray (at end)");
            
            // Ask the scroll controller to add the piece at the end
            scrollController.AddPieceAtEnd(piece);
        }

        private void HighlightSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count) return;

            Debug.Log($"[PuzzleBoard] HighlightSlot called for slot {slotIndex}");
            
            currentHighlightedSlot = slotIndex;

            // Create highlight object if it doesn't exist
            if (slotHighlight == null)
            {
                Debug.Log($"[PuzzleBoard] Creating slot highlight object");
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
                
                Debug.Log($"[PuzzleBoard] Highlight created with shader: {highlightShader?.name ?? "NULL"}");
            }

            // Position and scale the highlight in SLOT ROOT local space
            PuzzleSlot slot = slots[slotIndex];
            float pieceSize = puzzleConfig != null ? puzzleConfig.pieceSizeCm * 0.01f : 0.05f;
            
            // Convert slot world position to local position relative to slotRoot
            Vector3 localPos = slotRoot.InverseTransformPoint(slot.position);
            localPos.z = 0.017f; // Slightly in front of grid and numbers for visibility
            
            slotHighlight.transform.SetParent(slotRoot, false);
            slotHighlight.transform.localPosition = localPos;
            slotHighlight.transform.localRotation = Quaternion.Euler(0, 180, 0); // Rotate 180° to face user
            // Make it slightly larger than the piece for better visibility
            slotHighlight.transform.localScale = new Vector3(pieceSize * 1.1f, pieceSize * 1.1f, 1f);
            slotHighlight.SetActive(true);
            
            Debug.Log($"[PuzzleBoard] Highlight positioned at local: {localPos}, world: {slotHighlight.transform.position}, scale {slotHighlight.transform.localScale}, active: {slotHighlight.activeSelf}, renderer enabled: {slotHighlight.GetComponent<Renderer>()?.enabled}");
        }

        public void ClearSlotHighlight()
        {
            if (slotHighlight != null)
            {
                slotHighlight.SetActive(false);
            }
            currentHighlightedSlot = -1;
        }

        private void CreateSlotsFromCount(int pieceCount)
        {
            if (currentTexture == null)
            {
                Debug.LogError("No texture assigned for puzzle generation");
                return;
            }

            CalculateGridDimensions(pieceCount, currentTexture.width, currentTexture.height, out int cols, out int rows);

            float pieceSize = puzzleConfig != null ? puzzleConfig.pieceSizeCm * 0.01f : 0.05f;
            float sizeX = cols * pieceSize;
            float sizeY = rows * pieceSize;
            float cellWidth = pieceSize;
            float cellHeight = pieceSize;

            int id = 0;
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    if (id >= pieceCount)
                    {
                        // Note: With adaptive grid, we might exceed or fall short of exact pieceCount.
                        // We strictly fill the grid we calculated.
                        // To allow the loop to finish the grid, we use 'id' as just a counter,
                        // but we should ensure unique IDs if we go over initial pieceCount?
                        // Actually, better to just let it increment. The target count is soft.
                    }

                    Vector3 localPos = new Vector3(
                        (-sizeX * 0.5f) + cellWidth * 0.5f + cellWidth * x,
                        (sizeY * 0.5f) - cellHeight * 0.5f - cellHeight * y,
                        0.015f // Correct Z for visual placement
                    );

                    PieceMorphology morphology = GenerateMorphology(x, y, cols, rows);

                    PuzzleSlot slot = new PuzzleSlot
                    {
                        pieceId = id,
                        row = y,
                        col = x,
                        position = slotRoot.TransformPoint(localPos),
                        rotation = slotRoot.rotation,
                        morphology = morphology
                    };

                    slots.Add(slot);
                    morphologyByPieceId[id] = morphology;

                    // Create Piece Visual
                    CreatePiece(id, x, y, cols, rows, morphology, pieceSize, currentTexture);

                    id++;
                }
            }

            // Create Visual Board Context
            CreateBoardVisual(cols * pieceSize, rows * pieceSize);

            // Shuffle and populate scroll
            InitializeScroll();
        }

        private void CreateSlots(ArtworkDefinition definition, int pieceCount)
        {
            var textureToUse = definition.puzzleTexture != null ? definition.puzzleTexture : definition.fullImage.texture;
            if (textureToUse == null) return;

            CalculateGridDimensions(pieceCount, textureToUse.width, textureToUse.height, out int cols, out int rows);

            float pieceSize = puzzleConfig != null ? puzzleConfig.pieceSizeCm * 0.01f : 0.05f;
            float sizeX = cols * pieceSize;
            float sizeY = rows * pieceSize;
            float cellWidth = pieceSize;
            float cellHeight = pieceSize;

            int id = 0;
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    Vector3 localPos = new Vector3(
                        (-sizeX * 0.5f) + cellWidth * 0.5f + cellWidth * x,
                        (sizeY * 0.5f) - cellHeight * 0.5f - cellHeight * y,
                        0.015f // Correct Z for visual placement per user
                    );

                    PieceMorphology morphology = GenerateMorphology(x, y, cols, rows);

                    PuzzleSlot slot = new PuzzleSlot
                    {
                        pieceId = id,
                        row = y,
                        col = x,
                        position = slotRoot.TransformPoint(localPos),
                        rotation = slotRoot.rotation,
                        morphology = morphology
                    };

                    slots.Add(slot);
                    morphologyByPieceId[id] = morphology;

                    // Create Piece Visual
                    CreatePiece(id, x, y, cols, rows, morphology, pieceSize, textureToUse);

                    id++;
                }
            }

            // Create Visual Board Context
            CreateBoardVisual(sizeX, sizeY);

            // Shuffle and populate scroll
            InitializeScroll();
        }

        private void CreateBoardVisual(float width, float height)
        {
            if (slotRoot == null) return;

            Debug.Log($"[PuzzleBoard] Creating Board Visual: {width}x{height}");

            // Look for existing board visual
            Transform existing = slotRoot.Find("BoardVisual");
            if (existing != null) Destroy(existing.gameObject);

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

            // Set Material Color (White canvas)
            var renderer = boardViz.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Try different shaders until we find one that works
                Shader boardShader = Shader.Find("Universal Render Pipeline/Lit");
                if (boardShader == null) boardShader = Shader.Find("Standard");
                if (boardShader == null) boardShader = Shader.Find("Diffuse");
                if (boardShader == null) boardShader = Shader.Find("Mobile/Diffuse");
                
                Material boardMat = new Material(boardShader);
                boardMat.color = Color.white; // White canvas like a painting
                renderer.material = boardMat;
                
                Debug.Log($"[PuzzleBoard] Board using shader: {boardShader?.name ?? "NULL (will be PINK!)"}");
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
            // Look for existing grid
            Transform existing = parent.Find("GridLines");
            if (existing != null) Destroy(existing.gameObject);

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

            float pieceSize = puzzleConfig != null ? puzzleConfig.pieceSizeCm * 0.01f : 0.05f;

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

            Debug.Log($"[PuzzleBoard] Created {slots.Count} slot number labels");
        }

        private void CalculateGridDimensions(int targetCount, int texWidth, int texHeight, out int cols, out int rows)
        {
            float ratio = (float)texWidth / texHeight;
            // rows = sqrt(target / ratio)
            rows = Mathf.RoundToInt(Mathf.Sqrt(targetCount / ratio));
            if (rows < 2) rows = 2;
            cols = Mathf.RoundToInt(rows * ratio);
            if (cols < 2) cols = 2;
        }

        private PieceMorphology GenerateMorphology(int col, int row, int numCols, int numRows)
        {
            bool parity = (col + row) % 2 == 0;
            PieceEdgeState innerState = parity ? PieceEdgeState.Positive : PieceEdgeState.Negative;
            PieceMorphology m = new PieceMorphology
            {
                top = row == 0 ? PieceEdgeState.Flat : innerState,
                right = col == numCols - 1 ? PieceEdgeState.Flat : innerState,
                bottom = row == numRows - 1 ? PieceEdgeState.Flat : innerState,
                left = col == 0 ? PieceEdgeState.Flat : innerState
            };

            return m;
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

        private void CreatePiece(int id, int col, int row, int gridCols, int gridRows, PieceMorphology morphology, float pieceSize, Texture2D texture)
        {
            if (piecePrefab == null) return;

            // Instantiate parented to slotRoot so they move with the board
            PuzzlePiece piece = Instantiate(piecePrefab, slotRoot);
            piece.transform.position = slotRoot.position; // Initially at root, scroll will move them
            piece.transform.rotation = Quaternion.identity;

            piece.Initialize(id, GetSlotIndex(row, col), morphology);
            piece.name = $"Piece_{id}_{col}_{row}";

            // Debug.Log($"[PuzzleBoard] Created Piece {id} at {col},{row}. Pos: {piece.transform.position}");

            // Generate Mesh
            Mesh mesh = PieceMeshGenerator.GeneratePieceMesh(morphology, pieceSize, col, row, gridCols, gridRows);

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

            // Add collider for interaction
            var collider = piece.GetComponentInChildren<MeshCollider>();
            if (collider != null)
            {
                collider.sharedMesh = mesh;
            }

            activePieces.Add(piece);
        }

        private void InitializeScroll()
        {
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

            Debug.Log($"[PuzzleBoard] Forced ScrollController position to: {scrollController.transform.localPosition}");

            // Create visual background for the tray
            // REMOVED: User requested to remove the visual tray.
            // CreateTrayVisual(scrollController.transform);

            Debug.Log($"[PuzzleBoard] Forced ScrollController position to: {scrollController.transform.localPosition}");

            // Shuffle active pieces for initial display
            var shuffledPieces = PieceShuffler.GetShuffledCopy(activePieces);

            List<Transform> pieceTransforms = new List<Transform>();
            foreach (var p in shuffledPieces)
            {
                pieceTransforms.Add(p.transform);
                // Fix: Do NOT call ReturnToPool here, as it starts a coroutine that overrides the position
                // set by scrollController.Initialize. Just set the state directly.
                p.SetState(PieceState.InPool);
            }

            Debug.Log($"[PuzzleBoard] Initializing Scroll with {pieceTransforms.Count} pieces.");
            scrollController.Initialize(pieceTransforms);
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
    }
}
