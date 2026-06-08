# Art Unbound — Documento de Decisiones de Diseño

**Versión:** Adenda v1.2 al GDD v3.0
**Fecha:** Mayo 2026
**Alcance:** Consolida las decisiones sobre comercialización, tamaños de rompecabezas, sistema de recompensas/logros y elementos cosméticos de progresión. Sustituye lo indicado en las secciones correspondientes del GDD v3.0.

---

## 1. Resumen ejecutivo de cambios

Cuatro bloques de decisiones:

1. **Modelo de comercialización:** se abandona el pago inicial ($9.99) + 17 packs DLC ($2.99 c/u) + bundles. Se adopta **Free-to-Play con desbloqueo único**: app gratuita con ~12 obras icónicas, y un solo IAP de **$9.99** que desbloquea el catálogo completo (240+ obras restantes).

2. **Tamaños de rompecabezas:** se reencuadran de "dificultades" (Fácil/Normal/Difícil) a **compromisos de tiempo** (Relajado / Intermedio / Maratón), porque la fatiga del visor invalida el modelo de rompecabezas físico de horas. Los logros pasan a ser **size-agnostic**, y como consecuencia el **marco del cuadro es constante** (sin variantes por tamaño) y la **medalla de completación es única** (binaria). Bronce/plata/oro sobreviven solo en la placa de estatus, donde reflejan acumulación, no una elección puntual.

3. **Sistema de recompensas:** capa de progresión basada en **logros** (no en niveles que gateen contenido) cuya divisa son **objetos cosméticos visibles en el espacio del usuario** (placas, lámpara, cédulas) más una **placa de estatus por tier**. El acceso al contenido queda totalmente desacoplado de la progresión.

4. **Elementos cosméticos concretos:** se define qué objetos son únicos (una lámpara, una cédula) y cuáles son coleccionables (placas por pintor y por movimiento), con el set inicial y los umbrales definidos a partir del catálogo real de 252 obras.

---

## 2. Modelo de comercialización

### 2.1. Decisión

| Aspecto | GDD v3.0 (anterior) | Decisión actual |
|---|---|---|
| Entrada | Pago de $9.99 | **App gratuita (F2P)** |
| Contenido gratis | N/A | **~12 obras icónicas** |
| Monetización | 17 packs DLC a $2.99 + bundles + lealtad | **Un único IAP: "Desbloquear catálogo completo" a $9.99** |
| Packs temáticos como SKU | Sí (17) | **No.** Los assets permanecen en el proyecto pero NO se exponen como productos de tienda |
| Cuadro Semanal / live-ops | Planeado | Fuera del alcance de lanzamiento |

### 2.2. Justificación

- **Por qué F2P en vez de pago de entrada:** en la Meta Horizon Store post-fusión la *discoverability* está degradada y hay saturación de apps. Un muro de pago de $10 para un desarrollador desconocido, compitiendo contra títulos consolidados (Puzzling Places, Jigsaw Night), mata el funnel de adquisición. "Gratis" multiplica instalaciones y da acceso a las listas de "mejores apps gratuitas", el principal multiplicador de top-of-funnel.

- **Por qué un solo IAP y no la economía de packs:** el catálogo de 17 SKUs + bundles + entitlements por pack + restore purchases + testing en la matriz Quest 2/3/Pro × MR/VR es el mayor sumidero de ingeniería con el payoff más incierto. Un desbloqueo único es un **flag de entitlement booleano**, trivial de implementar y mantener. Se conserva la opción de trocear en packs *después*, como update, solo si hay tracción (decisión reversible).

- **Por qué $9.99 y no $12.99:** hay un umbral psicológico fuerte entre "$10" y "$13". En F2P, la decisión ya no es "¿vale la pena este juego?" sino "ya jugué 12 obras y me gustó, ¿desbloqueo el resto?". A $9.99, desbloquear 240+ obras se siente como propina. Se puede subir después; bajar precio se ve mal.

### 2.3. Tier gratuito — criterio de selección

Las ~12 obras gratuitas deben ser **las más reconocibles del catálogo**, no una muestra al azar. El tier gratis no es un cebo capado: debe ser tan satisfactorio que genere boca-en-boca y conversión. Candidatas naturales del Base Set (a confirmar): Mona Lisa, The Starry Night, Girl with a Pearl Earring, The Kiss (Klimt), The Birth of Venus, Las Meninas, The Creation of Adam, The Night Watch, Liberty Leading the People, Sunflowers, The Last Supper, The School of Athens.

### 2.4. Mensaje de tienda

