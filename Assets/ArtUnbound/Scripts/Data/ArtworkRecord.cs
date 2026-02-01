using System;

namespace ArtUnbound.Data
{
    /// <summary>
    /// Stores the best record for a specific artwork at a specific piece count.
    /// </summary>
    [Serializable]
    public class ArtworkRecord
    {
        public int pieceCount;
        public int bestTimeSec;
        public int bestScore;
        public FrameTier bestFrameTier;
        public string completedAt;
        public bool isCompleted;

        /// <summary>
        /// Alias for bestFrameTier for consistency with UI code.
        /// </summary>
        public FrameTier frameTier
        {
            get => bestFrameTier;
            set => bestFrameTier = value;
        }

        public ArtworkRecord()
        {
            pieceCount = 0;
            bestTimeSec = int.MaxValue;
            bestScore = 0;
            bestFrameTier = FrameTier.Madera;
            completedAt = string.Empty;
            isCompleted = false;
        }

        public ArtworkRecord(int pieceCount)
        {
            this.pieceCount = pieceCount;
            bestTimeSec = int.MaxValue;
            bestScore = 0;
            bestFrameTier = FrameTier.Madera;
            completedAt = string.Empty;
            isCompleted = false;
        }

        /// <summary>
        /// Updates the record if the new result is better.
        /// </summary>
        /// <returns>True if record was updated.</returns>
        public bool TryUpdateRecord(int timeSec, int score, FrameTier tier)
        {
            bool updated = false;

            // Mark as completed on first completion or any update
            if (!isCompleted)
            {
                isCompleted = true;
                updated = true;
            }

            if (score > bestScore)
            {
                bestScore = score;
                updated = true;
            }

            if (timeSec < bestTimeSec)
            {
                bestTimeSec = timeSec;
                updated = true;
            }

            if (tier > bestFrameTier)
            {
                bestFrameTier = tier;
                updated = true;
            }

            if (updated)
            {
                completedAt = DateTime.UtcNow.ToString("o");
            }

            return updated;
        }
    }
}
