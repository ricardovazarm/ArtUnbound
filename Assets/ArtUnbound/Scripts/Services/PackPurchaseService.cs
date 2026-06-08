using System;
using System.Linq;
using ArtUnbound.Data;
using UnityEngine;

namespace ArtUnbound.Services
{
    /// <summary>
    /// Handles pack and bundle purchase queries and (stub) purchase flow.
    /// Replace PurchasePack/PurchaseBundle internals with real Meta IAP when ready.
    /// </summary>
    public class PackPurchaseService : MonoBehaviour
    {
        /// <summary>
        /// Single SKU that unlocks the entire catalog (freemium "complete gallery" model).
        /// Stored in SaveData.purchasedPackIds like any other purchased id.
        /// </summary>
        public const string CatalogSku = "catalog_complete";

        [SerializeField] private ArtworkPackCatalog packCatalog;
        [SerializeField] private BundleCatalog bundleCatalog;

        [Header("Complete Catalog Purchase")]
        [Tooltip("Precio mostrado en el boton de comprar el catalogo completo.")]
        [SerializeField] private string catalogPrice = "$9.99";

        private SaveDataService saveDataService;

        public ArtworkPackCatalog PackCatalog => packCatalog;
        public BundleCatalog BundleCatalog => bundleCatalog;
        public string CatalogPrice => catalogPrice;

        public void Initialize(SaveDataService sds)
        {
            saveDataService = sds;
        }

        /// <summary>
        /// True once the user has purchased the complete catalog.
        /// </summary>
        public bool IsCatalogPurchased() => IsPurchased(CatalogSku);

        /// <summary>
        /// Initiates the complete-catalog purchase. Reuses the PurchasePack stub
        /// (marks CatalogSku as purchased and saves). Replace with Meta IAP for production.
        /// </summary>
        public void PurchaseCatalog(Action onSuccess, Action onFailure = null)
            => PurchasePack(CatalogSku, onSuccess, onFailure);

        /// <summary>
        /// An artwork is locked when it is not flagged isFree and the complete catalog
        /// has not been purchased.
        /// </summary>
        public bool IsArtworkLocked(ArtworkDefinition artwork)
            => artwork != null && !artwork.isFree && !IsCatalogPurchased();

        /// <summary>
        /// Returns true if the pack/bundle has been purchased or if id is null/empty (base-game artwork).
        /// </summary>
        public bool IsPurchased(string id)
        {
            if (string.IsNullOrEmpty(id)) return true;
            return saveDataService != null && saveDataService.IsPurchased(id);
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
        /// Returns the bundle that contains this pack, or null if no bundle includes it.
        /// </summary>
        public BundleDefinition GetBundleContainingPack(string packId)
        {
            if (bundleCatalog == null || string.IsNullOrEmpty(packId)) return null;
            return bundleCatalog.bundles.FirstOrDefault(
                b => b.packs != null && b.packs.Any(p => p != null && p.packId == packId));
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

        /// <summary>
        /// Initiates a bundle purchase. On success, marks the bundle and every included pack as purchased.
        /// Stub implementation: immediately grants everything.
        /// Replace body with Meta IAP call for production.
        /// </summary>
        public void PurchaseBundle(string bundleId, Action onSuccess, Action onFailure = null)
        {
            if (string.IsNullOrEmpty(bundleId) || bundleCatalog == null)
            {
                onFailure?.Invoke();
                return;
            }

            var bundle = bundleCatalog.bundles.FirstOrDefault(b => b != null && b.bundleId == bundleId);
            if (bundle == null)
            {
                Debug.LogWarning($"[PackPurchaseService] Bundle '{bundleId}' not found in catalog.");
                onFailure?.Invoke();
                return;
            }

            // --- Stub: grant bundle + all included packs ---
            saveDataService?.MarkAsPurchased(bundleId);
            int unlocked = 0;
            foreach (var pack in bundle.packs)
            {
                if (pack != null && !string.IsNullOrEmpty(pack.packId))
                {
                    saveDataService?.MarkAsPurchased(pack.packId);
                    unlocked++;
                }
            }
            Debug.Log($"[PackPurchaseService] Bundle '{bundleId}' purchased (stub). {unlocked} packs unlocked.");
            onSuccess?.Invoke();
            // --- End stub ---

            // TODO: replace stub with real IAP flow.
        }
    }
}
