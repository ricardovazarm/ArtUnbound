using System.Collections.Generic;
using UnityEngine;

namespace ArtUnbound.UI
{
    public class PieceScrollController : MonoBehaviour
    {
        [Header("Tray Size Constraints (in meters)")]
        [Tooltip("Fixed tray width in meters (default: 0.3m = 30cm)")]
        [SerializeField] private float trayWidthM = 0.3f;
        
        [Tooltip("Fixed tray height in meters (default: 0.4m = 40cm)")]
        [SerializeField] private float trayHeightM = 0.4f;
        
        [Tooltip("Minimum spacing between pieces in meters (default: 0.02m = 2cm)")]
        [SerializeField] private float minSpacingM = 0.02f;

        private void OnValidate()
        {
            // Force correct default values if they're still set to old values
            if (Mathf.Approximately(trayWidthM, 0.8f) || trayWidthM > 0.6f)
            {
                Debug.LogWarning("[PieceScrollController] Resetting trayWidthM from old value to 0.3m (30cm)");
                trayWidthM = 0.3f;
            }
            
            if (Mathf.Approximately(trayHeightM, 0.8f) || trayHeightM > 0.6f)
            {
                Debug.LogWarning("[PieceScrollController] Resetting trayHeightM from old value to 0.4m (40cm)");
                trayHeightM = 0.4f;
            }
        }

        [Header("Layout (Auto-calculated - Read Only)")]
        [SerializeField, HideInInspector] private int columns = 5; // Auto-calculated based on piece size
        [SerializeField, HideInInspector] private int visibleRows = 6; // Auto-calculated based on piece size
        
        private float scrollStep = 0.08f; // Auto-calculated based on piece size
        private float horizontalSpacing = 0.08f; // Auto-calculated based on piece size
        private float visibleHeight = 0.5f; // Auto-calculated
        private const float StartY = 0.20f; // Top row Y in tray grid (must match UpdateVisibility bounds)

        private readonly List<Transform> pieceItems = new List<Transform>();
        private float scrollOffset = 0f; // Global scroll offset for all pieces
        private float contentHeight = 0f;
        private float currentPieceSize = 0.05f; // Current piece size (set by Initialize)

        [Header("Pagination (UI)")]
        [SerializeField] private PiecesPanelController panelController;
        [Tooltip("Y rotation (degrees) to align tray with pagination panel. Default 15.")]
        [SerializeField] private float trayRotationY = 15f;
        [Tooltip("Extra X offset (meters) to center tray with pagination buttons. Negative = more to player's right. Default -0.08 (8cm).")]
        [SerializeField] private float trayOffsetX = -0.08f;

        private void Awake()
        {
            // Position tray to YOUR RIGHT (player's right side)
            // Canvas is rotated 180°, so player's right = negative X (system's left)
            transform.localPosition = new Vector3(-0.45f, 0f, 0f);
            transform.localRotation = Quaternion.identity;

        }

        /// <summary>
        /// Calculates optimal grid layout for the tray based on piece size.
        /// Tray is fixed at 30x40cm, pieces fit dynamically with minimum 2cm spacing.
        /// </summary>
        private void CalculateTrayLayout(float pieceSize)
        {
            currentPieceSize = pieceSize;
            
            // Calculate cell size (piece + minimum spacing)
            float cellSize = pieceSize + minSpacingM;
            
            // Calculate how many pieces fit in 30x40cm tray WITH spacing
            columns = Mathf.FloorToInt(trayWidthM / cellSize);
            visibleRows = Mathf.FloorToInt(trayHeightM / cellSize);
            
            // Ensure minimum values
            if (columns < 2) columns = 2;
            if (visibleRows < 2) visibleRows = 2;

            // Scroll step and spacing use the CELL size (piece + spacing)
            scrollStep = cellSize;
            horizontalSpacing = cellSize;
            
            // Calculate visible height (for scrolling logic)
            visibleHeight = (visibleRows - 1) * scrollStep;

        }

