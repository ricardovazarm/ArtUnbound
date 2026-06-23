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
    /// Panel de post-juego (GDD 4.8 / 11.4). Sin lenguaje de "premio": muestra un
    /// titulo de momento de museo ("Artwork Complete"), una linea de accion tenue
    /// ("Ready to hang in your gallery"), la instruccion de colgado (pinch / point&click) y,
    /// condicionalmente, la PLACA recien desbloqueada (visual + titulo) si completar la obra
    /// cruzo el umbral de un coleccionable (GDD 8.x, via GameBootstrap.LastEarnedPlaques).
    ///
    /// Colgar NO requiere boton: cuando hay paredes, ShowResults auto-habilita el agarre del
    /// marco (OnPlaceArtworkRequested) y muestra la instruccion. Salir se hace con el boton de
    /// salida del HUD (panel izquierdo). El unico boton de este panel es "Play Again" (re-armar la
    /// misma obra, p.ej. para que otra persona lo intente); por eso ya no hay "Hang it now" ni
    /// "Back to collection".
    /// </summary>
    public class PostGameController : MonoBehaviour
    {
        public event Action OnPlaceArtworkRequested;
        public event Action OnReplayRequested;
        // Conservado por compatibilidad de wiring (GameBootstrap aun se suscribe); ya no lo dispara
        // ningun boton de este panel. Salir = boton del HUD; colgar = auto al detectar pared.
        public event Action OnBackRequested;

        [Header("UI References")]
        [SerializeField] private GameObject panel;
        [Tooltip("Texto principal del panel. Compone titulo + linea de accion (+ titulo de placa si no hay contenedor visual).")]
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
        [Tooltip("Boton 'Play Again': re-arma la misma obra desde cero (misma dificultad). " +
                 "Reusa el viejo replayButton del prefab.")]
        [FormerlySerializedAs("replayButton")]
        [FormerlySerializedAs("hangButton")]
        [SerializeField] private Button playAgainButton;

        [Header("Placa desbloqueada (opcional)")]
        [Tooltip("Contenedor de la placa recien desbloqueada. Se activa SOLO si esta obra otorgo una placa. " +
                 "Si se deja vacio, el hito se muestra como linea de texto dentro del titulo (fallback).")]
        [SerializeField] private GameObject plaqueContainer;
        [Tooltip("RawImage donde se pinta la vista previa 3D de la placa (mismo render que las tarjetas de Collection).")]
        [SerializeField] private RawImage plaqueImage;
        [Tooltip("Encabezado (TMP) que se muestra junto a la placa, p.ej. 'Milestone unlocked!'. " +
                 "La placa ya trae su propio nombre en la imagen, asi que aqui solo va el encabezado, no el titulo.")]
        [FormerlySerializedAs("plaqueTitleText")]
        [SerializeField] private TMP_Text plaqueHeaderText;
        [Tooltip("Texto del encabezado de hito. Tambien se usa como linea de texto si no hay contenedor visual.")]
        [FormerlySerializedAs("milestonePrefix")]
        [SerializeField] private string milestoneHeader = "Milestone unlocked!";

        private PuzzleSessionData sessionData;
        private FrameTier awardedFrame;
        private bool _lastControllerMode;
        private bool _hangingTextInitialized;
        private int lastWallCount = -1;

        private void Awake()
        {
            if (playAgainButton != null)
                playAgainButton.onClick.AddListener(OnPlayAgainClicked);

            if (inputController == null)
                inputController = FindFirstObjectByType<HandTrackingInputController>();

            if (hangInstructionText != null)
                hangInstructionText.gameObject.SetActive(false);

            if (plaqueContainer != null)
                plaqueContainer.SetActive(false);

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

            UpdatePlaqueVisual();

            if (hangInstructionText != null)
            {
                bool hasWalls = lastWallCount > 0;
                hangInstructionText.gameObject.SetActive(hasWalls);
                if (hasWalls) RefreshHangingInstruction();
            }
        }

        /// <summary>
        /// Compone el mensaje del panel: titulo (Cormorant) + linea de accion tenue (Hanken). La linea
        /// de hito solo se agrega aqui como FALLBACK cuando no hay contenedor de placa visual cableado.
        /// </summary>
        private string ComposeMessage()
        {
            string msg = $"{completeTitle}\n<size=55%>{readyToHangLine}</size>";

            // Si hay contenedor visual, el hito se muestra ahi (no duplicar en texto). Como FALLBACK,
            // sin contenedor, mostramos el encabezado + el nombre de la placa para que se sepa cual fue.
            if (plaqueContainer == null)
            {
                var pick = GetEarnedPick();
                if (pick != null && !string.IsNullOrEmpty(pick.title))
                    msg += $"\n<size=50%>{milestoneHeader} {pick.title}</size>";
            }

            return msg;
        }

        /// <summary>
        /// Activa y puebla el contenedor de la placa recien desbloqueada (preview 3D + titulo). Si esta
        /// obra no otorgo placa, o no hay contenedor cableado, lo deja oculto.
        /// </summary>
        private void UpdatePlaqueVisual()
        {
            if (plaqueContainer == null) return;

            var pick = GetEarnedPick();
            if (pick == null)
            {
                plaqueContainer.SetActive(false);
                return;
            }

            plaqueContainer.SetActive(true);

            if (plaqueImage != null)
            {
                Texture preview = CollectiblePreviewRenderer.Instance.GetPreview(pick);
                plaqueImage.texture = preview;
                // Si no se pudo construir la preview, oculta solo la imagen (deja el titulo).
                plaqueImage.enabled = preview != null;

                // La preview es una textura cuadrada; sin esto la RawImage la estira al RectTransform
                // (la placa se veria "alargada"). El AspectRatioFitter la mantiene con su proporcion real.
                if (preview != null)
                {
                    var fitter = plaqueImage.GetComponent<AspectRatioFitter>();
                    if (fitter == null) fitter = plaqueImage.gameObject.AddComponent<AspectRatioFitter>();
                    fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                    fitter.aspectRatio = preview.height > 0 ? (float)preview.width / preview.height : 1f;
                }
            }

            if (plaqueHeaderText != null)
                plaqueHeaderText.text = milestoneHeader;
        }

        /// <summary>
        /// Coleccionable recien desbloqueado al completar esta obra (GameBootstrap.LastEarnedPlaques),
        /// priorizando uno con nombre (no el de estatus) para el mensaje/visual. null si no hubo placa.
        /// </summary>
        private static CollectibleDefinition GetEarnedPick()
        {
            var gb = ArtUnbound.Core.GameBootstrap.Instance;
            List<CollectibleDefinition> earned = gb != null ? gb.LastEarnedPlaques : null;
            if (earned == null || earned.Count == 0) return null;

            CollectibleDefinition pick = null;
            foreach (var p in earned)
            {
                if (p == null) continue;
                if (p.kind != CollectibleKind.Status) { pick = p; break; }
                pick ??= p;
            }
            return pick;
        }

        private void OnPlayAgainClicked()
        {
            ArtUnbound.Feedback.AudioManager.Instance?.PlayButtonClick();
            OnReplayRequested?.Invoke();
            // ReplayPuzzle (GameBootstrap) ya oculta este panel; no llamar Hide() aqui.
        }

        /// <summary>
        /// Deshabilita el boton "Play Again" mientras se cuelga el marco, para que el trigger/pinch
        /// que apunta al cuadro no lo clickee por accidente.
        /// </summary>
        public void SetHangingMode(bool isHanging)
        {
            if (playAgainButton != null)
                playAgainButton.interactable = !isHanging;
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

            if (plaqueContainer != null)
                plaqueContainer.SetActive(false);
        }

        public PuzzleSessionData GetSessionData() => sessionData;
        public FrameTier GetAwardedFrame() => awardedFrame;

        private void OnDestroy()
        {
            if (playAgainButton != null)
                playAgainButton.onClick.RemoveListener(OnPlayAgainClicked);
        }
    }
}
