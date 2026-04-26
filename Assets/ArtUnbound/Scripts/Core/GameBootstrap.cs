using System;
using System.Collections;
using System.Linq;
using ArtUnbound.Data;
using ArtUnbound.Feedback;
using ArtUnbound.Gameplay;
using ArtUnbound.MR;
using ArtUnbound.Services;
using ArtUnbound.UI;
using ArtUnbound.VR;
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
        [SerializeField] private ArtworkPackCatalog artworkPackCatalog;

    [Header("General UI")]
    [SerializeField] private Transform mainUICanvas;
    [SerializeField] private LoadingSpinner loadingSpinner;

    [Header("UI Controllers")]
        [SerializeField] private NativeGalleryController nativeGallery;          // Galería nativa (Prime Video style)
        [SerializeField] private PuzzleHUDController puzzleHUD;
        [SerializeField] private PuzzleAchievementsController puzzleAchievements;
        [SerializeField] private ArtUnbound.UI.PiecesPanelController puzzlePiecesPanel;
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
        
        [Header("MR Services")]
        [SerializeField] private SpatialPermissionService spatialPermissionService;
        [SerializeField] private WallDetectionService wallDetectionService;
        [SerializeField] private ArtworkHangingController artworkHangingController;
        [SerializeField] private WallAnchorManager wallAnchorManager;

        [Header("VR Mode")]
        [SerializeField] private VRModeController vrModeController;
        [SerializeField] private VRGalleryController vrGalleryController;
        [SerializeField] private VRLocomotionController vrLocomotionController;
        [SerializeField] private VRWallHangingController vrWallHangingController;
        [SerializeField] private GalleryCatalog galleryCatalog;

        [Header("Pack System")]
        [SerializeField] private PackPurchaseService packPurchaseService;

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
        private GalleryPersistenceService galleryPersistenceService;

        private string selectedArtworkId;
        private int selectedPieceCount = 64;
        private int selectedDifficultyIndex = 0; // 0=Easy(Bronce), 1=Normal(Plata), 2=Hard(Oro), 3=Expert(Platinum)
        private bool _boardPositioned = false; // Board position is calculated once and never changed again

        private bool _isQuest3OrPro;
        private Transform _nativeGalleryOriginalParent;

        /// <summary>True when the game is running in VR mode (no passthrough).</summary>
        public bool IsVRMode => vrModeController != null && vrModeController.IsVRMode;

        /// <summary>Exposes the active gallery ID for use by NativeGalleryController.</summary>
        public string ActiveVRGalleryId => vrGalleryController != null ? vrGalleryController.ActiveGalleryId : "gallery_classic";

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

            ApplyButtonTheme(); // Apply base theme first so filter/tab-specific colors aren't overwritten
            InitializeServices();
            HideAllPanels(); // Ensure clean state immediately
            LoadData();
            SetupEventListeners();
        }

        private void Start()
        {
            DetectDevice();

            bool shouldStartInVR = SaveData.preferVRMode || !_isQuest3OrPro;

            if (shouldStartInVR)
            {
                // VR path: skip passthrough setup, go straight to gallery
                TransitionToVRGallery();
            }
            else
            {
                // MR path (existing behavior)
                SetupCameraForPassthrough();
                HideAllPanels();

                if (mainUICanvas != null)
                    mainUICanvas.gameObject.SetActive(false);

                if (!SaveData.onboardingCompleted && onboardingController != null)
                    ShowOnboarding();
                else
                    TransitionToMainMenu();
            }

            // Update VR buttons on NativeGallery
            nativeGallery?.SetVRButtonsMode(IsVRMode, _isQuest3OrPro);

            if (!IsVRMode)
            {
                // Position the UI Canvas ergonomically (MR only)
                if (mainUICanvas != null)
                    StartCoroutine(PositionCanvasWithDelay(mainUICanvas));

                // Load saved spatial anchors (MR only)
                if (wallAnchorManager != null)
                    StartCoroutine(LoadAnchorsWithDelay());
            }
        }
        
        /// <summary>
        /// Coroutine to load spatial anchors after tracking stabilizes.
        /// </summary>
        private IEnumerator LoadAnchorsWithDelay()
        {
            // Wait for tracking to be stable (same delay as canvas positioning)
            yield return new WaitForSeconds(2f);
            
            Debug.Log("[GameBootstrap] Loading saved spatial anchors");
            wallAnchorManager.LoadAndSpawnAnchors();
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

            // Initialize pack purchase service
            if (packPurchaseService != null)
            {
                packPurchaseService.Initialize(saveDataService);
            }

            // Initialize NativeGallery (galería Prime Video style)
            if (nativeGallery != null)
            {
                nativeGallery.Initialize(localCatalogService, saveDataService);
                nativeGallery.SetPackPurchaseService(packPurchaseService);
            }

            // Initialize WallAnchorManager with SaveDataService
            if (wallAnchorManager != null)
            {
                wallAnchorManager.Initialize(saveDataService);
                Debug.Log("[GameBootstrap] WallAnchorManager initialized");
            }

            // VR services
            galleryPersistenceService = new GalleryPersistenceService(saveDataService);

            if (vrGalleryController != null)
                vrGalleryController.Initialize(saveDataService, galleryPersistenceService);

            if (vrWallHangingController != null)
                vrWallHangingController.Initialize(galleryPersistenceService);
        }

        private void DetectDevice()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                var headset = OVRPlugin.GetSystemHeadsetType();
                _isQuest3OrPro = headset == OVRPlugin.SystemHeadset.Meta_Quest_3
                              || headset == OVRPlugin.SystemHeadset.Meta_Quest_Pro
                              || headset == OVRPlugin.SystemHeadset.Meta_Quest_3S;
            }
            catch
            {
                // OVRPlugin not available — assume Quest 3
                _isQuest3OrPro = true;
            }
