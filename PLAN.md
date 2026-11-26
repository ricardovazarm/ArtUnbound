# PLAN.md — Estado del proyecto (actualizado 2026-11-25)

## Handoff

- **Bloqueos:** ninguno. Árbol: `Main.unity` queda con `enableDevCheats: 1` sin commitear a
  propósito (cédula y lámpara desbloqueadas; el botón "DEV" no sale porque `showDevButton`
  está apagado). El **Quest 3** y el **Quest 2** traen el mismo APK del 2026-11-22 19:42
  (2.2.1) con ese estado; en el Quest 2 es instalación limpia del 2026-11-25 y **nunca se ha
  abierto ahí** (ver "Trabajo 2026-11-25").
- **Decisiones que dependen del usuario:**
  - ¿Liberar el `ARAnchor` viejo al reposicionar/quitar un cuadro? Hoy se queda vivo en la
    escena (fuga por sesión; ver "Trabajo 2026-11-22, segunda tanda"). Recomendado hacerlo
    después de las capturas.
  - Sin trackear, commitear o ignorar: `AGENTS.md`, `.agents/`, `.codex/`,
    `scripts/Export-StoreImage.ps1` y `GeneratedAssets/` (86 MB).
  - `enableDevCheats` a 0 antes de cualquier build de release.
  - ¿El baño de luz de la lámpara se quita también en VR? Hoy se quitó en los dos modos.
  - Bug del gatillo que deja de responder: ¿resuelto? Si sí, retirar los logs `DIAG`.
  - **Dos placas inalcanzables:** ¿bajar los umbrales de `master_author_johannes_vermeer`
    (11→10) y `grandmaster_movement_dutch_golden_age` (36→35)? Ver "segunda tanda 2026-11-25".
- **Pendientes chicos:** primer arranque en el Quest 2 (VR en frío, con controles y logcat
  grabando); ver en el visor la lámpara sin baño de luz, reposicionar una de dos
  copias de la misma obra, y lo de "Trabajo 2026-11-16"; 5 capturas 2560×1440 y tráiler; ¿las
  placas Museum Label / Frame / Picture Light se distinguen en la miniatura? (mismo prefab);
  `unity pipeline install` en los otros proyectos de Unity; el `CLAUDE.md` del proyecto está
  rancio (marcos por dificultad, 0.4 m, Quest 3/Pro) y el GDD v6.1 ya no; al asset de "The
  Jewish Bride" le falta la comilla de cierre en `title`.

> Fuente de verdad del avance. Al iniciar cada sesión, leer este archivo y retomar desde
> la primera fase no completada.

## Contexto

Este archivo sustituyó al plan anterior (era GDD v4). Aquel trabajo fue absorbido y superado
por el **Plan de Alineación GDD v6** (`docs/Art-Unbound-Plan-Alineacion-GDDv6.md`), que se
ejecutó completo: los **Frentes 0–7 están implementados y cableados en escena** (modelo de
progresión, palomita en vez de medallas, tier global como material, cédula/marco/lámpara,
sistema de placas 23+estatus, sección Collection, limpieza del Store, post-juego sin medalla).
El detalle frente-por-frente vive en ese doc; aquí solo se rastrea lo que FALTA.

## Verificado hecho (no rehacer)

- Frentes 0–7 del plan GDD v6: código + escena. ✅
- Pendientes manuales del Editor ya resueltos (verificado en repo 2026-07-12):
  `CollectibleCatalog.asset` creado y asignado en `GameBootstrap`; prefabs `Plaque`,
  `Cedula` y `StatusPlaque` creados con sus 4 materiales por tier y cableados
  (`cedulaPrefab` en escena, `visualPrefab` en los assets); `StoreView` eliminado de
  `Main.unity`. ✅
- **IAP Meta verificado en device (2026-07-12):** el botón Buy abre el checkout, la compra
  se completa y **desbloquea las pinturas del catálogo**. ✅
- Auditoría del catálogo: 17 obras corregidas + duplicado de Vermeer eliminado (251 obras),
  commiteado en `21fc52f`. ✅
- **Trabajo post-corte del plan (commits 2026-07-20/21, código en main):**
  - Botón **Hang** en el detail de la galería + **multi-copia MR por anchorId**
    (`PlacedArtworkIdentifier.anchorId`; VR usa `instanceId`) — `3198fd4`. ✅
  - **Soporte de mano izquierda** en hand tracking (input y scroll del tray) — `a04bb6c`. ✅
  - Dev cheat de autocompletar puzzle + atlas SDF regenerados — `58968b0`. ✅
  - **Tutorial de mano fantasma** en primer arranque (`TutorialFlowController`,
    `TutorialHandController`) + re-armado en caliente desde el toggle de Settings —
    `a871d70`, `c58c332`. Su verificación en device va dentro de la Fase B. ✅ (código)

