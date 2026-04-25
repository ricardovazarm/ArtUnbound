# Plan: VR Mode con Galería Virtual

## Context

Art Unbound actualmente es 100% MR (passthrough). Se quiere agregar un Modo VR para:
- **Quest 2**: no tiene color passthrough → VR es el modo por defecto al entrar
- **Quest 3/Pro**: puede elegir VR desde un botón nuevo en la NavigationGallery existente; desde VR puede volver a MR con otro botón nuevo

En VR el usuario aparece en una galería virtual (piso, techo, paredes, luces). Puede armar puzzles igual que en MR, completarlos, y colgar los cuadros en las paredes de la galería usando el mismo flujo que MR (pinch/trigger sobre el cuadro → drag → soltar cerca de pared). La posición 3D de cada cuadro se guarda por galería. Hay un sistema de selección de galerías extensible (por ahora 1).

---

## Cambios en NavigationGallery (UI existente)

La NavigationGallery **conserva todos sus botones actuales sin cambios**. Solo se agregan botones nuevos:

| Botón nuevo | Dónde aparece | Condición | Acción |
|-------------|---------------|-----------|--------|
| **VR** | NavigationGallery (MR) | Solo Quest 3/Pro | `→ GameState.VRGallery` + guarda `preferVRMode = true` |
| **Galerías** | NavigationGallery (VR) | Siempre en VR | Abre `GallerySelectionController` |
| **MR** | NavigationGallery (VR) | Solo Quest 3/Pro | `→ VRModeController.DeactivateVRMode()` + `GameState.MainMenu` + guarda `preferVRMode = false` |

En Quest 2: NavigationGallery siempre muestra la versión VR (con "Galerías", sin "MR").

---

## Flujo de Colgar Cuadros en VR

**Mismo flujo que MR** — no hay botón especial en PostGame. El usuario:

1. Completa el puzzle → `PostGameController` se muestra (igual que ahora)
2. Cierra/minimiza el PostGame → el cuadro completado queda flotando en la galería
3. Pinch/trigger sobre el cuadro → `InteractionManager.HandlePinchStart()` lo detecta (ya funciona con controllers)
4. El cuadro sigue al controller mientras se arrastra
5. Usuario teleporta a la pared deseada (locomotión VR)
6. Al soltar cerca de una pared → `VRWallHangingController.TryPlaceOnWall()` detecta la pared virtual y cuelga el cuadro
7. `GalleryPersistenceService.SavePainting()` guarda la posición 3D

La diferencia con MR: la detección de pared usa **raycasting contra geometría de la galería** en lugar de `ARPlaneManager`. El `ArtworkHangingController` de MR se deja intacto; `VRWallHangingController` es el equivalente para VR.

---

## Arquitectura

### Nueva carpeta: `Assets/ArtUnbound/Scripts/VR/` (namespace `ArtUnbound.VR`)

| Clase | Propósito |
|-------|-----------|
| `VRModeController` | Singleton: activa/desactiva passthrough, habilita/deshabilita sistemas MR vs VR |
| `VRGalleryController` | Carga el prefab de galería activo, instancia cuadros guardados al entrar, gestiona switch de galería |
| `VRLocomotionController` | Thumbstick izquierdo → arco de teleport → release → teletransporte |
| `VRWallHangingController` | Raycast contra geometría de galería → detecta pared → coloca cuadro en VR |
| `GalleryPersistenceService` | Guarda y carga `GalleryPaintingData` por galería en `SaveData` |

### Nuevos scripts de UI: `Assets/ArtUnbound/Scripts/UI/` (carpeta existente)

| Clase | Propósito |
|-------|-----------|
| `NavigationGalleryVRButtonsController` | Componente adicional en el panel NavigationGallery existente; muestra/oculta los botones nuevos según modo y dispositivo |
| `GallerySelectionController` | Sub-panel que lista las galerías disponibles; genera una card por `GalleryDefinition` |

### Nuevos ScriptableObjects: `Assets/ArtUnbound/Data/`

```csharp
// GalleryDefinition.cs
[CreateAssetMenu(menuName = "ArtUnbound/Gallery Definition")]
public class GalleryDefinition : ScriptableObject {
    public string galleryId;           // "gallery_classic"
    public string displayName;         // "Galería Clásica"
    public Sprite thumbnail;
    public GameObject environmentPrefab;
}

// GalleryCatalog.cs
[CreateAssetMenu(menuName = "ArtUnbound/Gallery Catalog")]
public class GalleryCatalog : ScriptableObject {
    public List<GalleryDefinition> galleries;
    public string defaultGalleryId;
}
```

---

## Cambios en SaveData

