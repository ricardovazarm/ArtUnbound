using System;
using UnityEngine;
using UnityEngine.UI;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Mini-tabs controller (Paquetes / Bundles) at the top of the StoreView.
    /// Mirrors the BottomNav tab pattern from NativeGalleryController:
    /// each button toggles its target view and updates the colors so the
    /// active tab uses tabActiveColor and the inactive one tabInactiveColor.
    ///
    /// HIERARCHY SETUP IN UNITY:
    ///   TopTabs (HorizontalLayoutGroup)
    ///     ├── BtnPaquetes (Button)
    ///     └── BtnBundles  (Button)
    ///   PaquetesView (GameObject, active by default)
    ///   BundlesView  (GameObject, inactive by default)
    /// </summary>
    public class StoreTabsController : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button btnPaquetes;
        [SerializeField] private Button btnBundles;

        [Header("Views")]
        [SerializeField] private GameObject paquetesView;
        [SerializeField] private GameObject bundlesView;

        [Header("Colors")]
        [Tooltip("Color del tab activo (fondo opaco). El tab inactivo queda transparente.")]
        [SerializeField] private Color tabActiveColor = new Color32(0x89, 0x6C, 0x4A, 0xFF); // #896C4A

        /// <summary>Fires when the active tab changes. true = Paquetes, false = Bundles.</summary>
        public event Action<bool> OnTabChanged;

        private bool _isPaquetes = true;

        private void Awake()
        {
            if (btnPaquetes != null) btnPaquetes.onClick.AddListener(() => SwitchTab(true));
            if (btnBundles  != null) btnBundles.onClick.AddListener(()  => SwitchTab(false));
        }

        private void OnDestroy()
        {
            if (btnPaquetes != null) btnPaquetes.onClick.RemoveAllListeners();
            if (btnBundles  != null) btnBundles.onClick.RemoveAllListeners();
        }

        private void OnEnable()
        {
            ApplyTabState();
        }

        public void SwitchTab(bool isPaquetes)
        {
            if (_isPaquetes == isPaquetes) return;
            _isPaquetes = isPaquetes;
            ApplyTabState();
            OnTabChanged?.Invoke(_isPaquetes);
        }

        private void ApplyTabState()
        {
            if (paquetesView != null) paquetesView.SetActive(_isPaquetes);
            if (bundlesView  != null) bundlesView.SetActive(!_isPaquetes);
            SetTabColor(btnPaquetes, _isPaquetes);
            SetTabColor(btnBundles,  !_isPaquetes);
        }

        private void SetTabColor(Button btn, bool active)
        {
            if (btn == null) return;
            Color target = active ? tabActiveColor : UIButtonTheme.RestColor;
            var colors = btn.colors;
            // Update both normal and selected — clicking a tab makes it the EventSystem's
            // selected GameObject, which would otherwise repaint with selectedColor and
            // erase the active highlight.
            colors.normalColor = target;
            colors.selectedColor = target;
            btn.colors = colors;

            // Force the targetGraphic to repaint to the new color now. Unity's
            // Selectable.DoStateTransition only fires when state changes (hover, click),
            // so a pure ColorBlock update otherwise leaves the previous render in place.
            var graphic = btn.targetGraphic as Graphic;
            if (graphic != null)
                graphic.color = target;
        }
    }
}
