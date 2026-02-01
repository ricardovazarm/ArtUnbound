using System;
using ArtUnbound.Gameplay;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Controls the in-game HUD during puzzle gameplay.
    /// </summary>
    public class PuzzleHUDController : MonoBehaviour
    {
        public event Action OnPauseRequested;
        public event Action<bool> OnHelpModeToggled;

        [Header("Panel")]
        [SerializeField] private GameObject hudPanel;

        [Header("Timer Display")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Image timerIcon;

        [Header("Progress Display")]
        [SerializeField] private TextMeshProUGUI piecesText;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Image progressFill;

        [Header("Help Mode")]
        [SerializeField] private Toggle helpModeToggle;
        [SerializeField] private TextMeshProUGUI helpModeLabel;
        [SerializeField] private GameObject helpModeIndicator;

        [Header("Buttons")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button repositionButton;

        [Header("Mini Preview")]
        [SerializeField] private GameObject miniPreviewPanel;
        [SerializeField] private Image miniPreviewImage;
        [SerializeField] private Button togglePreviewButton;

        [Header("References")]
        [SerializeField] private PuzzleTimerController timerController;

        [Header("Visual Feedback")]
        [SerializeField] private Color normalProgressColor = new Color(0.2f, 0.6f, 1f);
        [SerializeField] private Color helpModeProgressColor = new Color(1f, 0.8f, 0.2f);

        public bool IsHelpModeEnabled => isHelpModeEnabled;

        private bool isHelpModeEnabled = false;
        private int totalPieces = 0;
        private int placedPieces = 0;
        private bool isMiniPreviewVisible = true;

        private void Awake()
        {
            if (pauseButton != null)
                pauseButton.onClick.AddListener(() => OnPauseRequested?.Invoke());

            if (helpModeToggle != null)
                helpModeToggle.onValueChanged.AddListener(OnHelpModeChanged);

            if (togglePreviewButton != null)
                togglePreviewButton.onClick.AddListener(ToggleMiniPreview);

            if (repositionButton != null)
                repositionButton.onClick.AddListener(OnRepositionRequested);

            Hide();
        }

        private void Update()
        {
            UpdateTimerDisplay();
        }

        /// <summary>
        /// Initializes the HUD for a new puzzle session.
        /// </summary>
        public void Initialize(int pieceCount, bool helpModeDefault = false)
        {
            totalPieces = pieceCount;
            placedPieces = 0;

            SetHelpMode(helpModeDefault);
            UpdateProgressDisplay();

            Show();
        }

        /// <summary>
        /// Updates the pieces placed count.
        /// </summary>
        public void UpdatePiecesPlaced(int placed)
        {
            placedPieces = placed;
            UpdateProgressDisplay();
        }

        /// <summary>
        /// Sets the help mode state.
        /// </summary>
        public void SetHelpMode(bool enabled)
        {
            isHelpModeEnabled = enabled;

            if (helpModeToggle != null)
                helpModeToggle.SetIsOnWithoutNotify(enabled);

            if (helpModeIndicator != null)
                helpModeIndicator.SetActive(enabled);

            if (helpModeLabel != null)
                helpModeLabel.text = enabled ? "Ayuda: ON" : "Ayuda: OFF";

            UpdateProgressColor();
        }

        /// <summary>
        /// Sets the mini preview image.
        /// </summary>
        public void SetPreviewImage(Texture2D texture)
        {
            if (miniPreviewImage != null && texture != null)
            {
                miniPreviewImage.sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );
            }
        }

        /// <summary>
        /// Sets the mini preview image from a sprite.
        /// </summary>
        public void SetPreviewImage(Sprite sprite)
        {
            if (miniPreviewImage != null && sprite != null)
                miniPreviewImage.sprite = sprite;
        }

        /// <summary>
        /// Shows or hides the reposition button (for Comfort Mode).
        /// </summary>
        public void SetRepositionButtonVisible(bool visible)
        {
            if (repositionButton != null)
                repositionButton.gameObject.SetActive(visible);
        }

        private void UpdateTimerDisplay()
        {
            if (timerText != null && timerController != null)
            {
                timerText.text = timerController.GetFormattedTime();
            }
        }

        private void UpdateProgressDisplay()
        {
            if (piecesText != null)
                piecesText.text = $"{placedPieces} / {totalPieces}";

            if (progressSlider != null)
            {
                float progress = totalPieces > 0 ? (float)placedPieces / totalPieces : 0f;
                progressSlider.value = progress;
            }
        }

        private void UpdateProgressColor()
        {
            if (progressFill != null)
            {
                progressFill.color = isHelpModeEnabled ? helpModeProgressColor : normalProgressColor;
            }
        }

        private void OnHelpModeChanged(bool isOn)
        {
            isHelpModeEnabled = isOn;

            if (helpModeIndicator != null)
                helpModeIndicator.SetActive(isOn);

            if (helpModeLabel != null)
                helpModeLabel.text = isOn ? "Ayuda: ON" : "Ayuda: OFF";

            UpdateProgressColor();
            OnHelpModeToggled?.Invoke(isOn);
        }

        private void ToggleMiniPreview()
        {
            isMiniPreviewVisible = !isMiniPreviewVisible;

            if (miniPreviewPanel != null)
                miniPreviewPanel.SetActive(isMiniPreviewVisible);
        }

        /// <summary>
        /// Shows or hides the mini preview.
        /// </summary>
        public void SetMiniPreviewVisible(bool visible)
        {
            isMiniPreviewVisible = visible;

            if (miniPreviewPanel != null)
                miniPreviewPanel.SetActive(visible);
        }

        private void OnRepositionRequested()
        {
            // This will be handled by the game controller
            Debug.Log("Reposition requested");
        }

        /// <summary>
        /// Shows a notification message on the HUD.
        /// </summary>
        public void ShowNotification(string message, float duration = 2f)
        {
            // Could be implemented with a coroutine to show/hide a notification text
            Debug.Log($"HUD Notification: {message}");
        }

        /// <summary>
        /// Highlights the progress when a piece is placed correctly.
        /// </summary>
        public void PulseProgress()
        {
            // Could animate the progress bar or show a visual feedback
        }

        /// <summary>
        /// Sets the timer controller reference.
        /// </summary>
        public void SetTimerController(PuzzleTimerController controller)
        {
            timerController = controller;
        }

        public void Show()
        {
            if (hudPanel != null)
                hudPanel.SetActive(true);
            else
                gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (hudPanel != null)
                hudPanel.SetActive(false);
            else
                gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (pauseButton != null) pauseButton.onClick.RemoveAllListeners();
            if (helpModeToggle != null) helpModeToggle.onValueChanged.RemoveAllListeners();
            if (togglePreviewButton != null) togglePreviewButton.onClick.RemoveAllListeners();
            if (repositionButton != null) repositionButton.onClick.RemoveAllListeners();
        }
    }
}
