using System;
using System.Collections;
using ArtUnbound.Data;
using UnityEngine;

namespace ArtUnbound.Gameplay
{
    /// <summary>
    /// Represents a single puzzle piece that can be grabbed, moved, and placed.
    /// </summary>
    public class PuzzlePiece : MonoBehaviour
    {
        public event Action<PuzzlePiece> OnReleased;
        public event Action<PuzzlePiece, PieceState> OnStateChanged;

        [Header("Configuration")]
        [SerializeField] private int pieceId;
        [SerializeField] private int correctSlotIndex;
        [SerializeField] private Transform grabAnchor;

        [Header("Visual Feedback")]
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private GameObject highlightObject;

        [Header("Animation")]
        [SerializeField] private float returnAnimationDuration = 0.5f;

        public int PieceId => pieceId;
        public int CorrectSlotIndex => correctSlotIndex;
        public Transform GrabAnchor => grabAnchor;
        public PieceMorphology Morphology => morphology;
        public PieceState CurrentState => currentState;
        public int CurrentSlotIndex { get; private set; } = -1;

        [SerializeField] private PieceMorphology morphology;
        private PieceState currentState = PieceState.InPool;
        private Vector3 poolPosition;
        private Coroutine returnCoroutine;

        private void Awake()
        {
            if (grabAnchor == null)
                grabAnchor = transform;

            if (meshRenderer == null)
                meshRenderer = GetComponentInChildren<MeshRenderer>();

            poolPosition = transform.position;
        }

        /// <summary>
        /// Sets the piece state and fires the state changed event.
        /// </summary>
        public void SetState(PieceState newState)
        {
            if (currentState == newState) return;

            PieceState oldState = currentState;
            currentState = newState;

            OnStateChanged?.Invoke(this, newState);
        }

        /// <summary>
        /// Called when the piece is grabbed by the user.
        /// </summary>
        public void SetDragged(bool isDragged)
        {
            if (isDragged)
            {
                if (returnCoroutine != null)
                {
                    StopCoroutine(returnCoroutine);
                    returnCoroutine = null;
                }

                SetState(PieceState.Grabbed);
            }
            else if (currentState == PieceState.Grabbed)
            {
                Release();
            }
        }

        /// <summary>
        /// Releases the piece, triggering validation.
        /// </summary>
        public void Release()
        {
            OnReleased?.Invoke(this);
        }

        /// <summary>
        /// Snaps the piece to a position on the board.
        /// </summary>
        public void SetSnapped(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            SetState(PieceState.Placed);
        }

        /// <summary>
        /// Returns the piece to the carousel/pool.
        /// </summary>
        public void ReturnToPool(Vector3 targetPosition)
        {
            SetState(PieceState.Returning);
            CurrentSlotIndex = -1;

            if (returnCoroutine != null)
                StopCoroutine(returnCoroutine);

            returnCoroutine = StartCoroutine(AnimateReturnToPool(targetPosition));
        }

        private IEnumerator AnimateReturnToPool(Vector3 targetPosition)
        {
            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            Quaternion targetRot = Quaternion.identity;
            float elapsed = 0f;

            while (elapsed < returnAnimationDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / returnAnimationDuration);

                transform.position = Vector3.Lerp(startPos, targetPosition, t);
                transform.rotation = Quaternion.Slerp(startRot, targetRot, t);

                yield return null;
            }

            transform.position = targetPosition;
            transform.rotation = targetRot;
            poolPosition = targetPosition;

            SetState(PieceState.InPool);
            returnCoroutine = null;
        }

        /// <summary>
        /// Applies a morphology to this piece.
        /// </summary>
        public void ApplyMorphology(PieceMorphology value)
        {
            morphology = value;
        }

        /// <summary>
        /// Sets the slot index where this piece is currently placed.
        /// </summary>
        public void SetSlotIndex(int slotIndex)
        {
            CurrentSlotIndex = slotIndex;
        }

        /// <summary>
        /// Initializes the piece with its ID and correct slot.
        /// </summary>
        public void Initialize(int id, int correctSlot, PieceMorphology morph)
        {
            pieceId = id;
            correctSlotIndex = correctSlot;
            morphology = morph;
            currentState = PieceState.InPool;
            CurrentSlotIndex = -1;
        }

        /// <summary>
        /// Checks if this piece is in its correct slot.
        /// </summary>
        public bool IsInCorrectSlot()
        {
            return CurrentSlotIndex == correctSlotIndex;
        }

        /// <summary>
        /// Checks if this piece is in a specific slot.
        /// </summary>
        public bool IsInSlot(int slotIndex)
        {
            return CurrentSlotIndex == slotIndex;
        }

        /// <summary>
        /// Sets the highlight state of the piece.
        /// </summary>
        public void SetHighlight(bool enabled, Color? color = null)
        {
            if (highlightObject != null)
            {
                highlightObject.SetActive(enabled);

                if (enabled && color.HasValue)
                {
                    var highlightRenderer = highlightObject.GetComponent<Renderer>();
                    if (highlightRenderer != null)
                    {
                        highlightRenderer.material.color = color.Value;
                    }
                }
            }
        }

        /// <summary>
        /// Stores the current position as the pool position.
        /// </summary>
        public void SetPoolPosition(Vector3 position)
        {
            poolPosition = position;
        }

        /// <summary>
        /// Gets the stored pool position.
        /// </summary>
        public Vector3 GetPoolPosition()
        {
            return poolPosition;
        }
    }
}
