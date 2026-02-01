using System;
using UnityEngine;

namespace ArtUnbound.Data
{
    /// <summary>
    /// Represents an artwork that has been placed/hung in the user's space.
    /// </summary>
    [Serializable]
    public class PlacedArtwork
    {
        public string artworkId;
        public string anchorId;
        public FrameTier frameTier;
        public SerializableVector3 position;
        public SerializableQuaternion rotation;
        public bool isActive;
        public string placedAt;
        public float scale;

        /// <summary>
        /// Alias for placedAt parsed as DateTime.
        /// </summary>
        public DateTime placedDate
        {
            get
            {
                if (DateTime.TryParse(placedAt, out DateTime date))
                    return date;
                return DateTime.Now;
            }
            set => placedAt = value.ToString("o");
        }

        public PlacedArtwork()
        {
            artworkId = string.Empty;
            anchorId = string.Empty;
            frameTier = FrameTier.Madera;
            position = new SerializableVector3();
            rotation = new SerializableQuaternion();
            isActive = true;
            placedAt = DateTime.UtcNow.ToString("o");
            scale = 1.0f;
        }

        public PlacedArtwork(string artworkId, string anchorId, FrameTier frameTier, Vector3 pos, Quaternion rot)
        {
            this.artworkId = artworkId;
            this.anchorId = anchorId;
            this.frameTier = frameTier;
            position = new SerializableVector3(pos);
            rotation = new SerializableQuaternion(rot);
            isActive = true;
            placedAt = DateTime.UtcNow.ToString("o");
            scale = 1.0f;
        }

        public Vector3 GetPosition() => position.ToVector3();
        public Quaternion GetRotation() => rotation.ToQuaternion();

        public void SetPosition(Vector3 pos)
        {
            position = new SerializableVector3(pos);
        }

        public void SetRotation(Quaternion rot)
        {
            rotation = new SerializableQuaternion(rot);
        }
    }

    /// <summary>
    /// Serializable version of Vector3 for JSON serialization.
    /// </summary>
    [Serializable]
    public struct SerializableVector3
    {
        public float x;
        public float y;
        public float z;

        public SerializableVector3(Vector3 v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
        }

        public Vector3 ToVector3() => new Vector3(x, y, z);
    }

    /// <summary>
    /// Serializable version of Quaternion for JSON serialization.
    /// </summary>
    [Serializable]
    public struct SerializableQuaternion
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public SerializableQuaternion(Quaternion q)
        {
            x = q.x;
            y = q.y;
            z = q.z;
            w = q.w;
        }

        public Quaternion ToQuaternion() => new Quaternion(x, y, z, w);
    }
}
