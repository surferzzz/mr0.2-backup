using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRFirstAid.VR
{
    /// <summary>
    /// VR手部控制器 - 管理手部追踪和交互
    /// </summary>
    public class VRHandController : MonoBehaviour
    {
        [Header("Hand References")]
        [SerializeField] private XRDirectInteractor leftHandInteractor;
        [SerializeField] private XRDirectInteractor rightHandInteractor;

        [Header("Hand Tracking")]
        [SerializeField] private bool enableHandTracking = true;

        private Transform leftHandTransform;
        private Transform rightHandTransform;

        public Transform LeftHandTransform => leftHandTransform;
        public Transform RightHandTransform => rightHandTransform;

        private void Start()
        {
            InitializeHands();
        }

        private void InitializeHands()
        {
            if (leftHandInteractor != null)
            {
                leftHandTransform = leftHandInteractor.transform;
            }

            if (rightHandInteractor != null)
            {
                rightHandTransform = rightHandInteractor.transform;
            }
        }

        /// <summary>
        /// 获取手部位置
        /// </summary>
        public Vector3 GetHandPosition(bool isLeftHand)
        {
            Transform handTransform = isLeftHand ? leftHandTransform : rightHandTransform;
            return handTransform != null ? handTransform.position : Vector3.zero;
        }

        /// <summary>
        /// 获取手部旋转
        /// </summary>
        public Quaternion GetHandRotation(bool isLeftHand)
        {
            Transform handTransform = isLeftHand ? leftHandTransform : rightHandTransform;
            return handTransform != null ? handTransform.rotation : Quaternion.identity;
        }

        /// <summary>
        /// 检查手部是否在指定位置附近
        /// </summary>
        public bool IsHandNearPosition(Vector3 targetPosition, float threshold, bool isLeftHand)
        {
            Vector3 handPosition = GetHandPosition(isLeftHand);
            return Vector3.Distance(handPosition, targetPosition) <= threshold;
        }

        /// <summary>
        /// 计算手部到目标位置的距离
        /// </summary>
        public float GetDistanceToPosition(Vector3 targetPosition, bool isLeftHand)
        {
            Vector3 handPosition = GetHandPosition(isLeftHand);
            return Vector3.Distance(handPosition, targetPosition);
        }
    }
}
