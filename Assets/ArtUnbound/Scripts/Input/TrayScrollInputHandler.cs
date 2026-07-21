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
        [Tooltip("Instancia IZQUIERDA de HandTrackingInputController. Si se deja vacia, se resuelve sola en Awake buscando la instancia con controllerNode = LeftHand.")]
        [SerializeField] private HandTrackingInputController handInputLeft;
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

        // Hand scroll state — una por mano (ambas pueden operar el tray, como el agarre de piezas)
        private class HandScrollState
        {
            public bool  fingerInTrayVolume;  // index tip currently within the tray bounds (any posture)
            public float scrollAtEntry;       // _targetScroll snapshot when the finger entered the volume
            public float lastIndexWorldY;
            public bool  wasPinching;
        }
        private readonly HandScrollState _rightHandState = new HandScrollState();
        private readonly HandScrollState _leftHandState  = new HandScrollState();

        private void Awake()
        {
            // Resolver las instancias por mano si faltan (la escena tiene una por lado).
            if (handInput == null || handInputLeft == null)
            {
                foreach (var c in FindObjectsByType<HandTrackingInputController>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (c.IsLeftHand)
                    {
                        if (handInputLeft == null) handInputLeft = c;
                    }
                    else if (handInput == null)
                    {
                        handInput = c;
                    }
                }
            }
        }

        // ── Properties ───────────────────────────────────────────────────────────

        private float ScrollSpeed    => puzzleConfig != null ? puzzleConfig.trayScrollSpeed        : 0.3f;
        private float ViewportWidth  => puzzleConfig != null ? puzzleConfig.trayViewportWidthCm  * 0.01f : 0.3f;
        private float ViewportHeight => puzzleConfig != null ? puzzleConfig.trayViewportHeightCm * 0.01f : 0.5f;

        // ── Update ───────────────────────────────────────────────────────────────

        private void Update()
        {
            HandleControllerScroll();
            HandleHandScroll(handInput,     _rightHandState);
            HandleHandScroll(handInputLeft, _leftHandState);
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

        private void HandleHandScroll(HandTrackingInputController input, HandScrollState s)
        {
            if (input == null || trayController == null) return;

            bool pinching = input.IsPinching;
            // Fingers closing toward a grab? Disengage scroll BEFORE the pinch completes so the
            // reach-to-grab approach never scrolls the tray.
            bool closing  = input.TryGetPinchDistance(out float pinchDist)
                            && pinchDist < grabIntentDistanceM;

            // Geometric test: is the index tip inside the tray volume (independent of finger posture)?
            bool hasTip = input.TryGetIndexTipPosition(out Vector3 indexWorldPos);
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
            if (inside && !s.fingerInTrayVolume)
            {
                s.fingerInTrayVolume = true;
                s.scrollAtEntry      = trayController.CurrentTargetScroll;
                s.lastIndexWorldY    = indexWorldPos.y;
                if (debugLogs) Debug.Log($"[TrayScroll] finger ENTERED tray ({(input.IsLeftHand ? "L" : "R")}, scrollAtEntry={s.scrollAtEntry:F3})");
            }
            else if (!inside)
            {
                s.fingerInTrayVolume = false;
            }

            // Rising edge of grab while the finger is inside the tray → revert any creep-in scroll,
            // so the row being grabbed stays exactly where it was (fixes "first row disappears on grab").
            if (pinching && !s.wasPinching && s.fingerInTrayVolume)
            {
                trayController.RevertScrollTo(s.scrollAtEntry);
                if (debugLogs) Debug.Log($"[TrayScroll] GRAB inside tray — reverted scroll to {s.scrollAtEntry:F3}");
            }
            s.wasPinching = pinching;

            // Apply scroll ONLY when inside, hand open (not forming a grab), and not pinching.
            if (s.fingerInTrayVolume && hasTip && !pinching && !closing)
            {
                float deltaY       = indexWorldPos.y - s.lastIndexWorldY;
                s.lastIndexWorldY  = indexWorldPos.y;
                trayController.ScrollBy(deltaY);
            }
            else if (hasTip)
            {
                // Keep the baseline current while scroll is suppressed so re-engaging doesn't jump.
                s.lastIndexWorldY = indexWorldPos.y;
            }
        }
    }
}
