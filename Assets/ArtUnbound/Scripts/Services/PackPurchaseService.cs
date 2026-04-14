using System;
using System.Linq;
using ArtUnbound.Data;
using UnityEngine;

namespace ArtUnbound.Services
{
    /// <summary>
    /// Handles pack purchase queries and (stub) purchase flow.
    /// Replace PurchasePack internals with real Meta IAP when ready.
    /// </summary>
    public class PackPurchaseService : MonoBehaviour
    {
        [SerializeField] private ArtworkPackCatalog packCatalog;

        private SaveDataService saveDataService;

        public ArtworkPackCatalog PackCatalog => packCatalog;

        public void Initialize(SaveDataService sds)
        {
            saveDataService = sds;
        }

        /// <summary>
        /// Returns true if the pack has been purchased or if packId is null/empty (base-game artwork).
        /// </summary>
        public bool IsPurchased(string packId)
        {
            if (string.IsNullOrEmpty(packId)) return true;
            return saveDataService != null && saveDataService.IsPurchased(packId);
        }

        /// <summary>
        /// Returns the pack that contains this artwork, or null if it is a base-game artwork.
        /// </summary>
        public ArtworkPackDefinition GetPackForArtwork(string artworkId)
        {
            if (packCatalog == null || string.IsNullOrEmpty(artworkId)) return null;
            return packCatalog.packs.FirstOrDefault(
                p => p.artworks != null && p.artworks.Any(a => a != null && a.artworkId == artworkId));
        }

        /// <summary>
        /// Checks if an artwork is locked (belongs to an unpurchased pack).
        /// </summary>
        public bool IsArtworkLocked(string artworkId)
        {
            var pack = GetPackForArtwork(artworkId);
            return pack != null && !IsPurchased(pack.packId);
        }

        /// <summary>
        /// Initiates a pack purchase.
        /// Stub implementation: immediately marks as purchased.
        /// Replace body with Meta IAP call for production.
        /// </summary>
        public void PurchasePack(string packId, Action onSuccess, Action onFailure = null)
        {
            if (string.IsNullOrEmpty(packId))
            {
                onFailure?.Invoke();
                return;
            }

            // --- Stub: grant immediately ---
            saveDataService?.MarkAsPurchased(packId);
            Debug.Log($"[PackPurchaseService] Pack '{packId}' purchased (stub).");
            onSuccess?.Invoke();
            // --- End stub ---

            // TODO: replace stub with real IAP flow, e.g.:
            // MetaIAP.Purchase(packId, onSuccess, onFailure);
        }
    }
}
