using UnityEngine;
using UnityEngine.XR;

namespace ArtUnbound.Feedback
{
    /// <summary>
    /// Controls haptic feedback for XR hand controllers.
    /// </summary>
    public class HapticController : MonoBehaviour
    {
        [Header("Haptic Settings")]
        [SerializeField] private float lightHapticAmplitude = 0.2f;
        [SerializeField] private float lightHapticDuration = 0.05f;

        [SerializeField] private float mediumHapticAmplitude = 0.5f;
        [SerializeField] private float mediumHapticDuration = 0.1f;

        [SerializeField] private float strongHapticAmplitude = 0.8f;
        [SerializeField] private float strongHapticDuration = 0.15f;

        [SerializeField] private float successHapticAmplitude = 0.6f;
        [SerializeField] private float successHapticDuration = 0.2f;

        [Header("Configuration")]
        [SerializeField] private bool hapticsEnabled = true;
        [SerializeField] private float globalAmplitudeMultiplier = 1.0f;

        private InputDevice leftController;
        private InputDevice rightController;

        public static HapticController Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            InitializeControllers();
        }

        private void InitializeControllers()
        {
            var leftHandDevices = new System.Collections.Generic.List<InputDevice>();
            var rightHandDevices = new System.Collections.Generic.List<InputDevice>();

            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller,
                leftHandDevices
            );

            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller,
                rightHandDevices
            );

            if (leftHandDevices.Count > 0)
                leftController = leftHandDevices[0];

            if (rightHandDevices.Count > 0)
                rightController = rightHandDevices[0];
        }

        /// <summary>
        /// Enables or disables haptic feedback.
        /// </summary>
        public void SetHapticsEnabled(bool enabled)
        {
            hapticsEnabled = enabled;
        }

        /// <summary>
        /// Sets the global amplitude multiplier (0-1).
        /// </summary>
        public void SetAmplitudeMultiplier(float multiplier)
        {
            globalAmplitudeMultiplier = Mathf.Clamp01(multiplier);
        }

        /// <summary>
        /// Sends a light haptic pulse (piece hover).
        /// </summary>
        public void PlayLightHaptic(HandSide hand = HandSide.Both)
        {
            PlayHaptic(hand, lightHapticAmplitude, lightHapticDuration);
        }

        /// <summary>
        /// Sends a medium haptic pulse (piece grab/release).
        /// </summary>
        public void PlayMediumHaptic(HandSide hand = HandSide.Both)
        {
            PlayHaptic(hand, mediumHapticAmplitude, mediumHapticDuration);
        }

        /// <summary>
        /// Sends a strong haptic pulse (error/invalid placement).
        /// </summary>
        public void PlayStrongHaptic(HandSide hand = HandSide.Both)
        {
            PlayHaptic(hand, strongHapticAmplitude, strongHapticDuration);
        }

        /// <summary>
        /// Sends a success haptic pattern (correct placement).
        /// </summary>
        public void PlaySuccessHaptic(HandSide hand = HandSide.Both)
        {
            PlayHaptic(hand, successHapticAmplitude, successHapticDuration);
        }

        /// <summary>
        /// Sends a custom haptic pulse.
        /// </summary>
        public void PlayHaptic(HandSide hand, float amplitude, float duration)
        {
            if (!hapticsEnabled) return;

            float adjustedAmplitude = amplitude * globalAmplitudeMultiplier;

            switch (hand)
            {
                case HandSide.Left:
                    SendHapticImpulse(leftController, adjustedAmplitude, duration);
                    break;
                case HandSide.Right:
                    SendHapticImpulse(rightController, adjustedAmplitude, duration);
                    break;
                case HandSide.Both:
                    SendHapticImpulse(leftController, adjustedAmplitude, duration);
                    SendHapticImpulse(rightController, adjustedAmplitude, duration);
                    break;
            }
        }

        /// <summary>
        /// Plays a haptic pattern for puzzle completion.
        /// </summary>
        public void PlayCompletionPattern()
        {
            if (!hapticsEnabled) return;

            StartCoroutine(CompletionPatternCoroutine());
        }

        private System.Collections.IEnumerator CompletionPatternCoroutine()
        {
            // Triple pulse pattern for completion
            for (int i = 0; i < 3; i++)
            {
                PlayHaptic(HandSide.Both, successHapticAmplitude, 0.1f);
                yield return new WaitForSeconds(0.15f);
            }

            // Final stronger pulse
            yield return new WaitForSeconds(0.1f);
            PlayHaptic(HandSide.Both, strongHapticAmplitude, 0.3f);
        }

        /// <summary>
        /// Plays a haptic pattern for piece snap.
        /// </summary>
        public void PlaySnapPattern(HandSide hand)
        {
            if (!hapticsEnabled) return;

            StartCoroutine(SnapPatternCoroutine(hand));
        }

        private System.Collections.IEnumerator SnapPatternCoroutine(HandSide hand)
        {
            // Quick double pulse for snap feedback
            PlayHaptic(hand, mediumHapticAmplitude, 0.05f);
            yield return new WaitForSeconds(0.08f);
            PlayHaptic(hand, successHapticAmplitude, 0.1f);
        }

        private void SendHapticImpulse(InputDevice device, float amplitude, float duration)
        {
            if (!device.isValid) return;

            HapticCapabilities capabilities;
            if (device.TryGetHapticCapabilities(out capabilities) && capabilities.supportsImpulse)
            {
                device.SendHapticImpulse(0, amplitude, duration);
            }
        }

        /// <summary>
        /// Stops all haptic feedback.
        /// </summary>
        public void StopAllHaptics()
        {
            StopHaptic(leftController);
            StopHaptic(rightController);
        }

        private void StopHaptic(InputDevice device)
        {
            if (!device.isValid) return;

            device.StopHaptics();
        }

        private void OnDestroy()
        {
            StopAllHaptics();
        }
    }

    public enum HandSide
    {
        Left,
        Right,
        Both
    }
}
