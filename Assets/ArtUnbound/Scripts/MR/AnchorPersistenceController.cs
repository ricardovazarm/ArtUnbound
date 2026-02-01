using System;
using System.Collections.Generic;
using ArtUnbound.Services;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ArtUnbound.MR
{
    public class AnchorPersistenceController : MonoBehaviour
    {
        public event Action OnAnchorRestoreFailed;
        public event Action<string, bool> OnAnchorSaved;

        [SerializeField] private ARAnchorManager anchorManager;
        // In a real implementation this would invoke the SaveDataService
        // [SerializeField] private SaveDataService saveDataService; 

        private List<ARAnchor> activeAnchors = new List<ARAnchor>();

        private void Awake()
        {
            if (anchorManager == null)
                anchorManager = FindFirstObjectByType<ARAnchorManager>();
        }

        public void SaveAnchor(string artworkId, Transform frame)
        {
            if (anchorManager == null) return;

            // Create a new ARAnchor at the frame's position
            // Note: In ARFoundation 6, we typically allow the manager to instantiate the anchor
            // or we add the component if we want to anchor an existing object.
            
            // Approach: Add ARAnchor component to the frame object (or a parent container)
            ARAnchor anchor = frame.GetComponent<ARAnchor>();
            if (anchor == null)
            {
                anchor = frame.gameObject.AddComponent<ARAnchor>();
            }

            // In a full implementation with Meta Persistence, we would interact with the subsystem here
            // e.g. XRAnchorSubsystem.TrySaveAnchor(anchor)
            
            if (anchor != null)
            {
                if (anchor.trackingState != TrackingState.None)
                {
                    activeAnchors.Add(anchor);
                    Debug.Log($"Anchor created for {artworkId} with ID: {anchor.trackableId}");
                    
                    // Notify success (simulate async save)
                    OnAnchorSaved?.Invoke(artworkId, true);
                    return;
                }
            }
            
            OnAnchorSaved?.Invoke(artworkId, false);
        }

        public void RestoreAnchors()
        {
            // For local anchors without cloud UUIDs, restoration is often handled 
            // by the subsystem automatically if 'requestedDetectionMode' includes previously saved maps,
            // or we need to load a map.
            
            // This is a simplified stub implementation for ARFoundation base.
            // On Meta Quest, you typically load the "Scene" or "Spatial Anchors" via OVR/Meta specific APIs
            // or the ARFoundation 6 Subsystem extensions.
            
            Debug.Log("Attempting to restore anchors...");
            
            // If checking for session persistence:
            if (anchorManager != null && anchorManager.subsystem != null)
            {
                // Logic to iterate trackables and re-associate with artwork IDs would go here.
                // For now, we assume success if the subsystem is running.
                if (anchorManager.subsystem.running)
                {
                    // Success (placeholder)
                    return;
                }
            }

            OnAnchorRestoreFailed?.Invoke();
        }
        
        public void ClearAnchors()
        {
            foreach(var anchor in activeAnchors)
            {
                if(anchor != null)
                    Destroy(anchor.gameObject);
            }
            activeAnchors.Clear();
        }
    }
}
