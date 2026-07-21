using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR; // Required for XRNode, InputDevice
using UnityEngine.XR.Hands; // Requires com.unity.xr.hands
using UnityEngine.XR.Management;

namespace ArtUnbound.Input
{
    public class HandTrackingInputController : MonoBehaviour
    {
        public event Action<Vector3, Quaternion> OnPinchStart;
        public event Action<Vector3, Quaternion> OnPinchHold;
        public event Action<Vector3, Quaternion> OnPinchEnd;
        public event Action<float> OnSwipeHorizontal;

        [Header("Input Setup")]
        public bool useControllers = true; // Make it accessible by InteractionManager
        [SerializeField] private XRNode controllerNode = XRNode.RightHand; // Primary controller

        [Header("Pinch Settings (Hand Tracking)")]
        [Tooltip("Max distance (cm) between thumb and index to register pinch. Higher = easier to trigger.")]
        [SerializeField] private float pinchThresholdCm = 3.5f;

        [Header("Swipe Settings")]
        [SerializeField] private float swipeVelocityThreshold = 0.8f;
        [SerializeField] private float minSwipeDistance = 0.05f;
        [SerializeField] private float maxSwipeVerticalVariance = 0.1f;
        [SerializeField] private float bufferTimeWindow = 0.15f;

        private float retryTimer = 0f; // NEW: Timer for device retries

        // XR Hands Subsystem Reference
        private XRHandSubsystem m_HandSubsystem;

        // Swipe Detection State
        private struct HandPositionSample
        {
            public Vector3 position;
            public float time;
        }

        private Queue<HandPositionSample> rightHandPositionBuffer = new Queue<HandPositionSample>();
        private float swipeCooldownTimer = 0f;
        private const float SWIPE_COOLDOWN = 0.5f;

        // State tracking
        private bool isPinchingRight;
        private bool wasTriggerPressed; // Track previous trigger state for events
        // If assigned, we move THIS object instead of the script's GameObject
        [Tooltip("Assign the Root GameObject of the controller (e.g., 'Left Controller') to move the whole hierarchy.")]
        [SerializeField] private Transform trackedObject; // Renamed from visualObject

        [Space(10)]
        [Header("Correction Offsets")]
        [SerializeField] private Vector3 visualPositionOffset;
        [SerializeField] private Vector3 visualRotationOffset;

        public Transform TrackedObject => trackedObject;

        /// <summary>True while a pinch (hand) or trigger press (controller) is active.</summary>
        public bool IsPinching => useControllers ? wasTriggerPressed : isPinchingRight;

        /// <summary>True si esta instancia rastrea la mano/control IZQUIERDO (controllerNode).</summary>
        public bool IsLeftHand => controllerNode == XRNode.LeftHand;

        /// <summary>True if a physical right controller device is currently connected and valid,
        /// regardless of whether hand tracking is also active.</summary>
        public bool HasValidController { get; private set; }

        private float _autoDetectTimer = 0f;
        private const float AutoDetectInterval = 1f;
        private Transform _xrOriginRoot;

        private void Start()
        {
            GetHandSubsystem();
            AutoDetectInputMode();
        }

        private void Update()
        {
            // Re-check input mode every second (user may put down controllers and use hands)
            _autoDetectTimer += Time.deltaTime;
            if (_autoDetectTimer >= AutoDetectInterval)
            {
                _autoDetectTimer = 0f;
                AutoDetectInputMode();
            }

            if (useControllers)
            {
                ProcessControllerInput();
            }
            else
            {
                if (m_HandSubsystem == null || !m_HandSubsystem.running)
                {
                    GetHandSubsystem();
                    return;
                }

                // Respetar la mano configurada en controllerNode (hay una instancia por mano en la
                // escena, cada una con su InteractionManager). Antes se leia rightHand fijo, lo que
                // dejaba muerta la mano izquierda en hand tracking.
                var trackedHand = (controllerNode == XRNode.LeftHand)
                    ? m_HandSubsystem.leftHand
                    : m_HandSubsystem.rightHand;
                if (trackedHand.isTracked)
                {
                    ProcessHand(trackedHand, controllerNode != XRNode.LeftHand);
                }
            }
        }

