using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Controls the piece count (difficulty) selector UI.
    /// </summary>
    public class PieceCountSelectorController : MonoBehaviour
    {
        public event Action<int> OnCountSelected;

        [Header("UI References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI titleText;

        [Header("Buttons")]
        [SerializeField] private Button btn64;
        [SerializeField] private Button btn144;
        [SerializeField] private Button btn256;
        [SerializeField] private Button btn512;
        [SerializeField] private Button cancelButton;

        [Header("Button Labels")]
        [SerializeField] private TextMeshProUGUI label64;
        [SerializeField] private TextMeshProUGUI label144;
        [SerializeField] private TextMeshProUGUI label256;
        [SerializeField] private TextMeshProUGUI label512;

        [Header("Configuration")]
        [SerializeField] private int[] availableCounts = { 64, 144, 256, 512 };

        public int SelectedCount { get; private set; } = 64;

        private void Awake()
        {
            if (btn64 != null)
                btn64.onClick.AddListener(() => SelectCount(64));

            if (btn144 != null)
                btn144.onClick.AddListener(() => SelectCount(144));

            if (btn256 != null)
                btn256.onClick.AddListener(() => SelectCount(256));

            if (btn512 != null)
                btn512.onClick.AddListener(() => SelectCount(512));

            if (cancelButton != null)
                cancelButton.onClick.AddListener(Hide);

            UpdateLabels();
            Hide();
        }

        /// <summary>
        /// Shows the selector panel.
        /// </summary>
        public void ShowSelector()
        {
            Show();
        }

        /// <summary>
        /// Shows the selector with a specific title.
        /// </summary>
        public void ShowSelector(string title)
        {
            if (titleText != null)
                titleText.text = title;

            Show();
        }

        /// <summary>
        /// Selects a piece count and fires the event.
        /// </summary>
        public void SelectCount(int count)
        {
            SelectedCount = count;
            OnCountSelected?.Invoke(count);
            Hide();
        }

        /// <summary>
        /// Gets the selected count.
        /// </summary>
        public int GetSelectedCount()
        {
            return SelectedCount;
        }

        /// <summary>
        /// Enables or disables specific piece count options.
        /// </summary>
        public void SetOptionEnabled(int count, bool enabled)
        {
            Button btn = GetButtonForCount(count);
            if (btn != null)
                btn.interactable = enabled;
        }

        /// <summary>
        /// Sets all options as enabled or disabled.
        /// </summary>
        public void SetAllOptionsEnabled(bool enabled)
        {
            if (btn64 != null) btn64.interactable = enabled;
            if (btn144 != null) btn144.interactable = enabled;
            if (btn256 != null) btn256.interactable = enabled;
            if (btn512 != null) btn512.interactable = enabled;
        }

        private Button GetButtonForCount(int count)
        {
            return count switch
            {
                64 => btn64,
                144 => btn144,
                256 => btn256,
                512 => btn512,
                _ => null
            };
        }

        private void UpdateLabels()
        {
            if (label64 != null) label64.text = "64 piezas\nFácil";
            if (label144 != null) label144.text = "144 piezas\nNormal";
            if (label256 != null) label256.text = "256 piezas\nDifícil";
            if (label512 != null) label512.text = "512 piezas\nExperto";
        }

        /// <summary>
        /// Gets the difficulty label for a piece count.
        /// </summary>
        public static string GetDifficultyLabel(int count)
        {
            return count switch
            {
                64 => "Fácil",
                144 => "Normal",
                256 => "Difícil",
                512 => "Experto",
                _ => "Personalizado"
            };
        }

        /// <summary>
        /// Gets the estimated time for a piece count (in minutes).
        /// </summary>
        public static int GetEstimatedTime(int count)
        {
            return count switch
            {
                64 => 5,
                144 => 15,
                256 => 30,
                512 => 60,
                _ => count / 10
            };
        }

        public void Show()
        {
            if (panel != null)
                panel.SetActive(true);
            else
                gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (panel != null)
                panel.SetActive(false);
            else
                gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (btn64 != null) btn64.onClick.RemoveAllListeners();
            if (btn144 != null) btn144.onClick.RemoveAllListeners();
            if (btn256 != null) btn256.onClick.RemoveAllListeners();
            if (btn512 != null) btn512.onClick.RemoveAllListeners();
            if (cancelButton != null) cancelButton.onClick.RemoveAllListeners();
        }
    }
}
