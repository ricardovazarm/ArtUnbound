# Documento de Diseño de Juego (GDD): Art Unbound
- Versión: 5.0
- Fecha: Junio 2026
- Plataforma: Meta Quest (Quest 2, Quest 3, Quest Pro)

---

## 1. Concepto de Alto Nivel

Art Unbound es un juego de puzzles para Meta Quest. Los jugadores reconstruyen obras maestras clásicas ensamblando piezas en un lienzo flotante frente a ellos, y al terminar pueden colgar el cuadro completado en su entorno, donde persiste entre sesiones.

En **Modo Mixto (MR)** — disponible en Quest 3 y Quest Pro — el cuadro se cuelga en una pared real del espacio físico del usuario mediante anclajes espaciales persistentes. En **Modo Virtual (VR)** — disponible en todos los dispositivos Quest — el cuadro se cuelga en las paredes de una galería de arte 3D inmersiva.

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
- **Flexibilidad ergonómica**: El armado ocurre siempre en un lienzo flotante a distancia fija (0.4 m del usuario), permitiendo jugar sentado o de pie en cualquier lugar. La integración con la pared es una recompensa post-juego.
- **Bienestar sin presión**: No hay penalizaciones, puntuaciones ni relojes que castiguen. El tamaño del rompecabezas es una elección de comodidad, no de dificultad competitiva.

---

## 4. Flujo del Juego

### 4.1. Menú Principal

El menú principal es un panel flotante en espacio MR/VR con tres zonas:

**Barra superior**
- Título de la app ("Art Unbound") a la izquierda.
- A la derecha: un **toggle de modo** (VR/MR) que alterna entre realidad mixta y galería virtual, y un acceso a **Configuración** (engrane) con los controles de volumen de música y de efectos.

**Cuerpo central — Catálogo de obras**
- Cuadrícula paginada de miniaturas, cada una con su título.
- Las obras completadas muestran una **medalla de completación única** (indica que la obra fue armada; no varía según el tamaño elegido).
- Seleccionar una obra abre su **detalle como ventana modal** (ver 4.2).

**Barra inferior — Navegación**
- **Home**: catálogo principal.
- **Search**: búsqueda de obras dentro del catálogo.
- **Collection**: inventario de exhibición del jugador (ver 8.5) — obras completadas y recompensas obtenidas, desde donde se cuelgan.

### 4.2. Detalle de Obra y Tamaños de Rompecabezas

Al seleccionar una obra del catálogo se abre su **detalle como ventana modal**, que muestra la pintura, su título, artista, museo de origen, movimiento artístico y año. El detalle se comporta distinto según si la obra está desbloqueada:

- **Obra desbloqueada:** muestra "Solve a puzzle" con **tres botones de tamaño** (A Coffee / A Break / A Movie) para iniciar el puzzle. Los botones muestran solo su etiqueta de tamaño (sin récord de tiempo).
- **Obra bloqueada** (no incluida en el tier gratuito, con el catálogo aún no comprado): en lugar de los botones de tamaño, el detalle muestra un **botón de compra** que dispara el desbloqueo único de todo el catálogo (ver sección 12). Este es el único punto de venta del juego: no existe una sección de tienda separada.

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

El tamaño es una decisión personal de comodidad. El cuadro final es idéntico en los tres tamaños — lo único que cambia es el número y tamaño de las piezas. Por ello, el tamaño elegido **no otorga marcos, medallas ni recompensas distintas**: completar una obra cuenta igual hacia los logros sin importar el tamaño (ver sección 9).

### 4.3. Marco del Cuadro

Todo cuadro completado se presenta con un **marco único y constante**, idéntico para todas las obras y todos los jugadores. El marco es parte de la presentación de la obra, no un elemento de recompensa: como el cuadro final es siempre el mismo, su marco también lo es.

### 4.4. Pantalla de Armado

Al iniciar el puzzle, el espacio se divide en tres zonas:

**Centro — Tablero del Puzzle**
- El lienzo flotante a 0.4 m del usuario donde se arman las piezas.
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

- **Con las manos (hand tracking, MR):** el usuario toma una pieza con pinch (pulgar-índice); la pieza sigue su mano y, al soltarla cerca del tablero, hace snap al **slot disponible más cercano** — no al punto exacto donde se suelta.
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
2. Se muestra el panel de post-juego: el cuadro enmarcado con la instrucción para colgarlo, el tiempo de resolución, y una **medalla de completación única** ("¡Felicidades, ganaste una medalla!"). La medalla no varía según el tamaño.
3. El jugador puede colgar el cuadro en una pared real (MR) o en la galería virtual (VR) — ver secciones 5 y 6.
4. Para colgar una obra ya completada más adelante, el jugador vuelve a entrar a verla y la cuelga desde ahí (o desde la sección Collection; ver 8.5).

---

## 5. Sistema de Colgado — Modo MR (Mixed Reality)

### 5.1. Flujo de Colocación

El colgado puede iniciarse al terminar una obra (post-juego), al volver a entrar a ver una obra completada, o desde la sección Collection (ver 8.5). El flujo admite dos modos de entrada:

- **Con las manos:** el usuario toma el cuadro, acerca la mano (con el cuadro) a una pared real y lo suelta; al quedar **a 20 cm o menos de la pared**, el cuadro se fija y se crea un anclaje espacial persistente.
- **Con los controles:** el usuario hace clic en el cuadro para **seleccionarlo** y luego clic en el punto de la pared donde quiere colgarlo.

Si se suelta o apunta lejos de una pared, el cuadro no se cuelga y sigue disponible para intentarlo de nuevo. El mismo flujo aplica a las **placas** (ver sección 8): son objetos colgables que el usuario coloca libremente donde desee.

### 5.2. Reposicionamiento y Retiro

- Los cuadros y placas ya colgados pueden tomarse de nuevo (con las manos o seleccionándolos con el control).
- Al recolocar cerca de una pared (≤ 20 cm) / apuntar a la pared: el objeto se reposiciona y el anclaje espacial se actualiza.
- Al soltar o apuntar lejos de una pared: el objeto se retira y desaparece.

### 5.3. Persistencia

- Los cuadros y placas colgados persisten entre sesiones mediante Meta Spatial Anchors (AR Foundation).
- Al iniciar la app, todos los objetos colgados anteriormente se reconstituyen en sus posiciones reales.
- La detección de paredes usa ARPlaneManager (planos verticales AR) con fallback por raycast.

### 5.4. Curaduría del Espacio

La pared real del usuario funciona como una **selección curada y rotativa** de sus obras favoritas, no como un archivo completo de todo lo armado. El usuario cuelga, descuelga e intercambia libremente las obras que desea exhibir. Las placas temáticas (sección 8.3) permiten representar logros de colección completos — por ejemplo, haber armado muchas obras de un mismo pintor — mediante un solo objeto, sin necesidad de colgar físicamente todas las obras correspondientes.

