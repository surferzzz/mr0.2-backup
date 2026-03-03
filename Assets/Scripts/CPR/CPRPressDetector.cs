using System;
using UnityEngine;

namespace CPRSimulation
{
    public enum DepthQuality { TooShallow, Good, TooDeep }
    public enum RateQuality { TooSlow, Good, TooFast }

    /// <summary>
    /// CPR按压检测器 - 基于碰撞检测手部按压深度
    /// 适配Meta XR Interaction SDK的Poke系统
    /// 医学标准：深度5-6cm，速率100-120次/分钟
    /// </summary>
    public class CPRPressDetector : MonoBehaviour
    {
        [Header("检测模式")]
        [Tooltip("使用碰撞触发器检测（推荐用于Meta XR SDK）")]
        public bool useCollisionDetection = true;

        [Header("引用设置")]
        [Tooltip("胸部表面位置（胸骨中央）")]
        public Transform chestSurface;
        
        [Tooltip("手掌Transform（备用，当不使用碰撞检测时）")]
        public Transform handPalm;

        [Header("碰撞检测设置")]
        [Tooltip("检测区域大小（用于自动创建触发器）")]
        public Vector3 detectionAreaSize = new Vector3(0.15f, 0.1f, 0.15f);
        
        [Tooltip("检测的手部层级")]
        public LayerMask handLayerMask = -1;

        [Header("检测参数")]
        [Tooltip("开始检测的最小深度(cm)")]
        public float minDetectionDepthCm = 1f;
        
        [Tooltip("位置平滑系数")]
        [Range(0.01f, 0.3f)]
        public float smoothing = 0.1f;

        [Header("医学标准")]
        [Tooltip("最小标准深度(cm)")]
        public float minGoodDepthCm = 5f;
        
        [Tooltip("最大标准深度(cm)")]
        public float maxGoodDepthCm = 6f;
        
        [Tooltip("最小标准速率(次/分钟)")]
        public float minGoodRateBPM = 100f;
        
        [Tooltip("最大标准速率(次/分钟)")]
        public float maxGoodRateBPM = 120f;

        [Header("实时数据（只读）")]
        [SerializeField] private float _currentDepthCm;
        [SerializeField] private float _currentRateBPM;
        [SerializeField] private int _compressionCount;
        [SerializeField] private bool _handInContact;

        // 公开属性
        public float CurrentDepthCm => _currentDepthCm;
        public float CurrentRateBPM => _currentRateBPM;
        public int CompressionCount => _compressionCount;
        public bool IsHandInContact => _handInContact;
        
        public DepthQuality DepthStatus
        {
            get
            {
                if (_currentDepthCm < minGoodDepthCm) return DepthQuality.TooShallow;
                if (_currentDepthCm > maxGoodDepthCm) return DepthQuality.TooDeep;
                return DepthQuality.Good;
            }
        }

        public RateQuality RateStatus
        {
            get
            {
                if (_currentRateBPM < minGoodRateBPM) return RateQuality.TooSlow;
                if (_currentRateBPM > maxGoodRateBPM) return RateQuality.TooFast;
                return RateQuality.Good;
            }
        }

        // 事件
        public event Action<float> OnDepthChanged;
        public event Action OnCompressionStart;
        public event Action OnCompressionEnd;
        public event Action<float, float> OnCompressionComplete; // (深度cm, 速率BPM)

        // 内部状态
        private float _restHeight;
        private Vector3 _smoothedPalmPos;
        private bool _isPressing;
        private float _lastCompressionTime = -1f;
        private float _peakDepthThisPress;
        private bool _isInitialized;
        private Transform _currentContactHand;
        private BoxCollider _triggerCollider;

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (chestSurface == null)
            {
                Debug.LogWarning("[CPRPressDetector] 未设置chestSurface，将使用自身位置作为胸部位置");
                chestSurface = transform;
            }

            _restHeight = chestSurface.position.y;
            _smoothedPalmPos = Vector3.zero;
            _isPressing = false;
            _currentDepthCm = 0f;
            _currentRateBPM = 0f;
            _compressionCount = 0;
            _lastCompressionTime = -1f;
            _handInContact = false;
            _isInitialized = true;

            // 设置碰撞检测
            if (useCollisionDetection)
            {
                SetupCollisionDetection();
            }

            Debug.Log($"[CPRPressDetector] 初始化完成，胸部基准高度: {_restHeight:F3}m, 碰撞检测: {useCollisionDetection}");
        }

        private void SetupCollisionDetection()
        {
            // 确保有Rigidbody（设为Kinematic）
            var rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            rb.isKinematic = true;
            rb.useGravity = false;

            // 创建触发器碰撞体
            _triggerCollider = GetComponent<BoxCollider>();
            if (_triggerCollider == null)
            {
                _triggerCollider = gameObject.AddComponent<BoxCollider>();
            }
            _triggerCollider.isTrigger = true;
            _triggerCollider.size = detectionAreaSize;
            _triggerCollider.center = Vector3.zero;

            Debug.Log("[CPRPressDetector] 碰撞检测区域已设置");
        }

        public void ResetStats()
        {
            _compressionCount = 0;
            _currentRateBPM = 0f;
            _lastCompressionTime = -1f;
        }

