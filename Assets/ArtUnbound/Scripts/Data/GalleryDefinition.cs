using UnityEngine;

namespace ArtUnbound.Data
{
    [CreateAssetMenu(fileName = "GalleryDefinition", menuName = "ArtUnbound/Gallery Definition")]
    public class GalleryDefinition : ScriptableObject
    {
        public string galleryId;
        public string displayName;
        public Sprite thumbnail;
        public GameObject environmentPrefab;
    }
}
