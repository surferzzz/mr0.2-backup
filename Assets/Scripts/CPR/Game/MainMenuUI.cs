using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace CPRSimulation.Game
{
    /// <summary>
    /// 主菜单UI控制器
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("UI面板")]
        public GameObject mainMenuPanel;
        public GameObject tutorialPanel;
        public GameObject settingsPanel;

        [Header("主菜单按钮")]
        public Button startButton;
        public Button tutorialButton;
        public Button settingsButton;
        public Button quitButton;

        [Header("教程面板按钮")]
        public Button beginTrainingButton;
        public Button backFromTutorialButton;

        [Header("场景设置")]
        public string trainingSceneName = "CPRSimulation";

        [Header("VR设置")]
        public bool isVRMode = true;

        private void Start()
        {
            SetupButtons();
            ShowMainMenu();
        }

        private void SetupButtons()
        {
            // 主菜单按钮
            if (startButton != null)
                startButton.onClick.AddListener(OnStartClicked);
            
            if (tutorialButton != null)
                tutorialButton.onClick.AddListener(OnTutorialClicked);
            
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsClicked);
            
            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);

            // 教程面板按钮
            if (beginTrainingButton != null)
                beginTrainingButton.onClick.AddListener(OnBeginTrainingClicked);
            
            if (backFromTutorialButton != null)
                backFromTutorialButton.onClick.AddListener(ShowMainMenu);
        }

        public void ShowMainMenu()
        {
            SetPanelActive(mainMenuPanel, true);
            SetPanelActive(tutorialPanel, false);
            SetPanelActive(settingsPanel, false);
        }

        private void OnStartClicked()
        {
            // 直接开始训练
            LoadTrainingScene();
        }

        private void OnTutorialClicked()
        {
            // 显示教程说明
            SetPanelActive(mainMenuPanel, false);
            SetPanelActive(tutorialPanel, true);
        }

        private void OnSettingsClicked()
        {
            SetPanelActive(mainMenuPanel, false);
            SetPanelActive(settingsPanel, true);
        }

        private void OnQuitClicked()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        private void OnBeginTrainingClicked()
        {
            LoadTrainingScene();
        }

        private void LoadTrainingScene()
        {
            Debug.Log($"[MainMenuUI] 加载训练场景: {trainingSceneName}");
            SceneManager.LoadScene(trainingSceneName);
        }

        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
                panel.SetActive(active);
        }
    }
}
