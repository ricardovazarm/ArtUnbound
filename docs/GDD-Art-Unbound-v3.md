# Documento de Diseño de Juego (GDD): Art Unbound
- Versión: 3.0 (Estado de Implementación Actualizado)
- Fecha: Mayo 2026
- Plataforma: Meta Quest (Quest 2, Quest 3, Quest Pro)

---

## 1. Concepto de Alto Nivel

Art Unbound es un juego de puzzles para Meta Quest. Los jugadores reconstruyen obras maestras clásicas ensamblando piezas en un lienzo flotante frente a ellos, y al terminar pueden colgar el cuadro completado en su entorno, donde persiste entre sesiones.

En **Modo Mixto (MR)** — disponible en Quest 3 y Quest Pro — el cuadro se cuelga en una pared real del espacio físico del usuario mediante anclajes espaciales persistentes. En **Modo Virtual (VR)** — disponible en todos los dispositivos Quest — el cuadro se cuelga en las paredes de una galería de arte 3D inmersiva.

---

## 2. Resumen del Juego

- **Género**: Puzzle, Casual.
- **Modos**: Realidad Mixta (MR) y Realidad Virtual (VR).
- **Propuesta de valor única**: El espacio del usuario es parte del juego. Los cuadros completados se convierten en decoración persistente — en la pared real del hogar (MR) o en la galería virtual personal (VR). Las piezas tienen grosor tridimensional (0.5 cm) que les otorga presencia física.

---

## 3. Pilares de Diseño

- **Inmersión tangible**: Las piezas tienen volumen de 0.5 cm y se ensamblan en un lienzo flotante ergonómico.
- **Magia cotidiana**: Las obras completadas decoran permanentemente el espacio del usuario — físico en MR, virtual en VR.
- **Flexibilidad ergonómica**: El armado ocurre siempre en un lienzo flotante a distancia fija (0.4 m del usuario), permitiendo jugar sentado o de pie en cualquier lugar. La integración con la pared es una recompensa post-juego.

---

## 4. Flujo del Juego

### 4.1. Menú Principal

El menú principal es un panel curvo en espacio MR/VR, dividido en tres zonas:

**Panel izquierdo — Configuración**
- Control de volumen de música.
- Control de volumen de efectos de sonido.
- Muestra el nombre de la canción y artista en reproducción actual.

**Panel central — Galería**
- Cuadrícula de obras con paginación.
- Muestra miniatura de cada obra con su marco ganado (si aplica) y porcentaje de avance (si está en progreso).
- Filtros: Todas / En Progreso / Completadas.

**Panel derecho — Detalle de Obra**
- Se activa al seleccionar cualquier obra de la galería.
- Muestra la pintura en tamaño grande, título, artista, museo de origen y descripción.
- Ofrece tres botones de dificultad para iniciar el puzzle, con el tiempo récord alcanzado en cada uno.

### 4.2. Dificultad y Marcos

| Dificultad | Piezas aprox.* | Marco ganado |
|------------|----------------|--------------|
| Fácil      | ~64            | Bronce       |
| Normal     | ~144           | Plata        |
| Difícil    | ~256           | Oro          |

*El número exacto varía según las proporciones (aspect ratio) de cada pintura original.

El marco se asigna exclusivamente por la dificultad seleccionada. No existen penalizaciones ni requisitos adicionales para obtener un marco.

### 4.3. Pantalla de Armado

Al iniciar el puzzle, el espacio se divide en tres zonas:

**Centro — Tablero del Puzzle**
- El lienzo flotante a 0.4 m del usuario donde se arman las piezas.
- Ocupa el espacio visual principal.

**Izquierda — Panel de Información**
- Título y artista de la obra.
- Imagen de referencia completa de la obra.
- Nombre de la canción y artista en reproducción.
- Contadores en tiempo real:
  - Total de piezas.
  - Piezas colocadas correctamente.
  - Piezas colocadas incorrectamente.
- Número de paredes detectadas en el entorno (solo modo MR).
- Botón de salida al menú principal.
- Botón "¿Cuáles están mal?" (ver sección 4.5).

**Derecha — Bandeja de Piezas**
- Piezas disponibles para colocar, organizadas en páginas.
- Botones de navegación para avanzar y retroceder entre páginas.

### 4.4. Mecánica de Colocación de Piezas

1. El usuario toma una pieza con gesto de pinch (pulgar-índice).
2. La pieza sigue la mano del usuario mientras está siendo sostenida.
3. Al soltar la pieza cerca del tablero, hace snap automático al **slot disponible más cercano** — no al punto exacto donde se suelta.
4. Si la pieza corresponde al slot correcto, el contador de correctas aumenta y se reproduce un efecto visual y sonoro de confirmación. Si no, aumenta el de incorrectas.
5. Una pieza colocada puede ser tomada nuevamente y reubicada.
6. Al completar el puzzle (todas las piezas en su slot correcto), se activa la animación de revelación.

