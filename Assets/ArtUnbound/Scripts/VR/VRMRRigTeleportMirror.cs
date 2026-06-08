using UnityEngine;

namespace ArtUnbound.VR
{
    /// <summary>
    /// Restaura la posicion del MR XR Origin al salir de VR mode.
    ///
    /// NOTA POST-CONSOLIDACION: el VR XR Origin fue eliminado; el MR rig ahora ES el rig VR y se
    /// mueve directamente via VRTeleportActivator. Este script ya NO debe copiar ninguna posicion
    /// externa sobre el MR rig (eso resetearia el teleport cada LateUpdate). Su unica funcion
    /// restante es cachear la posicion MR antes de entrar a VR y restaurarla al salir.
    /// </summary>
    public class VRMRRigTeleportMirror : MonoBehaviour
    {
        [Tooltip("Transform raiz del MR XR Origin (XR Rig). Se restaura a su posicion original al salir de VR.")]
        [SerializeField] private Transform mrXROrigin;

        [Tooltip("OBSOLETO: ya no se usa para mirroring. Mantenido para no perder la referencia en escena.")]
        [SerializeField] private Transform vrXROrigin;

        [Tooltip("VRModeController que indica si VR esta activo.")]
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
            if (mrXROrigin == null) return;

            bool vrActive = vrModeController != null
                ? vrModeController.IsVRMode
                : (vrXROrigin != null && vrXROrigin.gameObject.activeInHierarchy);

            if (vrActive)
            {
                // Cachear posicion MR al entrar a VR por primera vez en esta sesion.
                // NO copiar nada sobre el MR rig: VRTeleportActivator lo mueve directamente
                // y sobreescribirlo aqui cancelaria el teleport cada frame.
                if (!_isMirroring && _hasOriginal)
                {
                    _mrOriginalPos = mrXROrigin.position;
                    _mrOriginalRot = mrXROrigin.rotation;
                    _isMirroring = true;
                }
            }
            else if (_isMirroring && _hasOriginal)
            {
                mrXROrigin.SetPositionAndRotation(_mrOriginalPos, _mrOriginalRot);
                _isMirroring = false;
            }
        }
    }
}
