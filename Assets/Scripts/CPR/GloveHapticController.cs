using UnityEngine;
using Bhaptics.SDK2;

namespace CPRSimulation
{
    /// <summary>
    /// TactGlove触觉反馈控制器
    /// 根据CPR按压的深度和速度提供实时震动反馈
    /// </summary>
    public class GloveHapticController : MonoBehaviour
    {
        [Header("检测器引用")]
        public CPRPressDetector pressDetector;

        [Header("震动强度设置 (1-100)")]
        [Tooltip("深度过浅时的震动强度")]
        [Range(1, 100)] public int shallowIntensity = 30;
        
        [Tooltip("深度适中时的震动强度")]
        [Range(1, 100)] public int goodIntensity = 60;
        
        [Tooltip("深度过深时的震动强度")]
        [Range(1, 100)] public int deepIntensity = 95;

        [Header("节奏反馈设置")]
        [Tooltip("启用速率节奏反馈")]
        public bool enableRateFeedback = true;
        
        [Tooltip("节奏提示震动强度")]
        [Range(1, 100)] public int rhythmIntensity = 50;

        [Header("震动时长 (毫秒)")]
        [Range(10, 100)] public int pulseDurationMs = 30;

        [Header("bHaptics 事件（与 Developer 中事件名一致）")]
        [Tooltip("优先使用事件名；留空则用马达强度")]
        public bool useHapticEvents = true;
        [Tooltip("按压过浅时播放")]
        public string eventShallow = "cpr_shallow";
        [Tooltip("按压深度合适时播放")]
        public string eventGoodDepth = "cpr_good_depth";
        [Tooltip("按压过深时播放")]
        public string eventTooDeep = "cpr_too_deep";

        [Header("反馈时机")]
        [Tooltip("勾选：仅在每次按压完成时播放一次 cpr_* 事件（推荐，与手套三事件一一对应）")]
        public bool feedbackOnCompleteOnly = true;
        [Tooltip("未勾选时：按压过程中按深度实时震动（可能较频繁）")]
        public bool realtimeDepthHapticWhenNotCompleteOnly = false;

        [Header("调试")]
        public bool logHapticEvents = true;

        // bHaptics 震动数组（6个马达：5指尖 + 1手腕）
        private readonly int[] _motors = new int[6];
        private readonly int[] _durations = new int[6];
        private readonly int[] _shapes = new int[6];

        private float _lastRhythmTime;
        private const float GOOD_RHYTHM_INTERVAL = 0.545f; // 110 BPM 的间隔
        private bool _isBound = false;

        private void Start()
        {
            InitializeArrays();
            // CPRPressDetector 是运行时动态创建的，Start 时通常还不存在，
            // 真正的绑定由 BindDetector() 完成（由 CPRTrainingController 在创建检测区后调用）。
            // 此处仅做一次尝试性绑定，以兼容编辑器直接测试的情形。
            if (pressDetector != null)
                BindDetector(pressDetector);
            else
                Debug.Log("[GloveHapticController] 等待 CPRTrainingController 调用 BindDetector()");
        }

        /// <summary>
        /// 绑定/切换 CPRPressDetector，自动取消旧订阅并订阅新事件。
        /// CPRTrainingController 在创建检测区后必须调用此方法。
        /// </summary>
        public void BindDetector(CPRPressDetector detector)
        {
            // 取消旧订阅
            if (pressDetector != null)
            {
                pressDetector.OnDepthChanged -= OnDepthChanged;
                pressDetector.OnCompressionComplete -= OnCompressionComplete;
                pressDetector.OnCompressionStart -= OnCompressionStart;
            }
            pressDetector = detector;
            if (pressDetector != null)
            {
                pressDetector.OnDepthChanged += OnDepthChanged;
                pressDetector.OnCompressionComplete += OnCompressionComplete;
                pressDetector.OnCompressionStart += OnCompressionStart;
                _isBound = true;
                Debug.Log("[GloveHapticController] 已绑定 CPRPressDetector，事件订阅完成");
            }
            else
            {
                _isBound = false;
                Debug.LogWarning("[GloveHapticController] BindDetector 传入了 null");
            }
        }

        /// <summary>
        /// 直接按深度触发触觉（由 CPRTrainingController.OnCompressionDetected 直接调用，不依赖事件订阅）
        /// </summary>
        public void TriggerByDepth(float depthCm)
        {
            if (depthCm <= 0f) depthCm = 3f;
            string category = depthCm < minGoodDepthCm ? "过浅→cpr_shallow" : depthCm > maxGoodDepthCm ? "过深→cpr_too_deep" : "良好→cpr_good_depth";
            Debug.Log($"[GloveHaptic] ★ TriggerByDepth depth={depthCm:F1}cm {category} bHaptics={BhapticsLibrary.IsBhapticsAvailable(false)}");
            if (depthCm < minGoodDepthCm)    PlayShallowFeedback();
            else if (depthCm > maxGoodDepthCm) PlayDeepWarning();
            else                               PlayGoodFeedback();
        }