> **Nota**: No existe una distancia mínima de snap — cualquier pieza soltada cerca del tablero se coloca en el slot disponible más cercano. No hay rechazo por morfología; el sistema registra si el resultado es correcto o no mediante los contadores.

### 4.5. Resaltar Piezas Incorrectas

El panel izquierdo incluye un botón **"¿Cuáles están mal?"** que el usuario puede presionar en cualquier momento durante el armado. Al activarlo:

1. Cada pieza colocada en el lugar equivocado emite un **burst de partículas rojas** en su posición.
2. La pieza realiza un **movimiento de oscilación** (wiggle) de lado a lado durante ~0.45 segundos.
3. Se reproduce el **sonido de colocación incorrecta** una sola vez para todo el conjunto.
4. Los efectos se aplican de forma **escalonada** (80 ms entre pieza y pieza) para que el usuario pueda distinguir cuáles son.

El botón no mueve ni reposiciona las piezas — solo las señala visualmente para que el usuario decida si las quiere corregir.

### 4.6. Hitos durante el Armado

El sistema detecta y celebra:
- Completar una fila completa.
- Completar una columna completa.
- Completar un borde (lado exterior del puzzle).
- Completar el marco exterior completo.

Cada hito dispara un mensaje contextual en pantalla y efectos de partículas.

### 4.7. Finalización y Post-Juego

1. Al colocar todas las piezas correctamente se activa la animación de revelación: el cuadro completo aparece con el marco de la dificultad elegida.
2. Se muestra el panel de post-juego con el tiempo de resolución y el marco ganado.
3. El juego presenta la opción de colgar el cuadro en una pared.

---

## 5. Sistema de Colgado — Modo MR (Mixed Reality)

### 5.1. Flujo de Colocación

1. Desde el post-juego, el usuario toma el cuadro completado con un gesto de pinch.
2. Toda la UI se oculta durante el arrastre.
3. El cuadro sigue la mano y rota dinámicamente para alinearse con la pared más cercana.
4. Al soltar el cuadro **a 20 cm o menos de una pared real**, el cuadro se fija en esa posición y se crea un anclaje espacial persistente.
5. Al soltar **alejado de una pared**, el cuadro desaparece y la UI se restaura. El cuadro sigue disponible para intentar colgarlo de nuevo.

### 5.2. Reposicionamiento y Retiro

- Los cuadros ya colgados en paredes pueden tomarse de nuevo con pinch.
- Al soltar cerca de una pared (≤ 20 cm): el cuadro se reposiciona y el anclaje espacial se actualiza.
- Al soltar lejos de una pared: el cuadro se retira de la pared y desaparece.

### 5.3. Persistencia

- Los cuadros colgados persisten entre sesiones mediante Meta Spatial Anchors (AR Foundation).
- Al iniciar la app, todos los cuadros colgados anteriormente se reconstituyen en sus posiciones reales.
- La detección de paredes usa ARPlaneManager (planos verticales AR) con fallback por raycast.

---

## 6. Modo VR (Virtual Reality)

### 6.1. Galería Virtual

En modo VR el usuario se encuentra dentro de una galería de arte 3D inmersiva. El puzzle se arma en el mismo lienzo flotante que en MR, pero la interacción se basa en **apuntar y disparar con el controlador** en lugar de gestos de pinch. Las paredes de la galería son superficies virtuales donde puede colgar sus obras completadas.

### 6.2. Mecánica de Colocación de Piezas en VR

La interacción con las piezas en VR es diferente al modo MR:

1. El usuario apunta al controlador hacia una pieza en la bandeja.
2. Presiona el gatillo para **seleccionarla**.
3. Apunta hacia el slot del tablero donde quiere colocarla.
4. Presiona el gatillo nuevamente y la pieza se **mueve directamente a ese slot**.

No hay arrastre físico: la pieza salta del punto de selección al destino elegido. La validación de corrección (correcto/incorrecto) funciona igual que en MR.

### 6.3. Flujo de Colgado en VR

El flujo de colgar un cuadro en VR también es distinto al MR — no requiere arrastrarlo físicamente:

1. Desde el post-juego, el usuario apunta al cuadro completado y presiona el gatillo para **seleccionarlo**.
2. El usuario se desplaza (teleportación) hacia la pared de la galería donde quiere colocarlo.
3. Apunta al espacio exacto de la pared donde desea que quede.
4. Presiona el gatillo para **fijar el cuadro** en esa posición.

### 6.4. Reposicionamiento en VR

- Los cuadros ya colgados pueden reposicionarse con el mismo flujo: apuntar → gatillo para seleccionar, moverse a la nueva ubicación, apuntar → gatillo para fijar.

### 6.5. Diferencias respecto al modo MR

