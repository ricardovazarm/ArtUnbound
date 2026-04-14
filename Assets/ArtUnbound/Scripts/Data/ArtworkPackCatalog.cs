using System.Collections.Generic;
using UnityEngine;

namespace ArtUnbound.Data
{
    /// <summary>
    /// Master list of all purchasable artwork packs.
    /// </summary>
    [CreateAssetMenu(menuName = "ArtUnbound/Artwork Pack Catalog", fileName = "ArtworkPackCatalog")]
    public class ArtworkPackCatalog : ScriptableObject
    {
        public List<ArtworkPackDefinition> packs = new List<ArtworkPackDefinition>();
    }
}
