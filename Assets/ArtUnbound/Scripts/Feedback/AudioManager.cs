using System;
using System.Collections.Generic;
using ArtUnbound.Data;
using UnityEngine;

namespace ArtUnbound.Feedback
{
    /// <summary>
    /// Manages audio playback for sound effects and music.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource ambientSource;

        [Header("Sound Effects")]
        [SerializeField] private AudioClip pieceGrabSound;
        [SerializeField] private AudioClip pieceReleaseSound;
        [SerializeField] private AudioClip pieceSnapSound;
        [SerializeField] private AudioClip pieceIncorrectSound;
        [SerializeField] private AudioClip puzzleCompleteSound;
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioClip menuOpenSound;
        [SerializeField] private AudioClip menuCloseSound;
        [SerializeField] private AudioClip achievementSound;
        [SerializeField] private AudioClip newRecordSound;

        [Header("Music Library")]
        [SerializeField] private MusicLibrary musicLibrary;

        [Header("Ambient Sounds")]
        [SerializeField] private AudioClip galleryAmbient;

        [Header("Volume Settings")]
        [SerializeField] private float sfxVolume = 0.7f; // Standard UI sounds
        [SerializeField] private float musicVolume = 0.15f; // Background music - very low to prevent distortion
        [SerializeField] private float ambientVolume = 0.2f;
        [SerializeField] private float pieceVolume = 0.5f; // Piece sounds - slightly reduced to prevent clipping

        [Header("Configuration")]
        [SerializeField] private float musicFadeDuration = 1.0f;

        public static AudioManager Instance { get; private set; }

        /// <summary>Fired when the current track changes. Args: title, artist.</summary>
        public event Action<string, string> OnTrackChanged;

        /// <summary>Current track info for display.</summary>
        public (string title, string artist) CurrentTrack => (currentTrackTitle, currentTrackArtist);

        private Coroutine musicFadeCoroutine;
        private Dictionary<string, AudioClip> soundEffects;
        private bool musicPlaybackStarted;
        private int lastPlayedTrackIndex = -1;
        private string currentTrackTitle = "";
        private string currentTrackArtist = "";

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeSoundDictionary();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeSoundDictionary()
        {
            soundEffects = new Dictionary<string, AudioClip>
            {
                { "piece_grab", pieceGrabSound },
                { "piece_release", pieceReleaseSound },
                { "piece_snap", pieceSnapSound },
                { "piece_incorrect", pieceIncorrectSound },
                { "puzzle_complete", puzzleCompleteSound },
                { "button_click", buttonClickSound },
                { "menu_open", menuOpenSound },
                { "menu_close", menuCloseSound },
                { "achievement", achievementSound },
                { "new_record", newRecordSound }
            };
        }

