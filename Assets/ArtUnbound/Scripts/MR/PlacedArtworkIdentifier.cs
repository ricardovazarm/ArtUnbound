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

        /// <summary>
        /// GUID del ancla persistente (MR) de ESTA copia, o vacio si aun no se ha anclado
        /// (p.ej. la obra flotante del boton Hang del detalle). Permite varias copias de la
        /// misma obra: reposicionar/quitar opera sobre la entrada de esta ancla, nunca sobre
        /// las de otras copias con el mismo artworkId. Equivale al instanceId de VR.
        /// </summary>
        public string anchorId;
    }
}
