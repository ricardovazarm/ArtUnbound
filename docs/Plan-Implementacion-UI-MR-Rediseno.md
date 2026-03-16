# Plan de Implementación: Rediseño UI MR — Art Unbound

> Documento de planificación para implementar las mejoras de UI sugeridas por Gemini y Claude, con referencia visual tipo "Mystique Gallery: Puzzle Adventures".  
> **No incluye implementación** — solo el plan detallado.

---

## Resumen de Cambios

| # | Cambio | Referencia | Prioridad |
|---|--------|------------|-----------|
| 1 | Nuevo formato de botones (pill, circular glossy) | Imagen ref. | Alta |
| 2 | Quitar panel izquierdo (Configuration) | Sugerencia IA | Alta |
| 3 | Configuración en paleta de pintor en mano izquierda | Sugerencia IA | Baja (al final) |
| 4 | Sombra física en panel central | Imagen ref. | Media |
| 5 | Marcos dorados en cuadros (thumbnails + detalle) | Imagen ref. | Alta |
| 6 | Reorganizar panel derecho (info + botones) | Imagen ref. | Media |
| 7 | Paginación con nuevo estilo de botones (sin scroll) | Decisión usuario | Media |

---

## 0. Assets gráficos: enfoque de generación

**Decisión:** Los PNG de botones y marcos se generan con IA. El usuario los importa en Unity como Sprites.

### 0.1 Botones (PNG → Sprite)

| Asset | Descripción | Formato | Uso en Unity |
|-------|-------------|---------|--------------|
| `ButtonPill.png` | Cápsula horizontal, bordes redondeados, gris semi-transparente, glow sutil | PNG, fondo transparente | Importar como Sprite → `Image Type: Sliced` (bordes para 9-slice si aplica) |
| `ButtonCircleGlossy.png` | Círculo glossy/bubble, aspecto esférico con highlight | PNG, fondo transparente | Importar como Sprite → `Image Type: Simple` |
| `ButtonPrimary.png` | Rectángulo alargado, bordes redondeados, glow en bordes | PNG, fondo transparente | Importar como Sprite → `Image Type: Sliced` |

**Flujo:** IA genera los PNG → usuario los coloca en `Assets/ArtUnbound/UI/Sprites/` (o similar) → importar con Texture Type: Sprite → asignar a componentes Image en los prefabs.

### 0.2 Marcos (PNG → Sprite con 9-slice)

| Asset | Descripción | Formato | Uso en Unity |
|-------|-------------|---------|--------------|
| `FrameThumbnail.png` | Marco dorado clásico para thumbnails | PNG, centro transparente | Border L,R,T,B=32 → Sliced |
| `FrameDetail.png` | Marco para imagen grande del panel derecho | PNG, centro transparente | Border L,R,T,B=24 → Sliced |
| `FrameMadera.png` | Marco madera — **default** para obras no armadas | PNG, centro transparente | Border L,R,T,B=32 → Sliced |
| `FrameOro.png` | Marco oro (tier score 200+) | PNG, centro transparente | Border L,R,T,B=32 → Sliced |
| `FramePlata.png` | Marco plata (tier score 100+) | PNG, centro transparente | Border L,R,T,B=32 → Sliced |
| `FrameBronce.png` | Marco bronce (tier score 50+) | PNG, centro transparente | Border L,R,T,B=32 → Sliced |

**9-slice:** Las esquinas del marco no se deforman; los bordes se estiran para adaptarse a cualquier aspect ratio (cuadrado, apaisado, vertical).

---

## 1. Nuevo formato de botones

### 1.1 Descripción

Reemplazar los botones actuales (paletas circulares) por estilos que funcionen mejor en MR:

- **Filtros (All, In Progress, Completed):** Botones tipo pill (cápsula) con bordes redondeados, fondo gris semi-transparente, glow sutil al seleccionado.
- **Dificultad (Fácil, Normal, Difícil):** Botones circulares glossy/bubble, con glow interno cuando están seleccionados.
- **Paginación (Prev, Next):** Mismo estilo circular glossy que dificultad.
- **START PUZZLE:** Botón rectangular alargado, bordes redondeados, glow en los bordes.

### 1.2 Assets (generados por IA)

Los PNG se generan y el usuario los importa como Sprites. Ver sección 0.1.

### 1.3 Tareas técnicas

