using UnityEngine;

namespace CPRSimulation
{
    /// <summary>
    /// CPR 按压节拍器。指南推荐 100–120 次/分，默认取中值 110。
    ///
    /// 在此之前节奏引导只有两条：TactGlove 的震动，和 HUD 上的视觉节拍。
    /// 没戴手套的学员完全没有节奏参考 —— 而听觉节拍才是 CPR 教学的标准做法。
    ///
    /// 实现上不使用协程或 Update 计时：循环片段的长度本身就是一个节拍周期，
    /// 交给音频引擎循环播放，时序精确且每帧零开销。
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    [AddComponentMenu("CPR/CPR Metronome")]
    public class CPRMetronome : MonoBehaviour
    {
        [Tooltip("目标按压频率（次/分）。指南推荐 100-120，默认取中值。")]
        [Range(60f, 140f)]
        public float bpm = 110f;

        [Range(0f, 1f)]
        public float volume = 0.55f;

        [Tooltip("节拍音频率(Hz)。1200-1800 在头显扬声器上比较穿透。")]
        [Range(400f, 3000f)]
        public float clickFrequency = 1400f;

        private AudioSource _source;
        private float _builtBpm = -1f;
        private float _builtFrequency = -1f;

        public bool IsRunning => _source != null && _source.isPlaying;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = true;
            // 2D：节拍是教学引导音，不应随头部朝向或距离衰减。
            _source.spatialBlend = 0f;
            _source.volume = volume;
            Rebuild();
        }

        /// <summary>由 CPRTrainingController.ChangePhase 驱动，进出按压阶段自动起停。</summary>
        public void SetRunning(bool run)
        {
            if (_source == null)
                return;

            if (!run)
            {
                if (_source.isPlaying)
                    _source.Stop();
                return;
            }

            Rebuild();
            _source.volume = volume;
            if (!_source.isPlaying)
                _source.Play();
        }

        /// <summary>bpm 或音色变了才重新生成循环片段，其长度决定节拍间隔。</summary>
        private void Rebuild()
        {
            if (_source == null)
                return;

            if (_source.clip != null
                && Mathf.Approximately(_builtBpm, bpm)
                && Mathf.Approximately(_builtFrequency, clickFrequency))
            {
                return;
            }

            bool wasPlaying = _source.isPlaying;
            _source.Stop();
            _source.clip = ProceduralAudio.CreateMetronomeLoop(bpm, clickFrequency);
            _builtBpm = bpm;
            _builtFrequency = clickFrequency;

            if (wasPlaying)
                _source.Play();
        }

        private void OnValidate()
        {
            // 允许在 Play 模式下拖 Inspector 实时试听不同节奏。
            if (Application.isPlaying)
                Rebuild();
        }
    }
}
