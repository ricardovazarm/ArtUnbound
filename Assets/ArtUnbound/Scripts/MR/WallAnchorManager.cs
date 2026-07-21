using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using ArtUnbound.Data;
using ArtUnbound.Services;

using SerializableGuid = UnityEngine.XR.ARSubsystems.SerializableGuid;

namespace ArtUnbound.MR
{
    /// <summary>
    /// Manages persistent spatial anchors for artworks hung on walls.
    /// Uses AR Foundation's ARAnchorManager (TryAddAnchorAsync / TrySaveAnchorAsync /
    /// TryLoadAnchorAsync) which works correctly with the project's OpenXR setup.
    /// NOTE: OVRSpatialAnchor was the previous approach but requires OVRManager in the
    /// scene; this project uses AR Foundation + OpenXR, so OVRManager is absent.
    /// </summary>
    public class WallAnchorManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ARAnchorManager arAnchorManager;
        [SerializeField] private ArtworkCatalog artworkCatalog;

        private SaveDataService saveDataService;

        // artworkId → the live ARAnchor (only for artworks placed this session)
        private Dictionary<string, ARAnchor> activeAnchorsByArtworkId = new Dictionary<string, ARAnchor>();
        // artworkId → the frame GameObject parented to the anchor
        private Dictionary<string, GameObject> spawnedArtworks = new Dictionary<string, GameObject>();

        private bool _mrArtworksVisible = true;

        public event Action<string, Vector3> OnArtworkAnchored; // artworkId, position

        private void Awake()
        {
            if (arAnchorManager == null)
                arAnchorManager = FindFirstObjectByType<ARAnchorManager>();

            if (arAnchorManager == null)
                Debug.LogError("[WallAnchor] ARAnchorManager not found. Please add one to the XR Origin.");
        }

        public void Initialize(SaveDataService service)
        {
            saveDataService = service;
            Debug.Log("[WallAnchor] Initialized with SaveDataService");
        }

        // ─────────────────────────────────────────────────────────────────────
        //  CREATE & SAVE
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Places existingFrame on the wall and creates a persistent spatial anchor for it.
        /// Returns true immediately if parameters are valid; anchor save happens asynchronously.
        /// </summary>
        public bool CreateAnchor(string artworkId, Vector3 worldPosition, Quaternion worldRotation,
            FrameTier frameTier, Transform existingFrame = null,
            float boardWidth = 0.5f, float boardHeight = 0.5f)
        {
            if (string.IsNullOrEmpty(artworkId))
            {
                Debug.LogError("[WallAnchor] Cannot create anchor: artworkId is null or empty");
                return false;
            }
            if (existingFrame == null)
            {
                Debug.LogError("[WallAnchor] Cannot create anchor: existingFrame is null");
                return false;
            }
            if (arAnchorManager == null)
            {
                Debug.LogError("[WallAnchor] ARAnchorManager is null, cannot create anchor");
                return false;
            }

            Debug.Log($"[WallAnchor] CreateAnchor called for {artworkId} at {worldPosition}");

            // Tag the frame and register it immediately so the scene looks correct right away
            existingFrame.tag = "PlacedArtwork";
            // Ensure it's on PuzzlePiece layer so InteractionManager can detect it for repositioning
            int puzzlePieceLayer = LayerMask.NameToLayer("PuzzlePiece");
            if (puzzlePieceLayer >= 0) existingFrame.gameObject.layer = puzzlePieceLayer;
            var identifier = existingFrame.GetComponent<PlacedArtworkIdentifier>();
            if (identifier == null)
                identifier = existingFrame.gameObject.AddComponent<PlacedArtworkIdentifier>();
            identifier.artworkId = artworkId;
            spawnedArtworks[artworkId] = existingFrame.gameObject;

            // Create anchor asynchronously (fire-and-forget from caller's perspective)
            DoCreateAndSaveAnchor(artworkId, worldPosition, worldRotation, frameTier,
                                  existingFrame, boardWidth, boardHeight);

            OnArtworkAnchored?.Invoke(artworkId, worldPosition);
            return true;
        }

