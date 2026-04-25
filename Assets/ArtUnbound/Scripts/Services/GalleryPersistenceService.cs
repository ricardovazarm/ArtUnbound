using System.Collections.Generic;
using ArtUnbound.Data;

namespace ArtUnbound.Services
{
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

        public void SavePainting(string galleryId, GalleryPaintingData painting)
        {
            var data = _saveDataService.GetCachedData();
            if (!data.galleryPaintings.ContainsKey(galleryId))
                data.galleryPaintings[galleryId] = new List<GalleryPaintingData>();

            // Replace existing entry for same artworkId+difficulty, or add new
            var list = data.galleryPaintings[galleryId];
            int idx = list.FindIndex(p => p.artworkId == painting.artworkId && p.difficultyIndex == painting.difficultyIndex);
            if (idx >= 0)
                list[idx] = painting;
            else
                list.Add(painting);

            _saveDataService.MarkDirty();
        }

        public void RemovePainting(string galleryId, string artworkId, int difficultyIndex)
        {
            var data = _saveDataService.GetCachedData();
            if (!data.galleryPaintings.ContainsKey(galleryId)) return;
            data.galleryPaintings[galleryId].RemoveAll(p => p.artworkId == artworkId && p.difficultyIndex == difficultyIndex);
            _saveDataService.MarkDirty();
        }

        public void ClearGallery(string galleryId)
        {
            var data = _saveDataService.GetCachedData();
            if (data.galleryPaintings.ContainsKey(galleryId))
                data.galleryPaintings[galleryId].Clear();
            _saveDataService.MarkDirty();
        }
    }
}
