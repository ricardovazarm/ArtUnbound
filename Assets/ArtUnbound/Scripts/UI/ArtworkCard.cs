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
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI progressText;

        public Image ThumbnailImage => thumbnailImage;
        public TextMeshProUGUI TitleText => titleText;
        public TextMeshProUGUI ProgressText => progressText;
    }
}
