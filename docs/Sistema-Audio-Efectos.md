# Sistema de Audio y Efectos - Art Unbound

## Configuración Inicial

### 1. Crear el GameObject de AudioManager

1. En la escena `Main.unity`, crear un GameObject vacío llamado `AudioManager`
2. Agregar el componente `AudioManager` (Scripts/Services/AudioManager.cs)
3. El AudioManager se configurará automáticamente con dos AudioSources:
   - **SFX Source**: Para efectos de sonido
   - **Music Source**: Para música de fondo

### 2. Crear el GameObject de PieceEffectsManager

1. En la escena `Main.unity`, crear un GameObject vacío llamado `PieceEffectsManager`
2. Agregar el componente `PieceEffectsManager` (Scripts/Feedback/PieceEffectsManager.cs)

### 3. Asignar AudioClips

En el Inspector del `AudioManager`, asignar los siguientes clips de audio:

#### UI Sounds
- **Button Click Sound**: Sonido para clicks de botón (corto, clic sutil)

#### Gameplay Sounds
- **Piece Grab Sound**: Sonido al tomar una pieza (pop suave)
- **Piece Place Sound**: Sonido al colocar una pieza incorrectamente (thud suave)
- **Piece Correct Sound**: Sonido al colocar una pieza correctamente (ding satisfactorio)
- **Puzzle Complete Sound**: Sonido de celebración al completar el puzzle (fanfare corto)

## Sonidos Recomendados

### Dónde conseguir sonidos gratuitos:
- **Freesound.org** - Efectos de sonido con licencia Creative Commons
- **Zapsplat.com** - Biblioteca de efectos de sonido gratuitos
- **Unity Asset Store** - Paquetes de audio gratuitos

### Sugerencias por tipo:

1. **Button Click** (0.1-0.2s)
   - Búsqueda: "UI click", "button press", "soft click"
   - Debe ser muy corto y sutil

2. **Piece Grab** (0.2-0.3s)
   - Búsqueda: "pop", "pickup", "grab"
   - Un "pop" suave o "whoosh" corto

3. **Piece Place** (0.2-0.4s)
   - Búsqueda: "place", "drop", "thud"
   - Un "thud" suave pero audible

4. **Piece Correct** (0.3-0.5s)
   - Búsqueda: "success", "correct", "ding", "chime"
   - Un sonido positivo y satisfactorio

5. **Puzzle Complete** (1-2s)
   - Búsqueda: "success fanfare", "level complete", "victory"
   - Celebración breve pero memorable

## Efectos Visuales

### Partículas de Colocación Correcta

El sistema está configurado para mostrar partículas verdes cuando una pieza se coloca correctamente.

**Configuración en Inspector:**
- **Particle Count**: 15 (número de partículas)
- **Particle Lifetime**: 0.5s (duración)
- **Particle Speed**: 0.5m/s (velocidad de dispersión)
- **Particle Size**: 0.02m (2cm - tamaño)
- **Correct Placement Color**: Verde brillante (R:0.2, G:1.0, B:0.3)

### Personalización

Para ajustar los efectos visuales, modificar los valores en el Inspector de `PieceEffectsManager`:
- Aumentar `Particle Count` para más partículas
- Aumentar `Particle Speed` para dispersión más rápida
- Cambiar `Correct Placement Color` para diferente color

## Volúmenes

Los volúmenes predeterminados son:
- **SFX Volume**: 0.7 (70%)
- **Music Volume**: 0.3 (30%)

Estos pueden ajustarse en el Inspector del AudioManager o mediante código:
```csharp
AudioManager.Instance.SetSFXVolume(0.8f);
AudioManager.Instance.SetMusicVolume(0.5f);
```

## Integración Completada

El sistema está integrado en:
- ✅ **InteractionManager**: Sonido al tomar piezas
- ✅ **PuzzleBoard**: Sonidos y partículas al colocar piezas
- ✅ **TrayScrollButtons**: Sonido al presionar botones de scroll
- ✅ Detección automática de colocación correcta/incorrecta

## Próximos Pasos

1. Crear los GameObjects en la escena Main
2. Descargar o crear los clips de audio
3. Asignar los clips en el Inspector
4. Ajustar volúmenes y configuración de partículas al gusto
5. ¡Probar en el Quest!

## Notas

- El `AudioManager` persiste entre escenas (DontDestroyOnLoad)
- Las partículas se destruyen automáticamente después de reproducirse
- Los sonidos son 2D (no espaciales) para consistencia en VR
- El sistema funciona sin clips asignados (silencio si falta un clip)
