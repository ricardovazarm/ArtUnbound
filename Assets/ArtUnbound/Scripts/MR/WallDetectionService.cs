using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ArtUnbound.MR
{
    /// <summary>
    /// Detects walls in the room using AR Foundation plane tracking.
    /// Uses ARPlaneManager with Meta Scene Model (Space Setup).
    /// Accepts vertical planes OR planes with WallFace/InvisibleWallFace classification.
    /// </summary>
    public class WallDetectionService : MonoBehaviour
    {
        [SerializeField] private ARPlaneManager planeManager;

        private void Awake()
        {
            if (planeManager == null)
                planeManager = FindFirstObjectByType<ARPlaneManager>();
        }

        /// <summary>
        /// Scans for walls and logs the result.
        /// Returns the number of walls found.
        /// </summary>
        public int DetectWalls()
        {
            var (wallCount, totalPlanes, details) = GetWallCountWithDiagnostics();

            if (totalPlanes == 0)
                Debug.Log("[WallDetection] No planes from Scene Model. Check: 1) Space Setup completed, 2) Spatial data permission granted, 3) Meta Quest: Planes feature enabled in XR Plug-in Management.");
            else if (wallCount > 0)
                Debug.Log($"[WallDetection] Found {wallCount} wall(s) in the room. (Total planes: {totalPlanes})");
            else
                Debug.Log($"[WallDetection] No walls in {totalPlanes} planes. Plane details: {details}");

            return wallCount;
        }

        /// <summary>
        /// Returns the current count of detected walls.
        /// Walls = vertical alignment OR WallFace/InvisibleWallFace/DoorFrame/WindowFrame classification.
        /// </summary>
        public int GetWallCount()
        {
            var (count, _, _) = GetWallCountWithDiagnostics();
            return count;
        }

        private (int wallCount, int totalPlanes, string details) GetWallCountWithDiagnostics()
        {
            if (planeManager == null)
            {
                planeManager = FindFirstObjectByType<ARPlaneManager>();
                if (planeManager == null)
                {
                    Debug.LogWarning("[WallDetection] ARPlaneManager not found. Is XR Origin with AR components active?");
                    return (0, 0, "ARPlaneManager not found");
                }
            }

            int wallCount = 0;
            int total = 0;
            var detailParts = new System.Collections.Generic.List<string>();

            foreach (var plane in planeManager.trackables)
            {
                total++;
                bool isWall = IsWall(plane);
                if (isWall) wallCount++;

                if (total <= 5) // Log first 5 for diagnostics
                {
                    bool vert = plane.alignment == PlaneAlignment.Vertical;
                    var cls = plane.classifications;
                    detailParts.Add($"#{total} align={plane.alignment} vert={vert} cls={cls}");
                }
            }

            string details = detailParts.Count > 0 ? string.Join("; ", detailParts) : "none";
            return (wallCount, total, details);
        }

        private static bool IsWall(ARPlane plane)
        {
            // Meta Scene Model: vertical alignment OR semantic classification
            if (plane.alignment == PlaneAlignment.Vertical)
                return true;

            var c = plane.classifications;
            return (c & (PlaneClassifications.WallFace | PlaneClassifications.InvisibleWallFace |
                        PlaneClassifications.DoorFrame | PlaneClassifications.WindowFrame)) != 0;
        }

        /// <summary>
        /// Ensures plane detection is enabled for vertical (walls).
        /// Call when entering PostGame to maximize wall detection.
        /// </summary>
        public void EnsureWallDetectionEnabled()
        {
            if (planeManager == null)
                planeManager = FindFirstObjectByType<ARPlaneManager>();

            if (planeManager != null && !planeManager.enabled)
            {
                planeManager.enabled = true;
                planeManager.requestedDetectionMode = PlaneDetectionMode.Vertical;
            }
        }
    }
}
