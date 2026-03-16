# Meta Quest: Guías Oficiales de Interacción UI en Mixed Reality

> Documento generado a partir de la documentación oficial de Meta for Developers (developers.meta.com/horizon).  
> Última revisión de fuentes: Marzo 2025.

---

## Índice

1. [Consideraciones Clave de Diseño MR](#1-consideraciones-clave-de-diseño-mr)
2. [Requisitos de Diseño para Inputs y Hit Targets](#2-requisitos-de-diseño-para-inputs-y-hit-targets)
3. [Interacción con Contenido Virtual](#3-interacción-con-contenido-virtual)
4. [UI 2D en Espacio](#4-ui-2d-en-espacio)
5. [Componentes: Botones y Feedback](#5-componentes-botones-y-feedback)
6. [Input Modalities (Modalidades de Entrada)](#6-input-modalities-modalidades-de-entrada)
7. [Hands: Interacción con Manos](#7-hands-interacción-con-manos)
8. [Visual Design: Color, Tipografía e Iconos](#8-visual-design-color-tipografía-e-iconos)
9. [Spatial SDK y Apps Híbridas](#9-spatial-sdk-y-apps-híbridas)
10. [Referencias Oficiales](#10-referencias-oficiales)

---

## 1. Consideraciones Clave de Diseño MR

### Empezar con el mundo físico

- Usa metáforas del mundo físico como base: ¿cómo interactuaría el usuario con objetos reales?
- Los objetos virtuales deben coincidir en tamaño y profundidad con la realidad.
- Los objetos deben sentirse anclados (no atravesar paredes ni caer del suelo).
- Permite interacciones intuitivas: agarrar, pulsar botones, usar manijas.

### Eliminar restricciones del mundo físico

Una vez definidas las mecánicas base, puedes trascender lo físico:

- Conectar contenido virtual con el entorno físico (ej. interruptor virtual que controla lámparas reales).
- Dar "superpoderes" con input multimodal (ray cast desde la mano para agarrar a distancia).
- Traer usuarios remotos al espacio compartido.
- Crear portales o ventanas mágicas hacia espacios virtuales.

### Usar el entorno físico como lienzo

- Usa paredes, mesas y techos como superficies para colocar contenido virtual.
- Diseña alternativas de colocación: no todos los espacios tienen sofá, mesa o suficiente suelo.
- Haz que los objetos virtuales persistan como objetos físicos.

### Prototipar en espacios físicos reales

- Prueba en Meta Quest lo antes posible; lo diseñado en 2D suele comportarse distinto en MR.
- Usa placeholders 2D o 3D (cubos, imágenes) para validar tamaño, distancia, colocación y layout espacial.

---

## 2. Requisitos de Diseño para Inputs y Hit Targets

| Requisito | Detalle |
|-----------|---------|
| **Tamaño mínimo de hit target** | **48dp × 48dp** mínimo. **60dp × 60dp** recomendado para controles principales con Hand Tracking. |
| **Hit slop invisible** | Usa hit slop cuando los assets no cumplan el tamaño mínimo. Aplica también a iconos accionables. |
| **Espaciado entre componentes** | Deja espacio suficiente entre elementos; el hit slop puede extenderse más allá del componente visible. |
| **Estados hover y focus** | Todos los elementos interactivos deben tener estados hover y focus para soportar múltiples modalidades de input. |

---

## 3. Interacción con Contenido Virtual

### Colocación de objetos

- Distancia cómoda: ~**1 metro** ligeramente por debajo de la línea de visión.
- Alinear objetos con superficies (mesa, pared) para que se sientan realistas.
- Si un objeto es visualmente abrumador, revelarlo gradualmente con animación de fade-in.
- Evitar que objetos aparezcan de golpe muy cerca o muy grandes al iniciar la app.
- Dar feedback visual o auditivo al colocar objetos en superficies físicas.

### Billboarding (orientación de objetos)

- Hacer que etiquetas y objetos con información importante **siempre miren al usuario**.
- Considerar restricciones de eje para optimizar la experiencia.
- Aplicar billboarding a la UI para permitir interacción desde cualquier ángulo.

### Tamaño y distancia para percepción de profundidad

- Objetos más grandes que el espacio físico pueden confundir lo físico vs. virtual: usar fondo opaco o recortar.
- Objetos pequeños: dar señales visuales y auditivas claras para guiar al usuario.
- Modales/diálogos: considerar la profundidad; atenuar u ocultar contenido virtual detrás para evitar confusión.
- **No colocar objetos virtuales detrás de paredes u obstáculos físicos** — riesgo de lesiones y fatiga visual.

### Escalado angular consistente

- Usar **angular scaling** para información que deba leerse a cualquier distancia (diálogos, indicadores, flechas).
- Aplicar límites min/max de tamaño para evitar que objetos se acerquen o alejen demasiado.
- Usar animaciones al escalar para que el usuario entienda el cambio de distancia.

### Input directo e indirecto

- Soportar **ray-casting** desde las manos para interactuar a distancia.
- Permitir **interacción directa** (tocar, agarrar) para mayor inmersión.
- Considerar soporte de **controladores** cuando se necesite más precisión.

### Contenido anclado vs. head-locked

- **Evitar** contenido head-locked (HUD fijo a la cabeza) en passthrough; fatiga y reduce usabilidad.
- **Preferir** contenido anclado al espacio o que siga al usuario con animación suave.
- Usar **sombras** para colocación más realista.

### Locomoción en MR

- **No usar teleport** en experiencias con passthrough; confunde el sentido de ubicación.
- Al alternar entre modos (passthrough ↔ inmersivo), usar transiciones (fade) para evitar confusión.

### Feedback visual y auditivo

- Feedback en estados **hover** y **pressed** es crítico (no hay feedback táctil físico).
- Incluir feedback de compresión, highlight al presionar y sonido.

### Confort

- ~50% de usuarios juegan sentados, ~50% de pie; soportar ambas posturas.
- Soportar interacciones indirectas para que el usuario pueda interactuar en posición relajada.
- Mantener contenido dentro del campo de visión; evitar giros excesivos de cabeza.
- Evitar obligar a agacharse para recoger objetos; implementar recuperación de objetos caídos.
- Ofrecer práctica de interacción en entorno relajado y sin tiempo límite.

### Responsividad espacial

- Diseñar para un **entorno mínimo viable** (espacio y superficies mínimas).
- El contenido virtual no debe sugerir comportamientos que choquen con el entorno físico.
- Considerar el flujo del usuario por el espacio; dejar espacio claro para moverse e interactuar.

### Seguridad

- Diseñar pensando en la seguridad del usuario; evitar objetos virtuales que oculten objetos físicos peligrosos (lámparas, jarrones).
- No colocar contenido detrás de paredes que invite al usuario a golpearse.

---

## 4. UI 2D en Espacio

### Posición

| Tipo de interacción | Distancia recomendada |
|--------------------|------------------------|
| **Interacción directa con manos** | ~**45 cm** del usuario |
| **Pantalla grande, interacción indirecta** | ~**1 metro** del usuario |
| **Manos + controladores** | ~**70 cm** + UI de manipulación (agarrar, mover, recolocar) |

- Considerar el **campo de visión (FOV)** al posicionar.
- Ejemplo: el teclado virtual no debe quedar demasiado bajo ni obstruir la vista.
- El usuario puede moverse libremente; no impedir que se acerque o que interactúe directamente.

### Tamaño

- Aplicar **angular scaling** para mantener legibilidad y tamaño de target al variar la distancia.
- La ventana puede escalar automáticamente según la distancia del usuario.

### Manipulación

- Ofrecer feedback claro para **agarrar, mover y recolocar** ventanas.
- Siempre mostrar feedback visual que invite a la interacción.

---

## 5. Componentes: Botones y Feedback

### Variantes de botones

| Variante | Uso |
|----------|-----|
| **Primary** | Acción principal, única recomendada en la vista |
| **Secondary** | Acciones de apoyo o continuación |
| **Borderless** | Acciones de baja prioridad, sin fondo |
| **Destructive** | Eliminar datos o sesiones; usar color rojo con cautela |

### Buenas prácticas

- **Etiquetas cortas y específicas**: "Guardar", "Eliminar", "Continuar".
- **Un botón primary por vista** para jerarquía clara.
- **Hit targets ≥ 48dp × 48dp**.
- **Estados hover, pressed y disabled** claros en todos los elementos interactivos.
- Evitar etiquetas vagas como "Click aquí" o "Enviar".
- No usar múltiples botones primary en la misma vista.
- No usar botón destructive para acciones no destructivas.

---

## 6. Input Modalities (Modalidades de Entrada)

| Modality | Descripción |
|----------|-------------|
| **Controllers** | Precisión y fiabilidad; modalidad por defecto en Meta Horizon OS. |
| **Hands** | Mayor presencia y naturalidad; no sustituye controladores en todos los casos. |
| **Voice** | Más rápido que escribir; útil cuando las manos están ocupadas. |
| **Head gaze** | Fallback cuando no hay otras opciones; cursor en centro de la vista; botón de volumen para seleccionar. |
| **Keyboard** | Teclados físicos vía Bluetooth. |
| **Mouse** | Mouse Bluetooth para control preciso. |
| **Gamepad** | Controladores de consola para juegos. |
| **Stylus** | Dibujo y escritura en 2D y 3D. |

---

## 7. Hands: Interacción con Manos

### Interacciones del Interaction SDK (ISDK)

| Interacción | Descripción |
|-------------|-------------|
| **Grab** | Agarrar objetos virtuales |
| **Ray** | Seleccionar botones o paneles desde distancia |
| **Poke** | Tocar botones o paneles directamente |
| **Distance Grab** | Agarrar objetos a distancia |
| **Hand Posing** | Definir poses ideales para objetos agarrados |

### Principios de diseño

- **Usabilidad**: Las manos deben permitir al menos lo que se haría en el mundo real (agarrar, pulsar botones).
- **Accesibilidad**: Ofrecer alternativas para usuarios con movilidad limitada.
- **Multimodal**: Combinar manos, controladores, voz, etc.
- **Affordances**: Feedback visual y auditivo inmediato al completar acciones.
- **Las manos no son controladores**: No limitarse a adaptar interacciones de controlador; aprovechar las ventajas de las manos.

### Confort

- **Posición**: Mantener brazos cerca del cuerpo, codos alineados con las caderas.
- **Distancia**: Evitar alcances frecuentes; colocar lo más usado más cerca del cuerpo.

### Mapeo Controller → Hands

| Controller | Hands |
|------------|-------|
| Touch con punta del controlador | Selección directa con punta del índice |
| Trigger | Pinch (tap con índice) |
| Grab | Palm Grab o Hold pinch |
| Thumbstick | Microgesture |
| Menu Button | System Gesture (mano derecha) |

---

## 8. Visual Design: Color, Tipografía e Iconos

### Color

| Requisito | Detalle |
|-----------|---------|
| **Contraste** | **4.5:1** para texto normal; **3:1** para headlines y elementos no textuales. |
| **Evitar blanco y negro puros** | Usar grises claros y oscuros. Fondo claro no más brillante que `#DADADA`. |
| **Probar en headset** | Los colores suelen verse más saturados en el dispositivo. |

### Tipografía

| Requisito | Detalle |
|-----------|---------|
| **Tamaño mínimo** | **14px** para legibilidad mínima; **18px** para lectura cómoda. |
| **Fuente** | Sans-serif legible, con alta x-height y contadores grandes. Inter (Meta Horizon OS UI Set) recomendada. |
| **Peso** | Black, Bold, Medium y Regular más legibles que Light y Thin. |

### Iconos

- Iconos simples y reconocibles.
- Evitar iconos complejos con trazos muy finos.
- Grid 24×24 px; construir en 192×192 px para futuras resoluciones.

---

## 9. Spatial SDK y Apps Híbridas

### Apps híbridas

- Preferir apps que funcionen en modo **inmersivo** y **no inmersivo**.
- Permite ventanas del sistema, multitarea, posicionamiento junto a otras apps.
- El modo inmersivo debe activarse **explicitamente por el usuario**.

### Passthrough

- Mantener el passthrough cuando sea posible para conservar contexto.
- Evitar activar modo exclusivo en passthrough sin aviso.
- Cuidar la escala y cuánto del view ocupa el contenido para no perder contexto.

### Transiciones de inmersión

- Transiciones graduales con animaciones (fade, etc.).
- Evitar movimientos bruscos que desorienten.

### Contenido interactivo

- Clarificar qué es interactivo: glow al focus, colocación, audio espacial, movimiento.

---

## 10. Referencias Oficiales

- [Key considerations (MR Design Guideline)](https://developers.meta.com/horizon/design/mr-design-guideline/)
- [Hands Design](https://developers.meta.com/horizon/resources/hands-design-ui/)
- [Input Modalities](https://developers.meta.com/horizon/resources/bp-userinput/)
- [Design Requirements (Android apps)](https://developers.meta.com/horizon/documentation/android-apps/design-requirements)
- [Buttons Best Practices](https://developers.meta.com/horizon/design/buttons_bp/)
- [Spatial SDK Design Tips](https://developers.meta.com/horizon/documentation/spatial-sdk/spatial-sdk-design-tips)
- [Health & Safety](https://developers.meta.com/horizon/design/mr-health-safety-guideline/)
- [Scene Understanding](https://developers.meta.com/horizon/design/mr-design-scene/)
- [Passthrough](https://developers.meta.com/horizon/design/mr-design-passthrough/)
- [Spatial Anchors](https://developers.meta.com/horizon/design/mr-design-spatial-anchors/)

---

*Documento generado para consulta interna en proyectos de Meta Quest / Horizon OS.*
