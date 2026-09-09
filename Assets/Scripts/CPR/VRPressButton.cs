using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CPRSimulation
{
    /// <summary>
    /// 按压式按钮：显示按住进度，可选「按满时长才算激活」。
    /// 兼容 OVRInputModule（VR 射线）、鼠标、触摸等 Pointer 事件。
    /// 挂在任意带 Button 的物体上即可生效。
    ///
    /// 激活始终由 Button 自己的 OnPointerClick 完成，本组件不再手动 Invoke onClick
    /// —— 那样会让「按满 holdTime」的一次按压触发两次。
    /// requireHoldToActivate 为 true 时，用暂时关闭 interactable 的方式
    /// 挡掉「没按满就松手」那次点击。
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class VRPressButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [Header("按压设置")]
        [Tooltip("需要按住多长时间才触发点击（秒）")]
        [Range(0.1f, 3f)]
        public float holdTime = 0.6f;

        [Tooltip("可选：用于显示按压进度的 Image。留空则自动生成，见 autoCreateProgressImage。")]
        public Image progressImage;

        [Tooltip("没有指定 progressImage 时自动生成一个覆盖层。\n" +
                 "这个进度指示在 VR 里不是装饰而是必需的 —— 按钮要按住 holdTime 秒才触发，" +
                 "而 VR 里没有触觉点击反馈，没有进度显示的话学员会以为按钮坏了并提前松手。")]
        public bool autoCreateProgressImage = true;

        [Tooltip("自动生成的覆盖层用哪种填充方式。矩形按钮用 Horizontal（横向扫过）读起来最自然。")]
        public Image.FillMethod progressFillMethod = Image.FillMethod.Horizontal;

        [Tooltip("true = 只有按住满 holdTime 才算激活，快速点击无效（VR 里可防止射线误触）。\n" +
                 "false = 保持 Button 原生点击行为，holdTime 只驱动进度显示。\n\n" +
                 "默认 false 的原因：本组件原来会在按满 holdTime 时手动调 onClick，" +
                 "但它没有实现 IPointerClickHandler，Button 自己的点击照样发生 —— " +
                 "Unity 的顺序是 OnPointerDown → OnPointerUp → OnPointerClick，" +
                 "于是「按住满 0.6 秒」会触发两次。实际使用中很少按到 0.6 秒，" +
                 "所以平时只有 Button 那一次、看不出问题。false 保持这个既有表现并去掉重复触发。")]
        public bool requireHoldToActivate = false;

        private Button _button;
        private bool _isPressing;
        private bool _holdSatisfied;
        private bool _restoreInteractable;
        private float _pressTimer;

        private void Awake()
        {
            _button = GetComponent<Button>();

            if (progressImage == null && autoCreateProgressImage)
                progressImage = CreateProgressOverlay();

            if (progressImage != null)
            {
                progressImage.type = Image.Type.Filled;
                progressImage.fillMethod = progressFillMethod;
                progressImage.fillAmount = 0f;
            }
        }

        /// <summary>
        /// 生成一个盖在按钮上的进度覆盖层，不需要额外美术资源 ——
        /// 直接复用按钮自己的精灵，圆角形状天然对得上。
        /// </summary>
        private Image CreateProgressOverlay()
        {
            var go = new GameObject("PressProgress", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;

            var img = go.GetComponent<Image>();
            Image host = GetComponent<Image>();
            img.sprite = host != null ? host.sprite : null;
            img.color = new Color(1f, 1f, 1f, 0.38f);
            img.raycastTarget = false;   // 别把按钮自己的射线挡掉

            // 按钮自身的 Image 是组件、总是先画；子物体按顺序画在其后。
            // 放到最前面 => 盖在按钮图之上、在文字标签之下，不会把文字冲淡。
            go.transform.SetAsFirstSibling();

            return img;
        }

        private void Update()
        {
            if (!_isPressing || _button == null || !_button.interactable)
                return;

            _pressTimer += Time.unscaledDeltaTime;

            if (progressImage != null)
            {
                progressImage.fillAmount = Mathf.Clamp01(_pressTimer / holdTime);
            }

            if (_pressTimer >= holdTime)
            {
                _isPressing = false;
                _holdSatisfied = true;
                if (progressImage != null)
                    progressImage.fillAmount = 1f;

                // 注意：这里不再手动 Invoke onClick。
                // 激活一律交给 Button 自己的 OnPointerClick，避免同一次按压触发两次。
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_button == null || !_button.interactable)
                return;

            _isPressing = true;
            _pressTimer = 0f;
            _holdSatisfied = false;
            if (progressImage != null)
                progressImage.fillAmount = 0f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // Unity 的点击顺序是 OnPointerDown -> OnPointerUp -> OnPointerClick，
            // 所以在这里把 interactable 关掉，就能挡住紧随其后的那次 OnPointerClick。
            // 这是「按不满时长就不算激活」唯一不需要接管 onClick 的做法。
            if (requireHoldToActivate && !_holdSatisfied && _button != null)
            {
                _button.interactable = false;
                _restoreInteractable = true;
            }

            ResetPress();
        }

        private void LateUpdate()
        {
            // 下一帧再恢复，确保这一轮的 OnPointerClick 已经被跳过
            if (_restoreInteractable)
            {
                _restoreInteractable = false;
                if (_button != null)
                    _button.interactable = true;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // 射线或手指移出按钮区域时取消
            ResetPress();
        }

        private void ResetPress()
        {
            _isPressing = false;
            _pressTimer = 0f;
            if (progressImage != null)
                progressImage.fillAmount = 0f;
        }
    }
}

