using System;
using System.Linq;
using ArtUnbound.Data;
using ArtUnbound.Feedback;
using ArtUnbound.Gameplay;
using ArtUnbound.MR;
using ArtUnbound.Services;
using ArtUnbound.UI;
using UnityEngine;
using UnityEngine.XR;

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

        [Header("General UI")]
        [SerializeField] private Transform mainUICanvas;
        [SerializeField] private LoadingSpinner loadingSpinner;

        [Header("UI Controllers")]
        [SerializeField] private UnifiedMainMenuController unifiedMainMenu; // Unified main menu (replaces old menu system)
        [SerializeField] private PuzzleHUDController puzzleHUD;
        [SerializeField] private PuzzleAchievementsController puzzleAchievements;
        [SerializeField] private PostGameController postGameController;
        [SerializeField] private OnboardingController onboardingController;

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
            HideAllPanels(); // Ensure clean state immediately
            LoadData();
            SetupEventListeners();
        }

        private void Start()
        {
            SetupCameraForPassthrough();
            HideAllPanels();

            // Hide canvas until calibration completes (avoids visible jump)
            if (mainUICanvas != null)
            {
                mainUICanvas.gameObject.SetActive(false);
            }

            // Check for onboarding
            if (!SaveData.onboardingCompleted && onboardingController != null)
            {
                ShowOnboarding();
            }
            else
            {
                TransitionToMainMenu();
            }

            // Position the UI Canvas ergonomically with a delay to allow XR tracking to initialize
            if (mainUICanvas != null)
            {
                StartCoroutine(PositionCanvasWithDelay(mainUICanvas));
            }
        }

        private void SetupCameraForPassthrough()
        {
            if (Camera.main != null)
            {
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = new Color(0, 0, 0, 0); // Transparent black

                // Enable AR Camera Manager if present (Required for Passthrough)
                var arCameraManager = Camera.main.GetComponent<UnityEngine.XR.ARFoundation.ARCameraManager>();
                if (arCameraManager != null)
                {
                    arCameraManager.enabled = true;
                }
            }
        }

        private void InitializeServices()
        {
            saveDataService = new SaveDataService();
            weeklyUnlockService = new WeeklyUnlockService();
            localTelemetryService = new LocalTelemetryService();
            localCatalogService = new LocalCatalogService(artworkCatalog);
            
            // Initialize UnifiedMainMenu with services
            if (unifiedMainMenu != null)
            {
                unifiedMainMenu.Initialize(localCatalogService, saveDataService);
            }
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
            // Unified Main Menu
            if (unifiedMainMenu != null)
            {
                unifiedMainMenu.OnStartPuzzle += OnUnifiedMenuStartPuzzle;
            }

            // Post Game
            if (postGameController != null)
            {
                postGameController.OnPlaceArtworkRequested += OnPlaceArtworkRequested;
                postGameController.OnReplayRequested += ReplayPuzzle;
            }

            // Onboarding
            if (onboardingController != null)
            {
                onboardingController.OnOnboardingComplete += OnOnboardingComplete;
                onboardingController.OnOnboardingSkipped += OnOnboardingComplete;
            }

            // HUD
            if (puzzleHUD != null)
            {
                puzzleHUD.OnExitRequested += QuitToMenu; // Changed: exit returns to main menu
            }

            // Puzzle Board
            if (puzzleBoard != null)
            {
                puzzleBoard.OnPlacementSuccess += OnPieceCorrectlyPlaced;
                puzzleBoard.OnPuzzleComplete += OnPuzzleComplete;
                puzzleBoard.OnBoardStateChanged += OnBoardStateChanged; // Auto-save trigger
                puzzleBoard.OnMilestoneAchieved += OnMilestoneAchieved;
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

            // UnifiedMainMenu
            if (unifiedMainMenu != null)
            {
                unifiedMainMenu.OnStartPuzzle += (artworkId, pieceCount) =>
                {
                    selectedArtworkId = artworkId;
                    selectedPieceCount = pieceCount;
                    StartPuzzle();
                };

                unifiedMainMenu.OnMusicVolumeChanged += (volume) =>
                {
                    if (audioManager != null)
                        audioManager.SetMusicVolume(volume);
                };

                unifiedMainMenu.OnSoundVolumeChanged += (volume) =>
                {
                    if (audioManager != null)
                        audioManager.SetSfxVolume(volume);
                };
            }
        }

        #region State Transitions

        private void SetState(GameState newState)
        {
            if (CurrentState == newState) return;

            GameState previousState = CurrentState;
            CurrentState = newState;

            OnGameStateChanged?.Invoke(newState);
        }

        private void ShowOnboarding()
        {
            SetState(GameState.Onboarding);
            HideAllPanels();

            if (onboardingController != null)
                onboardingController.StartOnboarding();
        }

        public void TransitionToMainMenu()
        {
            SetState(GameState.MainMenu);

            HideAllPanels();

            // Use UnifiedMainMenu
            if (unifiedMainMenu != null)
            {
                unifiedMainMenu.Show();
            }
            else
            {
                Debug.LogError("[GameBootstrap] UnifiedMainMenuController reference is missing in Inspector!");
            }

            if (audioManager != null)
                audioManager.PlayMenuMusic();
        }

        /// <summary>
        /// Handler for UnifiedMainMenu's OnStartPuzzle event.
        /// Receives artworkId and pieceCount directly from the unified menu.
        /// </summary>
        private void OnUnifiedMenuStartPuzzle(string artworkId, int pieceCount)
        {
            selectedArtworkId = artworkId;
            selectedPieceCount = pieceCount;
            
            
            StartPuzzle();
        }

        private void TransitionToPlaying()
        {
            SetState(GameState.Playing);

            HideAllPanels();

            // Ensure puzzle board is visible
            if (puzzleBoard != null)
            {
                puzzleBoard.gameObject.SetActive(true);
            }

            if (puzzleHUD != null)
            {
                puzzleHUD.Initialize(selectedPieceCount, true); // Help mode always enabled
                
                // Set artwork information in HUD
                var currentArtwork = localCatalogService?.GetById(selectedArtworkId);
                if (currentArtwork != null)
                {
                    puzzleHUD.SetArtworkInfo(
                        currentArtwork.title,
                        currentArtwork.author,
                        currentArtwork.description
                    );
                }
                
                puzzleHUD.Show();
            }

            puzzleAchievements?.Show();

            // Timer is already started in InitializePuzzleBoard(), so don't restart it here
            // if (timerController != null)
            //     timerController.StartTimer();

            if (audioManager != null)
                audioManager.PlayGameplayMusic();
        }


        #endregion

        #region Event Handlers

        private void StartPuzzle()
        {
            // Save last played artwork and difficulty
            SaveData.lastArtworkId = selectedArtworkId;
            SaveData.lastPieceCount = selectedPieceCount;
            saveDataService.Save(SaveData);
            
            
            // Create session
            // Force Comfort Mode logic for puzzle start (Floating Board)
            CurrentGameMode = GameMode.Comfort;

            CurrentSession = new PuzzleSessionData
            {
                artworkId = selectedArtworkId,
                pieceCount = selectedPieceCount,
                gameMode = CurrentGameMode,
                helpModeUsed = true // Help mode always enabled
            };
            CurrentSession.StartSession();

            // Always start with Comfort/Floating positioning
            StartComfortPositioning();
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

            // Sync PuzzleBoard to match Canvas exactly
            if (puzzleBoard != null && mainUICanvas != null)
            {
                // Use Canvas position and rotation as the base
                // Board should be at EXACTLY the same position
                puzzleBoard.transform.position = mainUICanvas.position;
                
                // Board rotation = Canvas rotation + 180° to face the user
                Quaternion canvasRotation = mainUICanvas.rotation;
                Quaternion boardRotation = canvasRotation * Quaternion.Euler(0f, 180f, 0f);
                puzzleBoard.transform.rotation = boardRotation;

            }

            InitializePuzzleBoard();
            TransitionToPlaying();
        }

        private void InitializePuzzleBoard()
        {
            if (puzzleBoard == null) return;

            // Ensure board is active and visible
            puzzleBoard.gameObject.SetActive(true);

            var artworkData = localCatalogService?.GetArtworkById(selectedArtworkId);
            Texture2D artworkTexture = artworkData?.fullImage?.texture;

            puzzleBoard.Initialize(selectedPieceCount, artworkTexture);
            
            // IMPORTANT: Update selectedPieceCount to the ACTUAL piece count
            // (may differ from target due to aspect ratio)
            int actualPieceCount = puzzleBoard.TotalPieces;
            if (actualPieceCount != selectedPieceCount)
            {
                Debug.Log($"[PIECE] GameBootstrap: piece count adjusted target={selectedPieceCount} -> actual={actualPieceCount}");
                selectedPieceCount = actualPieceCount;
            }
            
            // Re-initialize HUD with the ACTUAL piece count
            if (puzzleHUD != null)
            {
                puzzleHUD.Initialize(actualPieceCount, true); // Help mode always enabled
            }
            
            // Check if there's a saved session to restore
            var savedSession = saveDataService.LoadSession();
            
            if (savedSession != null 
                && savedSession.artworkId == selectedArtworkId 
                && !savedSession.isCompleted
                && savedSession.placedPieces != null
                && savedSession.placedPieces.Count > 0)
            {
                // Use the saved session instead of creating a new one
                CurrentSession = savedSession;
                
                // Update piece count to actual (in case it was saved with target count)
                CurrentSession.pieceCount = actualPieceCount;
                
                // Restore pieces on the board (now with one less piece if it was complete)
                Debug.Log($"[PIECE] GameBootstrap: restoring session savedPlaced={savedSession.placedPieces.Count}, actualPieceCount={actualPieceCount}");
                puzzleBoard.RestoreBoardState(savedSession.placedPieces);
                
                // Update HUD with ACTUAL correct pieces count (from board, not session)
                if (puzzleHUD != null)
                {
                    int correctPieces = puzzleBoard.SnappedCount;
                    int totalPieces = puzzleBoard.TotalPieces;
                    Debug.Log($"[PIECE] GameBootstrap restore: HUD updated {correctPieces}/{totalPieces} correct");
                    
                    puzzleHUD.UpdatePiecesPlaced(correctPieces);
                    
                    // Force refresh display
                    if (correctPieces > 0)
                    {
                        // Trigger another update to ensure UI refreshes
                        StartCoroutine(RefreshHUDAfterDelay(correctPieces));
                    }
                }
                
                // Start timer from saved elapsed time
                if (timerController != null)
                {
                    timerController.StartTimer(savedSession.elapsedTime);
                }
            }
            else
            {
                // No saved session or different puzzle - start fresh
                
                // Clear any old session that doesn't match
                if (savedSession != null)
                {
                    saveDataService.ClearSession();
                }
                
                // Update CurrentSession piece count if it exists
                if (CurrentSession != null)
                {
                    CurrentSession.pieceCount = actualPieceCount;
                }
                
                // Just make sure timer starts from 0
                if (timerController != null)
                {
                    timerController.StartTimer(0f);
                }
            }
        }
        private void OnPieceCorrectlyPlaced(PuzzlePiece piece)
        {
            if (CurrentSession != null)
            {
                CurrentSession.piecesPlaced++;
            }

            if (puzzleHUD != null)
            {
                puzzleHUD.UpdatePiecesPlaced(CurrentSession?.piecesPlaced ?? 0);
            }

            // Audio is now handled in PuzzleBoard based on correctness
            // Removed: audioManager.PlayPieceSnap() to avoid duplicate sounds

            if (hapticController != null)
                hapticController.PlaySnapPattern(HandSide.Both);
        }

        private void OnMilestoneAchieved(ArtUnbound.UI.MilestoneType type, int edgeCount)
        {
            puzzleAchievements?.ShowMilestone(type, edgeCount);
        }

        private void OnPuzzleComplete()
        {
            SetState(GameState.PostGame);

            if (timerController != null)
                timerController.StopTimer();

            if (CurrentSession != null)
            {
                CurrentSession.isCompleted = true;
                CurrentSession.EndSession();
            }

            // IMPORTANT: Clear saved session since puzzle is completed
            saveDataService.ClearSession();

            // Get completion time from the timer (simple counter, source of truth)
            int timeSec = timerController != null ? timerController.GetElapsedSeconds() : (CurrentSession?.GetElapsedSeconds() ?? 1);
            
            // Frame tier now based ONLY on difficulty (piece count)
            FrameTier frameTier = GetFrameTierFromPieceCount(selectedPieceCount);

            // Check for new record (based on TIME only)
            var progress = SaveData.GetProgress(selectedArtworkId);
            var existingRecord = progress?.GetRecordForPieceCount(selectedPieceCount);
            bool isNewRecord = existingRecord == null || timeSec < existingRecord.bestTimeSec;
            int previousBestTime = existingRecord?.bestTimeSec ?? 0;


            // Save progress to permanent records (score = 0, only time matters now)
            saveDataService.UpdateArtworkProgress(selectedArtworkId, selectedPieceCount, 0, timeSec, frameTier);
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

            // Hide scroll buttons when puzzle is complete
            if (puzzleBoard != null)
                puzzleBoard.HideScrollButtons();

            // Keep board and HUD visible, only show post game panel on top
            // puzzleHUD?.Hide(); // Uncomment if you want to hide HUD
            
            // Show post game screen (without hiding everything else)
            if (postGameController != null)
            {
                postGameController.ShowResults(CurrentSession, timeSec, previousBestTime, frameTier, isNewRecord);
            }
            else
            {
                Debug.LogError("[GameBootstrap] postGameController is NULL! Cannot show post-game panel.");
            }
        }


        private void QuitToMenu()
        {
            // Save session with current timer before clearing (user may exit without placing a piece)
            SaveCurrentSessionIfPlaying();
            CurrentSession = null;
            
            // Hide the puzzle board
            if (puzzleBoard != null)
            {
                puzzleBoard.gameObject.SetActive(false);
            }
            
            // Return to main menu (which shows UnifiedMainMenu)
            TransitionToMainMenu();
        }

        /// <summary>
        /// Called whenever a piece is placed, removed, or moved on the board.
        /// Triggers immediate auto-save of current progress.
        /// </summary>
        private void OnBoardStateChanged()
        {
            if (CurrentSession == null || puzzleBoard == null) return;
            
            // Capture current board state
            var boardState = puzzleBoard.GetCurrentBoardState();
            
            // Update session with current state
            CurrentSession.placedPieces = boardState;
            CurrentSession.piecesPlaced = boardState.Count;
            
            // Use the actual gameplay timer (simple counter), NOT wall-clock time
            if (timerController != null)
            {
                CurrentSession.elapsedTime = timerController.ElapsedTime;
            }
            
            // Save immediately to disk
            saveDataService.SaveSession(CurrentSession);
            
            Debug.Log($"[PIECE] Auto-save: placed={CurrentSession.piecesPlaced}, total={boardState.Count}");
        }

        // OnHelpModeToggled removed - help mode always enabled
        // private void OnHelpModeToggled(bool enabled)
        // {
        //     if (CurrentSession != null)
        //     {
        //         CurrentSession.helpModeUsed = CurrentSession.helpModeUsed || enabled;
        //     }
        // }

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
            FrameTier frameTier = postGameController?.GetAwardedFrame() ?? FrameTier.Bronce;

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
            unifiedMainMenu?.Hide();
            puzzleHUD?.Hide();
            puzzleAchievements?.Hide();
            postGameController?.Hide();
            onboardingController?.Hide();
            
            // Hide puzzle board when switching to menu
            if (puzzleBoard != null)
            {
                puzzleBoard.gameObject.SetActive(false);
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            SaveCurrentSessionIfPlaying();
            saveDataService?.SaveIfDirty();
        }

        private void OnApplicationQuit()
        {
            SaveCurrentSessionIfPlaying();
            saveDataService?.SaveIfDirty();
        }

        /// <summary>
        /// Saves the current puzzle session with the actual timer value.
        /// Called when exiting to menu, pausing, or quitting so elapsed time is preserved.
        /// </summary>
        private void SaveCurrentSessionIfPlaying()
        {
            if (CurrentSession == null || puzzleBoard == null || saveDataService == null) return;
            if (CurrentSession.isCompleted) return;

            var boardState = puzzleBoard.GetCurrentBoardState();
            CurrentSession.placedPieces = boardState;
            CurrentSession.piecesPlaced = boardState.Count;
            if (timerController != null)
            {
                CurrentSession.elapsedTime = timerController.ElapsedTime;
            }
            saveDataService.SaveSession(CurrentSession);
        }

        private void OnDestroy()
        {
            // Clean up event listeners
            if (puzzleBoard != null)
            {
                puzzleBoard.OnBoardStateChanged -= OnBoardStateChanged;
                puzzleBoard.OnMilestoneAchieved -= OnMilestoneAchieved;
            }
        }

        private void PositionCanvasErgonomically(Transform canvasTransform)
        {
            if (canvasTransform == null) return;

            Transform headTransform = Camera.main != null ? Camera.main.transform : null;
            if (headTransform == null) return;

            // Constants matching ComfortModeController
            float distanceFromHead = 0.4f;
            float heightOffset = -0.15f;
            float tiltAngle = 15f;

            // Get forward direction, keeping it horizontal
            Vector3 forward = headTransform.forward;
            forward.y = 0f;
            forward.Normalize();

            // Calculate position
            Vector3 targetPosition = headTransform.position + forward * distanceFromHead;
            targetPosition.y += heightOffset;

            // Calculate rotation (facing the user with tilt)
            Quaternion targetRotation = Quaternion.LookRotation(-forward) * Quaternion.Euler(tiltAngle, 0f, 0f);

            canvasTransform.position = targetPosition;
            canvasTransform.rotation = targetRotation;

        }

        private const float DefaultPlacementDistance = 0.4f;

        private System.Collections.IEnumerator PositionCanvasWithDelay(Transform canvasTransform)
        {
            // Hide canvas until calibration is complete (avoids visible jump)
            if (canvasTransform != null)
            {
                canvasTransform.gameObject.SetActive(false);
            }

            Transform headTransform = Camera.main != null ? Camera.main.transform : null;
            if (headTransform == null)
            {
                Debug.LogError("[GameBootstrap] FATAL: Camera.main is null at startup.");
                if (canvasTransform != null)
                    canvasTransform.gameObject.SetActive(true);
                yield break;
            }

            // Show loading spinner (must be outside mainUICanvas to be visible during calibration)
            if (loadingSpinner != null)
            {
                loadingSpinner.Show("Calibrando...");
            }

            // WAIT FOR TRACKING STABILIZATION: 2 seconds
            yield return new WaitForSeconds(2.0f);

            // FINAL POSITIONING: Use actual head height
            Vector3 finalHeadPos = headTransform.position;
            
            // Validate head height is reasonable
            if (finalHeadPos.y < 1.2f)
            {
                Debug.LogWarning($"[GameBootstrap] Head too low ({finalHeadPos.y}m). Using default 1.6m.");
                finalHeadPos.y = 1.6f;
            }
            else if (finalHeadPos.y > 2.0f)
            {
                Debug.LogWarning($"[GameBootstrap] Head too high ({finalHeadPos.y}m). Clamping to 2.0m.");
                finalHeadPos.y = 2.0f;
            }

            // Recalculate forward direction
            Vector3 forward = headTransform.forward;
            forward.y = 0f;
            forward.Normalize();

            float heightOffset = -0.15f;
            float distance = DefaultPlacementDistance;

            // Calculate final target position with actual head height
            Vector3 targetPosition = finalHeadPos + forward * distance;
            targetPosition.y += heightOffset;

            // Update rotations
            Quaternion canvasRotation = Quaternion.LookRotation(forward);
            Quaternion boardRotation = canvasRotation * Quaternion.Euler(0f, 180f, 0f); // Exactly 180° difference

            // Set final positions
            canvasTransform.position = targetPosition;
            canvasTransform.rotation = canvasRotation;

            if (puzzleBoard != null)
            {
                puzzleBoard.transform.position = targetPosition;
                puzzleBoard.transform.rotation = boardRotation;
            }

            // Show canvas only after calibration is complete
            if (canvasTransform != null)
            {
                canvasTransform.gameObject.SetActive(true);
            }
            
            // Hide loading spinner
            if (loadingSpinner != null)
            {
                loadingSpinner.Hide();
            }
        }

        private System.Collections.IEnumerator RefreshHUDAfterDelay(int correctPieces)
        {
            yield return new UnityEngine.WaitForSeconds(0.1f);
            
            if (puzzleHUD != null)
            {
                puzzleHUD.UpdatePiecesPlaced(correctPieces);
            }
        }

        /// <summary>
        /// Determines the frame tier based on piece count (difficulty).
        /// Easy = Bronce, Normal = Plata, Hard = Oro, Expert = Platinum
        /// </summary>
        private FrameTier GetFrameTierFromPieceCount(int pieceCount)
        {
            // Easy: 64 pieces
            if (pieceCount <= 64)
                return FrameTier.Bronce;
            
            // Normal: ~130-144 pieces (anything between Easy and Hard)
            if (pieceCount <= 200)
                return FrameTier.Plata;
            
            // Hard: 256 pieces
            if (pieceCount <= 256)
                return FrameTier.Oro;
            
            // Expert: 512 pieces
            return FrameTier.Platinum;
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
