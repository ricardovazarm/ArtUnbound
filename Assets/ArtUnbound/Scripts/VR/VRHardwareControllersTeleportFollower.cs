using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace ArtUnbound.VR
{
    /// <summary>
    /// Mantiene los hijos de HardwareControllers visualmente alineados con el jugador
    /// despues de un teleport en VR.
    ///
    /// Problema: los scripts de tracking (HandTrackingInputController, IndexTipFollower)
    /// hacen `transform.position = pose.position` cada Update, donde `pose.position` viene
    /// de XRHandSubsystem en TRACKING space (sin la transform del XR Origin aplicada).
    /// Cuando el jugador teleporta, el XR Origin VR se mueve, pero las poses siguen
    /// reportando coords de tracking — asi que los widgets se quedan en la posicion fisica
    /// original, anclados al lugar donde el jugador "aparecio" inicialmente.
    ///
    /// Solucion: en LateUpdate (despues de que los Updates de tracking ya escribieron las
    /// posiciones world-space), sumamos a cada hijo de HardwareControllers el delta de
    /// cuanto se ha movido el XR Origin VR desde que se activo VR mode. Asi el offset
    /// del teleport se aplica encima del world position que escribio el tracking.
    ///
    /// Mover solo el ROOT de HardwareControllers (como se intentaba antes) NO funciona aqui,
    /// porque el world position de cada hijo es sobrescrito por su propio Update cada frame,
    /// ignorando al padre. Por eso aplicamos el offset directamente a cada hijo.
    /// </summary>
    public class VRHardwareControllersTeleportFollower : MonoBehaviour
    {
        [Tooltip("TeleportationProvider que mueve el XR Origin VR. Solo se usa para auto-wire de TeleportationAreas en escena.")]
        [SerializeField] private TeleportationProvider provider;

        [Tooltip("Root de los controladores fisicos (HardwareControllers). El offset se aplica a cada uno de sus hijos en LateUpdate.")]
        [SerializeField] private Transform hardwareControllersRoot;

        [Tooltip("Camera del XR Origin VR. El transform raiz del XR Origin se infiere subiendo dos niveles (Camera -> CameraOffset -> XR Origin).")]
        [SerializeField] private Transform xrCamera;

        private Transform _xrOriginTransform;
        private Vector3 _initialXROriginPos;
        private bool _initialized;

        private void OnEnable()
        {
            ResolveXROriginTransform();
            if (_xrOriginTransform != null)
            {
                _initialXROriginPos = _xrOriginTransform.position;
                _initialized = true;
            }

            // Auto-wire scene TeleportationAreas/Anchors a este provider.
            // No podemos referenciar el provider desde el prefab del Floor (Gallery_Classic),
            // asi que lo asignamos en runtime para garantizar que use el nuestro y no
            // cualquier otro TeleportationProvider que haya en la escena (ej: el del XR Origin MR).
            if (provider != null)
            {
                var areas = FindObjectsByType<BaseTeleportationInteractable>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var area in areas)
                    area.teleportationProvider = provider;
            }
        }

        private void OnDisable()
        {
            _initialized = false;
        }

        private void LateUpdate()
        {
            if (!_initialized || _xrOriginTransform == null || hardwareControllersRoot == null) return;

            Vector3 offset = _xrOriginTransform.position - _initialXROriginPos;
            offset.y = 0f;
            if (offset.sqrMagnitude < 0.0001f) return;

            int childCount = hardwareControllersRoot.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = hardwareControllersRoot.GetChild(i);
                child.position += offset;
            }
        }

        private void ResolveXROriginTransform()
        {
            if (xrCamera == null)
            {
                _xrOriginTransform = null;
                return;
            }

            // Jerarquia esperada: XR Origin (VR) > Camera Offset > Main Camera
            Transform offset = xrCamera.parent;
            _xrOriginTransform = offset != null ? offset.parent : null;
        }
    }
}
