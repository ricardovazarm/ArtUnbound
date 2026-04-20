# Setup: Piece Tray ScrollView

Panel de piezas del rompecabezas basado en ScrollRect (reemplaza el sistema de paginación manual).

## Modelo de interacción

| Origen de la pieza | Cómo se activa | Mecanismo |
|---|---|---|
| **Thumbnail (bandeja)** | Near pinch, remote pinch, controlador | `IPointerDownHandler` → `InteractionManager.BeginExternalDrag()` |
| **Pieza en el board** | Near pinch físico sobre la pieza 3D | `Physics.OverlapSphere` → grab normal |

Los thumbnails **no tienen BoxCollider**. El `Image` es el hit target para `OVRRaycaster`.

---

## PASO 1 — Limpiar el PiecesPanel existente

1. En la **Hierarchy**, localiza el panel derecho del HUD. Debería llamarse algo como `PiecesPanel` o estar dentro de `RightPanel`.

2. Expande el árbol de `PiecesPanel` y **elimina** los siguientes GameObjects (clic derecho → Delete):
   - El botón de scroll arriba (↑) — probablemente `ScrollUp` o `BtnScrollUp`
   - El botón de scroll abajo (↓) — probablemente `ScrollDown` o `BtnScrollDown`
   - El texto de paginación — probablemente `PageIndicator` o similar (el que decía "1 / 1")

3. Selecciona el GameObject que tiene el componente `PiecesPanelController`.
   - En el Inspector verás que solo tiene **un campo**: `Panel`.
   - Si Unity muestra campos en amarillo/rojo de campos viejos (`Scroll Up Button`, `Scroll Down Button`, `Page Text`), compilar ya los habrá limpiado.
   - El campo `Panel` debe apuntar al GameObject raíz del PiecesPanel. Si `PiecesPanelController` está directamente en ese mismo GameObject, puedes dejarlo vacío (el script usa `gameObject` como fallback).

---

## PASO 2 — Crear el prefab `PieceThumbnailButton`

Mismo patrón que los cards de la galería: **Button → quitar hijo Text → configurar Image**.

### 2a — Crear el GameObject base

1. En la **Hierarchy**, haz clic derecho en cualquier Canvas existente → **UI → Button - TextMeshPro**.
   - Unity crea un GameObject `Button` con un hijo `Text (TMP)`.
2. Renombra el GameObject a **`PieceThumbnailButton`** (F2 o doble clic).

### 2b — Eliminar el hijo Text

1. Expande `PieceThumbnailButton` en la Hierarchy.
2. Selecciona el hijo **`Text (TMP)`** → clic derecho → **Delete**.
   - El prefab no necesita texto; la imagen del artwork ocupa todo el espacio.

### 2c — Configurar el componente Button

Con `PieceThumbnailButton` seleccionado, en el Inspector busca el componente **Button**:

| Campo | Valor |
|---|---|
| **Interactable** | ✓ activado |
| **Transition** | Color Tint |
| **Target Graphic** | Image (el del mismo GameObject — viene asignado por defecto, no cambiar) |
| **Navigation** | **None** |
| **On Click ()** | Dejar completamente **vacío** |

> **Por qué Navigation = None:** Evita que el joystick o el sistema de navegación UI salte entre thumbnails involuntariamente.

### 2d — Configurar el componente Image

En el **mismo** GameObject `PieceThumbnailButton`, busca el componente **Image**:

| Campo | Valor |
|---|---|
| **Source Image** | **None** (se asigna en runtime) |
| **Color** | Blanco — RGBA (255, 255, 255, 255) |
| **Raycast Target** | **✓ activado** |
| **Maskable** | ✓ (default) |

> **Por qué Raycast Target = ✓:** El `OVRRaycaster` necesita esto para detectar cuándo el jugador apunta al thumbnail. Sin esto, `IPointerDownHandler` nunca se dispara.

### 2e — Agregar hijo ArtworkImage (RawImage)

