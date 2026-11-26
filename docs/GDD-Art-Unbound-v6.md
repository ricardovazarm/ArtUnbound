# Documento de Diseño de Juego (GDD): Art Unbound
- Versión: 6.1
- Fecha: Noviembre 2026
- Plataforma: Meta Quest (Quest 2, Quest 3, Quest 3S, Quest Pro)

**Cambios respecto a 6.0 (junio 2026).** Esta revisión alinea el documento con lo que está implementado en el juego:
- Tutorial de mano fantasma en el primer arranque (4.9).
- Hand tracking con cualquiera de las dos manos (4.5).
- Post-juego sin botones de colgado: el cuadro queda listo para tomarse; único botón "Play Again" (4.8).
- Botón "Hang on Wall" en el detalle de las obras completadas, y la misma obra puede colgarse varias veces (4.2, 5.1).
- Collection lista placas y mejoras; las obras completadas se cuelgan desde su detalle (8.5).
- Las mejoras Cédula/Marco/Lámpara también otorgan una placa colgable; la lámpara ya no proyecta luz sobre la pintura (8.2).
- Menú y tablero a 0.75 m (antes 0.4 m); recentrado y cambio automático manos/controles (11.8).
- Galería VR alineada al menú y teleport solo sobre piso (6).
- Catálogo de 251 obras; Apéndice A regenerado desde el catálogo real.
- Chips de filtro de la búsqueda: diferidos, no implementados (11.7).

---

## 1. Concepto de Alto Nivel

Art Unbound es un juego de puzzles para Meta Quest. Los jugadores reconstruyen obras maestras clásicas ensamblando piezas en un lienzo flotante frente a ellos, y al terminar pueden colgar el cuadro completado en su entorno, donde persiste entre sesiones.

En **Modo Mixto (MR)** — disponible en Quest 3, Quest 3S y Quest Pro — el cuadro se cuelga en una pared real del espacio físico del usuario mediante anclajes espaciales persistentes. En **Modo Virtual (VR)** — disponible en todos los dispositivos Quest — el cuadro se cuelga en las paredes de una galería de arte 3D inmersiva.

El espacio del usuario se transforma con el tiempo en su propio museo personal: las obras que completa, las placas que gana y los elementos de galería que desbloquea conviven en sus paredes como un registro visible de su recorrido.

---

## 2. Resumen del Juego

- **Género**: Puzzle, Casual, Bienestar.
- **Modos**: Realidad Mixta (MR) y Realidad Virtual (VR).
- **Modelo de negocio**: Free-to-Play con un desbloqueo único de pago (ver sección 12).
- **Propuesta de valor única**: El espacio del usuario es parte del juego. Los cuadros completados se convierten en decoración persistente — en la pared real del hogar (MR) o en la galería virtual personal (VR). Las piezas tienen grosor tridimensional (0.5 cm) que les otorga presencia física.

**Roles de los dos modos (intención de diseño).** MR y VR no compiten: cada uno hace lo que el otro físicamente no puede, y cumplen funciones distintas.
- **MR es el modo distintivo y el gancho de adquisición.** Poner arte clásico persistente en tu hogar real es lo único que ningún competidor replica; es el ángulo con el que se vende y diferencia el juego. Su retención se construye sobre la magia del mundo real (ver 15.6).
- **VR es el modo de profundidad, social y retención.** Entornos tematizados, espacio ilimitado y compañía remota viven aquí (ver 15.1–15.5), además de ser la puerta de entrada para dispositivos sin passthrough.
- Regla práctica: **se desarrolla profundidad en VR, pero se vende y diferencia por MR.** Ambos modos deben tener su propio loop de retención; ninguno debe quedar como el modo "flaco".

---

## 3. Pilares de Diseño

- **Inmersión tangible**: Las piezas tienen volumen de 0.5 cm y se ensamblan en un lienzo flotante ergonómico.
- **Magia cotidiana**: Las obras completadas decoran permanentemente el espacio del usuario — físico en MR, virtual en VR.
- **Flexibilidad ergonómica**: El armado ocurre siempre en un lienzo flotante a distancia fija (0.75 m del usuario, unos 25 cm por debajo de la altura de los ojos; la misma posición que el menú), permitiendo jugar sentado o de pie en cualquier lugar. La integración con la pared es una recompensa post-juego.
- **Bienestar sin presión**: No hay penalizaciones, puntuaciones ni relojes que castiguen. El tamaño del rompecabezas es una elección de comodidad, no de dificultad competitiva.

---

## 4. Flujo del Juego

### 4.1. Menú Principal

El menú principal es un panel flotante en espacio MR/VR, colocado a 0.75 m del usuario. Tiene un cuerpo central que cambia según la pestaña y una barra de navegación:

**Configuración (pestaña Settings)**
- Volumen de música y volumen de efectos.
- Toggle de **hápticos**.
- Toggles para mostrar/ocultar **cédulas**, **marcos** y **lámparas** (ver 8.2). Cada fila aparece solo cuando esa mejora ya está desbloqueada.
- Toggle **"Show tutorial"**: vuelve a activar el tutorial de mano fantasma sin reiniciar (ver 4.9).

**Cuerpo central — Catálogo de obras**
- Cuadrícula paginada de miniaturas, cada una con su título. Por defecto muestra primero las 12 obras gratuitas y, al hacer scroll, el resto del catálogo.
- Las obras completadas muestran un **badge de completada**: una palomita dentro de un círculo (`icon_completed`), colocada en una esquina de la miniatura sobre un pequeño disco oscuro para que sea legible sobre cualquier obra. Indica que la obra fue armada; no varía según el tamaño elegido. **No se usa una medalla para esto** (la medalla se reserva para hitos; ver sección 8).
- Las obras **bloqueadas** se muestran **a imagen completa, sin candado encima**: la miniatura nunca se tapa. El único indicador de bloqueo es un **pequeño candado** (`icon_lock`) en una esquina (distinta a la del badge de completada), también sobre disco oscuro. El deseo de compra nace de ver la obra, no de esconderla.
- **Búsqueda integrada en el propio catálogo** (no hay pantalla de búsqueda separada): un campo de búsqueda sobre el grid. Con el campo vacío se ve el catálogo completo; al escribir, el mismo grid se filtra in-situ. Los **chips de filtro tocables** (por movimiento, artista y estado) están diferidos y no existen todavía. Ver sección 11.7.
- Seleccionar una obra abre su **detalle como ventana modal** (ver 4.2).

**Barra inferior — Navegación**
- **Home**: catálogo principal (con búsqueda integrada).
- **Collection**: placas y mejoras del jugador, obtenidas y por obtener (ver 8.5). Las placas obtenidas se cuelgan desde aquí.
- **Settings**: configuración (arriba).
- **Botón de modo**: "VR" cuando se está en MR y "MR" cuando se está en VR. Solo aparece en visores con passthrough (Quest 3, 3S y Pro); el Quest 2 arranca y permanece en VR. Entrar a VR exige controles: si se toca "VR" con las manos, un aviso lo indica durante 3 segundos.

### 4.2. Detalle de Obra y Tamaños de Rompecabezas

Al seleccionar una obra del catálogo se abre su **detalle como ventana modal**, que muestra la pintura, su título, artista, museo de origen, movimiento artístico y año. El detalle se comporta distinto según si la obra está desbloqueada:

- **Obra desbloqueada:** muestra "Solve a puzzle" con **tres botones de tamaño** (A Coffee / A Break / A Movie) para iniciar el puzzle. Los botones muestran solo su etiqueta de tamaño (sin récord de tiempo).
- **Obra ya completada:** además de los botones de tamaño aparece el botón **"Hang on Wall"**. Al tocarlo, la obra enmarcada aparece flotando junto al panel, lista para tomarla y colgarla (ver 5.1). Puede usarse las veces que se quiera: cada vez genera una copia nueva.
- **Obra bloqueada** (no incluida en el tier gratuito, con el catálogo aún no comprado): en lugar de los botones de tamaño, el detalle muestra un **botón de compra** que dispara el desbloqueo único de todo el catálogo (ver sección 12). Este es el único punto de venta del juego: no existe una sección de tienda separada. La obra bloqueada **se muestra completa y nítida** (nunca tapada por un candado); el estado de bloqueo se comunica por la ausencia de los botones de tamaño y la presencia del botón de compra, más un pequeño candado de esquina en la miniatura del catálogo (ver 4.1). El botón de compra muestra el **precio localizado que devuelve Meta**, nunca un precio escrito a mano.

**Disposición del modal (la pintura como protagonista).** La imagen ocupa el lado izquierdo grande, sobre una "pared de galería" oscura, centrada y **contenida sin recortar** (una obra apaisada deja aire arriba/abajo; una vertical llena de alto). Los datos (título, artista, museo, movimiento, año) van a la derecha, con la acción —los botones de tamaño o el botón de compra— anclada abajo. El panel del modal es **100% opaco** (no translúcido), ya que ocupa casi todo el campo de visión y la transparencia dejaría ver el entorno detrás.

Los tres tamaños se plantean como **compromisos de tiempo** según cuánto desee el jugador permanecer en una sesión, no como niveles de dificultad competitiva.

El número de piezas no es un valor fijo: el sistema lo **deriva del tamaño físico objetivo de cada pieza** (definido en `PuzzleConfig`) sobre un tablero que cabe en un cuadrado de 0.6 m. El algoritmo calcula columnas y filas dividiendo cada dimensión del tablero entre el tamaño objetivo de pieza, por lo que el conteo final **varía según el aspect ratio** de cada pintura.

| Tamaño | Tamaño de pieza | Piezas aprox.* | Tiempo estimado** |
|--------|-----------------|----------------|-------------------|
| A Coffee     | 8 cm | ~40–64   | ~10–20 min |
| A Break      | 6 cm | ~60–100  | ~25–45 min |
| A Movie      | 4 cm | ~120–225 | ~1–2 h |

*El conteo varía por aspect ratio: a igual tamaño de pieza, las obras cuadradas (1:1) generan más piezas que las apaisadas (16:9). Por ejemplo, a 8 cm: una obra 1:1 da ~64 piezas y una 16:9 da ~40.
**Estimaciones a validar en playtesting; varían por jugador.

Piezas más grandes (tamaño de entrada) son además más fáciles de ver y manipular, reforzando que el tamaño menor es también el más cómodo, adecuado para la audiencia casual.

El tamaño es una decisión personal de comodidad. El cuadro final es idéntico en los tres tamaños — lo único que cambia es el número y tamaño de las piezas. Por ello, el tamaño elegido **no otorga marcos, placas ni recompensas distintas**: completar una obra cuenta igual hacia los logros sin importar el tamaño (ver sección 9).

### 4.3. Marco del Cuadro

Por defecto, todo cuadro completado se presenta **sin marco, montado sobre una base/borde de madera lisa**. Un borde liso escala correctamente a cualquier aspect ratio (a diferencia de un marco ornamentado, que se deforma al estirarse entre proporciones tan distintas como una obra vertical y una panorámica).

El **marco es un asset cosmético que se desbloquea por progreso** (a las 25 obras; ver 8.4), no un elemento mecánico. El marco perdió cualquier significado funcional —el número de piezas se deriva del tamaño de pieza, no del marco (ver 4.2)—, por lo que es puramente de presentación. Es un marco liso/uniforme que solo cambia de grosor o material según el tier, escalable a cualquier proporción. Una vez desbloqueado se aplica automáticamente a todas las obras colgadas, con un **toggle en Configuración** para apagarlo y dejar los cuadros sobre la base de madera.

### 4.4. Pantalla de Armado

Al iniciar el puzzle, el espacio se divide en tres zonas:

**Centro — Tablero del Puzzle**
- El lienzo flotante a 0.75 m del usuario donde se arman las piezas. Aparece en la misma posición en la que estaba el menú.
- Ocupa el espacio visual principal.

**Izquierda — Panel de Información**
- Título y artista de la obra.
- Imagen de referencia completa de la obra.
- Barra de progreso del armado.
- Contadores en tiempo real: total de piezas, colocadas correctamente, colocadas incorrectamente, y temporizador.
- Nombre de la pieza musical y compositor en reproducción.
- Botón **Exit** (salir al menú principal).
- Botón **Check** (resalta las piezas mal colocadas; ver sección 4.6).

