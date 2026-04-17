# Setup NativeGallery en Unity Editor

Guía paso a paso para construir la jerarquía de la galería nativa (estilo Prime Video / Meta Store)  
en Unity 6 LTS. Seguir en orden — cada paso asume que el anterior está hecho.

---

## Requisitos previos

- Meta XR All-in-One SDK v85+ instalado (ya está en el proyecto)
- TextMeshPro importado (ya está)
- La escena `Assets/ArtUnbound/Scenes/Main.unity` abierta

---

## PASO 1 — Crear el GameObject raíz

1. En la jerarquía de la escena, haz clic derecho → **Create Empty**
2. Renómbralo `NativeGallery`
3. En el Inspector, haz clic en **Add Component** → busca `Native Gallery Controller` → agrégalo
4. Deja el GameObject **desactivado** por ahora (la casilla junto al nombre en el Inspector).  
   El script lo activa solo cuando `Show()` es llamado.

---

## PASO 2 — Canvas World Space

> ✅ **Verificado directamente contra la escena funcionando** (`MainCanvasPanel`):  
> El proyecto usa **XR Interaction Toolkit**, no Meta ISDK. El approach correcto es  
> `GraphicRaycaster` + **`TrackedDeviceGraphicRaycaster`** — exactamente igual que el `MainCanvasPanel`  
> que ya funciona con ray y poke. No necesitas `OVRRaycaster` ni `PointableCanvas`.

1. Selecciona `NativeGallery` en la jerarquía
2. Clic derecho sobre él → **UI > Canvas**  
   Esto crea un Canvas como hijo de `NativeGallery`
3. Renómbralo `NativeGalleryCanvas`
4. En el Inspector del Canvas:
   - **Render Mode** → `World Space`
   - **Event Camera** → asigna la `Main Camera`
5. En el **Rect Transform** del Canvas:
   - Width: `1000`
   - Height: `750`
   - Scale X: `0.001` | Scale Y: `0.001` | Scale Z: `0.001`  
     *(Esto convierte 1000 unidades de canvas = 1 metro en world space)*
   - Pos X/Y/Z: `0, 0, 0` (la posición la maneja el script)
6. El `Graphic Raycaster` estándar lo agrega Unity automáticamente — **déjalo**.
7. **Agregar** `Tracked Device Graphic Raycaster`:
   - Add Component → busca `Tracked Device Graphic Raycaster` → agrégalo  
     *(está en el paquete XR Interaction Toolkit, namespace `UnityEngine.XR.Interaction.Toolkit.UI`)*
   - Configuración: deja todos los valores por defecto

> **¿Por qué funciona sin tocar nada más?**  
> El `XRUIInputModule` ya está en el `EventSystem` de tu escena (dentro de `MR Interaction Setup`).  
> El `TrackedDeviceGraphicRaycaster` en el Canvas es todo lo que necesitas — el módulo detecta  
> automáticamente todos los Canvas que tengan este componente y les envía los eventos de ray y poke.  
> Así es exactamente como funciona el `MainCanvasPanel` existente.

---

## PASO 3 — Panel raíz de contenido (contentRoot)

1. Selecciona el Canvas en la jerarquía
2. Clic derecho → **UI > Image** → renómbralo `ContentRoot`
3. En su **Rect Transform**:
   - Anchors: **stretch-stretch** (el cuadrado de anchors en las 4 esquinas)
   - Left/Right/Top/Bottom: `0`
4. En el componente **Image**:
   - Color: `R:0  G:0  B:0  A:180` (negro semitransparente)
   - Image Type: `Sliced` (para bordes suaves si añades un sprite más adelante)
5. Este `ContentRoot` es el que el script oculta mientras espera que el tracking sea válido.  
   Asígnalo en `NativeGalleryController > Content Root`.

---

## PASO 4 — Header

1. Selecciona `ContentRoot` → clic derecho → **UI > Text - TextMeshPro** → renómbralo `Header`
2. **Rect Transform**:
   - Anchors: `top-stretch`
   - Pivot: `0.5, 1`
   - Height: `70`
   - Left/Right: `0`, Top: `0`
3. **TextMeshPro**:
   - Text: `🎨 Art Unbound` *(nombre del juego, no se traduce)*
   - Font Size: `42`
   - Alignment: Center Middle
   - Color: Blanco

