using UnityEngine;
using UnityEngine.UI;
using ArtUnbound.Gameplay;

namespace ArtUnbound.UI
{
    /// <summary>
    /// A UI element (RawImage) that represents a puzzle piece inside the PieceTrayPanel ScrollRect.
    /// Lives as a child of the ScrollRect Content transform (managed by GridLayoutGroup).
    ///
    /// The BoxCollider is on the same interactable layer as the 3-D pieces so
    /// InteractionManager's OverlapSphere can find it.
    /// LinkedPiece provides the reference back to the 3D piece.
    /// </summary>
    public class PieceThumbnailItem : MonoBehaviour
    {
        /// <summary>The PuzzlePiece that this thumbnail represents.</summary>
        public PuzzlePiece LinkedPiece { get; private set; }

        /// <summary>
        /// Sets up the RawImage UV crop, BoxCollider size, and layer.
        /// Must be called immediately after instantiating the prefab.
        /// The prefab is expected to already have a RawImage and BoxCollider.
        /// </summary>
        /// <param name="owner">The PuzzlePiece this thumbnail represents.</param>
        /// <param name="artworkTexture">Full artwork texture (Read/Write enabled).</param>
        /// <param name="col">Grid column of the piece (0-based, left→right).</param>
        /// <param name="row">Grid row of the piece (0-based, top→bottom).</param>
        /// <param name="cols">Total puzzle grid columns.</param>
        /// <param name="rows">Total puzzle grid rows.</param>
        /// <param name="worldSizeM">
        /// World-space cell size in metres — used to size the BoxCollider.
        /// Must match canvas scale × GridLayoutGroup cell size in pixels
        /// (e.g. canvas scale 0.001 × cell 75 px → 0.075 m).
        /// </param>
        public void Initialize(PuzzlePiece owner,
                               Texture2D artworkTexture,
                               int col, int row, int cols, int rows,
                               float worldSizeM)
        {
            LinkedPiece = owner;

            // ── Layer: match the piece's physics collider so OverlapSphere finds us ──
            var existingCollider = owner.GetComponentInChildren<Collider>(true);
            gameObject.layer = existingCollider != null
                ? existingCollider.gameObject.layer
                : owner.gameObject.layer;

            // ── RawImage: crop the artwork to this piece's UV region ─────────────
            //
            // The board is rotated 180° in Y, so the horizontal column is mirrored:
            //   texCol = cols - 1 - col
            // Unity textures have V=0 at the bottom; puzzle row 0 is visually at the top,
            // so we flip vertically:
            //   vOffset = 1 - (row + 1) / rows
            var rawImage = GetComponent<RawImage>();
            if (rawImage == null)
            {
                Debug.LogWarning("[PieceThumbnailItem] No RawImage found on prefab. Add one.", this);
            }
            else if (artworkTexture != null)
            {
                int   texCol  = cols - 1 - col;
                float uScale  = 1f / cols;
                float vScale  = 1f / rows;
                float uOffset = (float)texCol / cols;
                float vOffset = 1f - (float)(row + 1) / rows;

                rawImage.texture = artworkTexture;
                rawImage.uvRect  = new Rect(uOffset, vOffset, uScale, vScale);
            }

            // ── BoxCollider for sphere-overlap / raycast detection ───────────────
            var col3D = GetComponent<BoxCollider>();
            if (col3D == null)
            {
                Debug.LogWarning("[PieceThumbnailItem] No BoxCollider found on prefab. Add one.", this);
            }
            else
            {
                col3D.size   = new Vector3(worldSizeM * 0.9f, worldSizeM * 0.9f, 0.015f);
                col3D.center = Vector3.zero;
            }
        }
    }
}
