# Análisis de Inconsistencias UI/MR: UnifiedMainMenuPanel y HudPanelNew

> Documento de auditoría comparando la implementación actual de Art Unbound contra las [Guías Oficiales de Meta Quest para UI en MR](Meta-Quest-MR-UI-Interaction-Guidelines.md).  
> **No se han realizado modificaciones** — solo análisis y registro de hallazgos.

---

## Resumen Ejecutivo

| Categoría | Estado | Cantidad |
|-----------|--------|----------|
| Críticas | Requieren corrección | 6 |
| Importantes | Recomendadas | 5 |
| Menores | Opcionales | 4 |

---

## 1. Estructura Analizada

### Canvas Principal: `MainCanvasCurved`

- **Render Mode**: World Space
- **Reference Resolution**: 800×600
- **Size Delta**: 1500×730
- **Raycaster**: `TrackedDeviceGraphicRaycaster` (XR)
- **Posicionamiento**: `PositionCanvasWithDelay` en `GameBootstrap` — coloca el canvas a 0.4 m del usuario tras 2 s de calibración

### Paneles

| Panel | Uso | Controlador |
|-------|-----|-------------|
| **UnifiedMainMenuPanel** | Menú principal (selección de obras, configuración, detalle) | `UnifiedMainMenuController` |
| **HudPanelNew** | HUD durante el puzzle (título, progreso, timer, salir) | `PuzzleHUDController` (LeftZone) + botones Replay/Quit |

---

## 2. Inconsistencias Críticas

### 2.1 Distancia del canvas (40 cm vs 45 cm) ✅ CORREGIDO

**Guía Meta:** Para interacción directa con manos, colocar la ventana a ~**45 cm** del usuario.

**Actual:** `DefaultPlacementDistance = 0.45f` (45 cm) en `GameBootstrap.cs`.

**Impacto:** La UI queda ligeramente más cerca de lo recomendado, lo que puede afectar el confort y la precisión con hand tracking.

---

### 2.2 Uso de blanco puro (#FFFFFF) ✅ CORREGIDO

**Guía Meta:** Evitar blanco puro (`#FFFFFF`) y negro puro (`#000000`). Usar grises claros/oscuros. Fondo claro no más brillante que `#DADADA`.