**Archivo**: `Assets/ArtUnbound/Scripts/Data/SaveData.cs`

```csharp
// Agregar a la clase SaveData:
public bool preferVRMode = false;
public string lastGalleryId = "gallery_classic";
public Dictionary<string, List<GalleryPaintingData>> galleryPaintings = new();
```

**Clase nueva** (mismo archivo o `GalleryPaintingData.cs`):

```csharp
[Serializable]
public class GalleryPaintingData {
    public string artworkId;
    public int difficultyIndex;     // Para determinar el frame tier
    public float[] position;        // Vector3 serializable (x, y, z)
    public float[] rotation;        // Quaternion serializable (x, y, z, w)
}
```

`SaveDataService` no necesita cambios estructurales — Newtonsoft.Json maneja el nuevo Dictionary automáticamente con defaults.

---

## Cambios en GameBootstrap

**Archivo**: `Assets/ArtUnbound/Scripts/Core/GameBootstrap.cs`

1. **Nuevo estado en `GameState` enum**: `VRGallery`

2. **Detección de dispositivo** (en `Awake()` o `Start()`):
   ```csharp
   bool isQuest2 = OVRPlugin.GetSystemHeadsetType() == OVRPlugin.SystemHeadset.Quest2;
   if (isQuest2) saveData.preferVRMode = true; // Hardware override, no persiste
   ```

3. **`SetState(GameState.VRGallery)`**:
   - Llama `VRModeController.ActivateVRMode()`
   - Llama `VRGalleryController.LoadGallery(saveData.lastGalleryId)`
   - Activa `VRLocomotionController`
   - Muestra NavigationGallery con botones VR (vía `NavigationGalleryVRButtonsController`)
   - Oculta todos los demás panels (`HideAllPanels()`)

4. **Post-puzzle en VR**: `OnPuzzleComplete` → `PostGameController` → al cerrarse vuelve a `GameState.VRGallery` en lugar de `GameState.MainMenu`

5. **Referencias serializadas nuevas** en el Inspector:
   - `VRModeController vrModeController`
   - `VRGalleryController vrGalleryController`
   - `VRLocomotionController vrLocomotionController`
   - `GalleryCatalog galleryCatalog`

---

## VRModeController

**Archivo**: `Assets/ArtUnbound/Scripts/VR/VRModeController.cs`

```csharp
public void ActivateVRMode() {
    // Desactivar passthrough
    arCameraManager.enabled = false;
    arCameraBackground.enabled = false;
    mainCamera.clearFlags = CameraClearFlags.Skybox;

    // Desactivar sistemas MR (no destruir, solo disable)
    arPlaneManager.enabled = false;
    arAnchorManager.enabled = false;
    spatialPermissionService.gameObject.SetActive(false);
    wallPlacementDetector.gameObject.SetActive(false);
    wallAnchorManager.gameObject.SetActive(false);
    artworkHangingController.gameObject.SetActive(false);

    // Activar sistemas VR
    vrGalleryController.gameObject.SetActive(true);
    vrLocomotionController.gameObject.SetActive(true);
    vrWallHangingController.gameObject.SetActive(true);
}

public void DeactivateVRMode() {
    // Inverso del anterior
    // Restaurar passthrough + sistemas MR
    // Desactivar sistemas VR
}
```

---

## VRLocomotionController (Teleport)

**Archivo**: `Assets/ArtUnbound/Scripts/VR/VRLocomotionController.cs`

- Usa `TeleportationProvider` + `TeleportationArea` del XR Interaction Toolkit (ya en el proyecto)
- Input: **Left Thumbstick** (`XRI LeftHand/Primary2DAxis`) activa el ray de teleport
- Release del thumbstick confirma → **no usa trigger**, sin conflicto con grab de piezas/cuadros
- `TeleportationArea` cubre el piso del prefab de galería activo
- Solo activo cuando `GameState == VRGallery` o `GameState == Playing` (en VR mode)

---

## VRWallHangingController

**Archivo**: `Assets/ArtUnbound/Scripts/VR/VRWallHangingController.cs`

Diferencia clave vs MR: detección de pared via raycast contra capa `VRWall` en lugar de `ARPlaneManager`.

```csharp
// En Update(), cuando hay un frame siendo arrastrado:
if (Physics.Raycast(controllerPosition, controllerForward, out hit, 2f, vrWallLayerMask)) {
    ghostPreview.SetPositionAndRotation(
        hit.point + hit.normal * 0.02f,
        Quaternion.LookRotation(-hit.normal));
}

// En OnTriggerReleased():
public bool TryPlaceOnWall(GameObject frame, Vector3 nearPosition) {
    // Raycast desde nearPosition a las 8 direcciones
    // Si pared encontrada a < 0.25m: colocar frame, llamar GalleryPersistenceService.SavePainting()
    // Si no: devolver frame a posición flotante inicial
}
```

