# Restaura el save.json de respaldo para probar la pantalla final
# Ejecutar desde la raíz del proyecto o desde SaveDataBackup

$destDir = "$env:USERPROFILE\AppData\LocalLow\Ajolote Studios\ArtUnbound"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourceFile = Join-Path $scriptDir "save.json"

if (-not (Test-Path $sourceFile)) {
    Write-Host "Error: No se encontró save.json en $scriptDir" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $destDir)) {
    New-Item -ItemType Directory -Path $destDir -Force | Out-Null
}

Copy-Item $sourceFile (Join-Path $destDir "save.json") -Force
Write-Host "Respaldo restaurado. Abre el juego y selecciona 'A Sunday on La Grande Jatte' (70 piezas)." -ForegroundColor Green
