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
  (sesiones previas)