---

## PASO 5 — ContentArea (zona central)

1. Selecciona `ContentRoot` → clic derecho → **Create Empty** → renómbralo `ContentArea`
2. Agrega un componente **Rect Transform** si no lo tiene (se agrega solo al crear hijo de Canvas)
3. **Rect Transform**:
   - Anchors: `stretch-stretch`
   - Top: `70` (espacio para el Header)
   - Bottom: `80` (espacio para el BottomNav)
   - Left/Right: `0`

---

## PASO 6 — CatalogView (grid de todas las obras)

1. Selecciona `ContentArea` → clic derecho → **UI > Scroll View** → renómbralo `CatalogView`
2. **Rect Transform** del `CatalogView`:
   - Anchors: `stretch-stretch`, Left/Right/Top/Bottom: `0`
3. En el componente **Scroll Rect**:
   - `Horizontal`: **OFF**
   - `Vertical`: **ON**
   - `Scroll Sensitivity`: `30`
   - `Movement Type`: `Clamped`
4. Expande `CatalogView` en la jerarquía. Verás `Viewport > Content`.
5. Selecciona el `Content` dentro de `CatalogView > Viewport > Content`:
   - **Rect Transform**: Anchors `top-stretch`, Pivot `0.5, 1`, Height `0` (se expande automático)
   - Agrega **Grid Layout Group**:
     - Cell Size: `220, 270`
     - Spacing: `10, 10`
     - Start Corner: `Upper Left`
     - Start Axis: `Horizontal`
     - Child Alignment: `Upper Center`
     - Constraint: `Fixed Column Count` → Count: `4`
   - Agrega **Content Size Fitter**:
     - Horizontal Fit: `Unconstrained`
     - Vertical Fit: `Preferred Size`
6. Asigna el `Content` al campo `Catalog Grid Container` en `NativeGalleryController`.

---

## PASO 7 — CompletedView (obras completadas)

Mismos pasos que el PASO 6, pero:
1. Renómbralo `CompletedView`
2. El `Content` de su ScrollRect asígnalo a `Completed Grid Container`
3. Puedes duplicar `CatalogView` (Ctrl+D) y renombrarlo para ahorrar tiempo

---

## PASO 8 — SearchView (búsqueda)

1. Selecciona `ContentArea` → clic derecho → **Create Empty** → renómbralo `SearchView`
2. **Rect Transform**: Anchors `stretch-stretch`, todo a `0`
3. Agrega **Vertical Layout Group**:
   - Spacing: `10`
   - Child Alignment: `Upper Left`
   - Control Child Size → Width: ☐ OFF, Height: ☐ OFF
   - Child Force Expand → Width: ✅ ON, Height: ☐ OFF
4. Dentro de `SearchView`:

   **a) Campo de búsqueda:**
   - Clic derecho → **UI > Input Field - TextMeshPro** → renómbralo `SearchInput`
   - **Rect Transform** Height: `60`
   - En el componente **TMP_InputField**:
     - Placeholder text: `Search by title, artist or movement...`
     - Font Size: `28`
   - Asígnalo a `Search Input Field` en `NativeGalleryController`

   **b) Label "Sin resultados":**
   - Clic derecho → **UI > Text - TextMeshPro** → renómbralo `SearchEmptyLabel`
   - Text: `Type to search by title, artist or movement`
   - Font Size: `28`, Color: gris, Alignment: Center
   - Asígnalo a `Search Empty Label`

   **c) Grid de resultados:**
   - Crea un **Scroll View** igual que en PASO 6, renómbralo `SearchResults`
   - Su `Content` asígnalo a `Search Grid Container`

---

## PASO 9 — SettingsView (configuración)

1. Selecciona `ContentArea` → clic derecho → **Create Empty** → renómbralo `SettingsView`
2. **Rect Transform**: Anchors `stretch-stretch`, Left/Right: `100`, Top/Bottom: `50`  
   *(márgenes para que no quede pegado a los bordes)*
3. Agrega **Vertical Layout Group**:
   - Spacing: `30`
   - Child Alignment: `Upper Left`
   - Control Child Size → Width: ☐ OFF, Height: ☐ OFF
   - Child Force Expand → Width: ✅ ON, Height: ☐ OFF
