# Configurar Loading Spinner - Art Unbound

## Script Creado

✅ **`LoadingSpinner.cs`** está en `Assets/ArtUnbound/Scripts/UI/`

## Pasos para Configurar en Unity

### 1. Crear el GameObject del Spinner

1. En la jerarquía, dentro de **`MainUICanvas`**, crear:
   ```
   MainUICanvas
   └── LoadingSpinner (GameObject nuevo)
       └── SpinnerImage (Image)
       └── LoadingText (TextMeshPro - Text) [Opcional]
   ```

### 2. Configurar LoadingSpinner (Padre)

**En el GameObject `LoadingSpinner`:**

1. Agregar componente `LoadingSpinner` (el script)
2. Configurar posición:
   - **Rect Transform**: 
     - Anchors: Center-Center
     - Position: (0, 0, 1) - Adelante del canvas
     - Width: 200, Height: 200

3. Agregar `Canvas Group` (se agrega automáticamente si no existe)

### 3. Crear la Imagen del Spinner

**En el hijo `SpinnerImage`:**

1. Agregar componente `Image`
2. Configurar:
   - **Rect Transform**:
     - Anchors: Stretch-Stretch (Full size del padre)
     - Left: 0, Top: 0, Right: 0, Bottom: 0
   
   - **Image**:
     - **Sprite**: Usa una imagen circular con transparencia
       - Puedes crear una simple en Photoshop/Figma
       - O buscar "loading spinner icon" en Google Images
       - Formato PNG con transparencia
       - Debe tener un diseño asimétrico para ver la rotación
   
   - **Color**: Blanco o el color que prefieras

### 4. Crear el Texto (Opcional)

**En el hijo `LoadingText`:**

1. Agregar `TextMeshPro - Text`
2. Configurar:
   - **Rect Transform**:
     - Anchors: Bottom-Center
     - Position Y: -80 (debajo del spinner)
     - Width: 300, Height: 50
   
   - **TextMeshPro**:
     - Text: "Cargando..."
     - Font Size: 24
     - Alignment: Center-Center
     - Color: Blanco

### 5. Conectar Referencias en LoadingSpinner Script

**En el Inspector del GameObject `LoadingSpinner`:**

1. **Spinner Transform**: Arrastra `SpinnerImage` aquí (el que rota)
2. **Canvas Group**: Debería auto-asignarse
3. **Loading Text**: Arrastra el TextMeshPro si lo creaste
4. **Rotation Speed**: 360 (grados por segundo)
5. **Default Text**: "Inicializando..."

### 6. Conectar en GameBootstrap

1. Selecciona el GameObject `GameBootstrap` en la escena
2. En el Inspector, busca la sección **"General UI"**
3. Arrastra el GameObject `LoadingSpinner` al campo **"Loading Spinner"**

## Imagen del Spinner (Opciones)

### Opción 1: Crear en Unity (Simple)

1. Crear una imagen blanca circular con un cuarto faltante:
   - Círculo blanco 80% completo
   - El 20% restante transparente
   - Esto crea el efecto de "loading"

### Opción 2: Usar Imagen Pre-hecha

Busca en Google Images: "loading spinner icon transparent PNG"
- Debe ser **PNG con transparencia**
- Debe ser **asimétrica** (no un círculo perfecto)
- Ejemplos: spinner de puntos, arco circular, etc.

### Opción 3: Usar UI Toolkit (Avanzado)

Si prefieres algo más elaborado, puedes usar una animación de UI Toolkit.

## Configuración Final

Una vez todo conectado:

1. **Desactiva** el GameObject `LoadingSpinner` en la jerarquía (debe estar inactivo por defecto)
2. El script lo activará automáticamente cuando sea necesario
3. Prueba el juego - debería aparecer durante 2 segundos al inicio

## Personalización

### Cambiar el Texto

```csharp
loadingSpinner.Show("Tu mensaje aquí");
```

### Cambiar Velocidad de Rotación

En el Inspector: `Rotation Speed = 180` (más lento) o `540` (más rápido)

### Cambiar Color

Modifica el color del componente `Image` en `SpinnerImage`

## Resultado Esperado

✅ Al iniciar el juego:
1. Canvas aparece inmediatamente a 1.7m
2. Spinner aparece en el centro con "Calibrando..."
3. Gira durante 2 segundos
4. Desaparece cuando el canvas se reposiciona a la altura correcta

---

**Nota**: Si no tienes una imagen de spinner, puedes usar temporalmente cualquier imagen cuadrada y verás cómo rota. Luego puedes reemplazarla por una mejor.
