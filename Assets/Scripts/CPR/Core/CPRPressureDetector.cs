using UnityEngine;
using System;

namespace CPRSimulation.Core
{
    /// <summary>
    /// CPR按压检测器 - 检测按压深度和频率
    /// </summary>
    public class CPRPressureDetector : MonoBehaviour
    {
        [Header("检测设置")]
        [Tooltip("检测区域大小")]
        public Vector3 detectionSize = new Vector3(0.15f, 0.1f, 0.15f);
        
        [Tooltip("手部层级")]
        public LayerMask handLayerMask = -1;

        [Header("CPR标准参数")]
        [Tooltip("目标按压深度(米) - 标准5-6cm")]
        public float targetDepthMin = 0.05f;
        public float targetDepthMax = 0.06f;
        
        [Tooltip("目标按压频率 - 标准100-120次/分钟")]
        public int targetRateMin = 100;
        public int targetRateMax = 120;

        [Header("当前状态 (只读)")]
        [SerializeField] private float currentDepth = 0f;
        [SerializeField] private int currentRate = 0;
        [SerializeField] private int compressionCount = 0;
        [SerializeField] private bool isCompressing = false;

        [Header("调试")]
        public bool showDebugGizmos = true;
        public bool showDebugLog = false;

        // 事件
        public event Action<float> OnDepthChanged;
        public event Action<int> OnRateChanged;
        public event Action<CompressionQuality> OnCompressionComplete;
        public event Action OnCompressionStart;

        // 私有变量
        private Vector3 restPosition;
        private float maxDepthThisPress = 0f;
        private float lastCompressionTime = 0f;
        private float[] recentCompressionTimes = new float[5];
        private int compressionTimeIndex = 0;
        private Transform trackedHand;
        private Collider handCollider;

        // 属性
        public float CurrentDepth => currentDepth;
        public int CurrentRate => currentRate;
        public int CompressionCount => compressionCount;
        public bool IsCompressing => isCompressing;

        private void Start()
        {
            restPosition = transform.position;
            
            // 添加触发器碰撞体
            var col = GetComponent<BoxCollider>();
            if (col == null)
            {
                col = gameObject.AddComponent<BoxCollider>();
            }
            col.isTrigger = true;
            col.size = detectionSize;

            // 添加刚体（用于触发检测）
            var rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            rb.isKinematic = true;
            rb.useGravity = false;

            if (showDebugLog)
                Debug.Log("[CPR] 按压检测器已初始化");
        }

