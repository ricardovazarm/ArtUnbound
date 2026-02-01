# Plan de Implementación (Unity IDE)
- Proyecto base: Unity 6 (6000.0+) + OpenXR + Meta XR Core SDK
- Versión Unity Objetivo: **6000.3.1f1**
- Objetivo: Guía paso a paso "Click-by-Click" para la configuración de Art Unbound.

## 0. Configuración Inicial del Paquete (Unity Package Manager)
**Objetivo:** Instalar dependencias necesarias.
1. Abre Unity y tu proyecto.
2. Ve al menú superior: `Window` > `Package Manager`.
3. En la ventana **Package Manager**, localiza el menú desplegable en la esquina superior izquierda que dice **Packages: In Project** y cámbialo a **Packages: Unity Registry**.
4. Usa la barra de búsqueda (esquina superior derecha) para buscar e instalar (Check-box "Install" o botón "Install" abajo a la derecha) los siguientes paquetes uno por uno:
   - **OpenXR Plugin** (com.unity.xr.openxr).
   - **XR Interaction Toolkit** (com.unity.xr.interaction.toolkit).
   - **AR Foundation** (com.unity.xr.arfoundation) - *Versión 6.0 o superior recomendada para Unity 6*.
   - **Input System** (com.unity.inputsystem) - *Suele pedir reinicio del editor, acepta si lo pide*.
   - **Universal RP** (com.unity.render-pipelines.universal) - *Solo si no creaste el proyecto con la plantilla URP*.
5. (Para integración con Meta) Necesitas el paquete de Meta OpenXR.
   - Si no aparece en el Registry, ve a `Edit` > `Project Settings` > `Package Manager`.
   - Asegúrate de tener "Enable Pre-release Packages" activado si es necesario, o agrega el Scoped Registry de Meta si usas sus herramientas específicas (aunque el plugin oficial de Unity "OpenXR Plugin" suele ser suficiente para la base).
   - **Recomendación:** Instala el paquete **Meta XR Core SDK** desde el Asset Store o via Package Manager (Add package from git url: `com.meta.xr.sdk.core`) si requieres funcionalidades específicas de Meta que no están en el estándar OpenXR nativo. Para este plan, asumiremos el flujo estándar de **OpenXR Plugin** con el Feature Group de **Meta Quest**.

## 1. Preparación de Estructura de Proyecto
**Objetivo:** Organizar los archivos.
1. En la ventana **Project** (generalmente abajo), haz clic derecho sobre la carpeta `Assets` en la columna izquierda.
2. Selecciona `Create` > `Folder`. Nómbrala `ArtUnbound`.
3. Haz doble clic en `ArtUnbound` para entrar.
4. Repite el proceso `Create` > `Folder` para crear las siguientes carpetas dentro:
   - `Scenes`
   - `Prefabs`
   - `Materials`
   - `Artworks` (Aquí irán las texturas de los cuadros)
   - `Audio`
   - `UI`
   - `Scripts` (Si no existe, aunque el código generado ya debería estar aquí).

## 2. Creación y Guardado de la Escena Principal
**Objetivo:** Tener una escena limpia.
1. Ve al menú `File` > `New Scene`.
2. En la ventana emergente, selecciona **Basic (Built-in)** o **Basic (URP)** según tu render pipeline. Si dudas, elige **Empty Scene** para empezar desde cero.
3. Presiona `Create`.
4. Ve al menú `File` > `Save As...`.
5. Navega a `Assets/ArtUnbound/Scenes/`.
6. En "File name", escribe `Main`.
7. Presiona `Save`.

## 3. Configuración del Build Profile (Android)
**Objetivo:** Configurar para Quest 3.
1. Ve al menú `File` > `Build Profiles` (Nota: En Unity 6 esto reemplaza al antiguo Build Settings para gestión avanzada, pero el atajo `Ctrl+Shift+B` abre la ventana clásica "Build Settings" que ahora gestiona perfiles).
2. En la ventana **Build Profiles** (o Build Settings), en la columna "Platform", selecciona **Android**.
3. Haz clic en el botón **Switch Platform** (esquina inferior derecha). *Espera a que recompile los scripts*.
4. Asegúrate de que **Texture Compression** esté en `ASTC`.
5. En la lista superior "Scenes In Build", asegúrate de que esté vacía o solo tenga tu escena.
6. Haz clic en el botón `Add Open Scenes`. Verifica que aparezca `ArtUnbound/Scenes/Main` marcado con un check.

## 4. Configuración del Player (Player Settings)
**Objetivo:** Ajustes técnicos para VR.
1. En la ventana **Build Settings**, haz clic en el botón `Player Settings...` (esquina inferior izquierda).
2. Se abrirá **Project Settings**. Asegúrate de estar en la pestaña **Player** (columna izquierda) y en la pestaña del icono de **Android** (robot verde) en el panel derecho.
3. Despliega la sección **Other Settings**:
   - Busca **Rendering** > **Color Space**. Cámbialo a `Linear`.
   - Busca **Rendering** > **Auto Graphics API**. Desmárcalo. Asegúrate de que `OpenGLES3` o `Vulkan` estén en la lista (Vulkan es recomendado para Quest 3 + Unity 6, pero GLES3 es más estable si tienes problemas).
   - Busca **Configuration** > **Scripting Backend**. Cámbialo a `IL2CPP`.
   - Busca **Configuration** > **Target Architectures**. Marca **solamente** `ARM64`. Desmarca ARMv7.
4. Despliega la sección **Resolution and Presentation**:
   - **Default Orientation**: `Landscape Left`.
5. Cierra la sección Player pero no la ventana.