**Derecha — Bandeja de Piezas**
- Piezas disponibles para colocar, organizadas en páginas.
- Botones de navegación para avanzar y retroceder entre páginas.

### 4.5. Mecánica de Colocación de Piezas

El armado admite **dos modos de entrada**:

- **Con las manos (hand tracking, MR):** el usuario toma una pieza con pinch (pulgar-índice); la pieza sigue su mano y, al soltarla cerca del tablero, hace snap al **slot disponible más cercano** — no al punto exacto donde se suelta. Funciona con **cualquiera de las dos manos**, igual que el scroll de la bandeja.
- **Con los controles (MR y VR):** el usuario apunta y presiona el gatillo para **seleccionar una pieza**, luego apunta y presiona el gatillo en la **celda de destino** del tablero, y la pieza se coloca exactamente ahí. En MR con controles el comportamiento es idéntico al de VR.

En ambos casos: si la pieza queda en su slot correcto, aumenta el contador de correctas y hay confirmación visual y sonora; si no, aumenta el de incorrectas. Una pieza colocada puede tomarse de nuevo y reubicarse. El puzzle se completa cuando todas las piezas están en su slot correcto, lo que activa la animación de revelación.

> **Nota**: No hay rechazo por morfología; el sistema registra si el resultado es correcto o no mediante los contadores.

### 4.6. Resaltar Piezas Incorrectas (botón "Check")

El panel izquierdo incluye un botón **"Check"** que el usuario puede presionar en cualquier momento durante el armado. Al activarlo:

1. Cada pieza colocada en el lugar equivocado emite un **burst de partículas rojas** en su posición.
2. La pieza realiza un **movimiento de oscilación** (wiggle) de lado a lado durante ~0.45 segundos.
3. Se reproduce el **sonido de colocación incorrecta** una sola vez para todo el conjunto.
4. Los efectos se aplican de forma **escalonada** (80 ms entre pieza y pieza) para que el usuario pueda distinguir cuáles son.

El botón no mueve ni reposiciona las piezas — solo las señala visualmente para que el usuario decida si las quiere corregir.

### 4.7. Hitos durante el Armado

El sistema detecta y celebra:
- Completar una fila completa.
- Completar una columna completa.
- Completar un borde (lado exterior del puzzle).
- Completar el marco exterior completo.

Cada hito dispara un mensaje contextual en pantalla y efectos de partículas.

### 4.8. Finalización y Post-Juego

1. Al colocar todas las piezas correctamente se activa la animación de revelación: el cuadro aparece completo con su marco.
2. Se muestra el panel de post-juego con un mensaje **genérico de finalización, sin lenguaje de "premio"**:
   - Título: **"Artwork Complete"** (Cormorant Garamond).
   - Línea de acción (tenue): **"Ready to hang in your gallery"** (Hanken).
   - **Colgar no requiere botón.** Cuando hay paredes disponibles, el cuadro terminado queda listo para tomarse y sobre él aparece la instrucción, que cambia según el modo de entrada activo: con manos, *"Pinch to grab the frame and release it against a wall to hang your masterpiece."*; con controles, *"Point and click to select the frame, then click on a wall to hang it."*
   - Único botón: **"Play Again"**, que vuelve a armar la misma obra desde cero con el mismo tamaño (por ejemplo, para que lo intente otra persona). Para salir se usa el botón **Exit** del panel izquierdo.
   - **No aparece ninguna medalla ni texto de "ganaste una medalla".** Completar una obra la marca como completada (badge de palomita en el catálogo) y habilita colgarla; la recompensa real es el cuadro en la pared, no un icono. Las placas se reservan para hitos (ver sección 8).
   - **Hito condicional:** solo si completar esta obra **cruza el umbral de una placa** (p. ej. la última obra de un pintor, o la 25 de un movimiento), el panel muestra la placa recién obtenida bajo el encabezado **"Milestone unlocked!"**. Si no se cruzó ningún umbral, no aparece.
3. El jugador puede colgar el cuadro en una pared real (MR) o en la galería virtual (VR) — ver secciones 5 y 6.
4. Para colgar una obra ya completada más adelante, el jugador abre su detalle en el catálogo y usa el botón **"Hang on Wall"** (ver 4.2).

### 4.9. Tutorial de Primer Arranque

La primera vez que se abre el juego, una **mano fantasma** enseña por demostración sobre la interfaz real, en tres momentos:

1. Con el catálogo visible: toca la primera obra.
2. Con el detalle abierto: toca el botón del tamaño más pequeño.
3. Ya en el puzzle: hace pinch sobre una pieza de la bandeja y la lleva a su slot correcto.

Cada demostración se reproduce una vez y se desvanece. **Nunca bloquea la entrada ni espera confirmación**: el jugador puede adelantarse en cualquier momento. Al terminar la tercera, el tutorial queda marcado como visto. El toggle "Show tutorial" de Settings lo vuelve a activar en caliente y arranca al regresar al catálogo. El tutorial solo corre en la ruta de MR; sustituye al carrusel de onboarding anterior, que ya no se usa.

---

## 5. Sistema de Colgado — Modo MR (Mixed Reality)

### 5.1. Flujo de Colocación

El colgado tiene tres puntos de entrada:

- **Post-juego:** al terminar una obra, el cuadro recién armado queda listo para tomarse (ver 4.8).
- **Detalle de una obra completada:** el botón "Hang on Wall" hace aparecer la obra enmarcada flotando junto al panel, con el lado mayor de 0.5 m (ver 4.2). Si se cierra el detalle sin colgarla, desaparece.
- **Collection:** para las placas obtenidas (ver 8.5), con la misma mecánica.

**La misma obra puede colgarse varias veces.** Cada copia es independiente: tiene su propio anclaje, se reposiciona y se retira por separado, y persiste por separado.

El flujo admite dos modos de entrada:

- **Con las manos:** el usuario toma el cuadro, acerca la mano (con el cuadro) a una pared real y lo suelta; al quedar **a 20 cm o menos de la pared**, el cuadro se fija y se crea un anclaje espacial persistente.
- **Con los controles:** el usuario hace clic en el cuadro para **seleccionarlo** y luego clic en el punto de la pared donde quiere colgarlo.

Si se suelta o apunta lejos de una pared, el cuadro no se cuelga y sigue disponible para intentarlo de nuevo. El mismo flujo aplica a las **placas** (ver sección 8): son objetos colgables que el usuario coloca libremente donde desee.

### 5.2. Reposicionamiento y Retiro

- Los cuadros y placas ya colgados pueden tomarse de nuevo (con las manos o seleccionándolos con el control).
- Al recolocar cerca de una pared (≤ 20 cm) / apuntar a la pared: el objeto se reposiciona y el anclaje espacial se actualiza.
- Al soltar o apuntar lejos de una pared: el objeto se retira y desaparece.

### 5.3. Persistencia

- Los cuadros y placas colgados persisten entre sesiones mediante anclas espaciales (AR Foundation sobre el runtime de Meta).
- Al iniciar la app, todos los objetos colgados anteriormente se reconstituyen en sus posiciones reales, incluidas todas las copias de una misma obra.
- La detección de paredes usa ARPlaneManager (planos verticales AR) con fallback por raycast.

### 5.4. Curaduría del Espacio

La pared real del usuario funciona como una **selección curada y rotativa** de sus obras favoritas, no como un archivo completo de todo lo armado. El usuario cuelga, descuelga e intercambia libremente las obras que desea exhibir. Las placas temáticas (sección 8.3) permiten representar logros de colección completos — por ejemplo, haber armado muchas obras de un mismo pintor — mediante un solo objeto, sin necesidad de colgar físicamente todas las obras correspondientes.

---

## 6. Modo VR (Virtual Reality)

### 6.1. Galería Virtual

En modo VR el usuario se encuentra dentro de una galería de arte 3D inmersiva, jugada con los controles (apuntar y gatillo). El puzzle se arma en el mismo lienzo flotante que en MR. Las paredes de la galería son superficies virtuales donde puede colgar sus obras completadas y placas. A diferencia de MR, la galería virtual no tiene límite de espacio físico: el usuario puede exhibir su colección completa en múltiples paredes.

La galería arranca como un **entorno base deliberadamente sobrio**, que el jugador va poblando y mejorando con los **assets que desbloquea** mediante logros (ver sección 8). El estado inicial es austero a propósito — es el "antes" de una progresión—, pero debe ser digno y acogedor para el jugador nuevo que aún no ha desbloqueado nada.

**Entrada a VR.** En Quest 3, 3S y Pro el juego arranca siempre en MR y el jugador pasa a VR con el botón de modo; se requieren controles. Al entrar, **la galería se acomoda alrededor del menú**: ni el menú ni el jugador se mueven. En Quest 2, que no tiene MR, el juego arranca directamente en la galería. Los cuadros colgados en MR se ocultan mientras se está en VR, y viceversa.

**Construcción de la galería base (decisión de producción).** La sala se **construye internamente como geometría paramétrica** (caja: piso, techo y paredes como volúmenes simples) en vez de comprarse como entorno completo del Asset Store, por tres razones de peso para Quest: (1) geometría mínima = presupuesto sano de polígonos y draw calls para mantener 72–90 FPS en estéreo; (2) la iluminación se **hornea** (baked lightmaps), no en tiempo real — en URP las luces de spot/point en tiempo real son caras y tienen restricciones de sombra; los spots del riel son sobre todo decorativos + lightmap; (3) el color de pared y los materiales son **variables propias**, de modo que la misma caja se **re-tinta** para generar las galerías temáticas de Fase 2 (vinotinto, verde inglés, gris cálido) sin comprar ni optimizar salas nuevas. Del Asset Store se aprovechan solo **materiales** (parquet/pared/zócalo) y props (bancas, pedestales, riel de luz), no la sala. El generador expone parámetros (dimensiones, número de anclajes de obra, separación, altura de cuadro) y coloca anclajes (`ArtAnchor`) a altura de museo (~1.5 m al centro) sobre cada pared. Dimensiones base de referencia: 12 × 8 × 4 m; navegación por teleport. *Estado: el horneado de luces para Quest sigue pendiente.*

### 6.2. Mecánica de Colocación de Piezas en VR

La colocación con controles es la descrita en 4.5: apuntar + gatillo para seleccionar una pieza, apuntar + gatillo en la celda de destino para colocarla ahí. La validación (correcto/incorrecto) funciona igual que en MR.

### 6.3. Flujo de Colgado en VR

Los puntos de entrada son los mismos que en MR (ver 5.1): post-juego, botón "Hang on Wall" del detalle, y Collection para las placas. También aquí la misma obra puede colgarse varias veces. El flujo con controles:

1. El usuario apunta al cuadro completado y presiona el gatillo para **seleccionarlo**.
2. Se desplaza (teleportación) hacia la pared de la galería donde quiere colocarlo.
3. Apunta al punto exacto de la pared y presiona el gatillo para **fijar el cuadro**.

### 6.4. Reposicionamiento en VR

- Los cuadros y placas ya colgados pueden reposicionarse con el mismo flujo: apuntar → gatillo para seleccionar, moverse a la nueva ubicación, apuntar → gatillo para fijar.

### 6.5. Diferencias respecto al modo MR

| Aspecto | Modo MR | Modo VR |
|---------|---------|---------|
| Entorno | Habitación real (passthrough) | Galería 3D virtual (base, se puebla con assets desbloqueados) |
| Entrada soportada | Manos o controles | Controles |
| Colocación de piezas | Manos: al slot más cercano · Controles: a la celda elegida | Controles: a la celda elegida |
| Colgado de cuadros | Manos: acercar a la pared y soltar · Controles: clic en cuadro + clic en la pared | Controles: clic + teleportación + clic en la pared |
| Detección de paredes | ARPlaneManager (planos AR reales) | Raycasting contra layer VRWall |
| Persistencia | Anclas espaciales (una por copia) | JSON local (GalleryPersistenceService), poses relativas a la galería |
| Espacio de exhibición | Limitado por la pared física real | Ilimitado (múltiples paredes virtuales) |
| Disponibilidad | Quest 3, Quest 3S y Quest Pro | Todos los dispositivos Quest |
| Tutorial de primer arranque | Sí | No |

