using UnityEngine;

namespace CPRSimulation
{
    /// <summary>
    /// 把 CPR 反馈 UI（World Space Canvas / HUD 根物体）固定在患者头部附近。
    /// 位置完全不依赖相机（既不依赖朝向也不依赖位置）——因此无论用户怎么转头、走动，
    /// UI 都稳定停在患者头旁，不会“围绕一个点旋转”。
    /// 朝向可选择仅绕 Y 轴朝向用户（保持竖直、正对、可读），这只改朝向不改位置，不会产生绕点漂移。
    /// 挂到 TrainingCanvas / HUD 根物体上，Canvas 需为 World Space。
    /// </summary>
    public class CPRHUDFollowCamera : MonoBehaviour
    {
        [Header("锚定")]
        [Tooltip("勾选时：UI 固定在患者头部附近（推荐）；不勾选：UI 退化为摄像头前方")]
        public bool placeUINearPatient = true;
        [Tooltip("锚点（一般为患者头部骨骼）。留空则自动从场景查找患者头部，再退回胸部检测点。")]
        public Transform anchorOverride;
        [Tooltip("UI 相对锚点（头部）的抬高高度（米）")]
        public float heightAboveAnchor = 0.25f;
        [Tooltip("UI 相对锚点的额外水平偏移（患者本地左右/前后，米）。x=右,z=前。用于让面板偏到头部一侧。")]
        public Vector3 localPlanarOffset = Vector3.zero;

        [Header("朝向")]
        [Tooltip("仅绕 Y 轴朝向用户（可读性更好）。关闭则保持锚点朝向。")]
        public bool faceCamera = true;

        [Tooltip("朝向平滑时间常数（秒）。0=瞬间转到位。位置本来就有平滑，" +
                 "朝向若是瞬间赋值，就会出现「位置慢慢跟、朝向猛地弹」。")]
        [Range(0f, 1f)] public float rotationSmoothing = 0.25f;

        [Tooltip("俯身按压时视线几乎正对面板上方，把 y 归零后的水平向量极短，" +
                 "方位角会剧烈抖动甚至 180° 翻转。水平距离小于此值就保持当前朝向。")]
        [Range(0f, 1f)] public float faceDeadzoneRadius = 0.35f;

        [Tooltip("首次朝向用户后锁定，不再跟随。按压过程中用户基本不动，" +
                 "持续 billboard 只会带来抖动 —— 觉得转动碍事就打开这个。")]
        public bool lockFacingAfterFirstOrient = false;

        [Header("其它")]
        [Tooltip("不靠近患者时：UI 与摄像头的水平距离（米）")]
        public float distanceInFront = 0.75f;
        [Tooltip("仅当此物体激活时更新（例如仅 CPR 阶段）")]
        public bool onlyWhenActive = true;
        [Tooltip("位置平滑时间常数（秒）。0=瞬间到位。患者基本静止，可保持较小值。")]
        [Range(0f, 0.5f)] public float followSmoothing = 0.08f;

        [Header("地面以上")]
        [Tooltip("地面高度（世界坐标 Y），用于限制 UI 不沉入地下")]
        public float groundLevel = 0f;
        [Tooltip("UI 最低高度 = groundLevel + 此值（米）")]
        public float minHeightAboveGround = 0.8f;

        // —— 以下为已废弃字段，仅保留以兼容旧序列化数据 ——
        [HideInInspector] public float patientToCameraBlend = 0.4f;
        [HideInInspector] public float chestHeightOffset = 0.5f;
        [HideInInspector] public float distanceFromPatient = 0.45f;

        private Transform _cameraTransform;
        private OVRCameraRig _cameraRig;
        private Canvas _canvas;
        private Transform _patientAnchor;
        private bool _hasInitializedPosition;
        private bool _hasOrientedOnce;

