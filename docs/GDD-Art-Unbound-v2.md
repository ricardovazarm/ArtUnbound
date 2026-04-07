# Documento de Diseño de Juego (GDD): Art Unbound
- Versión: 2.0 (Estado Final de Implementación)
- Fecha: Abril 2026
- Plataforma: Meta Quest 3 y Meta Quest Pro

---

## 1. Concepto de Alto Nivel

Art Unbound es un juego de puzzles en Realidad Mixta (RM) para Meta Quest. Los jugadores reconstruyen obras maestras clásicas ensamblando piezas en un lienzo flotante frente a ellos, y al terminar pueden colgar el cuadro completado en una pared real de su entorno físico, donde persiste entre sesiones.

---

## 2. Resumen del Juego

- **Género**: Puzzle, Realidad Mixta, Casual.
- **Propuesta de valor única**: El espacio real del usuario es parte del juego. Los cuadros completados se convierten en decoración persistente del hogar, colgados en paredes reales mediante anclajes espaciales. Las piezas tienen grosor tridimensional (0.5 cm) que les otorga presencia física.

---

## 3. Pilares de Diseño

- **Inmersión tangible**: Las piezas tienen volumen de 0.5 cm y se ensamblan en un lienzo flotante ergonómico.
- **Magia cotidiana**: Las obras completadas decoran permanentemente el espacio físico del usuario.
- **Flexibilidad ergonómica**: El armado ocurre siempre en un lienzo flotante a distancia fija (0.4 m del usuario), permitiendo jugar sentado o de pie en cualquier lugar. La integración con la pared es solo una recompensa post-juego.

---

## 4. Flujo del Juego

### 4.1. Menú Principal

El menú principal es un panel curvo en espacio MR, dividido en tres zonas:

**Panel izquierdo — Configuración**
- Control de volumen de música.
- Control de volumen de efectos de sonido.
- Muestra el nombre de la canción y artista en reproducción actual.

**Panel central — Galería**
- Cuadrícula 3×3 de obras con paginación.
- Muestra miniatura de cada obra con su marco ganado (si aplica) y porcentaje de avance (si está en progreso).
- Filtros: Todas / En Progreso / Completadas.

**Panel derecho — Detalle de Obra**
- Se activa al seleccionar cualquier obra de la galería.
- Muestra la pintura en tamaño grande, título, artista, museo de origen y descripción.
- Ofrece tres botones de dificultad para iniciar el puzzle, con el tiempo récord alcanzado en cada uno.

### 4.2. Dificultad y Marcos

| Dificultad | Piezas | Marco ganado |
|------------|--------|--------------|
| Fácil      | 64     | Bronce       |
| Normal     | 144    | Plata        |
| Difícil    | 256    | Oro          |

El marco se asigna exclusivamente por la dificultad seleccionada. No existen penalizaciones ni requisitos adicionales para obtener un marco.

### 4.3. Pantalla de Armado

Al iniciar el puzzle, el espacio se divide en tres zonas:

**Centro — Tablero del Puzzle**
- El lienzo flotante a 0.4 m del usuario donde se arman las piezas.
- Ocupa el espacio visual principal.

**Izquierda — Panel de Información**
- Título y artista de la obra.
- Nombre de la canción y artista en reproducción.
- Contadores en tiempo real:
  - Total de piezas.
  - Piezas colocadas correctamente.
  - Piezas colocadas incorrectamente.
- Imagen de referencia completa de la obra.
- Número de paredes detectadas en el entorno.
- Botón de salida al menú principal.

**Derecha — Bandeja de Piezas**
- Piezas disponibles para colocar, organizadas en páginas.
- Botones de navegación para avanzar y retroceder entre páginas.

### 4.4. Mecánica de Colocación de Piezas

1. El usuario toma una pieza con gesto de pinch (pulgar-índice).
2. La pieza sigue la mano del usuario mientras está siendo sostenida.
3. Al soltar cerca del tablero, la pieza hace snap automático al slot más cercano disponible.
4. Si la pieza corresponde al slot correcto, el contador de correctas aumenta; si no, aumenta el de incorrectas.
5. Una pieza colocada puede ser tomada nuevamente y reubicada.
6. Al completar el puzzle (todas las piezas en su slot correcto), se activa la animación de revelación.

> **Nota**: No existe una distancia mínima de snap — cualquier pieza soltada cerca del tablero se coloca en el slot disponible más cercano. No hay rechazo por morfología; el sistema registra si el resultado es correcto o no mediante los contadores.

### 4.5. Hitos durante el Armado

El sistema detecta y celebra:
- Completar una fila completa.
- Completar una columna completa.
- Completar un borde (lado exterior del puzzle).
- Completar el marco exterior completo.

Cada hito dispara un mensaje contextual en pantalla y efectos de partículas.

### 4.6. Finalización y Post-Juego

