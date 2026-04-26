using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ArtUnbound.Input;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Controls the right-side PiecesPanel shown while assembling a puzzle.
    /// Responsible only for showing/hiding the panel — layout and scrolling are
    /// now handled by the ScrollRect + GridLayoutGroup inside the panel
    /// (managed by PieceTrayGridController).
    ///
    /// Instruction visibility:
    ///   - On Show(): instruction text fades in, stays for visibleDuration, then
    ///     fades out while a small help button fades in.
    ///   - Clicking the help button replays the cycle.
    ///   - Switching input mode (controllers <-> hands) replays the cycle so the
    ///     user sees the relevant instructions.
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

        [Tooltip("Button shown when the instruction text auto-hides. Clicking it replays the instruction.")]
        [SerializeField] private Button helpButton;

        [Tooltip("HandTrackingInputController used to detect hand vs controller mode.")]
        [SerializeField] private HandTrackingInputController inputController;

        [Header("Instructions")]
        [TextArea(2, 4)]
        [SerializeField] private string handsInstruction = "Grab the pieces and place them on the board to complete the puzzle";
        [TextArea(2, 4)]
        [SerializeField] private string controllerInstruction = "Point and click to select a piece, then click a cell on the board to place it.";

        [Header("Timing")]
        [Tooltip("Seconds the instruction text stays fully visible before fading out.")]
        [SerializeField] private float visibleDuration = 15f;
        [Tooltip("Fade in/out duration in seconds.")]
        [SerializeField] private float fadeDuration = 0.4f;

        private CanvasGroup _instructionGroup;
        private CanvasGroup _helpButtonGroup;
        private Coroutine _routine;
        private bool _lastControllerMode;

        private void Awake()
        {
            if (instructionText != null)
                _instructionGroup = GetOrAddCanvasGroup(instructionText.gameObject);

            if (helpButton != null)
            {
                _helpButtonGroup = GetOrAddCanvasGroup(helpButton.gameObject);
                helpButton.onClick.AddListener(OnHelpClicked);
            }

            // Both start invisible — InstructionCycle fades them in/out as needed.
            SetGroupAlpha(_instructionGroup, 0f);
            SetGroupAlpha(_helpButtonGroup, 0f);

            Hide();
        }

        private void OnDestroy()
        {
            if (helpButton != null)
                helpButton.onClick.RemoveListener(OnHelpClicked);
        }

        private void Update()
        {
            if (instructionText == null || inputController == null) return;
            bool isController = inputController.useControllers;
            if (isController == _lastControllerMode) return;
            _lastControllerMode = isController;
            instructionText.text = isController ? controllerInstruction : handsInstruction;

            // Mode changed mid-game — replay the cycle so the user sees the new instructions.
            bool panelVisible = panel != null ? panel.activeInHierarchy : gameObject.activeInHierarchy;
            if (panelVisible)
                StartInstructionCycle();
        }

        // ── Panel visibility ─────────────────────────────────────────────────

        /// <summary>Show the pieces panel (called when a puzzle starts).</summary>
        public void Show()
        {
            if (panel != null) panel.SetActive(true);
            else               gameObject.SetActive(true);
            RefreshInstruction();
            StartInstructionCycle();
        }

        /// <summary>Hide the pieces panel (called when a puzzle completes).</summary>
        public void Hide()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
            SetGroupAlpha(_instructionGroup, 0f);
            SetGroupAlpha(_helpButtonGroup, 0f);

            if (panel != null) panel.SetActive(false);
            else               gameObject.SetActive(false);
        }

        private void RefreshInstruction()
        {
            if (instructionText == null || inputController == null) return;
            bool isController = inputController.useControllers;
            _lastControllerMode = isController;
            instructionText.text = isController ? controllerInstruction : handsInstruction;
        }

        // ── Help button + fade cycle ─────────────────────────────────────────

        private void OnHelpClicked()
        {
            StartInstructionCycle();
        }

        private void StartInstructionCycle()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(InstructionCycle());
        }

        private IEnumerator InstructionCycle()
        {
            // Hide button, show text.
            yield return FadeGroup(_helpButtonGroup, 0f);
            yield return FadeGroup(_instructionGroup, 1f);

            // Hold the text on screen.
            yield return new WaitForSeconds(visibleDuration);

            // Fade text out, button in.
            yield return FadeGroup(_instructionGroup, 0f);
            yield return FadeGroup(_helpButtonGroup, 1f);

            _routine = null;
        }

        private IEnumerator FadeGroup(CanvasGroup group, float targetAlpha)
        {
            if (group == null) yield break;

            // Update interactivity at the start of the fade so a fading-out button
            // can't be clicked during its disappearance.
            bool willBeVisible = targetAlpha > 0.5f;
            group.interactable = willBeVisible;
            group.blocksRaycasts = willBeVisible;

            float duration = Mathf.Max(0f, fadeDuration);
            if (duration <= 0f)
            {
                group.alpha = targetAlpha;
                yield break;
            }

            float start = group.alpha;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                group.alpha = Mathf.Lerp(start, targetAlpha, Mathf.Clamp01(t / duration));
                yield return null;
            }
            group.alpha = targetAlpha;
        }

        private static CanvasGroup GetOrAddCanvasGroup(GameObject go)
        {
            var cg = go.GetComponent<CanvasGroup>();
            return cg != null ? cg : go.AddComponent<CanvasGroup>();
        }

        private static void SetGroupAlpha(CanvasGroup group, float alpha)
        {
            if (group == null) return;
            group.alpha = alpha;
            bool visible = alpha > 0.5f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }

        // ── No-ops kept for backward compatibility ───────────────────────────
        // PieceScrollController still calls these; they do nothing now that
        // pagination has been replaced by a ScrollRect.

        public void SetPaginationButtonStates(bool canScrollUp, bool canScrollDown) { }
        public void SetPageIndicator(int currentPage, int totalPages) { }
    }
}
