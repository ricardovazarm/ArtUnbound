using ArtUnbound.Data;
using ArtUnbound.Gameplay;
using UnityEngine;

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
        [SerializeField] private LineRenderer rayVisualizer;

        private PuzzlePiece currentDraggedPiece;
        private float currentDragDistance;
        private Vector3 dragOffset;

        private void Start()
        {
            if (inputController != null)
            {
                inputController.OnPinchStart += HandlePinchStart;
                inputController.OnPinchHold += HandlePinchHold;
                inputController.OnPinchEnd += HandlePinchEnd;
            }
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
            Ray ray = new Ray(position, rotation * Vector3.forward);
            
            if (rayVisualizer != null)
            {
                rayVisualizer.enabled = true;
                rayVisualizer.SetPosition(0, position);
                rayVisualizer.SetPosition(1, position + ray.direction * rayLength);
            }

            if (Physics.Raycast(ray, out RaycastHit hit, rayLength, interactableLayer))
            {
                PuzzlePiece piece = hit.collider.GetComponentInParent<PuzzlePiece>();
                if (piece != null)
                {
                    currentDraggedPiece = piece;
                    currentDragDistance = hit.distance;
                    dragOffset = piece.transform.position - hit.point;
                    
                    piece.SetDragged(true);
                }
            }
        }

        private void HandlePinchHold(Vector3 position, Quaternion rotation)
        {
            Ray ray = new Ray(position, rotation * Vector3.forward);

            if (rayVisualizer != null)
            {
                rayVisualizer.SetPosition(0, position);
                rayVisualizer.SetPosition(1, position + ray.direction * rayLength);
            }

            if (currentDraggedPiece != null)
            {
                // Move piece
                Vector3 targetPoint = ray.GetPoint(currentDragDistance) + dragOffset;
                
                // Simple smoothing
                currentDraggedPiece.transform.position = Vector3.Lerp(currentDraggedPiece.transform.position, targetPoint, Time.deltaTime * 20f);
                
                // Align rotation to user (optional) or keep board alignment
                // For a 2D puzzle on a wall, we usually keep rotation locked to the board's plane or local rotation
            }
        }

        private void HandlePinchEnd(Vector3 position, Quaternion rotation)
        {
            if (rayVisualizer != null)
            {
                rayVisualizer.enabled = false;
            }

            if (currentDraggedPiece != null)
            {
                // Try validation logic inside PuzzlePiece or Board
                bool snapped = false; 

                // We can check with PuzzleBoard here if we had a reference, 
                // but PuzzlePiece calls Release(), which should fire an event the Board listens to.
                // However, the Board listens to OnRelease?
                // Actually, PuzzleBoard only has TrySnapPiece(piece). 
                // We need to call that.
                
                // Find board? Or assume Singleton? 
                // Better: GameBootstrap.Instance.PuzzleBoard.TrySnapPiece(currentDraggedPiece);
                
                var board = FindFirstObjectByType<PuzzleBoard>(); // Creating a direct dependency or cache it
                if (board != null)
                {
                    snapped = board.TrySnapPiece(currentDraggedPiece);
                }

                currentDraggedPiece.SetDragged(false); // Validate/Snap or Return managed internally
                
                if (!snapped && currentDraggedPiece.CurrentState != PieceState.Placed)
                {
                    // Maybe return to scroll? logic handled by piece release?
                }

                currentDraggedPiece = null;
            }
        }
    }
}