4. Dentro de `SettingsView`, crea estas secciones:

   **a) Music:**
   - **Create Empty** → `MusicSection` → Horizontal Layout Group
   - Child: TMP `🎵 Music` (Width 200)
   - Child: Slider (`UI > Slider`)
     - Min Value: `0`, Max Value: `1`, Value: `0.5`
     - Assign to `Music Slider`
   - Child: empty TMP (Width 80) → assign to `Music Value Text`  
     *(the script writes "50%" here)*

   **b) Sound Effects:**
   - Same as Music but label `🔊 SFX`
   - Slider → `Sfx Slider`, Label → `Sfx Value Text`

   **c) Haptics:**
   - **Create Empty** → `HapticsSection` → Horizontal Layout Group
   - Child: TMP `📳 Haptics`
   - Child: Toggle (`UI > Toggle`)
     - Is On: `true`
     - Assign to `Haptics Toggle`

---

## PASO 10 — DetailPanel (panel de detalle de obra)

El DetailPanel es un overlay modal que cubre todo el canvas cuando el usuario toca una card.
Muestra la imagen grande de la obra a la izquierda y la info + botones de dificultad a la derecha.

### 10a — Overlay base

1. Selecciona `ContentRoot` (NO `ContentArea`) → clic derecho → **UI > Image** → renómbralo `DetailPanel`
2. **Rect Transform**:
   - Anchors: **stretch-stretch**, todos los offsets en `0` (cubre todo el ContentRoot)
3. **Image** component:
   - Color: `R:0  G:0  B:0  A:220` (overlay oscuro semitransparente)
   - `Raycast Target: ✅ ON` (bloquea clics al contenido detrás)
4. Desactiva el GameObject: desmarca la casilla junto al nombre en el Inspector
5. Asígnalo al campo `Detail Panel` en `NativeGalleryController`

---

### 10b — InnerPanel (tarjeta blanca central)

1. Clic derecho sobre `DetailPanel` → **UI > Image** → renómbralo `InnerPanel`
2. **Rect Transform**:
   - Anchors: **middle-center** (el preset del centro)
   - Pivot: `0.5, 0.5`
   - Width: `900`, Height: `560`
   - Pos X: `0`, Pos Y: `0`
3. **Image** component:
   - Color: `R:18  G:18  B:18  A:255` (gris oscuro, combina con el estilo del juego)
   - `Raycast Target: ✅ ON`

---

### 10c — BtnClose (botón ✕)

1. Clic derecho sobre `InnerPanel` → **UI > Button - TextMeshPro** → renómbralo `BtnClose`
2. **Rect Transform**:
   - Anchors: **top-right**, Pivot: `1, 1`
   - Width: `50`, Height: `50`
   - Pos X: `-10`, Pos Y: `-10`
3. **Image** component → Color: `#555555`
4. **TMP hijo** (el texto del botón):
   - Text: `✕`
   - Font Size: `28`, Alignment: Center Middle, Color: blanco
5. Asígnalo al campo `Btn Detail Close` en `NativeGalleryController`

---

### 10d — ArtworkImage (imagen grande a la izquierda)

1. Clic derecho sobre `InnerPanel` → **UI > Image** → renómbralo `ArtworkImage`
2. **Rect Transform**:
   - Anchors: **left-stretch**, Pivot: `0, 0.5`
   - Pos X: `20` (distancia desde el borde izquierdo del InnerPanel)
   - Width: `380`
   - Top: `20`, Bottom: `20`
3. **Image** component:
   - **Source Image**: asigna cualquier sprite temporal (ej. una de las pinturas del catálogo)  
     *(necesitas una imagen asignada para que aparezcan los campos `Image Type` y `Preserve Aspect`)*
   - `Image Type`: `Simple`
   - `Preserve Aspect`: ✅ ON
   - **Color**: `R:255 G:255 B:255 A:0` (transparente — el script asigna el sprite en runtime)
   - **Raycast Target**: ☐ OFF
   - Una vez configurado, puedes dejar el sprite temporal o quitarlo — el script lo sobreescribe
4. Asígnalo al campo `Detail Artwork Image` en `NativeGalleryController`

---

### 10e — InfoArea (columna derecha con texto y botones)

1. Clic derecho sobre `InnerPanel` → **Create Empty** → renómbralo `InfoArea`
2. **Rect Transform**:
   - Anchors: **stretch-stretch**
   - Left: `420`, Right: `20`, Top: `20`, Bottom: `20`
