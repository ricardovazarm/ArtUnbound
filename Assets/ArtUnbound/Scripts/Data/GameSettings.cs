using System;

namespace ArtUnbound.Data
{
    /// <summary>
    /// User preferences and game settings.
    /// </summary>
    [Serializable]
    public class GameSettings
    {
        // Audio settings
        public float sfxVolume = 0.7f;
        public float musicVolume = 0.5f;
        public bool hapticsEnabled = true;

        // Gameplay settings
        public bool helpModeDefault = true;
        public bool showOnboarding = true;
        public int defaultPieceCount = 64;

        // Visual settings
        public bool showGrid = false;
        public bool highContrast = false;

        // Control settings
        public float pieceSnapDistance = 0.03f;

        public GameSettings()
        {
            sfxVolume = 0.7f;
            musicVolume = 0.5f;
            hapticsEnabled = true;
            helpModeDefault = true;
            showOnboarding = true;
            defaultPieceCount = 64;
            showGrid = false;
            highContrast = false;
            pieceSnapDistance = 0.03f;
        }
    }
}
