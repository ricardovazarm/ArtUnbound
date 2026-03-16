# Inventario de Problemas de UI — Art Unbound vs Meta Quest

> Documento generado a partir del análisis de la documentación de Meta Quest (MR Design Guidelines, Fonts & Icons, Buttons, Accessibility, Display, Comfort) y el estado actual de Art Unbound.  
> **Fuentes:** [Meta-Quest-MR-UI-Interaction-Guidelines.md](Meta-Quest-MR-UI-Interaction-Guidelines.md), [Analisis-UI-MR-Inconsistencias.md](Analisis-UI-MR-Inconsistencias.md), developers.meta.com/horizon/design

---

## Observaciones desde captura en MR (referencia visual)

*Basado en screenshot del menú principal en passthrough sobre un entorno real (sala con paredes de madera, sillón, mesa, estantería).*

### Estructura visible

| Zona | Contenido | Profundidad aparente |
|------|-----------|----------------------|
| **Left (Configuration)** | Título, sliders Music/Sounds, checkbox Tutorial, "2/44 obras completadas", pista musical actual | Más cercana al usuario |
| **Center** | Filtros (All, in Progress, Completed), grid 3×3 de thumbnails, paginación "1/5" + Next | Más alejada |
| **Right** | Metadatos obra, imagen grande, botones Fácil/Normal/Difícil | Similar al centro |

### Hallazgos desde la captura

- **Oclusión por objetos físicos:** El botón "Next" queda parcialmente tapado por el sillón del entorno real. En MR, la UI flotante puede superponerse con muebles; si el usuario no puede recolocar el menú, controles clave pueden quedar inaccesibles.
- **Texto blanco sobre fondo semi-transparente:** Sobre paredes de madera cálida el contraste puede ser aceptable, pero variará según el entorno (paredes claras, ventanas, etc.).
- **Sliders Music/Sounds:** Los handles circulares parecen pequeños; verificar que cumplan 48×48 dp para hand tracking.
- **Checkbox Tutorial:** Visible; confirma que el checkmark es un target pequeño que requiere hit slop.
- **Pista musical larga:** "Minuet in G major, Anh. 114 - Notebook for Anna Magdalena Bach - Johann Sebastian Bach" — texto muy largo en la parte inferior; riesgo de overflow o truncado poco elegante.
- **Layout en tres profundidades:** Las zonas a distintas distancias pueden afectar la legibilidad y el alcance al interactuar.

---

## Resumen Ejecutivo

| Severidad | Cantidad | Descripción |
|-----------|----------|-------------|
| **Críticos** | 3 | Incumplen requisitos mínimos de Meta; afectan usabilidad y accesibilidad |
| **Importantes** | 6 | Recomendaciones de diseño que mejoran la experiencia MR |
| **Menores** | 5 | Opcionales; validación en dispositivo o mejoras incrementales |

---

## 1. Problemas Críticos

### 1.1 Hit slop ausente en elementos pequeños