        public void Initialize(List<Transform> pieces, float pieceSize)
        {
            // Calculate tray layout based on piece size
            CalculateTrayLayout(pieceSize);

            pieceItems.Clear();
            scrollOffset = 0f; // Reset scroll position

            if (pieces != null)
            {
                pieceItems.AddRange(pieces);
            }

            if (pieceItems.Count == 0) return;

            // Calculate total content height for grid layout
            // Total rows = ceil(pieceCount / columns)
            int totalRows = Mathf.CeilToInt((float)pieceItems.Count / columns);
            contentHeight = totalRows * scrollStep;

            int piecesPerPage = columns * visibleRows;
            int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)pieceItems.Count / piecesPerPage));

            // Apply initial layout
            RepositionAllPieces();
            UpdateVisibility();
            UpdateButtonStates();

            // Apply Y rotation and X offset to align tray with pagination panel
            transform.localRotation = Quaternion.Euler(0f, trayRotationY, 0f);
            transform.localPosition += new Vector3(trayOffsetX, 0f, 0f);
            panelController.Show();
            // Refresh button states now that the panel is active — the first
            // UpdateButtonStates() call above ran while the panel was still
            // inactive, so the Canvas hadn't laid out the buttons yet.
            UpdateButtonStates();
        }

        /// <summary>
        /// DEPRECATED: Old Initialize method kept for backward compatibility.
        /// Use Initialize(pieces, pieceSize) instead.
        /// </summary>
        public void Initialize(List<Transform> pieces)
        {
            // Default to 5cm pieces if piece size not provided
            Initialize(pieces, 0.05f);
        }

        /// <summary>Rows per page = visible rows on screen. Each Up/Down scrolls one full page.</summary>
        private int RowsPerPage => visibleRows;

        public void ScrollUp()
        {
            ScrollBy(scrollStep * RowsPerPage);
        }

        public void ScrollDown()
        {
            ScrollBy(-scrollStep * RowsPerPage);
        }

        // Compatibility methods for existing Unity button connections
        public void ScrollLeft() => ScrollUp();
        public void ScrollRight() => ScrollDown();

        private void ScrollBy(float amount)
        {
            if (pieceItems == null || pieceItems.Count == 0) return;

            int totalRows = Mathf.CeilToInt((float)pieceItems.Count / columns);
            float totalContentHeight = totalRows * scrollStep;
            int piecesPerPage = columns * visibleRows;
            int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)pieceItems.Count / piecesPerPage));
            float pageStep = scrollStep * RowsPerPage;
            // Allow reaching the last page: must scroll at least (totalPages-1)*pageStep to show page N
            float minScrollForLastPage = (totalPages - 1) * pageStep;
            float maxScrollDown = -Mathf.Max(0f, totalContentHeight - visibleHeight, minScrollForLastPage);
            float maxScrollUp = 0f;

            // When only one pagination page remains, list packs from the top — a negative offset
            // from "page 2" would leave the tray visually empty while the UI still shows 1/1.
            if (totalPages <= 1)
            {
                scrollOffset = 0f;
                RepositionAllPieces();
                UpdateVisibility();
                UpdateButtonStates();
                return;
            }

            if (totalContentHeight <= visibleHeight)
            {
                scrollOffset = 0f;
                RepositionAllPieces();
                UpdateVisibility();
                UpdateButtonStates();
                return;
            }

            // Don't scroll down when already on last page (prevents scrolling past content)
            int currentPage = Mathf.Clamp(1 + Mathf.RoundToInt(-scrollOffset / pageStep), 1, totalPages);
            if (amount < 0f && currentPage >= totalPages) return;

            float newScrollOffset = scrollOffset + amount;
            scrollOffset = Mathf.Clamp(newScrollOffset, maxScrollDown, maxScrollUp);

            RepositionAllPieces();
            UpdateVisibility();
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            int totalRows = Mathf.CeilToInt((float)pieceItems.Count / columns);
            float totalContentHeight = totalRows * scrollStep;
            int piecesPerPage = columns * visibleRows;
            int totalPages = pieceItems.Count == 0 ? 1 : Mathf.Max(1, Mathf.CeilToInt((float)pieceItems.Count / piecesPerPage));
            float pageStep = scrollStep * RowsPerPage;
            float minScrollForLastPage = (totalPages - 1) * pageStep;
            float maxScrollDown = -Mathf.Max(0f, totalContentHeight - visibleHeight, minScrollForLastPage);
            bool canScrollDown = scrollOffset > maxScrollDown + 0.01f;
            bool canScrollUp = scrollOffset < -0.01f;

            int currentPage = totalPages <= 1 ? 1 : Mathf.Clamp(1 + Mathf.RoundToInt(-scrollOffset / pageStep), 1, totalPages);

            // Disable "next" when on last page to prevent scrolling past content
            bool upEnabled = totalPages > 1 && canScrollUp;
            bool downEnabled = totalPages > 1 && canScrollDown && currentPage < totalPages;

            panelController.SetPaginationButtonStates(upEnabled, downEnabled);
            panelController.SetPageIndicator(currentPage, totalPages);
        }

        private void OnPieceStateChanged(ArtUnbound.Gameplay.PuzzlePiece piece, ArtUnbound.Data.PieceState newState)
        {
            if (newState == ArtUnbound.Data.PieceState.Placed)
            {
                CloseGap(piece);
            }
        }

        private void CloseGap(ArtUnbound.Gameplay.PuzzlePiece removedPiece)
        {
            if (removedPiece == null) return;
            
            int index = pieceItems.IndexOf(removedPiece.transform);
            if (index < 0) return;

            pieceItems.RemoveAt(index);

            SyncScrollOffsetAfterPieceCountChanged();
        }

        /// <summary>
        /// After pool size changes, fix scroll so we are not stuck on a stale "page 2" offset
        /// when only one pagination page remains (ScrollBy used to return without resetting offset).
        /// </summary>
        private void SyncScrollOffsetAfterPieceCountChanged()
        {
            if (pieceItems == null || pieceItems.Count == 0)
            {
                scrollOffset = 0f;
                UpdateButtonStates();
                return;
            }

            int totalRows = Mathf.CeilToInt((float)pieceItems.Count / columns);
            float totalContentHeight = totalRows * scrollStep;
            int piecesPerPage = columns * visibleRows;
            int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)pieceItems.Count / piecesPerPage));
            float pageStep = scrollStep * RowsPerPage;
            float minScrollForLastPage = (totalPages - 1) * pageStep;
            float maxScrollDown = -Mathf.Max(0f, totalContentHeight - visibleHeight, minScrollForLastPage);
            float maxScrollUp = 0f;

            if (totalPages <= 1 || totalContentHeight <= visibleHeight)
                scrollOffset = 0f;
            else
                scrollOffset = Mathf.Clamp(scrollOffset, maxScrollDown, maxScrollUp);

            RepositionAllPieces();
            UpdateVisibility();
            UpdateButtonStates();

            // If still no pool pieces in view (multi-page only), step toward previous page
            if (totalPages > 1 && !AnyPoolPieceVisibleInTrayViewport())
            {
                ScrollUp();
            }
        }

        private bool AnyPoolPieceVisibleInTrayViewport()
        {
            float yMax = StartY + 0.05f;
            float yMin = StartY - visibleHeight - 0.05f;
            float gridWidth = (columns - 1) * horizontalSpacing;
            float halfWidth = gridWidth / 2f;

            for (int i = 0; i < pieceItems.Count; i++)
            {
                if (pieceItems[i] == null) continue;

                var p = pieceItems[i].GetComponent<ArtUnbound.Gameplay.PuzzlePiece>();
                if (p != null && p.CurrentState == ArtUnbound.Data.PieceState.Placed)
                    continue;

                Vector3 localPos = pieceItems[i].localPosition;
                bool inY = localPos.y >= yMin && localPos.y <= yMax;
                bool inX = localPos.x >= -halfWidth - 0.05f && localPos.x <= halfWidth + 0.05f;
                if (inY && inX)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Adds a piece at the beginning of the grid (top-left position).
        /// All existing pieces shift to the right/down. Used when returning a piece from the board.
        /// </summary>
        public void AddPieceAtBeginning(ArtUnbound.Gameplay.PuzzlePiece piece)
        {
            if (piece == null) return;

            // Insert at the beginning of the list (top position)
            pieceItems.Insert(0, piece.transform);
            piece.transform.SetParent(transform, false);
            piece.transform.localScale = Vector3.one;
            
            // Set piece state
            piece.SetState(ArtUnbound.Data.PieceState.InPool);
            
            // Reposition ALL pieces - this will put the new piece at the top and shift everything down
            RepositionAllPieces();
            UpdateVisibility();
            UpdateButtonStates();
        }

        /// <summary>
        /// Adds a piece at the end of the grid (bottom-right position).
        /// Used when returning a piece that was already placed on the board.
        /// </summary>
        public void AddPieceAtEnd(ArtUnbound.Gameplay.PuzzlePiece piece)
        {
            if (piece == null) return;

            pieceItems.Add(piece.transform);
            piece.transform.SetParent(transform, false);
            piece.transform.localScale = Vector3.one;
            
            // Set piece state
            piece.SetState(ArtUnbound.Data.PieceState.InPool);
            
            // Reposition ALL pieces - this will put the new piece at the bottom
            RepositionAllPieces();
            UpdateVisibility();
            UpdateButtonStates();
        }

        /// <summary>
        /// Repositions all pieces in a grid layout based on their index and current scroll offset.
        /// Grid: 5 columns × N rows, with pieces arranged left-to-right, top-to-bottom.
        /// </summary>
        private void RepositionAllPieces()
        {
            // Grid layout: pieces flow left-to-right, top-to-bottom
            int visualIndex = 0;

            // Calculate horizontal positions for 5 columns
            float gridWidth = (columns - 1) * horizontalSpacing;
            float startX = -gridWidth / 2f;

            for (int i = 0; i < pieceItems.Count; i++)
            {
                if (pieceItems[i] == null) continue;

                var p = pieceItems[i].GetComponent<ArtUnbound.Gameplay.PuzzlePiece>();
                if (p == null) continue;

                // Only reposition pieces that are in the pool or returning
                if (p.CurrentState == ArtUnbound.Data.PieceState.InPool || 
                    p.CurrentState == ArtUnbound.Data.PieceState.Returning)
                {
                    pieceItems[i].SetParent(transform, false);
                    pieceItems[i].localScale = Vector3.one;
                    
                    // Calculate grid position
                    int column = visualIndex % columns;
                    int row = visualIndex / columns;
                    
                    float xPos = startX + (column * horizontalSpacing);
                    float yPos = StartY - (row * scrollStep) - scrollOffset;
                    
                    Vector3 newLocalPos = new Vector3(xPos, yPos, 0.025f);
                    pieceItems[i].localPosition = newLocalPos;
                    pieceItems[i].localRotation = Quaternion.identity;

                    // Update the piece's pool position
                    p.SetPoolPosition(pieceItems[i].position);
                    
                    // Subscribe to state changes if not already subscribed
                    p.OnStateChanged -= OnPieceStateChanged;
                    p.OnStateChanged += OnPieceStateChanged;

                    visualIndex++;
                }
            }
        }

        public void OnSwipe(float delta)
        {
            // Legacy Swipe or fine-tune
            ScrollBy(delta * 0.5f);
        }

        private void UpdateVisibility()
        {
            float yMax = StartY + 0.05f;
            float yMin = StartY - visibleHeight - 0.05f;
            float gridWidth = (columns - 1) * horizontalSpacing;
            float halfWidth = gridWidth / 2f;
            int visibleCount = 0;

            for (int i = 0; i < pieceItems.Count; i++)
            {
                if (pieceItems[i] == null) continue;

                var p = pieceItems[i].GetComponent<ArtUnbound.Gameplay.PuzzlePiece>();
                if (p != null && p.CurrentState == ArtUnbound.Data.PieceState.Placed)
                    continue;

                Vector3 localPos = pieceItems[i].localPosition;
                bool isVisibleVertical = localPos.y >= yMin && localPos.y <= yMax;
                bool isVisibleHorizontal = localPos.x >= -halfWidth - 0.05f && localPos.x <= halfWidth + 0.05f;
                bool isVisible = isVisibleVertical && isVisibleHorizontal;
                if (isVisible) visibleCount++;

                if (pieceItems[i].gameObject.activeSelf != isVisible)
                {
                    pieceItems[i].gameObject.SetActive(isVisible);
                }
            }
        }

        /// <summary>
        /// Call after RestoreBoardState to ensure remaining tray pieces are visible.
        /// </summary>
        public void RefreshTrayAfterRestore()
        {
            RepositionAllPieces();
            UpdateVisibility();
            UpdateButtonStates();
        }

        /// <summary>
        /// Removes a piece from the tray (used when restoring saved state).
        /// The piece is deactivated so it doesn't appear in the tray.
        /// </summary>
        public void RemovePieceFromTray(ArtUnbound.Gameplay.PuzzlePiece piece)
        {
            if (piece == null) return;
            
            piece.gameObject.SetActive(false);
        }

        /// <summary>
        /// Hides the scroll buttons (called when puzzle is complete)
        /// </summary>
        public void HideScrollButtons()
        {
            panelController?.Hide();
        }
    }
}
