using System;
using ArtUnbound.Data;
using ArtUnbound.Gameplay;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Controls the post-game results screen.
    /// </summary>
    public class PostGameController : MonoBehaviour
    {
        public event Action OnPlaceArtworkRequested;
        public event Action OnReplayRequested;
        public event Action OnReturnToMenuRequested;

        [Header("UI References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI piecesText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI frameTierText;
        [SerializeField] private Image artworkPreview;
        [SerializeField] private Image frameIcon;
        [SerializeField] private GameObject newRecordIndicator;

        [Header("Buttons")]
        [SerializeField] private Button placeButton;
        [SerializeField] private Button replayButton;
        [SerializeField] private Button menuButton;

        [Header("Frame Icons")]
        [SerializeField] private Sprite maderaIcon;
        [SerializeField] private Sprite bronceIcon;
        [SerializeField] private Sprite plataIcon;
        [SerializeField] private Sprite oroIcon;
        [SerializeField] private Sprite ebanoIcon;

        private PuzzleSessionData sessionData;
        private int finalScore;
        private FrameTier awardedFrame;
        private bool isNewRecord;

        private void Awake()
        {
            if (placeButton != null)
                placeButton.onClick.AddListener(OnPlaceArtworkClicked);

            if (replayButton != null)
                replayButton.onClick.AddListener(OnReplayClicked);

            if (menuButton != null)
                menuButton.onClick.AddListener(OnMenuClicked);

            Hide();
        }

        /// <summary>
        /// Shows the results screen with the given data.
        /// </summary>
        public void ShowResults(PuzzleSessionData data, int score, FrameTier frame, bool newRecord = false)
        {
            sessionData = data;
            finalScore = score;
            awardedFrame = frame;
            isNewRecord = newRecord;

            UpdateUI();
            Show();
        }

        private void UpdateUI()
        {
            if (titleText != null)
                titleText.text = "¡Puzzle Completado!";

            if (timeText != null)
                timeText.text = FormatTime(sessionData.GetElapsedSeconds());

            if (piecesText != null)
            {
                string difficulty = PieceCountSelectorController.GetDifficultyLabel(sessionData.pieceCount);
                piecesText.text = $"Dificultad: {difficulty}";
            }

            if (scoreText != null)
                scoreText.text = finalScore.ToString();

            if (frameTierText != null)
                frameTierText.text = GetFrameTierName(awardedFrame);

            if (frameIcon != null)
                frameIcon.sprite = GetFrameIcon(awardedFrame);

            if (newRecordIndicator != null)
                newRecordIndicator.SetActive(isNewRecord);
        }

        private string FormatTime(int totalSeconds)
        {
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return $"{minutes:D2}:{seconds:D2}";
        }

        private string GetFrameTierName(FrameTier tier)
        {
            return tier switch
            {
                FrameTier.Madera => "Marco de Madera",
                FrameTier.Bronce => "Marco de Bronce",
                FrameTier.Plata => "Marco de Plata",
                FrameTier.Oro => "Marco de Oro",
                FrameTier.Ebano => "Marco de Ébano",
                _ => "Marco"
            };
        }

        private Sprite GetFrameIcon(FrameTier tier)
        {
            return tier switch
            {
                FrameTier.Madera => maderaIcon,
                FrameTier.Bronce => bronceIcon,
                FrameTier.Plata => plataIcon,
                FrameTier.Oro => oroIcon,
                FrameTier.Ebano => ebanoIcon,
                _ => maderaIcon
            };
        }

        /// <summary>
        /// Sets the artwork preview image.
        /// </summary>
        public void SetArtworkPreview(Texture2D texture)
        {
            if (artworkPreview != null && texture != null)
            {
                artworkPreview.sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );
            }
        }

        public void Show()
        {
            if (panel != null)
                panel.SetActive(true);
            else
                gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (panel != null)
                panel.SetActive(false);
            else
                gameObject.SetActive(false);
        }

        private void OnPlaceArtworkClicked()
        {
            OnPlaceArtworkRequested?.Invoke();
            Hide();
        }

        private void OnReplayClicked()
        {
            OnReplayRequested?.Invoke();
            Hide();
        }

        private void OnMenuClicked()
        {
            OnReturnToMenuRequested?.Invoke();
            Hide();
        }

        /// <summary>
        /// Gets the current session data.
        /// </summary>
        public PuzzleSessionData GetSessionData() => sessionData;

        /// <summary>
        /// Gets the final score.
        /// </summary>
        public int GetFinalScore() => finalScore;

        /// <summary>
        /// Gets the awarded frame tier.
        /// </summary>
        public FrameTier GetAwardedFrame() => awardedFrame;

        private void OnDestroy()
        {
            if (placeButton != null)
                placeButton.onClick.RemoveListener(OnPlaceArtworkClicked);

            if (replayButton != null)
                replayButton.onClick.RemoveListener(OnReplayClicked);

            if (menuButton != null)
                menuButton.onClick.RemoveListener(OnMenuClicked);
        }
    }
}
