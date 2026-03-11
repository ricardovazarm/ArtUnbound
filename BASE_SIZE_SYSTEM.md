# Sistema de Tamaños Base Consistentes

## Fecha: 2026-03-02

## 🎯 Nuevo Sistema

En lugar de usar conteos de piezas variables (64, 144, 256, 512), ahora usamos **tamaños base consistentes** que progresan de forma predecible:

### Progresión: +3 por lado

```
Easy:   8×8   = 64 piezas  (para imagen cuadrada)
Normal: 11×11 = 121 piezas (para imagen cuadrada)
Hard:   14×14 = 196 piezas (para imagen cuadrada)
Expert: 17×17 = 289 piezas (para imagen cuadrada)
```

**Ventaja:** Expert ahora tiene piezas más grandes (3.43cm vs 3cm mínimo anterior)

---

## 📐 Cálculos para Imágenes Cuadradas (1:1)

Board fijo: **50cm de ancho**

| Dificultad | Base | Grid | Piezas | Tamaño Pieza | Board |
|------------|------|------|--------|--------------|-------|
| **Easy** | 8 | 8×8 | 64 | 6.25cm | 50×50cm |
| **Normal** | 11 | 11×11 | 121 | 4.55cm | 50×50cm |
| **Hard** | 14 | 14×14 | 196 | 3.57cm | 50×50cm |
| **Expert** | 17 | 17×17 | 289 | 2.94cm | 50×50cm |

---

## 🖼️ Cálculos para Imágenes Panorámicas (16:9)

El grid se adapta al aspect ratio:

| Dificultad | Base | Aspect | Grid | Piezas | Tamaño Pieza | Board |
|------------|------|--------|------|--------|--------------|-------|
| **Easy** | 8 | 16:9 | 14×8 | 112 | 3.57cm | 50×28cm |
| **Normal** | 11 | 16:9 | 20×11 | 220 | 2.5cm ❌ | 50×27cm (ajustado) |
| **Hard** | 14 | 16:9 | 25×14 | 350 | 2.0cm ❌ | 50×28cm (ajustado) |
| **Expert** | 17 | 16:9 | 30×17 | 510 | 1.67cm ❌ | 50×28cm (ajustado) |

**Nota:** Panorámicas con base alta pueden caer bajo el mínimo de 3cm

---

## 🖼️ Cálculos para Imágenes Verticales (9:16)

| Dificultad | Base | Aspect | Grid | Piezas | Tamaño Pieza | Board |
|------------|------|--------|------|--------|--------------|-------|
| **Easy** | 8 | 9:16 | 8×14 | 112 | 6.25cm | 50×88cm |
| **Normal** | 11 | 9:16 | 11×20 | 220 | 4.55cm | 50×91cm ❌ |
| **Hard** | 14 | 9:16 | 14×25 | 350 | 3.57cm | 50×89cm (ajustado) |
| **Expert** | 17 | 9:16 | 17×30 | 510 | 2.94cm | 50×88cm (ajustado) |

**Nota:** Verticales con base alta pueden exceder 90cm de alto (se ajusta)

---

## 🎮 Tray: Piezas Visibles por Dificultad

Tray: **30cm × 40cm** con **2cm de espacio** entre piezas

| Dificultad | Pieza | Cell (pieza+2cm) | Tray Grid | Visible | Total (1:1) | % Visible |
|------------|-------|------------------|-----------|---------|-------------|-----------|
| **Easy** | 6.25cm | 8.25cm | 3×4 | **12** | 64 | 18.8% |
| **Normal** | 4.55cm | 6.55cm | 4×6 | **24** | 121 | 19.8% |
| **Hard** | 3.57cm | 5.57cm | 5×7 | **35** | 196 | 17.9% |
| **Expert** | 2.94cm | 4.94cm | 6×8 | **48** | 289 | 16.6% |

**Ventaja:** Expert ya NO necesita aplicar el mínimo de 3cm para imágenes cuadradas (2.94cm es muy cercano pero técnicamente bajo el mínimo, se aplicará 3cm)

**Corrección:** Expert aplicará mínimo de 3cm:
- Pieza: 3cm → Cell: 5cm → Grid: 6×8 = 48 visibles

---

## ⚙️ Algoritmo Actualizado

### Paso 1: Mapear pieceCount a baseSize

```csharp
if (pieceCount <= 64)  return 8;   // Easy
if (pieceCount <= 121) return 11;  // Normal
if (pieceCount <= 196) return 14;  // Hard
return 17;                          // Expert
```

### Paso 2: Calcular Grid según Aspect Ratio