---

## 6. Modo VR (Virtual Reality)

### 6.1. Galería Virtual

En modo VR el usuario se encuentra dentro de una galería de arte 3D inmersiva, jugada con los controles (apuntar y gatillo). El puzzle se arma en el mismo lienzo flotante que en MR. Las paredes de la galería son superficies virtuales donde puede colgar sus obras completadas y placas. A diferencia de MR, la galería virtual no tiene límite de espacio físico: el usuario puede exhibir su colección completa en múltiples paredes.

La galería arranca como un **entorno base deliberadamente sobrio**, que el jugador va poblando y mejorando con los **assets que desbloquea** mediante logros (ver sección 8). El estado inicial es austero a propósito — es el "antes" de una progresión—, pero debe ser digno y acogedor para el jugador nuevo que aún no ha desbloqueado nada.

### 6.2. Mecánica de Colocación de Piezas en VR

La colocación con controles es la descrita en 4.5: apuntar + gatillo para seleccionar una pieza, apuntar + gatillo en la celda de destino para colocarla ahí. La validación (correcto/incorrecto) funciona igual que en MR.

### 6.3. Flujo de Colgado en VR

El colgado puede iniciarse desde el post-juego, al re-entrar a una obra completada, o desde Collection. El flujo con controles:

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
| Persistencia | Meta Spatial Anchors | JSON local (GalleryPersistenceService) |
| Espacio de exhibición | Limitado por la pared física real | Ilimitado (múltiples paredes virtuales) |
| Disponibilidad | Quest 3 y Quest Pro únicamente | Todos los dispositivos Quest |

### 6.6. Locomoción en VR

- **Teleportación**: apuntar al suelo con el controlador y presionar el gatillo para desplazarse.
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
- **Cada recompensa es visible en el espacio.** La pared (MR) o la galería (VR) es la vitrina del jugador y lo que comparte con otros. Las recompensas son objetos que se cuelgan o se aplican a las obras, no entradas en un menú oculto.
- **La jerarquía visual refleja logro acumulado, nunca decisiones puntuales.** Los rangos bronce/plata/oro/platino existen solo en la placa de estatus, ganados por acumulación; no se aplican a obras individuales.

### 8.2. Objetos Cosméticos Únicos

- **La Lámpara.** Un único objeto de iluminación tipo museo que, una vez desbloqueado, aparece sobre cada cuadro colgado. No tiene variantes.
- **La Cédula.** Una única placa-cédula estilo museo (título y artista) que, una vez desbloqueada, aparece debajo de cada cuadro colgado. No tiene variantes.

Ambos son atributos visuales de las obras ya colgadas y elevan el aspecto de galería de toda la pared de golpe.

### 8.3. Placas Temáticas

Objetos colgables que el jugador obtiene al alcanzar el umbral de obras completadas de un pintor o un movimiento artístico. Se cuelgan libremente donde el usuario desee.

- Son assets de diseño fijo y dimensión constante, con identidad visual propia por tema (por ejemplo, motivo de girasoles para Van Gogh, azul de Delft para Vermeer).
- Una placa temática representa un logro de colección completo mediante un solo objeto, resolviendo el límite de espacio físico de la pared.

**Condición de desbloqueo:** completar **6 obras** del pintor o movimiento correspondiente, o **todas** sus obras si el catálogo contiene menos de 6 de ese pintor/movimiento. El tamaño en que se completaron las obras es indiferente. La galería temática del mismo tema (Fase 2, sección 15.5) es un tier superior que usa un umbral mayor de 12 obras.

**Set inicial de placas (ver sección 8.6).**

### 8.4. Placa de Estatus

Objeto colgable único que muestra el avance global del jugador en el mundo, en lugar de en una pantalla de menú.

- Se desbloquea al completar el primer rompecabezas y se cuelga donde el usuario desee.
- Es de **tamaño fijo**, dimensionado para alojar todos los logros eventualmente.
- Tiene **tiers por nivel** — **Bronce → Plata → Oro → Platino** — que suben según el número total de rompecabezas armados. Al alcanzar un tier, el jugador cuelga la nueva placa y retira la anterior; todas comparten dimensión, por lo que el reemplazo conserva el anclaje en la pared.

**Composición:**
- **Cabecera fija (determinista):** rango/título, agregados de progreso (por ejemplo, obras armadas y placas obtenidas) y el logro cumbre. Es la parte comparable de un vistazo entre jugadores.
- **Cuerpo dinámico (cronológico):** los medallones de logro se pueblan en el orden en que se obtienen, contando la historia personal del jugador.
- **Estados visuales:** los logros obtenidos aparecen a color; los pendientes, como slots vacíos en silueta (aspiracionales, sin texto de requisitos). La información de requisitos vive en la sección Collection del menú, no en la placa.

### 8.5. Collection (Menú)

Sección del menú que unifica en un solo lugar todo lo que el jugador puede exhibir y todo lo que puede llegar a obtener:

- **Obras completadas**, listas para colgar. Al seleccionar una, el jugador entra directo al modo de colocación (sin panel de detalle intermedio), reutilizando el mecanismo de colgado de las secciones 5 (MR) y 6 (VR). Esto unifica el colgado: cuadros, placas, lámpara y cédula se exhiben desde un único lugar, en lugar de obligar a buscar cada obra en el catálogo.
- **Placas temáticas y placa de estatus** obtenidas, también colgables desde aquí.
- **Objetos aún no obtenidos** (placas, lámpara, cédula): se muestran como bloqueados, y al seleccionarlos una ventana indica el logro o condición necesaria para desbloquearlos. Es el lugar donde el jugador descubre qué puede obtener y cómo.

La lámpara y la cédula, una vez desbloqueadas, no se cuelgan individualmente: son atributos que se aplican automáticamente a las obras colgadas (ver 8.2). En Collection aparecen como objetos desbloqueables con su condición.

### 8.6. Set Inicial de Placas Temáticas

**Placas por pintor (5):** umbral de placa = 6 obras.

| Pintor | Obras en catálogo | Umbral placa | Umbral galería (Fase 2) |
|--------|-------------------|--------------|--------------------------|
| Vincent van Gogh | 34 | 6 | 12 |
| Claude Monet | 15 | 6 | 12 |
| Pierre-Auguste Renoir | 11 | 6 | todas (11) |
| Johannes Vermeer | 11 | 6 | todas (11) |
| Rembrandt van Rijn | 10 | 6 | todas (10) |

**Placas por movimiento (7):** umbral de placa = 6 obras; todos superan 12 para la galería.

| Movimiento | Obras | Umbral placa | Umbral galería (Fase 2) |
|------------|-------|--------------|--------------------------|
| Post-Impresionismo | 51 | 6 | 12 |
| Impresionismo | 40 | 6 | 12 |
| Renacimiento (incluye Manierismo) | 41 | 6 | 12 |
| Siglo de Oro Holandés | 36 | 6 | 12 |
| Realismo | 23 | 6 | 12 |
| Barroco | 21 | 6 | 12 |
| Romanticismo (incluye Rococó y Neoclasicismo) | 33 | 6 | 12 |

