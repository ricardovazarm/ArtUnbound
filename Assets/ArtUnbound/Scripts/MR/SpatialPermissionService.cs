using UnityEngine;
using UnityEngine.XR.ARFoundation;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace ArtUnbound.MR
{
    /// <summary>
    /// Requests the Meta Quest spatial data permission (USE_SCENE) required for
    /// ARPlaneManager / Scene Model (Space Setup). Without this permission, plane
    /// queries return zero results.
    /// </summary>
    public class SpatialPermissionService : MonoBehaviour
    {
        public const string SpatialPermissionId = "com.oculus.permission.USE_SCENE";

        [SerializeField] private ARPlaneManager planeManager;

        public bool HasPermission { get; private set; }

        private void Awake()
        {
            if (planeManager == null)
                planeManager = FindFirstObjectByType<ARPlaneManager>();

            // Disable plane manager until permission is granted (per Meta docs)
            if (planeManager != null)
                planeManager.enabled = false;
        }

        private void Start()
        {
            Debug.Log("[SpatialPermission] Start() - Platform: " + Application.platform + ", UNITY_ANDROID: " +
#if UNITY_ANDROID
                "defined"
#else
                "NOT defined (Editor or non-Android)"
#endif
            );
#if UNITY_ANDROID
            RequestSpatialPermission();
#else
            Debug.Log("[SpatialPermission] Skipping request (not Android build). Enabling plane manager for Editor.");
            OnPermissionGranted(SpatialPermissionId);
#endif
        }

#if UNITY_ANDROID
        private void RequestSpatialPermission()
        {
            var alreadyGranted = Permission.HasUserAuthorizedPermission(SpatialPermissionId);
            Debug.Log($"[SpatialPermission] HasUserAuthorizedPermission={alreadyGranted}");

            if (alreadyGranted)
            {
                Debug.Log("[SpatialPermission] Permission already granted, enabling plane manager.");
                OnPermissionGranted(SpatialPermissionId);
                return;
            }

            var callbacks = new PermissionCallbacks();
            callbacks.PermissionDenied += OnPermissionDenied;
            callbacks.PermissionGranted += OnPermissionGranted;

            Debug.Log("[SpatialPermission] Requesting com.oculus.permission.USE_SCENE for Scene Model / wall detection.");
            Permission.RequestUserPermission(SpatialPermissionId, callbacks);
        }
#endif

        private void OnPermissionGranted(string permission)
        {
            HasPermission = true;
            Debug.Log("[SpatialPermission] Spatial data permission granted. Enabling plane detection.");

            if (planeManager != null)
            {
                planeManager.enabled = true;
                planeManager.requestedDetectionMode = UnityEngine.XR.ARSubsystems.PlaneDetectionMode.Vertical;
            }
        }

        private void OnPermissionDenied(string permission)
        {
            HasPermission = false;
            Debug.LogWarning("[SpatialPermission] Spatial data permission denied. Wall detection will not work. User can grant it in Settings > Apps > Art Unbound > Permissions.");
        }
    }
}