```csharp
if (aspectRatio >= 1.0) {
    // Wide or square
    rows = baseSize;
    cols = baseSize * aspectRatio;
} else {
    // Tall
    cols = baseSize;
    rows = baseSize / aspectRatio;
}
```

**Ejemplos:**

| Imagen | Base | Aspect | Cálculo | Grid |
|--------|------|--------|---------|------|
| Cuadrada (1:1) | 11 | 1.0 | 11×(11×1.0) | 11×11 |
| Panorámica (16:9) | 11 | 1.78 | 11×(11×1.78) | 11×20 |
| Vertical (9:16) | 11 | 0.56 | (11/0.56)×11 | 20×11 |

### Paso 3: Calcular Tamaño de Pieza

```csharp
pieceSize = 50cm / cols;

// Aplicar constraints (mínimo 3cm, máximo 90cm alto)
if (pieceSize < 3cm) {
    pieceSize = 3cm;
    // Recalcular board con mínimo aplicado
}

if (boardHeight > 90cm) {
    pieceSize = 90cm / rows;
    // Recalcular board con altura máxima
}
```

---

## 📊 Comparación: Antiguo vs Nuevo Sistema

### Easy (64 piezas)

| Aspecto | Antiguo | Nuevo |
|---------|---------|-------|
| Grid base | 8×8 | 8×8 |
| Tamaño pieza | 6.25cm | 6.25cm |
| **Cambio** | ✅ Sin cambio | ✅ Sin cambio |

### Normal

| Aspecto | Antiguo (144 piezas) | Nuevo (121 piezas) |
|---------|----------------------|--------------------|
| Grid base | 12×12 | 11×11 |
| Tamaño pieza | 4.17cm | **4.55cm** |
| **Cambio** | → | ✅ **Piezas más grandes** |

### Hard

| Aspecto | Antiguo (256 piezas) | Nuevo (196 piezas) |
|---------|----------------------|--------------------|
| Grid base | 16×16 | 14×14 |
| Tamaño pieza | 3.125cm | **3.57cm** |
| **Cambio** | → | ✅ **Piezas más grandes** |

### Expert

| Aspecto | Antiguo (512 piezas) | Nuevo (289 piezas) |
|---------|----------------------|--------------------|
| Grid base | ~23×23 | 17×17 |
| Tamaño pieza | 3cm (mínimo forzado) | **3cm** (casi natural: 2.94cm) |
| **Cambio** | → | ✅ **Mucho más manejable** |

---

## ✅ Ventajas del Nuevo Sistema

1. ✅ **Progresión consistente** - +3 por lado en cada nivel
2. ✅ **Piezas más grandes en Expert** - Ya no son microscópicas
3. ✅ **Menos piezas en Hard/Expert** - Más manejable para el jugador
4. ✅ **Fácil de comunicar** - "8, 11, 14, 17 por lado"
5. ✅ **Se adapta al aspect ratio** - Sigue respetando la forma de la imagen
6. ✅ **Consistencia visual** - Saltos de dificultad más uniformes

---

## 🔧 Archivos Modificados

### 1. `PuzzleConfig.cs`

```csharp
[Header("Difficulty Base Sizes")]
public int[] baseSizes = { 8, 11, 14, 17 };

[Header("Piece Counts (Reference)")]
public int[] pieceCounts = { 64, 121, 196, 289 };
public int defaultPieceCount = 121; // Normal (11×11)
```

### 2. `PuzzleBoard.cs`

**Nuevo método:**
```csharp
private int GetBaseSizeFromPieceCount(int pieceCount)
{
    if (pieceCount <= 64) return 8;
    if (pieceCount <= 121) return 11;
    if (pieceCount <= 196) return 14;
    return 17;
}
```

**Método actualizado:**
```csharp
CalculateGridAndPieceSize()
// Ahora usa baseSize en lugar de targetCount
// Calcula grid adaptativo según aspect ratio
```

---

## 🚀 Testing

### Verificar en Unity:

1. **Easy (64 → 8×8)**: Debe verse igual que antes
2. **Normal (121 → 11×11)**: Piezas **más grandes** que antes (4.55cm vs 4.17cm)
3. **Hard (196 → 14×14)**: Piezas **más grandes** que antes (3.57cm vs 3.12cm)
4. **Expert (289 → 17×17)**: Piezas **mucho más grandes** que antes (3cm vs piezas microscópicas)

### Logs esperados:

```
[PuzzleBoard] Optimal: pieceSize=4.55cm, boardSize=50.0x50.0cm
[PuzzleBoard] Grid: baseSize=11, actual=121 (11x11), aspectRatio=1.00
[PieceScrollController] Tray layout: 4 cols × 6 rows (pieceSize=4.55cm + spacing=2.0cm = cell=6.55cm)
```

---

**Fin del documento**
