using System;

namespace ArtUnbound.Data
{
    /// <summary>
    /// Represents a piece that has been placed on the puzzle board.
    /// Used for saving/restoring puzzle progress.
    /// </summary>
    [Serializable]
    public class PlacedPieceData
    {
        public int pieceId;      // Unique ID of the piece
        public int slotIndex;    // Index of the slot where the piece is placed

        public PlacedPieceData()
        {
            pieceId = -1;
            slotIndex = -1;
        }

        public PlacedPieceData(int pieceId, int slotIndex)
        {
            this.pieceId = pieceId;
            this.slotIndex = slotIndex;
        }
    }
}