El thumbnail usa un `RawImage` hijo para mostrar el recorte UV del artwork sin necesitar Read/Write en la textura.

1. Con `PieceThumbnailButton` seleccionado en la Hierarchy, haz clic derecho → **UI → Raw Image**.
   - Unity crea un hijo llamado `RawImage`.
2. Renómbralo a **`ArtworkImage`**.
3. Selecciona `ArtworkImage`. En el **RectTransform**:
   - Ancla: **Alt + Stretch/Stretch** (ocupa todo el espacio del padre).
   - Left = 0, Right = 0, Top = 0, Bottom = 0.
4. En el componente **Raw Image**:
   - **Texture**: None (se asigna en runtime).
   - **Color**: Blanco — RGBA (255, 255, 255, **255**) — alpha = 1.
   - **Raycast Target**: **✗ desactivado** (el padre Image ya es el hit target).

### 2f — Agregar el script PieceThumbnailItem

1. Selecciona el GameObject raíz **`PieceThumbnailButton`** (no el hijo ArtworkImage).
2. En el Inspector haz clic en **Add Component**.
3. Busca **`PieceThumbnailItem`** (está en `ArtUnbound > UI`).
4. Agrégalo. Verás el campo serializado **Artwork Image**.
5. Arrastra el hijo **`ArtworkImage`** al campo **Artwork Image**.

> **No agregar BoxCollider** de ningún tipo. La detección es por `OVRRaycaster` sobre el `Image`, no por física.

### 2g — Guardar como prefab

1. En el **Project**, navega a `Assets/ArtUnbound/Prefabs/UI/`.
2. Arrastra `PieceThumbnailButton` desde la **Hierarchy** hasta esa carpeta.
3. Unity preguntará — elige **Original Prefab**.
4. **Borra el GameObject** de la Hierarchy (ya está guardado): clic derecho → Delete.

---

## PASO 3 — Construir el ScrollView dentro de PiecesPanel

### 3a — Crear el ScrollView

1. En la **Hierarchy**, selecciona el GameObject **`PiecesPanel`**.
2. Clic derecho sobre `PiecesPanel` → **UI → Scroll View**.
   - Unity crea automáticamente esta jerarquía **dentro** de `PiecesPanel`:
   ```
   PiecesPanel
   └─ Scroll View
       ├─ Viewport
       │   └─ Content
       ├─ Scrollbar Horizontal
       └─ Scrollbar Vertical
   ```
3. Renombra `Scroll View` a **`ScrollView`** (sin espacio).

### 3b — Posicionar y dimensionar el ScrollView

1. Selecciona `ScrollView`.
2. En el **RectTransform**, configura para que ocupe todo el espacio disponible dentro de `PiecesPanel`:
   - Haz clic en el icono de **Anchor** (arriba a la izquierda del RectTransform).
   - Mantén **Alt** presionado y selecciona **Stretch/Stretch** (estira en ambas direcciones y posiciona).
   - Left = 0, Right = 0, Top = 0, Bottom = 0.

### 3c — Configurar el componente ScrollRect

Con `ScrollView` seleccionado, en el Inspector busca **Scroll Rect**:

| Campo | Valor |
|---|---|
| **Content** | Arrastra el GameObject `Content` (hijo de Viewport) |
| **Horizontal** | **✗ desactivado** |
| **Vertical** | **✓ activado** |
| **Movement Type** | **Elastic** |
| **Elasticity** | 0.1 |
| **Inertia** | ✓ |
| **Deceleration Rate** | 0.135 |
| **Scroll Sensitivity** | 1 |
| **Viewport** | Arrastra el GameObject `Viewport` |

### 3d — Eliminar Scrollbar Horizontal

1. Selecciona **`Scrollbar Horizontal`** (hijo directo de `ScrollView`) → clic derecho → **Delete**.
2. En `ScrollView` → componente **Scroll Rect** → campo **Horizontal Scrollbar**: si quedó con referencia rota, haz clic en el círculo a la derecha y selecciona **None**.

