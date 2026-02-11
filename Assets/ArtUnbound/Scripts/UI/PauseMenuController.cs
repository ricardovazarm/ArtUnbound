using System;
using ArtUnbound.Gameplay;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Controls the pause menu during puzzle gameplay.
    /// </summary>
    public class PauseMenuController : MonoBehaviour
    {
        public event Action OnResumeRequested;
        public event Action OnQuitRequested;
        public event Action<bool> OnHelpModeToggled;

        [Header("Panels")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject confirmQuitPanel;

        [Header("Pause Panel UI")]
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI piecesText;
        [SerializeField] private Toggle helpModeToggle;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button quitButton;

        [Header("Confirm Quit Panel UI")]
        [SerializeField] private Button confirmQuitButton;
        [SerializeField] private Button cancelQuitButton;

        [Header("References")]
        [SerializeField] private PuzzleTimerController timerController;

        public bool IsPaused => isPaused;

        private bool isPaused = false;
        private int currentPiecesPlaced = 0;
        private int totalPieces = 0;

        private void Awake()
        {
            // Setup button listeners
            if (resumeButton != null)
                resumeButton.onClick.AddListener(Resume);

            if (quitButton != null)
                quitButton.onClick.AddListener(ShowQuitConfirmation);

            if (confirmQuitButton != null)
                confirmQuitButton.onClick.AddListener(ConfirmQuit);

            if (cancelQuitButton != null)
                cancelQuitButton.onClick.AddListener(HideQuitConfirmation);

            if (helpModeToggle != null)
                helpModeToggle.onValueChanged.AddListener(OnHelpModeChanged);

            Hide();
        }

        /// <summary>
        /// Pauses the game and shows the pause menu.
        /// </summary>
        public void Pause()
        {
            if (isPaused) return;

            isPaused = true;

            if (timerController != null)
                timerController.PauseTimer();

            UpdateUI();
            Show();
        }

        /// <summary>
        /// Resumes the game and hides the pause menu.
        /// </summary>
        public void Resume()
        {
            if (!isPaused) return;

            isPaused = false;

            if (timerController != null)
                timerController.ResumeTimer();

            Hide();
            OnResumeRequested?.Invoke();
        }

        /// <summary>
        /// Toggles the pause state.
        /// </summary>
        public void TogglePause()
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }

        /// <summary>
        /// Updates the displayed pieces count.
        /// </summary>
        public void UpdatePiecesCount(int placed, int total)
        {
            currentPiecesPlaced = placed;
            totalPieces = total;

            if (isPaused)
                UpdateUI();
        }

        /// <summary>
        /// Sets the help mode toggle state.
        /// </summary>
        public void SetHelpMode(bool enabled)
        {
            if (helpModeToggle != null)
                helpModeToggle.SetIsOnWithoutNotify(enabled);
        }

        private void UpdateUI()
        {
            if (timeText != null && timerController != null)
                timeText.text = timerController.GetFormattedTime();

            if (piecesText != null)
                piecesText.text = $"{currentPiecesPlaced} / {totalPieces}";
        }

        private void OnHelpModeChanged(bool isOn)
        {
            OnHelpModeToggled?.Invoke(isOn);
        }

        private void ShowQuitConfirmation()
        {
            if (confirmQuitPanel != null)
                confirmQuitPanel.SetActive(true);
        }

        private void HideQuitConfirmation()
        {
            if (confirmQuitPanel != null)
                confirmQuitPanel.SetActive(false);
        }

        private void ConfirmQuit()
        {
            isPaused = false;
            Hide();
            OnQuitRequested?.Invoke();
        }

        public void Show()
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            if (pausePanel != null)
                pausePanel.SetActive(true);

            HideQuitConfirmation();
        }

        public void Hide()
        {
            if (pausePanel != null)
                pausePanel.SetActive(false);
            else
                gameObject.SetActive(false);

            HideQuitConfirmation();
        }

        private void OnDestroy()
        {
            if (resumeButton != null)
                resumeButton.onClick.RemoveListener(Resume);

            if (quitButton != null)
                quitButton.onClick.RemoveListener(ShowQuitConfirmation);

            if (confirmQuitButton != null)
                confirmQuitButton.onClick.RemoveListener(ConfirmQuit);

            if (cancelQuitButton != null)
                cancelQuitButton.onClick.RemoveListener(HideQuitConfirmation);

            if (helpModeToggle != null)
                helpModeToggle.onValueChanged.RemoveListener(OnHelpModeChanged);
        }
    }
}