        private void AutoDetectInputMode()
        {
            // Check for a valid controller on THIS instance's side (controllerNode)
            var devices = new System.Collections.Generic.List<InputDevice>();
            InputDeviceCharacteristics side = (controllerNode == XRNode.LeftHand)
                ? InputDeviceCharacteristics.Left
                : InputDeviceCharacteristics.Right;
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Controller | side, devices);
            bool hasController = devices.Count > 0 && devices[0].isValid;

            // Check for active hand tracking on this instance's side
            bool hasHands = m_HandSubsystem != null
                         && m_HandSubsystem.running
                         && ((controllerNode == XRNode.LeftHand)
                              ? m_HandSubsystem.leftHand.isTracked
                              : m_HandSubsystem.rightHand.isTracked);

            HasValidController = hasController;

            bool newMode = hasController && !hasHands;
            if (newMode != useControllers)
            {
                useControllers = newMode;
                Debug.Log($"[HandTracking] Input mode → {(useControllers ? "CONTROLLER" : "HAND TRACKING")}");
            }
        }

        private void ProcessControllerInput()
        {
            // 1. Ensure we have a valid device
            if (!targetDevice.isValid)
            {
                InitializeController();
            }

            if (!targetDevice.isValid) return;

            // 2. Get Data from cached device
            bool hasPos = targetDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 devicePos);
            bool hasRot = targetDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion deviceRot);
            bool hasTrigger = targetDevice.TryGetFeatureValue(CommonUsages.triggerButton, out bool isTriggerPressed);

            if (!hasPos || !hasRot) return;

            // FIX: Update the visual transform of this controller so it doesn't stay at 0,0,0
            if (trackedObject != null)
            {
                // Apply offsets if visual object is assigned
                trackedObject.localPosition = devicePos + visualPositionOffset;
                trackedObject.localRotation = deviceRot * Quaternion.Euler(visualRotationOffset);
            }
            else
            {
                transform.localPosition = devicePos;
                transform.localRotation = deviceRot;
            }

            // 3. Map Trigger to "Pinch" Events (transform to world space first)
            var xrRoot = GetXROriginRoot();
            Vector3 worldPos = xrRoot != null ? xrRoot.TransformPoint(devicePos) : devicePos;
            Quaternion worldRot = xrRoot != null ? xrRoot.rotation * deviceRot : deviceRot;

            if (isTriggerPressed && !wasTriggerPressed)
                OnPinchStart?.Invoke(worldPos, worldRot);
            else if (isTriggerPressed && wasTriggerPressed)
                OnPinchHold?.Invoke(worldPos, worldRot);
            else if (!isTriggerPressed && wasTriggerPressed)
                OnPinchEnd?.Invoke(worldPos, worldRot);

            wasTriggerPressed = isTriggerPressed;

            // if (axis.x != 0) OnSwipeHorizontal?.Invoke(axis.x);
        }

        private float debugPoseTimer = 0f;
        private InputDevice targetDevice; // Cache the device to avoid frequent lookups

        public bool TryGetIndexTipPosition(out Vector3 worldPosition)
        {
            if (m_HandSubsystem != null && m_HandSubsystem.running)
            {
                var hand = (controllerNode == XRNode.LeftHand) ? m_HandSubsystem.leftHand : m_HandSubsystem.rightHand;
                if (hand.isTracked)
                {
                    var joint = hand.GetJoint(XRHandJointID.IndexTip);
                    if (joint.TryGetPose(out Pose pose))
                    {
                        worldPosition = pose.position;
                        return true;
                    }
                }
            }
            worldPosition = Vector3.zero;
            return false;
        }

        /// <summary>
        /// Returns the current thumb-tip ↔ index-tip distance (meters) for the tracked hand.
        /// Used to detect a *forming* grab gesture (fingers closing) so tray scroll can disengage
        /// BEFORE the pinch threshold is crossed — reaching in to grab a tray piece must not scroll.
        /// Returns false in controller mode or when the hand/joints aren't tracked.
        /// </summary>
        public bool TryGetPinchDistance(out float meters)
        {
            if (!useControllers && m_HandSubsystem != null && m_HandSubsystem.running)
            {
                var hand = (controllerNode == XRNode.LeftHand) ? m_HandSubsystem.leftHand : m_HandSubsystem.rightHand;
                if (hand.isTracked)
                {
                    var thumb = hand.GetJoint(XRHandJointID.ThumbTip);
                    var index = hand.GetJoint(XRHandJointID.IndexTip);
                    if (thumb.TryGetPose(out Pose tp) && index.TryGetPose(out Pose ip))
                    {
                        meters = Vector3.Distance(tp.position, ip.position);
                        return true;
                    }
                }
            }
            meters = float.MaxValue;
            return false;
        }

        public bool GetPointerPose(out Vector3 position, out Quaternion rotation)
        {
            if (useControllers)
            {
                // 1. Try to find the device if we haven't already or if it became invalid
                if (!targetDevice.isValid)
                {
                    InitializeController();
                }

                // 2. If valid, get data
                if (targetDevice.isValid)
                {
                    // FIX: Try to get 'Pointer' pose first (Aiming direction), otherwise 'Device' pose (Grip)
                    var pointerPosUsage = new InputFeatureUsage<Vector3>("PointerPosition");
                    var pointerRotUsage = new InputFeatureUsage<Quaternion>("PointerRotation");

                    bool hasPointerPos = targetDevice.TryGetFeatureValue(pointerPosUsage, out position);
                    bool hasPointerRot = targetDevice.TryGetFeatureValue(pointerRotUsage, out rotation);

                    if (!hasPointerPos || !hasPointerRot)
                    {
                        // Fallback to Grip pose
                        bool p = targetDevice.TryGetFeatureValue(CommonUsages.devicePosition, out position);
                        bool r = targetDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out rotation);

                        // Apply offset for Grip -> Pointer approximation (Touch controllers match ~40 deg tilt)
                        if (r) rotation = rotation * Quaternion.Euler(40, 0, 0);
                    }

                    // Transform from tracking space (local to XR Origin) to world space
                    var xrRoot = GetXROriginRoot();
                    if (xrRoot != null)
                    {
                        position = xrRoot.TransformPoint(position);
                        rotation = xrRoot.rotation * rotation;
                    }
                    return true;
                }
            }
            else
            {
                // Hand Tracking Mode Pointer
                if (m_HandSubsystem != null && m_HandSubsystem.running)
                {
                    var hand = (controllerNode == XRNode.LeftHand) ? m_HandSubsystem.leftHand : m_HandSubsystem.rightHand;
                    if (hand.isTracked)
                    {
                        var palm = hand.GetJoint(XRHandJointID.Palm);
                        if (palm.TryGetPose(out Pose palmPose))
                        {
                            var indexTip = hand.GetJoint(XRHandJointID.IndexTip);
                            var thumbTip = hand.GetJoint(XRHandJointID.ThumbTip);
                            
                            if (indexTip.TryGetPose(out Pose indexPose) && thumbTip.TryGetPose(out Pose thumbPose))
                            {
                                position = (indexPose.position + thumbPose.position) * 0.5f;
                            }
                            else
                            {
                                position = palmPose.position;
                            }
                            
                            // Approximate pointing direction using index finger
                            var indexProx = hand.GetJoint(XRHandJointID.IndexProximal);
                            if (indexProx.TryGetPose(out Pose proxPose) && indexTip.TryGetPose(out Pose tipPose))
                            {
                                Vector3 dir = (tipPose.position - proxPose.position).normalized;
                                if (dir != Vector3.zero) rotation = Quaternion.LookRotation(dir);
                                else rotation = palmPose.rotation;
                            }
                            else
                            {
                                // Default to a reasonable palm offset
                                rotation = palmPose.rotation * Quaternion.Euler(0, 90, 0); // Adjust based on XR Hands standard
                            }
                            // Transform from tracking space to world space
                            var xrRoot = GetXROriginRoot();
                            if (xrRoot != null)
                            {
                                position = xrRoot.TransformPoint(position);
                                rotation = xrRoot.rotation * rotation;
                            }
                            return true;
                        }
                    }
                }
            }

            // Fallback
            position = Vector3.zero;
            rotation = Quaternion.identity;
            return false;
        }

        // Camera.main → Camera Offset → XR Origin (XR Rig)
        private Transform GetXROriginRoot()
        {
            if (_xrOriginRoot == null && Camera.main != null)
                _xrOriginRoot = Camera.main.transform.parent?.parent;
            return _xrOriginRoot;
        }

        private void InitializeController()
        {
            if (Time.time < retryTimer) return; // Wait before retrying
            retryTimer = Time.time + 1.0f; // Retry every 1 second

            // Robust way to find controller
            var devices = new List<InputDevice>();
            InputDeviceCharacteristics characteristics = InputDeviceCharacteristics.Controller;

            if (controllerNode == XRNode.RightHand)
                characteristics |= InputDeviceCharacteristics.Right;
            else if (controllerNode == XRNode.LeftHand)
                characteristics |= InputDeviceCharacteristics.Left;

            InputDevices.GetDevicesWithCharacteristics(characteristics, devices);

            if (devices.Count > 0)
            {
                targetDevice = devices[0];
            }
        }

        private void GetHandSubsystem()
        {
            if (useControllers) return; // Don't start hands if using controllers

            var subsystems = new List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);

            if (subsystems.Count > 0)
            {
                m_HandSubsystem = subsystems[0];
                // Ensure it's running
                if (!m_HandSubsystem.running)
                {
                    m_HandSubsystem.Start();
                }
            }
        }

        // private float debugLogTimer = 0f; // Removed - not used

        private void ProcessHand(XRHand hand, bool isRight)
        {
            // 1. Get Palm or Index Tip Position for Swipe
            // The Palm is usually stable. Index Tip is also good.
            // Let's use the Palm for "Hand Swipe" to decouple from finger wiggling.
            var palmJoint = hand.GetJoint(XRHandJointID.Palm);


            if (!palmJoint.TryGetPose(out Pose palmPose))
            {
                return; // Pose not valid this frame
            }

            Vector3 handPosition = palmPose.position;
            Quaternion handRotation = palmPose.rotation;

            // Keep trackedObject current so BeginExternalDrag can read the hand position.
            if (trackedObject != null)
            {
                trackedObject.position = handPosition;
                trackedObject.rotation = handRotation;
            }
            else
            {
                transform.position = handPosition;
                transform.rotation = handRotation;
            }

            // 2. Pinch Detection (Using XRHand native pinch data if available, or manual check)
            // Calculate pinch center (midpoint between thumb and index) for accurate interaction
            var thumbTip = hand.GetJoint(XRHandJointID.ThumbTip);
            var indexTip = hand.GetJoint(XRHandJointID.IndexTip);
            Vector3 pinchPosition = handPosition; // Fallback to palm
            bool currentPinch = false;

            if (thumbTip.TryGetPose(out Pose thumbPose) && indexTip.TryGetPose(out Pose indexPose))
            {
                float dist = Vector3.Distance(thumbPose.position, indexPose.position);
                float thresholdM = pinchThresholdCm * 0.01f;
                if (dist < thresholdM)
                {
                    currentPinch = true;
                }
                // Pinch Center is midpoint
                pinchPosition = (thumbPose.position + indexPose.position) * 0.5f;
            }

            if (currentPinch && !isPinchingRight)
            {
                Debug.Log($"[HandTracking] Pinch START at {pinchPosition} (Palm: {handPosition})");
                OnPinchStart?.Invoke(pinchPosition, handRotation);
            }
            else if (currentPinch && isPinchingRight)
            {
                OnPinchHold?.Invoke(pinchPosition, handRotation);
            }
            else if (!currentPinch && isPinchingRight)
            {
                Debug.Log($"[HandTracking] Pinch END at {pinchPosition}");
                OnPinchEnd?.Invoke(pinchPosition, handRotation);
            }

            isPinchingRight = currentPinch;

            // 3. Swipe Detection
            DetectSwipe(handPosition);
        }

        private float pinchDebugTimer = 0f;

        private bool CheckPinch(XRHand hand)
        {
            var thumbTip = hand.GetJoint(XRHandJointID.ThumbTip);
            var indexTip = hand.GetJoint(XRHandJointID.IndexTip);

            if (thumbTip.TryGetPose(out Pose thumbPose) && indexTip.TryGetPose(out Pose indexPose))
            {
                float dist = Vector3.Distance(thumbPose.position, indexPose.position);

                // Debug distance occasionally
                if (Time.time > pinchDebugTimer)
                {
                    // Debug.Log($"[HandTracking] Pinch Dist: {dist:F3} (Thresh: 0.02)");
                    pinchDebugTimer = Time.time + 1.0f;
                }

                float thresholdM = pinchThresholdCm * 0.01f;
                if (dist < thresholdM)
                {
                    return true;
                }
            }
            return false;
        }

        private void DetectSwipe(Vector3 currentPosition)
        {
            float currentTime = Time.time;

            // Add current sample to buffer
            rightHandPositionBuffer.Enqueue(new HandPositionSample { position = currentPosition, time = currentTime });

            // Remove old samples
            while (rightHandPositionBuffer.Count > 0 && (currentTime - rightHandPositionBuffer.Peek().time) > bufferTimeWindow)
            {
                rightHandPositionBuffer.Dequeue();
            }

            if (rightHandPositionBuffer.Count < 5) return;
            if (currentTime < swipeCooldownTimer) return;

            HandPositionSample startSample = rightHandPositionBuffer.Peek();
            HandPositionSample endSample = new HandPositionSample { position = currentPosition, time = currentTime };

            Vector3 totalDisplacement = endSample.position - startSample.position;
            float timeDelta = endSample.time - startSample.time;

            if (timeDelta <= 0.0001f) return;

            Vector3 averageVelocity = totalDisplacement / timeDelta;

            // Check Swipe Criteria
            if (Mathf.Abs(totalDisplacement.x) < minSwipeDistance) return;
            if (Mathf.Abs(totalDisplacement.y) > maxSwipeVerticalVariance) return;

            if (Mathf.Abs(averageVelocity.x) > swipeVelocityThreshold)
            {
                // DISABLED: User prefers UI Buttons for scrolling to avoid jittery air-swipe return strokes.
                // OnSwipeHorizontal?.Invoke(averageVelocity.x);

                swipeCooldownTimer = currentTime + SWIPE_COOLDOWN;
                rightHandPositionBuffer.Clear();
            }
        }

        // Keep simulations for Editor workflow
        public void SimulatePinchStart(Vector3 worldPosition) => OnPinchStart?.Invoke(worldPosition, Quaternion.identity);
        public void SimulatePinchHold(Vector3 worldPosition) => OnPinchHold?.Invoke(worldPosition, Quaternion.identity);
        public void SimulatePinchEnd(Vector3 worldPosition) => OnPinchEnd?.Invoke(worldPosition, Quaternion.identity);
        public void SimulateSwipeHorizontal(float delta) => OnSwipeHorizontal?.Invoke(delta);
    }
}
