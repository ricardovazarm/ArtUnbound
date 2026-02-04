# Plan de Implementacion (C#)
- Proyecto base: Unity Mixed Reality Template
- Objetivo: Implementar la logica del juego con preferencia a codigo C#

## 1. Estructura y convenciones
- Crear carpeta `Assets/ArtUnbound/Scripts` con subcarpetas:
  - `Core/` (arranque, flujo de escenas)
  - `MR/` (paredes, anclajes, lienzo)
  - `Gameplay/` (puzzle, piezas, snapping)
  - `Input/` (hand tracking)
  - `UI/` (menus, HUD, galeria)
  - `Data/` (ScriptableObjects y modelos)
  - `Services/` (guardado local, telemetria, desbloqueo semanal)
- Convencion de namespaces: `ArtUnbound.Core`, `ArtUnbound.Gameplay`, etc.
- Todos los scripts deben ser componentes puros; evitar logica en la escena.

## 2. ScriptableObjects (Datos)
### 2.1. `ArtworkDefinition`
- Campos obligatorios:
  - `string artworkId` (ID unico)
  - `string title`, `string author`, `int year`
  - `float aspectRatio` (ancho/alto)
  - `Vector2 baseSizePortrait` (alto=0.70, ancho calculado)
  - `Vector2 baseSizeLandscape` (alto=0.50, ancho calculado)
  - `Texture2D previewTexture`
  - `Texture2D puzzleTexture`
- Metodo util:
  - `Vector2 GetBaseSize(Orientation orientation)`

### 2.2. `PuzzleConfig`
- Campos:
  - `int[] pieceCounts = {64,144,256,512}`
  - `float snapDistanceCm = 3.0f`
  - `float pinchRangeCm = 1.0f`
  - `float pieceSizeCm = 5.0f` (ancho/alto)
  - `float pieceThicknessCm = 0.5f`
  - `bool helpModeDefault`
  - `bool useGridSnapping = true`
  - `bool useTriangularMorphology = true`

### 2.3. `FrameConfig`
- Campos:
  - `FrameTier tier` (madera, bronce, plata, oro, ebano)
  - `Material frameMaterial`
  - `int scoreThreshold`
  - `bool requiresNoHelp`

## 3. Modelos de datos (C# puros)
- `SaveData`:
  - `Dictionary<string, ArtworkProgress> progressByArtwork`
  - `List<string> completedArtworks`
  - `List<PlacedArtwork> placedArtworks` (cuadros colgados y retirados)
  - `GameSettings settings`
  - `string lastArtworkId`
  - `GameMode lastGameMode` (Galeria o Confort)
  - `bool onboardingCompleted`
  - `DateTime firstPlayDate`
- `ArtworkProgress`:
  - `Dictionary<int, ArtworkRecord> recordsByPieceCount` (records por 64/144/256/512)
  - `FrameTier bestFrameTier` (mejor marco obtenido en cualquier config)
  - `DateTime firstCompletedAt`
  - `int totalCompletions`
- `PieceEdgeState` (enum):
  - `Flat`, `Positive`, `Negative`
- `PieceMorphology`:
  - `PieceEdgeState top, right, bottom, left`
  - `bool IsCompatibleWith(PieceMorphology other, EdgeSide mySide)`
- `PieceState` (enum):
  - `InPool`, `Grabbed`, `Placed`, `Returning`
- `GameMode` (enum):
  - `Gallery`, `Comfort`
- `EdgeSide` (enum):
  - `Top`, `Right`, `Bottom`, `Left`
- `ArtworkRecord`:
  - `int pieceCount`
  - `int bestTimeSec`
  - `int bestScore`
  - `FrameTier bestFrameTier`
  - `DateTime completedAt`
- `PlacedArtwork`:
  - `string artworkId`
  - `string anchorId`
  - `FrameTier frameTier`
  - `Vector3 position`
  - `Quaternion rotation`
  - `bool isActive` (true si está colgado, false si retirado)
