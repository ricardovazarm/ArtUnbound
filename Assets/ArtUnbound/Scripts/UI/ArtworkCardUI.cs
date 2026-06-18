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
    ///     ├── Author         (TMP_Text, raycastTarget=OFF)
    ///     └── CompletedBadge (Image con el sprite `icon_completed` —palomita—; el codigo solo
    ///                         la muestra/oculta. raycastTarget=OFF)
    ///
    /// El badge de completada es UNA sola palomita, igual para toda obra completada (GDD 4.1):
    /// no varia segun el tamano/dificultad. Las medallas se reservan para placas (Frente 4).
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ArtworkCardUI : MonoBehaviour
    {
        [SerializeField] private Image       thumbnailImage;
        [Tooltip("Opcional. RawImage para la miniatura 3D del coleccionable (placa/estatus). Si se deja vacio, se crea en runtime clonando el rect del thumbnail.")]
        [SerializeField] private RawImage    previewImage;
        [SerializeField] private TMP_Text    titleText;
        [SerializeField] private TMP_Text    authorText;
        [Tooltip("Badge de palomita (icon_completed). Su sprite se asigna en el prefab; el codigo solo lo muestra/oculta.")]
        [SerializeField] private GameObject  completedBadge;

        [Tooltip("Icono de candado mostrado cuando la obra esta bloqueada (no gratis y catalogo no comprado).")]
        [SerializeField] private GameObject  lockBadge;

        [Tooltip("Color aplicado al thumbnail cuando la obra esta bloqueada (atenuado).")]
        [SerializeField] private Color       lockedTint = new Color(0.45f, 0.45f, 0.45f, 1f);

        private Button                        _button;
        private ArtworkDefinition             _artwork;
        private Action<ArtworkDefinition>     _onTap;
        private Action                        _onTapSimple; // para items genericos (placas/mejoras)

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
        /// Configura la tarjeta. El badge de palomita se muestra si la obra fue completada
        /// (mismo badge para todas; ver GDD 4.1).
        /// </summary>
        public void Setup(ArtworkDefinition artwork, bool isCompleted,
                          Action<ArtworkDefinition> onTap, bool isLocked = false)
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
                thumbnailImage.color   = isLocked ? lockedTint : Color.white;
            }

            // Candado: visible cuando la obra esta bloqueada tras la compra del catalogo.
            if (lockBadge != null)
                lockBadge.SetActive(isLocked);

            // Titulo y autor
            if (titleText != null)
                titleText.text = artwork?.title ?? string.Empty;

            if (authorText != null)
                authorText.text = artwork?.author ?? string.Empty;

            // Badge de completada: una sola palomita, sin variar por dificultad.
            if (completedBadge != null)
                completedBadge.SetActive(isCompleted);
        }

        /// <summary>
        /// Configura la tarjeta para un item GENERICO sin ArtworkDefinition (placas y mejoras de
        /// presentacion en la seccion Collection). Usa titulo + subtitulo (condicion) y un callback
        /// simple. El thumbnail se oculta.
        /// </summary>
        public void SetupGeneric(string title, string subtitle, bool isCompleted, bool isLocked, Action onTap,
                                 Texture previewTexture = null, Sprite iconSprite = null)
        {
            _artwork = null;
            _onTap = null;
            _onTapSimple = onTap;

            // Imagen de la tarjeta: miniatura 3D (RawImage) para colgables, o icono Sprite para
            // upgrades, o nada. Solo una de las dos se muestra.
            var raw = ResolvePreviewImage();
            if (previewTexture != null && raw != null)
            {
                raw.texture = previewTexture;
                raw.enabled = true;
                // No atenuar la miniatura 3D al estar bloqueada: el badge de candado ya comunica el
                // estado, y el lockedTint oscurecia el metal + el grabado oscuro (texto invisible).
                raw.color = Color.white;
                if (thumbnailImage != null) thumbnailImage.enabled = false;
            }
            else if (iconSprite != null && thumbnailImage != null)
            {
                thumbnailImage.sprite = iconSprite;
                thumbnailImage.enabled = true;
                thumbnailImage.preserveAspect = true;
                thumbnailImage.color = isLocked ? lockedTint : Color.white;
                if (raw != null) raw.enabled = false;
            }
            else
            {
                if (thumbnailImage != null) thumbnailImage.enabled = false;
                if (raw != null) raw.enabled = false;
            }

            if (lockBadge != null)
                lockBadge.SetActive(isLocked);

            if (titleText != null)
                titleText.text = title ?? string.Empty;

            if (authorText != null)
                authorText.text = subtitle ?? string.Empty;

            if (completedBadge != null)
                completedBadge.SetActive(isCompleted);
        }

        /// <summary>
        /// Devuelve el RawImage de preview (el asignado en el prefab o, si no hay, uno creado en
        /// runtime que clona el rect del thumbnail). Null si no hay thumbnail de referencia.
        /// </summary>
        private RawImage ResolvePreviewImage()
        {
            if (previewImage != null) return previewImage;
            if (thumbnailImage == null) return null;

            var src = thumbnailImage.rectTransform;
            var go = new GameObject("PreviewImage", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(src.parent, false);
            rt.anchorMin = src.anchorMin;
            rt.anchorMax = src.anchorMax;
            rt.pivot = src.pivot;
            rt.sizeDelta = src.sizeDelta;
            rt.anchoredPosition = src.anchoredPosition;
            rt.offsetMin = src.offsetMin;
            rt.offsetMax = src.offsetMax;
            rt.localScale = Vector3.one;
            rt.SetSiblingIndex(src.GetSiblingIndex() + 1);

            previewImage = go.AddComponent<RawImage>();
            previewImage.raycastTarget = false;
            return previewImage;
        }

        private void HandleTap()
        {
            if (_onTapSimple != null) { _onTapSimple(); return; }
            _onTap?.Invoke(_artwork);
        }
    }
}
