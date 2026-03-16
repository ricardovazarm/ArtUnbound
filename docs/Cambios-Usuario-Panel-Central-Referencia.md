# Cambios del usuario — Referencia para no sobreescribir

> Documento de referencia con los cambios que el usuario ha hecho recientemente. **No modificar ni revertir** estos aspectos al implementar el rediseño del panel central.

---

## 1. ArtworkCard.prefab

### Cambios aplicados

| Aspecto | Estado actual |
|--------|----------------|
| **TitleText** | Eliminado del prefab. `titleText: {fileID: 0}` en ArtworkCard.cs |
| **ProgressText** | Eliminado del prefab. `progressText: {fileID: 0}` en ArtworkCard.cs |
| **ThumbContainer** | Anclas cambiadas a `(0,0)-(1,1)` con `SizeDelta (-20,-20)` — ocupa toda la tarjeta con padding |
| **ThumbnailImage** | Hijo de ThumbContainer, anclado `(0,0)-(1,1)` con `SizeDelta (-25, 0)` |

### Implicaciones

- Las tarjetas del catálogo muestran **solo el thumbnail** (imagen de la obra), sin título ni porcentaje de progreso superpuesto.
- `UnifiedMainMenuController.SetupArtworkCard()` sigue llamando a `TitleText` y `ProgressText` con comprobación `!= null`, por lo que no hay error; simplemente no se muestran.

---

## 2. UnifiedMainMenuController.cs

### Cambios aplicados

| Aspecto | Estado actual |
|--------|----------------|
| **Paginación** | `catalogPageLeftButton`, `catalogPageRightButton`, `catalogPageText` — reemplaza ScrollRect |
| **Grid** | 3×3 fijo, 9 ítems por página (`ItemsPerPage = 9`) |
| **Métodos** | `RefreshCatalogPage()`, `GoToPrevPage()`, `GoToNextPage()`, `UpdatePageIndicator()`, `UpdatePageButtons()` |
| **Botones de página** | Se ocultan (SetActive) en primera/última página, no se deshabilitan |
| **Music track** | `musicTrackText`, `SetupMusicTrackDisplay()`, `OnMusicTrackChanged`, `UpdateMusicTrackText()` |
| **Sonido** | `PlayButtonClick()` en todos los botones (filtros, paginación, dificultad, selección) |
| **Colores** | `normalButtonColor = (0.92, 0.92, 0.92)`; `highlightedColor`, `pressedColor` para feedback Meta MR |

---

## 3. Main.unity — CenterZone

### Estructura actual

- **CenterZone** (RectTransform 500×0)
  - Hijos: catalogGrid, filtros, CatalogPaginationBar, etc.
- **CatalogPaginationBar**
  - `CatalogPageLeftButton` (100×65)
  - `CatalogPageText` ("1 / 5")
  - `CatalogPageRightButton` (100×65)

---

## 4. Qué NO hacer al modificar el panel central

1. **No restaurar** TitleText ni ProgressText en ArtworkCard.
2. **No cambiar** la lógica de paginación (3×3, botones Prev/Next).
3. **No quitar** PlayButtonClick() de los botones.
4. **No modificar** normalButtonColor, highlightedColor, pressedColor.
5. **No tocar** la integración de musicTrackText con AudioManager.
6. **No sustituir** catalogPageLeftButton/catalogPageRightButton por scroll.

---

## 5. Qué SÍ se puede hacer (según Plan-Implementacion-UI-MR-Rediseno)

- Añadir **sombra física** al panel central.
- Añadir **marcos** a las tarjetas (FrameThumbnail como hijo de ThumbContainer o ArtworkCard).
- Cambiar el **formato visual** de los botones (usar sprites ButtonPill, ButtonCircleGlossy) manteniendo la lógica actual.
- Ajustar layout, espaciado, tamaños — sin romper la paginación ni la estructura de CenterZone.

---

## 6. Cambios aplicados (formato botones zona central)

**Fecha:** 2026-03-14

- **Filtros (All, In Progress, Completed):** Sprite cambiado a `ButtonPill.png`.
- **Paginación (Prev, Next):** Sprite cambiado a `ButtonCircleGlossy.png`.
- **Sprites:** `ButtonPill.png` y `ButtonCircleGlossy.png` configurados como `textureType: Sprite` en sus .meta.

## 7. Borde del panel del catálogo

**Fecha:** 2026-03-14

- **PanelRounded.png:** El borde sutil dorado/crema está integrado en el sprite (generado por `create_panel_rounded()` con `border_rgba`).
- **CatalogPanelBorder:** Deshabilitado; ya no se usa (el sprite `PanelRoundedBorder` no existía en Unity).
