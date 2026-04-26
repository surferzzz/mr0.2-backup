using UnityEngine;

namespace VRFirstAid.CPR
{
    /// <summary>
    /// 播放节拍器（音频 + 触觉）。把它挂到场景里的空物体上，赋 AudioSource。
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class MetronomeTick : MonoBehaviour
    {
        [Tooltip("节拍速度 BPM，心肺复苏推荐 100-120。")]
        public float bpm = 110f;
        public bool autoStart = true;
        public TactGloveFeedback haptics;
        public int hapticIntensity = 40;

        private AudioSource _audio;
        private float _nextTick;
        private bool _running;

        private void Awake()
        {
            _audio = GetComponent<AudioSource>();
            _audio.loop = false;
        }

        private void OnEnable()
        {
            if (autoStart) StartMetronome();
        }

        public void StartMetronome()
        {
            _running = true;
            _nextTick = Time.time;
        }

        public void StopMetronome()
        {
            _running = false;
        }

        private void Update()
        {
            if (!_running || bpm <= 0f) return;

            if (Time.time >= _nextTick)
            {
                _audio.Play();
                haptics?.PlayMetronomeTick(hapticIntensity);
                _nextTick = Time.time + 60f / bpm;
            }
        }
    }
}
