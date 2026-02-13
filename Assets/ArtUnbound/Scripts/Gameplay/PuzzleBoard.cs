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

            // TEMPORARY FIX: Force PieceTray position every frame
            if (scrollController != null)
            {
                if (Vector3.Distance(scrollController.transform.localPosition, new Vector3(0, -0.4f, 0)) > 0.01f)
                {
                    if (Time.frameCount % 60 == 0) // Log once per second approx
                    {
                        Debug.LogWarning($"[PuzzleBoard] PieceTray drifted to {scrollController.transform.localPosition}. Forcing back to (0, -0.4, 0).");
                    }
                    scrollController.transform.localPosition = new Vector3(0, -0.4f, 0);
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
            float margin = 0.02f; // 2cm margin
            float thickness = 0.01f; // 1cm thickness
            boardViz.transform.localScale = new Vector3(width + margin * 2, height + margin * 2, thickness);

            // Position behind pieces (Z+ is forward, if pieces are at 0, board should be at +thickness/2 or -thickness/2? 
            // If user looks at -Z, pieces are at Z=0. Board should be further away (more negative? or positive?)
            // If pieces face -Z (towards user), board should be at Z > 0 (behind pieces).
            // Actually, let's just put it at Z = 0.01f (behind pieces if camera is at -Z)
            boardViz.transform.localPosition = new Vector3(0, 0, 0.01f);

            // Set Material Color (Dark semi-transparent or wood)
            var renderer = boardViz.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.2f, 0.2f, 0.2f, 1f); // Dark Grey
                // Force shader to standard if possible, but default primitive has default material.
            }

            // Remove collider if it interferes with raycast (though we might want it for placement later)
            // For now, remove to avoid blocking piece interaction if it's in front
            Destroy(boardViz.GetComponent<Collider>());
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

            // Always enforce position for PieceTray/Scroll to ensure visibility
            // This fixes issue where tray might be at strange coordinates from prefab/scene layout
            scrollController.transform.localPosition = new Vector3(0, -0.4f, 0);
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