        private async void DoCreateAndSaveAnchor(string artworkId, Vector3 worldPosition,
            Quaternion worldRotation, FrameTier frameTier, Transform existingFrame,
            float boardWidth, float boardHeight)
        {
            // Create a temporary parent so the frame stays visible while we await
            var tempParent = new GameObject($"TempAnchor_{artworkId}");
            tempParent.transform.SetPositionAndRotation(worldPosition, worldRotation);
            existingFrame.SetParent(tempParent.transform, worldPositionStays: true);

            // Ask AR Foundation to create a spatial anchor at the wall pose
            var pose = new UnityEngine.Pose(worldPosition, worldRotation);
            var addResult = await arAnchorManager.TryAddAnchorAsync(pose);

            if (!addResult.status.IsSuccess())
            {
                Debug.LogError($"[WallAnchor] TryAddAnchorAsync failed for {artworkId}: {addResult.status}");
                // Keep temp parent so the artwork stays visible (unsaved)
                // Still ensure the BoxCollider is enabled so it can be grabbed
                if (existingFrame != null)
                {
                    var col = existingFrame.GetComponent<BoxCollider>();
                    if (col == null)
                    {
                        col = existingFrame.gameObject.AddComponent<BoxCollider>();
                        col.size = new Vector3(boardWidth, boardHeight, 0.05f);
                    }
                    col.enabled = true;
                }
                return;
            }

            ARAnchor anchor = addResult.value;
            activeAnchorsByArtworkId[artworkId] = anchor;

            // Re-parent to the real AR anchor (world position stays correct)
            existingFrame.SetParent(anchor.transform, worldPositionStays: true);
            Destroy(tempParent);

            // Ensure a grabbable BoxCollider exists and is enabled.
            // CleanupArtworkHanging() fires before this await completes and calls
            // DisableFrameGrab() → BoxCollider.enabled = false. We restore it here so
            // the placed artwork can be grabbed for repositioning (matches SpawnArtworkAtAnchor).
            if (existingFrame != null)
            {
                var col = existingFrame.GetComponent<BoxCollider>();
                if (col == null)
                {
                    col = existingFrame.gameObject.AddComponent<BoxCollider>();
                    col.size = new Vector3(boardWidth, boardHeight, 0.05f);
                }
                col.enabled = true;
            }

            Debug.Log($"[WallAnchor] Anchor created for {artworkId}, saving to device storage...");

            // Persist the anchor so it survives across sessions
            var saveResult = await arAnchorManager.TrySaveAnchorAsync(anchor);

            if (!saveResult.status.IsSuccess())
            {
                Debug.LogError($"[WallAnchor] TrySaveAnchorAsync failed for {artworkId}: {saveResult.status}");
                return;
            }

            SerializableGuid persistentGuid = saveResult.value;
            Debug.Log($"[WallAnchor] Anchor saved. Persistent GUID: {persistentGuid.guid}");

            // Record anchor data so we can reload it next session
            var anchoredArtwork = new AnchoredArtwork(
                artworkId,
                persistentGuid.guid.ToString(),
                Vector3.zero,        // local offset = zero (anchor IS at frame position)
                Quaternion.identity,
                1f,
                frameTier,
                boardWidth,
                boardHeight
            );
            saveDataService?.AddAnchoredArtwork(anchoredArtwork);

            // Sellar el GUID en el identifier de ESTA copia: reposicionar/quitar despues opera
            // solo sobre su entrada, sin pisar otras copias colgadas de la misma obra.
            if (existingFrame != null)
            {
                var idComp = existingFrame.GetComponent<PlacedArtworkIdentifier>();
                if (idComp != null) idComp.anchorId = persistentGuid.guid.ToString();
            }

            Debug.Log($"[WallAnchor] Anchor data saved for {artworkId}");
        }

