---
name: calc-piezas
description: Calcula el grid de piezas y dimensiones del board que generara una pintura en Art Unbound. Usalo para previsualizar (curacion) cuantas piezas tendra una imagen, que tan grandes seran y de que dimensiones quedara el board en cada dificultad. NO escribe los valores en ningun lado: con el nuevo algoritmo el conteo lo deriva PuzzleBoard en runtime, no necesita estar pre-cacheado.
argument-hint: <ruta_imagen>
allowed-tools: [Bash, Read]
---

# Calculadora de Piezas — Art Unbound

El usuario invoco este skill con: $ARGUMENTS

Esto es una **previsualizacion** del rompecabezas que generara una imagen al meterla al catalogo. El conteo y dimensiones se calculan en runtime por `PuzzleBoard.CalculateGridAndPieces`, no se almacenan en el .asset. Usa este skill para curacion: decidir si una imagen es buena candidata, ver si Hard genera demasiadas piezas, etc.

## Paso 1 — Obtener dimensiones de la imagen

```bash
python -c "from PIL import Image; img = Image.open('$ARGUMENTS'); print(img.size)"
```

Si PIL no esta disponible, usa el tool Read para inferir dimensiones del header de la imagen.

## Paso 2 — Aplicar el algoritmo (mirror exacto de `PuzzleBoard.CalculateGridAndPieces`)

**Configuracion en `PuzzleConfig.asset`:**
- `boardMaxSizeM`   = 0.60 m  → caja maxima cuadrada (lado de 60 cm)
- `maxPieceSizeM`   = 0.05 m  → pieza Easy (no mas grande que esto)
- `minPieceSizeM`   = 0.03 m  → pieza Hard (no mas chica que esto)
- Normal target    = (max + min) / 2 = 0.04 m

**Algoritmo:**

```
aspectRatio = ancho_px / alto_px

# Paso A — board dentro de la caja maxima respetando ratio
SI aspectRatio >= 1 (landscape o cuadrado):
    boardWidth  = boardMaxSizeM
    boardHeight = boardMaxSizeM / aspectRatio
SINO (portrait):
    boardHeight = boardMaxSizeM
    boardWidth  = boardMaxSizeM * aspectRatio

# Paso B — para cada dificultad, target piece size:
target[Easy]   = maxPieceSizeM           (0.05)
target[Normal] = (max + min) / 2         (0.04)
target[Hard]   = minPieceSizeM           (0.03)

# Paso C — para cada eje (cols por boardWidth, rows por boardHeight):
approx = boardSpan / target
floor  = max(2, floor(approx))
ceil   = max(2, ceil(approx))
SI floor == ceil: count = floor
SINO segun dificultad:
    Easy   → ceil   (round UP, pieza ≤ max)
    Hard   → floor  (round DOWN, pieza ≥ min)
    Normal → la que produzca pieza mas cercana al target

# Paso D — dimensiones reales de pieza
pieceWidth  = boardWidth  / cols
pieceHeight = boardHeight / rows

totalPiezas = cols * rows
```

## Paso 3 — Presentar resultados

**Header con info de la imagen:**
- Ruta y nombre.
- Dimensiones en pixeles (ancho × alto).
- Aspect ratio (4 decimales).
- Orientacion: landscape (ratio > 1), portrait (ratio < 1) o cuadrado.

**Dimensiones del board (mismas para las 3 dificultades, dependen solo del aspect ratio):**
- boardWidth × boardHeight en cm.

**Tabla de las 3 dificultades:**

| Dificultad | Grid (cols × rows) | Pieza (W × H) en cm | Total piezas |
|------------|--------------------|---------------------|--------------|
| Easy       | C × R              | W.WW × H.HH         | N            |
| Normal     | C × R              | W.WW × H.HH         | N            |
| Hard       | C × R              | W.WW × H.HH         | N            |

**Notas a agregar al final:**
- Si `pieceWidth` y `pieceHeight` difieren mas del 10%: la pieza es notablemente rectangular, mencionalo.
- Si Easy genera <30 piezas: imagen muy chica/cuadrada, puede sentirse trivial.
- Si Hard genera >300 piezas: imagen muy grande/panoramica, puede sentirse abrumador.
- Si Hard cae al limite de 3 cm exactos en algun eje: vale la pena saber que no hay margen.

## Importante

**NO** sugerir copiar valores a `ArtworkDefinition.asset` — esos campos (`pieceCountEasy/Normal/Hard`) ya no existen en el modelo. Si el usuario pregunta donde meter los numeros, responder que no hay donde: el sistema los recalcula al iniciar cada puzzle.