### 6.6. Locomoción en VR

- **Teleportación**: apuntar al suelo con el controlador y presionar el gatillo para desplazarse. Solo se acepta el **piso** como destino; las paredes y otras superficies se rechazan.
- **Snap Turn**: rotación en incrementos fijos (izquierda/derecha con el joystick).
- No se usa locomoción continua para reducir mareo.

---

## 7. Morfología de las Piezas

### 7.1. Estados de Arista

Cada lado de una pieza puede tener uno de tres estados:
- **Plano**: exclusivo para bordes exteriores del puzzle.
- **Positivo**: pestaña triangular hacia afuera.
- **Negativo**: ranura triangular hacia adentro.

Piezas vecinas siempre tienen aristas complementarias (Positivo ↔ Negativo) en su borde compartido, garantizando que morfológicamente encajen.

### 7.2. Variedad

Las piezas internas generan su morfología a partir de aristas compartidas asignadas con una semilla determinista basada en el ID de la obra. Esto produce hasta 16 combinaciones distintas por pieza interna (2⁴), dando a cada obra un patrón único y reproducible entre sesiones.

### 7.3. Colocación y Validación

El sistema no rechaza piezas por morfología. Cualquier pieza soltada cerca del tablero se coloca automáticamente en el slot disponible más cercano. La corrección de la colocación se registra mediante los contadores del panel de información:
- **Correctas**: piezas en su slot correspondiente por ID.
- **Incorrectas**: piezas colocadas en un slot que no les corresponde.

El puzzle se considera completo cuando todas las piezas están en sus slots correctos.

---

## 8. Sistema de Recompensas y Progresión

El juego incluye una capa de progresión basada en **logros**, cuya divisa son objetos cosméticos visibles en el espacio del usuario. Esta capa es independiente del acceso al contenido: lo que el jugador desbloquea con la compra (sección 12) es el catálogo de obras; los logros nunca bloquean obras ni contenido pagado, solo otorgan elementos decorativos.

### 8.1. Principios

- **Acceso y progresión están separados.** Los logros no gatean obras; premian la exploración del catálogo sin forzar un orden.
- **Completar una obra no es un logro con nombre.** Es un hecho binario (hecha / no hecha) que se marca con el **badge de palomita** (ver 4.1) y habilita colgar la obra. Los **logros con nombre** (placas) se reservan para **hitos** de colección o de comportamiento. Por eso completar un solo rompecabezas no otorga "medalla": la recompensa es poder colgar el cuadro.
- **Cada recompensa es visible en el espacio.** La pared (MR) o la galería (VR) es la vitrina del jugador y lo que comparte con otros. Las recompensas son objetos que se cuelgan o se aplican a las obras, no entradas en un menú oculto.
- **La jerarquía visual refleja logro acumulado, nunca decisiones puntuales.** Los rangos cobre/plata/oro/platino se ganan por acumulación de obras (placa de estatus, ver 8.4) y definen el material de **todas** las placas del museo; no se aplican a obras individuales ni dependen de decisiones puntuales como el tamaño elegido.

### 8.2. Mejoras de Presentación (desbloqueos automáticos por progreso)

Lo único que el jugador **cuelga manualmente** son **cuadros y placas**. Las mejoras de presentación se **desbloquean automáticamente** al alcanzar umbrales de obras completadas (ver 8.4) y se **aplican solas** a las obras colgadas, elevando el aspecto de galería de toda la pared. Cada una tiene un **toggle individual en Configuración**, que aparece al desbloquearse, por si el jugador prefiere una pared más limpia. Un cambio de toggle se refleja en cada obra la siguiente vez que se instancia (al colgarla o al cargar la galería), no en vivo sobre las ya colgadas.

Además, al alcanzar cada umbral el jugador recibe una **placa conmemorativa colgable** de esa mejora (**Museum Label**, **Frame**, **Picture Light**), que aparece en Collection y se cuelga como cualquier otra placa (ver 8.5).

- **La Cédula** (desbloqueo a las 10 obras). Placa-cédula estilo museo que aparece debajo de cada cuadro colgado, con el mismo lenguaje metálico que las placas de logro (mismo material del tier vigente, texto grabado oscuro). Campos y tipografía:
  - **Título de la obra** → Cormorant Garamond SemiBold, color `#241B12`.
  - **Autor** → Cormorant Garamond *Italic*, color `#2E2418`.
  - **Año · Movimiento · Museo** → Hanken Grotesk Regular, pequeño y tenue, color `#4A3A28`, en una sola línea separada por " · " (p. ej. "1665 · Dutch Golden Age · Mauritshuis").
  - Nota VR: la cédula carga bastante texto en poco espacio y cuelga lejos; validar legibilidad a escala/distancia reales. Título y autor son indispensables; año/movimiento/museo son "bonito de saber" y pueden recortarse si no se leen.
- **El Marco** (desbloqueo a las 25 obras). Marco liso/uniforme sobre la base de madera de cada cuadro (ver 4.3). Escalable a cualquier aspect ratio.
- **La Lámpara** (desbloqueo a las 50 obras). Luminaria tipo museo montada sobre cada cuadro colgado: placa de fijación a la pared, brazo y pantalla inclinada hacia la pintura, con la boca encendida. **No proyecta un baño de luz sobre la pintura**: se probó y se retiró porque alteraba la obra.

Las tres aplican igual en MR y VR. No tienen variantes propias: su acabado lo determina el tier de material de la placa de estatus (ver 8.4). *Estado: el marco aún no tiene material Platino propio y usa el de Oro.*

### 8.3. Placas (Maestro y Gran Maestro)

Objetos colgables que el jugador obtiene al **dominar** un pintor o un movimiento artístico completando obras. Se cuelgan libremente donde el usuario desee. Son los **únicos logros con nombre** del juego (junto con las placas de comportamiento y la placa de estatus).

- Son assets de diseño fijo y dimensión constante, con identidad visual propia por tema (por ejemplo, motivo de girasoles para Van Gogh, azul de Delft para Vermeer). Se recomienda producirlas con **plantillas por familia** (una base para "Maestro de autor", otra para "movimiento", otra para "progreso/estatus") cambiando solo texto e icono, para que se lean como un sistema coherente y para reducir el trabajo de arte.
- El tamaño en que se completaron las obras es indiferente.

**Reglas de umbral (deliberadamente exigentes — una placa debe sentirse ganada):**

**Por autor — "Maestro":** se obtiene al **completar TODAS las obras del autor** presentes en el catálogo. Aplica a autores con **6 o más obras**, con **Da Vinci como única excepción** (5 obras). Excepción de escala: **Van Gogh** tiene tantas obras (34) que su Maestro se alcanza a las **15**, no al completarlas todas.

**Por autor — "Gran Maestro":** existe **solo para Van Gogh**, al **completar sus 34 obras**. Es el logro más difícil y prestigioso del juego; ningún otro autor tiene Gran Maestro.

**Por movimiento — "Maestro" y "Gran Maestro":** solo los movimientos con **más de 10 obras** tienen placa. La regla depende del tamaño del movimiento:
- Movimientos **grandes (más de 25 obras)** — Post-Impresionismo, Impresionismo, Renacimiento, Siglo de Oro Holandés: **Maestro al llegar a 25** obras del movimiento; **Gran Maestro al completarlas todas**.
- Movimientos **medianos (11–25 obras)** — Realismo, Barroco, Romanticismo: **solo Maestro, al completarlas todas** (no tienen Gran Maestro).
- Movimientos con 10 obras o menos (Manierismo, Rococó, Neoclasicismo, Simbolismo, Pre-Rafaelita) **no tienen placa de movimiento** en lanzamiento; son candidatos para actualizaciones futuras.

Las obras de un pintor cuentan también para su movimiento; el progreso hacia ambas placas avanza en paralelo. El set completo de lanzamiento está en la sección 8.6.

**Formato visual único (el contenido es lo importante, no la forma).**

Todas las placas —Maestro, Gran Maestro, comportamiento y la de estatus— comparten **un mismo molde rectangular tipo cédula de museo**. Entre una placa y otra **solo cambia el texto**; con el progreso global **solo cambia el material** (ver 8.4). No hay formas ni siluetas distintas por tema: una pared de placas idénticas en el mismo material se lee como una colección curada y coherente con la estética de museo, y evita el aspecto de "trofeos de juego casual".

- **Producción: 1 molde × 4 materiales = 4 assets base.** El texto se compone **dinámicamente con TMP** encima del molde. Esto reduce el arte de placas de ~23 piezas a 4, y permite contenido dinámico gratis (fecha, subtítulos, variantes Maestro/Maestros de Fase 2) sin diseñar arte nuevo.

**Anatomía y tipografía de la placa** (de arriba a abajo) — *font de cada parte para construirla:*

| Parte | Contenido (ejemplo) | Fuente | Tamaño/peso |
|-------|---------------------|--------|-------------|
| **Título del logro** (hero) | "Van Gogh — Master", "Impressionism — Grand Master", "On Display" | **Cormorant Garamond SemiBold** | Grande — la línea que se lee de un vistazo |
| **Condición / subtítulo** | "Completed all 34 works", "Reached 25 works", "Hung 5 artworks" | **Hanken Grotesk Medium** | Pequeño, en versalitas o normal |
| **Fecha de obtención** (recomendada) | "Earned June 2026" | **Hanken Grotesk Regular** | El más pequeño, tenue |
| **Grabado monocromo** (opcional) | motivo del tema (girasol, etc.) | — (grabado en el mismo metal, sin color) | Pequeño, no obligatorio |

- **Color del texto:** como la placa es metálica y clara, el texto va **grabado en tono oscuro** (NO el hueso `#EFE9DF` que se usa sobre paneles oscuros). Tono de grabado recomendado: **`#241B12`** (oscuro cálido), legible sobre los cuatro materiales. La **jerarquía se logra por tamaño y peso, no por color** (el título domina; condición y fecha decrecen).
- **Material por tier (acabado de la placa):** Cobre → Plata → Oro → Platino, determinado por el tier de estatus global (8.4) y aplicado a todas las placas a la vez. En producción, el molde se reskinea en los 4 materiales.
- **VR:** como todo el peso recae en la tipografía, dimensionar la placa y el texto con resolución y tamaño suficientes para leerse cómodo a la distancia de colgado. Validar a escala real.

La **placa de estatus** sigue el mismo sistema tipográfico (Cormorant SemiBold para el rango/título; Hanken para agregados y fecha), sobre el mismo molde y material del tier vigente.

### 8.4. Placa de Estatus y Escalera de Progreso Global

Objeto colgable único que muestra el avance global del jugador en el mundo, en lugar de en una pantalla de menú. Es el **motor de una sola escalera de progreso** (por total de obras completadas) que en cada peldaño hace dos cosas: **sube el material/tier** y, en algunos peldaños, **desbloquea una mejora de presentación**.

- Se desbloquea al completar el primer rompecabezas y se cuelga donde el usuario desee.
- Es de **tamaño fijo**, dimensionado para alojar todos los logros eventualmente.

**Escalera de progreso global:**

| Total de obras | Rango | Material/tier | Desbloqueo de presentación |
|----------------|-------|---------------|----------------------------|
| Primera obra | **Visitor** | **Cobre** | Placa de estatus desbloqueada |
| 10 | **Collector** | **Plata** | **Cédula** (datos bajo cada cuadro) |
| 25 | **Connoisseur** | **Oro** | **Marco** (sobre la base de madera) |
| 50 | **Curator** | **Platino** | **Lámpara** (luminaria sobre cada cuadro) |
| 100 | **Patron** | Platino (sin cambio) | Hito reconocido; **sin asset nuevo por ahora** |

El **rango** es el texto principal (hero) de la placa de estatus y evoluciona con el avance: Visitor → Collector → Connoisseur → Curator → Patron, contando la historia de "visito arte" a "soy mecenas de mi propio museo". El peldaño de 100 (Patron) no da material nuevo (ya estás en Platino) pero **sí otorga el rango máximo**, por lo que no queda vacío.

