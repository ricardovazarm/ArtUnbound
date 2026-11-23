# PATTERNS.md — memoria dura del proyecto

Una línea por patrón. Solo lo que costó trabajo aprender y el `/clear` borraría. Tope ~50.

- **P-001** · Unity batchmode — usa EXACTAMENTE la versión de `ProjectSettings/ProjectVersion.txt`
  (hoy 6000.3.10f1): `"C:/Program Files/Unity/Hub/Editor/6000.3.10f1/Editor/Unity.exe" -batchmode
  -nographics -quit -projectPath <proj> -logFile <log>`. Con otra versión el editor reescribe
  `ProjectVersion.txt`, `Packages/packages-lock.json` y
  `Assets/Settings/Project Configuration/UniversalRenderPipelineGlobalSettings.asset`
  (revertir con `git checkout --`). Antes verifica que no exista `Temp/UnityLockfile`. No hay
  tests: la "suite" es compilar; verde = exit 0 y 0 `error CS` en el log. (sesión 2026-10-29)
- **P-002** · Line endings mixtos — `GameBootstrap.cs`, `HandTrackingInputController.cs`,
  `WallAnchorManager.cs`, `GrabbableFrame.cs` y `PlacedArtworkFactory.cs` son CRLF; otros .cs
  y `PLAN.md` son LF. Al parchear por script respeta el estilo de cada archivo o el diff se
  infla con el archivo completo. (sesión 2026-10-29)
- **P-003** · Bash en Windows — los heredocs `python - <<'EOF'` fallan por parsing del shell;
  escribe el script en el scratchpad y córrelo con `python -I script.py`. (sesión 2026-10-29)
- **P-004** · Dimensiones de obras colgadas — NUNCA persistir `boardWidth/boardHeight` desde un
  `BoxCollider`: `GrabbableFrame` lo re-ajusta al pinchar y una AABB en mundo mezcla ancho con
  profundidad y suma cédula/lámpara. Fuente de verdad: `PlacedArtworkIdentifier.boardWidth/Height`
  (sellado por quien construye) vía `WallAnchorManager.ResolveArtworkDimensions`. (2026-10-29)
- **P-005** · Orientación de PlacedArtwork — la cara visible es +Z del root (quad con flip
  180°). Para encararla al usuario desde un panel usa `LookRotation(-panel.forward)`, como
  `OnHangPlaqueFromCollection`; en pared, `LookRotation(normal)`. (2026-10-29)
- **P-006** · Modo de input — `InputDevice.isValid` sigue `true` con el control dormido, no
  sirve para detectar que el OS pasó a manos; se usa `CommonUsages.isTracked` con 2 polls
  seguidos para entrar a modo control (pendiente confirmar en Quest). (2026-10-29)
- **P-007** · Build Android — "Split Application Binary" (OBB) rompe Build & Run y se disfraza
  de crash de TMP; dejarlo DESACTIVADO. (sesiones previas)
- **P-008** · `PuzzleBoard.GetSlotIndex` asume grid cuadrado (solo vecinos de morfología);
  para la posición correcta de una pieza usa `GetCorrectSlotIndexForPiece`, nunca
  `PuzzlePiece.CorrectSlotIndex`. (sesiones previas)
- **P-009** · Multi-copia — la misma obra puede colgarse varias veces: en MR opera por
  `PlacedArtworkIdentifier.anchorId`, en VR por `instanceId`; nunca por `artworkId`.
  (sesiones previas) **Mordio el 2026-11-16:** `WallAnchorManager.spawnedArtworks` va por
  `artworkId` y con dos copias solo conoce una (la otra quedo visible en VR); para recorrer
  TODAS las instancias MR usa `_mrArtworkObjects`. El ancla viva de una copia se busca en
  `_liveAnchorByArtwork` (por GameObject; corregido 2026-11-22).
- **P-010** · Compilar con el editor ABIERTO — batchmode no corre (lock), pero
  `dotnet build Assembly-CSharp.csproj -nologo -v q -clp:"ErrorsOnly;Summary"` SI compila los
  csproj que Unity genera (~6 s, dotnet 10). Dos trampas: (1) `EnableDefaultItems=false`, un
  `.cs` NUEVO no esta en el csproj hasta que Unity lo regenere; (2) 0 errores no prueba que el
  binario traiga lo nuevo: `grep -c -a "<Simbolo>" Temp/bin/Debug/Assembly-CSharp.dll`. Solo
  verifica compilacion; el editor no recarga hasta recuperar foco o un Refresh. (2026-11-16,
  receta traida de TochitoClashUnity P-129)
