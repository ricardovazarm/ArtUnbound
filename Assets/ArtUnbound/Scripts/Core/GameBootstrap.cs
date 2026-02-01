using System;
using ArtUnbound.Data;
using ArtUnbound.Feedback;
using ArtUnbound.Gameplay;
using ArtUnbound.MR;
using ArtUnbound.Services;
using ArtUnbound.UI;
using UnityEngine;

namespace ArtUnbound.Core
{
    /// <summary>
    /// Main game bootstrap and state manager.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        public static GameBootstrap Instance { get; private set; }

        public event Action<GameState> OnGameStateChanged;
        public event Action<SaveData> OnDataLoaded;

        [Header("Data Assets")]
        [SerializeField] private ArtworkCatalog artworkCatalog;
        [SerializeField] private PuzzleConfig puzzleConfig;
        [SerializeField] private FrameConfigSet frameConfigSet;

        [Header("UI Controllers")]
        [SerializeField] private MainMenuController mainMenuController;
        [SerializeField] private GalleryPanelController galleryPanelController;
        [SerializeField] private ArtworkDetailController artworkDetailController;
        [SerializeField] private PieceCountSelectorController pieceCountSelector;
        [SerializeField] private PuzzleHUDController puzzleHUD;
        [SerializeField] private PauseMenuController pauseMenuController;
        [SerializeField] private PostGameController postGameController;
        [SerializeField] private OnboardingController onboardingController;
        [SerializeField] private SettingsController settingsController;

        [Header("Gameplay Controllers")]
        [SerializeField] private PuzzleBoard puzzleBoard;
        [SerializeField] private ScoringController scoringController;
        [SerializeField] private PuzzleTimerController timerController;

        [Header("MR Controllers")]
        [SerializeField] private WallSelectionController wallSelectionController;
        [SerializeField] private WallHighlightController wallHighlightController;
        [SerializeField] private ComfortModeController comfortModeController;
        [SerializeField] private CanvasFrameController canvasFrameController;

        [Header("Feedback Controllers")]
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private HapticController hapticController;
        [SerializeField] private FrameAnimationController frameAnimationController;

        public SaveData SaveData { get; private set; }
        public GameState CurrentState { get; private set; } = GameState.Loading;
        public GameMode CurrentGameMode { get; private set; } = GameMode.Gallery;
        public PuzzleSessionData CurrentSession { get; private set; }

        private SaveDataService saveDataService;
        private LocalCatalogService localCatalogService;
        private WeeklyUnlockService weeklyUnlockService;
        private LocalTelemetryService localTelemetryService;

