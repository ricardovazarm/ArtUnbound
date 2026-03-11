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
    /// </summary>
    public class UnifiedMainMenuController : MonoBehaviour
    {
        #region Events
        public event Action<string, int> OnStartPuzzle; // artworkId, pieceCount
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
        [SerializeField] private Toggle tutorialToggle;
        [SerializeField] private TextMeshProUGUI globalStatsText; // "12/24 obras completadas"

        [Header("Center Zone - Catalog")]
        [SerializeField] private GameObject catalogGrid; // Container with Grid Layout Group
        [SerializeField] private ScrollRect catalogScrollRect;
        [SerializeField] private Button filterAllButton;
        [SerializeField] private Button filterInProgressButton;
        [SerializeField] private Button filterCompletedButton;
        [SerializeField] private GameObject artworkCardPrefab; // Prefab for artwork thumbnail cards

        [Header("Right Zone - Detail Panel")]
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private Image detailArtworkImage;
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

        [Header("Visual Feedback")]
        [SerializeField] private Color normalButtonColor = Color.white;       // Botón normal (más brillante)
        [SerializeField] private Color dimmedButtonColor = new Color(0.5f, 0.5f, 0.5f, 1f); // Botón no seleccionado (oscuro)

        [Header("Configuration")]
        [SerializeField] private PuzzleConfig puzzleConfig; // Reference to PuzzleConfig for piece counts
        #endregion

        #region Private Fields
        private LocalCatalogService catalogService;
        private SaveDataService saveDataService;
        private SaveData saveData;
        
        private List<ArtworkDefinition> allArtworks = new List<ArtworkDefinition>();
        private List<GameObject> artworkCards = new List<GameObject>();
        private ArtworkDefinition selectedArtwork;
        
        private FilterType currentFilter = FilterType.All;
        
        // Difficulty names - piece counts loaded from PuzzleConfig
        private Dictionary<int, string> difficultyNames;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            LoadPieceCountsFromConfig();
            SetupButtonListeners();
        }

        private void OnDestroy()
        {
            RemoveButtonListeners();
        }
        #endregion

        #region Initialization
        /// <summary>
        /// Loads piece counts from PuzzleConfig and sets up difficulty names.
        /// </summary>
        private void LoadPieceCountsFromConfig()
        {
            // Load PuzzleConfig if not assigned
            if (puzzleConfig == null)
            {
                puzzleConfig = Resources.Load<PuzzleConfig>("Data/PuzzleConfig");
                if (puzzleConfig == null)
                {
                    Debug.LogError("[UnifiedMainMenu] PuzzleConfig not found! Using default piece counts.");
                    // Fallback to default values
                    difficultyNames = new Dictionary<int, string>
                    {
                        { 64, "Fácil" },
                        { 121, "Normal" },
                        { 196, "Difícil" },
                        { 289, "Experto" }
                    };
                    return;
                }
            }

            // Build difficulty names from PuzzleConfig
            difficultyNames = new Dictionary<int, string>();
            string[] difficultyLabels = { "Fácil", "Normal", "Difícil", "Experto" };
            
            for (int i = 0; i < puzzleConfig.pieceCounts.Length && i < difficultyLabels.Length; i++)
            {
                difficultyNames[puzzleConfig.pieceCounts[i]] = difficultyLabels[i];
            }

            Debug.Log($"[UnifiedMainMenu] Loaded piece counts: {string.Join(", ", puzzleConfig.pieceCounts)}");
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
                filterAllButton.onClick.AddListener(() => ApplyFilter(FilterType.All));
            
            if (filterInProgressButton != null)
                filterInProgressButton.onClick.AddListener(() => ApplyFilter(FilterType.InProgress));
            
            if (filterCompletedButton != null)
                filterCompletedButton.onClick.AddListener(() => ApplyFilter(FilterType.Completed));

            // Difficulty buttons - use values from PuzzleConfig
            if (puzzleConfig != null && puzzleConfig.pieceCounts.Length >= 4)
            {
                if (easyButton != null)
                    easyButton.onClick.AddListener(() => StartPuzzle(puzzleConfig.pieceCounts[0])); // Easy
                
                if (normalButton != null)
                    normalButton.onClick.AddListener(() => StartPuzzle(puzzleConfig.pieceCounts[1])); // Normal
                
                if (hardButton != null)
                    hardButton.onClick.AddListener(() => StartPuzzle(puzzleConfig.pieceCounts[2])); // Hard
                
                if (expertButton != null)
                    expertButton.onClick.AddListener(() => StartPuzzle(puzzleConfig.pieceCounts[3])); // Expert
            }
            else
            {
                Debug.LogWarning("[UnifiedMainMenu] PuzzleConfig not loaded properly, using fallback piece counts");
                // Fallback to default values
                if (easyButton != null)
                    easyButton.onClick.AddListener(() => StartPuzzle(64));
                
                if (normalButton != null)
                    normalButton.onClick.AddListener(() => StartPuzzle(121));
                
                if (hardButton != null)
                    hardButton.onClick.AddListener(() => StartPuzzle(196));
                
                if (expertButton != null)
                    expertButton.onClick.AddListener(() => StartPuzzle(289));
            }

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
            
            List<ArtworkDefinition> filteredArtworks = GetFilteredArtworks();
            PopulateCatalogGrid(filteredArtworks);
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
            
            // Check if there's a saved session for this artwork
            var session = saveDataService.LoadSession();
            return session != null && session.artworkId == artworkId && !session.isCompleted;
        }

        private bool IsArtworkCompleted(string artworkId)
        {
            if (saveData == null) return false;
            
            var progress = saveData.GetProgress(artworkId);
            return progress != null && progress.HasBeenCompleted();
        }

        private void UpdateFilterButtonVisuals()
        {
            UpdateButtonColor(filterAllButton, currentFilter == FilterType.All);
            UpdateButtonColor(filterInProgressButton, currentFilter == FilterType.InProgress);
            UpdateButtonColor(filterCompletedButton, currentFilter == FilterType.Completed);
        }

        private void UpdateButtonColor(Button button, bool isSelected)
        {
            if (button == null) return;
            
            var colors = button.colors;
            colors.normalColor = isSelected ? normalButtonColor : dimmedButtonColor;
            button.colors = colors;
        }
        #endregion

        #region Catalog Grid Population
        private void PopulateCatalogGrid(List<ArtworkDefinition> artworks)
        {
            // Clear existing cards
            foreach (var card in artworkCards)
            {
                if (card != null)
                    Destroy(card);
            }
            artworkCards.Clear();

            if (catalogGrid == null)
            {
                Debug.LogError("[UnifiedMainMenu] CatalogGrid is NULL! Assign it in the Inspector.");
                return;
            }

            if (artworkCardPrefab == null)
            {
                Debug.LogError("[UnifiedMainMenu] ArtworkCardPrefab is NULL! Assign it in the Inspector.");
                return;
            }

            Debug.Log($"[UnifiedMainMenu] Populating grid with {artworks.Count} artworks");

            // Create cards for filtered artworks
            foreach (var artwork in artworks)
            {
                GameObject card = Instantiate(artworkCardPrefab, catalogGrid.transform);
                SetupArtworkCard(card, artwork);
                artworkCards.Add(card);
                
                Debug.Log($"[UnifiedMainMenu] Created card for: {artwork.title}");
            }

            Debug.Log($"[UnifiedMainMenu] Grid population complete: {artworkCards.Count} cards created");
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
            }

            // Set title text
            if (artworkCard.TitleText != null)
            {
                artworkCard.TitleText.text = artwork.title;
            }

            // Add click listener
            var button = card.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => SelectArtwork(artwork));
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
            Debug.Log($"[UnifiedMainMenu] Setup card: {artwork.title}, Completed: {isCompleted}, Progress: {progressPercent:F0}%");
        }

        private float GetArtworkProgress(string artworkId)
        {
            var session = saveDataService.LoadSession();
            if (session != null && session.artworkId == artworkId && !session.isCompleted)
            {
                if (session.pieceCount > 0)
                {
                    return (session.piecesPlaced / (float)session.pieceCount) * 100f;
                }
            }
            return 0f;
        }

        #endregion

        #region Artwork Selection & Detail
        private void SelectArtwork(ArtworkDefinition artwork)
        {
            selectedArtwork = artwork;
            UpdateDetailPanel();
            
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

            // Set artwork image
            if (detailArtworkImage != null && selectedArtwork.fullImage != null)
            {
                detailArtworkImage.sprite = selectedArtwork.fullImage;
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

        private void UpdateDifficultyButtons()
        {
            if (puzzleConfig != null && puzzleConfig.pieceCounts.Length >= 4)
            {
                UpdateDifficultyButton(easyButton, easyButtonText, puzzleConfig.pieceCounts[0]);     // Easy
                UpdateDifficultyButton(normalButton, normalButtonText, puzzleConfig.pieceCounts[1]); // Normal
                UpdateDifficultyButton(hardButton, hardButtonText, puzzleConfig.pieceCounts[2]);     // Hard
                UpdateDifficultyButton(expertButton, expertButtonText, puzzleConfig.pieceCounts[3]); // Expert
            }
            else
            {
                // Fallback
                UpdateDifficultyButton(easyButton, easyButtonText, 64);
                UpdateDifficultyButton(normalButton, normalButtonText, 121);
                UpdateDifficultyButton(hardButton, hardButtonText, 196);
                UpdateDifficultyButton(expertButton, expertButtonText, 289);
            }
        }

        private void UpdateDifficultyButton(Button button, TextMeshProUGUI buttonText, int pieceCount)
        {
            if (button == null || buttonText == null || selectedArtwork == null)
                return;

            string difficultyName = difficultyNames.ContainsKey(pieceCount) ? difficultyNames[pieceCount] : pieceCount.ToString();
            
            // Check if this difficulty is in progress (saved session)
            bool hasProgress = HasProgressForDifficulty(selectedArtwork.artworkId, pieceCount);
            // Check if this is the last played difficulty for the last played artwork
            bool isLastPlayed = saveData != null && 
                selectedArtwork.artworkId == saveData.lastArtworkId && 
                pieceCount == saveData.lastPieceCount;
            bool isSelected = hasProgress || isLastPlayed;
            
            if (hasProgress)
            {
                buttonText.text = $"Continuar {difficultyName}";
            }
            else
            {
                buttonText.text = difficultyName;
            }
            
            // Highlight selected button (más claro), dim others (oscuro)
            var colors = button.colors;
            colors.normalColor = isSelected ? normalButtonColor : dimmedButtonColor;
            button.colors = colors;
        }

        private bool HasProgressForDifficulty(string artworkId, int pieceCount)
        {
            var session = saveDataService.LoadSession();
            return session != null && 
                   session.artworkId == artworkId && 
                   session.pieceCount == pieceCount && 
                   !session.isCompleted &&
                   session.piecesPlaced > 0;
        }
        #endregion

        #region Starting Puzzle
        private void StartPuzzle(int pieceCount)
        {
            if (selectedArtwork == null)
            {
                Debug.LogWarning("[UnifiedMainMenu] No artwork selected!");
                return;
            }

            Debug.Log($"[UnifiedMainMenu] Starting puzzle: {selectedArtwork.title} with {pieceCount} pieces");
            OnStartPuzzle?.Invoke(selectedArtwork.artworkId, pieceCount);
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
        #endregion

        #region Helper Enums
        private enum FilterType
        {
            All,
            InProgress,
            Completed
        }
        #endregion
    }
}
