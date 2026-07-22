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
        /// <summary>Catalogo de obras (fallback para VRGalleryController al reconstruir cuadros colgados).</summary>
        public ArtworkCatalog ArtworkCatalog => artworkCatalog;
        [SerializeField] private PuzzleConfig puzzleConfig;
        [SerializeField] private FrameConfigSet frameConfigSet;
        [Tooltip("Catalogo de coleccionables (placas + mejoras). Generar con Tools/ArtUnbound/Generate Collectibles.")]
        [SerializeField] private CollectibleCatalog collectibleCatalog;
        /// <summary>Catalogo de coleccionables (lo usa WallAnchorManager para reconstruir placas colgadas al cargar).</summary>
        public CollectibleCatalog CollectibleCatalog => collectibleCatalog;
        [Tooltip("Prefab de la cedula de museo (Tools/ArtUnbound/Create Cedula In Scene). Vacio = molde procedural.")]
        [SerializeField] private GameObject cedulaPrefab;
        public GameObject CedulaPrefab => cedulaPrefab;

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

        [Header("Tutorial (mano fantasma)")]
        [Tooltip("Tutorial guiado de primera vez (reemplaza al carrusel de onboarding, que ya no se invoca).")]
        [SerializeField] private ArtUnbound.Tutorial.TutorialFlowController tutorialFlow;

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
        [SerializeField] private VRWallHangingController vrWallHangingController;
        [SerializeField] private GalleryCatalog galleryCatalog;

        [Header("Pack System")]
        [SerializeField] private PackPurchaseService packPurchaseService;

        [Header("Dev Cheats")]
        [Tooltip("DEV ONLY: muestra un boton verde 'DEV' en el HUD que auto-completa el puzzle actual. Usado para preparar cuadros colgados para capturas de marketing. APAGAR antes de builds de release.")]
        [SerializeField] private bool enableDevCheats = false;

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
        private CollectibleService collectibleService;

        /// <summary>Servicio de coleccionables (GDD 8.x). Lo consume Collection (Frente 5).</summary>
        public CollectibleService CollectibleService => collectibleService;

        /// <summary>Coleccionables recien otorgados al completar la ultima obra (linea de hito del post-juego).</summary>
        public System.Collections.Generic.List<ArtUnbound.Data.CollectibleDefinition> LastEarnedPlaques { get; private set; }
        private WeeklyUnlockService weeklyUnlockService;
        private LocalTelemetryService localTelemetryService;
        private GalleryPersistenceService galleryPersistenceService;

        private string selectedArtworkId;
        private int selectedPieceCount = 64;
        private int selectedDifficultyIndex = 0; // 0=A Coffee, 1=A Break, 2=A Movie (solo define el tamano de pieza; NO el marco — el material es global, GDD 4.2/8.4)
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

            InitializeServices();
            HideAllPanels(); // Ensure clean state immediately
            LoadData();
            SetupEventListeners();

            // El tutorial decide si se arma esta sesion (primera vez o toggle de Settings).
            tutorialFlow?.Initialize(saveDataService, SaveData);
        }

        private void Start()
        {
            DetectDevice();

            // Always boot in MR on Quest 3/Pro — the user opts into VR via the in-game button.
            // We intentionally don't persist the last mode: a previously-saved preferVRMode caused
            // VR→MR transitions to fail on cold-boot (passthrough never initialised), so we boot
            // fresh in MR every session. Non-Quest-3 devices fall back to VR (no passthrough).
            bool shouldStartInVR = !_isQuest3OrPro;

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

                // El carrusel de onboarding (OnboardingController) queda fuera del arranque a
                // proposito: el tutorial de mano fantasma (TutorialFlowController) ensena por
                // demostracion sobre la UI real, sin bloquear.
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
            collectibleService = new CollectibleService(saveDataService, localCatalogService, collectibleCatalog);

            // Initialize pack purchase service (+ arranca Meta IAP: entitlement, precio real y
            // restauracion de la compra desde Meta). En el Editor InitializePlatform es no-op.
            if (packPurchaseService != null)
            {
                packPurchaseService.Initialize(saveDataService);
                packPurchaseService.InitializePlatform();
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

            // Otorga retroactivamente las placas por umbral ya merecidas (autor/movimiento/estatus y las
            // mejoras Cedula/Frame/Lamp): asi quien ya cruzo el umbral recibe su placa al cargar, sin
            // esperar a completar otra obra. Solo otorga lo legitimamente ganado (idempotente).
            collectibleService?.EvaluateOnCompletion();

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
                postGameController.OnBackRequested += OnBackFromPostGame;
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
                // Salir por el HUD tambien limpia el flujo de colgado: en PostGame el agarre del marco
                // se auto-habilita al detectar pared, y sin esta limpieza los eventos quedarian suscritos
                // (y se duplicarian en la siguiente partida). CleanupArtworkHanging es no-op durante Playing.
                puzzleHUD.OnExitRequested += OnBackFromPostGame;
                puzzleHUD.OnHighlightWrongRequested += HighlightWrongPieces;

                if (enableDevCheats)
                {
                    puzzleHUD.EnableDevCompleteButton();
                    puzzleHUD.OnDevCompleteRequested += OnDevCompletePuzzle;
                }
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
                nativeGallery.OnHangArtworkRequested += OnHangArtworkFromDetail;
                nativeGallery.OnDetailClosed         += OnDetailClosed;
                nativeGallery.OnHangPlaqueRequested  += OnHangPlaqueFromCollection;
                nativeGallery.OnPlaqueViewCancelled  += OnPlaqueViewCancelled;
                // Toggle "Show tutorial on next launch": persistir; se evalua al arrancar.
                nativeGallery.OnTutorialToggleChanged += OnTutorialToggleChanged;
            }

            // PlaqueView: el panel lo posee NativeGalleryController (overlay sobre el menu, como el
            // detalle). GameBootstrap solo administra la placa 3D y escucha cuando se resuelve el colgado.
            if (artworkHangingController != null)
            {
                artworkHangingController.OnWallArtworkResolved += OnPlaqueHangResolved;
                artworkHangingController.OnWallArtworkResolved += OnDetailArtworkHangResolved;
            }
            // En VR el colgado de la placa se resuelve por VRWallHangingController (pared virtual +
            // persistencia de galeria), no por ArtworkHangingController. Escuchar ambos.
            if (vrWallHangingController != null)
            {
                vrWallHangingController.OnWallArtworkResolved += OnPlaqueHangResolved;
                vrWallHangingController.OnWallArtworkResolved += OnDetailArtworkHangResolved;
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

        public void TransitionToVRGallery(bool reloadGallery = true)
        {
            SetState(GameState.VRGallery);
            HideAllPanels();

            if (vrModeController != null && !vrModeController.IsVRMode)
                vrModeController.ActivateVRMode();

            // Hide MR-placed artworks — they live in real-world space and should not
            // appear inside the VR gallery environment.
            wallAnchorManager?.SetMRVisible(false);

            // Ensure canvas is visible for the VR navigation panel
            if (mainUICanvas != null)
                mainUICanvas.gameObject.SetActive(true);

            if (reloadGallery)
            {
                string galleryId = SaveData?.lastGalleryId ?? "gallery_classic";
                vrGalleryController?.LoadGallery(galleryId);
            }

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
                // so it stays at the same spot the user saw it in MR.
                // Do NOT call ResetPosition() here — keeping _hasBeenPositioned=true
                // prevents PositionAndReveal from recalculating and shifting the panel.
                nativeGallery.transform.SetParent(null, worldPositionStays: true);

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
            TransitionToVRGallery();
        }

        /// <summary>Called when user taps "MR" button in VR mode (Quest 3/Pro only).</summary>
        private void OnMRModeRequested()
        {
            // Destroy the 3-D gallery environment BEFORE disabling VR systems so the
            // room prefab (which lives outside vrGalleryController's hierarchy) is removed.
            vrGalleryController?.UnloadGallery();
            vrModeController?.DeactivateVRMode();
            nativeGallery?.SetVRButtonsMode(false, _isQuest3OrPro);

            // Re-attach NativeGallery to its original parent (under mainUICanvas)
            if (nativeGallery != null && _nativeGalleryOriginalParent != null)
                nativeGallery.transform.SetParent(_nativeGalleryOriginalParent, worldPositionStays: true);

            // Restore MR-placed artworks now that passthrough is back.
            wallAnchorManager?.SetMRVisible(true);

            SetupCameraForPassthrough();
            TransitionToMainMenu();
        }

        /// <summary>
        /// Handler for UnifiedMainMenu's OnStartPuzzle event.
        /// Receives artworkId and difficultyIndex (0=Easy, 1=Normal, 2=Hard). The actual piece
        /// count is derived by PuzzleBoard once the board is generated.
        /// </summary>
        private void OnUnifiedMenuStartPuzzle(string artworkId, int difficultyIndex)
        {
            // Defensa: nunca iniciar un puzzle de una obra bloqueada (no gratis y catalogo
            // no comprado). El detail panel ya lo previene en la UI; esto es respaldo.
            if (packPurchaseService != null)
            {
                var artwork = localCatalogService?.GetById(artworkId);
                if (packPurchaseService.IsArtworkLocked(artwork))
                {
                    Debug.LogWarning($"[GameBootstrap] Obra bloqueada, no se inicia puzzle: {artworkId}");
                    return;
                }
            }

            selectedArtworkId = artworkId;
            selectedDifficultyIndex = difficultyIndex;
            selectedPieceCount = 0; // Will be set by PuzzleBoard.TotalPieces after Initialize.
            StartPuzzle();
        }

        // Placa flotante activa en el flujo de PlaqueView (aun no resuelta). Permite que el boton Close
        // la destruya y que el handler de resolucion sepa cual placa se acaba de colgar/retirar.
        private GameObject _activePlaqueGO;
        private string _activePlaqueId;

        /// <summary>
        /// Collection (GDD 8.5): colgar una obra ya completada. La instancia como objeto colgable
        /// (tag "PlacedArtwork") frente al usuario, reutilizando el flujo de grab/colocacion existente.
        /// </summary>
        private GameObject _activeHangArtworkGO;
        private string _activeHangArtworkId;

        /// <summary>
        /// Detail: colgar una obra ya completada desde su panel de detalle (boton Hang). Instancia la
        /// obra 3D enmarcada (tag "PlacedArtwork") a un costado del panel para tomarla y colgarla,
        /// reutilizando el flujo de grab/colocacion existente. El menu NO se oculta (mismo patron que
        /// el PlaqueView); al cerrar el detalle sin colgar, OnDetailClosed destruye la obra flotante.
        /// </summary>
        private void OnHangArtworkFromDetail(string artworkId)
        {
            // Una sola obra flotante a la vez: si habia otra sin colgar, se reemplaza.
            if (_activeHangArtworkGO != null) Destroy(_activeHangArtworkGO);
            _activeHangArtworkGO = null;
            _activeHangArtworkId = null;

            // Dimensiones con el aspect ratio real de la obra (lado mayor = 0.5 m).
            float w = 0.5f, h = 0.5f;
            var def = artworkCatalog != null ? artworkCatalog.artworks?.Find(a => a != null && a.artworkId == artworkId) : null;
            Texture tex = def != null
                ? (def.puzzleTexture != null ? (Texture)def.puzzleTexture : def.fullImage?.texture)
                : null;
            if (tex != null && tex.width > 0 && tex.height > 0)
            {
                float aspect = (float)tex.width / tex.height;
                if (aspect >= 1f) h = w / aspect; else w = h * aspect;
            }

            var go = ArtUnbound.VR.PlacedArtworkFactory.Build(artworkId, GetCurrentFrameTier(), w, h, artworkCatalog);
            if (go == null) return;

            var panelT = nativeGallery != null ? nativeGallery.DetailPanelTransform : null;
            if (panelT != null)
            {
                // A la derecha del panel y un poco hacia el usuario, para no tapar el detalle.
                // La cara visible de la obra mira hacia -Z del root (quad con flip interno de 180°),
                // asi que igualar la orientacion del menu (+Z alejandose del usuario) la deja de frente.
                go.transform.position = panelT.position + panelT.right * 0.45f - panelT.forward * 0.15f;
                go.transform.rotation = Quaternion.LookRotation(panelT.forward, Vector3.up);
            }
            else
            {
                PlaceInFrontOfUser(go.transform, 0.55f);
            }

            _activeHangArtworkGO = go;
            _activeHangArtworkId = artworkId;
        }

        /// <summary>
        /// Toggle "Show tutorial" de Settings: persiste la preferencia (GameSettings.showOnboarding)
        /// y re-arma/desarma el tutorial EN CALIENTE (encenderlo lo arranca al volver al catalogo,
        /// sin reiniciar el juego).
        /// </summary>
        private void OnTutorialToggleChanged(bool show)
        {
            if (SaveData?.settings != null)
            {
                SaveData.settings.showOnboarding = show;
                saveDataService?.MarkDirty();
            }

            tutorialFlow?.SetArmedFromToggle(show);
        }

        /// <summary>Detail cerrado (close, cambio de tab o Hide del menu): destruye la obra flotante no colgada.</summary>
        private void OnDetailClosed()
        {
            if (_activeHangArtworkGO != null) Destroy(_activeHangArtworkGO);
            _activeHangArtworkGO = null;
            _activeHangArtworkId = null;
        }

        /// <summary>
        /// Tras soltar la obra del boton Hang (colgada en pared o retirada), el flujo de colocacion ya
        /// resolvio su destino (anclada y persistida, o destruida): solo soltar las referencias para
        /// que OnDetailClosed no la destruya despues.
        /// </summary>
        private void OnDetailArtworkHangResolved(string artworkId)
        {
            if (_activeHangArtworkGO == null || artworkId != _activeHangArtworkId) return;
            _activeHangArtworkGO = null;
            _activeHangArtworkId = null;
        }

        /// <summary>
        /// Collection: el gallery mostro el PlaqueView (overlay sobre el menu) y pide instanciar la placa
        /// 3D al frente. NO se oculta el menu (eso lo decide el gallery). La placa nace con tag
        /// "PlacedArtwork" + PlacedArtworkIdentifier para que el flujo de grab/reposicion la reconozca.
        /// </summary>
        private void OnHangPlaqueFromCollection(string plaqueId)
        {
            var def = collectibleCatalog != null ? collectibleCatalog.GetById(plaqueId) : null;
            var go = ArtUnbound.MR.CollectibleFactory.Build(def, GetCurrentFrameTier());
            if (go == null) return;

            // Id no-vacio: InteractionManager lo lee y TryRepositionWallArtwork ya no destruye el clon.
            // El artworkId se mantiene limpio ("plaque_<id>"); la unicidad entre copias colgadas la da el
            // instanceId (GalleryPaintingInstance), asignado al colgarla en la pared VR (como el anchorId de MR).
            var id = go.AddComponent<ArtUnbound.MR.PlacedArtworkIdentifier>();
            id.artworkId = $"plaque_{plaqueId}";

            // Colocar la placa donde esta el PlaquePanel (no a una distancia fija del head), para que
            // aparezca "casi donde esta el panel". Fallback: al frente del usuario.
            var panelT = nativeGallery != null ? nativeGallery.PlaquePanelTransform : null;
            if (panelT != null)
            {
                PlaceFacingUserAt(go.transform, panelT.position);
                // Igualar la orientacion del MENU (no la camara actual, que en VR puede diferir si giraste
                // la cabeza despues de abrir el menu). El usuario lee el menu desde su lado -Z; la cara de
                // la placa (+Z, ver CollectibleFactory.FaceContentTowardPlusZ) debe ver hacia ese lado.
                go.transform.rotation = Quaternion.LookRotation(-panelT.forward, Vector3.up);
            }
            else
            {
                PlaceInFrontOfUser(go.transform, 0.45f);
                // Sin panel de referencia: encarar al usuario. La cara de la placa es +Z; los helpers la
                // dejan con +Z alejado del usuario, asi que giramos 180° para que mire hacia ti.
                go.transform.rotation *= Quaternion.Euler(0f, 180f, 0f);
            }

            _activePlaqueGO = go;
            _activePlaqueId = id.artworkId;
        }

        /// <summary>Close del PlaqueView (cancelar): destruye la placa flotante. El gallery ya oculto el overlay.</summary>
        private void OnPlaqueViewCancelled()
        {
            if (_activePlaqueGO != null)
                Destroy(_activePlaqueGO);
            _activePlaqueGO = null;
            _activePlaqueId = null;
        }

        /// <summary>
        /// Tras soltar la placa (colgada en pared o retirada). Solo actua sobre la placa activa del
        /// PlaqueView (no interfiere al reposicionar obras ya colgadas). La placa colgada persiste como
        /// las obras (anclada en SaveData y reconstruida al cargar). Cierra el overlay; el menu nunca se
        /// oculto, asi que la card de la placa sigue ahi ("reaparece la copia").
        /// </summary>
        private void OnPlaqueHangResolved(string artworkId)
        {
            if (_activePlaqueGO == null || artworkId != _activePlaqueId) return;

            _activePlaqueGO = null;
            _activePlaqueId = null;

            nativeGallery?.HidePlaqueView();
        }

        /// <summary>Coloca un transform frente a la camara (a 'dist' m), mirando hacia el usuario.</summary>
        private void PlaceInFrontOfUser(Transform t, float dist)
        {
            if (t == null) return;
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 fwd = cam.transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
            fwd.Normalize();
            t.position = cam.transform.position + fwd * dist;
            t.rotation = Quaternion.LookRotation(fwd, Vector3.up);
        }

        /// <summary>
        /// Coloca un transform en una posicion del mundo (p.ej. la del PlaquePanel), mirando al usuario.
        /// Lo desplaza un poco hacia la camara para que no quede encimado en el plano del panel.
        /// </summary>
        private void PlaceFacingUserAt(Transform t, Vector3 worldPos)
        {
            if (t == null) return;
            var cam = Camera.main;
            if (cam == null) { t.position = worldPos; return; }
            Vector3 fwd = cam.transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
            fwd.Normalize();
            t.position = worldPos - fwd * 0.12f; // un poco hacia el usuario, frente al panel
            t.rotation = Quaternion.LookRotation(fwd, Vector3.up);
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

            // The board picks the grid + piece size from the difficulty index. The piece
            // count comes out as a consequence and is read back via TotalPieces.
            if (artworkData != null)
            {
                puzzleBoard.Initialize(artworkData, selectedDifficultyIndex);
            }
            else
            {
                puzzleBoard.Initialize(selectedDifficultyIndex, artworkTexture);
            }

            // The actual piece count is determined by the board after generation.
            int actualPieceCount = puzzleBoard.TotalPieces;
            selectedPieceCount = actualPieceCount;
            
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
                    // El material del marco deriva del TIER GLOBAL del jugador (GDD 8.4),
                    // no de la dificultad ni del bestFrameTier por-obra (legacy).
                    FrameTier frameTier = GetCurrentFrameTier();

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
            
            // Check for new record (based on TIME only)
            var progress = SaveData.GetProgress(selectedArtworkId);
            var existingRecord = progress?.GetRecordForPieceCount(selectedPieceCount);
            bool isNewRecord = existingRecord == null || timeSec < existingRecord.bestTimeSec;
            int previousBestTime = existingRecord?.bestTimeSec ?? 0;

            // Save progress to permanent records (score = 0, only time matters now).
            // bestFrameTier por-obra es legacy: el material ahora deriva del TIER GLOBAL del
            // jugador (GDD 8.4), no de la dificultad. Se pasa el tier global para no romper la firma.
            saveDataService.UpdateArtworkProgress(selectedArtworkId, selectedPieceCount, 0, timeSec, GetCurrentFrameTier());
            SaveData = saveDataService.GetCachedData();

            // Material de marco/placas = tier global del jugador, ya incluyendo esta obra recien completada.
            FrameTier frameTier = GetCurrentFrameTier();

            // Evalua placas de autor/movimiento/estatus (GDD 8.6). Las recien otorgadas alimentan
            // la linea de hito condicional del post-juego (Frente 7).
            LastEarnedPlaques = collectibleService?.EvaluateOnCompletion();

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


        /// <summary>Post-juego "Back to collection": limpia el colgado y vuelve al menu (GDD 4.8).</summary>
        private void OnBackFromPostGame()
        {
            CleanupArtworkHanging();
            QuitToMenu();
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
        /// <summary>
        /// DEV CHEAT (enableDevCheats): auto-completa el puzzle actual colocando todas las
        /// piezas restantes. Dispara el flujo normal de completado, asi el marco terminado
        /// se puede colgar en pared como si se hubiera armado a mano.
        /// </summary>
        private void OnDevCompletePuzzle()
        {
            if (CurrentState != GameState.Playing || puzzleBoard == null) return;

            Debug.Log("[GameBootstrap] DEV cheat: auto-completing current puzzle");
            puzzleBoard.DebugCompleteRemaining();
        }

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
        
        // Placas de comportamiento (colgar 1a / Curator) — GDD 8.6.
        collectibleService?.EvaluateOnHang();

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

            // Placas de comportamiento (colgar 1a / Curator) — GDD 8.6.
            collectibleService?.EvaluateOnHang();

            CleanupArtworkHanging();
            // Gallery is already loaded — only show the UI, don't reload (avoids
            // teleporting user back to spawn point and destroying the just-placed frame).
            TransitionToVRGallery(reloadGallery: false);
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
            // Si veniamos de PostGame con paredes, el colgado quedo auto-habilitado: limpiarlo antes
            // de re-armar para no arrastrar eventos suscritos a la nueva partida.
            CleanupArtworkHanging();

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
        /// Material/tier del marco derivado del TIER GLOBAL del jugador (GDD 8.4) y gateado por
        /// el desbloqueo del Marco (Madera = base de madera lisa hasta las 25 obras / toggle off).
        /// Delega en PresentationDecorator para una sola fuente de verdad (MR y VR).
        /// </summary>
        private FrameTier GetCurrentFrameTier() => ArtUnbound.MR.PresentationDecorator.CurrentFrameTier();

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
