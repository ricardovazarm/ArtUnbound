using System;
using System.Collections.Generic;

namespace ArtUnbound.Data
{
    /// <summary>
    /// Main save data container for all persistent game data.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public List<ArtworkProgress> progressByArtwork = new List<ArtworkProgress>();
        public List<string> completedArtworks = new List<string>();
        public List<PlacedArtwork> placedArtworks = new List<PlacedArtwork>();
        public GameSettings settings = new GameSettings();
        public string lastArtworkId = string.Empty;
        public GameMode lastGameMode = GameMode.Gallery;
        public bool onboardingCompleted = false;
        public string firstPlayDate = string.Empty;

        /// <summary>
        /// Alias for progressByArtwork for consistency with UI code.
        /// </summary>
        public List<ArtworkProgress> artworkProgress => progressByArtwork;

        public SaveData()
        {
            progressByArtwork = new List<ArtworkProgress>();
            completedArtworks = new List<string>();
            placedArtworks = new List<PlacedArtwork>();
            settings = new GameSettings();
            lastArtworkId = string.Empty;
            lastGameMode = GameMode.Gallery;
            onboardingCompleted = false;
            firstPlayDate = DateTime.UtcNow.ToString("o");
        }

        /// <summary>
        /// Gets the progress for a specific artwork, or creates a new one if not found.
        /// </summary>
        public ArtworkProgress GetOrCreateProgress(string artworkId)
        {
            var progress = progressByArtwork.Find(p => p.artworkId == artworkId);
            if (progress == null)
            {
                progress = new ArtworkProgress { artworkId = artworkId };
                progressByArtwork.Add(progress);
            }
            return progress;
        }

        /// <summary>
        /// Gets the progress for a specific artwork, or null if not found.
        /// </summary>
        public ArtworkProgress GetProgress(string artworkId)
        {
            return progressByArtwork.Find(p => p.artworkId == artworkId);
        }

        /// <summary>
        /// Gets a list of all completed artwork progress records.
        /// </summary>
        public List<ArtworkProgress> GetCompletedArtworks()
        {
            var completed = new List<ArtworkProgress>();
            foreach (var progress in progressByArtwork)
            {
                if (progress.HasBeenCompleted())
                {
                    completed.Add(progress);
                }
            }
            return completed;
        }

        /// <summary>
        /// Gets a list of all completed artwork IDs.
        /// </summary>
        public List<string> GetCompletedArtworkIds()
        {
            var completed = new List<string>();
            foreach (var progress in progressByArtwork)
            {
                if (progress.HasBeenCompleted())
                {
                    completed.Add(progress.artworkId);
                }
            }
            return completed;
        }

        /// <summary>
        /// Gets progress records for artworks that have been "saved" (retired from display but kept).
        /// </summary>
        public List<ArtworkProgress> GetSavedArtworks()
        {
            var saved = new List<ArtworkProgress>();
            foreach (var placed in placedArtworks)
            {
                if (!placed.isActive)
                {
                    var progress = GetProgress(placed.artworkId);
                    if (progress != null)
                    {
                        saved.Add(progress);
                    }
                }
            }
            return saved;
        }

        /// <summary>
        /// Gets a list of artwork IDs that have been "saved" (retired from display but kept).
        /// </summary>
        public List<string> GetSavedArtworkIds()
        {
            var saved = new List<string>();
            foreach (var placed in placedArtworks)
            {
                if (!placed.isActive)
                {
                    saved.Add(placed.artworkId);
                }
            }
            return saved;
        }

        /// <summary>
        /// Gets all active (currently hung) artworks.
        /// </summary>
        public List<PlacedArtwork> GetActivePlacedArtworks()
        {
            return placedArtworks.FindAll(p => p.isActive);
        }

        /// <summary>
        /// Gets all retired (stored but not hung) artworks.
        /// </summary>
        public List<PlacedArtwork> GetRetiredArtworks()
        {
            return placedArtworks.FindAll(p => !p.isActive);
        }

        /// <summary>
        /// Adds a new placed artwork.
        /// </summary>
        public void AddPlacedArtwork(PlacedArtwork artwork)
        {
            placedArtworks.Add(artwork);
        }

        /// <summary>
        /// Removes a placed artwork by its ID.
        /// </summary>
        public bool RemovePlacedArtwork(string artworkId)
        {
            return placedArtworks.RemoveAll(p => p.artworkId == artworkId) > 0;
        }
    }
}
