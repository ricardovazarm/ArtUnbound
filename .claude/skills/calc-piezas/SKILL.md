---
name: calc-piezas
description: Calcula el grid de piezas para un rompecabezas de Art Unbound dada una imagen. Úsalo cuando el usuario pregunte cuántas piezas tendrá una imagen específica, qué grid generará, o mencione una ruta de imagen junto con "piezas", "rompecabezas", "grid" o "dificultad".
argument-hint: <ruta_imagen>
allowed-tools: [Bash, Read]
---

# Calculadora de Piezas — Art Unbound

El usuario invocó este skill con: $ARGUMENTS

## Instrucciones

### Paso 1 — Obtener dimensiones reales de la imagen

Usa Bash con Python para leer las dimensiones exactas en píxeles:

```bash
python -c "from PIL import Image; img = Image.open('$ARGUMENTS'); print(img.size)"
```

Si PIL no está disponible, intenta con:
```bash
python -c "import struct,zlib; print(open('$ARGUMENTS','rb').read())" 2>/dev/null
```

O como último recurso, usa el tool Read para ver la imagen visualmente e inferir las dimensiones aproximadas desde los metadatos que muestre.

### Paso 2 — Aplicar el algoritmo (de `PuzzleBoard.CalculateGridAndPieceSize`)

Usa las dimensiones obtenidas y aplica esto para **cada una de las 4 dificultades**:

**Configuración fija (PuzzleConfig.asset):**
- `boardWidth`    = 0.50 m — fijo
- `boardMaxHeight` = 0.90 m — máximo
- `minPieceSize`  = 0.03 m — mínimo
- Targets por dificultad: Easy=64, Normal=121, Hard=196, Expert=289

**Fórmulas:**
```
aspectRatio = ancho_px / alto_px

rows = round( sqrt(targetCount / aspectRatio) )   // mínimo 2
cols = round( rows * aspectRatio )                 // mínimo 2

pieceSizeFromWidth = boardWidth / cols
boardHeight = pieceSizeFromWidth * rows

SI boardHeight > 0.90:
    pieceSize = 0.90 / rows              → nota: "altura máx"
SINO SI pieceSizeFromWidth < 0.03:
    pieceSize = 0.03                     → nota: "pieza mínima"
SINO:
    pieceSize = pieceSizeFromWidth       → nota: normal

piezas_reales = cols × rows
board_cm = (pieceSize×cols)*100  ×  (pieceSize×rows)*100
```

### Paso 3 — Presentar resultados

Muestra primero la info de la imagen:
- Ruta, dimensiones (px), aspect ratio, orientación (portrait/landscape/cuadrada)

Luego la tabla de dificultades:

| Dificultad | Target | Grid     | Piezas reales | Tamaño pieza | Board         | Restricción |
|------------|--------|----------|---------------|--------------|---------------|-------------|
| Easy       | 64     | C × R    | N             | X.X cm       | WW × HH cm   |             |
| Normal     | 121    | C × R    | N             | X.X cm       | WW × HH cm   |             |
| Hard       | 196    | C × R    | N             | X.X cm       | WW × HH cm   |             |
| Expert     | 289    | C × R    | N             | X.X cm       | WW × HH cm   |             |

Si hay diferencia entre el target y las piezas reales, explica brevemente por qué (ajuste por aspect ratio).
