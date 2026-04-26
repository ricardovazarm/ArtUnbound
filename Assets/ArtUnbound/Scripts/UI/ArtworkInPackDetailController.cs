using System;
using ArtUnbound.Data;
using ArtUnbound.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Modal split-view detail shown when the user clicks an artwork inside a pack section.
    /// LEFT  = artwork image (preserveAspect)
    /// RIGHT = artwork metadata (title/artist/museum/description) + Buy Pack button (or Owned badge).
    ///
    /// Same template as the Catalog DetailPanel but the action area is a Buy Pack
    /// instead of difficulty buttons.
    /// </summary>
    public class ArtworkInPackDetailController : MonoBehaviour
    {
        [Header("Panel root")]
        [SerializeField] private GameObject panelRoot;

        [Header("Left side (image)")]
        [SerializeField] private Image artworkImage;

        [Header("Right side (data)")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text artistText;
        [SerializeField] private TMP_Text descriptionText;

        [Header("Right side (action)")]
        [SerializeField] private Button     btnBuyPack;
        [SerializeField] private TMP_Text   btnBuyPackText;
        [SerializeField] private GameObject ownedBadge;
        [SerializeField] private Button     btnClose;

        public event Action<ArtworkPackDefinition> OnPackPurchased;

        private PackPurchaseService    _purchaseService;
        private ArtworkPackDefinition  _currentPack;
        private ArtworkDefinition      _currentArtwork;

        private void Awake()
        {
            if (btnBuyPack != null) btnBuyPack.onClick.AddListener(HandleBuyClicked);
            if (btnClose   != null) btnClose.onClick.AddListener(Hide);
        }

        private void OnDestroy()
        {
            if (btnBuyPack != null) btnBuyPack.onClick.RemoveListener(HandleBuyClicked);
            if (btnClose   != null) btnClose.onClick.RemoveListener(Hide);
        }

        public void Initialize(PackPurchaseService service)
        {
            _purchaseService = service;
        }

        public void Show(ArtworkDefinition artwork, ArtworkPackDefinition pack)
        {
            if (artwork == null || pack == null)
            {
                Debug.LogWarning("[ArtworkInPackDetail] Show called with null artwork or pack.");
                return;
            }
            _currentArtwork = artwork;
            _currentPack    = pack;

            if (artworkImage != null)
            {
                Sprite img = artwork.fullImage ?? artwork.thumbnail;
                artworkImage.sprite         = img;
                artworkImage.enabled        = img != null;
                artworkImage.preserveAspect = true;
            }

            if (titleText != null) titleText.text = artwork.title ?? string.Empty;
            if (artistText != null)
            {
                string author = artwork.author ?? string.Empty;
                artistText.text = artwork.year > 0 ? $"{author}, {artwork.year}" : author;
            }
            if (descriptionText != null) descriptionText.text = artwork.description ?? string.Empty;

            UpdateBuyOwnedState(pack);

            if (panelRoot != null) panelRoot.SetActive(true);
        }

        public void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            _currentPack    = null;
            _currentArtwork = null;
        }

        private void UpdateBuyOwnedState(ArtworkPackDefinition pack)
        {
            bool owned = _purchaseService != null && _purchaseService.IsPurchased(pack.packId);

            if (btnBuyPack != null) btnBuyPack.gameObject.SetActive(!owned);
            if (ownedBadge != null) ownedBadge.SetActive(owned);
            if (!owned && btnBuyPackText != null) btnBuyPackText.text = $"Buy Pack {pack.price}";
        }

        private void HandleBuyClicked()
        {
            if (_currentPack == null || _purchaseService == null) return;
            if (_purchaseService.IsPurchased(_currentPack.packId))
            {
                Debug.Log($"[ArtworkInPackDetail] Pack '{_currentPack.packId}' already owned.");
                return;
            }

            var packBeingPurchased = _currentPack;
            _purchaseService.PurchasePack(packBeingPurchased.packId,
                onSuccess: () =>
                {
                    OnPackPurchased?.Invoke(packBeingPurchased);
                    Hide();
                });
        }
    }
}
