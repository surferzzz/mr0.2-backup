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

        [Tooltip("使用掌心(腕根与中指掌骨中点)作为接触点，比单用腕根更接近真实掌根按压点。")]
        public bool usePalmCenter = true;

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
        private Transform _leftMiddleBone;
        private Transform _rightMiddleBone;
        private Transform _leftHandAnchor;
        private Transform _rightHandAnchor;
        // 掌心代理：每帧更新为「腕根/中指掌骨」中点，作为更准确的接触点
        private Transform _leftPalmProxy;
        private Transform _rightPalmProxy;
        private bool _leftPalmProxyValid;
        private bool _rightPalmProxyValid;

        public bool IsLeftTracking => _leftTracking;
        public bool IsRightTracking => _rightTracking;
        public bool IsAnyHandTracking => _leftTracking || _rightTracking;
        public Vector3 ActiveHandPosition => _activeHandPosition;

        /// <summary>
        /// 左/右手各自的接触点世界坐标。追踪不到时返回 false。
        ///
        /// ActiveHandPosition 只给「当前活跃的那只手」，判不了双手同时做的动作。
        /// 意识确认要分辨「拍的是左肩还是右肩」，必须按手区分，所以补这两个入口。
        /// 取值顺序和 GetActiveHandPosition 一致：掌心代理优先，退回手部锚点。
        /// </summary>
        public bool TryGetLeftHandPosition(out Vector3 position)
        {
            return TryGetHandPosition(true, out position);
        }

        public bool TryGetRightHandPosition(out Vector3 position)
        {
            return TryGetHandPosition(false, out position);
        }

        private bool TryGetHandPosition(bool left, out Vector3 position)
        {
            position = Vector3.zero;

            Transform palm = left ? GetLeftPalmTransform() : GetRightPalmTransform();
            if (palm != null && palm.position.sqrMagnitude > 0.0001f)
            {
                position = palm.position;
                return true;
            }

            Transform anchor = left ? _leftHandAnchor : _rightHandAnchor;
            if (anchor != null && anchor.position.sqrMagnitude > 0.0001f)
            {
                position = anchor.position;
                return true;
            }

            return false;
        }

        private void Start()
        {
            if (autoFindOVRHands)
            {
                FindOVRHands();
            }

            // 掌心代理对象（运行时创建，不参与场景保存）
            _leftPalmProxy = new GameObject("CPR_LeftPalmCenter").transform;
            _rightPalmProxy = new GameObject("CPR_RightPalmCenter").transform;
            _leftPalmProxy.SetParent(transform, false);
            _rightPalmProxy.SetParent(transform, false);

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
            UpdatePalmProxies();
            UpdateDetectorReference();
        }

        /// <summary>
        /// 每帧把掌心代理放到「腕根与中指掌骨中点」，更贴近真实掌根接触点。
        /// 缺中指骨时退化为腕根位置。
        /// </summary>
        private void UpdatePalmProxies()
        {
            _leftPalmProxyValid = UpdatePalmProxy(_leftPalmProxy, _leftPalmBone, _leftMiddleBone);
            _rightPalmProxyValid = UpdatePalmProxy(_rightPalmProxy, _rightPalmBone, _rightMiddleBone);
        }

        private bool UpdatePalmProxy(Transform proxy, Transform wrist, Transform middle)
        {
            if (proxy == null || wrist == null) return false;
            proxy.position = (middle != null)
                ? Vector3.Lerp(wrist.position, middle.position, 0.5f)
                : wrist.position;
            return true;
        }

        private Transform FindBone(OVRSkeleton skeleton, OVRSkeleton.BoneId boneId)
        {
            if (skeleton == null || skeleton.Bones == null || skeleton.Bones.Count == 0)
                return null;
            foreach (var bone in skeleton.Bones)
            {
                if (bone.Id == boneId)
                    return bone.Transform;
            }
            return null;
        }

        private void UpdateTrackingStatus()
        {
            // 左手のトラッキング状態を更新（手のひらボーンがあれば使用し、なければアンカー位置を使用する）
            _leftTracking = false;
            if (leftOVRHand != null && leftOVRHand.IsTracked)
            {
                if (_leftPalmBone == null)
                    _leftPalmBone = FindPalmBone(_leftSkeleton);
                if (_leftMiddleBone == null)
                    _leftMiddleBone = FindBone(_leftSkeleton, OVRSkeleton.BoneId.Hand_Middle1);
                _leftTracking = _leftPalmBone != null || _leftHandAnchor != null;
            }
            else if (leftHandFallback != null || _leftHandAnchor != null)
            {
                _leftTracking = true;
            }

            // 右手のトラッキング状態を更新
            _rightTracking = false;
            if (rightOVRHand != null && rightOVRHand.IsTracked)
            {
                if (_rightPalmBone == null)
                    _rightPalmBone = FindPalmBone(_rightSkeleton);
                if (_rightMiddleBone == null)
                    _rightMiddleBone = FindBone(_rightSkeleton, OVRSkeleton.BoneId.Hand_Middle1);
                _rightTracking = _rightPalmBone != null || _rightHandAnchor != null;
            }
            else if (rightHandFallback != null || _rightHandAnchor != null)
            {
                _rightTracking = true;
            }

            // 現在有効な手の位置を更新
            _activeHandPosition = GetActiveHandPosition();
        }

        private void UpdateDetectorReference()
        {
            if (pressDetector == null || pressDetector.useCollisionDetection) return;

            // 従来モード：検出器に手のひらのTransformを渡す
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
            if (usePalmCenter && _leftPalmProxyValid && _leftTracking)
                return _leftPalmProxy;
            if (_leftPalmBone != null && _leftTracking)
                return _leftPalmBone;
            if (leftHandFallback != null) return leftHandFallback;
            return _leftHandAnchor;
        }

        private Transform GetRightPalmTransform()
        {
            if (usePalmCenter && _rightPalmProxyValid && _rightTracking)
                return _rightPalmProxy;
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
