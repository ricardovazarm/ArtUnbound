using System.Collections.Generic;
using ArtUnbound.Data;

namespace ArtUnbound.Services
{
    /// <summary>
    /// Persistencia de obras/placas colgadas por galeria VR. Espeja el metodo de MR (WallAnchorManager):
    /// una LISTA de entradas donde cada copia colgada tiene su instanceId unico (equivalente al anchorId),
    /// y el artworkId se mantiene limpio. Colgar AGREGA una entrada (no sobrescribe), por eso persisten
    /// varias copias de la misma obra. Se guarda a disco de inmediato en cada cambio (como MR).
    /// </summary>
    public class GalleryPersistenceService
    {
        private readonly SaveDataService _saveDataService;

        public GalleryPersistenceService(SaveDataService saveDataService)
        {
            _saveDataService = saveDataService;
        }

        public List<GalleryPaintingData> GetPaintings(string galleryId)
        {
            var data = _saveDataService.GetCachedData();
            if (data.galleryPaintings.TryGetValue(galleryId, out var list))
                return new List<GalleryPaintingData>(list);
            return new List<GalleryPaintingData>();
        }

        /// <summary>
        /// Inserta/actualiza una copia colgada por su instanceId (upsert) y guarda a disco. Una copia
        /// nueva (instanceId nuevo) se AGREGA; reposicionar una existente (mismo instanceId) la reemplaza.
        /// </summary>
        public void SavePainting(string galleryId, GalleryPaintingData painting)
        {
            if (painting == null) return;

            var data = _saveDataService.GetCachedData();
            if (!data.galleryPaintings.ContainsKey(galleryId))
                data.galleryPaintings[galleryId] = new List<GalleryPaintingData>();

            var list = data.galleryPaintings[galleryId];
            int idx = !string.IsNullOrEmpty(painting.instanceId)
                ? list.FindIndex(p => p.instanceId == painting.instanceId)
                : -1;
            if (idx >= 0)
                list[idx] = painting;
            else
                list.Add(painting);

            _saveDataService.Save(data); // guardado inmediato, como MR.AddAnchoredArtwork
        }

        /// <summary>
        /// Actualiza SOLO la pose (posicion/rotacion) de la copia con ese instanceId, preservando el
        /// resto de campos (artworkId, tamaño, tier). Para reposicionar sin perder datos. Devuelve true
        /// si encontro la entrada.
        /// </summary>
        public bool UpdatePaintingPose(string galleryId, string instanceId, UnityEngine.Vector3 position, UnityEngine.Quaternion rotation)
        {
            if (string.IsNullOrEmpty(instanceId)) return false;
            var data = _saveDataService.GetCachedData();
            if (!data.galleryPaintings.TryGetValue(galleryId, out var list)) return false;
            var entry = list.Find(p => p.instanceId == instanceId);
            if (entry == null) return false;
            entry.Position = position;
            entry.Rotation = rotation;
            _saveDataService.Save(data);
            return true;
        }

        /// <summary>Quita la copia colgada con ese instanceId y guarda a disco.</summary>
        public void RemovePainting(string galleryId, string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId)) return;
            var data = _saveDataService.GetCachedData();
            if (!data.galleryPaintings.ContainsKey(galleryId)) return;
            int removed = data.galleryPaintings[galleryId].RemoveAll(p => p.instanceId == instanceId);
            if (removed > 0)
                _saveDataService.Save(data);
        }

        public void ClearGallery(string galleryId)
        {
            var data = _saveDataService.GetCachedData();
            if (data.galleryPaintings.ContainsKey(galleryId))
            {
                data.galleryPaintings[galleryId].Clear();
                _saveDataService.Save(data);
            }
        }
    }
}