El prefab de galería debe tener sus paredes en la capa `VRWall` (Layer nuevo a crear en Unity).

---

## VRGalleryController

**Archivo**: `Assets/ArtUnbound/Scripts/VR/VRGalleryController.cs`

```csharp
public void LoadGallery(string galleryId) {
    if (activeGalleryInstance != null) Destroy(activeGalleryInstance);
    var def = galleryCatalog.galleries.Find(g => g.galleryId == galleryId);
    activeGalleryInstance = Instantiate(def.environmentPrefab);
    SpawnSavedPaintings(galleryId);
}

public void SwitchGallery(string newGalleryId) {
    saveData.lastGalleryId = newGalleryId;
    saveDataService.MarkDirty();
    LoadGallery(newGalleryId);
    RepositionNavigationPanel(); // Flota frente al usuario tras el switch
}

private void SpawnSavedPaintings(string galleryId) {
    var paintings = galleryPersistenceService.GetPaintings(galleryId);
    foreach (var data in paintings) {
        // Instanciar frame prefab con artwork + tier correcto en su posición guardada
    }
}
```

---

## GallerySelectionController

**Archivo**: `Assets/ArtUnbound/Scripts/UI/GallerySelectionController.cs`

- Sub-panel dentro del World Space canvas de NavigationGallery
- Lee `GalleryCatalog.galleries` y genera una card por galería (thumbnail + nombre)
- Galería activa: card con indicador "Actual", botón deshabilitado
- Al seleccionar otra: llama `VRGalleryController.SwitchGallery(id)`
- Con 1 sola galería: panel con una card marcada "Actual" — arquitectura lista para agregar más

---

## Prefab: Galería Clásica

**Archivo**: `Assets/ArtUnbound/Prefabs/VR/Gallery_Classic.prefab`

Geometría optimizada para Quest 2 (URP Lit básico, sin PBR pesado):

- Piso plano (20m × 12m)
- 4 paredes perimetrales (4m de altura) — layer `VRWall`
- Techo plano
- 4–6 luces `Point Light` o `Directional` suaves, warm white
- `TeleportationArea` en el piso
- Sin oclusión compleja — draw calls mínimos

---

## Flujo de Entrada por Dispositivo

```
App Start
├── Quest 2 detectado
│   └── SetState(VRGallery) → galería default + NavigationGallery en modo VR
└── Quest 3/Pro detectado
    ├── saveData.preferVRMode == true
    │   └── SetState(VRGallery)
    └── saveData.preferVRMode == false (default)
        └── SetState(Loading → Onboarding → MainMenu) [flujo MR actual sin cambios]
```

---

## Flujo Completo en VR

```
VRGallery
├── NavigationGallery (botones existentes + nuevos)
│   ├── [Botones existentes sin cambios]
│   ├── "Galerías" → GallerySelectionController
│   │   └── Selecciona galería → SwitchGallery → reload environment
│   └── "MR" (Quest 3 only) → DeactivateVRMode → MainMenu
│
├── Puzzle Flow (igual que MR, sin passthrough)
│   ArtworkSelection → ComfortPositioning → Playing → PostGame
│   └── PostGame cierra → vuelve a VRGallery
│
└── Colgar cuadro (mismo flujo que MR)
    Pinch cuadro → drag → teleport a pared → soltar → VRWallHangingController
    └── GalleryPersistenceService.SavePainting(galleryId, position, rotation)
```

---

## Archivos a Modificar

| Archivo | Cambio |
|---------|--------|
| `Assets/ArtUnbound/Scripts/Core/GameBootstrap.cs` | `+GameState.VRGallery`, detección Quest 2, wiring de nuevas referencias, flujo post-puzzle en VR |
| `Assets/ArtUnbound/Scripts/Data/SaveData.cs` | `+preferVRMode`, `+lastGalleryId`, `+galleryPaintings` |
| `Assets/ArtUnbound/Scenes/Main.unity` | +GameObjects VR (desactivados por defecto); +botones nuevos en NavigationGallery existente |

## Archivos Nuevos

```
Assets/ArtUnbound/Scripts/VR/
  VRModeController.cs
  VRGalleryController.cs
  VRLocomotionController.cs
  VRWallHangingController.cs
  GalleryPersistenceService.cs

Assets/ArtUnbound/Scripts/UI/
  NavigationGalleryVRButtonsController.cs
  GallerySelectionController.cs

Assets/ArtUnbound/Data/
  GalleryDefinition.cs
  GalleryCatalog.cs
  GalleryCatalog.asset
  Galleries/Gallery_Classic.asset

Assets/ArtUnbound/Prefabs/VR/
  Gallery_Classic.prefab
  GallerySelectionPanel.prefab
```

