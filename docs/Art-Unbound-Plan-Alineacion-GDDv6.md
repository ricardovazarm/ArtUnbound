# Art Unbound — Plan de Alineacion GDD v6 (Checklist de Implementacion)

> Documento de seguimiento. Marca `[x]` cada tarea al completarla.
> Origen: revision discrepancia-por-discrepancia de `docs/GDD-Art-Unbound-v6.md` contra el
> codigo, con decisiones tomadas con el usuario (junio 2026).

## Orden acordado de ejecucion
Los frentes 1-7 son una capa de progresion muy acoplada. Se construye primero el **Frente 0
(datos)**, luego la **limpieza (6)**, luego UI/logica. Orden definido:

**0 -> 6 -> 1 -> 2 -> 3 -> 4 -> 5 -> 7**

1. **Frente 0** — Modelo de datos de progresion (base comun).
2. **Frente 6** — Limpieza de monetizacion (quitar Store/packs antes de tocar UI).
3. **Frente 1** — Recompensa = palomita (quitar medallas).
4. **Frente 2** — Tier global como material.
5. **Frente 3** — Cedula / Marco / Lampara.
6. **Frente 4** — Sistema de Placas.
7. **Frente 5** — Seccion Collection.
8. **Frente 7** — Post-juego sin medalla.

---

## Pendientes manuales en el Editor (consolidado)
> Todo el codigo y el cableado critico de escena ya estan hechos. Esto es lo que QUEDA por hacer a mano en Unity.

**Necesario:**
- [ ] **Coleccionables (assets):** correr `Tools/ArtUnbound/Generate Collectibles` (crea los assets `CollectibleDefinition` + `CollectibleCatalog.asset` en ingles) y **asignar el `CollectibleCatalog.asset`** al campo `collectibleCatalog` del `GameBootstrap` en la escena. Sin esto, Collection no lista placas/mejoras y no se otorgan placas. (Opcional: asignar `icon`/`visualPrefab` por coleccionable y reordenar el catalogo.)
- [ ] **Prefab de placa:** correr `Tools/ArtUnbound/Create Plaque In Scene` (crea la placa con Real Material `RM Brass common` + 2 TMP + 4 remaches, todo cableado a `CollectibleLabel` incl. `tieredRenderers`), **asignar los 4 materiales por tier** (Cobre/Plata/Oro/Platino) en el `CollectibleLabel`, ajustarla y arrastrarla a Project para generar el prefab; luego asignar ese prefab al campo `visualPrefab` de los `CollectibleDefinition` colgables. En runtime `CollectibleFactory` inyecta titulo/condicion (`CollectibleLabel.Apply`) y el acabado del tier de estatus (`CollectibleLabel.ApplyTier`, igual que la cedula). Si dejas un material de tier vacio, conserva el del prefab. *(Si reusas el prefab que ya creaste sin recrearlo: solo abrelo y asigna los 4 materiales en su `CollectibleLabel`; `ApplyTier` usa todos los renderers del prefab si `tieredRenderers` esta vacio.)*
- [ ] **Prefab de cedula:** correr `Tools/ArtUnbound/Create Cedula In Scene` (placa + 3 renglones: Titulo Cormorant SemiBold 23/`#241B12`, Autor Cormorant Italic 16/`#2E2418`, Ano-Movimiento-Museo Hanken Regular 11/`#4A3A28`, cableados a `CedulaLabel`), **asignar los 4 materiales por tier** (Cobre/Plata/Oro/Platino) en el `CedulaLabel`, arrastrarla a Project para el prefab y asignar ese prefab al campo `cedulaPrefab` del `GameBootstrap`. En runtime `PresentationDecorator` clona la cedula bajo cada obra colgada, le inyecta su info (`CedulaLabel.Apply`) y aplica el material del tier de estatus (`CedulaLabel.ApplyTier`). Sin prefab, usa el molde procedural anterior. **Verificar orientacion** del texto al colgar (si queda al reves, cambiar el yaw del clon en `PresentationDecorator.BuildCedula`).
- [ ] **Prefab de placa de estatus:** correr `Tools/ArtUnbound/Create Status Plaque In Scene` (overline estatico "Your museum at a glance" + hero del rango + divisor + agregados, cableados a `CollectibleLabel`), **asignar los 4 materiales por tier**, arrastrarla a Project y asignar el prefab al campo `visualPrefab` del asset **`status`**. En runtime `CollectibleFactory` (caso `Status`) inyecta el rango (`GetStatusRank`) en el hero y los agregados (`GetStatusAggregates`) en el subtitulo, y aplica el material del tier de estatus. Sin prefab cae al molde procedural (rango + agregados en 2 lineas).
- [ ] **Iconos de upgrades en Collection:** asignar el campo `icon` (Sprite) de `upgrade_cedula`/`upgrade_frame`/`upgrade_lamp`; en Collection los NO colgables muestran ese icono. Los colgables (placas/estatus) muestran una **miniatura 3D del propio asset** generada en runtime (`CollectiblePreviewRenderer`, render-to-texture; sin setup manual — el `RawImage` se crea solo clonando el rect del thumbnail). La miniatura sale correcta una vez asignado el `visualPrefab`; verificar que el **fondo del preview sea transparente** en URP (si sale negro, ajustar `backgroundColor`/pipeline).
- [ ] **Frente 6:** borrar el GameObject `StoreView` (y sus hijos Pack/Bundle) del prefab/escena del menu y quitar los componentes con "missing script" que dejaron los scripts borrados (StoreViewController, PackSectionUI, etc.). En el GameObject de `PackPurchaseService`, las refs de inspector a los catalogos pack/bundle quedan vacias (sin efecto).

