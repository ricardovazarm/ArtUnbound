using UnityEngine;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Controls the right-side PiecesPanel shown while assembling a puzzle.
    /// Responsible only for showing/hiding the panel — layout and scrolling are
    /// now handled by the ScrollRect + GridLayoutGroup inside the panel
    /// (managed by PieceTrayGridController).
    ///
    /// SetPaginationButtonStates() and SetPageIndicator() are kept as no-ops
    /// so PieceScrollController compiles without changes.
    /// </summary>
    public class PiecesPanelController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Root GameObject of the pieces panel. If null, this GameObject is used.")]
        [SerializeField] private GameObject panel;

        private void Awake() => Hide();

        // ── Panel visibility ─────────────────────────────────────────────────

        /// <summary>Show the pieces panel (called when a puzzle starts).</summary>
        public void Show()
        {
            if (panel != null) panel.SetActive(true);
            else               gameObject.SetActive(true);
        }

        /// <summary>Hide the pieces panel (called when a puzzle completes).</summary>
        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
            else               gameObject.SetActive(false);
        }

        // ── No-ops kept for backward compatibility ───────────────────────────
        // PieceScrollController still calls these; they do nothing now that
        // pagination has been replaced by a ScrollRect.

        public void SetPaginationButtonStates(bool canScrollUp, bool canScrollDown) { }
        public void SetPageIndicator(int currentPage, int totalPages) { }
    }
}