---

## Verificación / Testing

1. **Editor**: Forzar `preferVRMode = true` en SaveData → verificar que `VRModeController.ActivateVRMode()` deshabilita ARCameraManager sin errores NullRef; galería instancia correctamente.

2. **Quest 2 físico**:
   - Entra directo en galería (sin MR)
   - Teleport con thumbstick izquierdo, trigger libre para interacciones
   - Completar puzzle → cerrar PostGame → pinch cuadro → teleport a pared → soltar → cuadro cuelga
   - Cerrar app → reabrir → cuadro aparece en misma posición 3D

3. **Quest 3 físico**:
   - Entra en modo MR por defecto
   - NavigationGallery muestra botón "VR" → presionar → entra en galería virtual
   - NavigationGallery en VR muestra botón "MR" → presionar → regresa a passthrough
   - Panel "Galerías" muestra 1 galería marcada "Actual"

---

## Unity Editor Setup

All C# scripts are already created. This section covers everything that must be done inside the Unity Editor to complete the feature.

---

### 1. Create the `VRWall` layer

`Edit → Project Settings → Tags and Layers → Layers`

Add in the first available slot (e.g. User Layer 8 or higher):
```
Name: VRWall
```

Note the layer number — you will need it when assigning the `vrWallLayerMask` field on `VRWallHangingController` and `VRLocomotionController`.

---

### 2. Create the ScriptableObject assets

#### 2a. GalleryCatalog

- In the Project panel, navigate to `Assets/ArtUnbound/Data/`
- Right-click → **Create → ArtUnbound → Gallery Catalog**
- Rename to `GalleryCatalog`
- Fields:
  - `Default Gallery Id`: `gallery_classic`
  - `Galleries`: leave empty for now (filled in step 2b)

#### 2b. GalleryDefinition — Classic Gallery

- Create subfolder `Assets/ArtUnbound/Data/Galleries/`
- Right-click → **Create → ArtUnbound → Gallery Definition**
- Rename to `Gallery_Classic`
- Fields:
  - `Gallery Id`: `gallery_classic`
  - `Display Name`: `Classic Gallery`
  - `Thumbnail`: assign when artwork is ready (can be left empty for now)
  - `Environment Prefab`: assign the prefab from step 3

- Open `GalleryCatalog.asset` → drag `Gallery_Classic.asset` into the `Galleries` array

---

### 3. Create the `Gallery_Classic` prefab

#### 3a. Build the base structure

Each visual element needs its own child GameObject under `Gallery_Classic`. The final hierarchy looks like this:

```
Gallery_Classic          ← root Empty GameObject
├── Floor
├── Wall_North
├── Wall_South
├── Wall_East
├── Wall_West
├── Ceiling
└── Lights
    ├── Light_NE
    ├── Light_NW
    ├── Light_SE
    └── Light_SW
```

**Tip**: instead of creating an Empty and manually adding Mesh Filter + Mesh Renderer, right-click on `Gallery_Classic` in the Hierarchy → **3D Object → Cube** (or Plane for the floor). Unity creates the child with Mesh Filter, Mesh Renderer, and Box Collider already attached. Just rename it and set the position/scale.

---

**Floor**
- Right-click `Gallery_Classic` → **3D Object → Plane** → rename to `Floor`
- Transform:
  - Position: `(0, 0, 0)`
  - Scale: `(2, 1, 1.2)` — a Unity Plane is 10×10 units, so this gives 20m × 12m
- Material: In the Project panel, right-click → **Create → Material**, rename to `Mat_GalleryFloor`
  - Shader: `Universal Render Pipeline/Lit`
  - Click the **Base Map** color swatch → set RGB to `(220, 215, 205)`
  - Drag `Mat_GalleryFloor` into `Mesh Renderer → Materials → Element 0`
- Layer: `Default`
- Add Component: `TeleportationArea` (from XR Interaction Toolkit)
  - Set `Match Orientation` to `World Space Up`
- The `Box Collider` is already added by Unity when creating a Plane — leave it as-is

---

**Wall_North, Wall_South, Wall_East, Wall_West** (4 walls)
- Right-click `Gallery_Classic` → **3D Object → Cube** → rename to `Wall_North`, repeat for each
- Set position and scale for each:
  | Child name | Position | Scale |
  |------------|----------|-------|
  | Wall_North | (0, 2, 6) | (20, 4, 0.2) |
  | Wall_South | (0, 2, -6) | (20, 4, 0.2) |
  | Wall_East  | (10, 2, 0) | (0.2, 4, 12) |
  | Wall_West  | (-10, 2, 0) | (0.2, 4, 12) |
