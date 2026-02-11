# Documento de Diseño de Juego (GDD): Art Unbound
- Versión: 4.2 (Enfoque en Meta Quest 3/Pro y Arquitectura Local)
- Fecha: 20 de enero de 2026
- Autor(es): [Tu Nombre] y Gemini AI
- Plataforma: Meta Quest 3 y Pro (Optimizado para Quest 3)

## 1. Concepto de Alto Nivel
Art Unbound es un juego de puzzles en Realidad Mixta (RM) para Meta Quest. Los jugadores transforman las paredes de su hogar en lienzos vivos donde reconstruyen obras maestras. Diseñado exclusivamente para seguimiento de manos, ofrece una experiencia táctil donde piezas con volumen real se colocan sobre las superficies del mundo físico, permitiendo una decoración persistente del entorno.

## 2. Resumen del Juego
- Género: Puzzle, Realidad Mixta (RM), Casual.
- Propuesta de valor única: El uso del espacio real como tablero y la fisicidad tridimensional de sus piezas. A diferencia de otros puzzles virtuales, las piezas tienen grosor (0.5 cm) y peso visual. El juego permite decorar permanentemente la casa con los logros del usuario, fusionando la jugabilidad con la personalización del hogar.

## 3. Pilares de Diseño
- Inmersión tangible: El mundo real es el soporte. Las piezas tienen un volumen de 0.5 cm y responden a las leyes físicas de contacto con las superficies.
- Magia cotidiana: Transformar paredes vacías en galerías personales llenas de arte clásico.
- Flexibilidad ergonómica: El armado se realiza siempre en un lienzo flotante ergonómico, permitiendo jugar cómodamente sentado o de pie. La integración con la pared ocurre solo al final como recompensa decorativa.

## 4. Mecánicas Principales de Juego
### 4.1. Flujo Inicial de UX (El Inicio)
El juego prioriza la inmediatez y la ergonomía, eliminando la fricción de escanear paredes al inicio:

- **Inicio Inmediato**: Al seleccionar una obra, el lienzo aparece automáticamente **flotando frente al usuario** a una distancia ergonómica.
- **Ajuste de Posición**: El usuario tiene un breve momento para confirmar o ajustar la posición del lienzo flotante antes de comenzar.
- **Armado**: Todo el proceso de armado sucede en este lienzo flotante, permitiendo jugar sentado o de pie en cualquier lugar.
- **Colgado (Post-Juego)**: La selección de pared se reserva exclusivamente para el momento de "Colgar" la obra una vez terminada.

### 4.2. Fisicidad e Interacción
- Profundidad 3D de piezas: Grosor real de 0.5 cm. Esto otorga presencia física para que el jugador sienta que manipula objetos sólidos.
- Tamaño de pieza: 5 cm x 5 cm (ancho x alto).
- Morfología de Muescas Triangulares: En lugar de curvas tradicionales, las piezas utilizan un sistema de pestañas y ranuras triangulares para la guía morfológica.
- Lógica Combinatoria (81 variantes): Cada uno de los 4 lados de una pieza puede tener uno de tres estados: Plano (para bordes del cuadro), Positivo (triángulo hacia afuera) o Negativo (triángulo hacia adentro). Esto permite una variedad de hasta 3^4 = 81 tipos de piezas únicas, garantizando que cada posición en el puzzle sea lógicamente singular.
- Colocación Directa: Las piezas se apoyan físicamente contra la pared o plano. El jugador las toma en pinza y las coloca; la pieza mantiene su volumen de 0.5 cm proyectado hacia afuera.
- Sistema de Snap Magnético (Grid Snapping): El lienzo posee una rejilla invisible. Al soltar una pieza cerca de una celda, esta se alinea automáticamente al centro de la coordenada. No existen colisiones físicas para evitar inestabilidad en el seguimiento de manos.
- Modos de Asistencia (Configurables):
  - Modo Ayuda ON: Al soltar a 3 cm o menos del lugar correcto, la pieza hace snap magnético y dispara un haptic, un destello visual y un sonido de confirmación.
  - Modo Ayuda OFF (Manual): No hay feedback ni atracción magnética. El usuario debe colocar la pieza por pura observación. El puzzle solo se valida al estar 100% correcto.
- Seguimiento de Manos:
  - Pinch (Pellizco): Alcance de 1 cm para tomar piezas.
  - Scroll Horizontal: Gesto lateral para navegar por el carrusel de piezas bajo el lienzo.

### 4.3. Retroalimentación (Feedback)
- Simulación háptica: Respuesta visual y sonora suave al contacto con la superficie.
- Visual: Sombras dinámicas proyectadas por las piezas (0.5 cm) sobre el lienzo, reforzando la integración con la iluminación real.
- Validación de Encuentro: El sistema verifica si los triángulos de la pieza recién colocada coinciden con las piezas adyacentes.
- Alerta de Error: Si la pieza se coloca en una celda donde sus triángulos no embonan con los vecinos, la pieza emitirá un brillo rojo sutil (en Modo Ayuda) indicando el error de colocación.
- Feedback de Éxito: Solo en Modo Ayuda y únicamente si la pieza está en su lugar correcto; entonces se emite un destello blanco o el haptic de confirmación definido.

## 5. Contenido y Progresión
### 5.1. Puntuación y Recompensas Visuales
- Variables: Se registra el número de piezas (64 a 512) y el tiempo de resolución.
- Factor de Ayuda: Completar con ayuda penaliza la puntuación. El Marco de Oro y Ébano es exclusivo para resoluciones en modo manual y máxima dificultad.
- Marcos Evolucionables: Madera (Básico), Bronce/Plata (Intermedio), Oro y Ébano (Maestría).
- Persistencia: Los cuadros terminados pueden ser reubicados en cualquier lugar de la casa mediante Spatial Anchors.

### 5.2. Galería Personal
Inventario local donde se guardan las obras completadas, récords históricos y cuadros retirados de las paredes para su posterior reuso.

## 6. Arquitectura Técnica y Gestión de Datos
### 6.1. Almacenamiento Local (Offline First)
Para reducir la complejidad inicial, el juego operará de forma local:

- Updates Trimestrales: Los nuevos cuadros (aprox. 12 por trimestre) se incluyen en actualizaciones de la app.
- Reloj del Sistema: El "Cuadro Semanal" se desbloquea basado en la fecha del dispositivo.
- Local Save: Los récords y posiciones de cuadros se guardan en el almacenamiento interno del visor.

### 6.2. Evaluación de Escalabilidad Futura
Más adelante se evaluará si es necesario implementar el uso de servicios en la nube (como Firebase) para las siguientes funciones:

- Cloud Storage: Para descarga de cuadros individuales sin actualizar la app completa.
- Firestore: Para respaldo de récords y persistencia entre dispositivos (Cross-device).
- Remote Config: Para eventos globales y ajustes de lógica en tiempo real.

## 7. Flujo Post-Juego
- Validación: Chequeo de piezas colocadas correctamente.
- Animación de Recompensa: El cuadro se "viste" con el marco ganado.
- Colocación: El cuadro se vuelve movible para que el usuario lo cuelgue en su lugar preferido de la casa.
- Guardado: Persistencia de la posición física mediante anclajes espaciales.