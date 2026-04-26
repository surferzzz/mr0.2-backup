using System;
using UnityEngine;

namespace VRFirstAid.CPR
{
    /// <summary>
    /// 基于手掌世界坐标与胸口基准面的垂直差值，估算按压深度并发事件。
    /// 不依赖力传感器，只需左右手 Transform（XR Hands/控制器 Anchor 均可）。
    /// </summary>
    public class CompressionDetector : MonoBehaviour
    {
        [Header("References")]
        public Transform chestSurface;
        public Transform leftPalm;
        public Transform rightPalm;

        [Header("Tuning (meters)")]
        [Tooltip("低通滤波平滑掌心位置，值越大越平滑。")]
        public float smoothing = 0.15f;

        [Tooltip("无手输入时深度回弹到零的速度。")]
        public float returnSpeed = 1.5f;

        [Header("Detection")]
        [Tooltip("当深度超过该阈值时触发一次 OnCompressionDetected（米）。建议 0.01~0.02。")]
        public float pressThresholdMeters = 0.01f;
        [Tooltip("胸骨按压时允许的水平偏差（米），用于判定位置是否正确。")]
        public float horizontalTolerance = 0.05f;
        [Tooltip("按压区域标记（可选），用于可视化/校准。")]
        public Transform compressionArea;

        private float _restHeight;
        private bool _zeroed;
        private Vector3 _smoothedPalm;
        private bool _wasPressing;
        private bool _detecting;
        private int _compressionCount;
        private float _depthSum;
        private float _avgRateBpm;
        private float _lastCompressionTime = -1f;
        private Vector3 _lastCompressionPos;
        private bool _correctPosition;

        public float CurrentDepthMeters { get; private set; }
        public float CurrentDepthMillimeters => CurrentDepthMeters * 1000f;

        /// <summary>深度每次变化都会回调（米）。</summary>
        public event Action<float> OnCompression;
        /// <summary>深度回到零附近时触发。</summary>
        public event Action OnRelease;
        /// <summary>深度首次跨过 pressThresholdMeters 时触发一次，用于计数。</summary>
        public event Action OnCompressionDetected;

        private void Start()
        {
            if (chestSurface == null)
            {
                Debug.LogWarning("[CompressionDetector] ChestSurface 未指定，无法标定。");
                enabled = false;
                return;
            }

            CalibrateRestHeight();
        }

        private void Update()
        {
            if (!_zeroed || !_detecting) return;

            var hasLeft = leftPalm != null;
            var hasRight = rightPalm != null;

            if (!hasLeft && !hasRight)
            {
                // 没有手输入时逐渐回到静止高度
                CurrentDepthMeters = Mathf.MoveTowards(CurrentDepthMeters, 0f, returnSpeed * Time.deltaTime);
                if (CurrentDepthMeters <= 0.001f)
                {
                    CurrentDepthMeters = 0f;
                    if (_wasPressing)
                    {
                        _wasPressing = false;
                    }
                    OnRelease?.Invoke();
                }
                return;
            }

            var samplePos = Vector3.zero;
            var count = 0;
            if (hasLeft)
            {
                samplePos += leftPalm.position;
                count++;
            }
            if (hasRight)
            {
                samplePos += rightPalm.position;
                count++;
            }
            samplePos /= Mathf.Max(1, count);

            // 低通滤波
            if (_smoothedPalm == Vector3.zero)
                _smoothedPalm = samplePos;
            _smoothedPalm = Vector3.Lerp(_smoothedPalm, samplePos, 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.0001f, smoothing)));

            var depth = Mathf.Max(0f, _restHeight - _smoothedPalm.y);
            var depthChanged = !Mathf.Approximately(depth, CurrentDepthMeters);
            CurrentDepthMeters = depth;

            if (depthChanged)
            {
                if (depth > 0.001f)
                {
                    if (!_wasPressing && depth >= pressThresholdMeters)
                    {
                        _wasPressing = true;
                        OnCompressionDetected?.Invoke();
                        RecordCompression(depth);
                    }
                    OnCompression?.Invoke(CurrentDepthMeters);
                }
                else
                {
                    if (_wasPressing) _wasPressing = false;
                    OnRelease?.Invoke();
                }
            }
        }

        public void CalibrateRestHeight()
        {
            _restHeight = chestSurface.position.y;
            _zeroed = true;
            _smoothedPalm = Vector3.zero;
            CurrentDepthMeters = 0f;
            _compressionCount = 0;
            _depthSum = 0f;
            _avgRateBpm = 0f;
            _lastCompressionTime = -1f;
            _correctPosition = false;
            Debug.Log($"[CompressionDetector] RestHeight set to {_restHeight:F4}");
        }

        public void StartDetection()
        {
            _detecting = true;
            _wasPressing = false;
            CurrentDepthMeters = 0f;
            _compressionCount = 0;
            _depthSum = 0f;
            _avgRateBpm = 0f;
            _lastCompressionTime = -1f;
        }

        public void StopDetection()
        {
            _detecting = false;
            _wasPressing = false;
            CurrentDepthMeters = 0f;
        }

        private void RecordCompression(float depth)
        {
            _compressionCount++;
            _depthSum += depth;
            _lastCompressionPos = _smoothedPalm;

            if (_lastCompressionTime > 0f)
            {
                var interval = Time.time - _lastCompressionTime;
                if (interval > 0.01f)
                {
                    var instRate = 60f / interval;
                    // simple running average
                    _avgRateBpm = (_avgRateBpm * (_compressionCount - 2) + instRate) / Mathf.Max(1, _compressionCount - 1);
                }
            }
            _lastCompressionTime = Time.time;

            // 水平距离判定
            if (chestSurface != null)
            {
                var chestXZ = new Vector2(chestSurface.position.x, chestSurface.position.z);
                var palmXZ = new Vector2(_smoothedPalm.x, _smoothedPalm.z);
                _correctPosition = Vector2.Distance(chestXZ, palmXZ) <= horizontalTolerance;
            }
        }

        public float GetAverageDepth() => _compressionCount > 0 ? _depthSum / _compressionCount : 0f;
        public float GetAverageRate() => _avgRateBpm;
        public float GetCompressionRate() => _avgRateBpm;
        public Vector3 GetCompressionPosition() => _lastCompressionPos;
        public bool IsCorrectPosition() => _correctPosition;
    }
}
