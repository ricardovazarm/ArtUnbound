using System.Collections;
using ArtUnbound.Core;
using ArtUnbound.Data;
using ArtUnbound.Services;
using UnityEngine;

namespace ArtUnbound.Tutorial
{
    /// <summary>
    /// Orchestrates the three ghost-hand tutorial demos on first run:
    ///   1. Gallery visible  → tap demo over the first catalog card
    ///   2. Detail opened    → tap demo over the easy-difficulty button
    ///   3. Puzzle playing   → pinch-and-carry demo from a tray piece to its correct slot
    /// Each demo plays once and fades out; input is never blocked and no confirmation is
    /// awaited. Armed only on the MR path when !onboardingCompleted OR settings.showOnboarding.
    /// Completing demo 3 persists onboardingCompleted=true and showOnboarding=false.
    /// The legacy carousel OnboardingController is intentionally not used.
    /// </summary>
    public class TutorialFlowController : MonoBehaviour
    {
        [SerializeField] private TutorialHandController tutorialHand;
        [SerializeField] private ArtUnbound.UI.NativeGalleryController nativeGallery;
        [SerializeField] private ArtUnbound.Gameplay.PuzzleBoard puzzleBoard;

        [Tooltip("Espera tras revelarse la galeria antes de la demo 1 (deja asentar el layout del grid).")]
        [SerializeField] private float galleryDemoDelay = 0.5f;
        [Tooltip("Espera tras entrar a Playing antes de la demo 3 (el tray asienta posiciones y el jugador orienta la vista).")]
        [SerializeField] private float playingDemoDelay = 0.75f;

        private SaveDataService _saveDataService;
        private bool _armed;
        private bool _step1Done, _step2Done, _step3Done;
        private int _highlightedSlot = -1;

        /// <summary>Called by GameBootstrap after the save is loaded. Decides if the tutorial runs this session.</summary>
        public void Initialize(SaveDataService saveDataService, SaveData saveData)
        {
            _saveDataService = saveDataService;

            bool showAgain = saveData?.settings != null && saveData.settings.showOnboarding;
            _armed = saveData != null && (!saveData.onboardingCompleted || showAgain);
            Debug.Log($"[Tutorial] Initialize: armed={_armed} (onboardingCompleted={saveData?.onboardingCompleted}, showOnboarding={showAgain})");
            if (!_armed) return;

            if (GameBootstrap.Instance != null)
                GameBootstrap.Instance.OnGameStateChanged += HandleStateChanged;

            if (nativeGallery != null)
            {
                nativeGallery.OnGalleryRevealed += HandleGalleryRevealed;
                nativeGallery.OnDetailShown     += HandleDetailShown;
                nativeGallery.OnDetailClosed    += HandleDetailClosed;
            }
        }

