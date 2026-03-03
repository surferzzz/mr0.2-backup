using UnityEngine;
using System.Collections;

namespace CPRSimulation.Core
{
    /// <summary>
    /// 患者倒地控制器 - 使用代码动画替代有问题的FBX动画
    /// </summary>
    public class PatientFallController : MonoBehaviour
    {
        [Header("倒地设置")]
        public float fallDuration = 2f;
        public float finalRotationX = -90f; // 躺平角度
        public AnimationCurve fallCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("状态")]
        [SerializeField] private bool hasFallen = false;
        [SerializeField] private bool isFalling = false;

        private Vector3 startPosition;
        private Quaternion startRotation;
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private bool initialized = false;

        public bool HasFallen => hasFallen;
        public bool IsFalling => isFalling;

        public event System.Action OnFallComplete;

        private void Awake()
        {
            var animator = GetComponent<Animator>();
            if (animator != null)
            {
                animator.applyRootMotion = false;
            }
        }

        private void Start()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (initialized) return;
            initialized = true;

            startPosition = transform.position;
            startRotation = transform.rotation;
            
            targetRotation = Quaternion.Euler(finalRotationX, transform.eulerAngles.y, 0);
            targetPosition = new Vector3(startPosition.x, 0.05f, startPosition.z);
            
            Debug.Log($"[PatientFall] 初始化完成 - 起始位置: {startPosition}, 目标旋转: {targetRotation.eulerAngles}");
        }

        /// <summary>
        /// 触发倒地
        /// </summary>
        public void TriggerFall()
        {
            EnsureInitialized();
            if (!isFalling && !hasFallen)
            {
                Debug.Log("[PatientFall] TriggerFall 被调用！");
                StartCoroutine(FallRoutine());
            }
            else
            {
                Debug.LogWarning($"[PatientFall] TriggerFall 被忽略 - isFalling={isFalling}, hasFallen={hasFallen}");
            }
        }

        /// <summary>
        /// 立即躺下（跳过动画）
        /// </summary>
        public void SetLyingDown()
        {
            transform.position = targetPosition;
            transform.rotation = targetRotation;
            hasFallen = true;
            isFalling = false;
        }

        /// <summary>
        /// 重置到站立状态
        /// </summary>
        public void ResetToStanding()
        {
            StopAllCoroutines();
            transform.position = startPosition;
            transform.rotation = startRotation;
            hasFallen = false;
            isFalling = false;
        }

        private IEnumerator FallRoutine()
        {
            isFalling = true;
            float elapsed = 0f;

            Vector3 fallStartPos = transform.position;
            Quaternion fallStartRot = transform.rotation;

            Debug.Log("[PatientFall] 开始倒地动画");

            while (elapsed < fallDuration)
            {
                elapsed += Time.deltaTime;
                float t = fallCurve.Evaluate(elapsed / fallDuration);

                // 插值位置和旋转
                transform.position = Vector3.Lerp(fallStartPos, targetPosition, t);
                transform.rotation = Quaternion.Slerp(fallStartRot, targetRotation, t);

                yield return null;
            }

            // 确保最终位置精确
            transform.position = targetPosition;
            transform.rotation = targetRotation;

            isFalling = false;
            hasFallen = true;

            Debug.Log("[PatientFall] 倒地完成");
            OnFallComplete?.Invoke();
        }
    }
}
