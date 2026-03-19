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

    /// <summary>
    /// Serializable Vector3 for JSON persistence.
    /// </summary>
    [Serializable]
    public struct SerializableVector3
    {
        public float x, y, z;

        public SerializableVector3(Vector3 vector)
        {
            x = vector.x;
            y = vector.y;
            z = vector.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }

        public static implicit operator Vector3(SerializableVector3 sv)
        {
            return sv.ToVector3();
        }

        public static implicit operator SerializableVector3(Vector3 v)
        {
            return new SerializableVector3(v);
        }
    }

    /// <summary>
    /// Serializable Quaternion for JSON persistence.
    /// </summary>
    [Serializable]
    public struct SerializableQuaternion
    {
        public float x, y, z, w;

        public SerializableQuaternion(Quaternion quaternion)
        {
            x = quaternion.x;
            y = quaternion.y;
            z = quaternion.z;
            w = quaternion.w;
        }

        public Quaternion ToQuaternion()
        {
            return new Quaternion(x, y, z, w);
        }

        public static implicit operator Quaternion(SerializableQuaternion sq)
        {
            return sq.ToQuaternion();
        }

        public static implicit operator SerializableQuaternion(Quaternion q)
        {
            return new SerializableQuaternion(q);
        }
    }
}
