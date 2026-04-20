# Contexto: Reemplazo del PieceTray con ScrollView

Este documento captura todo el diseño, las decisiones y los cambios de código ya realizados
para la nueva bandeja de piezas basada en ScrollRect. Úsalo para continuar el trabajo
en una nueva conversación de Claude.

---

## Qué se está construyendo y por qué

El sistema original mostraba las piezas del rompecabezas en un carrusel 3D horizontal
(`PieceScrollController`) con botones de paginación manual ↑/↓ (`PiecesPanelController`).

El objetivo es **reemplazar esa paginación manual** por un panel de thumbnails 2D con
`ScrollRect + GridLayoutGroup`, exactamente igual a como funciona `NativeGalleryController`
(el menú principal del juego). El jugador ve un grid de imágenes de las piezas y las toma
de ahí para colocarlas en el board.

---

## Organización de la UI durante el juego

El layout durante el ensamblado del rompecabezas tiene **tres zonas**:

```
[Panel izquierdo]          [Centro]              [Panel derecho]
PuzzleHUDController    PuzzleAchievements     PiecesPanelController
- Imagen referencia        Controller             - PiecesPanel  ← nuevo ScrollView aquí
- Contador de piezas    - Mensajes de            - PostGamePanel ← sin cambios
- Artista / título         logros (fila,
- Canción actual           columna, etc.)
```

El **panel derecho** tiene dos componentes que se alternan:
- `PiecesPanel` — visible mientras se arma el rompecabezas (aquí va el nuevo ScrollView)
- `PostGamePanel` — visible al completar o al ver un rompecabezas ya terminado

Esta organización **no cambia**. Solo cambia el interior del `PiecesPanel`.

---

## Modelo de interacción — decisión de diseño clave

Hay **dos formas distintas** de interactuar con piezas y cada una usa un mecanismo diferente:

| Situación | Mecanismo | Por qué |
|---|---|---|
| **Tomar pieza del panel (thumbnail)** | `IPointerDownHandler` → `BeginExternalDrag()` | El thumbnail es UI — funciona con near pinch, remote pinch y controlador a través de `OVRRaycaster` |
| **Tomar pieza del board (3D)** | `Physics.OverlapSphere` → grab normal | La pieza ya existe en el mundo 3D con su `MeshCollider` habilitado |

Los thumbnails **no tienen BoxCollider**. La detección es visual a través del `Image` (Graphic)
que detecta `OVRRaycaster`. El `Physics.OverlapSphere` en `InteractionManager` ya no busca
thumbnails — solo busca piezas 3D en el board.

### Por qué no BoxCollider en los thumbnails

El sistema original usaba `Physics.OverlapSphere` porque las piezas existían como objetos
3D en el mundo y el jugador las agarraba físicamente. Con el nuevo sistema de thumbnails,
la interacción es diferente:
- No es un "grab físico" — es activar un elemento de UI
- Debe funcionar con remote pinch (ray interactor), no solo near pinch
- El Button + IPointerDownHandler funciona con todos los métodos de input de Meta XR

### Qué pasa al tomar un thumbnail

Las piezas 3D **siguen existiendo** en la escena aunque no sean visibles. `PuzzlePiece.ShowThumbnailMode()`
deshabilita el `MeshRenderer` y el `MeshCollider`. Al activar un thumbnail, `BeginExternalDrag()`
simplemente hace visible la pieza 3D ya existente y la pone en estado `Grabbed`.
No hay "creación" de pieza — solo cambio de visibilidad.

---

## Tamaño de los thumbnails — decisión de diseño clave

El tamaño de los thumbnails en el grid **debe coincidir exactamente** con el tamaño visual
de las piezas en el board. Si el thumbnail se ve de un tamaño y al tomarlo la pieza 3D
aparece de otro, el efecto visual es extraño.

`PuzzleBoard` calcula `pieceSizeM` automáticamente según el tamaño del board y su aspect ratio.
El usuario **no puede ni debe controlar** ese valor directamente.

`PieceTrayGridController.ApplyGridLayout(pieceSizeM)` convierte ese valor a píxeles:

```
cellPx  = pieceSizeM / canvas.lossyScale.x   (sin clamps)
columns = viewportWidth / (cellPx + spacing)
```

Resultado natural por dificultad:

| Dificultad | Piezas | Columnas aprox. |
|---|---|---|
| Easy   |  64 (8×8)   | ~5 (piezas grandes) |
| Normal | 144 (12×12) | ~8 |
| Hard   | 256 (16×16) | ~10 |
| Expert | 512 (22×22) | ~13 (piezas pequeñas) |