- `GameSettings`:
  - `bool helpModeDefault`
  - `float sfxVolume`
  - `float musicVolume`
  - `bool showOnboarding`
- `PuzzleSessionData`:
  - `string artworkId`
  - `int pieceCount`
  - `float elapsedTime`
  - `int piecesPlaced`
  - `bool helpModeUsed`
  - `DateTime startedAt`

## 4. Componentes (MonoBehaviours) con responsabilidades y API
### 4.1. `GameBootstrap`
- Serializa referencias a servicios y config.
- `void Awake()`:
  - Inicializa `SaveDataService`, `LocalCatalogService`, `WeeklyUnlockService`.
  - Emite evento `OnBootstrapReady`.

### 4.2. `WallSelectionController`
- `void StartWallScan()` inicia deteccion de paredes.
- `void ConfirmWall(Plane wall)` crea el marco del lienzo.
- Expone evento `OnWallConfirmed`.

### 4.3. `CanvasFrameController`
- Campos: `Vector2 currentSize`, `Orientation orientation`.
- Metodos:
  - `SetBaseSize(ArtworkDefinition def)`
  - `ResizeFromCorner(Vector3 handPos)`
  - `bool IsInsideFrame(Vector3 worldPos)`

### 4.3.1. `ComfortModeController`
- Es el controlador **por defecto** para el inicio.
- Campos:
  - `float distanceFromHead = 0.8f`
  - `float tiltAngle = 15f`
  - `Transform headTransform`
  - `bool isLocked = false`
- Metodos:
  - `void PositionCanvas()`: Calcula y aplica posicion ergonomica.
  - `void LockPosition()`: Fija el lienzo, `isLocked = true`.
  - `void UnlockPosition()`: Permite reposicionar.
  - `Vector3 GetErgonomicPosition()`: Retorna posicion ideal.
  - `Quaternion GetErgonomicRotation()`: Retorna rotacion con tilt.
- Logica de posicionamiento:
  ```csharp
  Vector3 forward = headTransform.forward;
  forward.y = 0; // Mantener horizontal
  forward.Normalize();
  Vector3 position = headTransform.position + forward * distanceFromHead;
  position.y -= 0.1f; // Ligeramente abajo del nivel de ojos
  Quaternion rotation = Quaternion.LookRotation(-forward) * Quaternion.Euler(tiltAngle, 0, 0);
  ```

### 4.4. `PuzzleBoard`
- Campos: `List<PuzzleSlot> slots`, `float snapDistanceCm`.
- Metodos:
  - `Initialize(ArtworkDefinition def, int pieceCount)`
  - `bool TrySnapPiece(PuzzlePiece piece)`
  - `bool IsComplete()`
- Eventos:
  - `OnPieceSnapped`, `OnCompleted`
 - Validaciones:
   - Grid snapping al centro de celda.
   - Compatibilidad triangular con vecinos.
   - Feedback de error/exito segun modo ayuda y celda correcta.

### 4.5. `PuzzlePiece`
- Campos:
  - `int pieceId`
  - `Transform grabAnchor`
  - `PieceState currentState = PieceState.InPool`
  - `PieceMorphology morphology`
  - `int correctSlotIndex` (indice de su posicion correcta)
- Metodos:
  - `void SetState(PieceState newState)`
  - `void SetDragged(bool isDragged)`: Cambia a Grabbed/InPool.
  - `void SetSnapped(Vector3 pos, Quaternion rot)`: Cambia a Placed.
  - `void ReturnToPool(Vector3 poolPosition)`: Inicia animacion de retorno.
  - `bool IsInCorrectSlot(int slotIndex)`: Verifica si esta en su lugar.
- Emite `OnReleased` para que `PuzzleBoard` valide.
- Emite `OnStateChanged(PieceState)` para UI/feedback.

### 4.5.1. `PieceMeshGenerator` (static class)
- Constantes:
  - `const float PIECE_SIZE = 0.05f` (5cm)
  - `const float PIECE_THICKNESS = 0.005f` (0.5cm)
  - `const float TRIANGLE_SIZE = 0.015f` (1.5cm)
