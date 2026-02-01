using System;
using System.Collections.Generic;
using System.Linq;
using ArtUnbound.Data;

namespace ArtUnbound.Services
{
    /// <summary>
    /// Service for managing weekly artwork highlights and unlocks.
    /// </summary>
    public class WeeklyUnlockService
    {
        private ArtworkCatalog catalog;
        private DateTime referenceDate = new DateTime(2024, 1, 1);

        public WeeklyUnlockService()
        {
        }

        public WeeklyUnlockService(ArtworkCatalog catalog)
        {
            this.catalog = catalog;
        }

        /// <summary>
        /// Sets the catalog reference.
        /// </summary>
        public void SetCatalog(ArtworkCatalog catalog)
        {
            this.catalog = catalog;
        }

        /// <summary>
        /// Gets the weekly artwork ID for a given date.
        /// </summary>
        public string GetWeeklyArtworkId(DateTime now, ArtworkCatalog catalog)
        {
            if (catalog == null || catalog.artworks.Count == 0)
            {
                return string.Empty;
            }

            int week = GetWeekNumber(now);
            int index = week % catalog.artworks.Count;
            return catalog.artworks[index].artworkId;
        }

        /// <summary>
        /// Gets the current weekly artwork ID.
        /// </summary>
        public string GetCurrentWeeklyArtwork()
        {
            if (catalog == null || catalog.artworks.Count == 0)
            {
                return string.Empty;
            }

            return GetWeeklyArtworkId(DateTime.Now, catalog);
        }

        /// <summary>
        /// Gets the current week number since reference date.
        /// </summary>
        public int GetCurrentWeekNumber()
        {
            return GetWeekNumber(DateTime.Now);
        }

        /// <summary>
        /// Gets the week number for a given date.
        /// </summary>
        public int GetWeekNumber(DateTime date)
        {
            TimeSpan diff = date - referenceDate;
            return (int)(diff.TotalDays / 7);
        }

        /// <summary>
        /// Checks if an artwork is unlocked based on the current week.
        /// </summary>
        public bool IsArtworkUnlocked(ArtworkDefinition artwork)
        {
            if (artwork == null) return false;
            if (artwork.isBaseContent) return true;
            if (!artwork.requiresUnlock) return true;

            int currentWeek = GetCurrentWeekNumber();
            return artwork.unlockWeek <= currentWeek;
        }

        /// <summary>
        /// Gets the list of artworks unlocked in a specific week.
        /// </summary>
        public List<ArtworkDefinition> GetUnlocksForWeek(int weekNumber)
        {
            if (catalog?.artworks == null)
                return new List<ArtworkDefinition>();

            return catalog.artworks
                .Where(a => a != null && a.requiresUnlock && a.unlockWeek == weekNumber)
                .ToList();
        }

        /// <summary>
        /// Gets all currently unlocked artworks.
        /// </summary>
        public List<ArtworkDefinition> GetAllUnlockedArtworks()
        {
            if (catalog?.artworks == null)
                return new List<ArtworkDefinition>();

            int currentWeek = GetCurrentWeekNumber();

            return catalog.artworks
                .Where(a => a != null && IsArtworkUnlocked(a))
                .ToList();
        }

        /// <summary>
        /// Gets the number of days until the next weekly unlock.
        /// </summary>
        public int GetDaysUntilNextUnlock()
        {
            DateTime now = DateTime.Now;
            int dayOfWeek = (int)now.DayOfWeek;

            // Assuming week starts on Monday
            int daysUntilMonday = (8 - dayOfWeek) % 7;
            if (daysUntilMonday == 0) daysUntilMonday = 7;

            return daysUntilMonday;
        }

        /// <summary>
        /// Gets the next artwork to be unlocked.
        /// </summary>
        public ArtworkDefinition GetNextUnlock()
        {
            if (catalog?.artworks == null)
                return null;

            int currentWeek = GetCurrentWeekNumber();

            return catalog.artworks
                .Where(a => a != null && a.requiresUnlock && a.unlockWeek > currentWeek)
                .OrderBy(a => a.unlockWeek)
                .FirstOrDefault();
        }

        /// <summary>
        /// Gets artwork recommendation based on player progress.
        /// </summary>
        public ArtworkDefinition GetRecommendedArtwork(SaveData saveData)
        {
            if (catalog?.artworks == null || catalog.artworks.Count == 0)
                return null;

            var unlocked = GetAllUnlockedArtworks();
            if (unlocked.Count == 0)
                return null;

            // Prioritize: 1) Weekly highlight, 2) Uncompleted, 3) Random
            string weeklyId = GetCurrentWeeklyArtwork();
            var weekly = unlocked.FirstOrDefault(a => a.artworkId == weeklyId);

            // Check if weekly is not completed
            if (weekly != null)
            {
                var progress = saveData?.GetProgress(weekly.artworkId);
                if (progress == null || progress.GetBestRecord() == null)
                {
                    return weekly;
                }
            }

            // Find uncompleted artworks
            var uncompleted = unlocked
                .Where(a =>
                {
                    var progress = saveData?.GetProgress(a.artworkId);
                    return progress == null || progress.GetBestRecord() == null;
                })
                .ToList();

            if (uncompleted.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, uncompleted.Count);
                return uncompleted[randomIndex];
            }

            // All completed, return random
            int index = UnityEngine.Random.Range(0, unlocked.Count);
            return unlocked[index];
        }
    }
}
