using UnityEngine;

namespace CPRSimulation
{
    /// <summary>
    /// 使 CPR 反馈 UI（Canvas 或 HUD 根物体）始终位于摄像头前方，随头显移动
    /// 挂到 TrainingCanvas 或 TrainingHUDPanel 的父物体上，并确保 Canvas 为 World Space
    /// </summary>
    public class CPRHUDFollowCamera : MonoBehaviour
    {
        [Header("跟随设置")]
        [Tooltip("UI 与摄像头的距离（米），越小越近")]
        public float distanceInFront = 0.75f;
        [Tooltip("勾选时：UI 放在患者胸部附近（离人物近）；不勾选：UI 在摄像头前方固定距离")]
        public bool placeUINearPatient = true;
        [Tooltip("仅「靠近患者」时有效：胸部参考点与摄像头之间的比例(0~1)，0=贴胸部 1=贴摄像头")]
        [Range(0f, 1f)] public float patientToCameraBlend = 0.4f;
        [Tooltip("始终面向摄像头（可读性更好）")]
        public bool faceCamera = true;
        [Tooltip("仅当此物体激活时跟随（例如仅 CPR 阶段）")]
        public bool onlyWhenActive = true;

        [Header("地面以上")]
        [Tooltip("地面高度（世界坐标 Y），用于限制 UI 不沉入地下")]
        public float groundLevel = 0f;
        [Tooltip("UI 最低高度 = groundLevel + 此值（米），弯腰时 UI 不会低于此高度")]
        public float minHeightAboveGround = 0.8f;

        private Transform _cameraTransform;
        private OVRCameraRig _cameraRig;
        private Canvas _canvas;
        private Transform _patientChestRef;

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
            var detector = GameObject.Find("CPR_ChestDetector");
            _patientChestRef = detector != null ? detector.transform : null;
        }

        private void LateUpdate()
        {
            if (_cameraTransform == null) return;
            if (onlyWhenActive && !gameObject.activeInHierarchy) return;

            if (placeUINearPatient && _patientChestRef == null)
            {
                var go = GameObject.Find("CPR_ChestDetector");
                _patientChestRef = go != null ? go.transform : null;
            }
            Vector3 pos;
            if (placeUINearPatient && _patientChestRef != null)
            {
                Vector3 chestPos = _patientChestRef.position + Vector3.up * 0.15f;
                Vector3 camPos = _cameraTransform.position + _cameraTransform.forward * distanceInFront;
                pos = Vector3.Lerp(chestPos, camPos, patientToCameraBlend);
            }
            else
            {
                pos = _cameraTransform.position + _cameraTransform.forward * distanceInFront;
            }
            float minY = groundLevel + minHeightAboveGround;
            if (pos.y < minY)
                pos.y = minY;
            transform.position = pos;
            if (faceCamera)
                transform.rotation = Quaternion.LookRotation(transform.position - _cameraTransform.position);
        }
    }
}
