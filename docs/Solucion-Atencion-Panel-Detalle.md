# Solución: Guía de Atención al Panel de Detalle — Art Unbound

> **Problema identificado con usuarios reales.**  
> El usuario selecciona una pintura en el panel central y no descubre que el panel derecho se ha cargado con los detalles.

---

## Problemática

### Descripción del flujo actual

El menú principal tiene dos paneles en world space:

| Panel | Contenido | Posición |
|-------|-----------|----------|
| **Central** | Galería de pinturas (grid 3×3, filtros, paginación) | Frente al usuario |
| **Derecho** | Detalle de la obra seleccionada, botones de dificultad | 45° a la derecha, rotado 35° hacia el usuario |

Al seleccionar una pintura en el panel central, los detalles se cargan silenciosamente en el panel derecho. El único feedback actual es un sonido.

### Raíz del problema

**Capa 1 — Descubrimiento:** El usuario no sabe que existe un panel derecho. La primera vez que entra al menú, el panel derecho está vacío o no visible. No hay ninguna señal que le indique que seleccionar una pintura genera contenido en otro lugar del espacio.

**Capa 2 — Proximidad:** Algunos usuarios se acercan al panel central para ver las pinturas en detalle antes de seleccionar. A distancias cortas (~30–40 cm), la separación angular efectiva entre los paneles aumenta considerablemente — lo que desde lejos son 45° desde cerca puede sentirse como 70–80°. El panel derecho queda fuera del campo de visión cómodo sin mover la cabeza.

**Resultado:** El usuario selecciona una pintura, escucha el sonido, y al no ver nada cambia frente a él, concluye que "no pasó nada".

---

## Solución: Rastro de Partículas como Attentional Cue

### Concepto

Cuando el usuario selecciona una pintura, se dispara un efecto de partículas que **nace en la tarjeta seleccionada** y **viaja por el espacio 3D hasta el panel derecho**, terminando con un pequeño destello de llegada.

Este patrón se llama *attentional cue* — en lugar de mover el contenido hacia el usuario, se mueve la *mirada* del usuario hacia el contenido. Funciona porque el movimiento periférico activa el reflejo visual de seguir objetos en movimiento de forma involuntaria. El usuario lo sigue con la vista sin necesidad de instrucciones.

### Por qué es la solución correcta para este contexto

- Ocurre exactamente donde el usuario ya está mirando (la tarjeta tocada).
- No interrumpe el flujo ni desplaza nada del layout.
- Es transitorio — desaparece solo sin dejar ruido visual.
- Funciona tanto para usuarios nuevos (descubrimiento) como para usuarios cercanos al panel (proximidad).
- Escala bien: si el usuario ya conoce el layout, el efecto se vuelve una confirmación agradable, no una distracción.

---

## Especificación del Efecto

### Origen y destino

- **Origen:** Posición world space del centro de la tarjeta de pintura seleccionada en el panel central.
- **Destino:** Posición world space del centro del panel derecho (o de la imagen de la obra dentro del panel).
- Las partículas deben existir en **world space**, no como hijos de ninguno de los dos paneles. Esto es crítico para que el vuelo 3D entre paneles se vea correcto en MR con passthrough.

### Trayectoria

- Arco suave entre origen y destino — no línea recta. Una curva Bézier con el punto de control ligeramente elevado da sensación de "vuelo".
- Duración total del recorrido: **0.4–0.6 segundos**. Más lento y el usuario pierde el hilo; más rápido y no da tiempo a seguirlo.

### Apariencia de las partículas

- **Color:** Dorado/blanco cálido (`#FFD98A` o similar). Debe contrastar con fondos claros y oscuros del passthrough. Evitar colores fríos o grises que se mimetizan con paredes.
- **Forma:** Puntos pequeños o estrellas de 4 puntas. No círculos sólidos — la transparencia y el glow funcionan mejor en MR.
- **Cantidad:** 8–15 partículas. Suficientes para que el rastro sea visible pero sin saturar la escena.
- **Cola (trail):** Cada partícula debe llevar un trail corto (lifetime ~0.15s) para dar la sensación de movimiento direccional y velocidad.
- **Tamaño:** Empiezan más grandes en el origen (~2–3 cm world space) y van reduciéndose al llegar al destino, para dar sensación de perspectiva y dirección.

### Efecto de llegada

Al terminar el recorrido, un pequeño **destello/pop** en el panel derecho: un flash blanco suave que se expande y desvanece en ~0.3 segundos. Esto confirma visualmente el punto de destino y actúa como "atracción final" si el usuario ya está mirando hacia la derecha.

### Feedback de sonido

Mantener el sonido actual de selección en el origen. Opcionalmente agregar un sonido suave de "chegada" (un tono corto y limpio, no intrusivo) sincronizado con el destello de llegada.

---

## Implementación (componentes Unity)

### Nuevos assets necesarios

| Asset | Tipo | Descripción |
|-------|------|-------------|
| `SelectionTrailEffect` | Prefab de Particle System | Sistema de partículas con trail, configurado en world space |
| `PanelArrivalFlash` | Prefab o efecto en UI | Flash de llegada sobre el panel derecho |

### Script nuevo: `SelectionTrailController.cs`

Namespace: `ArtUnbound.Feedback`

Responsabilidades:
- Recibir un evento de selección con la posición world space de la tarjeta origen.
- Instanciar (o reutilizar vía pool) el prefab `SelectionTrailEffect`.
- Animar las partículas a lo largo de una curva Bézier desde origen hasta el centro del panel derecho.
- Al completar el recorrido, disparar el efecto de llegada en el panel derecho.
- Destruir o retornar al pool el sistema de partículas cuando termine.

Dependencias:
- `UnifiedMainMenuController` — para obtener la posición world space del panel derecho en runtime.
- `PieceEffectsManager` (patrón de referencia) — para seguir el mismo patrón singleton/pool ya existente en `ArtUnbound.Feedback`.

### Punto de integración en código existente

En `UnifiedMainMenuController`, en el método que actualmente maneja la selección de una obra (probablemente `OnArtworkSelected` o similar):

1. Obtener la posición world space de la tarjeta seleccionada.
2. Llamar a `SelectionTrailController.Instance.PlayTrail(origen, destino)`.
3. El resto del flujo (cargar detalles en panel derecho) no cambia.

### Consideraciones técnicas

- El Particle System debe tener **Simulation Space = World** para que las partículas no se peguen al transform del emisor.
- La curva Bézier puede implementarse con `Vector3.Lerp` en cascada o con `AnimationCurve`. No usar `Physics` — es animación pura.
- El efecto debe correr independientemente del framerate (`useUnscaledTime = false` está bien; `deltaTime` normal).
- En el caso de que el panel derecho no esté visible aún (primera selección), el efecto igual debe dispararse — es precisamente cuando más importa.

---

## Criterios de éxito

- El usuario voltea hacia el panel derecho dentro de los 2 segundos después de seleccionar una pintura, sin instrucciones verbales.
- El efecto no se percibe como un error ni como algo roto.
- Usuarios que ya conocen el layout reportan el efecto como "agradable" o "confirmación", no como distracción.