- Metodos:
  - `static Mesh GenerateMesh(PieceMorphology morphology)`
  - `static void AddFrontFace(List<Vector3> verts, List<int> tris, PieceMorphology m)`
  - `static void AddBackFace(List<Vector3> verts, List<int> tris)`
  - `static void AddSideFaces(List<Vector3> verts, List<int> tris, PieceMorphology m)`
  - `static void AddTriangleExtrusion(List<Vector3> verts, List<int> tris, EdgeSide side, bool isPositive)`
  - `static Vector2[] CalculateUVs(int pieceIndex, int columns, int rows, float aspectRatio)`
- Algoritmo GenerateMesh:
  ```csharp
  // 1. Crear vertices base (cuadrado 5x5cm)
  // 2. Por cada lado (top, right, bottom, left):
  //    - Si Positive: agregar triangulo hacia afuera
  //    - Si Negative: crear hueco triangular
  //    - Si Flat: borde recto
  // 3. Duplicar cara frontal con offset Z para grosor
  // 4. Conectar caras con quads laterales
  // 5. Calcular normales
  // 6. Retornar mesh
  ```

### 4.6. `HandTrackingInputController`
- Detecta `PinchStart`, `PinchHold`, `PinchEnd`.
- Detecta `SwipeHorizontal` para scroll.
- Parametros: `pinchRangeCm`.
- Expone eventos para `PuzzlePiece` y `PieceScrollController`.

### 4.7. `HelpModeController`
- `bool IsHelpEnabled`
- `void SetHelp(bool enabled)`
- `void PlayHelpFeedback(Vector3 pos)`

### 4.8. `PieceScrollController`
- `void Initialize(List<PuzzlePiece> pieces)`
- `void OnSwipe(float delta)`

### 4.9. `GalleryController`
- `void ShowCompleted()`
- `void SpawnArtworkFrame(string artworkId)`

### 4.10. `AnchorPersistenceController`
- `void SaveAnchor(string artworkId, Transform frame)`
- `void RestoreAnchors()`
- Si falla, emite `OnAnchorRestoreFailed`.

### 4.12. `PieceCountSelectorController`
- Campos:
  - `int[] availableCounts = {64, 144, 256, 512}`
  - `int selectedCount = 64`
- Metodos:
  - `void ShowSelector()`: Muestra UI de seleccion.
  - `void SelectCount(int count)`: Guarda seleccion.
  - `int GetSelectedCount()`: Retorna count elegido.
- Eventos: `OnCountSelected(int count)`

### 4.13. `PostGameController`
- Campos:
  - `PuzzleSessionData sessionData`
  - `int finalScore`
  - `FrameTier awardedFrame`
- Metodos:
  - `void ShowResults(PuzzleSessionData data, int score, FrameTier frame)`
  - `void OnPlaceArtwork()`: Inicia flujo de colocar cuadro.
  - `void OnReturnToMenu()`: Vuelve al menu principal.
  - `void OnReplay()`: Reinicia el mismo puzzle.
- UI muestra: tiempo, piezas, score, marco ganado, botones de accion.

### 4.14. `GalleryPanelController`
- Campos:
  - `List<ArtworkProgress> completedArtworks`
  - `List<PlacedArtwork> placedArtworks`
  - `List<PlacedArtwork> retiredArtworks`
- Metodos:
  - `void ShowGallery()`: Muestra panel de galeria.
  - `void ShowCompletedTab()`: Lista obras completadas con records.
  - `void ShowPlacedTab()`: Lista cuadros colgados en casa.
  - `void ShowRetiredTab()`: Lista cuadros retirados.
  - `void OnSelectArtwork(string artworkId)`: Muestra detalle.
  - `void OnReplayArtwork(string artworkId)`: Inicia rejuego.
  - `void OnRelocateArtwork(string artworkId)`: Inicia reubicacion.
- Eventos: `OnArtworkSelected`, `OnReplayRequested`

