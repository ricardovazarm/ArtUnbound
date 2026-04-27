using System;
using ArtUnbound.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Componente de una tarjeta de obra en la galería nativa.
    ///
    /// PREFAB HIERARCHY:
    ///   ArtworkCard (Button, 220×270 en canvas-units)
    ///     ├── Thumbnail      (Image, stretch-stretch, Bottom:50, preserveAspect, raycastTarget=OFF)
    ///     ├── Title          (TMP Bold blanco, bottom-stretch H:50, raycastTarget=OFF)
    ///     └── CompletedBadge (Image, top-right, sprite swapped to medal based on FrameTier, raycastTarget=OFF)
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ArtworkCardUI : MonoBehaviour
    {
        [SerializeField] private Image       thumbnailImage;
        [SerializeField] private TMP_Text    titleText;
        [SerializeField] private GameObject  completedBadge;

        private Button                        _button;
        private Image                         _completedBadgeImage;
        private ArtworkDefinition             _artwork;
        private Action<ArtworkDefinition>     _onTap;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(HandleTap);
            if (completedBadge != null)
                _completedBadgeImage = completedBadge.GetComponent<Image>();
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(HandleTap);
        }

        /// <summary>
        /// Configura la tarjeta con la obra y la mejor medalla obtenida.
        /// El badge se oculta si bestTier == Madera (no completada).
        /// Bronce, Plata y Oro usan su sprite correspondiente.
        /// </summary>
        public void Setup(ArtworkDefinition artwork, FrameTier bestTier,
                          Sprite bronzeMedal, Sprite silverMedal, Sprite goldMedal,
                          Action<ArtworkDefinition> onTap)
        {
            _artwork = artwork;
            _onTap   = onTap;

            // Thumbnail
            Sprite sprite = artwork?.thumbnail ?? artwork?.fullImage;
            if (thumbnailImage != null)
            {
                thumbnailImage.sprite  = sprite;
                thumbnailImage.enabled = sprite != null;
                thumbnailImage.preserveAspect = true;
            }

            // Título
            if (titleText != null)
                titleText.text = artwork?.title ?? string.Empty;

            // Badge: pick medal sprite based on highest difficulty completed
            if (completedBadge != null)
            {
                bool hasMedal = bestTier > FrameTier.Madera;
                completedBadge.SetActive(hasMedal);

                if (hasMedal && _completedBadgeImage != null)
                {
                    Sprite medal = bestTier switch
                    {
                        FrameTier.Bronce => bronzeMedal,
                        FrameTier.Plata  => silverMedal,
                        _                => goldMedal, // Oro
                    };
                    if (medal != null) _completedBadgeImage.sprite = medal;
                }
            }
        }

        private void HandleTap() => _onTap?.Invoke(_artwork);
    }
}