## 5. Configuración XR (XR Plug-in Management)
**Objetivo:** Activar OpenXR y características de Meta.
1. En la misma ventana **Project Settings**, ve a la pestaña **XR Plug-in Management** (columna izquierda, abajo).
2. En la pestaña de Android (icono robot), marca la casilla **OpenXR**. *Espera a que instale paquetes si faltan*.
3. Una vez marcado, aparecerá un signo de exclamación rojo o amarillo. Haz clic en él y luego en **Fix All** si aparecen advertencias de validación.
4. Debajo de "XR Plug-in Management" en la columna izquierda, haz clic en el sub-menú **OpenXR**.
5. En "Interaction Profiles", haz clic en el botón `+`. Agrega:
   - **Oculus Touch Controller Profile**.
   - **Meta Quest Touch Pro Controller Profile** (si usas Quest Pro/3).
   - **Eye Gaze Interaction Profile** (Opcional, útil para Quest Pro).
6. En "OpenXR Feature Groups" (sección Android), marca **Meta Quest Support**.
7. En la lista de "Features" que aparece, marca las siguientes casillas (es crucíal para el hardware integration):
   - [x] **Meta Quest Feature Group** (debe estar activo).
   - [x] **Hand Tracking Subsystem**: Permite usar las manos.
   - [x] **Meta Hand Tracking Aim**: Mejora el apuntado con manos.
   - [x] **Meta Quest: Camera (Passthrough)**: Permite ver el mundo real.
   - [x] **Meta Quest: Anchors**: Para guardar la posición de los cuadros.
   - [x] **Meta Quest: Planes**: Para detectar paredes.
   - [ ] **Meta Quest: Meshing**: (Déjalo desactivado a menos que necesites malla 3D de toda la habitación, para paredes basta 'Planes').

## 6. Configuración de la Escena (Scene Setup)
**Objetivo:** Crear la cámara XR y habilitar Passthrough.

### 6.1. XR Rig
1. En la ventana **Hierarchy** (izquierda), haz clic derecho en un espacio vacío.
2. Selecciona `XR` > `XR Origin (VR)`. (Esto creará un objeto "XR Origin" con una cámara dentro).
3. Elimina la "Main Camera" que venía por defecto en la escena (Click derecho > Delete), ya que XR Origin trae la suya.
4. Selecciona el objeto `XR Origin` en la jerarquía.
5. En el **Inspector**, busca el componente `XROrigin`. Asegúrate de que "Tracking Origin Mode" esté en `Floor`.

### 6.2 Configuración de Passthrough (AR Camera)
1. Despliega el objeto `XR Origin` > `Camera Offset`.
2. Selecciona el objeto hijo `Main Camera`.
3. En el **Inspector**, en el componente **Camera**:
   - **Clear Flags**: Cambia a `Solid Color`.
   - **Background**: Haz clic en el color negro. En la ventana de color, baja el canal **Alpha (A)** a 0. El color de fondo debe ser transparente.
4. En el **Inspector**, haz clic en `Add Component`.
5. Escribe `AR Camera Manager` y selecciónalo. (Necesario para Passthrough en AR Foundation 6).
6. Haz clic en `Add Component` nuevamente.
7. Escribe `AR Camera Background` y selecciónalo. *Nota: En Quest, el Passthrough suele sobreescribir el fondo automáticamente si el alpha es 0, pero este componente ayuda a la compatibilidad con AR Foundation estándar.*

### 6.3 AR Managers (Paredes y Anclajes)
1. Selecciona el objeto `XR Origin` en la jerarquía.
2. **AR Plane Manager (Paredes):**
   - Click `Add Component` > busca `AR Plane Manager`.
   - En el campo **Detection Mode**, abre el dropdown y selecciona **Vertical**. (Solo nos interesan paredes).
   - El campo **Plane Prefab** está vacío. Lo crearemos más tarde (Paso 11).
3. **AR Anchor Manager (Persistencia):**
   - Click `Add Component` > busca `AR Anchor Manager`.
4. **AR Raycast Manager (Interacción):**
   - Click `Add Component` > busca `AR Raycast Manager`.

## 7. Tags y Layers
**Objetivo:** Definir capas para física y renderizado.
1. Ve al menú superior derecha (en el Inspector): dropdown `Layers` > `Edit Layers...`.
2. En la sección **Layers**:
   - En el User Layer 6, escribe: `PuzzlePiece`.
   - En el User Layer 7, escribe: `PuzzleBoard`.
   - En el User Layer 8, escribe: `MRSurface`.
   - En el User Layer 9, escribe: `UI_World`.
3. No necesitamos Physics Matrix compleja por ahora, pero asegúrate de que haya colisión entre `PuzzlePiece` y `PuzzleBoard`.

## 8. Creación de Prefabs (Detallado)

### 8.1. CanvasFrame (El Marco)
1. En **Hierarchy**, click derecho > `Create Empty`. Nómbralo `CanvasFrame`.
2. Click derecho sobre `CanvasFrame` > `3D Object` > `Cube`. Nómbralo `Visual`.
3. Selecciona `Visual`. En el Inspector, transform scale: `X: 0.8, Y: 0.6, Z: 0.05`.
4. Selecciona `CanvasFrame` (padre). Click `Add Component` > `Box Collider`.
   - Click en el botón `Edit Collider` y ajusta el tamaño para que coincida con el visual (aprox `X: 0.8, Y: 0.6, Z: 0.05`).
5. En el dropdown **Layer** (arriba a la derecha), selecciona `PuzzleBoard`. Responde "Yes, change children" si pregunta.
6. Arrastra el objeto `CanvasFrame` desde la Jerarquía hacia la carpeta `Assets/ArtUnbound/Prefabs`.
7. Borra el objeto de la escena.

### 8.2. PuzzlePiece (La Pieza)
1. En **Hierarchy**, click derecho > `3D Object` > `Cube`. Nómbralo `PuzzlePiece`.
2. En el Inspector:
   - **Scale**: `X: 0.05, Y: 0.05, Z: 0.005` (5cm x 5cm x 5mm).
3. Asegúrate de que tenga `Box Collider` (viene por defecto).
4. `Add Component` > `Rigidbody`.
   - Desmarca **Use Gravity**.
   - Marca **Is Kinematic** (porque las moveremos con las manos o código).
