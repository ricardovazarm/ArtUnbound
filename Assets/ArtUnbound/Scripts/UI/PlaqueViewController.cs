using System;
using ArtUnbound.Input;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Vista para colgar una PLACA desbloqueada desde Collection (analoga al detalle de obra).
    /// Muestra una ventana con la instruccion de colgado (manos vs control, configurable) mientras la
    /// placa 3D flota al frente, agarrable y colgable en una pared como en el PostGame. El boton Close
    /// cancela: GameBootstrap destruye la placa flotante y regresa a Collection.
    ///
    /// Es solo panel: GameBootstrap es dueno de la placa (igual que PostGame no es dueno del marco).
    /// </summary>
    public class PlaqueViewController : MonoBehaviour
    {
        public event Action OnCloseRequested;

        [Header("UI References")]
        [SerializeField] private GameObject panel;

        [Header("Hang Plaque Instruction")]
        [Tooltip("Texto (TMP) con la instruccion de colgar la placa.")]
        [SerializeField] private TMP_Text instructionText;
        [Tooltip("HandTrackingInputController para elegir el texto de manos vs control.")]
        [SerializeField] private HandTrackingInputController inputController;
        [TextArea(2, 4)]
        [SerializeField] private string handsInstruction = "Pinch to grab the plaque and release it against a wall to hang your plaque.";
        [TextArea(2, 4)]
        [SerializeField] private string controllerInstruction = "Point and click to select the plaque, then click on a wall to hang it.";

        [Header("Buttons")]
        [SerializeField] private Button closeButton;

        /// <summary>Transform del panel (PlaquePanel): GameBootstrap lo usa para colocar la placa 3D al frente del panel.</summary>
        public Transform PanelTransform => panel != null ? panel.transform : transform;

        private bool _hangingTextInitialized;
        private bool _lastControllerMode;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseClicked);

            if (inputController == null)
                inputController = FindFirstObjectByType<HandTrackingInputController>();

            // NO llamar Hide() aqui: si el panel arranca apagado, Awake() corre la primera vez que
            // Show() lo activa -> volveria a apagarlo en el mismo frame. El estado inicial oculto lo
            // garantiza NativeGalleryController.Initialize() (llama a Hide()).
        }

        private void Update()
        {
            if (instructionText == null || inputController == null) return;
            if (!instructionText.gameObject.activeInHierarchy) return;

            bool isController = inputController.HasValidController;
            if (_hangingTextInitialized && isController == _lastControllerMode) return;
            _hangingTextInitialized = true;
            _lastControllerMode = isController;
            instructionText.text = isController ? controllerInstruction : handsInstruction;
        }

        /// <summary>Aplica de inmediato el texto de instruccion correcto (manos vs control).</summary>
        public void RefreshInstruction()
        {
            if (instructionText == null) return;
            if (inputController == null)
                inputController = FindFirstObjectByType<HandTrackingInputController>();

            bool isController = inputController != null && inputController.HasValidController;
            _lastControllerMode = isController;
            _hangingTextInitialized = true;
            instructionText.text = isController ? controllerInstruction : handsInstruction;
        }

        public void Show()
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
            if (panel != null)
                panel.SetActive(true);
            RefreshInstruction();
        }

        public void Hide()
        {
            if (panel != null)
                panel.SetActive(false);
            else
                gameObject.SetActive(false);
        }

        private void OnCloseClicked()
        {
            ArtUnbound.Feedback.AudioManager.Instance?.PlayButtonClick();
            OnCloseRequested?.Invoke();
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(OnCloseClicked);
        }
    }
}
