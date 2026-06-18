using System.Collections.Generic;
using UnityEngine;

namespace ArtUnbound.Data
{
    /// <summary>
    /// Catalogo maestro de coleccionables. La LISTA esta ordenada: ese orden es el que se muestra
    /// en la seccion Collection. Agrega/quita/reordena libremente desde el Inspector.
    /// Generalo de inicio con Tools/ArtUnbound/Generate Collectibles.
    /// </summary>
    [CreateAssetMenu(menuName = "ArtUnbound/Collectible Catalog", fileName = "CollectibleCatalog")]
    public class CollectibleCatalog : ScriptableObject
    {
        [Tooltip("Coleccionables en el orden en que aparecen en Collection.")]
        public List<CollectibleDefinition> collectibles = new List<CollectibleDefinition>();

        public CollectibleDefinition GetById(string id)
        {
            if (string.IsNullOrEmpty(id) || collectibles == null) return null;
            return collectibles.Find(c => c != null && c.id == id);
        }
    }
}
