# Sistema de Tamaño Dinámico de Board y Piezas

## Fecha: 2026-03-02

## 📋 Resumen del Cambio

Se ha implementado un **sistema completamente nuevo** para calcular el tamaño del puzzle board y las piezas. 

**ANTES** (Sistema fijo):
- Tamaño de pieza = **FIJO** (5cm)
- Tamaño del board = número de piezas × 5cm
- **Problema**: En dificultades altas (Hard/Expert), el board era ENORME y tapaba los paneles laterales

**AHORA** (Sistema dinámico):
- Tamaño del board = **FIJO** (60cm de ancho)
- Tamaño de pieza = **DINÁMICO** (calculado según el board)
- **Resultado**: El board siempre cabe en el espacio disponible

---

## 🎯 Parámetros del Sistema

### Board Constraints (en PuzzleConfig):

```csharp
boardWidthM = 0.5f;         // 50cm de ancho (fijo)
boardMaxHeightM = 0.9f;     // 90cm de alto (máximo)
minPieceSizeM = 0.03f;      // 3cm por pieza (mínimo)
```

### Tray Constraints (en PieceScrollController):

```csharp
trayWidthM = 0.3f;          // 30cm de ancho (fijo)
trayHeightM = 0.4f;         // 40cm de alto (fijo)
minSpacingM = 0.02f;        // 2cm de espacio mínimo entre piezas
```

---

## 🔢 Ejemplos de Cálculo

### Imagen Cuadrada (Aspect Ratio 1:1):

#### Easy (64 piezas):
```
Grid: 8×8
Tamaño pieza: 50cm / 8 = 6.25cm ✅
Board: 50cm × 50cm
Cell tray: 6.25cm + 2cm = 8.25cm
Tray: 3 cols × 4 rows (30cm / 8.25cm × 40cm / 8.25cm)
Piezas visibles: 3×4 = 12 piezas (de 64 total) → Requiere scroll
```

#### Normal (144 piezas):
```
Grid: 12×12
Tamaño pieza: 50cm / 12 = 4.17cm ✅
Board: 50cm × 50cm
Cell tray: 4.17cm + 2cm = 6.17cm
Tray: 4 cols × 6 rows (30cm / 6.17cm × 40cm / 6.17cm)
Piezas visibles: 4×6 = 24 piezas (de 144 total) → Requiere scroll
```

#### Hard (256 piezas):
```
Grid: 16×16
Tamaño pieza: 50cm / 16 = 3.125cm ✅
Board: 50cm × 50cm
Cell tray: 3.125cm + 2cm = 5.125cm
Tray: 5 cols × 7 rows (30cm / 5.125cm × 40cm / 5.125cm)
Piezas visibles: 5×7 = 35 piezas (de 256 total) → Requiere scroll
```

#### Expert (512 piezas):
```
Grid: 22×23 (aproximado)
Tamaño pieza: 50cm / 22 = 2.27cm ❌ (< 3cm mínimo!)
→ Se aplica mínimo: 3cm
Cell tray: 3cm + 2cm = 5cm
Tray: 6 cols × 8 rows (30cm / 5cm × 40cm / 5cm)
Piezas visibles: 6×8 = 48 piezas (de 512 total) → Requiere scroll
Board real: 66cm × 69cm (excede ancho, se calcula desde altura)
```

---

### Imagen Panorámica (Aspect Ratio 16:9):

#### Normal (144 piezas):
```
Grid: 16×9 (se ajusta al aspecto)
Tamaño pieza: 50cm / 16 = 3.125cm ✅
Board: 50cm × 28.1cm
Cell tray: 3.125cm + 2cm = 5.125cm
Tray: 5 cols × 7 rows
```

#### Expert (512 piezas):
```
Grid: 30×17 (aproximado)
Tamaño pieza: 50cm / 30 = 1.67cm ❌ (< 3cm mínimo!)
→ Se aplica mínimo: 3cm
→ Board real: 90cm × 51cm (se calcula desde altura máxima)
Cell tray: 3cm + 2cm = 5cm
Tray: 6 cols × 8 rows
```

---

### Imagen Vertical (Aspect Ratio 9:16):

#### Normal (144 piezas):
```
Grid: 9×16
Tamaño pieza: 50cm / 9 = 5.56cm ✅
Board: 50cm × 88.9cm ✅ (< 90cm máximo)
Cell tray: 5.56cm + 2cm = 7.56cm
Tray: 3 cols × 5 rows
```

---

## ⚙️ Algoritmo de Cálculo

