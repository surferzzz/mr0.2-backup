using UnityEngine;

namespace CPRSimulation
{
    /// <summary>
    /// 手部追踪适配器 - 获取Meta Quest手部追踪的手掌位置
    /// 支持OVRHand和OVRSkeleton，以及手动指定的Transform
    /// </summary>
    public class HandTrackingAdapter : MonoBehaviour
    {
        [Header("追踪模式")]
        [Tooltip("自动查找OVR手部组件")]
        public bool autoFindOVRHands = true;

        [Header("手部选择")]
        [Tooltip("使用哪只手进行CPR检测")]
        public HandSide preferredHand = HandSide.Right;
        
        [Tooltip("允许双手检测")]
        public bool allowBothHands = true;

        [Header("OVR引用")]
        public OVRHand leftOVRHand;
        public OVRHand rightOVRHand;

        [Header("备用Transform引用")]
        [Tooltip("如果没有OVR组件，使用这些Transform")]
        public Transform leftHandFallback;
        public Transform rightHandFallback;

        [Header("目标检测器")]
        public CPRPressDetector pressDetector;

        [Header("状态（只读）")]
        [SerializeField] private bool _leftTracking;
        [SerializeField] private bool _rightTracking;
        [SerializeField] private Vector3 _activeHandPosition;

        public enum HandSide { Left, Right, Both }

        private OVRSkeleton _leftSkeleton;
        private OVRSkeleton _rightSkeleton;
        private Transform _leftPalmBone;
        private Transform _rightPalmBone;
        private Transform _leftHandAnchor;
        private Transform _rightHandAnchor;

        public bool IsLeftTracking => _leftTracking;
        public bool IsRightTracking => _rightTracking;
        public bool IsAnyHandTracking => _leftTracking || _rightTracking;
        public Vector3 ActiveHandPosition => _activeHandPosition;

        private void Start()
        {
            if (autoFindOVRHands)
            {
                FindOVRHands();
            }

            if (pressDetector == null)
            {
                pressDetector = FindObjectOfType<CPRPressDetector>();
            }

            // 获取骨骼组件
            if (leftOVRHand != null)
                _leftSkeleton = leftOVRHand.GetComponent<OVRSkeleton>();
            if (rightOVRHand != null)
                _rightSkeleton = rightOVRHand.GetComponent<OVRSkeleton>();

            // 手部锚点备用（手掌骨骼未就绪时用锚点位置，保证距离检测有值）
            var rig = FindObjectOfType<OVRCameraRig>();
            if (rig != null)
            {
                _leftHandAnchor = rig.leftHandAnchor;
                _rightHandAnchor = rig.rightHandAnchor;
                if (_rightHandAnchor != null || _leftHandAnchor != null)
                    Debug.Log("[HandTrackingAdapter] 已绑定手部锚点备用");
            }
        }

        private void FindOVRHands()
        {
            var allHands = FindObjectsOfType<OVRHand>();
            foreach (var hand in allHands)
            {
                var skeleton = hand.GetComponent<OVRSkeleton>();
                if (skeleton == null) continue;

                var skeletonType = skeleton.GetSkeletonType();
                if (skeletonType == OVRSkeleton.SkeletonType.HandLeft && leftOVRHand == null)
                {
                    leftOVRHand = hand;
                    _leftSkeleton = skeleton;
                    Debug.Log("[HandTrackingAdapter] 找到左手OVR组件");
                }
                else if (skeletonType == OVRSkeleton.SkeletonType.HandRight && rightOVRHand == null)
                {
                    rightOVRHand = hand;
                    _rightSkeleton = skeleton;
                    Debug.Log("[HandTrackingAdapter] 找到右手OVR组件");
                }
            }
        }

        private void Update()
        {
            UpdateTrackingStatus();
            UpdateDetectorReference();
        }

