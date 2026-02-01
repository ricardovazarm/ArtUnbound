using System.Collections.Generic;
using UnityEngine;

namespace ArtUnbound.Gameplay
{
    /// <summary>
    /// Utility class for shuffling puzzle pieces using Fisher-Yates algorithm.
    /// </summary>
    public static class PieceShuffler
    {
        /// <summary>
        /// Shuffles a list of puzzle pieces in place using Fisher-Yates algorithm.
        /// </summary>
        public static void Shuffle(List<PuzzlePiece> pieces)
        {
            int n = pieces.Count;
            for (int i = n - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (pieces[i], pieces[j]) = (pieces[j], pieces[i]);
            }
        }

        /// <summary>
        /// Shuffles a list of puzzle pieces with a specific seed for reproducibility.
        /// </summary>
        public static void ShuffleWithSeed(List<PuzzlePiece> pieces, int seed)
        {
            Random.InitState(seed);
            Shuffle(pieces);
        }

        /// <summary>
        /// Returns a new shuffled list without modifying the original.
        /// </summary>
        public static List<PuzzlePiece> GetShuffledCopy(List<PuzzlePiece> pieces)
        {
            List<PuzzlePiece> copy = new List<PuzzlePiece>(pieces);
            Shuffle(copy);
            return copy;
        }

        /// <summary>
        /// Returns a new shuffled list with a specific seed without modifying the original.
        /// </summary>
        public static List<PuzzlePiece> GetShuffledCopyWithSeed(List<PuzzlePiece> pieces, int seed)
        {
            Random.InitState(seed);
            return GetShuffledCopy(pieces);
        }

        /// <summary>
        /// Shuffles an array of integers (useful for shuffling indices).
        /// </summary>
        public static void ShuffleIndices(int[] indices)
        {
            int n = indices.Length;
            for (int i = n - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (indices[i], indices[j]) = (indices[j], indices[i]);
            }
        }

        /// <summary>
        /// Creates and returns a shuffled array of indices from 0 to count-1.
        /// </summary>
        public static int[] CreateShuffledIndices(int count)
        {
            int[] indices = new int[count];
            for (int i = 0; i < count; i++)
            {
                indices[i] = i;
            }
            ShuffleIndices(indices);
            return indices;
        }
    }
}
