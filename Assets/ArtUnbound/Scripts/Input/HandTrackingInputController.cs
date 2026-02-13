using System;
using System.Collections.Generic;
using UnityEngine;
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

        [Header("Pinch Settings")]
        [SerializeField] private float pinchThreshold = 0.8f; // Note: XR Hands usually gives direct boolean for pinch or strength 0-1

        [Header("Swipe Settings")]
        [SerializeField] private float swipeVelocityThreshold = 0.8f;
        [SerializeField] private float minSwipeDistance = 0.05f;
        [SerializeField] private float maxSwipeVerticalVariance = 0.1f;
        [SerializeField] private float bufferTimeWindow = 0.15f;

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

        private void Start()
        {
            GetHandSubsystem();
        }

        private void Update()
        {
            if (m_HandSubsystem == null || !m_HandSubsystem.running)
            {
                GetHandSubsystem();
                return;
            }

            // Get Right Hand
            var rightHand = m_HandSubsystem.rightHand;

            if (rightHand.isTracked)
            {
                ProcessHand(rightHand, true);
            }
        }

        private void GetHandSubsystem()
        {
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

        private float debugLogTimer = 0f;

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

            // Debug Log every 1s
            if (isRight && Time.time > debugLogTimer)
            {
                Debug.Log($"[HandTracking] Right Hand Tracked. Palm Pos: {handPosition}");
                debugLogTimer = Time.time + 1.0f;
            }

            // 2. Pinch Detection (Using XRHand native pinch data if available, or manual check)
            // XRHand doesn't expose a simple "IsPinching" float directly on the struct in early versions,
            // but often extensions or the Joint data can be used. 
            // However, typical setup uses "XR Hand Tracking Events" component for high level, but we are in low level.
            // Let's calculate pinch: Distance between ThumbTip and IndexTip.

            bool currentPinch = CheckPinch(hand);

            if (currentPinch && !isPinchingRight)
            {
                Debug.Log($"[HandTracking] Pinch START at {handPosition}");
                OnPinchStart?.Invoke(handPosition, handRotation);
            }
            else if (currentPinch && isPinchingRight)
            {
                // OnPinchHold?.Invoke(handPosition, handRotation); 
            }
            else if (!currentPinch && isPinchingRight)
            {
                Debug.Log($"[HandTracking] Pinch END at {handPosition}");
                OnPinchEnd?.Invoke(handPosition, handRotation);
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
                Debug.Log($"[HandTracking] SWIPE DETECTED (XR Hands)! VelX: {averageVelocity.x:F2}");

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
