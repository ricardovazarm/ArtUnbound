# PuzzleHUDController - Panel In-Game (Zona Izquierda)

## Fecha: 2026-03-02

## 📋 Resumen

El `PuzzleHUDController` controla el **panel izquierdo** que se muestra durante el gameplay. Muestra información de la obra, progreso del puzzle, timer, y un botón para salir.

---

## 🎯 Diseño Visual (LEFT ZONE)

```
┌─────────────────────────────────┐
│                                 │
│  Título de la Obra              │
│  Autor                          │
│  Breve descripción...           │
│                                 │
│  ⏱️ 02:35                        │
│                                 │
│  🧩 45 / 130                    │
│  [████████░░░░] 34%            │
│                                 │
│  [  Salir  ]                   │
│                                 │
└─────────────────────────────────┘
```

**Notas de diseño:**
- Panel rotado similar al estilo del Main Menu
- Siempre visible durante el gameplay
- El botón "Salir" vuelve al Main Menu

---

## 🔧 Campos a Asignar en Inspector

### **En el GameObject `LeftZone` (o como se llame tu panel izquierdo):**

```
PuzzleHUDController (Script)
│
├── Panel
│   └── hudPanel: LeftZone
│       └── El GameObject completo del panel izquierdo
│
├── Artwork Info
│   ├── artworkTitleText: TextMeshProUGUI
│   │   └── Muestra el título de la obra
│   ├── artworkArtistText: TextMeshProUGUI
│   │   └── Muestra el autor
│   └── artworkDescriptionText: TextMeshProUGUI
│       └── Muestra la descripción
│
├── Timer Display
│   ├── timerText: TextMeshProUGUI
│   │   └── Muestra el tiempo en formato "MM:SS"
│   └── timerIcon: Image (opcional)
│       └── Icono de reloj
│
├── Progress Display
│   ├── piecesText: TextMeshProUGUI
│   │   └── Muestra "45 / 130"
│   ├── progressSlider: Slider
│   │   └── Barra visual de progreso
│   └── progressFill: Image
│       └── La imagen "Fill" del slider
│
├── Buttons
│   └── quitButton: Button
│       └── Botón "Salir" al Main Menu
│
├── References
│   └── timerController: PuzzleTimerController
│       └── IMPORTANTE: Referencia al timer del juego
│
└── Visual Feedback
    └── progressColor: Color
        └── Color de la barra de progreso (default: azul)
```

---

## ⚙️ Comportamiento del Sistema

### 1. **Inicialización**

Cuando empieza el juego:
```csharp
puzzleHUD.Initialize(pieceCount, true);
puzzleHUD.SetArtworkInfo(title, artist, description);
puzzleHUD.Show();
```

### 2. **Actualización de Progreso**

Cada vez que se coloca una pieza:
```csharp
puzzleHUD.UpdatePiecesPlaced(correctPieces);
```

### 3. **Timer Automático**

El timer se actualiza automáticamente en `Update()`:
- Lee el tiempo del `timerController`
- Lo muestra en formato `MM:SS`

### 4. **Botón Salir**

Cuando el jugador presiona "Salir":
- Dispara el evento `OnExitRequested`
- `GameBootstrap` lo captura y ejecuta `QuitToMenu()`
- Guarda el progreso automáticamente
- Vuelve al Main Menu

---

## 🗑️ Elementos ELIMINADOS (vs Versión Anterior)

### ❌ Mini Preview:
```csharp
// ELIMINADOS:
[SerializeField] private GameObject miniPreviewPanel;
[SerializeField] private Image miniPreviewImage;
[SerializeField] private Button togglePreviewButton;
private bool isMiniPreviewVisible;

// Métodos eliminados:
SetPreviewImage(Texture2D texture)
SetPreviewImage(Sprite sprite)
ToggleMiniPreview()
SetMiniPreviewVisible(bool visible)
```

**Razón**: Ya no se necesita porque el modo ayuda (siluetas) está siempre activo.

### ❌ Reposition Button:
```csharp
// ELIMINADO:
[SerializeField] private Button repositionButton;

// Método eliminado:
SetRepositionButtonVisible(bool visible)
OnRepositionRequested()
```

**Razón**: La posición inicial del board siempre es cómoda y no requiere ajustes manuales.

---

## ✅ Elementos ESENCIALES (Mantener)

### Panel Básico:
- `hudPanel` - Panel izquierdo completo

### Información de la Obra:
- `artworkTitleText` - Título
- `artworkArtistText` - Autor  
- `artworkDescriptionText` - Descripción

### Timer:
- `timerText` - Tiempo en formato MM:SS
- `timerController` - **CRÍTICO**: Referencia al PuzzleTimerController

### Progreso:
- `piecesText` - "45 / 130"
- `progressSlider` - Barra visual
- `progressFill` - Color de la barra

### Control:
- `quitButton` - Botón "Salir"

---

## 🎮 Flujo de Usuario

```
Jugador empieza puzzle
         ↓
HUD aparece (panel izquierdo)
         ↓
Muestra:
├─ Título, Autor, Descripción
├─ Timer contando (00:00 → XX:XX)
└─ Progreso (0 / 130 → 45 / 130)
         ↓
Jugador coloca piezas
├─ HUD actualiza progreso automáticamente
├─ Timer sigue contando
└─ Barra visual se llena
         ↓
Jugador termina o quiere salir
├─ Completa puzzle → PostGame panel aparece (derecha)
└─ Presiona "Salir" → Guarda y vuelve al menú
```

---

## 🐛 Debugging

### Verificar en Consola:

```
[GameBootstrap] HUD initialized with actual piece count: 130
[GameBootstrap] Updating HUD after restore: 45/130 correct pieces
```

### Verificar en Inspector:

- `hudPanel` debe estar **asignado**
- `timerController` debe estar **asignado** (crítico)
- Todos los TextMeshProUGUI deben estar **asignados**
- `quitButton` debe estar **asignado**

### Verificar en Jerarquía (durante Play):

- `LeftZone` debe estar **activo** durante gameplay
- `timerText` debe mostrar tiempo incrementando
- `piecesText` debe actualizarse cuando colocas piezas
- `progressSlider.value` debe ir de 0 a 1

---

## 📊 Estructura Simplificada

**ANTES** (versión anterior):
```
PuzzleHUDController
├── Artwork Info
├── Timer
├── Progress
├── Quit Button
├── Reposition Button  ❌
├── Mini Preview Panel ❌
├── Toggle Preview Button ❌
└── Timer Controller
```

**AHORA** (versión actual):
```
PuzzleHUDController
├── Artwork Info
├── Timer
├── Progress
├── Quit Button
└── Timer Controller  ✅ ESENCIAL
```

**Reducción**: De 10 elementos a 5 elementos esenciales (50% más simple)

---

## 🚀 Próximos Pasos

1. **En Unity:**
   - Crear el panel izquierdo con diseño rotado
   - Asignar todos los campos en el Inspector
   - **IMPORTANTE**: Asignar `timerController` (busca el GameObject con `PuzzleTimerController`)
   - Probar durante gameplay

2. **Testing:**
   - Verificar que el timer cuenta correctamente
   - Verificar que el progreso se actualiza al colocar piezas
   - Verificar que "Salir" vuelve al menú y guarda progreso

---

**Fin del documento**
