using System;
using System.IO;
using ArtUnbound.Data;
using UnityEngine;

namespace ArtUnbound.Services
{
    /// <summary>
    /// Service for saving and loading game data.
    /// </summary>
    public class SaveDataService
    {
        private const string SAVE_FILE_NAME = "save.json";
        private const string BACKUP_FILE_NAME = "save_backup.json";
        private const string SESSION_FILE_NAME = "session.json";

        private readonly string savePath;
        private readonly string backupPath;
        private readonly string sessionPath;

        private SaveData cachedData;
        private bool isDirty = false;

        public event Action<SaveData> OnDataLoaded;
        public event Action<SaveData> OnDataSaved;

        public SaveDataService()
        {
            savePath = Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
            backupPath = Path.Combine(Application.persistentDataPath, BACKUP_FILE_NAME);
            sessionPath = Path.Combine(Application.persistentDataPath, SESSION_FILE_NAME);
        }

        /// <summary>
        /// Loads the save data from disk.
        /// </summary>
        public SaveData Load()
        {
            try
            {
                if (File.Exists(savePath))
                {
                    string json = File.ReadAllText(savePath);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        cachedData = JsonUtility.FromJson<SaveData>(json);
                        if (cachedData != null)
                        {
                            OnDataLoaded?.Invoke(cachedData);
                            return cachedData;
                        }
                    }
                }

                // Try backup if main save fails
                if (File.Exists(backupPath))
                {
                    string backupJson = File.ReadAllText(backupPath);
                    if (!string.IsNullOrWhiteSpace(backupJson))
                    {
                        cachedData = JsonUtility.FromJson<SaveData>(backupJson);
                        if (cachedData != null)
                        {
                            Debug.LogWarning("Loaded from backup save file.");
                            OnDataLoaded?.Invoke(cachedData);
                            return cachedData;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading save data: {e.Message}");
            }

            cachedData = new SaveData();
            OnDataLoaded?.Invoke(cachedData);
            return cachedData;
        }

        /// <summary>
        /// Saves the data to disk.
        /// </summary>
        public void Save(SaveData data)
        {
            if (data == null) return;

            try
            {
                // Create backup of existing save
                if (File.Exists(savePath))
                {
                    File.Copy(savePath, backupPath, true);
                }

                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(savePath, json);

                cachedData = data;
                isDirty = false;

                OnDataSaved?.Invoke(data);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error saving data: {e.Message}");
            }
        }

        /// <summary>
        /// Gets the cached data without loading from disk.
        /// </summary>
        public SaveData GetCachedData()
        {
            return cachedData ?? Load();
        }

        /// <summary>
        /// Marks the data as dirty (needs saving).
        /// </summary>
        public void MarkDirty()
        {
            isDirty = true;
        }

        /// <summary>
        /// Saves if data is dirty.
        /// </summary>
        public void SaveIfDirty()
        {
            if (isDirty && cachedData != null)
            {
                Save(cachedData);
            }
        }

        /// <summary>
        /// Saves a puzzle session to a separate file.
        /// </summary>
        public void SaveSession(PuzzleSessionData session)
        {
            if (session == null) return;

            try
            {
                string json = JsonUtility.ToJson(session, true);
                File.WriteAllText(sessionPath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error saving session: {e.Message}");
            }
        }

        /// <summary>
        /// Loads a puzzle session from the separate file.
        /// </summary>
        public PuzzleSessionData LoadSession()
        {
            try
            {
                if (File.Exists(sessionPath))
                {
                    string json = File.ReadAllText(sessionPath);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        return JsonUtility.FromJson<PuzzleSessionData>(json);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading session: {e.Message}");
            }

            return null;
        }

        /// <summary>
        /// Clears the saved session.
        /// </summary>
        public void ClearSession()
        {
            try
            {
                if (File.Exists(sessionPath))
                {
                    File.Delete(sessionPath);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error clearing session: {e.Message}");
            }
        }

        /// <summary>
        /// Checks if a session exists.
        /// </summary>
        public bool HasSavedSession()
        {
            return File.Exists(sessionPath);
        }

        /// <summary>
        /// Updates artwork progress and saves.
        /// </summary>
        public void UpdateArtworkProgress(string artworkId, int pieceCount, int score, int timeSec, FrameTier frameTier)
        {
            var data = GetCachedData();
            var progress = data.GetOrCreateProgress(artworkId);

            bool isNewRecord = progress.RecordCompletion(pieceCount, timeSec, score, frameTier);

            if (isNewRecord)
            {
                Debug.Log($"New record for {artworkId} at {pieceCount} pieces: {score} points");
            }

            Save(data);
        }

        /// <summary>
        /// Adds a placed artwork and saves.
        /// </summary>
        public void AddPlacedArtwork(PlacedArtwork placed)
        {
            var data = GetCachedData();
            data.AddPlacedArtwork(placed);
            Save(data);
        }

        /// <summary>
        /// Removes a placed artwork and saves.
        /// </summary>
        public void RemovePlacedArtwork(string artworkId)
        {
            var data = GetCachedData();
            data.RemovePlacedArtwork(artworkId);
            Save(data);
        }

        /// <summary>
        /// Updates game settings and saves.
        /// </summary>
        public void UpdateSettings(GameSettings settings)
        {
            var data = GetCachedData();
            data.settings = settings;
            Save(data);
        }

        /// <summary>
        /// Marks onboarding as completed.
        /// </summary>
        public void CompleteOnboarding()
        {
            var data = GetCachedData();
            data.onboardingCompleted = true;
            Save(data);
        }

        /// <summary>
        /// Deletes all save data.
        /// </summary>
        public void DeleteAllData()
        {
            try
            {
                if (File.Exists(savePath))
                    File.Delete(savePath);

                if (File.Exists(backupPath))
                    File.Delete(backupPath);

                if (File.Exists(sessionPath))
                    File.Delete(sessionPath);

                cachedData = new SaveData();
                isDirty = false;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error deleting save data: {e.Message}");
            }
        }

        /// <summary>
        /// Gets the save file path.
        /// </summary>
        public string GetSavePath() => savePath;

        /// <summary>
        /// Checks if save data exists.
        /// </summary>
        public bool HasSaveData() => File.Exists(savePath);
    }
}
