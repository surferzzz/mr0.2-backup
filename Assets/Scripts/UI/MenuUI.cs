using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRFirstAid.Core;

namespace VRFirstAid.UI
{
    /// <summary>
    /// 主菜单UI - 管理主菜单界面
    /// </summary>
    public class MenuUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button cprTrainingButton;
        [SerializeField] private Button equipmentDemoButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("Settings Panel")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Toggle handTrackingToggle;
        [SerializeField] private Button closeSettingsButton;

        [Header("Score Display")]
        [SerializeField] private TextMeshProUGUI averageScoreText;
        [SerializeField] private Button viewHistoryButton;

        private void Start()
        {
            InitializeUI();
            UpdateScoreDisplay();
        }

        private void InitializeUI()
        {
            // 绑定按钮事件
            if (cprTrainingButton != null)
            {
                cprTrainingButton.onClick.AddListener(OnCPRTrainingClicked);
            }

            if (equipmentDemoButton != null)
            {
                equipmentDemoButton.onClick.AddListener(OnEquipmentDemoClicked);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(OnSettingsClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(OnQuitClicked);
            }

            if (closeSettingsButton != null)
            {
                closeSettingsButton.onClick.AddListener(OnCloseSettingsClicked);
            }

            if (viewHistoryButton != null)
            {
                viewHistoryButton.onClick.AddListener(OnViewHistoryClicked);
            }

            // 设置面板初始状态
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }

            // 初始化设置
            if (volumeSlider != null)
            {
                volumeSlider.value = AudioListener.volume;
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            }
        }

        private void OnCPRTrainingClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadCPRTraining();
            }
            else
            {
                Debug.LogWarning("GameManager instance not found!");
            }
        }

        private void OnEquipmentDemoClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadEquipmentDemo();
            }
            else
            {
                Debug.LogWarning("GameManager instance not found!");
            }
        }

        private void OnSettingsClicked()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
            }
        }

        private void OnCloseSettingsClicked()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }

        private void OnQuitClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.QuitGame();
            }
        }

        private void OnVolumeChanged(float value)
        {
            AudioListener.volume = value;
        }

        private void OnViewHistoryClicked()
        {
            // 显示历史记录
            ShowHistory();
        }

        private void UpdateScoreDisplay()
        {
            if (ScoreSystem.Instance != null && averageScoreText != null)
            {
                float averageScore = ScoreSystem.Instance.GetAverageScore();
                averageScoreText.text = $"平均分: {averageScore:F1}/100";
            }
        }

        private void ShowHistory()
        {
            if (ScoreSystem.Instance != null)
            {
                var history = ScoreSystem.Instance.GetScoreHistory();
                string historyText = "历史记录:\n";

                foreach (var score in history)
                {
                    historyText += $"{score.timestamp:HH:mm:ss} - {score.overallScore:F1}分\n";
                }

                Debug.Log(historyText);
                // 可以在这里显示历史记录UI
            }
        }
    }
}
