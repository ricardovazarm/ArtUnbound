using System.Collections.Generic;
using ArtUnbound.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Manages the piece thumbnail panel that lets players grab puzzle pieces.
    ///
    /// Uses Unity ScrollRect + GridLayoutGroup — no manual pagination.
    ///
    /// At runtime only the cell size is calculated (from pieceSizeM in world units).
    /// Spacing, padding and constraint are configured in the Inspector so the
    /// GridLayoutGroup auto-fits as many columns as fit in the panel width.
    ///
    ///   Easy   ( 64 pcs, large pieces)  → fewer, larger thumbnails per row
    ///   Normal (144 pcs, medium pieces) → medium thumbnails per row
    ///   Hard   (256 pcs, small pieces)  → more, smaller thumbnails per row
    ///   Expert (512 pcs, tiny pieces)   → many small thumbnails per row
    ///
    /// Inspector requirements on the Content GridLayoutGroup:
    ///   Constraint      = Flexible
    ///   Start Axis      = Horizontal
    ///   Spacing X/Y     = set freely (e.g. 10–20 px for ~1–2 cm gaps)
    ///   Padding         = set freely
    ///
    /// See docs/Setup-PieceTray-ScrollView.md for full Unity Editor setup.
    /// </summary>
    public class PieceTrayGridController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Content transform inside the ScrollRect — must have GridLayoutGroup + ContentSizeFitter.")]
        [SerializeField] private Transform contentRoot;

        [Tooltip("Prefab: Button + Image + PieceThumbnailItem (no BoxCollider).")]
        [SerializeField] private GameObject thumbnailPrefab;

        // ── Runtime ─────────────────────────────────────────────────────────────
        private readonly List<PieceThumbnailItem> _items = new List<PieceThumbnailItem>();

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Instantiates one thumbnail per piece and sets the cell size from pieceSizeM.
        /// Spacing, padding and constraint are read from the Inspector — no overrides.
        /// Called by PuzzleBoard after all pieces are created and shuffled.
        /// </summary>
        public void Initialize(IList<PuzzlePiece> pieces,
                               Texture2D artworkTexture,
                               int gridCols, int gridRows,
                               float pieceSizeM)
        {
            foreach (var old in _items)
                if (old != null) Destroy(old.gameObject);
            _items.Clear();

            if (contentRoot == null)
            {
                Debug.LogError("[PieceTrayGridController] contentRoot is not assigned.");
                return;
            }
            if (thumbnailPrefab == null)
            {
                Debug.LogError("[PieceTrayGridController] thumbnailPrefab is not assigned.");
                return;
            }

            // Activate BEFORE layout so the ScrollRect viewport has a valid rect.
            gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();

            ApplyGridLayout(pieceSizeM);

            foreach (var piece in pieces)
            {
                if (piece == null) continue;

                int col = piece.PieceId % gridCols;
                int row = piece.PieceId / gridCols;

                var go  = Instantiate(thumbnailPrefab, contentRoot);
                go.name = $"Thumb_{piece.PieceId}";

                var item = go.GetComponent<PieceThumbnailItem>();
                if (item == null)
                    item = go.AddComponent<PieceThumbnailItem>();

                item.Initialize(piece, artworkTexture, col, row, gridCols, gridRows, pieceSizeM);
                _items.Add(item);

                piece.SetThumbnailItem(item);
            }

        }

        /// <summary>Permanently removes a thumbnail (piece correctly placed on board).</summary>
        public void RemoveThumbnail(int pieceId)
        {
            int idx = FindIndex(pieceId);
            if (idx < 0) return;
            Destroy(_items[idx].gameObject);
            _items.RemoveAt(idx);
        }

        /// <summary>
        /// Moves a thumbnail to the last position in the grid.
        /// Call BEFORE the piece's state changes to InPool.
        /// </summary>
        public void MoveToEnd(int pieceId)
        {
            int idx = FindIndex(pieceId);
            if (idx < 0) return;
            var item = _items[idx];
            _items.RemoveAt(idx);
            _items.Add(item);
            item.transform.SetAsLastSibling();
        }

        // ── Panel visibility ─────────────────────────────────────────────────────

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);

        // ── Private helpers ──────────────────────────────────────────────────────

        /// <summary>
        /// Sets GridLayoutGroup.cellSize from the puzzle's piece size in world units.
        /// Spacing, padding and constraint stay as configured in the Inspector.
        /// GridLayoutGroup.Constraint.Flexible + StartAxis.Horizontal will auto-fit
        /// as many columns as the panel width allows.
        /// </summary>
        private void ApplyGridLayout(float pieceSizeM)
        {
            var grid = contentRoot.GetComponent<GridLayoutGroup>();
            if (grid == null)
            {
                Debug.LogWarning("[PieceTrayGridController] No GridLayoutGroup on contentRoot.");
                return;
            }

            // lossyScale = world-units-per-canvas-pixel.
            // Use X and Y independently so cells appear square even if the canvas
            // has non-uniform scale (e.g. a tilted World Space panel on Quest).
            var canvas = GetComponentInParent<Canvas>();
            float worldPerPxX = (canvas != null && canvas.transform.lossyScale.x > 0f)
                ? canvas.transform.lossyScale.x : 0.001f;
            float worldPerPxY = (canvas != null && canvas.transform.lossyScale.y > 0f)
                ? canvas.transform.lossyScale.y : 0.001f;

            // The 3-D piece bounding box is 1.3× the nominal piece size because
            // triangular tabs extend TAB_HEIGHT (15%) beyond the square core on each side.
            const float pieceWorldScale = 1f + 2f * 0.15f; // 1.3
            float cellW = Mathf.Round(pieceSizeM * pieceWorldScale / worldPerPxX);
            float cellH = Mathf.Round(pieceSizeM * pieceWorldScale / worldPerPxY);

            grid.cellSize = new Vector2(cellW, cellH);

        }

        private int FindIndex(int pieceId) =>
            _items.FindIndex(t => t != null
                                  && t.LinkedPiece != null
                                  && t.LinkedPiece.PieceId == pieceId);
    }
}
