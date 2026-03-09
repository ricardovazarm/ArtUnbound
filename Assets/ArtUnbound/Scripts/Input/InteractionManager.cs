using ArtUnbound.Data;
using ArtUnbound.Gameplay;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ArtUnbound.Input
{
    /// <summary>
    /// Bridges HandTracking input with Gameplay objects (PuzzlePieces).
    /// Handles Raycasting and Dragging logic.
    /// </summary>
    public class InteractionManager : MonoBehaviour
    {
        [SerializeField] private HandTrackingInputController inputController;
        [SerializeField] private LayerMask interactableLayer;
        [SerializeField] private float rayLength = 2.0f;
        [SerializeField] private bool useRaycast = true; // Enabled for Controllers
        [SerializeField] private LineRenderer rayVisualizer;

        private PuzzlePiece currentDraggedPiece;
        private float currentDragDistance;
        private Vector3 dragOffset;
        private int targetSlotIndex = -1; // Store the slot shown in the highlight
        private bool pieceWasFromBoard = false; // Track if the piece was grabbed from the board

        private void OnEnable()
        {
            if (inputController != null)
            {
                inputController.OnPinchStart += HandlePinchStart;
                inputController.OnPinchHold += HandlePinchHold;
                inputController.OnPinchEnd += HandlePinchEnd;
            }
        }

        private void Start()
        {
            if (rayVisualizer != null)
            {
                rayVisualizer.enabled = false;
                rayVisualizer.useWorldSpace = true;
                rayVisualizer.positionCount = 2;
                rayVisualizer.startWidth = 0.02f;
                rayVisualizer.endWidth = 0.02f;

                if (rayVisualizer.sharedMaterial == null)
                {
                    Shader s = Shader.Find("Sprites/Default");
                    if (s == null) s = Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply");
                    if (s == null) s = Shader.Find("UI/Default");

                    if (s != null)
                    {
                        rayVisualizer.material = new Material(s);
                        rayVisualizer.material.color = Color.red;
                    }
                }

                rayVisualizer.startColor = Color.red;
                rayVisualizer.endColor = Color.red;
            }
        }

        private void Update()
        {
            // El Ray Visualizer manual ha sido eliminado para evitar conflictos.
            // Si necesitas un láser visual para la UI, te recomiendo usar el 
            // XRRayInteractor o Near-Far Interactor nativo de Unity en los prefabs de las manos.
        }

        private void OnDestroy()
        {
            if (inputController != null)
            {
                inputController.OnPinchStart -= HandlePinchStart;
                inputController.OnPinchHold -= HandlePinchHold;
                inputController.OnPinchEnd -= HandlePinchEnd;
            }
        }

        private void HandlePinchStart(Vector3 position, Quaternion rotation)
        {
            // GUARD: Don't grab a new piece if we're already holding one
            if (currentDraggedPiece != null)
            {
                return;
            }

            currentDragDistance = 0f;
            dragOffset = Vector3.zero;

            // METHOD 1: Proximity/Sphere Overlap (Primary for Hand Tracking)
            float grabRadius = 0.015f; // 1.5cm radius
            Collider[] colliders = Physics.OverlapSphere(position, grabRadius, interactableLayer);

            PuzzlePiece bestPiece = null;
            float bestDist = float.MaxValue;

            if (colliders.Length > 0)
            {
                foreach (var col in colliders)
                {
                    var p = col.GetComponentInParent<PuzzlePiece>();
                    if (p != null && p.CurrentState != PieceState.Grabbed)
                    {
                        float d = Vector3.Distance(position, p.transform.position);
                        if (d < bestDist)
                        {
                            bestDist = d;
                            bestPiece = p;
                        }
                    }
                }
                
                // CRITICAL FIX: Only grab if piece is ACTUALLY within the grab radius
                // This prevents grabbing pieces that are far away but have large colliders
                if (bestPiece != null && bestDist > grabRadius)
                {
                    bestPiece = null; // Reject pieces outside the threshold
                }
            }

            // METHOD 2: Raycast Fallback (for Controllers)
            if (useRaycast && bestPiece == null)
            {
                Ray ray = new Ray(position, rotation * Vector3.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, rayLength, interactableLayer))
                {
                    var piece = hit.collider.GetComponentInParent<PuzzlePiece>();
                    if (piece != null && piece.CurrentState != PieceState.Placed && piece.CurrentState != PieceState.Grabbed)
                    {
                        // CRITICAL: Validate distance for raycast too
                        float rayDist = Vector3.Distance(position, piece.transform.position);
                        if (rayDist <= grabRadius)
                        {
                            bestPiece = piece;
                        }
                    }
                }
            }

            if (bestPiece != null)
            {
                // Track if the piece was grabbed from the board
                pieceWasFromBoard = (bestPiece.CurrentState == PieceState.Placed);
                
                // Play grab sound
                if (ArtUnbound.Feedback.AudioManager.Instance != null)
                {
                    ArtUnbound.Feedback.AudioManager.Instance.PlayPieceGrab();
                }
                
                // If the piece was placed on the board, remove it from the slot first
                if (pieceWasFromBoard)
                {
                    var board = FindFirstObjectByType<PuzzleBoard>();
                    if (board != null)
                    {
                        board.RemovePieceFromSlot(bestPiece);
                    }
                }
                
                currentDraggedPiece = bestPiece;
                dragOffset = bestPiece.transform.position - position;
                bestPiece.SetDragged(true);
            }
        }

        private void HandlePinchHold(Vector3 position, Quaternion rotation)
        {
            if (currentDraggedPiece != null)
            {
                // Move piece to follow hand position directly with offset
                Vector3 targetPoint = position + dragOffset;

                // Direct follow (no lerp) for better responsiveness
                // The piece should feel "glued" to the fingers
                currentDraggedPiece.transform.position = targetPoint;

                // Keep original rotation (don't rotate the piece)
                // The piece maintains board orientation for proper snapping
                
                // Update slot highlight using the ACTUAL piece position (not calculated position)
                // This ensures the highlight is always based on where the piece really is
                var board = FindFirstObjectByType<PuzzleBoard>();
                if (board != null)
                {
                    targetSlotIndex = board.UpdateSlotHighlight(currentDraggedPiece.transform.position);
                }
            }
        }

        private void HandlePinchEnd(Vector3 position, Quaternion rotation)
        {
            if (rayVisualizer != null)
            {
                rayVisualizer.enabled = false;
            }

            // Guard against processing the same pinch end twice
            if (currentDraggedPiece == null)
            {
                targetSlotIndex = -1;
                return;
            }
            
            // Store references before clearing
            var piece = currentDraggedPiece;
            var slotIndex = targetSlotIndex;
            var wasFromBoard = pieceWasFromBoard;
            
            // Clear IMMEDIATELY to prevent double processing
            piece.SetDragged(false);
            currentDraggedPiece = null;
            targetSlotIndex = -1;
            pieceWasFromBoard = false;
            
            // Try to snap the piece to the board using the slot we stored from the highlight
            var board = FindFirstObjectByType<PuzzleBoard>();
            if (board != null)
            {
                bool snapped = false;
                
                if (slotIndex >= 0)
                {
                    snapped = board.SnapPieceToSlot(piece, slotIndex);
                }
                
                if (!snapped)
                {
                    // Failed to snap - return based on origin
                    if (wasFromBoard)
                    {
                        board.ReturnPieceToTray(piece);
                    }
                    else
                    {
                        piece.ReturnToPool(piece.GetPoolPosition());
                    }
                }
                
                // Clear highlight after snap attempt
                board.ClearSlotHighlight();
            }
        }

        private bool TryClickUI(Vector3 origin, Vector3 direction)
        {
            Ray ray = new Ray(origin, direction);
            float closestDist = rayLength;
            Selectable hitSelectable = null;

            // Find all active selectables (Buttons, Toggles, etc.)
            Selectable[] selectables = FindObjectsByType<Selectable>(FindObjectsSortMode.None);
            foreach (var sel in selectables)
            {
                if (sel.interactable && sel.gameObject.activeInHierarchy)
                {
                    RectTransform rect = sel.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        // Raycast against the front plane of the RectTransform
                        Plane plane = new Plane(-rect.forward, rect.position);
                        if (!plane.Raycast(ray, out float enter))
                        {
                            // Try the back plane just in case
                            plane = new Plane(rect.forward, rect.position);
                            plane.Raycast(ray, out enter);
                        }

                        if (enter > 0 && enter < closestDist)
                        {
                            Vector3 hitPoint = ray.GetPoint(enter);
                            Vector3 localHit = rect.InverseTransformPoint(hitPoint);

                            if (rect.rect.Contains(localHit))
                            {
                                closestDist = enter;
                                hitSelectable = sel;
                            }
                        }
                    }
                }
            }

            if (hitSelectable != null)
            {
                PointerEventData pointerData = new PointerEventData(EventSystem.current);
                ExecuteEvents.Execute(hitSelectable.gameObject, pointerData, ExecuteEvents.pointerClickHandler);
                ExecuteEvents.Execute(hitSelectable.gameObject, pointerData, ExecuteEvents.submitHandler);
                
                Button btn = hitSelectable as Button;
                if (btn != null && btn.onClick != null)
                {
                    btn.onClick.Invoke();
                }

                return true;
            }

            return false;
        }
    }
}
