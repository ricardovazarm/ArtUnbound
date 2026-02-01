using System;
using System.Collections.Generic;

namespace ArtUnbound.Data
{
    /// <summary>
    /// Stores progress and records for a specific artwork.
    /// </summary>
    [Serializable]
    public class ArtworkProgress
    {
        public string artworkId;
        public List<ArtworkRecord> recordsByPieceCount = new List<ArtworkRecord>();
        public FrameTier bestFrameTier = FrameTier.Madera;
        public string firstCompletedAt = string.Empty;
        public int totalCompletions = 0;

        public ArtworkProgress()
        {
            artworkId = string.Empty;
            recordsByPieceCount = new List<ArtworkRecord>();
            bestFrameTier = FrameTier.Madera;
            firstCompletedAt = string.Empty;
            totalCompletions = 0;
        }

        /// <summary>
        /// Gets or creates a record for a specific piece count.
        /// </summary>
        public ArtworkRecord GetOrCreateRecord(int pieceCount)
        {
            var record = recordsByPieceCount.Find(r => r.pieceCount == pieceCount);
            if (record == null)
            {
                record = new ArtworkRecord(pieceCount);
                recordsByPieceCount.Add(record);
            }
            return record;
        }

        /// <summary>
        /// Gets the record for a specific piece count, or null if not found.
        /// </summary>
        public ArtworkRecord GetRecordForPieceCount(int pieceCount)
        {
            return recordsByPieceCount.Find(r => r.pieceCount == pieceCount);
        }

        /// <summary>
        /// Gets the best record across all piece counts.
        /// </summary>
        public ArtworkRecord GetBestRecord()
        {
            ArtworkRecord best = null;
            int highestScore = -1;

            foreach (var record in recordsByPieceCount)
            {
                if (record.isCompleted && record.bestScore > highestScore)
                {
                    highestScore = record.bestScore;
                    best = record;
                }
            }

            return best;
        }

        /// <summary>
        /// Records a completion of this artwork.
        /// </summary>
        /// <returns>True if a new record was set.</returns>
        public bool RecordCompletion(int pieceCount, int timeSec, int score, FrameTier frameTier)
        {
            var record = GetOrCreateRecord(pieceCount);
            bool newRecord = record.TryUpdateRecord(timeSec, score, frameTier);

            if (frameTier > bestFrameTier)
            {
                bestFrameTier = frameTier;
            }

            if (string.IsNullOrEmpty(firstCompletedAt))
            {
                firstCompletedAt = DateTime.UtcNow.ToString("o");
            }

            totalCompletions++;

            return newRecord;
        }

        /// <summary>
        /// Gets the best time for a specific piece count.
        /// </summary>
        public int GetBestTime(int pieceCount)
        {
            var record = recordsByPieceCount.Find(r => r.pieceCount == pieceCount);
            return record?.bestTimeSec ?? int.MaxValue;
        }

        /// <summary>
        /// Gets the best score for a specific piece count.
        /// </summary>
        public int GetBestScore(int pieceCount)
        {
            var record = recordsByPieceCount.Find(r => r.pieceCount == pieceCount);
            return record?.bestScore ?? 0;
        }

        /// <summary>
        /// Checks if this artwork has been completed at any difficulty.
        /// </summary>
        public bool HasBeenCompleted()
        {
            return totalCompletions > 0;
        }
    }
}
