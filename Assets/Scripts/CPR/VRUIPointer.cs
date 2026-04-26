using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CPRSimulation
{
    /// <summary>
    /// VR 手部/控制器 UI 射线交互 - 支持手柄扳机与手部捏合点击
    /// 射线可从右手或右手柄发出，点击为扳机键或食指捏合（Pinch）
    /// 编辑器下可用空格键或鼠标左键模拟点击（解决无设备/ACTIONSET 报错时无法交互）
    /// </summary>
    public class VRUIPointer : MonoBehaviour
    {
        [Header("设置")]
        [Tooltip("射线最大长度")]
        public float rayLength = 10f;

        [Tooltip("射线颜色")]
        public Color laserColor = new Color(0.3f, 0.7f, 1f, 0.8f);

        [Tooltip("射线命中UI时的颜色")]
        public Color laserHitColor = new Color(0.2f, 1f, 0.3f, 0.9f);

        [Tooltip("射线宽度")]
        public float laserWidth = 0.005f;

        [Tooltip("光标大小")]
        public float cursorSize = 0.02f;

        [Header("手部/控制器")]
        [Tooltip("优先使用手部追踪（射线从右手发出）；否则使用右手柄")]
        public bool preferHandTracking = true;

        [Tooltip("仅在手势捏合时显示射线（捏射线）；推荐开启，捏一下出射线")]
        public bool showRayOnlyWhenPinching = true;

        [Tooltip("用右手食指+拇指捏合触发点击（优先于扳机）；需手部追踪")]
        public bool useHandPinchForClick = true;

        [Tooltip("捏合/扳机键：无手部捏合时用此键（食指捏合或右手扳机）")]
        public OVRInput.Button clickButton = OVRInput.Button.SecondaryIndexTrigger;

        [Header("编辑器回退")]
        [Tooltip("在 Editor 下用空格/鼠标左键模拟点击（无设备或 XR 报错时仍可点 UI）")]
        public bool editorFallbackClick = true;

        private LineRenderer _laser;
        private GameObject _cursor;
        private OVRInputModule _inputModule;
        private Transform _pointerOrigin;
        private OVRCameraRig _cameraRig;
        private OVRHand _rightHand;
        private bool _wasPinching;

        private void Start()
        {
            _cameraRig = FindObjectOfType<OVRCameraRig>();
            if (_cameraRig == null)
            {
                Debug.LogError("[VRUIPointer] OVRCameraRig not found!");
                enabled = false;
                return;
            }

            UpdatePointerOrigin();
            if (_pointerOrigin == null)
            {
                Debug.LogError("[VRUIPointer] No right hand or controller anchor found!");
                enabled = false;
                return;
            }

            SetupOVRInputModule();
            SetupLaser();
            SetupCursor();
            SetupCanvasForVR();
            FindRightHand();
        }

        private void FindRightHand()
        {
            if (_cameraRig?.rightHandAnchor != null)
                _rightHand = _cameraRig.rightHandAnchor.GetComponentInChildren<OVRHand>(true);
            if (_rightHand == null)
            {
                var hands = FindObjectsOfType<OVRHand>();
                foreach (var h in hands)
                {
                    var skel = h.GetComponent<OVRSkeleton>();
                    if (skel != null && (skel.GetSkeletonType() == OVRSkeleton.SkeletonType.HandRight || skel.GetSkeletonType() == OVRSkeleton.SkeletonType.XRHandRight))
                    {
                        _rightHand = h;
                        break;
                    }
                }
            }
        }

        private void UpdatePointerOrigin()
        {
            bool handConnected = OVRInput.IsControllerConnected(OVRInput.Controller.RHand);
            bool controllerConnected = OVRInput.IsControllerConnected(OVRInput.Controller.RTouch);

            if (preferHandTracking && _cameraRig.rightHandAnchor != null && (handConnected || !controllerConnected))
                _pointerOrigin = _cameraRig.rightHandAnchor;
            else if (_cameraRig.rightControllerAnchor != null)
                _pointerOrigin = _cameraRig.rightControllerAnchor;
            else
                _pointerOrigin = _cameraRig.rightHandAnchor;
        }

        private void SetupOVRInputModule()
        {
            _inputModule = FindObjectOfType<OVRInputModule>();
            if (_inputModule == null)
            {
                var eventSystem = FindObjectOfType<EventSystem>();
                if (eventSystem != null)
                {
                    _inputModule = eventSystem.gameObject.AddComponent<OVRInputModule>();
                    var standalone = eventSystem.GetComponent<StandaloneInputModule>();
                    if (standalone != null)
                        standalone.enabled = false;
                }
            }

            if (_inputModule != null)
            {
                _inputModule.rayTransform = _pointerOrigin;
                _inputModule.joyPadClickButton = clickButton;
            }
        }

        private void SetupLaser()
        {
            var laserObj = new GameObject("LaserPointer");
            laserObj.transform.SetParent(_pointerOrigin, false);

            _laser = laserObj.AddComponent<LineRenderer>();
            _laser.positionCount = 2;
            _laser.startWidth = laserWidth;
            _laser.endWidth = laserWidth * 0.5f;
            _laser.useWorldSpace = true;

            // 创建激光材质
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = laserColor;
            _laser.material = mat;
            _laser.startColor = laserColor;
            _laser.endColor = new Color(laserColor.r, laserColor.g, laserColor.b, 0.2f);
        }

        private void SetupCursor()
        {
            _cursor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _cursor.name = "VRCursor";
            _cursor.transform.localScale = Vector3.one * cursorSize;

            // 移除碰撞体
            var col = _cursor.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // 设置材质
            var renderer = _cursor.GetComponent<Renderer>();
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = laserHitColor;
            renderer.material = mat;

            _cursor.SetActive(false);
        }

        private void SetupCanvasForVR()
        {
            // 找到所有 WorldSpace Canvas 并配置
            var canvases = FindObjectsOfType<Canvas>();
            foreach (var canvas in canvases)
            {
                if (canvas.renderMode == RenderMode.WorldSpace)
                {
                    // 设置 worldCamera 为 CenterEyeAnchor 的相机
                    if (canvas.worldCamera == null && _cameraRig.centerEyeAnchor != null)
                    {
                        var cam = _cameraRig.centerEyeAnchor.GetComponent<Camera>();
                        if (cam != null)
                        {
                            canvas.worldCamera = cam;
                        }
                    }

                    // 添加 OVRRaycaster（如果没有的话）
                    if (canvas.GetComponent<OVRRaycaster>() == null)
                    {
                        var ovrRaycaster = canvas.gameObject.AddComponent<OVRRaycaster>();
                        ovrRaycaster.pointer = _laser.gameObject;

                        // 禁用标准 GraphicRaycaster，用 OVRRaycaster 代替
                        var graphicRaycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
                        if (graphicRaycaster != null)
                            graphicRaycaster.enabled = false;
                    }
                }
            }
        }

        private void Update()
        {
            if (_pointerOrigin == null || _laser == null) return;

            UpdatePointerOrigin();
            if (_inputModule != null && _inputModule.rayTransform != _pointerOrigin)
                _inputModule.rayTransform = _pointerOrigin;

            bool pinchOrTrigger = OVRInput.Get(clickButton);
            bool showRay = !showRayOnlyWhenPinching || pinchOrTrigger;
            if (showRayOnlyWhenPinching && !showRay)
            {
                _laser.enabled = false;
                if (_cursor != null) _cursor.SetActive(false);
                HandleTriggerClick();
                return;
            }

            bool handConnected = OVRInput.IsControllerConnected(OVRInput.Controller.RHand);
            bool controllerConnected = OVRInput.IsControllerConnected(OVRInput.Controller.RTouch);
            _laser.enabled = showRay && (handConnected || controllerConnected);

#if UNITY_EDITOR
            if (_laser.enabled == false && _pointerOrigin != null)
                _laser.enabled = showRay;
#endif

            UpdateLaser();
            HandlePinchClick();
            HandleTriggerClick();
#if UNITY_EDITOR
            EditorFallbackClick();
#endif
        }

        private void HandlePinchClick()
        {
            if (!useHandPinchForClick) return;
            if (_rightHand == null)
            {
                if (Time.frameCount % 60 == 0) FindRightHand();
                return;
            }
            if (!_rightHand.IsDataValid) return;
            bool pinching = _rightHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
            if (pinching && !_wasPinching)
                TryClickUnderPointer();
            _wasPinching = pinching;
        }

        private void TryClickUnderPointer()
        {
            if (EventSystem.current == null || _pointerOrigin == null) return;
            var ray = new Ray(_pointerOrigin.position, _pointerOrigin.forward);
            var pointerData = new OVRPointerEventData(EventSystem.current)
            {
                worldSpaceRay = ray,
                position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f),
                pressPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)
            };
            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);
            if (results.Count == 0) return;
            var go = results[0].gameObject;
            var button = go.GetComponent<Button>() ?? go.GetComponentInParent<Button>();
            if (button != null && button.enabled && button.interactable)
                button.onClick.Invoke();
            else
                ExecuteEvents.Execute(go, pointerData, ExecuteEvents.pointerClickHandler);
        }

        private void UpdateLaser()
        {
            if (!_laser.enabled) return;

            Vector3 origin = _pointerOrigin.position;
            Vector3 direction = _pointerOrigin.forward;

            bool hitUI = false;
            Vector3 endPoint = origin + direction * rayLength;

            RaycastHit hit;
            if (Physics.Raycast(origin, direction, out hit, rayLength))
            {
                endPoint = hit.point;
                hitUI = true;
            }

            _laser.SetPosition(0, origin);
            _laser.SetPosition(1, endPoint);

            Color currentColor = hitUI ? laserHitColor : laserColor;
            _laser.startColor = currentColor;
            _laser.endColor = new Color(currentColor.r, currentColor.g, currentColor.b, 0.2f);

            if (_cursor != null)
            {
                if (hitUI)
                {
                    _cursor.SetActive(true);
                    _cursor.transform.position = endPoint;
                    _cursor.transform.LookAt(origin);
                }
                else
                {
                    _cursor.SetActive(false);
                }
            }
        }

        private void HandleTriggerClick()
        {
            // 点击由 OVRInputModule 根据 joyPadClickButton（捏合/扳机）自动处理
        }

#if UNITY_EDITOR
        private void EditorFallbackClick()
        {
            if (!editorFallbackClick || EventSystem.current == null) return;
            if (!Input.GetKeyDown(KeyCode.Space) && !Input.GetMouseButtonDown(0)) return;

            var pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition,
                pressPosition = Input.mousePosition
            };
            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);
            if (results.Count == 0) return;

            var first = results[0].gameObject;
            var button = first.GetComponent<Button>() ?? first.GetComponentInParent<Button>();
            if (button != null && button.enabled && button.interactable)
                button.onClick.Invoke();
            else
                ExecuteEvents.Execute(first, pointerData, ExecuteEvents.pointerClickHandler);
        }
#endif

        private void OnDestroy()
        {
            if (_cursor != null)
                Destroy(_cursor);
        }
    }
}
