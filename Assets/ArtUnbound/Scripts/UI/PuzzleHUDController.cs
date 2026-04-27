using System;
using ArtUnbound.Gameplay;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Controls the in-game HUD during puzzle gameplay (LEFT ZONE).
    /// Shows artwork info, timer, progress, and quit button.
    /// Help mode is always enabled.
    /// </summary>
    public class PuzzleHUDController : MonoBehaviour
    {
        public event Action OnExitRequested;
        public event Action OnHighlightWrongRequested;

        [Header("Panel")]
        [SerializeField] private GameObject hudPanel;

        [Header("Artwork Info")]
        [SerializeField] private TextMeshProUGUI artworkTitleText;
        [SerializeField] private TextMeshProUGUI artworkArtistText;
        [SerializeField] private TextMeshProUGUI artworkDescriptionText;
        [Tooltip("Image in Leftzone showing the artwork as reference while assembling the puzzle.")]
        [SerializeField] private Image artworkReferenceImage;

        [Header("Timer Display")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Image timerIcon;

        [Header("Difficulty Icon")]
        [Tooltip("Icono mostrado segun la dificultad seleccionada (Easy/Normal/Hard).")]
        [SerializeField] private Image difficultyIcon;
        [Tooltip("Sprite para Easy (difficulty-easy).")]
        [SerializeField] private Sprite easyIcon;
        [Tooltip("Sprite para Normal/Medium (difficulty-medium).")]
        [SerializeField] private Sprite mediumIcon;
        [Tooltip("Sprite para Hard (difficulty-hard).")]
        [SerializeField] private Sprite hardIcon;

        [Header("Progress Display")]
        [SerializeField] private TextMeshProUGUI totalPiecesText;
        [SerializeField] private TextMeshProUGUI correctPiecesText;
        [SerializeField] private TextMeshProUGUI incorrectPiecesText;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Image progressFill;

        [Header("Buttons")]
        [SerializeField] private Button quitButton;
        [SerializeField] private Button highlightWrongButton;

        [Header("Music Track Display")]
        [SerializeField] private TextMeshProUGUI musicTrackText;

        [Header("References")]
        [SerializeField] private PuzzleTimerController timerController;

        [Header("Visual Feedback")]
        [SerializeField] private Color progressColor = new Color(0.2f, 0.6f, 1f);

        public bool IsHelpModeEnabled => true;

        private int totalPieces = 0;
        private int placedPieces = 0;
        private int incorrectPieces = 0;

        private void Awake()
        {
            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);

            if (highlightWrongButton != null)
                highlightWrongButton.onClick.AddListener(() => OnHighlightWrongRequested?.Invoke());

            SetupMusicTrackDisplay();
            Hide();
        }

        private void OnEnable()
        {
            if (musicTrackText != null && ArtUnbound.Feedback.AudioManager.Instance != null)
            {
                ArtUnbound.Feedback.AudioManager.Instance.OnTrackChanged += OnMusicTrackChanged;
                var (title, artist) = ArtUnbound.Feedback.AudioManager.Instance.CurrentTrack;
                UpdateMusicTrackText(title, artist);
            }
        }

        private void OnDisable()
        {
            if (ArtUnbound.Feedback.AudioManager.Instance != null)
                ArtUnbound.Feedback.AudioManager.Instance.OnTrackChanged -= OnMusicTrackChanged;
        }

        private void SetupMusicTrackDisplay()
        {
            if (musicTrackText != null && ArtUnbound.Feedback.AudioManager.Instance != null)
            {
                var (title, artist) = ArtUnbound.Feedback.AudioManager.Instance.CurrentTrack;
                UpdateMusicTrackText(title, artist);
            }
        }

        private void OnMusicTrackChanged(string title, string artist)
        {
            UpdateMusicTrackText(title, artist);
        }

        private void UpdateMusicTrackText(string title, string artist)
        {
            if (musicTrackText == null) return;
            if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(artist))
            {
                musicTrackText.text = "";
                return;
            }
            musicTrackText.text = $"{artist ?? ""}-{title ?? ""}";
        }

        private void Update()
        {
            UpdateTimerDisplay();
        }

        /// <summary>
        /// Initializes the HUD for a new puzzle session.
        /// Help mode is always enabled.
        /// </summary>
        public void Initialize(int pieceCount, bool helpModeDefault = false)
        {
            totalPieces = pieceCount;
            placedPieces = 0;
            incorrectPieces = 0;

            // Help mode always enabled - parameter ignored
            UpdateProgressDisplay();

            Show();
        }

        /// <summary>
        /// Updates the pieces placed count (correct only).
        /// </summary>
        public void UpdatePiecesPlaced(int placed)
        {
            placedPieces = placed;
            UpdateProgressDisplay();
        }

        /// <summary>
        /// Updates progress with correct and incorrect counts.
        /// </summary>
        public void UpdateProgress(int correct, int incorrect)
        {
            placedPieces = correct;
            incorrectPieces = incorrect;
            UpdateProgressDisplay();
        }

        /// <summary>
        /// Sets the artwork information (title, artist, description).
        /// </summary>
        public void SetArtworkInfo(string title, string artist, string description)
        {
            if (artworkTitleText != null)
                artworkTitleText.text = title ?? "";

            if (artworkArtistText != null)
                artworkArtistText.text = artist ?? "";

            if (artworkDescriptionText != null)
                artworkDescriptionText.text = description ?? "";
        }

        /// <summary>
        /// Sets the artwork reference image (full image) shown in the Leftzone for puzzle assembly.
        /// </summary>
        public void SetArtworkReference(Sprite sprite)
        {
            if (artworkReferenceImage != null)
            {
                artworkReferenceImage.sprite = sprite;
                artworkReferenceImage.enabled = sprite != null;
            }
        }

        /// <summary>
        /// Sets the difficulty icon based on the selected difficulty index.
        /// 0=Easy, 1=Normal, 2=Hard.
        /// </summary>
        public void SetDifficulty(int difficultyIndex)
        {
            if (difficultyIcon == null) return;
            Sprite sprite = difficultyIndex switch
            {
                0 => easyIcon,
                1 => mediumIcon,
                2 => hardIcon,
                _ => easyIcon
            };
            difficultyIcon.sprite = sprite;
            difficultyIcon.enabled = sprite != null;
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
            if (totalPiecesText != null)
                totalPiecesText.text = totalPieces.ToString();

            if (correctPiecesText != null)
                correctPiecesText.text = placedPieces.ToString();

            if (incorrectPiecesText != null)
                incorrectPiecesText.text = incorrectPieces.ToString();

            if (progressSlider != null)
            {
                float progress = totalPieces > 0 ? (float)placedPieces / totalPieces : 0f;
                progressSlider.value = progress;
            }
            
            // Update progress color
            if (progressFill != null)
            {
                progressFill.color = progressColor;
            }
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
            // Use activeInHierarchy: activeSelf can be true while a parent is inactive,
            // in which case the content would still be invisible.
            if (!gameObject.activeInHierarchy)
                gameObject.SetActive(true);

            if (hudPanel != null)
                hudPanel.SetActive(true);
        }

        public void Hide()
        {
            if (hudPanel != null)
                hudPanel.SetActive(false);
            else
                gameObject.SetActive(false);
        }

        private void OnQuitClicked()
        {
            if (ArtUnbound.Feedback.AudioManager.Instance != null)
                ArtUnbound.Feedback.AudioManager.Instance.PlayButtonClick();
            OnExitRequested?.Invoke();
        }

        private void OnDestroy()
        {
            if (ArtUnbound.Feedback.AudioManager.Instance != null)
                ArtUnbound.Feedback.AudioManager.Instance.OnTrackChanged -= OnMusicTrackChanged;
            if (quitButton != null) 
                quitButton.onClick.RemoveAllListeners();
        }
    }
}
