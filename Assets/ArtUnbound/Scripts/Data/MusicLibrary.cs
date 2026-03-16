using UnityEngine;

namespace ArtUnbound.Data
{
    [System.Serializable]
    public class MusicTrackEntry
    {
        public AudioClip clip;
        public string title;
        public string artist;
    }

    [CreateAssetMenu(fileName = "MusicLibrary", menuName = "ArtUnbound/Music Library")]
    public class MusicLibrary : ScriptableObject
    {
        public MusicTrackEntry[] tracks = System.Array.Empty<MusicTrackEntry>();
    }
}