        /// <summary>
        /// Sets the SFX volume (0-1).
        /// </summary>
        public void SetSfxVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            if (sfxSource != null)
                sfxSource.volume = sfxVolume;
        }

        /// <summary>
        /// Sets the music volume (0-1).
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            if (musicSource != null)
                musicSource.volume = musicVolume;
        }

        /// <summary>
        /// Sets the ambient volume (0-1).
        /// </summary>
        public void SetAmbientVolume(float volume)
        {
            ambientVolume = Mathf.Clamp01(volume);
            if (ambientSource != null)
                ambientSource.volume = ambientVolume;
        }

        /// <summary>
        /// Plays a sound effect by name.
        /// </summary>
        public void PlaySound(string soundName)
        {
            if (sfxSource == null || string.IsNullOrEmpty(soundName)) return;

            if (soundEffects.TryGetValue(soundName, out AudioClip clip) && clip != null)
            {
                sfxSource.PlayOneShot(clip, sfxVolume);
            }
        }

        /// <summary>
        /// Plays a sound effect clip directly.
        /// </summary>
        public void PlaySound(AudioClip clip)
        {
            if (sfxSource == null || clip == null) return;
            sfxSource.PlayOneShot(clip, sfxVolume);
        }

        /// <summary>
        /// Plays piece grab sound.
        /// </summary>
        public void PlayPieceGrab()
        {
            if (sfxSource == null || pieceGrabSound == null) return;
            sfxSource.PlayOneShot(pieceGrabSound, pieceVolume);
        }

        /// <summary>
        /// Plays piece release sound.
        /// </summary>
        public void PlayPieceRelease()
        {
            if (sfxSource == null || pieceReleaseSound == null) return;
            sfxSource.PlayOneShot(pieceReleaseSound, pieceVolume);
        }

        /// <summary>
        /// Plays piece snap (correct placement) sound.
        /// </summary>
        public void PlayPieceSnap()
        {
            if (sfxSource == null || pieceSnapSound == null) return;
            sfxSource.PlayOneShot(pieceSnapSound, pieceVolume);
        }

        /// <summary>
        /// Plays piece incorrect placement sound.
        /// </summary>
        public void PlayPieceIncorrect()
        {
            if (sfxSource == null || pieceIncorrectSound == null) return;
            sfxSource.PlayOneShot(pieceIncorrectSound, pieceVolume);
        }

        /// <summary>
        /// Plays puzzle completion sound.
        /// </summary>
        public void PlayPuzzleComplete()
        {
            PlaySound("puzzle_complete");
        }

        /// <summary>
        /// Plays button click sound.
        /// </summary>
        public void PlayButtonClick()
        {
            PlaySound("button_click");
        }

        /// <summary>
        /// Plays new record sound.
        /// </summary>
        public void PlayNewRecord()
        {
            PlaySound("new_record");
        }

        /// <summary>
        /// Plays milestone sound (row/column/edge completion).
        /// Uses achievement sound for motivational feedback.
        /// </summary>
        public void PlayMilestone()
        {
            PlaySound("achievement");
        }

        /// <summary>
        /// Starts continuous music playback from the library. Music continues across menu and gameplay.
        /// Call once at startup; tracks change randomly when each song ends.
        /// Ignores subsequent calls if already started.
        /// </summary>
        public void StartMusicPlayback()
        {
            if (musicPlaybackStarted)
                return;

            if (musicLibrary == null || musicLibrary.tracks == null || musicLibrary.tracks.Length == 0)
                return;

            musicPlaybackStarted = true;
            PlayNextRandomTrack();
        }

        private void Update()
        {
            if (!musicPlaybackStarted || musicSource == null || musicFadeCoroutine != null)
                return;

            if (!musicSource.isPlaying && musicSource.clip != null)
                PlayNextRandomTrack();
        }

        private void PlayNextRandomTrack()
        {
            var tracks = musicLibrary.tracks;
            if (tracks.Length == 0) return;

            int index;
            if (tracks.Length == 1)
                index = 0;
            else
            {
                do
                {
                    index = UnityEngine.Random.Range(0, tracks.Length);
                } while (index == lastPlayedTrackIndex);
            }

            lastPlayedTrackIndex = index;
            var entry = tracks[index];
            if (entry.clip == null) return;

            currentTrackTitle = entry.title ?? "";
            currentTrackArtist = entry.artist ?? "";
            OnTrackChanged?.Invoke(currentTrackTitle, currentTrackArtist);

            PlayMusic(entry.clip, loop: false);
        }

        /// <summary>
        /// Plays a music track with fade.
        /// </summary>
        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (musicSource == null) return;

            if (musicFadeCoroutine != null)
            {
                StopCoroutine(musicFadeCoroutine);
            }

            musicFadeCoroutine = StartCoroutine(FadeToNewMusic(clip, loop));
        }

        private System.Collections.IEnumerator FadeToNewMusic(AudioClip newClip, bool loop)
        {
            // Fade out current music
            if (musicSource.isPlaying)
            {
                float startVolume = musicSource.volume;
                float elapsed = 0f;

                while (elapsed < musicFadeDuration / 2)
                {
                    elapsed += Time.deltaTime;
                    musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / (musicFadeDuration / 2));
                    yield return null;
                }
            }

            // Change clip
            musicSource.Stop();
            musicSource.clip = newClip;
            musicSource.loop = loop;

            if (newClip != null)
            {
                // Fade in new music
                musicSource.Play();
                float elapsed = 0f;

                while (elapsed < musicFadeDuration / 2)
                {
                    elapsed += Time.deltaTime;
                    musicSource.volume = Mathf.Lerp(0f, musicVolume, elapsed / (musicFadeDuration / 2));
                    yield return null;
                }

                musicSource.volume = musicVolume;
            }

            musicFadeCoroutine = null;
        }

        /// <summary>
        /// Stops music with fade.
        /// </summary>
        public void StopMusic()
        {
            if (musicSource == null) return;

            if (musicFadeCoroutine != null)
            {
                StopCoroutine(musicFadeCoroutine);
            }

            musicFadeCoroutine = StartCoroutine(FadeOutMusic());
        }

        private System.Collections.IEnumerator FadeOutMusic()
        {
            float startVolume = musicSource.volume;
            float elapsed = 0f;

            while (elapsed < musicFadeDuration)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / musicFadeDuration);
                yield return null;
            }

            musicSource.Stop();
            musicSource.volume = musicVolume;
            musicFadeCoroutine = null;
        }

        /// <summary>
        /// Starts gallery ambient sound.
        /// </summary>
        public void StartGalleryAmbient()
        {
            if (ambientSource == null || galleryAmbient == null) return;

            ambientSource.clip = galleryAmbient;
            ambientSource.loop = true;
            ambientSource.volume = ambientVolume;
            ambientSource.Play();
        }

        /// <summary>
        /// Stops ambient sound.
        /// </summary>
        public void StopAmbient()
        {
            if (ambientSource != null)
            {
                ambientSource.Stop();
            }
        }

        /// <summary>
        /// Pauses all audio.
        /// </summary>
        public void PauseAll()
        {
            if (musicSource != null) musicSource.Pause();
            if (ambientSource != null) ambientSource.Pause();
        }

        /// <summary>
        /// Resumes all audio.
        /// </summary>
        public void ResumeAll()
        {
            if (musicSource != null) musicSource.UnPause();
            if (ambientSource != null) ambientSource.UnPause();
        }

        /// <summary>
        /// Mutes all audio.
        /// </summary>
        public void MuteAll(bool mute)
        {
            if (sfxSource != null) sfxSource.mute = mute;
            if (musicSource != null) musicSource.mute = mute;
            if (ambientSource != null) ambientSource.mute = mute;
        }
    }
}
