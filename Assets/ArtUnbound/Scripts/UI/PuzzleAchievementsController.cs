using System.Collections;
using UnityEngine;
using TMPro;

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
        [SerializeField] private float milestoneTextDuration = 2f;

        [Header("Message Strings (edit for localization)")]
        [SerializeField] private string rowComplete = "¡Fila completa!";
        [SerializeField] private string columnComplete = "¡Columna completa!";
        [SerializeField] private string rowAndColumnComplete = "¡Fila y Columna completas!";
        [SerializeField] private string edgeComplete = "¡Borde completo!";
        [Tooltip("{0} = count. E.g. '¡{0} bordes completos!'")]
        [SerializeField] private string edgesCompleteFormat = "¡{0} bordes completos!";
        [SerializeField] private string allEdgesComplete = "¡Marco completo!";

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
            milestoneText.gameObject.SetActive(true);

            StopAllCoroutines();
            StartCoroutine(ClearMilestoneAfterDelay(d));
        }

        private IEnumerator ClearMilestoneAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (milestoneText != null)
                milestoneText.text = "";
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
            if (milestoneText != null)
                milestoneText.text = "";
        }
    }
}
