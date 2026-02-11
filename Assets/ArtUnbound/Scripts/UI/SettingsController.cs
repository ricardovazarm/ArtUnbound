using System;
using ArtUnbound.Data;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Controls the settings/preferences panel.
    /// </summary>
    public class SettingsController : MonoBehaviour
    {
        public event Action<GameSettings> OnSettingsChanged;
        public event Action OnCloseRequested;

        [Header("Panel")]
        [SerializeField] private GameObject panel;

        [Header("Audio Settings")]
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private TextMeshProUGUI sfxVolumeText;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private TextMeshProUGUI musicVolumeText;

        [Header("Gameplay Settings")]
        [SerializeField] private Toggle helpModeDefaultToggle;
        [SerializeField] private Toggle showOnboardingToggle;
        [SerializeField] private TMP_Dropdown defaultPieceCountDropdown;

        [Header("Visual Settings")]
        [SerializeField] private Toggle showGridToggle;
        [SerializeField] private Toggle highContrastToggle;

        [Header("Controls")]
        [SerializeField] private Slider pieceSnapDistanceSlider;
        [SerializeField] private TextMeshProUGUI snapDistanceText;

        [Header("Navigation")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button resetButton;

        [Header("Version Info")]
        [SerializeField] private TextMeshProUGUI versionText;

        private GameSettings currentSettings;
        private bool isUpdating = false;

        private void Awake()
        {
            SetupListeners();
            Hide();
        }

        private void SetupListeners()
        {
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);

            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

            if (helpModeDefaultToggle != null)
                helpModeDefaultToggle.onValueChanged.AddListener(OnHelpModeDefaultChanged);

            if (showOnboardingToggle != null)
                showOnboardingToggle.onValueChanged.AddListener(OnShowOnboardingChanged);

            if (defaultPieceCountDropdown != null)
                defaultPieceCountDropdown.onValueChanged.AddListener(OnDefaultPieceCountChanged);

            if (showGridToggle != null)
                showGridToggle.onValueChanged.AddListener(OnShowGridChanged);

            if (highContrastToggle != null)
                highContrastToggle.onValueChanged.AddListener(OnHighContrastChanged);

            if (pieceSnapDistanceSlider != null)
                pieceSnapDistanceSlider.onValueChanged.AddListener(OnSnapDistanceChanged);

            if (closeButton != null)
                closeButton.onClick.AddListener(() => OnCloseRequested?.Invoke());

            if (resetButton != null)
                resetButton.onClick.AddListener(ResetToDefaults);
        }

        /// <summary>
        /// Shows the settings panel with the given settings.
        /// </summary>
        public void ShowSettings(GameSettings settings)
        {
            currentSettings = settings ?? new GameSettings();
            UpdateUI();
            Show();
        }

        /// <summary>
        /// Gets the current settings.
        /// </summary>
        public GameSettings GetCurrentSettings()
        {
            return currentSettings;
        }

        private void UpdateUI()
        {
            isUpdating = true;

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = currentSettings.sfxVolume;
                UpdateVolumeText(sfxVolumeText, currentSettings.sfxVolume);
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = currentSettings.musicVolume;
                UpdateVolumeText(musicVolumeText, currentSettings.musicVolume);
            }

            if (helpModeDefaultToggle != null)
                helpModeDefaultToggle.isOn = currentSettings.helpModeDefault;

            if (showOnboardingToggle != null)
                showOnboardingToggle.isOn = currentSettings.showOnboarding;

            if (defaultPieceCountDropdown != null)
            {
                int index = GetPieceCountDropdownIndex(currentSettings.defaultPieceCount);
                defaultPieceCountDropdown.value = index;
            }

            if (showGridToggle != null)
                showGridToggle.isOn = currentSettings.showGrid;

            if (highContrastToggle != null)
                highContrastToggle.isOn = currentSettings.highContrast;

            if (pieceSnapDistanceSlider != null)
            {
                pieceSnapDistanceSlider.value = currentSettings.pieceSnapDistance;
                UpdateSnapDistanceText();
            }

            if (versionText != null)
                versionText.text = $"v{Application.version}";

            isUpdating = false;
        }

        private void UpdateVolumeText(TextMeshProUGUI text, float value)
        {
            if (text != null)
                text.text = $"{Mathf.RoundToInt(value * 100)}%";
        }

        private void UpdateSnapDistanceText()
        {
            if (snapDistanceText != null)
                snapDistanceText.text = $"{currentSettings.pieceSnapDistance:F2}m";
        }

        private int GetPieceCountDropdownIndex(int pieceCount)
        {
            return pieceCount switch
            {
                64 => 0,
                144 => 1,
                256 => 2,
                512 => 3,
                _ => 0
            };
        }

        private int GetPieceCountFromDropdownIndex(int index)
        {
            return index switch
            {
                0 => 64,
                1 => 144,
                2 => 256,
                3 => 512,
                _ => 64
            };
        }

        private void OnSfxVolumeChanged(float value)
        {
            if (isUpdating) return;

            currentSettings.sfxVolume = value;
            UpdateVolumeText(sfxVolumeText, value);
            NotifySettingsChanged();
        }

        private void OnMusicVolumeChanged(float value)
        {
            if (isUpdating) return;

            currentSettings.musicVolume = value;
            UpdateVolumeText(musicVolumeText, value);
            NotifySettingsChanged();
        }

        private void OnHelpModeDefaultChanged(bool value)
        {
            if (isUpdating) return;

            currentSettings.helpModeDefault = value;
            NotifySettingsChanged();
        }

        private void OnShowOnboardingChanged(bool value)
        {
            if (isUpdating) return;

            currentSettings.showOnboarding = value;
            NotifySettingsChanged();
        }

        private void OnDefaultPieceCountChanged(int index)
        {
            if (isUpdating) return;

            currentSettings.defaultPieceCount = GetPieceCountFromDropdownIndex(index);
            NotifySettingsChanged();
        }

        private void OnShowGridChanged(bool value)
        {
            if (isUpdating) return;

            currentSettings.showGrid = value;
            NotifySettingsChanged();
        }

        private void OnHighContrastChanged(bool value)
        {
            if (isUpdating) return;

            currentSettings.highContrast = value;
            NotifySettingsChanged();
        }

        private void OnSnapDistanceChanged(float value)
        {
            if (isUpdating) return;

            currentSettings.pieceSnapDistance = value;
            UpdateSnapDistanceText();
            NotifySettingsChanged();
        }

        private void NotifySettingsChanged()
        {
            OnSettingsChanged?.Invoke(currentSettings);
        }

        /// <summary>
        /// Resets all settings to their default values.
        /// </summary>
        public void ResetToDefaults()
        {
            currentSettings = new GameSettings();
            UpdateUI();
            NotifySettingsChanged();
        }

        public void Show()
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            if (panel != null)
                panel.SetActive(true);
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
            if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.RemoveAllListeners();
            if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.RemoveAllListeners();
            if (helpModeDefaultToggle != null) helpModeDefaultToggle.onValueChanged.RemoveAllListeners();
            if (showOnboardingToggle != null) showOnboardingToggle.onValueChanged.RemoveAllListeners();
            if (defaultPieceCountDropdown != null) defaultPieceCountDropdown.onValueChanged.RemoveAllListeners();
            if (showGridToggle != null) showGridToggle.onValueChanged.RemoveAllListeners();
            if (highContrastToggle != null) highContrastToggle.onValueChanged.RemoveAllListeners();
            if (pieceSnapDistanceSlider != null) pieceSnapDistanceSlider.onValueChanged.RemoveAllListeners();
            if (closeButton != null) closeButton.onClick.RemoveAllListeners();
            if (resetButton != null) resetButton.onClick.RemoveAllListeners();
        }
    }
}