| Aspecto | Modo MR | Modo VR |
|---------|---------|---------|
| Entorno | Habitación real (passthrough) | Galería 3D virtual |
| Interacción con piezas | Pinch + arrastre físico | Apuntar + gatillo (selección y destino) |
| Interacción con cuadros | Pinch + arrastre hacia pared | Apuntar + gatillo + moverse + apuntar + gatillo |
| Detección de paredes | ARPlaneManager (planos AR reales) | Raycasting contra layer VRWall |
| Persistencia | Meta Spatial Anchors | JSON local (GalleryPersistenceService) |
| Disponibilidad | Quest 3 y Quest Pro únicamente | Todos los dispositivos Quest |

### 6.6. Locomoción en VR

- **Teleportación**: apuntar al suelo con el controlador y presionar el gatillo para desplazarse.
- **Snap Turn**: rotación en incrementos fijos (izquierda/derecha con el joystick).
- No se usa locomoción continua para reducir mareo.

---

## 7. Morfología de las Piezas

### 7.1. Forma Base

Todas las piezas son **cuadradas**. Lo que las diferencia entre sí es el perfil de cada uno de sus cuatro lados, que puede tener uno de tres estados:

- **Plano**: el lado es recto, sin muesca. Exclusivo de los bordes exteriores del puzzle — toda pieza que esté en el perímetro tiene al menos un lado plano.
- **Positivo (tab)**: el lado tiene una muesca triangular que sobresale hacia afuera.
- **Negativo (blank)**: el lado tiene una muesca triangular que entra hacia adentro.

El grosor de cada pieza es de **0.5 cm**, lo que les da presencia física tridimensional al agarrarlas.

### 7.2. Complementariedad entre Piezas Vecinas

Dos piezas adyacentes siempre tienen lados complementarios en su borde compartido: si una tiene Positivo, la otra tiene Negativo, y viceversa. Esto garantiza que, visualmente, las muescas encajen entre sí aunque el sistema no valide morfología al colocar — la pieza correcta siempre tiene el perfil que "completa" a sus vecinas.

### 7.3. Generación Procedural en Tiempo de Ejecución

Las mallas de las piezas **no están pre-generadas ni almacenadas** — se construyen completamente en tiempo de ejecución al iniciar cada puzzle. El sistema (`PieceMeshGenerator`) recibe la configuración del grid y genera cada malla de forma procedural, calculando los vértices de las muescas triangulares para cada lado según la morfología asignada.

La morfología de cada arista interna se asigna usando una semilla determinista derivada del ID de la obra, lo que garantiza que el mismo puzzle siempre genere el mismo patrón de piezas entre sesiones, sin necesidad de guardar las mallas.

### 7.4. Colocación y Validación

El sistema no rechaza piezas por morfología. Cualquier pieza soltada cerca del tablero se coloca automáticamente en el slot disponible más cercano. La corrección de la colocación se registra mediante los contadores del panel de información:
- **Correctas**: piezas en su slot correspondiente por ID.
- **Incorrectas**: piezas colocadas en un slot que no les corresponde.

El puzzle se considera completo cuando todas las piezas están en sus slots correctos.

---

## 8. Contenido

### 8.1. Catálogo de Obras

Las obras del catálogo son pinturas de museos reconocidos a nivel mundial, creadas por artistas clásicos de renombre como Van Gogh, Rembrandt, Monet, Vermeer, Cézanne, Renoir, entre otros.

- El catálogo utiliza exclusivamente **obras de dominio público**, lo que permite su uso sin restricciones legales.
- Cada obra tiene: ID único, título, artista, museo de origen, descripción, thumbnail y textura de alta resolución (hasta 4096×4096).
- El catálogo se amplía mediante actualizaciones de la aplicación.

### 8.2. Packs Temáticos

El contenido está organizado en **18 colecciones temáticas**:

| Pack | Temática |
|------|----------|
| Base | Obras icónicas de la historia del arte |
| Van Gogh — Luz y Color en Provenza | Paisajes y naturalezas vivas |
| Van Gogh — Espejos del Alma | Autorretratos y vida interior |
| Monet — Jardines de Luz | Nenúfares y jardines en distintas luces |
| Rembrandt y Los Maestros Holandeses | Luz y sombra del Siglo de Oro holandés |
| Vermeer y el Interior Holandés | Escenas cotidianas con luz de ventana |
| El Nacimiento del Renacimiento | Botticelli, Fra Angelico, Primitivos Italianos |
| Alto Renacimiento y Manierismo | Miguel Ángel, Rafael, Tiziano |
| Grandes Maestros de Europa | De Durero a Turner |
| Razón y Revolución | Del Rococó al Romanticismo |
| Caravaggio y el Drama Barroco | Teatralidad del claroscuro |
| La Familia Impresionista | Morisot, Cassatt y el círculo de Manet |
| Renoir y Degas — Tardes Parisinas | Bailarinas, cafés y luz artificial |
| Sueños Post-Impresionistas | Cézanne, Gauguin, Seurat |
| Campos de Labor | Campesinos y realismo de Van Gogh |
| Frontera Americana | Naturaleza salvaje y vida en el oeste |
| El Arte de la Quietud | Naturalezas muertas a través de los siglos |
| Musas y Visiones | Simbolismo y belleza de fin de siglo |

