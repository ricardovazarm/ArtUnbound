using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using ArtUnbound.Data;
using ArtUnbound.Services;

namespace ArtUnbound.MR
{
    /// <summary>
    /// Manages spatial anchors for artworks hung on walls.
    /// Handles creating, saving, loading, and spawning anchored artworks.
    /// </summary>
    public class WallAnchorManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ARAnchorManager anchorManager;
        private SaveDataService saveDataService;

        private Dictionary<string, ARAnchor> activeAnchors = new Dictionary<string, ARAnchor>();
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
        /// Creates a spatial anchor at the specified position and anchors the existing frame.
        /// </summary>
        public bool CreateAnchor(string artworkId, Vector3 worldPosition, Quaternion worldRotation, FrameTier frameTier, Transform existingFrame = null)
        {
            if (anchorManager == null)
            {
                Debug.LogError("[WallAnchor] Cannot create anchor: ARAnchorManager is null");
                return false;
            }

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
            Debug.Log($"[WallAnchor] Frame has {existingFrame.childCount} children");

            // Create a new GameObject to hold the anchor
            GameObject anchorObject = new GameObject($"Anchor_{artworkId}");
            anchorObject.transform.position = worldPosition;
            anchorObject.transform.rotation = worldRotation;

            // Add ARAnchor component
            ARAnchor anchor = anchorObject.AddComponent<ARAnchor>();
            
            if (anchor == null)
            {
                Debug.LogError("[WallAnchor] Failed to create ARAnchor component");
                Destroy(anchorObject);
                return false;
            }

            // Generate unique ID for the anchor
            string anchorId = Guid.NewGuid().ToString();
            
            // Store anchor reference
            activeAnchors[anchorId] = anchor;

            // Save to persistent storage
            SaveAnchorData(artworkId, anchorId, Vector3.zero, Quaternion.identity, 1f, frameTier);

            // Parent the frame to the anchor
            existingFrame.SetParent(anchor.transform, true); // Keep world position and rotation
            
            // Tag the frame so it can be detected by other systems
            existingFrame.tag = "PlacedArtwork";
            
            // Store reference
            spawnedArtworks[artworkId] = existingFrame.gameObject;
            
            Debug.Log($"[WallAnchor] Anchored frame {existingFrame.name} for {artworkId} at {worldPosition}");
            Debug.Log($"[WallAnchor] Frame hierarchy: {existingFrame.name} -> {anchor.name}");
            Debug.Log($"[WallAnchor] Frame final position: {existingFrame.position}, rotation: {existingFrame.rotation.eulerAngles}");

            Debug.Log($"[WallAnchor] Created anchor {anchorId} for artwork {artworkId} at {worldPosition}");
            
            OnArtworkAnchored?.Invoke(artworkId, worldPosition);
            
            return true;
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
        /// Loads all saved anchors and spawns artworks at their positions.
        /// Call this on app startup.
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

            foreach (var anchoredArtwork in anchoredArtworks)
            {
                // Try to find existing anchor by ID
                // Note: AR Foundation doesn't persist anchor IDs across sessions automatically.
                // We'll need to use Meta's Spatial Anchor API for true persistence.
                // For now, we'll skip loading until Meta Spatial Anchors are implemented.
                
                Debug.LogWarning($"[WallAnchor] Anchor persistence not yet implemented. Artwork {anchoredArtwork.artworkId} will not be restored.");
            }
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
            
            // Update the anchor's position and rotation
            anchorTransform.position = newPosition;
            anchorTransform.rotation = newRotation;
            
            Debug.Log($"[WallAnchor] Updated anchor for {artworkId} to position {newPosition}, rotation {newRotation.eulerAngles}");
            
            // TODO: Update save data with new position
            
            return true;
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
