using UnityEngine;
#if BHAPTICS_SDK2
using Bhaptics.SDK2;
#endif

namespace CPRSimulation.Core
{
    /// <summary>
    /// bHaptics手套震动反馈控制器
    /// </summary>
    public class GloveHapticFeedback : MonoBehaviour
    {
        [Header("引用")]
        public CPRPressureDetector pressureDetector;

        [Header("震动强度设置 (0-100)")]
        [Range(0, 100)]
        public int lightFeedbackIntensity = 30;
        [Range(0, 100)]
        public int mediumFeedbackIntensity = 60;
        [Range(0, 100)]
        public int strongFeedbackIntensity = 100;

        [Header("震动持续时间 (毫秒)")]
        public int feedbackDurationMs = 100;

        [Header("节拍器设置")]
        public bool enableRhythmGuide = true;
        [Range(0, 100)]
        public int rhythmIntensity = 40;
        public float rhythmBPM = 110f; // 标准CPR频率

        [Header("调试")]
        public bool showDebugLog = true;
        public bool testModeWithoutDevice = false;

        private float lastRhythmTime = 0f;
        private float rhythmInterval => 60f / rhythmBPM;
        private bool isRhythmActive = false;

        private void Start()
        {
            // 初始化bHaptics
#if BHAPTICS_SDK2
            if (!testModeWithoutDevice)
            {
                try
                {
                    BhapticsLibrary.Initialize();
                    if (showDebugLog)
                        Debug.Log("[Haptic] bHaptics SDK 已初始化");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[Haptic] bHaptics初始化失败: {e.Message}");
                }
            }
#endif

            // 自动查找检测器
            if (pressureDetector == null)
            {
                pressureDetector = FindObjectOfType<CPRPressureDetector>();
            }

            // 订阅事件
            if (pressureDetector != null)
            {
                pressureDetector.OnDepthChanged += OnDepthChanged;
                pressureDetector.OnCompressionStart += OnCompressionStart;
                pressureDetector.OnCompressionComplete += OnCompressionComplete;
                
                if (showDebugLog)
                    Debug.Log("[Haptic] 已连接到按压检测器");
            }
            else
            {
                Debug.LogWarning("[Haptic] 未找到 CPRPressureDetector!");
            }
        }

        private void Update()
        {
            // 节拍器功能
            if (enableRhythmGuide && isRhythmActive)
            {
                if (Time.time - lastRhythmTime >= rhythmInterval)
                {
                    PlayRhythmBeat();
                    lastRhythmTime = Time.time;
                }
            }
        }

        private void OnDestroy()
        {
            // 取消订阅
            if (pressureDetector != null)
            {
                pressureDetector.OnDepthChanged -= OnDepthChanged;
                pressureDetector.OnCompressionStart -= OnCompressionStart;
                pressureDetector.OnCompressionComplete -= OnCompressionComplete;
            }
        }

        /// <summary>
        /// 深度变化时的反馈
        /// </summary>
        private void OnDepthChanged(float depth)
        {
            // 根据深度计算震动强度
            int intensity;
            
            if (depth < 0.03f)
            {
                intensity = lightFeedbackIntensity;
            }
            else if (depth < 0.05f)
            {
                intensity = mediumFeedbackIntensity;
            }
            else
            {
                intensity = strongFeedbackIntensity;
            }

            // 实时震动反馈
            PlayGloveVibration(intensity, 50); // 短促震动
        }

        /// <summary>
        /// 按压开始
        /// </summary>
        private void OnCompressionStart()
        {
            if (showDebugLog)
                Debug.Log("[Haptic] 按压开始 - 轻微震动");
            
            PlayGloveVibration(lightFeedbackIntensity, feedbackDurationMs);
        }