**Opcional (pulido — todo funciona con fallback):**
- [ ] **Frente 2/3:** material/malla Platino dedicados en `PlacedArtworkController` y `FrameAnimationController` (hoy caen a Oro); entrada Platino en `FrameConfigSet`; asignar `frameMaderaMaterial` en `PuzzleBoard` (hoy color madera procedural).
- [ ] **Frente 3:** afinar estilo/tamano de los 3 toggles de Config (reusan el estilo del toggle de Hapticos con label Text legacy); ajustar tamano/posicion de cedula y lampara a escala real; valorar hornear las luces (lightmaps) para rendimiento en Quest.
- [ ] **Frente 7:** afinar el estilo del boton "Back to collection" duplicado si se desea.

**Ya hecho en el Editor (no requiere accion):** sprite `icon_completed` del badge (F1); 3 toggles de Config (F3); material `Frame_Platino.mat` + su slot en `PuzzleBoard` (F2); cableado del boton COLLECTION (F5); botones "Hang it now" / "Back to collection" del post-juego (F7).

**Antes de todo:** abrir Unity, confirmar 0 errores de consola, y hacer un playthrough (completar -> post-juego sin medalla -> colgar -> placas/Collection).

---

## Frente 0 — Modelo de datos de progresion (base comun)  ✅ HECHO
Cimiento de todos los demas. Vive en `SaveData` / `SaveDataService` / `Data/`.
- [x] **Tier global del jugador** Cobre->Plata->Oro->Platino — `Data/PlayerTier.cs` (nuevo enum; Platino incluido).
- [x] Funcion `GetPlayerTier()` + umbrales (1 / 10 / 25 / 50 / 100) — `Data/ProgressionRules.cs` + `SaveData.GetPlayerTier()` / `GetCompletedCount()`.
- [x] **Flags de presentacion** + toggles — `GameSettings.showCedula/showMarco/showLampara` + `ProgressionRules.Cedula/Marco/LamparaUnlocked()` + `SaveData.IsCedulaActive()/IsMarcoActive()/IsLamparaActive()` (desbloqueo derivado del conteo, no se duplica estado).
- [x] **Progreso por autor y por movimiento** — `Services/ProgressionStats.cs` (`CompletedInAuthor/Movement`, `CompletedByAuthor/Movement`, totales del catalogo).
- [x] **Modelo de Placas** + estado obtenida/fecha — `Data/PlaqueType.cs`, `Data/PlaqueDefinition.cs`, `Data/EarnedPlaque.cs` + `SaveData.earnedPlaques` / `HasPlaque()` / `GrantPlaque()`.
- [x] **Migracion de save**: campos nuevos son aditivos y retro-compatibles (JsonUtility + defaults; helpers null-safe). El retiro de `bestFrameTier` como recompensa se hace en Frente 2.

