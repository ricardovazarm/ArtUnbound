# PLAN.md — Implementación de cambios GDD v4 / Decisiones de Diseño v1.2

> Fuente de verdad del avance. Al iniciar cada sesión, leer este archivo y retomar desde
> la primera fase no completada. No avanzar de fase hasta que la anterior esté marcada
> como completada (con nota de qué quedó probado).

## Contexto y supuestos (confirmados con el usuario)

- **Punto de partida fijo:** terminar la funcionalidad de **colgar cuadros en VR** (hay bugs en el flujo).
- **Alcance:** la **funcionalidad** del sistema de recompensas debe quedar **completa y data-driven**.
  El número de placas temáticas (pintor/movimiento) es **independiente del código**: se configura
  por datos (ScriptableObjects/catálogo), de modo que agregar/quitar placas no requiere recompilar.
- **IAP:** por ahora **solo el flag** de entitlement (`catalogUnlocked`) + gateo del tier gratuito +
  compra **stub**. La integración real con Meta Store queda diferida (fuera de este plan).
- **Fuera de alcance** (post-lanzamiento, sección 9.3 del doc): Cuadro Semanal/live-ops, cloud,
  multijugador, troceo del catálogo en packs.

## Estado verificado del código (ya implementado — NO rehacer)

- Snap "slot más cercano sin rechazo" + contadores correctas/incorrectas → `PuzzleBoard.TrySnapPiece` ✅
- Feature **"¿Cuáles están mal?"** (wiggle + burst rojo + sonido) → presente en `PuzzleBoard`,
  `PuzzleHUDController`, `PuzzlePiece`, `AudioManager` ✅
- Flujo VR de colocación de piezas (apuntar+gatillo) y de colgado (select→apuntar→fijar) → cableado
  en `InteractionManager` + `VRWallHangingController` + `GameBootstrap` (con bugs, ver Fase 0).
- Persistencia VR vía `GalleryPersistenceService` + `GalleryPaintingData` ✅ (con limitación de
  reconstrucción, ver Fase 0).

## Nota sobre "salida verificable" en este proyecto

No hay test runner JS/Java ni build CLI: el desarrollo es dentro del Unity Editor (ver CLAUDE.md).
Por eso cada fase define su salida como una de estas, según corresponda:
- **Test EditMode** (Unity Test Framework) para lógica pura sin escena → lo corres desde
  `Window > General > Test Runner > EditMode > Run All`. Aplica a fases con lógica (3).
- **Comprobación en Play Mode** (Editor) con pasos concretos y resultado observable.
- **Comprobación en dispositivo (Quest)** con pasos concretos (para interacción XR real).
- En todos los casos, requisito previo: **el proyecto compila sin errores** en la consola de Unity.

---

## FASE 0 — Terminar "colgar cuadros en VR" (punto de partida)

**Objetivo:** dejar el flujo de colgado VR 100% funcional: colocar un cuadro completado en una pared
de la galería, que persista entre sesiones reconstruyéndose con la obra real, y poder reposicionarlo/retirarlo.

**Trabajo:**
1. **Diagnóstico de los bugs del flujo** (colaborativo): reproducir en el Quest el flujo
   select→teleport→apuntar→fijar, capturando `logcat` (vía HzOSDevMCP `get_device_logcat` /
   `stream_device_logcat`) y screenshots. Identificar dónde se rompe (selección de marco, raycast a
   `VRWall`, instanciado del clon, guardado, o transición de UI).
   - Puntos sospechosos a revisar: `InteractionManager` HANG phase (path VR, líneas ~793-808),
     `VRWallHangingController.RaycastToWall/PlaceFrameAtWall`, `GameBootstrap.OnVRFramePlaced`,
     y el `vrWallLayerMask`/layer `VRWall` de la galería.
2. **Arreglar** los bugs encontrados (código, no parches que oculten el fallo).
3. **Reconstrucción real al recargar:** `VRGalleryController.SpawnSavedPaintings` hoy instancia un
   prefab placeholder. Hacer que el cuadro recargado muestre la **textura/marco de la obra real**
   (resolviendo `artworkId` contra el catálogo). *(El mismo TODO existe en MR — ver Fase 4 para
   unificar el reconstructor; aquí basta dejar VR correcto.)*
