using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArtUnbound.VR
{
    /// <summary>
    /// Snap turn de 45 grados usando el thumbstick IZQUIERDO (sector lateral).
    /// El sector frontal (|y|>=|x|) queda libre para teleport; solo gira cuando |x|>|y|.
    /// Bypasses XRI SnapTurnProvider/LocomotionMediator para evitar conflictos con el CIAM.
    /// Gira el XR Origin alrededor de la posicion de la camara (pivot en el suelo bajo la cabeza).
    /// </summary>
    public class VRSnapTurnController : MonoBehaviour
    {
        [Tooltip("XR Origin del rig. Se asigna por VRModeController en runtime.")]
        public XROrigin xrOrigin;

        [Tooltip("Angulo de giro en grados por snap.")]
        [SerializeField] private float turnAngle = 45f;

        [Tooltip("Threshold minimo del eje X del thumbstick para disparar el snap.")]
        [SerializeField] private float threshold = 0.6f;

        [Tooltip("Segundos de cooldown entre snaps consecutivos.")]
        [SerializeField] private float debounceSeconds = 0.25f;

        private InputAction _rightStick;
        private float _lastTurnTime = -999f;
        private bool _wasNeutral = true;
        private float _nextDiagLog = 0f;

        private void OnEnable()
        {
            _rightStick = new InputAction("VRSnapTurn", InputActionType.Value,
                binding: "<XRController>{LeftHand}/thumbstick");
            _rightStick.Enable();
            _wasNeutral = true;
            _nextDiagLog = Time.time + 1f;
            Debug.Log($"[VRSnapTurn] OnEnable — xrOrigin={(xrOrigin != null ? xrOrigin.gameObject.name : "NULL")} action={_rightStick.bindings.Count} bindings");
        }

        private void OnDisable()
        {
            _rightStick?.Disable();
            _rightStick?.Dispose();
            _rightStick = null;
            Debug.Log("[VRSnapTurn] OnDisable");
        }

        private void Update()
        {
            if (xrOrigin == null || _rightStick == null) return;

            Vector2 stick = _rightStick.ReadValue<Vector2>();
            float x = stick.x;
            float y = stick.y;

            // Periodic diagnostic so we can confirm the stick is being read.
            if (Time.time >= _nextDiagLog)
            {
                _nextDiagLog = Time.time + 2f;
                Debug.Log($"[VRSnapTurn] DIAG stick=({x:F3},{y:F3}) threshold={threshold} wasNeutral={_wasNeutral}");
            }

            // Only snap when the horizontal component dominates — a forward push
            // (|y| >= |x|) is for teleport and must not also trigger a turn.
            if (Mathf.Abs(x) <= Mathf.Abs(y))
            {
                _wasNeutral = true;
                return;
            }

            bool neutral = Mathf.Abs(x) < threshold;

            if (neutral)
            {
                _wasNeutral = true;
                return;
            }

            // Snap una sola vez por flick: requiere volver al neutro entre turnos.
            if (!_wasNeutral) return;
            if (Time.time - _lastTurnTime < debounceSeconds) return;

            _wasNeutral = false;
            _lastTurnTime = Time.time;

            float angle = x > 0f ? turnAngle : -turnAngle;

            // Girar alrededor de la posicion de la camara proyectada al suelo del rig,
            // para que el usuario rote sobre sus pies y no sobre el origen del rig.
            Camera cam = xrOrigin.Camera;
            Vector3 pivot = cam != null ? cam.transform.position : xrOrigin.transform.position;
            pivot.y = xrOrigin.transform.position.y;

            xrOrigin.transform.RotateAround(pivot, Vector3.up, angle);
            Debug.Log($"[VRSnapTurn] Giro {angle}° alrededor de {pivot}");
        }
    }
}
