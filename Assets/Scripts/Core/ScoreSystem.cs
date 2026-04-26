using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRFirstAid.Core
{
    /// <summary>
    /// CPR评分数据
    /// </summary>
    [Serializable]
    public class CPRScoreData
    {
        public float compressionDepthAccuracy;      // 按压深度准确性 (0-100)
        public float compressionRateAccuracy;       // 按压频率准确性 (0-100)
        public float compressionPositionAccuracy;   // 按压位置准确性 (0-100)
        public float breathingAccuracy;              // 人工呼吸准确性 (0-100)
        public float overallScore;                  // 总分 (0-100)
        public DateTime timestamp;                  // 时间戳

        public CPRScoreData()
        {
            timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// 评分系统 - 评估CPR训练表现
    /// </summary>
    public class ScoreSystem : MonoBehaviour
    {
        public static ScoreSystem Instance { get; private set; }

        [Header("Scoring Weights")]
        [SerializeField] private float compressionDepthWeight = 0.3f;
        [SerializeField] private float compressionRateWeight = 0.3f;
        [SerializeField] private float compressionPositionWeight = 0.2f;
        [SerializeField] private float breathingWeight = 0.2f;

        [Header("Target Values")]
        [SerializeField] private float targetCompressionDepth = 5.5f; // cm
        [SerializeField] private float targetCompressionDepthTolerance = 1f; // ±1cm
        [SerializeField] private float targetCompressionRate = 110f; // 次/分钟
        [SerializeField] private float targetCompressionRateTolerance = 10f; // ±10次/分钟

        private List<CPRScoreData> scoreHistory = new List<CPRScoreData>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 计算按压深度得分
        /// </summary>
        public float CalculateCompressionDepthScore(float actualDepth)
        {
            float difference = Mathf.Abs(actualDepth - targetCompressionDepth);
            if (difference <= targetCompressionDepthTolerance)
            {
                // 在容差范围内，线性计算得分
                return 100f * (1f - (difference / targetCompressionDepthTolerance));
            }
            return 0f;
        }

        /// <summary>
        /// 计算按压频率得分
        /// </summary>
        public float CalculateCompressionRateScore(float actualRate)
        {
            float difference = Mathf.Abs(actualRate - targetCompressionRate);
            if (difference <= targetCompressionRateTolerance)
            {
                return 100f * (1f - (difference / targetCompressionRateTolerance));
            }
            return 0f;
        }

        /// <summary>
        /// 计算按压位置得分
        /// </summary>
        public float CalculateCompressionPositionScore(bool isCorrectPosition)
        {
            return isCorrectPosition ? 100f : 0f;
        }

        /// <summary>
        /// 计算人工呼吸得分
        /// </summary>
        public float CalculateBreathingScore(bool isCorrect)
        {
            return isCorrect ? 100f : 0f;
        }

        /// <summary>
        /// 计算总分
        /// </summary>
        public CPRScoreData CalculateOverallScore(
            float compressionDepth,
            float compressionRate,
            bool compressionPosition,
            bool breathing)
        {
            CPRScoreData score = new CPRScoreData();

            score.compressionDepthAccuracy = CalculateCompressionDepthScore(compressionDepth);
            score.compressionRateAccuracy = CalculateCompressionRateScore(compressionRate);
            score.compressionPositionAccuracy = CalculateCompressionPositionScore(compressionPosition);
            score.breathingAccuracy = CalculateBreathingScore(breathing);

            // 加权平均计算总分
            score.overallScore =
                score.compressionDepthAccuracy * compressionDepthWeight +
                score.compressionRateAccuracy * compressionRateWeight +
                score.compressionPositionAccuracy * compressionPositionWeight +
                score.breathingAccuracy * breathingWeight;

            // 保存到历史记录
            scoreHistory.Add(score);

            return score;
        }

        /// <summary>
        /// 获取历史记录
        /// </summary>
        public List<CPRScoreData> GetScoreHistory()
        {
            return new List<CPRScoreData>(scoreHistory);
        }

        /// <summary>
        /// 清除历史记录
        /// </summary>
        public void ClearHistory()
        {
            scoreHistory.Clear();
        }

        /// <summary>
        /// 获取平均分
        /// </summary>
        public float GetAverageScore()
        {
            if (scoreHistory.Count == 0) return 0f;

            float sum = 0f;
            foreach (var score in scoreHistory)
            {
                sum += score.overallScore;
            }
            return sum / scoreHistory.Count;
        }
    }
}
