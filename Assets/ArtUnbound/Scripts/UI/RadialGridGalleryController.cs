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
    /// Controlador principal de la Galería Radial de selección de pinturas.
    ///
    /// ARQUITECTURA:
    ///   • Grid virtual de 14 filas × 15 columnas (210 slots para 202 pinturas).
    ///   • Solo se instancian ~60 celdas (object pooling).
    ///   • El "viewport" es un círculo de <see cref="visibleRadius"/> metros.
    ///   • Celdas dentro del viewport: visibles con fade radial; fuera: inactivas.
    ///   • Navegación: palma abierta → delta de posición → desplaza el offset del grid.
    ///   • Selección: pinch sobre celda seleccionable (zona A o B) → panel de detalle.
    ///
    /// INTEGRACIÓN:
    ///   • Expone el mismo evento <see cref="OnStartPuzzle"/> que UnifiedMainMenuController.
    ///   • GameBootstrap lo registra igual: OnStartPuzzle += OnUnifiedMenuStartPuzzle.
    ///   • Activar/desactivar con <see cref="Show"/> / <see cref="Hide"/>.
    ///
    /// CONFIGURACIÓN EN UNITY EDITOR:
    ///   Ver el comentario de la región "HIERARCHY SETUP" al final del archivo.
    /// </summary>
    public class RadialGridGalleryController : MonoBehaviour
    {
        // ════════════════════════════════════════════════════════════════════════
        //  EVENTOS
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Se dispara cuando el usuario elige una dificultad para comenzar el puzzle.
        /// Firma: (artworkId, pieceCount, difficultyIndex)  — igual que UnifiedMainMenu.
        /// </summary>
        public event Action<string, int, int> OnStartPuzzle;

        // ════════════════════════════════════════════════════════════════════════
        //  SERIALIZED FIELDS — GRID
        // ════════════════════════════════════════════════════════════════════════

        [Header("Grid — Dimensiones")]
        [Tooltip("Número de columnas del grid virtual.")]
        [SerializeField] private int gridColumns = 15;

        [Tooltip("Número de filas del grid virtual.")]
        [SerializeField] private int gridRows = 14;

        [Tooltip("Tamaño de cada celda en metros (ancho = alto = cuadrado).")]
        [SerializeField] private float cellSize = 0.15f;

        [Tooltip("Radio del círculo visible en metros. Fuera de él las celdas se apagan.")]
        [SerializeField] private float visibleRadius = 0.50f;

        [Tooltip("Distancia (metros) del grid al usuario al mostrarse.")]
        [SerializeField] private float gridDistance = 0.70f;

        [Tooltip("Desplazamiento vertical (metros) respecto a la altura de los ojos. Negativo = más abajo.")]
        [SerializeField] [Range(-0.40f, 0.10f)] private float heightOffset = -0.15f;

        [Header("Grid — Navegación")]
        [Tooltip("Drag por frame (0=sin freno, 0.15=freno fuerte). Controla la inercia.")]
        [SerializeField] [Range(0.05f, 0.25f)] private float inertiaDrag = 0.10f;

        [Tooltip("Velocidad mínima (grid-unidades/s) por debajo de la cual se activa el snap.")]
        [SerializeField] private float snapVelocityThreshold = 0.05f;

        [Tooltip("Duración del snap hacia la celda más cercana al soltar (segundos).")]
        [SerializeField] private float snapDuration = 0.18f;

        [Header("Grid — Pool")]
        [Tooltip("Número de celdas preasignadas. Debe ser >= celdas visibles en el círculo (~50).")]
        [SerializeField] private int cellPoolSize = 64;

        // ════════════════════════════════════════════════════════════════════════
        //  SERIALIZED FIELDS — REFERENCIAS DE ESCENA
        // ════════════════════════════════════════════════════════════════════════

        [Header("Referencias — Escena")]
        [Tooltip("Transform vacío hijo del objeto. Las celdas se crean como hijos de él.")]
        [SerializeField] private Transform gridRoot;

        [Tooltip("RadialGridInputHandler en la misma jerarquía.")]
        [SerializeField] private RadialGridInputHandler inputHandler;

        [Header("Referencias — Panel de Detalle")]
        [Tooltip("GameObject raíz del panel de detalle (Canvas World Space o panel 3D).")]
        [SerializeField] private GameObject detailPanel;

        [Tooltip("RawImage donde se muestra la imagen completa de la obra seleccionada.")]
        [SerializeField] private RawImage detailArtworkImage;

        [SerializeField] private TextMeshProUGUI detailTitle;
        [SerializeField] private TextMeshProUGUI detailAuthor;
        [SerializeField] private TextMeshProUGUI detailDescription;

        [Tooltip("Botón Fácil.")]
        [SerializeField] private Button easyButton;
        [Tooltip("Botón Normal.")]
        [SerializeField] private Button normalButton;
        [Tooltip("Botón Difícil.")]
        [SerializeField] private Button hardButton;
        [Tooltip("Botón para cerrar el panel de detalle.")]
        [SerializeField] private Button closeButton;

        [Tooltip("Overlay semiopaco que oscurece el grid cuando una pintura está seleccionada.")]
        [SerializeField] private GameObject dimmingOverlay;

        // ════════════════════════════════════════════════════════════════════════
        //  ESTADO INTERNO
        // ════════════════════════════════════════════════════════════════════════

        // Lista ordenada de obras (del catálogo)
        private List<ArtworkDefinition> _artworks = new List<ArtworkDefinition>();

        // Offset virtual del grid: índice de columna/fila que está en el centro
        private float _offsetX;   // columna centrada (0 … gridColumns-1)
        private float _offsetY;   // fila centrada    (0 … gridRows-1)

        // Velocidad de inercia en grid-unidades/segundo
        private Vector2 _velocity;

        // Pool de celdas
        private readonly Queue<RadialGridCell>              _pool       = new Queue<RadialGridCell>();
        private readonly Dictionary<(int, int), RadialGridCell> _active = new Dictionary<(int, int), RadialGridCell>();

        // Estado de la galería
        private bool _isVisible;
        private bool _isDetailShown;
        private ArtworkDefinition  _currentDetailArtwork;
        private RadialGridCell     _expandedCell;

        // Coroutines
        private Coroutine _snapCoroutine;
        private Coroutine _selectionCoroutine;

        // Escala final de la celda expandida (aproximadamente 4× para que ocupe ~60 cm)
        private const float EXPANDED_SCALE_MULTIPLIER = 4.0f;

        // ════════════════════════════════════════════════════════════════════════
        //  INICIALIZACIÓN
        // ════════════════════════════════════════════════════════════════════════

        private void Awake()
        {
            // Wiring de botones del panel de detalle
            if (easyButton)   easyButton.onClick.AddListener(()   => StartPuzzle(0));
            if (normalButton) normalButton.onClick.AddListener(() => StartPuzzle(1));
            if (hardButton)   hardButton.onClick.AddListener(()   => StartPuzzle(2));
            if (closeButton)  closeButton.onClick.AddListener(CloseDetail);

            gameObject.SetActive(false);
        }

        /// <summary>
        /// Llamado por GameBootstrap en InitializeServices().
        /// Carga el catálogo y prepara el pool de celdas.
        /// </summary>
        public void Initialize(LocalCatalogService catalogService, SaveDataService saveDataService)
        {
            if (catalogService != null)
                _artworks = new List<ArtworkDefinition>(catalogService.GetAll());

            BuildCellPool();
        }

        // ─────────────────────────────────────────────────────────────────────────
        private void BuildCellPool()
        {
            if (gridRoot == null)
            {
                Debug.LogError("[RadialGrid] gridRoot no asignado en el Inspector.");
                return;
            }

            for (int i = 0; i < cellPoolSize; i++)
            {
                // IMPORTANTE: AddComponent primero, SetParent después.
                // Awake() NO se ejecuta si el padre está en una jerarquía inactiva.
                // Al crear el GO en la raíz de la escena (activo), Awake() corre y
                // _sr queda asignado antes de que lo emparentemos al gridRoot inactivo.
                var go   = new GameObject($"Cell_{i:00}");
                go.AddComponent<SpriteRenderer>();
                var cell = go.AddComponent<RadialGridCell>();  // Awake() corre aquí ✓
                go.transform.SetParent(gridRoot, false);       // Emparetar DESPUÉS
                go.SetActive(false);
                _pool.Enqueue(cell);
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        //  SHOW / HIDE
        // ════════════════════════════════════════════════════════════════════════

        // v2025-04-15 — dwell activation + Y-fix + positioning coroutine
        public void Show()
        {
            if (_isVisible) return;
            _isVisible = true;

            // Offset inicial: centro del catálogo
            _offsetX = Mathf.Floor(gridColumns / 2f);
            _offsetY = Mathf.Floor(gridRows    / 2f);
            _velocity = Vector2.zero;

            HideDetailImmediate();

            // El GameObject debe estar ACTIVO antes de StartCoroutine;
            // las corrutinas no corren en objetos inactivos.
            gameObject.SetActive(true);
            StartCoroutine(PositionAndReveal());
        }

        /// <summary>
        /// Espera a que el head-tracking esté estabilizado, posiciona el grid y lo revela.
        /// Mismo patrón que GameBootstrap.PositionCanvasWithDelay.
        /// </summary>
        private IEnumerator PositionAndReveal()
        {
            // Ocultar sólo las celdas (no el GO completo) para poder seguir corriendo
            if (gridRoot != null) gridRoot.gameObject.SetActive(false);

            if (Camera.main == null)
            {
                Debug.LogWarning("[RadialGrid] Camera.main es null. Mostrando sin posicionar.");
                if (gridRoot != null) gridRoot.gameObject.SetActive(true);
                RefreshCells();
                inputHandler?.SetActive(true);
                yield break;
            }

            // Si el head-tracking todavía no dio una posición válida (y < 1.2 m),
            // esperamos hasta MAX_WAIT segundos (igual que PositionCanvasWithDelay usa 2 s).
            const float MAX_WAIT = 3f;
            float waited = 0f;
            while (Camera.main.transform.position.y < 1.2f && waited < MAX_WAIT)
            {
                yield return null;
                waited += Time.deltaTime;
            }

            PositionGridInFrontOfUser();

            // Revelar celdas y arrancar input
            if (gridRoot != null) gridRoot.gameObject.SetActive(true);
            RefreshCells();
            inputHandler?.SetActive(true);
        }

        public void Hide()
        {
            if (!_isVisible) return;
            _isVisible = false;

            inputHandler?.SetActive(false);
            HideDetailImmediate();
            ReturnAllCellsToPool();

            gameObject.SetActive(false);
        }

        // ─────────────────────────────────────────────────────────────────────────
        private void PositionGridInFrontOfUser()
        {
            if (Camera.main == null) return;

            Transform cam     = Camera.main.transform;
            Vector3   headPos = cam.position;

            // Clamp de altura igual que GameBootstrap (evita posiciones absurdas al arrancar)
            headPos.y = Mathf.Clamp(headPos.y, 1.2f, 2.0f);

            // Dirección horizontal hacia donde mira el usuario
            Vector3 forward = cam.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            forward.Normalize();

            // Posición: delante + ligero descenso para que el centro del grid
            // quede a la altura natural de las manos (igual que el canvas principal)
            Vector3 targetPos = headPos + forward * gridDistance;
            targetPos.y      += heightOffset;

            transform.position = targetPos;

            // -forward: el panel encara al usuario (+Z del grid apunta hacia la cámara).
            // Así local.z > 0 significa "frente al panel" en IsHandInPanelZone.
            transform.rotation = Quaternion.LookRotation(-forward, Vector3.up);
        }

        // ════════════════════════════════════════════════════════════════════════
        //  UPDATE — INERCIA
        // ════════════════════════════════════════════════════════════════════════

        private void Update()
        {
            if (!_isVisible || _isDetailShown) return;

            if (_velocity.sqrMagnitude > snapVelocityThreshold * snapVelocityThreshold)
            {
                // Aplicar desplazamiento
                _offsetX += _velocity.x * Time.deltaTime;
                _offsetY += _velocity.y * Time.deltaTime;
                ClampOffset();

                // Aplicar drag (frame-rate independent)
                float dragFactor = Mathf.Pow(1f - inertiaDrag, Time.deltaTime * 60f);
                _velocity *= dragFactor;

                // Si la velocidad cae por debajo del umbral, iniciar snap
                if (_velocity.sqrMagnitude <= snapVelocityThreshold * snapVelocityThreshold)
                {
                    _velocity = Vector2.zero;
                    TriggerSnap();
                }

                RefreshCells();
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        //  API PÚBLICA — INPUT (llamada desde RadialGridInputHandler)
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>La palma abierta entró en el panel — listo para navegar.</summary>
        /// <summary>
        /// Llamado por el InputHandler mientras el usuario hace dwell (palma quieta frente al grid).
        /// <paramref name="t"/> va de 0 (recién entró) a 1 (listo para navegar).
        /// Cuando se llama con t=0 se cancela el feedback visual.
        /// </summary>
        public void OnDwellProgress(float t)
        {
            if (gridRoot == null) return;
            // Pulso suave: el grid crece de 1.00 a 1.04 conforme el dwell avanza
            float scale = 1f + 0.04f * Mathf.SmoothStep(0f, 1f, t);
            gridRoot.localScale = new Vector3(scale, scale, 1f);
        }

        public void OnPalmNavigationStart()
        {
            // Devolver escala a normal con un pequeño "pop"
            if (gridRoot != null)
                gridRoot.localScale = Vector3.one;

            StopSnapCoroutine();
            _velocity = Vector2.zero;
        }

        /// <summary>
        /// La palma se desplazó <paramref name="worldDelta2D"/> metros en el plano del panel.
        /// Positivo X = grid se mueve a la derecha (se ven más obras a la derecha).
        /// Positivo Y = grid sube (se ven más obras arriba).
        /// </summary>
        public void OnPalmNavigationDelta(Vector2 worldDelta2D)
        {
            if (_isDetailShown) return;

            // Convertir metros → grid-unidades
            // +X = desplazar contenido a la derecha,  +Y = desplazar contenido hacia arriba
            _offsetX += worldDelta2D.x / cellSize;
            _offsetY += worldDelta2D.y / cellSize;
            ClampOffset();
            RefreshCells();
        }

        /// <summary>
        /// El usuario soltó la palma. <paramref name="worldVelocity2D"/> es la velocidad
        /// promedio (metros/s) para aplicar inercia.
        /// </summary>
        public void OnPalmNavigationEnded(Vector2 worldVelocity2D)
        {
            if (_isDetailShown) return;

            // Convertir velocidad de metros/s a grid-unidades/s (misma convención que Delta)
            _velocity = new Vector2(
                worldVelocity2D.x / cellSize,
                worldVelocity2D.y / cellSize
            );
        }

        /// <summary>
        /// El usuario hizo pinch en la posición <paramref name="worldPos"/>.
        /// Busca la celda más cercana dentro del radio de selección.
        /// </summary>
        public void OnPinchAtPosition(Vector3 worldPos, float searchRadius, float depthTolerance)
        {
            if (!_isVisible) return;

            // Si el panel de detalle está abierto, los clics son manejados por los botones de Unity UI
            if (_isDetailShown) return;

            // Proyectar la posición al espacio local del gridRoot
            if (gridRoot == null) return;
            Vector3 local = gridRoot.InverseTransformPoint(worldPos);

            // Ignorar si está demasiado lejos del plano del grid
            if (Mathf.Abs(local.z) > depthTolerance) return;

            // Buscar la celda más cercana en las activas
            RadialGridCell nearest     = null;
            float          nearestDist = searchRadius;

            foreach (var cell in _active.Values)
            {
                if (!cell.IsSelectable || !cell.HasArtwork) continue;

                Vector3 cellLocal = gridRoot.InverseTransformPoint(cell.transform.position);
                float   dist2D    = new Vector2(cellLocal.x - local.x, cellLocal.y - local.y).magnitude;

                if (dist2D < nearestDist)
                {
                    nearestDist = dist2D;
                    nearest     = cell;
                }
            }

            if (nearest != null)
                OnCellSelected(nearest);
        }

        // ════════════════════════════════════════════════════════════════════════
        //  GESTIÓN DEL POOL Y POSICIONAMIENTO
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Actualiza qué celdas son visibles según el offset actual y
        /// recalcula sus posiciones y efectos visuales.
        /// </summary>
        private void RefreshCells()
        {
            if (gridRoot == null) return;

            // ── 1. Calcular qué posiciones (col, row) deben estar activas ────────
            var needed  = new HashSet<(int, int)>();
            int cCol    = Mathf.RoundToInt(_offsetX);
            int cRow    = Mathf.RoundToInt(_offsetY);
            int range   = Mathf.CeilToInt(visibleRadius / cellSize) + 1;  // +1 de margen

            for (int row = cRow - range; row <= cRow + range; row++)
            {
                for (int col = cCol - range; col <= cCol + range; col++)
                {
                    if (col < 0 || col >= gridColumns) continue;
                    if (row < 0 || row >= gridRows)    continue;

                    // ¿Esta posición cae dentro del círculo visible (+ margen)?
                    Vector2 lp   = CellLocalPos(col, row);
                    if (lp.magnitude <= visibleRadius * 1.15f)
                        needed.Add((col, row));
                }
            }

            // ── 2. Devolver al pool las celdas que ya no se necesitan ────────────
            var toReturn = new List<(int, int)>();
            foreach (var kv in _active)
            {
                if (!needed.Contains(kv.Key))
                {
                    kv.Value.SetArtwork(null, 0, 0, cellSize);
                    kv.Value.gameObject.SetActive(false);
                    _pool.Enqueue(kv.Value);
                    toReturn.Add(kv.Key);
                }
            }
            foreach (var k in toReturn) _active.Remove(k);

            // ── 3. Asignar celdas a las posiciones que las necesitan ─────────────
            foreach (var pos in needed)
            {
                (int col, int row) = pos;

                if (!_active.ContainsKey(pos))
                {
                    if (_pool.Count == 0)
                    {
                        Debug.LogWarning("[RadialGrid] Pool agotado — aumenta cellPoolSize en el Inspector.");
                        continue;
                    }

                    RadialGridCell cell  = _pool.Dequeue();
                    int            idx   = row * gridColumns + col;
                    ArtworkDefinition aw = idx >= 0 && idx < _artworks.Count ? _artworks[idx] : null;
                    cell.SetArtwork(aw, col, row, cellSize);
                    _active[pos] = cell;
                }

                // ── 4. Actualizar posición y visuals ─────────────────────────────
                RadialGridCell c  = _active[pos];
                Vector2        lp = CellLocalPos(col, row);
                c.transform.localPosition = new Vector3(lp.x, lp.y, 0f);

                float normDist = lp.magnitude / visibleRadius;
                c.UpdateRadialVisuals(normDist);
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Posición local (en el espacio de gridRoot) de la celda (col, row)
        /// dado el offset actual del grid.
        /// </summary>
        private Vector2 CellLocalPos(int col, int row)
        {
            return new Vector2(
                (col - _offsetX) * cellSize,
                (_offsetY - row) * cellSize   // Y: fila 0 arriba → valores positivos arriba
            );
        }

        // ─────────────────────────────────────────────────────────────────────────
        private void ClampOffset()
        {
            _offsetX = Mathf.Clamp(_offsetX, 0f, gridColumns - 1f);
            _offsetY = Mathf.Clamp(_offsetY, 0f, gridRows    - 1f);
        }

        // ─────────────────────────────────────────────────────────────────────────
        private void ReturnAllCellsToPool()
        {
            foreach (var kv in _active)
            {
                kv.Value.SetArtwork(null, 0, 0, cellSize);
                kv.Value.gameObject.SetActive(false);
                _pool.Enqueue(kv.Value);
            }
            _active.Clear();
        }

        // ════════════════════════════════════════════════════════════════════════
        //  SNAP AL CENTRO
        // ════════════════════════════════════════════════════════════════════════

        private void TriggerSnap()
        {
            StopSnapCoroutine();
            _snapCoroutine = StartCoroutine(SnapCoroutine());
        }

        private void StopSnapCoroutine()
        {
            if (_snapCoroutine != null)
            {
                StopCoroutine(_snapCoroutine);
                _snapCoroutine = null;
            }
        }

        private IEnumerator SnapCoroutine()
        {
            float targetX = Mathf.Round(Mathf.Clamp(_offsetX, 0f, gridColumns - 1f));
            float targetY = Mathf.Round(Mathf.Clamp(_offsetY, 0f, gridRows    - 1f));
            float startX  = _offsetX;
            float startY  = _offsetY;
            float elapsed = 0f;

            while (elapsed < snapDuration)
            {
                elapsed += Time.deltaTime;
                float t     = Mathf.Clamp01(elapsed / snapDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);   // ease-out cubic
                _offsetX    = Mathf.Lerp(startX, targetX, eased);
                _offsetY    = Mathf.Lerp(startY, targetY, eased);
                RefreshCells();
                yield return null;
            }

            _offsetX = targetX;
            _offsetY = targetY;
            RefreshCells();
            _snapCoroutine = null;
        }

        // ════════════════════════════════════════════════════════════════════════
        //  SELECCIÓN DE CELDA
        // ════════════════════════════════════════════════════════════════════════

        private void OnCellSelected(RadialGridCell cell)
        {
            if (_isDetailShown) return;

            _expandedCell = cell;
            StopSnapCoroutine();
            _velocity = Vector2.zero;

            if (_selectionCoroutine != null) StopCoroutine(_selectionCoroutine);
            _selectionCoroutine = StartCoroutine(ExpandCellCoroutine(cell));
        }

        // ─────────────────────────────────────────────────────────────────────────
        private IEnumerator ExpandCellCoroutine(RadialGridCell cell)
        {
            _isDetailShown = true;

            // Escala base antes de la animación (incluye el factor radial actual)
            float startScale = cell.transform.localScale.x;
            float targetScale = (cellSize / Mathf.Max(cell.transform.localScale.x, 0.001f))
                                 * startScale * EXPANDED_SCALE_MULTIPLIER;
            // Simplificado: escalar hasta EXPANDED_SCALE_MULTIPLIER veces la escala de celda al 100%
            // En la práctica: _baseScale * EXPANDED_SCALE_MULTIPLIER
            // Pero no tenemos acceso directo a _baseScale desde aquí.
            // Usamos una escala relativa respecto al tamaño de celda en mundo:
            float worldTargetSize = cellSize * EXPANDED_SCALE_MULTIPLIER;  // e.g. 0.15 * 4 = 0.60m

            // Mover celda ligeramente hacia el usuario para que quede sobre el dimming
            Vector3 startPos = cell.transform.localPosition;
            Vector3 targetPos = new Vector3(startPos.x, startPos.y, cellSize * 0.3f);

            float duration = 0.30f;
            float elapsed  = 0f;

            // Activar dimming
            SetDimming(true);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t     = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);  // ease-out cubic

                // Escalar la celda proporcionalmente al tamaño de mundo deseado
                float currentWorldSize = Mathf.Lerp(cellSize, worldTargetSize, eased);
                // La celda usa localScale donde _baseScale * 1.0 = cellSize en mundo
                // Así que: localScale = currentWorldSize / cellSize * baseScaleAt1.0
                // Sabemos startScale ≈ _baseScale * 1.0 (si estaba en zona A)
                cell.transform.localScale = Vector3.one * (startScale * (currentWorldSize / cellSize));
                cell.transform.localPosition = Vector3.Lerp(startPos, targetPos, eased);

                yield return null;
            }

            // Mostrar panel de detalle
            ShowDetailPanel(cell.Artwork);
        }

        // ════════════════════════════════════════════════════════════════════════
        //  PANEL DE DETALLE
        // ════════════════════════════════════════════════════════════════════════

        private void ShowDetailPanel(ArtworkDefinition artwork)
        {
            if (artwork == null || detailPanel == null) return;

            _currentDetailArtwork = artwork;

            // ── Rellenar datos ────────────────────────────────────────────────
            if (detailTitle)       detailTitle.text       = artwork.title;
            if (detailAuthor)      detailAuthor.text      = artwork.author;
            if (detailDescription) detailDescription.text = artwork.description;

            if (detailArtworkImage != null)
            {
                Texture2D tex = null;
                if (artwork.fullImage  != null) tex = artwork.fullImage.texture;
                else if (artwork.thumbnail != null) tex = artwork.thumbnail.texture;
                detailArtworkImage.texture = tex;
            }

            // ── Actualizar textos de los botones de dificultad ────────────────
            SetDifficultyButtonText(easyButton,   artwork, 0);
            SetDifficultyButtonText(normalButton, artwork, 1);
            SetDifficultyButtonText(hardButton,   artwork, 2);

            // ── Posicionar el panel a la derecha de la celda expandida ────────
            if (_expandedCell != null && gridRoot != null)
            {
                float expandedWorldSize = cellSize * EXPANDED_SCALE_MULTIPLIER;
                // Desplazar en el espacio local del gridRoot
                Vector3 cellLocal = _expandedCell.transform.localPosition;
                float   xOffset   = expandedWorldSize * 0.55f + 0.10f; // margen derecho
                detailPanel.transform.localPosition = new Vector3(
                    cellLocal.x + xOffset,
                    cellLocal.y,
                    cellLocal.z - cellSize * 0.1f
                );
                detailPanel.transform.localRotation = Quaternion.identity;
            }

            detailPanel.SetActive(true);
        }

        // ─────────────────────────────────────────────────────────────────────────
        private void SetDifficultyButtonText(Button btn, ArtworkDefinition artwork, int diffIdx)
        {
            if (btn == null) return;
            int pieceCount = artwork.GetPieceCount(diffIdx);
            var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
                tmp.text = $"{pieceCount} Pieces";
        }

        // ─────────────────────────────────────────────────────────────────────────
        /// <summary>El usuario cerró el panel de detalle.</summary>
        private void CloseDetail()
        {
            if (_selectionCoroutine != null) StopCoroutine(_selectionCoroutine);
            _selectionCoroutine = StartCoroutine(CollapseCoroutine());
        }

        // ─────────────────────────────────────────────────────────────────────────
        private IEnumerator CollapseCoroutine()
        {
            HideDetailImmediate();

            if (_expandedCell != null)
            {
                float startScale = _expandedCell.transform.localScale.x;
                // Escala objetivo: como si estuviera en zona A (normalizedDist ≈ 0 → scale = 1.0 * baseScale)
                // Aproximamos con startScale / EXPANDED_SCALE_MULTIPLIER
                float targetScale = startScale / EXPANDED_SCALE_MULTIPLIER;

                Vector3 startPos  = _expandedCell.transform.localPosition;
                Vector3 targetPos = new Vector3(startPos.x, startPos.y, 0f);

                float duration = 0.22f;
                float elapsed  = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t     = Mathf.Clamp01(elapsed / duration);
                    float eased = 1f - Mathf.Pow(1f - t, 3f);
                    _expandedCell.transform.localScale    = Vector3.one * Mathf.Lerp(startScale, targetScale, eased);
                    _expandedCell.transform.localPosition = Vector3.Lerp(startPos, targetPos, eased);
                    yield return null;
                }

                _expandedCell.transform.localPosition = targetPos;
                _expandedCell = null;
            }

            _isDetailShown = false;

            // Restaurar visuals del grid completo
            RefreshCells();
        }

        // ─────────────────────────────────────────────────────────────────────────
        private void HideDetailImmediate()
        {
            if (detailPanel != null) detailPanel.SetActive(false);
            SetDimming(false);
            _isDetailShown = false;
            _currentDetailArtwork = null;
        }

        // ─────────────────────────────────────────────────────────────────────────
        private void SetDimming(bool on)
        {
            if (dimmingOverlay != null)
                dimmingOverlay.SetActive(on);
        }

        // ════════════════════════════════════════════════════════════════════════
        //  INICIAR PUZZLE
        // ════════════════════════════════════════════════════════════════════════

        private void StartPuzzle(int difficultyIndex)
        {
            if (_currentDetailArtwork == null) return;

            int pieceCount = _currentDetailArtwork.GetPieceCount(difficultyIndex);
            OnStartPuzzle?.Invoke(_currentDetailArtwork.artworkId, pieceCount, difficultyIndex);
        }
    }

}