### 4.15. `ArtworkDetailController`
- Muestra estadisticas detalladas de una obra:
  - Records por cada pieceCount (64, 144, 256, 512)
  - Mejor marco obtenido
  - Fecha de primera completion
- Metodos:
  - `void ShowDetail(string artworkId)`
  - `void OnReplay(int pieceCount)`: Rejugar con X piezas.

### 4.16. `PlacedArtworkController`
- Controla un cuadro ya colgado en la casa.
- Campos:
  - `PlacedArtwork data`
  - `bool isBeingMoved`
- Metodos:
  - `void OnGrab()`: Inicia reubicacion.
  - `void OnRelease()`: Confirma nueva posicion o cancela.
  - `void OnRetire()`: Retira el cuadro y lo guarda en galeria.
  - `void OnDelete()`: Elimina el cuadro permanentemente.
- Interaccion: Pellizco largo para activar menu contextual.

### 4.17. `PauseMenuController`
- Campos:
  - `bool isPaused`
  - `PuzzleSessionData currentSession`
- Metodos:
  - `void Pause()`: Pausa el puzzle, muestra menu.
  - `void Resume()`: Continua el puzzle.
  - `void Quit()`: Abandona puzzle (confirmar).
  - `void ToggleHelp()`: Cambia modo ayuda mid-game.
- UI: Botones Resume, Toggle Ayuda, Salir.

### 4.18. `OnboardingController`
- Controla el tutorial de primera vez.
- Campos:
  - `int currentStep`
  - `bool isComplete`
- Metodos:
  - `void StartOnboarding()`: Inicia tutorial.
  - `void NextStep()`: Avanza al siguiente paso.
  - `void Skip()`: Salta tutorial.
  - `void Complete()`: Marca como completado.
- Pasos del tutorial:
  1. Bienvenida y concepto del juego.
  2. Seleccionar modo (Galeria/Confort).
  3. Detectar/posicionar lienzo.
  4. Tomar una pieza (gesto pinza).
  5. Colocar pieza en el lienzo.
  6. Usar el carrusel (gesto scroll).
  7. Completar mini-puzzle de practica (4 piezas).

### 4.19. `SettingsController`
- Campos:
  - `GameSettings settings`
- Metodos:
  - `void ShowSettings()`: Muestra panel.
  - `void SetHelpModeDefault(bool enabled)`
  - `void SetSFXVolume(float volume)`
  - `void SetMusicVolume(float volume)`
  - `void ResetOnboarding()`: Permite ver tutorial de nuevo.
  - `void SaveSettings()`
- Persistencia en `SaveDataService`.

### 4.20. `HapticController`
- Controla vibracion de manos usando Meta SDK.
- Metodos:
  - `void PlayGrabHaptic(Hand hand)`: Vibracion corta al tomar.
  - `void PlayPlaceHaptic(Hand hand)`: Vibracion de confirmacion.
  - `void PlayErrorHaptic(Hand hand)`: Vibracion de error.
  - `void PlaySuccessHaptic(Hand hand)`: Vibracion de exito.
- Usa `OVRInput.SetControllerVibration` o equivalente OpenXR.

### 4.21. `WallHighlightController`
- Visualiza paredes detectadas para seleccion.
- Campos:
  - `Material highlightMaterial`
  - `List<GameObject> highlightedWalls`
- Metodos:
  - `void HighlightWalls(List<Plane> walls)`: Muestra overlay en paredes.
  - `void SetSelectedWall(Plane wall)`: Destaca pared seleccionada.
  - `void ClearHighlights()`: Limpia visualizacion.

### 4.22. `FrameAnimationController`
- Controla animacion del marco al completar puzzle.
- Metodos:
  - `IEnumerator PlayFrameReveal(FrameTier tier, Transform target)`
  - Secuencia:
    1. Fade in de las 4 barras del marco.
    2. Brillo/particulas segun tier.
    3. Sonido de revelacion.

