using System.Collections;
using UnityEngine;
using VRFirstAid.Core;

namespace VRFirstAid.CPR
{
    /// <summary>
    /// CPR状态枚举
    /// </summary>
    public enum CPRState
    {
        Idle,
        CheckConsciousness,
        CallEmergency,
        Compressions,
        Breaths,
        CheckCycle,
        Completed
    }

    /// <summary>
    /// CPR管理器 - 管理CPR训练的完整流程
    /// </summary>
    public class CPRManager : MonoBehaviour
    {
        [Header("State Management")]
        [SerializeField] private CPRState currentState = CPRState.Idle;

        [Header("CPR Parameters")]
        [SerializeField] private int targetCompressions = 30;
        [SerializeField] private int targetBreaths = 2;
        [SerializeField] private int targetCycles = 5;
        [SerializeField] private float consciousnessCheckDuration = 10f;

        [Header("Components")]
        [SerializeField] public CompressionDetector compressionDetector;
        [SerializeField] public BreathingDetector breathingDetector;
        [SerializeField] public CPRFeedback feedback;

        [Header("Current Progress")]
        [SerializeField] private int currentCompressionCount = 0;
        [SerializeField] private int currentBreathCount = 0;
        [SerializeField] private int currentCycleCount = 0;

        // 事件
        public System.Action<CPRState> OnStateChanged;
        public System.Action<int> OnCompressionCountChanged;
        public System.Action<int> OnBreathCountChanged;
        public System.Action<int> OnCycleCountChanged;
        public System.Action OnCPRCompleted;

        public CPRState CurrentState => currentState;
        public int CurrentCompressionCount => currentCompressionCount;
        public int CurrentBreathCount => currentBreathCount;
        public int CurrentCycleCount => currentCycleCount;

        private void Start()
        {
            InitializeCPR();
        }

        private void InitializeCPR()
        {
            if (compressionDetector != null)
            {
                compressionDetector.OnCompressionDetected += HandleCompressionDetected;
            }

            if (breathingDetector != null)
            {
                breathingDetector.OnBreathDetected += HandleBreathDetected;
            }

            ChangeState(CPRState.CheckConsciousness);
        }

        /// <summary>
        /// 改变CPR状态
        /// </summary>
        public void ChangeState(CPRState newState)
        {
            if (currentState == newState) return;

            currentState = newState;
            OnStateChanged?.Invoke(currentState);

            // 根据状态执行相应逻辑
            switch (currentState)
            {
                case CPRState.CheckConsciousness:
                    StartCoroutine(CheckConsciousnessCoroutine());
                    break;
                case CPRState.CallEmergency:
                    CallEmergency();
                    break;
                case CPRState.Compressions:
                    StartCompressions();
                    break;
                case CPRState.Breaths:
                    StartBreaths();
                    break;
                case CPRState.CheckCycle:
                    CheckCycle();
                    break;
                case CPRState.Completed:
                    CompleteCPR();
                    break;
            }
        }

        private IEnumerator CheckConsciousnessCoroutine()
        {
            if (feedback != null)
            {
                feedback.ShowInstruction("检查患者意识和呼吸（10秒）");
            }

            yield return new WaitForSeconds(consciousnessCheckDuration);

            ChangeState(CPRState.CallEmergency);
        }

        private void CallEmergency()
        {
            if (feedback != null)
            {
                feedback.ShowInstruction("呼叫急救（120）");
            }

            // 模拟呼叫急救，等待一段时间后进入按压阶段
            StartCoroutine(DelayedStateChange(CPRState.Compressions, 2f));
        }

        private void StartCompressions()
        {
            currentCompressionCount = 0;
            if (feedback != null)
            {
                feedback.ShowInstruction($"开始胸外按压：目标 {targetCompressions} 次");
            }

            if (compressionDetector != null)
            {
                compressionDetector.StartDetection();
            }
        }

        private void StartBreaths()
        {
            currentBreathCount = 0;
            if (feedback != null)
            {
                feedback.ShowInstruction($"人工呼吸：目标 {targetBreaths} 次");
            }

            if (breathingDetector != null)
            {
                breathingDetector.StartDetection();
            }
        }

        private void HandleCompressionDetected()
        {
            currentCompressionCount++;
            OnCompressionCountChanged?.Invoke(currentCompressionCount);

            if (feedback != null)
            {
                feedback.UpdateCompressionCount(currentCompressionCount, targetCompressions);
            }

            if (currentCompressionCount >= targetCompressions)
            {
                if (compressionDetector != null)
                {
                    compressionDetector.StopDetection();
                }
                ChangeState(CPRState.Breaths);
            }
        }

        private void HandleBreathDetected()
        {
            currentBreathCount++;
            OnBreathCountChanged?.Invoke(currentBreathCount);

            if (feedback != null)
            {
                feedback.UpdateBreathCount(currentBreathCount, targetBreaths);
            }

            if (currentBreathCount >= targetBreaths)
            {
                if (breathingDetector != null)
                {
                    breathingDetector.StopDetection();
                }
                ChangeState(CPRState.CheckCycle);
            }
        }

        private void CheckCycle()
        {
            currentCycleCount++;
            OnCycleCountChanged?.Invoke(currentCycleCount);

            if (currentCycleCount >= targetCycles)
            {
                ChangeState(CPRState.Completed);
            }
            else
            {
                // 重置计数，继续下一轮
                currentCompressionCount = 0;
                currentBreathCount = 0;
                ChangeState(CPRState.Compressions);
            }
        }

        private void CompleteCPR()
        {
            if (feedback != null)
            {
                feedback.ShowInstruction("CPR训练完成！");
            }

            // 计算并显示分数
            if (ScoreSystem.Instance != null && compressionDetector != null && breathingDetector != null)
            {
                var score = ScoreSystem.Instance.CalculateOverallScore(
                    compressionDetector.GetAverageDepth(),
                    compressionDetector.GetAverageRate(),
                    compressionDetector.IsCorrectPosition(),
                    breathingDetector.IsCorrectBreathing()
                );

                if (feedback != null)
                {
                    feedback.ShowScore(score);
                }
            }

            OnCPRCompleted?.Invoke();
        }

        private IEnumerator DelayedStateChange(CPRState newState, float delay)
        {
            yield return new WaitForSeconds(delay);
            ChangeState(newState);
        }

        /// <summary>
        /// 重置CPR训练
        /// </summary>
        public void ResetCPR()
        {
            currentCompressionCount = 0;
            currentBreathCount = 0;
            currentCycleCount = 0;

            if (compressionDetector != null)
            {
                compressionDetector.StopDetection();
            }

            if (breathingDetector != null)
            {
                breathingDetector.StopDetection();
            }

            ChangeState(CPRState.CheckConsciousness);
        }

        private void OnDestroy()
        {
            if (compressionDetector != null)
            {
                compressionDetector.OnCompressionDetected -= HandleCompressionDetected;
            }

            if (breathingDetector != null)
            {
                breathingDetector.OnBreathDetected -= HandleBreathDetected;
            }
        }
    }
}