## Fixes 2026-10-29 (código compilado, PENDIENTE verificar en Quest)

Sesión de arreglos sueltos tras un playtest del usuario en Quest. No cierra ninguna fase.
Compiló en verde con **Unity 6000.3.10f1** en batchmode (0 `error CS`, exit 0); el proyecto
no tiene tests automatizados, la única verificación posible aquí es compilar. **Ninguno de
estos cambios ha sido visto funcionando en el visor.**

1. **Hang desde el detalle salía de espaldas.** `GameBootstrap.OnHangArtworkFromDetail`
   usaba `LookRotation(+panel.forward)`; ahora `-panel.forward` (misma convención que
   `OnHangPlaqueFromCollection`; la cara visible de `PlacedArtworkFactory` es +Z del root).
   *Mirar:* Hang desde el detalle → la obra aparece de frente al usuario.
2. **Al pasar de controles a manos, el post-juego seguía en modo control.**
   `HandTrackingInputController.AutoDetectInputMode` decidía con `InputDevice.isValid`, que
   sigue `true` con el control dormido. Ahora usa `CommonUsages.isTracked`, exige 2 polls
   seguidos para pasar a CONTROLLER (a HANDS es inmediato), no cambia a media interacción y
   expone `IsControllerMode`; `PostGameController` y `PlaqueViewController` lo consultan en
   lugar de `HasValidController`.
   *Mirar:* soltar los controles en post-juego → UI de manos; logcat
   `[HandTracking] Input mode →` no debe alternar solo. **Hipótesis no verificada:** que
   Meta reporte `isTracked=false` en los controles cuando el OS cambia a manos.
3. **Recenter (botón Meta sostenido) dejaba el menú encima del usuario.** `GameBootstrap`
   se suscribe a `XRInputSubsystem.trackingOriginUpdated` (coroutine que espera al
   subsystem hasta 10 s). En MainMenu/ArtworkSelection recoloca la galería
   (`ResetPosition` + `Hide/Show`); en Playing/Paused/PostGame/ComfortPositioning solo deja
   `_recenterPending` y recoloca al volver al menú. En VR no hace nada.
   *Mirar:* logcat `[GameBootstrap] Tracking origin updated (recenter)` al sostener el botón
   Meta; el menú debe reaparecer enfrente. **Hipótesis no verificada:** que el runtime de
   Meta dispare ese evento con el recenter del sistema.
4. **Cuadros recargados salían "portrait y muy delgados" salvo el último.** Al reposicionar
   una obra colgada desde el detalle, `GrabbableFrame.Awake` re-ajustaba el BoxCollider con
   la AABB en ejes de mundo (mezcla ancho/profundidad, suma cédula y lámpara) y
   `WallAnchorManager.UpdateAnchorForSpecificArtwork` persistía esas medidas. Ahora:
   `PlacedArtworkIdentifier.boardWidth/boardHeight` es la fuente de verdad (lo sella quien
   construye: `PlacedArtworkFactory`, `SpawnArtworkAtAnchor`, `CreateAnchor`);
   `ResolveArtworkDimensions` resuelve identifier → quad `FullImageReveal` → 0.5×0.5, nunca
   el collider; `GrabbableFrame` calcula el collider en espacio local, excluye `Cedula` y
   `Lampara` y respeta el collider pre-dimensionado; `HealStoredDimensions` repara al cargar
   las entradas cuyo aspecto difiere >5% de la textura (lado mayor 0.5 m, `MarkDirty`).
   *Mirar:* al reabrir, warning `[WallAnchor] Healed stored dimensions ...` por cada cuadro
   roto y los cuadros salen bien; a la segunda apertura ya no hay warning. Colgar uno nuevo
   en una pared no alineada con los ejes del cuarto, reposicionarlo, reabrir.

**Viaja en el mismo commit (trabajo del usuario del 2026-08-03, sin verificar):** ancla
única de UI en `GameBootstrap` — el board se ancla a la pose de `nativeGallery` en vez de
`mainUICanvas`, se eliminó `PositionCanvasErgonomically()`, `DefaultPlacementDistance`
0.55→0.75 y `heightOffset` -0.15→-0.25.

