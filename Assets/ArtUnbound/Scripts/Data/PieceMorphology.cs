using System;

namespace ArtUnbound.Data
{
    /// <summary>
    /// Defines the edge morphology (shape) of a puzzle piece.
    /// </summary>
    [Serializable]
    public struct PieceMorphology
    {
        public PieceEdgeState top;
        public PieceEdgeState right;
        public PieceEdgeState bottom;
        public PieceEdgeState left;

        public PieceMorphology(PieceEdgeState top, PieceEdgeState right, PieceEdgeState bottom, PieceEdgeState left)
        {
            this.top = top;
            this.right = right;
            this.bottom = bottom;
            this.left = left;
        }

        /// <summary>
        /// Gets the edge state for a specific side.
        /// </summary>
        public PieceEdgeState GetEdge(EdgeSide side)
        {
            return side switch
            {
                EdgeSide.Top => top,
                EdgeSide.Right => right,
                EdgeSide.Bottom => bottom,
                EdgeSide.Left => left,
                _ => PieceEdgeState.Flat
            };
        }

        /// <summary>
        /// Checks if this piece's edge is compatible with another piece's edge.
        /// </summary>
        public bool IsCompatibleWith(PieceMorphology other, EdgeSide mySide)
        {
            PieceEdgeState myEdge = GetEdge(mySide);
            PieceEdgeState otherEdge = other.GetEdge(GetOppositeSide(mySide));

            return EdgesComplement(myEdge, otherEdge);
        }

        /// <summary>
        /// Gets the opposite side of a given side.
        /// </summary>
        public static EdgeSide GetOppositeSide(EdgeSide side)
        {
            return side switch
            {
                EdgeSide.Top => EdgeSide.Bottom,
                EdgeSide.Bottom => EdgeSide.Top,
                EdgeSide.Left => EdgeSide.Right,
                EdgeSide.Right => EdgeSide.Left,
                _ => EdgeSide.Top
            };
        }

        /// <summary>
        /// Gets the opposite edge state (Positive <-> Negative).
        /// </summary>
        public static PieceEdgeState GetOpposite(PieceEdgeState state)
        {
            return state switch
            {
                PieceEdgeState.Positive => PieceEdgeState.Negative,
                PieceEdgeState.Negative => PieceEdgeState.Positive,
                _ => PieceEdgeState.Flat
            };
        }

        /// <summary>
        /// Checks if two edges complement each other (can fit together).
        /// </summary>
        private static bool EdgesComplement(PieceEdgeState a, PieceEdgeState b)
        {
            if (a == PieceEdgeState.Flat && b == PieceEdgeState.Flat)
                return true;

            return (a == PieceEdgeState.Positive && b == PieceEdgeState.Negative)
                || (a == PieceEdgeState.Negative && b == PieceEdgeState.Positive);
        }

        /// <summary>
        /// Creates a morphology with all flat edges (for border pieces that haven't been assigned yet).
        /// </summary>
        public static PieceMorphology AllFlat()
        {
            return new PieceMorphology(PieceEdgeState.Flat, PieceEdgeState.Flat, PieceEdgeState.Flat, PieceEdgeState.Flat);
        }
    }
}
