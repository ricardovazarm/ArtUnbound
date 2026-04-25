using System.Collections.Generic;
using ArtUnbound.Data;
using ArtUnbound.Services;
using UnityEngine;

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

        private void Awake()
        {
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

            _activeGalleryId = null;
            Debug.Log("[VRGallery] Gallery unloaded");
        }

        /// <summary>Called by VRWallHangingController after successfully placing a painting.</summary>
        public void RegisterPlacedPainting(GameObject paintingGO)
        {
            if (!_spawnedPaintings.Contains(paintingGO))
                _spawnedPaintings.Add(paintingGO);
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

        private void SpawnSavedPaintings(string galleryId)
        {
            if (_persistenceService == null || galleryPaintingPrefab == null) return;

            var paintings = _persistenceService.GetPaintings(galleryId);
            foreach (var data in paintings)
            {
                var go = Instantiate(galleryPaintingPrefab, data.Position, data.Rotation);

                var identifier = go.GetComponent<ArtUnbound.MR.PlacedArtworkIdentifier>();
                if (identifier == null)
                    identifier = go.AddComponent<ArtUnbound.MR.PlacedArtworkIdentifier>();
                identifier.artworkId = data.artworkId;

                _spawnedPaintings.Add(go);
            }
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
