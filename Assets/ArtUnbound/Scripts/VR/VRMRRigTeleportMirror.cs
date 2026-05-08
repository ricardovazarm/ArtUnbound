using UnityEngine;

namespace ArtUnbound.VR
{
    /// <summary>
    /// Espeja la posicion del root del MR XR Rig sobre la del VR XR Origin mientras VR mode esta activo.
    ///
    /// Contexto: en VR mode el MR rig se deja prendido (desactivarlo rompe el teleport por dependencias
    /// del XR Interaction Toolkit). Pero los widgets visibles de los controllers/manos (Right Controller,
    /// Left Controller, Right Hand, Left Hand, etc.) son TrackedPoseDriver-driven y se posicionan relativo
    /// al MR XR Origin que esta fijo en (0,0,0). Despues de teleportar, el VR XR Origin se mueve pero el
    /// MR no, asi que los widgets quedan anclados al lugar original.
    ///
    /// Solucion: mientras VR este activo, copiamos la posicion del VR XR Origin al MR XR Origin cada
    /// LateUpdate. Como los TrackedPoseDriver corren en Update + BeforeRender y leen el world transform
    /// de su parent (el MR origin), al moverlo arrastramos a todos los widgets sin pelear con el sistema
    /// de tracking. Al salir de VR restauramos la posicion original del MR origin.
    ///
    /// El MR rig en si NO se desactiva, asi que no se rompe nada del setup XRI.
    /// </summary>
    public class VRMRRigTeleportMirror : MonoBehaviour
    {
        [Tooltip("Transform raiz del MR XR Origin (XR Rig). Su posicion se sobrescribe cuando VR esta activo.")]
        [SerializeField] private Transform mrXROrigin;

        [Tooltip("Transform raiz del XR Origin (VR). Su posicion es la fuente del mirror.")]
        [SerializeField] private Transform vrXROrigin;

        [Tooltip("VRModeController que indica si VR esta activo. Si es null, se usa activeInHierarchy del VR origin.")]
        [SerializeField] private VRModeController vrModeController;

        private Vector3 _mrOriginalPos;
        private Quaternion _mrOriginalRot;
        private bool _hasOriginal;
        private bool _isMirroring;

        private void Start()
        {
            if (mrXROrigin != null)
            {
                _mrOriginalPos = mrXROrigin.position;
                _mrOriginalRot = mrXROrigin.rotation;
                _hasOriginal = true;
            }
        }

        private void LateUpdate()
        {
            if (mrXROrigin == null || vrXROrigin == null) return;

            bool vrActive = vrModeController != null
                ? vrModeController.IsVRMode
                : vrXROrigin.gameObject.activeInHierarchy;

            if (vrActive)
            {
                if (!_isMirroring && _hasOriginal)
                {
                    _mrOriginalPos = mrXROrigin.position;
                    _mrOriginalRot = mrXROrigin.rotation;
                    _isMirroring = true;
                }

                mrXROrigin.SetPositionAndRotation(vrXROrigin.position, vrXROrigin.rotation);
            }
            else if (_isMirroring && _hasOriginal)
            {
                mrXROrigin.SetPositionAndRotation(_mrOriginalPos, _mrOriginalRot);
                _isMirroring = false;
            }
        }
    }
}
