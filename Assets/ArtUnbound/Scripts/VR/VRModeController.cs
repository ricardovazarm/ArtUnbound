using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ArtUnbound.VR
{
    /// <summary>
    /// Activates/deactivates VR mode by toggling passthrough and MR-specific systems.
    /// </summary>
    public class VRModeController : MonoBehaviour
    {
        [Header("Camera")]
        [SerializeField] private Camera mainCamera;

        [Header("AR Components (on XR Origin / Main Camera)")]
        [SerializeField] private ARCameraManager arCameraManager;
        [SerializeField] private ARCameraBackground arCameraBackground;
        [SerializeField] private ARPlaneManager arPlaneManager;
        [SerializeField] private ARAnchorManager arAnchorManager;

        [Header("MR Systems")]
        [Tooltip("AudioListener del MR Main Camera. Se desactiva en VR para evitar el warning de '2 audio listeners'.")]
        [SerializeField] private AudioListener mrAudioListener;
        [SerializeField] private MonoBehaviour spatialPermissionService;
        [SerializeField] private MonoBehaviour wallPlacementDetector;
        [SerializeField] private MonoBehaviour wallAnchorManager;
        [SerializeField] private MonoBehaviour artworkHangingController;
        [SerializeField] private MonoBehaviour wallHighlightController;
        [SerializeField] private MonoBehaviour wallDetectionService;

        [Header("VR Systems")]
        [SerializeField] private VRGalleryController vrGalleryController;
        [SerializeField] private VRWallHangingController vrWallHangingController;

        [Header("MR Rig VR Locomotion")]
        [Tooltip("MR Interaction Setup > XR Origin (XR Rig)/Locomotion/Teleportation. Se enciende solo en VR.")]
        [SerializeField] private GameObject mrTeleportationGO;
        [Tooltip("MR Interaction Setup > XR Origin (XR Rig)/Locomotion/Turn (SnapTurnProvider). Se enciende solo en VR.")]
        [SerializeField] private GameObject mrSnapTurnGO;
        [Tooltip("MR Interaction Setup > XR Origin (XR Rig)/Camera Offset/Left Controller/Teleport Interactor. Se enciende solo en VR.")]
        [SerializeField] private GameObject mrLeftTeleportInteractorGO;

        [Header("Obsolete (kept for migration; remove after full consolidation)")]
        [Tooltip("OBSOLETO: el rig VR se eliminara. No se toca en runtime.")]
        [SerializeField] private GameObject xrOriginVR;
        [Tooltip("OBSOLETO: la camara VR se eliminara. No se toca en runtime.")]
        [SerializeField] private Camera vrCamera;
        [Tooltip("OBSOLETO: ya no se desactiva el MR rig.")]
        [SerializeField] private GameObject mrXrRig;

        [Header("VR Skybox")]
        [SerializeField] private Material vrSkyboxMaterial;

        public bool IsVRMode { get; private set; }
        public Camera MainCamera => mainCamera;

        private Material _originalSkybox;

        private void Awake()
        {
            if (mainCamera == null) mainCamera = Camera.main;

            // VR systems start disabled
            SetVRSystemsActive(false);
            SetMRLocomotionForVR(false);
        }

        public void ActivateVRMode()
        {
            if (IsVRMode) return;
            IsVRMode = true;

            // Disable passthrough
            if (arCameraManager != null) arCameraManager.enabled = false;
            if (arCameraBackground != null) arCameraBackground.enabled = false;

            if (mainCamera != null)
            {
                _originalSkybox = RenderSettings.skybox;
                if (vrSkyboxMaterial != null)
                {
                    RenderSettings.skybox = vrSkyboxMaterial;
                    mainCamera.clearFlags = CameraClearFlags.Skybox;
                }
                else if (RenderSettings.skybox != null)
                {
                    // Use whatever skybox is already set in the project
                    mainCamera.clearFlags = CameraClearFlags.Skybox;
                }
                else
                {
                    // No skybox at all — use a neutral dark colour instead of black
                    mainCamera.clearFlags = CameraClearFlags.SolidColor;
                    mainCamera.backgroundColor = new Color(0.15f, 0.15f, 0.25f, 1f);
                }
            }

            // Disable MR spatial systems — use component.enabled instead of gameObject.SetActive
            // so we don't accidentally deactivate parent GameObjects that contain Main Camera.
            if (arPlaneManager != null) arPlaneManager.enabled = false;
            if (arAnchorManager != null) arAnchorManager.enabled = false;
            if (mrAudioListener != null) mrAudioListener.enabled = false;
            if (spatialPermissionService != null) spatialPermissionService.enabled = false;
            if (wallPlacementDetector != null) wallPlacementDetector.enabled = false;
            if (wallAnchorManager != null) wallAnchorManager.enabled = false;
            if (artworkHangingController != null) artworkHangingController.enabled = false;
            if (wallHighlightController != null) wallHighlightController.enabled = false;
            if (wallDetectionService != null) wallDetectionService.enabled = false;

            // Enable VR systems
            SetVRSystemsActive(true);
            SetMRLocomotionForVR(true);

            Debug.Log("[VRModeController] VR Mode activated");
        }

        public void DeactivateVRMode()
        {
            if (!IsVRMode) return;
            IsVRMode = false;

            // Re-enable passthrough
            if (arCameraManager != null) arCameraManager.enabled = true;
            if (arCameraBackground != null) arCameraBackground.enabled = true;

            if (mainCamera != null)
            {
                mainCamera.clearFlags = CameraClearFlags.SolidColor;
                mainCamera.backgroundColor = new Color(0, 0, 0, 0);
                if (_originalSkybox != null)
                    RenderSettings.skybox = _originalSkybox;
            }

            // Re-enable MR systems
            if (arPlaneManager != null) arPlaneManager.enabled = true;
            if (arAnchorManager != null) arAnchorManager.enabled = true;
            if (mrAudioListener != null) mrAudioListener.enabled = true;
            if (spatialPermissionService != null) spatialPermissionService.enabled = true;
            if (wallPlacementDetector != null) wallPlacementDetector.enabled = true;
            if (wallAnchorManager != null) wallAnchorManager.enabled = true;
            if (artworkHangingController != null) artworkHangingController.enabled = true;
            if (wallHighlightController != null) wallHighlightController.enabled = true;
            if (wallDetectionService != null) wallDetectionService.enabled = true;

            // Disable VR systems
            SetMRLocomotionForVR(false);
            SetVRSystemsActive(false);

            Debug.Log("[VRModeController] VR Mode deactivated — MR restored");
        }

        private void SetVRSystemsActive(bool active)
        {
            if (vrGalleryController != null) vrGalleryController.gameObject.SetActive(active);
            if (vrWallHangingController != null) vrWallHangingController.gameObject.SetActive(active);
        }

        private void SetMRLocomotionForVR(bool active)
        {
            if (mrTeleportationGO != null) mrTeleportationGO.SetActive(active);
            if (mrSnapTurnGO != null) mrSnapTurnGO.SetActive(active);
            if (mrLeftTeleportInteractorGO != null) mrLeftTeleportInteractorGO.SetActive(active);
        }
    }
}