### Paso 1: Calcular Grid Óptimo

```csharp
aspectRatio = textureWidth / textureHeight;
rows = sqrt(targetCount / aspectRatio);
cols = rows * aspectRatio;
```

### Paso 2: Calcular Tamaño de Pieza (con constraints)

```csharp
// Intento 1: Basado en ancho fijo (50cm)
pieceSizeFromWidth = 0.5f / cols;
boardHeight = pieceSizeFromWidth * rows;

if (boardHeight > 0.9f) {
    // CONSTRAINT 1: Altura excede máximo
    pieceSize = 0.9f / rows;  // Calcular desde altura
} 
else if (pieceSizeFromWidth < 0.03f) {
    // CONSTRAINT 2: Pieza demasiado pequeña
    pieceSize = 0.03f;  // Usar tamaño mínimo
}
else {
    // Todo OK, usar tamaño calculado desde ancho
    pieceSize = pieceSizeFromWidth;
}
```

### Paso 3: Calcular Layout del Tray (con espaciado)

```csharp
// Cell = pieza + espacio mínimo
cellSize = pieceSize + 0.02f;  // +2cm de espacio

// Calcular cuántas caben en 30×40cm
cols = floor(0.3f / cellSize);
rows = floor(0.4f / cellSize);

// Ejemplo: pieza 4cm + 2cm espacio = 6cm cell
// Cols: 30cm / 6cm = 5 columnas
// Rows: 40cm / 6cm = 6 filas
// Total visible: 5×6 = 30 piezas por página
```

---

## 📊 Comparación ANTES vs AHORA

### Easy (64 piezas):

| Aspecto | Antes | Ahora |
|---------|-------|-------|
| Grid | 8×8 | 8×8 |
| Tamaño pieza | 5cm | 6.25cm |
| Board | 40cm × 40cm | 50cm × 50cm |
| Tray visible | 30 piezas (5×6 fijo) | 12 piezas (3×4 adaptativo) |
| ¿Cabe? | ✅ | ✅ |

### Normal (144 piezas):

| Aspecto | Antes | Ahora |
|---------|-------|-------|
| Grid | 12×12 | 12×12 |
| Tamaño pieza | 5cm | 4.17cm |
| Board | 60cm × 60cm | 50cm × 50cm |
| Tray visible | 30 piezas (5×6 fijo) | 24 piezas (4×6 adaptativo) |
| ¿Cabe? | ✅ | ✅ |

### Hard (256 piezas):

| Aspecto | Antes | Ahora |
|---------|-------|-------|
| Grid | 16×16 | 16×16 |
| Tamaño pieza | 5cm | 3.125cm |
| Board | **80cm × 80cm** | **50cm × 50cm** |
| Tray visible | 30 piezas (pegadas) | 35 piezas (5×7 con espacio) |
| ¿Cabe? | ❌ **Tapa paneles** | ✅ |

### Expert (512 piezas):

| Aspecto | Antes | Ahora |
|---------|-------|-------|
| Grid | 22×23 | 22×23 |
| Tamaño pieza | 5cm | 3cm (mínimo aplicado) |
| Board | **110cm × 115cm** | **66cm × 69cm** |
| Tray visible | 30 piezas (muy pegadas) | 48 piezas (6×8 con espacio) |
| ¿Cabe? | ❌ **ENORME!** | ✅ |

---

## 🎮 Sistema del Tray (Piece Scroll)

El tray también se adapta dinámicamente **con espaciado**:

### ANTES (Fijo, sin espacio):
```
5 columnas × 6 filas (hardcoded)
= 30 piezas visibles
Sin espacio entre piezas (pegadas)
```

### AHORA (Dinámico con espaciado):
```
Tray: 30cm × 40cm (fijo)
Espacio: 2cm mínimo entre piezas
Cell = tamaño_pieza + 2cm
Columnas = 30cm / cell
Filas = 40cm / cell
```

### Ejemplos:

**Easy (pieza 6.25cm):**
```
Cell: 6.25cm + 2cm = 8.25cm
Cols: 30 / 8.25 = 3
Rows: 40 / 8.25 = 4
Visible: 3×4 = 12 piezas (de 64 total) → Requiere scroll
```

**Normal (pieza 4.17cm):**
```
Cell: 4.17cm + 2cm = 6.17cm
Cols: 30 / 6.17 = 4
Rows: 40 / 6.17 = 6
Visible: 4×6 = 24 piezas (de 144 total) → Requiere scroll
```

