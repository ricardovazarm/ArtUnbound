# PLAN.md — Estado del proyecto (actualizado 2026-10-29)

## Handoff

- **Bloqueos:** ninguno. Árbol: solo `Main.unity` queda modificado sin commitear a propósito
  (`enableDevCheats: 1`, toggle de pruebas del usuario); todo lo demás está pusheado.
- **Decisiones que dependen del usuario:**
  - `AGENTS.md`, `.agents/` y `.codex/` (config de Codex) siguen sin trackear: commitear o ignorar.
  - Regresar `enableDevCheats` a 0 en `Main.unity` antes de cualquier build de release.
  - Las Fases A/B/C son 100% manuales (Editor / Quest / Meta Dashboard).
- **Pendientes chicos:** verificar en Quest los 4 fixes + el ancla de UI de la sección
  "Fixes 2026-10-29" (hay logcat para cada uno); en el playthrough de Fase B cubrir también
  tutorial, mano izquierda y multi-copia MR.

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

---

## FASE A — Últimos pendientes del Editor

**Trabajo:**
1. Asignar el campo `icon` (Sprite) de `upgrade_cedula.asset`, `upgrade_frame.asset` y
   `upgrade_lamp.asset` (hoy `{fileID: 0}`) — sin esto, las tarjetas de mejoras en
   Collection salen sin icono.

**Salida verificable (Play Mode):** abrir Collection → las 3 mejoras muestran su icono.

**Estado:** ☐ Pendiente

---

## FASE B — Playthrough de verificación end-to-end

**Objetivo:** validar los frentes GDD v6 en ejecución real (no hay tests automatizados
para esto; ver la sección "Verificacion end-to-end" del doc de alineación).

**Salida verificable (Play Mode / Quest):**
- 0 errores en consola al abrir el proyecto.
- Completar una obra → card con palomita (no medalla); post-juego "Artwork Complete"
  sin medalla, con "Hang it now" / "Back to collection".
- Colgar la obra (MR y VR) → persiste al reabrir; con 10/25/50 obras (o forzando
  `save.json`) aparecen cédula/marco/lámpara y los toggles de Config las controlan.
- Cruzar umbral de autor/movimiento → se otorga la placa; se cuelga desde Collection.
- Collection lista completadas + placas + bloqueados con su condición.
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

Build & Run al Quest y recorrer los 4 puntos "Mirar" de "Fixes 2026-10-29" con `adb logcat`
filtrando `[HandTracking]`, `[GameBootstrap]` y `[WallAnchor]`. Si algo falla, abrir sesión
con el log. Si todo pasa, seguir con la Fase A (iconos en el Editor).

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
