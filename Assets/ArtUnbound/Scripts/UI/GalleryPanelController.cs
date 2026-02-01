using System;
using System.Collections.Generic;
using ArtUnbound.Data;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Controls the personal gallery panel with tabs for completed, hung, and saved artworks.
    /// </summary>
    public class GalleryPanelController : MonoBehaviour
    {
        public event Action<string> OnArtworkSelected;
        public event Action<string> OnPlayRequested;
        public event Action<string> OnHangRequested;
        public event Action<string> OnRelocateRequested;
        public event Action<string> OnRemoveRequested;

        [Header("Panel References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Transform contentContainer;
        [SerializeField] private GameObject artworkItemPrefab;

        [Header("Tab Buttons")]
        [SerializeField] private Button tabCompletadas;
        [SerializeField] private Button tabColgadas;
        [SerializeField] private Button tabGuardadas;

        [Header("Tab Indicators")]
        [SerializeField] private GameObject indicatorCompletadas;
        [SerializeField] private GameObject indicatorColgadas;
        [SerializeField] private GameObject indicatorGuardadas;

        [Header("Empty State")]
        [SerializeField] private GameObject emptyStateObject;
        [SerializeField] private TextMeshProUGUI emptyStateText;

        [Header("Navigation")]
        [SerializeField] private Button closeButton;

        public GalleryTab CurrentTab => currentTab;

        private GalleryTab currentTab = GalleryTab.Completadas;
        private List<ArtworkProgress> completedArtworks = new List<ArtworkProgress>();
        private List<PlacedArtwork> hungArtworks = new List<PlacedArtwork>();
        private List<ArtworkProgress> savedArtworks = new List<ArtworkProgress>();
        private List<GameObject> instantiatedItems = new List<GameObject>();

        public enum GalleryTab
        {
            Completadas,
            Colgadas,
            Guardadas
        }

        private void Awake()
        {
            if (tabCompletadas != null)
                tabCompletadas.onClick.AddListener(() => SwitchTab(GalleryTab.Completadas));

            if (tabColgadas != null)
                tabColgadas.onClick.AddListener(() => SwitchTab(GalleryTab.Colgadas));

            if (tabGuardadas != null)
                tabGuardadas.onClick.AddListener(() => SwitchTab(GalleryTab.Guardadas));

            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);

            Hide();
        }

        /// <summary>
        /// Shows the gallery panel with the specified tab.
        /// </summary>
        public void Show(GalleryTab tab = GalleryTab.Completadas)
        {
            if (panel != null)
                panel.SetActive(true);
            else
                gameObject.SetActive(true);

            SwitchTab(tab);
        }

        /// <summary>
        /// Hides the gallery panel.
        /// </summary>
        public void Hide()
        {
            if (panel != null)
                panel.SetActive(false);
            else
                gameObject.SetActive(false);
        }

        /// <summary>
        /// Sets the data for the gallery.
        /// </summary>
        public void SetData(List<ArtworkProgress> completed, List<PlacedArtwork> hung, List<ArtworkProgress> saved)
        {
            completedArtworks = completed ?? new List<ArtworkProgress>();
            hungArtworks = hung ?? new List<PlacedArtwork>();
            savedArtworks = saved ?? new List<ArtworkProgress>();

            RefreshCurrentTab();
        }

        /// <summary>
        /// Switches to the specified tab.
        /// </summary>
        public void SwitchTab(GalleryTab tab)
        {
            currentTab = tab;
            UpdateTabIndicators();
            RefreshCurrentTab();
        }

        private void UpdateTabIndicators()
        {
            if (indicatorCompletadas != null)
                indicatorCompletadas.SetActive(currentTab == GalleryTab.Completadas);

            if (indicatorColgadas != null)
                indicatorColgadas.SetActive(currentTab == GalleryTab.Colgadas);

            if (indicatorGuardadas != null)
                indicatorGuardadas.SetActive(currentTab == GalleryTab.Guardadas);
        }

        private void RefreshCurrentTab()
        {
            ClearItems();

            switch (currentTab)
            {
                case GalleryTab.Completadas:
                    PopulateCompletedArtworks();
                    break;
                case GalleryTab.Colgadas:
                    PopulateHungArtworks();
                    break;
                case GalleryTab.Guardadas:
                    PopulateSavedArtworks();
                    break;
            }
        }

        private void ClearItems()
        {
            foreach (var item in instantiatedItems)
            {
                if (item != null)
                    Destroy(item);
            }
            instantiatedItems.Clear();
        }

        private void PopulateCompletedArtworks()
        {
            if (completedArtworks.Count == 0)
            {
                ShowEmptyState("No tienes obras completadas.\n¡Resuelve tu primer puzzle!");
                return;
            }

            HideEmptyState();

            foreach (var artwork in completedArtworks)
            {
                CreateArtworkItem(artwork.artworkId, artwork.GetBestRecord()?.frameTier ?? FrameTier.Madera,
                    canHang: true, canPlay: true);
            }
        }

        private void PopulateHungArtworks()
        {
            if (hungArtworks.Count == 0)
            {
                ShowEmptyState("No tienes obras colgadas.\nCompleta un puzzle y cuélgalo en tu espacio.");
                return;
            }

            HideEmptyState();

            foreach (var placed in hungArtworks)
            {
                CreatePlacedArtworkItem(placed);
            }
        }

        private void PopulateSavedArtworks()
        {
            if (savedArtworks.Count == 0)
            {
                ShowEmptyState("No tienes puzzles guardados.\nPuedes guardar tu progreso durante el juego.");
                return;
            }

            HideEmptyState();

            foreach (var artwork in savedArtworks)
            {
                CreateSavedArtworkItem(artwork);
            }
        }

        private void CreateArtworkItem(string artworkId, FrameTier frameTier, bool canHang, bool canPlay)
        {
            if (artworkItemPrefab == null || contentContainer == null) return;

            var item = Instantiate(artworkItemPrefab, contentContainer);
            instantiatedItems.Add(item);

            var itemController = item.GetComponent<GalleryItemController>();
            if (itemController != null)
            {
                itemController.Setup(artworkId, frameTier, canHang, canPlay);
                itemController.OnSelected += () => OnArtworkSelected?.Invoke(artworkId);
                itemController.OnPlayClicked += () => OnPlayRequested?.Invoke(artworkId);
                itemController.OnHangClicked += () => OnHangRequested?.Invoke(artworkId);
            }
        }

        private void CreatePlacedArtworkItem(PlacedArtwork placed)
        {
            if (artworkItemPrefab == null || contentContainer == null) return;

            var item = Instantiate(artworkItemPrefab, contentContainer);
            instantiatedItems.Add(item);

            var itemController = item.GetComponent<GalleryItemController>();
            if (itemController != null)
            {
                itemController.SetupPlaced(placed);
                itemController.OnSelected += () => OnArtworkSelected?.Invoke(placed.artworkId);
                itemController.OnRelocateClicked += () => OnRelocateRequested?.Invoke(placed.artworkId);
                itemController.OnRemoveClicked += () => OnRemoveRequested?.Invoke(placed.artworkId);
            }
        }

        private void CreateSavedArtworkItem(ArtworkProgress artwork)
        {
            if (artworkItemPrefab == null || contentContainer == null) return;

            var item = Instantiate(artworkItemPrefab, contentContainer);
            instantiatedItems.Add(item);

            var itemController = item.GetComponent<GalleryItemController>();
            if (itemController != null)
            {
                itemController.SetupSaved(artwork);
                itemController.OnSelected += () => OnArtworkSelected?.Invoke(artwork.artworkId);
                itemController.OnPlayClicked += () => OnPlayRequested?.Invoke(artwork.artworkId);
            }
        }

        private void ShowEmptyState(string message)
        {
            if (emptyStateObject != null)
                emptyStateObject.SetActive(true);

            if (emptyStateText != null)
                emptyStateText.text = message;
        }

        private void HideEmptyState()
        {
            if (emptyStateObject != null)
                emptyStateObject.SetActive(false);
        }

        /// <summary>
        /// Gets the count for a specific tab.
        /// </summary>
        public int GetTabCount(GalleryTab tab)
        {
            return tab switch
            {
                GalleryTab.Completadas => completedArtworks.Count,
                GalleryTab.Colgadas => hungArtworks.Count,
                GalleryTab.Guardadas => savedArtworks.Count,
                _ => 0
            };
        }

        private void OnDestroy()
        {
            if (tabCompletadas != null) tabCompletadas.onClick.RemoveAllListeners();
            if (tabColgadas != null) tabColgadas.onClick.RemoveAllListeners();
            if (tabGuardadas != null) tabGuardadas.onClick.RemoveAllListeners();
            if (closeButton != null) closeButton.onClick.RemoveAllListeners();

            ClearItems();
        }
    }
}
