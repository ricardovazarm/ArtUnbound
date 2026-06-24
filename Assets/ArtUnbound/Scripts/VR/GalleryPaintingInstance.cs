using UnityEngine;

namespace ArtUnbound.VR
{
    /// <summary>
    /// Marca una obra/placa colgada en una galeria VR con su id de instancia unico. Es el equivalente
    /// VR del anchorId de MR (AnchoredArtwork.anchorId): identifica la copia EXACTA colgada para poder
    /// reposicionarla o quitarla sin confundir duplicados, manteniendo el artworkId limpio.
    /// La persistencia (galleryPaintings) usa este id como clave de la entrada.
    /// </summary>
    public class GalleryPaintingInstance : MonoBehaviour
    {
        public string instanceId;
    }
}
