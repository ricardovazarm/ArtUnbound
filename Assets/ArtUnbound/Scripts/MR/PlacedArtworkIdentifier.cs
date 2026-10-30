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

        /// <summary>
        /// Ancho/alto REALES de la obra (metros), sellados por quien la construye (factory del
        /// Hang del detalle, respawn de anclas, marco del puzzle). Es la fuente de verdad para
        /// persistir el tamano al colgar/reposicionar: el BoxCollider NO sirve para eso porque
        /// GrabbableFrame lo re-ajusta a la caja envolvente de los renderers (incluye cedula y
        /// lampara) y, si la obra no esta alineada con los ejes del mundo, mezcla ancho con
        /// profundidad — asi nacian los cuadros "portrait y delgados" al recargar.
        /// </summary>
        public float boardWidth;
        public float boardHeight;

        public bool HasDimensions => boardWidth > 0.01f && boardHeight > 0.01f;

        public void SetDimensions(float w, float h)
        {
            boardWidth = w;
            boardHeight = h;
        }
    }
}
