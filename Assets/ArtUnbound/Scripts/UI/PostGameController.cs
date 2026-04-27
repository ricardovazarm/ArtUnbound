using System;
using ArtUnbound.Data;
using ArtUnbound.Gameplay;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Controls the post-game results panel (RIGHT ZONE).
    /// NEW: Shows only completion message and new record text (time-based).
    /// Appears on the right side when puzzle is completed.
    /// </summary>
    public class PostGameController : MonoBehaviour
    {
        public event Action OnPlaceArtworkRequested;
        public event Action OnReplayRequested;

        [Header("UI References")]
        [SerializeField] private GameObject panel;
        // Completion time is shown by the HUD's frozen timer — no separate text needed here.
        [SerializeField] private TextMeshProUGUI medalText;       // "Congratulations you have earned a Bronze medal!!!"

        [Header("Medal Icon")]
        [Tooltip("Image que muestra la medalla ganada (mismo asset que aparece en NativeGallery).")]
        [SerializeField] private Image medalIcon;
        [SerializeField] private Sprite bronzeMedal;
        [SerializeField] private Sprite silverMedal;
        [SerializeField] private Sprite goldMedal;
        
        [Header("Hang Artwork Instruction")]
        [Tooltip("Text that appears above the painting with instruction to pinch and place. Located in a separate panel (center), not in the PostGame panel (right).")]
        [SerializeField] private GameObject hangInstructionText;  // "Pinch the paint to place it on the wall."

        [Header("Buttons")]
        [SerializeField] private Button replayButton;

        private PuzzleSessionData sessionData;
        private FrameTier awardedFrame;

        private void Awake()
        {
            if (replayButton != null)
                replayButton.onClick.AddListener(OnReplayClicked);

            // Make sure hang instruction is hidden initially
            if (hangInstructionText != null)
                hangInstructionText.SetActive(false);

            Hide();
        }

        private int lastWallCount = -1;

        /// <summary>
        /// Shows the results screen with the given data.
        /// wallCount: number of walls detected (0 = none). Used for on-device feedback when console is unavailable.
        /// </summary>
        public void ShowResults(PuzzleSessionData data, FrameTier frame, int wallCount = -1)
        {
            sessionData = data;
            awardedFrame = frame;
            lastWallCount = wallCount;

            UpdateUI();
            Show();
            
            // Automatically enable artwork hanging mode if walls were detected
            if (lastWallCount > 0)
            {
                Debug.Log($"[PostGame] Walls detected ({lastWallCount}), automatically enabling artwork hanging mode");
                OnPlaceArtworkRequested?.Invoke();
            }
            else
            {
                Debug.Log("[PostGame] No walls detected, artwork hanging mode NOT enabled");
            }
        }

        private void UpdateUI()
        {
            string medalName = GetMedalDisplayName(awardedFrame);

            if (medalText != null)
                medalText.text = $"Congratulations you have earned a {medalName} medal!!!";

            if (medalIcon != null)
            {
                Sprite sprite = awardedFrame switch
                {
                    FrameTier.Bronce => bronzeMedal,
                    FrameTier.Plata  => silverMedal,
                    _                => goldMedal, // Oro
                };
                medalIcon.sprite = sprite;
                medalIcon.enabled = sprite != null;
            }

            // Show/hide hang instruction text based on wall detection
            if (hangInstructionText != null)
            {
                bool hasWalls = lastWallCount > 0;
                hangInstructionText.SetActive(hasWalls);
            }
        }

        private static string GetMedalDisplayName(FrameTier tier)
        {
            return tier switch
            {
                FrameTier.Bronce => "Bronze",
                FrameTier.Plata => "Silver",
                FrameTier.Oro => "Gold",
                _ => "Bronze"
            };
        }

        /// <summary>
        /// Disables the replay button while the frame is in hanging mode so the controller
        /// trigger cannot accidentally click it while the user is trying to grab the frame.
        /// </summary>
        public void SetHangingMode(bool isHanging)
        {
            if (replayButton != null)
                replayButton.interactable = !isHanging;
        }

        public void Show()
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            if (panel != null)
                panel.SetActive(true);

            // Update UI to refresh instruction text based on lastWallCount
            UpdateUI();
        }

        public void Hide()
        {
            if (panel != null)
                panel.SetActive(false);
            else
                gameObject.SetActive(false);
            
            // Also hide the hang instruction text when hiding the panel
            if (hangInstructionText != null)
                hangInstructionText.SetActive(false);
        }

        private void OnReplayClicked()
        {
            if (ArtUnbound.Feedback.AudioManager.Instance != null)
                ArtUnbound.Feedback.AudioManager.Instance.PlayButtonClick();
            OnReplayRequested?.Invoke();
            Hide();
        }

        /// <summary>
        /// Gets the current session data.
        /// </summary>
        public PuzzleSessionData GetSessionData() => sessionData;

        /// <summary>
        /// Gets the awarded frame tier.
        /// </summary>
        public FrameTier GetAwardedFrame() => awardedFrame;

        private void OnDestroy()
        {
            if (replayButton != null)
                replayButton.onClick.RemoveListener(OnReplayClicked);
        }
    }
}
