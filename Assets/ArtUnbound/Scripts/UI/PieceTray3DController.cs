using System.Collections.Generic;
using ArtUnbound.Data;
using ArtUnbound.Gameplay;
using UnityEngine;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Manages the 3D piece tray: grid layout, smooth scroll, and viewport visibility.
    /// Pieces are arranged in a fixed-index grid (placed pieces leave an invisible hole).
    /// Scroll is clamped so there are no empty rows at top or bottom.
    /// </summary>
    public class PieceTray3DController : MonoBehaviour
    {
        [SerializeField] private PuzzleConfig puzzleConfig;

        [Tooltip("Logs each time a tray piece toggles visible/hidden, with the scroll value that caused it. " +
                 "Use to confirm the 'first row disappears on grab' issue is scroll-driven.")]
        [SerializeField] private bool debugVisibilityLogs = true;

        [Header("Runtime Info (read-only)")]
        [SerializeField] private int _columns;
        [SerializeField] private int _visibleRows;
        [SerializeField] private float _cellSize;
        [SerializeField] private float _maxScroll;
        [SerializeField] private float _currentScroll;

        private readonly List<PuzzlePiece> _pieces = new List<PuzzlePiece>();
        private float _targetScroll;

        private float ViewportWidth  => puzzleConfig != null ? puzzleConfig.trayViewportWidthCm  * 0.01f : 0.3f;
        private float ViewportHeight => puzzleConfig != null ? puzzleConfig.trayViewportHeightCm * 0.01f : 0.5f;
        private float Smoothing      => puzzleConfig != null ? puzzleConfig.trayScrollSmoothing : 10f;

        // ── Initialization ───────────────────────────────────────────────────────

        public void Initialize(List<PuzzlePiece> pieces, float pieceSize)
        {
            _pieces.Clear();
            if (pieces != null) _pieces.AddRange(pieces);

            float margin = puzzleConfig != null ? puzzleConfig.trayCellMargin : 1.2f;
            _cellSize    = pieceSize * margin;

            _columns     = Mathf.Max(1, Mathf.FloorToInt(ViewportWidth  / _cellSize));
            _visibleRows = Mathf.Max(1, Mathf.FloorToInt(ViewportHeight / _cellSize));
            _targetScroll = 0f;
            _currentScroll = 0f;

            RecalculateMaxScroll();

            foreach (var p in _pieces)
            {
                if (p == null) continue;
                p.transform.SetParent(transform, false);
                p.transform.localRotation = Quaternion.identity;
                p.transform.localScale    = Vector3.one;
                p.ShowPieceMode();
            }

            UpdatePositionsAndVisibility();
        }

        // ── Per-frame update ─────────────────────────────────────────────────────

        private void Update()
        {
            _currentScroll = Mathf.Lerp(_currentScroll, _targetScroll, Time.deltaTime * Smoothing);
            UpdatePositionsAndVisibility();
        }

        // ── Scroll ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Scrolls the tray by deltaMeters. Positive = rows move up (shows rows below).
        /// Clamped: no empty rows at top (scroll=0) or bottom (scroll=maxScroll).
        /// </summary>
        public void ScrollBy(float deltaMeters)
        {
            _targetScroll = Mathf.Clamp(_targetScroll + deltaMeters, 0f, _maxScroll);
        }

        /// <summary>Current scroll target (meters). Used to snapshot/restore scroll around a grab.</summary>
        public float CurrentTargetScroll => _targetScroll;

        /// <summary>
        /// Restores the scroll target to a previously-snapshotted value. Called when a grab is
        /// detected with the finger inside the tray, to undo any scroll that crept in while the
        /// user was reaching to grab a piece (otherwise the top row would stay hidden).
        /// </summary>
        public void RevertScrollTo(float targetScroll)
        {
            _targetScroll = Mathf.Clamp(targetScroll, 0f, _maxScroll);
        }

        // ── Piece management ─────────────────────────────────────────────────────

        /// <summary>
        /// Removes a piece from the tray list and compacts remaining pieces to fill the gap.
        /// Call this when a piece is successfully placed on the board.
        /// </summary>
        public void RemovePieceFromTray(PuzzlePiece piece)
        {
            if (piece == null) return;
            if (!_pieces.Remove(piece)) return;

            RecalculateMaxScroll();
            UpdatePositionsAndVisibility();
        }

        /// <summary>Adds a piece to the END of the tray grid (used for pieces returned from board).</summary>
        public void AddPieceAtEnd(PuzzlePiece piece)
        {
            if (piece == null) return;
            if (!_pieces.Contains(piece))
                _pieces.Add(piece);

            piece.transform.SetParent(transform, false);
            piece.transform.localScale    = Vector3.one;
            piece.transform.localRotation = Quaternion.identity;
            piece.SetState(PieceState.InPool);

            RecalculateMaxScroll();
            UpdatePositionsAndVisibility();
        }

        /// <summary>
        /// Animates a tray piece back to its original fixed grid position.
        /// Used when a piece grabbed from the tray fails to snap to the board.
        /// </summary>
        public void ReturnPieceToGrid(PuzzlePiece piece)
        {
            if (piece == null) return;
            int idx = _pieces.IndexOf(piece);
            if (idx < 0) { piece.SetState(PieceState.InPool); return; }

            piece.gameObject.SetActive(true);
            Vector3 worldTarget = transform.TransformPoint(GetLocalGridPosition(idx));
            piece.ReturnToPool(worldTarget);
        }

        /// <summary>Re-evaluates positions after a board state restore.</summary>
        public void RefreshAfterRestore() => UpdatePositionsAndVisibility();

        public void HideScrollButtons() { /* no scroll buttons in 3D system */ }

        /// <summary>
        /// Returns the piece whose grid cell contains the ray's intersection with the tray plane,
        /// or null if the ray misses the tray or no cell is hit. Works without mesh colliders.
        /// </summary>
        public PuzzlePiece GetPieceAtRaycast(Ray ray)
        {
            Plane trayPlane = new Plane(-transform.forward, transform.position);
            if (!trayPlane.Raycast(ray, out float dist) || dist <= 0f) return null;
            Vector3 local = transform.InverseTransformPoint(ray.GetPoint(dist));

            if (Mathf.Abs(local.x) > ViewportWidth  * 0.5f) return null;
            if (Mathf.Abs(local.y) > ViewportHeight * 0.5f) return null;

            float halfCell = _cellSize * 0.5f;
            for (int i = 0; i < _pieces.Count; i++)
            {
                var p = _pieces[i];
                if (p == null || !p.gameObject.activeSelf || p.CurrentState == PieceState.Placed) continue;
                Vector3 slot = GetLocalGridPosition(i);
                if (Mathf.Abs(local.x - slot.x) <= halfCell &&
                    Mathf.Abs(local.y - slot.y) <= halfCell)
                    return p;
            }
            return null;
        }

        /// <summary>
        /// Returns true if the given ray intersects the tray viewport plane.
        /// hitWorldY is the world-space Y of the intersection point (used for scroll delta).
        /// </summary>
        public bool RayHitsTray(Ray ray, out float hitWorldY)
        {
            Plane trayPlane = new Plane(-transform.forward, transform.position);
            if (!trayPlane.Raycast(ray, out float dist) || dist <= 0f)
            {
                hitWorldY = 0f;
                return false;
            }
            Vector3 hit   = ray.GetPoint(dist);
            Vector3 local = transform.InverseTransformPoint(hit);
            bool inside   = Mathf.Abs(local.x) < ViewportWidth  * 0.5f
                         && Mathf.Abs(local.y) < ViewportHeight * 0.5f;
            hitWorldY = hit.y;
            return inside;
        }

        // ── Internal helpers ─────────────────────────────────────────────────────

        private void RecalculateMaxScroll()
        {
            int totalRows = Mathf.CeilToInt((float)_pieces.Count / Mathf.Max(1, _columns));
            _maxScroll    = Mathf.Max(0f, (totalRows - _visibleRows) * _cellSize);
            _targetScroll = Mathf.Clamp(_targetScroll, 0f, _maxScroll);
        }

        private Vector3 GetLocalGridPosition(int idx)
        {
            int col  = idx % _columns;
            int row  = idx / _columns;
            float halfW = (_columns - 1) * _cellSize * 0.5f;
            float topY  = ViewportHeight * 0.5f - _cellSize * 0.5f;
            return new Vector3(
                -halfW + col * _cellSize,
                topY - row * _cellSize + _currentScroll,
                0f
            );
        }

        private void UpdatePositionsAndVisibility()
        {
            if (_columns <= 0 || _cellSize <= 0f) return;

            float halfView = ViewportHeight * 0.5f;
            float halfCell = _cellSize * 0.5f;

            for (int i = 0; i < _pieces.Count; i++)
            {
                var p = _pieces[i];
                if (p == null) continue;

                var state = p.CurrentState;

                // Board or grabbed: don't touch position or active state
                if (state == PieceState.Placed || state == PieceState.Grabbed)
                    continue;

                // Animating back to pool: keep active so coroutine can run
                if (state == PieceState.Returning)
                {
                    if (!p.gameObject.activeSelf) p.gameObject.SetActive(true);
                    continue;
                }

                // InPool: reposition to fixed grid slot, control visibility by viewport
                Vector3 localPos = GetLocalGridPosition(i);
                p.transform.localPosition = localPos;
                p.SetPoolPosition(p.transform.position);

                // A piece stays visible while it at least half-overlaps the viewport.
                // This tolerance (half a cell) is critical: the TOP row sits exactly on the upper
                // edge (localPos.y + halfCell == halfView), so a stricter test would flicker the
                // whole first row off on the slightest scroll — and the reach-to-grab motion always
                // nudges the proximity scroll a little, more so with the big pieces of small puzzles.
                // Deliberate scrolls (a full row or more) still page content as expected.
                float edgeTolerance = halfCell; // half a cell
                bool inViewport = localPos.y - halfCell >= -halfView - edgeTolerance
                               && localPos.y + halfCell <=  halfView + edgeTolerance;

                if (p.gameObject.activeSelf != inViewport)
                {
                    if (debugVisibilityLogs)
                        Debug.Log($"[Tray3D] piece idx={i} → {(inViewport ? "SHOW" : "HIDE")}  " +
                                  $"localY={localPos.y:F3} scroll(cur={_currentScroll:F3} tgt={_targetScroll:F3})");
                    p.gameObject.SetActive(inViewport);
                }
            }
        }
    }
}
