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
        private List<ArtworkDefinition> availableArtworks = new List<ArtworkDefinition>();
        private List<GameObject> instantiatedItems = new List<GameObject>();

        public enum GalleryTab
        {
            Completadas,
            Colgadas,
            Guardadas,
            Disponibles
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
        public void Show(GalleryTab? tab = null)
        {
            Debug.Log($"[GalleryPanelController] Show called. Requested tab: {tab}");

            // Ensure the script holder is active (Parent)
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            // Activate the assigned panel (Child/Self)
            if (panel != null)
            {
                panel.SetActive(true);
            }

            if (tab.HasValue)
            {
                SwitchTab(tab.Value);
            }
            else
            {
                // Ensure the current tab is refreshed/displayed
                RefreshCurrentTab();
            }
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
        public void SetData(List<ArtworkProgress> completed, List<PlacedArtwork> hung, List<ArtworkProgress> saved, List<ArtworkDefinition> available = null)
        {
            Debug.Log($"[GalleryPanelController] SetData called. Completed: {completed?.Count ?? 0}, Hung: {hung?.Count ?? 0}, Saved: {saved?.Count ?? 0}, Available: {available?.Count ?? 0}");
            completedArtworks = completed ?? new List<ArtworkProgress>();
            hungArtworks = hung ?? new List<PlacedArtwork>();
            savedArtworks = saved ?? new List<ArtworkProgress>();
            availableArtworks = available ?? new List<ArtworkDefinition>();

            // If no completed artworks, default to Available tab to let user pick one
            if (completedArtworks.Count == 0 && availableArtworks.Count > 0)
            {
                Debug.Log("[GalleryPanelController] No completed artworks found. Defaulting to 'Disponibles' tab.");
                currentTab = GalleryTab.Disponibles;
            }

            RefreshCurrentTab();
        }

        /// <summary>
        /// Switches to the specified tab.
        /// </summary>
        public void SwitchTab(GalleryTab tab)
        {
            currentTab = tab;
            UpdateTabIndicators();
            UpdateTabVisibility();
            RefreshCurrentTab();
        }

        private void UpdateTabVisibility()
        {
            bool isSelectionMode = currentTab == GalleryTab.Disponibles;

            // If we are in "Available/Play" mode, hide the navigation tabs (The Palette)
            // to make it feel like a distinct screen.
            // If we are in "My Gallery" mode, show them.

            bool showTabs = !isSelectionMode;

            if (tabCompletadas != null) tabCompletadas.gameObject.SetActive(showTabs);
            if (tabColgadas != null) tabColgadas.gameObject.SetActive(showTabs);
            if (tabGuardadas != null) tabGuardadas.gameObject.SetActive(showTabs);

            // Indicators should also be hidden if tabs are hidden
            if (!showTabs)
            {
                if (indicatorCompletadas != null) indicatorCompletadas.SetActive(false);
                if (indicatorColgadas != null) indicatorColgadas.SetActive(false);
                if (indicatorGuardadas != null) indicatorGuardadas.SetActive(false);
            }
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
            Debug.Log($"[GalleryPanelController] RefreshCurrentTab: {currentTab}");
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
                case GalleryTab.Disponibles:
                    PopulateAvailableArtworks();
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
                Debug.Log("[GalleryPanelController] PopulateCompletedArtworks: No artworks. Showing Empty State.");
                ShowEmptyState("No tienes obras completadas.\n¡Resuelve tu primer puzzle!");
                return;
            }

            Debug.Log($"[GalleryPanelController] PopulateCompletedArtworks: Creating {completedArtworks.Count} items.");
            HideEmptyState();

            foreach (var artwork in completedArtworks)
            {
                var def = availableArtworks.Find(a => a.artworkId == artwork.artworkId);
                CreateArtworkItem(artwork.artworkId, artwork.GetBestRecord()?.frameTier ?? FrameTier.Madera,
                    canHang: true, canPlay: true, def);
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

        private void PopulateAvailableArtworks()
        {
            if (availableArtworks.Count == 0)
            {
                ShowEmptyState("No hay obras disponibles en el catálogo.");
                return;
            }

            Debug.Log($"[GalleryPanelController] PopulateAvailableArtworks: Creating {availableArtworks.Count} items.");
            HideEmptyState();

            foreach (var artwork in availableArtworks)
            {
                CreateArtworkItem(artwork.artworkId, FrameTier.Madera,
                    canHang: false, canPlay: true, artwork);
            }
        }

        private void CreateArtworkItem(string artworkId, FrameTier frameTier, bool canHang, bool canPlay, ArtworkDefinition artwork = null)
        {
            if (artworkItemPrefab == null || contentContainer == null) return;

            var item = Instantiate(artworkItemPrefab, contentContainer);
            item.transform.localScale = Vector3.one;
            instantiatedItems.Add(item);

            var itemController = item.GetComponent<GalleryItemController>();
            if (itemController != null)
            {
                itemController.Setup(artworkId, frameTier, canHang, canPlay);

                if (artwork != null)
                {
                    itemController.SetThumbnail(artwork.thumbnail);
                }

                itemController.OnSelected += () => OnArtworkSelected?.Invoke(artworkId);
            }
        }

        private void CreatePlacedArtworkItem(PlacedArtwork placed)
        {
            if (artworkItemPrefab == null || contentContainer == null) return;

            var item = Instantiate(artworkItemPrefab, contentContainer);
            item.transform.localScale = Vector3.one;
            instantiatedItems.Add(item);

            var itemController = item.GetComponent<GalleryItemController>();
            if (itemController != null)
            {
                itemController.SetupPlaced(placed);
                itemController.OnSelected += () => OnArtworkSelected?.Invoke(placed.artworkId);
            }
        }

        private void CreateSavedArtworkItem(ArtworkProgress artwork)
        {
            if (artworkItemPrefab == null || contentContainer == null) return;

            var item = Instantiate(artworkItemPrefab, contentContainer);
            item.transform.localScale = Vector3.one;
            instantiatedItems.Add(item);

            var itemController = item.GetComponent<GalleryItemController>();
            if (itemController != null)
            {
                itemController.SetupSaved(artwork);
                itemController.OnSelected += () => OnArtworkSelected?.Invoke(artwork.artworkId);
            }
        }

        private void ShowEmptyState(string message)
        {
            Debug.Log("[GalleryPanelController] ShowEmptyState called.");
            if (emptyStateObject != null)
            {
                emptyStateObject.SetActive(true);
                Debug.Log($"[GalleryPanelController] EmptyState object active. Message: {message}");
            }
            else
            {
                Debug.LogError("[GalleryPanelController] EmptyStateObject reference is missing!");
            }

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
                GalleryTab.Disponibles => availableArtworks.Count,
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
