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
        [SerializeField] private ArtworkCatalog artworkCatalog;

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

            float boardW = 0.5f, boardH = 0.5f;
            puzzleBoard?.GetBoardDimensions(out boardW, out boardH);

            Vector3 offsetPos = wallPos + wallRot * Vector3.forward * 0.02f;
            frameClone.SetPositionAndRotation(offsetPos, wallRot);

            SavePainting(offsetPos, wallRot, boardW, boardH);
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

            float boardW = 0.5f, boardH = 0.5f;
            puzzleBoard?.GetBoardDimensions(out boardW, out boardH);

            Vector3 offsetPos = wallPosition + wallRotation * Vector3.forward * 0.02f;
            GameObject artworkGO = PlacedArtworkFactory.Build(
                _currentArtworkId, _currentFrameTier, boardW, boardH, artworkCatalog);
            artworkGO.transform.SetPositionAndRotation(offsetPos, wallRotation);

            Debug.Log($"[VRWallHanging] Placed at worldPos={offsetPos:F2}. Teleport near X={offsetPos.x:F1} Z={offsetPos.z:F1}");

            SavePainting(offsetPos, wallRotation, boardW, boardH);
            galleryController?.RegisterPlacedPainting(artworkGO);

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
                Vector3 previewPos = wallPos + wallRot * Vector3.forward * 0.02f;
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

            // Primary: VRWall layer (requires scene objects tagged with VRWall layer)
            if (vrWallLayerMask.value != 0 &&
                Physics.Raycast(ray, out RaycastHit hit, wallRaycastDistance, vrWallLayerMask))
            {
                wallPos = hit.point;
                wallRot = Quaternion.LookRotation(hit.normal, Vector3.up);
                Debug.Log($"[VRWall-Ray] HIT (VRWall) '{hit.collider.name}' dist={hit.distance:F2}m");
                return true;
            }

            // Fallback: all layers except PuzzlePiece/UI, filtered by surface normal
            // wall-like surfaces have a mostly-horizontal normal (small Y component)
            int puzzleLayer = LayerMask.NameToLayer("PuzzlePiece");
            int uiLayer     = LayerMask.NameToLayer("UI");
            LayerMask fallbackMask = ~0;
            if (puzzleLayer >= 0) fallbackMask &= ~(1 << puzzleLayer);
            if (uiLayer >= 0)     fallbackMask &= ~(1 << uiLayer);

            if (Physics.Raycast(ray, out RaycastHit fbHit, wallRaycastDistance, fallbackMask))
            {
                float normalY = Mathf.Abs(fbHit.normal.y);
                if (normalY < 0.5f)
                {
                    wallPos = fbHit.point;
                    wallRot = Quaternion.LookRotation(fbHit.normal, Vector3.up);
                    Debug.Log($"[VRWall-Ray] HIT (fallback) '{fbHit.collider.name}' layer={LayerMask.LayerToName(fbHit.collider.gameObject.layer)} dist={fbHit.distance:F2}m normalY={normalY:F2}");
                    return true;
                }
            }

            RaycastHit[] allHits = Physics.RaycastAll(ray, wallRaycastDistance);
            Debug.Log($"[VRWall-Ray] MISS  maxDist={wallRaycastDistance}m  mask={vrWallLayerMask.value}  origin={ray.origin:F2}  dir={ray.direction:F2}  unmasked_hits={allHits.Length}");
            foreach (var h in allHits)
                Debug.Log($"[VRWall-Ray]   hit '{h.collider.name}' layer={LayerMask.LayerToName(h.collider.gameObject.layer)} dist={h.distance:F2}m normalY={h.normal.y:F2}");
            return false;
        }

        /// <summary>Controller mode: fires OnFrameGrabbed to hide UI panels when frame is selected.</summary>
        public void NotifyFrameSelected()
        {
            OnFrameGrabbed?.Invoke();
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

            // Fallback mask excludes puzzle/UI layers when VRWall layer is not configured
            int puzzleLayer = LayerMask.NameToLayer("PuzzlePiece");
            int uiLayer     = LayerMask.NameToLayer("UI");
            LayerMask fallbackMask = ~0;
            if (puzzleLayer >= 0) fallbackMask &= ~(1 << puzzleLayer);
            if (uiLayer >= 0)     fallbackMask &= ~(1 << uiLayer);
            LayerMask activeMask = vrWallLayerMask.value != 0 ? vrWallLayerMask : fallbackMask;

            foreach (var dir in directions)
            {
                if (Physics.Raycast(origin, dir, out RaycastHit hit, wallProximityRadius, activeMask))
                {
                    if (activeMask == fallbackMask && Mathf.Abs(hit.normal.y) >= 0.5f)
                        continue; // skip floors/ceilings in fallback mode
                    wallPos = hit.point;
                    wallRot = Quaternion.LookRotation(hit.normal, Vector3.up);
                    return true;
                }
            }

            return false;
        }

        private void SavePainting(Vector3 position, Quaternion rotation,
                                   float boardW = 0.5f, float boardH = 0.5f)
        {
            if (_persistenceService == null || galleryController == null) return;

            var data = new GalleryPaintingData
            {
                artworkId       = _currentArtworkId,
                difficultyIndex = _currentDifficultyIndex,
                boardWidth      = boardW,
                boardHeight     = boardH,
                frameTier       = _currentFrameTier
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