**Qué NO se hizo y por qué:** no se tocó `enableDevCheats` (toggle de pruebas del usuario,
queda en 1 solo en el árbol local); el sanado de dimensiones deja en 0.5 m las entradas
colgadas desde el post-juego que estuvieran corruptas (tenían hasta 0.6 m), única pérdida de
información posible y aceptada.

### Decisiones
- **DS.1** Recenter a media partida NO mueve el board: solo marca `_recenterPending` y
  recoloca al regresar al menú. Descartado recolocar en caliente (perdería la pose del
  puzzle que el jugador acomodó).
- **DS.2** Dimensiones persistidas nunca salen de un BoxCollider; fuente de verdad el
  identifier sellado al construir. Descartado "arreglar el collider y seguir leyéndolo":
  cualquier componente futuro que lo toque volvería a corromper el save.
- **DS.3** Entradas corruptas se auto-sanan al cargar (lado mayor 0.5 m) en vez de pedir
  borrar `save.json`. Se pierde el tamaño original de las que venían del post-juego.
- **DS.4** Cambio a CONTROLLER exige 2 polls positivos; a HANDS es inmediato. Descartado
  simétrico: el falso positivo caro es quedarse en modo control sin controles.

## Trabajo 2026-11-16 (sesión que NO se cerró; commiteada el 2026-11-18)

Reconstruido desde el diff: esa sesión no pasó por `/cierra-sesion` y no dejó notas. No cierra
ninguna fase. Compila en el editor; **solo la lámpara nueva fue vista en el visor** (el usuario
la vio en MR el 2026-11-18), lo demás está pendiente de verificar.

1. **Lámpara rediseñada** (`PresentationDecorator.BuildLampara`): placa de fijación a la pared,
   brazo, pantalla inclinada y boca encendida (`LampGlow`, shader `Sprites/Default`). Antes era
   una barra suelta a 18 cm de la pared.
2. **Cédula + lámpara en el marco recién armado** (`WallAnchorManager.DecorateExistingFrame`):
   antes solo las recibían los cuadros reconstruidos. *Mirar:* completar un puzzle y colgarlo.
3. **Halo amarillo de selección en placas** (`InteractionManager.SetSelectionHalo`): 4 barras
   unlit sobre el contorno del collider, porque el brillo por emisión no se notaba en el visor.
4. **Obra flotante del botón Hang tomable con control a cualquier distancia**: el tope de 40 cm
   queda solo para cuadros ya anclados (los que tienen `anchorId`).
5. **MR↔VR con multi-copia** (`WallAnchorManager._mrArtworkObjects`): al entrar a VR se ocultan
   todas las copias MR, no solo una por `artworkId`. *Mirar:* dos copias de la misma obra → VR.
6. **Galería VR alineada al menú** (`VRGalleryController.AlignToMenu`, objeto `MenuAnchor` en
   `GalleryRoot.prefab`): ni el menú ni el jugador se mueven al entrar a VR, se acomoda la
   galería. Las poses de cuadros VR se persisten relativas a la galería (saves viejos siguen
   válidos sin migración). `GameBootstrap.AlignGalleryWhenMenuPositioned` cubre el arranque
   directo en VR.
7. **Teleport VR solo sobre piso** (`VRTeleportActivator`): antes aceptaba paredes.
8. **Dev cheat de presentación** (`SaveData.DevUnlockPresentation`, static, no se serializa):
   con `enableDevCheats` activo, cédula y lámpara salen sin llegar al umbral.
9. **`wallRaycastDistance` 2 → 6** en `Main.unity`.
10. **Logs `DIAG` del bug del gatillo que deja de responder** (`[HandTracking] hb`,
    `[HandTracking] trigger`, `[CTRL-Down]`, `[CTRL-RayDiag]`). Son diagnóstico: no consta si
    el bug se resolvió. Retirarlos cuando el usuario lo confirme.

## Trabajo 2026-11-18 (arte de tienda, ícono, splash y lámpara)

Sesión de arreglos sueltos; no cierra ninguna fase. Trabajaron en paralelo Claude y Codex.

- **Ícono del app:** `Assets/ArtUnbound/Branding/AppIcon.png` (512×512) asignado como Default
  Icon. Verificado: el APK lo trae en las 6 densidades y **el usuario lo vio en el visor**. El
  visor siguió mostrando el ícono viejo tras instalar encima (ver P-014).