| Tarea | Archivos / Componentes | Notas |
|------|------------------------|-------|
| Generar PNG de botones (IA) | `ButtonPill.png`, `ButtonCircleGlossy.png`, `ButtonPrimary.png` | Ver sección 0.1 |
| Importar como Sprites en Unity | `Assets/ArtUnbound/UI/Sprites/` | Texture Type: Sprite |
| Crear prefabs usando los sprites | `ButtonPill.prefab`, `ButtonCircleGlossy.prefab`, `ButtonPrimary.prefab` | Image component con sprite asignado |
| Sustituir botones en `UnifiedMainMenuController` | `UnifiedMainMenuController.cs`, `Main.unity` | filterAllButton, filterInProgressButton, etc. |
| Mantener hit targets ≥ 48×48 dp | Todos los botones | Meta: 60×60 dp recomendado para hand tracking |

### 1.4 Dependencias

- Sprites importados desde los PNG generados.

---

## 2. Quitar panel izquierdo (LeftZone)

**Cuándo:** En Fase 3, junto con la implementación de la paleta en mano. No se quita antes para no perder acceso a la configuración.

### 2.1 Descripción

Eliminar el panel de configuración fijo (LeftZone) que contiene:

- Título "Configuration"
- Sliders Music / Sounds
- Toggle Tutorial
- Texto "X/Y obras completadas"
- Pista musical actual

Todo esto pasará a la paleta en mano (ver sección 3). Hasta entonces, LeftZone se mantiene.

### 2.2 Tareas técnicas

| Tarea | Archivos / Componentes | Notas |
|------|------------------------|-------|
| Desactivar o eliminar LeftZone en escena | `Main.unity` → UnifiedMainMenuPanel → LeftZone | Desactivar primero para pruebas |
| Quitar referencias en `UnifiedMainMenuController` | `UnifiedMainMenuController.cs` | musicVolumeSlider, soundVolumeSlider, tutorialToggle, globalStatsText, musicTrackText |
| Mover lógica de configuración a nuevo controlador | Nuevo: `HandPaletteController.cs` o similar | Ver sección 3 |
| Ajustar layout del panel principal | `Main.unity` | Redistribuir ancho: Center + Right ocupan más espacio |
| Actualizar `GameBootstrap` si hay referencias | `GameBootstrap.cs` | Verificar eventos OnMusicVolumeChanged, etc. |

### 2.3 Consideraciones

- Los eventos `OnMusicVolumeChanged`, `OnSoundVolumeChanged`, `OnTutorialToggled` deben seguir funcionando vía la paleta en mano.
- El texto "X/Y obras completadas" puede ir en la paleta o en el panel central (header).

---

## 3. Configuración en paleta de pintor (mano izquierda)

**Prioridad:** Baja — se implementa al final (Fase 3). No es crítica para el funcionamiento del juego.

### 3.1 Descripción

Cuando el usuario muestre la palma de la mano izquierda, aparece una paleta de pintor flotante con:

- Sliders Music / Sounds
- Toggle Tutorial
- Progreso "X/Y obras completadas"
- Pista musical actual (truncada con ellipsis)

La paleta debe seguir la mano y orientarse hacia el usuario.

### 3.2 Tareas técnicas

| Tarea | Archivos / Componentes | Notas |
|------|------------------------|-------|
| Detectar palma de mano izquierda | Nuevo script o extender `HandTrackingInputController` | XRHandSubsystem, pose de mano |
| Crear prefab de paleta de pintor | `Assets/ArtUnbound/Prefabs/UI/HandPalette.prefab` | Forma de paleta, semi-transparente |
| Crear `HandPaletteController` | `Assets/ArtUnbound/Scripts/UI/HandPaletteController.cs` | Mostrar/ocultar, posicionar, eventos |
| Integrar sliders y toggle en paleta | HandPalette prefab | Hit targets ≥ 48 dp |
| Posicionar paleta sobre la palma | HandPaletteController | Offset relativo a mano, billboarding |
| Conectar eventos a servicios existentes | HandPaletteController → AudioManager, SaveData | Misma lógica que LeftZone |
| Mostrar solo en estado MainMenu / ArtworkSelection | `GameBootstrap`, `HandPaletteController` | Ocultar durante puzzle, post-game |

### 3.3 Dependencias

- XR Hands (`com.unity.xr.hands`) para detección de palma.
- Referencia a mano izquierda en el XR Origin / rig.

### 3.4 Referencia de diseño

- Forma: paleta ovalada o con muesca para el pulgar.
- Tamaño: cómodo para interacción a ~30–40 cm.
- Aparece/desaparece con animación suave (fade o scale).

---

## 4. Sombra física en panel central