        // ─────────────────────────────────────────────────────────────────────
        //  LOAD (called at app startup)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Loads all saved spatial anchors and spawns artwork at each position.
        /// </summary>
        public void LoadAndSpawnAnchors()
        {
            if (saveDataService == null)
            {
                Debug.LogWarning("[WallAnchor] SaveDataService is null, cannot load anchors");
                return;
            }

            var anchoredArtworks = saveDataService.GetAnchoredArtworks();
            if (anchoredArtworks == null || anchoredArtworks.Count == 0)
            {
                Debug.Log("[WallAnchor] No anchored artworks to load");
                return;
            }

            Debug.Log($"[WallAnchor] Loading {anchoredArtworks.Count} anchored artworks");

            foreach (var artworkData in anchoredArtworks)
            {
                DoLoadAndSpawnAnchor(artworkData);
            }
        }

        private async void DoLoadAndSpawnAnchor(AnchoredArtwork artworkData)
        {
            if (!Guid.TryParse(artworkData.anchorId, out Guid guid))
            {
                Debug.LogWarning($"[WallAnchor] Invalid anchor ID for {artworkData.artworkId}: '{artworkData.anchorId}'");
                return;
            }

            Debug.Log($"[WallAnchor] Loading anchor {guid} for {artworkData.artworkId}");

            var savedGuid = new SerializableGuid(guid);
            var loadResult = await arAnchorManager.TryLoadAnchorAsync(savedGuid);

            if (!loadResult.status.IsSuccess())
            {
                Debug.LogError($"[WallAnchor] TryLoadAnchorAsync failed for {artworkData.artworkId}: {loadResult.status}. Removing stale entry.");
                // Solo la entrada corrupta: otras copias de la misma obra pueden ser validas.
                saveDataService?.RemoveAnchoredArtworkByAnchorId(artworkData.anchorId);
                return;
            }

            ARAnchor anchor = loadResult.value;
            activeAnchorsByArtworkId[artworkData.artworkId] = anchor;

            Debug.Log($"[WallAnchor] Anchor loaded for {artworkData.artworkId}, trackingState: {anchor.trackingState}");

            // Spawn the artwork but keep it hidden until the anchor is fully localized.
            // Before localization, the anchor transform sits near the tracking origin (camera),
            // which would make the artwork appear to "follow the headset".
            GameObject artworkRoot = SpawnArtworkAtAnchor(artworkData, anchor.transform);

            if (artworkRoot != null)
            {
                if (anchor.trackingState == TrackingState.Tracking)
                {
                    // Already localized — show immediately
                    artworkRoot.SetActive(true);
                    Debug.Log($"[WallAnchor] Anchor already tracking, artwork shown for {artworkData.artworkId}");
                }
                else
                {
                    // Hide until the anchor localizes to avoid the floating-near-head artifact
                    artworkRoot.SetActive(false);
                    Debug.Log($"[WallAnchor] Waiting for anchor to localize before showing {artworkData.artworkId}");
                    StartCoroutine(WaitForAnchorTracking(anchor, artworkRoot, artworkData.artworkId));
                }
            }
        }

        private IEnumerator WaitForAnchorTracking(ARAnchor anchor, GameObject artworkRoot, string artworkId)
        {
            const float timeout = 30f;
            float elapsed = 0f;

            while (anchor != null && anchor.trackingState != TrackingState.Tracking && elapsed < timeout)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }

            if (artworkRoot == null) yield break;

            if (anchor != null && anchor.trackingState == TrackingState.Tracking)
                Debug.Log($"[WallAnchor] Anchor localized for {artworkId}, showing artwork");
            else
                Debug.LogWarning($"[WallAnchor] Anchor did not localize after {timeout}s for {artworkId}, showing anyway");

            artworkRoot.SetActive(_mrArtworksVisible);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  VISIBILITY (MR ↔ VR mode switch)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Shows or hides all MR-placed artworks. Call with false when entering VR mode,
        /// true when returning to MR. Newly localized anchors respect this flag too.
        /// </summary>
        public void SetMRVisible(bool visible)
        {
            _mrArtworksVisible = visible;
            foreach (var kvp in spawnedArtworks)
            {
                if (kvp.Value != null)
                    kvp.Value.SetActive(visible);
            }
            Debug.Log($"[WallAnchor] MR artworks {(visible ? "shown" : "hidden")} ({spawnedArtworks.Count} objects)");
        }