**Hard (pieza 3.125cm):**
```
Cell: 3.125cm + 2cm = 5.125cm
Cols: 30 / 5.125 = 5
Rows: 40 / 5.125 = 7
Visible: 5×7 = 35 piezas (de 256 total) → Requiere scroll
```

**Expert (pieza 3cm):**
```
Cell: 3cm + 2cm = 5cm
Cols: 30 / 5 = 6
Rows: 40 / 5 = 8
Visible: 6×8 = 48 piezas (de 512 total) → Requiere scroll
```

---

## 🔧 Archivos Modificados

### 1. `PuzzleConfig.cs`
```csharp
[Header("Board Size Constraints (in meters)")]
public float boardWidthM = 0.5f;      // Ancho fijo (50cm)
public float boardMaxHeightM = 0.9f;  // Alto máximo (90cm)
public float minPieceSizeM = 0.03f;   // Pieza mínima (3cm)
```

### 2. `PuzzleBoard.cs`

**Nuevo método:**
```csharp
CalculateGridAndPieceSize(
    int targetCount, 
    int texWidth, 
    int texHeight,
    out int cols, 
    out int rows, 
    out float pieceSize)
```

**Métodos actualizados:**
- `CreateSlotsFromCount()` - Usa nuevo cálculo
- `CreateSlots()` - Usa nuevo cálculo
- `InitializeScroll()` - Pasa pieceSize al tray

**Variable añadida:**
```csharp
private float currentPieceSize = 0.05f;
```

### 3. `PieceScrollController.cs`

**Nuevos parámetros:**
```csharp
[SerializeField] private float trayWidthM = 0.3f;    // 30cm
[SerializeField] private float trayHeightM = 0.4f;   // 40cm
[SerializeField] private float minSpacingM = 0.02f;  // 2cm espacio
```

**Nuevo método:**
```csharp
CalculateTrayLayout(float pieceSize)
// Calcula: cellSize = pieceSize + minSpacingM
// Luego: cols = floor(trayWidth / cellSize)
```

**Método actualizado:**
```csharp
Initialize(List<Transform> pieces, float pieceSize)  // ← Nuevo parámetro
```

---

## ✅ Ventajas del Nuevo Sistema

1. ✅ **Board siempre cabe** - 50cm de ancho fijo
2. ✅ **No tapa paneles laterales** en ninguna dificultad
3. ✅ **Piezas más pequeñas = más difícil** (lógico)
4. ✅ **Respeta aspect ratio** de cada pintura
5. ✅ **Tray se adapta** automáticamente al tamaño de pieza
6. ✅ **Espaciado entre piezas** (2cm mínimo) - no están pegadas
7. ✅ **Scroll automático** - solo muestra lo que cabe por página
8. ✅ **Constraints inteligentes** (min 3cm pieza, max 90cm alto)

---

## 🐛 Testing

### En Unity:

1. **Configurar PuzzleConfig**:
   - Abrir `Assets/ArtUnbound/Data/PuzzleConfig.asset`
   - Verificar: `boardWidthM = 0.5`, `boardMaxHeightM = 0.9`, `minPieceSizeM = 0.03`

2. **Probar cada dificultad**:
   - Easy (64): Debe verse grande (~6cm piezas), 12 visibles en tray + scroll
   - Normal (144): Debe verse medio (~4cm piezas), 24 visibles en tray + scroll
   - Hard (256): Debe caber perfectamente (~3cm piezas), 35 visibles + scroll
   - Expert (512): Debe caber (3cm piezas mínimas), 48 visibles + scroll

3. **Verificar espaciado en tray**:
   - Debe haber **2cm de espacio** entre cada pieza
   - NO deben estar pegadas
   - Debe haber scroll si no caben todas en una página

4. **Verificar en logs**:
```
[PuzzleBoard] Optimal: pieceSize=6.25cm, boardSize=50.0x50.0cm
[PuzzleBoard] Grid: target=64, actual=64 (8x8), aspectRatio=1.00
[PieceScrollController] Tray layout: 3 cols × 4 rows (pieceSize=6.25cm + spacing=2.0cm = cell=8.25cm)
```

---

## 🚀 Próximos Pasos (Opcional)

1. **Ajustar posición del tray** si 45cm de distancia no es suficiente
2. **Refinar constraints** si 3cm mínimo es muy pequeño
3. **Probar con imágenes de diferentes aspect ratios** (vertical, panorámico)
4. **Ajustar distancia del jugador** si las piezas Expert son muy pequeñas

---

**Fin del documento**
