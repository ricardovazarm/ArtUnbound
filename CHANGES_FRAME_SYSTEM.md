# Cambios al Sistema de Marcos y Récords

## Fecha: 2026-03-02

## Resumen de Cambios

Se ha rediseñado completamente el sistema de marcos (frames) y récords para simplificar la experiencia:

### ✅ 1. Marcos Basados en Dificultad (NO en Puntaje)

**ANTES:** Los marcos se determinaban por el puntaje obtenido (calculado con tiempo, dificultad, ayuda, etc.)

**AHORA:** Los marcos se determinan únicamente por el número de piezas del puzzle:

| Dificultad | Piezas | Marco     |
|------------|--------|-----------|
| Easy       | 64     | **Bronce**    |
| Normal     | 130-144| **Plata**     |
| Hard       | 256    | **Oro**       |
| Expert     | 512    | **Platinum**  |

### ✅ 2. Récords Basados en Tiempo (NO en Puntaje)

**ANTES:** Un nuevo récord significaba superar el puntaje anterior.

**AHORA:** Un nuevo récord significa completar el puzzle en **menos tiempo** que el récord anterior.

### ✅ 3. Panel Post-Game Simplificado

**ANTES:** El panel mostraba:
- Puntaje
- Tiempo
- Dificultad
- Marco obtenido
- Preview de la obra
- Muchos detalles

**AHORA:** El panel solo muestra:
- "¡Puzzle Completado!"
- Indicador de "Nuevo Récord" (solo si superaste tu mejor tiempo)

---

## Archivos Modificados

### 📁 Enum y Datos Core

1. **`FrameTier.cs`**
   - ❌ Eliminado: `Madera = 0` y `Ebano = 4`
   - ✅ Nuevo sistema: `Bronce (1)`, `Plata (2)`, `Oro (3)`, `Platinum (4)`

2. **`ArtworkProgress.cs`**
   - Actualizado valor por defecto: `FrameTier.Madera` → `FrameTier.Bronce`

3. **`ArtworkRecord.cs`**
   - Actualizado valor por defecto: `FrameTier.Madera` → `FrameTier.Bronce`
   - Actualizado método `TryUpdateRecord()`: Ahora un récord se determina por **tiempo más rápido**, no por puntaje más alto

4. **`PlacedArtwork.cs`**
   - Actualizado valor por defecto: `FrameTier.Madera` → `FrameTier.Bronce`

### 🎮 Lógica de Juego

5. **`GameBootstrap.cs`**
   - ✅ **Nueva función**: `GetFrameTierFromPieceCount(int pieceCount)` - Determina el marco basado en dificultad
   - ✅ Actualizado `OnPuzzleComplete()`:
     - Ya NO usa `ScoringController.GetFrameTier()`
     - Ahora usa `GetFrameTierFromPieceCount()`
     - Detecta nuevo récord comparando **tiempo**, no puntaje
     - Pasa `timeSec`, `previousBestTime`, `frameTier`, e `isNewRecord` al PostGameController
   - ✅ Actualizado fallback: `FrameTier.Madera` → `FrameTier.Bronce`

6. **`ScoringController.cs`**
   - ⚠️ Marcado como **obsoleto** para determinación de marcos
   - Mantenido para compatibilidad futura (como solicitó el usuario)
   - Agregado comentario: "Frame tier logic is now obsolete"

### 🎨 UI

7. **`PostGameController.cs`**
   - ✅ Completamente rediseñado para el nuevo sistema:
     - Removidos todos los campos de UI complejos (score, time display, frame icon, etc.)
     - Solo quedan: `completionText` y `newRecordIndicator`
   - ✅ Actualizada firma de `ShowResults()`:
     - **ANTES**: `ShowResults(data, score, frame, newRecord)`
     - **AHORA**: `ShowResults(data, timeSec, prevBestTime, frame, newRecord)`
   - ✅ Método `UpdateUI()` simplificado:
     - Solo muestra "¡Puzzle Completado!"
     - Solo muestra indicador si `isNewRecord == true`
   - ✅ Actualizado getter: `GetFinalScore()` → `GetCompletionTime()`

