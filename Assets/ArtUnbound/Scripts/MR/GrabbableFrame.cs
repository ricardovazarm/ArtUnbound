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
                if (frameCollider != null)
                {
                    frameCollider.enabled = isGrabbable;
                }
            }
        }

        public bool IsBeingDragged => isBeingDragged;
        public Transform ClonedFrame => clonedFrame;

        private void Awake()
        {
            // Add or get collider for grab detection
            frameCollider = GetComponent<BoxCollider>();
            if (frameCollider == null)
            {
                frameCollider = gameObject.AddComponent<BoxCollider>();
                frameCollider.size = new Vector3(0.5f, 0.5f, 0.05f); // Approximate frame size
            }
            
            // Start with collider disabled
            frameCollider.enabled = false;
            
            // Set to same layer as puzzle pieces so InteractionManager can detect it
            int puzzlePieceLayer = LayerMask.NameToLayer("PuzzlePiece");
            if (puzzlePieceLayer >= 0)
            {
                gameObject.layer = puzzlePieceLayer;
                Debug.Log($"[GrabbableFrame] Set to PuzzlePiece layer ({puzzlePieceLayer})");
            }
            else
            {
                Debug.LogWarning($"[GrabbableFrame] PuzzlePiece layer not found! Current layer: {LayerMask.LayerToName(gameObject.layer)} ({gameObject.layer})");
            }
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