La columna de galería se incluye como referencia; la galería temática es una funcionalidad de Fase 2 (sección 15.5). Para pintores con menos de 12 obras, la galería se obtiene al completarlas todas.

Las obras de un pintor cuentan también para su movimiento; el progreso hacia ambas placas avanza en paralelo. El catálogo de placas se amplía con actualizaciones (más pintores y movimientos).

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
- **Barra superior**: título, toggle de modo (VR/MR) y acceso a Configuración (engrane, con volúmenes de música y efectos).
- **Cuerpo central**: catálogo en cuadrícula paginada de miniaturas; las completadas muestran medalla única; seleccionar abre el detalle modal.
- **Barra inferior**: Home, Search, Collection.

### 11.2. Pantalla de Puzzle

Tres zonas (ver sección 4.4):
- **Centro**: tablero de armado.
- **Izquierda**: información de la obra, imagen de referencia, barra de progreso, contadores (total/correctas/incorrectas/temporizador), pieza musical en reproducción, botón Exit y botón Check.
- **Derecha**: bandeja de piezas paginada con botones de navegación.

### 11.3. Panel de Hitos

Mensajes contextuales al completar filas, columnas, bordes y el marco exterior completo.

### 11.4. Panel Post-Juego

Muestra el tiempo de resolución, actualiza el progreso de logros y presenta la opción de colgar el cuadro.

### 11.5. Temática Visual de Botones

Sistema de temas unificado: color normal `#896C4A`, hover/seleccionado `#d4c089`.

---

## 12. Modelo de Monetización

### 12.1. Estructura Free-to-Play

Art Unbound se distribuye como una **aplicación gratuita** en la Meta Quest Store. La app gratuita incluye un conjunto inicial de **~12 obras icónicas** completamente jugables, junto con todas las funcionalidades del juego (modos MR y VR, sistema de colgado, sistema de recompensas y progresión).

### 12.2. Desbloqueo del Catálogo Completo

El catálogo completo se desbloquea con una **compra única de $9.99 USD**, que otorga acceso permanente a las **240+ obras restantes**. Es una compra única, no una suscripción.

- **No existe una sección de tienda.** El desbloqueo se ofrece de forma contextual: al abrir el detalle de cualquier obra bloqueada, en lugar de los botones de tamaño aparece un **botón de compra**. Tocarlo dispara el desbloqueo único de **todo** el catálogo (no es compra por obra).
- No existen packs temáticos ni bundles.
- Tras la compra, todas las obras quedan disponibles de inmediato, sin progresión intermedia que limite su acceso.

### 12.3. Selección del Tier Gratuito

Las ~12 obras gratuitas son las más reconocibles del catálogo (por ejemplo, Mona Lisa, The Starry Night, Girl with a Pearl Earring, entre otras), de modo que la experiencia gratuita sea plenamente satisfactoria por sí misma.

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
- **Condición de desbloqueo:** la galería temática se otorga al completar **12 obras** del tema (o **todas**, si el tema tiene menos de 12), un tier por encima de la placa, que se gana con 6. Así la placa marca un primer dominio y la galería marca un compromiso serio con el tema. Es un umbral exigente pero alcanzable — deliberadamente no requiere completar pools enormes como las 34 de Van Gogh.
- **Liberación gradual como stream de contenido.** No se construyen todas de inicio; se liberan poco a poco como actualizaciones sustanciales (empezando por los temas más populares: Impresionismo, Van Gogh, Renacimiento). Liberar *galerías* progresivamente es legítimo porque son recompensas cosméticas/contenido — a diferencia de liberar *obras*, que gatearía el catálogo que el jugador compró.
- **La galería base permanece como entorno por defecto.** El jugador puede cambiar su galería VR activa entre las que haya desbloqueado y exhibir en cada una la colección correspondiente.
- **Incentivo de completitud y de invitación.** La galería es un motor fuerte para terminar las obras de un tema, y en conjunto con el co-op y las visitas (15.1) adquiere propósito social: se desbloquea una galería para tener un espacio que mostrar e invitar a otros.

### 15.6. Retención en MR: Instalaciones Temáticas y Curaduría del Hogar

La retención de MR no copia las features de VR (no se puede tematizar un cuarto real ni traer un avatar remoto cómodamente). Se construye sobre lo único que MR tiene: que es el hogar real del jugador. Tendrá un techo más ligero que la retención VR, pero un loop propio que VR no puede replicar.

- **Instalaciones temáticas (análogo MR de las galerías).** Al completar una colección (mismo umbral de 12 que la galería VR), el jugador desbloquea un *montaje* que aplica a una sección de su pared real: spotlights coordinados en un tono temático, un cartel/cédula del tema (p. ej. "Monet", con fechas) y la placa conmemorativa, dispuestos como una mini-exposición anclada al muro. Convierte una sección de la casa real en una "pared Monet" o un "rincón impresionista". Es el mismo loop de "completar colección → desbloquear montaje temático" que la galería VR, pero viviendo en el hogar.
- **Curaduría y expansión del hogar como ritual de regreso.** El límite de espacio de la pared real es un motor, no un defecto: obliga a curar (rotar obras, rediseñar la pared por temporada o para visitas). La progresión MR-nativa es expandir el museo a más muros y cuartos de la casa con el tiempo — "haz crecer tu museo por todo tu hogar", algo que VR no puede ofrecer.
- **Dimensión social MR-nativa.** En MR lo social no es co-op remoto, sino: (a) **compartir** — la captura de arte en una sala real es más impactante que en una galería virtual; y (b) **en persona** — alguien físicamente presente se pone el visor y recorre el museo en el espacio real del anfitrión.
- **Presencia ambiental (re-enganche pasivo).** En MR el museo ya está en el cuarto cada vez que el jugador se pone el visor, sin entrar a ninguna galería; la colección lo recibe. Es re-enganche pasivo exclusivo de MR.

---

## 16. Arquitectura Técnica (Resumen)

- **Motor**: Unity 6 LTS (6000.3.1f1)
- **SDK**: Meta XR All-in-One SDK v85.0.0
- **Tracking**: XR Hands (seguimiento de manos nativo) en MR; controladores en VR
- **Anclas espaciales (MR)**: AR Foundation + OpenXR (persistencia cross-session)
- **Persistencia VR**: JSON local via `GalleryPersistenceService`
- **Detección de paredes MR**: ARPlaneManager (planos verticales AR) con fallback por raycast
- **Detección de paredes VR**: Raycasting contra layer `VRWall`
- **Datos locales**: JSON via Newtonsoft.Json en `Application.persistentDataPath`
- **Renderizado**: Universal Render Pipeline (URP), passthrough MR en Quest 3/Pro

