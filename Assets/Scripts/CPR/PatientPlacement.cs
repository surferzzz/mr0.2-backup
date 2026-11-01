using System;
using UnityEngine;

namespace CPRSimulation
{
    /// <summary>
    /// 训练开始前，让用户把患者挪到自己真实房间里空着的那块地面上。
    ///
    /// 之前患者固定在世界原点、胸部检测区固定在 y=0.25m，用户真实房间里那个位置
    /// 常常是茶几/床沿/椅子腿，结果就是「压不下去」。这里提供一个放置步骤。
    ///
    /// 注意场景里 patient 和 CPR_ChestDetector 是两个平级的根物体，必须一起搬，
    /// 只挪患者会把检测区留在原地。所以运行时给它们套一个共同父节点再整体移动。
    /// </summary>
    public sealed class PatientPlacement : MonoBehaviour
    {
        public const string RigName = "CPR_PlacementRig";

        [Header("可放置范围")]
        [Tooltip("与 NoFallBoundaryClamp 的房间边界保持一致，略收一点留出跪姿空间。")]
        public float minX = -3.5f;
        public float maxX = 3.5f;
        public float minZ = -2.6f;
        public float maxZ = 3.1f;

        [Header("射线")]
        [Tooltip("只有射线明显朝下时才更新落点。指向确认面板时射线接近水平，" +
                 "不设这个阈值的话，一抬手患者就会被甩到面板底下。")]
        public float minDownwardComponent = 0.15f;
        public float maxDistance = 6f;

        [Header("旋转")]
        [Tooltip("放置时用右摇杆左右转患者朝向（度/秒）。")]
        public float rotateSpeedDegPerSec = 70f;

        /// <summary>患者仰卧后的大致占地，用来画地面轮廓（宽 x 长，米）。</summary>
        public Vector2 footprintSize = new Vector2(0.75f, 1.95f);

        public bool IsPlacing { get; private set; }
        public bool HasMoved { get; private set; }

        private Transform _rig;
        private Transform _patient;
        private Transform _detector;
        private GameObject _marker;
        private Renderer[] _markerRenderers;
        private OVRCameraRig _cameraRig;

        private float _groundY;
        private Vector3 _pendingPoint;
        private float _pendingYaw;
        private bool _hasPendingPoint;

        private static readonly Color ValidColor = new Color(0.20f, 0.85f, 0.40f, 0.45f);
        private static readonly Color InvalidColor = new Color(0.90f, 0.25f, 0.25f, 0.45f);

        /// <summary>
        /// 把患者和胸部检测器挂到同一个父节点下。幂等，重复调用不会套娃。
        /// 返回 false 表示场景里没找到患者，调用方应当跳过放置步骤。
        /// </summary>
        public bool EnsureRig(GameObject patientModel, CPRPressDetector pressDetector)
        {
            if (_rig != null)
                return true;

            _patient = patientModel != null ? patientModel.transform : null;
            if (_patient == null)
                return false;

            _detector = pressDetector != null
                ? (pressDetector.chestSurface != null ? pressDetector.chestSurface : pressDetector.transform)
                : null;

            // 检测器如果本来就是患者的子物体，就不用单独搬了。
            if (_detector != null && _detector.IsChildOf(_patient))
                _detector = null;

            var rigGo = new GameObject(RigName);
            _rig = rigGo.transform;

            // 轴心放在患者脚下的地面上：旋转时绕自己转，而不是绕世界原点划一个大圈。
            // 地面取 y=0 —— FlipToSupine() 翻身时会把患者的 y 硬设成 0，
            // 这里跟着用 0 才不会在翻身瞬间产生一个竖直跳变。
            Vector3 origin = _patient.position;
            _groundY = 0f;
            _rig.position = new Vector3(origin.x, _groundY, origin.z);
            _rig.rotation = Quaternion.identity;

            // worldPositionStays = true：保持两者当前的世界位姿和相对关系
            _patient.SetParent(_rig, true);
            if (_detector != null)
                _detector.SetParent(_rig, true);

            Debug.Log($"[PatientPlacement] 放置父节点已建立于 {_rig.position}，" +
                      $"包含患者「{_patient.name}」" +
                      (_detector != null ? $" 和检测器「{_detector.name}」" : "（检测器是患者子物体，无需单独搬运）"));
            return true;
        }

        public void BeginPlacement()
        {
            if (_rig == null)
                return;

            IsPlacing = true;
            _hasPendingPoint = false;
            _pendingPoint = _rig.position;
            _pendingYaw = _rig.eulerAngles.y;

            EnsureMarker();
            _marker.SetActive(true);
            UpdateMarkerTransform(_pendingPoint, _pendingYaw, IsInsideBounds(_pendingPoint));
        }

        /// <summary>确认当前落点。没指到过地面就保持原位，不做任何移动。</summary>
        public void ConfirmPlacement()
        {
            if (!IsPlacing)
                return;

            if (_hasPendingPoint && _rig != null)
            {
                Vector3 target = ClampToBounds(_pendingPoint);
                target.y = _groundY;
                _rig.SetPositionAndRotation(target, Quaternion.Euler(0f, _pendingYaw, 0f));
                HasMoved = true;
                Debug.Log($"[PatientPlacement] 患者已移动到 {target}，朝向 {_pendingYaw:F0}°");
            }

            EndPlacement();
        }

        public void CancelPlacement()
        {
            if (!IsPlacing)
                return;

            EndPlacement();
        }