5. Asigna **Layer**: `PuzzlePiece`.
6. `Add Component` > `Puzzle Piece` (Script). << **IMPORTANTE**
7. Arrastra a `Assets/ArtUnbound/Prefabs`.
7. Borra de la escena.

### 8.3. UI Prefabs (Ejemplos rápidos)
**Nota:** Prepara estos prefabs ahora, los scripts los pedirán más tarde.

1. **PieceScroll**:
   - `GameObject` > `UI` > `Panel`. Llámalo `PieceScroll`.
   - Inspector `Image`: Color Negro, Alpha 100.
   - `Add Component` > `Scroll Rect`.
   - Arrastra a `Assets/ArtUnbound/Prefabs`. Borra de escena.
2. **GalleryItem (Simple)**:
   - `GameObject` > `UI` > `Button - TextMeshPro`. Llámalo `GalleryItem`.
   - En el inspector, `Add Component` > `Gallery Item Controller` (El script generado).
   - Arrastra a `Assets/ArtUnbound/Prefabs`. Borra de escena.
3. **RecordItem (Simple)**:
   - `GameObject` > `UI` > `Panel`. Llámalo `RecordItem`.
   - **Configurar Tamaño:** En el Inspector > **Rect Transform**, haz clic en el icono de **Anchors** (cuadrado con líneas) y selecciona **Middle-Center** (o Top-Center). Ahora verás los campos `Width` y `Height`. Establece **Height** en `50`.
   - Click derecho en `RecordItem` > `UI` > `Text - TextMeshPro`. Llámalo `Title`.
   - Click derecho en `RecordItem` > `UI` > `Text - TextMeshPro`. Llámalo `Score`.
   - Arrastra `RecordItem` a `Assets/ArtUnbound/Prefabs`. Borra de escena.
4. **OnboardingDot (Simple)**:
   - `GameObject` > `UI` > `Image`. Llámalo `OnboardingDot`.
   - **Configurar Tamaño:** En el Inspector > **Rect Transform**, establece **Width** en `20` y **Height** en `20`.
   - Arrastra a `Assets/ArtUnbound/Prefabs`. Borra de escena.

### 8.4. Creación de Datos (ScriptableObjects)
**Objetivo:** Crear los archivos de configuración requeridos por el GameBootstrap.
1. En la ventana **Project**, ve a `Assets/ArtUnbound`. Crea una carpeta llamada `Data`.
2. Entra en `Data`.
3. Click derecho en el espacio vacío > `Create` > `ArtUnbound` > `Artwork Catalog`. Nómbralo `ArtworkCatalog`.
4. Click derecho > `Create` > `ArtUnbound` > `Puzzle Config`. Nómbralo `PuzzleConfig`.
5. Click derecho > `Create` > `ArtUnbound` > `Frame Config Set`. Nómbralo `FrameConfigSet`.

## 9. Inputs y Scripts (Wiring Inicial)
**Objetivo:** Conectar controladores lógicos básicos.

### 9.1. GameBootstrap (El cerebro)
1. En **Hierarchy**, click derecho > `Create Empty`. Nómbralo `GameBootstrap`.
2. En Inspector > `Add Component` > busca `Game Bootstrap` y añádelo.

### 9.2. Controladores de Hardware y MR
1. En **Hierarchy**, click derecho > `Create Empty`. Nómbralo `HardwareControllers`. (Este será el padre).

#### A) Wall Selection Controller
1. Click derecho sobre `HardwareControllers` > `Create Empty`. Nómbralo `WallSelection`.
2. En el Inspector, `Add Component` > `Wall Selection Controller`.
3. Arrastra el objeto `XR Origin` desde la Jerarquía al campo **Plane Manager**.
4. Arrastra el objeto `XR Origin` desde la Jerarquía al campo **Raycast Manager**.

#### B) Wall Highlight Controller
1. Click derecho sobre `HardwareControllers` > `Create Empty`. Nómbralo `WallHighlight`.
2. En el Inspector, `Add Component` > `Wall Highlight Controller`.
3. (Nota: Dejarás el campo de Material vacío por ahora, más tarde le asignas uno si creas un material transparente).

#### C) Hand Controller & Interaction
1. Click derecho sobre `HardwareControllers` > `Create Empty`. Nómbralo `HandController`.
2. En el Inspector, `Add Component` > `Hand Tracking Input Controller`.
3. En el **MISMO** objeto (`HandController`), haz click en `Add Component` > busca `Interaction Manager` (el script que recién creamos).
   - Arrastra el componente `Hand Tracking Input Controller` (que está arriba en el mismo inspector) al campo **Input Controller**.
   - (Opcional) Crea un `Layer` llamado "PuzzlePiece" y asígnalo al campo **Interactable Layer**.
   - (Opcional) Crea un LineRenderer y Arrastralo al campo **Ray Visualizer** para ver el puntero.

#### D) Anchor Controller
1. Click derecho sobre `HardwareControllers` > `Create Empty`. Nómbralo `AnchorController`.
2. En el Inspector, `Add Component` > `Anchor Persistence Controller`.
3. Arrastra el objeto `XR Origin` al campo **Anchor Manager**.

#### E) Comfort & Frames
1. Click derecho sobre `HardwareControllers` > `Create Empty`. Nómbralo `ComfortController`.
   - `Add Component` > `Comfort Mode Controller`.
   - Despliega `XR Origin` > `Camera Offset` y busca `Main Camera`. Arrastra `Main Camera` al campo **Head Transform**.
2. Click derecho sobre `HardwareControllers` > `Create Empty`. Nómbralo `CanvasFrameManager`.
   - `Add Component` > `Canvas Frame Controller`.

### 9.3. Controladores de Lógica (Gameplay)
1. En **Hierarchy**, click derecho > `Create Empty`. Nómbralo `GameplayControllers`.

