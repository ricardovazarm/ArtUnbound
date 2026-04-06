# Diagnóstico: Sistema de Colgar Cuadros

## Problema Reportado (Resuelto)
El usuario intentaba hacer pinch en el cuadro completado pero no se activaba el modo de agarre.

## Causa Raíz Identificada (Resuelto)
El botón "Colgar Cuadro" en el `PostGameController` estaba desactivado por defecto. 

**Solución implementada**: Se eliminó el botón y ahora el sistema se **activa automáticamente** cuando se muestra el PostGameController y se detectan paredes.

## Cambios Realizados

### 1. Activación automática del sistema (PostGameController.cs)

**Antes:**
- Usuario debía hacer clic en un botón "Colgar Cuadro" para activar el modo

**Ahora en `ShowResults()`:**
```csharp
// Automatically enable artwork hanging mode if walls were detected
if (lastWallCount > 0)
{
    Debug.Log($"[PostGame] Walls detected ({lastWallCount}), automatically enabling artwork hanging mode");
    OnPlaceArtworkRequested?.Invoke();
}
else
{
    Debug.Log("[PostGame] No walls detected, artwork hanging mode NOT enabled");
}
```

### 2. Nuevo campo para texto de instrucción

```csharp
[Header("Hang Artwork Instruction")]
[Tooltip("Text that appears above the painting with instruction to pinch and place. Located in a separate panel (center), not in the PostGame panel (right).")]
[SerializeField] private GameObject hangInstructionText;  // "Pinch the paint to place it on the wall."
```

Este texto:
- Se encuentra en el **centro de la escena** (sobre el cuadro)
- NO está en el panel derecho del PostGameController
- Se activa/desactiva automáticamente cuando se muestra/oculta el PostGameController
- Solo aparece si `wallCount > 0`

### 3. Control del texto en `UpdateUI()`:
```csharp
// Show/hide hang instruction text based on wall detection
if (hangInstructionText != null)
{
    bool hasWalls = lastWallCount > 0;
    hangInstructionText.SetActive(hasWalls);
    Debug.Log($"[PostGame] Hang instruction text active: {hasWalls} (walls: {lastWallCount})");
}
```

### 2. Logs de diagnóstico agregados

#### `ArtworkHangingController.cs`
- `EnableFrameGrab()`: Log cuando se habilita el agarre, confirmación de frame encontrado, collider añadido, y suscripción a eventos
- `OnPinchStart()`: Log de distancia al frame, umbral, y decisión de agarre
- `GrabFrame()`: Log detallado de cada paso (hand transform, attachment, placement detector)

#### `PostGameController.cs`
- `UpdateUI()`: Log de activación del botón "Colgar Cuadro"
- `OnPlaceArtworkClicked()`: Log cuando se hace clic en el botón

## Flujo Completo Esperado

### Cuando se completa un puzzle:
1. `GameBootstrap.OnPuzzleComplete()` → se llama
2. `PostGameController.ShowResults(wallCount)` → se muestra con `wallCount=7`
3. **AUTOMÁTICAMENTE** si `wallCount > 0`:
   - Se invoca `OnPlaceArtworkRequested`
   - Se activa el texto de instrucción "Pinch the paint to place it on the wall."
   - `GameBootstrap.OnPlaceArtworkRequested()` → inicializa `ArtworkHangingController`

### Cuando se entra a un puzzle ya completado:
1. `GameBootstrap.InitializePuzzleBoard()` → detecta que `displaySession.isCompleted == true`
2. `PostGameController.ShowResults(wallCount)` → se muestra con `wallCount=7`
3. **AUTOMÁTICAMENTE** si `wallCount > 0`:
   - Se invoca `OnPlaceArtworkRequested`
   - Se activa el texto de instrucción
   - Se habilita el modo de colgar cuadros

### Sistema activado - Usuario puede agarrar el cuadro:
1. `ArtworkHangingController.EnableFrameGrab()` → Log: "[ArtworkHanging] EnableFrameGrab called for {artworkId}, tier: {frameTier}"
2. Se añade collider al frame → Log: "[ArtworkHanging] Added BoxCollider to frame for pinch detection"
3. Se suscribe a eventos de pinch → Log: "[ArtworkHanging] Subscribed to pinch events"

### Cuando el usuario hace pinch cerca del frame:
1. `HandTrackingInputController` → Log: "[HandTracking] Pinch START at ..."
2. `ArtworkHangingController.OnPinchStart()` → Log: "[ArtworkHanging] OnPinchStart - State: Idle, Distance: X.XXXm"
3. Si está dentro del umbral (0.1m) → `GrabFrame()` → Log: "[ArtworkHanging] Frame is within grab distance"
4. Frame se adjunta a la mano → Log: "[ArtworkHanging] Frame attached to hand"
5. Se activa detector de pared → Log: "[ArtworkHanging] Started wall placement detection"
6. `GameBootstrap.OnFrameGrabbed()` → Log: "[GameBootstrap] Frame grabbed - UI hidden, placement mode active"

## Pasos de Prueba

