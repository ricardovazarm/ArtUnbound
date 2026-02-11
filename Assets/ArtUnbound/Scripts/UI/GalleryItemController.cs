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


        [Header("Display")]
        [SerializeField] private Image thumbnailImage;
        [SerializeField] private Image frameImage;

        [Header("Buttons")]
        [SerializeField] private Button selectButton;


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
            if (selectButton == null)
            {
                selectButton = GetComponent<Button>();
            }

            if (selectButton != null)
                selectButton.onClick.AddListener(() => OnSelected?.Invoke());



            // Auto-setup for Poke Interaction (Direct Touch)
            SetupPokeInteractions();
        }

        private void SetupPokeInteractions()
        {
            AddColliderToButton(selectButton);

        }

        private void AddColliderToButton(Button btn)
        {
            if (btn == null) return;

            // Ensure the button has a BoxCollider for Poke detection
            var collider = btn.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = btn.gameObject.AddComponent<BoxCollider>();
                collider.isTrigger = true;

                // Sizing specific to UI
                var rectTransform = btn.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    collider.size = new Vector3(rectTransform.rect.width, rectTransform.rect.height, 0.01f);
                    collider.center = new Vector3(0, 0, 0.005f); // Slight offset
                }
            }
        }

        /// <summary>
        /// Sets up the item for a completed artwork.
        /// </summary>
        public void Setup(string id, FrameTier tier, bool canHang, bool canPlay)
        {
            Debug.Log($"[GalleryItemController] Setup called for {id}. Tier: {tier}");
            artworkId = id;
            frameTier = tier;

            UpdateFrameDisplay();



        }

        /// <summary>
        /// Sets up the item for a placed/hung artwork.
        /// </summary>
        public void SetupPlaced(PlacedArtwork placed)
        {
            artworkId = placed.artworkId;
            frameTier = placed.frameTier;

            UpdateFrameDisplay();





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






        }

        /// <summary>
        /// Sets the thumbnail image.
        /// </summary>
        public void SetThumbnail(Sprite sprite)
        {
            if (thumbnailImage == null)
            {
                // Silent return or debug log level lowered, as user might want image-only or no-thumbnail
                // But if they want image-only, they DO need the thumbnail.
                // However, if they map the root image as thumbnail, that works too.
                return;
            }

            if (sprite == null)
            {
                Debug.LogWarning($"[GalleryItemController] Sprite passed to SetThumbnail is NULL for {artworkId}");
                return;
            }

            thumbnailImage.sprite = sprite;
            thumbnailImage.gameObject.SetActive(true); // Ensure it's visible
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



        private void UpdateFrameDisplay()
        {
            if (frameImage == null) return;

            Sprite newSprite = GetFrameSprite(frameTier);
            if (newSprite != null)
            {
                frameImage.gameObject.SetActive(true);
                frameImage.sprite = newSprite;
            }
            // If null, we keep the default sprite assigned in prefab (or do nothing)
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



        private void OnDestroy()
        {
            if (selectButton != null) selectButton.onClick.RemoveAllListeners();

        }
    }
}
