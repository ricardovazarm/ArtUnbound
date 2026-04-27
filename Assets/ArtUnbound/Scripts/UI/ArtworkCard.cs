using ArtUnbound.Data;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Component for artwork card in the catalog grid.
    /// Holds references to UI elements for easy setup.
    /// </summary>
    public class ArtworkCard : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Image thumbnailImage;
        [SerializeField] private Image frameImage;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI progressText;

        [Header("Frame Sprites")]
        [SerializeField] private Sprite frameMadera;
        [SerializeField] private Sprite frameBronce;
        [SerializeField] private Sprite framePlata;
        [SerializeField] private Sprite frameOro;

        public Image ThumbnailImage => thumbnailImage;
        public TextMeshProUGUI TitleText => titleText;
        public TextMeshProUGUI ProgressText => progressText;

        /// <summary>
        /// Sets the frame image based on the earned tier (Madera when not completed).
        /// </summary>
        public void SetFrameTier(FrameTier tier)
        {
            if (frameImage == null) return;

            Sprite sprite = GetFrameSprite(tier);
            if (sprite != null)
            {
                frameImage.gameObject.SetActive(true);
                frameImage.sprite = sprite;
            }
        }

        private Sprite GetFrameSprite(FrameTier tier)
        {
            return tier switch
            {
                FrameTier.Madera => frameMadera,
                FrameTier.Bronce => frameBronce,
                FrameTier.Plata => framePlata,
                FrameTier.Oro => frameOro,
                _ => frameMadera
            };
        }
    }
}
