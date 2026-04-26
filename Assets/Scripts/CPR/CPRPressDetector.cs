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

        [Header("距离备用（无碰撞时用手掌距离驱动）")]
        [Tooltip("碰撞无触发时，用手部追踪手掌与胸部距离驱动深度/UI/触觉")]
        public bool useDistanceFallback = true;
        [Tooltip("手掌与胸部距离小于此值视为「在按压」米")]
        public float palmDistanceThreshold = 0.5f;

        [Header("调试")]
        [Tooltip("在控制台打印所有进入触发器的物体名与是否识别为手")]
        public bool debugLogTriggers = false;

        [Header("检测参数")]
        [Tooltip("开始检测的最小深度(cm)")]
        public float minDetectionDepthCm = 1f;
        
        [Tooltip("位置平滑系数")]
        [Range(0.01f, 0.3f)]
        public float smoothing = 0.1f;

        [Tooltip("手掌在「未按压」时距胸部表面的高度(m)。患者躺平时手从正上方向下按，" +
                 "此值约为手悬停在胸口上方的距离。\n" +
                 "= 此高度时深度=0；完全压到胸面时深度= compressionApproachHeight*100 cm")]
        public float compressionApproachHeight = 0.12f;

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
        public bool IsInitialized => _isInitialized;
        private Transform _currentContactHand;
        private BoxCollider _triggerCollider;
        private HandTrackingAdapter _distanceFallbackAdapter;
        private bool _usingDistanceFallback;

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

            // 关键：基准高度 = 胸部表面 Y + 手悬停高度
            // 患者躺平时手从上方向下按压，手降到胸面以下时才算"深度"
            // 加上 compressionApproachHeight 后：手距胸面 12cm 时深度=0，压到胸面时深度=12cm
            _restHeight = chestSurface.position.y + compressionApproachHeight;
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

            if (useDistanceFallback)
                _distanceFallbackAdapter = FindObjectOfType<HandTrackingAdapter>();

            Debug.Log($"[CPRPressDetector] 初始化完成 | 胸面Y={chestSurface.position.y:F3}m + 悬停偏移{compressionApproachHeight}m = 基准{_restHeight:F3}m | 碰撞:{useCollisionDetection} 距离备用:{useDistanceFallback}(adapter={(_distanceFallbackAdapter != null)})");
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

            // 每帧动态更新基准高度，防止患者动画/位移导致基准钙假
            if (chestSurface != null)
                _restHeight = chestSurface.position.y + compressionApproachHeight;

            if (useCollisionDetection)
            {
                if (_currentContactHand != null)
                {
                    _usingDistanceFallback = false;
                    UpdateDepthFromCollision(_currentContactHand.position);
                }
                else
                {
                    bool usedFallback = TryDistanceFallback();
                    if (!usedFallback)
                    {
                        if (_usingDistanceFallback)
                        {
                            _usingDistanceFallback = false;
                            _handInContact = false;
                            ProcessDepthChange(0f); // 会触发 EndCompression
                        }
                        else if (_isPressing)
                            EndCompression();
                    }
                }
            }
            else if (handPalm != null)
            {
                // 传统模式：基于手掌Transform
                UpdateDepthDetection();
            }
        }

        /// <summary> 距离备用：用手掌与胸部距离驱动深度，不依赖触发器 </summary>
        private bool TryDistanceFallback()
        {
            if (!useDistanceFallback) return false;
            if (_distanceFallbackAdapter == null)
                _distanceFallbackAdapter = FindObjectOfType<HandTrackingAdapter>();
            if (_distanceFallbackAdapter == null) return false;
            Vector3 palm = _distanceFallbackAdapter.ActiveHandPosition;
            if (palm.sqrMagnitude < 0.0001f) return false;
            Transform refT = chestSurface != null ? chestSurface : transform;
            float dist = Vector3.Distance(palm, refT.position);
            if (dist <= palmDistanceThreshold)
            {
                _handInContact = true;
                _usingDistanceFallback = true;
                UpdateDepthFromCollision(palm);
                return true;
            }
            return false;
        }

        // 碰撞检测回调
        private void OnTriggerEnter(Collider other)
        {
            if (!useCollisionDetection || !_isInitialized) return;
            if (debugLogTriggers)
                Debug.Log($"[CPRPressDetector] TriggerEnter: {other.name} layer={LayerMask.LayerToName(other.gameObject.layer)} isHand={IsHandCollider(other)}");
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
            // 检查层级（若设置了手部专用 Layer）
            if (handLayerMask != -1 && ((1 << col.gameObject.layer) & handLayerMask) == 0)
                return false;

            // Meta/OVR：碰撞体在 LeftHandAnchor / RightHandAnchor 下即视为手
            Transform t = col.transform;
            while (t != null)
            {
                string p = t.name;
                if (p.Contains("LeftHandAnchor") || p.Contains("RightHandAnchor") ||
                    p.Contains("HandAnchor") || p.Contains("HandTracking"))
                    return true;
                t = t.parent;
            }

            // 通过名称或 tag 判断
            string name = col.name.ToLower();
            if (name.Contains("hand") || name.Contains("palm") || name.Contains("hands") ||
                name.Contains("finger") || name.Contains("poke") || name.Contains("wrist") ||
                name.Contains("bone") || name.Contains("customhand"))
                return true;
            if (col.CompareTag("Hand") || col.CompareTag("Player"))
                return true;

            return false;
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
