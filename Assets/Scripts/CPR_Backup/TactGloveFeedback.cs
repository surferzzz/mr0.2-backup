#if BHAPTICS_SDK2
using UnityEngine;
using Bhaptics.SDK2;

namespace VRFirstAid.CPR
{
    /// <summary>
    /// bHaptics TactGlove 触觉反馈：深按、浅按、释放、节拍。
    /// </summary>
    public class TactGloveFeedback : MonoBehaviour
    {
        [Range(1, 100)] public int goodIntensity = 55;
        [Range(1, 100)] public int shallowIntensity = 90;
        [Range(1, 100)] public int releaseIntensity = 30;
        [Tooltip("单次触发时长（毫秒）。")]
        public GlovePlayTime pulsePlayTime = GlovePlayTime.TwentyMS;
        public GloveShapeValue pulseShape = GloveShapeValue.Constant;
        public bool logIfUnavailable = true;

        private readonly int[] _motors = new int[6];
        private readonly int[] _times = new int[6];
        private readonly int[] _shapes = new int[6];

        private void Awake() => FillArrays(goodIntensity);

        public void PlayGood() => PlayBothGloves(goodIntensity);
        public void PlayShallow() => PlayBothGloves(shallowIntensity);
        public void PlayRelease() => PlayBothGloves(releaseIntensity);
        public void PlayMetronomeTick(int intensity) => PlayBothGloves(intensity);

        private void PlayBothGloves(int intensity)
        {
            if (!BhapticsLibrary.IsBhapticsAvailable(true))
            {
                if (logIfUnavailable) Debug.LogWarning("[TactGloveFeedback] bHaptics not available.");
                return;
            }

            FillArrays(intensity);
            BhapticsLibrary.PlayMotors((int)PositionType.GloveL, _motors, (int)pulsePlayTime);
            BhapticsLibrary.PlayMotors((int)PositionType.GloveR, _motors, (int)pulsePlayTime);
        }

        private void FillArrays(int intensity)
        {
            for (int i = 0; i < 6; i++)
            {
                _motors[i] = Mathf.Clamp(intensity, 1, 100);
                _times[i] = (int)pulsePlayTime;
                _shapes[i] = (int)pulseShape;
            }
        }
    }
}
#else
using UnityEngine;

namespace VRFirstAid.CPR
{
    /// <summary>
    /// 无 bHaptics SDK 时的占位实现，保证编译通过并输出警告。
    /// </summary>
    public class TactGloveFeedback : MonoBehaviour
    {
        [Range(1, 100)] public int goodIntensity = 55;
        [Range(1, 100)] public int shallowIntensity = 90;
        [Range(1, 100)] public int releaseIntensity = 30;
        public bool logIfUnavailable = true;

        public void PlayGood() => Warn();
        public void PlayShallow() => Warn();
        public void PlayRelease() => Warn();
        public void PlayMetronomeTick(int intensity) => Warn();

        private void Warn()
        {
            if (logIfUnavailable)
            {
                Debug.LogWarning("[TactGloveFeedback] bHaptics SDK not present; haptic call skipped.");
            }
        }
    }
}
#endif
