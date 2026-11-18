using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ArtUnbound.VR
{
    /// <summary>
    /// Activates/desactivates el Teleport Interactor segun el input del thumbstick.
    /// El teleport en si mueve directamente el XR Origin (bypassa el LocomotionMediator
    /// de XRI 3.x que rechazaba BeginLocomotion silenciosamente).
    /// </summary>
    public class VRTeleportActivator : MonoBehaviour
    {
        [Tooltip("GameObject del Teleport Interactor a activar/desactivar.")]
        [SerializeField] private GameObject teleportInteractor;

        [Tooltip("InputActionReference a la accion 'Teleport Mode Activate' (sector forward del thumbstick izquierdo).")]
        [SerializeField] private InputActionReference teleportModeActivate;

        [Header("XR Origin")]
        [Tooltip("XR Origin del rig. Si se deja vacio se busca automaticamente al activar.")]
        [SerializeField] private XROrigin xrOrigin;

        [Header("Ray Configuration")]
        [Tooltip("Pitch del rayo en grados. Negativo = hacia arriba. -35 produce arco parabolico natural.")]
        [SerializeField] private float teleportRayPitch = -35f;

        private bool _postponedDeactivate;
        private XRRayInteractor _rayInteractor;
        private Transform _rayOriginOverride;

        // Cache de hit actualizado cada frame en Update.
        // OnCancelTeleport llega antes del Update del frame actual,
        // asi que usamos datos del frame anterior que son validos.
        private Vector3 _cachedHitPos;
        private bool _hasValidHit;
        private string _cachedHitName = "?";
        private string _lastRejectReason = "sin datos";

        // Un destino valido es piso: normal casi vertical y a la altura del suelo del rig.
        private const float MinFloorNormalY = 0.7f;
        private const float MaxFloorHeightDelta = 0.15f;

        private void Awake()
        {
            if (teleportInteractor != null)
                _rayInteractor = teleportInteractor.GetComponent<XRRayInteractor>();
        }

        private void OnEnable()
        {
            if (teleportInteractor != null)
                teleportInteractor.SetActive(false);

            ConfigureRayInteractor();

            var action = teleportModeActivate != null ? teleportModeActivate.action : null;
            if (action != null)
            {
                action.started -= OnStartTeleport;
                action.canceled -= OnCancelTeleport;
                action.started += OnStartTeleport;
                action.canceled += OnCancelTeleport;
                action.Enable();
            }
        }

        private void OnDisable()
        {
            var action = teleportModeActivate != null ? teleportModeActivate.action : null;
            if (action != null)
            {
                action.started -= OnStartTeleport;
                action.canceled -= OnCancelTeleport;
            }

            if (teleportInteractor != null)
                teleportInteractor.SetActive(false);

            _hasValidHit = false;
        }

        private void ConfigureRayInteractor()
        {
            if (_rayInteractor == null) return;

            _rayInteractor.lineType = XRRayInteractor.LineType.ProjectileCurve;
            _rayInteractor.velocity = 6.5f;
            _rayInteractor.additionalGroundHeight = 0.1f;
            _rayInteractor.raycastMask = Physics.DefaultRaycastLayers;

            int teleportLayer = InteractionLayerMask.GetMask("Teleport");
            if (teleportLayer != 0)
                _rayInteractor.interactionLayers = teleportLayer;

            if (_rayOriginOverride == null)
            {
                var go = new GameObject("_TeleportRayOrigin");
                go.transform.SetParent(teleportInteractor.transform, worldPositionStays: false);
                _rayOriginOverride = go.transform;
                _rayInteractor.rayOriginTransform = _rayOriginOverride;
            }
        }

        private void OnStartTeleport(InputAction.CallbackContext _)
        {
            _postponedDeactivate = false;
            _hasValidHit = false;
            if (teleportInteractor != null)
                teleportInteractor.SetActive(true);
        }

        private void OnCancelTeleport(InputAction.CallbackContext _)
        {
            TriggerTeleport();
            _postponedDeactivate = true;
        }

        private void TriggerTeleport()
        {
            if (!_hasValidHit)
            {
                Debug.LogWarning($"[VRTeleport] Sin hit valido al soltar thumbstick ({_lastRejectReason})");
                return;
            }

            if (xrOrigin == null)
            {
                Debug.LogWarning("[VRTeleport] XROrigin no asignado");
                return;
            }

            // Si el XROrigin asignado en el Inspector es el rig obsoleto VR (inactivo tras
            // la consolidacion), buscamos el activo en la escena para no mover un objeto muerto.
            if (!xrOrigin.gameObject.activeInHierarchy)
            {
                Debug.LogWarning($"[VRTeleport] XROrigin '{xrOrigin.gameObject.name}' esta INACTIVO. Buscando uno activo...");
                xrOrigin = Object.FindFirstObjectByType<XROrigin>();
                if (xrOrigin == null || !xrOrigin.gameObject.activeInHierarchy)
                {
                    Debug.LogWarning("[VRTeleport] No se encontro un XROrigin activo");
                    return;
                }
                Debug.Log($"[VRTeleport] Usando XROrigin activo: '{xrOrigin.gameObject.name}'");
            }

            Transform originT = xrOrigin.transform;
            Camera cam = xrOrigin.Camera;

            Debug.Log($"[VRTeleport] Moviendo rig='{originT.name}' cam={cam?.name ?? "NULL"} | rigPos={originT.position} | hitPos={_cachedHitPos} | hitObj='{_cachedHitName}'");

            if (cam != null)
            {
                // Desplaza el rig para que la posicion XZ de la camara quede sobre el hit.
                // La Y del rig se preserva (el suelo de la galeria esta a la misma altura).
                float offsetX = cam.transform.position.x - originT.position.x;
                float offsetZ = cam.transform.position.z - originT.position.z;
                originT.position = new Vector3(
                    _cachedHitPos.x - offsetX,
                    originT.position.y,
                    _cachedHitPos.z - offsetZ);
            }
            else
            {
                originT.position = new Vector3(_cachedHitPos.x, originT.position.y, _cachedHitPos.z);
            }

            Debug.Log($"[VRTeleport] Rig movido a {originT.position} (hit={_cachedHitPos})");
        }

        private void Update()
        {
            // Cache hit ANTES del postponed deactivate para capturar el ultimo estado valido.
            UpdateHitCache();

            if (_postponedDeactivate)
            {
                _postponedDeactivate = false;
                _hasValidHit = false;
                if (teleportInteractor != null)
                    teleportInteractor.SetActive(false);
            }

            UpdateRayOriginRotation();
        }

        private void UpdateHitCache()
        {
            if (_rayInteractor == null) return;
            if (teleportInteractor == null || !teleportInteractor.activeSelf) return;

            bool hit = _rayInteractor.TryGetHitInfo(
                out Vector3 hitPos, out Vector3 hitNormal, out int _, out bool _);

            if (!hit)
            {
                _hasValidHit = false;
                _lastRejectReason = "el rayo no toca nada";
                return;
            }

            // Solo se acepta PISO: superficie que mira hacia arriba y a la altura del suelo del rig.
            // Antes se aceptaba cualquier collider (paredes incluidas) y la camara quedaba sobre el
            // plano del muro o del otro lado, fuera de la galeria.
            float floorY = (xrOrigin != null && xrOrigin.gameObject.activeInHierarchy)
                ? xrOrigin.transform.position.y
                : hitPos.y;
            string hitName = _rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit rh) && rh.collider != null
                ? rh.collider.gameObject.name
                : "?";

            if (hitNormal.y < MinFloorNormalY)
            {
                _hasValidHit = false;
                _lastRejectReason = $"no es piso: '{hitName}' normalY={hitNormal.y:F2} pos={hitPos:F2}";
                return;
            }
            if (Mathf.Abs(hitPos.y - floorY) > MaxFloorHeightDelta)
            {
                _hasValidHit = false;
                _lastRejectReason = $"fuera de la altura del piso: '{hitName}' y={hitPos.y:F2} piso={floorY:F2}";
                return;
            }

            _cachedHitPos = hitPos;
            _cachedHitName = hitName;
            _hasValidHit = true;
        }

        private void UpdateRayOriginRotation()
        {
            if (_rayOriginOverride == null) return;
            if (teleportInteractor == null || !teleportInteractor.activeSelf) return;

            Transform ctrl = teleportInteractor.transform.parent;
            if (ctrl == null) return;

            _rayOriginOverride.rotation = Quaternion.AngleAxis(teleportRayPitch, ctrl.right) * ctrl.rotation;
        }
    }
}