### 8.3. Cuadro Semanal *(funcionalidad futura — no implementada)*

Se planea un sistema de desbloqueo semanal basado en la fecha del dispositivo, donde una obra especial estará disponible por tiempo limitado cada semana. La infraestructura base (`WeeklyUnlockService`) ya existe en el proyecto pero no está activa.

---

## 9. Progresión y Guardado

- Se registra el **tiempo de resolución** por obra y por dificultad (récord histórico).
- No hay sistema de puntuación numérica ni penalizaciones.
- El progreso en puzzles incompletos se guarda automáticamente (al pausar o salir).
- Los datos se guardan localmente en el dispositivo en formato JSON.
- El guardado se realiza automáticamente en `OnApplicationPause` y `OnApplicationQuit`.

---

## 10. Audio

### 10.1. Música

La música del juego es **música clásica** de compositores reconocidos como Mozart, Beethoven, Bach, Debussy, Chopin, entre otros.

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

Panel curvo en espacio MR/VR con tres zonas (ver sección 4.1):
- **Izquierda**: Configuración de audio y canción en reproducción.
- **Centro**: Galería con cuadrícula paginada y filtros (Todas / En Progreso / Completadas).
- **Derecha**: Detalle de obra — pintura grande, título, artista, museo, descripción y selección de dificultad con récords.

### 11.2. Pantalla de Puzzle

Tres zonas (ver sección 4.3):
- **Centro**: Tablero de armado.
- **Izquierda**: Información de la obra, imagen de referencia, contadores de piezas, canción en reproducción, paredes detectadas (MR) y botón de salida.
- **Derecha**: Bandeja de piezas paginada con botones de navegación.

### 11.3. Panel de Hitos

Mensajes contextuales al completar filas, columnas, bordes y el marco exterior completo.

### 11.4. Panel Post-Juego

Muestra tiempo de resolución y marco ganado al completar el puzzle. Presenta la opción de colgar el cuadro.

### 11.5. Temática Visual de Botones

Sistema de temas unificado: color normal `#896C4A`, hover/seleccionado `#d4c089`.

---

## 12. Modelo de Monetización

### 12.1. Precio Base

Art Unbound se comercializa como una aplicación de pago en la **Meta Quest Store** a **$9.99 USD**. Este precio incluye el pack base completo con todas las obras de dominio público incluidas al momento de la compra y todas las funcionalidades del juego.

### 12.2. Paquetes de Contenido Adicional (DLC)

Las colecciones temáticas se ofrecen como contenido descargable opcional:
- **Precio por pack individual**: $2.99 USD
- **Bundles temáticos**: múltiples packs a precio reducido

Cada pack incluye obras de una colección temática específica que amplían el catálogo más allá del contenido base.

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

La versión actual opera de forma completamente local (offline first). Más adelante se evaluará la implementación de servicios en la nube:

- **Cloud Storage**: descarga de nuevas obras sin actualizar la app completa.
- **Firestore**: respaldo de récords y sincronización entre dispositivos.
- **Remote Config**: eventos globales y ajustes de lógica en tiempo real.
- **Updates trimestrales**: los nuevos cuadros (aprox. 12 por trimestre) actualmente se incluyen en actualizaciones de la app.

---

## 15. Arquitectura Técnica (Resumen)

- **Motor**: Unity 6 LTS (6000.3.1f1)
- **SDK**: Meta XR All-in-One SDK v85.0.0
- **Tracking**: XR Hands (seguimiento de manos nativo), sin necesidad de controladores
- **Anclas espaciales (MR)**: AR Foundation + OpenXR (persistencia cross-session)
- **Persistencia VR**: JSON local via `GalleryPersistenceService`
- **Detección de paredes MR**: ARPlaneManager (planos verticales AR) con fallback por raycast
- **Detección de paredes VR**: Raycasting contra layer `VRWall`
- **Datos locales**: JSON via Newtonsoft.Json en `Application.persistentDataPath`
- **Renderizado**: Universal Render Pipeline (URP), passthrough MR en Quest 3/Pro

---

## Apendice A: Catalogo Completo de Obras

El catalogo contiene **252 obras** en total: 48 en el pack base y 204 distribuidas en 17 packs tematicos (12 obras por pack), organizados en 3 waves.

> Las obras marcadas con * en el Base Set tienen `requiresUnlock: true` en sus assets — estan en la carpeta base pero no disponibles desde el inicio.

---

### Base Set (incluido con la app — $9.99)