#### A) PuzzleBoard
1. Click derecho sobre `GameplayControllers` > `Create Empty`. Nómbralo `PuzzleBoard`.
2. `Add Component` > `Puzzle Board`.
3. Click derecho sobre el objeto `PuzzleBoard` (en la jerarquía) > `Create Empty`. Nómbralo `SlotRoot`.
4. Selecciona de nuevo el objeto `PuzzleBoard`:
   - Arrastra `SlotRoot` al campo **Slot Root**.
   - Arrastra el prefab `PuzzlePiece` (que creaste en el paso 8.2) al campo **Piece Prefab**. 
   - **Creación de Bandeja de Piezas (Child):**
     1. Click derecho en `PuzzleBoard` > `UI` > `Canvas`. Nómbralo `PieceTray`.
     2. En el componente Canvas, cambia **Render Mode** a `World Space`.
     3. Ajusta su **Rect Transform**:
        - Pos Y: `-0.4` (para que quede debajo).
        - Width: `0.8`, Height: `0.2`.
        - Scale: `1, 1, 1` (o ajústalo según el tamaño de tu board, quizás 0.01 si el board es pequeño).
     4. `Add Component` > `Piece Scroll Controller` (Script).
     5. `Add Component` > `Scroll Rect` (opcional, si usas scroll nativo).
   - **Volviendo al PuzzleBoard (Root):**
     - Arrastra el objeto hijo `PieceTray` al campo **Scroll Controller**.
     - Arrastra el objeto `HardwareControllers/HandController` al campo **Input Controller**.
   - Arrastra el Asset `PuzzleConfig` al campo **Puzzle Config**.

#### B) Help Controller
1. Click derecho sobre `GameplayControllers` > `Create Empty`. Nómbralo `HelpController`.
2. `Add Component` > `Help Mode Controller`.
3. Ahora, selecciona de nuevo el objeto `PuzzleBoard`. Arrastra el objeto `HelpController` al campo **Help Mode Controller**.

#### C) Otros Controladores
1. Click derecho sobre `GameplayControllers` > `Create Empty`. Nómbralo `ScoringController`.
   - `Add Component` > `Scoring Controller`.
2. Click derecho sobre `GameplayControllers` > `Create Empty`. Nómbralo `TimerController`.
   - `Add Component` > `Puzzle Timer Controller`.
3. Click derecho sobre `GameplayControllers` > `Create Empty`. Nómbralo `HapticController`.
   - `Add Component` > `Haptic Controller`.
4. Click derecho sobre `GameplayControllers` > `Create Empty`. Nómbralo `AudioManager`.
   - `Add Component` > `Audio Manager`.
   - `Add Component` > `Audio Source`.
5. Click derecho sobre `GameplayControllers` > `Create Empty`. Nómbralo `FrameAnimation`.
   - `Add Component` > `Frame Animation Controller`.

## 10. Implementación de UI (Creación de Menús)
**Objetivo:** Crear el Canvas principal y asignar **todas** las referencias a los 9 paneles.

### 10.1. Canvas Principal
1. En **Hierarchy**, click derecho > `UI` > `Canvas`. Nómbralo `MainCanvas`.
2. En el Inspector de **Canvas**:
   - **Render Mode**: `World Space`.
   - **Event Camera**: Selecciona la **Main Camera** (del XR Origin).
3. En el **Inspector** de **Canvas Scaler**:
   - **Dynamic Pixels Per Unit**: 10.
   - **Reference Pixels Per Unit**: 100.
4. En **Rect Transform**:
   - **Pos**: `0, 1.2, 1.5`.
   - **Width**: `1280`, **Height**: `720`.
   - **Scale**: `0.001, 0.001, 0.001`.
5. Asegúrate de que exista un `EventSystem` en la escena (Unity lo crea con el Canvas). Si usas XR Interaction Toolkit, añade el componente `XR UI Input Module` al EventSystem y **remueve** el componente `Standalone Input Module` o `Input System UI Input Module` (el que aparezca). Solo debe quedar el `XR UI Input Module` activo.

### 10.2. Creación de Paneles
**Nota:** Crea todos los paneles como hijos de `MainCanvas`.

#### A) MainMenuPanel
1. Click derecho en `MainCanvas` > `UI` > `Panel`. Nómbralo `MainMenuPanel`.
2. `Add Component` > `Main Menu Controller`.
3. **Creación de Sub-Elementos (Menú Principal):**
   - Click derecho en `MainMenuPanel` > `UI` > `Text - TextMeshPro`. Nómbralo `TxtTitle`. (Escribe "Art Unbound").
   - Click derecho en `MainMenuPanel` > `UI` > `Button - TextMeshPro`. Nómbralo `BtnPlay`. (Texto "Jugar").
   - Click derecho en `MainMenuPanel` > `UI` > `Button - TextMeshPro`. Nómbralo `BtnGallery`. (Texto "Galería").
   - Click derecho en `MainMenuPanel` > `UI` > `Button - TextMeshPro`. Nómbralo `BtnSettings`. (Texto "Opciones").
   - Click derecho en `MainMenuPanel` > `UI` > `Text - TextMeshPro`. Nómbralo `TxtStats`. (Texto "Completados: 0").
4. **Creación de Sub-Elementos (Selección Modo):**
   - Click derecho en `MainMenuPanel` > `UI` > `Panel` (hazlo más pequeño). Nómbralo `GameModePanel`. Desactívalo inicialmente.
   - Click derecho en `GameModePanel` > `UI` > `Button - TextMeshPro`. Nómbralo `BtnGalleryMode`. (Texto "Modo Pared Real").
   - Click derecho en `GameModePanel` > `UI` > `Button - TextMeshPro`. Nómbralo `BtnComfortMode`. (Texto "Modo Flotante").
   - Click derecho en `GameModePanel` > `UI` > `Button - TextMeshPro`. Nómbralo `BtnCancel`. (Texto "Cancelar").
   - Click derecho en `GameModePanel` > `UI` > `Text - TextMeshPro`. Nómbralo `TxtModeDesc`. (Texto "Descripción...").
