using UnityEngine;
using System.Collections;

namespace CPRSimulation
{
    /// <summary>
    /// Adds smooth show/hide transitions to UI panels.
    /// Attach to each panel GameObject. Uses CanvasGroup for fade + scale pop.
    /// Works with both SetActive and programmatic Show()/Hide().
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIPanelAnimator : MonoBehaviour
    {
        [Header("Animation")]
        public float duration = 0.3f;
        public AnimationStyle showStyle = AnimationStyle.PopIn;
        public AnimationStyle hideStyle = AnimationStyle.FadeOut;

        public enum AnimationStyle
        {
            None,
            FadeIn,
            FadeOut,
            PopIn,      // scale 0.8→1.0 + fade, easeOutBack
            PopOut,     // scale 1.0→0.8 + fade, easeInBack
            SlideUp,    // local Y +30 → 0 + fade
            SlideDown   // local Y 0 → -30 + fade
        }

        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;
        private Vector3 _originalLocalPos;
        private Coroutine _currentAnim;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _rectTransform = GetComponent<RectTransform>();
            _originalLocalPos = _rectTransform != null ? _rectTransform.localPosition : Vector3.zero;
        }

        private void OnEnable()
        {
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            if (showStyle != AnimationStyle.None)
                PlayShow();
        }

        public void PlayShow()
        {
            if (_currentAnim != null) StopCoroutine(_currentAnim);
            _currentAnim = StartCoroutine(AnimateIn());
        }

        public void PlayHide()
        {
            if (_currentAnim != null) StopCoroutine(_currentAnim);
            _currentAnim = StartCoroutine(AnimateOut());
        }

        public void PlayHideAndDeactivate()
        {
            if (_currentAnim != null) StopCoroutine(_currentAnim);
            _currentAnim = StartCoroutine(AnimateOutAndDeactivate());
        }

        private IEnumerator AnimateIn()
        {
            gameObject.SetActive(true);
            _canvasGroup.alpha = 0f;

            if (_rectTransform != null)
            {
                _rectTransform.localPosition = _originalLocalPos;
                if (showStyle == AnimationStyle.PopIn)
                    _rectTransform.localScale = Vector3.one * 0.8f;
                else if (showStyle == AnimationStyle.SlideUp)
                    _rectTransform.localPosition = _originalLocalPos + Vector3.up * 30f;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                _canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

                if (_rectTransform != null)
                {
                    if (showStyle == AnimationStyle.PopIn)
                    {
                        float s = EaseOutBack(0.8f, 1f, t);
                        _rectTransform.localScale = Vector3.one * s;
                    }
                    else if (showStyle == AnimationStyle.SlideUp)
                    {
                        float y = Mathf.Lerp(30f, 0f, EaseOutCubic(t));
                        _rectTransform.localPosition = _originalLocalPos + Vector3.up * y;
                    }
                }

                yield return null;
            }

            _canvasGroup.alpha = 1f;
            if (_rectTransform != null)
            {
                _rectTransform.localScale = Vector3.one;
                _rectTransform.localPosition = _originalLocalPos;
            }
        }

        private IEnumerator AnimateOut()
        {
            float elapsed = 0f;
            Vector3 startScale = _rectTransform != null ? _rectTransform.localScale : Vector3.one;
            Vector3 startPos = _rectTransform != null ? _rectTransform.localPosition : Vector3.zero;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                _canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

                if (_rectTransform != null)
                {
                    if (hideStyle == AnimationStyle.PopOut)
                    {
                        float s = Mathf.Lerp(1f, 0.8f, EaseInBack(t));
                        _rectTransform.localScale = startScale * s;
                    }
                    else if (hideStyle == AnimationStyle.SlideDown)
                    {
                        float y = Mathf.Lerp(0f, -30f, EaseInCubic(t));
                        _rectTransform.localPosition = startPos + Vector3.up * y;
                    }
                }

                yield return null;
            }

            _canvasGroup.alpha = 0f;
        }

        private IEnumerator AnimateOutAndDeactivate()
        {
            yield return AnimateOut();
            gameObject.SetActive(false);
        }

        // ── Easing helpers ──
        private static float EaseOutBack(float from, float to, float t)
        {
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            return from + (to - from) * (1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f));
        }

        private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
        private static float EaseInCubic(float t) => t * t * t;
        private static float EaseInBack(float t)
        {
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            return c3 * t * t * t - c1 * t * t;
        }
    }
}