| # | Titulo | Artista | Ano |
|---|--------|---------|-----|
| 1 | A Bar at the Folies-Bergere * | Edouard Manet | 1882 |
| 2 | A Sunday on La Grande Jatte | Georges Seurat | 1886 |
| 3 | Bal du moulin de la Galette | Pierre-Auguste Renoir | 1876 |
| 4 | Bridge over a Pond of Water Lilies * | Claude Monet | 1899 |
| 5 | Girl with a Pearl Earring | Johannes Vermeer | 1665 |
| 6 | Hunters in the Snow | Pieter Bruegel the Elder | 1565 |
| 7 | Impression, Sunrise | Claude Monet | 1872 |
| 8 | Lady with an Ermine | Leonardo da Vinci | 1490 |
| 9 | Las Meninas | Diego Velazquez | 1656 |
| 10 | Liberty Leading the People | Eugene Delacroix | 1830 |
| 11 | Luncheon of the Boating Party | Pierre-Auguste Renoir | 1881 |
| 12 | Luncheon on the Grass | Edouard Manet | 1863 |
| 13 | Mona Lisa | Leonardo da Vinci | 1503 |
| 14 | Napoleon Crossing the Alps | Jacques-Louis David | 1801 |
| 15 | Olympia | Edouard Manet | 1863 |
| 16 | Portrait of Henry VIII | Hans Holbein the Younger | 1537 |
| 17 | Primavera | Sandro Botticelli | 1480 |
| 18 | Saturn Devouring His Son | Francisco de Goya | 1823 |
| 19 | Sunflowers (1887) * | Vincent van Gogh | 1887 |
| 20 | The Ambassadors | Hans Holbein the Younger | 1533 |
| 21 | The Anatomy Lesson of Dr. Nicolaes Tulp | Rembrandt van Rijn | 1632 |
| 22 | The Angelus | Jean-Francois Millet | 1859 |
| 23 | The Arnolfini Portrait | Jan van Eyck | 1434 |
| 24 | The Bedroom | Vincent van Gogh | 1888 |
| 25 | The Birth of Venus | Sandro Botticelli | 1485 |
| 26 | The Calling of Saint Matthew | Caravaggio | 1600 |
| 27 | The Creation of Adam | Michelangelo | 1512 |
| 28 | The Death of Marat | Jacques-Louis David | 1793 |
| 29 | The Fighting Temeraire | J.M.W. Turner | 1839 |
| 30 | The Garden of Earthly Delights | Hieronymus Bosch | 1503 |
| 31 | The Gleaners | Jean-Francois Millet | 1857 |
| 32 | The Grand Odalisque | Jean-Auguste-Dominique Ingres | 1814 |
| 33 | The Hay Wain | John Constable | 1821 |
| 34 | The Kiss | Gustav Klimt | 1907 |
| 35 | The Milkmaid | Johannes Vermeer | 1658 |
| 36 | The Night Watch | Rembrandt van Rijn | 1642 |
| 37 | The Raft of the Medusa | Theodore Gericault | 1819 |
| 38 | The Return of the Prodigal Son | Rembrandt van Rijn | 1668 |
| 39 | The School of Athens | Raphael | 1511 |
| 40 | The Starry Night | Vincent van Gogh | 1889 |
| 41 | The Third of May 1808 | Francisco de Goya | 1814 |
| 42 | Tower of Babel * | Pieter Bruegel the Elder | 1563 |
| 43 | Venus of Urbino | Titian | 1538 |
| 44 | View of Delft | Johannes Vermeer | 1660 |
| 45 | Wanderer above the Sea of Fog | Caspar David Friedrich | 1818 |
| 46 | Wheatfield with Crows * | Vincent van Gogh | 1890 |
| 47 | Where Do We Come From? What Are We? Where Are We Going? | Paul Gauguin | 1898 |
| 48 | Whistler's Mother | James McNeill Whistler | 1871 |

---

### Wave 01 — Packs DLC ($2.99 c/u)

#### Birth of the Renaissance

| # | Titulo | Artista |
|---|--------|---------|
| 1 | Adoration of the Magi | Sandro Botticelli |
| 2 | Ginevra de' Benci | Leonardo da Vinci |
| 3 | Lamentation of Christ | Giotto |
| 4 | Lamentation over the Dead Christ | Andrea Mantegna |
| 5 | Madonna of the Goldfinch | Raphael |
| 6 | Pentecost | Giotto |
| 7 | Saint Francis in Ecstasy | Giovanni Bellini |
| 8 | Saint George and the Dragon | Raphael |
| 9 | Small Cowper Madonna | Raphael |
| 10 | The Annunciation | Fra Angelico |
| 11 | The Baptism of Christ | Piero della Francesca |
| 12 | The Descent from the Cross | Rogier van der Weyden |

#### Caravaggio & The Baroque Drama