### 3e — Configurar Scrollbar Vertical

1. Selecciona **`Scrollbar Vertical`** (hijo de `ScrollView`). No cambiar nada en el componente Scrollbar.
2. De vuelta en `ScrollView` → componente **Scroll Rect** → campo **Vertical Scrollbar Visibility**: **Auto Hide And Expand Viewport**.
   - El scrollbar aparecerá solo cuando el contenido sea más largo que el viewport.

### 3f — Configurar Viewport

1. Selecciona el GameObject **`Viewport`** (hijo de `ScrollView`).
2. Componente **Image**:
   - **Color**: Alpha = **0** (completamente transparente — solo sirve para el Mask, no debe verse).
3. Componente **Mask**:
   - **Show Mask Graphic**: **✗ desactivado**.
4. **RectTransform**: debe estar en Stretch/Stretch con offsets 0 (Unity lo configura así automáticamente).

### 3g — Configurar Content

1. Selecciona el GameObject **`Content`** (hijo de `Viewport`).
2. **RectTransform** — anchor para que se estire horizontalmente y empiece desde arriba:
   - Anchor: **Top/Stretch** (X stretch, Y top).
   - Left = 0, Right = 0, Top = 0.
   - Height = 0 (ContentSizeFitter la ajusta automáticamente).
   - Pivot: (0.5, 1) — ancla desde arriba.

3. **Add Component → Grid Layout Group**:

| Campo | Valor |
|---|---|
| **Padding** | Left=10, Right=10, Top=10, Bottom=10 _(ajusta a gusto)_ |
| **Cell Size** | X=75, Y=75 _(valor inicial — `PieceTrayGridController` lo sobreescribe en runtime)_ |
| **Spacing** | X=10, Y=10 _(ajusta a gusto, no se toca en runtime)_ |
| **Start Corner** | **Upper Left** |
| **Start Axis** | **Horizontal** |
| **Child Alignment** | **Upper Center** |
| **Constraint** | **Flexible** |

> **Por qué Flexible:** `GridLayoutGroup` con `Constraint = Flexible` y `Start Axis = Horizontal` auto-ajusta cuántas columnas caben en el ancho del panel según el `cellSize`. No se necesita fijar ni calcular en código el número de columnas. Solo el `cellSize` se recalcula en runtime (proporcional al tamaño mundial de las piezas); spacing y padding quedan fijos en el Inspector.

4. **Add Component → Content Size Fitter**:

| Campo | Valor |
|---|---|
| **Horizontal Fit** | **Unconstrained** |
| **Vertical Fit** | **Preferred Size** |

> `Preferred Size` hace que el `Content` crezca verticalmente conforme se agregan thumbnails, habilitando el scroll.

---

## PASO 4 — Agregar PieceTrayGridController al ScrollView

1. Selecciona el GameObject **`ScrollView`**.
2. **Add Component → `PieceTrayGridController`** (búscalo como "PieceTrayGrid").
3. En el Inspector verás dos campos bajo **References**:

| Campo | Valor |
|---|---|
| **Content Root** | Arrastra el GameObject **`Content`** (el que tiene GridLayoutGroup) |
| **Thumbnail Prefab** | Arrastra el prefab **`PieceThumbnailButton`** desde `Assets/ArtUnbound/Prefabs/UI/` |

No hay campos de tamaño que configurar — el tamaño de los thumbnails se deriva automáticamente de `pieceSizeM` para que coincida exactamente con el tamaño visual de las piezas en el board.

---

## PASO 5 — Asignar en PuzzleBoard

1. En la **Hierarchy**, selecciona el GameObject **`PuzzleBoard`**.
2. En el Inspector busca el campo **`Piece Tray Controller`**.
3. Arrastra el GameObject **`ScrollView`** (el que tiene `PieceTrayGridController`) a ese campo.

---

## PASO 6 — Verificar PiecesPanelController

