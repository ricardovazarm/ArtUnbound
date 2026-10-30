using System;
using UnityEngine;

namespace ArtUnbound.MR
{
    /// <summary>
    /// Makes a completed puzzle frame grabbable using the same interaction system as puzzle pieces.
    /// This component should be added to the SlotRoot when the frame becomes grabbable.
    /// </summary>
    public class GrabbableFrame : MonoBehaviour
    {
        [Header("Interaction")]
        [SerializeField] private bool isGrabbable = false;
        
        private Vector3 grabOffset;
        private bool isBeingDragged = false;
        private BoxCollider frameCollider;
        private Transform originalFrame; // Reference to original frame
        private Transform clonedFrame; // The clone we're actually dragging
        
        // Event fired when frame is grabbed (so UI can hide)
        public event Action OnFrameGrabbed;

        public bool IsGrabbable
        {
            get => isGrabbable;
            set
            {
                isGrabbable = value;
                // Refresh reference if the previous collider was destroyed
                if (frameCollider == null)
                    frameCollider = GetComponent<BoxCollider>();
                if (frameCollider != null)
                    frameCollider.enabled = isGrabbable;
            }
        }

        public bool IsBeingDragged => isBeingDragged;
        public Transform ClonedFrame => clonedFrame;

        private void Awake()
        {
            frameCollider = GetComponent<BoxCollider>();
            // Un root con PlacedArtworkIdentifier dimensionado (PlacedArtworkFactory,
            // SpawnArtworkAtAnchor) ya trae un collider = geometria real de la obra: no pisarlo.
            // En cualquier otro caso (marco del puzzle, placas) se ajusta a los renderers. No basta
            // con "ya habia collider": el del SlotRoot sobrevive entre partidas y heredaria el
            // tamano de la obra anterior.
            var identifier = GetComponent<PlacedArtworkIdentifier>();
            bool trustExistingCollider = frameCollider != null
                && identifier != null && identifier.HasDimensions
                && frameCollider.size.x > 0.01f && frameCollider.size.y > 0.01f;
            if (frameCollider == null)
                frameCollider = gameObject.AddComponent<BoxCollider>();

            if (!trustExistingCollider)
                FitColliderToRenderers();

            frameCollider.enabled = false;

            int puzzlePieceLayer = LayerMask.NameToLayer("PuzzlePiece");
            if (puzzlePieceLayer >= 0)
                gameObject.layer = puzzlePieceLayer;
            else
                Debug.LogWarning($"[GrabbableFrame] PuzzlePiece layer not found! layer={LayerMask.LayerToName(gameObject.layer)}");
        }

        private void FitColliderToRenderers()
        {
            var renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
            if (renderers.Length == 0)
            {
                frameCollider.center = Vector3.zero;
                frameCollider.size = new Vector3(0.5f, 0.5f, 0.05f);
                Debug.LogWarning("[GrabbableFrame] No renderers found; using fallback collider size");
                return;
            }

            // Caja envolvente en ESPACIO LOCAL del root: se llevan las 8 esquinas de cada renderer
            // a coordenadas locales y se encapsulan. La version anterior tomaba la AABB en ejes del
            // mundo y la dividia por la escala: con la obra girada respecto a X/Z del mundo, el
            // ancho se mezclaba con la profundidad y el collider salia "portrait y delgado".
            // Se excluyen cedula y lampara (decoracion fuera del lienzo).
            bool any = false;
            Bounds local = new Bounds();
            for (int i = 0; i < renderers.Length; i++)
            {
                if (IsDecoration(renderers[i].transform)) continue;
                Bounds b = renderers[i].bounds;
                Vector3 min = b.min, max = b.max;
                for (int c = 0; c < 8; c++)
                {
                    var corner = new Vector3(
                        (c & 1) == 0 ? min.x : max.x,
                        (c & 2) == 0 ? min.y : max.y,
                        (c & 4) == 0 ? min.z : max.z);
                    Vector3 p = transform.InverseTransformPoint(corner);
                    if (!any) { local = new Bounds(p, Vector3.zero); any = true; }
                    else local.Encapsulate(p);
                }
            }

            if (!any)
            {
                frameCollider.center = Vector3.zero;
                frameCollider.size = new Vector3(0.5f, 0.5f, 0.05f);
                Debug.LogWarning("[GrabbableFrame] Only decoration renderers found; using fallback collider size");
                return;
            }

            // Cap Z depth to 0.05m world-space: child renderers (puzzle pieces in tray)
            // inflate the bounding box in Z, causing the collider to engulf the controller
            // position and making Physics.RaycastAll return 0 hits (ray starts inside collider).
            Vector3 s = transform.lossyScale;
            const float maxWorldDepth = 0.05f;
            float localZ = Mathf.Abs(s.z) > 0.001f ? maxWorldDepth / Mathf.Abs(s.z) : maxWorldDepth;
            frameCollider.center = new Vector3(local.center.x, local.center.y, local.center.z);
            frameCollider.size = new Vector3(local.size.x, local.size.y, localZ);

            Debug.Log($"[GrabbableFrame] Collider fit to renderers: center={frameCollider.center} size={frameCollider.size}");
        }

