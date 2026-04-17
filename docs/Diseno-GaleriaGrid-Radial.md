# Diseño de Galería: Grid Radial con Blur Dinámico
**Art Unbound — Pantalla de Selección de Pinturas**  
**Versión**: 1.1 | **Fecha**: 2026-04-14  
**Alcance v1**: Grid funcional con navegación y selección. Etiquetas de corriente artística fuera de scope (v2).

---

## 1. Problema que resuelve

El sistema actual de selección de pinturas usa 3 paneles flotantes con paginación:
- Panel central: grid de 12 pinturas por página → **17 páginas** para 202 pinturas
- Panel derecho: detalle de la pintura seleccionada
- Navegación: botones "Siguiente / Anterior"

**Fricciones identificadas:**
- 17 páginas de navegación → fatiga de paginación
- La interfaz se siente como una aplicación de escritorio, no como MR
- No hay sensación de escala ni abundancia del catálogo
- Los botones de página son pequeños y requieren precisión

---

## 2. Solución: Grid Radial con Blur Dinámico

Un único panel plano flotante que muestra las 202 pinturas en un grid continuo de **14 filas × 15 columnas**, con un efecto de enfoque radial: las pinturas del centro se ven nítidas y las pinturas se difuminan progresivamente hacia los bordes hasta casi desaparecer. El usuario navega moviendo la palma abierta sobre el panel y selecciona haciendo pinch sobre cualquiera de las 9 pinturas centrales.

---

## 3. Especificaciones del Grid

### Dimensiones
```
Pinturas:         15cm × 15cm cada una (cuadradas, sin margen visible)
Grid completo:    15 columnas × 15cm = 2.25m de ancho
                  14 filas    × 15cm = 2.10m de alto
Posición inicial: centrado frente al usuario a ~70cm de distancia
```

### Distribución inicial
```
El grid inicia con la fila 7 y columna 8 al centro visual del panel.

  Columnas visibles a cada lado del centro:  7
  Filas visibles arriba del centro:          6
  Filas visibles abajo del centro:           7
```

### Campo visual: círculo de 1 metro de diámetro
Solo existe un "ojo de buey" de 1m de diámetro donde las pinturas son visibles. Fuera de él, todo es transparente.

```
Zona A — Centro nítido (3×3 = 9 pinturas):
  45cm × 45cm | Sin blur | SELECCIONABLES

Zona B — Anillo interior (siguiente corona):
  Blur leve (~20%) | Visibles con detalle reducido | SELECCIONABLES

Zona C — Anillo medio:
  Blur medio (~50%) | Reconocibles pero sin texto | No seleccionables

Zona D — Anillo exterior:
  Blur fuerte (~80%) | Solo silueta de color | No seleccionables

Zona E — Borde del círculo:
  Fade a transparente | Comunica "hay más allá" | No seleccionables
```

**Diagrama de zonas:**
```
        ░░░░░░░░░░░░░░░░░░░░░
      ░░  [D]   [D]   [D]   ░░
    ░░  [C]   [B]   [B]   [C]  ░░
    ░  [C]  [B]  [A] [A] [B]  [C] ░
    ░  [C]  [B]  [A] [A] [B]  [C] ░
    ░░  [C]   [B]   [B]   [C]  ░░
      ░░  [D]   [D]   [D]   ░░
        ░░░░░░░░░░░░░░░░░░░░░
```

---

## 4. Sistema de Navegación

### Gesto: Palma Abierta

El usuario extiende la mano con la **palma abierta orientada hacia el panel**. El sistema detecta esta pose y activa el modo navegación. Al mover la mano, el grid se desplaza en la misma dirección.

```
Condición de activación:
  ✓ Dedos extendidos (hand pose = open)
  ✓ Normal de la palma apuntando hacia el panel (±30°)
  ✓ Mano dentro de 40cm del plano del panel

Feedback visual de activación:
  → Glow suave en el borde del panel
  → Cursor sutil que sigue la palma sobre el grid
  → El usuario sabe inmediatamente que está en modo navegación
```

