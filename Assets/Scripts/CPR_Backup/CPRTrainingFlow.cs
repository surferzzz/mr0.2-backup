using UnityEngine;

namespace VRFirstAid.CPR
{
    /// <summary>
    /// 将按压检测、统计与触觉反馈串起来。
    /// </summary>
    public class CPRTrainingFlow : MonoBehaviour
    {
        [Header("References")]
        public CompressionDetector detector;
        public CompressionStats stats;
        public TactGloveFeedback haptics;

        [Header("Targets")]
        [Tooltip("有效按压最小深度 (mm)。美国心协推荐 50-60mm。")]
        public float minDepthMm = 50f;
        [Tooltip("视为完成释放的深度 (mm)。")]
        public float releaseResetMm = 5f;
        [Tooltip("速率目标下限/上限 (CPM)。")]
        public Vector2 rateTarget = new Vector2(100f, 120f);

        [Header("Shallow提示")]
        public float shallowWarnCooldown = 0.8f;

        private bool _inCompression;
        private float _nextShallowTime;

        private void Reset()
        {
            rateTarget = new Vector2(100f, 120f);
            releaseResetMm = 5f;
            minDepthMm = 50f;
        }

        private void OnEnable()
        {
            if (detector != null)
            {
                detector.OnCompression += HandleCompressionDepth;
                detector.OnCompressionDetected += HandleCompressionDetected;
                detector.OnRelease += HandleRelease;
            }
        }

        private void OnDisable()
        {
            if (detector != null)
            {
                detector.OnCompression -= HandleCompressionDepth;
                detector.OnCompressionDetected -= HandleCompressionDetected;
                detector.OnRelease -= HandleRelease;
            }
        }

        private void HandleCompressionDetected()
        {
            stats?.RegisterCompression(detector.CurrentDepthMeters);
            haptics?.PlayGood();
        }

        private void HandleCompressionDepth(float depthMeters)
        {
            var depthMm = depthMeters * 1000f;

            if (!_inCompression && depthMm >= minDepthMm)
            {
                _inCompression = true;
                // 已在 HandleCompressionDetected 计数与触觉
            }
            else if (!_inCompression && depthMm > 1f && depthMm < minDepthMm)
            {
                if (Time.time >= _nextShallowTime)
                {
                    haptics?.PlayShallow();
                    _nextShallowTime = Time.time + shallowWarnCooldown;
                }
            }
        }

        private void HandleRelease()
        {
            if (_inCompression && detector.CurrentDepthMillimeters <= releaseResetMm)
            {
                _inCompression = false;
                haptics?.PlayRelease();
            }
        }
    }
}
