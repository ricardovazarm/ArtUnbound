using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Controls the first-time user onboarding/tutorial flow.
    /// </summary>
    public class OnboardingController : MonoBehaviour
    {
        public event Action OnOnboardingComplete;
        public event Action OnOnboardingSkipped;

        [Header("Panel")]
        [SerializeField] private GameObject panel;

        [Header("Step Content")]
        [SerializeField] private TextMeshProUGUI stepTitleText;
        [SerializeField] private TextMeshProUGUI stepDescriptionText;
        [SerializeField] private Image stepImage;
        [SerializeField] private GameObject stepAnimationContainer;

        [Header("Navigation")]
        [SerializeField] private Button nextButton;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private TextMeshProUGUI nextButtonText;

        [Header("Progress Indicator")]
        [SerializeField] private Transform dotsContainer;
        [SerializeField] private GameObject dotPrefab;
        [SerializeField] private Color dotActiveColor = Color.white;
        [SerializeField] private Color dotInactiveColor = new Color(1f, 1f, 1f, 0.3f);

        [Header("Onboarding Steps")]
        [SerializeField] private OnboardingStep[] steps;

        public int CurrentStepIndex => currentStepIndex;
        public bool IsComplete => isComplete;

        private int currentStepIndex = 0;
        private bool isComplete = false;
        private Image[] dotImages;

        [Serializable]
        public class OnboardingStep
        {
            public string title;
            [TextArea(2, 4)]
            public string description;
            public Sprite image;
            public GameObject animationPrefab;
            public bool requiresHandTracking;
            public bool requiresPassthrough;
        }

        private void Awake()
        {
            if (nextButton != null)
                nextButton.onClick.AddListener(NextStep);

            if (previousButton != null)
                previousButton.onClick.AddListener(PreviousStep);

            if (skipButton != null)
                skipButton.onClick.AddListener(Skip);

            Hide();
        }

        /// <summary>
        /// Starts the onboarding from the beginning.
        /// </summary>
        public void StartOnboarding()
        {
            currentStepIndex = 0;
            isComplete = false;

            CreateProgressDots();
            UpdateStep();
            Show();
        }

        /// <summary>
        /// Goes to the next step or completes the onboarding.
        /// </summary>
        public void NextStep()
        {
            if (currentStepIndex < steps.Length - 1)
            {
                currentStepIndex++;
                UpdateStep();
            }
            else
            {
                Complete();
            }
        }

        /// <summary>
        /// Goes to the previous step.
        /// </summary>
        public void PreviousStep()
        {
            if (currentStepIndex > 0)
            {
                currentStepIndex--;
                UpdateStep();
            }
        }

        /// <summary>
        /// Skips the onboarding.
        /// </summary>
        public void Skip()
        {
            isComplete = true;
            Hide();
            OnOnboardingSkipped?.Invoke();
        }

        /// <summary>
        /// Completes the onboarding.
        /// </summary>
        private void Complete()
        {
            isComplete = true;
            Hide();
            OnOnboardingComplete?.Invoke();
        }

        /// <summary>
        /// Jumps to a specific step.
        /// </summary>
        public void GoToStep(int index)
        {
            if (index >= 0 && index < steps.Length)
            {
                currentStepIndex = index;
                UpdateStep();
            }
        }

        private void UpdateStep()
        {
            if (steps == null || steps.Length == 0) return;

            var step = steps[currentStepIndex];

            if (stepTitleText != null)
                stepTitleText.text = step.title;

            if (stepDescriptionText != null)
                stepDescriptionText.text = step.description;

            if (stepImage != null)
            {
                if (step.image != null)
                {
                    stepImage.gameObject.SetActive(true);
                    stepImage.sprite = step.image;
                }
                else
                {
                    stepImage.gameObject.SetActive(false);
                }
            }

            UpdateAnimation(step);
            UpdateNavigation();
            UpdateProgressDots();
        }

        private void UpdateAnimation(OnboardingStep step)
        {
            if (stepAnimationContainer == null) return;

            // Clear existing animations
            foreach (Transform child in stepAnimationContainer.transform)
            {
                Destroy(child.gameObject);
            }

            // Instantiate new animation if available
            if (step.animationPrefab != null)
            {
                Instantiate(step.animationPrefab, stepAnimationContainer.transform);
            }
        }

        private void UpdateNavigation()
        {
            bool isFirstStep = currentStepIndex == 0;
            bool isLastStep = currentStepIndex == steps.Length - 1;

            if (previousButton != null)
                previousButton.gameObject.SetActive(!isFirstStep);

            if (nextButtonText != null)
                nextButtonText.text = isLastStep ? "Comenzar" : "Siguiente";

            if (skipButton != null)
                skipButton.gameObject.SetActive(!isLastStep);
        }

        private void CreateProgressDots()
        {
            if (dotsContainer == null || dotPrefab == null) return;

            // Clear existing dots
            foreach (Transform child in dotsContainer)
            {
                Destroy(child.gameObject);
            }

            // Create new dots
            dotImages = new Image[steps.Length];
            for (int i = 0; i < steps.Length; i++)
            {
                var dot = Instantiate(dotPrefab, dotsContainer);
                dotImages[i] = dot.GetComponent<Image>();
            }
        }

        private void UpdateProgressDots()
        {
            if (dotImages == null) return;

            for (int i = 0; i < dotImages.Length; i++)
            {
                if (dotImages[i] != null)
                {
                    dotImages[i].color = i == currentStepIndex ? dotActiveColor : dotInactiveColor;
                }
            }
        }

        /// <summary>
        /// Gets the default onboarding steps if none are configured.
        /// </summary>
        public static OnboardingStep[] GetDefaultSteps()
        {
            return new OnboardingStep[]
            {
                new OnboardingStep
                {
                    title = "¡Bienvenido a Art Unbound!",
                    description = "Resuelve puzzles de obras de arte famosas en realidad mixta y crea tu propia galería personal."
                },
                new OnboardingStep
                {
                    title = "Usa tus manos",
                    description = "Utiliza tus manos para agarrar y mover las piezas del puzzle. Pellizca para seleccionar y suelta para colocar.",
                    requiresHandTracking = true
                },
                new OnboardingStep
                {
                    title = "Modo Galería",
                    description = "Ancla el puzzle a una pared real de tu espacio. Busca superficies planas y selecciona donde quieres colocar el puzzle."
                },
                new OnboardingStep
                {
                    title = "Modo Confort",
                    description = "El puzzle flota frente a ti en una posición ergonómica. Ideal para sesiones largas de juego.",
                    requiresPassthrough = true
                },
                new OnboardingStep
                {
                    title = "Colecciona marcos",
                    description = "Completa puzzles rápidamente para ganar marcos de mejor calidad: Madera, Bronce, Plata, Oro y Ébano."
                },
                new OnboardingStep
                {
                    title = "Tu galería personal",
                    description = "Cuelga tus obras completadas en las paredes de tu espacio real y presume tu colección."
                }
            };
        }

        public void Show()
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            if (panel != null)
                panel.SetActive(true);
        }

        public void Hide()
        {
            if (panel != null)
                panel.SetActive(false);
            else
                gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (nextButton != null) nextButton.onClick.RemoveAllListeners();
            if (previousButton != null) previousButton.onClick.RemoveAllListeners();
            if (skipButton != null) skipButton.onClick.RemoveAllListeners();
        }
    }
}
