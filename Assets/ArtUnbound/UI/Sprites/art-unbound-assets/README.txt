ART UNBOUND - Assets de UI (PNG para Unity)
===========================================

PALETA (tinte de los assets blancos en Unity):
  Texto/hueso (principal) ... #EFE9DF   (títulos, valores, texto fuerte)
  Texto suave (secundario) .. #C9C1B4   (artista, descripciones, cuerpo)
  Texto tenue (terciario) ... #8A8276   (etiquetas en versalitas, pies, "placeholder de…")
  Laton claro (acento).. #D4C089
  Laton oscuro ......... #896C4A
  Base de panel ........ #1C1916



PANELES (/panels) - 9-SLICE
  panel_dark_opaque.png ....... 192x192 (fondo casi opaco)
  panel_dark_translucent.png .. 192x192 (semitransparente; prueba ambos en MR)
  Sprite Editor -> Border: L=48 R=48 T=48 B=48 | Image Type: Sliced

BOTONES (/buttons) - 9-SLICE, blancos (tinelos)
  button_outline.png .. 96x96  Border 24 (botones normales)
  button_solid.png .... 96x96  Border 24 (boton de compra/primario)
  toggle_pill.png ..... 240x96 Border L=120 R=120 T=0 B=0 (estira solo horizontal)

ICONOS (/icons) - 128x128, blancos con alfa (tinelos). Sprite normal (sin 9-slice):
  settings, home, search, collection, check_eye, exit, close,
  chevron_left, chevron_right, play, medal
  (No incluido 'store': se elimina de la UI.)

GLIFOS DE TAMANO (/sizes) - 128x128, blancos:
  size_a_coffee (3x3) | size_a_break (4x4) | size_an_afternoon (6x6)

NOTAS
  - Iconos/botones en BLANCO para tenir con Image.color. Paneles ya traen color horneado.
  - 'medal' es un icono PLANO placeholder; para relieve metalico usa un asset pintado/3D.
  - Import: Texture Type 'Sprite (2D and UI)', Alpha Is Transparency ON, Compression alta/None.


====================================================
TIPOGRAFIA
====================================================

Dos familias, ambas Google Fonts (licencia SIL Open Font License -> uso libre,
incluso embebidas en el juego comercial):

  Cormorant Garamond  (serif de "cedula de museo": titulos de obra, artista, branding)
    https://fonts.google.com/specimen/Cormorant+Garamond
  Hanken Grotesk      (sans limpia: toda la UI funcional)
    https://fonts.google.com/specimen/Hanken+Grotesk

Regla simple:
  Cormorant Garamond = todo lo "expositivo" (nombre de arte / artista / marca).
  Hanken Grotesk     = toda la UI funcional (botones, etiquetas, contadores, nav).

MAPEO POR TEXTO
  Branding "Art Unbound" ............ Cormorant Garamond 600 ("Unbound" en italic 400)
  Titulo de obra (catalogo) ......... Cormorant Garamond 500
  Titulo de obra (detalle modal) .... Cormorant Garamond 600 (grande)
  Nombre del artista ................ Cormorant Garamond Italic 400 (en laton)
  Titulo de obra (armado) ........... Cormorant Garamond 600
  Etiquetas metadatos (MUSEO, etc.).. Hanken Grotesk 500  (MAYUSCULAS + tracking amplio)
  Valores de metadatos .............. Hanken Grotesk 400
  Botones de tamano (A Coffee...).... Hanken Grotesk 500
  Boton de compra ................... Hanken Grotesk 600 (subtexto 400)
  Navegacion inferior (Home/Search/Collection) . Hanken Grotesk 500 (MAYUSCULAS + tracking)
  Toggle de modo (VR/MR) ............ Hanken Grotesk 400 (MAYUSCULAS)
  Etiquetas de seccion ("Solve a puzzle") ...... Hanken Grotesk 400 (MAYUSCULAS + tracking)
  Contadores y temporizador ......... Hanken Grotesk 400 (el numero en 600)
  Pieza musical en reproduccion ..... Hanken Grotesk 400

YA INCLUIDOS en la carpeta /fonts (no necesitas descargar nada):
  CormorantGaramond-Medium.ttf (500)
  CormorantGaramond-SemiBold.ttf (600)
  CormorantGaramond-Italic.ttf (400 italic)
  HankenGrotesk-Regular.ttf (400)
  HankenGrotesk-Medium.ttf (500)
  HankenGrotesk-SemiBold.ttf (600)
  + CormorantGaramond-OFL.txt y HankenGrotesk-OFL.txt (licencias).

NOTA: en Google Fonts estas familias ahora son "variable fonts" (un solo archivo con
un slider de peso). TextMeshPro prefiere fuentes ESTATICAS (un archivo por peso), por
eso ya las instancie en los 6 TTF de arriba. Eso explica que al bajarlas del sitio "no
descargaran todos los tamanos": era un unico archivo variable, no uno por peso.


====================================================
MINI-TUTORIAL: FUENTES EN UNITY (TextMeshPro)
====================================================
Importante: TMP NO interpola pesos como el navegador. Cada peso (400, 500, 600,
italic) es un Font Asset separado. Por eso descargas un TTF por peso.

PASO 1 - Usar los TTF ya incluidos
  Estan en /fonts (6 archivos estaticos, un peso por archivo). No necesitas bajar
  ni instanciar nada de Google Fonts. Arrastralos a Assets/Fonts/ en tu proyecto.

PASO 2 - Crear un Font Asset de TMP por cada TTF
  Selecciona un .ttf -> click derecho -> Create -> TextMeshPro -> Font Asset.
  (O: Window -> TextMeshPro -> Font Asset Creator, eliges el TTF y le das Generate
   Font Atlas, luego Save.)
  Repite para los 6 TTF. Tendras 6 Font Assets (.asset).

  Recomendado en el Font Asset Creator (texto del juego en ingles):
    - Character Set: "ASCII" o "Custom Range" Latin basico (achica el atlas).
    - Render Mode: SDFAA (nitido en VR a cualquier distancia).
    - Atlas Resolution: 1024x1024 suele bastar; sube a 2048 si recortas glifos.

PASO 3 - Asignar la fuente a cada texto
  En cada componente TextMeshPro - Text, campo "Font Asset" -> elige el Font Asset
  segun el MAPEO de arriba. (No uses "Bold"/"Italic" del estilo para cambiar peso:
  usa el Font Asset del peso correcto; el estilo Bold de TMP es un "faux bold".)

PASO 4 - Replicar el look del mockup
  - VERSALITAS: escribe el texto en MAYUSCULAS (o pon el campo en mayusculas) y
    sube "Character Spacing" en el TMP (p. ej. 4 a 8) para el tracking amplio.
    Aplica esto a: nav inferior, etiquetas de metadatos, toggle, "Solve a puzzle".
  - COLOR LATON: en el componente TMP, Vertex Color = #D4C089 (o usa <color> tags).
    Hueso/texto normal = #EFE9DF.
  - El italic del nombre del artista: usa CormorantGaramond-Italic Font Asset
    (no el faux italic).

PASO 5 (opcional) - Fuente por defecto
  Project Settings -> TextMeshPro Settings -> Default Font Asset = Hanken Grotesk 400,
  para que los textos nuevos salgan ya con la sans de UI.

CONSEJO VR: usa Render Mode SDF/SDFAA (no Bitmap) para que el texto se vea nitido
sin importar la distancia a la que el jugador mire el panel.