        private float minGoodDepthCm => pressDetector != null ? pressDetector.minGoodDepthCm : 5f;
        private float maxGoodDepthCm => pressDetector != null ? pressDetector.maxGoodDepthCm : 6f;

        /// <summary>
        /// 停止当前及后续所有震动反馈（用于训练结束或中途退出）
        /// </summary>
        public void StopAllFeedback()
        {
            enableRateFeedback = false;
            _lastRhythmTime = 0f;

            // 取消事件订阅，防止后续再被触发
            if (pressDetector != null)
            {
                pressDetector.OnDepthChanged -= OnDepthChanged;
                pressDetector.OnCompressionComplete -= OnCompressionComplete;
                pressDetector.OnCompressionStart -= OnCompressionStart;
            }
            pressDetector = null;
            _isBound = false;

            try
            {
                if (BhapticsLibrary.IsBhapticsAvailable(false))
                {
                    BhapticsLibrary.StopAll();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[GloveHaptic] StopAllFeedback 调用失败: " + e.Message);
            }

            if (logHapticEvents)
                Debug.Log("[GloveHaptic] StopAllFeedback: 已停止所有触觉反馈");
        }

        /// <summary>
        /// 为新一轮训练重置内部状态（在 StartCPRPhase 时调用）。
        /// </summary>
        public void ResetForNewSession()
        {
            enableRateFeedback = true;
            _lastRhythmTime = 0f;
        }

        private void OnDestroy()
        {
            if (pressDetector != null)
            {
                pressDetector.OnDepthChanged -= OnDepthChanged;
                pressDetector.OnCompressionComplete -= OnCompressionComplete;
                pressDetector.OnCompressionStart -= OnCompressionStart;
            }
        }

        private void InitializeArrays()
        {
            for (int i = 0; i < 6; i++)
            {
                _durations[i] = pulseDurationMs;
                _shapes[i] = 0; // Constant wave shape
            }
        }

        private void Update()
        {
            // 自动重试绑定：如果还没绑定，每秒扫一次场景里的 CPRPressDetector
            if (!_isBound)
            {
                _autoBindTimer += Time.deltaTime;
                if (_autoBindTimer >= 1f)
                {
                    _autoBindTimer = 0f;
                    var det = FindObjectOfType<CPRPressDetector>();
                    if (det != null)
                    {
                        Debug.Log("[GloveHapticController] Update 自动找到并绑定 CPRPressDetector");
                        BindDetector(det);
                    }
                }
            }

            if (!enableRateFeedback || pressDetector == null) return;

            // 如果速率过慢，提供节奏引导
            if (pressDetector.RateStatus == RateQuality.TooSlow && 
                pressDetector.CompressionCount > 2)
            {
                float timeSinceLastRhythm = Time.time - _lastRhythmTime;
                if (timeSinceLastRhythm >= GOOD_RHYTHM_INTERVAL)
                {
                    PlayRhythmGuide();
                    _lastRhythmTime = Time.time;
                }
            }
        }
        private float _autoBindTimer = 1f; // 启动就立刻扫一次

        /// <summary>
        /// 深度变化时的实时反馈（仅在未启用“仅完成时反馈”时生效）
        /// </summary>
        private void OnDepthChanged(float depthCm)
        {
            if (feedbackOnCompleteOnly && !realtimeDepthHapticWhenNotCompleteOnly) return;
            if (depthCm < 1f) return;

            int intensity = CalculateIntensityByDepth(depthCm);
            PlayHapticPulse(intensity);
        }

        /// <summary>
        /// 按压开始时的反馈（仅完成模式时不播放，避免与完成时事件重复）
        /// </summary>
        private void OnCompressionStart()
        {
            if (feedbackOnCompleteOnly) return;
            PlayHapticPulse(20);
        }

        /// <summary>
        /// 按压完成时：按深度质量播放对应 bHaptics 事件（cpr_shallow / cpr_good_depth / cpr_too_deep）
        /// </summary>
        private void OnCompressionComplete(float depthCm, float rateBPM)
        {
            var depthStatus = pressDetector.DepthStatus;
            var rateStatus = pressDetector.RateStatus;

            switch (depthStatus)
            {
                case DepthQuality.TooShallow:
                    PlayShallowFeedback();
                    if (logHapticEvents) Debug.Log($"[GloveHaptic] 按压完成 过浅 → {eventShallow} ({depthCm:F1}cm)");
                    break;
                case DepthQuality.TooDeep:
                    PlayDeepWarning();
                    if (logHapticEvents) Debug.Log($"[GloveHaptic] 按压完成 过深 → {eventTooDeep} ({depthCm:F1}cm)");
                    break;
                default:
                    PlayGoodFeedback();
                    if (logHapticEvents) Debug.Log($"[GloveHaptic] 按压完成 良好 → {eventGoodDepth} ({depthCm:F1}cm, {rateBPM:F0}BPM)");
                    break;
            }

            if (logHapticEvents && (rateStatus == RateQuality.TooSlow || rateStatus == RateQuality.TooFast))
                Debug.Log($"[GloveHaptic] 速率: {(rateStatus == RateQuality.TooSlow ? "偏慢" : "偏快")} {rateBPM:F0}BPM");
        }

        /// <summary>
        /// 根据深度计算震动强度
        /// </summary>
        private int CalculateIntensityByDepth(float depthCm)
        {
            if (depthCm < pressDetector.minGoodDepthCm)
            {
                // 深度不足：强度随深度增加
                float t = depthCm / pressDetector.minGoodDepthCm;
                return Mathf.RoundToInt(Mathf.Lerp(10, shallowIntensity, t));
            }
            else if (depthCm <= pressDetector.maxGoodDepthCm)
            {
                // 标准深度：稳定的良好反馈
                return goodIntensity;
            }
            else
            {
                // 深度过深：强度急剧增加警告
                float overDepth = depthCm - pressDetector.maxGoodDepthCm;
                float t = Mathf.Clamp01(overDepth / 2f); // 超过2cm达到最大强度
                return Mathf.RoundToInt(Mathf.Lerp(goodIntensity, deepIntensity, t));
            }
        }

        /// <summary>
        /// 深度过浅的反馈 - 短促弱震动 或 播放 cpr_shallow 事件
        /// </summary>
        private void PlayShallowFeedback()
        {
            if (useHapticEvents && !string.IsNullOrEmpty(eventShallow))
            {
                try
                {
                    BhapticsLibrary.Play(eventShallow);
                    if (logHapticEvents) Debug.Log("[GloveHaptic] 播放事件: " + eventShallow);
                    return;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("[GloveHaptic] Play 事件失败，降级为 PlayMotors: " + e.Message);
                }
            }
            SetAllMotors(shallowIntensity);
            _durations[0] = 20;
            PlayBothGloves();
        }

        /// <summary>
        /// 深度良好的反馈 - 平稳中等震动 或 播放 cpr_good_depth 事件
        /// </summary>
        private void PlayGoodFeedback()
        {
            if (useHapticEvents && !string.IsNullOrEmpty(eventGoodDepth))
            {
                try
                {
                    BhapticsLibrary.Play(eventGoodDepth);
                    if (logHapticEvents) Debug.Log("[GloveHaptic] 播放事件: " + eventGoodDepth);
                    return;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("[GloveHaptic] Play 事件失败，降级为 PlayMotors: " + e.Message);
                }
            }
            SetAllMotors(goodIntensity);
            _durations[0] = pulseDurationMs;
            PlayBothGloves();
        }

        /// <summary>
        /// 深度过深的警告 - 强烈震动 或 播放 cpr_too_deep 事件
        /// </summary>
        private void PlayDeepWarning()
        {
            if (useHapticEvents && !string.IsNullOrEmpty(eventTooDeep))
            {
                try
                {
                    BhapticsLibrary.Play(eventTooDeep);
                    if (logHapticEvents) Debug.Log("[GloveHaptic] 播放事件: " + eventTooDeep);
                    return;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("[GloveHaptic] Play 事件失败，降级为 PlayMotors: " + e.Message);
                }
            }
            SetAllMotors(deepIntensity);
            _durations[0] = 50;
            PlayBothGloves();
        }

        /// <summary>
        /// 节奏引导震动 - 用于速率过慢时
        /// </summary>
        private void PlayRhythmGuide()
        {
            SetAllMotors(rhythmIntensity);
            _durations[0] = 15;
            PlayBothGloves();

            if (logHapticEvents)
            {
                Debug.Log("[GloveHaptic] 节奏引导震动");
            }
        }

        /// <summary>
        /// 播放指定强度的震动
        /// </summary>
        private void PlayHapticPulse(int intensity)
        {
            SetAllMotors(intensity);
            PlayBothGloves();
        }

        private void SetAllMotors(int intensity)
        {
            intensity = Mathf.Clamp(intensity, 1, 100);
            for (int i = 0; i < 6; i++)
            {
                _motors[i] = intensity;
                _durations[i] = pulseDurationMs;
                _shapes[i] = 0;
            }
        }

        private void PlayBothGloves()
        {
            if (!BhapticsLibrary.IsBhapticsAvailable(false))
            {
                if (logHapticEvents)
                {
                    Debug.LogWarning("[GloveHaptic] bHaptics不可用");
                }
                return;
            }

            BhapticsLibrary.PlayMotors((int)PositionType.GloveL, _motors, pulseDurationMs);
            BhapticsLibrary.PlayMotors((int)PositionType.GloveR, _motors, pulseDurationMs);
        }

        /// <summary>
        /// 手动触发测试震动
        /// </summary>
        [ContextMenu("测试震动 - 弱")]
        public void TestWeak() => PlayHapticPulse(30);

        [ContextMenu("测试震动 - 中")]
        public void TestMedium() => PlayHapticPulse(60);

        [ContextMenu("测试震动 - 强")]
        public void TestStrong() => PlayHapticPulse(95);
    }
}