## Frente 1 — Recompensa = palomita (quitar medallas)  ✅ HECHO (codigo)
- [x] `ArtworkCardUI.Setup`: badge unico (palomita) — nueva firma `Setup(artwork, bool isCompleted, onTap, isLocked)`; eliminado el switch de sprite por `FrameTier` y el cache `_completedBadgeImage`. El badge solo se muestra/oculta; su sprite vive en el prefab.
- [x] `NativeGalleryController`: removidos los campos `bronzeMedal/silverMedal/goldMedal`; `SpawnCard` recibe `bool isCompleted`; `GetBestTier` reemplazado por `IsArtworkCompleted()` (via `HasBeenCompleted()`).
- [x] `lockBadge` (candado) intacto — sigue mostrandose con `isLocked`.
- [x] Prefab `ArtworkCard2`: el Image del `CompletedBadge` ya apunta al sprite **`icon_completed`** (palomita, guid `dbfe1261...`). Wireado directo en el `.prefab`. Los slots de medallas bronce/plata/oro desaparecen del inspector de NativeGallery (refs serializadas se descartan solas).

## Frente 2 — Tier global como material (desacople de dificultad)  ✅ HECHO (codigo)
- [x] `GetFrameTierFromDifficultyIndex` eliminado; reemplazado por `GameBootstrap.GetCurrentFrameTier()` que mapea el **tier global** (`SaveData.GetPlayerTier()`) -> `FrameTier` (Cobre->Bronce, Plata->Plata, Oro->Oro, Platino->Platino). Usado en el flujo de completado y en la restauracion del post-juego. El comentario stale de `selectedDifficultyIndex` actualizado (ahora solo define tamano de pieza).
- [x] Material de marco/placas deriva del tier global, no de la dificultad. En `OnPuzzleComplete` el tier se calcula **despues** de registrar la obra (incluye la recien completada).
- [x] **Platino** anadido a `FrameTier` (=4). Soportado en `PuzzleBoard.GetFrameMaterial` (`framePlatinoMaterial` nuevo + color fallback platino), `FrameAnimationController`, `PlacedArtworkFactory` (VR), `WallAnchorManager` (MR) y `PlacedArtworkController` (MR). Sin material/malla dedicada cae a Oro / color platino procedural.
- [x] Material `Frame_Platino.mat` creado (copia de Oro, color platino `0.88/0.90/0.94`, guid `b7c1f3a9...`) y cableado al slot `framePlatinoMaterial` de `PuzzleBoard` en `Main.unity`.
- [ ] **PENDIENTE (Editor, opcional):** entrada Platino en `FrameConfigSet` y malla/material Platino dedicados en `PlacedArtworkController`/`FrameAnimationController` (hoy caen a Oro como fallback).
- *Nota:* `bestFrameTier` por-obra queda como legacy (ya no lo lee nada para mostrar). La compuerta "el marco solo aparece a las 25 obras" es del **Frente 3**; aqui solo se cambio el origen del material.

