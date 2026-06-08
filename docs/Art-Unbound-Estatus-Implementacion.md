# Art Unbound — Estatus de Implementación

**Fecha:** Junio 2026
**Propósito:** Mapear qué del GDD v5 está construido hoy y qué falta. El GDD describe el estado **final** del juego; este documento es el seguimiento de avance para guiar el desarrollo. No es parte del GDD.

---

## 1. Implementado (estado actual del build)

- **Menú principal:** barra superior (título, toggle de modo VR/MR, Configuración), catálogo en cuadrícula paginada de miniaturas, navegación inferior con **Home y Search**.
- **Detalle de obra:** ventana modal con imagen, título, artista, museo, movimiento y año; tres botones de tamaño (A Coffee / A Break / An Afternoon) para obras desbloqueadas.
- **Tamaños:** derivados del tamaño físico de pieza (8 / 6 / 4 cm) vía `PuzzleConfig`.
- **Armado de puzzle:** pantalla de tres paneles (referencia + info | tablero | bandeja). Panel de info con barra de progreso, contadores (total / correctas / incorrectas), temporizador, pieza musical, botón Exit y botón de resaltado de piezas mal colocadas.
- **Colocación de piezas:** manos (al slot más cercano) y controles (a la celda elegida).
- **Colgado de obras:** funcional con manos y con controles; persistencia por anclajes espaciales (MR) y JSON local (VR). Re-colgado al volver a entrar a ver la obra.
- **Modos MR y VR:** ambos funcionales, con toggle en el menú. Galería VR base (entorno sobrio).
- **Audio:** música clásica de dominio público con título/compositor en pantalla; efectos de sonido.
- **Medalla de completación:** se otorga al terminar una obra.
- **Catálogo:** 252 obras.

---

## 2. Pendiente (diseñado en el GDD, no construido)

### 2.1. Cambios sobre lo ya implementado

| Pendiente | GDD | Nota |
|-----------|-----|------|
| Renombrar el botón "Misplaced" a **"Check"** | 4.6 | Cambio de etiqueta |
| Quitar la **tienda (Store)** del menú y su contenido de packs | 4.1, 12 | Hoy el nav es Home/Search/Store con packs |
| **Botón de compra en el detalle de obras bloqueadas** | 4.2, 12 | Reemplaza los botones de tamaño cuando la obra está bloqueada; dispara el desbloqueo único de todo el catálogo |
| Agregar **Collection** a la navegación inferior | 4.1, 8.5 | Hub de exhibición; hoy no existe |

### 2.2. Modelo de monetización (F2P)

- **Desbloqueo único de $9.99** del catálogo completo (IAP). Hoy el modelo es de packs en la tienda; hay que migrar al desbloqueo único disparado desde el botón de compra del detalle.
- Definir y marcar las **~12 obras gratuitas** (tier gratis) vs. el resto bloqueado.

### 2.3. Sistema de recompensas y progresión (sección 8 — nada implementado)

- **Medalla como token de progreso:** conectar la medalla de completación al avance hacia placas/assets (hoy es solo una felicitación).
- **Placas temáticas** (por pintor y movimiento): set inicial de 5 pintores + 7 movimientos; umbral de 6 obras; assets con diseño temático.
- **Placa de estatus** por tiers (Bronce → Plata → Oro → Platino) con cabecera fija y cuerpo dinámico de logros.
- **Lámpara** y **cédula** únicas (atributos visuales de las obras colgadas).
- **Collection (menú):** inventario de exhibición unificado (obras + recompensas) y descubrimiento de objetos bloqueados con sus condiciones.
- **Assets desbloqueables de la galería VR:** los logros desbloquean elementos que mejoran el entorno VR base.

### 2.4. Fase 2 (sección 15 — posterior, sujeta a tracción)

- **Galerías sociales (visitas)** y **armado en compañía** (sesiones co-op en paralelo) en VR.
- **Placas Maestro / Maestros** (solo vs. co-op) y reglas anti-trivialización.
- **Galerías temáticas (VR)** desbloqueables al completar 12 obras de un tema.
- **Retención MR:** instalaciones temáticas, curaduría/expansión del hogar, función de compartir.

### 2.5. Escalabilidad futura (sección 14)

- Cloud Storage, Firestore, Remote Config, **Cuadro Semanal** (infraestructura base existe), ampliación de catálogo y de placas.

---

## 3. Sugerencia de orden de trabajo

1. **Migración de monetización** (quitar Store/packs → botón de compra + desbloqueo único + definir las 12 gratis) y rename de "Check". Son cambios chicos sobre lo ya hecho y dejan el modelo de negocio correcto para lanzar.
2. **Sistema de recompensas base:** medalla-como-token + placa de estatus por tiers + placas temáticas + Collection. Es el loop de retención de Fase 1.
3. **Lámpara, cédula y assets de galería VR.**
4. **Fase 2** solo si hay tracción tras el lanzamiento.

> Nota: el lanzamiento mínimo viable no requiere todo el punto 2 completo, pero sí el punto 1 (monetización correcta) y un loop de recompensa básico que dé sensación de progreso.
