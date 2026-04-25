using System.Collections.Generic;
using UnityEngine;

namespace ArtUnbound.Data
{
    /// <summary>
    /// Defines a discounted bundle of multiple ArtworkPackDefinitions.
    /// Purchasing a bundle marks all included packs as purchased.
    /// </summary>
    [CreateAssetMenu(menuName = "ArtUnbound/Bundle Definition", fileName = "NewBundle")]
    public class BundleDefinition : ScriptableObject
    {
        [Tooltip("Unique identifier for this bundle, e.g. 'bundle_wave01_founders'")]
        public string bundleId;

        [Tooltip("Display name shown in the UI")]
        public string bundleName;

        [Tooltip("Price string shown on the unlock button, e.g. '$12.99'")]
        public string price;

        [Tooltip("All packs included in this bundle")]
        public List<ArtworkPackDefinition> packs = new List<ArtworkPackDefinition>();
    }
}
