using System.Collections.Generic;
using ArtUnbound.Data;
using UnityEngine;

namespace ArtUnbound.Gameplay
{
    /// <summary>
    /// Calculates scores and determines frame tiers based on puzzle completion.
    /// </summary>
    public class ScoringController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private FrameConfigSet frameConfigSet;

        [Header("Scoring Parameters")]
        [SerializeField] private float helpPenaltyMultiplier = 0.5f;

        // Difficulty multipliers by piece count
        private static readonly Dictionary<int, float> DifficultyMultipliers = new Dictionary<int, float>
        {
            { 64, 1.0f },
            { 144, 1.5f },
            { 256, 2.0f },
            { 512, 3.0f }
        };

        // Default thresholds if no FrameConfigSet is provided
        private static readonly int[] DefaultThresholds = { 0, 50, 100, 200, 300 };

        /// <summary>
        /// Calculates the score based on time, piece count, and help mode.
        /// Formula: baseScore = (pieceCount * 100) / timeSec * multiplier * helpFactor
        /// </summary>
        public int CalculateScore(int timeSec, int pieceCount, bool helpMode)
        {
            // Prevent division by zero
            timeSec = Mathf.Max(1, timeSec);

            // Base score: pieces * 100 / time
            float baseScore = (pieceCount * 100f) / timeSec;

            // Apply difficulty multiplier
            float multiplier = GetDifficultyMultiplier(pieceCount);
            baseScore *= multiplier;

            // Apply help penalty
            if (helpMode)
            {
                baseScore *= helpPenaltyMultiplier;
            }

            return Mathf.FloorToInt(baseScore);
        }

        /// <summary>
        /// Gets the difficulty multiplier for a given piece count.
        /// </summary>
        public float GetDifficultyMultiplier(int pieceCount)
        {
            if (DifficultyMultipliers.TryGetValue(pieceCount, out float multiplier))
            {
                return multiplier;
            }

            // Interpolate for non-standard piece counts
            if (pieceCount < 64) return 0.8f;
            if (pieceCount < 144) return 1.0f + (pieceCount - 64) / 80f * 0.5f;
            if (pieceCount < 256) return 1.5f + (pieceCount - 144) / 112f * 0.5f;
            if (pieceCount < 512) return 2.0f + (pieceCount - 256) / 256f * 1.0f;
            return 3.0f + (pieceCount - 512) / 512f * 0.5f;
        }

        /// <summary>
        /// Determines the frame tier based on score, help mode, and piece count.
        /// </summary>
        public FrameTier GetFrameTier(int score, bool helpMode, int pieceCount = 64)
        {
            // Ebano requires: score >= 300, no help, and at least 256 pieces
            if (score >= 300 && !helpMode && pieceCount >= 256)
            {
                return FrameTier.Ebano;
            }

            // Oro requires: score >= 200 and no help
            if (score >= 200 && !helpMode)
            {
                return FrameTier.Oro;
            }

            // Use FrameConfigSet if available
            if (frameConfigSet != null && frameConfigSet.configs.Count > 0)
            {
                return GetFrameTierFromConfigSet(score, helpMode);
            }

            // Default tier determination
            if (score >= 100) return FrameTier.Plata;
            if (score >= 50) return FrameTier.Bronce;
            return FrameTier.Madera;
        }

        /// <summary>
        /// Gets frame tier using the FrameConfigSet.
        /// </summary>
        private FrameTier GetFrameTierFromConfigSet(int score, bool helpMode)
        {
            FrameTier best = FrameTier.Madera;

            for (int i = 0; i < frameConfigSet.configs.Count; i++)
            {
                FrameConfig config = frameConfigSet.configs[i];
                if (config == null) continue;

                // Skip tiers that require no help if help was used
                if (helpMode && config.requiresNoHelp) continue;

                if (score >= config.scoreThreshold)
                {
                    best = config.tier;
                }
            }

            return best;
        }

        /// <summary>
        /// Legacy method for backwards compatibility.
        /// </summary>
        public FrameTier GetFrameTier(int score, bool helpMode)
        {
            return GetFrameTier(score, helpMode, 64);
        }

        /// <summary>
        /// Gets scoring summary for display.
        /// </summary>
        public ScoringSummary GetScoringSummary(int timeSec, int pieceCount, bool helpMode)
        {
            int score = CalculateScore(timeSec, pieceCount, helpMode);
            FrameTier tier = GetFrameTier(score, helpMode, pieceCount);
            float multiplier = GetDifficultyMultiplier(pieceCount);

            return new ScoringSummary
            {
                Score = score,
                FrameTier = tier,
                TimeSec = timeSec,
                PieceCount = pieceCount,
                HelpModeUsed = helpMode,
                DifficultyMultiplier = multiplier
            };
        }
    }

    /// <summary>
    /// Summary of scoring calculation for display purposes.
    /// </summary>
    public struct ScoringSummary
    {
        public int Score;
        public FrameTier FrameTier;
        public int TimeSec;
        public int PieceCount;
        public bool HelpModeUsed;
        public float DifficultyMultiplier;
    }
}