### 4.1 Descripción

Añadir una sombra (drop shadow) al panel central del catálogo para dar sensación de profundidad y anclaje en MR.

### 4.2 Tareas técnicas

| Tarea | Archivos / Componentes | Notas |
|------|------------------------|-------|
| Añadir componente de sombra al panel central | `Main.unity` → CenterZone o CatalogPanel | Unity UI Shadow o Outline |
| O usar imagen de sombra detrás del panel | Sprite de sombra suave | Más control artístico |
| Ajustar offset y blur de sombra | Inspector | Sutil, no exagerado |
| Validar en passthrough | Headset | Que la sombra se vea bien sobre fondo real |

### 4.3 Opciones técnicas

- **Unity UI Shadow:** `Add Component → Shadow` en el Image del panel.
- **Sprite de sombra:** Imagen PNG con gradiente suave, como hijo detrás del panel.
- **URP:** Decal o quad con material de sombra proyectada (más complejo).

---

## 5. Marcos en cuadros

### 5.1 Descripción

Añadir marcos visibles a las obras:

- **Thumbnails en grid:** Marco dorado/orlado, estilo clásico (como en imagen de referencia).
- **Imagen grande en panel derecho:** Marco más simple (borde oscuro o dorado según diseño).

### 5.2 Assets (generados por IA, 9-slice)

Los PNG se generan y el usuario los importa como Sprites con **9-slice** para que se adapten a cualquier tamaño sin deformar esquinas. Ver sección 0.2.

| Asset | Uso |
|-------|-----|
| `FrameThumbnail.png` | Thumbnails del grid (ArtworkCard) |
| `FrameDetail.png` | Imagen grande en panel derecho |

**Configuración en Unity:** Sprite Editor → definir Border (L, R, T, B) para que esquinas queden fijas y bordes se estiren. `Image Type: Sliced`.

### 5.3 Tareas técnicas

| Tarea | Archivos / Componentes | Notas |
|------|------------------------|-------|
| Generar PNG de marcos (IA) | `FrameThumbnail.png`, `FrameDetail.png` | Ver sección 0.2 |
| Importar como Sprites, configurar 9-slice | Sprite Editor → Border | Esquinas fijas, bordes que se estiran |
| Integrar marco en `ArtworkCard` | `ArtworkCard.prefab` | Image con FrameThumbnail como hijo del thumbnail, anclado para seguir tamaño |
| Integrar marco en panel derecho | DetailPanel, alrededor de detailArtworkImage | Image con FrameDetail, anclado |
| Ajustar color/tinte si hace falta | Material o Color en Image | Dorado clásico |

### 5.4 Relación con sistema de marcos existente

- `FrameConfigSet.asset` ya define materiales por tier (Madera, Bronce, Plata, Oro, Ébano).
- Los marcos del **catálogo** pueden usar un material fijo (dorado clásico).
- Los marcos de **obras completadas** (en pared) siguen usando el sistema de tiers por puntuación.

---

## 6. Reorganizar panel derecho

### 6.1 Descripción

Cambiar layout y estilo del panel de detalle:

- **Imagen grande** arriba, con marco.
- **Información** debajo: título, artista, año, museo, movimiento, piezas, dificultad — en formato compacto (ej. "Johannes Vermeer | 1665 | 500 Pieces | Medium Difficulty").
- **Botón START PUZZLE** grande, rectangular, prominente.
- **Botones de dificultad** (Fácil, Normal, Difícil) en formato circular glossy, debajo del botón principal.

### 6.2 Tareas técnicas

| Tarea | Archivos / Componentes | Notas |
|------|------------------------|-------|
| Rediseñar layout del DetailPanel | `Main.unity` → RightZone → DetailPanel | Vertical layout: Image → Info → Start → Difficulty |
| Crear o reutilizar componente de info compacta | DetailPanel | Una o dos líneas de texto con separadores |
| Sustituir botones de dificultad por nuevo estilo | easyButton, normalButton, hardButton | ButtonCircleGlossy |
| Añadir botón START PUZZLE si no existe | DetailPanel | O renombrar/rediseñar el actual |
| Ajustar orden de elementos | RectTransform, Layout Group | Vertical, espaciado consistente |

### 6.3 Consideraciones

- El botón Expert puede mantenerse o integrarse según diseño.
- La descripción larga puede colapsarse o mostrarse en tooltip si hay poco espacio.

---

## 7. Paginación con nuevo estilo (sin scroll)

### 7.1 Descripción