        private string selectedArtworkId;
        private int selectedPieceCount = 64;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            InitializeServices();
            LoadData();
            SetupEventListeners();
        }

        private void Start()
        {
            // Check for onboarding
            if (!SaveData.onboardingCompleted && onboardingController != null)
            {
                ShowOnboarding();
            }
            else
            {
                TransitionToMainMenu();
            }
        }

        private void InitializeServices()
        {
            saveDataService = new SaveDataService();
            weeklyUnlockService = new WeeklyUnlockService();
            localTelemetryService = new LocalTelemetryService();
            localCatalogService = new LocalCatalogService(artworkCatalog);
        }

        private void LoadData()
        {
            SaveData = saveDataService.Load();

            // Apply saved settings
            if (SaveData.settings != null)
            {
                ApplySettings(SaveData.settings);
            }

            OnDataLoaded?.Invoke(SaveData);
        }

        private void SetupEventListeners()
        {
            // Main Menu
            if (mainMenuController != null)
            {
                mainMenuController.OnPlayRequested += OnPlayRequested;
                mainMenuController.OnGalleryRequested += ShowGallery;
                mainMenuController.OnSettingsRequested += ShowSettings;
                mainMenuController.OnGameModeSelected += OnGameModeSelected;
                mainMenuController.OnWeeklyArtworkSelected += OnWeeklyArtworkSelected;
            }

            // Gallery
            if (galleryPanelController != null)
            {
                galleryPanelController.OnArtworkSelected += OnArtworkSelected;
                galleryPanelController.OnPlayRequested += StartPuzzleWithArtwork;
            }

            // Artwork Detail
            if (artworkDetailController != null)
            {
                artworkDetailController.OnPlayWithPieceCount += OnPlayWithPieceCount;
                artworkDetailController.OnBackRequested += ShowGallery;
            }

            // Piece Count Selector
            if (pieceCountSelector != null)
            {
                pieceCountSelector.OnCountSelected += OnPieceCountSelected;
            }

            // Pause Menu
            if (pauseMenuController != null)
            {
                pauseMenuController.OnResumeRequested += ResumePuzzle;
                pauseMenuController.OnQuitRequested += QuitToMenu;
                pauseMenuController.OnHelpModeToggled += OnHelpModeToggled;
            }

            // Post Game
            if (postGameController != null)
            {
                postGameController.OnPlaceArtworkRequested += OnPlaceArtworkRequested;
                postGameController.OnReplayRequested += ReplayPuzzle;
                postGameController.OnReturnToMenuRequested += TransitionToMainMenu;
            }

            // Onboarding
            if (onboardingController != null)
            {
                onboardingController.OnOnboardingComplete += OnOnboardingComplete;
                onboardingController.OnOnboardingSkipped += OnOnboardingComplete;
            }

            // Settings
            if (settingsController != null)
            {
                settingsController.OnSettingsChanged += OnSettingsChanged;
                settingsController.OnCloseRequested += HideSettings;
            }

            // HUD
            if (puzzleHUD != null)
            {
                puzzleHUD.OnPauseRequested += PausePuzzle;
                puzzleHUD.OnHelpModeToggled += OnHelpModeToggled;
            }

            // Puzzle Board
            if (puzzleBoard != null)
            {
                puzzleBoard.OnPieceSnapped += OnPieceSnapped;
                puzzleBoard.OnPuzzleComplete += OnPuzzleComplete;
            }

            // Wall Selection
            if (wallHighlightController != null)
            {
                wallHighlightController.OnWallSelected += OnWallSelected;
            }

            // Comfort Mode
            if (comfortModeController != null)
            {
                comfortModeController.OnPositionLocked += OnComfortPositionLocked;
            }
        }

        #region State Transitions

        private void SetState(GameState newState)
        {
            if (CurrentState == newState) return;

            GameState previousState = CurrentState;
            CurrentState = newState;

            Debug.Log($"Game state: {previousState} -> {newState}");
            OnGameStateChanged?.Invoke(newState);
        }

        private void ShowOnboarding()
        {
            SetState(GameState.Onboarding);

            if (onboardingController != null)
                onboardingController.StartOnboarding();
        }

        public void TransitionToMainMenu()
        {
            SetState(GameState.MainMenu);

            HideAllPanels();

            if (mainMenuController != null)
            {
                mainMenuController.Initialize(SaveData);

                // Set weekly highlight
                var weeklyArtwork = weeklyUnlockService?.GetCurrentWeeklyArtwork();
                if (weeklyArtwork != null)
                {
                    var artworkData = localCatalogService?.GetArtworkById(weeklyArtwork);
                    mainMenuController.SetWeeklyHighlight(weeklyArtwork, artworkData);
                }

                mainMenuController.Show();
            }

            if (audioManager != null)
                audioManager.PlayMenuMusic();
        }

        private void ShowGallery()
        {
            SetState(GameState.Gallery);

            HideAllPanels();

            if (galleryPanelController != null)
            {
                galleryPanelController.SetData(
                    SaveData.GetCompletedArtworks(),
                    SaveData.placedArtworks,
                    SaveData.GetSavedArtworks()
                );
                galleryPanelController.Show();
            }
        }

        private void ShowSettings()
        {
            if (settingsController != null)
            {
                settingsController.ShowSettings(SaveData.settings);
            }
        }

        private void HideSettings()
        {
            if (settingsController != null)
                settingsController.Hide();
        }

        private void TransitionToPlaying()
        {
            SetState(GameState.Playing);

            HideAllPanels();

            if (puzzleHUD != null)
            {
                puzzleHUD.Initialize(selectedPieceCount, SaveData.settings?.helpModeDefault ?? false);
                puzzleHUD.SetRepositionButtonVisible(CurrentGameMode == GameMode.Comfort);
                puzzleHUD.Show();
            }

            if (timerController != null)
                timerController.StartTimer();

            if (audioManager != null)
                audioManager.PlayGameplayMusic();
        }

        #endregion

        #region Event Handlers

        private void OnPlayRequested()
        {
            // Show artwork selection or piece count selection
            ShowGallery();
        }

        private void OnGameModeSelected(GameMode mode)
        {
            CurrentGameMode = mode;
            SaveData.lastGameMode = mode;
            saveDataService.Save(SaveData);
        }

        private void OnWeeklyArtworkSelected(string artworkId)
        {
            selectedArtworkId = artworkId;

            if (pieceCountSelector != null)
            {
                pieceCountSelector.ShowSelector("Selecciona dificultad");
            }
        }

        private void OnArtworkSelected(string artworkId)
        {
            selectedArtworkId = artworkId;

            var progress = SaveData.GetProgress(artworkId);
            var artworkData = localCatalogService?.GetArtworkById(artworkId);

            if (artworkDetailController != null)
            {
                artworkDetailController.ShowArtworkDetail(artworkId, progress, artworkData);
            }
        }

        private void StartPuzzleWithArtwork(string artworkId)
        {
            selectedArtworkId = artworkId;

            if (pieceCountSelector != null)
            {
                pieceCountSelector.ShowSelector("Selecciona dificultad");
            }
        }

        private void OnPieceCountSelected(int count)
        {
            selectedPieceCount = count;
            StartPuzzle();
        }

        private void OnPlayWithPieceCount(int count)
        {
            selectedPieceCount = count;
            StartPuzzle();
        }

        private void StartPuzzle()
        {
            // Create session
            CurrentSession = new PuzzleSessionData
            {
                artworkId = selectedArtworkId,
                pieceCount = selectedPieceCount,
                gameMode = CurrentGameMode,
                helpModeUsed = SaveData.settings?.helpModeDefault ?? false
            };
            CurrentSession.StartSession();

            // Start wall selection for Gallery mode, or position for Comfort mode
            if (CurrentGameMode == GameMode.Gallery)
            {
                StartWallSelection();
            }
            else
            {
                StartComfortPositioning();
            }
        }

        private void StartWallSelection()
        {
            SetState(GameState.WallSelection);

            if (wallHighlightController != null)
            {
                wallHighlightController.StartSelection();
            }
        }

        private void StartComfortPositioning()
        {
            SetState(GameState.ComfortPositioning);

            if (comfortModeController != null)
            {
                comfortModeController.StartPositioning();
            }
        }

        private void OnWallSelected(Vector3 position, Quaternion rotation)
        {
            // Position the puzzle board
            if (puzzleBoard != null)
            {
                puzzleBoard.transform.position = position;
                puzzleBoard.transform.rotation = rotation;
            }

            InitializePuzzleBoard();
            TransitionToPlaying();
        }

        private void OnComfortPositionLocked()
        {
            InitializePuzzleBoard();
            TransitionToPlaying();
        }

        private void InitializePuzzleBoard()
        {
            if (puzzleBoard == null) return;

            var artworkData = localCatalogService?.GetArtworkById(selectedArtworkId);
            Texture2D artworkTexture = artworkData?.fullImage?.texture;

            puzzleBoard.Initialize(selectedPieceCount, artworkTexture);
        }

        private void OnPieceSnapped(int gridX, int gridY)
        {
            if (CurrentSession != null)
            {
                CurrentSession.piecesPlaced++;
            }

            if (puzzleHUD != null)
            {
                puzzleHUD.UpdatePiecesPlaced(CurrentSession?.piecesPlaced ?? 0);
            }

            if (audioManager != null)
                audioManager.PlayPieceSnap();

            if (hapticController != null)
                hapticController.PlaySnapPattern(HandSide.Both);
        }

        private void OnPuzzleComplete()
        {
            SetState(GameState.PostGame);

            if (timerController != null)
                timerController.StopTimer();

            if (CurrentSession != null)
            {
                CurrentSession.EndSession();
            }

            // Calculate score
            int timeSec = CurrentSession?.GetElapsedSeconds() ?? 0;
            bool helpMode = CurrentSession?.helpModeUsed ?? false;

            int score = scoringController != null
                ? scoringController.CalculateScore(timeSec, selectedPieceCount, helpMode)
                : 0;

            FrameTier frameTier = scoringController != null
                ? scoringController.GetFrameTier(score, helpMode, selectedPieceCount)
                : FrameTier.Madera;

            // Check for new record
            var progress = SaveData.GetProgress(selectedArtworkId);
            var existingRecord = progress?.GetRecordForPieceCount(selectedPieceCount);
            bool isNewRecord = existingRecord == null || score > existingRecord.bestScore;

            // Save progress
            saveDataService.UpdateArtworkProgress(selectedArtworkId, selectedPieceCount, score, timeSec, frameTier);
            SaveData = saveDataService.GetCachedData();

            // Play effects
            if (audioManager != null)
            {
                audioManager.PlayPuzzleComplete();
                if (isNewRecord)
                    audioManager.PlayNewRecord();
            }

            if (hapticController != null)
                hapticController.PlayCompletionPattern();

            if (frameAnimationController != null)
                frameAnimationController.PlayFrameReveal(frameTier);

            // Show post game screen
            if (postGameController != null)
            {
                postGameController.ShowResults(CurrentSession, score, frameTier, isNewRecord);
            }
        }

        private void PausePuzzle()
        {
            SetState(GameState.Paused);

            if (pauseMenuController != null)
            {
                pauseMenuController.UpdatePiecesCount(
                    CurrentSession?.piecesPlaced ?? 0,
                    selectedPieceCount
                );
                pauseMenuController.Pause();
            }

            if (audioManager != null)
                audioManager.PauseAll();
        }

        private void ResumePuzzle()
        {
            SetState(GameState.Playing);

            if (pauseMenuController != null)
                pauseMenuController.Resume();

            if (audioManager != null)
                audioManager.ResumeAll();
        }

        private void QuitToMenu()
        {
            // Save session if in progress
            if (CurrentSession != null && CurrentSession.piecesPlaced > 0)
            {
                saveDataService.SaveSession(CurrentSession);
            }

            CurrentSession = null;
            TransitionToMainMenu();
        }

        private void OnHelpModeToggled(bool enabled)
        {
            if (CurrentSession != null)
            {
                CurrentSession.helpModeUsed = CurrentSession.helpModeUsed || enabled;
            }

            // Update puzzle board visualization
            if (puzzleBoard != null)
            {
                // Toggle help visualization
            }
        }

        private void OnPlaceArtworkRequested()
        {
            // Start wall selection for hanging
            if (wallHighlightController != null)
            {
                wallHighlightController.OnWallSelected += OnHangWallSelected;
                wallHighlightController.StartSelection();
            }
        }

        private void OnHangWallSelected(Vector3 position, Quaternion rotation)
        {
            wallHighlightController.OnWallSelected -= OnHangWallSelected;

            // Get the frame tier from the completed puzzle
            FrameTier frameTier = postGameController?.GetAwardedFrame() ?? FrameTier.Madera;

            // Create placed artwork
            var placed = new PlacedArtwork
            {
                artworkId = selectedArtworkId,
                frameTier = frameTier,
                placedDate = DateTime.Now,
                scale = 1.0f
            };
            placed.SetPosition(position);
            placed.SetRotation(rotation);

            saveDataService.AddPlacedArtwork(placed);
            SaveData = saveDataService.GetCachedData();

            TransitionToMainMenu();
        }

        private void ReplayPuzzle()
        {
            StartPuzzle();
        }

        private void OnOnboardingComplete()
        {
            saveDataService.CompleteOnboarding();
            SaveData = saveDataService.GetCachedData();

            TransitionToMainMenu();
        }

        private void OnSettingsChanged(GameSettings settings)
        {
            ApplySettings(settings);
            saveDataService.UpdateSettings(settings);
            SaveData = saveDataService.GetCachedData();
        }

        private void ApplySettings(GameSettings settings)
        {
            if (settings == null) return;

            if (audioManager != null)
            {
                audioManager.SetSfxVolume(settings.sfxVolume);
                audioManager.SetMusicVolume(settings.musicVolume);
            }
        }

        #endregion

        private void HideAllPanels()
        {
            mainMenuController?.Hide();
            galleryPanelController?.Hide();
            artworkDetailController?.Hide();
            pieceCountSelector?.Hide();
            puzzleHUD?.Hide();
            pauseMenuController?.Hide();
            postGameController?.Hide();
            onboardingController?.Hide();
            settingsController?.Hide();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && CurrentState == GameState.Playing)
            {
                PausePuzzle();
            }

            // Auto-save
            saveDataService?.SaveIfDirty();
        }

        private void OnApplicationQuit()
        {
            saveDataService?.SaveIfDirty();
        }
    }

    /// <summary>
    /// Represents the current game state.
    /// </summary>
    public enum GameState
    {
        Loading,
        Onboarding,
        MainMenu,
        Gallery,
        ArtworkSelection,
        WallSelection,
        ComfortPositioning,
        Playing,
        Paused,
        PostGame
    }
}
