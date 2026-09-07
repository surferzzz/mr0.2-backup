using UnityEngine;

namespace CPRSimulation
{
    /// <summary>
    /// 用代码合成提示音。项目里 Assets/Audio 是空的、没有任何音频素材，
    /// 所以直接生成 PCM，避免训练全程无声，也不用为了几个提示音引入外部资源。
    /// </summary>
    public static class ProceduralAudio
    {
        public const int SampleRate = 44100;

        /// <summary>短促的「嗒」声：正弦波 + 指数衰减。</summary>
        public static AudioClip CreateClick(string name, float frequency, float durationSec,
                                            float decay = 55f, float volume = 0.8f)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(SampleRate * durationSec));
            var data = new float[count];
            FillTone(data, 0, count, frequency, decay, volume);
            return Build(name, data);
        }

        /// <summary>
        /// 一个完整节拍周期：开头一声 click，后面补静音，总长正好 60/bpm 秒。
        /// AudioSource 设成 loop 后由音频引擎按采样驱动，时序是采样级精确的，
        /// 既不受帧率抖动影响，也不需要每帧做任何计时工作。
        /// </summary>
        public static AudioClip CreateMetronomeLoop(float bpm, float frequency = 1400f,
                                                    float clickSeconds = 0.045f, float volume = 0.85f)
        {
            bpm = Mathf.Clamp(bpm, 30f, 240f);
            int period = Mathf.Max(2, Mathf.RoundToInt(SampleRate * 60f / bpm));
            int click = Mathf.Min(period, Mathf.Max(1, Mathf.RoundToInt(SampleRate * clickSeconds)));

            var data = new float[period];   // 其余样本保持 0，即静音
            FillTone(data, 0, click, frequency, 55f, volume);
            return Build($"CPR_Metronome_{bpm:F0}bpm", data);
        }

        /// <summary>依次播放若干音级，用于「开始」「完成」这类事件提示。</summary>
        public static AudioClip CreateSequence(string name, float[] frequencies,
                                               float noteSeconds = 0.13f, float volume = 0.5f)
        {
            if (frequencies == null || frequencies.Length == 0)
                return CreateClick(name, 880f, 0.1f);

            int per = Mathf.Max(1, Mathf.RoundToInt(SampleRate * noteSeconds));
            var data = new float[per * frequencies.Length];
            for (int n = 0; n < frequencies.Length; n++)
                FillTone(data, n * per, per, frequencies[n], 11f, volume);
            return Build(name, data);
        }

        private static void FillTone(float[] data, int offset, int count,
                                     float frequency, float decay, float volume)
        {
            // 起音处直接从 0 跳到峰值会有爆音，用约 1.5ms 淡入抹掉。
            const int fadeInSamples = 66;

            for (int i = 0; i < count && offset + i < data.Length; i++)
            {
                float t = i / (float)SampleRate;
                float envelope = Mathf.Exp(-decay * t);
                if (i < fadeInSamples)
                    envelope *= i / (float)fadeInSamples;

                float sample = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * volume;
                data[offset + i] = Mathf.Clamp(sample, -1f, 1f);
            }
        }

        private static AudioClip Build(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
