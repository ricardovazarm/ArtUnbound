using System;
using ArtUnbound.Data;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Controls an individual artwork item in the gallery list.
    /// </summary>
    public class GalleryItemController : MonoBehaviour
    {
        public event Action OnSelected;
        public event Action OnPlayClicked;
        public event Action OnHangClicked;
        public event Action OnRelocateClicked;
        public event Action OnRemoveClicked;

        [Header("Display")]
        [SerializeField] private Image thumbnailImage;
        [SerializeField] private Image frameImage;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private GameObject progressIndicator;
        [SerializeField] private TextMeshProUGUI progressText;

        [Header("Buttons")]
        [SerializeField] private Button selectButton;
        [SerializeField] private Button playButton;
        [SerializeField] private Button hangButton;
        [SerializeField] private Button relocateButton;
        [SerializeField] private Button removeButton;

        [Header("Frame Sprites")]
        [SerializeField] private Sprite frameMadera;
        [SerializeField] private Sprite frameBronce;
        [SerializeField] private Sprite framePlata;
        [SerializeField] private Sprite frameOro;
        [SerializeField] private Sprite frameEbano;

        public string ArtworkId => artworkId;

        private string artworkId;
        private FrameTier frameTier;

        private void Awake()
        {
            if (selectButton != null)
                selectButton.onClick.AddListener(() => OnSelected?.Invoke());

            if (playButton != null)
                playButton.onClick.AddListener(() => OnPlayClicked?.Invoke());

            if (hangButton != null)
                hangButton.onClick.AddListener(() => OnHangClicked?.Invoke());

            if (relocateButton != null)
                relocateButton.onClick.AddListener(() => OnRelocateClicked?.Invoke());

            if (removeButton != null)
                removeButton.onClick.AddListener(() => OnRemoveClicked?.Invoke());
        }

        /// <summary>
        /// Sets up the item for a completed artwork.
        /// </summary>
        public void Setup(string id, FrameTier tier, bool canHang, bool canPlay)
        {
            artworkId = id;
            frameTier = tier;

            UpdateFrameDisplay();
            HideProgressIndicator();

            if (playButton != null)
                playButton.gameObject.SetActive(canPlay);

            if (hangButton != null)
                hangButton.gameObject.SetActive(canHang);

            if (relocateButton != null)
                relocateButton.gameObject.SetActive(false);

            if (removeButton != null)
                removeButton.gameObject.SetActive(false);
        }

        /// <summary>
        /// Sets up the item for a placed/hung artwork.
        /// </summary>
        public void SetupPlaced(PlacedArtwork placed)
        {
            artworkId = placed.artworkId;
            frameTier = placed.frameTier;

            UpdateFrameDisplay();
            HideProgressIndicator();

            if (subtitleText != null)
            {
                string dateStr = FormatPlacedDate(placed.placedAt);
                subtitleText.text = $"Colgado el {dateStr}";
            }

            if (playButton != null)
                playButton.gameObject.SetActive(false);

            if (hangButton != null)
                hangButton.gameObject.SetActive(false);

            if (relocateButton != null)
                relocateButton.gameObject.SetActive(true);

            if (removeButton != null)
                removeButton.gameObject.SetActive(true);
        }

        private string FormatPlacedDate(string isoDate)
        {
            if (DateTime.TryParse(isoDate, out DateTime date))
            {
                return date.ToString("dd/MM/yyyy");
            }
            return "fecha desconocida";
        }

        /// <summary>
        /// Sets up the item for a saved/in-progress artwork.
        /// </summary>
        public void SetupSaved(ArtworkProgress progress)
        {
            artworkId = progress.artworkId;
            frameTier = progress.bestFrameTier;

            if (frameImage != null)
                frameImage.gameObject.SetActive(false);

            HideProgressIndicator();

            if (subtitleText != null)
            {
                subtitleText.text = "Guardado";
            }

            if (playButton != null)
            {
                playButton.gameObject.SetActive(true);
                var playText = playButton.GetComponentInChildren<TextMeshProUGUI>();
                if (playText != null)
                    playText.text = "Continuar";
            }

            if (hangButton != null)
                hangButton.gameObject.SetActive(false);

            if (relocateButton != null)
                relocateButton.gameObject.SetActive(false);

            if (removeButton != null)
                removeButton.gameObject.SetActive(false);
        }

        /// <summary>
        /// Sets up the item for a saved puzzle session with progress.
        /// </summary>
        public void SetupSavedSession(PuzzleSessionData session)
        {
            artworkId = session.artworkId;
            frameTier = FrameTier.Madera;

            if (frameImage != null)
                frameImage.gameObject.SetActive(false);

            ShowProgressIndicator(session.piecesPlaced, session.pieceCount);

            if (subtitleText != null)
            {
                int percentage = session.pieceCount > 0
                    ? (session.piecesPlaced * 100) / session.pieceCount
                    : 0;
                subtitleText.text = $"{percentage}% completado";
            }

            if (playButton != null)
            {
                playButton.gameObject.SetActive(true);
                var playText = playButton.GetComponentInChildren<TextMeshProUGUI>();
                if (playText != null)
                    playText.text = "Continuar";
            }

            if (hangButton != null)
                hangButton.gameObject.SetActive(false);

            if (relocateButton != null)
                relocateButton.gameObject.SetActive(false);

            if (removeButton != null)
                removeButton.gameObject.SetActive(false);
        }

        /// <summary>
        /// Sets the thumbnail image.
        /// </summary>
        public void SetThumbnail(Sprite sprite)
        {
            if (thumbnailImage != null && sprite != null)
                thumbnailImage.sprite = sprite;
        }

        /// <summary>
        /// Sets the thumbnail from a texture.
        /// </summary>
        public void SetThumbnail(Texture2D texture)
        {
            if (thumbnailImage != null && texture != null)
            {
                var sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );
                thumbnailImage.sprite = sprite;
            }
        }

        /// <summary>
        /// Sets the title text.
        /// </summary>
        public void SetTitle(string title)
        {
            if (titleText != null)
                titleText.text = title;
        }

        private void UpdateFrameDisplay()
        {
            if (frameImage == null) return;

            frameImage.gameObject.SetActive(true);
            frameImage.sprite = GetFrameSprite(frameTier);
        }

        private Sprite GetFrameSprite(FrameTier tier)
        {
            return tier switch
            {
                FrameTier.Madera => frameMadera,
                FrameTier.Bronce => frameBronce,
                FrameTier.Plata => framePlata,
                FrameTier.Oro => frameOro,
                FrameTier.Ebano => frameEbano,
                _ => frameMadera
            };
        }

        private void ShowProgressIndicator(int placed, int total)
        {
            if (progressIndicator != null)
                progressIndicator.SetActive(true);

            if (progressText != null)
                progressText.text = $"{placed}/{total}";
        }

        private void HideProgressIndicator()
        {
            if (progressIndicator != null)
                progressIndicator.SetActive(false);
        }

        private void OnDestroy()
        {
            if (selectButton != null) selectButton.onClick.RemoveAllListeners();
            if (playButton != null) playButton.onClick.RemoveAllListeners();
            if (hangButton != null) hangButton.onClick.RemoveAllListeners();
            if (relocateButton != null) relocateButton.onClick.RemoveAllListeners();
            if (removeButton != null) removeButton.onClick.RemoveAllListeners();
        }
    }
}
