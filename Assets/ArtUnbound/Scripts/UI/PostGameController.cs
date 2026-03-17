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

        [Header("Buttons")]
        [SerializeField] private Button placeButton;  // Hidden for now - behavior to change later
        [SerializeField] private Button replayButton;

        private PuzzleSessionData sessionData;
        private int completionTime;
        private int previousBestTime;
        private FrameTier awardedFrame;
        private bool isNewRecord;

        private void Awake()
        {
            if (placeButton != null)
            {
                placeButton.onClick.AddListener(OnPlaceArtworkClicked);
                placeButton.gameObject.SetActive(false);  // Hidden for now
            }

            if (replayButton != null)
                replayButton.onClick.AddListener(OnReplayClicked);

            Hide();
        }

        /// <summary>
        /// Shows the results screen with the given data.
        /// NEW: Only shows completion message and new record text (based on time).
        /// </summary>
        public void ShowResults(PuzzleSessionData data, int timeSec, int prevBestTime, FrameTier frame, bool newRecord = false)
        {
            Debug.Log($"[PostGameController] ShowResults called - Time: {timeSec}s, PrevBest: {prevBestTime}s, Frame: {frame}, NewRecord: {newRecord}");
            
            sessionData = data;
            completionTime = timeSec;
            previousBestTime = prevBestTime;
            awardedFrame = frame;
            isNewRecord = newRecord;

            UpdateUI();
            Show();
            
            Debug.Log($"[PostGameController] Panel shown. GameObject active: {gameObject.activeInHierarchy}");
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
        }

        public void Hide()
        {
            if (panel != null)
                panel.SetActive(false);
            else
                gameObject.SetActive(false);
        }

        private void OnPlaceArtworkClicked()
        {
            if (ArtUnbound.Feedback.AudioManager.Instance != null)
                ArtUnbound.Feedback.AudioManager.Instance.PlayButtonClick();
            OnPlaceArtworkRequested?.Invoke();
            Hide();
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
            if (placeButton != null)
                placeButton.onClick.RemoveListener(OnPlaceArtworkClicked);

            if (replayButton != null)
                replayButton.onClick.RemoveListener(OnReplayClicked);
        }
    }
}
