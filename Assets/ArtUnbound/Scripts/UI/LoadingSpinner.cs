using UnityEngine;
using UnityEngine.UI;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Simple loading spinner for VR.
    /// Rotates continuously while active.
    /// </summary>
    public class LoadingSpinner : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private float rotationSpeed = 360f; // Degrees per second
        [SerializeField] private Transform spinnerTransform; // The actual spinning element
        [SerializeField] private CanvasGroup canvasGroup; // For fading
        
        [Header("Optional Text")]
        [SerializeField] private TMPro.TextMeshProUGUI loadingText;
        [SerializeField] private string defaultText = "Inicializando...";

        private bool isSpinning = false;

        private void Awake()
        {
            // If no spinner transform specified, use self
            if (spinnerTransform == null)
            {
                spinnerTransform = transform;
            }

            // Get or add CanvasGroup for fading
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            // Start hidden
            Hide();
        }

        private void Update()
        {
            if (isSpinning && spinnerTransform != null)
            {
                // Rotate around Z axis
                spinnerTransform.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);
            }
        }

        /// <summary>
        /// Shows the spinner and starts rotating.
        /// </summary>
        public void Show(string message = null)
        {
            gameObject.SetActive(true);
            isSpinning = true;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            if (loadingText != null)
            {
                loadingText.text = message ?? defaultText;
            }

            Debug.Log($"[LoadingSpinner] Showing: {message ?? defaultText}");
        }

        /// <summary>
        /// Hides the spinner and stops rotating.
        /// </summary>
        public void Hide()
        {
            isSpinning = false;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            gameObject.SetActive(false);

            Debug.Log("[LoadingSpinner] Hidden");
        }

        /// <summary>
        /// Sets the loading text message.
        /// </summary>
        public void SetMessage(string message)
        {
            if (loadingText != null)
            {
                loadingText.text = message;
            }
        }

        /// <summary>
        /// Sets the rotation speed.
        /// </summary>
        public void SetRotationSpeed(float speed)
        {
            rotationSpeed = speed;
        }
    }
}
