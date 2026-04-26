using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRFirstAid.Core;

namespace VRFirstAid.CPR
{
    /// <summary>
    /// CPR反馈系统 - 提供视觉和音频反馈
    /// </summary>
    public class CPRFeedback : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] public TextMeshProUGUI instructionText;
        [SerializeField] public TextMeshProUGUI compressionCountText;
        [SerializeField] public TextMeshProUGUI breathCountText;
        [SerializeField] public TextMeshProUGUI cycleCountText;
        [SerializeField] private Image depthIndicator;
        [SerializeField] private Image rateIndicator;

        [Header("Visual Feedback")]
        [SerializeField] private Color correctColor = Color.green;
        [SerializeField] private Color incorrectColor = Color.red;
        [SerializeField] private Color neutralColor = Color.yellow;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip compressionSound;
        [SerializeField] private AudioClip breathSound;
        [SerializeField] private AudioClip successSound;

        private void Start()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }
        }

        /// <summary>
        /// 显示指令文本
        /// </summary>
        public void ShowInstruction(string instruction)
        {
            if (instructionText != null)
            {
                instructionText.text = instruction;
            }
            Debug.Log($"[CPR Instruction] {instruction}");
        }

        /// <summary>
        /// 更新按压计数
        /// </summary>
        public void UpdateCompressionCount(int current, int target)
        {
            if (compressionCountText != null)
            {
                compressionCountText.text = $"按压: {current}/{target}";
            }

            // 播放按压音效
            if (compressionSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(compressionSound);
            }
        }

        /// <summary>
        /// 更新呼吸计数
        /// </summary>
        public void UpdateBreathCount(int current, int target)
        {
            if (breathCountText != null)
            {
                breathCountText.text = $"呼吸: {current}/{target}";
            }

            // 播放呼吸音效
            if (breathSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(breathSound);
            }
        }

        /// <summary>
        /// 更新循环计数
        /// </summary>
        public void UpdateCycleCount(int current, int target)
        {
            if (cycleCountText != null)
            {
                cycleCountText.text = $"循环: {current}/{target}";
            }
        }

        /// <summary>
        /// 更新按压深度指示器
        /// </summary>
        public void UpdateDepthIndicator(float depthPercentage, bool isCorrect)
        {
            if (depthIndicator != null)
            {
                depthIndicator.fillAmount = depthPercentage;
                depthIndicator.color = isCorrect ? correctColor : incorrectColor;
            }
        }

        /// <summary>
        /// 更新按压频率指示器
        /// </summary>
        public void UpdateRateIndicator(float ratePercentage, bool isCorrect)
        {
            if (rateIndicator != null)
            {
                rateIndicator.fillAmount = ratePercentage;
                rateIndicator.color = isCorrect ? correctColor : incorrectColor;
            }
        }

        /// <summary>
        /// 显示位置反馈
        /// </summary>
        public void ShowPositionFeedback(bool isCorrect)
        {
            // 可以添加视觉反馈，如高亮显示
            Debug.Log($"位置反馈: {(isCorrect ? "正确" : "错误")}");
        }

        /// <summary>
        /// 显示分数
        /// </summary>
        public void ShowScore(CPRScoreData score)
        {
            string scoreText = $"CPR训练完成！\n" +
                              $"总分: {score.overallScore:F1}/100\n" +
                              $"按压深度: {score.compressionDepthAccuracy:F1}/100\n" +
                              $"按压频率: {score.compressionRateAccuracy:F1}/100\n" +
                              $"按压位置: {score.compressionPositionAccuracy:F1}/100\n" +
                              $"人工呼吸: {score.breathingAccuracy:F1}/100";

            if (instructionText != null)
            {
                instructionText.text = scoreText;
            }

            // 播放成功音效
            if (successSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(successSound);
            }

            Debug.Log(scoreText);
        }

        /// <summary>
        /// 清除所有反馈
        /// </summary>
        public void ClearFeedback()
        {
            if (instructionText != null)
            {
                instructionText.text = "";
            }

            if (compressionCountText != null)
            {
                compressionCountText.text = "";
            }

            if (breathCountText != null)
            {
                breathCountText.text = "";
            }

            if (cycleCountText != null)
            {
                cycleCountText.text = "";
            }
        }
    }
}