La página de tienda y el marketing deben **liderar con el gancho de la pared real** ("tu obra maestra terminada se queda colgada en tu pared, entre sesiones"), no con "252 pinturas". Ese gancho es el único diferenciador que la competencia no replica fácilmente. El catálogo de dominio público es commodity; la persistencia espacial en el hogar, no.

---

## 3. Tamaños de rompecabezas y dificultad

### 3.1. El problema detectado en playtesting

El GDD v3.0 ofrecía tres dificultades por número de piezas (Fácil ~64 / Normal ~144 / Difícil ~256), emulando el rompecabezas físico. El playtesting reveló una falla conceptual:

- Tiempos observados: pequeño ~20 min, mediano ~1 h, grande **sin completar ni una vez** (incluido el propio desarrollador).
- **El costo escala, la recompensa no.** Más piezas = ~4-5x más tiempo y esfuerzo, pero el cuadro final es **idéntico** (mismo tamaño, misma imagen, mismo objeto colgable). Solo cambia el tamaño de las piezas.
- Las piezas más pequeñas son **más difíciles de ver y agarrar** en un visor con resolución/passthrough limitados: la dificultad alta es además *menos cómoda de interactuar*.
- El modelo de rompecabezas físico (horas, social, dejar y retomar durante días) no aplica: **el visor cansa** y la fricción de re-entrada es alta.

### 3.2. Decisión

- **Reencuadre de los tres tamaños como compromisos de tiempo**, no como dificultad/prestigio:

| Nombre nuevo | Piezas aprox. | Tiempo estimado |
|---|---|---|
| Relajado | ~64 | ~20 min |
| Intermedio | ~144 | ~45 min |
| Maratón | ~150-180 (rebalancear desde 256) | ~2 h+ |

- El "Maratón" se conserva como **nicho deliberado** para el puzzler dedicado, no como requisito de nada. Rebalancearlo a la baja (256 → ~150-180) para que sea al menos completable.
- **Los logros son size-agnostic:** completar una obra cuenta igual sin importar el tamaño elegido. El tamaño es elección personal de comodidad, no eje de prestigio.
- **Marco constante (el marco sale del sistema de recompensas).** El cuadro final es idéntico sin importar el tamaño, así que su marco debe ser el mismo siempre. Se elimina la variación bronce/plata/oro por dificultad: ataba un "marco mejor" a la elección de comodidad, reintroduciendo por la puerta de atrás la jerarquía de prestigio que el reencuadre eliminó, y castigaba sutilmente a quien elegía el tamaño cómodo. El marco se vuelve una constante ("así se ve un cuadro colgado") y deja de ser un eje de premio; la expresión y la recompensa viven en lugares más ricos (placas, lámpara, cédula, placa de estatus).
- **El premio principal son los logros de amplitud**, no el grind de piezas.

### 3.3. Justificación

La alegría del producto es *ver y reconstruir distintas obras maestras*, no sufrir la misma imagen en piezas más chicas. Premiar amplitud (variedad de obras) en lugar de profundidad (más piezas) (a) empuja al jugador hacia el contenido y su disfrute, (b) no pelea contra la fatiga del visor, (c) produce una pared más variada y compartible, y (d) evita premiar resistencia en un juego de bienestar dirigido a público amplio y mayor.

---

## 4. Filosofía del sistema de recompensas

### 4.1. Principios

1. **Acceso ≠ progresión.** Lo comprado ($9.99) da acceso pleno e inmediato a todo el catálogo. La progresión (logros) corre en paralelo y **nunca gatea contenido ya pagado**. Acoplar ambos produce el peor escenario: el comprador siente que no tiene lo que pagó, y la progresión hereda la fricción del paywall.

2. **Cada recompensa debe ser visible en el espacio.** La pared (MR) o la galería (VR) es la vitrina de trofeos y es lo que se fotografía y comparte. Una recompensa de menú no genera referral; una colgada en la sala, sí. La divisa principal de logros es **cosmética visible en el mundo**.

3. **Sin progresión que imponga qué jugar.** El atractivo del catálogo es la libertad de elección estética. No se bloquean obras por orden de avance. La progresión premia exploración, no la fuerza.

4. **UI diegética sobre menús.** En MR/VR, mostrar el avance como objetos en el espacio se siente mejor que esconderlo en un menú.

### 4.2. Las cuatro dimensiones desacopladas

| Dimensión | Qué es | Dónde vive |
|---|---|---|
| **Acceso** | El catálogo comprado | Entitlement (IAP) |
| **Maestría** | Logros completados | Lógica de logros |
| **Expresión** | Cosméticos aplicados/colgados | La pared real (MR) / galería (VR) |
| **Estatus** | Rango/tier acumulado | La placa de estatus + título |

