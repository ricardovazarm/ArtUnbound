using System.Collections.Generic;
using UnityEngine;

namespace ArtUnbound.UI
{
    public class PieceScrollController : MonoBehaviour
    {
        [SerializeField] private float scrollStep = 0.1f;
        [SerializeField] private float visibleHeight = 0.5f; // Vertical tray height
        [SerializeField] private int columns = 5; // Number of columns in the grid
        [SerializeField] private float horizontalSpacing = 0.08f; // Spacing between columns

        private readonly List<Transform> pieceItems = new List<Transform>();
        private float scrollOffset = 0f; // Global scroll offset for all pieces
        private float contentHeight = 0f;
        private int visibleRows = 6; // Number of rows visible at once

        private void Awake()
        {
            // GRID TRAY: 5 columns × 6 visible rows = 30 pieces visible
            columns = 5;
            visibleRows = 6;
            visibleHeight = (visibleRows - 1) * 0.08f; // 5 spaces * 0.08 = 0.40m
            scrollStep = 0.08f; // 8cm spacing between pieces vertically
            horizontalSpacing = 0.08f; // 8cm spacing between columns

            // Position tray to YOUR RIGHT (player's right side)
            // Canvas is rotated 180°, so player's right = negative X (system's left)
            // With 5 columns at 0.08m spacing, total width = 4 * 0.08 = 0.32m
            // Move tray far to the right to avoid overlapping the canvas
            transform.localPosition = new Vector3(-0.45f, 0f, 0f);
            transform.localRotation = Quaternion.identity;

            // Add scroll buttons component if not already present
            if (GetComponent<TrayScrollButtons>() == null)
            {
                gameObject.AddComponent<TrayScrollButtons>();
            }
        }

        public void Initialize(List<Transform> pieces)
        {
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

            // Apply initial layout
            RepositionAllPieces();
            UpdateVisibility();
            UpdateButtonStates();

            // Create scroll buttons when puzzle starts
            var scrollButtons = GetComponent<TrayScrollButtons>();
            if (scrollButtons != null)
            {
                scrollButtons.Initialize();
            }
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
            if (index < 0) return;

            // Remove the piece from the list
            pieceItems.RemoveAt(index);

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

            // Add at the end of the list
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
            // Grid layout: 5 columns, pieces flow left-to-right, top-to-bottom
            float startY = 0.20f;
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
                    float yPos = startY - (row * scrollStep) - scrollOffset;
                    
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
            float halfHeight = visibleHeight / 2f;
            
            // Calculate horizontal bounds for the grid (5 columns)
            float gridWidth = (columns - 1) * horizontalSpacing;
            float halfWidth = gridWidth / 2f;
            
            for (int i = 0; i < pieceItems.Count; i++)
            {
                if (pieceItems[i] == null) continue;

                Vector3 localPos = pieceItems[i].localPosition;
                
                // Check if piece is within visible bounds (with small buffer)
                bool isVisibleVertical = localPos.y >= -halfHeight - 0.05f && localPos.y <= halfHeight + 0.05f;
                bool isVisibleHorizontal = localPos.x >= -halfWidth - 0.05f && localPos.x <= halfWidth + 0.05f;
                bool isVisible = isVisibleVertical && isVisibleHorizontal;

                if (pieceItems[i].gameObject.activeSelf != isVisible)
                {
                    pieceItems[i].gameObject.SetActive(isVisible);
                }
            }
        }
    }
}
