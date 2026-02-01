using System.Collections.Generic;
using UnityEngine;

namespace ArtUnbound.Data
{
    [CreateAssetMenu(menuName = "ArtUnbound/Frame Config Set", fileName = "FrameConfigSet")]
    public class FrameConfigSet : ScriptableObject
    {
        public List<FrameConfig> configs = new List<FrameConfig>();

        public FrameConfig GetConfig(FrameTier tier)
        {
            for (int i = 0; i < configs.Count; i++)
            {
                if (configs[i] != null && configs[i].tier == tier)
                {
                    return configs[i];
                }
            }
            return null;
        }
    }
}
