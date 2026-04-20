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

        [Header("Debug — UV Region")]
        [Tooltip("UV rect in texture space used to generate this piece's 3D mesh (before board-rotation correction). Compare with the thumbnail's uvRect to verify they match.")]
        [SerializeField] private Rect _meshUvRegion;
        public Rect MeshUvRegion => _meshUvRegion;
        public void SetMeshUvRegion(Rect region) { _meshUvRegion = region; }

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

        // Global guard: only one piece can be in Grabbed state at a time.
        private static PuzzlePiece _globalGrabbedPiece = null;

        /// <summary>The piece currently grabbed across all InteractionManager instances, or null.</summary>
        public static PuzzlePiece GlobalGrabbedPiece => _globalGrabbedPiece;

        /// <summary>
        /// Force-clears the global grab lock and resets the piece to InPool (thumbnail visible).
        /// Call this when the lock is detected as stuck (no InteractionManager is tracking it).
        /// </summary>
        public static void ForceReleaseGlobalLock()
        {
            if (_globalGrabbedPiece == null) return;
            var stuck = _globalGrabbedPiece;
            _globalGrabbedPiece = null;
            stuck.currentState = PieceState.InPool;
            stuck.ShowThumbnailMode();
            Debug.LogWarning($"[PuzzlePiece] Force-released stuck global lock on piece {stuck.pieceId}");
        }

        // 2D thumbnail overlay (shown in tray; hidden when grabbed / placed on board)
        private ArtUnbound.UI.PieceThumbnailItem _thumbnailItem;
        private MeshCollider _meshCollider;

        private void Awake()
        {
            if (grabAnchor == null)
                grabAnchor = transform;

            if (meshRenderer == null)
                meshRenderer = GetComponentInChildren<MeshRenderer>(true);

            // Cache collider before the thumbnail child is created by PuzzleBoard
            _meshCollider = GetComponentInChildren<MeshCollider>(true);

            poolPosition = transform.position;
        }

        // ── Thumbnail helpers ────────────────────────────────────────────────────

        /// <summary>
        /// Called by PieceTrayGridController after creating the thumbnail in the panel.
        /// Stores the reference and immediately switches to thumbnail mode.
        /// </summary>
        public void SetThumbnailItem(ArtUnbound.UI.PieceThumbnailItem item)
        {
            _thumbnailItem = item;
            ShowThumbnailMode();   // start life in the tray as a flat 2D image
        }

        /// <summary>
        /// Enables the 3-D mesh and collider; dims the thumbnail to show the piece is grabbed.
        /// The thumbnail stays in the grid so the slot is still visible while the piece is held.
        /// </summary>
        public void ShowPieceMode()
        {
            if (meshRenderer  == null) meshRenderer  = GetComponentInChildren<MeshRenderer>(true);
            if (_meshCollider == null) _meshCollider = GetComponentInChildren<MeshCollider>(true);

            if (meshRenderer  != null) meshRenderer.enabled  = true;
            if (_meshCollider != null) _meshCollider.enabled = true;
            if (_thumbnailItem != null) _thumbnailItem.SetGhost(true);
        }

        /// <summary>
        /// Hides the 3-D mesh and collider; restores the thumbnail to full opacity.
        /// </summary>
        public void ShowThumbnailMode()
        {
            if (meshRenderer  == null) meshRenderer  = GetComponentInChildren<MeshRenderer>(true);
            if (_meshCollider == null) _meshCollider = GetComponentInChildren<MeshCollider>(true);

            if (meshRenderer  != null) meshRenderer.enabled  = false;
            if (_meshCollider != null) _meshCollider.enabled = false;
            if (_thumbnailItem != null) _thumbnailItem.SetGhost(false);
        }

        /// <summary>
        /// Sets the piece state and fires the state changed event.
        /// Also switches between 2D thumbnail mode (tray) and 3D mesh mode (board/grabbed).
        /// </summary>
        public void SetState(PieceState newState)
        {
            if (currentState == newState) return;

            // Block if another piece is already grabbed.
            if (newState == PieceState.Grabbed && _globalGrabbedPiece != null && _globalGrabbedPiece != this)
            {
                Debug.LogWarning($"[PuzzlePiece] Blocked grab on piece {pieceId} — piece {_globalGrabbedPiece.pieceId} is already grabbed.");
                return;
            }

            // Release global lock when leaving Grabbed.
            if (currentState == PieceState.Grabbed && _globalGrabbedPiece == this)
                _globalGrabbedPiece = null;

            // Acquire global lock when entering Grabbed.
            if (newState == PieceState.Grabbed)
                _globalGrabbedPiece = this;

            currentState = newState;

            // Switch visual mode based on state
            switch (newState)
            {
                case PieceState.InPool:
                case PieceState.Returning:
                case PieceState.Grabbed:
                case PieceState.Placed:
                    ShowPieceMode();   // 3D mesh always visible
                    break;
            }

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
        /// Plays a short left-right wiggle to signal the piece is in the wrong position.
        /// </summary>
        public void PlayWiggleEffect()
        {
            if (currentState == PieceState.Grabbed) return; // don't wiggle while held
            StartCoroutine(WiggleCoroutine());
        }

        private IEnumerator WiggleCoroutine()
        {
            Quaternion originalRotation = transform.rotation;
            float duration = 0.45f;
            float elapsed = 0f;
            float frequency = 28f;   // oscillations per second
            float amplitude = 4f;    // degrees

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float decay = 1f - (elapsed / duration);
                float angle = Mathf.Sin(elapsed * frequency) * amplitude * decay;
                transform.rotation = originalRotation * Quaternion.AngleAxis(angle, transform.up);
                yield return null;
            }

            transform.rotation = originalRotation;
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
            // Target Rotation: Match Parent (Tray) Rotation to stay aligned/vertical with board
            Quaternion targetRot = transform.parent != null ? transform.parent.rotation : Quaternion.identity;
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
        /// Highlights this piece as "selected" for controller point-to-place interaction.
        /// Uses highlightObject if available, otherwise applies emission to the mesh renderer.
        /// </summary>
        public void SetSelected(bool selected)
        {
            if (highlightObject != null)
            {
                SetHighlight(selected, Color.yellow);
                return;
            }

            // Fallback: emission on mesh renderer
            var rend = meshRenderer != null ? meshRenderer : GetComponentInChildren<MeshRenderer>();
            if (rend == null) return;

            if (selected)
            {
                rend.material.EnableKeyword("_EMISSION");
                rend.material.SetColor("_EmissionColor", Color.yellow * 0.4f);
            }
            else
            {
                rend.material.DisableKeyword("_EMISSION");
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
