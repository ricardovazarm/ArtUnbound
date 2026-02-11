using System;
using System.Collections.Generic;
using ArtUnbound.Data;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Controls the artwork detail panel showing records and options.
    /// </summary>
    public class ArtworkDetailController : MonoBehaviour
    {
        public event Action<int> OnPlayWithPieceCount;
        public event Action OnHangRequested;
        public event Action OnBackRequested;

        [Header("Panel")]
        [SerializeField] private GameObject panel;

        [Header("Artwork Info")]
        [SerializeField] private Image artworkImage;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI artistText;
        [SerializeField] private TextMeshProUGUI descriptionText;

        [Header("Records Container")]
        [SerializeField] private Transform recordsContainer;
        [SerializeField] private GameObject recordItemPrefab;

        [Header("Best Record Display")]
        [SerializeField] private GameObject bestRecordPanel;
        [SerializeField] private TextMeshProUGUI bestScoreText;
        [SerializeField] private TextMeshProUGUI bestTimeText;
        [SerializeField] private Image bestFrameIcon;

        [Header("Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button hangButton;
        [SerializeField] private Button backButton;

        [Header("Piece Count Selection")]
        [SerializeField] private PieceCountSelectorController pieceCountSelector;

        [Header("Frame Icons")]
        [SerializeField] private Sprite maderaIcon;
        [SerializeField] private Sprite bronceIcon;
        [SerializeField] private Sprite plataIcon;
        [SerializeField] private Sprite oroIcon;
        [SerializeField] private Sprite ebanoIcon;

        private string currentArtworkId;
        private ArtworkProgress currentProgress;
        private List<GameObject> instantiatedRecordItems = new List<GameObject>();

        private void Awake()
        {
            if (playButton != null)
                playButton.onClick.AddListener(OnPlayClicked);

            if (hangButton != null)
                hangButton.onClick.AddListener(() => OnHangRequested?.Invoke());

            if (backButton != null)
                backButton.onClick.AddListener(() => OnBackRequested?.Invoke());

            if (pieceCountSelector != null)
                pieceCountSelector.OnCountSelected += OnPieceCountSelected;

            Hide();
        }

        /// <summary>
        /// Shows the detail panel for an artwork.
        /// </summary>
        public void ShowArtworkDetail(string artworkId, ArtworkProgress progress, ArtworkDefinition artworkData = null)
        {
            Debug.Log($"[ArtworkDetailController] ShowArtworkDetail called for {artworkId}. Panel active? {(panel != null ? panel.activeSelf : "Panel is null")}");
            currentArtworkId = artworkId;
            currentProgress = progress;

            UpdateArtworkInfo(artworkData);
            UpdateRecords(progress);
            UpdateBestRecord(progress);
            UpdateButtons(progress);

            Show();
        }

        private void UpdateArtworkInfo(ArtworkDefinition data)
        {
            if (data != null)
            {
                if (titleText != null)
                    titleText.text = data.title;

                if (artistText != null)
                    artistText.text = data.artist;

                if (descriptionText != null)
                    descriptionText.text = data.description;

                if (artworkImage != null && data.thumbnail != null)
                    artworkImage.sprite = data.thumbnail;
            }
            else
            {
                if (titleText != null)
                    titleText.text = currentArtworkId;

                if (artistText != null)
                    artistText.text = "";

                if (descriptionText != null)
                    descriptionText.text = "";
            }
        }

        private void UpdateRecords(ArtworkProgress progress)
        {
            ClearRecordItems();

            if (progress == null || progress.recordsByPieceCount == null) return;

            int[] pieceCounts = { 64, 144, 256, 512 };

            foreach (int pieceCount in pieceCounts)
            {
                var record = progress.GetRecordForPieceCount(pieceCount);
                CreateRecordItem(pieceCount, record);
            }
        }

        private void CreateRecordItem(int pieceCount, ArtworkRecord record)
        {
            if (recordItemPrefab == null || recordsContainer == null) return;

            var item = Instantiate(recordItemPrefab, recordsContainer);
            instantiatedRecordItems.Add(item);

            var titleTMP = item.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
            var scoreTMP = item.transform.Find("Score")?.GetComponent<TextMeshProUGUI>();
            var timeTMP = item.transform.Find("Time")?.GetComponent<TextMeshProUGUI>();
            var frameImg = item.transform.Find("FrameIcon")?.GetComponent<Image>();

            string difficulty = PieceCountSelectorController.GetDifficultyLabel(pieceCount);

            if (titleTMP != null)
                titleTMP.text = $"{pieceCount} piezas ({difficulty})";

            if (record != null && record.isCompleted)
            {
                if (scoreTMP != null)
                    scoreTMP.text = $"Puntuación: {record.bestScore}";

                if (timeTMP != null)
                    timeTMP.text = $"Tiempo: {FormatTime(record.bestTimeSec)}";

                if (frameImg != null)
                {
                    frameImg.gameObject.SetActive(true);
                    frameImg.sprite = GetFrameIcon(record.frameTier);
                }
            }
            else
            {
                if (scoreTMP != null)
                    scoreTMP.text = "Sin completar";

                if (timeTMP != null)
                    timeTMP.text = "";

                if (frameImg != null)
                    frameImg.gameObject.SetActive(false);
            }
        }

        private void ClearRecordItems()
        {
            foreach (var item in instantiatedRecordItems)
            {
                if (item != null)
                    Destroy(item);
            }
            instantiatedRecordItems.Clear();
        }

        private void UpdateBestRecord(ArtworkProgress progress)
        {
            var bestRecord = progress?.GetBestRecord();

            if (bestRecord != null && bestRecord.isCompleted)
            {
                if (bestRecordPanel != null)
                    bestRecordPanel.SetActive(true);

                if (bestScoreText != null)
                    bestScoreText.text = bestRecord.bestScore.ToString();

                if (bestTimeText != null)
                    bestTimeText.text = FormatTime(bestRecord.bestTimeSec);

                if (bestFrameIcon != null)
                    bestFrameIcon.sprite = GetFrameIcon(bestRecord.frameTier);
            }
            else
            {
                if (bestRecordPanel != null)
                    bestRecordPanel.SetActive(false);
            }
        }

        private void UpdateButtons(ArtworkProgress progress)
        {
            bool hasCompletedAny = progress?.GetBestRecord()?.isCompleted ?? false;

            if (hangButton != null)
                hangButton.interactable = hasCompletedAny;
        }

        private void OnPlayClicked()
        {
            if (pieceCountSelector != null)
            {
                Hide();
                pieceCountSelector.ShowSelector("Selecciona dificultad");
            }
            else
            {
                OnPlayWithPieceCount?.Invoke(64);
            }
        }

        private void OnPieceCountSelected(int count)
        {
            OnPlayWithPieceCount?.Invoke(count);
        }

        private string FormatTime(int totalSeconds)
        {
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return $"{minutes:D2}:{seconds:D2}";
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
        /// Sets the artwork image.
        /// </summary>
        public void SetArtworkImage(Texture2D texture)
        {
            if (artworkImage != null && texture != null)
            {
                artworkImage.sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );
            }
        }

        public void Show()
        {
            // Ensure the script holder is active (consistent with ArtworkSelectionController)
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            if (panel != null)
                panel.SetActive(true);
        }

        public void Hide()
        {
            if (panel != null)
                panel.SetActive(false);
            else
                gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (playButton != null) playButton.onClick.RemoveAllListeners();
            if (hangButton != null) hangButton.onClick.RemoveAllListeners();
            if (backButton != null) backButton.onClick.RemoveAllListeners();

            if (pieceCountSelector != null)
                pieceCountSelector.OnCountSelected -= OnPieceCountSelected;

            ClearRecordItems();
        }
    }
}
