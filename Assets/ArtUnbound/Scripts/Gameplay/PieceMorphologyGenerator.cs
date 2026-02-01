using ArtUnbound.Data;
using UnityEngine;

namespace ArtUnbound.Gameplay
{
    /// <summary>
    /// Generates valid morphologies for all pieces in a puzzle grid.
    /// Ensures that adjacent pieces have complementary edges.
    /// </summary>
    public static class PieceMorphologyGenerator
    {
        /// <summary>
        /// Generates a grid of morphologies where adjacent pieces have compatible edges.
        /// </summary>
        /// <param name="columns">Number of columns in the grid.</param>
        /// <param name="rows">Number of rows in the grid.</param>
        /// <param name="seed">Optional seed for random generation. Use -1 for random seed.</param>
        /// <returns>Array of morphologies, indexed by row * columns + col.</returns>
        public static PieceMorphology[] GenerateGrid(int columns, int rows, int seed = -1)
        {
            if (seed >= 0)
            {
                Random.InitState(seed);
            }

            int total = columns * rows;
            PieceMorphology[] morphologies = new PieceMorphology[total];

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    int index = row * columns + col;
                    morphologies[index] = GeneratePieceMorphology(col, row, columns, rows, morphologies);
                }
            }

            return morphologies;
        }

        /// <summary>
        /// Generates morphology for a single piece based on its position and neighbors.
        /// </summary>
        private static PieceMorphology GeneratePieceMorphology(int col, int row, int columns, int rows, PieceMorphology[] existing)
        {
            PieceEdgeState top, right, bottom, left;

            // Top edge: Flat if first row, otherwise opposite of neighbor above
            if (row == 0)
            {
                top = PieceEdgeState.Flat;
            }
            else
            {
                int aboveIndex = (row - 1) * columns + col;
                top = PieceMorphology.GetOpposite(existing[aboveIndex].bottom);
            }

            // Left edge: Flat if first column, otherwise opposite of neighbor to the left
            if (col == 0)
            {
                left = PieceEdgeState.Flat;
            }
            else
            {
                int leftIndex = row * columns + (col - 1);
                left = PieceMorphology.GetOpposite(existing[leftIndex].right);
            }

            // Right edge: Flat if last column, otherwise random
            if (col == columns - 1)
            {
                right = PieceEdgeState.Flat;
            }
            else
            {
                right = GetRandomInnerEdge();
            }

            // Bottom edge: Flat if last row, otherwise random
            if (row == rows - 1)
            {
                bottom = PieceEdgeState.Flat;
            }
            else
            {
                bottom = GetRandomInnerEdge();
            }

            return new PieceMorphology(top, right, bottom, left);
        }

        /// <summary>
        /// Returns a random inner edge state (Positive or Negative).
        /// </summary>
        private static PieceEdgeState GetRandomInnerEdge()
        {
            return Random.value > 0.5f ? PieceEdgeState.Positive : PieceEdgeState.Negative;
        }

        /// <summary>
        /// Validates that all adjacent pieces in the grid have compatible edges.
        /// </summary>
        public static bool ValidateGrid(PieceMorphology[] morphologies, int columns, int rows)
        {
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    int index = row * columns + col;
                    PieceMorphology current = morphologies[index];

                    // Check right neighbor
                    if (col < columns - 1)
                    {
                        int rightIndex = row * columns + (col + 1);
                        if (!current.IsCompatibleWith(morphologies[rightIndex], EdgeSide.Right))
                        {
                            Debug.LogError($"Incompatible edges at ({col},{row}) -> ({col + 1},{row})");
                            return false;
                        }
                    }

                    // Check bottom neighbor
                    if (row < rows - 1)
                    {
                        int bottomIndex = (row + 1) * columns + col;
                        if (!current.IsCompatibleWith(morphologies[bottomIndex], EdgeSide.Bottom))
                        {
                            Debug.LogError($"Incompatible edges at ({col},{row}) -> ({col},{row + 1})");
                            return false;
                        }
                    }
                }
            }

            return true;
        }
    }
}