        private void Update()
        {
            if (!_isInitialized) return;

            if (useCollisionDetection)
            {
                // 碰撞检测模式：在OnTriggerStay中处理
                if (_currentContactHand != null)
                {
                    UpdateDepthFromCollision(_currentContactHand.position);
                }
                else if (_isPressing)
                {
                    // 手离开了，结束按压
                    EndCompression();
                }
            }
            else if (handPalm != null)
            {
                // 传统模式：基于手掌Transform
                UpdateDepthDetection();
            }
        }

        // 碰撞检测回调
        private void OnTriggerEnter(Collider other)
        {
            if (!useCollisionDetection || !_isInitialized) return;
            
            if (IsHandCollider(other))
            {
                _handInContact = true;
                _currentContactHand = other.transform;
                Debug.Log($"[CPRPressDetector] 手部进入检测区域: {other.name}");
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (!useCollisionDetection || !_isInitialized) return;
            
            if (IsHandCollider(other) && _currentContactHand == null)
            {
                _currentContactHand = other.transform;
                _handInContact = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!useCollisionDetection || !_isInitialized) return;
            
            if (other.transform == _currentContactHand)
            {
                _handInContact = false;
                _currentContactHand = null;
                Debug.Log("[CPRPressDetector] 手部离开检测区域");
            }
        }

        private bool IsHandCollider(Collider col)
        {
            // 检查是否是手部碰撞体
            if (handLayerMask != -1 && ((1 << col.gameObject.layer) & handLayerMask) == 0)
                return false;

            // 通过名称或tag判断
            string name = col.name.ToLower();
            return name.Contains("hand") || name.Contains("palm") || 
                   name.Contains("finger") || name.Contains("poke") ||
                   col.CompareTag("Hand") || col.CompareTag("Player");
        }

        private void UpdateDepthFromCollision(Vector3 handPosition)
        {
            // 平滑手掌位置
            if (_smoothedPalmPos == Vector3.zero)
                _smoothedPalmPos = handPosition;
            
            _smoothedPalmPos = Vector3.Lerp(
                _smoothedPalmPos, 
                handPosition, 
                1f - Mathf.Exp(-Time.deltaTime / smoothing)
            );

            // 计算深度（基于Y轴向下的距离）
            float depthMeters = Mathf.Max(0f, _restHeight - _smoothedPalmPos.y);
            float newDepthCm = depthMeters * 100f;

            ProcessDepthChange(newDepthCm);
        }

        private void UpdateDepthDetection()
        {
            // 平滑手掌位置
            if (_smoothedPalmPos == Vector3.zero)
                _smoothedPalmPos = handPalm.position;
            
            _smoothedPalmPos = Vector3.Lerp(
                _smoothedPalmPos, 
                handPalm.position, 
                1f - Mathf.Exp(-Time.deltaTime / smoothing)
            );

            // 计算深度（米转厘米）
            float depthMeters = Mathf.Max(0f, _restHeight - _smoothedPalmPos.y);
            float newDepthCm = depthMeters * 100f;

            ProcessDepthChange(newDepthCm);
        }

        private void ProcessDepthChange(float newDepthCm)
        {
            // 深度变化检测
            if (!Mathf.Approximately(newDepthCm, _currentDepthCm))
            {
                _currentDepthCm = newDepthCm;
                OnDepthChanged?.Invoke(_currentDepthCm);
            }

            // 按压状态检测
            bool nowPressing = _currentDepthCm >= minDetectionDepthCm;

            if (nowPressing && !_isPressing)
            {
                // 开始按压
                _isPressing = true;
                _peakDepthThisPress = _currentDepthCm;
                OnCompressionStart?.Invoke();
            }
            else if (nowPressing && _isPressing)
            {
                // 按压中，记录峰值
                _peakDepthThisPress = Mathf.Max(_peakDepthThisPress, _currentDepthCm);
            }
            else if (!nowPressing && _isPressing)
            {
                // 按压结束
                EndCompression();
            }
        }

        private void EndCompression()
        {
            if (!_isPressing) return;
            
            _isPressing = false;
            _currentDepthCm = 0f;
            RecordCompression();
            OnCompressionEnd?.Invoke();
        }

        private void RecordCompression()
        {
            _compressionCount++;

            // 计算速率
            if (_lastCompressionTime > 0f)
            {
                float interval = Time.time - _lastCompressionTime;
                if (interval > 0.1f)
                {
                    float instantRate = 60f / interval;
                    // 平滑速率计算
                    _currentRateBPM = Mathf.Lerp(_currentRateBPM, instantRate, 0.3f);
                }
            }
            _lastCompressionTime = Time.time;

            OnCompressionComplete?.Invoke(_peakDepthThisPress, _currentRateBPM);
            
            Debug.Log($"[CPRPressDetector] 按压#{_compressionCount}: 深度={_peakDepthThisPress:F1}cm, 速率={_currentRateBPM:F0}BPM");
        }

        // 用于调试 - 在Scene视图中绘制检测区域
        private void OnDrawGizmos()
        {
            if (chestSurface == null) return;

            // 绘制胸部位置
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(chestSurface.position, 0.05f);

            // 绘制标准深度范围
            Gizmos.color = Color.green;
            Vector3 minDepthPos = chestSurface.position - Vector3.up * (minGoodDepthCm / 100f);
            Vector3 maxDepthPos = chestSurface.position - Vector3.up * (maxGoodDepthCm / 100f);
            Gizmos.DrawWireSphere(minDepthPos, 0.03f);
            Gizmos.DrawWireSphere(maxDepthPos, 0.03f);
        }
    }
}