Mantener la paginación con botones Prev/Next (no scroll), pero usando el nuevo formato de botones circular glossy.

### 7.2 Tareas técnicas

| Tarea | Archivos / Componentes | Notas |
|------|------------------------|-------|
| Sustituir catalogPageLeftButton y catalogPageRightButton | `Main.unity`, `UnifiedMainMenuController` | Usar prefab ButtonCircleGlossy |
| Mantener lógica de paginación | `UnifiedMainMenuController` | UpdatePageButtons, etc. |
| Texto "X/Y" entre botones | CatalogPageText | Estilo coherente con resto de UI |

---

## 8. Orden de implementación sugerido

```
Fase 0 — Assets gráficos (prerrequisito)
├── 0.1 Generar PNG de botones (ButtonPill, ButtonCircleGlossy, ButtonPrimary)
├── 0.2 Generar PNG de marcos (FrameThumbnail, FrameDetail)
└── 0.3 Usuario importa en Unity como Sprites; configurar 9-slice en marcos

Fase 1 — Fundación
├── 1.1 Crear prefabs usando los sprites importados
├── 1.2 Sustituir botones en menú (filtros, dificultad, paginación)
└── 1.3 Validar hit targets y feedback en headset

Fase 2 — Layout y pulido visual (LeftZone se mantiene por ahora)
├── 2.1 Redistribuir layout (Center + Right ocupan más espacio; LeftZone sigue visible)
├── 2.2 Sombra en panel central
├── 2.3 Marcos en thumbnails y detalle
└── 2.4 Reorganizar panel derecho

Fase 3 — Paleta en mano (al final, no crítica para el juego)
├── 3.1 Implementar HandPaletteController + detección de palma
├── 3.2 Crear prefab HandPalette con sliders, toggle, progreso
├── 3.3 Migrar configuración a paleta
└── 3.4 Quitar LeftZone y redistribuir layout definitivo
```

**Nota:** El menú en la mano (paleta de pintor) se deja al final porque no es necesario para que el juego funcione. LeftZone se mantiene hasta tener la paleta implementada; al completar Fase 3 se elimina.

---

## 9. Assets PNG a generar (IA) — resumen

| PNG | Descripción | Destino en Unity |
|-----|-------------|------------------|
| `ButtonPill.png` | Cápsula, bordes redondeados, gris semi-transparente | Sprite → filtros |
| `ButtonCircleGlossy.png` | Círculo glossy/bubble | Sprite → dificultad, paginación |
| `ButtonPrimary.png` | Rectángulo alargado, glow | Sprite → START PUZZLE |
| `FrameThumbnail.png` | Marco dorado clásico (9-slice) | Sprite → thumbnails |
| `FrameDetail.png` | Marco para imagen grande (9-slice) | Sprite → panel derecho |
| `FrameMadera.png` | Marco madera (9-slice) — **default** obras no armadas | Sprite → tier Madera (score 0) |
| `FrameOro.png` | Marco oro (9-slice) | Sprite → tier Oro (score 200+) |
| `FramePlata.png` | Marco plata (9-slice) | Sprite → tier Plata (score 100+) |
| `FrameBronce.png` | Marco bronce (9-slice) | Sprite → tier Bronce (score 50+) |

**Flujo:** Generar → guardar en proyecto → importar como Sprite → asignar a prefabs.

---

## 10. Archivos principales a modificar

| Archivo | Cambios |
|---------|---------|
| `UnifiedMainMenuController.cs` | Quitar refs LeftZone, nuevos botones, layout |
| `Main.unity` | Eliminar/desactivar LeftZone, nuevos prefabs, sombra |
| `ArtworkCard.prefab` | Añadir marco (FrameThumbnail) a thumbnail |
| `GameBootstrap.cs` | Conectar HandPalette si aplica |
| Nuevo: `HandPaletteController.cs` | Lógica paleta en mano |
| Nuevo: prefabs UI | ButtonPill, ButtonCircleGlossy, ButtonPrimary, HandPalette |

---

## 11. Referencias

- [Problemas-UI-Meta-Quest-Inventario.md](Problemas-UI-Meta-Quest-Inventario.md) — inventario de problemas actuales
- [Meta-Quest-MR-UI-Interaction-Guidelines.md](Meta-Quest-MR-UI-Interaction-Guidelines.md) — guías de diseño Meta
- Imagen de referencia: "Mystique Gallery: Puzzle Adventures" (paneles, botones, marcos, sombras)
- `FrameConfigSet.asset`, `FrameAnimationController.cs` — sistema de marcos existente
