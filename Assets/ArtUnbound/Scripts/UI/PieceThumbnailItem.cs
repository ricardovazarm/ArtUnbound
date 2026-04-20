using ArtUnbound.Data;
using ArtUnbound.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ArtUnbound.UI
{
    /// <summary>
    /// UI element (Button + RawImage) that represents a puzzle piece inside the
    /// PieceTrayPanel ScrollRect.
    ///
    /// Preferred prefab structure (no Read/Write required on the artwork texture):
    ///   PieceThumbnailButton (root)
    ///     ├─ Button + Image (background frame, TargetGraphic for color transitions)
    ///     ├─ PieceThumbnailItem (this script)
    ///     └─ ArtworkImage (child)
    ///          └─ RawImage  ← shows the artwork crop via uvRect
    ///
    /// At runtime, Initialize() injects a ShapeMask child between the root and ArtworkImage
    /// so the thumbnail is clipped to the actual puzzle piece silhouette.
    ///
    /// Interaction model:
    ///   Near pinch / remote pinch / controller → OVRRaycaster → OnPointerDown → BeginExternalDrag
    /// </summary>
    public class PieceThumbnailItem : MonoBehaviour,
        IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private RawImage artworkImage;

        [Header("Debug")]
        [SerializeField] private PuzzlePiece _linkedPiece;
        [SerializeField] private Rect _debugUvRect;

        /// <summary>The PuzzlePiece this thumbnail represents.</summary>
        public PuzzlePiece LinkedPiece => _linkedPiece;

        private ArtUnbound.Input.InteractionManager _interactionManager;
        private ScrollRect _parentScrollRect;
        private float _lastGrabTime = -10f;
        private const float GrabCooldownSec = 0.4f;
        private int _linkedPieceId = -1;

        // Sprite cache keyed by morphology index (0-80). Static: shared across all thumbnails.
        private static readonly Sprite[] _shapeCache = new Sprite[81];
        private const int MaskTexSize = 128;

        // Geometry constants — must match PieceMeshGenerator.
        private const float Half    = 0.5f;
        private const float TabH    = 0.15f;  // tab height as fraction of piece size
        private const float TabHW   = 0.20f;  // tab half-width = TAB_WIDTH(0.4) * 0.5

        /// <summary>
        /// Sets the artwork region for this thumbnail and applies the piece silhouette mask.
        /// </summary>
        public void Initialize(PuzzlePiece owner,
                               Texture2D artworkTexture,
                               int col, int row, int cols, int rows,
                               float worldSizeM)
        {
            _linkedPiece   = owner;
            _linkedPieceId = owner.PieceId;

            if (artworkTexture == null) return;

            if (artworkImage == null)
            {
                Debug.LogWarning("[PieceThumbnailItem] artworkImage not assigned in Inspector.", this);
                return;
            }

            artworkImage.texture = artworkTexture;

            // Use the core UV region stored on the piece (same region used to generate the 3D mesh).
            // Negative width compensates for the canvas being a child of the board (rotated 180° Y),
            // which would otherwise mirror the texture horizontally.
            var r = owner.MeshUvRegion;
            var rect = new Rect(r.xMax, r.y, -r.width, r.height);
            artworkImage.uvRect = rect;
            _debugUvRect = rect;

            // Ensure the thumbnail starts fully visible regardless of the piece's
            // previous state (e.g. if the board was re-initialized mid-session).
            SetGhost(false);

            // Swap left↔right edges for the mask: the canvas 180° Y rotation mirrors the
            // silhouette horizontally, so we pre-flip to cancel that out.
            var m = owner.Morphology;
            ApplyPieceShapeMask(new PieceMorphology(m.top, m.left, m.bottom, m.right));
        }

        // ── Shape mask ───────────────────────────────────────────────────────────

        private void ApplyPieceShapeMask(PieceMorphology morphology)
        {
            int idx = PieceMeshGenerator.GetMorphologyIndex(morphology);
            if (_shapeCache[idx] == null)
                _shapeCache[idx] = GeneratePieceShapeSprite(morphology);

            // Find or create the ShapeMask child that clips ArtworkImage.
            Transform maskTf = transform.Find("ShapeMask");
            if (maskTf == null)
            {
                var maskGO = new GameObject("ShapeMask");
                maskGO.layer = gameObject.layer;
                maskGO.transform.SetParent(transform, false);

                var maskRT = maskGO.AddComponent<RectTransform>();
                maskRT.anchorMin  = Vector2.zero;
                maskRT.anchorMax  = Vector2.one;
                maskRT.offsetMin  = Vector2.zero;
                maskRT.offsetMax  = Vector2.zero;

                maskGO.AddComponent<CanvasRenderer>();

                var maskImg = maskGO.AddComponent<Image>();
                maskImg.sprite        = _shapeCache[idx];
                maskImg.raycastTarget = false;

                var mask = maskGO.AddComponent<Mask>();
                mask.showMaskGraphic = false;

                // Re-parent ArtworkImage so it is clipped by the mask.
                artworkImage.transform.SetParent(maskGO.transform, false);
                var artRT = artworkImage.GetComponent<RectTransform>();
                artRT.anchorMin  = Vector2.zero;
                artRT.anchorMax  = Vector2.one;
                artRT.offsetMin  = Vector2.zero;
                artRT.offsetMax  = Vector2.zero;
            }
            else
            {
                maskTf.GetComponent<Image>().sprite = _shapeCache[idx];
            }
        }

        // ── Sprite / texture generation ──────────────────────────────────────────

        /// <summary>
        /// Rasterises the piece silhouette into a Texture2D and wraps it in a Sprite.
        /// The square core occupies the central 1/(1+2*TAB_HEIGHT) of the texture;
        /// tabs fit in the outer bands so the sprite fits exactly within the cell rect.
        /// </summary>
        private static Sprite GeneratePieceShapeSprite(PieceMorphology morphology)
        {
            int size = MaskTexSize;
            var pixels = new Color32[size * size];

            // In normalised coords the piece spans [-Half, Half] and tabs can
            // extend up to ±(Half + TabH) = ±0.65.  Map ±maxExtent → [0, size].
            const float maxExtent = Half + TabH; // 0.65

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = ((x + 0.5f) / size * 2f - 1f) * maxExtent;
                    float ny = ((y + 0.5f) / size * 2f - 1f) * maxExtent;

                    pixels[y * size + x] = IsInsidePiece(nx, ny, morphology)
                        ? new Color32(255, 255, 255, 255)
                        : new Color32(0,   0,   0,   0);
                }
            }

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixels32(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        /// <summary>
        /// Returns true if (px, py) is inside the piece silhouette.
        /// Coordinates are normalised: square = [-0.5, 0.5], tabs extend to ±0.65.
        /// </summary>
        private static bool IsInsidePiece(float px, float py, PieceMorphology m)
        {
            // 1. Negative edges — cut triangles OUT of the square first.
            if (m.bottom == PieceEdgeState.Negative &&
                PointInTriangle(px, py,  -TabHW, -Half,  TabHW, -Half,  0, -Half + TabH)) return false;
            if (m.top    == PieceEdgeState.Negative &&
                PointInTriangle(px, py,  -TabHW,  Half,  TabHW,  Half,  0,  Half - TabH)) return false;
            if (m.right  == PieceEdgeState.Negative &&
                PointInTriangle(px, py,   Half, -TabHW,  Half,  TabHW,  Half - TabH,  0)) return false;
            if (m.left   == PieceEdgeState.Negative &&
                PointInTriangle(px, py,  -Half, -TabHW, -Half,  TabHW, -Half + TabH,  0)) return false;

            // 2. Main square.
            if (px >= -Half && px <= Half && py >= -Half && py <= Half) return true;

            // 3. Positive edges — add triangles BEYOND the square.
            if (m.bottom == PieceEdgeState.Positive &&
                PointInTriangle(px, py,  -TabHW, -Half,  TabHW, -Half,  0, -Half - TabH)) return true;
            if (m.top    == PieceEdgeState.Positive &&
                PointInTriangle(px, py,  -TabHW,  Half,  TabHW,  Half,  0,  Half + TabH)) return true;
            if (m.right  == PieceEdgeState.Positive &&
                PointInTriangle(px, py,   Half, -TabHW,  Half,  TabHW,  Half + TabH,  0)) return true;
            if (m.left   == PieceEdgeState.Positive &&
                PointInTriangle(px, py,  -Half, -TabHW, -Half,  TabHW, -Half - TabH,  0)) return true;

            return false;
        }

        private static bool PointInTriangle(float px, float py,
                                            float ax, float ay,
                                            float bx, float by,
                                            float cx, float cy)
        {
            float d1 = Cross(px, py, ax, ay, bx, by);
            float d2 = Cross(px, py, bx, by, cx, cy);
            float d3 = Cross(px, py, cx, cy, ax, ay);
            return !((d1 < 0 || d2 < 0 || d3 < 0) && (d1 > 0 || d2 > 0 || d3 > 0));
        }

        private static float Cross(float px, float py, float ax, float ay, float bx, float by)
            => (px - bx) * (ay - by) - (ax - bx) * (py - by);

        // ── Lifecycle ────────────────────────────────────────────────────────────

        private void Awake()
        {
            // Cache the ScrollRect so drag events can be forwarded to it for scrolling.
            _parentScrollRect = GetComponentInParent<ScrollRect>();
        }

        // ── Ghost state ──────────────────────────────────────────────────────────

        /// <summary>
        /// Dims the thumbnail while the piece is held in hand (ghost=true),
        /// or restores full opacity when the piece is returned to the tray (ghost=false).
        /// The thumbnail always stays in the grid — it is only permanently removed
        /// by PieceTrayGridController.RemoveThumbnail() when correctly snapped.
        /// </summary>
        public void SetGhost(bool ghost)
        {
            if (artworkImage == null) return;
            var c = artworkImage.color;
            c.a = ghost ? 0.25f : 1f;
            artworkImage.color = c;
        }

        // ── Drag forwarding → ScrollRect ─────────────────────────────────────────
        // Without these, the Button component on the thumbnail root consumes pointer
        // events and the parent ScrollRect never receives drag input for scrolling.

        public void OnBeginDrag(PointerEventData eventData) =>
            _parentScrollRect?.OnBeginDrag(eventData);

        public void OnDrag(PointerEventData eventData) =>
            _parentScrollRect?.OnDrag(eventData);

        public void OnEndDrag(PointerEventData eventData) =>
            _parentScrollRect?.OnEndDrag(eventData);

        // ── IPointerDownHandler ──────────────────────────────────────────────────

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_linkedPiece == null || _linkedPieceId < 0) return;
            if (_linkedPiece.CurrentState == PieceState.Grabbed) return;
            if (_linkedPiece.CurrentState == PieceState.Placed)  return;

            // Cooldown prevents OVR firing both near-pinch and remote-pinch for the same gesture.
            if (Time.time - _lastGrabTime < GrabCooldownSec) return;

            if (_interactionManager == null)
                _interactionManager =
                    FindFirstObjectByType<ArtUnbound.Input.InteractionManager>();

            // Don't start a second drag if one is already active.
            if (_interactionManager == null || _interactionManager.IsDragging) return;

            _lastGrabTime = Time.time;
            _interactionManager.BeginExternalDrag(_linkedPiece);

            // Self-ghost: confirm this exact thumbnail's piece is now being dragged.
            if (_interactionManager.CurrentDraggedPiece != null &&
                _interactionManager.CurrentDraggedPiece.PieceId == _linkedPieceId)
            {
                SetGhost(true);
            }
        }
    }
}