## Frente 3 — Cedula / Marco / Lampara (mejoras de presentacion)  ✅ HECHO (codigo)
- [x] **Auto-desbloqueo** 10/25/50 — ya en Frente 0 (`ProgressionRules` + `SaveData.IsCedulaActive/IsMarcoActive/IsLamparaActive`), consumido por el nuevo `MR/PresentationDecorator`.
- [x] **Auto-aplicacion** a obras colgadas MR+VR — `PresentationDecorator.Decorate()` llamado al instanciar la obra en `VR/PlacedArtworkFactory` (VR) y `MR/WallAnchorManager.SpawnArtworkAtAnchor` (MR). Lee el `SaveData` GLOBAL (`GameBootstrap.Instance`), asi que se **re-aplica** cada vez que la obra se instancia (al colgar / al cargar la galeria) reflejando el tier actual.
- [x] **Marco gateado** — `GameBootstrap.GetCurrentFrameTier()` delega en `PresentationDecorator.CurrentFrameTier()`: Madera (base de madera) hasta las 25 obras / toggle off; tier global (Oro/Platino) si esta activo. `PuzzleBoard` ahora soporta Madera (`frameMaderaMaterial` + color madera).
- [x] **Toggles en Configuracion** — `cedulaToggle/marcoToggle/lamparaToggle` en `NativeGalleryController` (PopulateSettings + WireSettingsControls + `SavePresentationToggles()` que persiste en `GameSettings`).
- [x] **Assets procedurales** — cedula (placa coloreada por tier + TMP 3D titulo/autor en grabado oscuro `#241B12`), lampara (spot calido + fixture), marco gateado. Sin prefabs hechos a mano.
- [x] **Acabado por tier global** — `PresentationDecorator.FrameBarColor(tier)` y `CurrentFrameTier()`.
- [x] Los 3 GameObjects `Toggle` (CedulaToggle/MarcoToggle/LamparaToggle, con label) creados en `SettingsView` (dentro de su VerticalLayoutGroup) y cableados a `cedulaToggle/marcoToggle/lamparaToggle` en `NativeGalleryController`, editando `Main.unity` (duplicado del toggle de Hapticos con fileIDs nuevos).
- [ ] **PENDIENTE (Editor, opcional):** afinar estilo/tamano de los 3 toggles en el layout (hoy reusan el estilo del toggle de Hapticos con label Text legacy). Opcional: asignar `frameMaderaMaterial`/`framePlatinoMaterial` en `PuzzleBoard`; ajustar tamano/posicion de cedula y lampara a escala real; valorar hornear luces para Quest.
- *Nota:* el cambio de toggle **persiste** y surte efecto al re-instanciar las obras (re-entrar a la galeria / re-colgar). El refresco en vivo de obras ya colgadas queda como mejora futura.

## Frente 4 — Sistema de Placas (23 + estatus)  ✅ HECHO (codigo)
- [x] **Set 23 + estatus** generado de forma determinista desde el catalogo + reglas — `Services/PlaqueProvider.cs` (no hardcodea conteos; agrupa por autor/movimiento). 9 Maestro autor + 1 Gran Maestro VG + 7 Maestro mov + 4 Gran Maestro mov + 2 comportamiento + 1 estatus.
- [x] Umbrales **autor**: >=6 obras (+ Da Vinci 5); Van Gogh a 15; Gran Maestro solo Van Gogh (34).
- [x] Umbrales **movimiento**: >10 obras; grandes (>25) -> Maestro a 25 + Gran Maestro al completar; medianos (11-25) -> solo Maestro al completar.
- [x] **Comportamiento** (`hung_first`, `on_display`=5 obras colgadas A LA VEZ) y **estatus** (escalera global).
- [x] **Ajuste GDD 8.4/8.6 (rangos + On Display):** la placa `behavior_curator` se renombro a **On Display** (id estable por save-compat) con condicion "5 artworks on display at once"; `CollectibleService.HungCount` solo cuenta obras `isActive` (exhibidas a la vez, no acumuladas/retiradas). La placa de **estatus** muestra solo estado global: **rango dinamico** (`ProgressionRules.GetRankName`: Visitor/Collector/Connoisseur/Curator/Patron en 1/10/25/50/100) como hero + **agregados** (`CollectibleService.GetStatusAggregates`: "completadas / total · N plaques"), inyectados en `CollectibleFactory` (`ResolveDisplayText`, caso `Status`). Asset `status` renombrado a "Museum Status" (solo etiqueta de Collection/fallback).
- [x] **Evaluacion** — `Services/PlaqueService.cs`: `EvaluateOnCompletion()` (autor/movimiento/estatus) y `EvaluateOnHang()` (comportamiento), otorga via `SaveData.GrantPlaque` y devuelve las nuevas. Cableado en `GameBootstrap`: al completar (tras registrar) y en `OnFramePlaced`/`OnVRFramePlaced`. `GameBootstrap.LastEarnedPlaques` expone las recien otorgadas para el post-juego (Frente 7) y `GameBootstrap.PlaqueService` para Collection (Frente 5).
- [x] **Placa colgable** — `MR/PlaqueFactory.cs`: molde (quad) coloreado por tier global + texto TMP dinamico (titulo/condicion/fecha, grabado `#241B12`); tag `PlacedArtwork` para reutilizar grab/colgado. Un molde x 4 materiales (color por tier).
- [ ] **Frente 5** integrara la seleccion/colgado de placas desde Collection y su listado con condicion.