- Material: Create `Mat_GalleryWall` the same way as the floor → Base Map color RGB `(240, 235, 225)`
  - Assign `Mat_GalleryWall` to `Mesh Renderer → Materials → Element 0` on all 4 walls
- **Layer: `VRWall`** ← required for wall hanging and locomotion raycasts — set this on each wall GameObject
- The `Box Collider` is already added by Unity — leave it as-is

---

**Ceiling**
- Right-click `Gallery_Classic` → **3D Object → Cube** → rename to `Ceiling`
- Position: `(0, 4, 0)` — Scale: `(20, 0.2, 12)`
- Material: assign `Mat_GalleryWall`
- Layer: `Default`

---

**Lights**
- Right-click `Gallery_Classic` → **Create Empty** → rename to `Lights`
- Inside `Lights`, right-click → **Light → Point Light** four times, rename `Light_NE`, `Light_NW`, `Light_SE`, `Light_SW`
- Positions: `(8, 3.5, 5)`, `(-8, 3.5, 5)`, `(8, 3.5, -5)`, `(-8, 3.5, -5)`
- Each light: Intensity `1.5`, Range `12`, Color warm white (click color swatch → H≈40, S≈15, V≈100)
- Optional fill light: right-click `Lights` → **Light → Directional Light**, rename to `Light_Fill`
  - Position: `(0, 0, 0)` (irrelevant for directional lights — direction is what matters)
  - Rotation: `(50, -30, 0)`
  - Intensity: `0.3`

#### 3b. Save as Prefab

- Drag `Gallery_Classic` from the Hierarchy into `Assets/ArtUnbound/Prefabs/VR/`
  - Create the folder if it doesn't exist
- Unity will ask — choose **Create Original Prefab**
- Assign this prefab to the `Environment Prefab` field of `Gallery_Classic.asset`
- Delete the GameObject from the scene (it is now saved as a prefab)

---

### 4. Add VR GameObjects to `Main.unity`

Open `Assets/ArtUnbound/Scenes/Main.unity`.

**Create the root container:**
1. In the Hierarchy, right-click on an empty area → **Create Empty**, name it `[VR Systems]`
2. In the Inspector, uncheck the checkbox next to the GameObject name to **disable it** (this is important — VR systems start inactive)

All VR children go inside `[VR Systems]`. To create each child: right-click `[VR Systems]` → **Create Empty**, rename it, then add the component via **Add Component** in the Inspector.

---

#### 4a. VRModeController

1. Right-click `[VR Systems]` → **Create Empty** → rename to `VRModeController`
2. With `VRModeController` selected → **Add Component** → search `VRModeController` → click it
3. Assign each field by dragging the listed GameObject into the slot. When a field expects a component (not a bare transform), drag the **GameObject** — Unity automatically picks up the correct component:

   | Field | What to drag |
   |-------|-------------|
   | Main Camera | Expand `XR Origin → Camera Offset` → drag **`Main Camera`** |
   | Ar Camera Manager | Drag **`Main Camera`** (it has ARCameraManager on it) |
   | Ar Camera Background | Drag **`Main Camera`** (it has ARCameraBackground on it) |
   | Ar Plane Manager | Drag **`XR Origin`** (ARPlaneManager is on XR Origin) |
   | Ar Anchor Manager | Drag **`XR Origin`** (ARAnchorManager is on XR Origin) |
   | Spatial Permission Service | Drag **`MR Interaction Setup`** (SpatialPermissionService is a component on it) |
   | Wall Detection Service | Drag **`MR Interaction Setup`** (WallDetectionService is also on it) |
   | Wall Placement Detector | Drag **`ArtworkHangingController`** (WallPlacementDetector is a component on it) |
   | Wall Anchor Manager | Drag **`WallAnchorManager`** |
   | Artwork Hanging Controller | Drag **`ArtworkHangingController`** |
   | Wall Highlight Controller | Drag **`WallHighlight`** |
   | Vr Gallery Controller | Leave empty for now — fill after step 4b |
   | Vr Locomotion Controller | Leave empty for now — fill after step 4c |
   | Vr Wall Hanging Controller | Leave empty for now — fill after step 4d |
   | Vr Skybox Material | Leave empty (Unity will use its default skybox) |

---

#### 4b. VRGalleryController

