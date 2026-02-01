using System;
using UnityEngine;

namespace ArtUnbound.Gameplay
{
    /// <summary>
    /// Controls the timer for a puzzle session.
    /// </summary>
    public class PuzzleTimerController : MonoBehaviour
    {
        public event Action<float> OnTimerUpdate;
        public event Action OnTimerStarted;
        public event Action OnTimerStopped;
        public event Action OnTimerPaused;
        public event Action OnTimerResumed;

        [SerializeField] private bool startOnAwake = false;

        public float ElapsedTime => elapsedTime;
        public bool IsRunning => isRunning;
        public bool IsPaused => isPaused;

        private float elapsedTime = 0f;
        private bool isRunning = false;
        private bool isPaused = false;

        private void Awake()
        {
            if (startOnAwake)
            {
                StartTimer();
            }
        }

        private void Update()
        {
            if (isRunning && !isPaused)
            {
                elapsedTime += Time.deltaTime;
                OnTimerUpdate?.Invoke(elapsedTime);
            }
        }

        /// <summary>
        /// Starts the timer from zero.
        /// </summary>
        public void StartTimer()
        {
            elapsedTime = 0f;
            isRunning = true;
            isPaused = false;
            OnTimerStarted?.Invoke();
        }

        /// <summary>
        /// Stops the timer completely.
        /// </summary>
        public void StopTimer()
        {
            isRunning = false;
            isPaused = false;
            OnTimerStopped?.Invoke();
        }

        /// <summary>
        /// Pauses the timer without resetting.
        /// </summary>
        public void PauseTimer()
        {
            if (isRunning && !isPaused)
            {
                isPaused = true;
                OnTimerPaused?.Invoke();
            }
        }

        /// <summary>
        /// Resumes the timer from paused state.
        /// </summary>
        public void ResumeTimer()
        {
            if (isRunning && isPaused)
            {
                isPaused = false;
                OnTimerResumed?.Invoke();
            }
        }

        /// <summary>
        /// Toggles pause state.
        /// </summary>
        public void TogglePause()
        {
            if (isPaused)
                ResumeTimer();
            else
                PauseTimer();
        }

        /// <summary>
        /// Resets the timer to zero without stopping.
        /// </summary>
        public void ResetTimer()
        {
            elapsedTime = 0f;
        }

        /// <summary>
        /// Gets the elapsed time in whole seconds (minimum 1 to avoid division by zero).
        /// </summary>
        public int GetElapsedSeconds()
        {
            return Mathf.Max(1, Mathf.FloorToInt(elapsedTime));
        }

        /// <summary>
        /// Gets a formatted time string (MM:SS).
        /// </summary>
        public string GetFormattedTime()
        {
            int totalSeconds = GetElapsedSeconds();
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return $"{minutes:D2}:{seconds:D2}";
        }

        /// <summary>
        /// Gets a formatted time string with milliseconds (MM:SS.mmm).
        /// </summary>
        public string GetFormattedTimeWithMillis()
        {
            int totalSeconds = (int)elapsedTime;
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            int millis = (int)((elapsedTime - totalSeconds) * 1000);
            return $"{minutes:D2}:{seconds:D2}.{millis:D3}";
        }
    }
}
