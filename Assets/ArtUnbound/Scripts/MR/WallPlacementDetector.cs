using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ArtUnbound.MR
{
    /// <summary>
    /// Detects valid wall surfaces for artwork placement using raycasting.
    /// Shows a ghost preview when a valid placement is found.
    /// </summary>
    public class WallPlacementDetector : MonoBehaviour
    {
        [Header("Raycast Settings")]
        [SerializeField] private float raycastDistance = 2f; // Max 2m from hand
        [SerializeField] private LayerMask wallLayerMask = ~0; // All layers by default
        [SerializeField] private float minDistanceToOtherArtworks = 0.15f; // 15cm minimum spacing

        [Header("Ghost Preview")]
        [SerializeField] private GameObject ghostPreviewPrefab;
        [SerializeField] private Color validColor = new Color(0f, 1f, 0f, 0.5f); // Green semi-transparent
        [SerializeField] private Color invalidColor = new Color(1f, 0f, 0f, 0.5f); // Red semi-transparent

        private GameObject ghostPreview;
        private ARPlaneManager planeManager;
        private bool isActive;
        private bool hasValidPlacement;
        private Vector3 lastValidPosition;
        private Quaternion lastValidRotation;

        public bool HasValidPlacement => hasValidPlacement;
        public Vector3 ValidPosition => lastValidPosition;
        public Quaternion ValidRotation => lastValidRotation;

        private void Awake()
        {
            planeManager = FindFirstObjectByType<ARPlaneManager>();
        }

    /// <summary>
    /// Checks if there's a valid wall near the given position (without continuous tracking).
    /// Used when releasing the frame to check for placement.
    /// </summary>
    /// <param name="maxDistance">Override the search radius. Default (-1) uses raycastDistance.
    /// Pass a small value (e.g. 0.35f) to distinguish "near wall" from "in the middle of the room".</param>
    public bool CheckNearbyWall(Vector3 position, out Vector3 wallPosition, out Quaternion wallRotation, float maxDistance = -1f)
    {
        float searchDistance = maxDistance > 0f ? maxDistance : raycastDistance;
        wallPosition = position;
        wallRotation = Quaternion.identity;

        // First, try to find AR vertical planes directly (more reliable for Quest)
        if (planeManager != null)
        {
            float closestDistance = float.MaxValue;
            ARPlane closestPlane = null;
            Vector3 closestPoint = Vector3.zero;

            foreach (var plane in planeManager.trackables)
            {
                if (plane.alignment == PlaneAlignment.Vertical)
                {
                    // Find the closest point on the plane to the position
                    Vector3 planeCenter = plane.transform.position;
                    Vector3 planeNormal = plane.transform.up; // For vertical planes, 'up' is the outward normal

                    // Project position onto plane
                    Vector3 toPosition = position - planeCenter;
                    float distanceAlongNormal = Vector3.Dot(toPosition, planeNormal);
                    Vector3 projectedPoint = position - planeNormal * distanceAlongNormal;

                    // Check if projected point is within plane bounds
                    Vector3 localPoint = plane.transform.InverseTransformPoint(projectedPoint);
                    Vector2 localPoint2D = new Vector2(localPoint.x, localPoint.z);

                    // Simple bounds check (planes are roughly rectangular)
                    float halfWidth = plane.size.x / 2f;
                    float halfHeight = plane.size.y / 2f;

                    if (Mathf.Abs(localPoint2D.x) <= halfWidth && Mathf.Abs(localPoint2D.y) <= halfHeight)
                    {
                        float distance = Mathf.Abs(distanceAlongNormal);

                        if (distance < closestDistance && distance < searchDistance)
                        {
                            closestDistance = distance;
                            closestPlane = plane;
                            closestPoint = projectedPoint;
                        }
                    }
                }
            }
            
            if (closestPlane != null && !IsOccupiedByOtherArtwork(closestPoint))
            {
                wallPosition = closestPoint;
                
                // Get the plane's normal (perpendicular to wall surface)
                Vector3 planeNormal = closestPlane.transform.up;
                
                // Determine which direction the artwork should face
                // It should face AWAY from the wall, towards the user
                // Check which side of the plane the user/camera is on
                Vector3 cameraPosition = Camera.main != null ? Camera.main.transform.position : position;
                Vector3 toCamera = cameraPosition - closestPoint;
                
                // If the plane normal points towards the camera, use it as-is
                // If it points away from camera, flip it
                float dotToCamera = Vector3.Dot(planeNormal, toCamera.normalized);
                Vector3 outwardNormal = dotToCamera > 0 ? planeNormal : -planeNormal;
                
                // The artwork should face in the direction of the outward normal
                wallRotation = Quaternion.LookRotation(outwardNormal);
                
                Debug.Log($"[WallPlacement] Found AR plane wall at distance: {closestDistance:F2}m, plane: {closestPlane.name}");
                Debug.Log($"[WallPlacement] Plane normal: {planeNormal}, Dot to camera: {dotToCamera:F2}, Outward normal: {outwardNormal}");
                Debug.Log($"[WallPlacement] Wall rotation: {wallRotation.eulerAngles}");
                return true;
            }
        }
        
        // Fallback: Raycast in multiple directions to find a nearby wall (for non-AR walls or as backup)
        Vector3[] directions = new Vector3[]
        {
            Vector3.forward,
            Vector3.back,
            Vector3.left,
            Vector3.right,
            (Vector3.forward + Vector3.left).normalized,
            (Vector3.forward + Vector3.right).normalized,
            (Vector3.back + Vector3.left).normalized,
            (Vector3.back + Vector3.right).normalized
        };

        float closestDistance_raycast = float.MaxValue;
        bool foundWall = false;
        Vector3 bestWallNormal = Vector3.zero;

        foreach (var direction in directions)
        {
            Ray ray = new Ray(position, direction);

            if (Physics.Raycast(ray, out RaycastHit hit, searchDistance, wallLayerMask))
            {
                if (IsWallSurface(hit) && !IsOccupiedByOtherArtwork(hit.point))
                {
                    float distance = Vector3.Distance(position, hit.point);
                    if (distance < closestDistance_raycast)
                    {
                        closestDistance_raycast = distance;
                        wallPosition = hit.point;
                        bestWallNormal = hit.normal;
                        foundWall = true;
                    }
                }
            }
        }
        
        if (foundWall)
        {
            // Determine which direction the artwork should face
            // The hit normal points away from the surface we hit
            // We want the artwork to face the same direction as the normal (outward from wall)
            Vector3 cameraPosition = Camera.main != null ? Camera.main.transform.position : position;
            Vector3 toCamera = cameraPosition - wallPosition;
            
            // If the wall normal points towards the camera, use it as-is
            // If it points away from camera, flip it
            float dotToCamera = Vector3.Dot(bestWallNormal, toCamera.normalized);
            Vector3 outwardNormal = dotToCamera > 0 ? bestWallNormal : -bestWallNormal;
            
            wallRotation = Quaternion.LookRotation(outwardNormal);
            
            Debug.Log($"[WallPlacement] Found wall via raycast at distance: {closestDistance_raycast:F2}m");
            Debug.Log($"[WallPlacement] Wall normal: {bestWallNormal}, Dot to camera: {dotToCamera:F2}, Outward normal: {outwardNormal}");
        }
        else
        {
            Debug.Log($"[WallPlacement] No wall found near position {position}");
        }
        
        return foundWall;
    }

        /// <summary>
        /// Starts detecting valid wall placements.
        /// </summary>
        public void StartDetection()
        {
            isActive = true;
            CreateGhostPreview();
            Debug.Log("[WallPlacement] Started detection");
        }

        /// <summary>
        /// Stops detecting and hides the ghost preview.
        /// </summary>
        public void StopDetection()
        {
            isActive = false;
            hasValidPlacement = false;
            
            if (ghostPreview != null)
            {
                ghostPreview.SetActive(false);
            }

            Debug.Log("[WallPlacement] Stopped detection");
        }

        private void Update()
        {
            if (!isActive) return;

            UpdatePlacementDetection();
        }

        private void UpdatePlacementDetection()
        {
            // Raycast from the frame's current position forward
            Ray ray = new Ray(transform.position, transform.forward);
            
            if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, wallLayerMask))
            {
                // Check if hit surface is a wall (vertical plane)
                bool isWall = IsWallSurface(hit);
                
                // Check if there's enough space (no other artworks nearby)
                bool hasSpace = !IsOccupiedByOtherArtwork(hit.point);

                if (isWall && hasSpace)
                {
                    // Valid placement found
                    hasValidPlacement = true;
                    lastValidPosition = hit.point;
                    lastValidRotation = Quaternion.LookRotation(-hit.normal); // Face outward from wall

                    UpdateGhostPreview(true, hit.point, lastValidRotation);
                }
                else
                {
                    // Invalid placement
                    hasValidPlacement = false;
                    UpdateGhostPreview(false, hit.point, Quaternion.LookRotation(-hit.normal));
                }
            }
            else
            {
                // No surface hit - hide ghost
                hasValidPlacement = false;
                if (ghostPreview != null)
                {
                    ghostPreview.SetActive(false);
                }
            }
        }

        private bool IsWallSurface(RaycastHit hit)
        {
            // Check if the normal is roughly vertical (pointing horizontally)
            float verticalDot = Vector3.Dot(hit.normal, Vector3.up);
            bool isVertical = Mathf.Abs(verticalDot) < 0.5f; // Normal is horizontal (wall-like)

            // Try to find the AR plane at this position
            if (planeManager != null)
            {
                foreach (var plane in planeManager.trackables)
                {
                    if (plane.alignment == PlaneAlignment.Vertical)
                    {
                        // Check if hit point is on this plane
                        Vector3 localPoint = plane.transform.InverseTransformPoint(hit.point);
                        if (Mathf.Abs(localPoint.z) < 0.1f) // Within 10cm of plane
                        {
                            return true;
                        }
                    }
                }
            }

            return isVertical;
        }

        private bool IsOccupiedByOtherArtwork(Vector3 position)
        {
            // Check for nearby artworks using sphere overlap
            Collider[] nearbyColliders = Physics.OverlapSphere(position, minDistanceToOtherArtworks);
            
            foreach (var collider in nearbyColliders)
            {
                // Check if this collider belongs to another placed artwork
                if (collider.CompareTag("PlacedArtwork"))
                {
                    return true;
                }
            }

            return false;
        }

        private void CreateGhostPreview()
        {
            if (ghostPreview != null)
            {
                ghostPreview.SetActive(true);
                return;
            }

            if (ghostPreviewPrefab != null)
            {
                ghostPreview = Instantiate(ghostPreviewPrefab);
                ghostPreview.name = "WallPlacementGhost";
            }
            else
            {
                // Create simple quad as ghost
                ghostPreview = GameObject.CreatePrimitive(PrimitiveType.Quad);
                ghostPreview.name = "WallPlacementGhost";
                Destroy(ghostPreview.GetComponent<Collider>());
                
                // Create semi-transparent material
                var renderer = ghostPreview.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                    if (shader == null) shader = Shader.Find("Unlit/Color");
                    
                    Material mat = new Material(shader);
                    mat.color = validColor;
                    renderer.material = mat;
                }
            }

            ghostPreview.SetActive(false);
        }

        private void UpdateGhostPreview(bool isValid, Vector3 position, Quaternion rotation)
        {
            if (ghostPreview == null) return;

            ghostPreview.SetActive(true);
            ghostPreview.transform.position = position + rotation * Vector3.forward * 0.01f; // 1cm from wall
            ghostPreview.transform.rotation = rotation;

            // Update color based on validity
            var renderer = ghostPreview.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                Color targetColor = isValid ? validColor : invalidColor;
                renderer.material.color = targetColor;
            }

            // Optional: Add pulsing effect
            float pulse = Mathf.Sin(Time.time * 3f) * 0.1f + 0.9f;
            ghostPreview.transform.localScale = Vector3.one * pulse;
        }

        private void OnDestroy()
        {
            if (ghostPreview != null)
            {
                Destroy(ghostPreview);
            }
        }
    }
}
