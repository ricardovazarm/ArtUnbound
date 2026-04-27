using System;
using ArtUnbound.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Sub-panel shown when the user taps "Galerías" in the NavigationGallery (VR mode).
    /// Lists all available galleries; the active one is shown as "Actual" (disabled).
    /// </summary>
    public class GallerySelectionController : MonoBehaviour
    {
        public event Action<string> OnGallerySelected;

        [Header("References")]
        [SerializeField] private GalleryCatalog galleryCatalog;
        [SerializeField] private Transform gridContainer;
        [SerializeField] private GameObject galleryCardPrefab;

        private string _activeGalleryId;

        private void Awake()
        {
            gameObject.SetActive(false);
        }

        public void Show(string currentGalleryId)
        {
            _activeGalleryId = currentGalleryId;
            PopulateGrid();
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void PopulateGrid()
        {
            if (gridContainer == null || galleryCardPrefab == null || galleryCatalog == null) return;

            // Clear existing cards
            for (int i = gridContainer.childCount - 1; i >= 0; i--)
                Destroy(gridContainer.GetChild(i).gameObject);

            foreach (var gallery in galleryCatalog.galleries)
            {
                if (gallery == null) continue;
                bool isCurrent = gallery.galleryId == _activeGalleryId;
                SpawnCard(gallery, isCurrent);
            }
        }

        private void SpawnCard(GalleryDefinition gallery, bool isCurrent)
        {
            var go = Instantiate(galleryCardPrefab, gridContainer);

            var card = go.GetComponent<GalleryCardUI>();
            if (card != null)
            {
                card.Setup(gallery, isCurrent, OnCardTapped);
            }
            else
            {
                Debug.LogWarning($"[GallerySelectionController] El prefab '{galleryCardPrefab.name}' no tiene GalleryCardUI.");
            }

            // Apply the central theme so spawned cards match the rest of the UI
            // (transparent rest, semi-brown hover).
            UIButtonTheme.ApplyTo(go.GetComponent<Button>());
        }

        private void OnCardTapped(string galleryId)
        {
            OnGallerySelected?.Invoke(galleryId);
            Hide();
        }
    }
}
