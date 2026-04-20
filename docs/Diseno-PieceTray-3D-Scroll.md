# Diseño: Piece Tray 3D con Scroll

## Contexto

El tray de piezas anterior usaba thumbnails 2D en un ScrollRect de Unity UI. Después de múltiples intentos fallidos de coordinar el grab desde UI con el sistema de pinch de manos, se decidió volver al sistema de piezas 3D puro, agregando scroll continuo en lugar de paginación por botones.

---

## Posición y orientación del Tray

Vista desde arriba (top-down):

```
       ------        ← Board (rotación Y = 0°, frente al usuario)
             \       ← Tray (rotación Y = -35°, angulado hacia el usuario)
             |
          Usuario
```

El tray es un **plano flat** posicionado a la derecha del board, rotado **-35° en Y** (configurable). No es una superficie curva — es un rectángulo plano inclinado. Esto hace que las piezas de los extremos queden a una distancia similar al usuario en lugar de quedar muy al costado.

**Posición del tray:** a la derecha del board, mismo centro vertical, ligeramente hacia adelante para que el usuario no tenga que estirarse.

---

## Viewport

- **Tamaño fijo:** 50 cm × 50 cm (configurable en `PuzzleConfig`)
- **Ventana visible:** el tray solo muestra las piezas que caen dentro de este rectángulo
- Las piezas fuera del viewport se desactivan (`SetActive(false)`) para rendimiento

---

## Grid Layout

Las piezas se acomodan en una cuadrícula de **N columnas × M renglones** dentro del viewport.

### Tamaño de celda
```
cellSize = pieceWorldSizeM * 1.2f   // pieza + margen del 20%
```

### Columnas
```
columns = Mathf.FloorToInt(viewportWidth / cellSize)
```

### Renglones visibles
```
visibleRows = Mathf.FloorToInt(viewportHeight / cellSize)
```

**Relación pieza-renglones visibles** (ejemplo con viewport 50 cm):

| Dificultad | Piezas | Tamaño pieza | Columnas | Renglones visibles |
|---|---|---|---|---|
| Fácil      | 64     | 8 cm         | 6        | 6                  |
| Normal     | 144    | 6 cm         | 8        | 8                  |
| Difícil    | 256    | 5 cm         | 10       | 10                 |
| Experto    | 512    | 3.5 cm       | 14       | 14                 |

### Posición local de cada pieza
```
localX = (col - columns / 2f + 0.5f) * cellSize
localY = -row * cellSize + scrollOffset
localZ = 0
```

---

## Sistema de Scroll

### Variable central
```
float targetScrollOffset   // actualizado por input
float currentScrollOffset  // lerp suave hacia target cada frame
```

### Límites
```
totalRows   = Mathf.CeilToInt(totalPieces / (float)columns)
maxScroll   = Mathf.Max(0, (totalRows - visibleRows) * cellSize)
targetScrollOffset = Mathf.Clamp(targetScrollOffset, 0, maxScroll)
```

### Activación por viewport
Una pieza está visible si su `localY` calculada cae dentro de la ventana:
```
float pieceY = -row * cellSize + currentScrollOffset
bool inViewport = pieceY > -viewportHeight / 2f - cellSize
               && pieceY <  viewportHeight / 2f + cellSize
piece.SetActive(inViewport && piece.CurrentState == PieceState.InPool)
```

### Suavidad
```csharp
currentScrollOffset = Mathf.Lerp(currentScrollOffset, targetScrollOffset, Time.deltaTime * 10f)
```

---

## Flujos de Interacción Completos

---

### 🖐 MANOS — Scroll del Tray

1. El dedo índice entra al **trigger collider del tray** (volumen invisible que cubre el viewport)
2. Se registra la posición Y inicial del index tip
3. Mientras el índice se mueve **sin pinch activo**:
   - Delta Y negativo (mano sube) → `ScrollBy(+delta)` → piezas suben → aparecen renglones de abajo
   - Delta Y positivo (mano baja) → `ScrollBy(-delta)` → piezas bajan → aparecen renglones de arriba
