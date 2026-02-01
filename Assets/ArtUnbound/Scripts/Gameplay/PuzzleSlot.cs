using UnityEngine;
using ArtUnbound.Data;

namespace ArtUnbound.Gameplay
{
    public struct PuzzleSlot
    {
        public int pieceId;
        public int row;
        public int col;
        public Vector3 position;
        public Quaternion rotation;
        public PieceMorphology morphology;
    }
}
