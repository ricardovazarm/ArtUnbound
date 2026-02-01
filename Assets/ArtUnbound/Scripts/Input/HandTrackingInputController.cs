using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace ArtUnbound.Input
{
    public class HandTrackingInputController : MonoBehaviour
    {
        public event Action<Vector3> OnPinchStart;
        public event Action<Vector3> OnPinchHold;
        public event Action<Vector3> OnPinchEnd;
        public event Action<float> OnSwipeHorizontal;

        [SerializeField] private float pinchThreshold = 0.8f;
        [SerializeField] private float swipeVelocityThreshold = 0.5f;

        private bool isPinchingRight;
        private bool isPinchingLeft;
        
        private InputDevice rightHandDevice;
        private InputDevice leftHandDevice;

        private Vector3 lastRightHandPosition;
        private float swipeCheckTimer = 0f;

        private void Update()
        {
            UpdateDevices();
            ProcessHand(rightHandDevice, true);
            // ProcessHand(leftHandDevice, false); // Optional: Enable for left hand if needed
        }

        private void UpdateDevices()
        {
            if (!rightHandDevice.isValid)
            {
                GetDevice(InputDeviceCharacteristics.HandTracking | InputDeviceCharacteristics.Right, ref rightHandDevice);
            }
            
            if (!leftHandDevice.isValid)
            {
                GetDevice(InputDeviceCharacteristics.HandTracking | InputDeviceCharacteristics.Left, ref leftHandDevice);
            }
        }

        private void GetDevice(InputDeviceCharacteristics characteristics, ref InputDevice device)
        {
            List<InputDevice> devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(characteristics, devices);
            if (devices.Count > 0)
            {
                device = devices[0];
            }
        }

        private void ProcessHand(InputDevice device, bool isRight)
        {
            if (!device.isValid) return;

            // Check Pinch (Index Finger + Thumb)
            // Note: Common usage map - Trigger or PrimaryButton usually maps to pinch in Hand Tracking profiles
            bool currentPinch = false;
            
            // Try to get specific feature usage for pinch strength if available (e.g. subsystem specific)
            // Fallback to primary button for broad compatibility
            if (device.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue))
            {
                currentPinch = triggerValue > pinchThreshold;
            }
            else if (device.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryPressed))
            {
                currentPinch = primaryPressed;
            }

            // Get Hand Position
            Vector3 handPosition = Vector3.zero;
            if (device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos))
            {
                handPosition = pos;
            }

            // Handle State Changes
            bool wasPinching = isRight ? isPinchingRight : isPinchingLeft;

            if (currentPinch && !wasPinching)
            {
                OnPinchStart?.Invoke(handPosition);
            }
            else if (currentPinch && wasPinching)
            {
                OnPinchHold?.Invoke(handPosition);
            }
            else if (!currentPinch && wasPinching)
            {
                OnPinchEnd?.Invoke(handPosition);
            }

            if (isRight) isPinchingRight = currentPinch;
            else isPinchingLeft = currentPinch;

            // Handle Swipe (Simple velocity check on X axis)
            if (isRight)
            {
                float velocityX = (handPosition.x - lastRightHandPosition.x) / Time.deltaTime;
                if (Mathf.Abs(velocityX) > swipeVelocityThreshold)
                {
                    // Rate limit swipes
                    if (Time.time > swipeCheckTimer)
                    {
                        OnSwipeHorizontal?.Invoke(velocityX);
                        swipeCheckTimer = Time.time + 0.5f;
                    }
                }
                lastRightHandPosition = handPosition;
            }
        }

        // Public simulation methods for Editor testing
        public void SimulatePinchStart(Vector3 worldPosition) => OnPinchStart?.Invoke(worldPosition);
        public void SimulatePinchHold(Vector3 worldPosition) => OnPinchHold?.Invoke(worldPosition);
        public void SimulatePinchEnd(Vector3 worldPosition) => OnPinchEnd?.Invoke(worldPosition);
        public void SimulateSwipeHorizontal(float delta) => OnSwipeHorizontal?.Invoke(delta);
    }
}
