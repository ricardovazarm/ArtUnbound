# Actualización de Conteos de Piezas

## Fecha: 2026-03-02

## 📋 Cambio Simple

Se actualizaron los **conteos de piezas objetivo** para una progresión más consistente:

### Antes:
```
Easy:   64 piezas  (8×8)
Normal: 144 piezas (12×12)
Hard:   256 piezas (16×16)
Expert: 512 piezas (23×23)
```

### Ahora:
```
Easy:   64 piezas  (8×8)    [Sin cambio]
Normal: 121 piezas (11×11)  [Menos piezas]
Hard:   196 piezas (14×14)  [Menos piezas]
Expert: 289 piezas (17×17)  [Mucho menos piezas]
```

**Progresión:** Aproximadamente +3 filas/columnas por nivel (8 → 11 → 14 → 17)

---

## 🎯 Ventajas

1. ✅ **Expert mucho más jugable** - 289 vs 512 piezas (casi la mitad)
2. ✅ **Piezas más grandes** - Especialmente en Hard/Expert
3. ✅ **Progresión más suave** - Saltos más equilibrados entre niveles

---

## 📐 Tamaños Calculados (Imágenes Cuadradas 1:1)

Board fijo: **50cm de ancho**

| Dificultad | Target | Grid Aprox | Piezas Reales | Tamaño Pieza | Board |
|------------|--------|------------|---------------|--------------|-------|
| **Easy** | 64 | ~8×8 | 64 | 6.25cm | 50×50cm |
| **Normal** | 121 | ~11×11 | 121 | 4.55cm | 50×50cm |
| **Hard** | 196 | ~14×14 | 196 | 3.57cm | 50×50cm |
| **Expert** | 289 | ~17×17 | 289 | 2.94cm | 50×50cm |

**Nota:** El grid real se calcula dinámicamente según el aspect ratio de cada imagen. Los valores mostrados son para imágenes cuadradas (1:1).

---

## ⚙️ Algoritmo (Sin Cambios)

El algoritmo de cálculo **sigue siendo el mismo**:

```csharp
// 1. Calcular grid basado en targetCount y aspect ratio
rows = sqrt(targetCount / aspectRatio);
cols = rows * aspectRatio;

// 2. Calcular tamaño de pieza
pieceSize = 50cm / cols;

// 3. Aplicar constraints (min 3cm, max 90cm alto)
```

**Lo único que cambió:** Los números en `pieceCounts = { 64, 121, 196, 289 }`

---

## 🎮 Impacto en el Tray (30×40cm con 2cm espacio)

| Dificultad | Pieza | Cell | Grid | Visible | Total | % Visible |
|------------|-------|------|------|---------|-------|-----------|
| **Easy** | 6.25cm | 8.25cm | 3×4 | 12 | 64 | 18.8% |
| **Normal** | 4.55cm | 6.55cm | 4×6 | 24 | 121 | 19.8% |
| **Hard** | 3.57cm | 5.57cm | 5×7 | 35 | 196 | 17.9% |
| **Expert** | 2.94cm | 4.94cm | 6×8 | 48 | 289 | 16.6% |

---

## 🔧 Archivo Modificado

### `PuzzleConfig.cs`

```csharp
[Header("Piece Counts")]
public int[] pieceCounts = { 64, 121, 196, 289 };  // Antes: { 64, 144, 256, 512 }
public int defaultPieceCount = 121;                 // Antes: 144
```

**Eso es todo.** El resto del sistema funciona igual.

---

## 🐛 Testing

Al probar en Unity, deberías ver:

1. **Easy (64)**: Sin cambios - igual que antes
2. **Normal (121)**: Piezas más grandes (4.55cm vs 4.17cm)
3. **Hard (196)**: Piezas más grandes (3.57cm vs 3.12cm)
4. **Expert (289)**: MUCHO más manejable (289 vs 512 piezas)

---

**Fin del documento**
