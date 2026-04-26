# Setup StoreView en Unity Editor (rediseño tabs + secciones + detail panels)

Guia paso a paso para construir el **Store** dentro de la NativeGallery: dos tabs arriba (Paquetes / Bundles), cada uno con secciones tipo Netflix, y dos detail panels modales (artwork-in-pack y pack-in-bundle).

> **Antes de empezar**, los scripts ya existen:
> - `Assets/ArtUnbound/Scripts/UI/GridThumbItemUI.cs`
> - `Assets/ArtUnbound/Scripts/UI/StoreTabsController.cs`
> - `Assets/ArtUnbound/Scripts/UI/PackSectionUI.cs`
> - `Assets/ArtUnbound/Scripts/UI/BundleSectionUI.cs`
> - `Assets/ArtUnbound/Scripts/UI/ArtworkInPackDetailController.cs`
> - `Assets/ArtUnbound/Scripts/UI/PackInBundleDetailController.cs`
> - `Assets/ArtUnbound/Scripts/UI/StoreViewController.cs` (rewriteado)
> - `NativeGalleryController.cs` ya tiene `btnStore`, `storeView`, `storeViewController`
> - `GameBootstrap.cs` ya llama a `nativeGallery.SetPackPurchaseService(packPurchaseService)`
>
> **Lo que falta**: prefabs + jerarquia en escena + wirear refs en Inspector.

---

## PASO 0 — Refresh Unity

1. Assets > Refresh (`Ctrl+R`)
2. Espera la compilacion
3. Verifica que la consola este sin errores rojos. Los 4 scripts viejos (PackCardUI, BundleCardUI, PackDetailController, BundleDetailController) ya fueron borrados — si Unity se queja de referencias rotas en algun MonoBehaviour, reasignalo a los nuevos componentes (esto se hace en los pasos siguientes).

---

## PASO 1 — Prefab `GridThumbItem` (duplicar ArtworkCard2)

Visualmente identico al `ArtworkCard2.prefab` que ya usa el CatalogView. Solo se usa en el **BundleSection** (mostrar packs como icono). Para los artwork icons del PackSection vamos a reusar `ArtworkCard2` directamente — ver PASO 2.

### 1.1 Duplicar ArtworkCard2

1. En el Project Window, selecciona `Assets/ArtUnbound/Prefabs/ArtworkCard2.prefab`
2. `Ctrl+D` para duplicar
3. Renombra el duplicado a `GridThumbItem`
4. Doble click para abrir el prefab en edicion

### 1.2 Reemplazar el script

1. En el Inspector del root `GridThumbItem`, busca el componente **Artwork Card UI** y **borralo**
2. Add Component > **Grid Thumb Item UI** (script `GridThumbItemUI`)

### 1.3 Renombrar children

- Hijo `Thumbnail` -> renombrar `CoverImage`
- Hijo `Title` -> renombrar `LabelText`
- Hijo `CompletedBadge` -> dejalo o borralo (no lo usamos en el store por ahora)

### 1.4 Wirear

En el componente **Grid Thumb Item UI**:
- `Cover Image` -> `CoverImage`
- `Label Text` -> `LabelText`

### 1.5 Guardar

Save prefab. El look (220×270, thumbnail centrada con preserveAspect, titulo abajo 50px) queda identico al catalogo automaticamente.

---

## PASO 2 — Prefab `PackSectionItem`

Una seccion = un pack: header con nombre + precio + Buy a la derecha + grid de 12 obras debajo.

### 2.1 Root

1. En el Canvas, click derecho > **Create Empty** > renombrar `PackSectionItem`
2. **RectTransform**: Width `1100`, Height `420` (provisional, el ContentSizeFitter lo ajusta)
3. Add Component > **Vertical Layout Group**:
   - Spacing `8`
   - Child Alignment `Upper Left`
   - Control Child Size: Width **ON**, Height **OFF**
   - Child Force Expand: Width **ON**, Height **OFF**
4. Add Component > **Content Size Fitter**: Vertical Fit `Preferred Size`
5. Add Component > **Pack Section UI**

