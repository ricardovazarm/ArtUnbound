using UnityEngine;
using UnityEngine.InputSystem;

namespace ArtUnbound.VR
{
    /// <summary>
    /// Activa/desactiva el GameObject del Teleport Interactor segun el input del thumbstick.
    /// Sin este gating, el XRRayInteractor dibuja su linea todo el tiempo aunque no se este
    /// pidiendo teleport. Replica el patron del Unity VR Template (ControllerInputActionManager).
    ///
    /// Comportamiento:
    /// - OnEnable: oculta el interactor (SetActive(false)).
    /// - Action started (thumbstick empujado): muestra el interactor (SetActive(true)).
    /// - Action canceled (thumbstick soltado): pospone la desactivacion 1 frame para que
    ///   el XRRayInteractor tenga oportunidad de procesar el select y completar el teleport.
    /// </summary>
    public class VRTeleportActivator : MonoBehaviour
    {
        [Tooltip("GameObject del Teleport Interactor a activar/desactivar (normalmente XR Origin (VR)/Camera Offset/LeftHand/Teleport Interactor).")]
        [SerializeField] private GameObject teleportInteractor;

        [Tooltip("InputActionReference a la accion 'Teleport Mode Activate' (Sector forward del thumbstick izquierdo).")]
        [SerializeField] private InputActionReference teleportModeActivate;

        private bool _postponedDeactivate;

        private void OnEnable()
        {
            if (teleportInteractor != null)
                teleportInteractor.SetActive(false);

            var action = teleportModeActivate != null ? teleportModeActivate.action : null;
            if (action != null)
            {
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
        }

        private void OnStartTeleport(InputAction.CallbackContext _)
        {
            _postponedDeactivate = false;
            if (teleportInteractor != null)
                teleportInteractor.SetActive(true);
        }

        private void OnCancelTeleport(InputAction.CallbackContext _)
        {
            // Esperamos un frame: el XRInteractionManager corre antes que este Update,
            // asi el Teleport Interactor alcanza a procesar el select y disparar el teleport.
            _postponedDeactivate = true;
        }

        private void Update()
        {
            if (_postponedDeactivate)
            {
                _postponedDeactivate = false;
                if (teleportInteractor != null)
                    teleportInteractor.SetActive(false);
            }
        }
    }
}
