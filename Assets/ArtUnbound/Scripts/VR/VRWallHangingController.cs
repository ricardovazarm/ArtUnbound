using System;
using ArtUnbound.Data;
using ArtUnbound.Gameplay;
using ArtUnbound.MR;
using ArtUnbound.Services;
using UnityEngine;

namespace ArtUnbound.VR
{
    /// <summary>
    /// VR equivalent of ArtworkHangingController.
    /// Detects walls via Physics.Raycast against VRWall layer instead of ARPlaneManager.
    /// Saves painting positions via GalleryPersistenceService instead of WallAnchorManager.
    /// </summary>
    public class VRWallHangingController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PuzzleBoard puzzleBoard;
        [SerializeField] private VRGalleryController galleryController;

        [Header("Wall Detection")]
        [SerializeField] private LayerMask vrWallLayerMask;
        [SerializeField] private float wallProximityRadius = 0.35f;
        [SerializeField] private float wallRaycastDistance = 2f;

        [Header("Settings")]
        [SerializeField] private float placementAnimationDuration = 0.5f;
        [SerializeField] private GameObject ghostPreviewPrefab;

        public event Action OnFrameGrabbed;
        public event Action OnFramePlaced;
        public event Action OnPlacementCancelled;

        private GalleryPersistenceService _persistenceService;
        private string _currentArtworkId;
        private int _currentDifficultyIndex;
        private FrameTier _currentFrameTier;
        private Transform _completedFrame;
        private GameObject _ghostInstance;

        private bool _isAnimating;
        private float _animationTime;
        private Vector3 _animStartPos, _animTargetPos;
        private Quaternion _animStartRot, _animTargetRot;

        private void Awake()
        {
            gameObject.SetActive(false);

            if (ghostPreviewPrefab != null)
            {
                _ghostInstance = Instantiate(ghostPreviewPrefab);
                _ghostInstance.SetActive(false);
            }
        }

        public void Initialize(GalleryPersistenceService persistenceService)
        {
            _persistenceService = persistenceService;
        }

        // ─── Same public API as ArtworkHangingController ───────────────────────

        public void EnableFrameGrab(string artworkId, int difficultyIndex, FrameTier frameTier)
        {
            if (puzzleBoard == null)
            {
                Debug.LogError("[VRWallHanging] PuzzleBoard is null");
                return;
            }

            _currentArtworkId = artworkId;
            _currentDifficultyIndex = difficultyIndex;
            _currentFrameTier = frameTier;

            _completedFrame = GetCompletedFrameTransform();
            if (_completedFrame == null)
            {
                Debug.LogError("[VRWallHanging] No completed frame found");
                return;
            }

            var grabbable = _completedFrame.GetComponent<GrabbableFrame>()
                            ?? _completedFrame.gameObject.AddComponent<GrabbableFrame>();
            grabbable.OnFrameGrabbed += HandleFrameGrabbed;
            grabbable.IsGrabbable = true;

            Debug.Log($"[VRWallHanging] Frame grab enabled for {artworkId}");
        }

        public void DisableFrameGrab()
        {
            if (_completedFrame != null)
            {
                var grabbable = _completedFrame.GetComponent<GrabbableFrame>();
                if (grabbable != null)
                {
                    grabbable.OnFrameGrabbed -= HandleFrameGrabbed;
                    grabbable.IsGrabbable = false;
                    Destroy(grabbable);
                }
            }
            _completedFrame = null;
            HideGhost();
            Debug.Log("[VRWallHanging] Frame grab disabled");
        }

        /// <summary>Called by InteractionManager on trigger release (same flow as MR).</summary>
        public bool TryPlaceOnWall(Transform frameClone, Vector3 position)
        {
            if (frameClone == null)
            {
                OnPlacementCancelled?.Invoke();
                return false;
            }

            if (!FindNearbyVRWall(position, out Vector3 wallPos, out Quaternion wallRot))
            {
                Debug.Log("[VRWallHanging] No VR wall found nearby");
                OnPlacementCancelled?.Invoke();
                return false;
            }

            Vector3 offsetPos = wallPos + wallRot * Vector3.forward * 0.005f;
            frameClone.SetPositionAndRotation(offsetPos, wallRot);

            SavePainting(offsetPos, wallRot);
            galleryController?.RegisterPlacedPainting(frameClone.gameObject);

            HideGhost();
            OnFramePlaced?.Invoke();
            Debug.Log($"[VRWallHanging] Painting placed at {offsetPos}");
            return true;
        }