### Mapeo del movimiento
```
Mano se mueve ARRIBA    → Grid sube    (pinturas bajan)
Mano se mueve ABAJO     → Grid baja    (pinturas suben)
Mano se mueve DERECHA   → Grid va derecha
Mano se mueve IZQUIERDA → Grid va izquierda

Velocidad: proporcional a la velocidad del movimiento de la mano
```

### Inercia y Snapping (obligatorio)
```
Flick rápido → el grid continúa deslizándose y desacelera gradualmente
               (como el scroll de un smartphone)

Al detenerse → la pintura más cercana al centro hace snap automático
               al punto focal central

Easing:       ease-out en 250-350ms para TODAS las animaciones de grid
              (nunca movimiento lineal instantáneo)
```

### Desactivación del modo navegación
```
El modo navegación se desactiva cuando:
  → La palma deja de estar orientada hacia el panel
  → La mano se aleja más de 40cm del plano
  → El usuario hace pinch (pasa a modo selección)
```

---

## 5. Eficiencia de Navegación

Con el grid de 14×15 y zona de selección de 3 filas/columnas:

### Navegación Vertical
```
Posición inicial: fila 7 al centro
Filas por "movimiento": ~3 (el alto de la zona de selección)

  Centro → fondo del catálogo:   2 movimientos hacia abajo
  Fondo  → inicio del catálogo:  4 movimientos hacia arriba
  Catálogo completo:             máximo 4 gestos verticales
```

### Navegación Horizontal
```
Posición inicial: columna 8 al centro
Columnas por "movimiento": ~3

  Centro → orilla derecha:       ~3 movimientos
  Orilla → orilla opuesta:       ~4 movimientos
  Catálogo completo:             máximo 4 gestos horizontales
```

**Conclusión:** El catálogo completo de 202 pinturas es accesible en un máximo de **4 gestos** en cualquier dirección desde cualquier posición.

---

## 6. Sistema de Selección

### Zona seleccionable
Las **9 pinturas del centro** (3×3) son seleccionables. Las pinturas de la Zona B (anillo interior con blur leve) también son seleccionables para mayor fluidez.

```
Zona de selección = ~45cm × 45cm
                  = área confortable de interacción frente al usuario
                  = sin necesidad de estirar el brazo
```

### Gesto de selección: Pinch
El usuario junta pulgar e índice (pinch) apuntando hacia cualquier pintura de la zona seleccionable.

**Disambiguación con navegación:**
```
PALMA ABIERTA  ≠  PINCH

Son posturas físicamente opuestas. El Quest 3 las distingue
con alta confiabilidad. No hay zona gris entre ellas.

→ Zero ambigüedad de intención
→ No se necesitan umbrales de tiempo ni distancia
→ No se necesita lógica de detección compleja
```

---

## 7. Flujo al Seleccionar una Pintura

### Fase 1 — Selección (0.0s → 0.3s)
```
1. Usuario hace pinch sobre una pintura
2. Esa pintura escala suavemente hasta ~40cm × 40cm in-place
3. El grid NO se mueve — la pintura crece sobre él
4. Un overlay semi-opaco oscurece el grid detrás (dimming ~60%)
5. La pintura ampliada flota ligeramente hacia el usuario (+5cm en Z)
```

### Fase 2 — Detalle visible (0.3s en adelante)
```
Aparecen alrededor de la pintura ampliada:
  ┌─────────────────────────────────┐
  │                                 │
  │  [Imagen de la pintura ~40cm]   │
  │                                 │
  │  Título de la obra              │
  │  Artista • Año                  │
  │  Museo / Colección              │
  │  Corriente artística            │
  │                                 │
  │  [Fácil]  [Normal]  [Difícil]   │
  │  64 pzs   144 pzs   256 pzs     │
  │                                 │
  │              [✕ Cerrar]         │
  └─────────────────────────────────┘

Los detalles aparecen con fade-in escalonado (no todos a la vez)
```

### Fase 3 — Cerrar (pinch en ✕ o pinch fuera del panel)
```
1. La pintura encoge de regreso a su tamaño original en el grid
2. El dimming del grid se desvanece
3. El grid queda exactamente como estaba — sin reordenamiento
4. El usuario puede continuar explorando
```

---

## 8. Navegación por Corriente Artística *(fuera de scope — v2)*