1. Right-click `[VR Systems]` → **Create Empty** → rename to `VRGalleryController`
2. **Add Component** → search `VRGalleryController` → click it
3. Assign:

   | Field | What to drag |
   |-------|-------------|
   | Gallery Catalog | From Project window drag **`GalleryCatalog.asset`** (created in step 2) |
   | Gallery Painting Prefab | `GalleryPainting.prefab` — see **Step 4b-ii** below |
   | Navigation Panel | In the Hierarchy find **`NativeGallery`** and drag it into this field |

4. Go back to `VRModeController` and drag **`VRGalleryController`** into the **Vr Gallery Controller** field.

##### Step 4b-ii: Create the GalleryPainting Prefab

> `WallAnchorManager` does NOT use a spawning prefab — it works with an already-built frame Transform. You need to create a dedicated prefab from scratch.

1. In the Hierarchy, right-click → **3D Object → Quad**, name it `GalleryPainting`
2. In the Inspector, set **Tag** to `PlacedArtwork` and **Layer** to `PuzzlePiece`
3. Set Transform **Scale** to `(0.5, 0.5, 1)`
4. The Quad comes with a **Mesh Collider** by default — right-click it in the Inspector → **Remove Component**
5. Click **Add Component** → search `Box Collider` → add it → set **Size** to `(0.5, 0.5, 0.05)`
6. Click **Add Component** → search `PlacedArtworkIdentifier` → add it
7. On the **Mesh Renderer**, any URP Lit material works (the artwork texture is applied at runtime)
8. In the Project window, navigate to `Assets/ArtUnbound/Prefabs/` and create a subfolder `VR` if it doesn't exist (right-click → **Create → Folder**)
9. Drag the `GalleryPainting` GameObject from the Hierarchy into `Assets/ArtUnbound/Prefabs/VR/` — Unity creates `GalleryPainting.prefab`
10. Delete the `GalleryPainting` GameObject from the scene (right-click → **Delete**)
11. Drag `GalleryPainting.prefab` from the Project window into the **Gallery Painting Prefab** field on `VRGalleryController`

---

#### 4c. VRLocomotionController

1. Right-click `[VR Systems]` → **Create Empty** → rename to `VRLocomotionController`
2. **Add Component** → search `VRLocomotionController` → click it

**Assign Teleportation Provider:**
3. Drag **`XR Origin`** into the **Teleportation Provider** field — `TeleportationProvider` is a component on `XR Origin`, Unity picks it up automatically

**Assign Left Thumbstick Action:**
4. Click the **circle picker** (◎) to the right of the **Left Thumbstick Action** field
5. In the search box type `Thumbstick`
6. Select **`XRI Default Input Actions / XRI Left / Thumbstick`**

**Create the Arc Line Renderer (child of VRLocomotionController):**
7. Right-click `VRLocomotionController` → **Create Empty** → rename to `TeleportArc`
8. With `TeleportArc` selected → **Add Component** → search `Line Renderer` → add it
9. In the Line Renderer Inspector:
   - **Width** is a curve graph. Click the grey graph area to open the **Curve Editor**. Inside, single-click a dot to select it (turns white), then **right-click the selected dot → Edit Key...** → set **Value** to `0.01` → OK. Repeat for the second dot. If Edit Key does not appear, simply drag both dots to the very bottom of the graph (as close to 0.0 as possible) — 0.01 is visually near-zero on the scale.
   - **Positions → Size**: `2` (leave both positions at `0,0,0` — code overrides them at runtime)
   - **Materials → Element 0**: create a new Material (right-click in Project → **Create → Material**), name it `TeleportArcMat`, set Shader to `Universal Render Pipeline/Unlit`, color white. Drag it into the slot.
10. Drag the **`TeleportArc`** GameObject into the **Arc Line Renderer** field on `VRLocomotionController`

**Create the Teleport Reticle Prefab:**
11. In the Hierarchy, right-click → **3D Object → Cylinder**, name it `TeleportReticle`
12. Set Transform **Scale** to `(0.5, 0.01, 0.5)` (flat disc shape)
13. Create a new Material named `TeleportReticleMat`:
    - Shader: `Universal Render Pipeline/Lit`
    - Base Map color: green, **Alpha ~80** (drag the Alpha slider left to make it semi-transparent)
    - In the Material Inspector, set **Surface Type** to **Transparent**
14. Assign `TeleportReticleMat` to the Cylinder's Mesh Renderer
15. Remove the **Capsule Collider** that Unity adds automatically (right-click → **Remove Component**)
16. Drag `TeleportReticle` from the Hierarchy into `Assets/ArtUnbound/Prefabs/VR/` to create `TeleportReticle.prefab`
17. Delete `TeleportReticle` from the scene
18. Drag `TeleportReticle.prefab` into the **Reticle Prefab** field on `VRLocomotionController`

