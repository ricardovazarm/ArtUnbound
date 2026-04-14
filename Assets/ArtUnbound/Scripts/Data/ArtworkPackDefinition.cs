using System.Collections.Generic;
using UnityEngine;

namespace ArtUnbound.Data
{
    /// <summary>
    /// Defines a purchasable DLC pack containing a set of artworks.
    /// </summary>
    [CreateAssetMenu(menuName = "ArtUnbound/Artwork Pack Definition", fileName = "NewArtworkPack")]
    public class ArtworkPackDefinition : ScriptableObject
    {
        [Tooltip("Unique identifier for this pack, e.g. 'pack_impressionism'")]
        public string packId;

        [Tooltip("Display name shown in the UI")]
        public string packName;

        [Tooltip("Price string shown on the unlock button, e.g. '$2.99'")]
        public string price;

        [Tooltip("All artworks included in this pack")]
        public List<ArtworkDefinition> artworks = new List<ArtworkDefinition>();
    }
}