- **P-011** · Unity MCP (editor abierto) — desde el 2026-11-22 es el del **Unity CLI**: servidor
  `unity-editor-mcp` (`unity mcp --tool-groups`) registrado a nivel USUARIO en Claude Code con
  `unity mcp configure claude-code` (sirve a todos los proyectos), mas el paquete
  `com.unity.pipeline` en CADA proyecto (`unity pipeline install` una vez, y darle foco al
  editor para que lo resuelva). Diagnostico: `unity pipeline editors` y `unity status` deben dar
  `serverReachable true` / `ready`. Tools utiles: `script_eval` (C# inline con `return ...;`),
  `console` (`status` trae `compilationFailed` y conteo de errores), `editor`. El MCP viejo del
  paquete `com.unity.ai.assistant` (`relay_win.exe --mcp` en `.mcp.json`) esta OBSOLETO y ya no
  existe aqui: si alguna vez responde "Connection revoked" y *Project Settings > AI > Unity MCP
  Server* dice "Up to 0 direct connections allowed", NO es la aprobacion (los botones de aprobar
  solo loguean "No pending approval found" en `Logs/traces.jsonl`): el limite sale del
  entitlement de la cuenta y la licencia gratuita da 0 desde la 2.7 del paquete. No perder
  tiempo re-aprobando ni reconectando (costo ~10 intentos el 2026-11-22). Sigue valiendo:
  ningun mensaje de exito es evidencia de compilacion; compilo si
  `Library/ScriptAssemblies/Assembly-CSharp.dll` es mas nueva que el `.cs` editado y hay 0
  `error CS` en `$LOCALAPPDATA/Unity/Editor/Editor.log` (si el resultado es identico la DLL no
  cambia de fecha). Con el editor SIN foco el auto-refresh no dispara; la receta
  `UnityEditor.AssetDatabase.Refresh(); UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();`
  SI funciona con `script_eval` (probado 2 veces el 2026-11-22; la DLL sale ~15-25 s despues). No mandar comandos al editor mientras corre un
  Build. **Regla del usuario: tras editar C# se compila SIEMPRE en el editor antes de entregar**
  (`dotnet build` solo no cuenta). (2026-11-16; reescrito 2026-11-22 por la migracion al CLI)
- **P-012** · Log del Quest — el buffer de logcat es de 256 KB y pierde las lineas de Unity en
  minutos: para diagnosticar se deja grabando ANTES de reproducir, con el adb del editor:
  `"C:/Program Files/Unity/Hub/Editor/6000.3.10f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe" logcat -v time -T 1 -s Unity > archivo.txt`
  en background. (2026-11-16)
- **P-013** · Unity MCP con VARIOS editores abiertos — antes de cualquier comando que modifique
  algo, corre uno que solo devuelva `Application.dataPath` y confirma el proyecto (el 2026-11-18,
  con el relay viejo, un comando corrio en TochitoClashUnity). Con el Unity CLI existe
  `unity mcp --project-path <ruta>` para fijar el editor; no se ha probado con dos editores
  abiertos. (2026-11-18; reescrito 2026-11-22)
- **P-014** · Icono y splash en Quest — (1) instalar el APK ENCIMA no refresca el icono de la
  biblioteca aunque el APK lo traiga (`aapt2 dump badging` lo confirma): hubo que reiniciar el
  visor (o desinstalar, que borra `save.json`). (2) `ProjectSettings.asset` editado en disco con
  el editor abierto SI lo recarga Unity al recuperar foco (visto 3 veces), pero Project Settings
  cambiados en el editor no llegan a disco hasta *File > Save Project*. (3) El Quest respeta
  `m_SplashScreenBackgroundColor`; el Virtual Reality Splash Image no tiene color propio, asi que
  va con PNG transparente (uno con fondo se ve como recuadro). (2026-11-18)
- **P-015** · Build y deploy al Quest desde el MCP — `build run` con `target: Android`,
  `output-path: ArtUnbound_v1.apk`, `confirm: true` devuelve `queued` al instante (~7 min de
  build). NO pedir `build status` al terminar: devuelve el BuildReport completo (3.4 MB) y
  revienta el limite de la herramienta. Se confirma con el cambio de fecha del APK mas la linea
  `Build Finished, Result: Success` en `$LOCALAPPDATA/Unity/Editor/Editor.log`. Instalar con el
  adb del editor (ruta en P-012): `adb install -r ArtUnbound_v1.apk` (conserva `save.json`) y
  comprobar `adb shell dumpsys package com.ajolotestudios.artunbound | grep lastUpdateTime`.
  (2026-11-22)