        /// <summary>
        /// 按压完成时的反馈
        /// </summary>
        private void OnCompressionComplete(CompressionQuality quality)
        {
            switch (quality)
            {
                case CompressionQuality.Perfect:
                    // 完美按压 - 双次短促强震动
                    PlaySuccessFeedback();
                    if (showDebugLog)
                        Debug.Log("[Haptic] 完美按压! 强反馈");
                    break;

                case CompressionQuality.Good:
                    // 良好按压 - 中等强度单次震动
                    PlayGloveVibration(mediumFeedbackIntensity, feedbackDurationMs);
                    if (showDebugLog)
                        Debug.Log("[Haptic] 良好按压");
                    break;

                case CompressionQuality.NeedsImprovement:
                    // 需要改进 - 轻微震动
                    PlayGloveVibration(lightFeedbackIntensity, feedbackDurationMs);
                    if (showDebugLog)
                        Debug.Log("[Haptic] 需要改进");
                    break;

                case CompressionQuality.TooShallow:
                    // 太浅 - 警告模式（快速多次弱震动）
                    PlayWarningFeedback();
                    if (showDebugLog)
                        Debug.Log("[Haptic] 按压太浅!");
                    break;
            }
        }

        /// <summary>
        /// 播放手套震动
        /// </summary>
        public void PlayGloveVibration(int intensity, int durationMs)
        {
            if (testModeWithoutDevice)
            {
                if (showDebugLog)
                    Debug.Log($"[Haptic-Test] 震动: 强度={intensity}, 持续={durationMs}ms");
                return;
            }

#if BHAPTICS_SDK2
            try
            {
                // 创建震动模式 - 所有6个马达
                int[] motorIntensities = new int[6];
                for (int i = 0; i < 6; i++)
                {
                    motorIntensities[i] = intensity;
                }

                // 双手震动
                BhapticsLibrary.PlayGlove(
                    GloveType.Left,
                    motorIntensities,
                    durationMs);

                BhapticsLibrary.PlayGlove(
                    GloveType.Right,
                    motorIntensities,
                    durationMs);
            }
            catch (System.Exception e)
            {
                if (showDebugLog)
                    Debug.LogWarning($"[Haptic] 震动播放失败: {e.Message}");
            }
#else
            if (showDebugLog)
                Debug.Log($"[Haptic-Sim] 模拟震动: 强度={intensity}, 持续={durationMs}ms");
#endif
        }

        /// <summary>
        /// 成功反馈 - 双次短促强震动
        /// </summary>
        private void PlaySuccessFeedback()
        {
            PlayGloveVibration(strongFeedbackIntensity, 80);
            Invoke(nameof(PlaySuccessFeedbackSecond), 0.15f);
        }

        private void PlaySuccessFeedbackSecond()
        {
            PlayGloveVibration(strongFeedbackIntensity, 80);
        }

        /// <summary>
        /// 警告反馈 - 快速多次弱震动
        /// </summary>
        private void PlayWarningFeedback()
        {
            StartCoroutine(WarningFeedbackRoutine());
        }

        private System.Collections.IEnumerator WarningFeedbackRoutine()
        {
            for (int i = 0; i < 3; i++)
            {
                PlayGloveVibration(lightFeedbackIntensity, 50);
                yield return new WaitForSeconds(0.1f);
            }
        }

        /// <summary>
        /// 播放节拍
        /// </summary>
        private void PlayRhythmBeat()
        {
            if (testModeWithoutDevice)
            {
                if (showDebugLog)
                    Debug.Log("[Haptic-Test] 节拍震动");
                return;
            }

#if BHAPTICS_SDK2
            try
            {
                // 节拍使用手指尖震动
                int[] fingerTipPattern = new int[] { rhythmIntensity, 0, 0, 0, 0, 0 };
                
                BhapticsLibrary.PlayGlove(GloveType.Left, fingerTipPattern, 50);
                BhapticsLibrary.PlayGlove(GloveType.Right, fingerTipPattern, 50);
            }
            catch { }
#else
            if (showDebugLog)
                Debug.Log("[Haptic-Sim] 节拍震动");
#endif
        }

        /// <summary>
        /// 开始节拍器
        /// </summary>
        public void StartRhythmGuide()
        {
            isRhythmActive = true;
            lastRhythmTime = Time.time;
            if (showDebugLog)
                Debug.Log($"[Haptic] 节拍器启动 - {rhythmBPM} BPM");
        }

        /// <summary>
        /// 停止节拍器
        /// </summary>
        public void StopRhythmGuide()
        {
            isRhythmActive = false;
            if (showDebugLog)
                Debug.Log("[Haptic] 节拍器停止");
        }

        /// <summary>
        /// 测试震动
        /// </summary>
        [ContextMenu("测试震动")]
        public void TestVibration()
        {
            PlayGloveVibration(strongFeedbackIntensity, 200);
        }
    }
}