---

## 5. Catálogo de objetos de recompensa

### 5.1. Objetos ÚNICOS (se desbloquea "el", no varios)

- **La lámpara / spotlight.** Un único objeto de iluminación tipo museo que aparece encima de cada cuadro colgado una vez desbloqueado. Sin variantes ni estilos múltiples.
- **La cédula informativa.** Una única placa-cédula estilo museo (título + artista) que va debajo de cada cuadro colgado una vez desbloqueada. Sin variantes.

> Ambas son atributos visuales de las obras ya colgadas, no objetos colgables independientes. Elevan el "look galería" de toda la pared de golpe.

### 5.2. Placa de ESTATUS (única, por tiers, con contenido dinámico)

Objeto colgable que muestra el avance global. Reemplaza cualquier pantalla de "perfil/estadísticas" de menú: el avance se ve **en el mundo**.

- **Se desbloquea al completar el primer rompecabezas** y se cuelga donde el usuario quiera (mismo mecanismo de colgado que un cuadro).
- **Tamaño fijo**, dimensionado para que quepan todos los logros eventualmente.
- **Tiers por nivel** (el nivel sube por número de rompecabezas armados): **Bronce → Plata → Oro → Platino** (más tiers si se desea). Al alcanzar un tier, el objeto entra a tus assets; lo cuelgas y retiras el anterior.
- **Propiedad técnica clave:** como todos los tiers son de tamaño idéntico, el intercambio es un *drop-in replacement* — mismo punto de anclaje, sin re-anclar ni recalcular. El tier es la "piel" de prestigio del mismo tablero.

**Composición (dos partes):**

- **Cabecera fija (determinista):** rango/título + agregados + logro cumbre. Aquí va lo comparable de un vistazo, ej: `Maestro Curador · 127/252 obras · 9/12 placas`. El agregado es la señal de comparación más fuerte y es determinista por naturaleza.
- **Cuerpo dinámico (cronológico):** los medallones de logro se pueblan en el orden en que se obtienen. Cuenta la historia personal del jugador; el conteo sigue siendo comparable aunque el orden varíe.

**Estados visuales:** logros obtenidos a color/palomeados; pendientes como **slots vacíos en silueta** (aspiracionales, sin texto de requisitos). El "qué necesito para desbloquear esto" NO vive en la placa del mundo — vive en el catálogo del menú (sección 6).

### 5.3. Placas TEMÁTICAS (múltiples, coleccionables)

Objetos colgables que entran a los assets del jugador al alcanzar el umbral de un pintor o movimiento. Se cuelgan libremente donde el usuario quiera (no se asume curaduría espacial por "alas").

- **Assets fijos con diseño propio.** Al ser de dimensión fija y autorada (no se estiran a proporciones arbitrarias como los marcos de cuadro), pueden tener todo el detalle ornamental deseado: girasoles para Van Gogh, azul de Delft para Vermeer, etc. Recupera la ambición estética imposible en los marcos por el problema de aspect ratio.
- **Set inicial y umbrales en la sección 7.**

### 5.4. El tope de pared y por qué valida las placas

Un cuadro ocupa ~1 m² promedio; nadie tiene pared real para colgar decenas. Esto **no es un bug, es lo que justifica las placas**:

- La pared nunca fue una vitrina completa, sino una **selección curada y rotativa de favoritos** — y el sistema ya lo soporta (colgar, descolgar, intercambiar). Espeja el coleccionismo real: posees muchas, exhibes pocas.
- La placa de "Maestro Van Gogh" **es la compresión de un logro demasiado grande para exhibir físicamente**: no cuelgas 12+ Van Gogh, cuelgas *un* objeto que prueba que los hiciste. La placa existe precisamente para resolver el límite de espacio.
- El modo **VR no tiene esta restricción**: ahí cabría la colección completa en múltiples paredes/salas si se desea.

### 5.5. Marco, medalla de completación y el principio de jerarquía

Consecuencia directa de que el tamaño sea una elección de comodidad y no un eje de prestigio:

- **Marco del cuadro: único y constante.** Sin variación bronce/plata/oro por dificultad (ver sección 3.2). El marco deja de ser un eje de premio.
- **Medalla de completación en la galería: única y binaria.** En la galería del menú, la medalla sobre un cuadro indica solo que ya lo armaste ("¿ya lo armé?"), sin variar por bronce/plata/oro. Una medalla que variara por dificultad reintroduciría la jerarquía en otro lugar; binaria es lo consistente.