4. **Reposicionar/retirar** un cuadro ya colgado en VR (paridad con el path MR
   `ControllerRepositionWallArtwork`): agarrar un cuadro colgado, moverlo a otra pared o retirarlo,
   actualizando `GalleryPersistenceService`.

**Salida verificable (en Quest):**
- Completar un puzzle en modo VR → colgarlo en una pared de la galería **sin errores en logcat**.
- Cerrar y reabrir la app: el cuadro **reaparece en la misma posición con la imagen real de la obra**.
- Agarrar el cuadro colgado, moverlo a otra pared y soltarlo → queda en la nueva posición; reabrir la
  app confirma la nueva posición. Retirarlo (soltar lejos de pared) → desaparece y no reaparece al reabrir.

**Estado:** ☐ Pendiente

---

## FASE 1 — Reencuadre de tamaños (Relajado / Intermedio / Maratón)

**Objetivo:** pasar de "dificultades" a **compromisos de tiempo** y rebalancear Maratón.

**Trabajo:**
- Renombrar las tres opciones en UI a **Relajado / Intermedio / Maratón** (`NativeGalleryController`
  / `DetailPanel`, strings ASCII sin acentos en assets/ScriptableObjects — ver memoria de proyecto).
- Rebalancear **Maratón** de ~256 a **~150-180** piezas (ajuste en `PuzzleConfig.asset` /
  conteos de dificultad; validar con la skill `calc-piezas`).
- Registrar **récord de tiempo por tamaño** (ya existe `recordsByPieceCount`); confirmar que el
  tamaño elegido **no** afecta logros (size-agnostic) — esto se consume en Fase 3.

**Salida verificable (Play Mode):**
- El menú de detalle muestra los tres nombres nuevos con su récord de tiempo por tamaño.
- Iniciar **Maratón** genera un grid de **~150-180** piezas (verificable con `calc-piezas` y en pantalla).
- Compila sin errores.

**Estado:** ☐ Pendiente

---

## FASE 2 — Marco constante + medalla de completación binaria

**Objetivo:** eliminar la jerarquía bronce/plata/oro **a nivel de cuadro individual**.

**Trabajo:**
- **Marco único y constante** para todo cuadro completado, sin importar el tamaño.
  Quitar la selección de marco por dificultad (`GameBootstrap.GetFrameTierFromDifficultyIndex` y el
  uso de `frameBronce/Plata/OroMaterial` en `PuzzleBoard`). Dejar un solo material/marco.
- **Medalla de completación binaria** en la galería del menú: "armada / no armada", sin variar por
  tier (`NativeGalleryController`).
- Introducir un enum **`StatusTier`** (`Bronce→Plata→Oro→Platino`) **solo para la placa de estatus**
  (Fase 6). Marcar `FrameTier` por-obra como obsoleto a nivel de cuadro (conservar el campo
  `ArtworkProgress.bestFrameTier` para compatibilidad de save; ya no se usa para el marco).

**Salida verificable (Play Mode):**
- Completar la misma obra en Relajado y en Maratón produce **el mismo marco** en ambos casos.
- En la galería, una obra completada muestra **una sola** medalla (idéntica) independientemente del tamaño.
- Compila sin errores.

**Estado:** ☐ Pendiente

---

## FASE 3 — Capa de datos y lógica de logros (data-driven)

**Objetivo:** el "cerebro" de la progresión, **completo y configurable por datos**. El número de
placas no está hardcodeado: se define en ScriptableObjects.

**Trabajo:**
- **Definiciones por datos:**
  - `RewardDefinition` (ScriptableObject base) con tipo: `UniqueCosmetic` (lámpara, cédula),
    `ThematicPlaque` (por `author` o por `artMovement` + umbral), `StatusPlaque` (por tier/nº puzzles).
  - `RewardCatalog` (ScriptableObject) que lista todas las `RewardDefinition`. **Agregar/quitar placas
    = editar este asset, sin tocar código.**
