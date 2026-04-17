using System.Collections.Generic;
using ArtUnbound.Input;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Maneja el input del Radial Grid Gallery:
    ///   • Palma abierta frente al panel  → modo navegación (mueve el grid)
    ///   • Pinch (pulgar+índice)          → selección de pintura
    ///
    /// Llama a RadialGridGalleryController mediante métodos públicos.
    /// Solo está activo mientras la galería está visible (SetActive).
    ///
    /// Detección de palma abierta:
    ///   1. La mano está dentro de la zona del panel (en frente, dentro de palmActivationDepth)
    ///   2. Distancia pulgar↔índice > pinchThresholdM  (no está haciendo pinch)
    ///   3. Distancia dedo medio↔muñeca > fingerExtensionM  (no está cerrando el puño)
    /// </summary>
    public class RadialGridInputHandler : MonoBehaviour
    {
        // ── Referencias ─────────────────────────────────────────────────────────
        [Header("Referencias")]
        [Tooltip("Controlador principal de la galería.")]
        [SerializeField] private RadialGridGalleryController gallery;

        [Tooltip("HandTrackingInputController del juego (para eventos de pinch).")]
        [SerializeField] private HandTrackingInputController handTracking;

        [Tooltip("Transform raíz del grid — se usa para calcular si la mano está en la zona correcta.")]
        [SerializeField] private Transform gridRoot;

        // ── Parámetros de navegación ─────────────────────────────────────────────
        [Header("Navegación por palma")]
        [Tooltip("Multiplicador de sensibilidad del scroll. Más alto = el grid se mueve más rápido.")]
        [SerializeField] [Range(0.5f, 3.0f)] private float navigationSensitivity = 1.4f;

        [Tooltip("Distancia máxima (metros) entre la mano y el plano del panel para activar navegación.")]
        [SerializeField] [Range(0.10f, 0.60f)] private float palmActivationDepth = 0.40f;

        [Tooltip("Semiancho del área activa del panel en X (metros). Fuera de este rango se ignora la mano.")]
        [SerializeField] [Range(0.30f, 1.00f)] private float panelHalfWidth = 0.70f;

        [Tooltip("Semialto del área activa del panel en Y (metros).")]
        [SerializeField] [Range(0.20f, 0.80f)] private float panelHalfHeight = 0.60f;

        // ── Parámetros de detección de mano abierta ──────────────────────────────
        [Header("Detección de mano abierta")]
        [Tooltip("Distancia mínima (metros) pulgar↔índice para considerar la mano NO haciendo pinch.")]
        [SerializeField] [Range(0.02f, 0.08f)] private float pinchThresholdM = 0.04f;

        [Tooltip("Distancia mínima (metros) dedo-medio↔muñeca para considerar los dedos extendidos.")]
        [SerializeField] [Range(0.05f, 0.15f)] private float fingerExtensionM = 0.09f;

        // ── Parámetros de dwell ──────────────────────────────────────────────────
        [Header("Activación por dwell")]
        [Tooltip("Tiempo (s) que la palma debe permanecer en la zona para activar el modo navegación.")]
        [SerializeField] [Range(0.3f, 3.0f)] private float dwellActivationTime = 1.5f;

        // ── Parámetros de selección ──────────────────────────────────────────────
        [Header("Selección por pinch")]
        [Tooltip("Radio (metros) de búsqueda de celda al hacer pinch.")]
        [SerializeField] [Range(0.01f, 0.08f)] private float selectionRadius = 0.04f;

        [Tooltip("Distancia máxima (metros) al plano del grid para que el pinch cuente como selección.")]
        [SerializeField] [Range(0.05f, 0.25f)] private float selectionDepthTolerance = 0.15f;

        // ── Estado interno ────────────────────────────────────────────────────────
        private XRHandSubsystem _handSubsystem;
        private bool            _isActive;
        private bool            _isNavigating;
        private bool            _navigatedThisFrame;
        private float           _dwellTimer;
        private Vector3         _lastPalmPos;

        // Historial de velocidad para calcular inercia al soltar
        private readonly Queue<Vector3> _velocityHistory = new Queue<Vector3>();
        private const int VEL_HISTORY_SIZE = 6;

        // ─────────────────────────────────────────────────────────────────────────
        private void Start()
        {
            if (handTracking != null)
            {
                handTracking.OnPinchStart += OnPinchStart;
            }

            TryGetHandSubsystem();
        }

        private void OnDestroy()
        {
            if (handTracking != null)
                handTracking.OnPinchStart -= OnPinchStart;
        }

        // ─────────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Activa o desactiva el handler.
        /// Cuando se desactiva, termina la navegación en curso (con inercia).
        /// </summary>
        public void SetActive(bool active)
        {
            _isActive = active;
            if (!active)
            {
                _dwellTimer = 0f;
                gallery?.OnDwellProgress(0f);
                EndNavigation();
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        private void Update()
        {
            if (!_isActive) return;

            if (_handSubsystem == null || !_handSubsystem.running)
            {
                TryGetHandSubsystem();
                return;
            }

            _navigatedThisFrame = false;

            // Revisamos primero la mano derecha; si no hay navegación, revisamos la izquierda
            TryNavigateWithHand(_handSubsystem.rightHand);
            if (!_navigatedThisFrame)
                TryNavigateWithHand(_handSubsystem.leftHand);

            // Ninguna mano cumplió las condiciones este frame
            if (!_navigatedThisFrame)
            {
                // Resetear dwell si estaba en progreso
                if (_dwellTimer > 0f)
                {
                    _dwellTimer = 0f;
                    gallery?.OnDwellProgress(0f);
                }
                // Terminar navegación si estaba activa
                if (_isNavigating)
                    EndNavigation();
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Verifica si <paramref name="hand"/> debe activar/continuar la navegación.
        /// </summary>
        private void TryNavigateWithHand(XRHand hand)
        {
            if (!hand.isTracked) return;

            if (!hand.GetJoint(XRHandJointID.Palm).TryGetPose(out Pose palmPose)) return;

            // ¿La mano está frente al panel y en la zona activa?
            if (!IsHandInPanelZone(palmPose.position)) return;

            // ¿La mano está abierta (no en pinch ni puño)?
            if (!IsHandOpen(hand)) return;

            _navigatedThisFrame = true;

            if (!_isNavigating)
            {
                // ── Fase de dwell: esperar dwellActivationTime segundos ──────────
                _dwellTimer += Time.deltaTime;
                float progress = Mathf.Clamp01(_dwellTimer / dwellActivationTime);
                gallery?.OnDwellProgress(progress);

                if (_dwellTimer < dwellActivationTime) return;   // todavía esperando

                // Dwell completado → activar navegación
                _dwellTimer = 0f;
                gallery?.OnDwellProgress(0f);   // reset visual inmediatamente
                _isNavigating = true;
                _lastPalmPos  = palmPose.position;
                _velocityHistory.Clear();
                gallery?.OnPalmNavigationStart();
                return;
            }

            // Continuación: calcular delta y enviarlo al controlador
            Vector3 worldDelta = palmPose.position - _lastPalmPos;
            _lastPalmPos = palmPose.position;

            // Registrar velocidad para inercia
            if (Time.deltaTime > 0f)
            {
                _velocityHistory.Enqueue(worldDelta / Time.deltaTime);
                if (_velocityHistory.Count > VEL_HISTORY_SIZE)
                    _velocityHistory.Dequeue();
            }

            // Solo enviar delta si supera el ruido mínimo (~0.5 mm)
            if (worldDelta.magnitude > 0.0005f)
            {
                Vector2 delta2D = new Vector2(worldDelta.x, worldDelta.y) * navigationSensitivity;
                gallery?.OnPalmNavigationDelta(delta2D);
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Termina la navegación y pasa la velocidad promedio al controlador
        /// para que aplique inercia.
        /// </summary>
        private void EndNavigation()
        {
            if (!_isNavigating) return;
            _isNavigating = false;

            Vector2 avgVelocity = Vector2.zero;
            if (_velocityHistory.Count > 0)
            {
                Vector3 sum = Vector3.zero;
                foreach (var v in _velocityHistory) sum += v;
                sum /= _velocityHistory.Count;
                avgVelocity = new Vector2(sum.x, sum.y) * navigationSensitivity;
            }

            gallery?.OnPalmNavigationEnded(avgVelocity);
            _velocityHistory.Clear();
        }

        // ─────────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Callback de HandTrackingInputController cuando se detecta un pinch.
        /// Delega al controlador para que encuentre la celda más cercana.
        /// </summary>
        private void OnPinchStart(Vector3 pinchWorldPos, Quaternion _)
        {
            if (!_isActive || _isNavigating) return;
            gallery?.OnPinchAtPosition(pinchWorldPos, selectionRadius, selectionDepthTolerance);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // ── Helpers de detección ─────────────────────────────────────────────────

        /// <summary>
        /// Devuelve true si <paramref name="worldPos"/> está frente al panel
        /// dentro del área activa definida por los parámetros del Inspector.
        /// </summary>
        private bool IsHandInPanelZone(Vector3 worldPos)
        {
            if (gridRoot == null) return false;

            Vector3 local = gridRoot.InverseTransformPoint(worldPos);

            // Z positivo = hacia el usuario (el panel mira al usuario con -forward)
            // Aceptamos entre 0 y palmActivationDepth metros frente al panel
            if (local.z <= 0f || local.z > palmActivationDepth) return false;

            // Debe estar dentro del área X/Y del panel
            if (Mathf.Abs(local.x) > panelHalfWidth)  return false;
            if (Mathf.Abs(local.y) > panelHalfHeight)  return false;

            return true;
        }

        /// <summary>
        /// Devuelve true si <paramref name="hand"/> muestra una palma abierta:
        ///   • No está haciendo pinch (thumb↔index suficientemente separados)
        ///   • Los dedos están extendidos (no es un puño)
        /// </summary>
        private bool IsHandOpen(XRHand hand)
        {
            // Condición 1: no pinch
            if (!hand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out Pose thumb)) return false;
            if (!hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose index)) return false;
            if (Vector3.Distance(thumb.position, index.position) < pinchThresholdM) return false;

            // Condición 2: dedos extendidos (dedo medio lejos de la muñeca)
            if (!hand.GetJoint(XRHandJointID.MiddleTip).TryGetPose(out Pose middle)) return false;
            if (!hand.GetJoint(XRHandJointID.Wrist).TryGetPose(out Pose wrist))      return false;
            if (Vector3.Distance(middle.position, wrist.position) < fingerExtensionM) return false;

            return true;
        }

        // ─────────────────────────────────────────────────────────────────────────
        private void TryGetHandSubsystem()
        {
            var list = new List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(list);
            if (list.Count > 0)
                _handSubsystem = list[0];
        }
    }
}
