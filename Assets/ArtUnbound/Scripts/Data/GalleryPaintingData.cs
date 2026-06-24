using System;
using UnityEngine;

namespace ArtUnbound.Data
{
    [Serializable]
    public class GalleryPaintingData
    {
        /// <summary>Id unico de esta copia colgada (equivalente al anchorId de MR). Clave de persistencia;
        /// permite varias copias de la misma obra/placa sin pisarse. El artworkId se mantiene limpio.</summary>
        public string instanceId;
        public string artworkId;
        public int difficultyIndex;
        public float posX, posY, posZ;
        public float rotX, rotY, rotZ, rotW;
        public float boardWidth;
        public float boardHeight;
        public FrameTier frameTier;

        public Vector3 Position
        {
            get => new Vector3(posX, posY, posZ);
            set { posX = value.x; posY = value.y; posZ = value.z; }
        }

        public Quaternion Rotation
        {
            get => new Quaternion(rotX, rotY, rotZ, rotW);
            set { rotX = value.x; rotY = value.y; rotZ = value.z; rotW = value.w; }
        }
    }
}
