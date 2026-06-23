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

        [Tooltip("Thumb-index distance (meters) below which the hand is treated as a forming grab, " +
                 "disengaging tray scroll. Must be above the pinch threshold (~3.5cm) so scroll stops " +
                 "BEFORE the grab completes. Reaching in to grab a piece must never scroll the tray.")]
        [SerializeField] private float grabIntentDistanceM = 0.06f;

        [SerializeField] private bool debugLogs = true;

        // Controller device cache
        private InputDevice _rightController;
        private float _retryTimer;

        // Hand scroll state
        private bool  _fingerInTrayVolume;  // index tip currently within the tray bounds (any posture)
        private float _scrollAtEntry;       // _targetScroll snapshot when the finger entered the volume
        private float _lastIndexWorldY;
        private bool  _wasPinching;

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

            bool pinching = handInput.IsPinching;
            // Fingers closing toward a grab? Disengage scroll BEFORE the pinch completes so the
            // reach-to-grab approach never scrolls the tray.
            bool closing  = handInput.TryGetPinchDistance(out float pinchDist)
                            && pinchDist < grabIntentDistanceM;

            // Geometric test: is the index tip inside the tray volume (independent of finger posture)?
            bool hasTip = handInput.TryGetIndexTipPosition(out Vector3 indexWorldPos);
            bool inside = false;
            if (hasTip)
            {
                Vector3 local = trayController.transform.InverseTransformPoint(indexWorldPos);
                inside = Mathf.Abs(local.x) < ViewportWidth  * 0.5f
                      && Mathf.Abs(local.y) < ViewportHeight * 0.5f
                      && Mathf.Abs(local.z) < scrollDepthTolerance;
            }

            // Track entry/exit of the tray volume; snapshot scroll on entry so a grab can undo any
            // scroll that crept in during the approach.
            if (inside && !_fingerInTrayVolume)
            {
                _fingerInTrayVolume = true;
                _scrollAtEntry      = trayController.CurrentTargetScroll;
                _lastIndexWorldY    = indexWorldPos.y;
                if (debugLogs) Debug.Log($"[TrayScroll] finger ENTERED tray (scrollAtEntry={_scrollAtEntry:F3})");
            }
            else if (!inside)
            {
                _fingerInTrayVolume = false;
            }

            // Rising edge of grab while the finger is inside the tray → revert any creep-in scroll,
            // so the row being grabbed stays exactly where it was (fixes "first row disappears on grab").
            if (pinching && !_wasPinching && _fingerInTrayVolume)
            {
                trayController.RevertScrollTo(_scrollAtEntry);
                if (debugLogs) Debug.Log($"[TrayScroll] GRAB inside tray — reverted scroll to {_scrollAtEntry:F3}");
            }
            _wasPinching = pinching;

            // Apply scroll ONLY when inside, hand open (not forming a grab), and not pinching.
            if (_fingerInTrayVolume && hasTip && !pinching && !closing)
            {
                float deltaY     = indexWorldPos.y - _lastIndexWorldY;
                _lastIndexWorldY = indexWorldPos.y;
                trayController.ScrollBy(deltaY);
            }
            else if (hasTip)
            {
                // Keep the baseline current while scroll is suppressed so re-engaging doesn't jump.
                _lastIndexWorldY = indexWorldPos.y;
            }
        }
    }
}