### 2.2 HeaderRow

1. Click derecho sobre `PackSectionItem` > **Create Empty** > renombrar `HeaderRow`
2. Add Component > **Horizontal Layout Group**:
   - Spacing `15`, Child Alignment `Middle Left`
   - Padding: Left `10`, Right `10`
   - Child Force Expand: Width **OFF**, Height **OFF**
   - Control Child Size: Width **OFF**, Height **ON**
3. Add Component > **Layout Element**: Min Height `50`, Preferred Height `50`

#### 2.2.1 PackNameText

1. Hijo de `HeaderRow` > **UI > Text - TextMeshPro** > renombrar `PackNameText`
2. **TMP Text**: Font Size `22`, Bold, color blanco
3. Add Component > **Layout Element**: Flexible Width `1` (ocupa el espacio sobrante)
4. Raycast Target **OFF**

#### 2.2.2 BtnBuyPack

1. Hijo de `HeaderRow` > **UI > Button - TextMeshPro** > renombrar `BtnBuyPack`
2. Renombra el Text hijo a `BtnBuyPackText` (placeholder `Buy $2.99`, Bold, color negro). El precio va aqui — no hay TMP separado.
3. **Image** del btn: color `#d4c089`
4. Add Component > **Layout Element**: Preferred Width `160`, Preferred Height `44`

#### 2.2.3 OwnedBadge (alternativo a BtnBuyPack)

1. Hijo de `HeaderRow` > **UI > Image** > renombrar `OwnedBadge`
2. Color verde `#00C850`
3. Hijo TMP que diga `OWNED` (Bold, blanco, centrado)
4. **Layout Element**: Preferred Width `140`, Preferred Height `44`
5. Desactivar por defecto

### 2.3 ArtworksContainer

1. Hijo de `PackSectionItem` > **Create Empty** > renombrar `ArtworksContainer`
2. Add Component > **Grid Layout Group**:
   - Padding `5,5,5,5`
   - Cell Size `220 x 270` (mismas que CatalogView para consistencia visual)
   - Spacing `10 x 10`
   - Constraint `Fixed Column Count`, Count `4` (12 obras = 3 filas exactas)
3. Add Component > **Content Size Fitter**: Vertical Fit `Preferred Size`

### 2.4 Wirear el script

En el componente **Pack Section UI** del root:
- `Pack Name Text` -> `PackNameText`
- `Btn Buy Pack` -> `BtnBuyPack`
- `Btn Buy Pack Text` -> `BtnBuyPackText`
- `Owned Badge` -> `OwnedBadge`
- `Artworks Container` -> `ArtworksContainer`
- `Artwork Card Prefab` -> **`Assets/ArtUnbound/Prefabs/ArtworkCard2.prefab`** (reuso directo, mismo look que el catalogo)

### 2.5 Guardar como prefab

Arrastra `PackSectionItem` a `Assets/ArtUnbound/Prefabs/`. Borra de la jerarquia.

---

## PASO 3 — Prefab `BundleSectionItem`

Misma estructura que PackSectionItem pero con grid de packs (cell mas grande).

### 3.1 Duplicar PackSectionItem

1. Selecciona `Assets/ArtUnbound/Prefabs/PackSectionItem.prefab`, `Ctrl+D`
2. Renombra a `BundleSectionItem`
3. Abrelo (doble click) para editar

### 3.2 Cambios

- Borra el componente **Pack Section UI** del root
- Add Component > **Bundle Section UI**
- Renombra hijos del HeaderRow:
  - `PackNameText` -> `BundleNameText`
  - `BtnBuyPack` -> `BtnBuyBundle`, su Text hijo -> `BtnBuyBundleText` (placeholder `Buy $12.99` — el precio va en el boton)
- Renombra `ArtworksContainer` -> `PacksContainer`
- El GridLayoutGroup del PacksContainer mantiene `Cell Size 220 x 270` y `Count 4` (mismo estilo que el catalogo)

### 3.3 Wirear