## Frente 5 — Seccion Collection (contenido)  ✅ HECHO (codigo + escena)
- [x] `Tab.Collection` anadido en `NativeGalleryController`. **Reutiliza el grid del catalogo** (mismo `catalogView`) cambiando lo que se puebla y ocultando la barra de busqueda — evita crear una vista nueva en escena.
- [x] **Obras completadas** -> tarjetas; tap dispara `OnHangArtworkRequested` -> `GameBootstrap` instancia la obra como objeto `PlacedArtwork` frente al usuario (reutiliza grab/colocacion). Sin detalle intermedio.
- [x] **Placas obtenidas** -> tarjetas (via `ArtworkCardUI.SetupGeneric`); tap dispara `OnHangPlaqueRequested` -> `PlaqueFactory.Build` frente al usuario (objeto colgable).
- [x] **No obtenidos** (placas no ganadas + mejoras cedula/marco/lampara) -> tarjetas bloqueadas con su **condicion** como subtitulo (placa: su `subtitleText`; mejora: "Complete N artworks").
- [x] Boton **COLLECTION** (era el viejo `BtnStore` relabeleado, quedo sin cablear al quitar el Store) reutilizado y cableado a `btnCollection` -> `SwitchTab(Tab.Collection)` (escena editada).
- *Nota:* la **persistencia al colgar desde Collection** (anclar un objeto recien instanciado) reusa el flujo de reposicion existente; afinar el primer-anclado es mejora futura. La barra inferior queda Home + Collection (Config/VR-MR arriba).

## Frente 6 — Limpieza de monetizacion (unlock unico)  ✅ HECHO (codigo)
- [x] Quitar `Tab.Store`, `storeView`, `btnStore`, `indicatorStore`, `StoreViewController` — limpiado en `NativeGalleryController`; `StoreViewController.cs` eliminado.
- [x] Quitar `BundleCatalog`, `ArtworkPackCatalog`, `PurchasePack`/`PurchaseBundle` y refs — eliminados 9 `.cs` (UI: `StoreViewController`, `PackSectionUI`, `BundleSectionUI`, `ArtworkInPackDetailController`, `PackInBundleDetailController`; Data: `ArtworkPackCatalog`, `BundleCatalog`, `ArtworkPackDefinition`, `BundleDefinition`) + sus `.meta` + assets huerfanos `ArtworkPackCatalog.asset` / `BundleCatalog.asset`. `PackPurchaseService` reescrito a solo-catalogo.
- [x] Conservar `IsCatalogPurchased()` + compra contextual **$9.99** — intactos (`PackPurchaseService.PurchaseCatalog` / `IsArtworkLocked(ArtworkDefinition)`; `SaveData.purchasedPackIds` se conserva como almacen del SKU del catalogo).
- [x] Limpiar referencias en `GameBootstrap` (campo `artworkPackCatalog` removido). Verificado: 0 referencias colgando en `.cs`.
- [x] (Las carpetas-pack se quedan como organizacion interna de assets.)
- [ ] **PENDIENTE (Editor, manual):** en el prefab/escena del menu, borrar el GameObject `StoreView` (y sus hijos Pack/Bundle) y quitar los componentes con "missing script" que dejaron los scripts borrados. En el GameObject de `PackPurchaseService`, las referencias de inspector a los catalogos pack/bundle quedan vacias (sin efecto).

