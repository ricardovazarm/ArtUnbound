using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArtUnbound.MR
{
    /// <summary>
    /// Controls wall detection visualization and placement preview for MR.
    /// </summary>
    public class WallHighlightController : MonoBehaviour
    {
        public event Action<Vector3, Quaternion> OnWallSelected;
        public event Action OnSelectionCanceled;

        [Header("Visualization")]
        [SerializeField] private GameObject highlightPrefab;
        [SerializeField] private GameObject placementPreviewPrefab;
        [SerializeField] private Material validPlacementMaterial;
        [SerializeField] private Material invalidPlacementMaterial;
        [SerializeField] private LineRenderer pointerLine;

        [Header("Detection Settings")]
        [SerializeField] private float maxRayDistance = 10f;
        [SerializeField] private LayerMask wallLayerMask;
        [SerializeField] private float wallAngleThreshold = 30f;

        [Header("Preview Settings")]
        [SerializeField] private Vector2 defaultPreviewSize = new Vector2(0.5f, 0.5f);
        [SerializeField] private float previewOffset = 0.01f;

        [Header("References")]
        [SerializeField] private Transform rayOrigin;

        public bool IsSelecting => isSelecting;
        public bool HasValidTarget => hasValidTarget;
        public Vector3 TargetPosition => targetPosition;
        public Quaternion TargetRotation => targetRotation;

        private bool isSelecting = false;
        private bool hasValidTarget = false;
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private Vector3 targetNormal;

        private GameObject currentHighlight;
        private GameObject currentPreview;
        private MeshRenderer previewRenderer;

        private List<GameObject> detectedWallHighlights = new List<GameObject>();

        private void Awake()
        {
            if (rayOrigin == null)
            {
                var mainCam = Camera.main;
                if (mainCam != null)
                    rayOrigin = mainCam.transform;
            }

            if (pointerLine != null)
                pointerLine.enabled = false;
        }

        private void Update()
        {
            if (isSelecting)
            {
                UpdateWallDetection();
            }
        }

        /// <summary>
        /// Starts the wall selection mode.
        /// </summary>
        public void StartSelection()
        {
            isSelecting = true;
            hasValidTarget = false;

            if (pointerLine != null)
                pointerLine.enabled = true;

            CreatePreview();
        }

        /// <summary>
        /// Stops the wall selection mode.
        /// </summary>
        public void StopSelection()
        {
            isSelecting = false;
            hasValidTarget = false;

            if (pointerLine != null)
                pointerLine.enabled = false;

            DestroyPreview();
            ClearWallHighlights();
        }

        /// <summary>
        /// Confirms the current wall selection.
        /// </summary>
        public void ConfirmSelection()
        {
            if (!hasValidTarget) return;

            OnWallSelected?.Invoke(targetPosition, targetRotation);
            StopSelection();
        }

        /// <summary>
        /// Cancels the selection.
        /// </summary>
        public void CancelSelection()
        {
            StopSelection();
            OnSelectionCanceled?.Invoke();
        }

        /// <summary>
        /// Sets the preview size.
        /// </summary>
        public void SetPreviewSize(Vector2 size)
        {
            defaultPreviewSize = size;

            if (currentPreview != null)
            {
                currentPreview.transform.localScale = new Vector3(size.x, size.y, 0.01f);
            }
        }

        private void UpdateWallDetection()
        {
            if (rayOrigin == null) return;

            Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, maxRayDistance, wallLayerMask))
            {
                // Check if the surface is roughly vertical (a wall)
                float angle = Vector3.Angle(hit.normal, Vector3.up);
                bool isWall = angle > (90f - wallAngleThreshold) && angle < (90f + wallAngleThreshold);

                if (isWall)
                {
                    hasValidTarget = true;
                    targetPosition = hit.point + hit.normal * previewOffset;
                    targetNormal = hit.normal;
                    targetRotation = Quaternion.LookRotation(-hit.normal, Vector3.up);

                    UpdatePreviewPosition(true);
                    UpdatePointerLine(rayOrigin.position, hit.point, true);
                }
                else
                {
                    hasValidTarget = false;
                    UpdatePreviewPosition(false);
                    UpdatePointerLine(rayOrigin.position, hit.point, false);
                }
            }
            else
            {
                hasValidTarget = false;
                HidePreview();

                Vector3 endPoint = rayOrigin.position + rayOrigin.forward * maxRayDistance;
                UpdatePointerLine(rayOrigin.position, endPoint, false);
            }
        }

        private void CreatePreview()
        {
            if (placementPreviewPrefab != null && currentPreview == null)
            {
                currentPreview = Instantiate(placementPreviewPrefab);
                currentPreview.transform.localScale = new Vector3(
                    defaultPreviewSize.x,
                    defaultPreviewSize.y,
                    0.01f
                );

                previewRenderer = currentPreview.GetComponent<MeshRenderer>();
                currentPreview.SetActive(false);
            }
        }

        private void DestroyPreview()
        {
            if (currentPreview != null)
            {
                Destroy(currentPreview);
                currentPreview = null;
                previewRenderer = null;
            }
        }

        private void UpdatePreviewPosition(bool isValid)
        {
            if (currentPreview == null) return;

            currentPreview.SetActive(true);
            currentPreview.transform.position = targetPosition;
            currentPreview.transform.rotation = targetRotation;

            if (previewRenderer != null)
            {
                previewRenderer.material = isValid ? validPlacementMaterial : invalidPlacementMaterial;
            }
        }

        private void HidePreview()
        {
            if (currentPreview != null)
                currentPreview.SetActive(false);
        }

        private void UpdatePointerLine(Vector3 start, Vector3 end, bool isValid)
        {
            if (pointerLine == null) return;

            pointerLine.SetPosition(0, start);
            pointerLine.SetPosition(1, end);

            Color lineColor = isValid ? Color.green : Color.red;
            lineColor.a = 0.5f;

            pointerLine.startColor = lineColor;
            pointerLine.endColor = lineColor;
        }

        /// <summary>
        /// Highlights detected walls in the scene (from Scene Understanding).
        /// </summary>
        public void HighlightDetectedWalls(List<WallData> walls)
        {
            ClearWallHighlights();

            if (highlightPrefab == null) return;

            foreach (var wall in walls)
            {
                var highlight = Instantiate(highlightPrefab);
                highlight.transform.position = wall.center;
                highlight.transform.rotation = Quaternion.LookRotation(wall.normal, Vector3.up);
                highlight.transform.localScale = new Vector3(wall.size.x, wall.size.y, 0.01f);

                detectedWallHighlights.Add(highlight);
            }
        }

        private void ClearWallHighlights()
        {
            foreach (var highlight in detectedWallHighlights)
            {
                if (highlight != null)
                    Destroy(highlight);
            }
            detectedWallHighlights.Clear();
        }

        /// <summary>
        /// Manually sets the ray origin transform.
        /// </summary>
        public void SetRayOrigin(Transform origin)
        {
            rayOrigin = origin;
        }

        /// <summary>
        /// Gets the placement data for the current target.
        /// </summary>
        public (Vector3 position, Quaternion rotation, Vector3 normal) GetPlacementData()
        {
            return (targetPosition, targetRotation, targetNormal);
        }

        private void OnDestroy()
        {
            DestroyPreview();
            ClearWallHighlights();
        }

        private void OnDrawGizmosSelected()
        {
            if (rayOrigin != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(rayOrigin.position, rayOrigin.forward * maxRayDistance);
            }
        }
    }

    /// <summary>
    /// Data structure for detected wall information.
    /// </summary>
    [Serializable]
    public struct WallData
    {
        public Vector3 center;
        public Vector3 normal;
        public Vector2 size;
        public string anchorId;
    }
}