---

## Apendice A: Catalogo Completo de Obras

El catalogo contiene **252 obras** de dominio publico. La aplicacion gratuita incluye un conjunto inicial de obras iconicas; el resto se desbloquea con la compra unica del catalogo completo (ver seccion 12). El catalogo se amplia con actualizaciones de la aplicacion.

Las obras se listan a continuacion en orden alfabetico por titulo.

| # | Titulo | Artista | Ano | Movimiento | Museo |
|---|--------|---------|-----|------------|-------|
| 1 | A Bar at the Folies-Bergere | Edouard Manet | 1882 | Impressionism | The Courtauld Gallery, London |
| 2 | A Burial at Ornans | Gustave Courbet | 1850 | Realism | Musee d'Orsay, Paris |
| 3 | A Dutch Courtyard | Pieter de Hooch | 1658 | Dutch Golden Age | National Gallery of Art, Washington DC |
| 4 | A Lady Writing | Johannes Vermeer | 1665 | Dutch Golden Age | National Gallery of Art, Washington DC |
| 5 | A Sunday on La Grande Jatte | Georges Seurat | 1886 | Post-Impressionism | Art Institute of Chicago |
| 6 | A Young Girl Reading | Jean-Honore Fragonard | 1770 | Rococo | National Gallery of Art, Washington DC |
| 7 | Adeline Ravoux | Vincent van Gogh | 1890 | Post-Impressionism | Cleveland Museum of Art |
| 8 | Adoration of the Magi | Sandro Botticelli | 1475 | Renaissance | Uffizi Gallery, Florence |
| 9 | Allegory with Venus and Cupid | Agnolo Bronzino | 1545 | Mannerism | National Gallery, London |
| 10 | Almond Blossom | Vincent van Gogh | 1890 | Post-Impressionism | Van Gogh Museum, Amsterdam |
| 11 | Among the Sierra Nevada | Albert Bierstadt | 1868 | Romanticism | Smithsonian American Art Museum, Washington DC |
| 12 | Aristotle with a Bust of Homer | Rembrandt van Rijn | 1653 | Dutch Golden Age | The Metropolitan Museum of Art, New York |
| 13 | At the Moulin Rouge | Toulouse-Lautrec | 1892 | Post-Impressionism | Art Institute of Chicago |
| 14 | Bacchus and Ariadne | Tiziano | 1523 | Renaissance | Uffizi Gallery, Florence |
| 15 | Basket of Peaches | Jean-Baptiste Chardin | 1768 | Rococo | Louvre Museum, Paris |
| 16 | Bathers at Asnieres (Study) | Georges Seurat | 1884 | Post-Impressionism | Art Institute of Chicago |
| 17 | Beata Beatrix | Dante Gabriel Rossetti | 1870 | Pre-Raphaelite | Tate Britain |
| 18 | Before the Ballet | Edgar Degas | 1890 | Impressionism | National Gallery of Art, Washington DC |
| 19 | Bouquet of Sunflowers | Claude Monet | 1881 | Impressionism | The Metropolitan Museum of Art, New York |
| 20 | Breezing Up (A Fair Wind) | Winslow Homer | 1876 | Realism | National Gallery of Art, Washington DC |
| 21 | Bridge over a Pond of Water Lilies | Claude Monet | 1899 | Impressionism | The Metropolitan Museum of Art, New York |
| 22 | Cafe Terrace at Night | Vincent van Gogh | 1888 | Post-Impressionism | Kroller-Muller Museum, Otterlo |
| 23 | Card Players | Paul Cezanne | 1895 | Post-Impressionism | Musee d'Orsay, Paris |
| 24 | Christ in the Storm (Copy) | Rembrandt van Rijn | 1633 | Dutch Golden Age | Isabella Stewart Gardner Museum, Boston |
| 25 | Dance at Bougival | Pierre-Auguste Renoir | 1883 | Impressionism | Museum of Fine Arts, Boston |
| 26 | Dance at the Moulin de la Galette | Pierre-Auguste Renoir | 1876 | Impressionism | Musee d'Orsay, Paris |
| 27 | Dancers Practicing at the Barre | Edgar Degas | 1877 | Impressionism | The Metropolitan Museum of Art, New York |
| 28 | David with the Head of Goliath | Caravaggio | 1610 | Baroque | Borghese Gallery, Rome |
| 29 | Day of the God | Paul Gauguin | 1894 | Post-Impressionism | Art Institute of Chicago |
| 30 | Declaration of Independence | John Trumbull | 1819 | Neoclassicism | Yale University Art Gallery |
| 31 | Descent from the Cross | Rembrandt van Rijn | 1634 | Dutch Golden Age | Alte Pinakothek, Munich |
| 32 | Equestrian Portrait of Charles I | Anthony van Dyck | 1635 | Baroque | National Gallery, London |
| 33 | Et in Arcadia Ego | Nicolas Poussin | 1638 | Baroque | Louvre Museum, Paris |
| 34 | Field with Irises near Arles | Vincent van Gogh | 1888 | Post-Impressionism | Van Gogh Museum, Amsterdam |
| 35 | First Steps (After Millet) | Vincent van Gogh | 1890 | Post-Impressionism | The Metropolitan Museum of Art, New York |
| 36 | Flaming June | Frederic Leighton | 1895 | Realism | Ponce Museum of Art, Puerto Rico |
| 37 | Flower Beds at Vetheuil | Claude Monet | 1881 | Impressionism | Museum of Fine Arts, Boston |
| 38 | Flowering Plum Orchard | Vincent van Gogh | 1887 | Post-Impressionism | Van Gogh Museum, Amsterdam |
| 39 | Flowers in a Terracotta Vase | Jan van Huysum | 1736 | Baroque | National Gallery, London |
| 40 | Fur Traders on the Missouri | George Caleb Bingham | 1845 | Realism | The Metropolitan Museum of Art, New York |
| 41 | Gauguin's Chair | Vincent van Gogh | 1888 | Post-Impressionism | Van Gogh Museum, Amsterdam |
| 42 | Ginevra de' Benci | Leonardo da Vinci | 1474 | Renaissance | National Gallery of Art, Washington DC |
| 43 | Girl Reading a Letter at an Open Window | Johannes Vermeer | 1657 | Dutch Golden Age | Gemaldegalerie Alte Meister, Dresden |
| 44 | Girl with a Hoop | Pierre-Auguste Renoir | 1885 | Impressionism | National Gallery of Art, Washington DC |
| 45 | Girl with a Pearl Earring | Johannes Vermeer | 1665 | Dutch Golden Age | Mauritshuis, The Hague |
| 46 | Girl with a Watering Can | Pierre-Auguste Renoir | 1876 | Impressionism | National Gallery of Art, Washington DC |
| 47 | Glass of Lemonade | Gerard ter Borch | 1664 | Dutch Golden Age | Hermitage Museum, Saint Petersburg |
| 48 | Hunters in the Snow | Pieter Bruegel the Elder | 1565 | Renaissance | Kunsthistorisches Museum, Vienna |
| 49 | Immaculate Conception of the Venerables | Bartolome Esteban Murillo | 1665 | Baroque | Museo del Prado, Madrid |
| 50 | Impression, Sunrise | Claude Monet | 1872 | Impressionism | Musee Marmottan Monet, Paris |
| 51 | In the Meadow | Pierre-Auguste Renoir | 1888 | Impressionism | The Metropolitan Museum of Art, New York |
| 52 | Irises | Vincent van Gogh | 1889 | Post-Impressionism | The Metropolitan Museum of Art, New York |
| 53 | Isaac and Rebecca, Known as 'The Jewish Bride' | Rembrandt van Rijn | 1665 | Dutch Golden Age | Rijksmuseum, Amsterdam |
| 54 | Judith Beheading Holofernes (Caravaggio) | Caravaggio | 1599 | Baroque | Galleria Nazionale d'Arte Antica, Rome |
| 55 | Judith Slaying Holofernes | Artemisia Gentileschi | 1612 | Baroque | Uffizi Gallery, Florence |
| 56 | Kindred Spirits | Asher B. Durand | 1849 | Romanticism | Crystal Bridges Museum of American Art, Bentonville |
| 57 | Lady Agnew of Lochnaw | John Singer Sargent | 1892 | Realism | Scottish National Gallery, Edinburgh |
| 58 | Lady with an Ermine | Leonardo da Vinci | 1490 | Renaissance | Czartoryski Museum, Krakow |
| 59 | Lake Albano and Castel Gandolfo | Jean-Baptiste-Camille Corot | 1826 | Romanticism | The Metropolitan Museum of Art, New York |
| 60 | Lamentation of Christ | Giotto | 1305 | Renaissance | Scrovegni Chapel, Padua |
| 61 | Lamentation over the Dead Christ | Andrea Mantegna | 1480 | Renaissance | Pinacoteca di Brera, Milan |
| 62 | Landscape from Saint-Remy | Vincent van Gogh | 1889 | Post-Impressionism | Ny Carlsberg Glyptotek, Copenhagen |
| 63 | Las Meninas | Diego Velazquez | 1656 | Baroque | Museo del Prado, Madrid |
| 64 | Liberty Leading the People | Eugene Delacroix | 1830 | Romanticism | Louvre Museum, Paris |
| 65 | Luncheon of the Boating Party | Pierre-Auguste Renoir | 1881 | Impressionism | The Phillips Collection, Washington DC |
| 66 | Luncheon on the Grass | Edouard Manet | 1863 | Realism | Musee d'Orsay, Paris |
| 67 | Mada Primavesi | Gustav Klimt | 1912 | Symbolism | The Metropolitan Museum of Art, New York |
| 68 | Madame Georges Charpentier | Pierre-Auguste Renoir | 1878 | Impressionism | The Metropolitan Museum of Art, New York |
| 69 | Madame X | John Singer Sargent | 1884 | Realism | The Metropolitan Museum of Art, New York |
| 70 | Madonna of the Goldfinch (Copy) | Raphael | 1506 | Renaissance | Uffizi Gallery, Florence |
| 71 | Madonna with the Long Neck | Parmigianino | 1535 | Mannerism | Uffizi Gallery, Florence |
| 72 | Max Schmitt in a Single Scull | Thomas Eakins | 1871 | Realism | The Metropolitan Museum of Art, New York |
| 73 | Mona Lisa | Leonardo da Vinci | 1503 | Renaissance | Louvre Museum, Paris |
| 74 | Mont Sainte-Victoire | Paul Cezanne | 1904 | Post-Impressionism | Hermitage Museum, Saint Petersburg |
| 75 | Mount Corcoran | Albert Bierstadt | 1877 | Romanticism | National Gallery of Art, Washington DC |
| 76 | Napoleon Crossing the Alps | Jacques-Louis David | 1801 | Neoclassicism | Chateau de Malmaison |
| 77 | Niagara Falls | Frederic Edwin Church | 1857 | Romanticism | National Gallery of Art, Washington DC |
| 78 | Oath of the Horatii | Jacques-Louis David | 1784 | Neoclassicism | Louvre Museum, Paris |
| 79 | Olympia | Edouard Manet | 1863 | Realism | Musee d'Orsay, Paris |
| 80 | Ophelia | John Everett Millais | 1851 | Pre-Raphaelite | Tate Britain |
| 81 | Orpheus | Odilon Redon | 1903 | Symbolism | Cleveland Museum of Art |
| 82 | Pentecost | Giotto | 1305 | Renaissance | National Gallery, London |
| 83 | Piazza San Marco, Venice | Pierre-Auguste Renoir | 1881 | Impressionism | Minneapolis Institute of Art |
| 84 | Poplars (Three Pink Autumn Trees) | Claude Monet | 1891 | Impressionism | The Metropolitan Museum of Art, New York |
| 85 | Portrait of a Girl in Blue | Johannes C. Verspronck | 1641 | Dutch Golden Age | Rijksmuseum, Amsterdam |
| 86 | Portrait of Dr. Gachet (Etching) | Vincent van Gogh | 1890 | Post-Impressionism | Musee d'Orsay, Paris |
| 87 | Portrait of Henry VIII | Hans Holbein the Younger | 1537 | Renaissance | Thyssen-Bornemisza Museum, Madrid |
| 88 | Portrait of Juan de Pareja | Diego Velazquez | 1650 | Baroque | The Metropolitan Museum of Art, New York |
| 89 | Portrait of Madame Brunet | Edouard Manet | 1861 | Realism | J. Paul Getty Museum, Los Angeles |
| 90 | Portrait of the Infante Luis de Borbon | Francisco de Goya | 1783 | Rococo | Cleveland Museum of Art |
| 91 | Primavera | Sandro Botticelli | 1480 | Renaissance | Uffizi Gallery, Florence |
| 92 | Rain, Steam and Speed | Joseph Mallord William Turner | 1844 | Romanticism | National Gallery, London |
| 93 | Reading | Berthe Morisot | 1873 | Impressionism | Cleveland Museum of Art |
| 94 | Reclining Young Woman in Spanish Costume | Edouard Manet | 1862 | Realism | Yale University Art Gallery |
| 95 | Roses | Vincent van Gogh | 1890 | Post-Impressionism | The Metropolitan Museum of Art, New York |
| 96 | Rounded Flower Bed | Claude Monet | 1876 | Impressionism | Detroit Institute of Arts |
| 97 | Saint Francis in Ecstasy | Giovanni Bellini | 1480 | Renaissance | The Frick Collection, New York |
| 98 | Saint Francis in Meditation | Francisco de Zurbaran | 1635 | Baroque | National Gallery, London |
| 99 | Saint George and the Dragon | Raphael | 1506 | Renaissance | National Gallery of Art, Washington DC |
| 100 | Saint Jerome as Scholar | El Greco | 1610 | Mannerism | The Metropolitan Museum of Art, New York |
| 101 | Saturn Devouring His Son | Francisco de Goya | 1823 | Romanticism | Museo del Prado, Madrid |
| 102 | Self-Portrait (1500) | Albrecht Durer | 1500 | Renaissance | Alte Pinakothek, Munich |
| 103 | Self-Portrait (1659) | Rembrandt van Rijn | 1659 | Dutch Golden Age | National Gallery of Art, Washington DC |
| 104 | Self-Portrait (1887) | Vincent van Gogh | 1887 | Post-Impressionism | Art Institute of Chicago |
| 105 | Self-Portrait (1889) | Vincent van Gogh | 1889 | Post-Impressionism | National Gallery of Art, Washington DC |
| 106 | Self-Portrait (Degas) | Edgar Degas | 1855 | Impressionism | The Metropolitan Museum of Art, New York |
| 107 | Self-Portrait as a Painter | Vincent van Gogh | 1888 | Post-Impressionism | Van Gogh Museum, Amsterdam |
| 108 | Self-Portrait with a Straw Hat | Vincent van Gogh | 1887 | Post-Impressionism | The Metropolitan Museum of Art, New York |
| 109 | Self-Portrait with Bandaged Ear | Vincent van Gogh | 1889 | Post-Impressionism | The Courtauld Gallery, London |
| 110 | Self-Portrait with Grey Felt Hat | Vincent van Gogh | 1887 | Post-Impressionism | Van Gogh Museum, Amsterdam |
| 111 | Self-Portrait with Two Circles | Rembrandt van Rijn | 1665 | Dutch Golden Age | Kenwood House, London |
| 112 | Serenade | Judith Leyster | 1629 | Dutch Golden Age | Rijksmuseum, Amsterdam |
| 113 | Shoes | Vincent van Gogh | 1886 | Post-Impressionism | Van Gogh Museum, Amsterdam |
| 114 | Sistine Madonna | Raphael | 1512 | Renaissance | Gemaldegalerie Alte Meister, Dresden |
| 115 | Small Cowper Madonna | Raphael | 1505 | Renaissance | National Gallery of Art, Washington DC |
| 116 | Snap the Whip | Winslow Homer | 1872 | Realism | The Metropolitan Museum of Art, New York |
| 117 | Snow at Argenteuil | Claude Monet | 1875 | Impressionism | The Metropolitan Museum of Art, New York |
| 118 | Stag at Sharkey's | George Bellows | 1909 | Realism | Cleveland Museum of Art |
| 119 | Starry Night Over the Rhone | Vincent van Gogh | 1888 | Post-Impressionism | Musee d'Orsay, Paris |
| 120 | Still Life with a Glass and Oysters | Jan Davidsz de Heem | 1640 | Dutch Golden Age | The Metropolitan Museum of Art, New York |
| 121 | Still Life with a Silver Jug and a Porcelain Bowl | Willem Kalf | 1660 | Dutch Golden Age | Rijksmuseum, Amsterdam |
| 122 | Still Life with Apples and a Pot of Primroses | Paul Cezanne | 1890 | Post-Impressionism | The Metropolitan Museum of Art, New York |
| 123 | Still Life with Apples and Pears | Paul Cezanne | 1891 | Post-Impressionism | The Metropolitan Museum of Art, New York |
| 124 | Still Life with Flowers on a Marble Tabletop | Rachel Ruysch | 1716 | Dutch Golden Age | Rijksmuseum, Amsterdam |
| 125 | Still Life with Gilt Cup | Willem Claesz Heda | 1635 | Dutch Golden Age | Rijksmuseum, Amsterdam |
| 126 | Still Life with Ham | Willem Claesz Heda | 1650 | Dutch Golden Age | National Gallery of Art, Washington DC |
| 127 | Still Life with Musical Instruments, Books and Sculpture | Evaristo Baschenis | 1650 | Baroque | Museum Boijmans Van Beuningen, Rotterdam |
| 128 | Still Life with Oysters, a Silver Tazza, and Glassware | Willem Claesz Heda | 1635 | Dutch Golden Age | The Metropolitan Museum of Art, New York |
| 129 | Still Life with Quinces | Vincent van Gogh | 1887 | Post-Impressionism | The Courtauld Gallery, London |
| 130 | Sunflowers (1887) | Vincent van Gogh | 1887 | Post-Impressionism | The Metropolitan Museum of Art, New York |
| 131 | Sunflowers (Blue background) | Vincent van Gogh | 1888 | Post-Impressionism | Neue Pinakothek, Munich |
| 132 | Supper at Emmaus | Caravaggio | 1601 | Baroque | National Gallery, London |
| 133 | Susanna and the Elders | Jacopo Tintoretto | 1555 | Renaissance | Kunsthistorisches Museum, Vienna |
| 134 | The Absinthe Drinker | Edgar Degas | 1876 | Impressionism | Musee d'Orsay, Paris |
| 135 | The Ambassadors | Hans Holbein the Younger | 1533 | Renaissance | National Gallery, London |
| 136 | The Anatomy Lesson of Dr. Nicolaes Tulp | Rembrandt van Rijn | 1632 | Baroque | Mauritshuis, The Hague |
| 137 | The Angelus | Jean-Francois Millet | 1859 | Realism | Musee d'Orsay, Paris |
| 138 | The Annunciation (Copy) | Fra Angelico | 1434 | Renaissance | Museo del Prado, Madrid |
| 139 | The Argenteuil Bridge | Claude Monet | 1874 | Impressionism | National Gallery of Art, Washington DC |
| 140 | The Arnolfini Portrait | Jan van Eyck | 1434 | Renaissance | National Gallery, London |
| 141 | The Artist's Garden at Vetheuil | Claude Monet | 1880 | Impressionism | National Gallery of Art, Washington DC |
| 142 | The Banks of the Oise | Alfred Sisley | 1877 | Impressionism | National Gallery of Art, Washington DC |
| 143 | The Banquet of the Guard | Bartholomeus v. d. Helst | 1648 | Dutch Golden Age | Rijksmuseum, Amsterdam |
| 144 | The Baptism of Christ | Piero della Francesca | 1450 | Renaissance | National Gallery, London |
| 145 | The Basket of Apples | Paul Cezanne | 1893 | Post-Impressionism | Art Institute of Chicago |
| 146 | The Beach at Sainte-Adresse | Claude Monet | 1867 | Impressionism | The Metropolitan Museum of Art, New York |
| 147 | The Bedroom | Vincent van Gogh | 1888 | Post-Impressionism | Art Institute of Chicago |
| 148 | The Birth of Venus | Sandro Botticelli | 1485 | Renaissance | Uffizi Gallery, Florence |
| 149 | The Birth of Venus (Bouguereau) | William-Adolphe Bouguereau | 1879 | Realism | Musee d'Orsay, Paris |
| 150 | The Blue Boy | Thomas Gainsborough | 1770 | Rococo | Huntington Library, San Marino |
| 151 | The Brook | Paul Cezanne | 1900 | Post-Impressionism | Cleveland Museum of Art |
| 152 | The Burial of the Count of Orgaz | El Greco | 1586 | Mannerism | Church of Santo Tome, Toledo |
| 153 | The Burning of the Houses of Lords and Commons | Joseph Mallord William Turner | 1835 | Romanticism | Cleveland Museum of Art |
| 154 | The Calling of Saint Matthew | Caravaggio | 1600 | Baroque | San Luigi dei Francesi, Rome |
| 155 | The Child's Bath | Mary Cassatt | 1893 | Impressionism | Art Institute of Chicago |
| 156 | The Clothed Maja | Francisco de Goya | 1808 | Romanticism | Museo del Prado, Madrid |
| 157 | The Creation of Adam | Michelangelo | 1512 | Renaissance | Sistine Chapel, Vatican Museums |
| 158 | The Crucifixion of Saint Andrew | Caravaggio | 1607 | Baroque | Cleveland Museum of Art |
| 159 | The Dance Class | Edgar Degas | 1874 | Impressionism | The Metropolitan Museum of Art, New York |
| 160 | The Death of Marat | Jacques-Louis David | 1793 | Neoclassicism | Royal Museums of Fine Arts of Belgium |
| 161 | The Death of Socrates | Jacques-Louis David | 1787 | Neoclassicism | The Metropolitan Museum of Art, New York |
| 162 | The Descent from the Cross (Rubens) | Peter Paul Rubens | 1614 | Baroque | Cathedral of Our Lady, Antwerp |
| 163 | The Descent from the Cross (van der Weyden) | Rogier van der Weyden | 1435 | Renaissance | Museo del Prado, Madrid |
| 164 | The Dream | Henri Rousseau | 1910 | Post-Impressionism | Museum of Modern Art, New York |
| 165 | The Fifer | Edouard Manet | 1866 | Realism | Musee d'Orsay, Paris |
| 166 | The Fighting Temeraire | Joseph Mallord William Turner | 1839 | Romanticism | National Gallery, London |
| 167 | The Garden of Earthly Delights | Hieronymus Bosch | 1503 | Renaissance | Museo del Prado, Madrid |
| 168 | The Garden of Love | Peter Paul Rubens | 1633 | Baroque | Museo del Prado, Madrid |
| 169 | The Garden of the Asylum at Saint-Remy | Vincent van Gogh | 1889 | Post-Impressionism | Kroller-Muller Museum, Otterlo |
| 170 | The Gleaners | Jean-Francois Millet | 1857 | Realism | Musee d'Orsay, Paris |
| 171 | The Goldfinch | Carel Fabritius | 1654 | Dutch Golden Age | Mauritshuis, The Hague |
| 172 | The Grand Canal, Venice | Edouard Manet | 1874 | Impressionism | Shelburne Museum, Vermont |
| 173 | The Grand Canal, Venice (Canaleto) | Canaletto | 1730 | Baroque | The Metropolitan Museum of Art, New York |
| 174 | The Grand Odalisque | Jean-Auguste-Dominique Ingres | 1814 | Neoclassicism | Louvre Museum, Paris |
| 175 | The Gross Clinic | Thomas Eakins | 1875 | Realism | Philadelphia Museum of Art |
| 176 | The Harvest | Vincent van Gogh | 1888 | Post-Impressionism | Van Gogh Museum, Amsterdam |
| 177 | The Hay Wain | John Constable | 1821 | Romanticism | National Gallery, London |
| 178 | The Horse Fair | Rosa Bonheur | 1853 | Realism | The Metropolitan Museum of Art, New York |
| 179 | The Houses of Parliament, Sunset | Claude Monet | 1903 | Impressionism | National Gallery of Art, Washington DC |
| 180 | The Icebergs | Frederic Edwin Church | 1861 | Romanticism | Dallas Museum of Art |
| 181 | The Japanese Footbridge | Claude Monet | 1899 | Impressionism | National Gallery of Art, Washington DC |
| 182 | The Kiss | Francesco Hayez | 1859 | Romanticism | Pinacoteca di Brera, Milan |
| 183 | The Kiss (Gustav Klimt) | Gustav Klimt | 1907 | Symbolism | Belvedere Museum, Vienna |
| 184 | The Lacemaker | Johannes Vermeer | 1669 | Dutch Golden Age | Louvre Museum, Paris |
| 185 | The Lady of Shalott | John William Waterhouse | 1888 | Pre-Raphaelite | Tate Britain |
| 186 | The Langlois Bridge at Arles | Vincent van Gogh | 1888 | Post-Impressionism | Van Gogh Museum, Amsterdam |
| 187 | The Large Bathers | Paul Cezanne | 1906 | Post-Impressionism | Philadelphia Museum of Art |
| 188 | The Last Supper (Copy) | Giampietrino and Giovanni Antonio Boltraffio | 1520 | Renaissance | Louvre Museum, Paris |
| 189 | The Little Street | Johannes Vermeer | 1658 | Dutch Golden Age | Rijksmuseum, Amsterdam |
| 190 | The Merry Family | Jan Steen | 1668 | Dutch Golden Age | Rijksmuseum, Amsterdam |
| 191 | The Milkmaid | Johannes Vermeer | 1658 | Dutch Golden Age | Rijksmuseum, Amsterdam |
| 192 | The Monet Family in Their Garden | Edouard Manet | 1874 | Impressionism | The Metropolitan Museum of Art, New York |
| 193 | The Mousme | Vincent van Gogh | 1888 | Post-Impressionism | National Gallery of Art, Washington DC |
| 194 | The Night Cafe | Vincent van Gogh | 1888 | Post-Impressionism | Yale University Art Gallery |
| 195 | The Night Watch | Rembrandt van Rijn | 1642 | Dutch Golden Age | Rijksmuseum, Amsterdam |
| 196 | The Nude Maja | Francisco de Goya | 1800 | Romanticism | Museo del Prado, Madrid |
| 197 | The Oxbow | Thomas Cole | 1836 | Romanticism | The Metropolitan Museum of Art, New York |
| 198 | The Penitent Magdalene | Georges de La Tour | 1640 | Baroque | The Metropolitan Museum of Art, New York |
| 199 | The Potato Eaters | Vincent van Gogh | 1885 | Post-Impressionism | Van Gogh Museum, Amsterdam |
| 200 | The Raft of the Medusa | Theodore Gericault | 1819 | Romanticism | Louvre Museum, Paris |
| 201 | The Return of the Prodigal Son | Rembrandt van Rijn | 1668 | Dutch Golden Age | Hermitage Museum, Saint Petersburg |
| 202 | The Rokeby Venus | Diego Velazquez | 1651 | Baroque | National Gallery, London |
| 203 | The School of Athens | Raphael | 1511 | Renaissance | Apostolic Palace, Vatican Museums |
| 204 | The Seine at La Grande Jatte | Georges Seurat | 1888 | Post-Impressionism | Royal Museums of Fine Arts of Belgium, Brussels |
| 205 | The Silver Tureen | Jean-Baptiste Chardin | 1728 | Rococo | The Metropolitan Museum of Art, New York |
| 206 | The Skiff | Pierre-Auguste Renoir | 1875 | Impressionism | National Gallery, London |
| 207 | The Sleeping Gypsy | Henri Rousseau | 1897 | Post-Impressionism | Museum of Modern Art, New York |
| 208 | The Sower | Vincent van Gogh | 1888 | Post-Impressionism | Van Gogh Museum, Amsterdam |
| 209 | The Starry Night | Vincent van Gogh | 1889 | Post-Impressionism | Museum of Modern Art, New York |
| 210 | The Swing | Jean-Honore Fragonard | 1767 | Rococo | The Wallace Collection, London |
| 211 | The Third of May 1808 | Francisco de Goya | 1814 | Romanticism | Museo del Prado, Madrid |
| 212 | The Third-Class Carriage | Honore Daumier | 1864 | Realism | The Metropolitan Museum of Art, New York |
| 213 | The Threatened Swan | Jan Asselijn | 1650 | Dutch Golden Age | Rijksmuseum, Amsterdam |
| 214 | The Three Graces | Peter Paul Rubens | 1635 | Baroque | Museo del Prado, Madrid |
| 215 | The Transfiguration | Raphael | 1520 | Renaissance | Vatican Museums, Rome |
| 216 | The Triumph of Death | Pieter Bruegel the Elder | 1562 | Renaissance | Museo del Prado, Madrid |
| 217 | The Valley of the Seine, from the Hills of Giverny | Theodore Robinson | 1892 | Impressionism | National Gallery of Art, Washington DC |
| 218 | The Veteran in a New Field | Winslow Homer | 1865 | Realism | The Metropolitan Museum of Art, New York |
| 219 | The Virgin of the Rocks | Leonardo da Vinci | 1486 | Renaissance | Louvre Museum, Paris |
| 220 | The Wedding at Cana | Paolo Veronese | 1563 | Renaissance | Louvre Museum, Paris |
| 221 | The Windmill | Rembrandt van Rijn | 1645 | Dutch Golden Age | National Gallery of Art, Washington DC |
| 222 | The Windmill at Wijk bij Duurstede | Jacob van Ruisdael | 1670 | Dutch Golden Age | Rijksmuseum, Amsterdam |
| 223 | The Woman with a Fan | Pierre-Auguste Renoir | 1880 | Impressionism | Hermitage Museum, Saint Petersburg |
| 224 | The Yellow House | Vincent van Gogh | 1888 | Post-Impressionism | Van Gogh Museum, Amsterdam |
| 225 | Tower of Babel | Pieter Bruegel the Elder | 1563 | Renaissance | Kunsthistorisches Museum, Vienna |
| 226 | Two Sisters (On the Terrace) | Pierre-Auguste Renoir | 1881 | Impressionism | Art Institute of Chicago |
| 227 | Vase of Flowers (Pink Background) | Odilon Redon | 1906 | Symbolism | The Metropolitan Museum of Art, New York |
| 228 | Venus of Urbino | Titian | 1538 | Renaissance | Uffizi Gallery, Florence |
| 229 | View of Delft | Johannes Vermeer | 1660 | Dutch Golden Age | Mauritshuis, The Hague |
| 230 | View of Houses in Delft | Johannes Vermeer | 1658 | Dutch Golden Age | Rijksmuseum, Amsterdam |
| 231 | View of Toledo | El Greco | 1599 | Mannerism | The Metropolitan Museum of Art, New York |
| 232 | View of Vetheuil | Claude Monet | 1880 | Impressionism | The Metropolitan Museum of Art, New York |
| 233 | Village of Eragny | Camille Pissarro | 1895 | Impressionism | Birmingham Museum of Art |
| 234 | Virgin and Child with St. Anne | Leonardo da Vinci | 1503 | Renaissance | Louvre Museum, Paris |
| 235 | Vision After the Sermon | Paul Gauguin | 1888 | Post-Impressionism | Scottish National Gallery, Edinburgh |
| 236 | Walk on the Seashore | Joaquin Sorolla | 1909 | Realism | Sorolla Museum, Madrid |
| 237 | Wanderer above the Sea of Fog | Caspar David Friedrich | 1818 | Romanticism | Kunsthalle Hamburg |
| 238 | Washington Crossing the Delaware | Emanuel Leutze | 1851 | Romanticism | The Metropolitan Museum of Art, New York |
| 239 | Water Lilies (Agapanthus) | Claude Monet | 1920 | Impressionism | Cleveland Museum of Art |
| 240 | Watson and the Shark | John Singleton Copley | 1778 | Romanticism | National Gallery of Art, Washington DC |
| 241 | Wheatfield with Crows | Vincent van Gogh | 1890 | Post-Impressionism | Van Gogh Museum, Amsterdam |
| 242 | Wheatfield with Cypresses | Vincent van Gogh | 1889 | Post-Impressionism | The Metropolitan Museum of Art, New York |
| 243 | Where Do We Come From? What Are We? Where Are We Going? | Paul Gauguin | 1898 | Post-Impressionism | Museum of Fine Arts, Boston |
| 244 | Whistler's Mother | James McNeill Whistler | 1871 | Realism | Musee d'Orsay, Paris |
| 245 | Winter Landscape with Skaters | Hendrick Avercamp | 1608 | Dutch Golden Age | Rijksmuseum, Amsterdam |
| 246 | Woman Holding a Balance | Johannes Vermeer | 1664 | Dutch Golden Age | National Gallery of Art, Washington DC |
| 247 | Woman with a Parasol | Claude Monet | 1875 | Impressionism | National Gallery of Art, Washington DC |
| 248 | Woman with a Pearl Necklace | Johannes Vermeer | 1664 | Dutch Golden Age | Gemaldegalerie, Berlin |
| 249 | Women of Tahiti | Paul Gauguin | 1891 | Post-Impressionism | Musee d'Orsay, Paris |
| 250 | Young Hare | Albrecht Durer | 1502 | Renaissance | Albertina, Vienna |
| 251 | Young Woman with a Water Pitcher | Johannes Vermeer | 1662 | Dutch Golden Age | The Metropolitan Museum of Art, New York |
| 252 | Young Woman with Peonies | Frederic Bazille | 1870 | Impressionism | National Gallery of Art, Washington DC |