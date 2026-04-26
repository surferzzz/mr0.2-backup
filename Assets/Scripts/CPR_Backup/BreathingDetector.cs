using System.Collections;
using UnityEngine;
using VRFirstAid.VR;

namespace VRFirstAid.CPR
{
    /// <summary>
    /// 人工呼吸检测器 - 检测人工呼吸操作
    /// </summary>
    public class BreathingDetector : MonoBehaviour
    {
        [Header("Detection Settings")]
        [SerializeField] public Transform headArea; // 头部区域（假人头部）
        [SerializeField] private float breathingDistanceThreshold = 0.3f; // 呼吸距离阈值（米）
        [SerializeField] private float breathingDuration = 1f; // 单次呼吸持续时间（秒）
        [SerializeField] private float breathingInterval = 2f; // 呼吸间隔（秒）

        [Header("Hand Tracking")]
        [SerializeField] public VRHandController handController;
        [SerializeField] private bool useLeftHand = false;

        [Header("Detection State")]
        [SerializeField] private bool isDetecting = false;

        private bool isInBreathingPosition = false;
        private float breathingStartTime = 0f;
        private float lastBreathTime = 0f;
        private bool isBreathing = false;

        // 事件
        public System.Action OnBreathDetected;

        private void Start()
        {
            if (handController == null)
            {
                handController = FindObjectOfType<VRHandController>();
            }

            if (headArea == null)
            {
                Debug.LogWarning("HeadArea not assigned!");
            }
        }

        private void Update()
        {
            if (!isDetecting || headArea == null || handController == null) return;

            CheckBreathing();
        }

        /// <summary>
        /// 开始检测
        /// </summary>
        public void StartDetection()
        {
            isDetecting = true;
            isBreathing = false;
            lastBreathTime = Time.time;
        }

        /// <summary>
        /// 停止检测
        /// </summary>
        public void StopDetection()
        {
            isDetecting = false;
            isBreathing = false;
        }

        private void CheckBreathing()
        {
            Vector3 handPosition = handController.GetHandPosition(useLeftHand);
            Vector3 headPosition = headArea.position;

            float distance = Vector3.Distance(handPosition, headPosition);
            bool wasInPosition = isInBreathingPosition;
            isInBreathingPosition = distance <= breathingDistanceThreshold;

            if (isInBreathingPosition)
            {
                if (!wasInPosition)
                {
                    // 刚进入呼吸位置
                    breathingStartTime = Time.time;
                }

                // 检查是否持续足够时间（模拟嘴部覆盖）
                float elapsedTime = Time.time - breathingStartTime;
                if (elapsedTime >= breathingDuration && !isBreathing)
                {
                    // 检测到有效呼吸
                    if (Time.time - lastBreathTime >= breathingInterval)
                    {
                        RecordBreath();
                    }
                }
            }
            else
            {
                isBreathing = false;
            }
        }

        private void RecordBreath()
        {
            isBreathing = true;
            lastBreathTime = Time.time;
            OnBreathDetected?.Invoke();
        }

        /// <summary>
        /// 检查是否在正确位置进行人工呼吸
        /// </summary>
        public bool IsCorrectBreathing()
        {
            return isInBreathingPosition;
        }

        /// <summary>
        /// 检查是否正在进行人工呼吸
        /// </summary>
        public bool IsBreathing()
        {
            return isBreathing;
        }

        /// <summary>
        /// 获取到头部区域的距离
        /// </summary>
        public float GetDistanceToHead()
        {
            if (headArea == null || handController == null) return float.MaxValue;

            Vector3 handPosition = handController.GetHandPosition(useLeftHand);
            Vector3 headPosition = headArea.position;

            return Vector3.Distance(handPosition, headPosition);
        }
    }
}
