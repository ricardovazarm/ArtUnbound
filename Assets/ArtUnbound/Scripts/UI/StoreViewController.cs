using System;
using ArtUnbound.Data;
using ArtUnbound.Services;
using UnityEngine;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Populates the Store with two tabs (Paquetes / Bundles), each rendered as a
    /// vertical list of sections (Netflix style). Tapping an artwork in a pack
    /// section opens the ArtworkInPackDetailPanel; tapping a pack in a bundle
    /// section opens the PackInBundleDetailPanel.
    ///
    /// HIERARCHY SETUP IN UNITY:
    ///   StoreView
    ///     ├── TopTabs                       (StoreTabsController)
    ///     ├── PaquetesView                  (active by default)
    ///     │     └── ScrollRect > Viewport > PaquetesContent (VerticalLayoutGroup)
    ///     └── BundlesView                   (inactive by default)
    ///           └── ScrollRect > Viewport > BundlesContent (VerticalLayoutGroup)
    ///   ArtworkInPackDetailPanel  (sibling, modal)
    ///   PackInBundleDetailPanel   (sibling, modal)
    /// </summary>
    public class StoreViewController : MonoBehaviour
    {
        [Header("Tabs")]
        [SerializeField] private StoreTabsController storeTabsController;

        [Header("Section spawn points")]
        [Tooltip("Parent transform (with VerticalLayoutGroup) where PackSectionUI prefabs go.")]
        [SerializeField] private Transform paquetesContent;

        [Tooltip("Parent transform (with VerticalLayoutGroup) where BundleSectionUI prefabs go.")]
        [SerializeField] private Transform bundlesContent;

        [Header("Section prefabs")]
        [SerializeField] private GameObject packSectionPrefab;
        [SerializeField] private GameObject bundleSectionPrefab;

        [Header("Detail panels")]
        [SerializeField] private ArtworkInPackDetailController artworkInPackDetailController;
        [SerializeField] private PackInBundleDetailController  packInBundleDetailController;

        [Header("Empty Labels (optional)")]
        [SerializeField] private GameObject noBundlesLabel;
        [SerializeField] private GameObject noPacksLabel;

        private PackPurchaseService _purchaseService;

        public event Action<ArtworkPackDefinition> OnPackPurchased;
        public event Action<BundleDefinition>      OnBundlePurchased;

        public void Initialize(PackPurchaseService purchaseService)
        {
            _purchaseService = purchaseService;

            if (artworkInPackDetailController != null)
            {
                artworkInPackDetailController.Initialize(purchaseService);
                artworkInPackDetailController.OnPackPurchased -= HandlePackPurchasedFromDetail;
                artworkInPackDetailController.OnPackPurchased += HandlePackPurchasedFromDetail;
            }
            if (packInBundleDetailController != null)
            {
                packInBundleDetailController.Initialize(purchaseService);
                packInBundleDetailController.OnBundlePurchased -= HandleBundlePurchasedFromDetail;
                packInBundleDetailController.OnBundlePurchased += HandleBundlePurchasedFromDetail;
            }

            Populate();
        }

        private void OnDestroy()
        {
            if (artworkInPackDetailController != null)
                artworkInPackDetailController.OnPackPurchased -= HandlePackPurchasedFromDetail;
            if (packInBundleDetailController != null)
                packInBundleDetailController.OnBundlePurchased -= HandleBundlePurchasedFromDetail;
        }

        /// <summary>
        /// Rebuilds both lists. Call after Initialize and after every purchase to refresh.
        /// </summary>
        public void Populate()
        {
            if (_purchaseService == null)
            {
                Debug.LogWarning("[StoreView] Initialize() not called before Populate.");
                return;
            }

            ClearContainer(paquetesContent);
            ClearContainer(bundlesContent);

            // Paquetes
            int packCount = 0;
            var packCatalog = _purchaseService.PackCatalog;
            if (packCatalog != null && packSectionPrefab != null && paquetesContent != null)
            {
                foreach (var pack in packCatalog.packs)
                {
                    if (pack == null) continue;
                    SpawnPackSection(pack);
                    packCount++;
                }
            }
            if (noPacksLabel != null) noPacksLabel.SetActive(packCount == 0);

            // Bundles
            int bundleCount = 0;
            var bundleCatalog = _purchaseService.BundleCatalog;
            if (bundleCatalog != null && bundleSectionPrefab != null && bundlesContent != null)
            {
                foreach (var bundle in bundleCatalog.bundles)
                {
                    if (bundle == null) continue;
                    SpawnBundleSection(bundle);
                    bundleCount++;
                }
            }
            if (noBundlesLabel != null) noBundlesLabel.SetActive(bundleCount == 0);

            Debug.Log($"[StoreView] Populated: {packCount} packs, {bundleCount} bundles.");
        }

        private void SpawnPackSection(ArtworkPackDefinition pack)
        {
            var go      = Instantiate(packSectionPrefab, paquetesContent);
            var section = go.GetComponent<PackSectionUI>();
            if (section == null)
            {
                Debug.LogWarning("[StoreView] packSectionPrefab is missing PackSectionUI component.");
                return;
            }
            bool owned = _purchaseService.IsPurchased(pack.packId);
            section.Setup(pack, owned);

            section.OnBuyPackTapped  -= HandleBuyPackTapped;
            section.OnBuyPackTapped  += HandleBuyPackTapped;
            section.OnArtworkTapped  -= HandleArtworkTapped;
            section.OnArtworkTapped  += HandleArtworkTapped;
        }

        private void SpawnBundleSection(BundleDefinition bundle)
        {
            var go      = Instantiate(bundleSectionPrefab, bundlesContent);
            var section = go.GetComponent<BundleSectionUI>();
            if (section == null)
            {
                Debug.LogWarning("[StoreView] bundleSectionPrefab is missing BundleSectionUI component.");
                return;
            }
            bool owned = _purchaseService.IsPurchased(bundle.bundleId);
            section.Setup(bundle, owned);

            section.OnBuyBundleTapped -= HandleBuyBundleTapped;
            section.OnBuyBundleTapped += HandleBuyBundleTapped;
            section.OnPackTapped      -= HandlePackInBundleTapped;
            section.OnPackTapped      += HandlePackInBundleTapped;
        }

        // ── Section header buttons ──────────────────────────────────────────────

        private void HandleBuyPackTapped(ArtworkPackDefinition pack)
        {
            if (pack == null || _purchaseService == null) return;
            if (_purchaseService.IsPurchased(pack.packId)) return;
            _purchaseService.PurchasePack(pack.packId,
                onSuccess: () =>
                {
                    OnPackPurchased?.Invoke(pack);
                    Populate();
                });
        }

        private void HandleBuyBundleTapped(BundleDefinition bundle)
        {
            if (bundle == null || _purchaseService == null) return;
            if (_purchaseService.IsPurchased(bundle.bundleId)) return;
            _purchaseService.PurchaseBundle(bundle.bundleId,
                onSuccess: () =>
                {
                    OnBundlePurchased?.Invoke(bundle);
                    Populate();
                });
        }

        // ── Icon clicks → detail panels ─────────────────────────────────────────

        private void HandleArtworkTapped(ArtworkDefinition artwork, ArtworkPackDefinition pack)
        {
            if (artworkInPackDetailController == null)
            {
                Debug.LogWarning("[StoreView] artworkInPackDetailController not assigned.");
                return;
            }
            artworkInPackDetailController.Show(artwork, pack);
        }

        private void HandlePackInBundleTapped(ArtworkPackDefinition pack, BundleDefinition bundle)
        {
            if (packInBundleDetailController == null)
            {
                Debug.LogWarning("[StoreView] packInBundleDetailController not assigned.");
                return;
            }
            packInBundleDetailController.Show(pack, bundle);
        }

        // ── Detail panel callbacks ──────────────────────────────────────────────

        private void HandlePackPurchasedFromDetail(ArtworkPackDefinition pack)
        {
            OnPackPurchased?.Invoke(pack);
            Populate();
        }

        private void HandleBundlePurchasedFromDetail(BundleDefinition bundle)
        {
            OnBundlePurchased?.Invoke(bundle);
            Populate();
        }

        private static void ClearContainer(Transform container)
        {
            if (container == null) return;
            for (int i = container.childCount - 1; i >= 0; i--)
                Destroy(container.GetChild(i).gameObject);
        }
    }
}
