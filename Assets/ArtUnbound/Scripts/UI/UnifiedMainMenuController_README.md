# Guía de Implementación: UnifiedMainMenuController

## ✅ Script Creado: `UnifiedMainMenuController.cs`

---

## 📋 Estructura del Canvas en Unity

### Canvas Principal: `MainMenuCanvas_Curved`
```
MainMenuCanvas_Curved (Canvas - World Space)
├─ UnifiedMainMenuPanel (GameObject)
│   ├─ LeftZone (GameObject) - Configuración
│   │   ├─ ConfigTitle (TextMeshProUGUI): "Configuración"
│   │   ├─ MusicVolumeLabel (TextMeshProUGUI): "Música"
│   │   ├─ MusicVolumeSlider (Slider)
│   │   ├─ SoundVolumeLabel (TextMeshProUGUI): "Sonidos"
│   │   ├─ SoundVolumeSlider (Slider)
│   │   ├─ TutorialLabel (TextMeshProUGUI): "Tutorial"
│   │   ├─ TutorialToggle (Toggle)
│   │   └─ GlobalStatsText (TextMeshProUGUI): "0/0 obras completadas"
│   │
│   ├─ CenterZone (GameObject) - Catálogo
│   │   ├─ FiltersPanel (GameObject)
│   │   │   ├─ FilterAllButton (Button): "Todas"
│   │   │   ├─ FilterInProgressButton (Button): "Por Completar"
│   │   │   └─ FilterCompletedButton (Button): "Mi Galería"
│   │   │
│   │   └─ CatalogScrollView (ScrollRect)
│   │       └─ CatalogGrid (GameObject con Grid Layout Group)
│   │           └─ (Aquí se instancian las tarjetas dinámicamente)
│   │
│   └─ RightZone (GameObject) - Detalle
│       ├─ DetailArtworkImage (Image)
│       ├─ DetailTitleText (TextMeshProUGUI)
│       ├─ DetailArtistText (TextMeshProUGUI)
│       ├─ DetailDescriptionText (TextMeshProUGUI)
│       └─ DifficultyButtons (GameObject)
│           ├─ EasyButton (Button)
│           │   └─ EasyButtonText (TextMeshProUGUI): "Fácil"
│           ├─ NormalButton (Button)
│           │   └─ NormalButtonText (TextMeshProUGUI): "Normal"
│           ├─ HardButton (Button)
│           │   └─ HardButtonText (TextMeshProUGUI): "Difícil"
│           └─ ExpertButton (Button)
│               └─ ExpertButtonText (TextMeshProUGUI): "Experto"
```

---

## 🎴 Prefab: ArtworkCard

Crea un prefab llamado `ArtworkCardPrefab.prefab` con esta estructura:

```
ArtworkCard (GameObject con Button y componente ArtworkCard)
├─ ThumbnailImage (Image) - La miniatura de la obra
├─ TitleText (TextMeshProUGUI) - Nombre de la obra
├─ CompletionFrame (Image) - Marco dorado/verde para obras completadas (SetActive = false por default)
└─ ProgressText (TextMeshProUGUI) - "45%" (SetActive = false por default)
```

### Configuración del Prefab:
- **Tamaño recomendado**: 200x250 pixels
- **Button Component**: Necesario para hacer clic
- **ArtworkCard Component**: IMPORTANTE - Agregar el script `ArtworkCard.cs` al GameObject raíz
- **Asignar referencias en el componente ArtworkCard**:
  - `Thumbnail Image` → ThumbnailImage (Image hijo)
  - `Title Text` → TitleText (TextMeshProUGUI hijo)
  - `Completion Frame` → CompletionFrame (GameObject hijo, desactivado por default)
  - `Progress Text` → ProgressText (TextMeshProUGUI hijo, desactivado por default)
- **CompletionFrame**: Debe ser hijo del ArtworkCard y estar desactivado por default

---

## 🔧 Configuración del Grid Layout Group

En `CatalogGrid`, agrega un **Grid Layout Group** con:
```
Cell Size: 200 x 250
Spacing: 10 x 10
Start Axis: Horizontal
Child Alignment: Upper Left
Constraint: Fixed Column Count = 3 (o el que prefieras)
```

---

## 🎨 Configuración de Colores (opcional)

Puedes ajustar estos colores en el Inspector del `UnifiedMainMenuController`:

- **Selected Filter Color**: Azul brillante (0.2, 0.6, 1, 1)
- **Normal Filter Color**: Gris (0.5, 0.5, 0.5, 1)
- **Selected Difficulty Color**: Verde (0.3, 0.8, 0.3, 1)
- **Normal Difficulty Color**: Azul (0.2, 0.6, 1, 1)

---

## 🔗 Referencias a Asignar en Inspector

En el componente `UnifiedMainMenuController`, asigna:

### Main Panel:
- `mainPanel` → UnifiedMainMenuPanel

