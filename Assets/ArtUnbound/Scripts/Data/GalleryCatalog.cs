using System.Collections.Generic;
using UnityEngine;

namespace ArtUnbound.Data
{
    [CreateAssetMenu(fileName = "GalleryCatalog", menuName = "ArtUnbound/Gallery Catalog")]
    public class GalleryCatalog : ScriptableObject
    {
        public List<GalleryDefinition> galleries = new List<GalleryDefinition>();
        public string defaultGalleryId = "gallery_classic";
    }
}
