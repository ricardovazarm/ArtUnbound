# Restaurar datos de guardado para probar pantalla final

Este respaldo contiene un puzzle **completado** ("A Sunday on La Grande Jatte", 70 piezas) para que puedas ver la pantalla final y la imagen completa sin tener que armar todo de nuevo.

## Cómo restaurar

1. **Cierra el juego** (si está abierto).
2. **Copia** los archivos de esta carpeta a la carpeta de guardado de Unity:
   - Origen: `SaveDataBackup/save.json`
   - Destino: `%USERPROFILE%\AppData\LocalLow\Ajolote Studios\ArtUnbound\save.json`

3. **Sobrescribe** el archivo existente.

### Comando rápido (PowerShell)

```powershell
Copy-Item "SaveDataBackup\save.json" "$env:USERPROFILE\AppData\LocalLow\Ajolote Studios\ArtUnbound\save.json" -Force
```

### O manualmente

1. Abre el Explorador de archivos.
2. Navega a `C:\Users\TU_USUARIO\AppData\LocalLow\Ajolote Studios\ArtUnbound\`
3. Reemplaza `save.json` con el archivo de esta carpeta.

## Cómo probar

1. Abre el juego.
2. Entra a la galería y selecciona **"A Sunday on La Grande Jatte"**.
3. Elige **70 piezas** (o la dificultad que coincida).
4. Al cargar, verás el puzzle ya completado con la imagen completa y el panel PostGame.

## Actualizar el respaldo

Si armas otro puzzle hasta el final y quieres respaldarlo:

1. Copia `save.json` desde `AppData\LocalLow\Ajolote Studios\ArtUnbound\` a esta carpeta `SaveDataBackup\`.
2. Sobrescribe el archivo existente.