En **Bundle Section UI**:
- `Bundle Name Text` -> `BundleNameText`
- `Btn Buy Bundle` -> `BtnBuyBundle`
- `Btn Buy Bundle Text` -> `BtnBuyBundleText`
- `Owned Badge` -> `OwnedBadge`
- `Packs Container` -> `PacksContainer`
- `Pack Icon Prefab` -> `Assets/ArtUnbound/Prefabs/GridThumbItem.prefab` (el duplicado del PASO 1)

### 3.4 Guardar

Save prefab.

---

## PASO 4 — Construir el `StoreView` con TopTabs + 2 sub-views

### 4.1 Crear el GameObject root `StoreView`

1. En la jerarquia, navega a `NativeGallery > NativeGalleryCanvas > ContentRoot > ContentArea`
2. Click derecho sobre `ContentArea` > **Create Empty** > renombrar `StoreView`
3. **RectTransform**:
   - Anchors: `stretch-stretch` (cuadrado de 4 esquinas)
   - Left/Right/Top/Bottom: `0` (rellena todo el ContentArea)
4. Add Component > **Store View Controller** (script `StoreViewController` — los campos los wireamos en PASO 7)
5. **Desactivar** el GameObject `StoreView` por defecto (el NativeGalleryController lo activa al cambiar al tab Store)

> **Nota:** el root no tiene `Image`, `ScrollRect`, ni `Mask`. Es solo un contenedor — los ScrollRects van adentro de cada sub-view (PaquetesView/BundlesView).

### 4.2 TopTabs

1. Click derecho sobre `StoreView` > **Create Empty** > renombrar `TopTabs`
2. **RectTransform**:
   - Anchors `top-stretch`, Pivot `0.5, 1`
   - Top `0`, Left `0`, Right `0`, Height `60`
3. Add Component > **Horizontal Layout Group**:
   - Spacing `10`, Child Alignment `Middle Center`
   - Padding `10,10,5,5`
   - Child Force Expand: Width **OFF**, Height **OFF**
4. Add Component > **Store Tabs Controller**

#### 4.2.1 BtnPaquetes

1. Hijo de `TopTabs` > **UI > Button - TextMeshPro** > renombrar `BtnPaquetes`
2. Text hijo: `Paquetes`, Font Size `18`, Bold, color blanco
3. **Image** del btn: color `#896C4A` (activo por default)
4. **Layout Element**: Preferred Width `200`, Preferred Height `44`

#### 4.2.2 BtnBundles

1. Duplicar `BtnPaquetes` y renombrar `BtnBundles`
2. Cambiar texto a `Bundles`
3. **Image**: color `#373737` (inactivo por default)

### 4.3 PaquetesView

1. Click derecho sobre `StoreView` > **UI > Scroll View** > renombrar `PaquetesView`
2. **RectTransform**:
   - Anchors `stretch-stretch`, Top `60` (debajo de TopTabs), Left/Right/Bottom `0`
3. **Image** del PaquetesView: Alpha `0` (transparente)
4. **Scroll Rect**: Horizontal **OFF**, Vertical **ON**, Movement Type `Elastic`, Sensitivity `30`
5. **Borra** el `Scrollbar Horizontal` hijo
6. (opcional) Borra el `Scrollbar Vertical` si no quieres barra visible
7. Selecciona el `Content` (hijo de Viewport):
   - **RectTransform**: Anchors `top-stretch`, Pivot `0.5, 1`, Top/Left/Right `0`
   - Renombrar a `PaquetesContent`
   - Add Component > **Vertical Layout Group**:
     - Padding `15,15,15,15`, Spacing `25`
     - Child Alignment `Upper Center`
     - Control Child Size: Width **ON**, Height **OFF**
     - Child Force Expand: Width **ON**, Height **OFF**
   - Add Component > **Content Size Fitter**: Vertical Fit `Preferred Size`

### 4.4 BundlesView

1. Duplica `PaquetesView`, renombra `BundlesView`
2. Renombra su `Content` a `BundlesContent`
3. **Desactivar** el GameObject `BundlesView` por defecto (Paquetes es el default activo)

### 4.5 Wirear StoreTabsController

