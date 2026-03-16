using UnityEngine;

namespace ArtUnbound.Data
{
    /// <summary>
    /// ScriptableObject containing artwork information for puzzles.
    /// </summary>
    [CreateAssetMenu(menuName = "ArtUnbound/Artwork Definition", fileName = "ArtworkDefinition")]
    public class ArtworkDefinition : ScriptableObject
    {
        [Header("Identification")]
        public string artworkId;

        [Header("Metadata")]
        public string title;
        public string author;
        public int year;
        [TextArea(2, 5)]
        public string description;
        public string museum;
        public string artMovement;

        /// <summary>
        /// Alias for author for UI compatibility.
        /// </summary>
        public string artist
        {
            get => author;
            set => author = value;
        }

        [Header("Display")]
        public float aspectRatio = 1.0f;

        [Header("Base Sizes (meters)")]
        public Vector2 baseSizePortrait = new Vector2(0.5f, 0.70f);
        public Vector2 baseSizeLandscape = new Vector2(0.7f, 0.50f);

        [Header("Textures")]
        public Sprite thumbnail;
        public Sprite fullImage;
        public Texture2D previewTexture;
        public Texture2D puzzleTexture;

        [Header("Unlock Settings")]
        public bool isBaseContent = true;
        public bool requiresUnlock = false;
        public int unlockWeek = 0;

        [Header("Piece Counts (per difficulty)")]
        [Tooltip("Use scripts/calc_piece_counts.py with image dimensions to get these values. 0 = use PuzzleConfig default.")]
        public int pieceCountEasy;
        public int pieceCountNormal;
        public int pieceCountHard;
        public int pieceCountExpert;

        [Header("Difficulty Hints")]
        public ArtworkComplexity complexity = ArtworkComplexity.Medium;
        [Range(1, 5)]
        public int colorVariety = 3;
        [Range(1, 5)]
        public int detailLevel = 3;

        /// <summary>
        /// Gets the base size for the given orientation.
        /// </summary>
        public Vector2 GetBaseSize(Orientation orientation)
        {
            return orientation == Orientation.Landscape ? baseSizeLandscape : baseSizePortrait;
        }

        /// <summary>
        /// Gets the display name for the artwork.
        /// </summary>
        public string GetDisplayName()
        {
            if (year > 0)
                return $"{title} ({year})";
            return title;
        }

        /// <summary>
        /// Gets the piece count for the given difficulty index (0=Easy, 1=Normal, 2=Hard, 3=Expert).
        /// Returns 0 if not set (caller should use PuzzleConfig default).
        /// </summary>
        public int GetPieceCount(int difficultyIndex)
        {
            return difficultyIndex switch
            {
                0 => pieceCountEasy,
                1 => pieceCountNormal,
                2 => pieceCountHard,
                3 => pieceCountExpert,
                _ => 0
            };
        }

        /// <summary>
        /// Gets the full attribution string.
        /// </summary>
        public string GetAttribution()
        {
            string attribution = author;
            if (year > 0)
                attribution += $", {year}";
            if (!string.IsNullOrEmpty(museum))
                attribution += $"\n{museum}";
            return attribution;
        }
    }

    /// <summary>
    /// Complexity level of an artwork for puzzle difficulty.
    /// </summary>
    public enum ArtworkComplexity
    {
        Easy,
        Medium,
        Hard,
        Expert
    }
}
