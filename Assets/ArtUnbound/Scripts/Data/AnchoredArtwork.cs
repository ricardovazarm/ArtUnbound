using System;
using UnityEngine;

namespace ArtUnbound.Data
{
    /// <summary>
    /// Data for an artwork hung on a wall using spatial anchors.
    /// Persisted to save data for restoration across sessions.
    /// </summary>
    [Serializable]
    public class AnchoredArtwork
    {
        public string artworkId;
        public string anchorId;           // UUID of the AR Anchor
        public SerializableVector3 localPosition;  // Position relative to anchor
        public SerializableQuaternion localRotation; // Rotation relative to anchor
        public float scale = 1f;
        public FrameTier frameTier;
        public long placedTimestamp;      // DateTime.Ticks

        public AnchoredArtwork()
        {
            artworkId = string.Empty;
            anchorId = string.Empty;
            localPosition = new SerializableVector3();
            localRotation = new SerializableQuaternion();
            scale = 1f;
            frameTier = FrameTier.Bronce;
            placedTimestamp = DateTime.UtcNow.Ticks;
        }

        public AnchoredArtwork(string artworkId, string anchorId, Vector3 position, Quaternion rotation, float scale, FrameTier tier)
        {
            this.artworkId = artworkId;
            this.anchorId = anchorId;
            this.localPosition = new SerializableVector3(position);
            this.localRotation = new SerializableQuaternion(rotation);
            this.scale = scale;
            this.frameTier = tier;
            this.placedTimestamp = DateTime.UtcNow.Ticks;
        }

        public DateTime GetPlacedDate()
        {
            return new DateTime(placedTimestamp);
        }
    }
}