## Frente 7 — Post-juego sin medalla  ✅ HECHO (codigo + escena)
- [x] `PostGameController` reescrito: sin `medalIcon`/sprites/`GetMedalDisplayName`. `medalText` -> `titleText` via `[FormerlySerializedAs]` (la escena remapea sola); `replayButton` -> `hangButton` igual.
- [x] Texto compuesto en `titleText`: **"Artwork Complete"** + linea tenue **"Ready to hang in your gallery"** (`<size>` para jerarquia).
- [x] Botones: **"Hang it now"** (primario -> `OnPlaceArtworkRequested`) y **"Back to collection"** (secundario -> nuevo `OnBackRequested` -> `GameBootstrap.OnBackFromPostGame` -> menu). En escena: el boton existente relabeleado a "Hang it now" + boton duplicado "Back to collection" cableado a `backButton`.
- [x] **Linea de hito condicional**: `GetMilestoneLine()` lee `GameBootstrap.LastEarnedPlaques` (Frente 4) y muestra "Milestone unlocked: {placa}" solo si esta obra otorgo una placa (prioriza placa con nombre sobre la de estatus).

---

## Estado global del plan
Frentes **0–7 implementados** (codigo + escena donde aplica). Pendientes por decision del usuario:
- **Chips de filtro** (Frente 8 diferido) — no ahora.
- **Pasada tipografica** — no es tarea (fuentes de referencia).
Fuera de alcance: Fase 2 (social/co-op/galerias tematicas).

---

## Ya hecho (referencia — no tocar)
- [x] Boton **Check** (resaltar incorrectas, 4.6) — `PuzzleHUDController` -> `GameBootstrap.HighlightWrongPiecesStaggered` -> `PuzzlePiece.PlayWiggleEffect`.
- [x] **Tamanos de pieza 8/6/4 cm** — `PuzzleConfig.asset` (0.08 / 0.04, tablero 0.6).
- [x] **Catalogo 252 obras** + botones    A Coffee/A Break/A Movie".
- [x] **Busqueda de texto** integrada + **estado vacio** ("No artworks found / Try another...").
- [x] **Nav** Home + Collection con gear/VR en barra superior (layout).
- [x] Morfologia determinista, hitos, colgado MR/VR, locomocion VR, audio, guardado JSON.

## Diferido (decision: no ahora)
- [ ] **Chips de filtro** (movimiento / artista / estado) sobre la busqueda.
- [ ] Pasada tipografica Cormorant/Hanken — *no es tarea* (fuentes solo de referencia).

## Fuera de alcance
- Fase 2 (GDD 15): social, co-op, placas de equipo, galerias tematicas.

---

## Verificacion end-to-end (por frente, en el Unity Editor)
Sin build/test CLI; todo se valida en Play (`Main.unity`) o en dispositivo:
- **F1/F7:** completar una obra -> card muestra palomita (no medalla); post-juego dice "Artwork Complete" sin medalla.
- **F2/F3:** alcanzar 10/25/50 obras (o forzar `SaveData`) -> se desbloquean cedula/marco/lampara y aparecen en obras colgadas; toggles en Config las prenden/apagan; material sube con el tier.
- **F4:** completar el umbral de un autor/movimiento -> se otorga la placa; colgable desde Collection; material segun tier global.
- **F5:** COLLECTION lista completadas + placas + bloqueados con condicion; colgar desde ahi funciona.
- **F6:** ya no existe Store; abrir obra bloqueada muestra boton de compra; comprar desbloquea todo.