- **Splash:** fondo blanco (`m_SplashScreenBackgroundColor`) y logo
  `LogoAjoloteStudiosLargeTransparente.png` en la lista de Logos y en Virtual Reality Splash
  Image. Ese PNG fue reemplazado por Codex con la variante limpia de texto rosa (detalle en
  "Arte de publicación"). **Visto bien en el visor por el usuario.** El logo de Unity quedó
  fuera del splash (cambio del usuario en el editor).
- **Lámpara sin baño de luz:** se eliminó el quad `LamparaLuz` y su textura `LampWash` de
  `PresentationDecorator`; queda la lámpara física con su boca encendida. Compilado en el
  editor (DLL de `ScriptAssemblies` posterior al `.cs`, sin el símbolo `LampWash`, 0
  `error CS`) y entró al APK de las 13:15. **Pendiente que el usuario lo confirme en el visor.**
- **Arte subido al Developer Dashboard por el usuario** (Distribución > Solicitudes de apps >
  v1 > Metadatos de la app). No consta desde aquí que todas las casillas quedaran en verde.

**Qué NO se hizo y por qué:** no se tocó `enableDevCheats`; no se commitearon `GeneratedAssets/`
ni la config de Codex (decisión del usuario pendiente); el logo transparente de "Art Unbound"
para la tienda sigue con alfa sucio y tipografía distinta al ícono (es un asset opcional).

### Decisiones
- **DS.5** El baño de luz se quitó en MR y VR a la vez: `PresentationDecorator` es compartido y
  separar por modo exigía pasar el modo desde tres llamadas. Reversible si el usuario lo quiere
  solo en VR.
- **DS.6** Splash de VR con el logo transparente y no con el de fondo blanco: el Quest sí
  respeta el color de fondo del splash, y la imagen con fondo propio (blanco no puro) se veía
  como un recuadro.

## Trabajo 2026-11-22 (botón DEV fuera de las capturas y migración del MCP de Unity)

Sesión de arreglos sueltos; no cierra ninguna fase ni afecta a las Fases A, B o C.

- **Botón "DEV" oculto para capturas.** El usuario no pudo tomar los screenshots de la tienda
  porque el botón verde "DEV" (cheat de autocompletar) salía en el HUD. Nuevo campo
  `GameBootstrap.showDevButton` (default `false`): el botón solo se crea con `enableDevCheats`
  **y** `showDevButton`. Con `enableDevCheats: 1` y `showDevButton` apagado, cédula y lámpara
  siguen desbloqueadas pero el HUD queda limpio. No se tocó la escena. Compilado en el editor
  (0 `error CS`, `compilationFailed: false`, campo presente por reflexión en el ensamblado
  cargado). **Verificado en Quest el 2026-11-22** con el APK de las 19:42: el usuario entró
  a un puzzle y el botón ya no aparece.
- **MCP de Unity migrado al Unity CLI.** El MCP del paquete `com.unity.ai.assistant` (relay)
  dejó de aceptar conexiones: "Connection revoked" / "Up to 0 direct connections allowed". No
  era la aprobación sino el límite del entitlement de la cuenta (detalle en P-011). Se
  actualizó el Unity CLI a 1.0.1, se registró `unity-editor-mcp` (`unity mcp --tool-groups`)
  a nivel usuario en Claude Code, se instaló `com.unity.pipeline` 1.0.1 en el proyecto
  (`Packages/manifest.json` y `packages-lock.json`) y se borró `.mcp.json` (no estaba
  trackeado). Verificado: `script_eval` corrió C# en el editor de ArtUnbound y devolvió su
  `dataPath`.

**Qué NO se hizo y por qué:** no se actualizó `com.unity.ai.assistant` (sigue en 2.10.0-pre.1;
la guía de Unity dice que <2.13 puede chocar con el CLI, pero aquí no dio conflicto y el
paquete ya no se usa para el MCP); no se quitó ese paquete del proyecto (decisión aparte); no
se tocó `enableDevCheats`; no se instaló el paquete Pipeline en otros proyectos.

### Decisiones
- **DS.7** Ocultar solo el botón con un flag aparte en vez de poner `enableDevCheats` en 0:
  las capturas necesitan cédula y lámpara desbloqueadas, que dependen del mismo flag. Se
  pierde el autocompletar mientras `showDevButton` esté apagado.
- **DS.8** Migrar al MCP del Unity CLI en vez de bajar `com.unity.ai.assistant` a 2.6.0-pre.1
  o activar el trial de Unity AI: es el camino que Unity soporta y anuncia como gratuito; el
  MCP del paquete está obsoleto. No se usó el simulador de tier del paquete para forzar el
  límite (sería saltarse el control de licencia).

