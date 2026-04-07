using UnityEngine;

namespace ArtUnbound.MR
{
    /// <summary>
    /// Identifies a placed artwork GameObject with its artworkId.
    /// Added to any GameObject tagged "PlacedArtwork" so InteractionManager
    /// can retrieve the artworkId when the user grabs it off the wall.
    /// </summary>
    public class PlacedArtworkIdentifier : MonoBehaviour
    {
        public string artworkId;
    }
}