5. **Creación de Sub-Elementos (Weekly Highlight):**
   - Click derecho en `MainMenuPanel` > `UI` > `Panel`. Nómbralo `WeeklyHighlightPanel`. Desactívalo inicialmente (Uncheck "Active" en Inspector).
   - Click derecho en `WeeklyHighlightPanel` > `UI` > `Image`. Nómbralo `ImgArtwork`.
   - Click derecho en `WeeklyHighlightPanel` > `UI` > `Text - TextMeshPro`. Nómbralo `TxtWeeklyTitle`. (Texto "Título de la Obra").
   - Click derecho en `WeeklyHighlightPanel` > `UI` > `Text - TextMeshPro`. Nómbralo `TxtWeeklyArtist`. (Texto "Artista").
   - Click derecho en `WeeklyHighlightPanel` > `UI` > `Button - TextMeshPro`. Nómbralo `BtnWeeklyPlay`. (Texto "Jugar Destacado").
6. **Asignación de Referencias (Main Menu Controller Inspector):**
   - **Menu Panel**: Arrastra `MainMenuPanel`.
   - **Title Text**: Arrastra `TxtTitle`.
   - **Play Button**: Arrastra `BtnPlay`.
   - **Gallery Button**: Arrastra `BtnGallery`.
   - **Settings Button**: Arrastra `BtnSettings`.
   - **Completed Count Text**: Arrastra `TxtStats`.
   - **Game Mode Panel**: Arrastra `GameModePanel`.
   - **Gallery Mode Button**: Arrastra `BtnGalleryMode`.
   - **Comfort Mode Button**: Arrastra `BtnComfortMode`.
   - **Cancel Mode Button**: Arrastra `BtnCancel`.
   - **Gallery Mode Description**: Arrastra `TxtModeDesc`.
   - **Weekly Highlight Panel**: Arrastra `WeeklyHighlightPanel`.
   - **Weekly Artwork Image**: Arrastra `ImgArtwork`.
   - **Weekly Artwork Title**: Arrastra `TxtWeeklyTitle`.
   - **Weekly Artwork Artist**: Arrastra `TxtWeeklyArtist`.
   - **Play Weekly Button**: Arrastra `BtnWeeklyPlay`.

#### B) GalleryPanel
1. Click derecho en `MainCanvas` > `UI` > `Panel`. Nómbralo `GalleryPanel`.
2. `Add Component` > `Gallery Panel Controller`.
3. **Creación:**
   - Click derecho en `GalleryPanel` > `UI` > `Scroll View`. Nómbralo `ScrollView`.
   - Click derecho en `GalleryPanel` > `UI` > `Button - TextMeshPro`. Nómbralo `BtnCompletadas`. (Texto "Completadas").
   - Click derecho en `GalleryPanel` > `UI` > `Button - TextMeshPro`. Nómbralo `BtnColgadas`. (Texto "Colgadas").
   - Click derecho en `GalleryPanel` > `UI` > `Button - TextMeshPro`. Nómbralo `BtnGuardadas`. (Texto "Guardadas").
   - Click derecho en `GalleryPanel` > `UI` > `Button - TextMeshPro`. Nómbralo `BtnClose` (Texto "X" o "Cerrar").
   - Click derecho en `GalleryPanel` > `UI` > `Panel`. Nómbralo `EmptyState`.
   - Click derecho en `EmptyState` > `UI` > `Text - TextMeshPro`. Nómbralo `TxtEmpty`. (Texto "No hay obras aquí").
4. **Asignación:**
   - **Panel**: Arrastra `GalleryPanel`.
   - **Content Container**: Arrastra `ScrollView/Viewport/Content`.
   - **Artwork Item Prefab**: Arrastra el prefab `GalleryItem` desde la carpeta Project (Assets/ArtUnbound/Prefabs).
   - **Tab Completadas**: Arrastra `BtnCompletadas`.
   - **Tab Colgadas**: Arrastra `BtnColgadas`.
   - **Tab Guardadas**: Arrastra `BtnGuardadas`.
   - **Close Button**: Arrastra `BtnClose`.
   - **Empty State Object**: Arrastra `EmptyState`.
   - **Empty State Text**: Arrastra `TxtEmpty`.

#### C) ArtworkDetailPanel
1. Click derecho en `MainCanvas` > `UI` > `Panel`. Nómbralo `ArtworkDetailPanel`.
2. `Add Component` > `Artwork Detail Controller`.
3. **Creación:**
   - Click derecho en `ArtworkDetailPanel` > `UI` > `Image`. Nómbralo `ImgPreview`.
   - Click derecho en `ArtworkDetailPanel` > `UI` > `Text - TextMeshPro`. Nómbralo `TxtTitle`. (Texto "Título de la Obra").
   - Click derecho en `ArtworkDetailPanel` > `UI` > `Text - TextMeshPro`. Nómbralo `TxtArtist`. (Texto "Artista").
   - Click derecho en `ArtworkDetailPanel` > `UI` > `Text - TextMeshPro`. Nómbralo `TxtDesc`. (Texto "Descripción...").
   - Click derecho en `ArtworkDetailPanel` > `UI` > `Panel`. Nómbralo `RecordsContainer`.
   - Click derecho en `ArtworkDetailPanel` > `UI` > `Panel`. Nómbralo `BestRecordPanel`.
   - Click derecho en `BestRecordPanel` > `UI` > `Text - TextMeshPro`. Nómbralo `TxtBestScore`. (Texto "Score: 0").
   - Click derecho en `BestRecordPanel` > `UI` > `Text - TextMeshPro`. Nómbralo `TxtBestTime`. (Texto "Time: 00:00").
   - Click derecho en `ArtworkDetailPanel` > `UI` > `Button - TextMeshPro`. Nómbralo `BtnPlay`. (Texto "Jugar").
   - Click derecho en `ArtworkDetailPanel` > `UI` > `Button - TextMeshPro`. Nómbralo `BtnHang`. (Texto "Colgar").
   - Click derecho en `ArtworkDetailPanel` > `UI` > `Button - TextMeshPro`. Nómbralo `BtnBack`. (Texto "Volver").
