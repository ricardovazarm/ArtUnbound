using UnityEngine;

namespace ArtUnbound.Data
{
    [CreateAssetMenu(menuName = "ArtUnbound/Puzzle Config", fileName = "PuzzleConfig")]
    public class PuzzleConfig : ScriptableObject
    {
        [Header("Piece Counts")]
        [Tooltip("Target piece counts: 8², 11², 14², 17² = 64, 121, 196, 289")]
        public int[] pieceCounts = { 64, 121, 196, 289 };
        public int defaultPieceCount = 121; // Normal difficulty (approx 11×11)

        [Header("Board Size Constraints (in meters)")]
        [Tooltip("Fixed board width in meters (default: 0.5m = 50cm)")]
        public float boardWidthM = 0.5f;
        
        [Tooltip("Maximum board height in meters (default: 0.9m = 90cm)")]
        public float boardMaxHeightM = 0.9f;
        
        [Tooltip("Minimum piece size in meters (default: 0.03m = 3cm)")]
        public float minPieceSizeM = 0.03f;

        [Header("Snapping & Interaction")]
        [Tooltip("Max distance (cm) from piece to slot center to allow snap. Higher = more forgiving.")]
        public float snapDistanceCm = 3.0f;
        [Tooltip("Max perpendicular distance (cm) from piece to board plane. Higher = allows piece slightly behind board.")]
        public float maxDepthToPlaneCm = 4.0f;
        public float pinchRangeCm = 1.0f;
        public float pieceThicknessCm = 0.5f;

        [Header("Gameplay Settings")]
        public bool helpModeDefault = true;
        public bool useGridSnapping = true;
        public bool useTriangularMorphology = true;

        [Header("Deprecated - Do Not Use")]
        [Tooltip("DEPRECATED: Piece size is now calculated dynamically based on board width")]
        public float pieceSizeCm = 5.0f; // Kept for backward compatibility, but not used
    }
}
