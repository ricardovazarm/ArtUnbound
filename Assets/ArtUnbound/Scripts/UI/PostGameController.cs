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
        [SerializeField] private TextMeshProUGUI completionText;  // "Puzzle Complete!"
        [SerializeField] private TextMeshProUGUI timeText;         // "Completed in: 01:23:02"
        [SerializeField] private TextMeshProUGUI frameText;       // "Bronze frame earned!"
        [SerializeField] private TextMeshProUGUI newRecordText;   // Kept for future use - currently hidden
        [SerializeField] private TextMeshProUGUI wallStatusText;  // "Paredes detectadas: Sí/No" (below board)
        
        [Header("Hang Artwork Instruction")]
        [Tooltip("Text that appears above the painting with instruction to pinch and place. Located in a separate panel (center), not in the PostGame panel (right).")]
        [SerializeField] private GameObject hangInstructionText;  // "Pinch the paint to place it on the wall."

        [Header("Buttons")]
        [SerializeField] private Button replayButton;

        private PuzzleSessionData sessionData;
        private int completionTime;
        private int previousBestTime;
        private FrameTier awardedFrame;
        private bool isNewRecord;

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
        public void ShowResults(PuzzleSessionData data, int timeSec, int prevBestTime, FrameTier frame, bool newRecord = false, int wallCount = -1)
        {
            sessionData = data;
            completionTime = timeSec;
            previousBestTime = prevBestTime;
            awardedFrame = frame;
            isNewRecord = newRecord;
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
            if (completionText != null)
                completionText.text = "Puzzle Complete!";

            if (timeText != null)
                timeText.text = $"Completed in: {FormatTime(completionTime)}";

            if (frameText != null)
                frameText.text = $"{GetFrameDisplayName(awardedFrame)} frame earned!";

            // New record - functionality kept, text hidden for now
            if (newRecordText != null)
                newRecordText.gameObject.SetActive(false);

            // Wall detection status — oculto (era texto de debug)
            if (wallStatusText != null)
                wallStatusText.gameObject.SetActive(false);
            
        // Show/hide hang instruction text based on wall detection
        if (hangInstructionText != null)
        {
            bool hasWalls = lastWallCount > 0;
            hangInstructionText.SetActive(hasWalls);
            
            // Check if the text is actually visible (parent might be inactive)
            bool isActiveInHierarchy = hangInstructionText.activeInHierarchy;
            bool isActiveSelf = hangInstructionText.activeSelf;
            bool parentActive = hangInstructionText.transform.parent != null && hangInstructionText.transform.parent.gameObject.activeInHierarchy;
            
            Debug.Log($"[PostGame] Hang instruction text - SetActive({hasWalls}), activeSelf: {isActiveSelf}, activeInHierarchy: {isActiveInHierarchy}, parent active: {parentActive}, walls: {lastWallCount}");
        }
        else
        {
            Debug.LogWarning("[PostGame] hangInstructionText is null! Please assign it in the Inspector.");
        }
        }

        private void EnsureWallStatusTextExists()
        {
            if (wallStatusText != null) return;
            if (panel == null) return;

            var go = new GameObject("WallStatusText");
            go.transform.SetParent(panel.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -95f);
            rect.sizeDelta = new Vector2(400f, 28f);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 18;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.9f, 0.9f, 0.9f, 0.9f);
            tmp.text = "Paredes: —";
            wallStatusText = tmp;
        }

        private string FormatTime(int totalSeconds)
        {
            int hours = totalSeconds / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            int seconds = totalSeconds % 60;
            if (hours > 0)
                return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
            return $"{minutes:D2}:{seconds:D2}";
        }

        private static string GetFrameDisplayName(FrameTier tier)
        {
            return tier switch
            {
                FrameTier.Bronce => "Bronze",
                FrameTier.Plata => "Silver",
                FrameTier.Oro => "Gold",
                FrameTier.Platinum => "Platinum",
                _ => "Bronze"
            };
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
        /// Gets the completion time in seconds.
        /// </summary>
        public int GetCompletionTime() => completionTime;

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
