using System;
using System.Collections.Generic;
using ArtUnbound.Data;
using ArtUnbound.Gameplay;
using ArtUnbound.Input;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using TMPro;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Panel de post-juego (GDD 4.8 / 11.4). SIN medalla ni lenguaje de "premio": muestra un
    /// titulo de momento de museo ("Artwork Complete"), una linea de accion tenue
    /// ("Ready to hang in your gallery") y, condicionalmente, una linea de hito si completar la
    /// obra cruzo el umbral de una placa (GDD 8.x, via GameBootstrap.LastEarnedPlaques).
    /// Botones: "Hang it now" (primario -> colgar) y "Back to collection" (secundario).
    /// </summary>
    public class PostGameController : MonoBehaviour
    {
        public event Action OnPlaceArtworkRequested;
        public event Action OnBackRequested;
        // Conservado por compatibilidad de wiring; ya no lo dispara ningun boton (GDD sin replay aqui).
        public event Action OnReplayRequested;

        [Header("UI References")]
        [SerializeField] private GameObject panel;
        [Tooltip("Texto principal del panel. Compone titulo + linea de accion + linea de hito condicional.")]
        [FormerlySerializedAs("medalText")]
        [SerializeField] private TextMeshProUGUI titleText;

        [Header("Textos (GDD 4.8)")]
        [SerializeField] private string completeTitle  = "Artwork Complete";
        [SerializeField] private string readyToHangLine = "Ready to hang in your gallery";

        [Header("Hang Artwork Instruction")]
        [Tooltip("Texto (TMP) sobre el cuadro (panel central) con la instruccion de colgarlo. Solo si hay paredes.")]
        [SerializeField] private TMP_Text hangInstructionText;
        [Tooltip("HandTrackingInputController para elegir el texto de manos vs control.")]
        [SerializeField] private HandTrackingInputController inputController;
        [TextArea(2, 4)]
        [SerializeField] private string handsHangingInstruction = "Pinch to grab the frame and release it against a wall to hang your masterpiece.";
        [TextArea(2, 4)]
        [SerializeField] private string controllerHangingInstruction = "Point and click to select the frame, then click on a wall to hang it.";

        [Header("Buttons")]
        [Tooltip("Boton primario 'Hang it now' (lleva directo a colgar). Reusa el viejo replayButton.")]
        [FormerlySerializedAs("replayButton")]
        [SerializeField] private Button hangButton;
        [Tooltip("Boton secundario 'Back to collection' (vuelve al menu).")]
        [SerializeField] private Button backButton;

        private PuzzleSessionData sessionData;
        private FrameTier awardedFrame;
        private bool _lastControllerMode;
        private bool _hangingTextInitialized;
        private int lastWallCount = -1;

        private void Awake()
        {
            if (hangButton != null)
                hangButton.onClick.AddListener(OnHangClicked);
            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);

            if (inputController == null)
                inputController = FindFirstObjectByType<HandTrackingInputController>();

            if (hangInstructionText != null)
                hangInstructionText.gameObject.SetActive(false);

            Hide();
        }

        private void Update()
        {
            if (hangInstructionText == null || inputController == null) return;
            if (!hangInstructionText.gameObject.activeInHierarchy) return;

            bool isController = inputController.HasValidController;
            if (_hangingTextInitialized && isController == _lastControllerMode) return;
            _hangingTextInitialized = true;
            _lastControllerMode = isController;
            hangInstructionText.text = isController ? controllerHangingInstruction : handsHangingInstruction;
        }

        /// <summary>Aplica de inmediato el texto de instruccion correcto (manos vs control).</summary>
        public void RefreshHangingInstruction()
        {
            if (hangInstructionText == null) return;
            if (inputController == null)
                inputController = FindFirstObjectByType<HandTrackingInputController>();

            bool isController = inputController != null && inputController.HasValidController;
            _lastControllerMode = isController;
            _hangingTextInitialized = true;
            hangInstructionText.text = isController ? controllerHangingInstruction : handsHangingInstruction;
        }

        /// <summary>
        /// Muestra el panel de resultados. wallCount: paredes detectadas (>0 habilita colgado auto).
        /// </summary>
        public void ShowResults(PuzzleSessionData data, FrameTier frame, int wallCount = -1)
        {
            sessionData = data;
            awardedFrame = frame;
            lastWallCount = wallCount;

            UpdateUI();
            Show();

            if (lastWallCount > 0)
            {
                Debug.Log($"[PostGame] Walls detected ({lastWallCount}), auto-enabling hanging mode");
                OnPlaceArtworkRequested?.Invoke();
            }
            else
            {
                Debug.Log("[PostGame] No walls detected, hanging mode NOT auto-enabled");
            }
        }

        private void UpdateUI()
        {
            if (titleText != null)
                titleText.text = ComposeMessage();

            if (hangInstructionText != null)
            {
                bool hasWalls = lastWallCount > 0;
                hangInstructionText.gameObject.SetActive(hasWalls);
                if (hasWalls) RefreshHangingInstruction();
            }
        }

        /// <summary>
        /// Compone el mensaje del panel: titulo (Cormorant) + linea de accion tenue (Hanken) +
        /// linea de hito CONDICIONAL solo si esta obra cruzo el umbral de una placa.
        /// </summary>
        private string ComposeMessage()
        {
            string msg = $"{completeTitle}\n<size=55%>{readyToHangLine}</size>";

            string milestone = GetMilestoneLine();
            if (!string.IsNullOrEmpty(milestone))
                msg += $"\n<size=50%>{milestone}</size>";

            return msg;
        }

        /// <summary>Linea de hito si completar esta obra otorgo alguna placa (GameBootstrap.LastEarnedPlaques).</summary>
        private static string GetMilestoneLine()
        {
            var gb = ArtUnbound.Core.GameBootstrap.Instance;
            List<CollectibleDefinition> earned = gb != null ? gb.LastEarnedPlaques : null;
            if (earned == null || earned.Count == 0) return null;
            // Prioriza un coleccionable con nombre (no el de estatus) para el mensaje.
            CollectibleDefinition pick = null;
            foreach (var p in earned)
            {
                if (p == null) continue;
                if (p.kind != CollectibleKind.Status) { pick = p; break; }
                pick ??= p;
            }
            return pick != null ? $"Milestone unlocked: {pick.title}" : null;
        }

        /// <summary>Desactiva los botones mientras se cuelga (para no clickear por accidente).</summary>
        public void SetHangingMode(bool isHanging)
        {
            if (hangButton != null) hangButton.interactable = !isHanging;
            if (backButton != null) backButton.interactable = !isHanging;
        }

        public void Show()
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
            if (panel != null)
                panel.SetActive(true);
            UpdateUI();
        }

        public void Hide()
        {
            if (panel != null)
                panel.SetActive(false);
            else
                gameObject.SetActive(false);

            if (hangInstructionText != null)
                hangInstructionText.gameObject.SetActive(false);
        }

        private void OnHangClicked()
        {
            ArtUnbound.Feedback.AudioManager.Instance?.PlayButtonClick();
            OnPlaceArtworkRequested?.Invoke();
        }

        private void OnBackClicked()
        {
            ArtUnbound.Feedback.AudioManager.Instance?.PlayButtonClick();
            OnBackRequested?.Invoke();
            Hide();
        }

        public PuzzleSessionData GetSessionData() => sessionData;
        public FrameTier GetAwardedFrame() => awardedFrame;

        private void OnDestroy()
        {
            if (hangButton != null) hangButton.onClick.RemoveListener(OnHangClicked);
            if (backButton != null) backButton.onClick.RemoveListener(OnBackClicked);
        }
    }
}
