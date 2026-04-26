using System;
using ArtUnbound.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArtUnbound.UI
{
    /// <summary>
    /// One bundle rendered as a Netflix-style section: header (name + price + Buy)
    /// on top and a grid of pack icons (with their pack hero) below.
    ///
    /// PREFAB HIERARCHY:
    ///   BundleSectionItem
    ///     ├── HeaderRow
    ///     │     ├── BundleNameText
    ///     │     ├── BundlePriceText
    ///     │     ├── BtnBuyBundle > BtnBuyBundleText
    ///     │     └── OwnedBadge
    ///     └── PacksContainer (GridLayoutGroup) ← spawn point for pack icons
    /// </summary>
    public class BundleSectionUI : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private TMP_Text   bundleNameText;
        [SerializeField] private Button     btnBuyBundle;
        [SerializeField] private TMP_Text   btnBuyBundleText;
        [SerializeField] private GameObject ownedBadge;

        [Header("Packs grid")]
        [SerializeField] private Transform  packsContainer;
        [SerializeField] private GameObject packIconPrefab; // GridThumbItemUI

        public event Action<BundleDefinition>                          OnBuyBundleTapped;
        public event Action<ArtworkPackDefinition, BundleDefinition>   OnPackTapped;

        private BundleDefinition _bundle;

        private void Awake()
        {
            if (btnBuyBundle != null) btnBuyBundle.onClick.AddListener(HandleBuyTapped);
        }

        private void OnDestroy()
        {
            if (btnBuyBundle != null) btnBuyBundle.onClick.RemoveListener(HandleBuyTapped);
        }

        public void Setup(BundleDefinition bundle, bool isPurchased)
        {
            _bundle = bundle;
            if (bundle == null) return;

            if (bundleNameText   != null) bundleNameText.text = bundle.bundleName ?? string.Empty;
            if (btnBuyBundle     != null) btnBuyBundle.gameObject.SetActive(!isPurchased);
            if (btnBuyBundleText != null && !isPurchased) btnBuyBundleText.text = $"Buy {bundle.price}";
            if (ownedBadge       != null) ownedBadge.SetActive(isPurchased);

            ClearContainer(packsContainer);
            if (packsContainer == null || packIconPrefab == null || bundle.packs == null) return;

            foreach (var pack in bundle.packs)
            {
                if (pack == null) continue;
                var go   = Instantiate(packIconPrefab, packsContainer);
                var item = go.GetComponent<GridThumbItemUI>();
                if (item != null)
                {
                    Sprite cover = pack.packImage;
                    if (cover == null && pack.artworks != null && pack.artworks.Count > 0)
                    {
                        var first = pack.artworks[0];
                        if (first != null) cover = first.thumbnail ?? first.fullImage;
                    }
                    item.Setup(cover, pack.packName);
                }
                var btn = go.GetComponent<Button>();
                if (btn != null)
                {
                    var captured = pack;
                    btn.onClick.AddListener(() => OnPackTapped?.Invoke(captured, _bundle));
                }
            }
        }

        private void HandleBuyTapped() => OnBuyBundleTapped?.Invoke(_bundle);

        private static void ClearContainer(Transform container)
        {
            if (container == null) return;
            for (int i = container.childCount - 1; i >= 0; i--)
                Destroy(container.GetChild(i).gameObject);
        }
    }
}
