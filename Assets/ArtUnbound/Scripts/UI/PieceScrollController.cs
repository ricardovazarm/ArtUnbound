using System.Collections.Generic;
using UnityEngine;

namespace ArtUnbound.UI
{
    public class PieceScrollController : MonoBehaviour
    {
        [SerializeField] private float scrollStep = 0.1f;
        [SerializeField] private float visibleWidth = 0.5f; // reduced from 0.9f for better reachability

        private readonly List<Transform> pieceItems = new List<Transform>();
        private float currentScrollX = 0f;
        private float contentWidth = 0f;

        private void Awake()
        {
            // FORCE overrides to ensure exactly 7 pieces are visible as requested
            // Inspector values might be stale (e.g. 0.9 width).
            visibleWidth = 0.5f;
            scrollStep = 0.08f; // 8cm spacing * 6 gaps = 48cm span. Fits in 50cm.
        }

        public void Initialize(List<Transform> pieces)
        {
            pieceItems.Clear();
            currentScrollX = 0f;

            if (pieces != null)
            {
                pieceItems.AddRange(pieces);
            }

            if (pieceItems.Count == 0) return;

            // Calculate total content width
            contentWidth = pieceItems.Count * scrollStep;

            // Initial Layout: Center the start of the list
            // Or better: Start at 0? 
            // Let's center the first few visible items.
            // If we start at X=0, pieces go 0, 0.1, 0.2...
            // Viewport is -0.45 to +0.45.
            // So index 0 at -width/2 + step/2?
            // Let's align content to start at left edge of viewport.
            float startX = -visibleWidth / 2f + scrollStep / 2f;

            for (int i = 0; i < pieceItems.Count; i++)
            {
                if (pieceItems[i] != null)
                {
                    pieceItems[i].SetParent(transform, false);
                    pieceItems[i].localScale = Vector3.one;
                    // Initial position (unscrolled)
                    // Z = 0.025f (0.005 more than TrayVisual) to sit visible on top.
                    pieceItems[i].localPosition = new Vector3(startX + (i * scrollStep), 0f, 0.025f);
                    pieceItems[i].localRotation = Quaternion.identity;
                    pieceItems[i].gameObject.layer = 0; // Default layer
                }
            }

            UpdateVisibility();
        }

        // Min Scroll: Content End touches Viewport Right
        // Max Scroll: Content Start touches Viewport Left
        // Actually, let's just move pieces and clamp their positions?
        // Better to track a 'scrollOffset' and re-apply.

        // Let's just move them for now and clamp the *first* and *last* item positions.

        public void ScrollLeft()
        {
            // Move content right to see left items
            ScrollBy(scrollStep * 3f);
        }

        public void ScrollRight()
        {
            // Move content left to see right items
            ScrollBy(-scrollStep * 3f);
        }

        private void ScrollBy(float amount)
        {
            if (pieceItems == null || pieceItems.Count == 0) return;

            // Simple move for now, same as OnSwipe logic
            for (int i = 0; i < pieceItems.Count; i++)
            {
                if (pieceItems[i] != null)
                    pieceItems[i].localPosition += new Vector3(amount, 0f, 0f);
            }
            UpdateVisibility();
        }

        public void OnSwipe(float delta)
        {
            // Legacy Swipe or fine-tune
            ScrollBy(delta * 0.5f);
        }

        private void UpdateVisibility()
        {
            float halfWidth = visibleWidth / 2f;
            for (int i = 0; i < pieceItems.Count; i++)
            {
                if (pieceItems[i] == null) continue;

                float x = pieceItems[i].localPosition.x;
                bool isVisible = x >= -halfWidth - 0.05f && x <= halfWidth + 0.05f; // Add buffer

                if (pieceItems[i].gameObject.activeSelf != isVisible)
                {
                    pieceItems[i].gameObject.SetActive(isVisible);
                }
            }
        }
    }
}