        /// <summary>Controller mode: place at a known wall position.</summary>
        public bool PlaceFrameAtWall(Vector3 wallPosition, Quaternion wallRotation)
        {
            if (_completedFrame == null) return false;

            Vector3 offsetPos = wallPosition + wallRotation * Vector3.forward * 0.005f;
            Transform clone = Instantiate(_completedFrame, offsetPos, wallRotation);
            clone.name = _completedFrame.name + "_VRPlaced";
            clone.SetParent(null);

            var grabbable = clone.GetComponent<GrabbableFrame>();
            if (grabbable != null) Destroy(grabbable);
            var col = clone.GetComponent<BoxCollider>();
            if (col != null) Destroy(col);
            foreach (var p in clone.GetComponentsInChildren<PuzzlePiece>())
                Destroy(p);

            SavePainting(offsetPos, wallRotation);
            galleryController?.RegisterPlacedPainting(clone.gameObject);

            OnFramePlaced?.Invoke();
            return true;
        }

        // ─── Preview ghost during drag ──────────────────────────────────────────

        public void UpdateWallPreview(Vector3 dragPosition)
        {
            if (_ghostInstance == null) return;

            if (FindNearbyVRWall(dragPosition, out Vector3 wallPos, out Quaternion wallRot))
            {
                _ghostInstance.SetActive(true);
                Vector3 previewPos = wallPos + wallRot * Vector3.forward * 0.005f;
                _ghostInstance.transform.SetPositionAndRotation(previewPos, wallRot);
            }
            else
            {
                HideGhost();
            }
        }

        // ─── Internal ─────────────────────────────────────────────────────────

        private void HandleFrameGrabbed()
        {
            OnFrameGrabbed?.Invoke();
        }

        /// <summary>Controller mode: raycast from pointer ray to a VR wall.</summary>
        public bool RaycastToWall(Ray ray, out Vector3 wallPos, out Quaternion wallRot)
        {
            wallPos = Vector3.zero;
            wallRot = Quaternion.identity;
            if (Physics.Raycast(ray, out RaycastHit hit, wallRaycastDistance, vrWallLayerMask))
            {
                wallPos = hit.point;
                wallRot = Quaternion.LookRotation(-hit.normal, Vector3.up);
                return true;
            }
            return false;
        }

        private bool FindNearbyVRWall(Vector3 origin, out Vector3 wallPos, out Quaternion wallRot)
        {
            wallPos = Vector3.zero;
            wallRot = Quaternion.identity;

            // 8-direction horizontal raycast
            Vector3[] directions = {
                Vector3.forward, Vector3.back, Vector3.left, Vector3.right,
                (Vector3.forward + Vector3.right).normalized, (Vector3.forward + Vector3.left).normalized,
                (Vector3.back + Vector3.right).normalized, (Vector3.back + Vector3.left).normalized
            };

            foreach (var dir in directions)
            {
                if (Physics.Raycast(origin, dir, out RaycastHit hit, wallProximityRadius, vrWallLayerMask))
                {
                    wallPos = hit.point;
                    // Rotation: face away from wall (toward user side)
                    wallRot = Quaternion.LookRotation(-hit.normal, Vector3.up);
                    return true;
                }
            }

            return false;
        }

        private void SavePainting(Vector3 position, Quaternion rotation)
        {
            if (_persistenceService == null || galleryController == null) return;

            var data = new GalleryPaintingData
            {
                artworkId = _currentArtworkId,
                difficultyIndex = _currentDifficultyIndex
            };
            data.Position = position;
            data.Rotation = rotation;

            _persistenceService.SavePainting(galleryController.ActiveGalleryId, data);
        }

        private void HideGhost()
        {
            if (_ghostInstance != null) _ghostInstance.SetActive(false);
        }

        private Transform GetCompletedFrameTransform()
        {
            if (puzzleBoard == null) return null;
            return puzzleBoard.transform.Find("SlotRoot");
        }

        private void OnDestroy()
        {
            DisableFrameGrab();
            if (_ghostInstance != null) Destroy(_ghostInstance);
        }
    }
}
