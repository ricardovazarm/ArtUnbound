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

        private void Start()
        {
            if (inputController != null)
            {
                inputController.OnSwipeHorizontal += OnSwipeInput;
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
                return;
            }

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
                return false;
            }

            float maxDistance = puzzleConfig != null ? puzzleConfig.snapDistanceCm * 0.01f : 0.03f;
            int bestIndex = -1;
            float bestDistance = float.MaxValue;

            if (IsDefaultMorphology(piece.Morphology))
            {
                piece.ApplyMorphology(GetMorphologyForPieceId(piece.PieceId));
            }

            for (int i = 0; i < slots.Count; i++)
            {
                if (placedBySlot.ContainsKey(i))
                {
                    continue;
                }

                float distance = Vector3.Distance(piece.transform.position, slots[i].position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }

            if (bestIndex < 0)
            {
                return false;
            }

            if (bestDistance <= maxDistance)
            {
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

                return true;
            }

            return false;
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
                        0f
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
                        0f
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

            // Shuffle and populate scroll
            InitializeScroll();
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

        private void CreatePiece(int id, int col, int row, int gridSize, PieceMorphology morphology, float pieceSize, Texture2D texture)
        {
            if (piecePrefab == null) return;

            PuzzlePiece piece = Instantiate(piecePrefab, slotRoot.position, Quaternion.identity); // Spawn at root mostly hidden, will be moved by Scroll
            piece.Initialize(id, GetSlotIndex(row, col), morphology);
            piece.name = $"Piece_{id}_{col}_{row}";

            // Generate Mesh
            Mesh mesh = PieceMeshGenerator.GeneratePieceMesh(morphology, pieceSize, col, row, gridSize, gridSize);
            
            // Assign Mesh
            var meshFilter = piece.GetComponentInChildren<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.mesh = mesh;
            }

            var meshRenderer = piece.GetComponentInChildren<MeshRenderer>();
            if (meshRenderer != null && texture != null)
            {
                meshRenderer.material.mainTexture = texture;
                // Important: Ensure shader handles transparency if using PNGs
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
            if (scrollController == null) return;

            // Shuffle active pieces for initial display
            var shuffledPieces = PieceShuffler.GetShuffledCopy(activePieces);
            
            List<Transform> pieceTransforms = new List<Transform>();
            foreach(var p in shuffledPieces)
            {
                pieceTransforms.Add(p.transform);
                // Reset piece state to InPool or similar handled by scroll controller logic
                p.ReturnToPool(Vector3.zero); // Position will be managed by scroll
            }

            scrollController.Initialize(pieceTransforms);
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