4. **Asignación:**
   - **Panel**: Arrastra `ArtworkDetailPanel`.
   - **Artwork Image**: Arrastra `ImgPreview`.
   - **Title Text**: Arrastra `TxtTitle`.
   - **Artist Text**: Arrastra `TxtArtist`.
   - **Description Text**: Arrastra `TxtDesc`.
   - **Records Container**: Arrastra `RecordsContainer`.
   - **Record Item Prefab**: Arrastra el prefab `RecordItem` desde Project.
   - **Best Record Panel**: Arrastra `BestRecordPanel`.
   - **Best Score Text**: Arrastra `TxtBestScore`.
   - **Best Time Text**: Arrastra `TxtBestTime`.
   - **Play Button**: Arrastra `BtnPlay`.
   - **Hang Button**: Arrastra `BtnHang`.
   - **Back Button**: Arrastra `BtnBack`.

#### D) PieceCountSelector
1. Click derecho en `MainCanvas` > `UI` > `Panel`. Nómbralo `PieceCountSelector`.
2. `Add Component` > `Piece Count Selector Controller`.
3. **Creación:**
   - Click derecho en `PieceCountSelector` > `UI` > `Button - TextMeshPro`. Nómbralo `Btn64`. (Hijo Text: "Seleccionar").
   - Click derecho en `PieceCountSelector` > `UI` > `Button - TextMeshPro`. Nómbralo `Btn144`. (Hijo Text: "Seleccionar").
   - Click derecho en `PieceCountSelector` > `UI` > `Button - TextMeshPro`. Nómbralo `Btn256`. (Hijo Text: "Seleccionar").
   - Click derecho en `PieceCountSelector` > `UI` > `Button - TextMeshPro`. Nómbralo `Btn512`. (Hijo Text: "Seleccionar").
   - Click derecho en `PieceCountSelector` > `UI` > `Button - TextMeshPro`. Nómbralo `BtnCancel`. (Hijo Text: "Cancelar").
   - Click derecho en `PieceCountSelector` > `UI` > `Text - TextMeshPro`. Nómbralo `Txt64`. (Texto "Fácil").
   - Click derecho en `PieceCountSelector` > `UI` > `Text - TextMeshPro`. Nómbralo `Txt144`. (Texto "Normal").
   - Click derecho en `PieceCountSelector` > `UI` > `Text - TextMeshPro`. Nómbralo `Txt256`. (Texto "Difícil").
   - Click derecho en `PieceCountSelector` > `UI` > `Text - TextMeshPro`. Nómbralo `Txt512`. (Texto "Experto").
   - Click derecho en `PieceCountSelector` > `UI` > `Text - TextMeshPro`. Nómbralo `TxtTitle`. (Texto "Selecciona número de piezas").
4. **Asignación:**
   - **Panel**: Arrastra `PieceCountSelector`.
   - **Title Text**: Arrastra `TxtTitle`.
   - **Btn 64**: Arrastra `Btn64`.
   - **Btn 144**: Arrastra `Btn144`.
   - **Btn 256**: Arrastra `Btn256`.
   - **Btn 512**: Arrastra `Btn512`.
   - **Cancel Button**: Arrastra `BtnCancel`.
   - **Label 64**: Arrastra `Txt64`.
   - **Label 144**: Arrastra `Txt144`.
   - **Label 256**: Arrastra `Txt256`.
   - **Label 512**: Arrastra `Txt512`.

#### E) PuzzleHUD
1. Click derecho en `MainCanvas` > `UI` > `Panel`. Nómbralo `HUD`.
2. `Add Component` > `Puzzle HUD Controller`.
3. **Creación:**
   - Click derecho en `HUD` > `UI` > `Text - TextMeshPro`. Nómbralo `TxtTimer`. (Texto "00:00").
   - Click derecho en `HUD` > `UI` > `Text - TextMeshPro`. Nómbralo `TxtPieces`. (Texto "0%").
   - Click derecho en `HUD` > `UI` > `Slider`. Nómbralo `SldProgress`.
   - Click derecho en `HUD` > `UI` > `Button - TextMeshPro`. Nómbralo `BtnPause`. (Texto "||").
   - Click derecho en `HUD` > `UI` > `Toggle`. Nómbralo `TglHelp`.
   - Click derecho en `HUD` > `UI` > `Text - TextMeshPro`. Nómbralo `TxtHelpLabel`. (Texto "Modo Ayuda").
4. **Asignación:**
   - **Hud Panel**: Arrastra `HUD`.
   - **Timer Text**: Arrastra `TxtTimer`.
   - **Pieces Text**: Arrastra `TxtPieces`.
   - **Progress Slider**: Arrastra `SldProgress`.
   - **Pause Button**: Arrastra `BtnPause`.
   - **Help Mode Toggle**: Arrastra `TglHelp`.
   - **Help Mode Label**: Arrastra `TxtHelpLabel`.