        private void OnDestroy()
        {
            if (GameBootstrap.Instance != null)
                GameBootstrap.Instance.OnGameStateChanged -= HandleStateChanged;

            if (nativeGallery != null)
            {
                nativeGallery.OnGalleryRevealed -= HandleGalleryRevealed;
                nativeGallery.OnDetailShown     -= HandleDetailShown;
                nativeGallery.OnDetailClosed    -= HandleDetailClosed;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  PASO 1 — tap sobre la primera card del catalogo
        // ─────────────────────────────────────────────────────────────────────

        private void HandleGalleryRevealed()
        {
            if (!CanPlay() || _step1Done) return;
            if (GameBootstrap.Instance != null &&
                GameBootstrap.Instance.CurrentState != GameState.MainMenu &&
                GameBootstrap.Instance.CurrentState != GameState.ArtworkSelection) return;

            _step1Done = true;
            StartCoroutine(PlayCardDemoDelayed());
        }

        private IEnumerator PlayCardDemoDelayed()
        {
            yield return new WaitForSeconds(galleryDemoDelay);

            var card = nativeGallery != null ? nativeGallery.FirstCatalogCardTransform : null;
            if (card == null)
            {
                Debug.Log("[Tutorial] Paso 1 omitido: no hay card visible");
                yield break;
            }

            Debug.Log("[Tutorial] Paso 1: demo de tap sobre la primera card");
            tutorialHand.PlayTapDemo(card, loops: 2);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  PASO 2 — tap sobre el boton de dificultad facil
        // ─────────────────────────────────────────────────────────────────────

        private void HandleDetailShown()
        {
            if (!CanPlay() || _step2Done) return;

            tutorialHand.StopDemo(); // si la demo 1 seguia corriendo

            var easyButton = nativeGallery != null ? nativeGallery.EasyButtonTransform : null;
            if (easyButton == null)
            {
                // Obra bloqueada (componente de compra visible): no marcar hecho,
                // se reintenta cuando el jugador abra una obra desbloqueada.
                Debug.Log("[Tutorial] Paso 2 pospuesto: detail de obra bloqueada");
                return;
            }

            _step2Done = true;
            Debug.Log("[Tutorial] Paso 2: demo de tap sobre el boton de dificultad");
            tutorialHand.PlayTapDemo(easyButton, loops: 2);
        }

        private void HandleDetailClosed()
        {
            if (tutorialHand != null && tutorialHand.IsPlaying)
                tutorialHand.StopDemo();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  PASO 3 — pinch de una pieza del tray hasta su slot correcto
        // ─────────────────────────────────────────────────────────────────────

        private void HandleStateChanged(GameState newState)
        {
            if (newState == GameState.Playing)
            {
                if (!CanPlay() || _step3Done) return;
                _step3Done = true;
                StartCoroutine(PlayPieceDemoDelayed());
            }
            else if (tutorialHand != null && tutorialHand.IsPlaying)
            {
                // Salir de la pantalla actual (menu, pausa, quit del puzzle) corta cualquier demo.
                tutorialHand.StopDemo();
                ClearHighlightIfAny();
            }
        }

        private IEnumerator PlayPieceDemoDelayed()
        {
            // El tray asienta posiciones/visibilidad en sus primeros Update()
            yield return null;
            yield return null;
            yield return new WaitForSeconds(playingDemoDelay);

            if (GameBootstrap.Instance == null || GameBootstrap.Instance.CurrentState != GameState.Playing)
                yield break;

            var piece = puzzleBoard != null && puzzleBoard.PieceTray != null
                ? puzzleBoard.PieceTray.GetFirstVisiblePiece()
                : null;
            if (piece == null)
            {
                Debug.Log("[Tutorial] Paso 3 omitido: no hay pieza visible en el tray");
                CompleteTutorial(); // no dejar el tutorial rearmandose para siempre
                yield break;
            }

            Vector3 from = piece.transform.position;
            // NO usar piece.CorrectSlotIndex: queda invalido tras la generacion (ver
            // PuzzleBoard.GetCorrectSlotIndexForPiece). Buscar el slot por pieceId, como el snap.
            int slot = puzzleBoard.GetCorrectSlotIndexForPiece(piece.PieceId);
            if (slot < 0)
            {
                Debug.LogWarning($"[Tutorial] Paso 3 omitido: sin slot para pieza {piece.PieceId}");
                CompleteTutorial();
                yield break;
            }
            Vector3 to = puzzleBoard.GetSlotPosition(slot);

            Debug.Log($"[Tutorial] Paso 3: demo de pinch pieza {piece.PieceId} -> slot {slot}");
            _highlightedSlot = slot;
            puzzleBoard.HighlightSlot(slot);

            tutorialHand.PlayGrabDemo(from, to, loops: 2, onFinished: () =>
            {
                ClearHighlightIfAny();
                CompleteTutorial();
            });
        }

        private void ClearHighlightIfAny()
        {
            if (_highlightedSlot < 0) return;
            _highlightedSlot = -1;
            puzzleBoard?.ClearSlotHighlight();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  ESTADO / PERSISTENCIA
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Tutorial armado, con actor asignado y NO en modo VR (el tutorial es solo de la ruta MR).</summary>
        private bool CanPlay()
        {
            if (!_armed || tutorialHand == null) return false;
            if (GameBootstrap.Instance != null && GameBootstrap.Instance.IsVRMode) return false;
            return true;
        }

        /// <summary>
        /// Persiste el tutorial como visto (al terminar la demo 3). Si el jugador no llega aqui
        /// en la sesion, no se persiste nada y el tutorial completo se rearma al siguiente
        /// arranque — aceptable porque nunca bloquea.
        /// </summary>
        private void CompleteTutorial()
        {
            if (!_armed) return;
            _armed = false;

            Debug.Log("[Tutorial] Completo: onboardingCompleted=true, showOnboarding=false");
            _saveDataService?.CompleteOnboarding();

            var data = _saveDataService?.GetCachedData();
            if (data?.settings != null)
            {
                data.settings.showOnboarding = false;
                _saveDataService.MarkDirty();
            }
        }
    }
}
