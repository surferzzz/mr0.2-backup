using UnityEngine;

namespace CPRSimulation
{
    /// <summary>
    /// Quest 3 VR移动控制：左摇杆移动，右摇杆转向
    /// 挂载到 OVRCameraRig 上
    /// </summary>
    public class VRLocomotion : MonoBehaviour
    {
        [Header("移动设置")]
        [Tooltip("移动速度 (米/秒)")]
        public float moveSpeed = 2.0f;

        [Tooltip("转向速度 (度/秒)")]
        public float turnSpeed = 90f;

        [Tooltip("是否使用快速转向(Snap Turn)")]
        public bool useSnapTurn = true;

        [Tooltip("快速转向角度")]
        public float snapTurnAngle = 45f;

        [Header("高级设置")]
        [Tooltip("摇杆死区")]
        [Range(0.05f, 0.5f)]
        public float thumbstickDeadzone = 0.15f;

        [Tooltip("是否根据头部朝向移动")]
        public bool headRelativeMovement = true;

        [Tooltip("是否固定Y轴(不飞行)")]
        public bool lockYAxis = true;

        private Transform _centerEye;
        private bool _snapTurnReady = true;
        private CharacterController _characterController;

        private void Start()
        {
            // 查找 CenterEyeAnchor 作为视角参考
            var cameraRig = GetComponent<OVRCameraRig>();
            if (cameraRig != null)
            {
                _centerEye = cameraRig.centerEyeAnchor;
            }

            if (_centerEye == null)
            {
                _centerEye = Camera.main?.transform;
            }

            // 添加 CharacterController 用于碰撞检测
            _characterController = GetComponent<CharacterController>();
            if (_characterController == null)
            {
                _characterController = gameObject.AddComponent<CharacterController>();
                _characterController.height = 1.8f;
                _characterController.center = new Vector3(0, 0.9f, 0);
                _characterController.radius = 0.15f;
                _characterController.skinWidth = 0.01f;
            }
        }

        private void Update()
        {
            HandleMovement();
            HandleRotation();
        }

        private void HandleMovement()
        {
            // 读取左摇杆输入
            Vector2 input = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);

            if (input.magnitude < thumbstickDeadzone)
                return;

            Vector3 moveDirection;

            if (headRelativeMovement && _centerEye != null)
            {
                // 根据头部朝向计算移动方向
                Vector3 forward = _centerEye.forward;
                Vector3 right = _centerEye.right;

                if (lockYAxis)
                {
                    forward.y = 0;
                    right.y = 0;
                    forward.Normalize();
                    right.Normalize();
                }

                moveDirection = forward * input.y + right * input.x;
            }
            else
            {
                moveDirection = transform.forward * input.y + transform.right * input.x;
            }

            if (lockYAxis)
            {
                moveDirection.y = 0;
            }

            // 使用 CharacterController 移动（带碰撞检测）
            if (_characterController != null)
            {
                // 加上重力
                Vector3 gravity = lockYAxis ? Vector3.down * 9.8f * Time.deltaTime : Vector3.zero;
                _characterController.Move(moveDirection * moveSpeed * Time.deltaTime + gravity);
            }
            else
            {
                transform.position += moveDirection * moveSpeed * Time.deltaTime;
            }
        }

        private void HandleRotation()
        {
            // 读取右摇杆输入
            Vector2 input = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);

            if (Mathf.Abs(input.x) < thumbstickDeadzone)
            {
                _snapTurnReady = true;
                return;
            }

            if (useSnapTurn)
            {
                // 快速转向模式
                if (_snapTurnReady)
                {
                    float direction = input.x > 0 ? 1f : -1f;
                    transform.Rotate(0, snapTurnAngle * direction, 0);
                    _snapTurnReady = false;
                }
            }
            else
            {
                // 平滑转向模式
                transform.Rotate(0, input.x * turnSpeed * Time.deltaTime, 0);
            }
        }
    }
}
