using System.Collections.Generic;
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

        [Header("Music Tracks")]
        [SerializeField] private AudioClip menuMusic;
        [SerializeField] private AudioClip gameplayMusic;
        [SerializeField] private AudioClip victoryMusic;

        [Header("Ambient Sounds")]
        [SerializeField] private AudioClip galleryAmbient;

        [Header("Volume Settings")]
        [SerializeField] private float sfxVolume = 1.0f;
        [SerializeField] private float musicVolume = 0.7f;
        [SerializeField] private float ambientVolume = 0.3f;

        [Header("Configuration")]
        [SerializeField] private float musicFadeDuration = 1.0f;

        public static AudioManager Instance { get; private set; }

        private Coroutine musicFadeCoroutine;
        private Dictionary<string, AudioClip> soundEffects;

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
            PlaySound("piece_grab");
        }

        /// <summary>
        /// Plays piece release sound.
        /// </summary>
        public void PlayPieceRelease()
        {
            PlaySound("piece_release");
        }

        /// <summary>
        /// Plays piece snap (correct placement) sound.
        /// </summary>
        public void PlayPieceSnap()
        {
            PlaySound("piece_snap");
        }

        /// <summary>
        /// Plays piece incorrect placement sound.
        /// </summary>
        public void PlayPieceIncorrect()
        {
            PlaySound("piece_incorrect");
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
        /// Plays menu music.
        /// </summary>
        public void PlayMenuMusic()
        {
            PlayMusic(menuMusic);
        }

        /// <summary>
        /// Plays gameplay music.
        /// </summary>
        public void PlayGameplayMusic()
        {
            PlayMusic(gameplayMusic);
        }

        /// <summary>
        /// Plays victory music.
        /// </summary>
        public void PlayVictoryMusic()
        {
            PlayMusic(victoryMusic);
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