- **Servicio de logros** (`AchievementService`, namespace `Services`):
  - Calcula obras **distintas** completadas (size-agnostic) desde `SaveData.completedArtworks`/`progressByArtwork`.
  - Cuenta por `author` y por `artMovement` cruzando contra el catálogo (`ArtworkCatalog`/`LocalCatalogService`;
    metadata `author`/`artMovement` ya existe en los `ArtworkDefinition` y en `artunbound_collection.json`).
  - Resuelve desbloqueo de placa temática: **≥ umbral** (default 12) o **todas** si el pool < umbral.
  - Resuelve **nivel** (nº de puzzles armados) y **tier de estatus** (`StatusTier`) por umbrales configurables.
  - Resuelve desbloqueo de lámpara y cédula por su condición configurada.
  - Expone `OnRewardUnlocked` y se invoca al completar puzzle (desde `GameBootstrap` post-completion).
- **Persistencia:** extender `SaveData` con set de recompensas desbloqueadas + orden cronológico
  (para el cuerpo dinámico de la placa de estatus). Guardado en `OnApplicationPause/Quit` (ya existe).

**Salida verificable (Test EditMode — `Run All` en Test Runner):**
- Tests con assertions reales sobre la lógica (cumpliendo la regla "si meto un bug, un test falla"):
  - Dado un set de obras completadas que alcanza 12 de un autor → la placa de ese autor sale desbloqueada;
    con 11 → NO (salvo que el pool del autor sea ≤ 11, entonces sí con "todas").
  - Pool pequeño (p.ej. Rembrandt con 10 en catálogo) → umbral = todas (10); 9 → no, 10 → sí.
  - Nº de puzzles armados cruza umbrales → `StatusTier` correcto (Bronce/Plata/Oro/Platino).
  - El tamaño (Relajado/Intermedio/Maratón) **no** cambia el conteo (size-agnostic).
  - Cambiar el `RewardCatalog` (agregar una placa nueva) → el servicio la evalúa sin cambios de código.
- Compila sin errores; todos los tests EditMode en verde.

**Estado:** ☐ Pendiente

---

## FASE 4 — Objetos de recompensa colgables + cosméticos sobre cuadros

**Objetivo:** materializar las recompensas como objetos **visibles en el espacio**, reutilizando el
sistema de colgado (MR: anclas; VR: JSON).

**Trabajo:**
- **Reconstructor unificado de objetos colgables:** un componente/factory que, dado un `RewardDefinition`
  o un `artworkId`, instancia el objeto correcto con su arte. Unifica el TODO de "placeholder quads"
  en MR (`WallAnchorManager.LoadAndSpawnAnchors`) y el de VR (Fase 0) para reconstruir el objeto real.
- **Placas temáticas y placa de estatus como objetos colgables:** entran a los assets del jugador al
  desbloquearse; se cuelgan con el **mismo flujo** que un cuadro (MR y VR), incluyendo persistencia.
- **Lámpara y cédula** como atributos visuales que aparecen **encima/debajo de cada cuadro colgado**
  una vez desbloqueados (no objetos independientes). Se aplican a los cuadros ya colgados al reconstruir.

**Salida verificable (Quest, y Play Mode donde aplique):**
- Con una placa desbloqueada (forzar vía datos de prueba): aparece como objeto agarrable y se cuelga
  en pared (MR) y en galería (VR); persiste al reabrir.
- Con lámpara/cédula desbloqueadas: cada cuadro colgado muestra la lámpara encima y la cédula
  (título+artista) debajo; sin desbloquear, no aparecen.
- Compila sin errores.

**Estado:** ☐ Pendiente

---

## FASE 5 — Galería de Recompensas (catálogo en menú)

**Objetivo:** el compendio/manual de descubrimiento en el menú.

**Trabajo:**
- Nueva sección de UI ("Galería de Recompensas") que **lista todos** los objetos obtenibles leyendo el
  `RewardCatalog` (data-driven: refleja cualquier número de placas).