- **El material del tier (Cobre → Plata → Oro → Platino) aplica a TODAS las placas del museo** — estatus, Maestro y comportamiento — no solo a la de estatus. Al subir de tier, toda la colección de placas "asciende de categoría" a la vez. Es una **propiedad global**, no se congela por placa: una placa ganada en cobre se actualiza a plata cuando el jugador sube. Esto da un progreso global muy visible y permite producir las placas como **una forma por tipo, reskineada en 4 materiales** (ver 8.3).
- **El peldaño de 100 queda deliberadamente sin asset de presentación nuevo por ahora.** Se evitó atarlo a una recompensa exclusiva de VR (p. ej. mejora de ambiente de galería), porque castigaría al jugador de MR —el modo diferenciador— obligándolo a entrar a VR solo para cobrarla. Cualquier recompensa futura para este o nuevos umbrales debe funcionar **igual en MR y VR**. A las 50 el jugador ya alcanzó Platino, así que en 100 conserva el tope de material y su museo lleno; el hito se reconoce vía la placa de estatus.

**Qué muestra la placa de estatus (solo estado global, sin duplicar logros):**

Como las placas de Maestro y comportamiento ya se cuelgan por separado en la pared, y la sección Collection ya lista lo obtenido y lo pendiente, la placa de estatus **no repite el inventario de logros**. Muestra únicamente el resumen global del jugador — "your museum at a glance":
- **Rango (hero):** el rango vigente (Visitor → Patron) en Cormorant Garamond SemiBold, grande. Es el texto que se lee de un vistazo y comunica el nivel de avance.
- **Agregados:** una línea de datos en Hanken pequeño con un par de totales — obras completadas (p. ej. "47 / 251") y placas obtenidas (p. ej. "8 plaques"). Números resumen, no la lista.
- **Material:** el acabado de la placa es el del tier vigente (Cobre/Plata/Oro/Platino), que sube con el rango.

No incluye la lista cronológica de logros (eso vive en las placas colgadas y en Collection), lo que además la hace más simple y mucho más legible en VR a distancia.

### 8.5. Collection (Menú)

Sección del menú que reúne las recompensas del jugador: lo que ya obtuvo y lo que puede llegar a obtener.

- **Placas obtenidas** (Maestro, Gran Maestro, comportamiento, estatus y las tres de mejora). Cada tarjeta muestra una miniatura 3D de la placa. Al seleccionar una se abre una ventana con la instrucción de colgado mientras la placa flota al frente, lista para tomarla y colgarla con el mismo mecanismo de las secciones 5 (MR) y 6 (VR). El botón Close cancela y regresa a Collection.
- **Placas aún no obtenidas:** se muestran bloqueadas, con la condición necesaria en la propia tarjeta (p. ej. "Completed all 15 works", "Complete 25 artworks"). Es el lugar donde el jugador descubre qué puede obtener y cómo.

**Las obras completadas no se listan en Collection.** Se cuelgan desde su detalle en el catálogo, con el botón "Hang on Wall" (ver 4.2).

La cédula, el marco y la lámpara se aplican automáticamente a las obras colgadas y se activan/desactivan desde Configuración (ver 8.2). Lo que aparece en Collection es su placa conmemorativa, que sí es colgable.

### 8.6. Set de Placas de Lanzamiento

Conteos tomados del catálogo real (campo `artMovement` / autor). Todas las condiciones son sobre **obras completadas**.

**Placas por autor — Maestro = completar TODAS las obras del autor (autores con ≥6, + Da Vinci como excepción). 9 placas:**

| Autor | Obras en catálogo | Maestro |
|-------|-------------------|---------|
| Vincent van Gogh | 34 | a las **15** (excepción de escala) |
| Claude Monet | 15 | completar las 15 |
| Pierre-Auguste Renoir | 11 | completar las 11 |
| Johannes Vermeer | 10 | completar las 10 |
| Rembrandt van Rijn | 10 | completar las 10 |
| Edouard Manet | 8 | completar las 8 |
| Paul Cezanne | 7 | completar las 7 |
| Raphael | 6 | completar las 6 |
| Leonardo da Vinci | 5 | completar las 5 (excepción, <6 por reconocimiento) |

**Placa por autor — Gran Maestro (solo Van Gogh). 1 placa:**

| Autor | Gran Maestro |
|-------|--------------|
| Vincent van Gogh | completar las **34** |

**Placas por movimiento — solo movimientos con >10 obras. 7 Maestro + 4 Gran Maestro = 11 placas:**

| Movimiento | Obras | Maestro | Gran Maestro |
|------------|-------|---------|--------------|
| Post-Impressionism | 51 | a las 25 | completar las 51 |
| Impressionism | 40 | a las 25 | completar las 40 |
| Renaissance | 36 | a las 25 | completar las 36 |
| Dutch Golden Age | 35 | a las 25 | completar las 35 |
| Realism | 23 | completar las 23 | — |
| Baroque | 21 | completar las 21 | — |
| Romanticism | 20 | completar las 20 | — |

*(Manierismo, Rococó, Neoclasicismo, Simbolismo y Pre-Rafaelita tienen ≤10 obras y no llevan placa de movimiento en lanzamiento.)*

> **Pendiente de implementación:** al retirar del catálogo el Vermeer duplicado, dos placas quedaron con el umbral anterior en sus assets (`Assets/ArtUnbound/Data/Collectibles/`): *Vermeer — Master* pide 11 obras y *Dutch Golden Age — Grand Master* pide 36. Con el catálogo actual (10 y 35) **no pueden obtenerse** hasta que se corrijan.

**Placas de comportamiento (premian *exhibir*, no *armar* — eje distinto al de la placa de estatus). 2 placas:**

| Placa | Condición |
|-------|-----------|
| First Hung | Colgar el primer cuadro (MR o VR) |
| On Display | Tener **5 obras colgadas a la vez** (MR o VR) |

*("On Display" mide obras exhibidas simultáneamente, no completadas, para no duplicar la escalera de progreso del estatus. Una vez ganada, se conserva aunque luego se descuelguen obras. El nombre "Curator" queda reservado como rango de la placa de estatus, ver 8.4.)*

**Placa de estatus (progreso global; ver 8.4):** muestra solo el estado global (rango + agregados), no la lista de logros. Rangos por umbral: primera obra **Visitor** (Cobre) · 10 **Collector** (Plata) · 25 **Connoisseur** (Oro) · 50 **Curator** (Platino) · 100 **Patron**. El material del tier aplica a todas las placas. Desbloqueos de presentación en el camino: Cédula (10), Marco (25), Lámpara (50).

**Total de lanzamiento:** 9 (Maestro autor) + 1 (Gran Maestro Van Gogh) + 7 (Maestro movimiento) + 4 (Gran Maestro movimiento) + 2 (comportamiento) = **23 placas con nombre**, más la placa de estatus (*Museum Status*) con sus 5 rangos y las tres placas de mejora (*Museum Label* a las 10, *Frame* a las 25, *Picture Light* a las 50; ver 8.2). En total, 27 coleccionables en Collection.

El catálogo de placas se amplía con actualizaciones (Gran Maestro de más autores, más movimientos al crecer el catálogo, y más placas de comportamiento de Fase 2; ver sección 15).

---

## 9. Progresión y Guardado

- Se registra el **tiempo de resolución** por obra y por tamaño (récord histórico).
- Se registra el conjunto de **obras completadas** (distintas), que alimenta el progreso hacia las placas temáticas y el nivel de la placa de estatus.
- Las obras completadas cuentan hacia los logros **sin importar el tamaño** en que se armaron.
- No hay sistema de puntuación numérica ni penalizaciones.
- El progreso en puzzles incompletos se guarda automáticamente (al pausar o salir).
- Los datos se guardan localmente en el dispositivo en formato JSON.
- El guardado se realiza automáticamente en `OnApplicationPause` y `OnApplicationQuit`.

---

## 10. Audio

### 10.1. Música

La música del juego es **música clásica** de compositores reconocidos como Vivaldi, Mozart, Beethoven, Bach, Debussy, Chopin, entre otros.

- Se utilizan exclusivamente **grabaciones de dominio público**, lo que permite su uso sin restricciones legales.
- La biblioteca de pistas es configurable (`MusicLibrary`) y se amplía con actualizaciones.
- El panel de configuración del menú principal y el panel de información en el puzzle muestran el título de la pieza y el compositor en reproducción.

### 10.2. Efectos de Sonido

- Sonido de confirmación y efecto visual en snap correcto.
- Efectos diferenciados para hitos (fila, columna, borde, marco completo).
- Volumen de música y efectos de sonido configurables independientemente.

---

## 11. Interfaz y UX

### 11.1. Menú Principal

Panel flotante en espacio MR/VR (ver sección 4.1):
- **Cuerpo central**: catálogo en cuadrícula paginada de miniaturas; las completadas muestran el **badge de palomita** (no medalla); las bloqueadas se ven completas con un candado de esquina (no tapadas); incluye búsqueda integrada (ver 11.7); seleccionar abre el detalle modal.
- **Barra inferior**: Home, Collection, Settings y el botón de modo VR/MR (este último solo en visores con passthrough). No hay destino "Search" separado; la búsqueda vive integrada en Home.
- **Settings**: volúmenes de música y efectos, hápticos, toggles de cédulas / marcos / lámparas (visibles al desbloquearse) y toggle del tutorial.

### 11.2. Pantalla de Puzzle

Tres zonas (ver sección 4.4):
- **Centro**: tablero de armado.
- **Izquierda**: información de la obra, imagen de referencia, barra de progreso, contadores (total/correctas/incorrectas/temporizador), pieza musical en reproducción, botón Exit y botón Check.
- **Derecha**: bandeja de piezas paginada con botones de navegación.

### 11.3. Panel de Hitos

Mensajes contextuales al completar filas, columnas, bordes y el marco exterior completo.

### 11.4. Panel Post-Juego

Mensaje "Artwork Complete" + "Ready to hang in your gallery" + instrucción de colgado sobre el cuadro (texto de manos o de controles según el modo activo) + botón "Play Again"; placa recién obtenida con el encabezado "Milestone unlocked!" si se cruzó un umbral (ver 4.8). Sin medalla y sin botones de colgado.

### 11.5. Temática Visual de Botones

Sistema de temas unificado: color normal `#896C4A`, hover/seleccionado `#d4c089`. El botón de compra (Unlock) usa un degradado de latón horneado `#D4C089 → #896C4A` con texto oscuro `#1A140C`.

### 11.6. Tipografía y Jerarquía de Texto

Dirección "museo premium". Dos familias con roles claros:
- **Cormorant Garamond** (serif): títulos de obra, branding y "momentos de museo" (p. ej. "Artwork Complete"). Viste el contenido.
- **Hanken Grotesk** (sans): toda la UI funcional — inputs, botones, contadores, etiquetas, metadatos, chips. Hace funcionar la interfaz. Nunca usar serif en texto pequeño o en movimiento: en VR los remates finos pierden nitidez.

**Jerarquía de color de texto (3 niveles + acento):**
- Principal `#EFE9DF` (hueso): títulos de obra, valores, texto fuerte.
- Secundario `#C9C1B4`: artista, descripciones, cuerpo.
- Terciario/tenue `#8A8276`: etiquetas en versalitas, leyendas, contadores secundarios, placeholders, estados "no encontrado".
- Acento `#D4C089` (latón claro): texto que debe destacar en cálido (p. ej. artista en cursiva, dato resaltado).

Base de panel `#1C1916`; panel del modal de detalle (100% opaco) `#18130F` — un poco más oscuro/cálido para sentirse "sala de visualización"; pared del área de imagen `#1B1714`.

### 11.7. Catálogo, Búsqueda y Filtros

