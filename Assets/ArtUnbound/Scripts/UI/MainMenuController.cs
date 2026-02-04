using System;
using ArtUnbound.Data;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Controls the main menu interface.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        public event Action OnPlayRequested;
        public event Action OnGalleryRequested;
        public event Action OnSettingsRequested;
        // public event Action<GameMode> OnGameModeSelected;
        public event Action<string> OnWeeklyArtworkSelected;

        [Header("Panel")]
        [SerializeField] private GameObject menuPanel;

        [Header("Title")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI subtitleText;

        [Header("Main Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button galleryButton;
        [SerializeField] private Button settingsButton;

        // [Header("Game Mode Selection")]
        // [SerializeField] private GameObject gameModePanel;
        // [SerializeField] private Button galleryModeButton;
        // [SerializeField] private Button comfortModeButton;
        // [SerializeField] private Button cancelModeButton;
        // [SerializeField] private TextMeshProUGUI galleryModeDescription;
        // [SerializeField] private TextMeshProUGUI comfortModeDescription;

        [Header("Weekly Highlight")]
        [SerializeField] private GameObject weeklyHighlightPanel;
        [SerializeField] private Image weeklyArtworkImage;
        [SerializeField] private TextMeshProUGUI weeklyArtworkTitle;
        [SerializeField] private TextMeshProUGUI weeklyArtworkArtist;
        [SerializeField] private Button playWeeklyButton;

        [Header("Stats Display")]
        [SerializeField] private TextMeshProUGUI completedCountText;
        [SerializeField] private TextMeshProUGUI hungCountText;
        [SerializeField] private TextMeshProUGUI totalTimeText;

        private string weeklyArtworkId;
        private SaveData playerData;

        private void Awake()
        {
            if (playButton != null)
                playButton.onClick.AddListener(OnPlayClicked);

            if (galleryButton != null)
                galleryButton.onClick.AddListener(() => OnGalleryRequested?.Invoke());

            if (settingsButton != null)
                settingsButton.onClick.AddListener(() => OnSettingsRequested?.Invoke());

            // GalleryModeListener Removed

            // ComfortModeListener Removed

            // CancelListeners Removed

            if (playWeeklyButton != null)
                playWeeklyButton.onClick.AddListener(OnPlayWeeklyClicked);

            SetupModeDescriptions();
            // HideGameModeSelection(); // Removed
        }

        private void SetupModeDescriptions()
        {
            // Removed
        }

        /// <summary>
        /// Initializes the menu with player data.
        /// </summary>
        public void Initialize(SaveData data)
        {
            playerData = data;
            UpdateStats();
            
            // Default to hidden unless explicitly shown later
            HideWeeklyHighlight();
        }

        /// <summary>
        /// Sets the weekly highlight artwork.
        /// </summary>
        public void SetWeeklyHighlight(string artworkId, ArtworkDefinition artworkData)
        {
            Debug.Log($"[MainMenu] SetWeeklyHighlight called for {artworkId}. Enabling panel.");
            weeklyArtworkId = artworkId;

            if (weeklyHighlightPanel != null)
                weeklyHighlightPanel.SetActive(true);

            if (artworkData != null)
            {
                if (weeklyArtworkTitle != null)
                    weeklyArtworkTitle.text = artworkData.title;

                if (weeklyArtworkArtist != null)
                    weeklyArtworkArtist.text = artworkData.artist;

                if (weeklyArtworkImage != null && artworkData.thumbnail != null)
                    weeklyArtworkImage.sprite = artworkData.thumbnail;
            }
        }

        /// <summary>
        /// Hides the weekly highlight panel.
        /// </summary>
        public void HideWeeklyHighlight()
        {
            Debug.Log("[MainMenu] HideWeeklyHighlight called. Disabling panel.");
            if (weeklyHighlightPanel != null)
                weeklyHighlightPanel.SetActive(false);
        }

        /// <summary>
        /// Updates the stats display.
        /// </summary>
        public void UpdateStats()
        {
            if (playerData == null) return;

            if (completedCountText != null)
            {
                int completed = playerData.GetCompletedArtworks().Count;
                completedCountText.text = completed.ToString();
            }

            if (hungCountText != null)
            {
                int hung = playerData.placedArtworks?.Count ?? 0;
                hungCountText.text = hung.ToString();
            }

            if (totalTimeText != null)
            {
                int totalMinutes = CalculateTotalPlayTime();
                int hours = totalMinutes / 60;
                int minutes = totalMinutes % 60;
                totalTimeText.text = hours > 0 ? $"{hours}h {minutes}m" : $"{minutes}m";
            }
        }

        private int CalculateTotalPlayTime()
        {
            if (playerData?.artworkProgress == null) return 0;

            int totalSeconds = 0;
            foreach (var progress in playerData.artworkProgress)
            {
                var bestRecord = progress.GetBestRecord();
                if (bestRecord != null && bestRecord.isCompleted)
                {
                    totalSeconds += bestRecord.bestTimeSec;
                }
            }

            return totalSeconds / 60;
        }

        private void OnPlayClicked()
        {
            // Direct play request without mode selection
            OnPlayRequested?.Invoke();
        }

        // private void ShowGameModeSelection() { }
        // private void HideGameModeSelection() { }
        // private void SelectGameMode(GameMode mode) { }

        private void OnPlayWeeklyClicked()
        {
            if (!string.IsNullOrEmpty(weeklyArtworkId))
            {
                OnWeeklyArtworkSelected?.Invoke(weeklyArtworkId);
            }
        }

        /// <summary>
        /// Sets the title text.
        /// </summary>
        public void SetTitle(string title, string subtitle = null)
        {
            if (titleText != null)
                titleText.text = title;

            if (subtitleText != null)
            {
                subtitleText.text = subtitle ?? "";
                subtitleText.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));
            }
        }

        public void Show()
        {
            if (menuPanel != null)
                menuPanel.SetActive(true);
            else
                gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (menuPanel != null)
                menuPanel.SetActive(false);
            else
                gameObject.SetActive(false);

            // HideGameModeSelection();
        }

        private void OnDestroy()
        {
            if (playButton != null) playButton.onClick.RemoveAllListeners();
            if (galleryButton != null) galleryButton.onClick.RemoveAllListeners();
            if (settingsButton != null) settingsButton.onClick.RemoveAllListeners();
            // Remove listeners for removed buttons
            if (playWeeklyButton != null) playWeeklyButton.onClick.RemoveAllListeners();
        }
    }
}
