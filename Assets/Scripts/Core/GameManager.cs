using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRFirstAid.Core
{
    /// <summary>
    /// 游戏管理器 - 管理游戏整体状态和场景切换
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Scene Names")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private string cprTrainingSceneName = "CPRTraining";
        [SerializeField] private string equipmentDemoSceneName = "EquipmentDemo";

        [Header("Game State")]
        [SerializeField] private bool isPaused = false;

        private void Awake()
        {
            // 单例模式
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

        private void Start()
        {
            // 初始化游戏
            InitializeGame();
        }

        private void InitializeGame()
        {
            // 游戏初始化逻辑
            Debug.Log("Game Manager Initialized");
        }

        /// <summary>
        /// 加载主菜单场景
        /// </summary>
        public void LoadMainMenu()
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }

        /// <summary>
        /// 加载CPR训练场景
        /// </summary>
        public void LoadCPRTraining()
        {
            SceneManager.LoadScene(cprTrainingSceneName);
        }

        /// <summary>
        /// 加载设备演示场景
        /// </summary>
        public void LoadEquipmentDemo()
        {
            SceneManager.LoadScene(equipmentDemoSceneName);
        }

        /// <summary>
        /// 暂停/继续游戏
        /// </summary>
        public void TogglePause()
        {
            isPaused = !isPaused;
            Time.timeScale = isPaused ? 0f : 1f;
        }

        /// <summary>
        /// 退出游戏
        /// </summary>
        public void QuitGame()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