## Trabajo 2026-11-22, segunda tanda (anclas por instancia, build al Quest, Fase A retirada)

Sesión de arreglos sueltos; no cierra ninguna fase.

- **Registro de anclas vivas por instancia.** En `WallAnchorManager`,
  `activeAnchorsByArtworkId` (por `artworkId`) se reemplazó por `_liveAnchorByArtwork`
  (cuadro colgado → su `ARAnchor`), con limpieza de entradas de cuadros destruidos. Era un
  defecto latente: el diccionario solo se usaba para un desvincular que ya ocurría por otro
  lado, así que **no hay cambio visible esperado**. Compilado en el editor (0 `error CS`, la
  DLL trae `_liveAnchorByArtwork` y ya no el nombre viejo) y dentro del APK de las 19:42.
  **Pendiente de verificar en Quest:** colgar dos copias de la misma obra, reposicionar una y
  reabrir; debe portarse igual que antes.
- **Hallazgo sin arreglar: fuga de `ARAnchor`.** Al reposicionar o quitar un cuadro se borra
  el ancla persistida (`TryEraseAnchorAsync`) pero el `ARAnchor` vivo se queda en la escena;
  el comentario viejo decía que `ARAnchorManager` lo limpiaba solo y es falso (doc del
  paquete AR Foundation, "Remove an anchor": hay que destruir el componente o llamar
  `TryRemoveAnchor`). Solo se acumula durante la sesión. El punto donde iría el arreglo es
  `WallAnchorManager.DoEraseAndRecreate` (ya tiene el ancla correcta de esa copia) y el
  retiro en `EraseArtworkStorageEntry`.
- **Build y deploy al Quest.** APK construido desde el editor (`Build Finished, Result:
  Success`, `ArtUnbound_v1.apk` 591 MB, 19:42) e instalado con `adb install -r` (conserva
  `save.json`); el visor reporta `lastUpdateTime` 19:42:47, versión 2.2.1. Receta en P-015.
- **Fase A retirada.** Pedía asignar el `icon` de `upgrade_cedula/frame/lamp`, pero esos
  assets son `hangable: 1` con prefab de placa: Collection los muestra con la miniatura 3D y
  nunca lee `icon`. Revisado en código que sus placas sí se otorgan:
  `CollectibleService.EvaluateOnCompletion` las da cuando las obras completadas llegan a
  10/25/50 (tipos Cedula/Frame/Lamp, mismos umbrales que `ProgressionRules`), al terminar un
  puzzle y al cargar el save. **No probado en el visor** (requiere un save con 10+ obras). El
  cheat `enableDevCheats` desbloquea cédula y lámpara en los cuadros pero NO otorga estas
  placas: con él salen con candado en Collection, y es lo esperado. Se corrigieron los
  tooltips de `hangable` e `icon` en `CollectibleDefinition`, que decían lo contrario.

**Qué NO se hizo y por qué:** no se arregló la fuga de `ARAnchor` (cambia el comportamiento
de anclas en el dispositivo y este build es el de las capturas; decisión del usuario
pendiente); no se revisó el reporte detallado del build (3.4 MB), solo el resultado del log y
la instalación; no se tocó `enableDevCheats`.

### Decisiones
- **DS.9** Las mejoras Cédula/Marco/Lámpara se quedan como placas colgables en Collection
  (miniatura 3D). Descartado volverlas no colgables con icono Sprite: era lo que asumía la
  Fase A, pero el catálogo se generó con `hangable = true` y el usuario lo prefiere así.
- **DS.10** El registro de anclas vivas se mantuvo (ahora por instancia) en vez de borrarlo
  aunque hoy no tenga efecto visible: es lo que hace falta para liberar el ancla correcta
  cuando se arregle la fuga.

## Trabajo 2026-11-25 (APK instalado en el Quest 2 para las capturas)

Sesión de preparación; no cierra ninguna fase y **no se tocó código, escena ni assets**.

- **APK instalado en el Quest 2.** El usuario hará las capturas en los dos visores. El Quest 2
  (serie `1WMHH843T01084`) no tenía Art Unbound; se instaló `ArtUnbound_v1.apk` del 2026-11-22
  19:42 con `adb install -r` (`Success`). El visor reporta 2.2.1, `firstInstallTime` y
  `lastUpdateTime` 2026-11-25 00:25:53. El Quest 3 (serie `2G0YC5ZF9K0BJ0`) sigue con el mismo
  APK (`lastUpdateTime` 2026-11-22 19:42:47).
