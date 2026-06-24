using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArtUnbound.Data
{
    [Serializable]
    public class SaveData : ISerializationCallbackReceiver
    {
        public List<ArtworkProgress> progressByArtwork = new List<ArtworkProgress>();
        public List<string> completedArtworks = new List<string>();
        public List<PlacedArtwork> placedArtworks = new List<PlacedArtwork>();
        /// <summary>In-progress puzzle sessions per artwork+pieceCount. Preserved when switching artworks.</summary>
        public List<PuzzleSessionData> inProgressSessions = new List<PuzzleSessionData>();
        /// <summary>Spatial anchors for artworks hung on walls.</summary>
        public List<AnchoredArtwork> anchoredArtworks = new List<AnchoredArtwork>();
        public GameSettings settings = new GameSettings();
        public string lastArtworkId = string.Empty;
        public int lastPieceCount = 144; // Last difficulty used
        public GameMode lastGameMode = GameMode.Gallery;
        public bool onboardingCompleted = false;
        public string firstPlayDate = string.Empty;
        /// <summary>Pack IDs that have been purchased by the user.</summary>
        public List<string> purchasedPackIds = new List<string>();

        // VR Mode
        public bool preferVRMode = false;
        public string lastGalleryId = "gallery_classic";

        /// <summary>
        /// Cuadros/placas colgados por galeria VR (key = galleryId). API de runtime. JsonUtility NO
        /// serializa diccionarios, asi que se persiste via <see cref="galleryPaintingsSerialized"/> +
        /// ISerializationCallbackReceiver. (Por eso VR no recordaba lo colgado: el dictionary se perdia.)
        /// </summary>
        [NonSerialized]
        public Dictionary<string, List<GalleryPaintingData>> galleryPaintings = new Dictionary<string, List<GalleryPaintingData>>();

        // Espejo serializable del diccionario: JsonUtility solo serializa listas y campos [SerializeField].
        [SerializeField]
        private List<GalleryPaintingsEntry> galleryPaintingsSerialized = new List<GalleryPaintingsEntry>();

        /// <summary>Placas con nombre obtenidas por el jugador (GDD 8.3). Ver EarnedPlaque.</summary>
        public List<EarnedPlaque> earnedPlaques = new List<EarnedPlaque>();

        /// <summary>
        /// Alias for progressByArtwork for consistency with UI code.
        /// </summary>
        public List<ArtworkProgress> artworkProgress => progressByArtwork;

        public SaveData()
        {
            progressByArtwork = new List<ArtworkProgress>();
            completedArtworks = new List<string>();
            placedArtworks = new List<PlacedArtwork>();
            inProgressSessions = new List<PuzzleSessionData>();
            anchoredArtworks = new List<AnchoredArtwork>();
            settings = new GameSettings();
            lastArtworkId = string.Empty;
            lastGameMode = GameMode.Gallery;
            onboardingCompleted = false;
            firstPlayDate = DateTime.UtcNow.ToString("o");
            purchasedPackIds = new List<string>();
            galleryPaintings = new Dictionary<string, List<GalleryPaintingData>>();
            earnedPlaques = new List<EarnedPlaque>();
        }

        /// <summary>Gets in-progress session for artwork+pieceCount, or null.</summary>
        public PuzzleSessionData GetInProgressSession(string artworkId, int pieceCount)
        {
            return inProgressSessions.Find(s =>
                s != null && s.artworkId == artworkId && s.pieceCount == pieceCount && !s.isCompleted);
        }

        /// <summary>Gets any session (in-progress or completed) for restoring the board view.</summary>
        public PuzzleSessionData GetSessionForArtwork(string artworkId, int pieceCount)
        {
            return inProgressSessions.Find(s =>
                s != null && s.artworkId == artworkId && s.pieceCount == pieceCount);
        }

        /// <summary>Adds or updates an in-progress session.</summary>
        public void SetInProgressSession(PuzzleSessionData session)
        {
            if (session == null) return;
            var existing = inProgressSessions.FindIndex(s =>
                s != null && s.artworkId == session.artworkId && s.pieceCount == session.pieceCount);
            if (existing >= 0)
                inProgressSessions[existing] = session;
            else
                inProgressSessions.Add(session);
        }

        /// <summary>Removes in-progress session for artwork+pieceCount.</summary>
        public void RemoveInProgressSession(string artworkId, int pieceCount)
        {
            inProgressSessions.RemoveAll(s =>
                s != null && s.artworkId == artworkId && s.pieceCount == pieceCount);
        }

        /// <summary>Gets all in-progress sessions for an artwork (any piece count).</summary>
        public List<PuzzleSessionData> GetInProgressSessionsForArtwork(string artworkId)
        {
            return inProgressSessions.FindAll(s =>
                s != null && s.artworkId == artworkId && !s.isCompleted);
        }

        /// <summary>
        /// Gets the progress for a specific artwork, or creates a new one if not found.
        /// </summary>
        public ArtworkProgress GetOrCreateProgress(string artworkId)
        {
            var progress = progressByArtwork.Find(p => p.artworkId == artworkId);
            if (progress == null)
            {
                progress = new ArtworkProgress { artworkId = artworkId };
                progressByArtwork.Add(progress);
            }
            return progress;
        }

        /// <summary>
        /// Gets the progress for a specific artwork, or null if not found.
        /// </summary>
        public ArtworkProgress GetProgress(string artworkId)
        {
            return progressByArtwork.Find(p => p.artworkId == artworkId);
        }

        /// <summary>
        /// Gets a list of all completed artwork progress records.
        /// </summary>
        public List<ArtworkProgress> GetCompletedArtworks()
        {
            var completed = new List<ArtworkProgress>();
            foreach (var progress in progressByArtwork)
            {
                if (progress.HasBeenCompleted())
                {
                    completed.Add(progress);
                }
            }
            return completed;
        }

        /// <summary>
        /// Gets a list of all completed artwork IDs.
        /// </summary>
        public List<string> GetCompletedArtworkIds()
        {
            var completed = new List<string>();
            foreach (var progress in progressByArtwork)
            {
                if (progress.HasBeenCompleted())
                {
                    completed.Add(progress.artworkId);
                }
            }
            return completed;
        }

        // ── Progresion global (GDD 8.2 / 8.4) ────────────────────────────────────

        /// <summary>Total de obras DISTINTAS completadas — motor de la escalera global.</summary>
        public int GetCompletedCount()
        {
            int count = 0;
            foreach (var progress in progressByArtwork)
                if (progress != null && progress.HasBeenCompleted()) count++;
            return count;
        }

        /// <summary>Tier/material global del jugador, derivado del total de obras completadas.</summary>
        public PlayerTier GetPlayerTier() => ProgressionRules.GetTier(GetCompletedCount());

        // Mejoras de presentacion "efectivas" = desbloqueadas por progreso Y con el toggle on.
        // El Frente 3 las consulta para decidir si aplicar cedula/marco/lampara a las obras colgadas.
        public bool IsCedulaActive()  => ProgressionRules.CedulaUnlocked(GetCompletedCount())  && (settings == null || settings.showCedula);
        public bool IsMarcoActive()   => ProgressionRules.MarcoUnlocked(GetCompletedCount())   && (settings == null || settings.showMarco);
        public bool IsLamparaActive() => ProgressionRules.LamparaUnlocked(GetCompletedCount()) && (settings == null || settings.showLampara);

        // ── Placas con nombre (GDD 8.3) ──────────────────────────────────────────

        /// <summary>True si el jugador ya obtuvo la placa con ese id.</summary>
        public bool HasPlaque(string plaqueId)
        {
            if (string.IsNullOrEmpty(plaqueId) || earnedPlaques == null) return false;
            return earnedPlaques.Exists(p => p != null && p.plaqueId == plaqueId);
        }

        /// <summary>
        /// Otorga una placa (idempotente). Devuelve el registro creado, o el existente si ya
        /// la tenia (en cuyo caso no se otorga de nuevo y la fecha original se conserva).
        /// </summary>
        public EarnedPlaque GrantPlaque(string plaqueId)
        {
            if (string.IsNullOrEmpty(plaqueId)) return null;
            if (earnedPlaques == null) earnedPlaques = new List<EarnedPlaque>();

            var existing = earnedPlaques.Find(p => p != null && p.plaqueId == plaqueId);
            if (existing != null) return existing;

            var earned = new EarnedPlaque(plaqueId);
            earnedPlaques.Add(earned);
            return earned;
        }

        /// <summary>
        /// Gets progress records for artworks that have been "saved" (retired from display but kept).
        /// </summary>
        public List<ArtworkProgress> GetSavedArtworks()
        {
            var saved = new List<ArtworkProgress>();
            foreach (var placed in placedArtworks)
            {
                if (!placed.isActive)
                {
                    var progress = GetProgress(placed.artworkId);
                    if (progress != null)
                    {
                        saved.Add(progress);
                    }
                }
            }
            return saved;
        }

        /// <summary>
        /// Gets a list of artwork IDs that have been "saved" (retired from display but kept).
        /// </summary>
        public List<string> GetSavedArtworkIds()
        {
            var saved = new List<string>();
            foreach (var placed in placedArtworks)
            {
                if (!placed.isActive)
                {
                    saved.Add(placed.artworkId);
                }
            }
            return saved;
        }

        /// <summary>
        /// Gets all active (currently hung) artworks.
        /// </summary>
        public List<PlacedArtwork> GetActivePlacedArtworks()
        {
            return placedArtworks.FindAll(p => p.isActive);
        }

        /// <summary>
        /// Gets all retired (stored but not hung) artworks.
        /// </summary>
        public List<PlacedArtwork> GetRetiredArtworks()
        {
            return placedArtworks.FindAll(p => !p.isActive);
        }

        /// <summary>
        /// Adds a new placed artwork.
        /// </summary>
        public void AddPlacedArtwork(PlacedArtwork artwork)
        {
            placedArtworks.Add(artwork);
        }

        /// <summary>
        /// Removes a placed artwork by its ID.
        /// </summary>
        public bool RemovePlacedArtwork(string artworkId)
        {
            return placedArtworks.RemoveAll(p => p.artworkId == artworkId) > 0;
        }

        // ── Serializacion del diccionario galleryPaintings (JsonUtility) ─────────

        /// <summary>Vuelca el diccionario al espejo serializable antes de guardar (JsonUtility).</summary>
        public void OnBeforeSerialize()
        {
            if (galleryPaintingsSerialized == null)
                galleryPaintingsSerialized = new List<GalleryPaintingsEntry>();
            galleryPaintingsSerialized.Clear();
            if (galleryPaintings == null) return;
            foreach (var kv in galleryPaintings)
                galleryPaintingsSerialized.Add(new GalleryPaintingsEntry { galleryId = kv.Key, paintings = kv.Value });
        }

        /// <summary>Reconstruye el diccionario desde el espejo serializable tras cargar (JsonUtility).</summary>
        public void OnAfterDeserialize()
        {
            galleryPaintings = new Dictionary<string, List<GalleryPaintingData>>();
            if (galleryPaintingsSerialized == null) return;
            foreach (var entry in galleryPaintingsSerialized)
            {
                if (entry == null || string.IsNullOrEmpty(entry.galleryId)) continue;
                galleryPaintings[entry.galleryId] = entry.paintings ?? new List<GalleryPaintingData>();
            }
        }
    }

    /// <summary>Entrada serializable (una galeria + sus cuadros/placas) usada para persistir el
    /// diccionario galleryPaintings con JsonUtility, que no soporta Dictionary.</summary>
    [Serializable]
    public class GalleryPaintingsEntry
    {
        public string galleryId;
        public List<GalleryPaintingData> paintings = new List<GalleryPaintingData>();
    }
}