3. Agrega **Vertical Layout Group**:
   - Spacing: `12`
   - Child Alignment: `Upper Left`
   - Control Child Size → Width: ✅ ON, Height: ☐ OFF
   - Child Force Expand → Width: ✅ ON, Height: ☐ OFF

Dentro de `InfoArea`, crea estos hijos en orden:

**a) TitleText**
- Clic derecho → **UI > Text - TextMeshPro** → renómbralo `TitleText`
- **Rect Transform**: Height: `80`  
  *(con Font Size 34, este alto da espacio exacto para 2 líneas — TMP corta con `...` si no cabe)*
- **TMP**:
  - Text: `Artwork Title`
  - Font Size: `34`, Font Style: **Bold**
  - Word Wrapping: ✅ ON
  - Overflow: `Ellipsis`
  - Alignment: Left Top
  - Color: blanco `#FFFFFF`
  - `Raycast Target: OFF`
- Asígnalo al campo `Detail Title Text`

**b) ArtistText**
- Clic derecho → **UI > Text - TextMeshPro** → renómbralo `ArtistText`
- **Rect Transform**: Height: `40`
- **TMP**:
  - Text: `Artist Name`
  - Font Size: `24`
  - Alignment: Left Middle
  - Color: gris claro `#AAAAAA`
  - `Raycast Target: OFF`
- Asígnalo al campo `Detail Artist Text`

**c) DescText**
- Clic derecho → **UI > Text - TextMeshPro** → renómbralo `DescText`
- **Rect Transform**: Height: `160`
- **TMP**:
  - Text: `Description...`
  - Font Size: `20`
  - Alignment: Left Top
  - Overflow: `Overflow` (el texto puede salirse — el contenedor lo limita visualmente)
  - Word Wrapping: ✅ ON
  - Color: `#CCCCCC`
  - `Raycast Target: OFF`
- Asígnalo al campo `Detail Description Text`

**e) ButtonsArea**
- Clic derecho → **Create Empty** → renómbralo `ButtonsArea`
- **Rect Transform**: Height: `70`
- Agrega **Horizontal Layout Group**:
  - Spacing: `10`
  - Child Alignment: `Middle Left`
  - Control Child Size → Width: ☐ OFF, Height: ✅ ON
  - Child Force Expand → Width: ✅ ON, Height: ☐ OFF

Dentro de `ButtonsArea`, crea 3 botones (**UI > Button - TextMeshPro**):

| Name | Label | Background color |
|---|---|---|
| `BtnEasy` | `64 Pieces` | Bronze `#CD7F32` |
| `BtnNormal` | `144 Pieces` | Silver `#9E9E9E` |
| `BtnHard` | `256 Pieces` | Gold `#FFD700` |

Para cada botón:
- **Rect Transform**: Height: `60` (el Horizontal Layout Group controla el Width)
- **Image** component → **Color** = el color de la tabla
- **TMP hijo**:
  - Font Size: `22`, Font Style: **Bold**
  - Alignment: Center Middle, Color: `#1A1A1A` (texto oscuro sobre colores claros)
- Asígnalo al campo correspondiente (`Btn Easy`, `Btn Normal`, `Btn Hard`)
- Asigna el TMP hijo a `Btn Easy Text`, `Btn Normal Text`, `Btn Hard Text`


---

## PASO 11 — BottomNav (barra de navegación inferior)

1. Selecciona `ContentRoot` → clic derecho → **UI > Image** → renómbralo `BottomNav`
2. **Rect Transform**:
   - Anchors: `bottom-stretch`
   - Pivot: `0.5, 0`
   - Left: `0`, Right: `0`
   - Pos Y: `0`
   - Height: `80`
3. Color: `R:15  G:15  B:15  A:255` (casi negro)
4. Agrega **Horizontal Layout Group**:
   - Spacing: `0`
   - Child Force Expand Width: ON, Height: ON
   - Child Alignment: Middle Center