> ⚠️ **Esta sección queda pendiente para una segunda iteración.** El grid v1 funciona sin categorías. Las pinturas se organizan en orden de catálogo. Una vez que el grid base esté funcionando y probado, se evaluará cómo organizar las 202 pinturas por corriente artística y agregar las etiquetas de navegación rápida.

**Concepto reservado para v2:**
Etiquetas flotantes sobre el grid (`[Renacimiento]` `[Barroco]` `[Impresionismo]` etc.) que al hacer pinch desplazan el grid automáticamente a esa sección. Requiere primero definir el orden de las pinturas dentro del grid por corriente artística.

---

## 9. Principios de Animación

Todas las animaciones deben seguir estas reglas:

```
Duración estándar:    250-350ms
Easing:               ease-out (nunca lineal, nunca ease-in-out brusco)
Movimiento del grid:  con inercia física, no se detiene instantáneamente
Snap al centro:       suave, ~150ms, curva de spring (slight overshoot + settle)
Dimming al selección: fade de 200ms
Expand de pintura:    scale + translate Z simultáneos, 300ms ease-out
Collapse de pintura:  inverso, 250ms
```

**Regla de oro:** Si el movimiento parece que "cortó" o fue instantáneo, necesita easing.

---

## 10. Decisiones de Diseño Descartadas

Estas opciones fueron evaluadas y descartadas con justificación:

| Opción | Por qué se descartó |
|---|---|
| **Curvatura del panel** | El blur radial ya resuelve la fatiga de reenfoque visual. Curvar el panel rompería el mapeo 1:1 del gesto de palma. |
| **Umbral tiempo/distancia** (tap vs drag) | El hand tracking del Quest tiene jitter de ~5-10mm. Un umbral de movimiento generaría falsos positivos y añadiría latencia perceptible a todas las selecciones. |
| **Desplazamiento en Z del panel** durante scroll | Innecesario y potencialmente desorientador en MR con passthrough. El dimming suave comunica el estado de navegación con menos intrusión. |
| **Drag en espacio vacío entre pinturas** | El espacio entre pinturas en un grid 14×15 es demasiado pequeño para ser un target confiable. |
| **Paginación con botones** | 17 páginas. Descartado por fatiga de navegación. |
| **Filtros por autor** | 82 autores distintos. Las corrientes artísticas son categorías más amplias y significativas. |

---

## 11. Consideraciones Técnicas para Unity

### Shader de blur radial
```
El blur NO debe ser un post-process de cámara (afectaría todo el MR).
Debe implementarse por pintura individual:

Opción A: Shader con parámetro "distancia al centro del grid"
          → cada RawImage recibe un valor 0.0-1.0 de blur
          → el shader aplica gaussian blur proporcional

Opción B: Material con textura de gradiente radial como máscara de opacidad
          → más simple, menos costoso en GPU
          → suficiente para el efecto deseado
```

### Rendimiento en Quest 3
```
202 RawImages activas simultáneamente puede ser costoso.
Estrategia recomendada: Object pooling

- Solo instanciar las pinturas visibles en el círculo de 1m (~40-50 pinturas)
- Las pinturas fuera del círculo: desactivar el GameObject, no destruir
- Al hacer scroll: reciclar los GameObjects que salen por un lado
  hacia el lado opuesto con nueva textura asignada

Texturas: cargar bajo demanda, cachear las últimas ~60 usadas
```

### Detección de pose de palma abierta
```
Usar XRHandSubsystem (ya integrado en el proyecto):
  - Detectar que todos los dedos estén extendidos (open hand)
  - Calcular la normal de la palma con el joint de la muñeca + nudillos
  - Verificar que la normal apunte hacia el panel (dot product > 0.7)
  - Umbral de activación: mantener la pose por 100ms (evita falsos positivos)

Referencia en el proyecto: HandTrackingInputController.cs ya usa
XRHandSubsystem para detectar pinch — extender desde ahí.
```

### Snap al centro
```
Al terminar el scroll con inercia:
  1. Calcular cuál es la pintura más cercana al centro del panel
  2. Calcular el offset necesario para centrarla exactamente
  3. Aplicar ese offset con DOTween (o iTween) con ease: OutBack
     (slight overshoot da sensación de "encaje físico")
```

