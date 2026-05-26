using System.Collections;
using UnityEngine;
using TMPro;
using ArtUnbound.Input;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Milestone type for puzzle achievements.
    /// </summary>
    public enum MilestoneType
    {
        Row,
        Column,
        RowAndColumn,
        Edge,
        Edges,
        AllEdges
    }

    /// <summary>
    /// Controls the achievements/milestones panel during puzzle gameplay.
    /// Shows milestone messages (row, column, edge completion) and can be extended
    /// for future achievement types.
    /// </summary>
    public class PuzzleAchievementsController : MonoBehaviour
    {
        [Header("Milestone Text")]
        [SerializeField] private TextMeshProUGUI milestoneText;
        [Tooltip("Panel that contains the text (for Show/Hide). If null, uses milestoneText's parent or the text itself.")]
        [SerializeField] private GameObject milestonePanel;
        [SerializeField] private float milestoneTextDuration = 2f;

        [Header("Hanging Instructions")]
        [Tooltip("Text field that shows hanging instructions. Optional.")]
        [SerializeField] private TMP_Text hangingInstructionText;
        [Tooltip("HandTrackingInputController used to detect hand vs controller mode.")]
        [SerializeField] private HandTrackingInputController inputController;
        [TextArea(2, 4)]
        [SerializeField] private string handsHangingInstruction = "Pinch to grab the frame and release it against a wall to hang your masterpiece.";
        [TextArea(2, 4)]
        [SerializeField] private string controllerHangingInstruction = "Point and click to select the frame, then click on a wall to hang it.";

        private bool _lastControllerMode;
        private bool _hangingTextInitialized;

        [Header("Message Strings (edit for localization)")]
        [SerializeField] private string rowComplete = "¡Fila completa!";
        [SerializeField] private string columnComplete = "¡Columna completa!";
        [SerializeField] private string rowAndColumnComplete = "¡Fila y Columna completas!";
        [SerializeField] private string edgeComplete = "¡Borde completo!";
        [Tooltip("{0} = count. E.g. '¡{0} bordes completos!'")]
        [SerializeField] private string edgesCompleteFormat = "¡{0} bordes completos!";
        [SerializeField] private string allEdgesComplete = "¡Marco completo!";

        private void Start()
        {
            if (inputController == null)
                inputController = FindFirstObjectByType<HandTrackingInputController>();
        }

        private void OnEnable()
        {
            _hangingTextInitialized = false;
        }

        private void Update()
        {
            if (hangingInstructionText == null || inputController == null) return;
            bool isController = inputController.HasValidController;
            if (_hangingTextInitialized && isController == _lastControllerMode) return;
            _hangingTextInitialized = true;
            _lastControllerMode = isController;
            hangingInstructionText.text = isController ? controllerHangingInstruction : handsHangingInstruction;
        }

        /// <summary>Called when the puzzle completes to show the correct hanging instruction immediately.</summary>
        public void RefreshHangingInstruction()
        {
            if (hangingInstructionText == null || inputController == null) return;
            _lastControllerMode = inputController.HasValidController;
            hangingInstructionText.text = _lastControllerMode ? controllerHangingInstruction : handsHangingInstruction;
        }

        /// <summary>
        /// Shows a milestone by type. Uses the configured message strings.
        /// </summary>
        public void ShowMilestone(MilestoneType type, int edgeCount = 0)
        {
            string message = GetMessageForType(type, edgeCount);
            ShowMilestoneText(message);
        }

        private string GetMessageForType(MilestoneType type, int edgeCount)
        {
            return type switch
            {
                MilestoneType.Row => rowComplete,
                MilestoneType.Column => columnComplete,
                MilestoneType.RowAndColumn => rowAndColumnComplete,
                MilestoneType.Edge => edgeComplete,
                MilestoneType.Edges => string.Format(edgesCompleteFormat, edgeCount),
                MilestoneType.AllEdges => allEdgesComplete,
                _ => ""
            };
        }

        /// <summary>
        /// Shows a milestone message directly (legacy / custom).
        /// </summary>
        public void ShowMilestoneText(string message, float duration = -1f)
        {
            if (milestoneText == null) return;

            float d = duration > 0 ? duration : milestoneTextDuration;
            milestoneText.text = message;
            var panel = GetMilestonePanel();
            if (panel != null) panel.SetActive(true);

            StopAllCoroutines();
            StartCoroutine(ClearMilestoneAfterDelay(d));
        }

        private GameObject GetMilestonePanel()
        {
            if (milestonePanel != null) return milestonePanel;
            if (milestoneText != null && milestoneText.transform.parent != null)
                return milestoneText.transform.parent.gameObject;
            return milestoneText != null ? milestoneText.gameObject : null;
        }

        private IEnumerator ClearMilestoneAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            var panel = GetMilestonePanel();
            if (panel != null) panel.SetActive(false);
            if (milestoneText != null) milestoneText.text = "";
        }

        /// <summary>
        /// Hides the achievements panel.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Shows the achievements panel and clears any leftover text from a previous session.
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
            ClearMilestoneText();
        }

        /// <summary>
        /// Clears the milestone text field immediately.
        /// </summary>
        public void ClearMilestoneText()
        {
            StopAllCoroutines();
            var panel = GetMilestonePanel();
            if (panel != null)
                panel.SetActive(false);
            if (milestoneText != null)
                milestoneText.text = "";
        }
    }
}
