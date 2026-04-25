using System.Collections.Generic;
using UnityEngine;

namespace ArtUnbound.Data
{
    /// <summary>
    /// Master list of all purchasable bundles.
    /// </summary>
    [CreateAssetMenu(menuName = "ArtUnbound/Bundle Catalog", fileName = "BundleCatalog")]
    public class BundleCatalog : ScriptableObject
    {
        public List<BundleDefinition> bundles = new List<BundleDefinition>();
    }
}
