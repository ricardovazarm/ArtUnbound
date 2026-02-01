# Guía: Cómo Agregar Nuevas Pinturas al Catálogo

Esta guía detalla el proceso para introducir nuevas obras de arte en **Art Unbound** para que aparezcan en la galería y puedan jugarse como puzzles.

## Requisitos Previos
- Tener acceso al Project de Unity.
- Tener el archivo de imagen de la obra (JPG o PNG).
- (Opcional) Tener una versión redimensionada para thumbnail (ej. 512x512).

---

## Paso 1: Importar la Textura

1.  Navega a la carpeta: `Assets/ArtUnbound/Artworks`.
2.  Crea una subcarpeta con el nombre del artista o ID de la obra si deseas mantener orden (Opcional).
3.  Arrastra tu imagen (ej. `MonaLisa_4k.jpg`) a esta carpeta.
4.  **Configuración de Importación (Import Settings):**
    - Selecciona la imagen en Unity.
    - En el **Inspector**:
        - **Texture Type**: `Sprite (2D and UI)`. (Crucial para usarla en UI).
        - **Sprite Mode**: `Single`.
        - **Max Size**: `4096` (o `8192` si es ultra alta definición y necesaria).
        - **Compression**: `High Quality` o `Normal Quality`. (Evitar `Low`).
        - **Check "Read/Write"**: ACTÍVALO. (Necesario para que el script `PuzzleBoard` lea los píxeles y corte las piezas).
    - Presiona **Apply** al final del Inspector.

## Paso 2: Crear la Definición de la Obra (Data Asset)

1.  Navega a la carpeta: `Assets/ArtUnbound/Data/Artworks` (Si no existe `Artworks`, créala dentro de `Data` para orden).
2.  Haz clic derecho en un espacio vacío.
3.  Selecciona: `Create` > `ArtUnbound` > `Artwork Definition`.
4.  Dale un nombre al archivo (ej. `Artwork_MonaLisa`).
5.  **Rellenar los Datos en el Inspector:**
    - **Identification:**
        - **Artwork Id**: Un ID único sin espacios (ej. `da_vinci_mona_lisa`).
    - **Metadata:**
        - **Title**: El nombre real (ej. `La Gioconda`).
        - **Author**: Artista (ej. `Leonardo da Vinci`).
        - **Year**: Año de creación (ej. `1503`).
        - **Description**: Breve historia o dato curioso.
        - **Museum**: Museo donde está (ej. `Museo del Louvre`).
        - **Art Movement**: (ej. `Renacimiento`).
    - **Display:**
        - **Aspect Ratio**: Calcula Ancho/Alto (ej. 0.67). *Nota: El sistema adaptativo del PuzzleBoard usará la textura real, este valor es más referencial para UI.*
    - **Textures:**
        - **Thumbnail**: Arrastra la imagen importada (o una versión pequeña).
        - **Full Image**: Arrastra la imagen importada (La de alta calidad).
        - **Puzzle Texture**: (Opcional) Si quieres una versión especial para el puzzle, úsala aquí. Si no, déjalo vacío y usará `Full Image`.
    - **Unlock Settings:**
        - **Is Base Content**: Marca `True` si debe estar disponible desde el inicio.
    - **Difficulty Hints:**
        - Configura `Complexity`, `Color Variety` y `Detail Level` según tu criterio (solo informativo).

## Paso 3: Registrar en el Catálogo

El sistema no detecta el archivo automáticamente, debes añadirlo a la lista maestra.

1.  Navega a la carpeta: `Assets/ArtUnbound/Data`.
2.  Selecciona el archivo **ArtworkCatalog**.
3.  En el Inspector, verás una lista llamada **Artworks**.
4.  Haz clic en el botón `+` para añadir un elemento al final.
5.  Arrastra tu nuevo archivo (ej. `Artwork_MonaLisa`) a ese nuevo espacio vacío.

## Paso 4: ¡Probar!

1.  Dale **Play** en Unity.
2.  Ve a "Galería".
3.  Deberías ver tu nueva pintura en la lista.
4.  Selecciónala y comprueba que al jugar, el puzzle se adapta correctamente a su forma (gracias al sistema de grid adaptativo).

---

### Tips Adicionales
*   **Imágenes Rectangulares:** No te preocupes por el Aspect Ratio. El sistema `PuzzleBoard` ajustará las piezas automáticamente.
*   **Pesos de Archivo:** Intenta que las imágenes `Full Image` no superen los 4-6 MB para no saturar la memoria del Quest.
*   **Read/Write Enabled:** Si olvidas activar esto en el Paso 1, el juego dará error al intentar cortar las piezas.