### 4.23. `PieceMorphologyGenerator`
- Genera morfologias validas para todas las piezas del puzzle.
- Metodos:
  - `static PieceMorphology[] GenerateGrid(int columns, int rows)`:
    ```csharp
    // Algoritmo:
    // 1. Crear grid de morfologias vacio
    // 2. Para cada pieza (i, j):
    //    - Borde superior: Flat si j==0, sino opuesto del vecino de arriba
    //    - Borde izquierdo: Flat si i==0, sino opuesto del vecino izquierdo
    //    - Borde derecho: Flat si i==columns-1, sino Random(Positive, Negative)
    //    - Borde inferior: Flat si j==rows-1, sino Random(Positive, Negative)
    // 3. Retornar array de morfologias
    ```
  - `static PieceEdgeState GetOpposite(PieceEdgeState state)`:
    - Positive -> Negative
    - Negative -> Positive
    - Flat -> Flat

### 4.24. `PieceShuffler`
- Aleatoriza el orden de las piezas para el carrusel.
- Metodos:
  - `static List<PuzzlePiece> Shuffle(List<PuzzlePiece> pieces)`:
    - Usa Fisher-Yates shuffle.
  - `static List<PuzzlePiece> ShuffleWithSeed(List<PuzzlePiece> pieces, int seed)`:
    - Shuffle determinista para reproducibilidad.

### 4.25. `PuzzleTimerController`
- Controla el cronometro del puzzle.
- Campos:
  - `float elapsedTime`
  - `bool isRunning`
- Metodos:
  - `void StartTimer()`
  - `void PauseTimer()`
  - `void ResumeTimer()`
  - `void StopTimer()`
  - `int GetElapsedSeconds()`
- Eventos: `OnTimerUpdate(float time)`

### 4.11. `ScoringController`
- Campos:
  - `Dictionary<int, float> difficultyMultipliers` = {64: 1.0f, 144: 1.5f, 256: 2.0f, 512: 3.0f}
  - `float helpPenaltyMultiplier = 0.5f`
  - `int[] frameThresholds` = {0, 50, 100, 200, 300} // Madera, Bronce, Plata, Oro, Ebano
- Metodos:
  - `int CalculateScore(int timeSec, int pieceCount, bool helpMode)`:
    ```csharp
    int baseScore = (pieceCount * 100) / Mathf.Max(timeSec, 1);
    float multiplier = difficultyMultipliers[pieceCount];
    float helpFactor = helpMode ? helpPenaltyMultiplier : 1.0f;
    return Mathf.FloorToInt(baseScore * multiplier * helpFactor);
    ```
  - `FrameTier GetFrameTier(int score, bool helpMode, int pieceCount)`:
    ```csharp
    if (score >= 300 && !helpMode && pieceCount >= 256) return FrameTier.Ebano;
    if (score >= 200 && !helpMode) return FrameTier.Oro;
    if (score >= 100) return FrameTier.Plata;
    if (score >= 50) return FrameTier.Bronce;
    return FrameTier.Madera;
    ```

## 5. Servicios (C# puros)
### 5.1. `WeeklyUnlockService`
- `string GetWeeklyArtworkId(DateTime now)`
- Regla: basado en semana del anio y lista local.

### 5.2. `LocalCatalogService`
- Carga `ArtworkDefinition` desde recursos locales.
- `ArtworkDefinition GetById(string id)`
- `List<ArtworkDefinition> GetAll()`

### 5.3. `LocalTelemetryService`
- `void LogEvent(string name, Dictionary<string, object> data)`
- Guardado local en archivo de texto para QA.

### 5.4. `SaveDataService`
- `SaveData Load()`
- `void Save(SaveData data)`
- `void SaveProgress(string artworkId, ArtworkRecord record)`
- `void AddPlacedArtwork(PlacedArtwork artwork)`
- `void UpdatePlacedArtwork(PlacedArtwork artwork)`
- `void RemovePlacedArtwork(string artworkId)`
- `void SaveSettings(GameSettings settings)`
- `void MarkOnboardingComplete()`
- `List<PlacedArtwork> GetActivePlacedArtworks()`: Solo isActive==true.
- `List<PlacedArtwork> GetRetiredArtworks()`: Solo isActive==false.
- Ubicacion: `Application.persistentDataPath/save.json`.

