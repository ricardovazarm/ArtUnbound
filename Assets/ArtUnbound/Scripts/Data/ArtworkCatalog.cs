using System.Collections.Generic;
using UnityEngine;

namespace ArtUnbound.Data
{
    [CreateAssetMenu(menuName = "ArtUnbound/Artwork Catalog", fileName = "ArtworkCatalog")]
    public class ArtworkCatalog : ScriptableObject
    {
        public List<ArtworkDefinition> artworks = new List<ArtworkDefinition>();
    }
}