---

## ⚠️ Archivos con Referencias a `FrameTier.Madera` y `FrameTier.Ebano` (NO Actualizados)

Los siguientes archivos aún contienen referencias al viejo sistema, pero **se dejaron intactos** por compatibilidad:

- `ArtworkSelectionController.cs`
- `PlacedArtworkController.cs` (meshes y materiales de marcos viejos)
- `FrameAnimationController.cs` (animaciones de marcos viejos)
- `ArtworkDetailController.cs` (íconos de marcos viejos)
- `GalleryItemController.cs` (visualización de marcos viejos)
- `GalleryPanelController.cs` (creación de items con marcos viejos)

### 🔧 Acción Requerida del Usuario

Estos archivos necesitarán actualizarse **en Unity Inspector**:

1. **Actualizar referencias a Sprites/Materiales/Meshes:**
   - Reemplazar referencias de "Madera" por "Bronce"
   - Reemplazar referencias de "Ebano" por "Platinum"
   - O eliminar esas referencias si ya no se usan

2. **Actualizar UI Prefabs:**
   - `PostGameController` necesita ser actualizado en el Inspector:
     - Asignar `completionText` (TextMeshProUGUI para "¡Puzzle Completado!")
     - Asignar `newRecordIndicator` (GameObject que se activa/desactiva)
     - Remover referencias viejas si existen

---

## 🧪 Testing

Para probar el nuevo sistema:

1. **Completar un puzzle por primera vez:**
   - Debería mostrar el panel con "¡Puzzle Completado!" y "Nuevo Récord"
   - El marco asignado debería corresponder a la dificultad (64=Bronce, 144=Plata, etc.)

2. **Completar el mismo puzzle de nuevo con mejor tiempo:**
   - Debería mostrar "Nuevo Récord" si el tiempo fue más rápido
   - NO debería mostrar "Nuevo Récord" si el tiempo fue más lento

3. **Verificar logs:**
   ```
   [GameBootstrap] Puzzle complete! Time: XXs, Frame: XXX, NewRecord: true/false, PreviousBest: XXs
   [PostGameController] ShowResults called - Time: XXs, PrevBest: XXs, Frame: XXX, NewRecord: true/false
   ```

---

## 🚀 Próximos Pasos (Por Implementar)

Según la solicitud del usuario:

### 1. HUD In-Game (Izquierdo)
- [ ] Crear panel rotado estilo Main Menu
- [ ] Mostrar: Piezas colocadas, Tiempo, Título/Autor/Descripción de la obra

### 2. Panel PostGame (Derecho)
- [x] Simplificar a solo "¡Puzzle Completado!" + indicador de récord
- [ ] Crear panel rotado estilo Main Menu (diseño visual pendiente)
- [ ] Integrar al HUD (aparece solo al completar)

### 3. Sistema de Marcos en UI
- [ ] Actualizar `UnifiedMainMenuController` para mostrar marcos por dificultad
- [ ] Actualizar `ArtworkCard` para mostrar el marco correspondiente
- [ ] Actualizar iconografía de Bronce/Plata/Oro/Platinum en todos los lugares

---

## 📝 Notas Importantes

1. **Compatibilidad con Saves Antiguos:**
   - Los saves antiguos con `FrameTier.Madera` o `FrameTier.Ebano` podrían causar errores
   - Considera agregar lógica de migración si es necesario

2. **Score se mantiene en 0:**
   - El campo `score` sigue existiendo en `ArtworkRecord` por compatibilidad
   - Pero ya NO se usa para determinar récords ni marcos

3. **`ScoringController` obsoleto:**
   - Se dejó el código intacto "por si acaso" (como solicitó el usuario)
   - Pero ya NO se usa para determinar `FrameTier`

---

**Fin del documento**