- **Sin pantalla de búsqueda separada.** El catálogo (Home) es siempre el estado por defecto y muestra todas las obras; la búsqueda es un filtro sobre ese mismo grid, no un destino aparte. Esto evita la pantalla en blanco y que la "búsqueda" duplique al catálogo. (El catálogo es finito —251 obras—, navegable; no requiere el patrón de búsqueda de catálogos gigantes tipo streaming.)
- **Campo de texto**: placeholder en Hanken tenue `#8A8276` ("Search artworks..."); el texto escrito en Hanken `#EFE9DF`. Hoy es el único filtro.
- **Estado sin resultados:** título **"No artworks found"** (Cormorant, `#EFE9DF`) + subtexto **"Try another name, artist, or movement"** (Hanken, `#8A8276`). Tono sobrio, sin humor ni emojis.
- **Diferido, no implementado:** los **filtros tocables (chips)** por movimiento, artista y estado (libre/bloqueada), y el botón **"Browse all"** del estado vacío. La intención de diseño se mantiene: en VR los chips deben priorizarse sobre el campo de texto, porque teclear con el teclado virtual es incómodo.

### 11.8. Posición del Menú, Recentrado y Modo de Entrada

- **Ancla única.** El menú y el tablero comparten posición: 0.75 m frente al usuario y unos 25 cm por debajo de la altura de los ojos, calculada al arrancar tras estabilizarse el tracking.
- **Recentrado.** Al recentrar con el botón Meta sostenido, el menú se recoloca frente al usuario. Si el recentrado ocurre a media partida, **el tablero no se mueve** (se perdería la posición que el jugador acomodó) y el menú se recoloca al volver a él. En VR no aplica.
- **Manos y controles.** El juego detecta solo si se usan manos o controles y adapta la interfaz (por ejemplo, las instrucciones de colgado). Pasar a manos es inmediato; pasar a controles exige una confirmación breve para evitar falsos positivos, y nunca cambia a media interacción.

---

## 12. Modelo de Monetización

### 12.1. Estructura Free-to-Play

Art Unbound se distribuye como una **aplicación gratuita** en la Meta Quest Store. La app gratuita incluye un conjunto inicial de **12 obras icónicas** completamente jugables, junto con todas las funcionalidades del juego (modos MR y VR, sistema de colgado, sistema de recompensas y progresión).

### 12.2. Desbloqueo del Catálogo Completo

El catálogo completo se desbloquea con una **compra única de $9.99 USD**, que otorga acceso permanente a las **239 obras restantes** (251 en total). Es una compra única, no una suscripción, implementada como un add-on de Meta IAP (`catalog_complete`); el precio que ve cada jugador es el localizado por Meta para su región.

- **No existe una sección de tienda.** El desbloqueo se ofrece de forma contextual: al abrir el detalle de cualquier obra bloqueada, en lugar de los botones de tamaño aparece un **botón de compra**. Tocarlo dispara el desbloqueo único de **todo** el catálogo (no es compra por obra).
- No existen packs temáticos ni bundles.
- Tras la compra, todas las obras quedan disponibles de inmediato, sin progresión intermedia que limite su acceso.

### 12.3. Selección del Tier Gratuito

Las **12 obras gratuitas** se eligieron por **máximo reconocimiento** (que el jugador diga "¡esa la conozco!") combinado con **variedad de formato, paleta y textura de armado**, para que el tier gratis muestre el rango del catálogo y despierte ganas de más. Criterio adicional de protección del incentivo de colección: **un solo Monet y un solo Van Gogh** en el tier gratis (no regalar las cartas que mueven las placas de Maestro).

| # | Obra | Artista | Movimiento |
|---|------|---------|------------|
| 1 | Mona Lisa | Leonardo da Vinci | Renaissance |
| 2 | The Starry Night | Vincent van Gogh | Post-Impressionism |
| 3 | Girl with a Pearl Earring | Johannes Vermeer | Dutch Golden Age |
| 4 | The Birth of Venus | Sandro Botticelli | Renaissance |
| 5 | The Kiss (Gustav Klimt) | Gustav Klimt | Symbolism |
| 6 | A Sunday on La Grande Jatte | Georges Seurat | Post-Impressionism |
| 7 | Impression, Sunrise | Claude Monet | Impressionism |
| 8 | The Night Watch | Rembrandt van Rijn | Dutch Golden Age |
| 9 | The Creation of Adam | Michelangelo | Renaissance |
| 10 | Wanderer above the Sea of Fog | Caspar David Friedrich | Romanticism |
| 11 | Whistler's Mother | James McNeill Whistler | Realism |
| 12 | Las Meninas | Diego Velazquez | Baroque |

Cubre Renacimiento, Post-impresionismo, Siglo de Oro Holandés, Simbolismo, Impresionismo, Romanticismo, Realismo y Barroco, con mezcla de formatos (vertical, horizontal, panorámico, casi cuadrado) y de texturas de armado (puntillismo, fresco, claroscuro). Se descartó deliberadamente del tier gratis *The Garden of Earthly Delights* (Bosch) por ser demasiado densa/perturbadora para una primera impresión contemplativa; permanece en el catálogo de pago.

---

## 13. Beneficios Cognitivos y de Bienestar

Art Unbound no es únicamente entretenimiento — combina deliberadamente tres actividades con beneficios documentados para la salud mental y cognitiva.

### 13.1. Rompecabezas y Función Cognitiva

Armar rompecabezas ejercita simultáneamente múltiples áreas del cerebro:

- **Memoria visual y espacial**: reconocer formas, colores y patrones y recordar dónde encajan.
- **Pensamiento lógico y resolución de problemas**: evaluar qué pieza corresponde a cada espacio.
- **Concentración y atención**: mantener el foco durante períodos sostenidos.
- **Coordinación visuomotora**: en Art Unbound esto se potencia gracias al seguimiento de manos en espacio real.

Estudios sugieren que este tipo de actividad contribuye a mantener la agilidad mental y puede ayudar a retardar el deterioro cognitivo asociado al envejecimiento.

### 13.2. Música Clásica y el Cerebro

La música clásica — repertorio central de Art Unbound — tiene efectos bien documentados:

- **Reducción del estrés y la ansiedad**: escuchar música clásica activa el sistema parasimpático, disminuyendo el cortisol.
- **Mejora de la concentración**: el conocido "efecto Mozart" sugiere que ciertos tipos de música mejoran temporalmente el rendimiento en tareas cognitivas.
- **Estado de flujo**: la música instrumental sin letra reduce distracciones y facilita el estado de concentración profunda (flow), ideal para el armado de puzzles.
- **Bienestar emocional**: la exposición regular a música clásica se asocia con mejoras en el estado de ánimo y reducción de la fatiga mental.

### 13.3. Arte y Contemplación

La exposición a obras de arte clásico tiene beneficios propios:

- **Estimulación estética**: apreciar una obra maestra activa regiones del cerebro asociadas al placer y la recompensa.
- **Conocimiento cultural**: cada puzzle es una oportunidad de conocer la obra, su autor, su historia y el museo donde se alberga.
- **Sentido de logro**: completar una obra y verla colgada en el propio espacio genera una satisfacción duradera que va más allá del juego.

### 13.4. La Experiencia Combinada

Art Unbound integra estas tres dimensiones en una sola sesión de juego: el usuario arma un puzzle con las manos en un espacio relajante, acompañado de música clásica, mientras reconstruye una obra maestra. Esta combinación crea una experiencia que es al mismo tiempo **estimulante cognitivamente, relajante emocionalmente y culturalmente enriquecedora** — accesible a cualquier edad y nivel de habilidad.

---

## 14. Escalabilidad Futura

La versión actual opera de forma completamente local (offline first). Más adelante se evaluará la implementación de servicios en la nube y nuevas funcionalidades:

- **Cloud Storage**: descarga de nuevas obras sin actualizar la app completa.
- **Firestore**: respaldo de récords y sincronización entre dispositivos.
- **Remote Config**: eventos globales y ajustes de lógica en tiempo real.
- **Cuadro Semanal**: sistema de desbloqueo semanal basado en la fecha del dispositivo, donde una obra especial estará disponible por tiempo limitado cada semana. La infraestructura base (`WeeklyUnlockService`) ya existe en el proyecto.
- **Ampliación del catálogo de placas**: más placas por pintor y movimiento, guiadas por el uso real.
- **Actualizaciones de contenido**: nuevas obras (aprox. 12 por trimestre) incluidas en actualizaciones de la app.

La iniciativa social y cooperativa, por su mayor alcance técnico, se trata por separado en la sección 15 (Fase 2).

---

## 15. Fase 2: Social, Co-op y Galerías Temáticas

Esta fase es posterior al lanzamiento y se aborda solo si el juego demuestra tracción. Agrupa iniciativas que profundizan la retención en **ambos modos**: en VR (galerías sociales, armado en compañía, galerías temáticas) y en MR (instalaciones temáticas y curaduría del hogar). El principio es que cada modo reciba retención diseñada para su propia magia, no copiada del otro: VR puede fabricar entornos y compañía remota; MR explota que es tu casa real.

Tiene dos tipos de costo distintos. El pilar **social/co-op** es el mayor esfuerzo técnico del proyecto (netcode multijugador, presencia, voz, manejo de desconexiones e infraestructura de red recurrente) y, a la vez, la funcionalidad que más eleva el techo del juego: jugar acompañado es el principal motor de boca-en-boca de la categoría y la única capacidad de la que hoy carece frente a la competencia consolidada. Conviene notar que, como el modelo es de **armado en paralelo** (cada jugador en su propio tablero; ver 15.2) y no colaborativo sobre un mismo rompecabezas, el netcode es más ligero de lo habitual: no requiere sincronizar el estado de cada pieza entre jugadores, solo presencia, voz y eventos de "obra completada". Las **galerías temáticas** son, en cambio, costo de producción de contenido (entornos 3D), y por eso funcionan como el stream de actualizaciones sustanciales que sostiene la retención a largo plazo.

Las tres piezas forman un loop reforzante: completar colecciones desbloquea galerías temáticas → las galerías son el espacio que se muestra y donde se hace co-op → la dimensión social da propósito a seguir completando colecciones. El alcance es **VR primero**: la galería virtual es un espacio compartido tratable; el co-op en MR (que requeriría shared spatial anchors / colocación física) queda como una consideración separada y posterior.

### 15.1. Galerías Sociales (visitas)

- El jugador puede **invitar a otros jugadores a su galería VR** para recorrerla juntos.
- Es la materialización del museo personal: en vez de compartir una captura, un amigo recorre la colección, ve las obras colgadas, las placas temáticas obtenidas y la placa de estatus en el espacio.
- Es el pago de toda la capa de recompensas y curaduría: el espacio construido por el jugador se vuelve un lugar social que mostrar.

### 15.2. Armado en Compañía (sesiones co-op)

El modelo es de **juego en paralelo, no colaborativo**: en una sesión compartida, cada jugador arma su **propio** rompecabezas en su **propio** tablero, en el mismo espacio, viéndose y platicando. No arman el mismo rompecabezas a varias manos — eso resulta incómodo porque cada quien tiene su forma de armar. El valor de la sesión es la compañía mientras cada uno está en lo suyo, igual que dos personas armando rompecabezas físicos en la misma mesa.

- Concretamente: en una galería compartida, cada participante tiene su tablero/caballete; arman en paralelo y conversan por voz.
- **Las obras armadas durante la sesión cuentan para el progreso de todos los participantes.** Si en la sesión un jugador termina una obra y otro termina otra (cada quien en su tablero), ambos acreditan ambas hacia sus placas, galerías y breadth. Así se reparten una colección "cada quien en lo suyo" y los dos avanzan.
- **El crédito compartido solo aplica en vivo, durante la sesión** (en la galería de cualquiera de los participantes). No se pueden armar obras por separado en solitario y luego "juntarlas": el beneficio existe únicamente por jugar realmente juntos en la misma sesión. Las obras armadas en solitario, fuera de sesión, cuentan solo para quien las armó.

### 15.3. Placas de Equipo (Maestro / Maestros)

- La placa de un tema varía según cómo se alcanzó su umbral:
  - **Todo en solitario:** "Maestro del…" (singular) — maestría individual.
  - **Con contribución de co-op:** "Maestros del…" (plural) — si alguna de las obras que contaron para tu umbral se armó en una sesión co-op, recibes la variante plural, memento de un logro compartido, con un diseño que lo distinga (p. ej. dos figuras en lugar de una).
- Un jugador que busque específicamente la placa singular puede alcanzar el umbral armando todas sus obras en solitario.