        private void OnTriggerEnter(Collider other)
        {
            // 检测手部进入
            if (IsHandCollider(other))
            {
                trackedHand = other.transform;
                handCollider = other;
                
                if (!isCompressing)
                {
                    StartCompression();
                }
                
                if (showDebugLog)
                    Debug.Log($"[CPR] 手部进入检测区域: {other.name}");
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (handCollider == other && trackedHand != null)
            {
                UpdateCompression();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (handCollider == other)
            {
                if (isCompressing)
                {
                    EndCompression();
                }
                
                trackedHand = null;
                handCollider = null;
                
                if (showDebugLog)
                    Debug.Log("[CPR] 手部离开检测区域");
            }
        }

        private bool IsHandCollider(Collider col)
        {
            // 检查层级
            if (handLayerMask != -1 && ((1 << col.gameObject.layer) & handLayerMask) == 0)
                return false;

            // 检查名称
            string name = col.name.ToLower();
            return name.Contains("hand") || 
                   name.Contains("palm") || 
                   name.Contains("finger") ||
                   name.Contains("poke") ||
                   col.GetComponent<Rigidbody>() != null; // VR手部通常有Rigidbody
        }

        private void StartCompression()
        {
            isCompressing = true;
            maxDepthThisPress = 0f;
            OnCompressionStart?.Invoke();
            
            if (showDebugLog)
                Debug.Log("[CPR] 按压开始");
        }

        private void UpdateCompression()
        {
            if (trackedHand == null) return;

            // 计算按压深度（手部Y位置相对于检测器位置）
            float handY = trackedHand.position.y;
            float detectorY = transform.position.y;
            
            // 深度 = 检测器位置 - 手部位置（手部按下去时Y变小）
            currentDepth = Mathf.Max(0, detectorY - handY + detectionSize.y * 0.5f);
            
            // 记录最大深度
            if (currentDepth > maxDepthThisPress)
            {
                maxDepthThisPress = currentDepth;
            }

            OnDepthChanged?.Invoke(currentDepth);
        }

        private void EndCompression()
        {
            isCompressing = false;
            
            // 只有达到一定深度才算一次有效按压
            if (maxDepthThisPress > 0.02f)
            {
                compressionCount++;
                
                // 计算频率
                float currentTime = Time.time;
                if (lastCompressionTime > 0)
                {
                    float interval = currentTime - lastCompressionTime;
                    if (interval > 0.1f && interval < 2f)
                    {
                        // 存储时间间隔
                        recentCompressionTimes[compressionTimeIndex] = interval;
                        compressionTimeIndex = (compressionTimeIndex + 1) % recentCompressionTimes.Length;
                        
                        // 计算平均频率
                        float avgInterval = CalculateAverageInterval();
                        if (avgInterval > 0)
                        {
                            currentRate = Mathf.RoundToInt(60f / avgInterval);
                            OnRateChanged?.Invoke(currentRate);
                        }
                    }
                }
                lastCompressionTime = currentTime;

                // 评估按压质量
                var quality = EvaluateCompression(maxDepthThisPress, currentRate);
                OnCompressionComplete?.Invoke(quality);
                
                if (showDebugLog)
                    Debug.Log($"[CPR] 按压完成 - 深度: {maxDepthThisPress * 100:F1}cm, 频率: {currentRate}/min, 质量: {quality}");
            }

            currentDepth = 0f;
            maxDepthThisPress = 0f;
        }

        private float CalculateAverageInterval()
        {
            float sum = 0;
            int count = 0;
            
            foreach (var interval in recentCompressionTimes)
            {
                if (interval > 0)
                {
                    sum += interval;
                    count++;
                }
            }
            
            return count > 0 ? sum / count : 0;
        }

        private CompressionQuality EvaluateCompression(float depth, int rate)
        {
            bool depthOk = depth >= targetDepthMin && depth <= targetDepthMax;
            bool rateOk = rate >= targetRateMin && rate <= targetRateMax;
            
            if (depthOk && rateOk)
                return CompressionQuality.Perfect;
            else if (depthOk || rateOk)
                return CompressionQuality.Good;
            else if (depth > 0.03f)
                return CompressionQuality.NeedsImprovement;
            else
                return CompressionQuality.TooShallow;
        }

        /// <summary>
        /// 重置计数器
        /// </summary>
        public void ResetCounters()
        {
            compressionCount = 0;
            currentRate = 0;
            lastCompressionTime = 0;
            Array.Clear(recentCompressionTimes, 0, recentCompressionTimes.Length);
        }

        private void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;

            // 绘制检测区域
            Gizmos.color = isCompressing ? Color.green : Color.yellow;
            Gizmos.DrawWireCube(transform.position, detectionSize);

            // 绘制深度指示
            if (currentDepth > 0)
            {
                Gizmos.color = Color.red;
                Vector3 depthPos = transform.position - Vector3.up * currentDepth;
                Gizmos.DrawLine(transform.position, depthPos);
                Gizmos.DrawWireSphere(depthPos, 0.02f);
            }
        }
    }

    /// <summary>
    /// 按压质量枚举
    /// </summary>
    public enum CompressionQuality
    {
        TooShallow,      // 太浅
        NeedsImprovement, // 需要改进
        Good,            // 良好
        Perfect          // 完美
    }
}