---

## 12. Resumen del Sistema

```
┌─────────────────────────────────────────────────────────┐
│              GALERÍA GRID RADIAL — FLUJO COMPLETO        │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  ENTRADA AL MENÚ                                         │
│    → Panel plano aparece a 70cm frente al usuario        │
│    → Grid centrado en fila 7, columna 8                  │
│    → Centro nítido, blur radial hacia los bordes         │
│                                                          │
│  NAVEGAR                                                 │
│    → Palma abierta hacia panel = modo navegación activo  │
│    → Mover mano = grid se desplaza con inercia           │
│    → Soltar = grid hace snap a la pintura más cercana    │
│                                                          │
│  SELECCIONAR                                             │
│    → Pinch en pintura de zona central (3×3 o anillo B)   │
│    → Pintura se expande in-place, grid se oscurece       │
│    → Aparecen detalles + 3 botones de dificultad         │
│    → Pinch en dificultad = inicia el puzzle              │
│    → Pinch en ✕ = pintura colapsa, grid vuelve           │
│                                                          │
│  GESTOS (sin ambigüedad)                                 │
│    PALMA ABIERTA  →  navegar                             │
│    PINCH          →  seleccionar / confirmar             │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

---

---

## 13. Setup en Unity Editor

### Paso 1 — Crear el objeto raíz

En el panel **Hierarchy** de la escena `Main.unity`:

```
Click derecho → Create Empty
Renombrar → "RadialGridGallery"
```

Con el objeto seleccionado, en el **Inspector** agregar los dos scripts:
```
Add Component → RadialGridGalleryController
Add Component → RadialGridInputHandler
```

---

### Paso 2 — Crear GridRoot

Dentro de `RadialGridGallery`:
```
Click derecho sobre RadialGridGallery → Create Empty
Renombrar → "GridRoot"
```

Dejar `Position = (0, 0, 0)`, `Rotation = (0, 0, 0)`, `Scale = (1, 1, 1)`.  
No necesita ningún componente adicional — las celdas se crean aquí en runtime.

---

### Paso 3 — Crear DimmingOverlay

El DimmingOverlay es un Quad (plano 3D plano) que oscurece el grid cuando el usuario selecciona una pintura.

**Crear el Quad:**
```
Click derecho sobre RadialGridGallery → 3D Object → Quad
Renombrar → "DimmingOverlay"
```

**Transform:**
```
Position: (0, 0, -0.05)   ← ligeramente frente al grid para tapar las celdas
Rotation: (0, 0, 0)
Scale:    (2, 2, 1)        ← cubre el área visible de 1m de diámetro con margen
```

**Crear el material semiopaco:**
```
Panel Project → Click derecho → Create → Material
Renombrar → "DimmingMaterial"

En el Inspector del material:
  Shader:        Universal Render Pipeline/Unlit
  Surface Type:  Transparent
  Base Color:    negro (R=0, G=0, B=0)
  Alpha:         150  (de 255) — equivale a ~60% de opacidad
```

Arrastrar `DimmingMaterial` al campo **Material** del Quad en el Inspector.

Finalmente, **desactivar el GameObject**:
```
Inspector → desmarcar el checkbox junto al nombre "DimmingOverlay"
```

---

### Paso 4 — Crear DetailPanel

El DetailPanel es un Canvas en World Space que muestra la info de la pintura seleccionada y los botones de dificultad.

**Crear el Canvas:**
```
Click derecho sobre RadialGridGallery → UI → Canvas
Renombrar → "DetailPanel"

En el Inspector del Canvas:
  Render Mode:   World Space
  Scale:         (0.001, 0.001, 0.001)   ← 1 pixel = 1 mm en mundo
  Width:         500    (= 50 cm)
  Height:        620    (= 62 cm)
  Position:      (0, 0, 0)   ← el controlador lo reposiciona en runtime