| # | Titulo | Artista |
|---|--------|---------|
| 1 | David with the Head of Goliath | Caravaggio |
| 2 | Et in Arcadia Ego | Nicolas Poussin |
| 3 | Immaculate Conception of the Venerables | Bartolome Esteban Murillo |
| 4 | Judith Beheading Holofernes | Caravaggio |
| 5 | Judith Slaying Holofernes | Artemisia Gentileschi |
| 6 | Portrait of Juan de Pareja | Diego Velazquez |
| 7 | Saint Francis in Meditation | Francisco de Zurbaran |
| 8 | Supper at Emmaus | Caravaggio |
| 9 | The Crucifixion of Saint Andrew | Caravaggio |
| 10 | The Grand Canal, Venice | Canaletto |
| 11 | The Penitent Magdalene | Georges de La Tour |
| 12 | The Rokeby Venus | Diego Velazquez |

#### Monet: Gardens of Light

| # | Titulo | Artista |
|---|--------|---------|
| 1 | Bouquet of Sunflowers | Claude Monet |
| 2 | Flower Beds at Vetheuil | Claude Monet |
| 3 | Poplars (Three Pink Autumn Trees) | Claude Monet |
| 4 | Rounded Flower Bed | Claude Monet |
| 5 | Snow at Argenteuil | Claude Monet |
| 6 | The Argenteuil Bridge | Claude Monet |
| 7 | The Artist's Garden at Vetheuil | Claude Monet |
| 8 | The Beach at Sainte-Adresse | Claude Monet |
| 9 | The Houses of Parliament, Sunset | Claude Monet |
| 10 | The Japanese Footbridge | Claude Monet |
| 11 | View of Vetheuil | Claude Monet |
| 12 | Water Lilies (Agapanthus) | Claude Monet |

#### Muses & Visions: Beauty at the Fin de Siecle

| # | Titulo | Artista |
|---|--------|---------|
| 1 | Beata Beatrix | Dante Gabriel Rossetti |
| 2 | Flaming June | Frederic Leighton |
| 3 | La Mousme | Vincent van Gogh |
| 4 | Lady Agnew of Lochnaw | John Singer Sargent |
| 5 | Mada Primavesi | Gustav Klimt |
| 6 | Madame X | John Singer Sargent |
| 7 | Ophelia | John Everett Millais |
| 8 | Orpheus | Odilon Redon |
| 9 | The Birth of Venus | William-Adolphe Bouguereau |
| 10 | The Lady of Shalott | John William Waterhouse |
| 11 | Vase of Flowers (Pink Background) | Odilon Redon |
| 12 | Young Woman with Peonies | Frederic Bazille |

#### Van Gogh: Light & Color in Provence

| # | Titulo | Artista |
|---|--------|---------|
| 1 | Almond Blossom | Vincent van Gogh |
| 2 | Cafe Terrace at Night | Vincent van Gogh |
| 3 | Field with Irises near Arles | Vincent van Gogh |
| 4 | Flowering Plum Orchard | Vincent van Gogh |
| 5 | Irises | Vincent van Gogh |
| 6 | Roses | Vincent van Gogh |
| 7 | Starry Night Over the Rhone | Vincent van Gogh |
| 8 | Sunflowers (Blue background) | Vincent van Gogh |
| 9 | The Garden of the Asylum at Saint-Remy | Vincent van Gogh |
| 10 | The Langlois Bridge at Arles | Vincent van Gogh |
| 11 | The Night Cafe | Vincent van Gogh |
| 12 | Wheatfield with Cypresses | Vincent van Gogh |

#### Vermeer & The Dutch Interior

| # | Titulo | Artista |
|---|--------|---------|
| 1 | A Dutch Courtyard | Pieter de Hooch |
| 2 | A Lady Writing | Johannes Vermeer |
| 3 | Girl Reading a Letter at an Open Window | Johannes Vermeer |
| 4 | Glass of Lemonade | Gerard ter Borch |
| 5 | Portrait of a Girl in Blue | Johannes C. Verspronck |
| 6 | Serenade | Judith Leyster |
| 7 | The Lacemaker | Johannes Vermeer |
| 8 | The Little Street | Johannes Vermeer |
| 9 | View of Houses in Delft | Johannes Vermeer |
| 10 | Woman Holding a Balance | Johannes Vermeer |
| 11 | Woman with a Pearl Necklace | Johannes Vermeer |
| 12 | Young Woman with a Water Pitcher | Johannes Vermeer |

---

### Wave 02 — Packs DLC ($2.99 c/u)

#### American Frontier: Wilderness & Grit

| # | Titulo | Artista |
|---|--------|---------|
| 1 | Among the Sierra Nevada | Albert Bierstadt |
| 2 | Breezing Up (A Fair Wind) | Winslow Homer |
| 3 | Kindred Spirits | Asher B. Durand |
| 4 | Max Schmitt in a Single Scull | Thomas Eakins |
| 5 | Mount Corcoran | Albert Bierstadt |
| 6 | Niagara Falls | Frederic Edwin Church |
| 7 | Stag at Sharkey's | George Bellows |
| 8 | The Gross Clinic | Thomas Eakins |
| 9 | The Icebergs | Frederic Edwin Church |
| 10 | The Oxbow | Thomas Cole |
| 11 | Washington Crossing the Delaware | Emanuel Leutze |
| 12 | Watson and the Shark | John Singleton Copley |

