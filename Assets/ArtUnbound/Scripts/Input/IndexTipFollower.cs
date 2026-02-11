using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;

namespace ArtUnbound.Input
{
    /// <summary>
    /// Forces the object to follow the Index Finger Tip using XR Hands.
    /// Attach this to the Poke Interactor.
    /// </summary>
    public class IndexTipFollower : MonoBehaviour
    {
        [Tooltip("Which hand to follow?")]
        public Handedness handedness = Handedness.Left;

        private XRHandSubsystem m_Subsystem;

        private void Update()
        {
            if (m_Subsystem == null || !m_Subsystem.running)
            {
                m_Subsystem = XRGeneralSettings.Instance?.Manager?.activeLoader?.GetLoadedSubsystem<XRHandSubsystem>();
                if (m_Subsystem == null) return;
            }

            var hand = handedness == Handedness.Left ? m_Subsystem.leftHand : m_Subsystem.rightHand;

            if (hand.isTracked)
            {
                var joint = hand.GetJoint(XRHandJointID.IndexTip);
                if (joint.TryGetPose(out Pose pose))
                {
                    transform.position = pose.position;
                    transform.rotation = pose.rotation;
                }
            }
        }
    }
}