## 6. Flujos detallados (C#)
### 6.1. Seleccion de obra
1) `GameBootstrap` inicializa servicios.
2) Usuario accede directamente a la lista de obras (sin selector de modo previo).
3) La preferencia de "Ayuda" se lee de `SaveData`.

### 6.2. Inicio de Puzzle (Flujo Unificado)
1) `ComfortModeController` es el controlador predeterminado.
2) `ComfortModeController.PositionCanvas()` calcula posicion ergonomica.
3) Se muestra preview del lienzo flotante.
4) Usuario confirma, `ComfortModeController.LockPosition()`.
5) `WeeklyUnlockService` determina el cuadro semanal.
6) `LocalCatalogService` devuelve lista local.

### 6.3. (Seccion eliminada - Inicio Modo Galeria removido)
El inicio en pared ya no existe. La pared solo se usa al finalizar.

### 6.4. Seleccion de obra
1) UI lista obras locales + semanal.
2) Usuario selecciona obra y numero de piezas.
3) `PuzzleBoard.Initialize(def, pieceCount)`.
4) `PieceMeshGenerator` genera meshes para cada pieza.
5) Piezas se agregan al carrusel (`PieceScrollController`).

### 6.5. Colocacion y snap
1) `HandTrackingInputController` detecta pinch en rango 1 cm.
2) `PuzzlePiece.SetState(Grabbed)`, pieza sigue la mano.
3) Al soltar, `PuzzleBoard.TrySnapPiece`:
   - Si fuera del marco:
     - `PuzzlePiece.SetState(Returning)`
     - Animacion de retorno al carrusel.
     - `PuzzlePiece.SetState(InPool)`
   - Si dentro del marco:
     - Valida distancia a celda mas cercana.
     - Si ayuda ON y distancia <= 3 cm: snap magnetico.
     - Si ayuda OFF: solo snap si posicion muy cercana.
     - Valida encaje triangular con vecinos (`PieceMorphology.IsCompatibleWith`).
     - `PuzzlePiece.SetState(Placed)`
4) En Modo Ayuda:
   - Brillo rojo si no embona con vecinos.
   - Destello/haptic solo si `piece.IsInCorrectSlot(slotIndex)`.

### 6.6. Finalizacion
1) `PuzzleBoard.IsComplete` retorna true (todas las piezas Placed).
2) `PuzzleTimerController.StopTimer()`.
3) `ScoringController.CalculateScore(timeSec, pieceCount, helpMode)`.
4) `ScoringController.GetFrameTier(score, helpMode, pieceCount)`.
5) `FrameAnimationController.PlayFrameReveal(tier, canvasTransform)`.
6) `PostGameController.ShowResults(sessionData, score, frame)`.

### 6.7. Pantalla Post-Juego
1) `PostGameController` muestra estadisticas:
   - Tiempo total
   - Piezas colocadas
   - Puntuacion final
   - Marco obtenido (con visual)
   - Comparacion con record anterior (si existe)
2) Usuario elige:
   - "Colocar Cuadro" -> Flujo 6.8
   - "Volver al Menu" -> Menu principal
   - "Rejugar" -> Reinicia mismo puzzle

### 6.8. Colocacion de cuadro completado
1) `PostGameController.OnPlaceArtwork()` activa modo colocacion.
2) Cuadro se vuelve movible (sigue la mano).
3) `PostGameController.OnPlaceArtwork()` activa modo colocacion.
4) `WallHighlightController` muestra paredes disponibles.
5) Usuario selecciona pared.
6) Se guarda el anclaje.
5) Al soltar:
   - `AnchorPersistenceController.SaveAnchor(artworkId, transform)`.
   - `PlacedArtwork` se crea y guarda en `SaveData`.
   - Confirmacion visual/sonora.

