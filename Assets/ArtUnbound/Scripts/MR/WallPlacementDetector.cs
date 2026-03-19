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
        public bool CheckNearbyWall(Vector3 position, out Vector3 wallPosition, out Quaternion wallRotation)
        {
            wallPosition = position;
            wallRotation = Quaternion.identity;
            
            // Raycast in multiple directions to find a nearby wall
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
            
            float closestDistance = float.MaxValue;
            bool foundWall = false;
            
            foreach (var direction in directions)
            {
                Ray ray = new Ray(position, direction);
                
                if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, wallLayerMask))
                {
                    if (IsWallSurface(hit) && !IsOccupiedByOtherArtwork(hit.point))
                    {
                        float distance = Vector3.Distance(position, hit.point);
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            wallPosition = hit.point;
                            wallRotation = Quaternion.LookRotation(-hit.normal);
                            foundWall = true;
                        }
                    }
                }
            }
            
            if (foundWall)
            {
                Debug.Log($"[WallPlacement] Found wall at distance: {closestDistance:F2}m");
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