---

## Archivos modificados

### `Assets/ArtUnbound/Scripts/UI/PieceThumbnailItem.cs` — REESCRITO

**Qué hace:** Representa un thumbnail de pieza en el panel. Usa `Image` con `Sprite.Create()`
(igual que los cards de la galería) en lugar de un quad 3D con mesh custom.
Implementa `IPointerDownHandler` para detectar la activación por cualquier método de input.

**Componentes requeridos en el prefab:** `Button` + `Image` + `PieceThumbnailItem`
(sin BoxCollider, sin hijo Text).

**Método clave:**
```csharp
public void Initialize(PuzzlePiece owner, Texture2D artworkTexture,
                       int col, int row, int cols, int rows, float worldSizeM)
// Crea un Sprite.Create() con el rect de píxeles correspondiente a esta pieza
// y lo asigna al Image component del prefab.
```

**Interacción:**
```csharp
public void OnPointerDown(PointerEventData eventData)
// Llama InteractionManager.BeginExternalDrag(LinkedPiece)
```

---

### `Assets/ArtUnbound/Scripts/UI/PieceTrayGridController.cs` — REESCRITO

**Qué hace:** Instancia un `PieceThumbnailButton` por pieza en el `Content` del ScrollRect.
No tiene paginación manual. El `GridLayoutGroup` + `ContentSizeFitter` manejan todo el layout.
Calcula dinámicamente el cell size y el número de columnas en `ApplyGridLayout(pieceSizeM)`.

**Campos en el Inspector:**
- `contentRoot` — el `Content` transform del ScrollRect (tiene GridLayoutGroup)
- `thumbnailPrefab` — el prefab `PieceThumbnailButton`

**Métodos públicos:**
```csharp
Initialize(pieces, artworkTexture, gridCols, gridRows, pieceSizeM)
RemoveThumbnail(pieceId)   // pieza colocada correctamente → destruye thumbnail
MoveToEnd(pieceId)         // pieza devuelta al tray → mueve thumbnail al final
Show() / Hide()
```

---

### `Assets/ArtUnbound/Scripts/UI/PiecesPanelController.cs` — SIMPLIFICADO

**Qué hace ahora:** Solo `Show()` / `Hide()` del panel derecho. Todo el código de
paginación (botones ↑/↓, indicador de página) fue eliminado.

`SetPaginationButtonStates()` y `SetPageIndicator()` se mantienen como **métodos vacíos**
(no-ops) porque `PieceScrollController` todavía los llama — no se tocó ese archivo.

---

### `Assets/ArtUnbound/Scripts/Input/InteractionManager.cs` — MODIFICADO

**Cambios realizados:**

1. **Eliminado** el bloque de detección de thumbnails via `Physics.OverlapSphere`
   (los thumbnails ya no tienen BoxCollider).

2. **Eliminado** el bloque "Handle thumbnail grab" en `HandlePinchStart`
   (el grab desde thumbnail ahora lo inicia `IPointerDownHandler`).

3. **Corregido** `HandlePinchEnd`: ahora captura `wasFromThumbnail` y siempre llama
   `board.ReturnPieceToTray(piece)` en fallo de snap — independientemente de si la pieza
   vino del board o del thumbnail panel.

4. **Agregado** método público:
```csharp
public void BeginExternalDrag(PuzzlePiece piece)
// Llamado por PieceThumbnailItem.OnPointerDown.
// Pone la pieza en estado Grabbed y la hace visible.
// HandlePinchHold la mueve a la mano en el siguiente frame.
// HandlePinchEnd intenta el snap al soltar.
```

---

### `Assets/ArtUnbound/Scripts/Gameplay/PuzzlePiece.cs` — MODIFICADO (sesión anterior)

Agrega soporte para el thumbnail item:
- `SetThumbnailItem(item)` — guarda referencia al thumbnail
- `ShowPieceMode()` — habilita MeshRenderer + MeshCollider, oculta thumbnail
- `ShowThumbnailMode()` — deshabilita MeshRenderer + MeshCollider, muestra thumbnail
- `SetState()` llama automáticamente Show/HidePieceMode según el estado

---

### `Assets/ArtUnbound/Scripts/Gameplay/PuzzleBoard.cs` — MODIFICADO (sesión anterior)

- Campo `[SerializeField] private PieceTrayGridController pieceTrayController`
- En `InitializeScroll()`: llama `pieceTrayController?.Initialize(...)`
- En `SnapPieceToSlot()`: llama `pieceTrayController?.RemoveThumbnail(piece.PieceId)`
- En `ReturnPieceToTray()`: llama `pieceTrayController?.MoveToEnd(piece.PieceId)` ANTES
  de `scrollController.AddPieceAtEnd(piece)`

