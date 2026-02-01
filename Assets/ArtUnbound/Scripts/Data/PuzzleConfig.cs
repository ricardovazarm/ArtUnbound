using UnityEngine;

namespace ArtUnbound.Data
{
    [CreateAssetMenu(menuName = "ArtUnbound/Puzzle Config", fileName = "PuzzleConfig")]
    public class PuzzleConfig : ScriptableObject
    {
        public int[] pieceCounts = { 64, 144, 256, 512 };
        public float snapDistanceCm = 3.0f;
        public float pinchRangeCm = 1.0f;
        public float pieceSizeCm = 5.0f;
        public float pieceThicknessCm = 0.5f;
        public bool helpModeDefault = true;
        public bool useGridSnapping = true;
        public bool useTriangularMorphology = true;
    }
}