        // ─────────────────────────────────────────────────────────────────────
        //  SPAWN (reconstruct frame at anchor)
        // ─────────────────────────────────────────────────────────────────────

        /// <returns>The root GameObject of the spawned artwork (inactive — caller decides when to show it).</returns>
        private GameObject SpawnArtworkAtAnchor(AnchoredArtwork artworkData, Transform anchorTransform)
        {
            // Las placas (id "plaque_<id>") se reconstruyen con CollectibleFactory, no como quad de obra.
            if (!string.IsNullOrEmpty(artworkData.artworkId) && artworkData.artworkId.StartsWith("plaque_"))
                return SpawnPlaqueAtAnchor(artworkData, anchorTransform);

            if (artworkCatalog == null)
            {
                Debug.LogError("[WallAnchor] Missing ArtworkCatalog reference for spawning artwork");
                return null;
            }

            ArtworkDefinition artworkDef = artworkCatalog.artworks.Find(a => a.artworkId == artworkData.artworkId);
            if (artworkDef == null)
            {
                Debug.LogError($"[WallAnchor] Artwork definition not found for {artworkData.artworkId}");
                return null;
            }

            float w = artworkData.boardWidth > 0f ? artworkData.boardWidth : 0.5f;
            float h = artworkData.boardHeight > 0f ? artworkData.boardHeight : 0.5f;

            // Root container (mirrors the SlotRoot clone structure)
            var root = new GameObject($"PlacedArtwork_{artworkData.artworkId}");
            root.tag = "PlacedArtwork";
            // Must be on PuzzlePiece layer so InteractionManager's OverlapSphere can detect it
            int puzzlePieceLayer = LayerMask.NameToLayer("PuzzlePiece");
            if (puzzlePieceLayer >= 0) root.layer = puzzlePieceLayer;
            var identifier = root.AddComponent<PlacedArtworkIdentifier>();
            identifier.artworkId = artworkData.artworkId;
            identifier.anchorId  = artworkData.anchorId;
            root.transform.SetParent(anchorTransform, worldPositionStays: false);
            root.transform.localPosition = artworkData.localPosition.ToVector3();
            root.transform.localRotation = artworkData.localRotation.ToQuaternion();

            // Image quad (same UV mapping as PuzzleBoard.ShowFullImageReveal)
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "FullImageReveal";
            Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(root.transform, worldPositionStays: false);
            quad.transform.localPosition = new Vector3(0f, 0f, 0.0155f);
            quad.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            quad.transform.localScale = new Vector3(w, h, 1f);

            // Resolve texture — same fallback chain as PuzzleBoard
            Texture texture = artworkDef.puzzleTexture != null
                ? (Texture)artworkDef.puzzleTexture
                : artworkDef.fullImage?.texture;

            if (texture != null)
            {
                var quadRenderer = quad.GetComponent<Renderer>();

                // Same shader search order as PuzzleBoard.ShowFullImageReveal
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Texture");
                if (shader == null) shader = Shader.Find("Sprites/Default"); // always present on device

                if (shader != null)
                {
                    var mat = new Material(shader);
                    if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", texture);
                    if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", texture);
                    // Standard UV mapping — same as PuzzleBoard.ApplyCompletedFullImageTextureMapping
                    if (mat.HasProperty("_BaseMap_ST")) mat.SetVector("_BaseMap_ST", new Vector4(1f, 1f, 0f, 0f));
                    if (mat.HasProperty("_MainTex_ST")) mat.SetVector("_MainTex_ST", new Vector4(1f, 1f, 0f, 0f));
                    quadRenderer.material = mat;
                    Debug.Log($"[WallAnchor] Image material assigned using shader '{shader.name}' for {artworkData.artworkId}");
                }
                else
                {
                    Debug.LogError($"[WallAnchor] No usable shader found for artwork image ({artworkData.artworkId})");
                }
            }
            else
            {
                Debug.LogWarning($"[WallAnchor] No texture on artwork definition for {artworkData.artworkId}");
            }

            // Frame bars — el marco se gatea por el tier global actual (Madera si el Marco aun
            // no esta desbloqueado), no por el frameTier guardado (que queda legacy).
            BuildFrameBars(root.transform, w, h, PresentationDecorator.CurrentFrameTier());

            // BoxCollider so the artwork can be re-grabbed
            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(w, h, 0.05f);

            // Cedula + lampara segun estado global (GDD 8.2), acabado por tier.
            PresentationDecorator.Decorate(root, artworkDef.title, artworkDef.author, w, h,
                artworkDef.year, artworkDef.artMovement, artworkDef.museum);

            spawnedArtworks[artworkData.artworkId] = root;
            Debug.Log($"[WallAnchor] Spawned artwork {artworkData.artworkId} ({w:F3}×{h:F3}m) at anchor (hidden until tracking)");
            return root;
        }