```

> **Canvas Scaler**: cuando el Canvas está en World Space, Unity pone el Canvas Scaler
> automáticamente en modo `World` y bloquea el dropdown — es correcto, no hay nada que cambiar.
> El tamaño se controla por el Scale del Rect Transform (0.001) que ya está bien.

> **"Upgrade to OVROverlayCanvas"**: es una sugerencia del SDK de Meta para mejorar
> nitidez visual. Ignorarlo por ahora — no afecta el funcionamiento.

**Crear los hijos del Canvas** (todos con Click derecho sobre DetailPanel):

```
1. UI → Image
   Renombrar → "Background"
   Rect Transform: stretch a todo el panel (Anchor: stretch-stretch)
   Color: negro con alpha ~200

2. UI → Raw Image
   Renombrar → "ArtworkImage"
   Rect Transform: parte superior del panel
   Tamaño sugerido: 460 × 300 px  (posición aprox. Y = 140)

3. UI → Text - TextMeshPro
   Renombrar → "TitleText"
   Font Size: 28,  Bold
   Posición: debajo de ArtworkImage
   Texto de ejemplo: "La noche estrellada"

4. UI → Text - TextMeshPro
   Renombrar → "AuthorText"
   Font Size: 22
   Posición: debajo de TitleText
   Texto de ejemplo: "Vincent van Gogh"

5. UI → Text - TextMeshPro
   Renombrar → "DescriptionText"
   Font Size: 18,  Color gris claro
   Overflow: Overflow o Truncate
   Posición: debajo de AuthorText
   Tamaño sugerido: 460 × 80 px  (2-3 líneas de descripción)

6. UI → Button - TextMeshPro  (repetir 3 veces)
   Renombrar → "EasyButton", "NormalButton", "HardButton"
   Distribuir horizontalmente en la parte inferior del panel
   Tamaño sugerido: 140 × 70 px cada uno
   Texto inicial: cualquiera — el controlador lo sobreescribe en runtime
   con el número de piezas de cada obra, por ejemplo: "22 Pieces", "64 Pieces"

7. UI → Button - TextMeshPro
   Renombrar → "CloseButton"
   Texto: "✕"
   Posición: esquina superior derecha del panel
```

**Desactivar el GameObject** DetailPanel al terminar:
```
Inspector → desmarcar el checkbox junto al nombre "DetailPanel"
```

---

### Paso 5 — Asignar referencias en el Inspector

Seleccionar el objeto `RadialGridGallery` y en el componente **RadialGridGalleryController** asignar:

| Campo | Qué arrastrar |
|---|---|
| `Grid Root` | Empty GameObject **GridRoot** |
| `Input Handler` | Componente `RadialGridInputHandler` del mismo objeto |
| `Detail Panel` | GameObject **DetailPanel** |
| `Detail Artwork Image` | RawImage **ArtworkImage** dentro de DetailPanel |
| `Detail Title` | TextMeshProUGUI **TitleText** |
| `Detail Author` | TextMeshProUGUI **AuthorText** |
| `Detail Description` | TextMeshProUGUI **DescriptionText** |
| `Easy Button` | Button **EasyButton** |
| `Normal Button` | Button **NormalButton** |
| `Hard Button` | Button **HardButton** |
| `Close Button` | Button **CloseButton** |
| `Dimming Overlay` | GameObject **DimmingOverlay** |

En el componente **RadialGridInputHandler** asignar:

| Campo | Qué arrastrar |
|---|---|
| `Gallery` | Componente `RadialGridGalleryController` del mismo objeto |
| `Hand Tracking` | `HandTrackingInputController` de la escena |
| `Grid Root` | Empty GameObject **GridRoot** |

---

### Paso 6 — Conectar en GameBootstrap

Seleccionar el objeto `GameBootstrap` en la escena y en su Inspector:

| Campo | Qué arrastrar |
|---|---|
| `Radial Grid Gallery` | Componente `RadialGridGalleryController` del objeto **RadialGridGallery** |
| `Use Radial Gallery` | ✅ marcar para activar la nueva galería |

> **Nota**: con `Use Radial Gallery` desmarcado el juego sigue usando el menú clásico sin ningún cambio. Útil para comparar o volver atrás rápidamente.

---

*Documento generado tras análisis comparativo con feedback de múltiples expertos en MR UX.*  
*Decisiones de diseño documentadas con justificación explícita para referencia futura.*
