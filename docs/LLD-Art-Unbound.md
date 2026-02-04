# Low Level Design (LLD): Art Unbound
- Version: 0.1
- Fecha: 8 de junio de 2025
- Autor(es): [Tu Nombre] y Gemini AI
- Plataforma: Meta Quest 3 y Meta Quest Pro

## 1. Objetivo
Documento de diseno detallado para implementar el juego en Unity, incluyendo clases, componentes, datos y flujos.

## 2. Estructura de Carpetas (propuesta)
- `Assets/ArtUnbound/Scripts`
  - `Core/` (bootstrap, scene flow)
  - `MR/` (paredes, anclajes, scene understanding)
  - `Gameplay/` (puzzle, piezas, snapping)
  - `UI/` (menus, galeria, HUD)
  - `Data/` (ScriptableObjects, DTOs)
  - `Services/` (catalogo local, telemetria local, Addressables)
- `Assets/ArtUnbound/Artworks/` (texturas y metadatos locales)
- `Assets/ArtUnbound/Prefabs/`
- `Assets/ArtUnbound/Materials/`
- `Assets/ArtUnbound/Audio/`

## 3. Datos y ScriptableObjects
### 3.1. `ArtworkDefinition` (ScriptableObject)
- `string artworkId`
- `string title`
- `string author`
- `int year`
- `float aspectRatio`
- `Vector2 baseSizePortrait` (alto=0.70, ancho calculado)
- `Vector2 baseSizeLandscape` (alto=0.50, ancho calculado)
- `Texture2D previewTexture`
- `AssetReference puzzleTextureRef` (Addressables locales)
- `AssetReference metadataRef` (Addressables locales)

### 3.2. `PuzzleConfig` (ScriptableObject)
- `int[] pieceCounts` (64, 144, 256, 512)
- `float snapDistanceCm` (3.0)
- `float pinchRangeCm` (1.0)
- `float pieceSizeCm` (5.0)
- `float pieceThicknessCm` (0.5)
- `bool helpModeDefault`
- `bool useGridSnapping`
- `bool useTriangularMorphology`

### 3.3. `FrameConfig` (ScriptableObject)
- `FrameTier tier` (madera, bronce, plata, oro, ebano)
- `Material frameMaterial`
- `int scoreThreshold`
- `bool requiresNoHelp` (true para oro/ebano)

## 4. Componentes Principales (MonoBehaviours)
### 4.1. `GameBootstrap`
- Inicializa servicios (catalogo local, desbloqueo semanal, Addressables, telemetria local).
- Carga escena inicial y verifica permisos MR.

### 4.2. `WallSelectionController`
- Detecta paredes y coloca el marco de lienzo.
- Se utiliza **exclusivamente en el flujo Post-Juego** para colgar obras terminadas.

### 4.3. `CanvasFrameController`
- Controla el marco de lineas.
- Aplica size base (portrait/landscape).
- Valida limites de tamaño.

### 4.3.1. `ComfortModeController`
- Posiciona el lienzo flotante frente al usuario.
- Es el controlador **por defecto** para el inicio de cualquier puzzle.
- Campos:
  - `float distanceFromHead = 0.8f` (metros)
  - `float tiltAngle = 15f` (grados hacia el usuario)
  - `Transform headTransform` (referencia a la camara/cabeza)
- Metodos:
  - `void PositionCanvas()`: Calcula posicion inicial basada en pose de cabeza.
  - `void LockPosition()`: Fija el lienzo en su posicion actual.
  - `Vector3 GetErgonomicPosition()`: Retorna posicion optima.

### 4.4. `PuzzleBoard`
- Genera grilla de piezas y posiciones objetivo (grid snapping).
- Valida colocacion (snap/validacion) y coincidencia morfologica con vecinos.
- Notifica progreso y completion.

### 4.5. `PuzzlePiece`
- Representa pieza 3D con collider.
- Permite tomar con pinza y soltar.
- Mantiene estados de borde (plano, positivo, negativo).
- Dispara eventos al snap correcto.
- Estados de pieza:
  - `InPool`: En el carrusel, disponible.
  - `Grabbed`: Siendo manipulada.
  - `Placed`: Colocada en el tablero.
  - `Returning`: Animandose de vuelta al carrusel.

### 4.5.1. `PieceMeshGenerator`
- Genera meshes procedurales para piezas con morfologia triangular.
- Campos estaticos:
  - `float triangleSize = 0.015f` (1.5cm)
  - `float pieceSize = 0.05f` (5cm)
  - `float pieceThickness = 0.005f` (0.5cm)
- Metodos:
  - `Mesh GeneratePieceMesh(PieceMorphology morphology)`: Crea mesh completo.
  - `void AddTriangleExtrusion(List<Vector3> verts, List<int> tris, EdgeSide side, PieceEdgeState state)`: Agrega triangulo positivo/negativo.
  - `Vector2[] CalculateUVs(int pieceIndex, int totalPieces, float aspectRatio)`: Calcula UVs para la textura.
- Algoritmo:
  1. Crear base cuadrada (5x5cm).
  2. Por cada lado, segun estado:
     - Plano: sin modificacion.
     - Positivo: extruir triangulo hacia afuera.
     - Negativo: crear hueco triangular.
  3. Extruir todo 0.5cm para dar grosor.
  4. Calcular normales y UVs.

### 4.6. `HandTrackingInputController`
- Detecta gestos: pellizco y deslizamiento.
- Rutea eventos a `PuzzlePiece` y `PieceScrollController`.

