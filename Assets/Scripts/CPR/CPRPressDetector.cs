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

        [Tooltip("动态基准自校准：手一进入接触区即以当前手高为基准，之后向下压才算深度。" +
                 "开启后不再依赖胸面检测球摆放是否精准（推荐）。")]
        public bool useDynamicRestCalibration = true;

        [Header("精度增强")]
        [Tooltip("沿胸部按压轴(chestSurface.up)投影计算深度，抗患者/床体倾斜。关闭则退回世界Y轴。")]
        public bool useChestAxis = true;

        [Tooltip("开始一次按压的深度阈值(cm)。高于释放阈值形成滞回，防止抖动重复计数。")]
        public float pressStartDepthCm = 2f;

        [Tooltip("结束一次按压的深度阈值(cm)。低于此值视为完全回弹/释放。")]
        public float releaseDepthCm = 1.5f;

        [Tooltip("两次按压最小时间间隔(秒)，用于去抖，防止单次按压被拆成多次。0.2s≈最高300次/分。")]
        public float minCompressionInterval = 0.2f;

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

        [Header("回弹（完全释放）")]
        [Tooltip("一次按压结束时，胸部残余下压超过此值则判定为回弹不足（倚靠）。" +
                 "AHA 要求每次按压后胸部完全回弹，倚靠会阻碍静脉回流。")]
        public float recoilToleranceCm = 1.0f;

        [Tooltip("单次按压超过此时长仍未回弹，则强制结束并标记为倚靠。" +
                 "没有这个超时的话，倚靠会让按压永远不结束，计数和频率随之停止。")]
        public float maxPressDurationSec = 1.5f;

        // 事件
        public event Action<float> OnDepthChanged;
        public event Action OnCompressionStart;
        public event Action OnCompressionEnd;
        public event Action<float, float> OnCompressionComplete; // (深度cm, 速率BPM)

        /// <summary>
        /// 一次按压的回弹评价：(残余下压cm, 是否回弹不足)。
        /// 单独开一个事件而不是改 OnCompressionComplete 的签名 ——
        /// 后者有 5 个订阅者分布在 4 个文件里。
        /// </summary>
        public event Action<float, bool> OnRecoilEvaluated;

        /// <summary>上一次按压结束时胸部的残余下压量(cm)。0 表示完全回弹。</summary>
        public float LastRecoilDeficitCm => _lastRecoilDeficitCm;

        /// <summary>上一次按压是否回弹不足。</summary>
        public bool LastRecoilIncomplete => _lastRecoilIncomplete;

        /// <summary>相对真实胸面静止位的绝对深度(cm)，不受动态零点漂移影响。</summary>
        public float AbsoluteDepthCm => _absDepthCm;

        // 内部状态
        // 手放在胸面上静止时的按压轴坐标（= 胸面 + 悬停高度）。
        // 既是固定基准模式的零点，也是动态零点的上限。
        private Vector3 _restPoint;
        private bool _hasRestPoint;
        private float _absDepthCm;
        private float _pressStartTime;
        private float _lastRecoilDeficitCm;
        private bool _lastRecoilIncomplete;
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
        private Vector3 _dynamicRestPos;
        private bool _hasDynamicRest;

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (chestSurface == null)
            {
                // 本组件通常就挂在位于胸口的专用空物体（CPR_ChestDetector）上，
                // 所以退回自身 transform 是预期行为，不是配置错误。
                // 真正要提防的是「按压轴指错方向」—— 见下面的角度自检。
                chestSurface = transform;
            }

            // 按压轴：仰卧患者身上应当接近世界 Y。
            // 注意不要把 chestSurface 指向 Mixamo 骨骼（如 mixamorig5:Spine2）——
            // 骨骼的 up 是沿骨头方向的，在仰卧患者身上大致指向头部（水平），会把轴彻底搞错。
            Vector3 axis = useChestAxis ? chestSurface.up.normalized : Vector3.up;
            float axisTilt = Vector3.Angle(axis, Vector3.up);
            if (useChestAxis && axisTilt > 30f)
            {
                Debug.LogWarning(
                    $"[CPRPressDetector] 按压轴偏离竖直方向 {axisTilt:F0}°（参考物体「{chestSurface.name}」的 up）。" +
                    "仰卧患者身上这个角度应该接近 0°。若 chestSurface 指向了骨骼，请改回胸口的空物体，" +
                    "或关闭 useChestAxis 退回世界 Y 轴。");
            }

            _restPoint = chestSurface.position + axis * compressionApproachHeight;
            _hasRestPoint = true;
            _absDepthCm = 0f;
            _lastRecoilDeficitCm = 0f;
            _lastRecoilIncomplete = false;
            _smoothedPalmPos = Vector3.zero;
            _hasDynamicRest = false;
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

            Debug.Log($"[CPRPressDetector] 初始化完成 | 参考「{chestSurface.name}」胸面Y={chestSurface.position.y:F3}m " +
                      $"+ 悬停偏移{compressionApproachHeight}m | 按压轴偏离竖直{axisTilt:F1}° | " +
                      $"动态基准:{useDynamicRestCalibration} 回弹容差{recoilToleranceCm}cm | " +
                      $"碰撞:{useCollisionDetection} 距离备用:{useDistanceFallback}(adapter={(_distanceFallbackAdapter != null)})");
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
            _lastRecoilDeficitCm = 0f;
            _lastRecoilIncomplete = false;
        }

        private void Update()
        {
            if (!_isInitialized) return;

            // 每帧动态更新基准点，防止患者动画/位移导致基准失效。
            // 原来这里只算 Y 高度，深度却是投影到按压轴算的 —— 轴一旦不是世界 Y，
            // 两者就不自洽。现在按同一个轴算成 Vector3。
            if (chestSurface != null)
            {
                Vector3 axis = useChestAxis ? chestSurface.up.normalized : Vector3.up;
                _restPoint = chestSurface.position + axis * compressionApproachHeight;
                _hasRestPoint = true;
            }

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
                            ProcessDepthChange(0f, 0f); // 会触发 EndCompression
                        }
                        else if (_isPressing)
                            EndCompression();

                        ResetDynamicRest();
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
                ResetDynamicRest();
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
            ComputeDepths(handPosition, out float smoothedDepthCm, out float rawDepthCm);
            ProcessDepthChange(smoothedDepthCm, rawDepthCm);
        }

        /// <summary>
        /// 深度を一元的に計算し、状態機械/UI用の「平滑化深度」（揺れを抑える）と、
        /// 実測ピーク記録用の「生深度」（平滑化によるピーク低下を避ける）を同時に返す。
        /// 深度は押圧軸へ投影して計算する。useChestAxis の場合は chestSurface.up、そうでなければ世界Y軸を使う。
        /// </summary>
        private void ComputeDepths(Vector3 handPosition, out float smoothedDepthCm, out float rawDepthCm)
        {
            // 手のひら位置を平滑化（状態安定用のみ。ピーク値には使わない）
            if (_smoothedPalmPos == Vector3.zero)
                _smoothedPalmPos = handPosition;

            _smoothedPalmPos = Vector3.Lerp(
                _smoothedPalmPos,
                handPosition,
                1f - Mathf.Exp(-Time.deltaTime / smoothing)
            );

            // 押圧軸：胸部表面の法線方向（上方向がリリース、下方向が押圧）。傾きに強い
            Vector3 axis = (useChestAxis && chestSurface != null)
                ? chestSurface.up.normalized
                : Vector3.up;

            // 真实胸面静止位。动态基准模式下深度是相对手自己的释放位算的，
            // 那个零点会漂；这个是不漂的绝对参考，用来量回弹和夹住零点。
            Vector3 restPoint = _hasRestPoint
                ? _restPoint
                : chestSurface.position + axis * compressionApproachHeight;

            _absDepthCm = Mathf.Max(0f, Vector3.Dot(restPoint - handPosition, axis)) * 100f;

            if (useDynamicRestCalibration)
            {
                // 初回接触：現在の手位置を「リリース基準」とする
                if (!_hasDynamicRest)
                {
                    _dynamicRestPos = _smoothedPalmPos;
                    _hasDynamicRest = true;
                }
                // 手が押圧軸に沿って高い位置（完全リリース）へ戻ったら基準を引き上げ、毎回の押圧を0から計算する
                else if (Vector3.Dot(_smoothedPalmPos - _dynamicRestPos, axis) > 0f)
                {
                    _dynamicRestPos = _smoothedPalmPos;
                }

                // 夹取：动态零点只能向上棘轮式抬升，没有上限的话，
                // 学员的手在触发区内向上弹一点，零点就跟着抬一点，
                // 下一次按压是从抬高后的位置起算 —— 深度被高报。
                // 真实胸面静止位就是物理上的天花板，手不可能比它更「静止」。
                float aboveRest = Vector3.Dot(_dynamicRestPos - restPoint, axis);
                if (aboveRest > 0f)
                    _dynamicRestPos -= axis * aboveRest;

                smoothedDepthCm = Mathf.Max(0f, Vector3.Dot(_dynamicRestPos - _smoothedPalmPos, axis)) * 100f;
                rawDepthCm = Mathf.Max(0f, Vector3.Dot(_dynamicRestPos - handPosition, axis)) * 100f;
            }
            else
            {
                // 固定基準：胸面 + ホバー高さ。軸方向に沿って下向きへ投影する
                smoothedDepthCm = Mathf.Max(0f, Vector3.Dot(restPoint - _smoothedPalmPos, axis)) * 100f;
                rawDepthCm = _absDepthCm;
            }
        }

        private void ResetDynamicRest()
        {
            _hasDynamicRest = false;
            _smoothedPalmPos = Vector3.zero;
        }

        private void UpdateDepthDetection()
        {
            ComputeDepths(handPalm.position, out float smoothedDepthCm, out float rawDepthCm);
            ProcessDepthChange(smoothedDepthCm, rawDepthCm);
        }

        /// <summary>
        /// 双阈值滞回状态机：
        /// 起按用 pressStartDepthCm、释放用 releaseDepthCm（&lt; 起按阈值），
        /// 中间区不切换状态，杜绝在单一阈值附近抖动导致的重复计数/漏记。
        /// 峰值用「原始深度」记录，避免平滑削峰。
        /// </summary>
        private void ProcessDepthChange(float smoothedDepthCm, float rawDepthCm)
        {
            // 对外暴露平滑深度（UI/触觉用，稳定不抖）
            if (!Mathf.Approximately(smoothedDepthCm, _currentDepthCm))
            {
                _currentDepthCm = smoothedDepthCm;
                OnDepthChanged?.Invoke(_currentDepthCm);
            }

            if (!_isPressing)
            {
                // 超过起按阈值才认定为一次按压开始
                if (smoothedDepthCm >= pressStartDepthCm)
                {
                    _isPressing = true;
                    _peakDepthThisPress = rawDepthCm;
                    _pressStartTime = Time.time;
                    OnCompressionStart?.Invoke();
                }
            }
            else
            {
                // 按压中：用原始深度记录真实峰值
                _peakDepthThisPress = Mathf.Max(_peakDepthThisPress, rawDepthCm);

                // 回弹到释放阈值以下才算一次结束
                if (smoothedDepthCm <= releaseDepthCm)
                {
                    EndCompression();
                }
                else if (Time.time - _pressStartTime > maxPressDurationSec)
                {
                    // 倚靠（回弹不足）：学员压下去之后没有完全松开。
                    // 动态零点只能向上抬，所以倚靠时深度永远降不到释放阈值以下，
                    // 这一次按压就永远不会「结束」—— 计数停住、频率跟着往下掉，
                    // 学员看到的是「频率太慢」，而真实错误是回弹不足，诊断被错配。
                    // 超时强制收尾，并让 EndCompression 用绝对深度判定回弹。
                    EndCompression();
                }
            }
        }

        private void EndCompression()
        {
            if (!_isPressing) return;
            
            _isPressing = false;

            // 回弹评价：用「相对真实胸面」的绝对深度，而不是相对动态零点的深度。
            // 动态零点会跟着手漂，用它算回弹永远是 0，测不出倚靠。
            _lastRecoilDeficitCm = _absDepthCm;
            _lastRecoilIncomplete = _absDepthCm > recoilToleranceCm;

            _currentDepthCm = 0f;
            RecordCompression();
            OnRecoilEvaluated?.Invoke(_lastRecoilDeficitCm, _lastRecoilIncomplete);
            OnCompressionEnd?.Invoke();
        }

        private void RecordCompression()
        {
            // 去抖：与上一次按压间隔过短视为同一次按压的抖动，不重复计数
            if (_lastCompressionTime > 0f &&
                (Time.time - _lastCompressionTime) < minCompressionInterval)
            {
                return;
            }

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
            
            Debug.Log($"[CPRPressDetector] 按压#{_compressionCount}: 深度={_peakDepthThisPress:F1}cm, " +
                      $"速率={_currentRateBPM:F0}BPM, 回弹残余={_lastRecoilDeficitCm:F1}cm" +
                      (_lastRecoilIncomplete ? " ← 回弹不足" : ""));
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