- **Revisado en el APK y en código, sin ejecutar:** el manifest declara
  `com.oculus.supportedDevices = quest2|questpro|quest3|quest3s`; `minSdk` 32 y el Quest 2
  está en SDK 34. `GameBootstrap.DetectDevice()` solo trata como MR a Quest 3, 3S y Pro: el
  Quest 2 arranca con `shouldStartInVR = true` y entra por `TransitionToVRGallery()` sin
  pasar por passthrough.

**Pendiente de verificar en el Quest 2 (nunca se ha abierto la app ahí):**
- Arranque en frío directo a VR: logcat debe decir `[GameBootstrap] Device: Quest3/Pro=False`
  y la galería debe quedar alineada al menú. Si sale el warning `El menu no se coloco a
  tiempo`, ese es el camino que falló (en el Quest 3 siempre se llega a VR desde MR, con el
  menú ya colocado).
- Rendimiento de la galería VR (no hay ninguna medición en ese visor).
- Si el botón MR queda oculto: no se revisó qué hace `SetVRButtonsMode(true, false)`.
- Qué pasa si se arranca en manos: el aviso que exige controles para VR solo está en el botón
  VR de `NativeGalleryController`, y en el Quest 2 se entra a VR sin pasar por ese botón.
- Empieza sin `save.json`: sale el tutorial y no hay obras completadas ni colgadas; para las
  capturas hay que poblarlo con el autocompletar del cheat.

**Qué NO se hizo y por qué:** no se abrió la app en el Quest 2 ni se grabó logcat (el usuario
cerró la sesión antes de probar); no se compiló nada porque no cambió código.

## Trabajo 2026-11-25, segunda tanda (GDD actualizado a v6.1)

Sesión de documentación. No cierra ninguna fase y no tocó código, escena ni assets.

**Qué se hizo:** `docs/GDD-Art-Unbound-v6.md` pasó a **v6.1 (noviembre 2026)** para reflejar
lo implementado, cotejado contra código, escena y assets (no contra este plan). Trae un bloque
"Cambios respecto a 6.0" al inicio. Lo principal: MR en Quest 3/3S/Pro y Quest 2 directo a VR;
lienzo y menú en un ancla única a 0.75 m; post-juego sin "Hang it now"/"Back to collection"
(solo "Play Again", el cuadro queda listo para tomarse); Collection lista solo placas y mejoras
(las obras se cuelgan con "Hang on Wall" desde su detalle); Settings con hápticos y toggle del
tutorial; tutorial de mano fantasma (§4.9); multi-copia; recentrado y cambio manos/controles
(§11.8); lámpara sin baño de luz; chips de filtro y "Browse all" marcados como no
implementados; catálogo de 251 obras y Apéndice A regenerado desde `ArtworkCatalog.asset`
(antes difería en 170 filas).

**Qué quedó probado:** compilación en batchmode con Unity 6000.3.10f1, exit 0 y 0 `error CS`
(no hay tests). El apéndice se cotejó por script contra los assets: 251 filas, 0 diferencias.
No hay nada que verificar en el visor: es solo documentación.

**Hallazgo, NO corregido (decisión del usuario):** dos placas no se pueden obtener.
`master_author_johannes_vermeer` tiene `threshold: 11` y el catálogo tiene 10 Vermeer;
`grandmaster_movement_dutch_golden_age` tiene `threshold: 36` y hay 35 obras. Quedaron así al
sacar el Vermeer duplicado (`21fc52f`); `CollectibleService.IsThresholdMet` compara con `>=`.
Los assets viven en `Assets/ArtUnbound/Data/Collectibles/`. El GDD lleva los conteos reales y
una nota de "pendiente de implementación". No se corrigió porque es dato de juego, no doc.

**Qué NO se hizo y por qué:** quedaron sin verificar en el GDD, tal cual estaban, el precio de
$9.99 USD (vive en el dashboard de Meta), las dimensiones 12×8×4 m de la galería y que las 12
gratuitas salgan primero. No se actualizó el `CLAUDE.md` del proyecto (rancio) ni los
comentarios de `CollectibleKind.cs`/`PopulateCollection` que dicen que las mejoras no son
colgables: fuera del alcance pedido.

---

## FASE A — Últimos pendientes del Editor (RETIRADA 2026-11-22, no aplica)

Pedía asignar el campo `icon` de las tres mejoras. No aplica: son placas colgables y
Collection no usa ese campo (ver DS.9). No se hizo ningún trabajo; no cuenta como completada.

**Estado:** ✖ Retirada

