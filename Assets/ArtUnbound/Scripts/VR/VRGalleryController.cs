using System.Collections.Generic;
using ArtUnbound.Data;
using ArtUnbound.Services;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace ArtUnbound.VR
{
    /// <summary>
    /// Manages the active VR gallery environment: instantiates the prefab, spawns saved paintings,
    /// and handles gallery switching.
    /// </summary>
    public class VRGalleryController : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private GalleryCatalog galleryCatalog;
        [SerializeField] private ArtUnbound.Data.ArtworkCatalog artworkCatalog;

        [Header("Painting Prefab")]
        [Tooltip("Prefab used to display a hung painting in the gallery. Needs a renderer and PlacedArtworkIdentifier.")]
        [SerializeField] private GameObject galleryPaintingPrefab;

        [Header("Navigation Panel")]
        [Tooltip("The floating NavigationGallery panel — repositioned in front of user on gallery switch.")]
        [SerializeField] private Transform navigationPanel;

        [Header("Player Rig")]
        [Tooltip("XR Origin root — teleported to the 'PlayerSpawnPoint' child in the environment prefab on load.")]
        [SerializeField] private Transform xrOrigin;

        private GalleryPersistenceService _persistenceService;
        private SaveDataService _saveDataService;

        private GameObject _activeGalleryInstance;
        private string _activeGalleryId;
        private List<GameObject> _spawnedPaintings = new List<GameObject>();

        private Vector3 _originalRigPos;
        private Quaternion _originalRigRot;
        private bool _hasOriginalRig;

        private void Awake()
        {
            // Cache the rig's initial position so we can restore it when leaving VR.
            // With a single rig (post XR Origin VR removal), the same rig that
            // teleports to the gallery spawn must return to the MR origin.
            if (xrOrigin != null)
            {
                _originalRigPos = xrOrigin.position;
                _originalRigRot = xrOrigin.rotation;
                _hasOriginalRig = true;
            }

            gameObject.SetActive(false);
        }

        public void Initialize(SaveDataService saveDataService, GalleryPersistenceService persistenceService)
        {
            _saveDataService = saveDataService;
            _persistenceService = persistenceService;
        }

        public void LoadGallery(string galleryId)
        {
            // Destroy previous gallery
            if (_activeGalleryInstance != null)
                Destroy(_activeGalleryInstance);

            foreach (var p in _spawnedPaintings)
                if (p != null) Destroy(p);
            _spawnedPaintings.Clear();

            _activeGalleryId = galleryId;

            var def = galleryCatalog?.galleries?.Find(g => g.galleryId == galleryId);
            if (def == null)
            {
                Debug.LogWarning($"[VRGallery] Gallery '{galleryId}' not found in catalog.");
                return;
            }

            if (def.environmentPrefab != null)
            {
                _activeGalleryInstance = Instantiate(def.environmentPrefab);
                EnsureTeleportableFloors(_activeGalleryInstance);
                TeleportToSpawnPoint(_activeGalleryInstance);
            }

            SpawnSavedPaintings(galleryId);
            Debug.Log($"[VRGallery] Loaded gallery '{galleryId}'");
        }

        public void SwitchGallery(string newGalleryId)
        {
            if (_saveDataService != null)
            {
                var data = _saveDataService.GetCachedData();
                data.lastGalleryId = newGalleryId;
                _saveDataService.MarkDirty();
            }

            LoadGallery(newGalleryId);
            RepositionNavigationPanel();
            // La galeria nueva nace en el origen: se vuelve a acomodar alrededor del menu.
            if (_lastMenu != null) AlignToMenu(_lastMenu);
        }

        // ─── Alineacion galeria <-> menu ───────────────────────────────────────────────
        //
        // Regla de diseno: ni el menu ni el jugador se mueven al entrar a VR. Lo que se acomoda es
        // la GALERIA, de modo que su punto "MenuAnchor" quede justo donde esta el menu y mirando
        // hacia donde mira el menu. Asi el menu aparece siempre en el mismo lugar del museo y el
        // jugador conserva, respecto al menu, la posicion que tenia en MR.
        //
        // Antes la galeria nacia siempre en el origen del tracking y el menu frente al usuario:
        // si el usuario no estaba parado en ese origen, el menu caia junto a (o detras de) una
        // pared central.

        /// <summary>Nombre del objeto vacio del prefab de galeria que marca donde va el menu
        /// (su +Z = hacia donde mira el jugador al leer el menu).</summary>
        public const string MenuAnchorName = "MenuAnchor";

        private Transform _lastMenu;

        /// <summary>
        /// Mueve y gira (solo en horizontal) la galeria activa para que su MenuAnchor coincida con
        /// <paramref name="menu"/>. Los cuadros ya colgados se mueven con ella. Si el prefab no
        /// trae MenuAnchor se usa el origen de la galeria. Devuelve false si no hay galeria o menu.
        /// </summary>
        public bool AlignToMenu(Transform menu)
        {
            if (menu == null || _activeGalleryInstance == null) return false;
            _lastMenu = menu;

            Transform gallery = _activeGalleryInstance.transform;

            Transform anchor = null;
            foreach (var t in _activeGalleryInstance.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == MenuAnchorName) { anchor = t; break; }
            }
            if (anchor == null)
                Debug.LogWarning($"[VRGallery] El prefab de la galeria no tiene '{MenuAnchorName}': se usa su origen como punto del menu.");

            Vector3 oldPos = gallery.position;
            Quaternion oldRot = gallery.rotation;

            // Pose del ancla relativa a la raiz de la galeria (sin escala: la raiz va a escala 1).
            Vector3 anchorWorldPos = anchor != null ? anchor.position : oldPos;
            Vector3 anchorWorldFwd = anchor != null ? anchor.forward : gallery.forward;
            Vector3 anchorLocalPos = Quaternion.Inverse(oldRot) * (anchorWorldPos - oldPos);
            Vector3 anchorLocalFwd = Flatten(Quaternion.Inverse(oldRot) * anchorWorldFwd, Vector3.forward);

            Vector3 menuFwd = Flatten(menu.forward, Vector3.forward);

            // Solo yaw: el piso de la galeria debe seguir horizontal.
            Quaternion newRot = Quaternion.LookRotation(menuFwd, Vector3.up)
                              * Quaternion.Inverse(Quaternion.LookRotation(anchorLocalFwd, Vector3.up));
            Vector3 newPos = menu.position - newRot * anchorLocalPos;
            newPos.y = oldPos.y; // la altura del piso no cambia; el menu conserva la suya

            // Los cuadros colgados viven en mundo (no son hijos de la galeria): se llevan con ella.
            foreach (var p in _spawnedPaintings)
            {
                if (p == null) continue;
                Vector3 lp = Quaternion.Inverse(oldRot) * (p.transform.position - oldPos);
                Quaternion lr = Quaternion.Inverse(oldRot) * p.transform.rotation;
                p.transform.SetPositionAndRotation(newPos + newRot * lp, newRot * lr);
            }

            gallery.SetPositionAndRotation(newPos, newRot);
            Physics.SyncTransforms();

            Debug.Log($"[VRGallery] Galeria alineada al menu: menuPos={menu.position:F2} menuYaw={menu.eulerAngles.y:F0} " +
                      $"galleryPos={newPos:F2} galleryYaw={newRot.eulerAngles.y:F0} anchor={(anchor != null ? "ok" : "FALTA")} " +
                      $"anchorLocal={anchorLocalPos:F2} cuadros={_spawnedPaintings.Count}");
            return true;
        }

        private static Vector3 Flatten(Vector3 v, Vector3 fallback)
        {
            v.y = 0f;
            return v.sqrMagnitude < 0.0001f ? fallback : v.normalized;
        }

        /// <summary>Pose de mundo -> pose relativa a la galeria activa (lo que se PERSISTE). Sin
        /// galeria activa devuelve la misma pose.</summary>
        public void WorldToGallery(Vector3 worldPos, Quaternion worldRot, out Vector3 localPos, out Quaternion localRot)
        {
            if (_activeGalleryInstance == null) { localPos = worldPos; localRot = worldRot; return; }
            Transform g = _activeGalleryInstance.transform;
            Quaternion inv = Quaternion.Inverse(g.rotation);
            localPos = inv * (worldPos - g.position);
            localRot = inv * worldRot;
        }

        /// <summary>Pose relativa a la galeria (la guardada) -> pose de mundo. Los saves anteriores
        /// a la alineacion estaban en mundo con la galeria en el origen, o sea que ya eran
        /// relativos a la galeria: siguen siendo validos sin migracion.</summary>
        public void GalleryToWorld(Vector3 localPos, Quaternion localRot, out Vector3 worldPos, out Quaternion worldRot)
        {
            if (_activeGalleryInstance == null) { worldPos = localPos; worldRot = localRot; return; }
            Transform g = _activeGalleryInstance.transform;
            worldPos = g.position + g.rotation * localPos;
            worldRot = g.rotation * localRot;
        }

        /// <summary>
        /// Destroys the active gallery environment and all spawned paintings.
        /// Call this before disabling VR mode so the 3D room does not linger in the scene.
        /// </summary>
        public void UnloadGallery()
        {
            if (_activeGalleryInstance != null)
            {
                Destroy(_activeGalleryInstance);
                _activeGalleryInstance = null;
            }

            foreach (var p in _spawnedPaintings)
                if (p != null) Destroy(p);
            _spawnedPaintings.Clear();

            // Restore rig to its original (MR) position. The same rig that was
            // teleported to the gallery spawn must return to origin so the
            // user reappears in their physical space.
            if (_hasOriginalRig && xrOrigin != null)
                xrOrigin.SetPositionAndRotation(_originalRigPos, _originalRigRot);

            _activeGalleryId = null;
            Debug.Log("[VRGallery] Gallery unloaded");
        }

        /// <summary>Called by VRWallHangingController after successfully placing a painting.</summary>
        public void RegisterPlacedPainting(GameObject paintingGO)
        {
            if (!_spawnedPaintings.Contains(paintingGO))
                _spawnedPaintings.Add(paintingGO);
        }

        /// <summary>
        /// Garantiza que cualquier GameObject en la galeria con layer "Teleportable" tenga TeleportationArea
        /// y este wireado al TeleportationProvider del rig activo. Algunas galerias importadas (ej: Lumina,
        /// AK Studio Art) traen el piso con MeshCollider pero sin TeleportationArea, lo que hace que el
        /// rayo del teleport se vea rojo (destino invalido).
        /// </summary>
        private void EnsureTeleportableFloors(GameObject root)
        {
            int teleportableLayer = LayerMask.NameToLayer("Teleportable");
            if (teleportableLayer < 0) return;

            // Use the Locomotion/Teleportation path specifically to avoid accidentally
            // picking up any stray TeleportationProvider that may be on the XR Origin root.
            TeleportationProvider provider = null;
            if (xrOrigin != null)
            {
                var locomotion = xrOrigin.Find("Locomotion");
                if (locomotion != null)
                    provider = locomotion.GetComponentInChildren<TeleportationProvider>(includeInactive: true);
                // Fallback in case the hierarchy differs
                if (provider == null)
                    provider = xrOrigin.GetComponentInChildren<TeleportationProvider>(includeInactive: true);
            }

            // XRI Interaction Layer "Teleport" = bit 31. Mantiene la separacion convencional
            // entre interactables de teleport y los demas (UI, grab, etc).
            int teleportInteractionLayer = UnityEngine.XR.Interaction.Toolkit.InteractionLayerMask.GetMask("Teleport");

            int addedCount = 0;
            var meshColliders = root.GetComponentsInChildren<MeshCollider>(true);
            foreach (var mc in meshColliders)
            {
                if (teleportableLayer >= 0 && mc.gameObject.layer != teleportableLayer) continue;
                var area = mc.GetComponent<BaseTeleportationInteractable>();
                if (area == null)
                {
                    area = mc.gameObject.AddComponent<TeleportationArea>();
                    if (teleportInteractionLayer != 0)
                        area.interactionLayers = teleportInteractionLayer;
                    addedCount++;
                }
                if (provider != null)
                    area.teleportationProvider = provider;
            }

            // Fallback: gallery prefabs from the Asset Store typically don't assign a custom
            // Unity layer to their floors. Wire all MeshColliders so the teleport arc always
            // has valid destinations without requiring manual layer setup on every gallery prefab.
            if (addedCount == 0)
            {
                foreach (var mc in meshColliders)
                {
                    if (mc.GetComponent<BaseTeleportationInteractable>() != null) continue;
                    var area = mc.gameObject.AddComponent<TeleportationArea>();
                    if (teleportInteractionLayer != 0)
                        area.interactionLayers = teleportInteractionLayer;
                    if (provider != null)
                        area.teleportationProvider = provider;
                    addedCount++;
                }
            }

            Debug.Log($"[VRGallery] EnsureTeleportableFloors: added {addedCount} TeleportationArea(s), provider={(provider != null ? provider.gameObject.name : "NULL")}, interactionLayer={teleportInteractionLayer}");
        }

        private void TeleportToSpawnPoint(GameObject galleryInstance)
        {
            if (xrOrigin == null) return;

            Transform spawnPoint = galleryInstance.transform.Find("PlayerSpawnPoint");
            if (spawnPoint == null) return;

            xrOrigin.position = spawnPoint.position;
            xrOrigin.rotation = spawnPoint.rotation;
            Debug.Log($"[VRGallery] Teleported XR Origin to PlayerSpawnPoint at {spawnPoint.position}");
        }

        /// <summary>
        /// Catalogo de obras a usar al reconstruir cuadros. Usa el campo serializado si esta asignado;
        /// si no (causa de que los cuadros recargados salieran con el material "Lit" por defecto en vez
        /// de su textura), cae al catalogo global de GameBootstrap.
        /// </summary>
        private ArtUnbound.Data.ArtworkCatalog EffectiveArtworkCatalog()
        {
            if (artworkCatalog != null) return artworkCatalog;
            return ArtUnbound.Core.GameBootstrap.Instance != null
                ? ArtUnbound.Core.GameBootstrap.Instance.ArtworkCatalog : null;
        }

        private void SpawnSavedPaintings(string galleryId)
        {
            if (_persistenceService == null) return;

            var paintings = _persistenceService.GetPaintings(galleryId);
            foreach (var data in paintings)
            {
                // Migracion: las entradas viejas (metodo #token, sin instanceId) se ignoran para no
                // mostrar cuadros/placas en blanco. Se vuelven a colgar con el formato nuevo.
                if (string.IsNullOrEmpty(data.instanceId)) continue;

                GameObject go;

                // Las placas (id "plaque_<id>") se reconstruyen con CollectibleFactory, igual que en MR
                // (WallAnchorManager.SpawnPlaqueAtAnchor); no son quads de obra.
                if (!string.IsNullOrEmpty(data.artworkId) && data.artworkId.StartsWith("plaque_"))
                {
                    go = BuildSavedPlaque(data);
                    if (go == null) continue;
                }
                else
                {
                    float w = data.boardWidth  > 0f ? data.boardWidth  : 0.5f;
                    float h = data.boardHeight > 0f ? data.boardHeight : 0.5f;
                    go = PlacedArtworkFactory.Build(data.artworkId, data.frameTier, w, h, EffectiveArtworkCatalog());
                }

                // La pose guardada es relativa a la galeria (que ya no esta siempre en el origen).
                GalleryToWorld(data.Position, data.Rotation, out Vector3 worldPos, out Quaternion worldRot);
                go.transform.SetPositionAndRotation(worldPos, worldRot);

                // Reponer el instanceId (clave de persistencia) para que reposicionar/quitar afecte esta
                // copia exacta. Equivale a re-vincular la pieza con su anchor en MR.
                var inst = go.GetComponent<GalleryPaintingInstance>() ?? go.AddComponent<GalleryPaintingInstance>();
                inst.instanceId = data.instanceId;

                _spawnedPaintings.Add(go);
                Debug.Log($"[VRGallery] Spawned saved '{data.artworkId}' at local {data.Position:F2}");
            }
        }

        /// <summary>Reconstruye una PLACA colgada (id "plaque_&lt;id&gt;") con CollectibleFactory y le
        /// repone el PlacedArtworkIdentifier para que se pueda volver a agarrar/reposicionar.</summary>
        private GameObject BuildSavedPlaque(GalleryPaintingData data)
        {
            string plaqueId = ArtUnbound.MR.CollectibleFactory.PlaqueCollectibleId(data.artworkId);
            var catalog = ArtUnbound.Core.GameBootstrap.Instance != null
                ? ArtUnbound.Core.GameBootstrap.Instance.CollectibleCatalog : null;
            var def = catalog != null ? catalog.GetById(plaqueId) : null;
            if (def == null)
                Debug.LogWarning($"[VRGallery] CollectibleDefinition not found for '{plaqueId}' — plaque may be blank");

            var go = ArtUnbound.MR.CollectibleFactory.Build(def, ArtUnbound.MR.PresentationDecorator.CurrentFrameTier());
            if (go == null)
            {
                Debug.LogError($"[VRGallery] CollectibleFactory.Build returned null for {data.artworkId}");
                return null;
            }

            var id = go.GetComponent<ArtUnbound.MR.PlacedArtworkIdentifier>();
            if (id == null) id = go.AddComponent<ArtUnbound.MR.PlacedArtworkIdentifier>();
            id.artworkId = data.artworkId;
            return go;
        }

        private void RepositionNavigationPanel()
        {
            if (navigationPanel == null || Camera.main == null) return;

            Transform cam = Camera.main.transform;
            Vector3 forward = cam.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            forward.Normalize();

            Vector3 pos = cam.position + forward * 0.75f;
            pos.y += 0.1f;
            navigationPanel.position = pos;
            navigationPanel.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        public string ActiveGalleryId => _activeGalleryId;
    }
}