        /// <summary>
        /// Reconstruye una PLACA colgada al cargar (id "plaque_&lt;plaqueId&gt;"). Usa CollectibleFactory
        /// (mismo visual que al colgarla); la parenta al anchor y le repone el PlacedArtworkIdentifier
        /// para que se pueda volver a agarrar/reposicionar. Devuelve el root (inactivo hasta tracking).
        /// </summary>
        private GameObject SpawnPlaqueAtAnchor(AnchoredArtwork data, Transform anchorTransform)
        {
            string plaqueId = CollectibleFactory.PlaqueCollectibleId(data.artworkId);
            var catalog = ArtUnbound.Core.GameBootstrap.Instance != null
                ? ArtUnbound.Core.GameBootstrap.Instance.CollectibleCatalog : null;
            var def = catalog != null ? catalog.GetById(plaqueId) : null;
            if (def == null)
                Debug.LogWarning($"[WallAnchor] CollectibleDefinition not found for '{plaqueId}' — plaque may be blank");

            var go = CollectibleFactory.Build(def, PresentationDecorator.CurrentFrameTier());
            if (go == null)
            {
                Debug.LogError($"[WallAnchor] CollectibleFactory.Build returned null for {data.artworkId}");
                return null;
            }

            // CollectibleFactory ya pone tag/layer/collider; reponer el identifier para re-agarre.
            var identifier = go.GetComponent<PlacedArtworkIdentifier>();
            if (identifier == null) identifier = go.AddComponent<PlacedArtworkIdentifier>();
            identifier.artworkId = data.artworkId;
            identifier.anchorId  = data.anchorId;

            go.transform.SetParent(anchorTransform, worldPositionStays: false);
            go.transform.localPosition = data.localPosition.ToVector3();
            go.transform.localRotation = data.localRotation.ToQuaternion();

            spawnedArtworks[data.artworkId] = go;
            Debug.Log($"[WallAnchor] Spawned plaque {data.artworkId} at anchor (hidden until tracking)");
            return go;
        }

        private void BuildFrameBars(Transform parent, float w, float h, FrameTier tier)
        {
            Material mat = GetFrameMaterial(tier);
            const float thickness = 0.02f;
            const float depth = 0.02f;

            var frameRoot = new GameObject("FullImageRevealFrame");
            frameRoot.transform.SetParent(parent, worldPositionStays: false);
            frameRoot.transform.localPosition = new Vector3(0f, 0f, 0.015f);
            frameRoot.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            CreateFrameBar("FrameTop",    frameRoot.transform, w + thickness * 2f, thickness, depth, 0f,             h / 2f + thickness / 2f, mat);
            CreateFrameBar("FrameBottom", frameRoot.transform, w + thickness * 2f, thickness, depth, 0f,            -h / 2f - thickness / 2f, mat);
            CreateFrameBar("FrameLeft",   frameRoot.transform, thickness,          h,          depth, -w / 2f - thickness / 2f, 0f, mat);
            CreateFrameBar("FrameRight",  frameRoot.transform, thickness,          h,          depth,  w / 2f + thickness / 2f, 0f, mat);
        }