**Remaining numeric fields** — set directly in the Inspector:
| Field | Value |
|-------|-------|
| Teleport Layer Mask | Uncheck everything, then check **Default** only (the gallery floor will be on Default layer; do NOT check VRWall) |
| Arc Segments | `20` |
| Arc Velocity | `8` |
| Arc Gravity | `9.8` |

19. Go back to `VRModeController` and drag **`VRLocomotionController`** into the **Vr Locomotion Controller** field.

---

#### 4d. VRWallHangingController

1. Right-click `[VR Systems]` → **Create Empty** → rename to `VRWallHangingController`
2. **Add Component** → search `VRWallHangingController` → click it
3. Assign:

   | Field | What to drag / enter |
   |-------|---------------------|
   | Puzzle Board | Find the **`PuzzleBoard`** GameObject in the Hierarchy and drag it |
   | Gallery Controller | Drag **`VRGalleryController`** (the child created in step 4b) |
   | Vr Wall Layer Mask | Uncheck everything, then check **VRWall** only |
   | Wall Proximity Radius | `0.35` |
   | Wall Raycast Distance | `2` |
   | Placement Animation Duration | `0.5` |
   | Ghost Preview Prefab | See step below, or leave empty to skip the placement preview |

**Create the Ghost Preview Prefab (optional but recommended):**
4. In the Hierarchy, right-click → **3D Object → Quad**, name it `GhostPreview`
5. Set Scale to `(0.5, 0.5, 1)`
6. Create a new Material named `GhostPreviewMat`:
   - Shader: `Universal Render Pipeline/Lit`
   - Base Map color: white, **Alpha ~60**
   - Surface Type: **Transparent**
7. Assign `GhostPreviewMat` to the Quad's Mesh Renderer
8. Remove the Mesh Collider (right-click → **Remove Component**)
9. Drag the `GhostPreview` GameObject into `Assets/ArtUnbound/Prefabs/VR/` → creates `GhostPreview.prefab`
10. Delete `GhostPreview` from the scene
11. Drag `GhostPreview.prefab` into the **Ghost Preview Prefab** field on `VRWallHangingController`

12. Go back to `VRModeController` and drag **`VRWallHangingController`** into the **Vr Wall Hanging Controller** field.

---

### 5. Assign VR references in `GameBootstrap`

1. In the Hierarchy, select the **`GameBootstrap`** GameObject
2. In the Inspector, scroll down to the **VR Mode** header
3. Assign each field:

   | Field | What to drag |
   |-------|-------------|
   | Vr Mode Controller | Drag **`VRModeController`** (child of `[VR Systems]`) |
   | Vr Gallery Controller | Drag **`VRGalleryController`** (child of `[VR Systems]`) |
   | Vr Locomotion Controller | Drag **`VRLocomotionController`** (child of `[VR Systems]`) |
   | Vr Wall Hanging Controller | Drag **`VRWallHangingController`** (child of `[VR Systems]`) |
   | Gallery Catalog | From Project window drag **`GalleryCatalog.asset`** |

---

### 6. Add VR buttons to the NativeGallery BottomNav

1. In the Hierarchy, expand **`NativeGallery`** → find the child named **`BottomNav`** (the horizontal bar with the existing buttons)
2. Find the existing button named **`BtnConfig`** inside `BottomNav`
3. Right-click `BtnConfig` → **Duplicate** — rename the copy to **`BtnVR`**
   - Select `BtnVR` → expand it → find the TMP text child → change the text to `VR`
   - In the Inspector, **uncheck** the checkbox next to the GameObject name to disable it
4. Repeat: duplicate `BtnConfig` again → rename to **`BtnMR`** → text `MR` → disable it
5. Repeat: duplicate `BtnConfig` again → rename to **`BtnGalleries`** → text `Galleries` → disable it
6. Select the **`NativeGallery`** GameObject → in the Inspector find the **`NativeGalleryController`** component
7. Assign the new buttons:

   | Field | What to drag |
   |-------|-------------|
   | Btn VR | Drag **`BtnVR`** |
   | Btn MR | Drag **`BtnMR`** |
   | Btn Galerias | Drag **`BtnGalleries`** |
   | Gallery Selection Controller | Leave empty for now — fill after step 7 |

---

### 7. Create and configure the GallerySelectionPanel

#### 7a. Create the panel

