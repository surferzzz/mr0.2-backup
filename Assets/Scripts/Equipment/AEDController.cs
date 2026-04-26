using System.Collections;
using UnityEngine;
using VRFirstAid.Core;

namespace VRFirstAid.Equipment
{
    /// <summary>
    /// AED设备状态
    /// </summary>
    public enum AEDState
    {
        Off,
        On,
        Analyzing,
        ShockAdvised,
        NoShockAdvised,
        ShockDelivered
    }

    /// <summary>
    /// AED设备控制器 - 管理AED设备的操作流程
    /// </summary>
    public class AEDController : EquipmentBase
    {
        [Header("AED Settings")]
        [SerializeField] private AEDState currentState = AEDState.Off;
        [SerializeField] private Transform leftPadPosition; // 左电极片位置
        [SerializeField] private Transform rightPadPosition; // 右电极片位置
        [SerializeField] private float analysisDuration = 5f; // 分析持续时间

        [Header("Visual Feedback")]
        [SerializeField] private GameObject powerButton;
        [SerializeField] private GameObject leftPad;
        [SerializeField] private GameObject rightPad;
        [SerializeField] private Light statusLight;
        [SerializeField] private Color analyzingColor = Color.yellow;
        [SerializeField] private Color shockColor = Color.red;
        [SerializeField] private Color safeColor = Color.green;

        [Header("Audio")]
        [SerializeField] private AudioClip powerOnSound;
        [SerializeField] private AudioClip analyzingSound;
        [SerializeField] private AudioClip shockAdvisedSound;
        [SerializeField] private AudioClip noShockSound;
        [SerializeField] private AudioClip shockDeliveredSound;

        // 事件
        public System.Action<AEDState> OnStateChanged;
        public System.Action OnShockDelivered;

        private bool isLeftPadPlaced = false;
        private bool isRightPadPlaced = false;

        protected override void InitializeEquipment()
        {
            base.InitializeEquipment();
            equipmentName = "AED (自动体外除颤器)";
            description = "用于心脏骤停的紧急除颤设备";
        }

        /// <summary>
        /// 打开AED
        /// </summary>
        public void TurnOn()
        {
            if (currentState != AEDState.Off) return;

            currentState = AEDState.On;
            OnStateChanged?.Invoke(currentState);

            if (powerButton != null)
            {
                powerButton.SetActive(true);
            }

            if (powerOnSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(powerOnSound);
            }

            ShowInstruction("AED已开启，请按照提示放置电极片");
        }

        /// <summary>
        /// 放置左电极片
        /// </summary>
        public void PlaceLeftPad(Transform padTransform)
        {
            if (currentState == AEDState.Off) return;

            isLeftPadPlaced = true;
            if (leftPad != null && leftPadPosition != null)
            {
                leftPad.transform.position = leftPadPosition.position;
                leftPad.transform.rotation = leftPadPosition.rotation;
                leftPad.SetActive(true);
            }

            CheckPadsPlacement();
        }

        /// <summary>
        /// 放置右电极片
        /// </summary>
        public void PlaceRightPad(Transform padTransform)
        {
            if (currentState == AEDState.Off) return;

            isRightPadPlaced = true;
            if (rightPad != null && rightPadPosition != null)
            {
                rightPad.transform.position = rightPadPosition.position;
                rightPad.transform.rotation = rightPadPosition.rotation;
                rightPad.SetActive(true);
            }

            CheckPadsPlacement();
        }

        private void CheckPadsPlacement()
        {
            if (isLeftPadPlaced && isRightPadPlaced && currentState == AEDState.On)
            {
                StartAnalysis();
            }
        }

        /// <summary>
        /// 开始分析心律
        /// </summary>
        private void StartAnalysis()
        {
            currentState = AEDState.Analyzing;
            OnStateChanged?.Invoke(currentState);

            if (statusLight != null)
            {
                statusLight.color = analyzingColor;
                statusLight.enabled = true;
            }

            if (analyzingSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(analyzingSound);
            }

            ShowInstruction("正在分析心律，请勿触碰患者...");

            StartCoroutine(AnalysisCoroutine());
        }

        private IEnumerator AnalysisCoroutine()
        {
            yield return new WaitForSeconds(analysisDuration);

            // 随机决定是否需要电击（实际应用中应该基于真实心律分析）
            bool needsShock = Random.Range(0f, 1f) > 0.5f;

            if (needsShock)
            {
                currentState = AEDState.ShockAdvised;
                OnStateChanged?.Invoke(currentState);

                if (statusLight != null)
                {
                    statusLight.color = shockColor;
                }

                if (shockAdvisedSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(shockAdvisedSound);
                }

                ShowInstruction("建议电击！请确保无人触碰患者，然后按下电击按钮");
            }
            else
            {
                currentState = AEDState.NoShockAdvised;
                OnStateChanged?.Invoke(currentState);

                if (statusLight != null)
                {
                    statusLight.color = safeColor;
                }

                if (noShockSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(noShockSound);
                }

                ShowInstruction("不建议电击，请继续CPR");
            }
        }

        /// <summary>
        /// 执行电击
        /// </summary>
        public void DeliverShock()
        {
            if (currentState != AEDState.ShockAdvised) return;

            currentState = AEDState.ShockDelivered;
            OnStateChanged?.Invoke(currentState);

            if (shockDeliveredSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(shockDeliveredSound);
            }

            OnShockDelivered?.Invoke();
            ShowInstruction("电击已执行，请继续CPR");

            // 重置状态，准备下一轮分析
            StartCoroutine(ResetAfterShock());
        }

        private IEnumerator ResetAfterShock()
        {
            yield return new WaitForSeconds(2f);
            isLeftPadPlaced = false;
            isRightPadPlaced = false;
            currentState = AEDState.On;
            OnStateChanged?.Invoke(currentState);
        }

        /// <summary>
        /// 重置AED
        /// </summary>
        public override void ResetEquipment()
        {
            base.ResetEquipment();
            currentState = AEDState.Off;
            isLeftPadPlaced = false;
            isRightPadPlaced = false;

            if (leftPad != null) leftPad.SetActive(false);
            if (rightPad != null) rightPad.SetActive(false);
            if (statusLight != null) statusLight.enabled = false;
        }

        protected override void OnEquipmentUsed()
        {
            TurnOn();
        }

        protected override void OnEquipmentActivated()
        {
            if (currentState == AEDState.ShockAdvised)
            {
                DeliverShock();
            }
        }

        protected override void OnEquipmentReset()
        {
            ResetEquipment();
        }

        private void ShowInstruction(string message)
        {
            Debug.Log($"[AED] {message}");
            // 可以在这里调用UI系统显示提示
        }

        public AEDState CurrentState => currentState;
        public bool ArePadsPlaced => isLeftPadPlaced && isRightPadPlaced;
    }
}
