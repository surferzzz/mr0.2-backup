using UnityEngine;
using UnityEngine.UI;

namespace CPRSimulation
{
    /// <summary>
    /// 世界空间 UI 的「靠太近就淡出」保护。
    ///
    /// 起因是实机反馈：说明面板停在倒地患者的正前方，学员走过去做胸骨压迫时
    /// 面板就横在身体和自己之间，既挡视线又抢手部射线，结果「近付くことができません」。
    ///
    /// **这个组件不会移动面板。** 面板摆在哪儿由调用方在阶段开始时算一次、定死，
    /// 之后位置不再变化 —— 按压过程中面板自己漂移比挡路更让人分神。
    /// 早期版本里有一段「侵入作业区就把面板推开」的硬约束，已经移除：
    /// 摆位本身（身体另一侧、板面与躯干平行）已经保证不重叠，
    /// 再加一个每帧改位置的纠正反而引入了不可预期的移动。
    ///
    /// 这里只剩一件事：学员的头一旦贴到面板上就淡出，并同步关掉 Raycast，
    /// 让它既看不见也点不到；退开后再淡回来。正常摆位下根本不会触发
    /// （俯身按压时头离面板约 0.77m，远在 fadeStartDistance 之外），
    /// 它是留给「患者被搬动 / 学员绕到另一侧」这类意外情况的兜底。
    ///
    /// 量的是「实际画出来的内容」的世界包围盒，不是 Canvas 根的 RectTransform：
    /// TrainingCanvas 根是 1920×1080，而 CPR 阶段真正可见的只有 920×620 的
    /// MergedCPRCoachContainer，按根矩形算会把判定放大一倍以上。
    ///
    /// 挂在世界空间 Canvas 的根物体上。
    /// </summary>
    [DisallowMultipleComponent]
    public class CprWorkspaceUIGuard : MonoBehaviour
    {
        [Header("靠近淡出")]
        [Tooltip("观察者到 UI 内容的距离大于此值 = 完全不透明（米）")]
        public float fadeStartDistance = 0.55f;

        [Tooltip("观察者到 UI 内容的距离小于此值 = 完全透明（米）")]
        public float fadeEndDistance = 0.28f;

        [Tooltip("淡出的最低透明度。0 = 完全消失。")]
        [Range(0f, 1f)] public float minAlpha = 0f;

        [Tooltip("透明度过渡的时间常数（秒），避免在边缘距离上闪烁。")]
        [Range(0f, 1f)] public float fadeSmoothing = 0.12f;

        [Tooltip("透明度低于此值就停止接收手部射线/点击，避免看不见却还挡着点。")]
        [Range(0f, 1f)] public float raycastCutoffAlpha = 0.35f;

        [Header("性能")]
        [Tooltip("重新收集可见图形的间隔（秒）。面板显示/隐藏后靠它跟上。")]
        public float graphicRescanInterval = 0.5f;

        private CanvasGroup _group;
        private GraphicRaycaster _raycaster;
        private bool _raycasterSuppressed;
        private bool _raycasterWasEnabled;
        private Transform _viewer;
        private Graphic[] _graphics;
        private float _nextRescanTime;
        private float _alpha = 1f;

        private readonly Vector3[] _corners = new Vector3[4];

        private void Awake()
        {
            _raycaster = GetComponent<GraphicRaycaster>();

            _group = GetComponent<CanvasGroup>();
            if (_group == null)
                _group = gameObject.AddComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            // 重新启用时立刻按当前距离定一次，不要从上次残留的透明度慢慢爬回来。
            _alpha = 1f;
            ApplyAlpha(1f);
            _nextRescanTime = 0f;
        }

        private void OnDisable()
        {
            // 组件被关掉（训练结束、切结果面板）时必须还原，
            // 否则 UI 会带着淡出后的透明度留在场景里，看起来像「坏了」。
            ApplyAlpha(1f);
        }

        private void LateUpdate()
        {
            Transform viewer = ResolveViewer();
            if (viewer == null)
                return;

            if (!TryGetContentBounds(out Bounds content))
                return;

            float distance = Vector3.Distance(viewer.position, content.ClosestPoint(viewer.position));

            float start = Mathf.Max(fadeStartDistance, fadeEndDistance + 0.01f);
            float target = Mathf.Lerp(minAlpha, 1f,
                Mathf.InverseLerp(fadeEndDistance, start, distance));

            _alpha = fadeSmoothing <= 0f
                ? target
                : Mathf.Lerp(_alpha, target, 1f - Mathf.Exp(-Time.deltaTime / fadeSmoothing));

            ApplyAlpha(_alpha);
        }

        private void ApplyAlpha(float alpha)
        {
            if (_group == null)
                return;

            _group.alpha = alpha;

            bool interactive = alpha >= raycastCutoffAlpha;
            _group.blocksRaycasts = interactive;
            _group.interactable = interactive;

            if (_raycaster == null)
                return;

            // 只允许「关掉」，还原时必须还原成别人设的值，不能无脑置 true。
            //
            // VRUIPointer 会给 Canvas 换上 OVRRaycaster，并**故意**把标准
            // GraphicRaycaster 关掉（见 VRUIPointer 里那段「禁用标准 GraphicRaycaster」）。
            // 早先这里写的是 `_raycaster.enabled = interactive`，
            // 等于在淡回来时把它又打开，两套 raycaster 同时工作 —— 按钮可能被点两次。
            if (!interactive)
            {
                if (!_raycasterSuppressed)
                {
                    _raycasterWasEnabled = _raycaster.enabled;
                    _raycasterSuppressed = true;
                }

                _raycaster.enabled = false;
            }
            else if (_raycasterSuppressed)
            {
                _raycaster.enabled = _raycasterWasEnabled;
                _raycasterSuppressed = false;
            }
        }

        /// <summary>当前真正画出来的内容的世界包围盒。没有可见内容时返回 false。</summary>
        private bool TryGetContentBounds(out Bounds bounds)
        {
            bounds = default;

            if (Time.unscaledTime >= _nextRescanTime || _graphics == null)
            {
                _graphics = GetComponentsInChildren<Graphic>(false);
                _nextRescanTime = Time.unscaledTime + Mathf.Max(0.1f, graphicRescanInterval);
            }

            bool any = false;
            for (int i = 0; i < _graphics.Length; i++)
            {
                Graphic g = _graphics[i];
                if (g == null || !g.isActiveAndEnabled)
                    continue;

                // 全透明的背景图不挡视线，不该把包围盒撑大。
                if (g.color.a <= 0.01f)
                    continue;

                g.rectTransform.GetWorldCorners(_corners);
                for (int c = 0; c < 4; c++)
                {
                    if (!any)
                    {
                        bounds = new Bounds(_corners[c], Vector3.zero);
                        any = true;
                    }
                    else
                    {
                        bounds.Encapsulate(_corners[c]);
                    }
                }
            }

            return any;
        }

        private Transform ResolveViewer()
        {
            if (_viewer != null)
                return _viewer;

            var rig = FindObjectOfType<OVRCameraRig>();
            if (rig != null && rig.centerEyeAnchor != null)
                _viewer = rig.centerEyeAnchor;
            else if (Camera.main != null)
                _viewer = Camera.main.transform;

            return _viewer;
        }
    }
}
