using System;
using ArtUnbound.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Componente de una tarjeta de galería VR (sub-panel "Galerias" del NativeGallery).
    ///
    /// PREFAB HIERARCHY:
    ///   GalleryCard (Button, 200×250 en canvas-units)
    ///     ├── Thumbnail (Image, child)
    ///     └── Label     (TMP_Text, child)
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class GalleryCardUI : MonoBehaviour
    {
        [SerializeField] private Image    thumbnailImage;
        [SerializeField] private TMP_Text labelText;

        private Button                _button;
        private GalleryDefinition     _gallery;
        private Action<string>        _onTap;

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

        /// <summary>
        /// Configura la tarjeta con la galeria. Si <paramref name="isCurrent"/> es true,
        /// el boton queda deshabilitado y el label muestra "(Actual)".
        /// </summary>
        public void Setup(GalleryDefinition gallery, bool isCurrent, Action<string> onTap)
        {
            _gallery = gallery;
            _onTap   = onTap;

            if (thumbnailImage != null)
            {
                thumbnailImage.sprite = gallery?.thumbnail;
                thumbnailImage.enabled = gallery?.thumbnail != null;
            }

            if (labelText != null)
                labelText.text = isCurrent
                    ? $"{gallery?.displayName}\n(Actual)"
                    : gallery?.displayName ?? string.Empty;

            if (_button != null)
                _button.interactable = !isCurrent;
        }

        private void HandleTap() => _onTap?.Invoke(_gallery?.galleryId);
    }
}