**Principio rector:**

> Bronce/plata/oro debe reflejar **logro acumulado** (cuánto has hecho en total), nunca una **decisión puntual de comodidad** (el tamaño de un rompecabezas individual).

Por eso los tiers bronce/plata/oro/platino **sobreviven únicamente en la placa de estatus** (sección 5.2), donde se *ganan* con el tiempo por número de rompecabezas armados. Un cuadro individual no los tiene, porque su tamaño fue solo comodidad. Esta distinción mantiene el sistema coherente: tiers para lo cumulativo, sí; tiers para lo puntual, no.

---

## 6. Catálogo de assets en menú (UI de descubrimiento)

Sección del menú (ej. "Galería de Recompensas" / "Assets") que funciona como compendio:

- Muestra **todos** los objetos obtenibles (lámpara, cédula, placas de estatus por tier, placas temáticas).
- Al hacer click en un objeto **bloqueado**, abre una ventana con el **logro/condición** necesaria para desbloquearlo.
- Es el **hogar de la información de requisitos** (no la placa del mundo). Resuelve "¿dónde viven los items bloqueados?": en el catálogo, no ensuciando el cuarto.

**División de responsabilidades:**
- **Placa del mundo:** escaparate. Obtenidos a color + slots vacíos en silueta. Glanceable, celebratorio. Sin texto de requisitos.
- **Catálogo del menú:** manual. Todo el inventario obtenible + condiciones de desbloqueo.

---

## 7. Definición del set inicial de placas temáticas

Definido a partir del catálogo real (252 obras), con conteos exactos por pintor y movimiento.

### 7.1. Condición de desbloqueo (resuelta)

- **Umbral general: ~12 obras** completadas del pintor/movimiento desbloquean su placa.
- **Regla para pools pequeños:** si el pintor/movimiento tiene **menos de 12 obras** en el catálogo, el umbral es **todas** sus obras. (Aplica a Renoir, Vermeer y Rembrandt; ver tabla.)
- **Size-agnostic:** cualquier tamaño (Relajado/Intermedio/Maratón) cuenta para el umbral.
- **Dificultad:** cualquier dificultad cuenta, para no alienar a la audiencia casual. Si se desea un logro de mayor prestigio, podría exigir que las obras se completen en tamaño **Maratón** (no "Oro", que ya no existe a nivel de cuadro individual).
- **Meta-logro "Completista":** completar el catálogo entero (o el total de un pintor/movimiento por encima del umbral) queda como trofeo de ultra-prestigio para el long-tail obsesivo, sin ser requisito de las placas estándar.

### 7.2. Placas por PINTOR — 5 iniciales

Criterio: representación suficiente para que el logro sea sustancial, combinado con reconocimiento e identidad visual.

| # | Pintor | Obras en catálogo | Umbral de placa | Identidad visual sugerida |
|---|--------|------|------|---------------------------|
| 1 | Vincent van Gogh | 34 | 12 | Dorado con relieve de girasoles / textura de pincelada |
| 2 | Claude Monet | 15 | 12 | Motivo de nenúfares, tonos agua |
| 3 | Pierre-Auguste Renoir | 11 | **todas (11)** | Tonos cálidos parisinos / luz de café |
| 4 | Johannes Vermeer | 11 | **todas (11)** | Azul de Delft, sobrio |
| 5 | Rembrandt van Rijn | 10 | **todas (10)** | Madera oscura con dorado tenue, claroscuro |

**Nota sobre Leonardo da Vinci:** el pintor más famoso del mundo, pero solo **5 obras** en el catálogo. Se deja para la segunda tanda porque un umbral tan bajo no sostiene una placa de prestigio. Si se prioriza el gancho de marketing de "coleccionar a Da Vinci", incluirlo exigiría sus 5 obras en tamaño **Maratón** para que cueste algo.

**Siguiente tanda (al agregar más):** Leonardo da Vinci (5), Caravaggio (5), Francisco de Goya (5), Raphael (6), Paul Cézanne (7), Edgar Degas (5), Edouard Manet (8). Para estos, el umbral será "todas" hasta que crezca su representación.

### 7.3. Placas por MOVIMIENTO — 7 iniciales

Criterio: solo movimientos con masa suficiente (≥21 obras) para sostener una placa. Los pequeños se fusionan para no dejar obras huérfanas. Todos superan 12, por lo que el umbral de 12 aplica a todos.