        /// <summary>True si el transform cuelga de la cedula o la lampara (PresentationDecorator).</summary>
        private bool IsDecoration(Transform t)
        {
            for (var p = t; p != null && p != transform; p = p.parent)
            {
                if (p.name == "Cedula" || p.name == "Lampara") return true;
            }
            return false;
        }

        public void StartDrag(Vector3 pinchPosition)
        {
            if (isBeingDragged) return;
            
            Debug.Log($"[GrabbableFrame] StartDrag called at pinch position: {pinchPosition}");
            
            isBeingDragged = true;
            
        // Store reference to original
        originalFrame = transform;
        Debug.Log($"[GrabbableFrame] Original frame: {originalFrame.name} at {originalFrame.position}");
        
        // Clone the entire frame (SlotRoot with all children), preserving world position/rotation
        clonedFrame = Instantiate(transform, originalFrame.position, originalFrame.rotation);
        clonedFrame.name = transform.name + "_Clone";
        clonedFrame.SetParent(null); // Make it independent
        clonedFrame.localScale = originalFrame.localScale; // Preserve scale
        
        Debug.Log($"[GrabbableFrame] Created clone: {clonedFrame.name} at {clonedFrame.position}");
        Debug.Log($"[GrabbableFrame] Clone has {clonedFrame.childCount} children");
            
            // Remove ONLY GrabbableFrame and colliders from clone (keep visual components)
            var grabbableFrameComp = clonedFrame.GetComponent<GrabbableFrame>();
            if (grabbableFrameComp != null) Destroy(grabbableFrameComp);
            
            var cloneCollider = clonedFrame.GetComponent<BoxCollider>();
            if (cloneCollider != null) Destroy(cloneCollider);
            
            // Remove PuzzlePiece scripts from children (but keep renderers)
            var childPieceScripts = clonedFrame.GetComponentsInChildren<ArtUnbound.Gameplay.PuzzlePiece>();
            Debug.Log($"[GrabbableFrame] Found {childPieceScripts.Length} PuzzlePiece scripts to remove");
            foreach (var piece in childPieceScripts)
            {
                Destroy(piece);
            }
            
            // Check renderers
            var renderers = clonedFrame.GetComponentsInChildren<Renderer>();
            Debug.Log($"[GrabbableFrame] Clone has {renderers.Length} renderers");
            foreach (var r in renderers)
            {
                Debug.Log($"  - Renderer: {r.name}, enabled: {r.enabled}, material: {r.material?.name}");
            }
            
            // Hide original frame
            originalFrame.gameObject.SetActive(false);
            Debug.Log($"[GrabbableFrame] Original frame hidden");
            
            // Calculate grab offset from cloned frame
            grabOffset = clonedFrame.position - pinchPosition;
            
            // Fire event so UI can hide
            OnFrameGrabbed?.Invoke();
            
            Debug.Log($"[GrabbableFrame] Started drag with clone at {clonedFrame.position}. Offset: {grabOffset}");
        }

        public void UpdateDrag(Vector3 pinchPosition)
        {
            if (isBeingDragged && clonedFrame != null)
            {
                clonedFrame.position = pinchPosition + grabOffset;
            }
        }

        public void EndDrag()
        {
            isBeingDragged = false;
            Debug.Log("[GrabbableFrame] Ended drag");
            
            // Note: We keep the clone in the world (this will be the anchored artwork)
            // The original stays hidden
        }
        
        public void RestoreOriginal()
        {
            if (originalFrame != null)
            {
                originalFrame.gameObject.SetActive(true);
                Debug.Log("[GrabbableFrame] Restored original frame");
            }
        }
    }
}
