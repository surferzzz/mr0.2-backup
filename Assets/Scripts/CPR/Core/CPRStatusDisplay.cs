using UnityEngine;
using UnityEngine.UI;

namespace CPRSimulation.Core
{
    /// <summary>
    /// CPR状态显示UI
    /// </summary>
    public class CPRStatusDisplay : MonoBehaviour
    {
        [Header("引用")]
        public CPRPressureDetector detector;

        [Header("UI元素")]
        public Text depthText;
        public Text rateText;
        public Text countText;
        public Text statusText;
        public Slider depthSlider;
        public Image statusIndicator;

        [Header("颜色设置")]
        public Color perfectColor = new Color(0.2f, 0.8f, 0.3f);
        public Color goodColor = new Color(0.9f, 0.8f, 0.2f);
        public Color needsWorkColor = new Color(0.9f, 0.5f, 0.2f);
        public Color tooShallowColor = new Color(0.9f, 0.3f, 0.3f);

        private void Start()
        {
            if (detector == null)
            {
                detector = FindObjectOfType<CPRPressureDetector>();
            }

            if (detector != null)
            {
                detector.OnDepthChanged += UpdateDepthDisplay;
                detector.OnRateChanged += UpdateRateDisplay;
                detector.OnCompressionComplete += UpdateStatusDisplay;
            }

            // 初始化显示
            UpdateAllDisplay();
        }

        private void OnDestroy()
        {
            if (detector != null)
            {
                detector.OnDepthChanged -= UpdateDepthDisplay;
                detector.OnRateChanged -= UpdateRateDisplay;
                detector.OnCompressionComplete -= UpdateStatusDisplay;
            }
        }

        private void Update()
        {
            if (detector != null)
            {
                // 更新计数（实时）
                if (countText != null)
                {
                    countText.text = $"按压次数: {detector.CompressionCount}";
                }
            }
        }

        private void UpdateDepthDisplay(float depth)
        {
            float depthCm = depth * 100f;
            
            if (depthText != null)
            {
                depthText.text = $"深度: {depthCm:F1} cm";
                
                // 颜色反馈
                if (depthCm >= 5f && depthCm <= 6f)
                    depthText.color = perfectColor;
                else if (depthCm >= 4f && depthCm <= 7f)
                    depthText.color = goodColor;
                else
                    depthText.color = tooShallowColor;
            }

            if (depthSlider != null)
            {
                depthSlider.value = Mathf.Clamp01(depthCm / 8f);
                
                // 滑块颜色
                var fillImage = depthSlider.fillRect?.GetComponent<Image>();
                if (fillImage != null)
                {
                    if (depthCm >= 5f && depthCm <= 6f)
                        fillImage.color = perfectColor;
                    else if (depthCm >= 4f)
                        fillImage.color = goodColor;
                    else
                        fillImage.color = tooShallowColor;
                }
            }
        }

        private void UpdateRateDisplay(int rate)
        {
            if (rateText != null)
            {
                rateText.text = $"频率: {rate}/分钟";
                
                // 颜色反馈
                if (rate >= 100 && rate <= 120)
                    rateText.color = perfectColor;
                else if (rate >= 80 && rate <= 140)
                    rateText.color = goodColor;
                else
                    rateText.color = needsWorkColor;
            }
        }

        private void UpdateStatusDisplay(CompressionQuality quality)
        {
            if (statusText != null)
            {
                switch (quality)
                {
                    case CompressionQuality.Perfect:
                        statusText.text = "★ 完美! ★";
                        statusText.color = perfectColor;
                        break;
                    case CompressionQuality.Good:
                        statusText.text = "良好";
                        statusText.color = goodColor;
                        break;
                    case CompressionQuality.NeedsImprovement:
                        statusText.text = "继续加油";
                        statusText.color = needsWorkColor;
                        break;
                    case CompressionQuality.TooShallow:
                        statusText.text = "按压更深!";
                        statusText.color = tooShallowColor;
                        break;
                }
            }

            if (statusIndicator != null)
            {
                switch (quality)
                {
                    case CompressionQuality.Perfect:
                        statusIndicator.color = perfectColor;
                        break;
                    case CompressionQuality.Good:
                        statusIndicator.color = goodColor;
                        break;
                    case CompressionQuality.NeedsImprovement:
                        statusIndicator.color = needsWorkColor;
                        break;
                    case CompressionQuality.TooShallow:
                        statusIndicator.color = tooShallowColor;
                        break;
                }
            }
        }

        private void UpdateAllDisplay()
        {
            if (detector == null) return;
            
            UpdateDepthDisplay(detector.CurrentDepth);
            UpdateRateDisplay(detector.CurrentRate);
        }
    }
}
