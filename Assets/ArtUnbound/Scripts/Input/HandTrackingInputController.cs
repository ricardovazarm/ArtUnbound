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

        private void Start()
        {
            if (!useControllers)
            {
                GetHandSubsystem();
            }
            else
            {
                Debug.Log("[HandTracking] Switched to CONTROLLER Input Mode.");
            }
        }

        private void Update()
        {
            if (useControllers)
            {
                ProcessControllerInput();
            }
            else
            {
                // Original Hand Tracking Logic
                if (m_HandSubsystem == null || !m_HandSubsystem.running)
                {
                    GetHandSubsystem();
                    return;
                }

                var rightHand = m_HandSubsystem.rightHand;
                if (rightHand.isTracked)
                {
                    ProcessHand(rightHand, true);
                }
            }
        }

        private void ProcessControllerInput()
        {
            // 1. Ensure we have a valid device
            if (!targetDevice.isValid)
            {
                InitializeController();
            }

            if (!targetDevice.isValid)
            {
                if (Time.time > debugPoseTimer)
                {
                    Debug.LogWarning($"[HandTracking] Processing Input: Device INVALID for {controllerNode}");
                }
                return;
            }

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

            if (Time.time > debugPoseTimer)
            {
                Debug.Log($"[HandTracking] Updating Transform to: {devicePos}");
            }

            // 3. Map Trigger to "Pinch" Events
            // Trigger Pressed = Pinch Start / Hold
            // Trigger Released = Pinch End

            if (isTriggerPressed && !wasTriggerPressed)
            {
                Debug.Log($"[ControllerInput] Trigger START at {devicePos}");
                OnPinchStart?.Invoke(devicePos, deviceRot);
            }
            else if (isTriggerPressed && wasTriggerPressed)
            {
                OnPinchHold?.Invoke(devicePos, deviceRot);
            }
            else if (!isTriggerPressed && wasTriggerPressed)
            {
                Debug.Log($"[ControllerInput] Trigger END at {devicePos}");
                OnPinchEnd?.Invoke(devicePos, deviceRot);
            }

            wasTriggerPressed = isTriggerPressed;

            // if (axis.x != 0) OnSwipeHorizontal?.Invoke(axis.x);
        }

        private float debugPoseTimer = 0f;
        private InputDevice targetDevice; // Cache the device to avoid frequent lookups

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

                    if (Time.time > debugPoseTimer)
                    {
                        Debug.Log($"[HandTracking] Controller Valid. Pos: {position} (Using Pointer: {hasPointerRot})");
                        debugPoseTimer = Time.time + 2.0f;
                    }

                    return true;
                }
                else
                {
                    if (Time.time > debugPoseTimer)
                    {
                        Debug.LogWarning($"[HandTracking] Controller for {controllerNode} NOT FOUND. Check connection.");
                        debugPoseTimer = Time.time + 2.0f;
                    }
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
                Debug.Log($"[HandTracking] Found Controller: {targetDevice.name}");
            }
            else
            {
                Debug.LogWarning($"[HandTracking] Searching for {controllerNode} controller... (Count: 0)");
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
                Debug.Log($"[HandTracking] Hand Subsystem Found and Linked.");
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

            // 2. Pinch Detection (Using XRHand native pinch data if available, or manual check)
            // Calculate pinch center (midpoint between thumb and index) for accurate interaction
            var thumbTip = hand.GetJoint(XRHandJointID.ThumbTip);
            var indexTip = hand.GetJoint(XRHandJointID.IndexTip);
            Vector3 pinchPosition = handPosition; // Fallback to palm
            bool currentPinch = false;

            if (thumbTip.TryGetPose(out Pose thumbPose) && indexTip.TryGetPose(out Pose indexPose))
            {
                float dist = Vector3.Distance(thumbPose.position, indexPose.position);
                if (dist < 0.025f) // Slightly increased threshold for reliability (2.5cm)
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

                if (dist < 0.02f) // 2cm
                {
                    // Debug.Log($"[HandTracking] Pinch Valid! Dist: {dist:F4}");
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