En `TopTabs` > componente **Store Tabs Controller**:
- `Btn Paquetes` -> `BtnPaquetes`
- `Btn Bundles` -> `BtnBundles`
- `Paquetes View` -> `PaquetesView`
- `Bundles View` -> `BundlesView`

### 4.6 Wirear StoreView en NativeGalleryController

Selecciona el GameObject `NativeGallery`. En el componente **Native Gallery Controller**:
- `Store View` -> arrastra el GameObject `StoreView` que acabas de crear
- `Store View Controller` -> arrastra el mismo `StoreView` (Unity tomara automaticamente el componente)
- `Btn Store` -> verifica que apunte al boton `BtnStore` del BottomNav (si ya existia, dejalo)

Sin este wireado el tab Store del BottomNav no abrira nada.

---

## PASO 5 — Crear `ArtworkInPackDetailPanel` (duplicar DetailPanel del catalogo)

Mismo enfoque que con ArtworkCard2: duplicas el `DetailPanel` que ya usa el CatalogView para tener look&feel identico, y solo cambias 2 cosas: los botones de difficulty → boton de compra, y el script.

### 5.1 Duplicar el DetailPanel existente

1. En la jerarquia, busca el GameObject `DetailPanel` (es el modal del catalogo — esta dentro del Canvas raiz, normalmente desactivado)
2. Selecciona y `Ctrl+D` para duplicar
3. Renombra el duplicado a `ArtworkInPackDetailPanel`
4. **Activar temporalmente** el GameObject para poder editarlo (lo desactivas al final)

### 5.2 Reemplazar el script del root

1. Busca el componente **Native Gallery Controller** o similar en el root del duplicado — el detail original NO tiene su propio controller (lo maneja NativeGalleryController), pero por seguridad asegurate que NO haya scripts del catalogo en el root
2. Add Component > **Artwork In Pack Detail Controller** (script `ArtworkInPackDetailController`)

### 5.3 Reemplazar los botones de difficulty por la zona de compra

Los 3 botones `BtnEasy` / `BtnNormal` / `BtnHard` ya no aplican. Vamos a poner en su lugar un solo boton de compra grande + un OwnedBadge alternativo + un texto de precio del pack.

1. **Borra** los GameObjects `BtnEasy`, `BtnNormal`, `BtnHard` (y sus textos hijos)
2. En el mismo nivel jerarquico donde estaban, crea:

   **BtnBuyPack** (Button):
   - Click derecho > **UI > Button - TextMeshPro**
   - Renombra a `BtnBuyPack`, hijo Text -> `BtnBuyPackText` (placeholder `Buy Pack $2.99`, Bold, negro, Size 18). El precio va aqui — no hay TMP separado.
   - Image del btn: color `#d4c089`
   - RectTransform: ocupa el ancho horizontal donde estaban los 3 botones, Height `60`, centrado

   **OwnedBadge** (Image, alternativo a BtnBuyPack):
   - Click derecho > **UI > Image**, renombra a `OwnedBadge`
   - Mismo RectTransform que BtnBuyPack (overlap)
   - Color verde `#00C850`
   - Hijo TMP que diga `OWNED` (Bold, blanco, centrado, Size 18)
   - **Desactivar** por defecto

### 5.4 Renombrar children (opcional pero recomendado)

Para que coincidan con los nombres del controller:
- `DescText` -> `DescriptionText` (si tu DetailPanel original lo llama distinto)

Los demas (`ArtworkImage`, `TitleText`, `ArtistText`, `BtnClose`) ya tienen nombres consistentes.

### 5.5 Wirear el Artwork In Pack Detail Controller

En el Inspector del root `ArtworkInPackDetailPanel`, componente **Artwork In Pack Detail Controller**:
- `Panel Root` -> arrastra el mismo `ArtworkInPackDetailPanel` (el root)
- `Artwork Image` -> `ArtworkImage`
- `Title Text` -> `TitleText`
- `Artist Text` -> `ArtistText`
- `Description Text` -> `DescriptionText` (o `DescText`, segun como se llame)
- `Btn Buy Pack` -> `BtnBuyPack`
- `Btn Buy Pack Text` -> `BtnBuyPackText`
- `Owned Badge` -> `OwnedBadge`
- `Btn Close` -> `BtnClose`

