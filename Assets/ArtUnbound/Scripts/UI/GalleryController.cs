using System.Collections.Generic;
using ArtUnbound.Data;
using ArtUnbound.Services;
using UnityEngine;

namespace ArtUnbound.UI
{
    public class GalleryController : MonoBehaviour
    {
        [SerializeField] private GameObject completedFramePrefab;
        [SerializeField] private Transform spawnRoot;
        [SerializeField] private SaveDataService saveDataService;

        public void ShowCompleted()
        {
            SaveData data = saveDataService != null ? saveDataService.Load() : null;
            if (data == null || completedFramePrefab == null || spawnRoot == null)
            {
                return;
            }

            for (int i = 0; i < data.completedArtworks.Count; i++)
            {
                Instantiate(completedFramePrefab, spawnRoot);
            }
        }
    }
}
