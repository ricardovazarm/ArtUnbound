using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ArtUnbound.Data;
using ArtUnbound.Services;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Unified main menu controller that combines artwork selection, gallery, and detail view.
    /// Designed for MR with a curved panel layout.
    /// Runs before GameBootstrap so StatefulButton markers are added before ApplyButtonTheme.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class UnifiedMainMenuController : MonoBehaviour
    {
        #region Events
        public event Action<string, int, int> OnStartPuzzle; // artworkId, pieceCount, difficultyIndex (0=Easy, 1=Normal, 2=Hard, 3=Expert)
        public event Action<float> OnMusicVolumeChanged;
        public event Action<float> OnSoundVolumeChanged;
        public event Action<bool> OnTutorialToggled;
        #endregion

        #region Serialized Fields - Main Panel
        [Header("Main Panel")]
        [SerializeField] private GameObject mainPanel;
        
        [Header("Left Zone - Configuration")]
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider soundVolumeSlider;
        [SerializeField] private TextMeshProUGUI musicTrackText;
        [SerializeField] private Toggle tutorialToggle;
        [SerializeField] private TextMeshProUGUI globalStatsText; // "12/24 obras completadas"

        [Header("Center Zone - Catalog")]
        [SerializeField] private GameObject catalogGrid; // Container with Grid Layout Group (3x3)
        [SerializeField] private Button catalogPageLeftButton;
        [SerializeField] private Button catalogPageRightButton;
        [SerializeField] private TextMeshProUGUI catalogPageText;
        [SerializeField] private Button filterAllButton;
        [SerializeField] private Button filterInProgressButton;
        [SerializeField] private Button filterCompletedButton;
        [SerializeField] private GameObject artworkCardPrefab; // Prefab for artwork thumbnail cards

        [Header("Right Zone - Detail Panel")]
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private Image detailArtworkImage;
        [SerializeField] private Image detailFrameImage;
        [SerializeField] private TextMeshProUGUI detailTitleText;
        [SerializeField] private TextMeshProUGUI detailArtistText;
        [SerializeField] private TextMeshProUGUI detailDescriptionText;
        [SerializeField] private Button easyButton;
        [SerializeField] private Button normalButton;
        [SerializeField] private Button hardButton;
        [SerializeField] private Button expertButton;
        [SerializeField] private TextMeshProUGUI easyButtonText;
        [SerializeField] private TextMeshProUGUI normalButtonText;
        [SerializeField] private TextMeshProUGUI hardButtonText;
        [SerializeField] private TextMeshProUGUI expertButtonText;
        [Tooltip("Progress sliders - show % when puzzle has progress. Leave null to hide.")]
        [SerializeField] private Slider easyProgressSlider;
        [SerializeField] private Slider normalProgressSlider;
        [SerializeField] private Slider hardProgressSlider;
        [SerializeField] private Slider expertProgressSlider;

        [Header("Configuration")]
        [SerializeField] private PuzzleConfig puzzleConfig; // Reference to PuzzleConfig for piece counts

        [Header("Frame Sprites (Detail Panel)")]
        [SerializeField] private Sprite frameMadera;
        [SerializeField] private Sprite frameBronce;
        [SerializeField] private Sprite framePlata;
        [SerializeField] private Sprite frameOro;
        [SerializeField] private Sprite frameEbano;
        #endregion

        #region Private Fields
        private LocalCatalogService catalogService;
        private SaveDataService saveDataService;
        private SaveData saveData;
        
        private List<ArtworkDefinition> allArtworks = new List<ArtworkDefinition>();
        private List<ArtworkDefinition> filteredArtworks = new List<ArtworkDefinition>();
        private List<GameObject> artworkCards = new List<GameObject>();
        private ArtworkDefinition selectedArtwork;
        
        private FilterType currentFilter = FilterType.All;
        private int currentPage;
        private const int Columns = 3;
        private const int Rows = 3;
        private const int ItemsPerPage = Columns * Rows; // 9
        
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            MarkStatefulButtons();
            SetupButtonListeners();
            SetupMusicTrackDisplay();
            SetupDetailImageSlot();
        }

        private void MarkStatefulButtons()
        {
            void AddIfMissing(Button b) { if (b != null && b.GetComponent<StatefulButton>() == null) b.gameObject.AddComponent<StatefulButton>(); }
            AddIfMissing(filterAllButton);
            AddIfMissing(filterInProgressButton);
            AddIfMissing(filterCompletedButton);
            AddIfMissing(easyButton);
            AddIfMissing(normalButton);
            AddIfMissing(hardButton);
            AddIfMissing(expertButton);
        }

        /// <summary>
        /// Creates a constraining slot for DetailImageContainer so AspectRatioFitter can adjust
        /// the frame to the image aspect ratio without expanding beyond the allotted space.
        /// </summary>
        private void SetupDetailImageSlot()
        {
            if (detailArtworkImage == null || detailPanel == null) return;

            var container = detailArtworkImage.transform.parent;
            if (container == null) return;

            // Already set up (e.g. from previous run in editor)
            if (container.parent != null && container.parent.name == "DetailImageSlot")
                return;

            var rt = container.GetComponent<RectTransform>();
            if (rt == null) return;

            // Create slot with same position/size as current container (max 300x300)
            var slot = new GameObject("DetailImageSlot");
            slot.transform.SetParent(detailPanel.transform, false);
            var slotRT = slot.AddComponent<RectTransform>();

            slotRT.anchorMin = rt.anchorMin;
            slotRT.anchorMax = rt.anchorMax;
            slotRT.anchoredPosition = rt.anchoredPosition;
            slotRT.sizeDelta = rt.sizeDelta;
            slotRT.pivot = rt.pivot;
            slotRT.localScale = Vector3.one;

            slot.transform.SetSiblingIndex(container.GetSiblingIndex());

            // Reparent container into slot; it will stretch to fill
            container.SetParent(slot.transform, false);
            var containerRT = container.GetComponent<RectTransform>();
            containerRT.anchorMin = Vector2.zero;
            containerRT.anchorMax = Vector2.one;
            containerRT.offsetMin = Vector2.zero;
            containerRT.offsetMax = Vector2.zero;
        }

        private void OnDestroy()
        {
            RemoveButtonListeners();
            if (ArtUnbound.Feedback.AudioManager.Instance != null)
                ArtUnbound.Feedback.AudioManager.Instance.OnTrackChanged -= OnMusicTrackChanged;
        }

        private void SetupMusicTrackDisplay()
        {
            if (musicTrackText == null || ArtUnbound.Feedback.AudioManager.Instance == null) return;
            ArtUnbound.Feedback.AudioManager.Instance.OnTrackChanged += OnMusicTrackChanged;
            var (title, artist) = ArtUnbound.Feedback.AudioManager.Instance.CurrentTrack;
            UpdateMusicTrackText(title, artist);
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
            musicTrackText.text = $"Song: {title ?? ""} - {artist ?? ""}";
        }
        #endregion

        #region Initialization
        /// <summary>
        /// Gets piece count for difficulty index (0=Easy, 1=Normal, 2=Hard, 3=Expert).
        /// Uses artwork's piece counts when set, else PuzzleConfig defaults.
        /// </summary>
        private int GetPieceCountForDifficulty(int index)
        {
            if (selectedArtwork != null && selectedArtwork.GetPieceCount(index) > 0)
                return selectedArtwork.GetPieceCount(index);
            if (puzzleConfig != null && puzzleConfig.pieceCounts.Length > index)
                return puzzleConfig.pieceCounts[index];
            return new[] { 64, 121, 196, 289 }[index];
        }

        /// <summary>
        /// Initializes the menu with services and data.
        /// </summary>
        public void Initialize(LocalCatalogService catalogService, SaveDataService saveDataService)
        {
            this.catalogService = catalogService;
            this.saveDataService = saveDataService;
            this.saveData = saveDataService.GetCachedData();

            LoadArtworks();
            UpdateGlobalStats();
            LoadLastPlayedArtwork();
            ApplyFilter(FilterType.All);
            
            // Load saved settings
            LoadSettings();
        }

        private void SetupButtonListeners()
        {
            // Filter buttons
            if (filterAllButton != null)
                filterAllButton.onClick.AddListener(() => { PlayButtonClick(); ApplyFilter(FilterType.All); });
            
            if (filterInProgressButton != null)
                filterInProgressButton.onClick.AddListener(() => { PlayButtonClick(); ApplyFilter(FilterType.InProgress); });
            
            if (filterCompletedButton != null)
                filterCompletedButton.onClick.AddListener(() => { PlayButtonClick(); ApplyFilter(FilterType.Completed); });

            if (catalogPageLeftButton != null)
                catalogPageLeftButton.onClick.AddListener(GoToPrevPage);
            if (catalogPageRightButton != null)
                catalogPageRightButton.onClick.AddListener(GoToNextPage);

            // Difficulty buttons - use artwork's piece counts when set, else PuzzleConfig
            if (easyButton != null)
                easyButton.onClick.AddListener(() => { var pc = GetPieceCountForDifficulty(0); if (pc > 0) { PlayButtonClick(); StartPuzzle(pc, 0); } });
            if (normalButton != null)
                normalButton.onClick.AddListener(() => { var pc = GetPieceCountForDifficulty(1); if (pc > 0) { PlayButtonClick(); StartPuzzle(pc, 1); } });
            if (hardButton != null)
                hardButton.onClick.AddListener(() => { var pc = GetPieceCountForDifficulty(2); if (pc > 0) { PlayButtonClick(); StartPuzzle(pc, 2); } });
            if (expertButton != null)
                expertButton.onClick.AddListener(() => { var pc = GetPieceCountForDifficulty(3); if (pc > 0) { PlayButtonClick(); StartPuzzle(pc, 3); } });

            // Settings controls
            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeSliderChanged);
            
            if (soundVolumeSlider != null)
                soundVolumeSlider.onValueChanged.AddListener(OnSoundVolumeSliderChanged);
            
            if (tutorialToggle != null)
                tutorialToggle.onValueChanged.AddListener(OnTutorialToggleChanged);
        }

        private void RemoveButtonListeners()
        {
            if (filterAllButton != null)
                filterAllButton.onClick.RemoveAllListeners();
            
            if (filterInProgressButton != null)
                filterInProgressButton.onClick.RemoveAllListeners();
            
            if (filterCompletedButton != null)
                filterCompletedButton.onClick.RemoveAllListeners();

            if (catalogPageLeftButton != null)
                catalogPageLeftButton.onClick.RemoveAllListeners();
            if (catalogPageRightButton != null)
                catalogPageRightButton.onClick.RemoveAllListeners();

            if (easyButton != null)
                easyButton.onClick.RemoveAllListeners();
            
            if (normalButton != null)
                normalButton.onClick.RemoveAllListeners();
            
            if (hardButton != null)
                hardButton.onClick.RemoveAllListeners();
            
            if (expertButton != null)
                expertButton.onClick.RemoveAllListeners();

            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.RemoveAllListeners();
            
            if (soundVolumeSlider != null)
                soundVolumeSlider.onValueChanged.RemoveAllListeners();
            
            if (tutorialToggle != null)
                tutorialToggle.onValueChanged.RemoveAllListeners();
        }
        #endregion

        #region Artwork Loading & Filtering
        private void LoadArtworks()
        {
            if (catalogService == null)
            {
                Debug.LogError("[UnifiedMainMenu] CatalogService is null!");
                return;
            }

            allArtworks = catalogService.GetAll();
            Debug.Log($"[UnifiedMainMenu] Loaded {allArtworks.Count} artworks");
        }

        private void ApplyFilter(FilterType filter)
        {
            currentFilter = filter;
            UpdateFilterButtonVisuals();
            
            filteredArtworks = GetFilteredArtworks();
            currentPage = 0;
            RefreshCatalogPage();
        }

        private List<ArtworkDefinition> GetFilteredArtworks()
        {
            switch (currentFilter)
            {
                case FilterType.All:
                    return allArtworks;
                
                case FilterType.InProgress:
                    return allArtworks.Where(artwork => HasInProgressSession(artwork.artworkId)).ToList();
                
                case FilterType.Completed:
                    return allArtworks.Where(artwork => IsArtworkCompleted(artwork.artworkId)).ToList();
                
                default:
                    return allArtworks;
            }
        }

        private bool HasInProgressSession(string artworkId)
        {
            if (saveData == null) return false;
            var sessions = saveData.GetInProgressSessionsForArtwork(artworkId);
            return sessions != null && sessions.Count > 0;
        }

        private bool IsArtworkCompleted(string artworkId)
        {
            if (saveData == null) return false;
            
            var progress = saveData.GetProgress(artworkId);
            return progress != null && progress.HasBeenCompleted();
        }

        private void UpdateFilterButtonVisuals()
        {
            UIButtonStatefullTheme.ApplyTo(filterAllButton, currentFilter == FilterType.All);
            UIButtonStatefullTheme.ApplyTo(filterInProgressButton, currentFilter == FilterType.InProgress);
            UIButtonStatefullTheme.ApplyTo(filterCompletedButton, currentFilter == FilterType.Completed);
        }
        #endregion

        #region Catalog Grid Population (Pagination 3x3)
        private void RefreshCatalogPage()
        {
            // Clear existing cards
            foreach (var card in artworkCards)
            {
                if (card != null)
                    Destroy(card);
            }
            artworkCards.Clear();

            if (catalogGrid == null || artworkCardPrefab == null)
            {
                Debug.LogError("[UnifiedMainMenu] CatalogGrid or ArtworkCardPrefab is NULL!");
                return;
            }

            int totalItems = filteredArtworks.Count;
            int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)totalItems / ItemsPerPage));
            currentPage = Mathf.Clamp(currentPage, 0, totalPages - 1);

            int startIndex = currentPage * ItemsPerPage;
            int count = Mathf.Min(ItemsPerPage, totalItems - startIndex);

            for (int i = 0; i < count; i++)
            {
                var artwork = filteredArtworks[startIndex + i];
                GameObject card = Instantiate(artworkCardPrefab, catalogGrid.transform);
                SetupArtworkCard(card, artwork);
                artworkCards.Add(card);
            }

            // Force layout rebuild so Content gets correct size from GridLayoutGroup
            if (catalogGrid != null)
            {
                Canvas.ForceUpdateCanvases();
                var catalogRt = catalogGrid.GetComponent<RectTransform>();
                if (catalogRt != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(catalogRt);
            }

            UpdatePageIndicator(totalPages);
            UpdatePageButtons(totalPages);
        }

        private void GoToPrevPage()
        {
            if (currentPage > 0)
            {
                PlayButtonClick();
                currentPage--;
                RefreshCatalogPage();
            }
        }

        private void GoToNextPage()
        {
            int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)filteredArtworks.Count / ItemsPerPage));
            if (currentPage < totalPages - 1)
            {
                PlayButtonClick();
                currentPage++;
                RefreshCatalogPage();
            }
        }

        private void UpdatePageIndicator(int totalPages)
        {
            if (catalogPageText == null) return;
            catalogPageText.text = totalPages <= 1 ? "" : $"{currentPage + 1} / {totalPages}";
        }

        private void UpdatePageButtons(int totalPages)
        {
            // Siempre visibles; deshabilitados cuando no aplican (Prev en pág 1, Next en última)
            if (catalogPageLeftButton != null)
            {
                catalogPageLeftButton.gameObject.SetActive(true);
                catalogPageLeftButton.interactable = currentPage > 0;
            }
            if (catalogPageRightButton != null)
            {
                catalogPageRightButton.gameObject.SetActive(true);
                catalogPageRightButton.interactable = currentPage < totalPages - 1 && totalPages > 1;
            }
        }

        private void SetupArtworkCard(GameObject card, ArtworkDefinition artwork)
        {
            // Get ArtworkCard component
            var artworkCard = card.GetComponent<ArtworkCard>();
            if (artworkCard == null)
            {
                Debug.LogError($"[UnifiedMainMenu] ArtworkCard component missing on prefab!");
                return;
            }

            // Set thumbnail image
            if (artworkCard.ThumbnailImage != null && artwork.thumbnail != null)
            {
                artworkCard.ThumbnailImage.sprite = artwork.thumbnail;

                // Match frame to image aspect ratio: ThumbContainer uses AspectRatioFitter
                var thumbContainer = artworkCard.ThumbnailImage.transform.parent;
                if (thumbContainer != null)
                {
                    var fitter = thumbContainer.GetComponent<AspectRatioFitter>();
                    if (fitter == null)
                        fitter = thumbContainer.gameObject.AddComponent<AspectRatioFitter>();
                    fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                    var r = artwork.thumbnail.rect;
                    fitter.aspectRatio = r.width / Mathf.Max(r.height, 0.001f);
                }
            }

            // Set title text
            if (artworkCard.TitleText != null)
            {
                artworkCard.TitleText.text = artwork.title;
            }

            // Add click listener — pass the card transform so SelectArtwork can fire the trail
            var button = card.GetComponent<Button>();
            if (button != null)
            {
                var cardTransform = card.transform;
                button.onClick.AddListener(() => SelectArtwork(artwork, cardTransform));
            }

            // Show progress percentage
            float progressPercent = GetArtworkProgress(artwork.artworkId);
            if (artworkCard.ProgressText != null)
            {
                if (progressPercent > 0 && progressPercent < 100)
                {
                    artworkCard.ProgressText.text = $"{progressPercent:F0}%";
                    artworkCard.ProgressText.gameObject.SetActive(true);
                }
                else
                {
                    artworkCard.ProgressText.gameObject.SetActive(false);
                }
            }

            var progress = saveData?.GetProgress(artwork.artworkId);
            bool isCompleted = progress != null && progress.HasBeenCompleted();
            var frameTier = (progress != null && progress.HasBeenCompleted()) ? progress.bestFrameTier : FrameTier.Madera;
            artworkCard.SetFrameTier(frameTier);
            Debug.Log($"[UnifiedMainMenu] Setup card: {artwork.title}, Completed: {isCompleted}, Frame: {frameTier}, Progress: {progressPercent:F0}%");
        }

        private float GetArtworkProgress(string artworkId)
        {
            if (saveData == null) return 0f;
            var sessions = saveData.GetInProgressSessionsForArtwork(artworkId);
            if (sessions == null || sessions.Count == 0) return 0f;
            // Use session with most pieces placed
            var best = sessions.OrderByDescending(s => s.piecesPlaced).FirstOrDefault();
            if (best != null && best.pieceCount > 0)
                return (best.piecesPlaced / (float)best.pieceCount) * 100f;
            return 0f;
        }

        #endregion

        #region Artwork Selection & Detail
        private void SelectArtwork(ArtworkDefinition artwork, Transform cardTransform = null)
        {
            PlayButtonClick();
            selectedArtwork = artwork;
            UpdateDetailPanel();

            // Fire attention trail from the selected card to the detail panel
            if (cardTransform != null && detailPanel != null &&
                ArtUnbound.Feedback.SelectionTrailController.Instance != null)
            {
                Vector3 origin = cardTransform.position;
                Vector3 destination = detailPanel.transform.position;
                ArtUnbound.Feedback.SelectionTrailController.Instance.PlayTrail(origin, destination);
            }

            Debug.Log($"[UnifiedMainMenu] Selected artwork: {artwork.title}");
        }

        private void LoadLastPlayedArtwork()
        {
            if (saveData == null || string.IsNullOrEmpty(saveData.lastArtworkId))
            {
                // Select first artwork by default
                if (allArtworks.Count > 0)
                {
                    SelectArtwork(allArtworks[0]);
                }
                return;
            }

            var lastArtwork = catalogService.GetById(saveData.lastArtworkId);
            if (lastArtwork != null)
            {
                SelectArtwork(lastArtwork);
            }
            else if (allArtworks.Count > 0)
            {
                SelectArtwork(allArtworks[0]);
            }
        }

        private void UpdateDetailPanel()
        {
            if (selectedArtwork == null || detailPanel == null)
                return;

            // Show detail panel
            detailPanel.SetActive(true);

            // Set detail frame tier (bronze, silver, etc. when completed; wood when not)
            var progress = saveData?.GetProgress(selectedArtwork.artworkId);
            var frameTier = (progress != null && progress.HasBeenCompleted()) ? progress.bestFrameTier : FrameTier.Madera;
            SetDetailFrameTier(frameTier);

            // Set artwork image and adjust frame (DetailImageContainer) to image aspect ratio.
            // DetailImageContainer is inside DetailImageSlot (300x300 max), so AspectRatioFitter
            // fits within that slot without overlapping the titles below.
            if (detailArtworkImage != null && selectedArtwork.fullImage != null)
            {
                detailArtworkImage.sprite = selectedArtwork.fullImage;
                var detailContainer = detailArtworkImage.transform.parent;
                if (detailContainer != null)
                {
                    var fitter = detailContainer.GetComponent<AspectRatioFitter>();
                    if (fitter == null)
                        fitter = detailContainer.gameObject.AddComponent<AspectRatioFitter>();
                    fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                    var r = selectedArtwork.fullImage.rect;
                    fitter.aspectRatio = r.width / Mathf.Max(r.height, 0.001f);
                }
            }

            // Set title
            if (detailTitleText != null)
            {
                detailTitleText.text = selectedArtwork.title;
            }

            // Set artist
            if (detailArtistText != null)
            {
                detailArtistText.text = selectedArtwork.author;
            }

            // Set description
            if (detailDescriptionText != null)
            {
                detailDescriptionText.text = selectedArtwork.description;
            }

            // Update difficulty buttons
            UpdateDifficultyButtons();
        }

        private void SetDetailFrameTier(FrameTier tier)
        {
            var frameImg = detailFrameImage;
            if (frameImg == null && detailPanel != null)
            {
                foreach (var t in detailPanel.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "FrameDetail")
                    {
                        frameImg = t.GetComponent<Image>();
                        break;
                    }
                }
            }
            if (frameImg == null) return;

            Sprite sprite = GetDetailFrameSprite(tier);
            if (sprite != null)
            {
                frameImg.gameObject.SetActive(true);
                frameImg.sprite = sprite;
            }
        }

        private Sprite GetDetailFrameSprite(FrameTier tier)
        {
            return tier switch
            {
                FrameTier.Madera => frameMadera,
                FrameTier.Bronce => frameBronce,
                FrameTier.Plata => framePlata,
                FrameTier.Oro => frameOro,
                FrameTier.Platinum => frameEbano,
                _ => frameMadera
            };
        }

        private void UpdateDifficultyButtons()
        {
            UpdateDifficultyButton(easyButton, easyButtonText, easyProgressSlider, GetPieceCountForDifficulty(0));
            UpdateDifficultyButton(normalButton, normalButtonText, normalProgressSlider, GetPieceCountForDifficulty(1));
            UpdateDifficultyButton(hardButton, hardButtonText, hardProgressSlider, GetPieceCountForDifficulty(2));
            UpdateDifficultyButton(expertButton, expertButtonText, expertProgressSlider, GetPieceCountForDifficulty(3));
        }

        private void UpdateDifficultyButton(Button button, TextMeshProUGUI buttonText, Slider progressSlider, int pieceCount)
        {
            if (button == null || buttonText == null || selectedArtwork == null)
                return;

            string label = $"{pieceCount} Pieces";
            var session = saveData?.GetInProgressSession(selectedArtwork.artworkId, pieceCount);
            bool hasProgress = session != null && session.piecesPlaced > 0;
            bool isLastPlayed = saveData != null &&
                selectedArtwork.artworkId == saveData.lastArtworkId &&
                pieceCount == saveData.lastPieceCount;
            bool isSelected = hasProgress || isLastPlayed;

            buttonText.text = label;
            UIButtonStatefullTheme.ApplyTo(button, isSelected);

            // Slider: show only when there's progress, set value to 0–1 (read-only progress bar)
            if (progressSlider != null)
            {
                progressSlider.gameObject.SetActive(hasProgress);
                if (hasProgress && session != null && session.pieceCount > 0)
                {
                    progressSlider.minValue = 0f;
                    progressSlider.maxValue = 1f;
                    progressSlider.value = session.piecesPlaced / (float)session.pieceCount;
                    progressSlider.interactable = false; // Display only, not draggable
                }
            }
        }

        private bool HasProgressForDifficulty(string artworkId, int pieceCount)
        {
            var session = saveData?.GetInProgressSession(artworkId, pieceCount);
            return session != null && session.piecesPlaced > 0;
        }
        #endregion

        #region Starting Puzzle
        private void StartPuzzle(int pieceCount, int difficultyIndex)
        {
            if (selectedArtwork == null)
            {
                Debug.LogWarning("[UnifiedMainMenu] No artwork selected!");
                return;
            }

            Debug.Log($"[UnifiedMainMenu] Starting puzzle: {selectedArtwork.title} with {pieceCount} pieces (difficulty {difficultyIndex})");
            OnStartPuzzle?.Invoke(selectedArtwork.artworkId, pieceCount, difficultyIndex);
        }
        #endregion

        #region Settings
        private void LoadSettings()
        {
            if (saveData == null || saveData.settings == null)
                return;

            // Load music volume
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = saveData.settings.musicVolume;
            }

            // Load sound volume (using sfxVolume)
            if (soundVolumeSlider != null)
            {
                soundVolumeSlider.value = saveData.settings.sfxVolume;
            }

            // Load tutorial setting (using showOnboarding)
            if (tutorialToggle != null)
            {
                tutorialToggle.isOn = saveData.settings.showOnboarding;
            }
        }

        private void OnMusicVolumeSliderChanged(float value)
        {
            if (saveData != null && saveData.settings != null)
            {
                saveData.settings.musicVolume = value;
                saveDataService.Save(saveData);
            }
            
            OnMusicVolumeChanged?.Invoke(value);
        }

        private void OnSoundVolumeSliderChanged(float value)
        {
            if (saveData != null && saveData.settings != null)
            {
                saveData.settings.sfxVolume = value;
                saveDataService.Save(saveData);
            }
            
            OnSoundVolumeChanged?.Invoke(value);
        }

        private void OnTutorialToggleChanged(bool isOn)
        {
            if (saveData != null && saveData.settings != null)
            {
                saveData.settings.showOnboarding = isOn;
                saveDataService.Save(saveData);
            }
            
            OnTutorialToggled?.Invoke(isOn);
        }
        #endregion

        #region Global Stats
        private void UpdateGlobalStats()
        {
            if (globalStatsText == null || saveData == null)
                return;

            int totalArtworks = allArtworks.Count;
            int completedCount = saveData.artworkProgress.Count(progress => progress.HasBeenCompleted());

            globalStatsText.text = $"{completedCount}/{totalArtworks} obras completadas";
        }
        #endregion

        #region Show/Hide
        public void Show()
        {
            if (mainPanel != null)
                mainPanel.SetActive(true);
            
            // Refresh music track display (music continues across menu/gameplay)
            if (musicTrackText != null && ArtUnbound.Feedback.AudioManager.Instance != null)
            {
                var (title, artist) = ArtUnbound.Feedback.AudioManager.Instance.CurrentTrack;
                UpdateMusicTrackText(title, artist);
            }
            
            // Refresh data
            if (catalogService != null && saveDataService != null)
            {
                saveData = saveDataService.GetCachedData();
                UpdateGlobalStats();
                ApplyFilter(currentFilter);
                LoadLastPlayedArtwork();
            }
        }

        public void Hide()
        {
            if (mainPanel != null)
                mainPanel.SetActive(false);
        }

        /// <summary>
        /// Refreshes filter and difficulty button colors. Call when canvas becomes visible
        /// (e.g. after calibration) so selected state is applied correctly.
        /// </summary>
        public void RefreshButtonColors()
        {
            UpdateFilterButtonVisuals();
            if (selectedArtwork != null)
                UpdateDifficultyButtons();
        }
        #endregion

        #region Helper Enums
        private enum FilterType
        {
            All,
            InProgress,
            Completed
        }
        #endregion

        #region Audio Feedback
        private void PlayButtonClick()
        {
            if (ArtUnbound.Feedback.AudioManager.Instance != null)
                ArtUnbound.Feedback.AudioManager.Instance.PlayButtonClick();
        }
        #endregion
    }
}