### 1. Entra al puzzle "The Bedroom" (ya completado)
**Logs esperados:**
```
[WallDetection] Found 7 wall(s) in the room. (Total planes: 7)
[PostGame] Hang instruction text active: True (walls: 7)
[PostGame] Walls detected (7), automatically enabling artwork hanging mode
[GameBootstrap] Started artwork hanging flow for bedroom_van_gogh
[ArtworkHanging] EnableFrameGrab called for bedroom_van_gogh, tier: Bronce
[ArtworkHanging] Found completed frame: SlotRoot
[ArtworkHanging] Added BoxCollider to frame for pinch detection
[ArtworkHanging] Subscribed to pinch events
[ArtworkHanging] Enabled frame grab for bedroom_van_gogh (Tier: Bronce), State: Idle
```

### 2. Verifica que el texto de instrucción aparezca
- Debe aparecer el texto "Pinch the paint to place it on the wall." en el centro de la escena (sobre el cuadro)
- Si no aparece, verifica que esté asignado en el Inspector del PostGameController
- Si no aparece, revisa el log: si `wallCount = 0`, el texto no se activará

### 3. El sistema ya está activo - No hay botón que presionar
- ✅ Ya puedes hacer pinch directamente en el cuadro
- ❌ NO hay botón "Colgar Cuadro" que presionar
**Logs esperados:**
```
[HandTracking] Pinch START at (X.XX, Y.YY, Z.ZZ) (Palm: (X.XX, Y.YY, Z.ZZ))
[ArtworkHanging] OnPinchStart - State: Idle, FramePos: (X.XX, Y.YY, Z.ZZ), PinchPos: (X.XX, Y.YY, Z.ZZ)
[ArtworkHanging] Distance to frame: 0.XXXm (threshold: 0.100m)
```

**Si la distancia es menor a 0.1m:**
```
[ArtworkHanging] Frame is within grab distance - attempting grab
[ArtworkHanging] Hand transform: XR Origin (Mobile) at (X.XX, Y.YY, Z.ZZ)
[ArtworkHanging] handAttachment.Attach() called
[ArtworkHanging] placementDetector.StartDetection() called
[ArtworkHanging] Frame grabbed successfully, state changed to Grabbed
[GameBootstrap] Frame grabbed - UI hidden, placement mode active
```

**Si la distancia es mayor a 0.1m:**
```
[ArtworkHanging] Frame is too far to grab
```

### 4. Acércate al cuadro e intenta hacer pinch sobre él
El umbral de agarre es 0.1m (10 cm). Opciones:
- **Aumentar el umbral** a 0.3m (30 cm) para facilitar el agarre
- **Agregar visualización debug** para ver el collider del frame
- **Verificar la posición del frame** vs la posición del pinch

### 5. Si "Frame is too far to grab"

### `ArtworkHangingController` (Inspector)
- **Grab Detection Radius**: 0.1m por defecto (puede aumentarse a 0.3m)
- **Placement Animation Duration**: 0.5s
- **Referencias**: handInput, handAttachment, placementDetector, anchorManager, puzzleBoard

### `PuzzleBoard.EnableFrameInteraction()`
- **BoxCollider size**: `(0.5f, 0.5f, 0.05f)` por defecto

## Posibles Problemas Adicionales

### Si el texto de instrucción no aparece:
- Verificar que el GameObject esté asignado en el Inspector del `PostGameController`
- Verificar que `wallCount > 0` en los logs
- Verificar que el GameObject tenga un componente de texto (TextMeshProUGUI o Text)

### Si el sistema no se activa automáticamente:
- Verificar que `wallCount > 0` en los logs
- Verificar que `OnPlaceArtworkRequested` se esté invocando (buscar "[PostGame] Walls detected")
- Verificar que `GameBootstrap` esté suscrito al evento `OnPlaceArtworkRequested`

### Si el botón aparece pero no funciona:
- ❌ **Ya NO hay botón** - El sistema se activa automáticamente

### Si el pinch no se detecta:
- Verificar que `handInput != null` en los logs
- Verificar que `OnPinchStart` event se esté disparando (buscar "[HandTracking] Pinch START")
- Verificar la distancia reportada vs el threshold

### Si el frame no se adjunta a la mano:
- Verificar que `handAttachment != null`
- Verificar que `handInput.TrackedObject != null`
- Verificar que `completedFrame != null`

## Próximos Pasos si el Problema Persiste

1. **Verificar configuración en Unity Inspector:**
   - Abrir escena `Main.unity`
   - Seleccionar `GameBootstrap` GameObject
   - Verificar que todos los campos estén asignados:
     - Artwork Hanging Controller
     - Wall Anchor Manager
     - Puzzle Board
     - Post Game Controller
     - Haptic Controller
   - Seleccionar `PostGameController` GameObject
   - Verificar que `Hang Instruction Text` esté asignado

2. **Verificar componentes en ArtworkHangingController:**
   - Hand Tracking Input Controller
   - Hand Attachment Controller (en el mismo GameObject)
   - Wall Placement Detector (en el mismo GameObject)
   - Wall Anchor Manager
   - Puzzle Board

3. **Aumentar el radio de detección:**
   - En el Inspector del `ArtworkHangingController`
   - Cambiar `Grab Detection Radius` de 0.1 a 0.3

4. **Crear el texto de instrucción si no existe:**
   - Crear un GameObject con TextMeshProUGUI en el centro de la escena
   - Posicionarlo sobre el cuadro (ej: Y = +0.3m)
   - Texto: "Pinch the paint to place it on the wall."
   - Asignarlo al campo `Hang Instruction Text` del PostGameController
   - Dejarlo desactivado en la jerarquía inicial