### Left Zone:
- `musicVolumeSlider` → MusicVolumeSlider
- `soundVolumeSlider` → SoundVolumeSlider
- `tutorialToggle` → TutorialToggle
- `globalStatsText` → GlobalStatsText

### Center Zone:
- `catalogGrid` → CatalogGrid (el GameObject con Grid Layout Group)
- `catalogScrollRect` → CatalogScrollView
- `filterAllButton` → FilterAllButton
- `filterInProgressButton` → FilterInProgressButton
- `filterCompletedButton` → FilterCompletedButton
- `artworkCardPrefab` → ArtworkCardPrefab (el prefab que creaste)

### Right Zone:
- `detailPanel` → RightZone (o el panel de detalle)
- `detailArtworkImage` → DetailArtworkImage
- `detailTitleText` → DetailTitleText
- `detailArtistText` → DetailArtistText
- `detailDescriptionText` → DetailDescriptionText
- `easyButton` → EasyButton
- `normalButton` → NormalButton
- `hardButton` → HardButton
- `expertButton` → ExpertButton
- `easyButtonText` → EasyButtonText
- `normalButtonText` → NormalButtonText
- `hardButtonText` → HardButtonText
- `expertButtonText` → ExpertButtonText

---

## 📊 Funcionalidades Implementadas

### ✅ Zona Central (Catálogo):
- Grid de obras con scroll
- 3 filtros: Todas / Por Completar / Mi Galería
- Marco visual en obras completadas
- % de progreso en obras iniciadas
- Click en cualquier obra para ver detalle

### ✅ Zona Derecha (Detalle):
- Imagen grande de la obra seleccionada
- Título, autor y descripción
- 4 botones de dificultad (64/144/256/512 piezas)
- Texto "Continuar [Dificultad]" si ya hay progreso guardado
- Carga automática de la última obra jugada al abrir el menú

### ✅ Zona Izquierda (Configuración):
- Slider de volumen de música (guarda automáticamente)
- Slider de volumen de sonidos (guarda automáticamente)
- Toggle de tutorial (guarda automáticamente)
- Texto con stats globales: "12/24 obras completadas"

### ✅ Eventos Disponibles:
- `OnStartPuzzle(string artworkId, int pieceCount)` - Se dispara al presionar un botón de dificultad
- `OnMusicVolumeChanged(float volume)` - Al cambiar el slider de música
- `OnSoundVolumeChanged(float volume)` - Al cambiar el slider de sonidos
- `OnTutorialToggled(bool enabled)` - Al cambiar el toggle de tutorial

---

## 🎮 Integración con GameBootstrap

Necesitarás conectar el `UnifiedMainMenuController` en `GameBootstrap.cs`:

```csharp
[SerializeField] private UnifiedMainMenuController unifiedMainMenu;

// En Awake():
if (unifiedMainMenu != null)
{
    unifiedMainMenu.Initialize(localCatalogService, saveDataService);
}

// En SetupEventListeners():
if (unifiedMainMenu != null)
{
    unifiedMainMenu.OnStartPuzzle += (artworkId, pieceCount) => 
    {
        selectedArtworkId = artworkId;
        selectedPieceCount = pieceCount;
        BeginPuzzle();
    };
    
    unifiedMainMenu.OnMusicVolumeChanged += (volume) => 
    {
        if (audioManager != null)
            audioManager.SetMusicVolume(volume);
    };
    
    unifiedMainMenu.OnSoundVolumeChanged += (volume) => 
    {
        if (audioManager != null)
            audioManager.SetSoundVolume(volume);
    };
}
```

---

## 🚀 Orden de Implementación Sugerido

1. **Crear el Canvas curvo** en Unity
2. **Crear la estructura de las 3 zonas** (Left/Center/Right)
3. **Crear el prefab ArtworkCard** con los elementos necesarios
4. **Agregar el componente UnifiedMainMenuController** al panel principal
5. **Asignar todas las referencias** en el Inspector
6. **Conectar en GameBootstrap** los eventos
7. **Probar en Play Mode**

---

## 💡 Notas Importantes

- El script maneja automáticamente la creación/destrucción de tarjetas en el grid
- Los filtros funcionan consultando el SaveData para ver qué obras están completadas o en progreso
- El botón "Continuar" aparece automáticamente si detecta progreso guardado
- Los stats globales se actualizan cada vez que se muestra el menú
- La última obra jugada se selecciona automáticamente al entrar

---

## 🐛 Testing Checklist

- [ ] Las tarjetas se crean correctamente en el grid
- [ ] Los filtros funcionan (Todas/Por Completar/Mi Galería)
- [ ] Click en una obra muestra su detalle correctamente
- [ ] Los botones de dificultad disparan el evento OnStartPuzzle
- [ ] El texto cambia a "Continuar" si hay progreso guardado
- [ ] Los sliders de volumen funcionan
- [ ] El toggle de tutorial funciona
- [ ] Los stats globales muestran el conteo correcto
- [ ] La última obra jugada se carga al entrar

---

¡Listo para usar! 🎉