        private static void CreateFrameBar(string barName, Transform parent, float width, float height,
            float depth, float localX, float localY, Material mat)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = barName;
            Destroy(cube.GetComponent<Collider>());
            cube.transform.SetParent(parent, worldPositionStays: false);
            cube.transform.localPosition = new Vector3(localX, localY, 0f);
            cube.transform.localRotation = Quaternion.identity;
            cube.transform.localScale = new Vector3(width, height, depth);
            if (mat != null)
                cube.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private Material GetFrameMaterial(FrameTier tier)
        {
            // Minimal fallback materials — the full material set is on PuzzleBoard
            // which is not available here; we use solid colors as stand-ins (incluye Madera).
            Color color = PresentationDecorator.FrameBarColor(tier);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) return null;
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else mat.color = color;
            return mat;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  REPOSITION
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Updates the world position of an already-placed artwork by erasing the old
        /// anchor and creating a new one at the new position.
        /// </summary>
        public bool UpdateAnchorPosition(string artworkId, Vector3 newPosition, Quaternion newRotation)
        {
            if (!spawnedArtworks.TryGetValue(artworkId, out GameObject artworkObject))
            {
                Debug.LogWarning($"[WallAnchor] Cannot update: artwork {artworkId} not found");
                return false;
            }

            // Delegar en la variante por-instancia: resuelve la entrada por el anchorId del
            // identifier del objeto, sin pisar otras copias colgadas de la misma obra.
            UpdateAnchorForSpecificArtwork(artworkId, artworkObject.transform, newPosition, newRotation);
            return true;
        }

