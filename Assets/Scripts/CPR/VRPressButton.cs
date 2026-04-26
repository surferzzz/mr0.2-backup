using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CPRSimulation
{
    /// <summary>
    /// 按压式按钮：需要“按住”一段时间才触发点击
    /// 兼容 OVRInputModule（VR 射线）、鼠标、触摸等 Pointer 事件。
    /// 挂在任意带 Button 的物体上即可生效。
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class VRPressButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [Header("按压设置")]
        [Tooltip("需要按住多长时间才触发点击（秒）")]
        [Range(0.1f, 3f)]
        public float holdTime = 0.6f;

        [Tooltip("可选：用于显示按压进度的 Image（例如圆形填充条）")]
        public Image progressImage;

        private Button _button;
        private bool _isPressing;
        private float _pressTimer;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (progressImage != null)
            {
                progressImage.type = Image.Type.Filled;
                progressImage.fillMethod = Image.FillMethod.Radial360;
                progressImage.fillAmount = 0f;
            }
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
                if (progressImage != null)
                    progressImage.fillAmount = 1f;

                _button.onClick?.Invoke();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_button == null || !_button.interactable)
                return;

            _isPressing = true;
            _pressTimer = 0f;
            if (progressImage != null)
                progressImage.fillAmount = 0f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            ResetPress();
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