- Ítems obtenidos: mostrados como desbloqueados. Ítem **bloqueado**: al seleccionarlo, ventana con la
  **condición/logro** necesaria (este es el hogar de los requisitos, NO la placa del mundo).
- Conectado al estado real del `AchievementService`.

**Salida verificable (Play Mode):**
- Abrir la sección: se ven **todos** los ítems del `RewardCatalog` (obtenidos vs bloqueados según progreso real).
- Click en un bloqueado → ventana con su condición (p.ej. "Completa 12 obras de Van Gogh").
- Agregar una placa al `RewardCatalog` aparece automáticamente en la lista sin cambios de código.
- Compila sin errores.

**Estado:** ☐ Pendiente

---

## FASE 6 — Placa de estatus (composición + tiers drop-in)

**Objetivo:** el objeto-mundo que muestra el avance global con tiers acumulativos.

**Trabajo:**
- **Composición de dos partes:**
  - **Cabecera fija (determinista):** rango/título + agregados (p.ej. `X/252 obras · Y/N placas`) +
    logro cumbre.
  - **Cuerpo dinámico (cronológico):** medallones de logro poblados en el orden obtenido; pendientes
    como **slots vacíos en silueta** (sin texto de requisitos).
- **Tiers `StatusTier` Bronce→Plata→Oro→Platino** por nº de puzzles armados; **tamaño fijo idéntico**
  entre tiers → **drop-in replacement** (mismo punto de anclaje al subir de tier).
- Se desbloquea al **completar el primer rompecabezas**.

**Salida verificable (Quest/Play Mode):**
- Tras el primer puzzle, la placa de estatus está disponible y se cuelga (MR y VR).
- La cabecera muestra agregados correctos; los logros obtenidos a color y los pendientes en silueta.
- Forzar el cruce de un umbral de tier → la placa cambia de "piel" conservando el anclaje/posición.
- Compila sin errores.

**Estado:** ☐ Pendiente

---

## FASE 7 — F2P: flag de entitlement + gateo del tier gratuito (solo flag)

**Objetivo:** estructura Free-to-Play con desbloqueo único, **sin** integración real de tienda.

**Trabajo:**
- Flag `catalogUnlocked` (bool) en `SaveData` (default `false`).
- Marcar las **~12 obras gratuitas icónicas** (las candidatas del doc 2.3) como jugables siempre;
  el resto del catálogo gateado tras `catalogUnlocked` (consolidar con `requiresUnlock`/`PackPurchaseService`).
- **UI de desbloqueo**: pantalla "Desbloquear catálogo completo $9.99" con **compra stub** que setea
  `catalogUnlocked = true` (sin SDK real).
- **Retirar de la tienda** los 17 packs DLC + bundles como SKUs: ocultar su UI de compra
  (`PackPurchaseService`/`BundleCatalog`). **Los assets y el código permanecen dormidos** (decisión reversible).

**Salida verificable (Play Mode):**
- Save nuevo (borrar `save.json`): solo las ~12 obras gratuitas son jugables; las demás muestran prompt de desbloqueo.
- Ejecutar la compra stub → todo el catálogo (240+) queda jugable inmediatamente; persiste al reabrir.
- La UI ya **no** expone packs/bundles individuales como productos.
- Compila sin errores.

**Estado:** ☐ Pendiente

---

## Resumen de dependencias

```
Fase 0 (VR hanging) ─┐
Fase 1 (tamaños) ────┤
Fase 2 (marco/medalla)┤
Fase 3 (lógica logros)├──> Fase 4 (objetos colgables) ──> Fase 6 (placa estatus)
                      └──> Fase 5 (catálogo menú)
Fase 7 (F2P flag)  — independiente (puede ir al final)
```

- Fases 1 y 2 son contenidas y player-facing; preparan el terreno size-agnostic y el marco constante.
- Fase 3 es el núcleo lógico (con tests EditMode) del que dependen 4, 5 y 6.
- Fase 7 es independiente y de bajo riesgo (solo flag).
