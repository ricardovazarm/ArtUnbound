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
    ///     └── CompletedBadge (Image ✓ verde, top-right, SetActive, raycastTarget=OFF)
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ArtworkCardUI : MonoBehaviour
    {
        [SerializeField] private Image       thumbnailImage;
        [SerializeField] private TMP_Text    titleText;
        [SerializeField] private GameObject  completedBadge;

        private Button                        _button;
        private ArtworkDefinition             _artwork;
        private Action<ArtworkDefinition>     _onTap;

        // ─────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(HandleTap);
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(HandleTap);
        }

        // ─────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Configura la tarjeta con la obra y el estado de progreso del usuario.
        /// </summary>
        public void Setup(ArtworkDefinition artwork, bool isCompleted,
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

            // Badge de completada
            if (completedBadge != null)
                completedBadge.SetActive(isCompleted);
        }

        // ─────────────────────────────────────────────────────────────────────
        private void HandleTap() => _onTap?.Invoke(_artwork);
    }
}
