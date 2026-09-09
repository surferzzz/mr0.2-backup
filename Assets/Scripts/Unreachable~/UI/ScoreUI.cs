using UnityEngine;
using TMPro;
using VRFirstAid.Core;

namespace VRFirstAid.UI
{
    /// <summary>
    /// 分数UI - 显示CPR训练分数
    /// </summary>
    public class ScoreUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject scorePanel;
        [SerializeField] private TextMeshProUGUI overallScoreText;
        [SerializeField] private TextMeshProUGUI depthScoreText;
        [SerializeField] private TextMeshProUGUI rateScoreText;
        [SerializeField] private TextMeshProUGUI positionScoreText;
        [SerializeField] private TextMeshProUGUI breathingScoreText;

        [Header("Progress Bars")]
        [SerializeField] private UnityEngine.UI.Image depthProgressBar;
        [SerializeField] private UnityEngine.UI.Image rateProgressBar;
        [SerializeField] private UnityEngine.UI.Image positionProgressBar;
        [SerializeField] private UnityEngine.UI.Image breathingProgressBar;

        [Header("Colors")]
        [SerializeField] private Color excellentColor = Color.green;
        [SerializeField] private Color goodColor = Color.yellow;
        [SerializeField] private Color poorColor = Color.red;

        private void Start()
        {
            if (scorePanel != null)
            {
                scorePanel.SetActive(false);
            }
        }

        /// <summary>
        /// 显示分数
        /// </summary>
        public void ShowScore(CPRScoreData scoreData)
        {
            if (scorePanel != null)
            {
                scorePanel.SetActive(true);
            }

            UpdateScoreDisplay(scoreData);
        }

        /// <summary>
        /// 隐藏分数面板
        /// </summary>
        public void HideScore()
        {
            if (scorePanel != null)
            {
                scorePanel.SetActive(false);
            }
        }

        /// <summary>
        /// 更新分数显示
        /// </summary>
        private void UpdateScoreDisplay(CPRScoreData scoreData)
        {
            // 更新总分
            if (overallScoreText != null)
            {
                overallScoreText.text = $"总分: {scoreData.overallScore:F1}/100";
                overallScoreText.color = GetScoreColor(scoreData.overallScore);
            }

            // 更新各项分数
            UpdateScoreBar(depthScoreText, depthProgressBar, scoreData.compressionDepthAccuracy, "按压深度");
            UpdateScoreBar(rateScoreText, rateProgressBar, scoreData.compressionRateAccuracy, "按压频率");
            UpdateScoreBar(positionScoreText, positionProgressBar, scoreData.compressionPositionAccuracy, "按压位置");
            UpdateScoreBar(breathingScoreText, breathingProgressBar, scoreData.breathingAccuracy, "人工呼吸");
        }

        /// <summary>
        /// 更新分数条
        /// </summary>
        private void UpdateScoreBar(TextMeshProUGUI text, UnityEngine.UI.Image progressBar, float score, string label)
        {
            if (text != null)
            {
                text.text = $"{label}: {score:F1}/100";
                text.color = GetScoreColor(score);
            }

            if (progressBar != null)
            {
                progressBar.fillAmount = score / 100f;
                progressBar.color = GetScoreColor(score);
            }
        }

        /// <summary>
        /// 根据分数获取颜色
        /// </summary>
        private Color GetScoreColor(float score)
        {
            if (score >= 80f)
            {
                return excellentColor;
            }
            else if (score >= 60f)
            {
                return goodColor;
            }
            else
            {
                return poorColor;
            }
        }

        /// <summary>
        /// 实时更新按压深度分数
        /// </summary>
        public void UpdateDepthScore(float score)
        {
            if (depthScoreText != null)
            {
                depthScoreText.text = $"按压深度: {score:F1}/100";
                depthScoreText.color = GetScoreColor(score);
            }

            if (depthProgressBar != null)
            {
                depthProgressBar.fillAmount = score / 100f;
                depthProgressBar.color = GetScoreColor(score);
            }
        }

        /// <summary>
        /// 实时更新按压频率分数
        /// </summary>
        public void UpdateRateScore(float score)
        {
            if (rateScoreText != null)
            {
                rateScoreText.text = $"按压频率: {score:F1}/100";
                rateScoreText.color = GetScoreColor(score);
            }

            if (rateProgressBar != null)
            {
                rateProgressBar.fillAmount = score / 100f;
                rateProgressBar.color = GetScoreColor(score);
            }
        }
    }
}
