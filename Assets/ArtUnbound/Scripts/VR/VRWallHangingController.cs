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
        // En VR se apunta a la pared desde lejos (a veces tras teleportarse); 2m era muy corto y el rayo
        // no alcanzaba la pared, asi que no se colgaba nada. 6m cubre el ancho de una sala de galeria.
        [SerializeField] private float wallRaycastDistance = 6f;

        [Header("Settings")]
        [SerializeField] private float placementAnimationDuration = 0.5f;
        [SerializeField] private GameObject ghostPreviewPrefab;

        public event Action OnFrameGrabbed;
        public event Action OnFramePlaced;
        public event Action OnPlacementCancelled;

        /// <summary>
        /// VR equivalente de ArtworkHangingController.OnWallArtworkResolved: disparado al resolver el
        /// soltado de una PLACA reposicionada (colgada en pared VR o retirada), con su artworkId.
        /// Lo usa el flujo de PlaqueView (GameBootstrap) para cerrar el overlay y regresar a Collection.
        /// </summary>
        public event Action<string> OnWallArtworkResolved;

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

            // artworkId limpio en el identifier (re-agarre); la unicidad de la copia la lleva el instanceId.
            var idComp = frameClone.GetComponent<PlacedArtworkIdentifier>()
                         ?? frameClone.gameObject.AddComponent<PlacedArtworkIdentifier>();
            idComp.artworkId = _currentArtworkId;

            Vector3 offsetPos = wallPos + wallRot * Vector3.forward * 0.02f;
            frameClone.SetPositionAndRotation(offsetPos, wallRot);

            string instanceId = EnsureInstanceId(frameClone.gameObject);
            SavePainting(instanceId, _currentArtworkId, offsetPos, wallRot, boardW, boardH);
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

            // instanceId unico por copia colgada (equivalente al anchorId de MR): permite varias copias
            // de la misma obra sin pisarse. El artworkId se mantiene limpio para buscar la textura.
            string instanceId = EnsureInstanceId(artworkGO);
            SavePainting(instanceId, _currentArtworkId, offsetPos, wallRotation, boardW, boardH);
            galleryController?.RegisterPlacedPainting(artworkGO);

            OnFramePlaced?.Invoke();
            return true;
        }

        /// <summary>
        /// Controller mode (VR): reposiciona en la pared virtual un PlacedArtwork ya colgado (cuadro o
        /// placa), o lo retira si <paramref name="placeOnWall"/> es false. Espeja
        /// ArtworkHangingController.ControllerRepositionWallArtwork de MR, pero por el sistema VR
        /// (capa VRWall + GalleryPersistenceService) en lugar de OVRSpatialAnchor.
        ///
        /// Si el objeto ya tiene instanceId (copia colgada/recargada) se actualiza SU entrada (preserva
        /// tamaño/datos); si no (primer colgado de una placa desde Collection) se crea la entrada.
        /// Dispara OnWallArtworkResolved al terminar.
        /// </summary>
        public void ControllerRepositionWallArtwork(string artworkId, GameObject go,
            bool placeOnWall, Vector3 wallPos, Quaternion wallRot)
        {
            if (go == null)
            {
                OnWallArtworkResolved?.Invoke(artworkId);
                return;
            }

            string galleryId = galleryController != null ? galleryController.ActiveGalleryId : null;

            if (placeOnWall)
            {
                // wallRot's +Z apunta hacia el usuario (normal de la pared) y el offset separa 2cm de la
                // pared a lo largo de +Z para evitar z-fighting.
                Vector3 offsetPos = wallPos + wallRot * Vector3.forward * 0.02f;
                go.transform.SetParent(null, worldPositionStays: true);
                go.transform.SetPositionAndRotation(offsetPos, wallRot);

                var inst = go.GetComponent<GalleryPaintingInstance>();
                bool alreadyHung = inst != null && !string.IsNullOrEmpty(inst.instanceId);

                if (alreadyHung && _persistenceService != null && galleryId != null
                    && _persistenceService.UpdatePaintingPose(galleryId, inst.instanceId, offsetPos, wallRot))
                {
                    Debug.Log($"[VRWallHanging] Repositioned {artworkId} (instance {inst.instanceId}) at {offsetPos:F2}");
                }
                else
                {
                    // Primer colgado (sin entrada previa): crear. Las placas vienen de Collection.
                    string instanceId = EnsureInstanceId(go);
                    bool isPlaque = !string.IsNullOrEmpty(artworkId) && artworkId.StartsWith("plaque_");
                    if (isPlaque) SavePlaque(instanceId, artworkId, offsetPos, wallRot);
                    else          SavePainting(instanceId, artworkId, offsetPos, wallRot);
                    Debug.Log($"[VRWallHanging] Hung {artworkId} on VR wall at {offsetPos:F2} (instance {instanceId})");
                }

                galleryController?.RegisterPlacedPainting(go);
            }
            else
            {
                // Soltado lejos de la pared: si ya estaba guardado, quitar su entrada; destruir el objeto.
                var inst = go.GetComponent<GalleryPaintingInstance>();
                if (inst != null && !string.IsNullOrEmpty(inst.instanceId)
                    && _persistenceService != null && galleryId != null)
                    _persistenceService.RemovePainting(galleryId, inst.instanceId);
                Destroy(go);
                Debug.Log($"[VRWallHanging] {artworkId} removed (aimed away from wall)");
            }

            OnWallArtworkResolved?.Invoke(artworkId);
        }

        /// <summary>Obtiene (o crea) el id de instancia unico del objeto colgado. Equivale al anchorId de MR.</summary>
        private string EnsureInstanceId(GameObject go)
        {
            var inst = go.GetComponent<GalleryPaintingInstance>() ?? go.AddComponent<GalleryPaintingInstance>();
            if (string.IsNullOrEmpty(inst.instanceId))
                inst.instanceId = System.Guid.NewGuid().ToString("N");
            return inst.instanceId;
        }

        private void SavePlaque(string instanceId, string artworkId, Vector3 position, Quaternion rotation)
        {
            if (_persistenceService == null || galleryController == null) return;

            var data = new GalleryPaintingData
            {
                instanceId      = instanceId,
                artworkId       = artworkId,   // "plaque_<id>" — VRGalleryController lo detecta al cargar
                difficultyIndex = 0,
                boardWidth      = 0f,
                boardHeight     = 0f,
                frameTier       = PresentationDecorator.CurrentFrameTier()
            };
            data.Position = position;
            data.Rotation = rotation;

            _persistenceService.SavePainting(galleryController.ActiveGalleryId, data);
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

        private void SavePainting(string instanceId, string artworkId, Vector3 position, Quaternion rotation,
                                   float boardW = 0.5f, float boardH = 0.5f)
        {
            if (_persistenceService == null || galleryController == null) return;

            var data = new GalleryPaintingData
            {
                instanceId      = instanceId,  // unico por copia colgada (clave de persistencia)
                artworkId       = artworkId,   // limpio (id real de la obra, para buscar la textura)
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