### 6.9. Restauracion de cuadros al inicio
1) `GameBootstrap` llama `AnchorPersistenceController.RestoreAnchors()`.
2) Para cada `PlacedArtwork` guardado:
   - Intenta cargar anclaje espacial.
   - Si exito: Instancia `PlacedArtworkController` en posicion.
   - Si falla: Agrega a lista de fallos.
3) Si hay fallos:
   - Muestra mensaje: "Algunos cuadros no pudieron restaurarse".
   - Opcion: "Reubicar manualmente" o "Retirar a galeria".

### 6.10. Galeria Personal
1) Usuario abre Galeria desde menu principal.
2) `GalleryPanelController.ShowGallery()` muestra tabs:
   - **Completadas**: Obras terminadas (jugables de nuevo).
   - **Colgadas**: Cuadros activos en la casa.
   - **Retiradas**: Cuadros guardados (no colgados).
3) Al seleccionar obra completada:
   - `ArtworkDetailController.ShowDetail(artworkId)`.
   - Muestra records por cada pieceCount.
   - Boton "Rejugar" con selector de piezas.
4) Al seleccionar cuadro colgado:
   - Opciones: "Ir a ubicacion", "Reubicar", "Retirar".
5) Al seleccionar cuadro retirado:
   - Opciones: "Colgar de nuevo", "Eliminar".

### 6.11. Reubicar cuadro colgado
1) Usuario selecciona "Reubicar" en galeria o interactua con cuadro.
2) `PlacedArtworkController.OnGrab()` activa modo movimiento.
3) Cuadro sigue la mano del usuario.
4) Al soltar:
   - Valida nueva posicion (debe ser en superficie valida).
   - `AnchorPersistenceController.UpdateAnchor(artworkId, newTransform)`.
   - Actualiza `PlacedArtwork.position/rotation`.

### 6.12. Retirar cuadro
1) Usuario selecciona "Retirar" en menu del cuadro.
2) Confirmacion: "¿Guardar en galeria?".
3) Si confirma:
   - `PlacedArtwork.isActive = false`.
   - `AnchorPersistenceController.RemoveAnchor(artworkId)`.
   - Cuadro desaparece con animacion.
   - Cuadro aparece en tab "Retiradas" de galeria.

### 6.13. Rejugar obra
1) Usuario selecciona "Rejugar" desde detalle de obra.
2) `PieceCountSelectorController.ShowSelector()`.
3) Usuario elige numero de piezas.
4) Continua flujo normal desde 6.2 o 6.3 (segun modo guardado).

### 6.14. Primera vez (Onboarding)
1) `GameBootstrap` detecta `SaveData.showOnboarding == true`.
2) `OnboardingController.StartOnboarding()`.
3) Pasos guiados:
   - Bienvenida con concepto del juego.
   - Seleccionar modo (forzado a Confort para tutorial).
   - Posicionar lienzo de practica.
   - Tutorial de gesto pinza (tomar pieza destacada).
   - Tutorial de colocar pieza (objetivo iluminado).
   - Tutorial de scroll en carrusel.
   - Mini-puzzle de 4 piezas para practicar.
4) Al completar:
   - `SaveData.showOnboarding = false`.
   - Transicion a menu principal.

### 6.15. Pausa durante puzzle
1) Usuario hace gesto de pausa (palma abierta mirando a la cara, por ejemplo).
2) `PauseMenuController.Pause()`:
   - `PuzzleTimerController.PauseTimer()`.
   - Muestra menu de pausa.
3) Opciones:
   - "Continuar" -> `Resume()`, timer continua.
   - "Modo Ayuda" -> Toggle on/off.
   - "Salir" -> Confirma, pierde progreso, vuelve a menu.

### 6.16. Seleccion de obra (detallado)
1) `ArtworkListController` muestra lista con secciones:
   - **Cuadro Semanal**: Destacado con borde especial, badge "SEMANAL".
   - **Cuadros Locales**: Grid de obras disponibles.
   - **Galeria Personal**: Acceso rapido a rejugar.