        private async void DoEraseAndRecreate(string artworkId, Vector3 newPosition,
            Quaternion newRotation, FrameTier frameTier, Transform existingFrame,
            float boardWidth, float boardHeight, string oldAnchorId)
        {
            // Erase old persistent anchor
            if (!string.IsNullOrEmpty(oldAnchorId) && Guid.TryParse(oldAnchorId, out Guid oldGuid))
            {
                var eraseStatus = await arAnchorManager.TryEraseAnchorAsync(new SerializableGuid(oldGuid));
                if (!eraseStatus.IsSuccess())
                    Debug.LogWarning($"[WallAnchor] TryEraseAnchorAsync failed for old anchor of {artworkId}: {eraseStatus}");

                // Solo la entrada de ESTA ancla: otras copias colgadas de la obra quedan intactas.
                saveDataService?.RemoveAnchoredArtworkByAnchorId(oldAnchorId);
            }

            if (activeAnchorsByArtworkId.TryGetValue(artworkId, out ARAnchor oldAnchor) && oldAnchor != null)
            {
                existingFrame.SetParent(null, worldPositionStays: true);
                // ARAnchorManager will clean up oldAnchor automatically
                activeAnchorsByArtworkId.Remove(artworkId);
            }

            DoCreateAndSaveAnchor(artworkId, newPosition, newRotation, frameTier,
                                  existingFrame, boardWidth, boardHeight);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  SPECIFIC-INSTANCE HELPERS (for repositioning/removing grabbed artworks)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Moves a SPECIFIC artwork transform to a new wall position and re-anchors it.
        /// Unlike UpdateAnchorPosition, this operates on the provided transform rather than
        /// whatever is currently stored in spawnedArtworks[artworkId].
        /// </summary>
        public void UpdateAnchorForSpecificArtwork(string artworkId, Transform artworkTransform,
            Vector3 newPosition, Quaternion newRotation)
        {
            if (artworkTransform == null) return;

            // Resolver la entrada de ESTA copia por su anchorId (multi-copia por obra). Si el
            // identifier no tiene anchorId es una copia nunca anclada (Hang del detalle):
            // existingData queda null y se crea una entrada nueva sin tocar las de otras copias.
            var idComp = artworkTransform.GetComponent<PlacedArtworkIdentifier>();
            string ownAnchorId = idComp != null ? idComp.anchorId : null;
            var artworks = saveDataService?.GetAnchoredArtworks();
            var existingData = !string.IsNullOrEmpty(ownAnchorId)
                ? artworks?.Find(a => a.anchorId == ownAnchorId)
                : null;

            artworkTransform.SetParent(null, worldPositionStays: true);
            artworkTransform.SetPositionAndRotation(newPosition, newRotation);

            // Point the dictionary at this specific object going forward
            spawnedArtworks[artworkId] = artworkTransform.gameObject;

            // Dimensiones: de la entrada existente, o del collider del objeto (primer colgado
            // de una copia flotante, cuyo tamano no esta aun en storage).
            float boardW = existingData?.boardWidth ?? 0f;
            float boardH = existingData?.boardHeight ?? 0f;
            if (boardW <= 0f || boardH <= 0f)
            {
                var col = artworkTransform.GetComponent<BoxCollider>();
                boardW = col != null && col.size.x > 0f ? col.size.x : 0.5f;
                boardH = col != null && col.size.y > 0f ? col.size.y : 0.5f;
            }

            DoEraseAndRecreate(artworkId, newPosition, newRotation,
                existingData?.frameTier ?? FrameTier.Bronce,
                artworkTransform,
                boardW,
                boardH,
                existingData?.anchorId);
        }

        /// <summary>
        /// Removes one anchor entry from storage without destroying any GameObjects.
        /// Use when the caller already handles destroying the physical object.
        /// Pasa el anchorId de la copia quitada para borrar SOLO su entrada; sin anchorId
        /// (legacy) cae a la primera entrada de ese artworkId.
        /// </summary>
        public void EraseArtworkStorageEntry(string artworkId, string anchorId = null)
        {
            var artworks = saveDataService?.GetAnchoredArtworks();
            var data = !string.IsNullOrEmpty(anchorId)
                ? artworks?.Find(a => a.anchorId == anchorId)
                : artworks?.Find(a => a.artworkId == artworkId);
            if (data == null) return;

            if (Guid.TryParse(data.anchorId, out Guid guid))
                DoEraseAnchor(new SerializableGuid(guid));

            saveDataService?.RemoveAnchoredArtworkByAnchorId(data.anchorId);

            if (spawnedArtworks.TryGetValue(artworkId, out GameObject stored) && stored == null)
                spawnedArtworks.Remove(artworkId);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  REMOVE
        // ─────────────────────────────────────────────────────────────────────

        public bool RemoveAnchoredArtwork(string artworkId)
        {
            if (spawnedArtworks.TryGetValue(artworkId, out GameObject artworkObject))
            {
                Destroy(artworkObject);
                spawnedArtworks.Remove(artworkId);
            }

            if (saveDataService != null)
            {
                // Erase persistent anchor in background
                var artworks = saveDataService.GetAnchoredArtworks();
                var data = artworks?.Find(a => a.artworkId == artworkId);
                if (data != null && Guid.TryParse(data.anchorId, out Guid guid))
                    DoEraseAnchor(new SerializableGuid(guid));

                if (saveDataService.RemoveAnchoredArtwork(artworkId))
                {
                    Debug.Log($"[WallAnchor] Removed anchored artwork {artworkId}");
                    return true;
                }
            }

            return false;
        }

        private async void DoEraseAnchor(SerializableGuid guid)
        {
            var status = await arAnchorManager.TryEraseAnchorAsync(guid);
            if (!status.IsSuccess())
                Debug.LogWarning($"[WallAnchor] DoEraseAnchor failed: {status}");
        }

        // ─────────────────────────────────────────────────────────────────────
        //  QUERIES
        // ─────────────────────────────────────────────────────────────────────

        public GameObject GetPlacedArtwork(string artworkId)
        {
            spawnedArtworks.TryGetValue(artworkId, out GameObject obj);
            return obj;
        }

        public List<string> GetAnchoredArtworkIds()
        {
            var ids = new List<string>();
            if (saveDataService != null)
                foreach (var a in saveDataService.GetAnchoredArtworks())
                    ids.Add(a.artworkId);
            return ids;
        }
    }
}
