using System.Collections.Generic;
using ArtUnbound.Gameplay;
using UnityEngine;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Manages the piece thumbnail panel that lets players grab puzzle pieces.
    ///
    /// Uses a Unity ScrollRect + GridLayoutGroup for layout and scrolling —
    /// no manual pagination code. Just like NativeGalleryController, this class
    /// only manages the data list; the Canvas/ScrollRect/GridLayoutGroup components
    /// in the scene handle all visual layout, scrolling, and clipping.
    ///
    /// ── Unity Editor setup ──────────────────────────────────────────────────
    ///  1. Create a World Space Canvas (Render Mode: World Space).
    ///     • Add OVRRaycaster (Meta XR SDK) and PointableCanvas so hand-tracking
    ///       works with the ScrollRect's scroll events.
    ///     • Scale: 0.001 (so 1 pixel = 1 mm in world space).
    ///
    ///  2. Canvas hierarchy:
    ///       Canvas
    ///       └─ ScrollRect (component on a child RectTransform)
    ///          ├─ Viewport  (Image + Mask; set as ScrollRect.viewport)
    ///          │   └─ Content (GridLayoutGroup + ContentSizeFitter vertical)
    ///          │       ← assign this as contentRoot below
    ///          └─ Scrollbar Vertical (optional)
    ///
    ///     ScrollRect settings: Vertical = true, Horizontal = false.
    ///     GridLayoutGroup: Constraint = Fixed Column Count = 3,
    ///                      Cell Size = 75×75 px, Spacing = 3×3 px.
    ///     ContentSizeFitter: Vertical Fit = Preferred Size.
    ///
    ///  3. Create the PieceThumbnailButton prefab:
    ///       • RectTransform (size driven by GridLayoutGroup — leave at 0,0 in prefab)
    ///       • RawImage component (texture set at runtime)
    ///       • BoxCollider component (size set at runtime from worldCellSizeM)
    ///       • PieceThumbnailItem script
    ///       • Layer: same as the puzzle pieces' interactable layer
    ///     Assign it to thumbnailPrefab below.
    ///
    ///  4. Add PieceTrayGridController to the Canvas root (or any convenient parent).
    ///     Assign contentRoot = Content transform, thumbnailPrefab = the prefab above.
    ///
    ///  5. Assign this controller to PuzzleBoard.pieceTrayController in the Inspector.
    ///  6. worldCellSizeM must equal canvasScale × gridLayoutCellSizePx
    ///     (e.g. 0.001 × 75 = 0.075 m).
    /// </summary>
    public class PieceTrayGridController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Content transform inside the ScrollRect — must have a GridLayoutGroup " +
                 "and a ContentSizeFitter (Vertical Fit = Preferred Size).")]
        [SerializeField] private Transform contentRoot;

        [Tooltip("Prefab with RawImage + BoxCollider + PieceThumbnailItem. " +
                 "RectTransform size is controlled by GridLayoutGroup.")]
        [SerializeField] private GameObject thumbnailPrefab;

        [Header("Sizing")]
        [Tooltip("World-space cell size in metres. " +
                 "Must match canvasScale × GridLayoutGroup cell size in pixels.\n" +
                 "Default: 0.001 canvas scale × 75 px cell = 0.075 m.")]
        [SerializeField] private float worldCellSizeM = 0.075f;

        // ── Runtime ─────────────────────────────────────────────────────────────
        private readonly List<PieceThumbnailItem> _items = new List<PieceThumbnailItem>();

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Instantiates one thumbnail prefab per piece and parents them to the
        /// ScrollRect Content. GridLayoutGroup + ContentSizeFitter handle all layout.
        /// Called by PuzzleBoard after all pieces are created and shuffled.
        /// </summary>
        public void Initialize(IList<PuzzlePiece> pieces,
                               Texture2D artworkTexture,
                               int gridCols, int gridRows,
                               float pieceSizeM)
        {
            // Clean up thumbnails from a previous session
            foreach (var old in _items)
                if (old != null) Destroy(old.gameObject);
            _items.Clear();

            if (contentRoot == null)
            {
                Debug.LogError("[PieceTrayGridController] contentRoot is not assigned! " +
                               "Assign the ScrollRect Content transform in the Inspector.");
                return;
            }

            if (thumbnailPrefab == null)
            {
                Debug.LogError("[PieceTrayGridController] thumbnailPrefab is not assigned! " +
                               "Create the PieceThumbnailButton prefab and assign it.");
                return;
            }

            foreach (var piece in pieces)
            {
                if (piece == null) continue;

                // piece id = row * cols + col (same loop order as PuzzleBoard.CreatePiece)
                int col = piece.PieceId % gridCols;
                int row = piece.PieceId / gridCols;

                var go  = Instantiate(thumbnailPrefab, contentRoot);
                go.name = $"Thumb_{piece.PieceId}";

                var item = go.GetComponent<PieceThumbnailItem>();
                if (item == null)
                    item = go.AddComponent<PieceThumbnailItem>();

                item.Initialize(piece, artworkTexture, col, row, gridCols, gridRows, worldCellSizeM);
                _items.Add(item);

                // Link thumbnail to piece — PuzzlePiece.SetState() auto-shows/hides it
                piece.SetThumbnailItem(item);
            }

            gameObject.SetActive(true);

            Debug.Log($"[PieceTrayGridController] Initialized with {_items.Count} thumbnails. " +
                      "ScrollRect + GridLayoutGroup handle layout and scrolling.");
        }

        /// <summary>
        /// Permanently removes a thumbnail from the grid.
        /// Call when a piece has been correctly snapped to its slot on the board.
        /// ContentSizeFitter automatically recalculates the content height.
        /// </summary>
        public void RemoveThumbnail(int pieceId)
        {
            int idx = FindIndex(pieceId);
            if (idx < 0) return;

            Destroy(_items[idx].gameObject);
            _items.RemoveAt(idx);
        }

        /// <summary>
        /// Moves a piece's thumbnail to the last position in the grid.
        /// Call when a piece returned to the tray after an incorrect board placement.
        ///
        /// IMPORTANT: call this BEFORE the piece's state transitions to InPool so the
        /// thumbnail is already at its final sibling index when ShowThumbnailMode
        /// calls SetActive(true).
        /// </summary>
        public void MoveToEnd(int pieceId)
        {
            int idx = FindIndex(pieceId);
            if (idx < 0) return;

            var item = _items[idx];
            _items.RemoveAt(idx);
            _items.Add(item);

            // Move to the end of the Content hierarchy —
            // GridLayoutGroup re-flows the grid automatically.
            item.transform.SetAsLastSibling();
        }

        // ── Panel visibility ─────────────────────────────────────────────────────

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);

        // ── Private helpers ──────────────────────────────────────────────────────

        private int FindIndex(int pieceId) =>
            _items.FindIndex(t => t != null
                                  && t.LinkedPiece != null
                                  && t.LinkedPiece.PieceId == pieceId);
    }
}