---

## Lo que falta: configuración en Unity Editor

Ver instrucciones detalladas en `docs/Setup-PieceTray-ScrollView.md`.

Resumen:

### 1. Limpiar PiecesPanel
- Borrar botones ScrollUp / ScrollDown y texto PageIndicator

### 2. Crear prefab `PieceThumbnailButton`
- **GameObject > UI > Button - TextMeshPro**
- Borrar hijo **Text (TMP)**
- Button: Navigation = None, OnClick vacío
- Image: Raycast Target = ✓, Source Image = None
- Agregar script **PieceThumbnailItem**
- Sin BoxCollider
- Guardar en `Assets/ArtUnbound/Prefabs/UI/`

### 3. Agregar ScrollView en PiecesPanel
- **UI > Scroll View** (Unity crea la jerarquía completa automáticamente)
- ScrollRect: Vertical ✓, Horizontal ✗, Movement Type = Elastic
- Viewport: Image Alpha = 0, Mask Show Graphic = ✗
- Content: agregar **GridLayoutGroup** (Fixed Column Count, Spacing 5×5, Upper Left,
  Horizontal, Upper Center) + **ContentSizeFitter** (Vertical = Preferred Size)
- Borrar Scrollbar Horizontal, conservar Scrollbar Vertical (Visibility = Auto Hide)

### 4. Agregar PieceTrayGridController en ScrollView
- Content Root → Content
- Thumbnail Prefab → PieceThumbnailButton

### 5. Asignar en PuzzleBoard
- Piece Tray Controller → ScrollView (el que tiene PieceTrayGridController)

---

## Flujo completo en runtime

```
PuzzleBoard.Initialize()
  → PieceTrayGridController.Initialize(pieces, texture, gridCols, gridRows, pieceSizeM)
      → ApplyGridLayout(pieceSizeM): calcula cellPx y columns, aplica a GridLayoutGroup
      → Instancia PieceThumbnailButton × N piezas en Content
      → GridLayoutGroup posiciona automáticamente
      → PuzzlePiece.SetThumbnailItem(item) → ShowThumbnailMode() → pieza 3D oculta

Jugador activa thumbnail (near pinch / remote pinch / controlador)
  → OVRRaycaster detecta Image → IPointerDownHandler.OnPointerDown
  → InteractionManager.BeginExternalDrag(piece)
      → piece.SetDragged(true) → SetState(Grabbed) → ShowPieceMode()
      → thumbnail.SetActive(false)
  → HandlePinchHold() mueve pieza a la mano cada frame
  → HandlePinchEnd() intenta snap al board

Snap correcto
  → PuzzleBoard.SnapPieceToSlot()
  → PieceTrayGridController.RemoveThumbnail(pieceId) → Destroy thumbnail

Snap fallido (suelta lejos del board)
  → PuzzleBoard.ReturnPieceToTray(piece)
      → PieceTrayGridController.MoveToEnd(pieceId) → thumbnail al final del grid
      → piece.ReturnToPool() → animación de vuelta → SetState(InPool) → ShowThumbnailMode()
      → thumbnail.SetActive(true) al final del grid

Jugador hace scroll en el panel
  → ScrollRect + OVRRaycaster + PointableCanvas lo manejan nativamente
  → sin código adicional

Jugador toma pieza del board (ya colocada, estado Placed)
  → Physics.OverlapSphere detecta MeshCollider (habilitado por ShowPieceMode)
  → grab normal, sin cambios respecto al sistema anterior
```

---

## Referencia rápida de archivos

| Archivo | Estado |
|---|---|
| `Scripts/UI/PieceThumbnailItem.cs` | ✅ Completo |
| `Scripts/UI/PieceTrayGridController.cs` | ✅ Completo |
| `Scripts/UI/PiecesPanelController.cs` | ✅ Completo |
| `Scripts/Input/InteractionManager.cs` | ✅ Completo |
| `Scripts/Gameplay/PuzzlePiece.cs` | ✅ Completo |
| `Scripts/Gameplay/PuzzleBoard.cs` | ✅ Completo |
| Prefab `PieceThumbnailButton` | ❌ Falta crear en Unity Editor |
| ScrollView en PiecesPanel | ❌ Falta crear en Unity Editor |
| Asignaciones en Inspector | ❌ Faltan asignar en Unity Editor |
