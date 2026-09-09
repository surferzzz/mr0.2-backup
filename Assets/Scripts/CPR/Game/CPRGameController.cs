using UnityEngine;
using UnityEngine.UI;

namespace CPRSimulation.Game
{
    /// <summary>
    /// CPR游戏流程控制器
    /// 控制：开始训练 -> 患者倒地 -> CPR训练 -> 结束
    /// </summary>
    public class CPRGameController : MonoBehaviour
    {
        [Header("患者模型")]
        public GameObject patientModel;
        public Core.PatientFallController fallController;

        [Header("UI引用")]
        public Button startButton;
        public Button quitButton;
        public GameObject menuPanel;
        public GameObject trainingPanel;

        [Header("CPR组件")]
        public Core.CPRPressureDetector pressureDetector;

        [Header("设置")]
        public float delayBeforeCPR = 2.5f;

        [Header("训练设置")]
        public int targetCompressions = 30;

        [Header("状态")]
        [SerializeField] private GameState currentState = GameState.Menu;

        public enum GameState
        {
            Menu,
            PatientFalling,
            CPRTraining,
            Completed
        }

        private void Start()
        {
            // 自动查找组件
            AutoFindComponents();
            
            // 绑定按钮
            SetupButtons();
            
            // 初始状态
            ShowMenu();
        }

        private void AutoFindComponents()
        {
            // 查找患者模型
            if (patientModel == null)
            {
                patientModel = GameObject.Find("XBot_Patient");
            }
            
            if (patientModel != null)
            {
                // 自动修复 Transform（检测到缩放过小则修正）
                if (patientModel.transform.localScale.x < 0.5f)
                {
                    Debug.Log($"[CPRGame] 自动修复: Scale {patientModel.transform.localScale} -> (1,1,1)");
                    patientModel.transform.localScale = Vector3.one;
                }
                if (patientModel.transform.position == Vector3.zero)
                {
                    patientModel.transform.position = new Vector3(0, 0, 2.5f);
                    Debug.Log("[CPRGame] 自动修复: Position -> (0, 0, 2.5)");
                }
                patientModel.transform.rotation = Quaternion.Euler(0, 180, 0);
                patientModel.SetActive(true);

                // 修复 Animator
                var animator = patientModel.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.applyRootMotion = false;
                }

                // 自动添加 PatientFallController（如果缺失）
                fallController = patientModel.GetComponent<Core.PatientFallController>();
                if (fallController == null)
                {
                    Debug.Log("[CPRGame] 自动修复: 添加 PatientFallController");
                    fallController = patientModel.AddComponent<Core.PatientFallController>();
                    fallController.fallDuration = 2f;
                    fallController.finalRotationX = -90f;
                }
            }

            // 查找检测器
            if (pressureDetector == null)
            {
                pressureDetector = FindObjectOfType<Core.CPRPressureDetector>();
            }

            // 查找UI
            if (startButton == null)
            {
                var startObj = GameObject.Find("StartButton");
                if (startObj != null)
                    startButton = startObj.GetComponent<Button>();
            }

            if (quitButton == null)
            {
                var quitObj = GameObject.Find("QuitButton");
                if (quitObj != null)
                    quitButton = quitObj.GetComponent<Button>();
            }
        }

        private void SetupButtons()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveAllListeners();
                startButton.onClick.AddListener(OnStartTraining);
                Debug.Log("[CPRGame] 开始按钮已绑定");
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveAllListeners();
                quitButton.onClick.AddListener(OnQuit);
                Debug.Log("[CPRGame] 退出按钮已绑定");
            }
        }

        public void ShowMenu()
        {
            currentState = GameState.Menu;
            
            if (menuPanel != null)
                menuPanel.SetActive(true);
            if (trainingPanel != null)
                trainingPanel.SetActive(false);

            Debug.Log("[CPRGame] 显示主菜单");
        }

        public void OnStartTraining()
        {
            Debug.Log("[CPRGame] ====== 点击开始训练！ ======");
            Debug.Log($"[CPRGame] patientModel={patientModel}, fallController={fallController}");
            
            currentState = GameState.PatientFalling;

            // 隐藏菜单
            if (menuPanel != null)
                menuPanel.SetActive(false);

            // 最后一次尝试获取 fallController
            if (fallController == null && patientModel != null)
            {
                fallController = patientModel.GetComponent<Core.PatientFallController>();
                if (fallController == null)
                {
                    Debug.Log("[CPRGame] 运行时添加 PatientFallController");
                    fallController = patientModel.AddComponent<Core.PatientFallController>();
                    fallController.fallDuration = 2f;
                    fallController.finalRotationX = -90f;
                }
            }

            // 触发倒地
            if (fallController != null)
            {
                // 先减再加：OnStartTraining 接在按钮点击上、可以被再次触发，
                // 而退订写在 OnPatientFallen 里（倒地完成才执行）。倒地前重复进入
                // 就会累积处理器，导致 StartCPRPhase 被 Invoke 多次。
                fallController.OnFallComplete -= OnPatientFallen;
                fallController.OnFallComplete += OnPatientFallen;
                fallController.TriggerFall();
                Debug.Log("[CPRGame] 患者开始倒地动画！");
            }
            else
            {
                Debug.LogWarning("[CPRGame] 警告: 没有找到 fallController，跳过倒地");
                Invoke(nameof(StartCPRPhase), delayBeforeCPR);
            }
        }

        private void OnPatientFallen()
        {
            if (fallController != null)
            {
                fallController.OnFallComplete -= OnPatientFallen;
            }
            
            // 倒地完成后开始CPR
            Invoke(nameof(StartCPRPhase), 0.5f);
        }

        private void StartCPRPhase()
        {
            currentState = GameState.CPRTraining;
            
            // 显示训练UI
            if (trainingPanel != null)
                trainingPanel.SetActive(true);

            // 启用检测器
            if (pressureDetector != null)
            {
                pressureDetector.ResetCounters();
                pressureDetector.gameObject.SetActive(true);
            }

            Debug.Log("[CPRGame] CPR训练开始！请按压患者胸部。");
        }

        public void OnQuit()
        {
            Debug.Log("[CPRGame] 退出游戏");
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        private void Update()
        {
            if (currentState == GameState.CPRTraining && pressureDetector != null)
            {
                // 检查是否完成目标次数
                if (pressureDetector.CompressionCount >= targetCompressions)
                {
                    CompleteTraining();
                }
            }
        }

        private void CompleteTraining()
        {
            if (currentState == GameState.Completed) return;
            
            currentState = GameState.Completed;
            Debug.Log("[CPRGame] 训练完成！");
            
            // 可以添加完成UI或返回菜单的逻辑
        }
    }
}
