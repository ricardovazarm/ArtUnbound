# 📋 PLAN DE IMPLEMENTACIÓN: Sistema de Colocación de Cuadros en Paredes

## 🎯 **Objetivo:**
Permitir al usuario tomar el cuadro completado con un Pinch, que se pegue a su mano, ocultar los paneles de UI, moverse por el cuarto, y colocar el cuadro en una pared donde quedará anclado permanentemente.

---

## 🏗️ **ARQUITECTURA PROPUESTA:**

```
┌─────────────────────────────────────────────────────┐
│  GameBootstrap (Orquestador)                        │
│  └─ Estado: PostGame                                │
│     └─ Detecta Pinch en Frame                       │
│        └─ Activa ArtworkHangingController           │
└─────────────────────────────────────────────────────┘
                          │
          ┌───────────────┴───────────────┐
          │                               │
┌─────────▼──────────────┐  ┌────────────▼────────────┐
│ ArtworkHangingController│  │ HandAttachmentController│
│ (Lógica de colocación)  │  │ (Marco sigue la mano)   │
│                         │  │                         │
│ ├─ Estados:             │  │ ├─ Attach to hand       │
│ │  ├─ Idle              │  │ ├─ Smooth follow        │
│ │  ├─ Grabbed           │  │ ├─ Rotation offset      │
│ │  ├─ Placing           │  │ └─ Scale adjustment     │
│ │  └─ Placed            │  └─────────────────────────┘
│ ├─ Wall raycast         │
│ ├─ Preview ghost        │
│ └─ Confirm placement    │
└─────────────────────────┘
          │
┌─────────▼──────────────┐
│ WallAnchorManager      │
│ (Persistencia)         │
│                        │
│ ├─ Create AR Anchor    │
│ ├─ Save to disk        │
│ ├─ Load on startup     │
│ └─ Spawn artwork       │
└────────────────────────┘
```

---

## 📦 **COMPONENTES A CREAR:**

### **1. `ArtworkHangingController.cs`** (NUEVO)
**Responsabilidad:** Orquestador principal del flujo de colocación

**Estados del sistema:**
```cs
enum HangingState {
    Idle,          // Esperando que el usuario tome el marco
    Grabbed,       // Marco pegado a la mano
    Previewing,    // Mostrando preview en pared
    Placing,       // Animación de colocación
    Placed         // Anclado permanentemente
}
```

**Funcionalidad:**
- ✅ Detectar Pinch sobre el marco completado
- ✅ Transición de estados
- ✅ Coordinar `HandAttachmentController` y `WallAnchorManager`
- ✅ Ocultar/mostrar UI según el estado
- ✅ Feedback visual (highlight, ghost, animaciones)

**Ubicación:** `Assets/ArtUnbound/Scripts/MR/ArtworkHangingController.cs`

---

### **2. `HandAttachmentController.cs`** (NUEVO)
**Responsabilidad:** Hacer que el marco siga la mano del usuario

**Funcionalidad:**
- ✅ Attach al transform de la mano (usar `HandTrackingInputController`)
- ✅ Smooth follow con `Vector3.Lerp()` / `Quaternion.Slerp()`
- ✅ Offset configurable (distancia de la palma)
- ✅ Rotación ergonómica (el cuadro mira hacia adelante, no hacia la mano)
- ✅ Escala opcional (el usuario puede ver el cuadro más chico mientras lo mueve)
- ✅ Detach cuando se coloca

**Ubicación:** `Assets/ArtUnbound/Scripts/MR/HandAttachmentController.cs`

---

### **3. `WallAnchorManager.cs`** (NUEVO - reemplaza `AnchorPersistenceController`)
**Responsabilidad:** Gestionar anclajes espaciales persistentes

**Funcionalidad:**
- ✅ `CreateAnchor(artworkId, position, rotation)` → Crear AR Anchor
- ✅ `SaveAnchorData()` → Persistir a disco (usar `SaveDataService`)
- ✅ `LoadAnchors()` → Cargar al iniciar el juego
- ✅ `SpawnArtwork(anchorData)` → Instanciar cuadros guardados
- ✅ Usar `ARAnchorManager` de AR Foundation
- ✅ Integración con Meta Spatial Anchors (persistencia entre sesiones)

