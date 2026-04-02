using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using ArtUnbound.Data;
using ArtUnbound.Services;
using ArtUnbound.Gameplay;

namespace ArtUnbound.MR
{
    /// <summary>
    /// Manages spatial anchors for artworks hung on walls using Meta Spatial Anchors.
    /// Handles creating, saving, loading, and spawning anchored artworks with persistence across sessions.
    /// </summary>
    public class WallAnchorManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ARAnchorManager anchorManager;
        [SerializeField] private ArtworkCatalog artworkCatalog;
        
        private SaveDataService saveDataService;

        private Dictionary<Guid, OVRSpatialAnchor> activeAnchors = new Dictionary<Guid, OVRSpatialAnchor>();
        private Dictionary<string, GameObject> spawnedArtworks = new Dictionary<string, GameObject>();

        public event Action<string, Vector3> OnArtworkAnchored; // artworkId, position

        private void Awake()
        {
            if (anchorManager == null)
            {
                anchorManager = FindFirstObjectByType<ARAnchorManager>();
            }

            if (anchorManager == null)
            {
                Debug.LogError("[WallAnchor] ARAnchorManager not found. Please add one to the XR Origin.");
            }
        }

        /// <summary>
        /// Initializes the WallAnchorManager with the SaveDataService.
        /// </summary>
        public void Initialize(SaveDataService service)
        {
            saveDataService = service;
            Debug.Log("[WallAnchor] Initialized with SaveDataService");
        }

        /// <summary>
        /// Creates a persistent spatial anchor at the specified position and anchors the existing frame.
        /// Uses Meta Spatial Anchors for persistence across sessions.
        /// </summary>
        public bool CreateAnchor(string artworkId, Vector3 worldPosition, Quaternion worldRotation, FrameTier frameTier, Transform existingFrame = null)
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
            
            Debug.Log($"[WallAnchor] CreateAnchor called for {artworkId}");
            Debug.Log($"[WallAnchor] Received frame: {existingFrame.name} at {existingFrame.position}");
            Debug.Log($"[WallAnchor] Frame rotation: {existingFrame.rotation.eulerAngles}");

            // Create a new GameObject to hold the anchor
            GameObject anchorObject = new GameObject($"Anchor_{artworkId}");
            anchorObject.transform.position = worldPosition;
            anchorObject.transform.rotation = worldRotation;

            Debug.Log($"[WallAnchor] Anchor GameObject created at {worldPosition}, rotation: {worldRotation.eulerAngles}");

            // Add OVRSpatialAnchor component for persistence
            OVRSpatialAnchor spatialAnchor = anchorObject.AddComponent<OVRSpatialAnchor>();
            
            if (spatialAnchor == null)
            {
                Debug.LogError("[WallAnchor] Failed to create OVRSpatialAnchor component");
                Destroy(anchorObject);
                return false;
            }

            Debug.Log($"[WallAnchor] OVRSpatialAnchor component added (enabled: {spatialAnchor.enabled}, GameObject active: {anchorObject.activeInHierarchy})");

            // Save spatial anchor for persistence (UUID will be assigned inside coroutine after Created)
            StartCoroutine(SaveSpatialAnchorAsync(spatialAnchor, artworkId, frameTier));

            // Parent the frame to the anchor
            existingFrame.SetParent(anchorObject.transform, true); // Keep world position and rotation
            
            // Tag the frame so it can be detected by other systems
            existingFrame.tag = "PlacedArtwork";
            
            // Store reference
            spawnedArtworks[artworkId] = existingFrame.gameObject;
            
            Debug.Log($"[WallAnchor] Anchored frame {existingFrame.name} for {artworkId} at {worldPosition}");
            Debug.Log($"[WallAnchor] Spatial Anchor will be saved asynchronously (UUID will be logged after Created)");

            OnArtworkAnchored?.Invoke(artworkId, worldPosition);
            