4. Al salir el índice del collider → se detiene el tracking del scroll

**Condición clave:** el scroll solo se activa si `HTPC.IsPinching == false`. Si hay pinch activo, el input se ignora para scroll (es un grab, no scroll).

---

### 🎮 CONTROLLER — Scroll del Tray

1. El usuario mueve el **thumbstick Y** del controller derecho
2. Cada frame: `ScrollBy(thumbstick.y * scrollSpeed * Time.deltaTime)`
   - Thumbstick arriba → piezas suben → renglones de abajo aparecen
   - Thumbstick abajo → piezas bajan → renglones de arriba aparecen
3. El scroll ocurre sin importar hacia dónde apunte el ray

**`trayScrollSpeed`** configurable en `PuzzleConfig` (recomendado: 0.3 m/s). Aplica tanto para thumbstick como para index tip.

---

### 🖐 MANOS — Grab desde Tray

1. Usuario hace **pinch** (pulgar + índice < 3.5 cm)
2. `HTPC.OnPinchStart` → `InteractionManager.HandlePinchStart`
3. Sphere overlap (radio 4 cm) busca el `PuzzlePiece` más cercano en estado `InPool`
4. Pieza encontrada → `SetDragged(true)` → `PieceState.Grabbed` → mesh 3D habilitado
5. En cada frame → `HandlePinchHold` → pieza sigue al midpoint pulgar-índice
6. Slot highlight aparece cuando la pieza se acerca al board

---

### 🎮 CONTROLLER — Grab desde Tray

1. Ray del controller apunta a una pieza del tray
2. Usuario presiona **Trigger**
3. `HTPC.OnPinchStart` (trigger mapeado como pinch) → `HandlePinchStart`
4. Ray hit o sphere overlap encuentra la pieza → `SetDragged(true)`
5. En cada frame → `HandlePinchHold` → pieza sigue al controller
6. Slot highlight aparece cuando la pieza se acerca al board

---

### 🖐 MANOS — Snap al Board

1. Pieza en mano, usuario la acerca a un slot del board
2. `HandlePinchHold` → `board.UpdateSlotHighlight(piecePosition)` → slot más cercano se ilumina
3. Usuario **suelta el pinch**
4. `HandlePinchEnd`:
   - Si pieza está dentro de `snapDistanceCm` del slot → **snap** ✓ → pieza queda en board
   - Si pieza está fuera → pieza regresa a **su posición original** en el tray (no al final)

---

### 🎮 CONTROLLER — Snap al Board

1. Pieza en mano siguiendo al controller, se acerca al board
2. `HandlePinchHold` → highlight del slot más cercano
3. Usuario **suelta el Trigger**
4. `HandlePinchEnd`:
   - Dentro de snapDistance → **snap** ✓
   - Fuera → pieza regresa a su posición original en el tray

---

### 🖐 MANOS — Grab desde Board (pieza ya colocada, no bloqueada)

1. Usuario hace **pinch** sobre una pieza del board (`PieceState.Placed`, no `Locked`)
2. `HandlePinchStart` → sphere overlap detecta la pieza → `board.RemovePieceFromSlot(piece)`
3. Flag `pieceWasFromBoard = true`
4. Pieza sigue a la mano
5. Al soltar:
   - Cerca de un slot → snap a nuevo slot ✓
   - Lejos del board → pieza va al **final del tray** (se mueve de posición)

---

### 🎮 CONTROLLER — Grab desde Board

1. Ray apunta a pieza del board + **Trigger**
2. `HandlePinchStart` → misma lógica que manos
3. Al soltar Trigger:
   - Cerca de slot → snap ✓
   - Lejos → pieza va al final del tray

---

## Diferencia clave: origen de la pieza al no snapear