### 15.4. Reglas anti-trivialización del co-op

- **Solo cuenta lo armado en vivo, en sesión** (ver 15.2): no se pueden combinar obras armadas por separado en solitario. Esto evita el "pooling" de inventarios y asegura que el crédito compartido represente haber jugado realmente juntos.
- **Escalamiento con muchos participantes (decisión de diseño):** el crédito compartido es justo con 2 jugadores (cada uno arma la mitad), pero se trivializa con grupos grandes (p. ej. 6 jugadores armando 2 obras cada uno alcanzan el umbral de 12). La regla de "en vivo en sesión" ya lo mitiga (coordinar una sesión VR sincrónica de muchos es raro). Si se requiere blindarlo más, las opciones son topar el crédito compartido a 2 jugadores o exigir una contribución personal mínima. La calibración se define con playtesting.

### 15.5. Galerías Temáticas (VR)

Entornos de galería VR completos, tematizados por movimiento o por pintor (p. ej. una galería del Renacimiento, una del Impresionismo, una de Monet), que el jugador desbloquea al completar el tema correspondiente. Es una recompensa de mayor escala que una placa: un espacio entero, no un objeto colgable.

- **Exclusivas de VR.** En MR el entorno es el cuarto real del usuario, así que no hay espacio que tematizar. Esto le da al modo VR una identidad propia y contenido exclusivo, en lugar de ser solo la alternativa para dispositivos sin passthrough.
- **Condición de desbloqueo:** la galería temática se otorga al obtener la **placa de Maestro** del tema correspondiente (ver 8.3/8.6) — es decir, completar todas las obras del autor/movimiento (o el umbral de Maestro que aplique, p. ej. 25 para los movimientos grandes, 15 para Van Gogh). La placa marca el dominio del tema y la galería es la recompensa de mayor escala de ese mismo dominio: un espacio entero, no un objeto colgable. Se construye **re-tintando la galería base** (ver 6.1), no como un entorno nuevo comprado.
- **Liberación gradual como stream de contenido.** No se construyen todas de inicio; se liberan poco a poco como actualizaciones sustanciales (empezando por los temas más populares: Impresionismo, Van Gogh, Renacimiento). Liberar *galerías* progresivamente es legítimo porque son recompensas cosméticas/contenido — a diferencia de liberar *obras*, que gatearía el catálogo que el jugador compró.
- **La galería base permanece como entorno por defecto.** El jugador puede cambiar su galería VR activa entre las que haya desbloqueado y exhibir en cada una la colección correspondiente.
- **Incentivo de completitud y de invitación.** La galería es un motor fuerte para terminar las obras de un tema, y en conjunto con el co-op y las visitas (15.1) adquiere propósito social: se desbloquea una galería para tener un espacio que mostrar e invitar a otros.

### 15.6. Retención en MR: Instalaciones Temáticas y Curaduría del Hogar

La retención de MR no copia las features de VR (no se puede tematizar un cuarto real ni traer un avatar remoto cómodamente). Se construye sobre lo único que MR tiene: que es el hogar real del jugador. Tendrá un techo más ligero que la retención VR, pero un loop propio que VR no puede replicar.

- **Instalaciones temáticas (análogo MR de las galerías).** Al obtener la **placa de Maestro** de una colección (mismo criterio que la galería VR; ver 8.3 y 15.5), el jugador desbloquea un *montaje* que aplica a una sección de su pared real: spotlights coordinados en un tono temático, un cartel/cédula del tema (p. ej. "Monet", con fechas) y la placa conmemorativa, dispuestos como una mini-exposición anclada al muro. Convierte una sección de la casa real en una "pared Monet" o un "rincón impresionista". Es el mismo loop de "dominar colección → desbloquear montaje temático" que la galería VR, pero viviendo en el hogar.
- **Curaduría y expansión del hogar como ritual de regreso.** El límite de espacio de la pared real es un motor, no un defecto: obliga a curar (rotar obras, rediseñar la pared por temporada o para visitas). La progresión MR-nativa es expandir el museo a más muros y cuartos de la casa con el tiempo — "haz crecer tu museo por todo tu hogar", algo que VR no puede ofrecer.
- **Dimensión social MR-nativa.** En MR lo social no es co-op remoto, sino: (a) **compartir** — la captura de arte en una sala real es más impactante que en una galería virtual; y (b) **en persona** — alguien físicamente presente se pone el visor y recorre el museo en el espacio real del anfitrión.
- **Presencia ambiental (re-enganche pasivo).** En MR el museo ya está en el cuarto cada vez que el jugador se pone el visor, sin entrar a ninguna galería; la colección lo recibe. Es re-enganche pasivo exclusivo de MR.

---

## 16. Arquitectura Técnica (Resumen)

- **Motor**: Unity 6 LTS (6000.3.10f1)
- **SDK**: Meta XR All-in-One SDK v85.0.0
- **Dispositivos**: Quest 3, 3S y Pro arrancan en MR; Quest 2 arranca directo en VR
- **Tracking**: XR Hands (seguimiento de manos nativo, ambas manos) o controladores en MR; controladores en VR
- **Anclas espaciales (MR)**: AR Foundation + OpenXR (persistencia cross-session)
- **Persistencia VR**: JSON local via `GalleryPersistenceService`
- **Detección de paredes MR**: ARPlaneManager (planos verticales AR) con fallback por raycast
- **Detección de paredes VR**: Raycasting contra layer `VRWall`
- **Datos locales**: JSON via Newtonsoft.Json en `Application.persistentDataPath`
- **Renderizado**: Universal Render Pipeline (URP), passthrough MR en Quest 3/Pro

---

## Apendice A: Catalogo Completo de Obras

El catalogo contiene **251 obras** de dominio publico. La aplicacion gratuita incluye un conjunto inicial de obras iconicas; el resto se desbloquea con la compra unica del catalogo completo (ver seccion 12). El catalogo se amplia con actualizaciones de la aplicacion.

Las obras se listan a continuacion en orden alfabetico por titulo.