**Guía Meta:** *"Usa hit slop invisible cuando los assets no cumplan el tamaño mínimo. Aplica también a iconos accionables."* — [Inputs y Hit Targets](Meta-Quest-MR-UI-Interaction-Guidelines.md#2-requisitos-de-diseño-para-inputs-y-hit-targets)

| Aspecto | Meta | Art Unbound |
|---------|------|-------------|
| Hit target mínimo | 48×48 dp | — |
| Hand tracking recomendado | 60×60 dp | — |
| Hit slop | Obligatorio si el asset es &lt; 48 dp | No implementado |

**Estado actual (confirmado en captura MR):**
- **Checkmark (Toggle Tutorial):** ~20×20 px — muy por debajo del mínimo; visible en panel Configuration.
- **Handles de sliders (Music, Sounds):** Pequeños circulares; probablemente &lt; 48 dp.
- **Iconos pequeños:** Sin `RaycastPadding` ni hit slop configurado.
- **Botones de paginación:** Altura en layout puede variar; verificar en runtime que cumplan ≥60 px.

**Impacto:** Dificultad para apuntar con ray cast o hand tracking; frustración en usuarios con movilidad limitada.

**Recomendación:** Añadir `RaycastPadding` en elementos &lt; 48 dp para expandir el área de hit sin cambiar el aspecto visual, o aumentar el tamaño visual del Checkmark a ≥48×48 px.

---

### 1.2 Fuente por debajo del mínimo en ProgressText (ArtworkCard)

**Guía Meta:** *"Tamaño mínimo 14px para legibilidad mínima; 18px para lectura cómoda."* — [Tipografía](Meta-Quest-MR-UI-Interaction-Guidelines.md#8-visual-design-color-tipografía-e-iconos)

| Aspecto | Meta | Art Unbound |
|---------|------|-------------|
| Mínimo legible | 14 px | — |
| Lectura cómoda | 18 px | — |
| Body 1 (Meta fonts) | 14/20 dp | — |
| Body 2 (suplementario) | 11/16 dp | — |

**Estado actual:**
- **TitleText (ArtworkCard):** 14 px — cumple mínimo.
- **ProgressText (ArtworkCard):** Asignado en escena; si usa 12 px o menos, incumple la guía.
- **MusicTrackText:** 18 px — OK.
- **Otros textos:** Mayoría 24–36 px — OK.

**Impacto:** Si ProgressText está por debajo de 14 px, reduce legibilidad en MR, sobre todo a distancia.

**Recomendación:** Verificar `ProgressText` en `Main.unity` y asegurar `m_fontSize ≥ 14`. Para contenido suplementario, Meta permite Body 2 (11 dp), pero las guías de interacción recomiendan 14 px mínimo.

---

### 1.3 Oclusión de UI por objetos físicos (controles inaccesibles)

**Guía Meta:** *"No colocar objetos virtuales detrás de paredes u obstáculos físicos — riesgo de lesiones y fatiga visual."* — [Tamaño y distancia](Meta-Quest-MR-UI-Interaction-Guidelines.md#3-interacción-con-contenido-virtual). Además, si la UI no se puede recolocar, el usuario no puede evitar que muebles del entorno la tapen.

**Estado actual (confirmado en captura MR):**
- El botón **"Next"** de paginación queda **parcialmente tapado por el sillón** del entorno real.
- La UI se coloca una vez al inicio y **no se puede mover**.
- En espacios con muebles, ventanas o personas, controles clave pueden quedar inaccesibles.

**Impacto:** El usuario no puede avanzar de página si un objeto físico obstruye el botón. Experiencia frustrante y dependiente del entorno.

**Recomendación:** Implementar **manipulación de ventana** (grab/move/place) para que el usuario pueda recolocar el menú y evitar obstáculos físicos. Alternativa: posicionar controles críticos (paginación, botones de acción) en zonas menos propensas a oclusión (p. ej. más centrales o elevadas).

---

## 2. Problemas Importantes

### 2.1 Sin angular scaling

**Guía Meta:** *"Usar angular scaling para mantener legibilidad y tamaño de target al variar la distancia."* — [UI 2D en Espacio](Meta-Quest-MR-UI-Interaction-Guidelines.md#4-ui-2d-en-espacio)

**Estado actual:** El canvas tiene escala fija. No hay lógica de escalado según la distancia usuario–canvas.

**Impacto:** Si el usuario se aleja o acerca, la legibilidad y el tamaño de los targets empeoran.

**Recomendación:** Implementar escalado angular con límites min/max para evitar que la UI se acerque o aleje demasiado.

---

### 2.2 Sin manipulación de ventana (grab/move/place)

**Guía Meta:** *"Ofrecer affordance para agarrar, mover y recolocar la ventana según preferencia del usuario."* — [UI 2D en Espacio](Meta-Quest-MR-UI-Interaction-Guidelines.md#4-ui-2d-en-espacio)

**Estado actual:** El canvas se coloca una vez al inicio (`PositionCanvasWithDelay`) y no se puede mover manualmente.

**Impacto:** Menor flexibilidad en espacios con diferentes tamaños o posturas (sentado vs de pie). **Además, sin recolocación el usuario no puede evitar oclusiones por muebles** (ver 1.3).

**Recomendación:** Añadir un área de agarre (grab affordance) para que el usuario pueda recolocar el menú y evitar obstáculos físicos.

---

### 2.3 LeftZone con rotación -35° (UnifiedMainMenuPanel)

**Guía Meta:** *"Billboarding para que la UI mire al usuario desde cualquier ángulo."* — [Interacción con Contenido Virtual](Meta-Quest-MR-UI-Interaction-Guidelines.md#3-interacción-con-contenido-virtual)

**Estado actual:** `LeftZone` tiene `m_LocalEulerAnglesHint: {x: 0, y: -35, z: 0}` para efecto de panel curvo.

**Impacto:** Puede dificultar la lectura desde ciertos ángulos; el billboarding ideal mantendría la UI siempre orientada al usuario.

**Recomendación:** Validar en dispositivo si la rotación afecta la legibilidad. Si es intencional para el layout curvo, documentar la decisión.

---

### 2.4 Sin botón de retroceso explícito

**Guía Meta:** *"Añadir botón de retroceso en la interfaz; el botón de sistema no está disponible en todas las modalidades de input."* — [Input Modalities](Meta-Quest-MR-UI-Interaction-Guidelines.md#6-input-modalities-modalidades-de-entrada)

**Estado actual:** La navegación depende del flujo de estados del juego (`GameBootstrap`). No hay botón "Atrás" o "Volver" visible en el menú principal.

**Impacto:** Usuarios que esperan un patrón de retroceso estándar pueden sentirse desorientados.

**Recomendación:** Evaluar si el flujo requiere retroceso explícito; si sí, añadir botón de retroceso visible.

---

### 2.5 Contraste y colores sin validación en headset

**Guía Meta:** *"Probar colores en headset; suelen verse más saturados. Contraste 4.5:1 texto, 3:1 no-texto."* — [Color](Meta-Quest-MR-UI-Interaction-Guidelines.md#8-visual-design-color-tipografía-e-iconos)

**Estado actual:** Colores definidos en editor sin documentación de validación en dispositivo. Uso de grises (#EBEBEB) en lugar de blanco puro ya implementado en varios elementos.

**Impacto:** Riesgo de contraste insuficiente o saturación excesiva en Quest.

**Recomendación:** Realizar sesión de pruebas en headset y documentar ajustes de contraste/saturación si es necesario.

---

### 2.6 Overflow / truncado en pista musical larga

**Guía Meta:** *"Etiquetas cortas y específicas"* — [Botones](Meta-Quest-MR-UI-Interaction-Guidelines.md#5-componentes-botones-y-feedback). Textos largos deben manejarse con ellipsis o scroll.

**Estado actual (confirmado en captura MR):**
- La pista actual se muestra completa: *"Minuet in G major, Anh. 114 - Notebook for Anna Magdalena Bach - Johann Sebastian Bach"*.
- Texto muy largo en la parte inferior del panel Configuration; riesgo de overflow, corte brusco o mala legibilidad.

**Impacto:** En pantallas pequeñas o con nombres largos, el texto puede desbordarse o truncarse de forma poco elegante.

**Recomendación:** Definir ancho máximo y overflow con ellipsis (`...`), o mostrar solo "Título - Autor" con tooltip/detalle al hover si aplica.

---

## 3. Problemas Menores

### 3.1 Distancia 70 cm para modo híbrido (manos + controladores)

**Guía Meta:** *"Si la app soporta manos y controladores, colocar a ~70 cm y ofrecer UI de manipulación."* — [Posición UI](Meta-Quest-MR-UI-Interaction-Guidelines.md#4-ui-2d-en-espacio)

**Estado actual:** 45 cm fijo (`DefaultPlacementDistance = 0.45f`). No hay modo híbrido de distancia ni manipulación.

**Impacto:** Menor; 45 cm es correcto para interacción directa con manos.

---

### 3.2 Fuente recomendada (Inter / Meta Horizon OS UI Set)

**Guía Meta:** *"Inter (Meta Horizon OS UI Set) recomendada para legibilidad."* — [Tipografía](Meta-Quest-MR-UI-Interaction-Guidelines.md#8-visual-design-color-tipografía-e-iconos)

**Estado actual:** Se usan fuentes por GUID (`2c84cc21be4a1584bb3129a834606c05`, etc.). No se confirma si es Inter.

**Recomendación:** Verificar el asset de fuente y considerar Inter si mejora legibilidad.

---

### 3.3 Iconos en grid 24×24 / 192×192

**Guía Meta:** *"Grid 24×24 px, construidos en 192×192 para futuras resoluciones."* — [Iconos](Meta-Quest-MR-UI-Interaction-Guidelines.md#8-visual-design-color-tipografía-e-iconos)

**Estado actual:** No se ha verificado el uso de este grid en los iconos.

---

### 3.4 Display: distancia mínima para fijación prolongada

**Guía Meta (Display):** Objetos que requieren fijación prolongada deben estar a ≥0.5 m; 1 m es cómodo para menús/GUIs.

**Estado actual:** 45 cm (0.45 m) — ligeramente por debajo del mínimo de 0.5 m para fijación prolongada.

**Impacto:** Bajo; 45 cm es el estándar para interacción directa con manos. Validar confort en sesiones largas.

---

### 3.5 Consideración de FOV para teclado virtual

**Guía Meta:** El teclado virtual no debe quedar demasiado bajo ni obstruir la vista.

**Estado actual:** No hay teclado virtual en la app; no aplica directamente.

---

## 4. Aspectos Correctos (Referencia)

| Aspecto | Estado |
|---------|--------|
| Distancia 45 cm | `DefaultPlacementDistance = 0.45f` en `GameBootstrap.cs` |
| Evitar blanco puro | `normalButtonColor` #EBEBEB, TitleText gris claro |
| TitleText ≥14 px | ArtworkCard cumple |
| Hit targets principales | Thumbnail 120×120, varios botones 80×60 o mayores |
| Feedback hover/pressed | `UpdateButtonColor()` con `highlightedColor` y `pressedColor` |
| Contenido world-locked | Canvas no head-locked |
| Soporte multimodal | `TrackedDeviceGraphicRaycaster` |
| Tamaños de fuente | Mayoría 24–36 px |

---

## 5. Priorización de Correcciones

| Prioridad | Problema | Esfuerzo estimado |
|-----------|----------|-------------------|
| P0 | Hit slop en Checkmark, handles de sliders e iconos pequeños | Bajo |
| P0 | Verificar/corregir ProgressText font size | Bajo |
| P0 | Oclusión por objetos físicos → implementar manipulación de ventana | Medio |
| P1 | Angular scaling | Medio |
| P1 | Manipulación de ventana (grab) — también mitiga oclusión | Medio |
| P1 | Validación de contraste en headset | Bajo (testing) |
| P1 | Overflow/truncado pista musical | Bajo |
| P2 | LeftZone rotación — validar en dispositivo | Bajo |
| P2 | Botón de retroceso (si aplica) | Bajo |
| P3 | Fuente Inter, grid de iconos | Bajo |

---

## 6. Referencias

- [Meta-Quest-MR-UI-Interaction-Guidelines.md](Meta-Quest-MR-UI-Interaction-Guidelines.md) — guías consolidadas
- [Analisis-UI-MR-Inconsistencias.md](Analisis-UI-MR-Inconsistencias.md) — auditoría previa
- [developers.meta.com/horizon/design](https://developers.meta.com/horizon/design/) — documentación oficial Meta
- `GameBootstrap.cs` — posicionamiento del canvas
- `UnifiedMainMenuController.cs` — lógica de botones y colores
- `PuzzleHUDController.cs` — HUD durante el puzzle
- `Main.unity` — escena principal
- `ArtworkCard.prefab` — tarjetas del catálogo