| Origen de la pieza | Al no snapear va a... |
|---|---|
| Desde tray (InPool) | Su **posición original** en el grid del tray |
| Desde board (Placed) | Al **final del tray** (último renglón) |

---

## Componentes a implementar

### Nuevos

| Script | Responsabilidad |
|---|---|
| `PieceTray3DController` | Posiciona piezas en grid, maneja scroll offset, activa/desactiva piezas por viewport |
| `TrayScrollInputHandler` | Lee index tip (manos) y thumbstick Y (controller), llama `ScrollBy()` |

### Modificados

| Script | Cambio |
|---|---|
| `HandTrackingInputController` | Agregar `public bool IsPinching` y evento `OnScrollVertical(float)` para thumbstick Y |
| `PuzzleBoard` | Reemplazar `PieceScrollController` con `PieceTray3DController`; posicionar tray con rotación Y = -35° |
| `PuzzleConfig` | Agregar: `trayViewportSizeCm`, `trayRotationY`, `trayScrollSpeed` |
| `InteractionManager` | Sin cambios en grab/hold/end — solo eliminar referencias a thumbnails |

### Eliminados

| Script | Razón |
|---|---|
| `PieceTray2DController` / `PieceTrayGridController` | Reemplazado por sistema 3D |
| `PieceThumbnailItem` | Ya no hay thumbnails 2D |
| `BeginExternalDrag` en InteractionManager | Ya no se necesita (grab solo por pinch físico) |

---

## Parámetros configurables (en PuzzleConfig)

| Parámetro | Valor por defecto | Descripción |
|---|---|---|
| `trayViewportSizeCm` | 50 × 50 cm | Tamaño del área visible del tray |
| `trayRotationY` | -35° | Ángulo de rotación del tray en Y |
| `trayScrollSpeed` | 0.3 m/s | Velocidad de scroll con thumbstick |
| `trayScrollSmoothing` | 10f | Factor del lerp de suavizado |
| `trayCellMargin` | 1.2f | Multiplicador de margen entre piezas |

---

## Configuración en el Editor de Unity

Ejecutar estos pasos en orden después de que el código esté implementado.

---

### Paso 1 — PuzzleConfig (ScriptableObject)

1. En el Project, localizar `Assets/ArtUnbound/Data/PuzzleConfig.asset`
2. Seleccionarlo y en el Inspector asignar los nuevos campos:
   - **Tray Viewport Size Cm:** `50`
   - **Tray Rotation Y:** `-35`
   - **Tray Scroll Speed:** `0.3`
   - **Tray Scroll Smoothing:** `10`
   - **Tray Cell Margin:** `1.2`

---

### Paso 2 — Crear el GameObject del Tray en la escena

1. En la Hierarchy, expandir el GameObject del **Board** (el que tiene `PuzzleBoard`)
2. Crear un **GameObject vacío** hijo del Board → nombrarlo `PieceTray`
3. En el Transform del `PieceTray`:
   - **Position:** `(0.6, 0, 0)` ← a la derecha del board; ajustar según tamaño del board
   - **Rotation:** `(0, -35, 0)` ← el ángulo configurable
   - **Scale:** `(1, 1, 1)`

> El valor exacto de Position X depende del ancho del board. La regla es: borde derecho del board + 0.05 m de separación + mitad del viewport (0.25 m) = posición X del tray.

---

### Paso 3 — Agregar PieceTray3DController

> Script ubicado en: `Assets/ArtUnbound/Scripts/UI/PieceTray3DController.cs`

1. Seleccionar el GameObject `PieceTray`
2. **Add Component → PieceTray3DController**
3. En el Inspector asignar:
   - **Puzzle Config:** arrastrar `PuzzleConfig.asset`
   - (Las piezas se asignan en runtime desde `PuzzleBoard`)

---

### Paso 4 — Detección del área del tray (sin collider)

No se necesita agregar ningún Box Collider. `TrayScrollInputHandler` detecta si el index tip está dentro del área del tray haciendo un **bounds check por código** cada frame:

```
localPos = InverseTransformPoint(indexTip.position)
inside = |localX| < viewportSize/2  &&  |localY| < viewportSize/2  &&  |localZ| < 12cm
```

No hay campo "Tray Trigger Collider" en el Inspector — la detección es automática basada en el tamaño configurado en `PuzzleConfig.trayViewportSizeCm`.

---

### Paso 5 — Agregar TrayScrollInputHandler

> Script ubicado en: `Assets/ArtUnbound/Scripts/Input/TrayScrollInputHandler.cs`

1. Seleccionar el GameObject `PieceTray`
2. **Add Component → TrayScrollInputHandler**
3. En el Inspector asignar:
   - **Tray Controller:** el componente `PieceTray3DController` del mismo GameObject
   - **Hand Input Controller:** el `HandTrackingInputController` de la escena
   - **Index Tip Transform:** si quieres scroll con manos, crear un GameObject vacío → Add Component → `IndexTipFollower` (en `Scripts/Input/`) → Handedness: Right → arrastrar ese GameObject aquí. Si lo dejas vacío, solo funciona el scroll con thumbstick.
   - **Puzzle Config:** `PuzzleConfig.asset`

---

### Paso 6 — HandTrackingInputController

No requiere ninguna configuración en el Inspector. La propiedad `IsPinching` es código C# interno — `TrayScrollInputHandler` la usa automáticamente para pausar el scroll mientras el usuario hace pinch.

---

### Paso 7 — PuzzleBoard

1. Seleccionar el GameObject del **Board**
2. En el componente `PuzzleBoard`, en el Inspector:
   - Asignar el campo **Piece Tray Controller:** arrastrar el componente `PieceTray3DController` del `PieceTray`
   - Eliminar o dejar vacío el campo del antiguo `PieceScrollController` si existe

---

### Paso 8 — Limpiar objetos obsoletos de la escena

Eliminar o desactivar los siguientes GameObjects/componentes que ya no se usan:

| Qué eliminar | Dónde está |
|---|---|
| GameObject con `PieceTrayGridController` | Hijo del Canvas o del Board |
| Canvas del tray 2D (si existe separado) | Hierarchy |
| Prefab `PieceThumbnailButton` en la escena | Hijo del ScrollRect |
| Componente `PieceScrollController` | Hijo del Board |

> No eliminar el Prefab `PieceThumbnailButton` del Project todavía — solo los instances en la escena.

---

### Paso 9 — Verificar capas (Layers)

Las piezas 3D deben estar en la capa asignada al **`interactableLayer`** del `InteractionManager`. No se necesita ninguna capa especial para el tray — no hay collider de tray que pueda interferir con el sphere overlap.

---

### Paso 10 — Play Mode: verificar en Editor

1. Presionar **Play**
2. En la Hierarchy, expandir `PieceTray` — deben aparecer los GameObjects de las piezas como hijos
3. En el Inspector del `PieceTray3DController` verificar:
   - **Columns:** debe mostrar el número calculado (ej. 10 para dificultad Normal)
   - **Visible Rows:** debe mostrar los renglones visibles
   - **Max Scroll:** debe ser mayor a 0 si hay más renglones que los visibles
4. En el panel **Scene**, mover el slider `Current Scroll Offset` manualmente para verificar que las piezas se mueven y las que salen del viewport se desactivan

---

### Paso 11 — Build y prueba en Quest

1. **File → Build Settings → Build** con el Quest conectado
2. Verificar en dispositivo:
   - Scroll con manos: index tip entra al tray → mover arriba/abajo → piezas se mueven suavemente y hacen clamp en los extremos
   - Scroll con thumbstick: thumbstick Y → mismo comportamiento
   - Grab: pinch sobre una pieza visible → pieza sigue al midpoint pulgar-índice
   - Snap: acercar pieza al board → highlight → soltar → snap o regresa al tray