#### F) PauseMenuPanel
1. Click derecho en `MainCanvas` > `UI` > `Panel`. Nómbralo `PauseMenuPanel`.
2. `Add Component` > `Pause Menu Controller`.
3. **Creación:**
   - Click derecho en `PauseMenuPanel` > `UI` > `Panel`. Nómbralo `PauseContent`.
   - Click derecho en `PauseContent` > `UI` > `Button - TextMeshPro`. Nómbralo `BtnResume`. (Texto "Reanudar").
   - Click derecho en `PauseContent` > `UI` > `Button - TextMeshPro`. Nómbralo `BtnQuit`. (Texto "Salir").
   - Click derecho en `PauseContent` > `UI` > `Text - TextMeshPro`. Nómbralo `TxtTime`. (Texto "00:00").
   - Click derecho en `PauseContent` > `UI` > `Text - TextMeshPro`. Nómbralo `TxtPieces`. (Texto "0/0").
   - Click derecho en `PauseMenuPanel` > `UI` > `Panel`. Nómbralo `ConfirmQuitPanel`. (Desactivado).
   - Click derecho en `ConfirmQuitPanel` > `UI` > `Button - TextMeshPro`. Nómbralo `BtnConfirm`. (Texto "Confirmar Salir").
   - Click derecho en `ConfirmQuitPanel` > `UI` > `Button - TextMeshPro`. Nómbralo `BtnCancelQuit`. (Texto "Cancelar").
4. **Asignación:**
   - **Pause Panel**: Arrastra `PauseContent`.
   - **Confirm Quit Panel**: Arrastra `ConfirmQuitPanel`.
   - **Resume Button**: Arrastra `BtnResume`.
   - **Quit Button**: Arrastra `BtnQuit`.
   - **Confirm Quit Button**: Arrastra `BtnConfirm`.
   - **Cancel Quit Button**: Arrastra `BtnCancelQuit`.
   - **Time Text**: Arrastra `TxtTime`.
   - **Pieces Text**: Arrastra `TxtPieces`.

#### G) PostGamePanel
1. Click derecho en `MainCanvas` > `UI` > `Panel`. Nómbralo `PostGamePanel`.
2. `Add Component` > `Post Game Controller`.
3. **Creación:**
   - Click derecho en `PostGamePanel` > `UI` > `Text - TextMeshPro`. Nómbralo `TxtTitle`. (Texto "¡Puzzle Completado!").
   - Click derecho en `PostGamePanel` > `UI` > `Text - TextMeshPro`. Nómbralo `TxtScore`. (Texto "Puntuación Final: 0").
   - Click derecho en `PostGamePanel` > `UI` > `Text - TextMeshPro`. Nómbralo `TxtTime`. (Texto "Tiempo Total: 00:00").
   - Click derecho en `PostGamePanel` > `UI` > `Text - TextMeshPro`. Nómbralo `TxtFrameName`. (Texto "Nombre del Marco").
   - Click derecho en `PostGamePanel` > `UI` > `Image`. Nómbralo `ImgResultPreview`.
   - Click derecho en `PostGamePanel` > `UI` > `Image`. Nómbralo `ImgFrameIcon`.
   - Click derecho en `PostGamePanel` > `UI` > `Text - TextMeshPro`. Nómbralo `NewRecordObj`. (Texto "¡NUEVO RÉCORD!").
   - Click derecho en `PostGamePanel` > `UI` > `Button - TextMeshPro`. Nómbralo `BtnPlace`. (Texto "Colocar en Pared").
   - Click derecho en `PostGamePanel` > `UI` > `Button - TextMeshPro`. Nómbralo `BtnReplay`. (Texto "Jugar de Nuevo").
   - Click derecho en `PostGamePanel` > `UI` > `Button - TextMeshPro`. Nómbralo `BtnMenu`. (Texto "Volver al Menú").
4. **Asignación:**
   - **Panel**: Arrastra `PostGamePanel`.
   - **Title Text**: Arrastra `TxtTitle`.
   - **Score Text**: Arrastra `TxtScore`.
   - **Time Text**: Arrastra `TxtTime`.
   - **Frame Tier Text**: Arrastra `TxtFrameName`.
   - **Artwork Preview**: Arrastra `ImgResultPreview`.
   - **Frame Icon**: Arrastra `ImgFrameIcon`.
   - **New Record Indicator**: Arrastra `NewRecordObj`.
   - **Place Button**: Arrastra `BtnPlace`.
   - **Replay Button**: Arrastra `BtnReplay`.
   - **Menu Button**: Arrastra `BtnMenu`.

#### H) OnboardingPanel
1. Click derecho en `MainCanvas` > `UI` > `Panel`. Nómbralo `OnboardingPanel`.
2. `Add Component` > `Onboarding Controller`.
3. **Creación:**
   - Click derecho en `OnboardingPanel` > `UI` > `Text - TextMeshPro`. Nómbralo `TxtStepTitle`. (Texto "Paso 1").
   - Click derecho en `OnboardingPanel` > `UI` > `Text - TextMeshPro`. Nómbralo `TxtStepDesc`. (Texto "Descripción del paso...").
   - Click derecho en `OnboardingPanel` > `UI` > `Image`. Nómbralo `ImgStep`.
   - Click derecho en `OnboardingPanel` > `UI` > `Button - TextMeshPro`. Nómbralo `BtnNext`. (Texto "Siguiente").
   - Click derecho en `OnboardingPanel` > `UI` > `Button - TextMeshPro`. Nómbralo `BtnPrev`. (Texto "Anterior").
   - Click derecho en `OnboardingPanel` > `UI` > `Button - TextMeshPro`. Nómbralo `BtnSkip`. (Texto "Saltar Tutorial").
   - Click derecho en `OnboardingPanel` > `Create Empty`. Nómbralo `DotsContainer`.
   - Click derecho en `OnboardingPanel` > `Create Empty`. Nómbralo `AnimContainer`.
4. **Asignación:**
   - **Panel**: Arrastra `OnboardingPanel`.
   - **Step Title Text**: Arrastra `TxtStepTitle`.
   - **Step Description Text**: Arrastra `TxtStepDesc`.
   - **Step Image**: Arrastra `ImgStep`.
   - **Next Button**: Arrastra `BtnNext`.
   - **Previous Button**: Arrastra `BtnPrev`.
   - **Skip Button**: Arrastra `BtnSkip`.
   - **Dots Container**: Arrastra `DotsContainer`.
   - **Dot Prefab**: Arrastra el prefab `OnboardingDot` (Assets/ArtUnbound/Prefabs).
   - **Step Animation Container**: Arrastra `AnimContainer`.

