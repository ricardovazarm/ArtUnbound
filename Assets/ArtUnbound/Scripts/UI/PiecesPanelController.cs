using TMPro;
using UnityEngine;
using ArtUnbound.Input;

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

        [Tooltip("Text field that shows the interaction instructions.")]
        [SerializeField] private TMP_Text instructionText;

        [Tooltip("HandTrackingInputController used to detect hand vs controller mode.")]
        [SerializeField] private HandTrackingInputController inputController;

        [Header("Instructions")]
        [TextArea(2, 4)]
        [SerializeField] private string handsInstruction = "Grab the pieces and place them on the board to complete the puzzle";
        [TextArea(2, 4)]
        [SerializeField] private string controllerInstruction = "Point and click to select a piece, then click a cell on the board to place it.";

        private bool _lastControllerMode;

        private void Awake() => Hide();

        private void Update()
        {
            if (instructionText == null || inputController == null) return;
            bool isController = inputController.useControllers;
            if (isController == _lastControllerMode) return;
            _lastControllerMode = isController;
            instructionText.text = isController ? controllerInstruction : handsInstruction;
        }

        // ── Panel visibility ─────────────────────────────────────────────────

        /// <summary>Show the pieces panel (called when a puzzle starts).</summary>
        public void Show()
        {
            if (panel != null) panel.SetActive(true);
            else               gameObject.SetActive(true);
            RefreshInstruction();
        }

        private void RefreshInstruction()
        {
            if (instructionText == null || inputController == null) return;
            bool isController = inputController.useControllers;
            _lastControllerMode = isController;
            instructionText.text = isController ? controllerInstruction : handsInstruction;
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