**Estructura de datos:**
```cs
[Serializable]
public class AnchoredArtwork {
    public string artworkId;
    public string anchorId;        // UUID del AR Anchor
    public Vector3 localPosition;  // Por si el anchor se mueve
    public Quaternion localRotation;
    public float scale;
    public FrameTier frameTier;
    public DateTime placedDate;
}
```

**Ubicación:** `Assets/ArtUnbound/Scripts/MR/WallAnchorManager.cs`

---

### **4. `WallPlacementDetector.cs`** (NUEVO)
**Responsabilidad:** Detectar paredes válidas para colocación

**Funcionalidad:**
- ✅ Raycast continuo desde la mano hacia adelante
- ✅ Validar que la superficie es una pared (usar `WallDetectionService`)
- ✅ Verificar que hay espacio suficiente (no colisiona con otros cuadros)
- ✅ Calcular posición y rotación correctas
- ✅ Mostrar preview ghost (semi-transparente)
- ✅ Cambiar color del preview (verde=válido, rojo=inválido)

**Ubicación:** `Assets/ArtUnbound/Scripts/MR/WallPlacementDetector.cs`

---

### **5. Modificar `GameBootstrap.cs`**
**Cambios:**
- ✅ Agregar referencia a `ArtworkHangingController`
- ✅ Nuevo método `StartArtworkHanging()` llamado desde `OnPlaceArtworkRequested()`
- ✅ Reemplazar lógica actual de `WallHighlightController` por el nuevo sistema
- ✅ Ocultar paneles UI cuando se agarra el marco
- ✅ Mostrar paneles cuando se cancela o completa

---

### **6. Modificar `PuzzleBoard.cs`**
**Cambios:**
- ✅ Agregar método `GetCompletedFrame()` → devuelve Transform del marco
- ✅ Método `EnableFrameInteraction(bool)` → hace el marco "agarrable"
- ✅ Agregar Collider al marco para detección de Pinch

---

### **7. Prefab: `ArtworkFrame`**
**Componentes necesarios:**
- ✅ MeshRenderer (marco + textura de la pintura)
- ✅ BoxCollider (para detectar Pinch)
- ✅ `FrameInteractable` component (marca como agarrable)
- ✅ Layer: `InteractableFrame` (nuevo layer)

---

## 🔄 **FLUJO COMPLETO DE INTERACCIÓN:**

### **Fase 1: Preparación (Estado PostGame)**
```
1. Usuario completa puzzle
   └─> GameBootstrap.OnEnterPostGame()
       ├─> PuzzleBoard.ShowFullImageReveal(frameTier)
       ├─> PostGameController.Show(wallCount)
       └─> ArtworkHangingController.EnableGrab(frame)
           └─> Añade Collider al marco
           └─> Muestra hint: "Pinch el marco para colgarlo"
```

### **Fase 2: Agarre (Grab)**
```
2. Usuario hace Pinch sobre el marco
   └─> HandTrackingInputController.OnPinchStart
       └─> ArtworkHangingController.OnPinchDetected()
           ├─> Raycast desde pinch detecta el marco
           ├─> Cambiar estado: Idle → Grabbed
           ├─> HandAttachmentController.Attach(frame, hand)
           │   └─> frame.parent = handTransform
           │   └─> Aplicar offset ergonómico
           ├─> GameBootstrap.HideAllPanels()
           └─> WallPlacementDetector.StartDetection()
```

### **Fase 3: Movimiento + Preview**
```
3. Usuario se mueve con el marco en la mano
   └─> Update() loop:
       ├─> HandAttachmentController.Update()
       │   └─> frame.position = Lerp(current, handPos + offset)
       │   └─> frame.rotation = Slerp(current, handRot * rotOffset)
       │
       └─> WallPlacementDetector.Update()
           ├─> Raycast desde mano hacia adelante
           ├─> Si hit && IsWall(hit):
           │   ├─> Mostrar ghost preview en hit.point
           │   ├─> Color = verde (válido)
           │   └─> hasValidPlacement = true
           └─> Else:
               ├─> Color = rojo (inválido)
               └─> hasValidPlacement = false
```

