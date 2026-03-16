using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Controls the PiecesPanel shown when assembling a puzzle.
    /// Shows the panel with pieces tray and pagination bar when puzzle starts,
    /// hides when puzzle completes.
    /// </summary>
    public class PiecesPanelController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private PieceScrollController scrollController;
        [SerializeField] private Button scrollUpButton;
        [SerializeField] private Button scrollDownButton;
        [SerializeField] private TextMeshProUGUI pageText;

        private void Awake()
        {
            if (scrollController == null)
                scrollController = GetComponentInChildren<PieceScrollController>();

            if (scrollUpButton != null)
                scrollUpButton.onClick.AddListener(OnScrollUpClicked);

            if (scrollDownButton != null)
                scrollDownButton.onClick.AddListener(OnScrollDownClicked);

            Hide();
        }

        private void OnDestroy()
        {
            if (scrollUpButton != null)
                scrollUpButton.onClick.RemoveAllListeners();
            if (scrollDownButton != null)
                scrollDownButton.onClick.RemoveAllListeners();
        }

        private void OnScrollUpClicked()
        {
            if (ArtUnbound.Feedback.AudioManager.Instance != null)
                ArtUnbound.Feedback.AudioManager.Instance.PlayButtonClick();
            if (scrollController != null)
                scrollController.ScrollUp();
        }

        private void OnScrollDownClicked()
        {
            if (ArtUnbound.Feedback.AudioManager.Instance != null)
                ArtUnbound.Feedback.AudioManager.Instance.PlayButtonClick();
            if (scrollController != null)
                scrollController.ScrollDown();
        }

        /// <summary>
        /// Called by PieceScrollController to show/hide pagination buttons.
        /// </summary>
        public void SetPaginationButtonStates(bool canScrollUp, bool canScrollDown)
        {
            if (scrollUpButton != null)
                scrollUpButton.gameObject.SetActive(canScrollUp);
            if (scrollDownButton != null)
                scrollDownButton.gameObject.SetActive(canScrollDown);
        }

        /// <summary>
        /// Show the pieces panel (when puzzle starts).
        /// </summary>
        public void Show()
        {
            if (panel != null)
                panel.SetActive(true);
            else
                gameObject.SetActive(true);
        }

        /// <summary>
        /// Hide the pieces panel (when puzzle completes).
        /// </summary>
        public void Hide()
        {
            if (panel != null)
                panel.SetActive(false);
            else
                gameObject.SetActive(false);
        }
    }
}
