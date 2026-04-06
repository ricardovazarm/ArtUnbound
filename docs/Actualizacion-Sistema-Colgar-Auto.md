# Actualización: Sistema de Colgar Cuadros - Activación Automática

## Cambios Recientes (18 Mar 2026)

### ❌ Eliminado
- **Botón "Colgar Cuadro"**: Ya no existe un botón físico en el PostGameController

### ✅ Nuevo Comportamiento

#### 1. Activación Automática
Cuando se muestra el `PostGameController` Y se detectaron paredes (`wallCount > 0`):
- El sistema de colgar cuadros se **activa automáticamente**
- El jugador puede inmediatamente hacer pinch en el cuadro para agarrarlo
- Ya no necesita hacer clic en ningún botón

#### 2. Texto de Instrucción Externo
Se agregó una nueva variable al `PostGameController`:
- **`hangInstructionText`** (GameObject): Referencia al texto "Pinch the paint to place it on the wall."
- Este texto está ubicado en el **centro de la escena** (sobre el cuadro), NO en el panel derecho
- Se activa/desactiva automáticamente junto con el PostGameController
- Solo aparece si se detectaron paredes

## Configuración Requerida

### En el Inspector del PostGameController

1. Localiza el GameObject del `PostGameController` en la jerarquía
2. En la sección **"Hang Artwork Instruction"**:
   - **Hang Instruction Text**: Arrastra aquí el GameObject que contiene el texto "Pinch the paint to place it on the wall."
   
**Características del texto:**
- Debe ser un GameObject con un componente `TextMeshProUGUI` o `Text`
- Ubicado en el centro de la escena, flotando sobre el cuadro
- Se recomienda usar World Space Canvas para que aparezca en 3D
- El PostGameController se encargará de activarlo/desactivarlo automáticamente

## Flujo Actualizado

### Cuando se completa un puzzle:
```
1. Usuario completa el último piece
2. PuzzleBoard.OnPuzzleComplete() → se dispara
3. GameBootstrap.OnPuzzleComplete() → ejecuta:
   - Detiene el timer
   - Calcula score y frame tier
   - Detecta paredes con WallDetectionService
   - Muestra PostGameController con wallCount
4. PostGameController.ShowResults(wallCount):
   - Muestra panel derecho con resultados
   - SI wallCount > 0:
     ✅ Activa el texto de instrucción "Pinch the paint..."
     ✅ Invoca automáticamente OnPlaceArtworkRequested
   - SI wallCount = 0:
     ❌ NO activa el texto
     ❌ NO habilita el modo de colgar
5. GameBootstrap.OnPlaceArtworkRequested():
   - Inicializa ArtworkHangingController
   - Habilita la interacción con el frame
   - El usuario YA puede hacer pinch para agarrar el cuadro
```

### Cuando se entra a un puzzle ya completado:
```
1. Usuario selecciona puzzle completado desde el menú
2. GameBootstrap.InitializePuzzleBoard():
   - Detecta displaySession.isCompleted == true
   - Salta al estado PostGame directamente
   - Restaura el board con todas las piezas colocadas
3. PostGameController.ShowResults(wallCount):
   - Muestra panel derecho con datos guardados
   - SI wallCount > 0:
     ✅ Activa el texto de instrucción
     ✅ Activa automáticamente el modo de colgar
   - SI wallCount = 0:
     ❌ NO activa funcionalidad de colgar
4. El usuario puede inmediatamente hacer pinch en el cuadro
```

### Agarrar y colocar:
```
1. Usuario hace pinch cerca del frame (< 10cm por defecto)
2. Frame se adjunta a la mano
3. Paneles UI se ocultan (incluyendo el texto de instrucción)
4. Aparece ghost preview en paredes
5. Usuario mueve la mano hacia una pared
6. Ghost preview se vuelve verde (válido) o rojo (inválido)
7. Usuario suelta el pinch:
   - Válido → Animación hacia pared + crear anchor
   - Inválido → Cancela y vuelve a mostrar PostGameController
```

## Logs de Diagnóstico

### Al mostrar PostGameController:
```
[WallDetection] Found 7 wall(s) in the room. (Total planes: 7)
[PostGame] Hang instruction text active: True (walls: 7)
[PostGame] Walls detected (7), automatically enabling artwork hanging mode
[GameBootstrap] Started artwork hanging flow for bedroom_van_gogh
```

### Si NO hay paredes:
```
[WallDetection] Found 0 wall(s) in the room. (Total planes: 0)
[PostGame] Hang instruction text active: False (walls: 0)
[PostGame] No walls detected, artwork hanging mode NOT enabled
```