#### High Renaissance & Mannerism

| # | Titulo | Artista |
|---|--------|---------|
| 1 | Allegory with Venus and Cupid | Agnolo Bronzino |
| 2 | Bacchus and Ariadne | Titian |
| 3 | Madonna with the Long Neck | Parmigianino |
| 4 | Saint Jerome as Scholar | El Greco |
| 5 | Sistine Madonna | Raphael |
| 6 | Susanna and the Elders | Jacopo Tintoretto |
| 7 | The Last Supper | Giampietrino |
| 8 | The Transfiguration | Raphael |
| 9 | The Virgin of the Rocks | Leonardo da Vinci |
| 10 | The Wedding at Cana | Paolo Veronese |
| 11 | View of Toledo | El Greco |
| 12 | Virgin and Child with St. Anne | Leonardo da Vinci |

#### Post-Impressionist Dreams

| # | Titulo | Artista |
|---|--------|---------|
| 1 | At the Moulin Rouge | Henri de Toulouse-Lautrec |
| 2 | Bathers at Asnieres (Study) | Georges Seurat |
| 3 | Card Players | Paul Cezanne |
| 4 | Day of the God (Mahana No Atua) | Paul Gauguin |
| 5 | Mont Sainte-Victoire | Paul Cezanne |
| 6 | The Brook | Paul Cezanne |
| 7 | The Dream | Henri Rousseau |
| 8 | The Large Bathers | Paul Cezanne |
| 9 | The Seine at La Grande Jatte | Georges Seurat |
| 10 | The Sleeping Gypsy | Henri Rousseau |
| 11 | Vision After the Sermon | Paul Gauguin |
| 12 | Women of Tahiti | Paul Gauguin |

#### Rembrandt & The Dutch Masters

| # | Titulo | Artista |
|---|--------|---------|
| 1 | Aristotle with a Bust of Homer | Rembrandt van Rijn |
| 2 | Christ in the Storm | Rembrandt van Rijn |
| 3 | Descent from the Cross | Rembrandt van Rijn |
| 4 | Isaac and Rebecca (The Jewish Bride) | Rembrandt van Rijn |
| 5 | Self-Portrait (1659) | Rembrandt van Rijn |
| 6 | Self-Portrait with Two Circles | Rembrandt van Rijn |
| 7 | The Banquet of the Guard | Bartholomeus van der Helst |
| 8 | The Goldfinch | Carel Fabritius |
| 9 | The Merry Family | Jan Steen |
| 10 | The Threatened Swan | Jan Asselijn |
| 11 | The Windmill | Rembrandt van Rijn |
| 12 | Winter Landscape with Skaters | Hendrick Avercamp |

#### Renoir & Degas: Parisian Afternoons

| # | Titulo | Artista |
|---|--------|---------|
| 1 | Before the Ballet | Edgar Degas |
| 2 | Dance at Bougival | Pierre-Auguste Renoir |
| 3 | Dancers Practicing at the Barre | Edgar Degas |
| 4 | Girl with a Hoop | Pierre-Auguste Renoir |
| 5 | Girl with a Watering Can | Pierre-Auguste Renoir |
| 6 | In the Meadow | Pierre-Auguste Renoir |
| 7 | Madame Georges Charpentier | Pierre-Auguste Renoir |
| 8 | Piazza San Marco, Venice | Pierre-Auguste Renoir |
| 9 | The Dance Class | Edgar Degas |
| 10 | The Skiff (La Yole) | Pierre-Auguste Renoir |
| 11 | The Woman with a Fan | Pierre-Auguste Renoir |
| 12 | Two Sisters (On the Terrace) | Pierre-Auguste Renoir |

#### Van Gogh: Mirrors of the Soul

| # | Titulo | Artista |
|---|--------|---------|
| 1 | Adeline Ravoux | Vincent van Gogh |
| 2 | Gauguin's Chair | Vincent van Gogh |
| 3 | Portrait of Dr. Gachet (Etching) | Vincent van Gogh |
| 4 | Self-Portrait (1887) | Vincent van Gogh |
| 5 | Self-Portrait (1889) | Vincent van Gogh |
| 6 | Self-Portrait as a Painter | Vincent van Gogh |
| 7 | Self-Portrait with a Straw Hat | Vincent van Gogh |
| 8 | Self-Portrait with Bandaged Ear | Vincent van Gogh |
| 9 | Self-Portrait with Grey Felt Hat | Vincent van Gogh |
| 10 | Shoes | Vincent van Gogh |
| 11 | The Potato Eaters | Vincent van Gogh |
| 12 | The Yellow House | Vincent van Gogh |

