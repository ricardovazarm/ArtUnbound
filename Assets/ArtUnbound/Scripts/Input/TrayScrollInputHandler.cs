using ArtUnbound.Data;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace ArtUnbound.Input
{
    /// <summary>
    /// Feeds scroll input to PieceTray3DController from two sources:
    ///   - Right-controller thumbstick Y axis (controller mode)
    ///   - Index finger tip entering the tray volume (hand tracking mode)
    /// Scroll direction: positive deltaY / thumbstick UP → pieces move up → shows rows below.
    /// </summary>
    public class TrayScrollInputHandler : MonoBehaviour
    {
        [SerializeField] private ArtUnbound.UI.PieceTray3DController trayController;
        [SerializeField] private HandTrackingInputController handInput;
        [SerializeField] private PuzzleConfig puzzleConfig;

        [Header("Thumbstick dead zone (0-1)")]
        [SerializeField] private float deadZone = 0.15f;

        [Tooltip("How close (in meters) the index tip must be to the tray surface to trigger scroll.")]
        [SerializeField] private float scrollDepthTolerance = 0.03f;

        // Controller device cache
        private InputDevice _rightController;
        private float _retryTimer;

        // Hand scroll state
        private bool  _indexInTray;
        private float _lastIndexWorldY;

        // ── Properties ───────────────────────────────────────────────────────────

        private float ScrollSpeed    => puzzleConfig != null ? puzzleConfig.trayScrollSpeed        : 0.3f;
        private float ViewportWidth  => puzzleConfig != null ? puzzleConfig.trayViewportWidthCm  * 0.01f : 0.3f;
        private float ViewportHeight => puzzleConfig != null ? puzzleConfig.trayViewportHeightCm * 0.01f : 0.5f;

        // ── Update ───────────────────────────────────────────────────────────────

        private void Update()
        {
            HandleControllerScroll();
            HandleHandScroll();
        }

        // ── Controller thumbstick ────────────────────────────────────────────────

        private void HandleControllerScroll()
        {
            if (!_rightController.isValid)
            {
                if (Time.time < _retryTimer) return;
                _retryTimer = Time.time + 1f;

                var devices = new List<InputDevice>();
                InputDevices.GetDevicesWithCharacteristics(
                    InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right, devices);
                if (devices.Count > 0) _rightController = devices[0];
                return;
            }

            if (!_rightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 axis))
                return;

            float y = axis.y;
            if (Mathf.Abs(y) < deadZone) return;

            // Thumbstick UP (y > 0) → pieces move up → rows below appear → positive scroll
            trayController?.ScrollBy(y * ScrollSpeed * Time.deltaTime);
        }

        // ── Hand / index-tip scroll ──────────────────────────────────────────────

        private void HandleHandScroll()
        {
            if (handInput == null || trayController == null) return;

            // While pinching, disable scroll (the user is grabbing a piece)
            if (handInput.IsPinching)
            {
                _indexInTray = false;
                return;
            }

            // Read index tip position directly from HandTrackingInputController
            if (!handInput.TryGetIndexTipPosition(out Vector3 indexWorldPos))
            {
                _indexInTray = false;
                return;
            }

            // Convert to tray local space for bounds check
            Vector3 local = trayController.transform.InverseTransformPoint(indexWorldPos);

            bool inside = Mathf.Abs(local.x) < ViewportWidth  * 0.5f
                       && Mathf.Abs(local.y) < ViewportHeight * 0.5f
                       && Mathf.Abs(local.z) < scrollDepthTolerance;

            if (inside)
            {
                if (!_indexInTray)
                {
                    // Finger just entered tray area — record baseline Y
                    _lastIndexWorldY = indexWorldPos.y;
                    _indexInTray     = true;
                }
                else
                {
                    // Direct 1:1 mapping: finger moves 1 cm → tray scrolls 1 cm
                    float deltaY     = indexWorldPos.y - _lastIndexWorldY;
                    _lastIndexWorldY = indexWorldPos.y;
                    trayController.ScrollBy(deltaY);
                }
            }
            else
            {
                _indexInTray = false;
            }
        }
    }
}