### **Fase 4: Colocación**
```
4. Usuario suelta el Pinch
   └─> HandTrackingInputController.OnPinchEnd
       └─> ArtworkHangingController.OnPinchReleased()
           ├─> SI hasValidPlacement:
           │   ├─> Cambiar estado: Grabbed → Placing
           │   ├─> HandAttachmentController.Detach()
           │   ├─> Animar marco a la pared (smooth)
           │   ├─> WallAnchorManager.CreateAnchor(artworkId, pos, rot)
           │   │   ├─> Crear ARAnchor
           │   │   ├─> Guardar en SaveDataService
           │   │   └─> anchoredArtworks.Add(...)
           │   ├─> Cambiar estado: Placing → Placed
           │   ├─> Audio: "colocación exitosa"
           │   ├─> Haptic: pulso confirmación
           │   └─> GameBootstrap.TransitionToMainMenu()
           │
           └─> ELSE (no válido):
               ├─> Animar marco de regreso al centro
               ├─> Audio: "error"
               ├─> Mostrar PostGameController de nuevo
               └─> Mensaje: "Acércate más a una pared"
```

### **Fase 5: Persistencia (Al reiniciar la app)**
```
5. App inicia
   └─> GameBootstrap.Start()
       └─> WallAnchorManager.LoadAndSpawnAnchors()
           ├─> Cargar SaveData.anchoredArtworks
           ├─> Para cada artwork:
           │   ├─> Encontrar ARAnchor por anchorId
           │   ├─> SI encontrado:
           │   │   ├─> Instanciar prefab del cuadro
           │   │   ├─> frame.position = anchor.transform * localPosition
           │   │   ├─> Aplicar frameTier material
           │   │   └─> Cuadro visible en la pared ✅
           │   └─> SI NO encontrado:
           │       └─> Log warning (usuario movió muebles)
           └─> Cuadros aparecen en las paredes al iniciar
```

---

## 🎨 **CONSIDERACIONES DE UX:**

### **Feedback Visual:**
1. **Marco en mano:**
   - Escala 80% del tamaño original (más manejable)
   - Ligera rotación hacia el usuario (+15° en X)
   - Sombra proyectada

2. **Ghost Preview:**
   - Copia semi-transparente del marco (alpha 0.5)
   - Verde pulsante si válido
   - Rojo + shake si inválido

3. **Animación de colocación:**
   - Smooth move hacia la pared (0.3s)
   - Scale: 80% → 100%
   - Rotation: ajuste final
   - Partículas al anclar

### **Feedback Audio:**
- Grab: "pick_up.wav" (sutil)
- Placing preview: "hover_wall.wav" (continuous while valid)
- Place success: "artwork_placed.wav" (satisfactorio)
- Place fail: "error.wav"

### **Feedback Háptico:**
- Grab: pulso corto
- Valid placement: pulso suave continuo
- Place success: doble pulso fuerte
- Place fail: vibración de error

---

## 🗂️ **ESTRUCTURA DE DATOS:**

### **Añadir a `SaveData.cs`:**
```cs
[Serializable]
public class SaveData {
    // ... existing fields ...
    
    public List<AnchoredArtwork> anchoredArtworks = new List<AnchoredArtwork>();
}

[Serializable]
public class AnchoredArtwork {
    public string artworkId;
    public string anchorId;  // UUID del Meta Spatial Anchor
    public SerializableVector3 position;
    public SerializableQuaternion rotation;
    public float scale;
    public FrameTier frameTier;
    public long placedTimestamp; // DateTime.Ticks
    
    // Helper para recuperar el cuadro
    public string GetFramePrefabPath() => $"Prefabs/Frames/Frame_{frameTier}";
}
```

---

## ⚙️ **CONFIGURACIÓN EN UNITY:**

### **Nuevos Layers:**
```
Layer 10: InteractableFrame
```

### **Nuevos Prefabs:**
```
Assets/ArtUnbound/Prefabs/
├─ Frames/
│  ├─ Frame_Bronce.prefab
│  ├─ Frame_Plata.prefab
│  ├─ Frame_Oro.prefab
│  └─ Frame_Ebano.prefab
├─ MR/
│  ├─ WallPlacementGhost.prefab (preview semi-transparente)
│  └─ PlacementParticles.prefab (efecto visual al anclar)
```

---

