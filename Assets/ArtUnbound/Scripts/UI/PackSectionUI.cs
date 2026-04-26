using System;
using ArtUnbound.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArtUnbound.UI
{
    /// <summary>
    /// One pack rendered as a Netflix-style section: header (name + price + Buy)
    /// on top and a grid of artwork icons below.
    ///
    /// PREFAB HIERARCHY:
    ///   PackSectionItem
    ///     ├── HeaderRow (HorizontalLayoutGroup)
    ///     │     ├── PackNameText  (TMP Bold large)
    ///     │     ├── PackPriceText (TMP)
    ///     │     ├── BtnBuyPack    (Button) > BtnBuyPackText
    ///     │     └── OwnedBadge    (Image with checkmark; alternative to BtnBuyPack)
    ///     └── ArtworksContainer (GridLayoutGroup) ← spawn point for artwork icons
    /// </summary>
    public class PackSectionUI : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private TMP_Text   packNameText;
        [SerializeField] private Button     btnBuyPack;
        [SerializeField] private TMP_Text   btnBuyPackText;
        [SerializeField] private GameObject ownedBadge;

        [Header("Artworks grid")]
        [SerializeField] private Transform  artworksContainer;
        [Tooltip("Prefab with ArtworkCardUI component (reuse Assets/ArtUnbound/Prefabs/ArtworkCard2.prefab).")]
        [SerializeField] private GameObject artworkCardPrefab;

        public event Action<ArtworkPackDefinition>                       OnBuyPackTapped;
        public event Action<ArtworkDefinition, ArtworkPackDefinition>    OnArtworkTapped;

        private ArtworkPackDefinition _pack;

        private void Awake()
        {
            if (btnBuyPack != null) btnBuyPack.onClick.AddListener(HandleBuyTapped);
        }

        private void OnDestroy()
        {
            if (btnBuyPack != null) btnBuyPack.onClick.RemoveListener(HandleBuyTapped);
        }

        public void Setup(ArtworkPackDefinition pack, bool isPurchased)
        {
            _pack = pack;
            if (pack == null) return;

            if (packNameText   != null) packNameText.text = pack.packName ?? string.Empty;
            if (btnBuyPack     != null) btnBuyPack.gameObject.SetActive(!isPurchased);
            if (btnBuyPackText != null && !isPurchased) btnBuyPackText.text = $"Buy {pack.price}";
            if (ownedBadge     != null) ownedBadge.SetActive(isPurchased);

            ClearContainer(artworksContainer);
            if (artworksContainer == null || artworkCardPrefab == null || pack.artworks == null) return;

            foreach (var artwork in pack.artworks)
            {
                if (artwork == null) continue;
                var go   = Instantiate(artworkCardPrefab, artworksContainer);
                var card = go.GetComponent<ArtworkCardUI>();
                if (card != null)
                {
                    var captured = artwork;
                    // No medal in store context: pass Madera + null sprites.
                    card.Setup(captured, FrameTier.Madera, null, null, null,
                               a => OnArtworkTapped?.Invoke(a, _pack));
                }
            }
        }

        private void HandleBuyTapped() => OnBuyPackTapped?.Invoke(_pack);

        private static void ClearContainer(Transform container)
        {
            if (container == null) return;
            for (int i = container.childCount - 1; i >= 0; i--)
                Destroy(container.GetChild(i).gameObject);
        }
    }
}