2) Cada item muestra:
   - Preview de la obra.
   - Titulo y autor.
   - Indicador si ya fue completada (checkmark + mejor marco).
3) Al seleccionar:
   - `PieceCountSelectorController.ShowSelector()`.
   - Usuario elige 64/144/256/512.
   - Continua al flujo de inicio de puzzle.

## 7. Persistencia local (detallado)
- Archivo: `save.json` en `persistentDataPath`.
- Estructura del archivo:
  ```json
  {
    "progressByArtwork": {
      "mona_lisa": {
        "recordsByPieceCount": {
          "64": {"bestTimeSec": 120, "bestScore": 150, "bestFrameTier": 2},
          "144": {"bestTimeSec": 300, "bestScore": 180, "bestFrameTier": 3}
        },
        "bestFrameTier": 3,
        "firstCompletedAt": "2025-06-15T10:30:00Z",
        "totalCompletions": 5
      }
    },
    "completedArtworks": ["mona_lisa", "starry_night"],
    "placedArtworks": [
      {
        "artworkId": "mona_lisa",
        "anchorId": "anchor_123",
        "frameTier": 3,
        "position": {"x": 1.5, "y": 1.6, "z": 0},
        "rotation": {"x": 0, "y": 0, "z": 0, "w": 1},
        "isActive": true
      }
    ],
    "settings": {
      "helpModeDefault": true,
      "sfxVolume": 0.7,
      "musicVolume": 0.5,
      "showOnboarding": false
    },
    "lastArtworkId": "mona_lisa",
    "lastGameMode": 0,
    "onboardingCompleted": true,
    "firstPlayDate": "2025-06-01T08:00:00Z"
  }
  ```
- Serializacion: `Newtonsoft.Json` (JsonUtility no soporta Dictionary).
- Backup: Guardar copia `.bak` antes de cada escritura.

## 8. Validaciones y reglas
- No permitir soltar piezas fuera del marco (retornan al carrusel).
- Snap magnetico solo en ayuda ON.
- Oro/Ebano solo con ayuda OFF.
- Tiempo minimo = 1 segundo (evitar division por cero).
- Cuadro semanal: Solo 1 activo por semana (basado en semana ISO del año).
- Limite de cuadros colgados: Maximo 20 activos (por rendimiento de anclajes).
- Records: Solo se actualiza si es mejor que el anterior.
- Onboarding: Se muestra solo la primera vez (o si usuario lo reactiva).
- Pausa: No disponible en los ultimos 30 segundos del puzzle (anti-cheat).

## 9. Eventos del sistema
Lista de eventos para comunicacion entre componentes:
- `OnBootstrapReady`: GameBootstrap listo.
- `OnModeSelected(GameMode)`: Usuario eligio modo.
- `OnWallConfirmed(Plane)`: Pared seleccionada.
- `OnCanvasPositioned(Transform)`: Lienzo en posicion.
- `OnArtworkSelected(ArtworkDefinition, int pieceCount)`: Obra elegida.
- `OnPuzzleStarted(PuzzleSessionData)`: Puzzle iniciado.
- `OnPieceGrabbed(PuzzlePiece)`: Pieza tomada.
- `OnPieceReleased(PuzzlePiece)`: Pieza soltada.
- `OnPiecePlaced(PuzzlePiece, int slotIndex)`: Pieza colocada.
- `OnPieceReturned(PuzzlePiece)`: Pieza volvio al carrusel.
- `OnPuzzleCompleted(PuzzleSessionData, int score, FrameTier)`: Puzzle completado.
- `OnArtworkPlaced(PlacedArtwork)`: Cuadro colgado.
- `OnArtworkRelocated(PlacedArtwork)`: Cuadro movido.
- `OnArtworkRetired(PlacedArtwork)`: Cuadro retirado.
- `OnSettingsChanged(GameSettings)`: Configuracion cambiada.
- `OnTimerUpdate(float elapsed)`: Actualizacion de tiempo.
- `OnPauseToggled(bool isPaused)`: Pausa activada/desactivada.
