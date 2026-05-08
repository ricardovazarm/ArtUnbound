using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.InputSystem;

namespace ArtUnbound.VR
{
    /// <summary>
    /// Thumbstick-based teleport locomotion for VR gallery.
    /// Left thumbstick forward → shows arc reticle → release → teleport.
    /// Uses XR Interaction Toolkit TeleportationProvider.
    /// </summary>
    public class VRLocomotionController : MonoBehaviour
    {
        [Header("Teleport")]
        [SerializeField] private TeleportationProvider teleportationProvider;

        [Header("Controller")]
        [Tooltip("Transform del controlador izquierdo. El arco de teleport saldra de aqui y apuntara hacia su forward.")]
        [SerializeField] private Transform leftControllerAnchor;

        [Tooltip("Root de los controladores fisicos (HardwareControllers). Se mueve con el teleport para que el tracking siga al jugador. Dejar null si los controladores ya son hijos del XR Origin.")]
        [SerializeField] private Transform hardwareControllersRoot;

        [Header("Input")]
        [SerializeField] private InputActionReference leftThumbstickAction;

        [Header("Arc Visual")]
        [SerializeField] private LineRenderer arcLineRenderer;
        [SerializeField] private GameObject reticlePrefab;
        [SerializeField] private LayerMask teleportLayerMask;
        [SerializeField] private int arcSegments = 20;
        [SerializeField] private float arcVelocity = 8f;
        [SerializeField] private float arcGravity = 9.8f;

        private bool _isAiming;
        private GameObject _reticleInstance;
        private Vector3 _teleportTarget;
        private bool _hasValidTarget;

        private const float ActivationThreshold = 0.5f;

        private void Awake()
        {
            gameObject.SetActive(false);

            if (arcLineRenderer != null)
                arcLineRenderer.enabled = false;

            if (reticlePrefab != null)
            {
                _reticleInstance = Instantiate(reticlePrefab);
                _reticleInstance.SetActive(false);
            }
        }

        private void OnEnable()
        {
            leftThumbstickAction?.action.Enable();
        }

        private void OnDisable()
        {
            StopAiming();
            leftThumbstickAction?.action.Disable();
        }

        private void Update()
        {
            if (leftThumbstickAction == null) return;

            Vector2 stick = leftThumbstickAction.action.ReadValue<Vector2>();
            bool pressing = stick.y > ActivationThreshold;

            if (pressing && !_isAiming)
                StartAiming();
            else if (!pressing && _isAiming)
                ConfirmTeleport();
            else if (_isAiming)
                UpdateArc();
        }

        private void StartAiming()
        {
            _isAiming = true;
            if (arcLineRenderer != null) arcLineRenderer.enabled = true;
        }

        private void StopAiming()
        {
            _isAiming = false;
            _hasValidTarget = false;
            if (arcLineRenderer != null) arcLineRenderer.enabled = false;
            if (_reticleInstance != null) _reticleInstance.SetActive(false);
        }

        private void ConfirmTeleport()
        {
            if (_hasValidTarget && teleportationProvider != null)
            {
                var request = new TeleportRequest
                {
                    destinationPosition = _teleportTarget,
                    matchOrientation = MatchOrientation.None
                };
                teleportationProvider.QueueTeleportRequest(request);

                if (hardwareControllersRoot != null && Camera.main != null)
                {
                    Vector3 camPos = Camera.main.transform.position;
                    Vector3 delta = new Vector3(
                        _teleportTarget.x - camPos.x,
                        0f,
                        _teleportTarget.z - camPos.z);
                    hardwareControllersRoot.position += delta;
                }
            }

            StopAiming();
        }

        private void UpdateArc()
        {
            if (leftControllerAnchor == null)
            {
                Debug.LogWarning("[VRLocomotionController] leftControllerAnchor no esta asignado. Asigna el LeftHand Controller en el Inspector.");
                return;
            }

            Vector3 origin = leftControllerAnchor.position;

            Vector3 forward = leftControllerAnchor.forward;
            forward.y = Mathf.Min(forward.y, -0.1f);
            forward.Normalize();

            Vector3 velocity = forward * arcVelocity;
            Vector3 gravity = new Vector3(0, -arcGravity, 0);

            var points = new Vector3[arcSegments];
            float stepTime = 0.1f;
            bool hit = false;

            for (int i = 0; i < arcSegments; i++)
            {
                float t = i * stepTime;
                points[i] = origin + velocity * t + 0.5f * gravity * t * t;

                if (i > 0 && Physics.Linecast(points[i - 1], points[i], out RaycastHit hitInfo, teleportLayerMask))
                {
                    points[i] = hitInfo.point;
                    _teleportTarget = hitInfo.point;
                    _hasValidTarget = true;

                    if (_reticleInstance != null)
                    {
                        _reticleInstance.SetActive(true);
                        _reticleInstance.transform.position = hitInfo.point + Vector3.up * 0.01f;
                    }

                    // Trim arc to hit point
                    if (arcLineRenderer != null)
                    {
                        arcLineRenderer.positionCount = i + 1;
                        arcLineRenderer.SetPositions(points);
                    }
                    hit = true;
                    break;
                }
            }

            if (!hit)
            {
                _hasValidTarget = false;
                if (_reticleInstance != null) _reticleInstance.SetActive(false);

                if (arcLineRenderer != null)
                {
                    arcLineRenderer.positionCount = arcSegments;
                    arcLineRenderer.SetPositions(points);
                }
            }
        }
    }
}
