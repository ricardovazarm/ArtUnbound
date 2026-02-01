using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ArtUnbound.Core
{
    public class WallSelectionController : MonoBehaviour
    {
        public event Action<Transform> OnWallConfirmed;

        [Header("AR Components")]
        [SerializeField] private ARPlaneManager planeManager;
        [SerializeField] private ARRaycastManager raycastManager;

        [Header("Configuration")]
        // [SerializeField] private float raycastDistance = 10f; // Unused
        [SerializeField] private Color validWallColor = Color.cyan;
        [SerializeField] private Color invalidColor = Color.red;

        private bool isScanning = false;
        private List<ARRaycastHit> hits = new List<ARRaycastHit>();

        private void Awake()
        {
            if (planeManager == null)
                planeManager = FindFirstObjectByType<ARPlaneManager>();
            
            if (raycastManager == null)
                raycastManager = FindFirstObjectByType<ARRaycastManager>();
        }

        public void StartWallScan()
        {
            if (planeManager != null)
            {
                planeManager.enabled = true;
                // Optional: Set requested detection mode to Vertical or specific classifications if supported
                planeManager.requestedDetectionMode = PlaneDetectionMode.Vertical;
            }
            isScanning = true;
        }

        public void StopWallScan()
        {
            isScanning = false;
            // We might want to keep the planes visible or hide them depending on design
            // For now, we keep manager enabled but stop processing input logic if needed
        }

        public void TrySelectWall(Vector3 rayOrigin, Vector3 rayDirection)
        {
            if (!isScanning || raycastManager == null) return;

            Ray ray = new Ray(rayOrigin, rayDirection);
            
            // Raycast against trackables (Planes)
            if (raycastManager.Raycast(ray, hits, TrackableType.PlaneWithinPolygon))
            {
                foreach (var hit in hits)
                {
                    ARPlane plane = planeManager.GetPlane(hit.trackableId);
                    if (plane != null && IsWall(plane))
                    {
                        // Found a valid wall
                        ConfirmWall(plane.transform);
                        return;
                    }
                }
            }
        }

        private bool IsWall(ARPlane plane)
        {
            // Simple check: Alignment is Vertical
            if (plane.alignment == PlaneAlignment.Vertical) return true;

            // Advanced check: Classification (Meta Quest / ARKit / ARCore)
            // Advanced check: Classification (Meta Quest / ARKit / ARCore)
            // Deprecated API usage fixed or removed for stability. 
            // Relying on Alignment for now as it's the most robust cross-platform check.
            /*
            if (plane.classifications.HasFlag(PlaneClassifications.Wall) || 
                plane.classifications.HasFlag(PlaneClassifications.Door) ||
                plane.classifications.HasFlag(PlaneClassifications.Window))
            {
                return true;
            }
            */

            return false;
        }

        public void ConfirmWall(Transform wallTransform)
        {
            Debug.Log($"Wall confirmed: {wallTransform.name}");
            OnWallConfirmed?.Invoke(wallTransform);
            StopWallScan();
        }
    }
}