        private void UpdateTrackingStatus()
        {
            // 更新左手状态（有手掌骨用手掌骨，否则用锚点也算“有位置”）
            _leftTracking = false;
            if (leftOVRHand != null && leftOVRHand.IsTracked)
            {
                if (_leftPalmBone == null)
                    _leftPalmBone = FindPalmBone(_leftSkeleton);
                _leftTracking = _leftPalmBone != null || _leftHandAnchor != null;
            }
            else if (leftHandFallback != null || _leftHandAnchor != null)
            {
                _leftTracking = true;
            }

            // 更新右手状态
            _rightTracking = false;
            if (rightOVRHand != null && rightOVRHand.IsTracked)
            {
                if (_rightPalmBone == null)
                    _rightPalmBone = FindPalmBone(_rightSkeleton);
                _rightTracking = _rightPalmBone != null || _rightHandAnchor != null;
            }
            else if (rightHandFallback != null || _rightHandAnchor != null)
            {
                _rightTracking = true;
            }

            // 更新活动手位置
            _activeHandPosition = GetActiveHandPosition();
        }

        private void UpdateDetectorReference()
        {
            if (pressDetector == null || pressDetector.useCollisionDetection) return;

            // 传统模式：提供手掌Transform给检测器
            Transform palmTransform = GetActivePalmTransform();
            if (palmTransform != null)
            {
                pressDetector.handPalm = palmTransform;
            }
        }

        private Transform FindPalmBone(OVRSkeleton skeleton)
        {
            if (skeleton == null || skeleton.Bones == null || skeleton.Bones.Count == 0)
                return null;

            // 优先使用手腕根部或中指根部作为"手掌"位置
            foreach (var bone in skeleton.Bones)
            {
                if (bone.Id == OVRSkeleton.BoneId.Hand_WristRoot)
                    return bone.Transform;
            }

            foreach (var bone in skeleton.Bones)
            {
                if (bone.Id == OVRSkeleton.BoneId.Hand_Middle1)
                    return bone.Transform;
            }

            return null;
        }

        private Vector3 GetActiveHandPosition()
        {
            Transform palm = GetActivePalmTransform();
            if (palm != null && palm.position.sqrMagnitude > 0.0001f)
                return palm.position;
            // 手掌骨骼未就绪时用手部锚点位置，保证距离检测能工作
            Transform anchor = GetActiveHandAnchor();
            return anchor != null ? anchor.position : Vector3.zero;
        }

        private Transform GetActiveHandAnchor()
        {
            switch (preferredHand)
            {
                case HandSide.Left: return _leftHandAnchor ?? (allowBothHands ? _rightHandAnchor : null);
                case HandSide.Right: return _rightHandAnchor ?? (allowBothHands ? _leftHandAnchor : null);
                case HandSide.Both: return _rightHandAnchor != null ? _rightHandAnchor : _leftHandAnchor;
                default: return _rightHandAnchor ?? _leftHandAnchor;
            }
        }

        private Transform GetActivePalmTransform()
        {
            // 根据偏好选择手
            switch (preferredHand)
            {
                case HandSide.Left:
                    return GetLeftPalmTransform() ?? (allowBothHands ? GetRightPalmTransform() : null);
                
                case HandSide.Right:
                    return GetRightPalmTransform() ?? (allowBothHands ? GetLeftPalmTransform() : null);
                
                case HandSide.Both:
                    // 双手模式：返回位置较低的手（更接近按压）
                    var left = GetLeftPalmTransform();
                    var right = GetRightPalmTransform();
                    
                    if (left == null) return right;
                    if (right == null) return left;
                    
                    return left.position.y < right.position.y ? left : right;
                
                default:
                    return GetRightPalmTransform() ?? GetLeftPalmTransform();
            }
        }

        private Transform GetLeftPalmTransform()
        {
            if (_leftPalmBone != null && _leftTracking)
                return _leftPalmBone;
            if (leftHandFallback != null) return leftHandFallback;
            return _leftHandAnchor;
        }

        private Transform GetRightPalmTransform()
        {
            if (_rightPalmBone != null && _rightTracking)
                return _rightPalmBone;
            if (rightHandFallback != null) return rightHandFallback;
            return _rightHandAnchor;
        }

        // 调试：显示手部追踪状态
        private void OnGUI()
        {
            if (!Application.isEditor) return;

            GUILayout.BeginArea(new Rect(10, 10, 200, 100));
            GUILayout.Label($"左手: {(_leftTracking ? "追踪中" : "未追踪")}");
            GUILayout.Label($"右手: {(_rightTracking ? "追踪中" : "未追踪")}");
            GUILayout.EndArea();
        }
    }
}