### 5.6 Desactivar

`ArtworkInPackDetailPanel` debe iniciar **desactivado** (el script lo activa en Show).

---

## PASO 6 — Crear `PackInBundleDetailPanel`

Mismo template, datos diferentes. Lo mas rapido es duplicar.

### 6.1 Duplicar y limpiar

1. Selecciona `ArtworkInPackDetailPanel`, `Ctrl+D`
2. Renombra `PackInBundleDetailPanel`
3. Borra el componente **Artwork In Pack Detail Controller** del root
4. Add Component > **Pack In Bundle Detail Controller**

### 6.2 Renombrar children

- `ArtworkImage` -> `PackHeroImage`
  - Cambia **Preserve Aspect** a **OFF** (la hero 3x2 fue diseñada para fill, no para preservar aspecto)
- `TitleText` -> `PackNameText`
- `ArtistText` -> `ArtworksListText` (este es el unico que cambia mas)
  - Cambia placeholder a `- Artwork 1\n- Artwork 2\n...`
  - Font Size `13`, Wrapping ON
  - Layout Element: Min Height `220`, Flexible Height `1` (ocupa todo el espacio)
- Borra `DescriptionText` (no se usa en este panel)
- `BtnBuyPack` -> `BtnBuyBundle`, su Text hijo -> `BtnBuyBundleText` (placeholder `Buy Bundle $12.99` — el precio va aqui)

### 6.3 Wirear

En **Pack In Bundle Detail Controller**:
- `Panel Root` -> `PackInBundleDetailPanel`
- `Pack Hero Image` -> `PackHeroImage`
- `Pack Name Text` -> `PackNameText`
- `Artworks List Text` -> `ArtworksListText`
- `Btn Buy Bundle` -> `BtnBuyBundle`
- `Btn Buy Bundle Text` -> `BtnBuyBundleText`
- `Owned Badge` -> `OwnedBadge`
- `Btn Close` -> `BtnClose`

### 6.4 Desactivar

`PackInBundleDetailPanel` debe iniciar **desactivado**.

---

## PASO 7 — Wirear `StoreViewController`

Selecciona el `StoreView`. En el componente **Store View Controller** asigna:

- `Store Tabs Controller` -> `TopTabs` (Unity tomara el componente)
- `Paquetes Content` -> `PaquetesContent` (el GameObject Content del PaquetesView)
- `Bundles Content` -> `BundlesContent`
- `Pack Section Prefab` -> `Assets/ArtUnbound/Prefabs/PackSectionItem.prefab`
- `Bundle Section Prefab` -> `Assets/ArtUnbound/Prefabs/BundleSectionItem.prefab`
- `Artwork In Pack Detail Controller` -> `ArtworkInPackDetailPanel` (Unity tomara el componente)
- `Pack In Bundle Detail Controller` -> `PackInBundleDetailPanel`
- `No Bundles Label` / `No Packs Label`: dejar vacio (opcional)

---

## PASO 8 — Generar pack heros y probar

### 8.1 Generar las 17 pack heros

En el chat con Claude:
```
/gen-pack-hero all
```

Esto genera 17 JPG en `Assets/ArtUnbound/Data/Packs/Hero Images/Wave0X/`. En Unity:
1. Selecciona cada JPG, Texture Type -> `Sprite (2D and UI)` -> Apply
2. Para cada PackDefinition (`Assets/ArtUnbound/Data/Packs/Wave0X/<pack>.asset`), arrastra el sprite correspondiente al campo `Pack Image`

(Si no asignas `packImage`, los iconos de pack en el tab Bundles caen al cover de la primera obra del pack — funciona, pero la hero 3x2 luce mejor.)

### 8.2 Probar end-to-end