## 🚧 **POSIBLES DESAFÍOS Y SOLUCIONES:**

### **Desafío 1: Detección de colisiones entre cuadros**
**Problema:** Dos cuadros se sobreponen en la misma pared
**Solución:**
- En `WallPlacementDetector.IsValidPlacement()`:
  - SphereCast para detectar otros cuadros cercanos
  - Radio = tamaño del marco + margen (10 cm)
  - Si detecta otro cuadro → placement inválido

### **Desafío 2: Precisión del anchor**
**Problema:** El anchor se desvía ligeramente al recargar
**Solución:**
- Usar Meta Spatial Anchors (no solo AR Foundation)
- Guardar `localPosition` relativa al anchor
- Al cargar, aplicar offset corrector si es necesario

### **Desafío 3: Performance con muchos cuadros**
**Problema:** 20+ cuadros en la escena afectan FPS
**Solución:**
- LOD system: reducir geometría de marcos lejanos
- Occlusion culling: no renderizar cuadros detrás de paredes
- Texture atlasing: un solo material para todos los marcos

### **Desafío 4: Usuario suelta el marco lejos de paredes**
**Problema:** No hay pared cerca, ¿qué hacer con el marco?
**Solución:**
- Animar de regreso a la posición del PuzzleBoard
- Mostrar mensaje: "Acércate a una pared para colgar"
- Permitir reintento inmediato

---

## 📝 **ARCHIVOS A CREAR/MODIFICAR:**

| Archivo | Acción | Líneas estimadas |
|---------|--------|------------------|
| `ArtworkHangingController.cs` | **CREAR** | ~200 |
| `HandAttachmentController.cs` | **CREAR** | ~80 |
| `WallAnchorManager.cs` | **CREAR** | ~150 |
| `WallPlacementDetector.cs` | **CREAR** | ~120 |
| `GameBootstrap.cs` | **MODIFICAR** | +30 |
| `PuzzleBoard.cs` | **MODIFICAR** | +20 |
| `SaveData.cs` | **MODIFICAR** | +20 |
| `SaveDataService.cs` | **MODIFICAR** | +15 |

**Total:** ~635 líneas de código nuevo

---

## ✅ **TESTING CHECKLIST:**

1. ✅ Pinch sobre marco lo agarra correctamente
2. ✅ Marco sigue la mano suavemente
3. ✅ Preview aparece en paredes válidas
4. ✅ Preview rechaza superficies no-verticales
5. ✅ Colocación crea anchor correctamente
6. ✅ Anchor persiste al reiniciar app
7. ✅ Múltiples cuadros no se sobreponen
8. ✅ Feedback audio/haptic funciona
9. ✅ UI se oculta/muestra correctamente
10. ✅ Performance: 72 FPS con 10+ cuadros

---

## 📚 **REFERENCIAS TÉCNICAS:**

### **AR Foundation 6.0:**
- [AR Anchor Manager](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.0/manual/features/anchors.html)
- [Trackable Lifecycle](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.0/manual/architecture/trackables.html)

### **Meta Spatial Anchors:**
- [Meta Quest Spatial Anchors](https://developer.oculus.com/documentation/unity/unity-spatial-anchors/)
- [Persistence Best Practices](https://developer.oculus.com/documentation/unity/unity-spatial-anchors-persist/)

### **XR Hands:**
- [Hand Tracking](https://docs.unity3d.com/Packages/com.unity.xr.hands@1.0/manual/index.html)
- [Pinch Detection](https://docs.unity3d.com/Packages/com.unity.xr.hands@1.0/manual/features/hand-tracking.html)

---

## 🎯 **SIGUIENTES PASOS:**

1. Revisar y aprobar este plan
2. Crear issues/tareas en el sistema de tracking (opcional)
3. Comenzar implementación por fases:
   - Fase 1: `HandAttachmentController` (base)
   - Fase 2: `WallPlacementDetector` (detección)
   - Fase 3: `ArtworkHangingController` (orquestación)
   - Fase 4: `WallAnchorManager` (persistencia)
   - Fase 5: Integración con `GameBootstrap`
   - Fase 6: Testing y polish

---

**Documento creado:** 2026-03-19
**Última actualización:** 2026-03-19
**Estado:** Pendiente de aprobación
