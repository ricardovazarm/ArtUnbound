using System;

namespace ArtUnbound.Data
{
    /// <summary>
    /// Data for the current puzzle session.
    /// </summary>
    [Serializable]
    public class PuzzleSessionData
    {
        public string artworkId;
        public int pieceCount;
        public float elapsedTime;
        public int piecesPlaced;
        public bool helpModeUsed;
        public GameMode gameMode;
        public string startedAt;
        public string endedAt;
        public bool isCompleted;

        public PuzzleSessionData()
        {
            artworkId = string.Empty;
            pieceCount = 64;
            elapsedTime = 0f;
            piecesPlaced = 0;
            helpModeUsed = false;
            gameMode = GameMode.Gallery;
            startedAt = string.Empty;
            endedAt = string.Empty;
            isCompleted = false;
        }

        public PuzzleSessionData(string artworkId, int pieceCount, bool helpMode)
        {
            this.artworkId = artworkId;
            this.pieceCount = pieceCount;
            elapsedTime = 0f;
            piecesPlaced = 0;
            helpModeUsed = helpMode;
            gameMode = GameMode.Gallery;
            startedAt = DateTime.UtcNow.ToString("o");
            endedAt = string.Empty;
            isCompleted = false;
        }

        public int GetElapsedSeconds() => Math.Max(1, (int)elapsedTime);

        /// <summary>
        /// Starts the session timer.
        /// </summary>
        public void StartSession()
        {
            startedAt = DateTime.UtcNow.ToString("o");
            isCompleted = false;
        }

        /// <summary>
        /// Ends the session and marks it as completed.
        /// </summary>
        public void EndSession()
        {
            endedAt = DateTime.UtcNow.ToString("o");
            isCompleted = piecesPlaced >= pieceCount;
        }

        /// <summary>
        /// Updates the elapsed time based on the start time.
        /// </summary>
        public void UpdateElapsedTime()
        {
            if (DateTime.TryParse(startedAt, out DateTime start))
            {
                elapsedTime = (float)(DateTime.UtcNow - start).TotalSeconds;
            }
        }
    }
}
