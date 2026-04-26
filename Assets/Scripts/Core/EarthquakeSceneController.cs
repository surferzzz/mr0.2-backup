using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using VRFirstAid.CPR;
using VRFirstAid.UI;

namespace VRFirstAid.Core
{
    /// <summary>
    /// 地震场景控制器 - 管理地震模拟和CPR训练流程
    /// </summary>
    public class EarthquakeSceneController : MonoBehaviour
    {
        [Header("Components")]
        [Tooltip("地震控制器")]
        public EarthquakeController earthquakeController;

        [Tooltip("CPR管理器")]
        public CPRManager cprManager;

        [Header("UI Elements")]
        [Tooltip("触发地震的按钮")]
        public Button triggerButton;

        [Tooltip("指导文本")]
        public TMPro.TextMeshProUGUI instructionText;

        [Header("Settings")]
        [Tooltip("地震结束后自动开始CPR训练")]
        public bool autoStartCPR = true;

        [Tooltip("地震结束后延迟多少秒开始CPR")]
        public float cprStartDelay = 2f;

        private enum SceneState
        {
            Idle,           // 等待开始
            Earthquake,     // 地震进行中
            PostEarthquake, // 地震结束
            CPRTraining     // CPR训练中
        }

        private SceneState currentState = SceneState.Idle;
        private EarthquakeUIStyler _styler;

        private void Awake()
        {
            AutoAssignReferences();
            EnsureStyler();
        }

        private void Start()
        {
            AutoAssignReferences();
            EnsureStyler();

            // 连接按钮事件
            if (triggerButton != null)
            {
                triggerButton.onClick.AddListener(OnTriggerButtonClicked);
            }
            else
            {
                Debug.LogWarning("[EarthquakeScene] Trigger button reference is missing; auto-binding failed.");
            }

            // 初始化CPR管理器为非激活状态
            if (cprManager != null)
            {
                cprManager.gameObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning("[EarthquakeScene] CPRManager reference is missing; CPR flow will be skipped.");
            }

            UpdateInstructionText("点击按钮开始地震模拟");
        }

        private void EnsureStyler()
        {
            if (_styler != null) return;

            if (triggerButton != null)
            {
                _styler = triggerButton.GetComponentInParent<EarthquakeUIStyler>(true);
            }

            if (_styler == null)
            {
                _styler = FindObjectOfType<EarthquakeUIStyler>(true);
            }

            if (_styler == null && triggerButton != null)
            {
                _styler = triggerButton.gameObject.AddComponent<EarthquakeUIStyler>();
            }

            if (_styler != null)
            {
                if (_styler.triggerButton == null) _styler.triggerButton = triggerButton;
                if (_styler.instructionText == null) _styler.instructionText = instructionText;
            }
        }

        /// <summary>
        /// 触发按钮点击事件
        /// </summary>
        private void OnTriggerButtonClicked()
        {
            if (currentState == SceneState.Idle)
            {
                StartEarthquake();
            }
            else if (currentState == SceneState.PostEarthquake)
            {
                StartCPRTraining();
            }
        }

        /// <summary>
        /// 开始地震模拟
        /// </summary>
        public void StartEarthquake()
        {
            AutoAssignReferences();
            CancelInvoke();

            if (earthquakeController == null)
            {
                Debug.LogWarning("EarthquakeController not assigned!");
                return;
            }

            currentState = SceneState.Earthquake;
            UpdateInstructionText("地震进行中...");

            // 禁用按钮
            if (triggerButton != null)
            {
                triggerButton.interactable = false;
            }

            // 触发地震
            earthquakeController.Play();

            // 在地震结束后调用
            Invoke(nameof(OnEarthquakeEnded), earthquakeController.duration);
        }

        /// <summary>
        /// 地震结束回调
        /// </summary>
        private void OnEarthquakeEnded()
        {
            currentState = SceneState.PostEarthquake;
            UpdateInstructionText("地震已结束。发现一名伤员倒地！");

            // 重新启用按钮，改变文本
            if (triggerButton != null)
            {
                triggerButton.interactable = true;
                var buttonText = triggerButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = "开始CPR训练";
                }
            }

            // 自动开始CPR训练
            if (autoStartCPR)
            {
                Invoke(nameof(StartCPRTraining), cprStartDelay);
            }
        }

        /// <summary>
        /// 开始CPR训练
        /// </summary>
        public void StartCPRTraining()
        {
            currentState = SceneState.CPRTraining;
            UpdateInstructionText("请开始对伤员进行心肺复苏");

            // 隐藏触发按钮UI
            if (triggerButton != null)
            {
                triggerButton.gameObject.SetActive(false);
            }

            // 启动CPR管理器
            if (cprManager != null)
            {
                cprManager.gameObject.SetActive(true);
                // CPRManager会自动开始训练流程
            }
            else
            {
                Debug.LogWarning("CPRManager not assigned! Please add CPR components to the scene.");
            }
        }

        /// <summary>
        /// 重置场景
        /// </summary>
        public void ResetScene()
        {
            CancelInvoke();
            currentState = SceneState.Idle;

            // 停止地震
            if (earthquakeController != null)
            {
                earthquakeController.Stop();
            }

            // 重置CPR管理器
            if (cprManager != null)
            {
                cprManager.ResetCPR();
                cprManager.gameObject.SetActive(false);
            }

            // 重置UI
            if (triggerButton != null)
            {
                triggerButton.gameObject.SetActive(true);
                triggerButton.interactable = true;
                var buttonText = triggerButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = "触发地震";
                }
            }

            UpdateInstructionText("点击按钮开始地震模拟");
        }

        private void AutoAssignReferences()
        {
            if (earthquakeController == null)
            {
                earthquakeController = FindObjectOfType<EarthquakeController>(true);
            }

            if (triggerButton == null)
            {
                triggerButton = FindObjectsOfType<Button>(true)
                    .FirstOrDefault(b => b != null &&
                                         b.name.IndexOf("trigger", StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (instructionText == null)
            {
                instructionText = FindObjectsOfType<TMPro.TextMeshProUGUI>(true)
                    .FirstOrDefault(t => t != null &&
                                         t.name.IndexOf("instruction", StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (cprManager == null)
            {
                cprManager = FindObjectOfType<CPRManager>(true);
            }
        }

        /// <summary>
        /// 更新指导文本
        /// </summary>
        private void UpdateInstructionText(string message)
        {
            if (instructionText != null)
            {
                instructionText.text = message;
            }
            Debug.Log($"[EarthquakeScene] {message}");
        }

        private void OnDestroy()
        {
            CancelInvoke();
            // 清理按钮事件
            if (triggerButton != null)
            {
                triggerButton.onClick.RemoveListener(OnTriggerButtonClicked);
            }
        }
    }
}
