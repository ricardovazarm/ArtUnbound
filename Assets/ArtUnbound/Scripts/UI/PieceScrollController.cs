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

        private void Awake()
        {
            // Position tray to YOUR RIGHT (player's right side)
            // Canvas is rotated 180°, so player's right = negative X (system's left)
            transform.localPosition = new Vector3(-0.45f, 0f, 0f);
            transform.localRotation = Quaternion.identity;

            // Add scroll buttons component if not already present
            if (GetComponent<TrayScrollButtons>() == null)
            {
                gameObject.AddComponent<TrayScrollButtons>();
            }
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

            Debug.Log($"[PIECE] Tray Initialize: totalPieces={pieceItems.Count}, columns={columns}, visibleRows={visibleRows}");
            if (pieceItems.Count == 0) return;

            // Calculate total content height for grid layout
            // Total rows = ceil(pieceCount / columns)
            int totalRows = Mathf.CeilToInt((float)pieceItems.Count / columns);
            contentHeight = totalRows * scrollStep;

            // Apply initial layout
            RepositionAllPieces();
            UpdateVisibility();
            UpdateButtonStates();

            // Create and show scroll buttons when puzzle starts
            var scrollButtons = GetComponent<TrayScrollButtons>();
            if (scrollButtons != null)
            {
                scrollButtons.Initialize();
                scrollButtons.Show(); // Make sure buttons are visible for new puzzle
            }
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

        public void ScrollUp()
        {
            ScrollBy(scrollStep * 3f);
        }

        public void ScrollDown()
        {
            ScrollBy(-scrollStep * 3f);
        }

        // Compatibility methods for existing Unity button connections
        public void ScrollLeft() => ScrollUp();
        public void ScrollRight() => ScrollDown();

        private void ScrollBy(float amount)
        {
            if (pieceItems == null || pieceItems.Count == 0)
            {
                return;
            }

            // Calculate scroll limits based on grid layout
            int totalRows = Mathf.CeilToInt((float)pieceItems.Count / columns);
            float totalContentHeight = totalRows * scrollStep;
            
            // ScrollDown makes offset negative (content moves up to see items below)
            // ScrollUp makes offset positive (content moves down to see items above)
            float maxScrollDown = -Mathf.Max(0f, totalContentHeight - visibleHeight);
            float maxScrollUp = 0f;
            
            // If content fits entirely in viewport, don't allow any scrolling
            if (totalContentHeight <= visibleHeight)
            {
                scrollOffset = 0f;
                UpdateButtonStates();
                return;
            }

            // Update global scroll offset with clamping
            float newScrollOffset = scrollOffset + amount;
            scrollOffset = Mathf.Clamp(newScrollOffset, maxScrollDown, maxScrollUp);

            // Reposition all pieces based on the new scroll offset
            RepositionAllPieces();
            UpdateVisibility();
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            var scrollButtons = GetComponent<TrayScrollButtons>();
            if (scrollButtons == null) return;

            // Calculate scroll limits based on grid layout
            int totalRows = Mathf.CeilToInt((float)pieceItems.Count / columns);
            float totalContentHeight = totalRows * scrollStep;
            float maxScrollDown = -Mathf.Max(0f, totalContentHeight - visibleHeight);
            
            bool canScrollDown = scrollOffset > maxScrollDown + 0.01f;
            bool canScrollUp = scrollOffset < -0.01f;

            scrollButtons.SetButtonStates(canScrollUp, canScrollDown);
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
            if (index < 0)
            {
                Debug.LogWarning($"[PIECE] CloseGap: piece {removedPiece.PieceId} NOT in pieceItems (count={pieceItems.Count})");
                return;
            }

            pieceItems.RemoveAt(index);
            Debug.Log($"[PIECE] CloseGap: piece {removedPiece.PieceId} removed from tray, trayCount={pieceItems.Count}");

            // Reposition all remaining pieces to close the gap
            RepositionAllPieces();
            UpdateVisibility();
            UpdateButtonStates();
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
            Debug.Log($"[PIECE] AddPieceAtEnd: piece {piece.PieceId} returned to tray, trayCount={pieceItems.Count}");
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
            // Align visibility bounds with grid layout: top at StartY, bottom at StartY - visibleHeight
            float yMax = StartY + 0.05f;
            float yMin = StartY - visibleHeight - 0.05f;

            // Calculate horizontal bounds for the grid
            float gridWidth = (columns - 1) * horizontalSpacing;
            float halfWidth = gridWidth / 2f;

            for (int i = 0; i < pieceItems.Count; i++)
            {
                if (pieceItems[i] == null) continue;

                var p = pieceItems[i].GetComponent<ArtUnbound.Gameplay.PuzzlePiece>();
                if (p != null && p.CurrentState == ArtUnbound.Data.PieceState.Placed)
                    continue; // Skip pieces on board - they use different coordinate space

                Vector3 localPos = pieceItems[i].localPosition;

                // Check if piece is within visible bounds (aligned with RepositionAllPieces layout)
                bool isVisibleVertical = localPos.y >= yMin && localPos.y <= yMax;
                bool isVisibleHorizontal = localPos.x >= -halfWidth - 0.05f && localPos.x <= halfWidth + 0.05f;
                bool isVisible = isVisibleVertical && isVisibleHorizontal;

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
            int inPoolCount = 0;
            foreach (var t in pieceItems)
            {
                var p = t?.GetComponent<ArtUnbound.Gameplay.PuzzlePiece>();
                if (p != null && (p.CurrentState == ArtUnbound.Data.PieceState.InPool || p.CurrentState == ArtUnbound.Data.PieceState.Returning))
                    inPoolCount++;
            }
            Debug.Log($"[PIECE] RemovePieceFromTray: piece {piece.PieceId} hidden (restore), trayInPoolCount={inPoolCount}, pieceItemsTotal={pieceItems.Count}");
        }

        /// <summary>
        /// Hides the scroll buttons (called when puzzle is complete)
        /// </summary>
        public void HideScrollButtons()
        {
            var scrollButtons = GetComponent<TrayScrollButtons>();
            if (scrollButtons != null)
            {
                scrollButtons.Hide();
            }
        }
    }
}