---

## FASE B — Playthrough de verificación end-to-end

**Objetivo:** validar los frentes GDD v6 en ejecución real (no hay tests automatizados
para esto; ver la sección "Verificacion end-to-end" del doc de alineación).

**Salida verificable (Play Mode / Quest):**
- 0 errores en consola al abrir el proyecto.
- Completar una obra → card con palomita (no medalla); post-juego "Artwork Complete"
  sin medalla, con "Play Again" como único botón y el cuadro listo para tomarse y colgarse
  (corregido 2026-11-25: "Hang it now" / "Back to collection" ya no existen).
- Colgar la obra (MR y VR) → persiste al reabrir; con 10/25/50 obras (o forzando
  `save.json`) aparecen cédula/marco/lámpara y los toggles de Config las controlan.
- Cruzar umbral de autor/movimiento → se otorga la placa; se cuelga desde Collection.
- Collection lista placas y mejoras, obtenidas y bloqueadas con su condición (corregido
  2026-11-25: las obras completadas ya no salen ahí, se cuelgan desde su detalle).
- Compra ya verificada en device ✅ (re-verificar solo la restauración: reabrir la app
  tras comprar → `GetViewerPurchases` regresa 1 compra y el catálogo sigue desbloqueado).
- **Tutorial (post-corte):** en primer arranque (o `save.json` limpio) aparece la mano
  fantasma guiando el primer snap; el toggle de Settings lo re-arma en caliente.
- **Mano izquierda (post-corte):** pinch, drag y scroll del tray funcionan con la
  izquierda igual que con la derecha.
- **Multi-copia MR (post-corte):** colgar la misma obra 2+ veces → cada copia persiste
  y se reposiciona de forma independiente al reabrir.
- **Fixes 2026-10-29:** los 4 puntos "Mirar" de esa sección + el ancla única de UI
  (menú y board aparecen a distancia/altura cómodas al arrancar).

**Estado:** ☐ Pendiente

---

## FASE C — Cierre de IAP (dashboard)

**Trabajo:**
- Confirmar en el Meta Dashboard que el add-on `catalog_complete` es **Durable**
  (no Consumible). Si fuera consumible, `GetViewerPurchases` no restaura y Meta
  auto-reembolsa a los 3 días.

**Salida verificable:** captura del dashboard con tipo Durable + reabrir la app tras la
compra y ver el catálogo desbloqueado sin re-comprar.

**Estado:** ☐ Pendiente (la compra en sí ya funcionó; falta confirmar tipo/restauración)

---

## Siguiente paso concreto

Primero, abrir la app en el **Quest 2** con controles y logcat grabando (P-012, con
`-s 1WMHH843T01084`) y revisar los puntos de "Trabajo 2026-11-25": es instalación limpia y
ahí arranca directo en VR, camino que nunca se ha probado.

Después, con el APK del 2026-11-22 19:42 que ya está en los dos visores (botón "DEV" ya
verificado fuera en el Quest 3): tomar las 5 capturas de 2560×1440, confirmar que las lámparas ya no pintan luz sobre la
pintura, reposicionar una de dos copias de la misma obra, y recorrer los puntos de "Trabajo
2026-11-16" y los 4 "Mirar" de "Fixes 2026-10-29" con logcat grabando desde antes (P-012).
Eso es la Fase B. Si algo falla, abrir sesión con el log. Después de las capturas: decidir el
arreglo de la fuga de `ARAnchor` y la Fase C (dashboard). En paralelo, para la tienda: el
tráiler.

## Arte de publicación — Meta Horizon Store

Flujo de arte separado de las fases de verificación del juego. Las capturas de Quest
proporcionadas son referencia visual únicamente; las capturas oficiales las hará el
usuario más adelante. (Desde el 2026-11-18 el arte ya está subido al Dashboard y el ícono
asignado en Unity; ver "Trabajo 2026-11-18".)

- **Fase Arte 1 — Concepto (completada).** Generar portada e ícono de Art Unbound.
  **Salida verificable:** abrir los dos bocetos y aprobar identidad, título y
  legibilidad del ícono a tamaño pequeño.
  **Avance:** portada `GeneratedAssets/StoreConcepts/art-unbound-cover-v1.png`
  y el ícono tipográfico `GeneratedAssets/StoreConcepts/art-unbound-icon-v2.png`
  aprobados por el usuario. El ícono ilustrado v1 se descartó.
