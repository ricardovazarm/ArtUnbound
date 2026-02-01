namespace ArtUnbound.Data
{
    /// <summary>
    /// Represents the current state of a puzzle piece.
    /// </summary>
    public enum PieceState
    {
        /// <summary>Piece is in the carousel, available to be picked up.</summary>
        InPool,

        /// <summary>Piece is being held by the user.</summary>
        Grabbed,

        /// <summary>Piece has been placed on the board.</summary>
        Placed,

        /// <summary>Piece is animating back to the carousel after being dropped outside the frame.</summary>
        Returning
    }
}