1. Al colocar todas las piezas correctamente se activa la animación de revelación: el cuadro completo aparece con el marco de la dificultad elegida.
2. Se muestra el panel de post-juego con el tiempo de resolución y el marco ganado.
3. El cuadro completado queda disponible para colgar en una pared.

---

## 5. Sistema de Colgado (Mixed Reality)

### 5.1. Flujo de Colocación

1. Desde el post-juego, el usuario toma el cuadro completado con un gesto de pinch.
2. Toda la UI se oculta durante el arrastre.
3. El cuadro sigue la mano y rota dinámicamente para alinearse con la pared más cercana.
4. Al soltar el cuadro **a 20 cm o menos de una pared**, el cuadro se fija en esa posición y se crea un anclaje espacial persistente.
5. Al soltar **alejado de una pared**, el cuadro desaparece y la UI se restaura. El cuadro sigue disponible para intentar colgarlo de nuevo.

### 5.2. Reposicionamiento y Retiro

- Los cuadros ya colgados en paredes pueden tomarse de nuevo con pinch.
- Al soltar cerca de una pared (≤ 20 cm): el cuadro se reposiciona y el anclaje espacial se actualiza.
- Al soltar lejos de una pared: el cuadro se retira permanentemente de la pared y desaparece.

### 5.3. Persistencia

- Los cuadros colgados persisten entre sesiones mediante Meta Spatial Anchors (AR Foundation).
- Al iniciar la app, todos los cuadros colgados anteriormente se reconstituyen en sus posiciones reales.

---

## 6. Morfología de las Piezas

### 6.1. Estados de Arista

Cada lado de una pieza puede tener uno de tres estados:
- **Plano**: exclusivo para bordes exteriores del puzzle.
- **Positivo**: pestaña triangular hacia afuera.
- **Negativo**: ranura triangular hacia adentro.

Piezas vecinas siempre tienen aristas complementarias (Positivo ↔ Negativo) en su borde compartido, garantizando que morfológicamente encajen.

### 6.2. Variedad

Las piezas internas (sin bordes planos) generan su morfología a partir de aristas compartidas asignadas aleatoriamente con una semilla determinista basada en el ID de la obra. Esto produce hasta 16 combinaciones distintas por pieza interna (2⁴), dando a cada obra un patrón único y reproducible entre sesiones.

### 6.3. Colocación y Validación

El sistema no rechaza piezas por morfología. Cualquier pieza soltada cerca del tablero se coloca automáticamente en el slot disponible más cercano. La corrección de la colocación se registra mediante los contadores del panel de información:
- **Correctas**: piezas en su slot correspondiente por ID.
- **Incorrectas**: piezas colocadas en un slot que no les corresponde.

El puzzle se considera completo cuando todas las piezas están en sus slots correctos.

---

## 7. Contenido

### 7.1. Catálogo de Obras

Las obras del catálogo son pinturas de museos reconocidos a nivel mundial, creadas por artistas clásicos de renombre como Van Gogh, Rembrandt, Monet, Vermeer, Cézanne, Renoir, entre otros.

- Actualmente el catálogo utiliza exclusivamente **obras de dominio público**, lo que permite su uso sin restricciones legales.
- En el futuro se evaluará la posibilidad de incluir obras protegidas por derechos de autor mediante el pago de regalías a museos o herederos.
- Cada obra tiene: ID único, título, artista, museo de origen, descripción, thumbnail y textura de alta resolución (hasta 4096×4096).
- El catálogo se amplía mediante actualizaciones de la aplicación.

### 7.2. Cuadro Semanal *(funcionalidad futura)*

Se planea un sistema de desbloqueo semanal basado en la fecha del dispositivo, donde una obra especial estará disponible por tiempo limitado cada semana. La infraestructura base (`WeeklyUnlockService`) ya existe en el proyecto.

---

## 8. Progresión y Guardado

- Se registra el **tiempo de resolución** por obra y por dificultad (récord histórico).
- No hay sistema de puntuación numérica ni penalizaciones.
- El progreso en puzzles incompletos se guarda automáticamente (al pausar o salir).
- Los datos se guardan localmente en el dispositivo en formato JSON.
- El guardado se realiza automáticamente en `OnApplicationPause` y `OnApplicationQuit`.

---

## 9. Audio

### 9.1. Música

La música del juego es **música clásica** de compositores reconocidos como Mozart, Beethoven, Bach, Debussy, Chopin, entre otros.

- Actualmente se utilizan exclusivamente **grabaciones de dominio público**, lo que permite su uso sin restricciones legales.
- En el futuro se evaluará la incorporación de grabaciones orquestales modernas de mayor calidad, sujeto a licenciamiento.
- La biblioteca de pistas es configurable (`MusicLibrary`) y se amplía con actualizaciones.
- El panel izquierdo del menú principal y el panel de información en el puzzle muestran el título de la pieza y el compositor en reproducción.

### 9.2. Efectos de Sonido

- Efectos de confirmación en snap correcto y en hitos.
- Volumen de música y efectos de sonido configurables independientemente desde el panel de configuración del menú principal.