---

### Wave 03 — Packs DLC ($2.99 c/u)

#### Fields of Labor: Van Gogh's Peasants & Realism

| # | Titulo | Artista |
|---|--------|---------|
| 1 | A Burial at Ornans | Gustave Courbet |
| 2 | First Steps (After Millet) | Vincent van Gogh |
| 3 | Fur Traders on the Missouri | George Caleb Bingham |
| 4 | Landscape from Saint-Remy | Vincent van Gogh |
| 5 | Snap the Whip | Winslow Homer |
| 6 | The Fifer | Edouard Manet |
| 7 | The Harvest | Vincent van Gogh |
| 8 | The Horse Fair | Rosa Bonheur |
| 9 | The Sower | Vincent van Gogh |
| 10 | The Third-Class Carriage | Honore Daumier |
| 11 | The Veteran in a New Field | Winslow Homer |
| 12 | Walk on the Seashore | Joaquin Sorolla |

#### Grand Masters of Europe: Durer to Turner

| # | Titulo | Artista |
|---|--------|---------|
| 1 | Equestrian Portrait of Charles I | Anthony van Dyck |
| 2 | Rain, Steam and Speed | J.M.W. Turner |
| 3 | Self-Portrait (1500) | Albrecht Durer |
| 4 | Still Life with Musical Instruments | Evaristo Baschenis |
| 5 | The Burial of the Count of Orgaz | El Greco |
| 6 | The Descent from the Cross | Peter Paul Rubens |
| 7 | The Garden of Love | Peter Paul Rubens |
| 8 | The Silver Tureen | Jean-Baptiste Chardin |
| 9 | The Three Graces | Peter Paul Rubens |
| 10 | The Triumph of Death | Pieter Bruegel the Elder |
| 11 | The Windmill at Wijk bij Duurstede | Jacob van Ruisdael |
| 12 | Young Hare | Albrecht Durer |

#### Reason & Revolution: Rococo to Romanticism

| # | Titulo | Artista |
|---|--------|---------|
| 1 | A Young Girl Reading | Jean-Honore Fragonard |
| 2 | Declaration of Independence | John Trumbull |
| 3 | Lake Albano and Castel Gandolfo | Jean-Baptiste-Camille Corot |
| 4 | Oath of the Horatii | Jacques-Louis David |
| 5 | Portrait of the Infante Luis de Borbon | Francisco de Goya |
| 6 | The Blue Boy | Thomas Gainsborough |
| 7 | The Burning of the Houses of Lords and Commons | J.M.W. Turner |
| 8 | The Clothed Maja | Francisco de Goya |
| 9 | The Death of Socrates | Jacques-Louis David |
| 10 | The Kiss | Francesco Hayez |
| 11 | The Nude Maja | Francisco de Goya |
| 12 | The Swing | Jean-Honore Fragonard |

#### The Art of Stillness: Still Lifes Across the Ages

| # | Titulo | Artista |
|---|--------|---------|
| 1 | Basket of Peaches | Jean-Baptiste Chardin |
| 2 | Flowers in a Terracotta Vase | Jan van Huysum |
| 3 | Still Life with a Glass and Oysters | Jan Davidsz de Heem |
| 4 | Still Life with a Silver Jug and a Porcelain Bowl | Willem Kalf |
| 5 | Still Life with Apples and a Pot of Primroses | Paul Cezanne |
| 6 | Still Life with Apples and Pears | Paul Cezanne |
| 7 | Still Life with Flowers on a Marble Tabletop | Rachel Ruysch |
| 8 | Still Life with Gilt Cup | Willem Claesz Heda |
| 9 | Still Life with Ham | Willem Claesz Heda |
| 10 | Still Life with Oysters, a Silver Tazza, and Glassware | Willem Claesz Heda |
| 11 | Still Life with Quinces | Vincent van Gogh |
| 12 | The Basket of Apples | Paul Cezanne |

#### The Impressionist Family: Morisot, Cassatt & Manet's Circle

| # | Titulo | Artista |
|---|--------|---------|
| 1 | Portrait of Madame Brunet | Edouard Manet |
| 2 | Reading (La Lecture) | Berthe Morisot |
| 3 | Reclining Young Woman in Spanish Costume | Edouard Manet |
| 4 | Self-Portrait | Edgar Degas |
| 5 | The Absinthe Drinker | Edgar Degas |
| 6 | The Banks of the Oise | Alfred Sisley |
| 7 | The Child's Bath | Mary Cassatt |
| 8 | The Grand Canal, Venice | Edouard Manet |
| 9 | The Monet Family in Their Garden | Edouard Manet |
| 10 | The Valley of the Seine, from the Hills of Giverny | Theodore Robinson |
| 11 | Village of Eragny | Camille Pissarro |
| 12 | Woman with a Parasol | Claude Monet |