1. Guarda escena (`Ctrl+S`)
2. Click en **Play**
3. Click en boton **Store** del BottomNav
4. Por default ves tab **Paquetes** activo (color marron) y **Bundles** inactivo (gris).
5. Scroll vertical: ves cada pack como seccion (header con nombre + precio + Buy a la derecha) + grid de 12 obras debajo.
6. Click en una obra (ej. Cafe Terrace at Night dentro del pack Van Gogh):
   - Se abre `ArtworkInPackDetailPanel` con la imagen left + datos right (titulo, artista+año, descripcion que ya incluye museo/movimiento) + boton `Buy Pack $2.99` abajo
   - X arriba a la derecha cierra
7. Click en `Buy Pack $2.99`:
   - Cierra el panel y la seccion del pack ahora muestra `Owned`
8. Click en tab **Bundles**:
   - Cambia a la vista de bundles
   - Founder's Collection aparece como seccion (header + Buy $12.99 + grid de 6 packs con sus heros)
9. Click en un pack-icono (ej. Vermeer):
   - Se abre `PackInBundleDetailPanel` con la pack hero left + nombre + lista de las 12 obras + boton `Buy Bundle $12.99` abajo
10. Click en `Buy Bundle $12.99`:
    - Cierra panel, los 6 packs del bundle ahora muestran Owned (en ambos tabs)

---

## Troubleshooting

**StoreView no aparece** → verifica `BtnStore` wireado en NativeGalleryController + `StoreView` GameObject wireado tambien + StoreView arranca desactivado.

**Tab no cambia al hacer click** → verifica que `BtnPaquetes`/`BtnBundles` esten wireados en `Store Tabs Controller`. Tambien que `PaquetesView` arranque activo y `BundlesView` desactivado.

**Sections no aparecen** → verifica `PaquetesContent`/`BundlesContent` wireados en `Store View Controller`, y que ambos prefabs (`PackSectionItem`, `BundleSectionItem`) esten asignados.

**Iconos vacios en grid** → en PackSection: `Artwork Card Prefab` debe ser `ArtworkCard2.prefab`. En BundleSection: `Pack Icon Prefab` debe ser `GridThumbItem.prefab`. Verifica tambien que ArtworkDefinitions tengan `thumbnail`/`fullImage` y los PackDefinitions tengan `packImage` (o un primer artwork con thumbnail).

**Click en icono no abre detail** → confirma que el prefab tiene un `Button` en el root (ArtworkCard2 ya lo trae; si duplicaste a GridThumbItem se preserva). Children (Thumbnail/CoverImage, Title/LabelText) deben tener `Raycast Target = OFF`.

**Detail no se cierra con X** → revisa que `BtnClose` este wireado en el detail controller correspondiente.

**Pack-icono en bundle muestra cover de pintura en vez de pack hero** → no asignaste `packImage` al PackDefinition (ejecuta `/gen-pack-hero all` y arrastra los JPG generados como sprites).

---

## Resumen de archivos creados

```
Assets/ArtUnbound/Prefabs/
  GridThumbItem.prefab          [duplicado de ArtworkCard2, solo para pack-icons del bundle]
  PackSectionItem.prefab        [nuevo]
  BundleSectionItem.prefab      [nuevo]

Assets/ArtUnbound/Data/Packs/Hero Images/
  Wave01/
    Van Gogh - Light & Color in Provence.jpg
    Monet - Gardens of Light.jpg
    ...
  Wave02/...
  Wave03/...

Assets/ArtUnbound/Scenes/Main.unity
  NativeGallery > NativeGalleryCanvas > ContentRoot > ContentArea
    StoreView                                                    [reestructurado]
      TopTabs (StoreTabsController)
        BtnPaquetes
        BtnBundles
      PaquetesView (ScrollRect)                                  [activo default]
        Viewport
          PaquetesContent (VerticalLayoutGroup)                  [spawn point]
      BundlesView (ScrollRect)                                   [inactivo default]
        Viewport
          BundlesContent (VerticalLayoutGroup)                   [spawn point]
    ArtworkInPackDetailPanel                                     [nuevo, modal, inactivo]
    PackInBundleDetailPanel                                      [nuevo, modal, inactivo]
```

Tiempo estimado: **45-60 min** primera vez, **20-30 min** si ya tienes el StoreView base de la version anterior.