---

## 10. Interfaz y UX

### 10.1. Menú Principal

Panel curvo en espacio MR con tres zonas (ver sección 4.1):
- **Izquierda**: Configuración de audio y canción en reproducción.
- **Centro**: Galería con cuadrícula 3×3, paginación y filtros (Todas / En Progreso / Completadas).
- **Derecha**: Detalle de obra seleccionada — pintura grande, título, artista, museo, descripción y selección de dificultad con récords.

### 10.2. Pantalla de Puzzle

Tres zonas (ver sección 4.3):
- **Centro**: Tablero de armado.
- **Izquierda**: Información de la obra, contadores de piezas, canción en reproducción, paredes detectadas y botón de salida.
- **Derecha**: Bandeja de piezas paginada con botones de navegación.

### 10.3. Panel de Hitos

Mensajes contextuales al completar filas, columnas, bordes y el marco exterior completo.

### 10.4. Panel Post-Juego

Muestra tiempo de resolución y marco ganado al completar el puzzle.

### 10.5. Temática Visual de Botones

Sistema de temas unificado: color normal `#896C4A`, hover/seleccionado `#d4c089`.

---

## 11. Beneficios Cognitivos y de Bienestar

Art Unbound no es únicamente entretenimiento — combina deliberadamente tres actividades con beneficios documentados para la salud mental y cognitiva.

### 11.1. Rompecabezas y Función Cognitiva

Armar rompecabezas ejercita simultáneamente múltiples áreas del cerebro:

- **Memoria visual y espacial**: reconocer formas, colores y patrones y recordar dónde encajan.
- **Pensamiento lógico y resolución de problemas**: evaluar qué pieza corresponde a cada espacio.
- **Concentración y atención**: mantener el foco durante períodos sostenidos.
- **Coordinación visuomotora**: en Art Unbound esto se potencia gracias al seguimiento de manos en espacio real.

Estudios sugieren que este tipo de actividad contribuye a mantener la agilidad mental y puede ayudar a retardar el deterioro cognitivo asociado al envejecimiento.

### 11.2. Música Clásica y el Cerebro

La música clásica — repertorio central de Art Unbound — tiene efectos bien documentados:

- **Reducción del estrés y la ansiedad**: escuchar música clásica activa el sistema parasimpático, disminuyendo el cortisol.
- **Mejora de la concentración**: el conocido "efecto Mozart" sugiere que ciertos tipos de música mejoran temporalmente el rendimiento en tareas cognitivas.
- **Estado de flujo**: la música instrumental sin letra reduce distracciones y facilita el estado de concentración profunda (flow), ideal para el armado de puzzles.
- **Bienestar emocional**: la exposición regular a música clásica se asocia con mejoras en el estado de ánimo y reducción de la fatiga mental.

### 11.3. Arte y Contemplación

La exposición a obras de arte clásico tiene beneficios propios:

- **Estimulación estética**: apreciar una obra maestra activa regiones del cerebro asociadas al placer y la recompensa.
- **Conocimiento cultural**: cada puzzle es una oportunidad de conocer la obra, su autor, su historia y el museo donde se alberga.
- **Sentido de logro**: completar una obra y verla colgada en el propio espacio genera una satisfacción duradera que va más allá del juego.

### 11.4. La Experiencia Combinada

Art Unbound integra estas tres dimensiones en una sola sesión de juego: el usuario arma un puzzle con las manos en un espacio relajante, acompañado de música clásica, mientras reconstruye una obra maestra. Esta combinación crea una experiencia que es al mismo tiempo **estimulante cognitivamente, relajante emocionalmente y culturalmente enriquecedora** — accesible a cualquier edad y nivel de habilidad.

---

## 12. Evaluación de Escalabilidad Futura

La versión actual opera de forma completamente local (offline first). Más adelante se evaluará la implementación de servicios en la nube para las siguientes funciones:

- **Cloud Storage**: Descarga de nuevas obras sin actualizar la app completa.
- **Firestore**: Respaldo de récords y sincronización entre dispositivos (cross-device).
- **Remote Config**: Eventos globales y ajustes de lógica en tiempo real.
- **Updates trimestrales**: Los nuevos cuadros (aprox. 12 por trimestre) actualmente se incluyen en actualizaciones de la app; con Cloud Storage esto podría hacerse sin update.

---

## 13. Arquitectura Técnica (Resumen)

- **Motor**: Unity 6 LTS (6000.3.1f1)
- **SDK**: Meta XR All-in-One SDK v85.0.0
- **Tracking**: XR Hands (seguimiento de manos nativo), sin necesidad de controladores
- **Anclas espaciales**: AR Foundation + OpenXR (persistencia cross-session)
- **Detección de paredes**: ARPlaneManager (planos verticales AR) con fallback por raycast
- **Datos locales**: JSON via Newtonsoft.Json en `Application.persistentDataPath`
- **Renderizado**: Universal Render Pipeline (URP), passthrough MR
