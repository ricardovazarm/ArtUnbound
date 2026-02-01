using UnityEngine;

namespace ArtUnbound.Data
{
    [CreateAssetMenu(menuName = "ArtUnbound/Frame Config", fileName = "FrameConfig")]
    public class FrameConfig : ScriptableObject
    {
        public FrameTier tier;
        public Material frameMaterial;
        public int scoreThreshold;
        public bool requiresNoHelp;
    }
}
