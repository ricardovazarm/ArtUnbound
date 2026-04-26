using System;
using System.Linq;
using System.Text;
using ArtUnbound.Data;
using ArtUnbound.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Modal split-view detail shown when the user clicks a pack inside a bundle section.
    /// LEFT  = pack hero image (3x2)
    /// RIGHT = pack name + multiline list of artworks contained + Buy Bundle button.
    /// </summary>
    public class PackInBundleDetailController : MonoBehaviour
    {
        [Header("Panel root")]
        [SerializeField] private GameObject panelRoot;

        [Header("Left side (image)")]
        [SerializeField] private Image packHeroImage;

        [Header("Right side (data)")]
        [SerializeField] private TMP_Text packNameText;
        [SerializeField] private TMP_Text artworksListText;

        [Header("Right side (action)")]
        [SerializeField] private Button     btnBuyBundle;
        [SerializeField] private TMP_Text   btnBuyBundleText;
        [SerializeField] private GameObject ownedBadge;
        [SerializeField] private Button     btnClose;

        public event Action<BundleDefinition> OnBundlePurchased;

        private PackPurchaseService    _purchaseService;
        private ArtworkPackDefinition  _currentPack;
        private BundleDefinition       _currentBundle;

        private void Awake()
        {
            if (btnBuyBundle != null) btnBuyBundle.onClick.AddListener(HandleBuyClicked);
            if (btnClose     != null) btnClose.onClick.AddListener(Hide);
        }

        private void OnDestroy()
        {
            if (btnBuyBundle != null) btnBuyBundle.onClick.RemoveListener(HandleBuyClicked);
            if (btnClose     != null) btnClose.onClick.RemoveListener(Hide);
        }

        public void Initialize(PackPurchaseService service)
        {
            _purchaseService = service;
        }

        public void Show(ArtworkPackDefinition pack, BundleDefinition bundle)
        {
            if (pack == null || bundle == null)
            {
                Debug.LogWarning("[PackInBundleDetail] Show called with null pack or bundle.");
                return;
            }
            _currentPack   = pack;
            _currentBundle = bundle;

            if (packHeroImage != null)
            {
                Sprite hero = pack.packImage;
                if (hero == null && pack.artworks != null && pack.artworks.Count > 0)
                {
                    var first = pack.artworks[0];
                    if (first != null) hero = first.thumbnail ?? first.fullImage;
                }
                packHeroImage.sprite         = hero;
                packHeroImage.enabled        = hero != null;
                packHeroImage.preserveAspect = false;
            }

            if (packNameText     != null) packNameText.text     = pack.packName ?? string.Empty;
            if (artworksListText != null) artworksListText.text = BuildArtworksList(pack);

            UpdateBuyOwnedState(bundle);

            if (panelRoot != null) panelRoot.SetActive(true);
        }

        public void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            _currentPack   = null;
            _currentBundle = null;
        }

        private static string BuildArtworksList(ArtworkPackDefinition pack)
        {
            if (pack.artworks == null) return string.Empty;
            var sb = new StringBuilder();
            foreach (var a in pack.artworks)
            {
                if (a == null || string.IsNullOrEmpty(a.title)) continue;
                sb.Append("- ").AppendLine(a.title);
            }
            return sb.ToString();
        }

        private void UpdateBuyOwnedState(BundleDefinition bundle)
        {
            bool owned = _purchaseService != null && _purchaseService.IsPurchased(bundle.bundleId);

            if (btnBuyBundle != null) btnBuyBundle.gameObject.SetActive(!owned);
            if (ownedBadge   != null) ownedBadge.SetActive(owned);
            if (!owned && btnBuyBundleText != null) btnBuyBundleText.text = $"Buy Bundle {bundle.price}";
        }

        private void HandleBuyClicked()
        {
            if (_currentBundle == null || _purchaseService == null) return;
            if (_purchaseService.IsPurchased(_currentBundle.bundleId))
            {
                Debug.Log($"[PackInBundleDetail] Bundle '{_currentBundle.bundleId}' already owned.");
                return;
            }

            var bundleBeingPurchased = _currentBundle;
            _purchaseService.PurchaseBundle(bundleBeingPurchased.bundleId,
                onSuccess: () =>
                {
                    OnBundlePurchased?.Invoke(bundleBeingPurchased);
                    Hide();
                });
        }
    }
}
