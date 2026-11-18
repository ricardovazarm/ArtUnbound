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
  TODAS las instancias MR usa `_mrArtworkObjects`. `activeAnchorsByArtworkId` tiene el mismo
  defecto y sigue sin corregir.
- **P-010** · Compilar con el editor ABIERTO — batchmode no corre (lock), pero
  `dotnet build Assembly-CSharp.csproj -nologo -v q -clp:"ErrorsOnly;Summary"` SI compila los
  csproj que Unity genera (~6 s, dotnet 10). Dos trampas: (1) `EnableDefaultItems=false`, un
  `.cs` NUEVO no esta en el csproj hasta que Unity lo regenere; (2) 0 errores no prueba que el
  binario traiga lo nuevo: `grep -c -a "<Simbolo>" Temp/bin/Debug/Assembly-CSharp.dll`. Solo
  verifica compilacion; el editor no recarga hasta recuperar foco o un Refresh. (2026-11-16,
  receta traida de TochitoClashUnity P-129)
- **P-011** · Unity MCP (editor abierto) — `.mcp.json` en la raiz lanza
  `~/.unity/relay/relay_win.exe --mcp` (paquete `com.unity.ai.assistant`; la conexion se aprueba
  en *Project Settings > AI* y Claude Code se reinicia para cargarlo). Tools: `Unity_RunCommand`
  (parametro `code`; clase `internal CommandScript` sin clases anidadas, sin `using
  System.Reflection`, tipos `Unity.*` con `global::`), `Unity_ManageMenuItem`,
  `Unity_ReadConsole`. Ningun mensaje de exito es evidencia: compilo si
  `Library/ScriptAssemblies/Assembly-CSharp.dll` es mas nueva que el `.cs` editado, y los
  errores se leen de `grep "error CS" "$LOCALAPPDATA/Unity/Editor/Editor.log"` (la consola del
  MCP puede salir vacia con el ensamblado roto). Con el editor SIN foco el auto-refresh no
  dispara. Si el cliente MCP sale "failed to connect", el relay suele seguir vivo
  (`netstat -ano | grep :900`): se le habla por stdio con JSON-RPC (`initialize` +
  `notifications/initialized` + `tools/call`). Verificado aqui con la 2.10 del paquete: el
  relay por stdio lista 33 tools (`Unity_RunCommand`, `Unity_ManageEditor`, `Unity_ReadConsole`,
  `Unity_ManageMenuItem`...) y en esta version el parametro de `Unity_RunCommand` es **`Code`**
  (mayuscula), no `code`. No mandar comandos al editor mientras corre un Build. **Receta para
  compilar en el editor (probada):** `Unity_RunCommand` con `AssetDatabase.Refresh();
  global::UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();` — funciona con
  el editor sin foco. Dos falsos negativos: un `touch` sin cambio de contenido NO dispara
  recompilacion (Unity compara contenido), y si el resultado es identico la DLL de
  `ScriptAssemblies` NO cambia de fecha; en ese caso la evidencia es una linea nueva
  `Domain Reload Profiling` en `Editor.log` y 0 `error CS`. **Regla del
  usuario: tras editar C# se compila SIEMPRE en el editor antes de entregar** (`dotnet build`
  solo no cuenta). (2026-11-16, de TochitoClash P-024/P-025/P-058/P-068)
- **P-012** · Log del Quest — el buffer de logcat es de 256 KB y pierde las lineas de Unity en
  minutos: para diagnosticar se deja grabando ANTES de reproducir, con el adb del editor:
  `"C:/Program Files/Unity/Hub/Editor/6000.3.10f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe" logcat -v time -T 1 -s Unity > archivo.txt`
  en background. (2026-11-16)
- **P-013** · Unity MCP con VARIOS editores abiertos — el cliente se engancha a uno cualquiera,
  no al del proyecto de la sesion (el 2026-11-18 quedo en TochitoClashUnity y un comando corrio
  alla). Antes de cualquier `Unity_RunCommand` que modifique algo, corre uno que solo haga
  `result.Log(Application.dataPath)` y confirma el proyecto. Si no es el correcto, no hay forma
  de elegirlo desde aqui: pedir al usuario que cierre los otros editores. Hablarle al relay por
  stdio tampoco sirve si el editor no publico su pipe (`\.\pipe\unity-mcp-*`): el relay se
  cuelga reintentando un pipe viejo de `~/.unity/mcp/connections/`. (2026-11-18)
- **P-014** · Icono y splash en Quest — (1) instalar el APK ENCIMA no refresca el icono de la
  biblioteca aunque el APK lo traiga (`aapt2 dump badging` lo confirma): hubo que reiniciar el
  visor (o desinstalar, que borra `save.json`). (2) `ProjectSettings.asset` editado en disco con
  el editor abierto SI lo recarga Unity al recuperar foco (visto 3 veces), pero Project Settings
  cambiados en el editor no llegan a disco hasta *File > Save Project*. (3) El Quest respeta
  `m_SplashScreenBackgroundColor`; el Virtual Reality Splash Image no tiene color propio, asi que
  va con PNG transparente (uno con fondo se ve como recuadro). (2026-11-18)
