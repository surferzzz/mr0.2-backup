using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace CPRSimulation.Game
{
    /// <summary>
    /// 游戏流程管理器 - 控制整个游戏的状态和场景切换
    /// </summary>
    public class GameFlowManager : MonoBehaviour
    {
        public static GameFlowManager Instance { get; private set; }

        [Header("场景名称")]
        public string mainMenuScene = "MainMenu";
        public string trainingScene = "CPRTraining";

        [Header("当前状态")]
        public GameState currentState = GameState.MainMenu;

        public enum GameState
        {
            MainMenu,       // 主菜单
            Tutorial,       // 教程说明
            Training,       // 训练中
            Evaluation,     // 评估结果
            Completed       // 完成
        }

        // 事件
        public static event System.Action<GameState> OnGameStateChanged;
        public static event System.Action OnTrainingStarted;
        public static event System.Action OnTrainingCompleted;

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
        /// 从主菜单开始教学
        /// </summary>
        public void StartTutorial()
        {
            ChangeState(GameState.Tutorial);
        }

        /// <summary>
        /// 进入训练场景
        /// </summary>
        public void EnterTrainingScene()
        {
            StartCoroutine(LoadSceneAsync(trainingScene, () => {
                ChangeState(GameState.Training);
            }));
        }

        /// <summary>
        /// 开始CPR训练（触发人物倒地等）
        /// </summary>
        public void StartCPRTraining()
        {
            OnTrainingStarted?.Invoke();
            Debug.Log("[GameFlow] CPR训练开始");
        }

        /// <summary>
        /// 完成训练
        /// </summary>
        public void CompleteTraining()
        {
            ChangeState(GameState.Evaluation);
            OnTrainingCompleted?.Invoke();
        }

        /// <summary>
        /// 返回主菜单
        /// </summary>
        public void ReturnToMainMenu()
        {
            StartCoroutine(LoadSceneAsync(mainMenuScene, () => {
                ChangeState(GameState.MainMenu);
            }));
        }

        /// <summary>
        /// 重新开始训练
        /// </summary>
        public void RestartTraining()
        {
            StartCoroutine(LoadSceneAsync(trainingScene, () => {
                ChangeState(GameState.Training);
            }));
        }

        private void ChangeState(GameState newState)
        {
            currentState = newState;
            OnGameStateChanged?.Invoke(newState);
            Debug.Log($"[GameFlow] 状态切换: {newState}");
        }

        private IEnumerator LoadSceneAsync(string sceneName, System.Action onComplete = null)
        {
            var operation = SceneManager.LoadSceneAsync(sceneName);
            while (!operation.isDone)
            {
                yield return null;
            }
            onComplete?.Invoke();
        }
    }
}
