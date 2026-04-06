# Solución: Build Android para Meta Quest falla

## Error: "The SDK directory is not writable"

```
Failed to install the following SDK components:
     build-tools;35.0.0 Android SDK Build-Tools 35
The SDK directory is not writable (C:\Program Files\Unity\Hub\Editor\...\AndroidPlayer\SDK)
```

Unity usa su SDK en `C:\Program Files\`, que no es escribible sin permisos de administrador. Gradle necesita instalar Build-Tools 35 ahí y falla.

---

## Solución automática (ANDROID_HOME)

**El proyecto ya incluye un post-procesador** que usa tu `ANDROID_HOME` automáticamente si está definido. Tu variable apunta a `C:\Users\rvazq\AppData\Local\Android\Sdk`, que es escribible.

Solo necesitas:

1. **Instalar Build-Tools 35** en tu SDK:
   - Abre **Android Studio** → Settings → Languages & Frameworks → Android SDK → SDK Tools.
   - Marca **Android SDK Build-Tools 35** (o superior) y Apply.
   - O en PowerShell: `cd "$env:ANDROID_HOME\cmdline-tools\latest\bin"` y `.\sdkmanager.bat "build-tools;35.0.0"` (antes ejecuta `.\sdkmanager.bat --licenses` si no lo has hecho).
2. **Vuelve a hacer Build & Run.** El post-procesador usará `ANDROID_HOME` automáticamente.

---

## Alternativa: ejecutar Unity como Administrador

Si prefieres seguir usando el SDK de Unity:

1. **Acepta licencias** (si no lo has hecho):
   ```powershell
   cd "C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\cmdline-tools\16.0\bin"
   .\sdkmanager.bat --licenses
   ```
2. **Ejecuta Unity Hub como Administrador** (clic derecho → "Ejecutar como administrador").
3. Abre el proyecto y haz Build & Run.

---

## Errores secundarios (no bloquean el build)

### XRSimulationPreferences: "Destination path name does already exist"

Si ves conflictos al mover assets de XR Simulator:

1. Cierra Unity.
2. Borra la carpeta `Assets/XR/Temp` si existe.
3. Opcional: borra la carpeta `Library` del proyecto (Unity la regenerará; el primer build será más lento).

### 2 URP assets included

Es normal que Unity incluya presets de Standalone; no afecta el build de Android.

---

## Resumen rápido

| Problema | Solución |
|----------|----------|
| SDK no escribible | El proyecto usa `ANDROID_HOME` automáticamente. Instala Build-Tools 35 en tu SDK. |
| Licencias no aceptadas | `sdkmanager --licenses` en PowerShell |
| SDK read-only (alternativa) | Ejecutar Unity Hub como Administrador |
| XRSimulation conflictos | Borrar `Assets/XR/Temp` y/o `Library` |