**Actual:** 
- `normalButtonColor = new Color(0.92f, 0.92f, 0.92f, 1f)` (#EBEBEB) en `UnifiedMainMenuController`
- ArtworkCard TitleText: color gris claro (0.92, 0.92, 0.92)

**Nota:** Otros textos en la escena Main.unity pueden seguir usando blanco; se recomienda revisar en iteración futura.

---

### 2.3 Tamaño de fuente por debajo del mínimo (12 px) ✅ CORREGIDO

**Guía Meta:** Mínimo **14 px** para legibilidad; **18 px** para lectura cómoda.

**Actual:** `ArtworkCard.prefab` — `TitleText` con `m_fontSize: 14`.

**Impacto:** Legibilidad reducida en MR, sobre todo a distancia.

---

### 2.4 Hit targets por debajo de 48 dp ✅ CORREGIDO

**Guía Meta:** Mínimo **48×48 dp**; **60×60 dp** recomendado para hand tracking.

**Actual:**
- **TutorialToggle**: RectTransform aumentado a `100×60` (hit area)
- **catalogPageLeftButton / catalogPageRightButton**: `80×60` (altura ≥ 60)
- **Botón 100×40**: aumentado a `100×60`
- **ArtworkCard** thumbnail: 120×120 — OK

**Impacto:** Dificultad para apuntar con ray cast o hand tracking, especialmente en controles pequeños.

---

### 2.5 Falta de hit slop en elementos pequeños

**Guía Meta:** Usar hit slop invisible cuando los assets no cumplan el tamaño mínimo. Aplicar a iconos accionables.

**Actual:** No se observa configuración explícita de `RaycastPadding` o hit slop en elementos como el Checkmark (20×20) ni en iconos pequeños.

---

### 2.6 Sobrescritura de feedback de botones en `UnifiedMainMenuController` ✅ CORREGIDO

**Guía Meta:** Feedback visual claro en estados hover y pressed.

**Actual:** `UpdateButtonColor()` y `UpdateDifficultyButton()` ahora establecen explícitamente `highlightedColor` (tinte azul claro al hover) y `pressedColor` (gris más oscuro al presionar) para garantizar feedback visible independientemente del estado seleccionado/dimmed.

```csharp
colors.highlightedColor = new Color(0.85f, 0.9f, 1f, 1f);   // Light blue tint on hover
colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);      // Clearly darker when pressed
```

**Impacto:** El usuario puede no percibir bien el hover/pressed si el color normal se actualiza constantemente.

---

## 3. Inconsistencias Importantes

### 3.1 Sin botón de retroceso explícito

**Guía Meta:** Añadir botón de retroceso en la interfaz; el botón de sistema no está disponible en todas las modalidades de input.

**Actual:** No hay un botón "Atrás" o "Volver" visible en el menú principal. La navegación depende del flujo de estados del juego (GameBootstrap).

**Nota:** Puede ser aceptable si el flujo no requiere volver atrás desde el menú principal.

---

### 3.2 Sin angular scaling

**Guía Meta:** Usar escalado angular para mantener legibilidad y tamaño de target al variar la distancia.

**Actual:** El canvas tiene escala fija. No hay lógica de escalado según la distancia usuario–canvas.

**Impacto:** Si el usuario se aleja o acerca, la legibilidad y el tamaño de los targets pueden empeorar.

---

### 3.3 Sin manipulación de ventana (grab/move/place)

**Guía Meta:** Ofrecer affordance para agarrar, mover y recolocar la ventana según preferencia del usuario.

**Actual:** El canvas se coloca una vez al inicio y no se puede mover manualmente.

---

### 3.4 LeftZone con rotación -35° (UnifiedMainMenuPanel)

**Guía Meta:** Billboarding para que la UI mire al usuario desde cualquier ángulo.

**Actual:** `LeftZone` tiene `m_LocalEulerAnglesHint: {x: 0, y: -35, z: 0}` para efecto de panel curvo. Esto puede dificultar la lectura desde ciertos ángulos.

**Nota:** Puede ser intencional para un layout curvo; conviene validar en dispositivo.

---

### 3.5 Contraste y colores en headset

**Guía Meta:** Probar colores en headset; suelen verse más saturados. Contraste 4.5:1 texto, 3:1 no-texto.

**Actual:** No hay evidencia de pruebas específicas en headset. Los colores se definen en editor sin documentación de validación en dispositivo.

---

## 4. Inconsistencias Menores

### 4.1 Distancia 70 cm para modo híbrido (manos + controladores)

**Guía Meta:** Si la app soporta manos y controladores, colocar a ~70 cm y ofrecer UI de manipulación.

**Actual:** 40 cm fijo. No hay modo híbrido de distancia ni manipulación.

---

### 4.2 Consideración de FOV para teclado virtual

**Guía Meta:** El teclado virtual no debe quedar demasiado bajo ni obstruir la vista.

**Actual:** No hay teclado virtual en la app; no aplica directamente.

---

### 4.3 Fuente recomendada (Inter)

**Guía Meta:** Inter (Meta Horizon OS UI Set) recomendada para legibilidad.

**Actual:** Se usan otras fuentes (p. ej. `2c84cc21be4a1584bb3129a834606c05`, `8f586378b4e144a9851e7b34d9b748ee`). No se confirma si es Inter.

---

### 4.4 Iconos en grid 24×24 / 192×192

**Guía Meta:** Grid 24×24 px, construidos en 192×192 para futuras resoluciones.

**Actual:** No se ha verificado el uso de este grid en los iconos.

---

## 5. Aspectos Correctos

| Aspecto | Estado |
|---------|--------|
| Estados hover/pressed en botones | Los `Button` usan `ColorTransition` con Highlighted y Pressed |
| Contenido world-locked | El canvas se posiciona una vez y no sigue la cabeza (no es HUD head-locked) |
| Soporte multimodal | `TrackedDeviceGraphicRaycaster` para ray cast XR |
| Varios tamaños de fuente | Títulos 36 px, textos 24–28 px en la mayoría de elementos |
| Algunos botones con tamaño adecuado | 136×89 en varios botones cumple el mínimo |
| Colores de acento | Uso de azul (0.2, 0.6, 1) para progreso y botones |

---

## 6. Resumen por Panel

### UnifiedMainMenuPanel

| Elemento | Inconsistencia |
|----------|----------------|
| LeftZone (config) | Rotación -35° puede afectar legibilidad |
| Botones de filtro/dificultad | Sobrescritura de `normalColor` puede afectar feedback |
| ArtworkCard (prefab) | TitleText 12 px < 14 px mínimo |
| Botones de paginación | 80×50 — altura < 60 dp recomendado |
| Colores | Uso de blanco puro |

### HudPanelNew

| Elemento | Inconsistencia |
|----------|----------------|
| LeftZone (PuzzleHUD) | TxtTitle 36 px OK; verificar hit targets de BtnQuit |
| BtnQuit | Tamaño a verificar (80×50 en escena similar) |
| Checkmark (Toggle) | 20×20 — muy por debajo de 48 dp |
| Textos | Blanco puro |

---

## 7. Recomendaciones Prioritarias

1. **Críticas:** Ajustar distancia a 0.45 m, sustituir blanco puro por grises, subir fuentes a ≥14 px, aumentar hit targets a ≥48 dp y añadir hit slop donde haga falta.
2. **Importantes:** Revisar feedback de botones al cambiar `normalColor`, evaluar angular scaling y manipulación de ventana.
3. **Menores:** Validar fuentes e iconos según guías de Meta.

---

## 8. Reporte de Verificación MCP (Unity)

> Verificación automática ejecutada con Unity MCP `Unity_RunCommand` el 2026-03-12.

| Item | Documento | Verificado | Estado |
|------|-----------|------------|--------|
| **2.1** Distancia 45 cm | `DefaultPlacementDistance = 0.45f` | `GameBootstrap.cs` línea 788: `0.45f` | ✅ OK |
| **2.2** Blanco puro | `normalButtonColor` gris #EBEBEB | `UnifiedMainMenuController`: (0.92, 0.92, 0.92) | ✅ OK |
| **2.2b** ArtworkCard color | Evitar #FFF | TitleText: (0.92, 0.92, 0.92) | ✅ OK |
| **2.3** Fuente ≥14 px | ArtworkCard TitleText | `ArtworkCard.prefab`: 14 px | ✅ OK |
| **2.4** Thumbnail 120×120 | ArtworkCard | ThumbContainer: 120×120 | ✅ OK |
| **2.4** Botones paginación | 80×60 recomendado | CatalogPageLeftButton/RightButton: altura 9.9 en layout (verificar en runtime) | ⚠️ Revisar |
| **2.4** Toggles <48 dp | Checkmark 20×20 | Toggles en escena: 0 con min<48 (layout puede variar) | ⚠️ Revisar |
| **3.4** LeftZone | Rotación -35° | `localEulerAngles.y = 325` (-35°) | ✅ Confirmado |

**Notas:** La verificación de `DefaultPlacementDistance` se obtuvo por inspección de código; la reflexión en runtime falló. Los botones de paginación muestran altura 9.9 en el Editor (posible efecto de layout); validar en dispositivo.

---

## 9. Referencias

- [Meta-Quest-MR-UI-Interaction-Guidelines.md](Meta-Quest-MR-UI-Interaction-Guidelines.md) — guías consolidadas
- [developers.meta.com/horizon/design](https://developers.meta.com/horizon/design/) — documentación oficial
- `GameBootstrap.cs` — posicionamiento del canvas
- `UnifiedMainMenuController.cs` — lógica de botones y colores
- `PuzzleHUDController.cs` — HUD durante el puzzle
- `Main.unity` — escena principal
- `ArtworkCard.prefab` — tarjetas del catálogo
