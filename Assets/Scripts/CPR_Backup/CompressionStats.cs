using System.Collections.Generic;
using UnityEngine;

namespace VRFirstAid.CPR
{
    /// <summary>
    /// 维护按压频率、深度统计。队列窗口默认10秒。
    /// </summary>
    public class CompressionStats : MonoBehaviour
    {
        [Tooltip("统计窗口秒数。")]
        public float windowSeconds = 10f;

        private readonly Queue<float> _times = new Queue<float>();
        private readonly Queue<float> _depths = new Queue<float>();

        public float CurrentCpm { get; private set; }
        public float AverageDepthMm { get; private set; }

        public void RegisterCompression(float depthMeters)
        {
            var now = Time.time;
            _times.Enqueue(now);
            _depths.Enqueue(depthMeters * 1000f);
            TrimOld(now);
            Recompute(now);
        }

        public void ClearAll()
        {
            _times.Clear();
            _depths.Clear();
            CurrentCpm = 0f;
            AverageDepthMm = 0f;
        }

        private void Update()
        {
            // 让窗口自然衰减
            if (_times.Count > 0)
            {
                var now = Time.time;
                TrimOld(now);
                Recompute(now);
            }
        }

        private void TrimOld(float now)
        {
            while (_times.Count > 0 && now - _times.Peek() > windowSeconds)
            {
                _times.Dequeue();
                _depths.Dequeue();
            }
        }

        private void Recompute(float now)
        {
            if (_times.Count == 0)
            {
                CurrentCpm = 0;
                AverageDepthMm = 0;
                return;
            }

            var elapsed = Mathf.Max(0.0001f, Mathf.Min(windowSeconds, now - _times.Peek()));
            CurrentCpm = _times.Count * (60f / elapsed);

            float sum = 0f;
            foreach (var d in _depths) sum += d;
            AverageDepthMm = sum / _depths.Count;
        }
    }
}
