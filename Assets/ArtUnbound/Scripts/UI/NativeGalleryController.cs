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
    ///              │    ├── CatalogView  (ScrollRect vertical → GridLayoutGroup 4 cols)
    ///              │    ├── CompletedView(ScrollRect vertical → GridLayoutGroup 4 cols)
    ///              │    ├── SettingsView (Layout vertical con Sliders y Toggle)
    ///              │    └── SearchView   (InputField arriba + ScrollRect de resultados)
    ///              ├── DetailPanel       (overlay oscuro, SetActive false por defecto)
    ///              │    ├── ArtworkImage (Image, preserve aspect)
    ///              │    ├── TitleText    (TMP bold)
    ///              │    ├── ArtistText   (TMP normal)
    ///              │    ├── DescText     (TMP small, scrollable)
    ///              │    ├── BtnEasy      (Button + TMP "64 Pieces")
    ///              │    ├── BtnNormal    (Button + TMP "144 Pieces")
    ///              │    ├── BtnHard      (Button + TMP "256 Pieces")
    ///              │    └── BtnClose     (Button "✕")
    ///              └── BottomNav        (h=80, HorizontalLayoutGroup)
    ///                   ├── BtnInicio       (Button + TMP "🏠 Inicio")
    ///                   ├── BtnCompletadas  (Button + TMP "✓ Completadas")
    ///                   ├── BtnConfig       (Button + TMP "⚙ Config")
    ///                   └── BtnBuscar       (Button + TMP "🔍 Buscar")
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

        /// <summary>Firma idéntica a UnifiedMainMenuController — GameBootstrap lo recibe igual.</summary>
        public event Action<string, int, int> OnStartPuzzle;

        /// <summary>Se dispara cuando el usuario mueve un slider de configuración.</summary>
        public event Action<float, float, bool> OnSettingsChanged; // (musicVol, sfxVol, haptics)

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
        [SerializeField] private Button  btnCompletadas;
        [SerializeField] private Button  btnConfig;
        [SerializeField] private Button  btnBuscar;

        [Tooltip("Color del tab seleccionado.")]
        [SerializeField] private Color tabActiveColor   = new Color(0.537f, 0.424f, 0.290f); // #896C4A
        [Tooltip("Color de tabs inactivos.")]
        [SerializeField] private Color tabInactiveColor = new Color(0.22f, 0.22f, 0.22f);

        // ── Paneles de vista ──────────────────────────────────────────────────
        [Header("Paneles de vista")]
        [SerializeField] private GameObject catalogView;
        [SerializeField] private GameObject completedView;
        [SerializeField] private GameObject settingsView;
        [SerializeField] private GameObject searchView;

        // ── Grids ──────────────────────────────────────────────────────────────
        [Header("Grids — Contenedores")]
        [Tooltip("Transform con GridLayoutGroup donde se instancian las cards del catálogo.")]
        [SerializeField] private Transform catalogGridContainer;

        [Tooltip("Transform con GridLayoutGroup donde se instancian las cards de completadas.")]
        [SerializeField] private Transform completedGridContainer;

        // ── Búsqueda ──────────────────────────────────────────────────────────
        [Header("Búsqueda")]
        [SerializeField] private TMP_InputField searchInputField;

        [Tooltip("Transform con GridLayoutGroup donde se instancian los resultados de búsqueda.")]
        [SerializeField] private Transform searchGridContainer;

        [Tooltip("Label que aparece cuando no hay resultados ni texto en el campo.")]
        [SerializeField] private TMP_Text searchEmptyLabel;

        [Tooltip("Label que aparece cuando el usuario no ha completado ninguna obra todavía.")]
        [SerializeField] private TMP_Text completedEmptyLabel;

        // ── Configuración ────────────────────────────────────────────────────
        [Header("Configuración")]
        [SerializeField] private Slider   musicSlider;
        [SerializeField] private Slider   sfxSlider;
        [SerializeField] private Toggle   hapticsToggle;
        [SerializeField] private TMP_Text musicValueText;
        [SerializeField] private TMP_Text sfxValueText;

        // ── Panel de Detalle ─────────────────────────────────────────────────
        [Header("Panel de Detalle")]
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private Image      detailArtworkImage;
        [SerializeField] private TMP_Text   detailTitleText;
        [SerializeField] private TMP_Text   detailArtistText;
        [SerializeField] private TMP_Text   detailDescriptionText;
        [SerializeField] private Button     btnEasy;
        [SerializeField] private Button     btnNormal;
        [SerializeField] private Button     btnHard;
        [SerializeField] private TMP_Text   btnEasyText;
        [SerializeField] private TMP_Text   btnNormalText;
        [SerializeField] private TMP_Text   btnHardText;
        [SerializeField] private Button     btnDetailClose;

        // ── Prefab ───────────────────────────────────────────────────────────
        [Header("Card Prefab")]
        [Tooltip("Prefab con ArtworkCardUI. Ver comentarios de jerarquía arriba.")]
        [SerializeField] private GameObject artworkCardPrefab;

        // ════════════════════════════════════════════════════════════════════════
        //  ESTADO PRIVADO
        // ════════════════════════════════════════════════════════════════════════

        private LocalCatalogService        _catalog;
        private SaveDataService            _saveData;
        private List<ArtworkDefinition>    _allArtworks = new List<ArtworkDefinition>();
        private ArtworkDefinition          _selectedArtwork;
        private bool                       _isVisible;
        private bool                       _isInitialized;
        private bool                       _catalogPopulated;
        private bool                       _hasBeenPositioned;
        private Camera                     _positioningCamera;

        private enum Tab { Catalog, Completed, Settings, Search }
        private Tab _currentTab = Tab.Catalog;

        // Fallback de piezas si la ArtworkDefinition no tiene configurados los valores
        private const int DEFAULT_EASY   = 64;
        private const int DEFAULT_NORMAL = 144;
        private const int DEFAULT_HARD   = 256;

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
            btnCompletadas?.onClick.RemoveAllListeners();
            btnConfig?.onClick.RemoveAllListeners();
            btnBuscar?.onClick.RemoveAllListeners();
            btnDetailClose?.onClick.RemoveAllListeners();
            btnEasy?.onClick.RemoveAllListeners();
            btnNormal?.onClick.RemoveAllListeners();
            btnHard?.onClick.RemoveAllListeners();
            musicSlider?.onValueChanged.RemoveAllListeners();
            sfxSlider?.onValueChanged.RemoveAllListeners();
            hapticsToggle?.onValueChanged.RemoveAllListeners();
            searchInputField?.onValueChanged.RemoveAllListeners();
            btnVR?.onClick.RemoveAllListeners();
            btnMR?.onClick.RemoveAllListeners();
            btnGalerias?.onClick.RemoveAllListeners();
            if (gallerySelectionController != null)
                gallerySelectionController.OnGallerySelected -= OnGallerySelectionChanged;
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

            _allArtworks = catalog.GetAll();
            _allArtworks.Sort((a, b) =>
                string.Compare(a.title, b.title, StringComparison.OrdinalIgnoreCase));

            WireTabButtons();
            WireDetailButtons();
            WireSettingsControls();
            WireSearchControls();
            WireVRButtons();

            if (detailPanel != null) detailPanel.SetActive(false);
            if (vrControllerRequiredPanel != null) vrControllerRequiredPanel.SetActive(false);

            _isInitialized = true;
            Debug.Log($"[NativeGallery] Inicializado con {_allArtworks.Count} obras.");
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

            // Poblar catálogo solo la primera vez (las cards no cambian)
            if (!_catalogPopulated)
            {
                PopulateCatalog();
                _catalogPopulated = true;
            }

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
            btnInicio?.onClick.AddListener(()      => SwitchTab(Tab.Catalog));
            btnCompletadas?.onClick.AddListener(() => SwitchTab(Tab.Completed));
            btnConfig?.onClick.AddListener(()      => SwitchTab(Tab.Settings));
            btnBuscar?.onClick.AddListener(()      => SwitchTab(Tab.Search));
        }

        private void SwitchTab(Tab tab, bool force = false)
        {
            if (_currentTab == tab && !force) return;
            _currentTab = tab;

            if (catalogView != null)   catalogView.SetActive(tab == Tab.Catalog);
            if (completedView != null) completedView.SetActive(tab == Tab.Completed);
            if (settingsView != null)  settingsView.SetActive(tab == Tab.Settings);
            if (searchView != null)    searchView.SetActive(tab == Tab.Search);

            UpdateTabColors();

            switch (tab)
            {
                case Tab.Completed: PopulateCompleted(); break;
                case Tab.Settings:  PopulateSettings();  break;
                case Tab.Search:
                    searchInputField?.SetTextWithoutNotify(string.Empty);
                    PopulateSearch(string.Empty);
                    break;
            }
        }

        private void UpdateTabColors()
        {
            SetTabColor(btnInicio,      _currentTab == Tab.Catalog);
            SetTabColor(btnCompletadas, _currentTab == Tab.Completed);
            SetTabColor(btnConfig,      _currentTab == Tab.Settings);
            SetTabColor(btnBuscar,      _currentTab == Tab.Search);
            // VR/MR/Galerías are action buttons, not tabs — always use inactive color
            SetTabColor(btnVR,       false);
            SetTabColor(btnMR,       false);
            SetTabColor(btnGalerias, false);
            Debug.Log($"[NativeGallery] UpdateTabColors — tab={_currentTab} | btnVR normalColor={tabInactiveColor} (active={btnVR != null && btnVR.gameObject.activeSelf})");
        }

        private void SetTabColor(Button btn, bool active)
        {
            if (btn == null) return;
            var colors        = btn.colors;
            colors.normalColor = active ? tabActiveColor : tabInactiveColor;
            btn.colors        = colors;
        }

        // ════════════════════════════════════════════════════════════════════════
        //  POPULATE GRIDS
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Instancia una card por cada obra del catálogo.
        /// Solo se llama una vez — las cards no se destruyen entre vistas.
        /// </summary>
        private void PopulateCatalog()
        {
            if (catalogGridContainer == null || artworkCardPrefab == null)
            {
                Debug.LogWarning("[NativeGallery] catalogGridContainer o artworkCardPrefab no asignados.");
                return;
            }

            ClearGrid(catalogGridContainer);

            SaveData saveData = _saveData.GetCachedData();
            foreach (var artwork in _allArtworks)
            {
                if (artwork == null) continue;
                bool completed = IsCompleted(artwork.artworkId, saveData);
                SpawnCard(artwork, completed, catalogGridContainer);
            }

            Debug.Log($"[NativeGallery] Catálogo poblado con {_allArtworks.Count} cards.");
        }

        private void PopulateCompleted()
        {
            if (completedGridContainer == null || artworkCardPrefab == null) return;
            ClearGrid(completedGridContainer);

            SaveData saveData = _saveData.GetCachedData();
            int count = 0;
            foreach (var artwork in _allArtworks)
            {
                if (artwork == null) continue;
                if (!IsCompleted(artwork.artworkId, saveData)) continue;
                SpawnCard(artwork, true, completedGridContainer);
                count++;
            }

            if (completedEmptyLabel != null)
                completedEmptyLabel.gameObject.SetActive(count == 0);

            Debug.Log($"[NativeGallery] Completadas: {count} obras.");
        }

        private void PopulateSearch(string query)
        {
            if (searchGridContainer == null || artworkCardPrefab == null) return;
            ClearGrid(searchGridContainer);

            bool emptyQuery = string.IsNullOrWhiteSpace(query);
            if (searchEmptyLabel != null)
                searchEmptyLabel.gameObject.SetActive(emptyQuery);

            if (emptyQuery) return;

            string lower = query.ToLowerInvariant();
            SaveData saveData = _saveData.GetCachedData();
            int count = 0;

            foreach (var artwork in _allArtworks)
            {
                if (artwork == null) continue;

                bool matchTitle    = !string.IsNullOrEmpty(artwork.title)       &&
                                     artwork.title.ToLowerInvariant().Contains(lower);
                bool matchAuthor   = !string.IsNullOrEmpty(artwork.author)      &&
                                     artwork.author.ToLowerInvariant().Contains(lower);
                bool matchMovement = !string.IsNullOrEmpty(artwork.artMovement) &&
                                     artwork.artMovement.ToLowerInvariant().Contains(lower);

                if (!matchTitle && !matchAuthor && !matchMovement) continue;

                bool completed = IsCompleted(artwork.artworkId, saveData);
                SpawnCard(artwork, completed, searchGridContainer);
                count++;
            }

            if (count == 0 && searchEmptyLabel != null)
            {
                searchEmptyLabel.text = $"No results for \"{query}\"";
                searchEmptyLabel.gameObject.SetActive(true);
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
        }

        // ── Helpers de grid ───────────────────────────────────────────────────

        private void SpawnCard(ArtworkDefinition artwork, bool isCompleted, Transform container)
        {
            var go   = Instantiate(artworkCardPrefab, container);
            var card = go.GetComponent<ArtworkCardUI>();
            if (card != null)
                card.Setup(artwork, isCompleted, OnCardTapped);
            else
                Debug.LogWarning($"[NativeGallery] El prefab '{artworkCardPrefab.name}' no tiene ArtworkCardUI.");
        }

        private static void ClearGrid(Transform container)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
                Destroy(container.GetChild(i).gameObject);
        }

        private static bool IsCompleted(string artworkId, SaveData saveData)
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

            // Textos de dificultad con piece counts reales
            int easy   = artwork.pieceCountEasy   > 0 ? artwork.pieceCountEasy   : DEFAULT_EASY;
            int normal = artwork.pieceCountNormal > 0 ? artwork.pieceCountNormal : DEFAULT_NORMAL;
            int hard   = artwork.pieceCountHard   > 0 ? artwork.pieceCountHard   : DEFAULT_HARD;

            if (btnEasyText   != null) btnEasyText.text   = $"{easy} Pieces";
            if (btnNormalText != null) btnNormalText.text = $"{normal} Pieces";
            if (btnHardText   != null) btnHardText.text   = $"{hard} Pieces";

            detailPanel.SetActive(true);
        }

        private void StartPuzzle(int difficultyIndex)
        {
            if (_selectedArtwork == null) return;

            int pieceCount = difficultyIndex switch
            {
                0 => _selectedArtwork.pieceCountEasy   > 0 ? _selectedArtwork.pieceCountEasy   : DEFAULT_EASY,
                1 => _selectedArtwork.pieceCountNormal > 0 ? _selectedArtwork.pieceCountNormal : DEFAULT_NORMAL,
                2 => _selectedArtwork.pieceCountHard   > 0 ? _selectedArtwork.pieceCountHard   : DEFAULT_HARD,
                _ => DEFAULT_NORMAL
            };

            Debug.Log($"[NativeGallery] Iniciando puzzle: {_selectedArtwork.title} | " +
                      $"difficulty={difficultyIndex} | pieces={pieceCount}");

            OnStartPuzzle?.Invoke(_selectedArtwork.artworkId, pieceCount, difficultyIndex);
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
            btnGalerias?.onClick.AddListener(OnGaleriasClicked);

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
            // Galerías: shown in VR mode on all devices
            bool showGalerias = isVRMode;

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

        private void OnGaleriasClicked()
        {
            if (gallerySelectionController == null) return;
            // Retrieve active gallery ID from GameBootstrap/VRGalleryController
            string activeId = ArtUnbound.Core.GameBootstrap.Instance != null
                ? ArtUnbound.Core.GameBootstrap.Instance.ActiveVRGalleryId
                : "gallery_classic";
            gallerySelectionController.Show(activeId);
        }

        private void OnGallerySelectionChanged(string galleryId)
        {
            ArtUnbound.Core.GameBootstrap.Instance?.SwitchVRGallery(galleryId);
        }

        private TouchScreenKeyboard _searchKeyboard;

        private void WireSearchControls()
        {
            searchInputField?.onValueChanged.AddListener(PopulateSearch);

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
                    PopulateSearch(kbText);
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