        private void Start()
        {
            _cameraRig = FindObjectOfType<OVRCameraRig>();
            if (_cameraRig != null && _cameraRig.centerEyeAnchor != null)
            {
                var cam = _cameraRig.centerEyeAnchor.GetComponent<Camera>();
                _cameraTransform = cam != null ? _cameraRig.centerEyeAnchor : _cameraRig.transform;
            }
            if (_cameraTransform == null && Camera.main != null)
                _cameraTransform = Camera.main.transform;

            _canvas = GetComponent<Canvas>();
            if (_canvas != null && _canvas.renderMode != RenderMode.WorldSpace)
            {
                _canvas.renderMode = RenderMode.WorldSpace;
                if (_cameraRig != null && _cameraRig.centerEyeAnchor != null)
                {
                    var cam = _cameraRig.centerEyeAnchor.GetComponent<Camera>();
                    if (cam != null) _canvas.worldCamera = cam;
                }
                if (_canvas.worldCamera == null && Camera.main != null)
                    _canvas.worldCamera = Camera.main;
            }

            ResolveAnchor();
        }

        private void ResolveAnchor()
        {
            if (anchorOverride != null)
            {
                _patientAnchor = anchorOverride;
                return;
            }

            // 从胸部检测点回溯到患者根，再找头部骨骼；找不到则退回胸部检测点
            var detector = GameObject.Find("CPR_ChestDetector");
            if (detector != null)
            {
                Transform head = FindDeepByName(detector.transform.root, "Head");
                _patientAnchor = head != null ? head : detector.transform;
            }
        }

        private static Transform FindDeepByName(Transform parent, string nameContains)
        {
            foreach (Transform child in parent)
            {
                if (child.name.IndexOf(nameContains, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return child;
                var result = FindDeepByName(child, nameContains);
                if (result != null) return result;
            }
            return null;
        }

        private void LateUpdate()
        {
            if (onlyWhenActive && !gameObject.activeInHierarchy) return;

            if (anchorOverride != null && _patientAnchor != anchorOverride)
                _patientAnchor = anchorOverride;
            if (placeUINearPatient && _patientAnchor == null)
                ResolveAnchor();

            Vector3 targetPos;

            if (placeUINearPatient && _patientAnchor != null)
            {
                // 纯世界定位：钉在头部锚点附近，不引用相机位置/朝向
                targetPos = _patientAnchor.position + Vector3.up * heightAboveAnchor;
                if (localPlanarOffset != Vector3.zero)
                {
                    targetPos += _patientAnchor.right * localPlanarOffset.x
                               + _patientAnchor.forward * localPlanarOffset.z;
                }
            }
            else if (_cameraTransform != null)
            {
                Vector3 fwd = _cameraTransform.forward;
                fwd.y = 0f;
                if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward;
                fwd.Normalize();
                targetPos = _cameraTransform.position + fwd * distanceInFront;
            }
            else
            {
                return;
            }

            float minY = groundLevel + minHeightAboveGround;
            if (targetPos.y < minY)
                targetPos.y = minY;

            if (!_hasInitializedPosition || followSmoothing <= 0f)
            {
                transform.position = targetPos;
                _hasInitializedPosition = true;
            }
            else
            {
                transform.position = Vector3.Lerp(
                    transform.position,
                    targetPos,
                    1f - Mathf.Exp(-Time.deltaTime / followSmoothing));
            }

            if (faceCamera && _cameraTransform != null && !(_hasOrientedOnce && lockFacingAfterFirstOrient))
            {
                // 仅绕 Y 轴朝向用户，保持竖直可读；这只改朝向、不改位置，不会绕点漂移
                Vector3 faceDir = transform.position - _cameraTransform.position;
                faceDir.y = 0f;

                // 死区：俯身到面板正上方附近时水平向量退化，方位角不再可信，保持原朝向。
                // 原来的阈值是 1e-4（约 1cm），在 5-30cm 这个真实按压距离上完全挡不住抖动。
                if (faceDir.sqrMagnitude > faceDeadzoneRadius * faceDeadzoneRadius)
                {
                    var target = Quaternion.LookRotation(faceDir.normalized, Vector3.up);

                    if (!_hasOrientedOnce || rotationSmoothing <= 0f)
                        transform.rotation = target;
                    else
                        transform.rotation = Quaternion.Slerp(
                            transform.rotation,
                            target,
                            1f - Mathf.Exp(-Time.deltaTime / rotationSmoothing));

                    _hasOrientedOnce = true;
                }
            }
        }
    }
}
