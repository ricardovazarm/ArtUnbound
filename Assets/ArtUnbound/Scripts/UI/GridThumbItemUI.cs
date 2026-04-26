using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Lightweight non-interactive grid item: cover image + label below.
    /// Used in PackDetail (12 artworks) and BundleDetail (N packs).
    ///
    /// PREFAB HIERARCHY:
    ///   GridThumbItem (~140x180)
    ///     ├── CoverImage (Image, 140x140 top, fill)
    ///     └── LabelText  (TMP small, 140x30 below cover)
    /// </summary>
    public class GridThumbItemUI : MonoBehaviour
    {
        [SerializeField] private Image    coverImage;
        [SerializeField] private TMP_Text labelText;

        public void Setup(Sprite cover, string label)
        {
            if (coverImage != null)
            {
                coverImage.sprite         = cover;
                coverImage.enabled        = cover != null;
                coverImage.preserveAspect = false;
            }
            if (labelText != null) labelText.text = label ?? string.Empty;
        }
    }
}
