using UnityEngine;

namespace ArtUnbound.Gameplay
{
    public class HelpModeController : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip helpClip;
        [SerializeField] private ParticleSystem helpFx;

        public bool IsHelpEnabled { get; private set; } = true;

        public void SetHelp(bool enabled)
        {
            IsHelpEnabled = enabled;
        }

        public void PlayHelpFeedback(Vector3 worldPosition)
        {
            if (!IsHelpEnabled)
            {
                return;
            }

            if (helpFx != null)
            {
                helpFx.transform.position = worldPosition;
                helpFx.Play();
            }

            if (audioSource != null && helpClip != null)
            {
                audioSource.PlayOneShot(helpClip);
            }
        }

        public void PlayErrorFeedback(Vector3 worldPosition, Color errorColor)
        {
            if (!IsHelpEnabled)
            {
                return;
            }

            if (helpFx != null)
            {
                var main = helpFx.main;
                main.startColor = errorColor;
                helpFx.transform.position = worldPosition;
                helpFx.Play();
            }
        }
    }
}
