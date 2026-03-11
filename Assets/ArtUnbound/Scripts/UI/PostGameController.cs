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
        [SerializeField] private TextMeshProUGUI completionText;  // "¡Puzzle Completado!"
        [SerializeField] private TextMeshProUGUI newRecordText;    // Shows "¡Nuevo Récord! XX:XX" or hidden if not a record

        [Header("Buttons")]
        [SerializeField] private Button placeButton;
        [SerializeField] private Button replayButton;

        private PuzzleSessionData sessionData;
        private int completionTime;
        private int previousBestTime;
        private FrameTier awardedFrame;
        private bool isNewRecord;

        private void Awake()
        {
            if (placeButton != null)
                placeButton.onClick.AddListener(OnPlaceArtworkClicked);

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
            // Show completion message
            if (completionText != null)
                completionText.text = "¡Puzzle Completado!";

            // Show new record text with time if it's a new record
            if (newRecordText != null)
            {
                if (isNewRecord)
                {
                    string timeFormatted = FormatTime(completionTime);
                    newRecordText.text = $"¡Nuevo Récord!\n{timeFormatted}";
                    newRecordText.gameObject.SetActive(true);
                }
                else
                {
                    newRecordText.gameObject.SetActive(false);
                }
            }
        }

        private string FormatTime(int totalSeconds)
        {
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return $"{minutes:D2}:{seconds:D2}";
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
            OnPlaceArtworkRequested?.Invoke();
            Hide();
        }

        private void OnReplayClicked()
        {
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