#### I) SettingsPanel
1. Click derecho en `MainCanvas` > `UI` > `Panel`. Nómbralo `SettingsPanel`.
2. `Add Component` > `Settings Controller`.
3. **Creación:**
   - Click derecho en `SettingsPanel` > `UI` > `Slider`. Nómbralo `SldMusic`.
   - Click derecho en `SettingsPanel` > `UI` > `Slider`. Nómbralo `SldSFX`.
   - Click derecho en `SettingsPanel` > `UI` > `Slider`. Nómbralo `SldSnapDist`.
   - Click derecho en `SettingsPanel` > `UI` > `Toggle`. Nómbralo `TglHelpDef`. (Hijo Label: "Activar Ayuda por Defecto").
   - Click derecho en `SettingsPanel` > `UI` > `Toggle`. Nómbralo `TglOnboard`. (Hijo Label: "Mostrar Tutorial").
   - Click derecho en `SettingsPanel` > `UI` > `Toggle`. Nómbralo `TglGrid`. (Hijo Label: "Mostrar Cuadrícula").
   - Click derecho en `SettingsPanel` > `UI` > `Toggle`. Nómbralo `TglHighContrast`. (Hijo Label: "Alto Contraste").
   - Click derecho en `SettingsPanel` > `UI` > `Dropdown - TextMeshPro`. Nómbralo `DrpPieceCount`.
   - Click derecho en `SettingsPanel` > `UI` > `Button - TextMeshPro`. Nómbralo `BtnClose`. (Texto "Cerrar").
   - Click derecho en `SettingsPanel` > `UI` > `Button - TextMeshPro`. Nómbralo `BtnReset`. (Texto "Restaurar Defaults").
4. **Asignación:**
   - **Panel**: Arrastra `SettingsPanel`.
   - **Music Volume Slider**: Arrastra `SldMusic`.
   - **Sfx Volume Slider**: Arrastra `SldSFX`.
   - **Help Mode Default Toggle**: Arrastra `TglHelpDef`.
   - **Show Onboarding Toggle**: Arrastra `TglOnboard`.
   - **Default Piece Count Dropdown**: Arrastra `DrpPieceCount`.
   - **Show Grid Toggle**: Arrastra `TglGrid`.
   - **High Contrast Toggle**: Arrastra `TglHighContrast`.
   - **Piece Snap Distance Slider**: Arrastra `SldSnapDist`.
   - **Close Button**: Arrastra `BtnClose`.
   - **Reset Button**: Arrastra `BtnReset`.

**(Nota: Una vez creados todos, desactiva todos los paneles EXCEPTO MainMenuPanel).**

## 11. Wiring Final en GameBootstrap
**Objetivo:** Conectar todo lo que acabamos de crear al cerebro central.

1. Selecciona el objeto `GameBootstrap` en la jerarquía.
2. **Sección Data Assets**: Arrastra los archivos creados en el paso 8.4 desde la carpeta `Assets/ArtUnbound/Data` a los campos del Inspector:
   - Campo **Artwork Catalog**: Arrastra el archivo `ArtworkCatalog`.
   - Campo **Puzzle Config**: Arrastra el archivo `PuzzleConfig`.
   - Campo **Frame Config Set**: Arrastra el archivo `FrameConfigSet`.
3. **Sección UI Controllers**: Arrastra los objetos **Paneles** creados en el paso 10 desde la Jerarquía a los campos del Inspector:
   - Campo **Main Menu Controller**: Arrastra `MainMenuPanel`.
   - Campo **Gallery Panel Controller**: Arrastra `GalleryPanel`.
   - Campo **Artwork Detail Controller**: Arrastra `ArtworkDetailPanel`.
   - Campo **Piece Count Selector**: Arrastra `PieceCountSelector`.
   - Campo **Puzzle HUD**: Arrastra `HUD`.
   - Campo **Pause Menu Controller**: Arrastra `PauseMenuPanel`.
   - Campo **Post Game Controller**: Arrastra `PostGamePanel`.
   - Campo **Onboarding Controller**: Arrastra `OnboardingPanel`.
   - Campo **Settings Controller**: Arrastra `SettingsPanel`.
3. **Sección Gameplay Controllers**: Arrastra desde `GameplayControllers`:
   - Campo **Puzzle Board**: Arrastra `PuzzleBoard`.
   - Campo **Scoring Controller**: Arrastra `ScoringController`.
   - Campo **Timer Controller**: Arrastra `TimerController`.
4. **Sección MR Controllers**: Arrastra desde `HardwareControllers`:
   - Campo **Wall Selection Controller**: Arrastra `WallSelection`.
   - Campo **Wall Highlight Controller**: Arrastra `WallHighlight`.
   - Campo **Comfort Mode Controller**: Arrastra `ComfortController`.
   - Campo **Canvas Frame Controller**: Arrastra `CanvasFrameManager`.
5. **Sección Feedback Controllers**: Arrastra desde `GameplayControllers`:
   - Campo **Audio Manager**: Arrastra `AudioManager`.
   - Campo **Haptic Controller**: Arrastra `HapticController`.
   - Campo **Frame Animation Controller**: Arrastra `FrameAnimation`.

## 12. Pasos Finales antes del Build
1. Ve a `File` > `Save` para guardar `Main.unity`.
2. Conecta tu Quest 3 por USB.
3. Ve a `File` > `Build and Run`.
   - **Nota:** Si aparece un aviso de "Missing Project ID" (ícono amarillo), es porque no has vinculado el proyecto a Unity Cloud. Es seguro presionar **Yes** para continuar con un build local.
4. Espera la compilación y despliegue.

## Verificación en Dispositivo
1. Al iniciar, deletías ver el mundo real (Passthrough) y el **Menú Principal** flotando frente a ti.
2. Hand Tracking debe funcionar para interactuar con los botones (si usas XR Hands + Interaction Toolkit configurado).
