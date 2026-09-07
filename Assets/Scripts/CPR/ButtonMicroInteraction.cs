using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

namespace CPRSimulation
{
    /// <summary>
    /// Adds subtle scale feedback to VR UI buttons on hover/press.
    /// Attach to any Button GameObject. Uses IPointerEnter/Exit/Down/Up.
    /// Compatible with OVRInputModule + OVRRaycaster.
    /// </summary>
    public class ButtonMicroInteraction : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Scale")]
        public float hoverScale = 1.08f;
        public float pressScale = 0.95f;
        public float transitionSpeed = 8f;

        [Header("Optional glow")]
        public bool enableGlow = false;
        [Range(0f, 1f)] public float glowAlpha = 0.15f;

        private Vector3 _targetScale = Vector3.one;
        private UnityEngine.UI.Image _image;
        private Color _originalColor;
        private Color _glowColor;
        private Coroutine _scaleRoutine;

        private void Awake()
        {
            _image = GetComponent<UnityEngine.UI.Image>();
            if (_image != null)
                _originalColor = _image.color;

            if (enableGlow)
            {
                _glowColor = _originalColor + new Color(0.15f, 0.15f, 0.1f, 0f);
                _glowColor.a = Mathf.Min(1f, _originalColor.a + glowAlpha);
            }
        }

        private void OnEnable()
        {
            _targetScale = Vector3.one;
            transform.localScale = Vector3.one;
            if (_image != null)
                _image.color = _originalColor;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _targetScale = Vector3.one * hoverScale;
            if (enableGlow && _image != null)
                _image.color = _glowColor;
            if (_scaleRoutine != null) StopCoroutine(_scaleRoutine);
            _scaleRoutine = StartCoroutine(SmoothScale());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _targetScale = Vector3.one;
            if (_image != null)
                _image.color = _originalColor;
            if (_scaleRoutine != null) StopCoroutine(_scaleRoutine);
            _scaleRoutine = StartCoroutine(SmoothScale());
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _targetScale = Vector3.one * pressScale;
            if (_scaleRoutine != null) StopCoroutine(_scaleRoutine);
            _scaleRoutine = StartCoroutine(SmoothScale());
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _targetScale = Vector3.one * hoverScale; // back to hover (still pointing)
        }

        private IEnumerator SmoothScale()
        {
            while (Vector3.Distance(transform.localScale, _targetScale) > 0.001f)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.unscaledDeltaTime * transitionSpeed);
                yield return null;
            }
            transform.localScale = _targetScale;
        }
    }
}