1. Selecciona el GameObject que tiene **`PiecesPanelController`**.
2. Campo **`Panel`**:
   - Si `PiecesPanelController` está en el mismo GO que `PiecesPanel`: puede quedar **vacío**.
   - Si está en un GO diferente: arrastra el GO raíz de `PiecesPanel`.

---

## PASO 7 — Verificar InteractionManager

1. Selecciona el GameObject que tiene **`InteractionManager`**.
2. Campo **Interactable Layer**: confirma que incluye el layer donde viven las piezas del board.
   - Los thumbnails **no** necesitan estar en este layer.
   - Este layer solo afecta a las piezas 3D sobre el board.

---

## Checklist antes de hacer Play

```
Jerarquía:
  [ ] PiecesPanel NO tiene ScrollUp, ScrollDown, PageIndicator

Prefab PieceThumbnailButton:
  [ ] Tiene Button + Image + PieceThumbnailItem en el root
  [ ] Tiene hijo ArtworkImage (RawImage) con Stretch/Stretch, offsets 0
  [ ] ArtworkImage → Raw Image → Color alpha = 255, Raycast Target = ✗
  [ ] PieceThumbnailItem → campo Artwork Image apunta al hijo ArtworkImage
  [ ] NO tiene hijo Text (TMP)
  [ ] NO tiene BoxCollider
  [ ] Image (root) → Raycast Target = ✓
  [ ] Button → Navigation = None
  [ ] Button → Target Graphic apunta al Image del mismo GO
  [ ] Button → On Click () está vacío

ScrollView (dentro de PiecesPanel):
  [ ] ScrollRect → Horizontal = ✗, Vertical = ✓
  [ ] ScrollRect → Movement Type = Elastic
  [ ] ScrollRect → Viewport apunta a Viewport
  [ ] ScrollRect → Content apunta a Content
  [ ] Scrollbar Horizontal fue eliminado y el campo en ScrollRect está vacío
  [ ] Scrollbar Vertical → Visibility = Auto Hide And Expand Viewport
  [ ] Viewport → Image Alpha = 0
  [ ] Viewport → Mask → Show Mask Graphic = ✗
  [ ] Content → GridLayoutGroup configurado (Constraint = **Flexible**, Start Axis = Horizontal)
  [ ] Content → ContentSizeFitter → Vertical = Preferred Size
  [ ] ScrollView tiene PieceTrayGridController

PieceTrayGridController (en ScrollView):
  [ ] Content Root → Content (el que tiene GridLayoutGroup)
  [ ] Thumbnail Prefab → PieceThumbnailButton (el prefab del Project)

PuzzleBoard:
  [ ] Piece Tray Controller → ScrollView (el que tiene PieceTrayGridController)

PiecesPanelController:
  [ ] Panel → PiecesPanel (o vacío si está en el mismo GO)
```

---

## Flujo en runtime

| Evento | Qué pasa |
|---|---|
| `PuzzleBoard.Initialize()` | `PieceTrayGridController.Initialize()` instancia un `PieceThumbnailButton` por pieza en `Content`; `GridLayoutGroup` los posiciona; `ContentSizeFitter` ajusta la altura automáticamente |
| Player activa un thumbnail (near/remote/controller) | `OVRRaycaster` detecta el `Image` → `IPointerDownHandler.OnPointerDown` → `InteractionManager.BeginExternalDrag(piece)` → pieza aparece en mano; thumbnail se oculta |
| Player suelta cerca de un slot correcto | `PuzzleBoard.SnapPieceToSlot()` → `RemoveThumbnail(pieceId)` → thumbnail destruido |
| Player suelta lejos del board | `PuzzleBoard.ReturnPieceToTray()` → `MoveToEnd()` → thumbnail reaparece al final del grid |
| Player hace scroll en el panel | `ScrollRect + OVRRaycaster + PointableCanvas` lo manejan nativamente — sin código adicional |
| Pieza en el board | `Physics.OverlapSphere` la detecta por su `MeshCollider` → grab normal (sin cambios) |