#else
            // In Editor, treat as Quest 3 so MR mode is default for testing
            _isQuest3OrPro = true;
#endif
            Debug.Log($"[GameBootstrap] Device: Quest3/Pro={_isQuest3OrPro}");
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
            // Unified Main Menu - OnStartPuzzle subscribed below with other handlers

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
                puzzleHUD.OnExitRequested += QuitToMenu;
                puzzleHUD.OnHighlightWrongRequested += HighlightWrongPieces;
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

            // NativeGallery
            if (nativeGallery != null)
            {
                nativeGallery.OnStartPuzzle    += OnUnifiedMenuStartPuzzle;
                nativeGallery.OnSettingsChanged += OnNativeGallerySettingsChanged;
                nativeGallery.OnVRModeRequested += OnVRModeRequested;
                nativeGallery.OnMRModeRequested += OnMRModeRequested;
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
            HidePiecesPagination();
            HideAllPanels();

            // Start music first so CurrentTrack is set before menu shows
            if (audioManager != null)
                audioManager.StartMusicPlayback();

            if (nativeGallery != null)
            {
                nativeGallery.Show();
            }
            else
            {
                Debug.LogError("[GameBootstrap] NativeGallery no está asignado en el Inspector.");
            }
        }

        // ─── VR Gallery Transitions ───────────────────────────────────────────

        public void TransitionToVRGallery()
        {
            SetState(GameState.VRGallery);
            HideAllPanels();

            if (vrModeController != null && !vrModeController.IsVRMode)
                vrModeController.ActivateVRMode();

            // Ensure canvas is visible for the VR navigation panel
            if (mainUICanvas != null)
                mainUICanvas.gameObject.SetActive(true);

            string galleryId = SaveData?.lastGalleryId ?? "gallery_classic";
            vrGalleryController?.LoadGallery(galleryId);

            if (vrLocomotionController != null)
                vrLocomotionController.gameObject.SetActive(true);

            // Show NativeGallery as the navigation panel in VR
            if (audioManager != null)
                audioManager.StartMusicPlayback();

            if (nativeGallery != null)
            {
                // Detach from any camera-following parent so the panel stays fixed in world space.
                // In MR the NativeGallery may be parented under mainUICanvas which tracks the camera.
                if (_nativeGalleryOriginalParent == null)
                    _nativeGalleryOriginalParent = nativeGallery.transform.parent;
                // worldPositionStays:true preserves the panel's current world position
                // so it doesn't snap to origin when detached from the canvas parent.
                nativeGallery.transform.SetParent(null, worldPositionStays: true);
                nativeGallery.ResetPosition();

                nativeGallery.SetVRButtonsMode(true, _isQuest3OrPro);
                if (vrModeController != null)
                    nativeGallery.SetPositioningCamera(vrModeController.MainCamera);
                nativeGallery.Show();
            }

            Debug.Log("[GameBootstrap] Transitioned to VRGallery");
        }

        /// <summary>Switches the active VR gallery. Called from NativeGalleryController.</summary>
        public void SwitchVRGallery(string galleryId)
        {
            vrGalleryController?.SwitchGallery(galleryId);
        }

        /// <summary>Called when user taps "VR" button in MR mode (Quest 3/Pro only).</summary>
        private void OnVRModeRequested()
        {
            SaveData.preferVRMode = true;
            saveDataService.MarkDirty();
            TransitionToVRGallery();
        }

        /// <summary>Called when user taps "MR" button in VR mode (Quest 3/Pro only).</summary>
        private void OnMRModeRequested()
        {
            SaveData.preferVRMode = false;
            saveDataService.MarkDirty();

            // Destroy the 3-D gallery environment BEFORE disabling VR systems so the
            // room prefab (which lives outside vrGalleryController's hierarchy) is removed.
            vrGalleryController?.UnloadGallery();
            vrModeController?.DeactivateVRMode();
            nativeGallery?.SetVRButtonsMode(false, _isQuest3OrPro);

            // Re-attach NativeGallery to its original parent (under mainUICanvas)
            if (nativeGallery != null && _nativeGalleryOriginalParent != null)
                nativeGallery.transform.SetParent(_nativeGalleryOriginalParent, worldPositionStays: true);

            // Force repositioning in front of user when the menu shows in MR
            nativeGallery?.ResetPosition();

            SetupCameraForPassthrough();
            TransitionToMainMenu();
        }

        /// <summary>
        /// Handler for UnifiedMainMenu's OnStartPuzzle event.
        /// Receives artworkId, pieceCount, and difficultyIndex (0=Easy, 1=Normal, 2=Hard, 3=Expert).
        /// </summary>
        private void OnUnifiedMenuStartPuzzle(string artworkId, int pieceCount, int difficultyIndex)
        {
            selectedArtworkId = artworkId;
            selectedPieceCount = pieceCount;
            selectedDifficultyIndex = difficultyIndex;
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
                puzzleHUD.SetDifficulty(selectedDifficultyIndex);

                // Set artwork information in HUD
                var currentArtwork = localCatalogService?.GetById(selectedArtworkId);
                if (currentArtwork != null)
                {
                    puzzleHUD.SetArtworkInfo(
                        currentArtwork.title,
                        currentArtwork.author,
                        currentArtwork.description
                    );
                    puzzleHUD.SetArtworkReference(currentArtwork.fullImage);
                }

                puzzleHUD.Show();
            }

            // Armando rompecabezas: mostrar instrucciones de piezas y panel de logros (milestones)
            puzzlePiecesPanel?.Show();
            puzzleAchievements?.Show();

            // Timer is already started in InitializePuzzleBoard(), so don't restart it here
            // if (timerController != null)
            //     timerController.StartTimer();

            // Music continues from menu - no change needed
        }


        #endregion

        #region Event Handlers

        private void StartPuzzle()
        {
            // Reset any hanging state from a previous completed puzzle
            CleanupArtworkHanging();

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
            if (!_boardPositioned && puzzleBoard != null)
            {
                puzzleBoard.transform.position = position;
                puzzleBoard.transform.rotation = rotation;
                _boardPositioned = true;
            }

            if (!InitializePuzzleBoard())
                TransitionToPlaying();
        }

        private void OnComfortPositionLocked()
        {
            // Position board only once — never move it again after first placement
            if (!_boardPositioned && puzzleBoard != null && mainUICanvas != null)
            {
                puzzleBoard.transform.position = mainUICanvas.position;
                Quaternion boardRotation = mainUICanvas.rotation * Quaternion.Euler(0f, 180f, 0f);
                puzzleBoard.transform.rotation = boardRotation;
                _boardPositioned = true;
            }

            if (!InitializePuzzleBoard())
                TransitionToPlaying();
        }

        /// <returns>True if restored a completed puzzle (caller should skip TransitionToPlaying).</returns>
        private bool InitializePuzzleBoard()
        {
            if (puzzleBoard == null) return false;

            // Ensure board is active and visible
            puzzleBoard.gameObject.SetActive(true);

            var artworkData = localCatalogService?.GetArtworkById(selectedArtworkId);
            Texture2D artworkTexture = artworkData?.fullImage?.texture;

            if (artworkData == null)
            {
                Debug.LogError($"[GameBootstrap] No artwork found for id='{selectedArtworkId}'. Pieces will NOT appear.");
            }
            else if (artworkTexture == null)
            {
                Debug.LogError($"[GameBootstrap] Artwork '{selectedArtworkId}' has no texture (fullImage.texture is null). Pieces will NOT appear.");
            }

            // Prefer Initialize(ArtworkDefinition) - it uses puzzleTexture or fullImage.texture
            if (artworkData != null)
            {
                puzzleBoard.Initialize(artworkData, selectedPieceCount);
            }
            else
            {
                puzzleBoard.Initialize(selectedPieceCount, artworkTexture);
            }
            
            // IMPORTANT: Update selectedPieceCount to the ACTUAL piece count
            // (may differ from target due to aspect ratio)
            int actualPieceCount = puzzleBoard.TotalPieces;
            if (actualPieceCount != selectedPieceCount)
            {
                selectedPieceCount = actualPieceCount;
            }
            
            // Re-initialize HUD with the ACTUAL piece count and artwork info
            if (puzzleHUD != null)
            {
                puzzleHUD.Initialize(actualPieceCount, true); // Help mode always enabled
                puzzleHUD.SetDifficulty(selectedDifficultyIndex);
                if (artworkData != null)
                {
                    puzzleHUD.SetArtworkInfo(
                        artworkData.title,
                        artworkData.author,
                        artworkData.description
                    );
                    puzzleHUD.SetArtworkReference(artworkData.fullImage);
                }
            }
            
            // Check for saved session (in-progress or completed)
            var savedSession = saveDataService.LoadSession(selectedArtworkId, actualPieceCount);
            var displaySession = savedSession ?? saveDataService.LoadSessionForDisplay(selectedArtworkId, actualPieceCount);

            if (displaySession != null && displaySession.placedPieces != null && displaySession.placedPieces.Count > 0)
            {
                CurrentSession = displaySession;
                CurrentSession.pieceCount = actualPieceCount;

                puzzleBoard.RestoreBoardState(displaySession.placedPieces);

                if (puzzleHUD != null)
                {
                    puzzleHUD.UpdateProgress(puzzleBoard.SnappedCount, puzzleBoard.IncorrectCount);
                    if (puzzleBoard.SnappedCount > 0)
                        StartCoroutine(RefreshHUDAfterDelay());
                }

                if (displaySession.isCompleted)
                {
                    SetState(GameState.PostGame);
                    // Ocultar la galería sin usar HideAllPanels (que también oculta el puzzleBoard)
                    nativeGallery?.Hide();
                    var record = SaveData.GetProgress(selectedArtworkId)?.GetRecordForPieceCount(actualPieceCount);
                    int timeSec = record?.bestTimeSec ?? displaySession.GetElapsedSeconds();
                    int difficultyIdx = GetDifficultyIndexFromPieceCount(selectedArtworkId, actualPieceCount);
                    FrameTier frameTier = record?.bestFrameTier ?? GetFrameTierFromDifficultyIndex(difficultyIdx);

                    if (timerController != null)
                    {
                        timerController.SetElapsedTime(timeSec);
                        timerController.StopTimer();
                    }
                    puzzleBoard?.HideScrollButtons();
                    puzzleBoard?.ShowFullImageReveal(frameTier);

                    int wallCount = IsVRMode ? 1 : DetectAndLogWalls();
                    if (postGameController != null)
                        postGameController.ShowResults(displaySession, frameTier, wallCount);

                    if (puzzleHUD != null) puzzleHUD.Show();
                    puzzleAchievements?.Show();
                    return true; // Skip TransitionToPlaying - we're in PostGame
                }
                else
                {
                    if (timerController != null)
                        timerController.StartTimer(displaySession.elapsedTime);
                }
            }
            else
            {
                if (CurrentSession != null)
                    CurrentSession.pieceCount = actualPieceCount;

                if (timerController != null)
                    timerController.StartTimer(0f);
            }
            return false;
        }
        private void OnPieceCorrectlyPlaced(PuzzlePiece piece)
        {
            if (CurrentSession != null)
            {
                CurrentSession.piecesPlaced++;
            }

            // HUD updated by OnBoardStateChanged

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
            // Don't call HideAllPanels - it hides puzzleBoard; we need board+full image visible
            puzzlePiecesPanel?.Hide();

            if (timerController != null)
                timerController.StopTimer();

            if (CurrentSession != null)
            {
                CurrentSession.isCompleted = true;
                CurrentSession.EndSession();
            }

            // Keep completed session in storage so user can return and see the assembled puzzle
            if (CurrentSession != null)
            {
                CurrentSession.placedPieces = puzzleBoard.GetCurrentBoardState();
                saveDataService.SaveSession(CurrentSession);
            }

            // Get completion time from the timer (simple counter, source of truth)
            int timeSec = timerController != null ? timerController.GetElapsedSeconds() : (CurrentSession?.GetElapsedSeconds() ?? 1);
            
            // Frame tier based on difficulty SIZE (Easy=Bronce, Normal=Plata, Hard=Oro, Expert=Platinum)
            FrameTier frameTier = GetFrameTierFromDifficultyIndex(selectedDifficultyIndex);

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
            {
                puzzleBoard.HideScrollButtons();
                puzzleBoard.ShowFullImageReveal(frameTier);
            }

            // Keep board and HUD visible, only show post game panel on top
            // puzzleHUD?.Hide(); // Uncomment if you want to hide HUD

            // In VR mode gallery always has walls; in MR detect AR planes
            int wallCount = IsVRMode ? 1 : DetectAndLogWalls();

            if (postGameController != null)
            {
                postGameController.ShowResults(CurrentSession, frameTier, wallCount);
            }
            else
            {
                Debug.LogError("[GameBootstrap] postGameController is NULL! Cannot show post-game panel.");
            }
        }


        private void QuitToMenu()
        {
            SaveCurrentSessionIfPlaying();
            CurrentSession = null;

            if (IsVRMode)
                TransitionToVRGallery();
            else
                TransitionToMainMenu();
        }

        /// <summary>
        /// Highlights all incorrectly placed pieces with a red burst + wiggle + sound.
        /// Triggered by the "highlight wrong" button in the HUD.
        /// </summary>
        private void HighlightWrongPieces()
        {
            if (puzzleBoard == null) return;

            var wrongPieces = puzzleBoard.GetIncorrectlyPlacedPieces();
            if (wrongPieces.Count == 0) return;

            // Play incorrect sound once for all pieces
            ArtUnbound.Feedback.AudioManager.Instance?.PlayPieceIncorrect();

            StartCoroutine(HighlightWrongPiecesStaggered(wrongPieces));
        }

        private System.Collections.IEnumerator HighlightWrongPiecesStaggered(
            System.Collections.Generic.List<ArtUnbound.Gameplay.PuzzlePiece> pieces)
        {
            float stagger = 0.08f; // seconds between each piece highlight
            foreach (var piece in pieces)
            {
                if (piece == null) continue;
                piece.PlayWiggleEffect();
                ArtUnbound.Feedback.PieceEffectsManager.Instance?.PlayWrongPlacementHighlight(piece.transform.position);
                yield return new WaitForSeconds(stagger);
            }
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
            
            // Update HUD with correct/incorrect counts
            if (puzzleHUD != null)
                puzzleHUD.UpdateProgress(puzzleBoard.SnappedCount, puzzleBoard.IncorrectCount);
            
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
            FrameTier frameTier = postGameController?.GetAwardedFrame() ?? FrameTier.Bronce;

            if (IsVRMode)
            {
                // VR path: use VRWallHangingController
                if (vrWallHangingController != null)
                {
                    vrWallHangingController.EnableFrameGrab(selectedArtworkId, selectedDifficultyIndex, frameTier);
                    ArtUnbound.Input.InteractionManager.BlockFrameSelectFor(0.5f);

                    if (puzzleBoard != null)
                        puzzleBoard.EnableFrameInteraction(true);

                    vrWallHangingController.OnFrameGrabbed += OnFrameGrabbed;
                    vrWallHangingController.OnFramePlaced  += OnVRFramePlaced;
                    vrWallHangingController.OnPlacementCancelled += OnPlacementCancelled;

                    postGameController?.SetHangingMode(true);
                }
                return;
            }

            // MR path (original behavior)
            if (artworkHangingController != null)
            {
                // Enable frame grabbing
                artworkHangingController.EnableFrameGrab(selectedArtworkId, frameTier);

                // Block all InteractionManagers from selecting the frame for 0.5s so the
                // trigger press that triggered this flow (e.g., confirming comfort position
                // or tapping a gallery card) cannot immediately select the frame on TriggerUp.
                ArtUnbound.Input.InteractionManager.BlockFrameSelectFor(0.5f);
                
                // Enable frame interaction on puzzle board
                if (puzzleBoard != null)
                {
                    puzzleBoard.EnableFrameInteraction(true);
                }
                
                // Subscribe to hanging events
                artworkHangingController.OnFrameGrabbed += OnFrameGrabbed;
                artworkHangingController.OnFramePlaced += OnFramePlaced;
                artworkHangingController.OnPlacementCancelled += OnPlacementCancelled;
                
                // Disable replay button so controller trigger can't accidentally click it
                // while the user is pointing at the frame to grab it.
                postGameController?.SetHangingMode(true);

                // DON'T hide panels yet - wait until user grabs the frame
                // HideAllPanels(); // Moved to OnFrameGrabbed()

                Debug.Log($"[GameBootstrap] Started artwork hanging flow for {selectedArtworkId}");
            }
            else
            {
                Debug.LogError("[GameBootstrap] ArtworkHangingController is null - cannot start hanging flow");
            }
        }

        private void OnFrameGrabbed()
        {
            Debug.Log("[GameBootstrap] Frame grabbed - hiding UI, placement mode active");
            
            // NOW hide UI panels to allow free movement
            HideAllPanels();
            
            // Play haptic feedback
            if (hapticController != null)
            {
                hapticController.PlayLightHaptic();
            }
        }

    private void OnFramePlaced()
    {
        Debug.Log($"[GameBootstrap] Frame placed successfully for {selectedArtworkId}");
        
        // Play success feedback
        if (audioManager != null)
        {
            audioManager.PlayPuzzleComplete(); // Reuse completion sound
        }
        
        if (hapticController != null)
        {
            hapticController.PlaySuccessHaptic();
        }
        
        // Hide the original PuzzleBoard (the clone is now on the wall)
        // DO NOT destroy it - it's reused for all puzzles
        if (puzzleBoard != null)
        {
            puzzleBoard.gameObject.SetActive(false);
            Debug.Log("[GameBootstrap] PuzzleBoard hidden after successful placement");
        }
        
        // Cleanup the hanging controller (unsubscribe events, etc)
        CleanupArtworkHanging();
        
        // Go directly to main menu (no need to show PostGame panel)
        Debug.Log("[GameBootstrap] Returning to main menu after artwork placement");
        TransitionToMainMenu();
    }

    private void OnPlacementCancelled()
    {
        Debug.Log("[GameBootstrap] Artwork placement cancelled - frame remains grabbable");

        // Do NOT call CleanupArtworkHanging() here — that would disable IsGrabbable on the
        // completed frame and prevent the user from trying to hang it again without restarting.
        // Cleanup only happens in OnFramePlaced (successful hang) or StartPuzzle (new puzzle).

        // Re-enable CenterZone (puzzleAchievements is the CenterZone GameObject)
        if (puzzleAchievements != null)
        {
            puzzleAchievements.gameObject.SetActive(true);
            Debug.Log("[GameBootstrap] CenterZone (puzzleAchievements) re-enabled");
        }
        
        // Show PostGame panel and board again; re-enable replay button
        if (postGameController != null)
        {
            postGameController.SetHangingMode(false);
            postGameController.Show();
            Debug.Log("[GameBootstrap] PostGame panel re-shown");
        }
        
        if (puzzleBoard != null)
        {
            puzzleBoard.gameObject.SetActive(true);
            Debug.Log("[GameBootstrap] PuzzleBoard re-enabled");
        }
        
        // Re-show the HUD
        if (puzzleHUD != null)
        {
            puzzleHUD.Show();
            Debug.Log("[GameBootstrap] PuzzleHUD re-shown");
        }
        else
        {
            Debug.LogWarning("[GameBootstrap] puzzleHUD is null!");
        }
    }

        private void OnVRFramePlaced()
        {
            Debug.Log($"[GameBootstrap] VR Frame placed for {selectedArtworkId}");

            if (audioManager != null)
                audioManager.PlayPuzzleComplete();
            if (hapticController != null)
                hapticController.PlaySuccessHaptic();

            if (puzzleBoard != null)
            {
                puzzleBoard.gameObject.SetActive(false);
            }

            CleanupArtworkHanging();
            TransitionToVRGallery();
        }

        private void CleanupArtworkHanging()
        {
            // MR hanging cleanup
            if (artworkHangingController != null)
            {
                artworkHangingController.OnFrameGrabbed -= OnFrameGrabbed;
                artworkHangingController.OnFramePlaced -= OnFramePlaced;
                artworkHangingController.OnPlacementCancelled -= OnPlacementCancelled;
                artworkHangingController.DisableFrameGrab();
            }

            // VR hanging cleanup
            if (vrWallHangingController != null)
            {
                vrWallHangingController.OnFrameGrabbed -= OnFrameGrabbed;
                vrWallHangingController.OnFramePlaced -= OnVRFramePlaced;
                vrWallHangingController.OnPlacementCancelled -= OnPlacementCancelled;
                vrWallHangingController.DisableFrameGrab();
            }

            if (puzzleBoard != null)
            {
                puzzleBoard.EnableFrameInteraction(false);
            }
        }

        private void ReplayPuzzle()
        {
            // Clear session so we start fresh (record/time/frame stay in ArtworkProgress)
            saveDataService.ClearSession(selectedArtworkId, selectedPieceCount);
            postGameController?.Hide();

            // Create fresh session and re-initialize board (pieces back to tray)
            CurrentSession = new PuzzleSessionData
            {
                artworkId = selectedArtworkId,
                pieceCount = selectedPieceCount,
                gameMode = CurrentGameMode,
                helpModeUsed = true
            };
            CurrentSession.StartSession();

            InitializePuzzleBoard();
            TransitionToPlaying();
        }

        private void OnOnboardingComplete()
        {
            saveDataService.CompleteOnboarding();
            SaveData = saveDataService.GetCachedData();

            TransitionToMainMenu();
        }

        /// <summary>
        /// Callback cuando el usuario mueve un slider en la NativeGallery.
        /// Persiste los valores y los aplica inmediatamente al AudioManager.
        /// </summary>
        private void OnNativeGallerySettingsChanged(float music, float sfx, bool haptics)
        {
            if (SaveData?.settings != null)
            {
                SaveData.settings.musicVolume    = music;
                SaveData.settings.sfxVolume      = sfx;
                SaveData.settings.hapticsEnabled = haptics;
                saveDataService.MarkDirty();
            }
            ApplySettings(SaveData?.settings);
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

        private void ApplyButtonTheme()
        {
            if (mainUICanvas != null)
                ArtUnbound.UI.UIButtonTheme.ApplyToAllIn(mainUICanvas);
            if (puzzleBoard != null)
                ArtUnbound.UI.UIButtonTheme.ApplyToAllIn(puzzleBoard.transform);
        }

        private void HideAllPanels()
        {
            nativeGallery?.Hide();
            puzzleHUD?.Hide();
            puzzleAchievements?.Hide();
            puzzlePiecesPanel?.Hide();
            postGameController?.Hide();
            onboardingController?.Hide();
            
            if (puzzleBoard != null)
                puzzleBoard.gameObject.SetActive(false);
        }

        /// <summary>Call when leaving puzzle (quit or complete) to hide pieces pagination panel.</summary>
        private void HidePiecesPagination()
        {
            puzzleBoard?.HideScrollButtons();
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

            // Constants matching ComfortModeController (Meta MR guideline: 45cm for direct hand interaction)
            float distanceFromHead = 0.45f;
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

        private const float DefaultPlacementDistance = 0.55f; // 55cm from user (10cm further than original 45cm)

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

            yield return null; // Wait one frame so Canvas/Button state is settled before applying colors

            // Hide loading spinner
            if (loadingSpinner != null)
            {
                loadingSpinner.Hide();
            }
        }

        private System.Collections.IEnumerator RefreshHUDAfterDelay()
        {
            yield return new UnityEngine.WaitForSeconds(0.1f);
            if (puzzleHUD != null && puzzleBoard != null)
                puzzleHUD.UpdateProgress(puzzleBoard.SnappedCount, puzzleBoard.IncorrectCount);
        }

        /// <summary>
        /// Determines the frame tier based on difficulty SIZE (which puzzle size was selected).
        /// Easy = Bronce, Normal = Plata, Hard = Oro, Expert = Platinum
        /// </summary>
        private FrameTier GetFrameTierFromDifficultyIndex(int difficultyIndex)
        {
            return difficultyIndex switch
            {
                0 => FrameTier.Bronce,   // Easy (smallest)
                1 => FrameTier.Plata,   // Normal
                2 => FrameTier.Oro,     // Hard
                3 => FrameTier.Platinum,// Expert (largest)
                _ => FrameTier.Bronce
            };
        }

        /// <summary>
        /// Infers difficulty index from piece count by matching against artwork's piece counts.
        /// Used when restoring a completed puzzle (we don't have the original difficulty index).
        /// Falls back to PuzzleConfig.pieceCounts if artwork has no custom counts.
        /// </summary>
        private int GetDifficultyIndexFromPieceCount(string artworkId, int pieceCount)
        {
            var artwork = localCatalogService?.GetArtworkById(artworkId);
            if (artwork != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (artwork.GetPieceCount(i) == pieceCount)
                        return i;
                }
            }
            if (puzzleConfig != null && puzzleConfig.pieceCounts != null)
            {
                for (int i = 0; i < puzzleConfig.pieceCounts.Length && i < 4; i++)
                {
                    if (puzzleConfig.pieceCounts[i] == pieceCount)
                        return i;
                }
            }
            return 0; // Default to Easy if no match
        }

        /// <summary>
        /// Detects walls in the room and logs the result. Called when entering PostGame.
        /// Returns the number of walls detected (0 if none or service unavailable).
        /// </summary>
        private int DetectAndLogWalls()
        {
            var service = wallDetectionService != null
                ? wallDetectionService
                : FindFirstObjectByType<WallDetectionService>();

            if (service != null)
            {
                service.EnsureWallDetectionEnabled();
                return service.DetectWalls();
            }
            
            Debug.LogWarning("[GameBootstrap] WallDetectionService not found. Wall detection disabled.");
            return 0;
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
        VRGallery,
        ArtworkSelection,
        WallSelection,
        ComfortPositioning,
        Playing,
        Paused,
        PostGame
    }
}
