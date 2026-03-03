using UnityEngine;
#if BHAPTICS_SDK2
using Bhaptics.SDK2;
#endif

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

        [Header("调试")]
        public bool logHapticEvents = true;

        // bHaptics 震动数组（6个马达：5指尖 + 1手腕）
        private readonly int[] _motors = new int[6];
        private readonly int[] _durations = new int[6];
        private readonly int[] _shapes = new int[6];

        private float _lastRhythmTime;
        private const float GOOD_RHYTHM_INTERVAL = 0.545f; // 110 BPM 的间隔

        private void Start()
        {
            if (pressDetector == null)
            {
                pressDetector = FindObjectOfType<CPRPressDetector>();
            }

            if (pressDetector != null)
            {
                pressDetector.OnDepthChanged += OnDepthChanged;
                pressDetector.OnCompressionComplete += OnCompressionComplete;
                pressDetector.OnCompressionStart += OnCompressionStart;
            }
            else
            {
                Debug.LogWarning("[GloveHapticController] 未找到CPRPressDetector");
            }

            InitializeArrays();
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

        /// <summary>
        /// 深度变化时的实时反馈
        /// </summary>
        private void OnDepthChanged(float depthCm)
        {
            if (depthCm < 1f) return; // 忽略微小深度

            int intensity = CalculateIntensityByDepth(depthCm);
            PlayHapticPulse(intensity);
        }

        /// <summary>
        /// 按压开始时的反馈
        /// </summary>
        private void OnCompressionStart()
        {
            // 开始按压时轻微震动提示
            PlayHapticPulse(20);
        }

        /// <summary>
        /// 按压完成时的反馈
        /// </summary>
        private void OnCompressionComplete(float depthCm, float rateBPM)
        {
            var depthStatus = pressDetector.DepthStatus;
            var rateStatus = pressDetector.RateStatus;

            // 根据深度质量给出反馈
            int intensity;
            string feedback;

            switch (depthStatus)
            {
                case DepthQuality.TooShallow:
                    intensity = shallowIntensity;
                    feedback = "太浅";
                    PlayShallowFeedback();
                    break;
                case DepthQuality.TooDeep:
                    intensity = deepIntensity;
                    feedback = "太深";
                    PlayDeepWarning();
                    break;
                default:
                    intensity = goodIntensity;
                    feedback = "良好";
                    PlayGoodFeedback();
                    break;
            }

            // 速率反馈
            string rateFeedback = rateStatus switch
            {
                RateQuality.TooSlow => "太慢",
                RateQuality.TooFast => "太快",
                _ => "节奏好"
            };

            if (logHapticEvents)
            {
                Debug.Log($"[GloveHaptic] 深度:{feedback}({depthCm:F1}cm) 速率:{rateFeedback}({rateBPM:F0}BPM) 强度:{intensity}");
            }
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
        /// 深度过浅的反馈 - 短促弱震动
        /// </summary>
        private void PlayShallowFeedback()
        {
            SetAllMotors(shallowIntensity);
            _durations[0] = 20;
            PlayBothGloves();
        }

        /// <summary>
        /// 深度良好的反馈 - 平稳中等震动
        /// </summary>
        private void PlayGoodFeedback()
        {
            SetAllMotors(goodIntensity);
            _durations[0] = pulseDurationMs;
            PlayBothGloves();
        }

        /// <summary>
        /// 深度过深的警告 - 强烈震动
        /// </summary>
        private void PlayDeepWarning()
        {
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
#if BHAPTICS_SDK2
            if (!BhapticsLibrary.IsBhapticsAvailable(false))
            {
                if (logHapticEvents)
                {
                    Debug.LogWarning("[GloveHaptic] bHaptics不可用");
                }
                return;
            }

            BhapticsLibrary.PlayGlove(PositionType.GloveL, _motors, _durations, _shapes);
            BhapticsLibrary.PlayGlove(PositionType.GloveR, _motors, _durations, _shapes);
#else
            if (logHapticEvents)
            {
                Debug.Log($"[GloveHaptic] 模拟震动 - 强度: {_motors[0]}");
            }
#endif
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