### 4.7. `HelpModeController`
- Activa/desactiva feedback extra.
- Dispara haptic/FX/sonido al acierto correcto en Modo Ayuda.
- Emite brillo rojo sutil cuando no embona con vecinos en Modo Ayuda.

### 4.8. `PieceScrollController`
- Administra carrusel de piezas.
- Soporta gesto de deslizamiento horizontal.

### 4.9. `GalleryController`
- Lista obras completadas y marcos.
- Permite invocar cuadros para colocarlos.

### 4.10. `AnchorPersistenceController`
- Guarda `lastPlacementId`.
- Restaura anclajes en inicio.
- Maneja fallo: muestra mensaje y solicita re-colocar.

### 4.11. `ScoringController`
- Calcula score y frame tier.
- Penaliza si `helpMode` activo.
- Campos:
  - `Dictionary<int, float> difficultyMultipliers` = {64: 1.0, 144: 1.5, 256: 2.0, 512: 3.0}
  - `float helpPenalty = 0.5f`
- Metodos:
  - `int CalculateScore(int timeSec, int pieceCount, bool helpMode)`
  - `FrameTier GetFrameTier(int score, bool helpMode, int pieceCount)`

### 4.12. Componentes de UI
- `PieceCountSelectorController`: Selector de dificultad (64/144/256/512).
- `PostGameController`: Pantalla de resultados post-puzzle.
- `GalleryPanelController`: Panel de galeria personal con tabs.
- `ArtworkDetailController`: Detalle de obra con records.
- `PauseMenuController`: Menu de pausa durante puzzle.
- `SettingsController`: Configuracion del juego.
- `OnboardingController`: Tutorial de primera vez.
- `PuzzleHUDController`: HUD durante gameplay.
- `ArtworkListController`: Lista de obras disponibles.

### 4.13. Componentes de Feedback
- `HapticController`: Vibracion de manos via Meta SDK.
- `FrameAnimationController`: Animacion de revelacion del marco.
- `AudioManager`: Gestion de efectos de sonido.

### 4.14. Componentes de Gestion de Cuadros
- `PlacedArtworkController`: Cuadro colgado interactuable.
- `WallHighlightController`: Visualizacion de paredes detectadas.

### 4.15. Utilidades
- `PieceMorphologyGenerator`: Genera morfologias validas para el puzzle.
- `PieceShuffler`: Aleatoriza piezas para el carrusel.
- `PuzzleTimerController`: Cronometro del puzzle.

## 5. Servicios
### 5.1. `WeeklyUnlockService`
- Calcula el cuadro semanal basado en el reloj del sistema.

### 5.2. `LocalCatalogService`
- Resuelve metadata desde assets locales.
- Provee acceso a `ArtworkDefinition`.

### 5.3. `LocalTelemetryService`
- Registra eventos locales para QA (sin envio a backend).

### 5.4. `SaveDataService`
- Guarda preferencias y estado local en `persistentDataPath`.

### 5.5. Servicios Futuros (Evaluacion)
- `CloudStorageService`: descarga de cuadros individuales sin actualizar la app completa.
- `CloudSyncService`: respaldo de records y persistencia entre dispositivos.
- `RemoteConfigService`: eventos globales y ajustes de logica en tiempo real.

## 6. Flujos Detallados
### 6.1. Inicio de Puzzle (Flujo Unificado)
1) `GameBootstrap` inicializa servicios.
2) Usuario selecciona obra (o cuadro semanal).
3) **Posicionamiento**:
   - `ComfortModeController` calcula posicion ergonomica frente al usuario.
   - Se muestra preview del lienzo flotante.
   - Usuario confirma posicion.
   - `ComfortModeController.LockPosition()` fija el lienzo para comenzar el armado.
4) `PuzzleBoard` se inicializa y comienza el juego.

### 6.2. (Deprecado - Inicio Modo Galeria eliminado)
El inicio en pared se ha eliminado para reducir friccion. La pared solo se usa al finalizar.

### 6.4. Colocacion de pieza
1) `PuzzlePiece` es tomada por pinza (estado cambia a `Grabbed`).
2) Al soltar, `PuzzleBoard` valida distancia (3 cm).
3) Si esta dentro del marco y distancia valida:
   - Hace snap al centro de celda.
   - Estado cambia a `Placed`.
   - Actualiza progreso.
4) Si esta fuera del marco:
   - Estado cambia a `Returning`.
   - Pieza se anima de vuelta al carrusel.
   - Estado cambia a `InPool`.
5) Valida encaje triangular con vecinos.
6) En Modo Ayuda: brillo rojo si no embona; destello/haptic solo si es celda correcta.

### 6.5. Finalizacion y marco
1) `PuzzleBoard` completa todas las piezas.
2) `ScoringController` calcula frame tier y muestra resultados.
3) Usuario elige "Colgar Cuadro".
4) `WallSelectionController` se activa para detectar paredes.
5) Usuario selecciona pared y `AnchorPersistenceController` guarda la ubicacion del cuadro terminado.

## 7. Assets Principales
- Prefabs: `CanvasFrame`, `PuzzlePiece`, `PieceScroll`, `CompletedFrame`.
- Materiales: `Frame_Madera`, `Frame_Bronce`, `Frame_Plata`, `Frame_Oro`, `Frame_Ebano`.
- Audio: `PiecePlace`, `HelpConfirm`, `PuzzleComplete`.

## 8. Persistencia
- Local: preferencias, helpMode, historial reciente.
- Distribucion: catalogo y metadata incluidos en la app (sin descarga en runtime).
