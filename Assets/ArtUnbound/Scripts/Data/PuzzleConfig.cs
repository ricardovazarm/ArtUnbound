using UnityEngine;

namespace ArtUnbound.Data
{
    [CreateAssetMenu(menuName = "ArtUnbound/Puzzle Config", fileName = "PuzzleConfig")]
    public class PuzzleConfig : ScriptableObject
    {
        [Header("Board Size Constraints (in meters)")]
        [Tooltip("Side of the square bounding box that the board fits into. Landscape paintings get this as max width; portrait paintings get this as max height. Default: 0.6m = 60cm.")]
        public float boardMaxSizeM = 0.6f;

        [Header("Piece Size Constraints (in meters)")]
        [Tooltip("Easy target piece size — also the absolute upper bound. No piece is bigger than this. Default: 0.05m = 5cm.")]
        public float maxPieceSizeM = 0.05f;
        [Tooltip("Hard target piece size — also the absolute lower bound. No piece is smaller than this. Default: 0.03m = 3cm. Normal difficulty derives as (max+min)/2.")]
        public float minPieceSizeM = 0.03f;

        /// <summary>
        /// Returns the target piece size in meters for the given difficulty index.
        /// 0=Easy (max), 1=Normal (midpoint), 2=Hard (min).
        /// </summary>
        public float GetTargetPieceSize(int difficultyIndex)
        {
            return difficultyIndex switch
            {
                0 => maxPieceSizeM,
                1 => (maxPieceSizeM + minPieceSizeM) * 0.5f,
                2 => minPieceSizeM,
                _ => maxPieceSizeM
            };
        }

        [Header("Snapping & Interaction")]
        [Tooltip("Max distance (cm) from piece to slot center to allow snap. Higher = more forgiving.")]
        public float snapDistanceCm = 3.0f;
        [Tooltip("Max perpendicular distance (cm) from piece to board plane. Higher = allows piece to pass through board more.")]
        public float maxDepthToPlaneCm = 5.0f;
        public float pinchRangeCm = 1.0f;
        public float pieceThicknessCm = 0.5f;

        [Header("Gameplay Settings")]
        public bool helpModeDefault = true;
        public bool useGridSnapping = true;
        public bool useTriangularMorphology = true;

        [Header("Piece Tray 3D")]
        [Tooltip("Width of the tray viewport in cm (horizontal, controls number of columns)")]
        public float trayViewportWidthCm = 30f;
        [Tooltip("Height of the tray viewport in cm (vertical, controls visible rows)")]
        public float trayViewportHeightCm = 50f;
        [Tooltip("Y rotation of the tray relative to the board (degrees). Negative = faces user's right.")]
        public float trayRotationY = -35f;
        [Tooltip("Horizontal offset from board center to tray center (m). Applied as negative X in inverted-canvas space.")]
        public float trayOffsetX = 0.45f;
        [Tooltip("Vertical offset from board center to tray center (m). Positive = up.")]
        public float trayOffsetY = 0f;
        [Tooltip("Depth offset from board center to tray center (m). Positive = toward user.")]
        public float trayOffsetZ = 0f;
        [Tooltip("Scroll speed in m/s per thumbstick unit")]
        public float trayScrollSpeed = 0.3f;
        [Tooltip("Lerp smoothing factor for scroll animation")]
        public float trayScrollSmoothing = 10f;
        [Tooltip("Cell size = pieceSize * trayCellMargin")]
        public float trayCellMargin = 1.2f;
    }
}