5. Crea 4 botones hijos (clic derecho sobre `BottomNav` → **UI > Button - TextMeshPro**):

   | Name | Label | Image Color |
   |---|---|---|
   | `BtnHome` | `🏠  Home` | `#896C4A` (active by default) |
   | `BtnCompleted` | `✓  Completed` | grey `#383838` |
   | `BtnSettings` | `⚙  Settings` | grey `#383838` |
   | `BtnSearch` | `🔍  Search` | grey `#383838` |

   Para cada botón:
   - Selecciona el botón → en el componente **Image** → **Color** = el color de la tabla
   - Selecciona el TMP hijo → Font Size: `26`, Alignment: Center Middle

6. Assign in `NativeGalleryController`:
   - `Btn Home` → BtnHome
   - `Btn Completed` → BtnCompleted
   - `Btn Settings` → BtnSettings
   - `Btn Search` → BtnSearch

---

## PASO 12 — Crear el ArtworkCard Prefab

El título va **debajo de la imagen** — las pinturas tienen distintos ratios (portrait, landscape)
y con `Preserve Aspect` se verían mal en un card cuadrado. Con este layout la imagen ocupa
la parte superior y el título tiene su propia franja abajo.

1. En la jerarquía, crea un GameObject temporal fuera de NativeGallery  
   (clic derecho en la raíz → **UI > Button - TextMeshPro**)
2. Renómbralo `ArtworkCard`
3. **Rect Transform**: Width: `220`, Height: `270`  
   *(220 de imagen + 50 de título)*
4. Quita el texto hijo que crea Unity por defecto (el TMP "Button") — no lo necesitamos
5. En el componente **Image** → Color: `#1A1A1A`

Crea los hijos **en este orden**:

   **a) Thumbnail** ← imagen superior
   - Clic derecho sobre `ArtworkCard` → **UI > Image** → renómbralo `Thumbnail`
   - Anchors: **stretch-stretch**
   - Left: `0`, Right: `0`, Top: `0`, Bottom: `50` *(deja espacio para el título abajo)*
   - Image component:
     - **Source Image**: asigna cualquier sprite temporal para que aparezcan los campos de configuración
     - `Image Type`: `Simple`
     - `Preserve Aspect`: ✅ ON
     - Color: `R:255 G:255 B:255 A:0` (transparente — el script asigna el sprite en runtime)
     - **Raycast Target**: ☐ OFF

   **b) Title** ← franja inferior
   - Clic derecho → **UI > Text - TextMeshPro** → renómbralo `Title`
   - Anchors: **bottom-stretch**, Pivot `0.5, 0`
   - Left: `5`, Right: `5`
   - Pos Y: `0`
   - Height: `50`
   - TMP:
     - Font Size: `18`
     - Word Wrapping: ✅ ON
     - Overflow: `Ellipsis`
     - Alignment: **Center Middle**
     - Color: **blanco** `#FFFFFF`
     - Font Style: **Bold**
     - `Raycast Target`: ☐ OFF

   **c) CompletedBadge** ← esquina superior derecha
   - Clic derecho → **UI > Image** → renómbralo `CompletedBadge`
   - Anchors: **top-right**, Pivot `1, 1`
   - Width: `36`, Height: `36`
   - Pos X: `-6`, Pos Y: `-6`
   - Color: `#4CAF50` (verde)
   - `Raycast Target`: ☐ OFF

6. Selecciona `ArtworkCard` → agrega el componente `Artwork Card UI`
7. Asigna en `Artwork Card UI`:
   - `Thumbnail Image` → el componente Image de `Thumbnail`
   - `Title Text` → el TMP de `Title`
   - `Completed Badge` → el GameObject `CompletedBadge`
8. **Guardar como Prefab**:
   - Arrastra `ArtworkCard` desde la jerarquía a `Assets/ArtUnbound/Prefabs/`
   - Elimina el `ArtworkCard` temporal de la jerarquía

---

## PASO 13 — Asignar en NativeGalleryController

Selecciona el GameObject `NativeGallery`. En el Inspector, asigna:

| Campo | Qué arrastrar |
|---|---|
| **Content Root** | El GameObject `ContentRoot` |
| **Btn Home** | The Button `BtnHome` |
| **Btn Completed** | The Button `BtnCompleted` |
| **Btn Settings** | The Button `BtnSettings` |
| **Btn Search** | The Button `BtnSearch` |
| **Catalog View** | El GameObject `CatalogView` |
| **Completed View** | El GameObject `CompletedView` |
| **Settings View** | El GameObject `SettingsView` |
| **Search View** | El GameObject `SearchView` |
| **Catalog Grid Container** | El `Content` dentro de `CatalogView > Viewport > Content` |
| **Completed Grid Container** | El `Content` dentro de `CompletedView > Viewport > Content` |
| **Search Input Field** | El componente TMP_InputField de `SearchInput` |
| **Search Grid Container** | El `Content` de `SearchResults > Viewport > Content` |
| **Search Empty Label** | El TMP de `SearchEmptyLabel` |
| **Music Slider** | El Slider de `MusicSection` |
| **Sfx Slider** | El Slider de `SfxSection` |
| **Haptics Toggle** | El Toggle de `HapticsSection` |
| **Music Value Text** | El TMP de porcentaje en `MusicSection` |
| **Sfx Value Text** | El TMP de porcentaje en `SfxSection` |
| **Detail Panel** | El GameObject `DetailPanel` |
| **Detail Artwork Image** | El Image de `ArtworkImage` |
| **Detail Title Text** | El TMP `TitleText` |
| **Detail Artist Text** | El TMP `ArtistText` |
| **Detail Description Text** | El TMP `DescText` |
| **Btn Easy** | El Button `BtnEasy` |
| **Btn Normal** | El Button `BtnNormal` |
| **Btn Hard** | El Button `BtnHard` |
| **Btn Easy Text** | El TMP hijo de `BtnEasy` |
| **Btn Normal Text** | El TMP hijo de `BtnNormal` |
| **Btn Hard Text** | El TMP hijo de `BtnHard` |
| **Btn Detail Close** | El Button `BtnClose` |
| **Artwork Card Prefab** | El prefab `ArtworkCard` creado en PASO 12 |

---

## PASO 14 — Asignar en GameBootstrap

1. Selecciona el GameObject `GameBootstrap` en la jerarquía
2. En el Inspector, en la sección **UI Controllers**:
   - Arrastra `NativeGallery` al campo `Native Gallery`
3. En la sección **Feature Flags**:
   - `Use Native Gallery` → ✅ ON
   - `Use Radial Gallery` → ❌ OFF

---

## PASO 15 — EventSystem: no necesitas hacer nada

> ✅ **Verificado directamente en la escena:**  
> El `EventSystem` dentro de `MR Interaction Setup` ya tiene **`XRUIInputModule`**  
> (XR Interaction Toolkit) configurado y funcionando. Este módulo maneja automáticamente  
> **todos** los Canvas World Space que tengan `TrackedDeviceGraphicRaycaster`.  
> No necesitas agregar `OVRInputModule`, `PointableCanvasModule`, ni ningún otro módulo.

### Verificación rápida

1. Selecciona `MR Interaction Setup > EventSystem` en la jerarquía
2. Confirma que tiene el componente **`XRUIInputModule`** con `Enable XR Input: ✅`
3. Si está — listo, no hay nada más que hacer

### ¿Qué hace el XRUIInputModule?

- Detecta automáticamente todos los `TrackedDeviceGraphicRaycaster` en la escena
- Envía eventos de ray (apuntar con la muñeca) y poke (dedo directo) a los elementos UI
- Funciona con los XR Ray Interactors que ya están configurados en el XR Rig
- Es la razón por la que el `MainCanvasPanel` funciona sin configuración adicional

---

## PASO 16 — Prueba rápida en el Editor

Antes de probar en el Quest:

1. Activa temporalmente el GameObject `NativeGallery` en la jerarquía
2. Dale Play en el Editor
3. En la ventana **Scene** deberías ver el panel flotando a `0.75m` frente a la cámara
4. En la consola busca: `[NativeGallery] Inicializado con 201 obras.`
5. Si hay errores de referencias null, revisa el PASO 13

---

## Notas de ajuste fino

- **Panel muy grande o pequeño**: ajusta `Gallery Distance` (default 0.75m) y el Scale del Canvas (0.001)
- **Grid tiene demasiadas/pocas columnas**: cambia `Constraint Count` en el `GridLayoutGroup`
- **Cards muy pequeñas**: aumenta `Cell Size` en el GridLayoutGroup (default 220×220)
- **El scroll es muy lento/rápido**: ajusta `Scroll Sensitivity` en el ScrollRect (default 30)
- **El teclado no aparece al buscar**: verifica que el EventSystem tenga `XRUIInputModule` con `Enable XR Input: ✅` (PASO 15)
