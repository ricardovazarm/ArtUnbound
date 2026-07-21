# PLAN.md — Estado del proyecto (actualizado 2026-07-12)

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
