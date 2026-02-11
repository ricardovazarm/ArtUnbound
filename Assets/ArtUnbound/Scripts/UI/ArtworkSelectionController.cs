using System;
using System.Collections.Generic;
using ArtUnbound.Data;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Controls the Artwork Selection screen (Play Mode).
    /// Displays available artworks in a grid for the user to start a new puzzle.
    /// </summary>
    public class ArtworkSelectionController : MonoBehaviour
    {
        public event Action<string> OnArtworkSelected;
        public event Action OnBackRequested;

        [Header("UI References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Transform contentContainer;
        [SerializeField] private GameObject artworkItemPrefab;
        [SerializeField] private Button backButton;
        [SerializeField] private TextMeshProUGUI titleText;

        [Header("Empty State")]
        [SerializeField] private GameObject emptyStateObject;
        [SerializeField] private TextMeshProUGUI emptyStateText;

        private List<ArtworkDefinition> availableArtworks = new List<ArtworkDefinition>();
        private List<GameObject> instantiatedItems = new List<GameObject>();

        private void Awake()
        {
            if (backButton != null)
                backButton.onClick.AddListener(() => OnBackRequested?.Invoke());

            Hide();
        }

        public void Show()
        {
            // Ensure the script holder is active
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            if (panel != null)
                panel.SetActive(true);

            RefreshGrid();
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
            else gameObject.SetActive(false);
        }

        public void SetData(List<ArtworkDefinition> artworks)
        {
            availableArtworks = artworks ?? new List<ArtworkDefinition>();
            RefreshGrid();
        }

        private void RefreshGrid()
        {
            ClearItems();

            if (availableArtworks.Count == 0)
            {
                ShowEmptyState("No hay obras disponibles.");
                return;
            }

            HideEmptyState();

            foreach (var artwork in availableArtworks)
            {
                CreateArtworkItem(artwork);
            }
        }

        private void CreateArtworkItem(ArtworkDefinition artwork)
        {
            if (artworkItemPrefab == null || contentContainer == null) return;

            var item = Instantiate(artworkItemPrefab, contentContainer);
            item.transform.localScale = Vector3.one;
            instantiatedItems.Add(item);

            var itemController = item.GetComponent<GalleryItemController>();
            if (itemController != null)
            {
                // In Play Mode, we only show specific buttons/actions
                // For now, simpler setup than Gallery
                itemController.Setup(artwork.artworkId, FrameTier.Madera, canHang: false, canPlay: true);

                itemController.SetThumbnail(artwork.thumbnail);

                // Clicking the item selects it
                itemController.OnSelected += () => 
                {
                    Debug.Log($"[ArtworkSelectionController] Item selected: {artwork.artworkId}");
                    OnArtworkSelected?.Invoke(artwork.artworkId);
                };
            }
        }

        private void ClearItems()
        {
            foreach (var item in instantiatedItems)
            {
                if (item != null) Destroy(item);
            }
            instantiatedItems.Clear();
        }

        private void ShowEmptyState(string message)
        {
            if (emptyStateObject != null)
            {
                emptyStateObject.SetActive(true);
                if (emptyStateText != null) emptyStateText.text = message;
            }
        }

        private void HideEmptyState()
        {
            if (emptyStateObject != null) emptyStateObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (backButton != null) backButton.onClick.RemoveAllListeners();
        }
    }
}
