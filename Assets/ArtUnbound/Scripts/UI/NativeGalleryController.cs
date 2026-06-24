using System;
using System.Collections;
using System.Collections.Generic;
using ArtUnbound.Data;
using ArtUnbound.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Galería de selección de obras al estilo Prime Video / Meta Store.
    /// Usa ray casting y poke nativos del Meta XR SDK — cero código de detección de manos.
    ///
    /// ══════════════════════════════════════════════════════════════════════
    ///  HIERARCHY SETUP EN UNITY EDITOR
    /// ══════════════════════════════════════════════════════════════════════
    ///
    ///  [NativeGallery]  ← este MonoBehaviour, gameObject inactivo al inicio
    ///    └── Canvas  (Render Mode: World Space, Scale: 0.001, size 1000×750)
    ///         ├── OVRRaycaster          ← REEMPLAZA GraphicRaycaster
    ///         ├── PointableCanvas       ← Meta XR SDK (conecta ray + poke con Unity UI)
    ///         └── [contentRoot] Panel   ← Image negra semitransparente, se oculta al posicionar
    ///              ├── Header            (TMP "Art Unbound", h=60)
    ///              ├── ContentArea       (anchors: stretch-stretch, menos 60 arriba y 80 abajo)
    ///              │    ├── CatalogView    (SearchInputField fijo arriba + ScrollRect vertical → GridLayoutGroup 4 cols.
    ///              │    │                   La búsqueda filtra el grid en vivo; EmptyLabel si no hay coincidencias.)
    ///              │    ├── CollectionView (vista propia, copia del Catalog SIN barra de búsqueda; su ScrollRect → GridLayoutGroup.
    ///              │    │                   Se puebla en PopulateCollection con obras completadas, placas y mejoras.)
    ///              │    └── SettingsView   (Layout vertical con Sliders y Toggle)
    ///              ├── DetailPanel       (overlay oscuro, SetActive false por defecto)
    ///              │    ├── ArtworkImage (Image, preserve aspect)
    ///              │    ├── TitleText    (TMP bold)
    ///              │    ├── ArtistText   (TMP normal)
    ///              │    ├── DescText     (TMP small, scrollable)
    ///              │    ├── BtnEasy      (Button + TMP "Easy")
    ///              │    ├── BtnNormal    (Button + TMP "Medium")
    ///              │    ├── BtnHard      (Button + TMP "Hard")
    ///              │    └── BtnClose     (Button "✕")
    ///              └── BottomNav        (h=80, HorizontalLayoutGroup)
    ///                   ├── BtnInicio       (Button + TMP "🏠 Inicio")
    ///                   └── BtnConfig       (Button + TMP "⚙ Config")
    ///
    ///  NOTAS CLAVE:
    ///  • El ScrollRect de cada grid debe tener Horizontal=false, Vertical=true.
    ///  • El GridLayoutGroup necesita Cell Size = (220,300), Spacing = (10,10), 4 cols.
    ///  • En EventSystem asegúrate de tener OVRInputModule (o el que Meta configura por defecto).
    ///  • El ArtworkCard prefab necesita el componente ArtworkCardUI.
    /// ══════════════════════════════════════════════════════════════════════
    /// </summary>
    public class NativeGalleryController : MonoBehaviour
    {
        // ════════════════════════════════════════════════════════════════════════
        //  EVENTOS
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>Disparado al iniciar un puzzle: (artworkId, difficultyIndex).</summary>
        public event Action<string, int> OnStartPuzzle;

        /// <summary>Se dispara cuando el usuario mueve un slider de configuración.</summary>
        public event Action<float, float, bool> OnSettingsChanged; // (musicVol, sfxVol, haptics)

        /// <summary>Collection: el usuario seleccionó una obra completada para colgarla (artworkId).</summary>
        public event Action<string> OnHangArtworkRequested;
        /// <summary>Collection: el usuario seleccionó una placa obtenida para colgarla (plaqueId).</summary>
        public event Action<string> OnHangPlaqueRequested;
        /// <summary>PlaqueView: el usuario cerró la vista sin colgar (cancelar) -> destruir la placa flotante.</summary>
        public event Action OnPlaqueViewCancelled;

        // ════════════════════════════════════════════════════════════════════════
        //  SERIALIZED — POSICIONAMIENTO
        // ════════════════════════════════════════════════════════════════════════

        [Header("Posicionamiento")]
        [Tooltip("Distancia (metros) al usuario cuando se muestra.")]
        [SerializeField] private float galleryDistance = 0.75f;

        [Tooltip("Offset vertical respecto a la altura de los ojos.")]
        [SerializeField] [Range(-0.40f, 0.40f)] private float heightOffset = 0.10f;

        // ════════════════════════════════════════════════════════════════════════
        //  SERIALIZED — REFERENCIAS DE JERARQUÍA
        // ════════════════════════════════════════════════════════════════════════

        [Header("Raíz de contenido")]
        [Tooltip("Panel hijo que se oculta mientras el tracking no es válido.")]
        [SerializeField] private GameObject contentRoot;

        // ── Tabs ──────────────────────────────────────────────────────────────
        [Header("Tabs — Botones")]
        [SerializeField] private Button  btnInicio;
        [SerializeField] private Button  btnConfig;
        [Tooltip("Boton COLLECTION de la barra inferior (inventario de obras + placas; GDD 8.5).")]
        [SerializeField] private Button  btnCollection;

        [Header("Tabs — Indicadores de tab activo")]
        [SerializeField] private GameObject indicatorInicio;
        [SerializeField] private GameObject indicatorConfig;

        // ── Paneles de vista ──────────────────────────────────────────────────
        [Header("Paneles de vista")]
        [SerializeField] private GameObject catalogView;
        [Tooltip("Vista propia de Collection (copia del Catalog sin barra de busqueda). Su grid se puebla en PopulateCollection.")]
        [SerializeField] private GameObject collectionView;
        [SerializeField] private GameObject settingsView;

        // ── Grids ──────────────────────────────────────────────────────────────
        [Header("Grids — Contenedores")]
        [Tooltip("Transform con GridLayoutGroup donde se instancian las cards del catálogo.")]
        [SerializeField] private Transform catalogGridContainer;

        [Tooltip("Transform con GridLayoutGroup de la vista Collection (dentro de collectionView). Donde se instancian sus cards.")]
        [SerializeField] private Transform collectionGridContainer;

        // ── Búsqueda (barra fija dentro del Catálogo) ─────────────────────────
        [Header("Búsqueda")]
        [Tooltip("Campo de texto fijo en la parte superior del Catálogo. Filtra el grid en vivo.")]
        [SerializeField] private TMP_InputField searchInputField;

        [Tooltip("Label que aparece cuando la búsqueda no coincide con ninguna obra del catálogo.")]
        [SerializeField] private TMP_Text searchEmptyLabel;

        // ── Configuración ────────────────────────────────────────────────────
        [Header("Configuración")]
        [SerializeField] private Slider   musicSlider;
        [SerializeField] private Slider   sfxSlider;
        [SerializeField] private Toggle   hapticsToggle;
        [SerializeField] private TMP_Text musicValueText;
        [SerializeField] private TMP_Text sfxValueText;

        [Header("Configuración — Mejoras de presentación (GDD 8.2)")]
        [Tooltip("Toggle de la Cédula (placa título/autor bajo cada cuadro). Solo surte efecto una vez desbloqueada (10 obras).")]
        [SerializeField] private Toggle   cedulaToggle;
        [Tooltip("Toggle del Marco (sobre la base de madera). Solo surte efecto una vez desbloqueado (25 obras).")]
        [SerializeField] private Toggle   marcoToggle;
        [Tooltip("Toggle de la Lámpara (luz museo sobre cada cuadro). Solo surte efecto una vez desbloqueada (50 obras).")]
        [SerializeField] private Toggle   lamparaToggle;

        // ── Panel de Detalle ─────────────────────────────────────────────────
        [Header("Panel de Detalle")]
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private Image      detailArtworkImage;
        [SerializeField] private TMP_Text   detailTitleText;
        [SerializeField] private TMP_Text   detailArtistText;
        [SerializeField] private TMP_Text   detailDescriptionText;
        [Tooltip("Texto de creditos de la obra. Queda en blanco si la obra no tiene credits.")]
        [SerializeField] private TMP_Text   detailCreditsText;
        [SerializeField] private Button     btnDetailClose;

        [Header("Detalle — Componente de Armado (obra desbloqueada)")]
        [Tooltip("Contenedor con los 3 botones de dificultad. Se muestra si la obra es gratis o el catalogo ya se compro.")]
        [SerializeField] private GameObject assemblyComponent;
        [SerializeField] private Button     btnEasy;
        [SerializeField] private Button     btnNormal;
        [SerializeField] private Button     btnHard;
        [SerializeField] private TMP_Text   btnEasyText;
        [SerializeField] private TMP_Text   btnNormalText;
        [SerializeField] private TMP_Text   btnHardText;

        // ── Panel de Placa (colgar una placa obtenida desde Collection) ───────
        [Header("Panel de Placa (PlaqueView)")]
        [Tooltip("Vista overlay para colgar una placa obtenida. Hermana de DetailPanel: se muestra SOBRE el menu (no lo oculta), igual que el detalle. GameBootstrap solo instancia/cuelga la placa 3D.")]
        [SerializeField] private PlaqueViewController plaqueView;

        [Header("Detalle — Componente de Compra (obra bloqueada)")]
        [Tooltip("Contenedor con el boton de comprar el catalogo completo. Se muestra si la obra esta bloqueada.")]
        [SerializeField] private GameObject purchaseComponent;
        [SerializeField] private Button     btnBuyCatalog;
        [SerializeField] private TMP_Text   buyCatalogPriceText;
        [Tooltip("Plantilla del texto de precio. {0} se reemplaza por el precio LOCALIZADO real de " +
                 "Meta. Ej: \"250+ artworks * one-time {0}\". Deja \"{0}\" para mostrar solo el precio. " +
                 "No escribas el precio a mano (politica IAP de Meta).")]
        [SerializeField] private string     buyCatalogPriceFormat = "{0}";

        // ── Prefab ───────────────────────────────────────────────────────────
        [Header("Card Prefab")]
        [Tooltip("Prefab con ArtworkCardUI. Ver comentarios de jerarquía arriba.")]
        [SerializeField] private GameObject artworkCardPrefab;

        // ════════════════════════════════════════════════════════════════════════
        //  ESTADO PRIVADO
        // ════════════════════════════════════════════════════════════════════════

        private LocalCatalogService        _catalog;
        private SaveDataService            _saveData;
        private PackPurchaseService        _purchaseService;
        private List<ArtworkDefinition>    _allArtworks = new List<ArtworkDefinition>();
        private ArtworkDefinition          _selectedArtwork;
        private bool                       _isVisible;
        private bool                       _isInitialized;
        private bool                       _catalogPopulated;
        private bool                       _hasBeenPositioned;
        private Camera                     _positioningCamera;

        private enum Tab { Catalog, Settings, Collection, Galleries }
        private Tab _currentTab = Tab.Catalog;

        // Mapeo card→obra del catálogo para filtrar (mostrar/ocultar) sin re-instanciar.
        private readonly List<(ArtworkDefinition art, GameObject go)> _catalogCards =
            new List<(ArtworkDefinition, GameObject)>();

        // Piece counts are derived by PuzzleBoard from the difficulty index — no defaults needed here.

        // ── VR Mode Buttons ───────────────────────────────────────────────────
        [Header("VR Mode Buttons (added to BottomNav)")]
        [Tooltip("Button 'VR' — visible in MR mode on Quest 3/Pro only.")]
        [SerializeField] private Button btnVR;
        [Tooltip("Button 'MR' — visible in VR mode on Quest 3/Pro only.")]
        [SerializeField] private Button btnMR;
        [Tooltip("Button 'Galerías' — visible in VR mode on all devices.")]
        [SerializeField] private Button btnGalerias;
        [SerializeField] private GallerySelectionController gallerySelectionController;

        [Header("VR Mode — Controller Required Warning")]
        [Tooltip("Panel shown for 3s when user taps VR without controllers active.")]
        [SerializeField] private GameObject vrControllerRequiredPanel;
        [Tooltip("Reference to HandTrackingInputController to detect controller presence.")]
        [SerializeField] private ArtUnbound.Input.HandTrackingInputController handTrackingInput;

        [Header("Feature Flags")]
        [Tooltip("Multi-gallery picker (VR mode). Disabled at launch; flip on when shipping DLC galleries.")]
        [SerializeField] private bool enableGallerySelection = false;

        /// <summary>Fired when the user taps the VR button (MR → VR transition).</summary>
        public event Action OnVRModeRequested;
        /// <summary>Fired when the user taps the MR button (VR → MR transition).</summary>
        public event Action OnMRModeRequested;

        private Coroutine _vrWarningCoroutine;

        // ════════════════════════════════════════════════════════════════════════
        //  LIFECYCLE
        // ════════════════════════════════════════════════════════════════════════

        private void Awake()
        {
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            // Limpiar listeners para evitar memory leaks
            btnInicio?.onClick.RemoveAllListeners();
            btnConfig?.onClick.RemoveAllListeners();
            btnCollection?.onClick.RemoveAllListeners();
            btnDetailClose?.onClick.RemoveAllListeners();
            btnEasy?.onClick.RemoveAllListeners();
            btnNormal?.onClick.RemoveAllListeners();
            btnHard?.onClick.RemoveAllListeners();
            btnBuyCatalog?.onClick.RemoveAllListeners();
            musicSlider?.onValueChanged.RemoveAllListeners();
            sfxSlider?.onValueChanged.RemoveAllListeners();
            hapticsToggle?.onValueChanged.RemoveAllListeners();
            cedulaToggle?.onValueChanged.RemoveAllListeners();
            marcoToggle?.onValueChanged.RemoveAllListeners();
            lamparaToggle?.onValueChanged.RemoveAllListeners();
            searchInputField?.onValueChanged.RemoveAllListeners();
            btnVR?.onClick.RemoveAllListeners();
            btnMR?.onClick.RemoveAllListeners();
            btnGalerias?.onClick.RemoveAllListeners();
            if (gallerySelectionController != null)
                gallerySelectionController.OnGallerySelected -= OnGallerySelectionChanged;
            if (plaqueView != null)
                plaqueView.OnCloseRequested -= OnPlaqueViewCloseClicked;
            if (_purchaseService != null)
                _purchaseService.OnPurchaseStateChanged -= HandlePurchaseStateChanged;
        }

        // ════════════════════════════════════════════════════════════════════════
        //  INITIALIZE
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Llamado por GameBootstrap.InitializeServices().
        /// Carga el catálogo y conecta todos los listeners de UI.
        /// </summary>
        public void Initialize(LocalCatalogService catalog, SaveDataService saveData)
        {
            _catalog  = catalog;
            _saveData = saveData;

            // Respetar el orden definido en ArtworkCatalog.asset (no reordenar).
            // El usuario controla el orden del menu y de la busqueda desde el asset.
            _allArtworks = catalog.GetAll();

            WireTabButtons();
            WireDetailButtons();
            WireSettingsControls();
            WireSearchControls();
            WireVRButtons();

            if (detailPanel != null) detailPanel.SetActive(false);
            if (vrControllerRequiredPanel != null) vrControllerRequiredPanel.SetActive(false);

            if (plaqueView != null)
            {
                plaqueView.OnCloseRequested += OnPlaqueViewCloseClicked;
                plaqueView.Hide();
            }

            _isInitialized = true;
            Debug.Log($"[NativeGallery] Inicializado con {_allArtworks.Count} obras.");
        }

        /// <summary>
        /// Inyecta el servicio de compra. Llamado por GameBootstrap tras inicializarlo.
        /// El unico punto de venta es el boton de compra contextual en el detalle de una obra
        /// bloqueada (modelo de unlock unico del catalogo; no hay tienda).
        /// </summary>
        public void SetPackPurchaseService(PackPurchaseService purchaseService)
        {
            if (_purchaseService != null)
                _purchaseService.OnPurchaseStateChanged -= HandlePurchaseStateChanged;

            _purchaseService = purchaseService;

            if (_purchaseService != null)
                _purchaseService.OnPurchaseStateChanged += HandlePurchaseStateChanged;
        }

        /// <summary>
        /// El estado de compra cambio (compra exitosa o restauracion asincrona desde Meta).
        /// Repuebla el catalogo (quita candados) y refresca el detalle si esta abierto.
        /// </summary>
        private void HandlePurchaseStateChanged()
        {
            PopulateCatalog();
            if (detailPanel != null && detailPanel.activeSelf && _selectedArtwork != null)
                ShowDetail(_selectedArtwork);
        }

        // ════════════════════════════════════════════════════════════════════════
        //  SHOW / HIDE
        // ════════════════════════════════════════════════════════════════════════

        public void Show()
        {
            if (_isVisible || !_isInitialized) return;
            _isVisible = true;

            // El GO debe estar activo ANTES de StartCoroutine
            gameObject.SetActive(true);
            StartCoroutine(PositionAndReveal());
        }

        public void Hide()
        {
            if (!_isVisible) return;
            _isVisible = false;

            if (detailPanel != null) detailPanel.SetActive(false);
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Resetea la posición para que el próximo Show() reposicione la galería
        /// frente al usuario. Llamar antes de Show() al regresar del rompecabezas.
        /// </summary>
        public void ResetPosition()
        {
            _hasBeenPositioned = false;
        }

        /// <summary>
        /// Provides a direct camera reference to use for positioning when Camera.main is unavailable (e.g. VR mode).
        /// Call this before Show() when switching to VR.
        /// </summary>
        public void SetPositioningCamera(Camera cam)
        {
            _positioningCamera = cam;
        }

        // ─────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Oculta el contenido hasta que el head-tracking sea válido,
        /// luego posiciona el panel frente al usuario y lo revela.
        /// Patrón idéntico a GameBootstrap.PositionCanvasWithDelay.
        /// </summary>
        private IEnumerator PositionAndReveal()
        {
            // Ocultar mientras esperamos (solo el contenido, no el GO raíz)
            if (contentRoot != null) contentRoot.SetActive(false);

            // Si ya fue posicionado, no reposicionar — el usuario puede estar
            // mirando en otra dirección al regresar de colocar un cuadro.
            if (_hasBeenPositioned)
            {
                RevealContent();
                yield break;
            }

            Camera cam = Camera.main ?? _positioningCamera;
            if (cam == null)
            {
                float waited = 0f;
                while (cam == null && waited < 3f)
                {
                    yield return null;
                    waited += Time.deltaTime;
                    cam = Camera.main ?? _positioningCamera;
                }
                if (cam == null)
                {
                    Debug.LogWarning("[NativeGallery] No hay cámara disponible tras espera. Mostrando sin posicionar.");
                    RevealContent();
                    yield break;
                }
            }

            // Poll until head tracking is stable (head above floor level).
            // During mode switches the headset is already worn so this exits immediately.
            // On cold boot the Quest starts at y≈0 until tracking initialises (~1-2 s).
            const float kStableThreshold = 0.5f;
            const float kMaxWait = 3f;
            float elapsed = 0f;
            while (cam.transform.position.y < kStableThreshold && elapsed < kMaxWait)
            {
                yield return null;
                elapsed += Time.deltaTime;
                cam = Camera.main ?? _positioningCamera;
                if (cam == null) break;
            }

            PositionInFrontOfUser();
            _hasBeenPositioned = true;
            RevealContent();
        }

        private void RevealContent()
        {
            // Actualizar caché de save data por si hay nuevas obras completadas
            // desde la última vez que se mostró la galería
            if (contentRoot != null) contentRoot.SetActive(true);

            // SwitchTab puebla el tab actual (Catalog/Collection/Settings); no es necesario
            // poblar el catalogo aparte. Marcamos el flag por compatibilidad.
            _catalogPopulated = true;
            SwitchTab(_currentTab, force: true);

            // Clear EventSystem selection so no button shows the golden "selected" highlight on reveal
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        }

        // ─────────────────────────────────────────────────────────────────────
        private void PositionInFrontOfUser()
        {
            Camera activeCam = Camera.main ?? _positioningCamera;
            if (activeCam == null) return;

            Transform cam     = activeCam.transform;
            Vector3   headPos = cam.position;
            headPos.y = Mathf.Clamp(headPos.y, 1.2f, 2.0f);

            Vector3 forward = cam.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            forward.Normalize();

            Vector3 targetPos = headPos + forward * galleryDistance;
            targetPos.y += heightOffset;

            transform.position = targetPos;
            // LookRotation(forward): el canvas enfrenta en la misma dirección que la cámara.
            // Unity UI es visible desde ambos lados — el usuario ve el contenido desde el lado -Z.
            // Convención idéntica a PositionCanvasWithDelay en GameBootstrap.
            transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        // ════════════════════════════════════════════════════════════════════════
        //  TAB SYSTEM
        // ════════════════════════════════════════════════════════════════════════

        private void WireTabButtons()
        {
            btnInicio?.onClick.AddListener(() => SwitchTab(Tab.Catalog));
            btnConfig?.onClick.AddListener(() => SwitchTab(Tab.Settings));
            btnCollection?.onClick.AddListener(() => SwitchTab(Tab.Collection));
        }

        private void SwitchTab(Tab tab, bool force = false)
        {
            if (_currentTab == tab && !force) return;
            _currentTab = tab;

            // NOTA: no usar el operador `?.` con objetos Unity. Una referencia
            // serializada sin asignar es un "fake-null" que `?.` NO detecta (usa
            // igualdad de referencia pura, no el `==` sobrecargado de Unity), por lo
            // que llamaria a SetActive y lanzaria UnassignedReferenceException,
            // abortando SwitchTab antes de activar catalogView (galeria en blanco).
            if (indicatorInicio != null) indicatorInicio.SetActive(tab == Tab.Catalog);
            if (indicatorConfig != null) indicatorConfig.SetActive(tab == Tab.Settings);

            // Cada pestaña tiene su propia vista. Collection ya NO reutiliza el catalogView:
            // vive en collectionView (su grid propio, sin barra de busqueda).
            if (catalogView != null)    catalogView.SetActive(tab == Tab.Catalog);
            if (collectionView != null) collectionView.SetActive(tab == Tab.Collection);
            if (settingsView != null)   settingsView.SetActive(tab == Tab.Settings);
            SetSearchBarActive(tab == Tab.Catalog);

            // Galleries is a tab-like view backed by gallerySelectionController. Show it
            // when the tab is selected; hide it whenever any other tab is active.
            if (tab == Tab.Galleries)
            {
                string activeId = ArtUnbound.Core.GameBootstrap.Instance != null
                    ? ArtUnbound.Core.GameBootstrap.Instance.ActiveVRGalleryId
                    : "gallery_classic";
                gallerySelectionController?.Show(activeId);
            }
            else
            {
                gallerySelectionController?.Hide();
            }

            // Close the modal detail panel so it doesn't survive across tab changes
            // (catalog artwork detail with the size buttons / purchase button).
            if (detailPanel != null) detailPanel.SetActive(false);

            switch (tab)
            {
                case Tab.Settings:   PopulateSettings();   break;
                case Tab.Catalog:    PopulateCatalog();    break;
                case Tab.Collection: PopulateCollection(); break;
            }
        }

        /// <summary>Muestra/oculta la barra de busqueda (solo aplica en el tab Catalogo).</summary>
        private void SetSearchBarActive(bool active)
        {
            if (searchInputField != null && searchInputField.gameObject.activeSelf != active)
                searchInputField.gameObject.SetActive(active);
            if (!active && searchEmptyLabel != null)
                searchEmptyLabel.gameObject.SetActive(false);
        }

        // Tab active/inactive coloring intentionally removed — tabs use the central
        // UIButtonTheme like every other button (transparent rest, semi-brown hover).
        // Active tab is implicit from which view is shown, matching Disney+/Prime UX.

        // ════════════════════════════════════════════════════════════════════════
        //  POPULATE GRIDS
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Instancia una card por cada obra del catálogo y registra el mapeo card→obra
        /// en <see cref="_catalogCards"/> para poder filtrar (mostrar/ocultar) en vivo
        /// sin re-instanciar. Solo se llama una vez — las cards no se destruyen entre vistas.
        /// </summary>
        private void PopulateCatalog()
        {
            if (catalogGridContainer == null || artworkCardPrefab == null)
            {
                Debug.LogWarning("[NativeGallery] catalogGridContainer o artworkCardPrefab no asignados.");
                return;
            }

            ClearGrid(catalogGridContainer);
            _catalogCards.Clear();

            SaveData saveData = _saveData.GetCachedData();
            foreach (var artwork in _allArtworks)
            {
                if (artwork == null) continue;
                bool isCompleted = IsArtworkCompleted(artwork.artworkId, saveData);
                GameObject go = SpawnCard(artwork, isCompleted, catalogGridContainer);
                if (go != null) _catalogCards.Add((artwork, go));
            }

            // Reaplica el filtro actual (campo vacío ⇒ se ven todas).
            FilterCatalog(searchInputField != null ? searchInputField.text : string.Empty);

            Debug.Log($"[NativeGallery] Catálogo poblado con {_catalogCards.Count} cards.");
        }

        /// <summary>
        /// Filtra el grid del catálogo mostrando/ocultando las cards ya instanciadas según
        /// el query (título / autor / movimiento). Campo vacío ⇒ se muestran todas.
        /// El GridLayoutGroup ignora los hijos inactivos y reacomoda solo.
        /// </summary>
        private void FilterCatalog(string query)
        {
            bool emptyQuery = string.IsNullOrWhiteSpace(query);
            string lower    = emptyQuery ? string.Empty : query.ToLowerInvariant();
            int visible     = 0;

            // Oculta el placeholder en cuanto hay texto. TMP lo hace solo al teclear
            // directo, pero en Quest el texto entra via SetTextWithoutNotify (teclado del
            // sistema), que no siempre lo refresca; lo forzamos aquí para cubrir ambas rutas.
            if (searchInputField != null && searchInputField.placeholder != null)
                searchInputField.placeholder.enabled = string.IsNullOrEmpty(query);

            foreach (var (art, go) in _catalogCards)
            {
                if (go == null) continue;

                bool match = emptyQuery;
                if (!emptyQuery && art != null)
                {
                    bool matchTitle    = !string.IsNullOrEmpty(art.title)       &&
                                         art.title.ToLowerInvariant().Contains(lower);
                    bool matchAuthor   = !string.IsNullOrEmpty(art.author)      &&
                                         art.author.ToLowerInvariant().Contains(lower);
                    bool matchMovement = !string.IsNullOrEmpty(art.artMovement) &&
                                         art.artMovement.ToLowerInvariant().Contains(lower);
                    match = matchTitle || matchAuthor || matchMovement;
                }

                go.SetActive(match);
                if (match) visible++;
            }

            if (searchEmptyLabel != null)
            {
                // Solo muestra/oculta el label; el texto se define en el Editor.
                bool showEmpty = !emptyQuery && visible == 0;
                searchEmptyLabel.gameObject.SetActive(showEmpty);
            }
        }

        /// <summary>
        /// Puebla el grid propio de Collection (collectionGridContainer) con placas obtenidas (colgables)
        /// y objetos no obtenidos (placas + mejoras de presentacion) mostrados como bloqueados con su
        /// condicion. Las obras completadas (pinturas) NO se listan aqui por decision de diseno.
        /// </summary>
        private void PopulateCollection()
        {
            if (collectionGridContainer == null || artworkCardPrefab == null)
            {
                Debug.LogWarning("[NativeGallery] collectionGridContainer o artworkCardPrefab no asignados (revisa la vista CollectionView en el inspector).");
                return;
            }
            ClearGrid(collectionGridContainer);

            SaveData saveData = _saveData != null ? _saveData.GetCachedData() : null;
            if (saveData == null) return;
            int completedCount = saveData.GetCompletedCount();

            // Coleccionables del catalogo (en su orden). Placas: obtenida -> colgar / bloqueada ->
            // condicion. Mejoras (no colgables): desbloqueada/locked segun obras completadas.
            // (Las obras completadas ya NO se muestran en Collection.)
            var gb = ArtUnbound.Core.GameBootstrap.Instance;
            var catalog = gb != null && gb.CollectibleService != null ? gb.CollectibleService.Catalog : null;
            if (catalog != null && catalog.collectibles != null)
            {
                foreach (var c in catalog.collectibles)
                {
                    if (c == null) continue;
                    var go = Instantiate(artworkCardPrefab, collectionGridContainer);
                    var card = go.GetComponent<ArtworkCardUI>();
                    if (card == null) continue;

                    if (c.hangable)
                    {
                        // Placa/estatus: miniatura 3D del propio asset (render-to-texture).
                        bool earned = saveData.HasPlaque(c.id);
                        string cid = c.id;
                        Texture preview = CollectiblePreviewRenderer.Instance.GetPreview(c);
                        if (earned)
                            card.SetupGeneric(c.title, c.conditionText, true, false,
                                () => ShowPlaqueView(cid), preview);
                        else
                            card.SetupGeneric(c.title, c.conditionText, false, true, null, preview);
                    }
                    else
                    {
                        // Mejora (cedula/marco/lampara): no es colgable -> icono Sprite del asset.
                        bool unlocked = completedCount >= c.threshold;
                        card.SetupGeneric(c.title, c.conditionText, unlocked, !unlocked, null, null, c.icon);
                    }
                }
            }
        }

        private void PopulateSettings()
        {
            SaveData saveData = _saveData.GetCachedData();
            if (saveData?.settings == null) return;

            if (musicSlider != null)
            {
                musicSlider.SetValueWithoutNotify(saveData.settings.musicVolume);
                UpdateMusicLabel(saveData.settings.musicVolume);
            }
            if (sfxSlider != null)
            {
                sfxSlider.SetValueWithoutNotify(saveData.settings.sfxVolume);
                UpdateSfxLabel(saveData.settings.sfxVolume);
            }
            if (hapticsToggle != null)
                hapticsToggle.SetIsOnWithoutNotify(saveData.settings.hapticsEnabled);

            if (cedulaToggle != null)
                cedulaToggle.SetIsOnWithoutNotify(saveData.settings.showCedula);
            if (marcoToggle != null)
                marcoToggle.SetIsOnWithoutNotify(saveData.settings.showMarco);
            if (lamparaToggle != null)
                lamparaToggle.SetIsOnWithoutNotify(saveData.settings.showLampara);
        }

        // ── Helpers de grid ───────────────────────────────────────────────────

        private GameObject SpawnCard(ArtworkDefinition artwork, bool isCompleted, Transform container)
        {
            var go   = Instantiate(artworkCardPrefab, container);
            var card = go.GetComponent<ArtworkCardUI>();
            if (card != null)
            {
                bool isLocked = _purchaseService != null && _purchaseService.IsArtworkLocked(artwork);
                card.Setup(artwork, isCompleted, OnCardTapped, isLocked);
            }
            else
                Debug.LogWarning($"[NativeGallery] El prefab '{artworkCardPrefab.name}' no tiene ArtworkCardUI.");
            return go;
        }

        private static void ClearGrid(Transform container)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
                Destroy(container.GetChild(i).gameObject);
        }

        private static bool IsArtworkCompleted(string artworkId, SaveData saveData)
        {
            if (saveData == null) return false;
            var progress = saveData.GetProgress(artworkId);
            return progress != null && progress.HasBeenCompleted();
        }

        // ════════════════════════════════════════════════════════════════════════
        //  DETAIL PANEL
        // ════════════════════════════════════════════════════════════════════════

        private void WireDetailButtons()
        {
            btnDetailClose?.onClick.AddListener(() =>
            {
                if (detailPanel != null) detailPanel.SetActive(false);
            });

            btnEasy?.onClick.AddListener(()   => StartPuzzle(0));
            btnNormal?.onClick.AddListener(() => StartPuzzle(1));
            btnHard?.onClick.AddListener(()   => StartPuzzle(2));

            btnBuyCatalog?.onClick.AddListener(BuyCompleteCatalog);
        }

        private void BuyCompleteCatalog()
        {
            if (_purchaseService == null || _selectedArtwork == null) return;
            if (_purchaseService.IsCatalogPurchased()) return;

            _purchaseService.PurchaseCatalog(
                onSuccess: () =>
                {
                    // Refrescar el detalle a modo armado y repoblar el catalogo para
                    // quitar candados de todas las cards.
                    ShowDetail(_selectedArtwork);
                    RepopulateCatalog();
                },
                onFailure: () =>
                {
                    // Compra cancelada o con error: el detalle se mantiene en modo compra.
                    Debug.LogWarning("[NativeGallery] Compra del catalogo cancelada o fallida.");
                });
        }

        /// <summary>
        /// Reconstruye el grid del catalogo (p.ej. tras comprar el catalogo completo).
        /// </summary>
        private void RepopulateCatalog()
        {
            PopulateCatalog();
        }

        private void OnCardTapped(ArtworkDefinition artwork)
        {
            _selectedArtwork = artwork;
            ShowDetail(artwork);
        }

        private void ShowDetail(ArtworkDefinition artwork)
        {
            if (detailPanel == null) return;

            // Imagen
            Sprite img = artwork.fullImage ?? artwork.thumbnail;
            if (detailArtworkImage != null)
            {
                detailArtworkImage.sprite          = img;
                detailArtworkImage.enabled         = img != null;
                detailArtworkImage.preserveAspect  = true;
            }

            if (detailTitleText       != null) detailTitleText.text       = artwork.title;
            if (detailArtistText      != null) detailArtistText.text      = artwork.author;
            if (detailDescriptionText != null) detailDescriptionText.text = artwork.description;
            // Creditos: en blanco si la obra no los tiene (null o vacio).
            if (detailCreditsText     != null) detailCreditsText.text     = artwork.credits ?? string.Empty;

            if (btnEasyText   != null) btnEasyText.text   = "A Coffee";
            if (btnNormalText != null) btnNormalText.text = "A Break";
            if (btnHardText   != null) btnHardText.text   = "A Movie";

            // Armado vs compra: la obra esta desbloqueada si es gratis o si el catalogo
            // completo ya fue comprado. Si no, se muestra el componente de compra.
            bool unlocked = artwork.isFree ||
                            (_purchaseService != null && _purchaseService.IsCatalogPurchased());

            if (assemblyComponent != null) assemblyComponent.SetActive(unlocked);
            if (purchaseComponent != null) purchaseComponent.SetActive(!unlocked);

            // Precio: si el campo esta asignado, se muestra el precio LOCALIZADO real que devuelve
            // Meta (politica IAP: no hardcodear precios), inyectado en la plantilla buyCatalogPriceFormat
            // (ej. "250+ artworks * one-time {0}"). Mientras Meta no responde, cae al precio de respaldo
            // del servicio. Si el campo queda sin asignar, la UI usa su texto fijo.
            if (!unlocked && buyCatalogPriceText != null && _purchaseService != null)
            {
                string fmt = string.IsNullOrEmpty(buyCatalogPriceFormat) ? "{0}" : buyCatalogPriceFormat;
                buyCatalogPriceText.text = string.Format(fmt, _purchaseService.CatalogPrice);
            }

            detailPanel.SetActive(true);
        }

        // ════════════════════════════════════════════════════════════════════════
        //  PLAQUE VIEW  (colgar una placa obtenida — overlay sobre el menu)
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Card de placa obtenida tocada: muestra el PlaqueView SOBRE el menu (como el detalle, sin
        /// ocultarlo) y pide a GameBootstrap instanciar la placa 3D al frente (via OnHangPlaqueRequested).
        /// </summary>
        private void ShowPlaqueView(string plaqueId)
        {
            if (plaqueView != null) plaqueView.Show();
            OnHangPlaqueRequested?.Invoke(plaqueId);
        }

        /// <summary>Transform del PlaquePanel (o null). GameBootstrap lo usa para spawnear la placa 3D frente al panel.</summary>
        public Transform PlaquePanelTransform => plaqueView != null ? plaqueView.PanelTransform : null;

        /// <summary>Boton Close del PlaqueView: oculta el overlay y avisa a GameBootstrap para destruir la placa flotante.</summary>
        private void OnPlaqueViewCloseClicked()
        {
            if (plaqueView != null) plaqueView.Hide();
            OnPlaqueViewCancelled?.Invoke();
        }

        /// <summary>Cierra el PlaqueView sin cancelar (llamado por GameBootstrap cuando la placa ya quedo colgada).</summary>
        public void HidePlaqueView()
        {
            if (plaqueView != null) plaqueView.Hide();
        }

        private void StartPuzzle(int difficultyIndex)
        {
            if (_selectedArtwork == null) return;

            // Defensa: una obra bloqueada nunca debe poder iniciar un puzzle.
            if (_purchaseService != null && _purchaseService.IsArtworkLocked(_selectedArtwork))
            {
                Debug.LogWarning($"[NativeGallery] Intento de armar obra bloqueada: {_selectedArtwork.title}");
                ShowDetail(_selectedArtwork); // re-muestra el componente de compra
                return;
            }

            Debug.Log($"[NativeGallery] Iniciando puzzle: {_selectedArtwork.title} | difficulty={difficultyIndex}");

            OnStartPuzzle?.Invoke(_selectedArtwork.artworkId, difficultyIndex);
        }

        // ════════════════════════════════════════════════════════════════════════
        //  SETTINGS
        // ════════════════════════════════════════════════════════════════════════

        private void WireSettingsControls()
        {
            musicSlider?.onValueChanged.AddListener(v =>
            {
                UpdateMusicLabel(v);
                FireSettingsChanged();
            });
            sfxSlider?.onValueChanged.AddListener(v =>
            {
                UpdateSfxLabel(v);
                FireSettingsChanged();
            });
            hapticsToggle?.onValueChanged.AddListener(_ => FireSettingsChanged());

            // Toggles de presentacion (cedula/marco/lampara): persisten la preferencia en
            // GameSettings. Surten efecto en obras colgadas al re-instanciarse (re-entrar a la
            // galeria / re-colgar). El desbloqueo por progreso se evalua aparte (IsCedulaActive...).
            cedulaToggle?.onValueChanged.AddListener(_ => SavePresentationToggles());
            marcoToggle?.onValueChanged.AddListener(_ => SavePresentationToggles());
            lamparaToggle?.onValueChanged.AddListener(_ => SavePresentationToggles());
        }

        private void SavePresentationToggles()
        {
            var data = _saveData?.GetCachedData();
            if (data?.settings == null) return;

            if (cedulaToggle != null)  data.settings.showCedula  = cedulaToggle.isOn;
            if (marcoToggle != null)   data.settings.showMarco   = marcoToggle.isOn;
            if (lamparaToggle != null) data.settings.showLampara = lamparaToggle.isOn;

            _saveData.MarkDirty();
        }

        private void UpdateMusicLabel(float v)
        {
            if (musicValueText != null)
                musicValueText.text = $"{Mathf.RoundToInt(v * 100)}%";
        }

        private void UpdateSfxLabel(float v)
        {
            if (sfxValueText != null)
                sfxValueText.text = $"{Mathf.RoundToInt(v * 100)}%";
        }

        private void FireSettingsChanged()
        {
            float music   = musicSlider   != null ? musicSlider.value   : 0.5f;
            float sfx     = sfxSlider     != null ? sfxSlider.value     : 0.7f;
            bool  haptics = hapticsToggle != null ? hapticsToggle.isOn  : true;
            OnSettingsChanged?.Invoke(music, sfx, haptics);
        }

        // ════════════════════════════════════════════════════════════════════════
        //  SEARCH
        // ════════════════════════════════════════════════════════════════════════

        // ════════════════════════════════════════════════════════════════════════
        //  VR MODE BUTTONS
        // ════════════════════════════════════════════════════════════════════════

        private void WireVRButtons()
        {
            btnVR?.onClick.AddListener(() =>
            {
                UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);

                bool hasControllers = handTrackingInput != null && handTrackingInput.useControllers;
                if (!hasControllers)
                {
                    ShowVRControllerWarning();
                    return;
                }

                OnVRModeRequested?.Invoke();
            });
            btnMR?.onClick.AddListener(() =>
            {
                UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
                OnMRModeRequested?.Invoke();
            });
            btnGalerias?.onClick.AddListener(() => SwitchTab(Tab.Galleries));

            if (gallerySelectionController != null)
                gallerySelectionController.OnGallerySelected += OnGallerySelectionChanged;

            // Hidden by default — GameBootstrap calls SetVRButtonsMode after initialization
            SetVRButtonVisibility(false, false, false);
        }

        /// <summary>
        /// Configures which VR-related buttons are visible based on current mode and device.
        /// Call from GameBootstrap when entering MR or VR mode.
        /// </summary>
        /// <param name="isVRMode">True if currently in VR mode.</param>
        /// <param name="isQuest3">True for Quest 3/Pro (supports MR mode switch).</param>
        public void SetVRButtonsMode(bool isVRMode, bool isQuest3)
        {
            // VR button: shown in MR mode, Quest 3/Pro only
            bool showVR = !isVRMode && isQuest3;
            // MR button: shown in VR mode, Quest 3/Pro only
            bool showMR = isVRMode && isQuest3;
            // Galerías: shown in VR mode only when DLC gallery selection is enabled (feature flag).
            bool showGalerias = isVRMode && enableGallerySelection;

            SetVRButtonVisibility(showVR, showMR, showGalerias);
        }

        private void SetVRButtonVisibility(bool showVR, bool showMR, bool showGalerias)
        {
            if (btnVR != null)       btnVR.gameObject.SetActive(showVR);
            if (btnMR != null)       btnMR.gameObject.SetActive(showMR);
            if (btnGalerias != null) btnGalerias.gameObject.SetActive(showGalerias);
        }

        private void ShowVRControllerWarning()
        {
            if (vrControllerRequiredPanel == null)
            {
                Debug.LogWarning("[NativeGallery] vrControllerRequiredPanel no asignado en el Inspector.");
                return;
            }

            if (_vrWarningCoroutine != null)
                StopCoroutine(_vrWarningCoroutine);

            vrControllerRequiredPanel.SetActive(true);
            _vrWarningCoroutine = StartCoroutine(HideVRWarningAfterDelay(3f));
        }

        private System.Collections.IEnumerator HideVRWarningAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (vrControllerRequiredPanel != null)
                vrControllerRequiredPanel.SetActive(false);
            _vrWarningCoroutine = null;
        }

        private void OnGallerySelectionChanged(string galleryId)
        {
            ArtUnbound.Core.GameBootstrap.Instance?.SwitchVRGallery(galleryId);
            // Return to the catalog so the user lands on the new gallery's artwork list.
            SwitchTab(Tab.Catalog, force: true);
        }

        private TouchScreenKeyboard _searchKeyboard;

        private void WireSearchControls()
        {
            searchInputField?.onValueChanged.AddListener(FilterCatalog);

            // En Quest (Android) el canvas es World Space y TMP_InputField no lanza
            // el teclado del sistema automáticamente con XR Interaction Toolkit.
            // Lo abrimos manualmente al recibir foco.
            searchInputField?.onSelect.AddListener(_ => OpenSystemKeyboard());
        }

        private void OpenSystemKeyboard()
        {
            if (!TouchScreenKeyboard.isSupported) return;
            _searchKeyboard = TouchScreenKeyboard.Open(
                searchInputField != null ? searchInputField.text : string.Empty,
                TouchScreenKeyboardType.Default,
                autocorrection : false,
                multiline       : false,
                secure          : false,
                alert           : false,
                textPlaceholder : "Search artworks..."
            );
        }

        private void Update()
        {
            if (_searchKeyboard == null) return;

            // Sincronizar texto del teclado del sistema → InputField → resultados
            if (_searchKeyboard.active)
            {
                string kbText = _searchKeyboard.text;
                if (searchInputField != null && searchInputField.text != kbText)
                {
                    searchInputField.SetTextWithoutNotify(kbText);
                    FilterCatalog(kbText);
                }
            }

            // Liberar referencia cuando el teclado se cierra
            if (_searchKeyboard.status == TouchScreenKeyboard.Status.Done ||
                _searchKeyboard.status == TouchScreenKeyboard.Status.Canceled ||
                _searchKeyboard.status == TouchScreenKeyboard.Status.LostFocus)
            {
                _searchKeyboard = null;
            }
        }
    }
}