| # | Placa de movimiento | Obras | Umbral | Notas |
|---|---------------------|-------|------|-------|
| 1 | Post-Impresionismo | 51 | 12 | El pool más grande |
| 2 | Impresionismo | 40 | 12 | |
| 3 | Renacimiento | 36 (+5 Manierismo) | 12 | Absorbe Manierismo (5) |
| 4 | Siglo de Oro Holandés | 36 | 12 | (Dutch Golden Age) |
| 5 | Realismo | 23 | 12 | |
| 6 | Barroco | 21 | 12 | |
| 7 | Romanticismo | 21 (+6 Rococó +6 Neoclasicismo) | 12 | Absorbe Rococó y Neoclasicismo → o placa "Razón y Revolución" |

**Movimientos sin placa propia de inicio:** Simbolismo (4) y Prerrafaelita (3) son demasiado pequeños. Sus obras cuentan para volumen total y placas de pintor, pero no tienen placa de movimiento dedicada hasta que crezcan o se fusionen en una placa "Fin de Siècle / Simbolismo".

> **Nota sobre solapamiento (intencional):** con umbral de 12, las obras de un pintor también suman a su movimiento (ej. completar 12 Van Gogh avanza Post-Impresionismo). Es deseable: múltiples barras de progreso avanzando en paralelo.

**Total de placas temáticas iniciales:** 5 pintores + 7 movimientos = **12 placas**.

---

## 8. Requisito de datos

Cada obra del catálogo necesita estar etiquetada por:

- **`author`** — ya existe en `artunbound_collection.json`.
- **`artMovement`** — ya existe en `artunbound_collection.json`.

No se requiere metadata nueva. Los logros de pintor/movimiento se resuelven contando obras *distintas* completadas que coincidan con cada `author` / `artMovement`, comparando contra el umbral (12, o el total del pool si es menor).

---

## 9. Alcance y secuenciación de implementación

### 9.1. Contexto de producción

La implementación se realiza con asistencia de Claude Code (el desarrollador define el goal, prueba e itera). La velocidad observada es alta (sistema de colgado ~2 sesiones de 2-3 h; salto MR→VR completo con locomoción ~4 sesiones). Esto invalida cualquier estimación de costo basada en horas de programación manual: construir este sistema es barato.

**Caveat:** las estimaciones por feature discreta son optimistas para un sistema transversal como este (toca persistencia, anclaje MR, galería VR, menú y catálogo a la vez). La cola de QA de integración no se abarata tan limpiamente como la implementación. Estimación realista: ~1 a 1.5 semanas con la cola de pruebas.

### 9.2. MVP del sistema de recompensas (pre-lanzamiento)

- Placas de estatus por tier (Bronce→Plata→Oro→Platino) — entregan la sensación de "niveles".
- Catálogo de assets en menú con ventana de requisitos.
- La lámpara única + la cédula única.
- Un set inicial reducido de placas temáticas reales (no las 12 completas de inicio).
- Reencuadre de tamaños (Relajado/Intermedio/Maratón) + logros size-agnostic.

### 9.3. Post-lanzamiento (si hay tracción)

- Amplitud completa de placas temáticas (más pintores, fusiones de movimiento), **guiada por qué obras/cuartos comparte realmente la gente**, no front-loadeada a ciegas.
- Cuadro Semanal / live-ops.
- Posible troceo del catálogo en packs si la demanda lo justifica.
- Multijugador / co-op (shared spatial anchors) — el mayor limitante del techo del juego, pero un proyecto en sí mismo; solo si la tracción orgánica lo amerita.

### 9.4. La restricción que ningún feature resuelve

La restricción vinculante no es la velocidad de build, sino la **adquisición**: si el juego encuentra audiencia frente a competencia consolidada (Puzzling Places, Jigsaw Night) en una tienda con descubrimiento degradado, sin multijugador. Parte de la capacidad de desarrollo liberada debe ir a distribución: página de tienda con el gancho de la pared, tráiler que muestre la transformación del cuarto en museo, contacto con reseñadores de VR, evaluación de Meta Horizon+. El sistema de placas ayuda a la adquisición vía referral (cuartos fotografiables), pero no sustituye el esfuerzo de distribución.

---

## 10. Criterio de salida (gestión del proyecto)

Dado que es un proyecto-hobby con potencial comercial, conviene fijar de antemano un umbral de decisión que convierta "¿le sigo invirtiendo?" en un experimento con criterio de salida: definir, antes de lanzar, una meta de ventas/conversión a X días (ej. 90), y si no se alcanza, dejar el juego en mantenimiento y arrancar el siguiente. Esto evita decisiones emocionales sobre cuánto tiempo seguir invirtiendo.