            return true;
        }

        /// <summary>
        /// Coroutine to save the spatial anchor asynchronously and persist to storage.
        /// </summary>
        private IEnumerator SaveSpatialAnchorAsync(OVRSpatialAnchor anchor, string artworkId, FrameTier frameTier)
        {
            Debug.Log($"[WallAnchor] SaveSpatialAnchorAsync started for {artworkId}, anchor is null: {anchor == null}");
            
            if (anchor == null)
            {
                Debug.LogError($"[WallAnchor] Anchor is null for {artworkId}, cannot save");
                yield break;
            }
            
            // CRITICAL: Wait at least 1 frame for Unity to initialize the OVRSpatialAnchor component
            yield return null;
            
            Debug.Log($"[WallAnchor] After first frame wait - GameObject active: {anchor.gameObject.activeInHierarchy}, Component enabled: {anchor.enabled}");
            
            int frameCount = 0;
            int maxWaitFrames = 600; // 10 seconds timeout
            
            // Wait for anchor to be created
            while (!anchor.Created && frameCount < maxWaitFrames)
            {
                frameCount++;
                if (frameCount % 60 == 0) // Log every 60 frames (~1 second)
                {
                    Debug.Log($"[WallAnchor] Still waiting for Created... Frame {frameCount}, Created: {anchor.Created}");
                }
                yield return null;
            }
            
            if (!anchor.Created)
            {
                Debug.LogError($"[WallAnchor] Anchor failed to reach Created state after {frameCount} frames for {artworkId}");
                Debug.LogError($"[WallAnchor] This may indicate: 1) Scene tracking not active, 2) Position not trackable, 3) Missing permissions");
                yield break;
            }

            Debug.Log($"[WallAnchor] Anchor created for {artworkId} after {frameCount} frames, UUID: {anchor.Uuid}");

            // NOW that Created is true, we can safely access the UUID and store the anchor
            Guid anchorUuid = anchor.Uuid;
            activeAnchors[anchorUuid] = anchor;
            
            Debug.Log($"[WallAnchor] Anchor UUID registered: {anchorUuid}");

            // Save the anchor to device storage for persistence
            var saveTask = anchor.SaveAnchorAsync();
            
            while (!saveTask.IsCompleted)
            {
                yield return null;
            }

            var saveResult = saveTask.GetAwaiter().GetResult();
            
            if (saveResult.Success)
            {
                Debug.Log($"[WallAnchor] Successfully saved anchor {anchor.Uuid} to device storage for {artworkId}");
                
                // Save anchor data to our save system
                SaveAnchorData(artworkId, anchor.Uuid.ToString(), Vector3.zero, Quaternion.identity, 1f, frameTier);
            }
            else
            {
                Debug.LogError($"[WallAnchor] Failed to save anchor for {artworkId}: {saveResult.Status}");
            }
        }

        /// <summary>
        /// Saves anchor data to persistent storage.
        /// </summary>
        private void SaveAnchorData(string artworkId, string anchorId, Vector3 localPosition, Quaternion localRotation, float scale, FrameTier frameTier)
        {
            if (saveDataService == null)
            {
                Debug.LogWarning("[WallAnchor] SaveDataService is null, cannot save anchor data");
                return;
            }

            var anchoredArtwork = new AnchoredArtwork(artworkId, anchorId, localPosition, localRotation, scale, frameTier);
            saveDataService.AddAnchoredArtwork(anchoredArtwork);

            Debug.Log($"[WallAnchor] Saved anchor data for {artworkId}");
        }

        /// <summary>
        /// Loads all saved spatial anchors and spawns artworks at their positions.
        /// Call this on app startup after Scene API is ready.
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

            // Extract UUIDs from saved artworks
            List<Guid> uuidsToLoad = new List<Guid>();
            foreach (var artwork in anchoredArtworks)
            {
                if (Guid.TryParse(artwork.anchorId, out Guid uuid))
                {
                    uuidsToLoad.Add(uuid);
                    Debug.Log($"[WallAnchor] Added UUID {uuid} for artwork {artwork.artworkId}");
                }
                else
                {
                    Debug.LogWarning($"[WallAnchor] Failed to parse UUID for artwork {artwork.artworkId}: {artwork.anchorId}");
                }
            }

            if (uuidsToLoad.Count == 0)
            {
                Debug.LogWarning("[WallAnchor] No valid UUIDs found in saved artworks");
                return;
            }

            Debug.Log($"[WallAnchor] Starting coroutine to load {uuidsToLoad.Count} UUIDs");
            // Start coroutine to load and spawn anchors
            StartCoroutine(LoadUnboundAnchorsCoroutine(uuidsToLoad, anchoredArtworks));
        }

        /// <summary>
        /// Coroutine to load unbound anchors and spawn artworks.
        /// </summary>
        private IEnumerator LoadUnboundAnchorsCoroutine(List<Guid> uuids, List<AnchoredArtwork> anchoredArtworks)
        {
            Debug.Log($"[WallAnchor] LoadUnboundAnchorsCoroutine started with {uuids.Count} UUIDs");
            
            // Buffer for unbound anchors
            List<OVRSpatialAnchor.UnboundAnchor> unboundAnchors = new List<OVRSpatialAnchor.UnboundAnchor>();

            Debug.Log($"[WallAnchor] Calling OVRSpatialAnchor.LoadUnboundAnchorsAsync...");
            
            // Load unbound anchors by UUID
            var loadTask = OVRSpatialAnchor.LoadUnboundAnchorsAsync(uuids, unboundAnchors);
            
            Debug.Log($"[WallAnchor] LoadUnboundAnchorsAsync task created, waiting for completion...");
            
            while (!loadTask.IsCompleted)
            {
                yield return null;
            }

            Debug.Log($"[WallAnchor] LoadUnboundAnchorsAsync completed");

            var loadResult = loadTask.GetAwaiter().GetResult();
            
            Debug.Log($"[WallAnchor] Load result - Success: {loadResult.Success}, Status: {loadResult.Status}");
            
            if (!loadResult.Success)
            {
                Debug.LogError($"[WallAnchor] Failed to load anchors: {loadResult.Status}");
                yield break;
            }

            Debug.Log($"[WallAnchor] Successfully loaded {unboundAnchors.Count} unbound anchors");

            // Create a dictionary for quick lookup
            Dictionary<string, AnchoredArtwork> artworksByAnchorId = new Dictionary<string, AnchoredArtwork>();
            foreach (var artwork in anchoredArtworks)
            {
                artworksByAnchorId[artwork.anchorId] = artwork;
            }

            // Localize and bind each anchor
            foreach (var unboundAnchor in unboundAnchors)
            {
                string anchorId = unboundAnchor.Uuid.ToString();
                
                if (!artworksByAnchorId.TryGetValue(anchorId, out AnchoredArtwork artworkData))
                {
                    Debug.LogWarning($"[WallAnchor] Found anchor {anchorId} but no matching artwork data");
                    continue;
                }

                // Start localization
                StartCoroutine(LocalizeAndBindAnchor(unboundAnchor, artworkData));
            }
        }

        /// <summary>
        /// Coroutine to localize an unbound anchor and bind it to a GameObject.
        /// </summary>
        private IEnumerator LocalizeAndBindAnchor(OVRSpatialAnchor.UnboundAnchor unboundAnchor, AnchoredArtwork artworkData)
        {
            Debug.Log($"[WallAnchor] Localizing anchor for artwork {artworkData.artworkId}");

            // Start localization
            var localizeTask = unboundAnchor.LocalizeAsync();
            
            while (!localizeTask.IsCompleted)
            {
                yield return null;
            }

            bool localized = localizeTask.GetAwaiter().GetResult();
            
            if (!localized)
            {
                Debug.LogError($"[WallAnchor] Failed to localize anchor for {artworkData.artworkId}");
                yield break;
            }

            Debug.Log($"[WallAnchor] Successfully localized anchor for {artworkData.artworkId}");

            // Create GameObject at anchor position
            GameObject anchorObject = new GameObject($"Anchor_{artworkData.artworkId}");
            OVRSpatialAnchor spatialAnchor = anchorObject.AddComponent<OVRSpatialAnchor>();

            // Bind the unbound anchor to the OVRSpatialAnchor component
            unboundAnchor.BindTo(spatialAnchor);

            // Wait for binding to complete
            while (!spatialAnchor.Created)
            {
                yield return null;
            }

            // Store the anchor
            activeAnchors[spatialAnchor.Uuid] = spatialAnchor;

            // Spawn the artwork at this anchor
            SpawnArtworkAtAnchor(artworkData, anchorObject.transform);
        }

        /// <summary>
        /// Spawns an artwork at the given anchor transform.
        /// </summary>
        private void SpawnArtworkAtAnchor(AnchoredArtwork artworkData, Transform anchorTransform)
        {
            if (artworkCatalog == null)
            {
                Debug.LogError("[WallAnchor] Missing ArtworkCatalog reference for spawning artwork");
                return;
            }

            // Find the artwork definition
            ArtworkDefinition artworkDef = artworkCatalog.artworks.Find(a => a.artworkId == artworkData.artworkId);
            if (artworkDef == null)
            {
                Debug.LogError($"[WallAnchor] Artwork definition not found for {artworkData.artworkId}");
                return;
            }

            // TODO: Implement full artwork spawning with puzzle pieces
            // For now, create a simple placeholder
            GameObject artworkObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            artworkObject.name = $"PlacedArtwork_{artworkData.artworkId}";
            artworkObject.transform.SetParent(anchorTransform, false);
            artworkObject.transform.localPosition = artworkData.localPosition.ToVector3();
            artworkObject.transform.localRotation = artworkData.localRotation.ToQuaternion();
            artworkObject.transform.localScale = Vector3.one * artworkData.scale * 0.5f;
            artworkObject.tag = "PlacedArtwork";

            if (artworkDef.puzzleTexture != null)
            {
                var renderer = artworkObject.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.material.mainTexture = artworkDef.puzzleTexture;
            }

            spawnedArtworks[artworkData.artworkId] = artworkObject;
            
            Debug.Log($"[WallAnchor] Spawned artwork {artworkData.artworkId} at anchor");
        }

        /// <summary>
        /// Removes an anchored artwork from the scene and save data.
        /// </summary>
        public bool RemoveAnchoredArtwork(string artworkId)
        {
            // Remove from scene
            if (spawnedArtworks.TryGetValue(artworkId, out GameObject artworkObject))
            {
                Destroy(artworkObject);
                spawnedArtworks.Remove(artworkId);
            }

            // Remove from save data
            if (saveDataService != null)
            {
                bool removed = saveDataService.RemoveAnchoredArtwork(artworkId);
                
                if (removed)
                {
                    Debug.Log($"[WallAnchor] Removed anchored artwork {artworkId}");
                    return true;
                }
            }

            return false;
        }
        
        /// <summary>
        /// Updates the position and rotation of an existing anchored artwork.
        /// Used when the user repositions an already-placed artwork.
        /// Note: OVRSpatialAnchor position updates require re-saving the anchor.
        /// </summary>
        public bool UpdateAnchorPosition(string artworkId, Vector3 newPosition, Quaternion newRotation)
        {
            if (!spawnedArtworks.ContainsKey(artworkId))
            {
                Debug.LogWarning($"[WallAnchor] Cannot update: artwork {artworkId} not found");
                return false;
            }
            
            GameObject artworkObject = spawnedArtworks[artworkId];
            Transform anchorTransform = artworkObject.transform.parent;
            
            if (anchorTransform == null)
            {
                Debug.LogError($"[WallAnchor] Artwork {artworkId} has no anchor parent");
                return false;
            }
            
            // Get the OVRSpatialAnchor component
            OVRSpatialAnchor spatialAnchor = anchorTransform.GetComponent<OVRSpatialAnchor>();
            if (spatialAnchor == null)
            {
                Debug.LogError($"[WallAnchor] Anchor for {artworkId} has no OVRSpatialAnchor component");
                return false;
            }
            
            // Update the anchor's position and rotation
            anchorTransform.position = newPosition;
            anchorTransform.rotation = newRotation;
            
            Debug.Log($"[WallAnchor] Updated anchor for {artworkId} to position {newPosition}, rotation {newRotation.eulerAngles}");
            
            // Re-save the anchor with new position
            StartCoroutine(ResaveSpatialAnchor(spatialAnchor, artworkId));
            
            return true;
        }
        
        /// <summary>
        /// Coroutine to re-save a spatial anchor after repositioning.
        /// </summary>
        private IEnumerator ResaveSpatialAnchor(OVRSpatialAnchor anchor, string artworkId)
        {
            // Erase the old anchor from storage
            var eraseTask = anchor.EraseAnchorAsync();
            while (!eraseTask.IsCompleted)
            {
                yield return null;
            }
            
            var eraseResult = eraseTask.GetAwaiter().GetResult();
            
            if (!eraseResult.Success)
            {
                Debug.LogWarning($"[WallAnchor] Failed to erase old anchor for {artworkId}, attempting save anyway");
            }
            
            // Save the anchor with new position
            var saveTask = anchor.SaveAnchorAsync();
            while (!saveTask.IsCompleted)
            {
                yield return null;
            }
            
            var saveResult = saveTask.GetAwaiter().GetResult();
            
            if (saveResult.Success)
            {
                Debug.Log($"[WallAnchor] Successfully re-saved anchor for {artworkId}");
            }
            else
            {
                Debug.LogError($"[WallAnchor] Failed to re-save anchor for {artworkId}: {saveResult.Status}");
            }
        }
        
        /// <summary>
        /// Gets the artwork GameObject for a given artworkId.
        /// Used to check if an artwork is already placed and to enable re-grabbing.
        /// </summary>
        public GameObject GetPlacedArtwork(string artworkId)
        {
            if (spawnedArtworks.TryGetValue(artworkId, out GameObject artworkObject))
            {
                return artworkObject;
            }
            return null;
        }

        /// <summary>
        /// Gets all currently anchored artwork IDs.
        /// </summary>
        public List<string> GetAnchoredArtworkIds()
        {
            var ids = new List<string>();
            
            if (saveDataService != null)
            {
                var anchoredArtworks = saveDataService.GetAnchoredArtworks();
                foreach (var artwork in anchoredArtworks)
                {
                    ids.Add(artwork.artworkId);
                }
            }

            return ids;
        }
    }
}
