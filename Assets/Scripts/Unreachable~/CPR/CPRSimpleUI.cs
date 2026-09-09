using UnityEngine;
using UnityEngine.UI;

namespace CPRSimulation
{
    /// <summary>
    /// 简单的CPR反馈UI - 显示深度条和速率指示
    /// </summary>
    public class CPRSimpleUI : MonoBehaviour
    {
        [Header("检测器引用")]
        public CPRPressDetector pressDetector;

        [Header("UI元素")]
        [Tooltip("深度进度条")]
        public Slider depthSlider;
        
        [Tooltip("深度数值文本")]
        public Text depthText;
        
        [Tooltip("速率数值文本")]
        public Text rateText;
        
        [Tooltip("按压计数文本")]
        public Text countText;
        
        [Tooltip("状态提示文本")]
        public Text statusText;

        [Header("颜色设置")]
        public Color shallowColor = new Color(1f, 0.6f, 0f);  // 橙色
        public Color goodColor = new Color(0.2f, 0.8f, 0.2f);  // 绿色
        public Color deepColor = new Color(1f, 0.2f, 0.2f);    // 红色

        [Header("深度条范围")]
        public float maxDisplayDepthCm = 8f;

        private Image _sliderFill;

        private void Start()
        {
            if (pressDetector == null)
            {
                pressDetector = FindObjectOfType<CPRPressDetector>();
            }

            if (depthSlider != null)
            {
                depthSlider.minValue = 0;
                depthSlider.maxValue = maxDisplayDepthCm;
                _sliderFill = depthSlider.fillRect?.GetComponent<Image>();
            }

            UpdateUI();
        }

        private void Update()
        {
            if (pressDetector == null) return;
            UpdateUI();
        }

        private void UpdateUI()
        {
            float depth = pressDetector?.CurrentDepthCm ?? 0f;
            float rate = pressDetector?.CurrentRateBPM ?? 0f;
            int count = pressDetector?.CompressionCount ?? 0;

            // 更新深度条
            if (depthSlider != null)
            {
                depthSlider.value = depth;
                
                // 更新颜色
                if (_sliderFill != null)
                {
                    _sliderFill.color = GetDepthColor(depth);
                }
            }

            // 更新深度文本
            if (depthText != null)
            {
                depthText.text = $"{depth:F1} cm";
                depthText.color = GetDepthColor(depth);
            }

            // 更新速率文本
            if (rateText != null)
            {
                rateText.text = $"{rate:F0} /min";
                rateText.color = GetRateColor(rate);
            }

            // 更新计数文本
            if (countText != null)
            {
                countText.text = $"按压: {count}";
            }

            // 更新状态提示
            if (statusText != null && pressDetector != null)
            {
                statusText.text = GetStatusMessage();
                statusText.color = GetStatusColor();
            }
        }

        private Color GetDepthColor(float depthCm)
        {
            if (pressDetector == null) return Color.white;

            if (depthCm < pressDetector.minGoodDepthCm)
                return shallowColor;
            if (depthCm > pressDetector.maxGoodDepthCm)
                return deepColor;
            return goodColor;
        }

        private Color GetRateColor(float rateBPM)
        {
            if (pressDetector == null) return Color.white;

            if (rateBPM < pressDetector.minGoodRateBPM && rateBPM > 0)
                return shallowColor;
            if (rateBPM > pressDetector.maxGoodRateBPM)
                return deepColor;
            return goodColor;
        }

        private string GetStatusMessage()
        {
            if (pressDetector == null) return "";

            var depthStatus = pressDetector.DepthStatus;
            var rateStatus = pressDetector.RateStatus;

            string depthMsg = depthStatus switch
            {
                DepthQuality.TooShallow => "按压更深",
                DepthQuality.TooDeep => "力度过大",
                _ => "深度良好"
            };

            string rateMsg = "";
            if (pressDetector.CompressionCount > 2)
            {
                rateMsg = rateStatus switch
                {
                    RateQuality.TooSlow => " | 加快节奏",
                    RateQuality.TooFast => " | 放慢节奏",
                    _ => " | 节奏良好"
                };
            }

            return depthMsg + rateMsg;
        }

        private Color GetStatusColor()
        {
            if (pressDetector == null) return Color.white;

            var depthStatus = pressDetector.DepthStatus;
            if (depthStatus == DepthQuality.Good)
            {
                var rateStatus = pressDetector.RateStatus;
                if (rateStatus == RateQuality.Good || pressDetector.CompressionCount <= 2)
                    return goodColor;
            }

            if (depthStatus == DepthQuality.TooDeep)
                return deepColor;

            return shallowColor;
        }
    }
}