        private void EndPlacement()
        {
            IsPlacing = false;
            if (_marker != null)
                _marker.SetActive(false);

            OnPlacementFinished?.Invoke();
        }

        public event Action OnPlacementFinished;

        private void Update()
        {
            if (!IsPlacing)
                return;

            HandleRotationInput();

            if (TryGetFloorPoint(out Vector3 point))
            {
                _pendingPoint = point;
                _hasPendingPoint = true;
            }

            UpdateMarkerTransform(_hasPendingPoint ? _pendingPoint : _rig.position,
                                  _pendingYaw,
                                  !_hasPendingPoint || IsInsideBounds(_pendingPoint));
        }

        private void HandleRotationInput()
        {
            float x = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).x;
            if (Mathf.Abs(x) < 0.2f)
                x = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).x;
            if (Mathf.Abs(x) < 0.2f)
                return;

            _pendingYaw += x * rotateSpeedDegPerSec * Time.deltaTime;
        }

        /// <summary>
        /// 用右手/右手柄的射线和地面平面求交。不走物理射线：地面有没有碰撞体、
        /// 碰撞体在哪一层都不确定，直接和 y = _groundY 的水平面求交更稳。
        /// </summary>
        private bool TryGetFloorPoint(out Vector3 point)
        {
            point = Vector3.zero;

            Transform origin = ResolvePointerOrigin();
            if (origin == null)
                return false;

            Vector3 dir = origin.forward;
            // 射线接近水平时不更新：那通常是用户在看确认面板，不是在指地面。
            if (-dir.y < minDownwardComponent)
                return false;

            float distance = (origin.position.y - _groundY) / -dir.y;
            if (distance <= 0.2f || distance > maxDistance)
                return false;

            point = origin.position + dir * distance;
            point.y = _groundY;
            return true;
        }

        /// <summary>
        /// 和 VRUIPointer 同一套判断：手柄开机用手柄锚点，放下手柄自动回到手部锚点。
        /// </summary>
        private Transform ResolvePointerOrigin()
        {
            if (_cameraRig == null)
                _cameraRig = FindObjectOfType<OVRCameraRig>();
            if (_cameraRig == null)
                return Camera.main != null ? Camera.main.transform : null;

            bool handConnected = OVRInput.IsControllerConnected(OVRInput.Controller.RHand);
            bool controllerConnected = OVRInput.IsControllerConnected(OVRInput.Controller.RTouch);

            if ((handConnected || !controllerConnected) && _cameraRig.rightHandAnchor != null)
                return _cameraRig.rightHandAnchor;
            if (_cameraRig.rightControllerAnchor != null)
                return _cameraRig.rightControllerAnchor;
            return _cameraRig.rightHandAnchor != null ? _cameraRig.rightHandAnchor : _cameraRig.centerEyeAnchor;
        }

        private bool IsInsideBounds(Vector3 p)
        {
            return p.x >= minX && p.x <= maxX && p.z >= minZ && p.z <= maxZ;
        }

        private Vector3 ClampToBounds(Vector3 p)
        {
            p.x = Mathf.Clamp(p.x, minX, maxX);
            p.z = Mathf.Clamp(p.z, minZ, maxZ);
            return p;
        }

        private void UpdateMarkerTransform(Vector3 position, float yaw, bool valid)
        {
            if (_marker == null)
                return;

            position.y = _groundY + 0.01f;   // 抬高 1cm，避免和地板 z-fighting
            _marker.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));

            Color c = valid ? ValidColor : InvalidColor;
            if (_markerRenderers != null)
            {
                foreach (var r in _markerRenderers)
                {
                    if (r != null && r.material != null)
                        r.material.color = c;
                }
            }
        }

        private void EnsureMarker()
        {
            if (_marker != null)
                return;

            _marker = new GameObject("CPR_PlacementMarker");

            // 躺下后的占地轮廓：让用户一眼看出需要多大一块空地。
            GameObject body = CreateFlatQuad("Footprint", _marker.transform,
                                             Vector3.zero, new Vector3(footprintSize.x, footprintSize.y, 1f));

            // 头部一侧的小标记，说明患者会朝哪个方向躺。
            CreateFlatQuad("HeadEnd", _marker.transform,
                           new Vector3(0f, 0.002f, footprintSize.y * 0.5f - 0.12f),
                           new Vector3(footprintSize.x * 0.55f, 0.22f, 1f));

            // 胸口位置（按压点）标一个更亮的小方块。
            CreateFlatQuad("ChestSpot", _marker.transform,
                           new Vector3(0f, 0.004f, footprintSize.y * 0.5f - 0.42f),
                           new Vector3(0.22f, 0.22f, 1f));

            _markerRenderers = _marker.GetComponentsInChildren<Renderer>(true);
            _marker.SetActive(false);
        }

        private static GameObject CreateFlatQuad(string name, Transform parent, Vector3 localPos, Vector3 localScale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);  // 平铺到地面
            go.transform.localScale = localScale;

            var col = go.GetComponent<Collider>();
            if (col != null)
                Destroy(col);

            var renderer = go.GetComponent<Renderer>();
            // 和 VRUIPointer 的激光用同一个 shader：项目在 URP 下已经验证可用。
            renderer.material = new Material(Shader.Find("Sprites/Default")) { color = ValidColor };
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            return go;
        }

        private void OnDestroy()
        {
            if (_marker != null)
                Destroy(_marker);
        }
    }
}
