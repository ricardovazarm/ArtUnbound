using System;
using System.Collections.Generic;
using System.Linq;
using ArtUnbound.Data;

namespace ArtUnbound.Services
{
    /// <summary>
    /// Service for accessing the local artwork catalog.
    /// </summary>
    public class LocalCatalogService
    {
        private readonly ArtworkCatalog catalog;
        private Dictionary<string, ArtworkDefinition> artworkCache;

        public LocalCatalogService(ArtworkCatalog catalog)
        {
            this.catalog = catalog;
            BuildCache();
        }

        private void BuildCache()
        {
            artworkCache = new Dictionary<string, ArtworkDefinition>();

            if (catalog?.artworks == null) return;

            foreach (var artwork in catalog.artworks)
            {
                if (artwork != null && !string.IsNullOrEmpty(artwork.artworkId))
                {
                    artworkCache[artwork.artworkId] = artwork;
                }
            }
        }

        /// <summary>
        /// Gets an artwork by ID.
        /// </summary>
        public ArtworkDefinition GetById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            if (artworkCache.TryGetValue(id, out var artwork))
            {
                return artwork;
            }

            return null;
        }

        /// <summary>
        /// Gets an artwork by ID (alias for compatibility).
        /// </summary>
        public ArtworkDefinition GetArtworkById(string id)
        {
            return GetById(id);
        }

        /// <summary>
        /// Gets all artworks in the catalog.
        /// </summary>
        public List<ArtworkDefinition> GetAll()
        {
            return catalog?.artworks != null
                ? new List<ArtworkDefinition>(catalog.artworks)
                : new List<ArtworkDefinition>();
        }

        /// <summary>
        /// Gets all unlocked artworks (base content + unlocked weekly).
        /// </summary>
        public List<ArtworkDefinition> GetUnlockedArtworks(int currentWeek)
        {
            if (catalog?.artworks == null)
                return new List<ArtworkDefinition>();

            return catalog.artworks
                .Where(a => a != null && (a.isBaseContent || a.unlockWeek <= currentWeek))
                .ToList();
        }

        /// <summary>
        /// Gets artworks that require unlock.
        /// </summary>
        public List<ArtworkDefinition> GetLockedArtworks(int currentWeek)
        {
            if (catalog?.artworks == null)
                return new List<ArtworkDefinition>();

            return catalog.artworks
                .Where(a => a != null && a.requiresUnlock && a.unlockWeek > currentWeek)
                .ToList();
        }

        /// <summary>
        /// Gets artworks filtered by complexity.
        /// </summary>
        public List<ArtworkDefinition> GetByComplexity(ArtworkComplexity complexity)
        {
            if (catalog?.artworks == null)
                return new List<ArtworkDefinition>();

            return catalog.artworks
                .Where(a => a != null && a.complexity == complexity)
                .ToList();
        }

        /// <summary>
        /// Gets the total count of artworks.
        /// </summary>
        public int GetTotalCount()
        {
            return catalog?.artworks?.Count ?? 0;
        }

        /// <summary>
        /// Checks if an artwork exists.
        /// </summary>
        public bool Exists(string id)
        {
            return artworkCache.ContainsKey(id);
        }

        /// <summary>
        /// Gets a random artwork from the catalog.
        /// </summary>
        public ArtworkDefinition GetRandom()
        {
            if (catalog?.artworks == null || catalog.artworks.Count == 0)
                return null;

            int index = UnityEngine.Random.Range(0, catalog.artworks.Count);
            return catalog.artworks[index];
        }

        /// <summary>
        /// Gets artworks by art movement.
        /// </summary>
        public List<ArtworkDefinition> GetByArtMovement(string movement)
        {
            if (catalog?.artworks == null || string.IsNullOrEmpty(movement))
                return new List<ArtworkDefinition>();

            return catalog.artworks
                .Where(a => a != null &&
                       !string.IsNullOrEmpty(a.artMovement) &&
                       a.artMovement.Equals(movement, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// Gets artworks by author.
        /// </summary>
        public List<ArtworkDefinition> GetByAuthor(string author)
        {
            if (catalog?.artworks == null || string.IsNullOrEmpty(author))
                return new List<ArtworkDefinition>();

            return catalog.artworks
                .Where(a => a != null &&
                       !string.IsNullOrEmpty(a.author) &&
                       a.author.Equals(author, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }
}