1. In the Hierarchy, expand **`NativeGallery`** and find the **World Space Canvas** that contains the existing panels (e.g. `MainMenuPanel`, `DetailPanel`)
2. Right-click that canvas → **Create Empty** → rename to **`GallerySelectionPanel`**
3. Add component **`Image`** → set Color to black with Alpha ~180 (semi-transparent background, matching other panels)
4. Set the RectTransform to fill the same area as the other panels (match their Anchors, Width, Height values)
5. Inside `GallerySelectionPanel`, right-click → **UI → Scroll View** → rename to `GalleryScrollView`
   - Delete the default **Scrollbar Horizontal** and **Scrollbar Vertical** children (not needed)
   - Select the **`Content`** child inside `Viewport` → rename it to **`GalleryGrid`**
   - With `GalleryGrid` selected → **Add Component** → search `Grid Layout Group` → add it
     - Cell Size: `(200, 250)`
     - Spacing: `(10, 10)`
     - Start Corner: `Upper Left`
     - Child Alignment: `Upper Left`
     - Constraint: `Fixed Column Count` → Count: `3`
   - Add component **`Content Size Fitter`** → Vertical Fit: `Preferred Size`
6. Add component **`GallerySelectionController`** to the `GallerySelectionPanel` GameObject
7. **Disable** `GallerySelectionPanel` (uncheck the checkbox in the Inspector) — code shows it on demand

#### 7b. Create the GalleryCard Prefab

1. In the Hierarchy, right-click → **UI → Button - TextMeshPro** → rename to **`GalleryCard`**
   - Set RectTransform Size to `(200, 250)`
   - Delete the default text child (we'll add our own children)
2. Right-click `GalleryCard` → **UI → Image** → rename to **`Thumbnail`**
   - Set Anchors to stretch top half: Anchor Min `(0, 0.3)`, Anchor Max `(1, 1)`, Left/Right/Top/Bottom offsets all `0`
3. Right-click `GalleryCard` → **UI → Text - TextMeshPro** → rename to **`Label`**
   - Set Anchors to bottom strip: Anchor Min `(0, 0)`, Anchor Max `(1, 0.3)`
   - Set Alignment: Center, Middle
   - Font size: `18`
4. Drag `GalleryCard` from the Hierarchy into `Assets/ArtUnbound/Prefabs/VR/` → creates `GalleryCard.prefab`
5. Delete `GalleryCard` from the scene

#### 7c. Wire GallerySelectionController

1. Select **`GallerySelectionPanel`** in the Hierarchy
2. In the **`GallerySelectionController`** component, assign:

   | Field | What to drag |
   |-------|-------------|
   | Gallery Catalog | `GalleryCatalog.asset` from the Project window |
   | Grid Container | Drag **`GalleryGrid`** (the Content child inside the Scroll View) |
   | Gallery Card Prefab | Drag **`GalleryCard.prefab`** from `Assets/ArtUnbound/Prefabs/VR/` |

3. Select the **`NativeGallery`** GameObject → find `NativeGalleryController` → assign:
   - **Gallery Selection Controller** → drag **`GallerySelectionPanel`**

---

### 8. Verify the floor collider in Gallery_Classic.prefab

`VRLocomotionController` uses `Physics.Linecast` against the **Teleport Layer Mask** to detect the floor. The gallery floor needs to be on the correct layer so the arc hits it.

1. Open `Gallery_Classic.prefab` for editing (double-click it in the Project window)
2. Select the **`Floor`** child GameObject
3. In the Inspector, set its **Layer** to **`Default`** (same layer you selected in the Teleport Layer Mask on `VRLocomotionController`)
4. Confirm the **Box Collider** on Floor is enabled (it should be, since you added it when building the prefab)
5. Save and close the prefab

> The `TeleportationArea` component on the floor is NOT required by `VRLocomotionController` — our script does its own arc raycast. `TeleportationProvider` on `XR Origin` is the only XR Toolkit dependency.

---

### 9. Test in the Editor (simulated VR mode)

To test without a physical device, temporarily force VR mode by opening `GameBootstrap.cs` and editing `DetectDevice()`:

```csharp
// At the top of DetectDevice(), add one of these overrides:
_isQuest3OrPro = false; // Simulates Quest 2 → enters VR directly on boot
// OR
_saveData.preferVRMode = true; // Simulates Quest 3 user who previously chose VR
```

Press **Play** and verify:
1. The scene enters `VRGallery` state (check the Console for `[GameBootstrap] SetState → VRGallery`)
2. The `Gallery_Classic` prefab instantiates in the world (visible in Scene view)
3. No `NullReferenceException` errors in the Console
4. `NativeGallery` shows **Galleries** button (and **MR** button if simulating Quest 3)
5. Passthrough is off — the camera background is the skybox, not the real world

Remove the override before building for device.