### Al agarrar el frame:
```
[HandTracking] Pinch START at (X.XX, Y.YY, Z.ZZ)
[ArtworkHanging] OnPinchStart - State: Idle, Distance: 0.XXXm
[ArtworkHanging] Frame is within grab distance - attempting grab
[ArtworkHanging] Frame grabbed successfully, state changed to Grabbed
[GameBootstrap] Frame grabbed - UI hidden, placement mode active
```

## Cambios en el Código

### PostGameController.cs

```csharp
[Header("Hang Artwork Instruction")]
[Tooltip("Text that appears above the painting. Located in a separate panel (center), not in PostGame panel (right).")]
[SerializeField] private GameObject hangInstructionText;
```

**`ShowResults()` - Ahora activa automáticamente el modo de colgar:**
```csharp
public void ShowResults(...)
{
    // ... código existente ...
    
    UpdateUI();
    Show();
    
    // ✅ NUEVO: Activación automática
    if (lastWallCount > 0)
    {
        Debug.Log($"[PostGame] Walls detected ({lastWallCount}), automatically enabling artwork hanging mode");
        OnPlaceArtworkRequested?.Invoke();
    }
    else
    {
        Debug.Log("[PostGame] No walls detected, artwork hanging mode NOT enabled");
    }
}
```

**`UpdateUI()` - Controla el texto de instrucción:**
```csharp
private void UpdateUI()
{
    // ... código existente ...
    
    // ✅ NUEVO: Show/hide hang instruction text
    if (hangInstructionText != null)
    {
        bool hasWalls = lastWallCount > 0;
        hangInstructionText.SetActive(hasWalls);
        Debug.Log($"[PostGame] Hang instruction text active: {hasWalls}");
    }
}
```

**`Hide()` - Oculta también el texto:**
```csharp
public void Hide()
{
    if (panel != null)
        panel.SetActive(false);
    else
        gameObject.SetActive(false);
    
    // ✅ NUEVO: Hide instruction text
    if (hangInstructionText != null)
        hangInstructionText.SetActive(false);
}
```

## Troubleshooting

### El texto "Pinch the paint..." no aparece
**Causa**: No está asignado en el Inspector
**Solución**: 
1. Crea un GameObject con TextMeshProUGUI en el centro de la escena
2. Asígnalo al campo `Hang Instruction Text` del PostGameController
3. El texto debe estar inicialmente desactivado en la jerarquía

### El sistema no se activa automáticamente
**Causa**: No se detectaron paredes (`wallCount = 0`)
**Solución**:
1. Verifica que Space Setup esté configurado en Meta Quest Link
2. Revisa que los permisos de scene data estén habilitados en XR Plugin Management
3. Busca en logs: `[WallDetection] Found X wall(s)`

### El frame no se puede agarrar
**Causa**: La distancia de detección es muy pequeña
**Solución**:
1. Aumenta `Grab Detection Radius` de 0.1 a 0.3 en el Inspector
2. Verifica que el frame tenga un BoxCollider
3. Revisa logs de distancia: `[ArtworkHanging] Distance to frame: X.XXXm`

## Ejemplo de Configuración del Texto

### Opción 1: World Space Canvas (Recomendado)
```
GameObject: HangInstructionCanvas
├─ Canvas (Render Mode: World Space)
│  ├─ Width: 800
│  ├─ Height: 200
│  ├─ Position: (0, 0.3, 0) // 30cm arriba del cuadro
│  └─ Scale: (0.001, 0.001, 0.001)
└─ TextMeshProUGUI: "Pinch the paint to place it on the wall."
   ├─ Font Size: 48
   ├─ Alignment: Center
   ├─ Color: Blanco con transparencia (1, 1, 1, 0.9)
   └─ Auto Size: Habilitado
```

### Opción 2: GameObject simple con TextMeshPro
```
GameObject: HangInstructionText
├─ RectTransform
│  ├─ Position: Relativo al cuadro en la escena
│  └─ Anchors: Center
└─ TextMeshProUGUI
   ├─ Text: "Pinch the paint to place it on the wall."
   ├─ Font: Inter-Regular (o tu fuente preferida)
   ├─ Size: 36
   └─ Color: Blanco brillante
```

## Resumen de Cambios

| Antes | Ahora |
|-------|-------|
| Botón "Colgar Cuadro" en PostGame panel | ❌ Eliminado |
| Usuario hace clic en botón | ✅ Activación automática al mostrar panel + paredes detectadas |
| Sin instrucción visible | ✅ Texto "Pinch the paint..." aparece automáticamente |
| Texto en panel derecho | ✅ Texto en centro de escena (sobre el cuadro) |
| Requiere 2 acciones (clic + pinch) | ✅ Requiere 1 acción (solo pinch) |
