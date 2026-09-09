using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRFirstAid.UI
{
    /// <summary>
    /// 教程步骤数据
    /// </summary>
    [System.Serializable]
    public class TutorialStep
    {
        public string title;
        public string description;
        public GameObject highlightTarget; // 要高亮显示的对象
        public float duration; // 步骤持续时间（0表示需要手动完成）
    }

    /// <summary>
    /// 教程UI - 管理教程引导系统
    /// </summary>
    public class TutorialUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject tutorialPanel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private Button previousButton;

        [Header("Tutorial Steps")]
        [SerializeField] private List<TutorialStep> tutorialSteps = new List<TutorialStep>();
        [SerializeField] private int currentStepIndex = 0;

        [Header("Visual Highlight")]
        [SerializeField] private Material highlightMaterial;
        [SerializeField] private Color highlightColor = Color.yellow;
        [SerializeField] private float highlightIntensity = 0.5f;

        private GameObject currentHighlightObject;
        private Material[] originalMaterials;
        private Renderer currentRenderer;

        // 自动前进计时器的句柄。必须保存，否则换步时停不掉上一个，
        // 手动点「下一步」会因为旧计时器到期而多前进一步。
        private Coroutine _autoNextRoutine;

        // 事件
        public System.Action OnTutorialCompleted;
        public System.Action OnTutorialSkipped;

        private void Start()
        {
            InitializeTutorial();
        }

        private void InitializeTutorial()
        {
            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(false);
            }

            if (nextButton != null)
            {
                nextButton.onClick.AddListener(OnNextClicked);
            }

            if (skipButton != null)
            {
                skipButton.onClick.AddListener(OnSkipClicked);
            }

            if (previousButton != null)
            {
                previousButton.onClick.AddListener(OnPreviousClicked);
            }
        }

        /// <summary>
        /// 开始教程
        /// </summary>
        public void StartTutorial()
        {
            currentStepIndex = 0;
            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(true);
            }
            ShowCurrentStep();
        }

        /// <summary>
        /// 显示当前步骤
        /// </summary>
        private void ShowCurrentStep()
        {
            if (currentStepIndex < 0 || currentStepIndex >= tutorialSteps.Count)
            {
                CompleteTutorial();
                return;
            }

            TutorialStep step = tutorialSteps[currentStepIndex];

            // 更新文本
            if (titleText != null)
            {
                titleText.text = step.title;
            }

            if (descriptionText != null)
            {
                descriptionText.text = step.description;
            }

            // 高亮显示目标对象
            HighlightTarget(step.highlightTarget);

            // 更新按钮状态
            if (previousButton != null)
            {
                previousButton.interactable = currentStepIndex > 0;
            }

            // 先停掉上一步遗留的自动前进计时器，再决定要不要开新的。
            //
            // 不停的话会漏掉一步：手动点「下一步」时，上一步的计时器还挂着，
            // 它到期后又调一次 OnNextClicked，于是直接跳过一步；
            // 而且每次手动点击都会多留一个孤儿计时器，越点越乱。
            if (_autoNextRoutine != null)
            {
                StopCoroutine(_autoNextRoutine);
                _autoNextRoutine = null;
            }

            if (nextButton != null)
            {
                // 如果步骤有持续时间，自动进入下一步
                if (step.duration > 0)
                {
                    _autoNextRoutine = StartCoroutine(AutoNextStep(step.duration));
                }
            }
        }

        /// <summary>
        /// 高亮显示目标对象
        /// </summary>
        private void HighlightTarget(GameObject target)
        {
            // 清除之前的高亮
            ClearHighlight();

            if (target == null) return;

            currentHighlightObject = target;
            currentRenderer = target.GetComponent<Renderer>();

            if (currentRenderer != null)
            {
                // 保存原始材质
                originalMaterials = currentRenderer.materials;

                // 创建高亮材质
                Material[] highlightMaterials = new Material[originalMaterials.Length];
                for (int i = 0; i < highlightMaterials.Length; i++)
                {
                    highlightMaterials[i] = highlightMaterial != null ? highlightMaterial : new Material(originalMaterials[i]);
                    highlightMaterials[i].color = highlightColor;
                    highlightMaterials[i].SetFloat("_EmissionIntensity", highlightIntensity);
                }

                currentRenderer.materials = highlightMaterials;
            }

            // 如果是可交互对象，可以添加额外的视觉效果
            var interactable = target.GetComponent<XRBaseInteractable>();
            if (interactable != null)
            {
                // 可以添加额外的交互提示
            }
        }

        /// <summary>
        /// 清除高亮
        /// </summary>
        private void ClearHighlight()
        {
            if (currentRenderer != null && originalMaterials != null)
            {
                currentRenderer.materials = originalMaterials;
            }

            currentHighlightObject = null;
            currentRenderer = null;
            originalMaterials = null;
        }

        private void OnNextClicked()
        {
            currentStepIndex++;
            ShowCurrentStep();
        }

        private void OnPreviousClicked()
        {
            if (currentStepIndex > 0)
            {
                currentStepIndex--;
                ShowCurrentStep();
            }
        }

        private void OnSkipClicked()
        {
            SkipTutorial();
        }

        private IEnumerator AutoNextStep(float delay)
        {
            yield return new WaitForSeconds(delay);
            _autoNextRoutine = null;   // 自己跑完了，句柄置空
            OnNextClicked();
        }

        /// <summary>
        /// 完成教程
        /// </summary>
        private void CompleteTutorial()
        {
            ClearHighlight();

            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(false);
            }

            OnTutorialCompleted?.Invoke();
        }

        /// <summary>
        /// 跳过教程
        /// </summary>
        private void SkipTutorial()
        {
            ClearHighlight();

            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(false);
            }

            OnTutorialSkipped?.Invoke();
        }

        /// <summary>
        /// 添加教程步骤
        /// </summary>
        public void AddTutorialStep(TutorialStep step)
        {
            tutorialSteps.Add(step);
        }

        /// <summary>
        /// 清除所有步骤
        /// </summary>
        public void ClearSteps()
        {
            tutorialSteps.Clear();
            currentStepIndex = 0;
        }

        private void OnDestroy()
        {
            ClearHighlight();
        }
    }
}