| # | Titulo | Artista | Ano | Movimiento | Museo |
|---|--------|---------|-----|------------|-------|
| 1 | A Bar at the Folies-Bergere | Edouard Manet | 1882 | Impressionism | Courtauld Gallery, London |
| 2 | A Burial at Ornans | Gustave Courbet | 1850 | Realism | Musee d'Orsay, Paris |
| 3 | A Dutch Courtyard | Pieter de Hooch | 1658 | Dutch Golden Age | National Gallery of Art |
| 4 | A Lady Writing | Johannes Vermeer | 1665 | Dutch Golden Age | National Gallery of Art |
| 5 | A Sunday on La Grande Jatte | Georges Seurat | 1886 | Post-Impressionism | Art Institute of Chicago |
| 6 | A Young Girl Reading | Jean-Honore Fragonard | 1770 | Rococo | National Gallery of Art |
| 7 | Adeline Ravoux | Vincent van Gogh | 1890 | Post-Impressionism | Cleveland Museum of Art |
| 8 | Adoration of the Magi | Sandro Botticelli | 1475 | Renaissance | Uffizi Gallery |
| 9 | Allegory with Venus and Cupid | Agnolo Bronzino | 1545 | Mannerism | National Gallery, London |
| 10 | Almond Blossom | Vincent van Gogh | 1890 | Post-Impressionism | Van Gogh Museum |
| 11 | Among the Sierra Nevada | Albert Bierstadt | 1868 | Romanticism | Smithsonian |
| 12 | Aristotle with a Bust of Homer | Rembrandt van Rijn | 1653 | Dutch Golden Age | The Met |
| 13 | At the Moulin Rouge | Toulouse-Lautrec | 1892 | Post-Impressionism | Art Institute of Chicago |
| 14 | Bacchus and Ariadne | Tiziano | 1523 | Renaissance | National Gallery, London |
| 15 | Bal du moulin de la Galette | Pierre-Auguste Renoir | 1876 | Impressionism | Musee d'Orsay |
| 16 | Basket of Peaches | Jean-Baptiste Chardin | 1768 | Rococo | Musee du Louvre |
| 17 | Bathers at Asnieres (Study) | Georges Seurat | 1884 | Post-Impressionism | Art Institute of Chicago |
| 18 | Beata Beatrix | Dante Gabriel Rossetti | 1870 | Pre-Raphaelite | Tate Britain |
| 19 | Before the Ballet | Edgar Degas | 1890 | Impressionism | National Gallery of Art |
| 20 | Bouquet of Sunflowers | Claude Monet | 1881 | Impressionism | The Met |
| 21 | Breezing Up (A Fair Wind) | Winslow Homer | 1876 | Realism | National Gallery of Art |
| 22 | Bridge over a Pond of Water Lilies | Claude Monet | 1899 | Impressionism | The Met |
| 23 | Cafe Terrace at Night | Vincent van Gogh | 1888 | Post-Impressionism | Kroller-Muller Museum |
| 24 | Card Players | Paul Cezanne | 1895 | Post-Impressionism | Musee d'Orsay |
| 25 | Christ in the Storm (Copy) | Rembrandt van Rijn | 1633 | Dutch Golden Age | Stewart Gardner |
| 26 | Dance at Bougival | Pierre-Auguste Renoir | 1883 | Impressionism | MFA Boston |
| 27 | Dancers Practicing at the Barre | Edgar Degas | 1877 | Impressionism | The Met |
| 28 | David with the Head of Goliath | Caravaggio | 1610 | Baroque | Borghese Gallery, Rome |
| 29 | Day of the God (Mahana No Atua) | Paul Gauguin | 1894 | Post-Impressionism | Art Institute of Chicago |
| 30 | Declaration of Independence | John Trumbull | 1819 | Neoclassicism | Yale Art Gallery |
| 31 | Descent from the Cross | Rembrandt van Rijn | 1634 | Dutch Golden Age | Alte Pinakothek / Rijks |
| 32 | Equestrian Portrait of Charles I | Anthony van Dyck | 1635 | Baroque | National Gallery (London) |
| 33 | Et in Arcadia Ego | Nicolas Poussin | 1638 | Baroque | Louvre Museum, Paris |
| 34 | Field with Irises near Arles | Vincent van Gogh | 1888 | Post-Impressionism | Van Gogh Museum |
| 35 | First Steps (After Millet) | Vincent van Gogh | 1890 | Post-Impressionism | The Met |
| 36 | Flaming June | Frederic Leighton | 1895 | Realism | Museo de Arte de Ponce, Puerto Rico |
| 37 | Flower Beds at Vetheuil | Claude Monet | 1881 | Impressionism | Museum of Fine Arts, Boston |
| 38 | Flowering Plum Orchard | Vincent van Gogh | 1887 | Post-Impressionism | Van Gogh Museum |
| 39 | Flowers in a Terracotta Vase | Jan van Huysum | 1736 | Baroque | The National Gallery, London |
| 40 | Fur Traders on the Missouri | George Caleb Bingham | 1845 | Realism | The Met |
| 41 | Gauguin's Chair | Vincent van Gogh | 1888 | Post-Impressionism | Van Gogh Museum |
| 42 | Ginevra de' Benci | Leonardo da Vinci | 1474 | Renaissance | National Gallery of Art |
| 43 | Girl Reading a Letter at an Open Window | Johannes Vermeer | 1657 | Dutch Golden Age | Gemaldegalerie Alte Meister, Dresden |
| 44 | Girl with a Hoop | Pierre-Auguste Renoir | 1885 | Impressionism | National Gallery of Art |
| 45 | Girl with a Pearl Earring | Johannes Vermeer | 1665 | Dutch Golden Age | Mauritshuis |
| 46 | Girl with a Watering Can | Pierre-Auguste Renoir | 1876 | Impressionism | National Gallery of Art |
| 47 | Glass of Lemonade | Gerard ter Borch | 1664 | Dutch Golden Age | State Hermitage Museum |
| 48 | Hunters in the Snow | Pieter Bruegel the Elder | 1565 | Renaissance | Kunsthistorisches Museum, Vienna |
| 49 | Immaculate Conception of the Venerables | Bartolome Esteban Murillo | 1665 | Baroque | Museo del Prado, Madrid |
| 50 | Impression, Sunrise | Claude Monet | 1872 | Impressionism | Musee Marmottan Monet |
| 51 | In the Meadow | Pierre-Auguste Renoir | 1888 | Impressionism | The Met |
| 52 | Irises | Vincent van Gogh | 1890 | Post-Impressionism | The Met |
| 53 | Isaac and Rebecca, Known as 'The Jewish Bride' | Rembrandt van Rijn | 1665 | Dutch Golden Age | Rijksmuseum |
| 54 | Judith Beheading Holofernes (Caravaggio) | Caravaggio | 1599 | Baroque | Galleria Nazionale d'Arte Antica, Rome |
| 55 | Judith Slaying Holofernes | Artemisia Gentileschi | 1620 | Baroque | Uffizi Gallery |
| 56 | Kindred Spirits | Asher B. Durand | 1849 | Romanticism | Crystal Bridges |
| 57 | La Mousme | Vincent van Gogh | 1888 | Post-Impressionism | National Gallery of Art |
| 58 | Lady Agnew of Lochnaw | John Singer Sargent | 1892 | Realism | Scottish National Gallery |
| 59 | Lady with an Ermine | Leonardo da Vinci | 1490 | Renaissance | Czartoryski Museum, Krakow |
| 60 | Lake Albano and Castel Gandolfo | Jean-Baptiste-Camille Corot | 1826 | Romanticism | The Met |
| 61 | Lamentation of Christ | Giotto | 1305 | Renaissance | Scrovegni Chapel, Padua |
| 62 | Lamentation over the Dead Christ | Andrea Mantegna | 1480 | Renaissance | Pinacoteca di Brera, Milan |
| 63 | Landscape from Saint-Remy | Vincent van Gogh | 1889 | Post-Impressionism | Ny Carlsberg Glyptotek, Copenhagen |
| 64 | Las Meninas | Diego Velazquez | 1656 | Baroque | Museo del Prado |
| 65 | Liberty Leading the People | Eugene Delacroix | 1830 | Romanticism | Louvre Museum, Paris |
| 66 | Luncheon of the Boating Party | Pierre-Auguste Renoir | 1881 | Impressionism | Phillips Collection |
| 67 | Luncheon on the Grass | Edouard Manet | 1863 | Realism | Musee d'Orsay, Paris |
| 68 | Mada Primavesi | Gustav Klimt | 1912 | Symbolism | The Met |
| 69 | Madame Georges Charpentier | Pierre-Auguste Renoir | 1878 | Impressionism | The Met |
| 70 | Madame X | John Singer Sargent | 1884 | Realism | The Met |
| 71 | Madonna of the Goldfinch (Copy) | Raphael | 1506 | Renaissance | Uffizi Gallery |
| 72 | Madonna with the Long Neck | Parmigianino | 1535 | Mannerism | Uffizi Gallery, Florence |
| 73 | Max Schmitt in a Single Scull | Thomas Eakins | 1871 | Realism | The Met |
| 74 | Mona Lisa | Leonardo da Vinci | 1503 | Renaissance | Musee du Louvre |
| 75 | Mont Sainte-Victoire | Paul Cezanne | 1904 | Post-Impressionism | Princeton University Art Museum |
| 76 | Mount Corcoran | Albert Bierstadt | 1877 | Romanticism | National Gallery of Art |
| 77 | Napoleon Crossing the Alps | Jacques-Louis David | 1802 | Neoclassicism | Palace of Versailles |
| 78 | Niagara Falls | Frederic Edwin Church | 1857 | Romanticism | National Gallery of Art |
| 79 | Oath of the Horatii | Jacques-Louis David | 1784 | Neoclassicism | Louvre Museum, Paris |
| 80 | Olympia | Edouard Manet | 1863 | Realism | Musee d'Orsay, Paris |
| 81 | Ophelia | John Everett Millais | 1851 | Pre-Raphaelite | Tate Britain |
| 82 | Orpheus | Odilon Redon | 1903 | Symbolism | Cleveland Museum of Art |
| 83 | Pentecost | Giotto | 1305 | Renaissance | National Gallery (London) |
| 84 | Piazza San Marco, Venice | Pierre-Auguste Renoir | 1881 | Impressionism | Minneapolis Institute of Art |
| 85 | Poplars (Three Pink Autumn Trees) | Claude Monet | 1891 | Impressionism | The Met |
| 86 | Portrait of a Girl in Blue | Johannes C. Verspronck | 1641 | Dutch Golden Age | Rijksmuseum |
| 87 | Portrait of Dr. Gachet | Vincent van Gogh | 1890 | Post-Impressionism | Musee d'Orsay |
| 88 | Portrait of Henry VIII | Hans Holbein the Younger | 1537 | Renaissance | Walker Art Gallery, Liverpool |
| 89 | Portrait of Juan de Pareja | Diego Velazquez | 1650 | Baroque | The Met |
| 90 | Portrait of Madame Brunet | Edouard Manet | 1861 | Realism | The J. Paul Getty Museum |
| 91 | Portrait of the Infante Luis de Borbon | Francisco de Goya | 1783 | Rococo | Cleveland Museum of Art |
| 92 | Primavera | Sandro Botticelli | 1480 | Renaissance | Uffizi Gallery |
| 93 | Rain, Steam and Speed | Joseph Mallord William Turner | 1844 | Romanticism | National Gallery, London |
| 94 | Reading (La Lecture) | Berthe Morisot | 1873 | Impressionism | Cleveland Museum of Art |
| 95 | Reclining Young Woman in Spanish Costume | Edouard Manet | 1862 | Realism | Yale Art Gallery |
| 96 | Roses | Vincent van Gogh | 1890 | Post-Impressionism | The Met |
| 97 | Rounded Flower Bed (Corbeille de fleurs) | Claude Monet | 1876 | Impressionism | Detroit Institute of Arts |
| 98 | Saint Francis in Ecstasy | Giovanni Bellini | 1480 | Renaissance | Frick Collection |
| 99 | Saint Francis in Meditation | Francisco de Zurbaran | 1635 | Baroque | National Gallery, London |
| 100 | Saint George and the Dragon | Raphael | 1506 | Renaissance | National Gallery of Art |
| 101 | Saint Jerome as Scholar | El Greco | 1610 | Mannerism | The Met |
| 102 | Saturn Devouring His Son | Francisco de Goya | 1823 | Romanticism | Museo del Prado |
| 103 | Self-Portrait (1500) | Albrecht Durer | 1500 | Renaissance | Alte Pinakothek, Munich |
| 104 | Self-Portrait (1659) | Rembrandt van Rijn | 1659 | Dutch Golden Age | National Gallery of Art |
| 105 | Self-Portrait (1887) | Vincent van Gogh | 1887 | Post-Impressionism | Art Institute of Chicago |
| 106 | Self-Portrait (1889) | Vincent van Gogh | 1889 | Post-Impressionism | National Gallery of Art |
| 107 | Self-Portrait (Degas) | Edgar Degas | 1855 | Impressionism | The Met |
| 108 | Self-Portrait as a Painter | Vincent van Gogh | 1888 | Post-Impressionism | Van Gogh Museum |
| 109 | Self-Portrait with a Straw Hat | Vincent van Gogh | 1887 | Post-Impressionism | The Met |
| 110 | Self-Portrait with Bandaged Ear | Vincent van Gogh | 1889 | Post-Impressionism | The Courtauld Gallery, London |
| 111 | Self-Portrait with Grey Felt Hat | Vincent van Gogh | 1887 | Post-Impressionism | Van Gogh Museum |
| 112 | Self-Portrait with Two Circles | Rembrandt van Rijn | 1665 | Dutch Golden Age | Kenwood House, London |
| 113 | Serenade | Judith Leyster | 1629 | Dutch Golden Age | Rijksmuseum |
| 114 | Shoes | Vincent van Gogh | 1886 | Post-Impressionism | Van Gogh Museum |
| 115 | Sistine Madonna | Raphael | 1512 | Renaissance | Gemaldegalerie Alte Meister, Dresden |
| 116 | Small Cowper Madonna | Raphael | 1505 | Renaissance | National Gallery of Art |
| 117 | Snap the Whip | Winslow Homer | 1872 | Realism | The Met |
| 118 | Snow at Argenteuil | Claude Monet | 1875 | Impressionism | Museum of Fine Arts, Boston |
| 119 | Stag at Sharkey's | George Bellows | 1909 | Realism | Cleveland Museum of Art |
| 120 | Starry Night Over the Rhone | Vincent van Gogh | 1888 | Post-Impressionism | Musee d'Orsay |
| 121 | Still Life with a Glass and Oysters | Jan Davidsz de Heem | 1640 | Dutch Golden Age | The Met |
| 122 | Still Life with a Silver Jug and a Porcelain Bowl | Willem Kalf | 1660 | Dutch Golden Age | Rijksmuseum |
| 123 | Still Life with Apples and a Pot of Primroses | Paul Cezanne | 1890 | Post-Impressionism | The Met |
| 124 | Still Life with Apples and Pears | Paul Cezanne | 1891 | Post-Impressionism | The Met |
| 125 | Still Life with Flowers on a Marble Tabletop | Rachel Ruysch | 1716 | Dutch Golden Age | Rijksmuseum |
| 126 | Still Life with Gilt Cup | Willem Claesz Heda | 1635 | Dutch Golden Age | Rijksmuseum |
| 127 | Still Life with Ham | Gerret Willemsz Heda | 1650 | Dutch Golden Age | National Gallery of Art |
| 128 | Still Life with Musical Instruments, Books and Sculpture | Evaristo Baschenis | 1650 | Baroque | Museum Boijmans Van Beuningen |
| 129 | Still Life with Oysters, a Silver Tazza, and Glassware | Willem Claesz Heda | 1635 | Dutch Golden Age | The Met |
| 130 | Still Life with Quinces | Vincent van Gogh | 1887 | Post-Impressionism | Galerie Neue Meister, Dresden |
| 131 | Sunflowers (1887) | Vincent van Gogh | 1887 | Post-Impressionism | The Met |
| 132 | Sunflowers (Blue background) | Vincent van Gogh | 1888 | Post-Impressionism | Neue Pinakothek, Munich |
| 133 | Supper at Emmaus | Caravaggio | 1601 | Baroque | National Gallery, London |
| 134 | Susanna and the Elders | Jacopo Tintoretto | 1555 | Renaissance | Museo del Prado |
| 135 | The Absinthe Drinker | Edgar Degas | 1876 | Impressionism | Musee d'Orsay, Paris |
| 136 | The Ambassadors | Hans Holbein the Younger | 1533 | Renaissance | National Gallery, London |
| 137 | The Anatomy Lesson of Dr. Nicolaes Tulp | Rembrandt van Rijn | 1632 | Baroque | Mauritshuis |
| 138 | The Angelus | Jean-Francois Millet | 1859 | Realism | Musee d'Orsay |
| 139 | The Annunciation (Copy) | Fra Angelico | 1425 | Renaissance | Prado / San Marco |
| 140 | The Argenteuil Bridge | Claude Monet | 1874 | Impressionism | National Gallery of Art |
| 141 | The Arnolfini Portrait | Jan van Eyck | 1434 | Renaissance | The National Gallery, London |
| 142 | The Artist's Garden at Vetheuil | Claude Monet | 1880 | Impressionism | National Gallery of Art |
| 143 | The Banks of the Oise | Alfred Sisley | 1877 | Impressionism | National Gallery of Art |
| 144 | The Banquet of the Guard | Bartholomeus v. d. Helst | 1648 | Dutch Golden Age | Rijksmuseum |
| 145 | The Baptism of Christ | Piero della Francesca | 1450 | Renaissance | National Gallery (London) |
| 146 | The Basket of Apples | Paul Cezanne | 1893 | Post-Impressionism | Art Institute of Chicago |
| 147 | The Beach at Sainte-Adresse | Claude Monet | 1867 | Impressionism | Art Institute of Chicago |
| 148 | The Bedroom | Vincent van Gogh | 1889 | Post-Impressionism | Art Institute of Chicago |
| 149 | The Birth of Venus | Sandro Botticelli | 1485 | Renaissance | Uffizi Gallery |
| 150 | The Birth of Venus (Bouguereau) | William-Adolphe Bouguereau | 1879 | Realism | Musee d'Orsay, Paris |
| 151 | The Blue Boy | Thomas Gainsborough | 1770 | Rococo | Huntington Library, San Marino |
| 152 | The Brook | Paul Cezanne | 1900 | Post-Impressionism | Cleveland Museum of Art |
| 153 | The Burial of the Count of Orgaz | El Greco | 1586 | Mannerism | Church of Santo Tome, Toledo |
| 154 | The Burning of the Houses of Lords and Commons | Joseph Mallord William Turner | 1835 | Romanticism | Cleveland Museum of Art |
| 155 | The Calling of Saint Matthew | Caravaggio | 1600 | Baroque | San Luigi dei Francesi, Rome |
| 156 | The Child's Bath | Mary Cassatt | 1893 | Impressionism | Art Institute of Chicago |
| 157 | The Clothed Maja | Francisco de Goya | 1808 | Romanticism | Museo del Prado, Madrid |
| 158 | The Creation of Adam | Michelangelo | 1512 | Renaissance | Sistine Chapel (Vatican Museums) |
| 159 | The Crucifixion of Saint Andrew | Caravaggio | 1607 | Baroque | Cleveland Museum of Art |
| 160 | The Dance Class | Edgar Degas | 1874 | Impressionism | The Met |
| 161 | The Death of Marat | Jacques-Louis David | 1793 | Neoclassicism | Royal Museums of Fine Arts of Belgium |
| 162 | The Death of Socrates | Jacques-Louis David | 1787 | Neoclassicism | The Met |
| 163 | The Descent from the Cross (Rubens) | Peter Paul Rubens | 1602 | Baroque | Siegerlandmuseum, Siegen |
| 164 | The Descent from the Cross (van der Weyden) | Rogier van der Weyden | 1435 | Renaissance | Museo del Prado, Madrid |
| 165 | The Dream | Henri Rousseau | 1910 | Post-Impressionism | Museum of Modern Art, New York |
| 166 | The Fifer | Edouard Manet | 1866 | Realism | Musee d'Orsay |
| 167 | The Fighting Temeraire | Joseph Mallord William Turner | 1839 | Romanticism | National Gallery, London |
| 168 | The Garden of Earthly Delights | Hieronymus Bosch | 1503 | Renaissance | Museo del Prado |
| 169 | The Garden of Love | Peter Paul Rubens | 1633 | Baroque | Museo del Prado |
| 170 | The Garden of the Asylum at Saint-Remy | Vincent van Gogh | 1889 | Post-Impressionism | Kroller-Muller Museum |
| 171 | The Gleaners | Jean-Francois Millet | 1857 | Realism | Musee d'Orsay |
| 172 | The Goldfinch | Carel Fabritius | 1654 | Dutch Golden Age | Mauritshuis |
| 173 | The Grand Canal, Venice | Edouard Manet | 1874 | Impressionism | Shelburne Museum |
| 174 | The Grand Canal, Venice (Canaleto) | Canaletto | 1730 | Baroque | The Met |
| 175 | The Grand Odalisque | Jean-Auguste-Dominique Ingres | 1814 | Neoclassicism | Louvre Museum, Paris |
| 176 | The Gross Clinic | Thomas Eakins | 1875 | Realism | Philadelphia Museum |
| 177 | The Harvest | Vincent van Gogh | 1888 | Post-Impressionism | Van Gogh Museum |
| 178 | The Hay Wain | John Constable | 1821 | Romanticism | National Gallery (London) |
| 179 | The Horse Fair | Rosa Bonheur | 1853 | Realism | The Met |
| 180 | The Houses of Parliament, Sunset | Claude Monet | 1903 | Impressionism | National Gallery of Art |
| 181 | The Icebergs | Frederic Edwin Church | 1861 | Romanticism | Dallas Museum of Art |
| 182 | The Japanese Footbridge | Claude Monet | 1899 | Impressionism | National Gallery of Art |
| 183 | The Kiss | Francesco Hayez | 1859 | Symbolism | Pinacoteca di Brera, Milan |
| 184 | The Kiss (Gustav Klimt) | Gustav Klimt | 1907 | Symbolism | Belvedere Museum |
| 185 | The Lacemaker | Johannes Vermeer | 1669 | Dutch Golden Age | Louvre Museum, Paris |
| 186 | The Lady of Shalott | John William Waterhouse | 1888 | Pre-Raphaelite | Tate Britain |
| 187 | The Langlois Bridge at Arles | Vincent van Gogh | 1888 | Post-Impressionism | Van Gogh Museum |
| 188 | The Large Bathers | Paul Cezanne | 1906 | Post-Impressionism | Philadelphia Museum |
| 189 | The Last Supper (Copy) | Giampietrino and Giovanni Antonio Boltraffio | 1520 | Renaissance | Royal Academy of Arts, London |
| 190 | The Little Street | Johannes Vermeer | 1658 | Dutch Golden Age | Rijksmuseum |
| 191 | The Merry Family | Jan Steen | 1668 | Dutch Golden Age | Rijksmuseum |
| 192 | The Milkmaid | Johannes Vermeer | 1658 | Dutch Golden Age | Rijksmuseum |
| 193 | The Monet Family in Their Garden | Edouard Manet | 1874 | Impressionism | The Met |
| 194 | The Night Cafe | Vincent van Gogh | 1888 | Post-Impressionism | Yale Art Gallery |
| 195 | The Night Watch | Rembrandt van Rijn | 1642 | Dutch Golden Age | Rijksmuseum |
| 196 | The Nude Maja | Francisco de Goya | 1800 | Romanticism | Museo del Prado, Madrid |
| 197 | The Oxbow | Thomas Cole | 1836 | Romanticism | The Met |
| 198 | The Penitent Magdalene | Georges de La Tour | 1640 | Baroque | The Metropolitan Museum of Art, New York |
| 199 | The Potato Eaters | Vincent van Gogh | 1885 | Post-Impressionism | Van Gogh Museum |
| 200 | The Raft of the Medusa | Theodore Gericault | 1819 | Romanticism | Louvre Museum, Paris |
| 201 | The Return of the Prodigal Son | Rembrandt van Rijn | 1668 | Dutch Golden Age | Hermitage Museum, Saint Petersburg |
| 202 | The Rokeby Venus | Diego Velazquez | 1651 | Baroque | National Gallery, London |
| 203 | The School of Athens | Raphael | 1511 | Renaissance | Apostolic Palace (Vatican Museums) |
| 204 | The Seine at La Grande Jatte | Georges Seurat | 1888 | Post-Impressionism | RMFAB, Bruselas |
| 205 | The Silver Tureen | Jean-Baptiste Chardin | 1728 | Rococo | The Met |
| 206 | The Skiff (La Yole) | Pierre-Auguste Renoir | 1875 | Impressionism | National Gallery (London) |
| 207 | The Sleeping Gypsy | Henri Rousseau | 1897 | Post-Impressionism | Museum of Modern Art, New York |
| 208 | The Sower | Vincent van Gogh | 1888 | Post-Impressionism | Van Gogh Museum |
| 209 | The Starry Night | Vincent van Gogh | 1889 | Post-Impressionism | Museum of Modern Art (MoMA) |
| 210 | The Swing | Jean-Honore Fragonard | 1767 | Rococo | Wallace Collection |
| 211 | The Third of May 1808 | Francisco de Goya | 1814 | Romanticism | Museo del Prado |
| 212 | The Third-Class Carriage | Honore Daumier | 1864 | Realism | The Met |
| 213 | The Threatened Swan | Jan Asselijn | 1650 | Dutch Golden Age | Rijksmuseum |
| 214 | The Three Graces | Peter Paul Rubens | 1635 | Baroque | Museo del Prado, Madrid |
| 215 | The Transfiguration | Raphael | 1520 | Renaissance | Vatican Museums |
| 216 | The Triumph of Death | Pieter Bruegel the Elder | 1562 | Renaissance | Museo del Prado |
| 217 | The Valley of the Seine, from the Hills of Giverny | Theodore Robinson | 1892 | Impressionism | National Gallery of Art |
| 218 | The Veteran in a New Field | Winslow Homer | 1865 | Realism | The Met |
| 219 | The Virgin of the Rocks | Leonardo da Vinci | 1508 | Renaissance | National Gallery, London |
| 220 | The Wedding at Cana | Paolo Veronese | 1563 | Renaissance | Louvre Museum, Paris |
| 221 | The Windmill | Rembrandt van Rijn | 1645 | Dutch Golden Age | National Gallery of Art |
| 222 | The Windmill at Wijk bij Duurstede | Jacob van Ruisdael | 1670 | Dutch Golden Age | Rijksmuseum |
| 223 | The Woman with a Fan | Pierre-Auguste Renoir | 1880 | Impressionism | The Hermitage |
| 224 | The Yellow House | Vincent van Gogh | 1888 | Post-Impressionism | Van Gogh Museum |
| 225 | Tower of Babel | Pieter Bruegel the Elder | 1563 | Renaissance | Kunsthistorisches Museum |
| 226 | Two Sisters (On the Terrace) | Pierre-Auguste Renoir | 1881 | Impressionism | Art Institute of Chicago |
| 227 | Vase of Flowers (Pink Background) | Odilon Redon | 1906 | Symbolism | The Met |
| 228 | Venus of Urbino | Titian | 1538 | Renaissance | Uffizi Gallery, Florence |
| 229 | View of Delft | Johannes Vermeer | 1660 | Dutch Golden Age | Mauritshuis, The Hague |
| 230 | View of Toledo | El Greco | 1599 | Mannerism | The Met |
| 231 | View of Vetheuil | Claude Monet | 1880 | Impressionism | The Met |
| 232 | Village of Eragny | Camille Pissarro | 1885 | Impressionism | Birmingham Museum of Art |
| 233 | Virgin and Child with St. Anne | Leonardo da Vinci | 1503 | Renaissance | Louvre Museum, Paris |
| 234 | Vision After the Sermon | Paul Gauguin | 1888 | Post-Impressionism | Scottish National Gallery |
| 235 | Walk on the Seashore | Joaquin Sorolla | 1909 | Realism | Museo Sorolla, Madrid |
| 236 | Wanderer above the Sea of Fog | Caspar David Friedrich | 1818 | Romanticism | Kunsthalle Hamburg |
| 237 | Washington Crossing the Delaware | Emanuel Leutze | 1851 | Romanticism | The Met |
| 238 | Water Lilies (Agapanthus) | Claude Monet | 1920 | Impressionism | Cleveland Museum of Art |
| 239 | Watson and the Shark | John Singleton Copley | 1778 | Romanticism | National Gallery of Art |
| 240 | Wheatfield with Crows | Vincent van Gogh | 1890 | Post-Impressionism | Van Gogh Museum |
| 241 | Wheatfield with Cypresses | Vincent van Gogh | 1889 | Post-Impressionism | The Met |
| 242 | Where Do We Come From? What Are We? Where Are We Going? | Paul Gauguin | 1898 | Post-Impressionism | Museum of Fine Arts, Boston |
| 243 | Whistler's Mother | James McNeill Whistler | 1871 | Realism | Musee d'Orsay |
| 244 | Winter Landscape with Skaters | Hendrick Avercamp | 1608 | Dutch Golden Age | Rijksmuseum |
| 245 | Woman Holding a Balance | Johannes Vermeer | 1664 | Dutch Golden Age | National Gallery of Art |
| 246 | Woman with a Parasol | Claude Monet | 1875 | Impressionism | National Gallery of Art |
| 247 | Woman with a Pearl Necklace | Johannes Vermeer | 1664 | Dutch Golden Age | Gemaldegalerie |
| 248 | Women of Tahiti | Paul Gauguin | 1891 | Post-Impressionism | Musee d'Orsay |
| 249 | Young Hare | Albrecht Durer | 1502 | Renaissance | Albertina, Vienna |
| 250 | Young Woman with a Water Pitcher | Johannes Vermeer | 1662 | Dutch Golden Age | The Met |
| 251 | Young Woman with Peonies | Frederic Bazille | 1870 | Impressionism | National Gallery of Art |