- **Fase Arte 2 — Exportación (en curso).** Adaptar el concepto aprobado a Hero
  3000×900, Landscape 2560×1440, Square 1440×1440, Portrait 1008×1440,
  Mini Landscape 1080×360 e ícono 512×512. Evaluar logo transparente e ícono
  espacial. **Salida verificable:** verificar dimensiones y canales de los PNG
  y revisar recortes y zonas seguras en el Dashboard.
  **Avance:** 10 PNG en `GeneratedAssets/MetaHorizonStore/` y paquete
  `GeneratedAssets/art-unbound-meta-store-art.zip` (10 entradas verificadas):
  cinco portadas, ícono de tienda, miniatura de tráiler, logo transparente
  y dos capas de ícono espacial. Se verificaron dimensiones/canales de los
  10 PNG; el primer plano se probó compuesto y respeta el margen de 18 px.
  No se cambió la portada maestra ni se usaron capturas como arte.
  **Corrección 2026-11-18:** el Dashboard del usuario exige capas espaciales
  de 512×512, no 180×180. Exportadas desde los másteres a
  `GeneratedAssets/art-unbound-meta-store-art/art-unbound-spatial-foreground-512.png`
  (RGBA) y `art-unbound-spatial-background-512.png` (RGB); medidas y cabeceras
  verificadas. Usar estas dos en lugar de las versiones de 180 del ZIP viejo.
  **Logo pendiente:** el logo con contorno tiene alfa sucio; los intentos de
  regeneración también dejaron residuos. No se sobrescribió el anterior.
  **Ajolote Studios (variantes creadas, pendiente aprobación):** revisar/limpiar el logo de
  `Assets/ArtUnbound/UI/Branding/`. La variante Transparente tiene borde
  blanco y texto negro; actualmente es la usada en el splash. Crear una
  variantes nuevas `LogoAjoloteStudiosTransparentClean.png` (texto negro) y
  `LogoAjoloteStudiosTransparentDarkClean.png` (texto rosa), generadas con
  ImageGen desde el original. Composición revisada sobre blanco, gris y negro;
  sin borde blanco visible a tamaño de uso. Persiste alfa residual casi
  invisible en el generado: no se afirma limpieza matemática del canal alfa.
  El usuario aprobó el texto rosa y pidió reemplazar el transparente:
  `LogoAjoloteStudiosLargeTransparente.png` ahora coincide con la variante rosa
  (hash verificado); su .meta quedó intacto para preservar referencias del splash.
  Respaldo en `GeneratedAssets/BrandingBackup/LogoAjoloteStudiosLargeTransparente-before-pink.png`.
  Splash probado en Quest el 2026-11-18: el usuario lo vio bien (fondo blanco).
  **Estado 2026-11-18:** el usuario subió el arte al Developer Dashboard. Falta
  que confirme que cada casilla quedó validada y revise zonas seguras y la
  máscara del ícono espacial ahí mismo. Después, Fase Arte 3.
- **Fase Arte 3 — Integración y capturas (en curso).** Asignar el ícono del
  build en Unity y preparar cinco tomas reales cuando el usuario las aporte.
  **Salida verificable:** comprobar el ícono en una build de Quest y que las
  cinco capturas distintas de 2560×1440 pasen la validación del Dashboard.
  **Avance:** ícono asignado y visto en el visor (2026-11-18). Faltan las cinco
  capturas y el tráiler (MP4, 30 s a 2 min).

## Pulido opcional (todo funciona con fallback — hacer solo si sobra tiempo)

- Material/malla **Platino** dedicados en `FrameConfigSet`, `PlacedArtworkController` y
  `FrameAnimationController` (hoy caen a Oro); `frameMaderaMaterial` en `PuzzleBoard`
  (hoy color madera procedural).
- Estilo/tamaño de los 3 toggles de presentación en Config; escala real de cédula y
  lámpara; hornear luces (lightmaps) para Quest; estilo del botón "Back to collection".
- Refresco en vivo de toggles sobre obras ya colgadas (hoy aplican al re-instanciar).
- Auditoría (opcionales): autor de Henry VIII → "After Hans Holbein (workshop)"; reponer
  una obra para volver a 252; borrar el `.asset` huérfano de "View of Houses in Delft".
- Borrar prefabs huérfanos del Store: `PackSectionItem.prefab`, `BundleSectionItem.prefab`.

## Diferido por decisión (no es deuda)

- Chips de filtro en la búsqueda (Frente 8).
- GDD Fase 2 (sección 15): social, co-op, galerías temáticas.
- GDD sección 14: cloud, Remote Config, Cuadro Semanal.
