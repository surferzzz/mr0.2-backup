using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace CPRSimulation
{
    /// <summary>
    /// 意识确认时「有没有出声呼唤」的检测。
    ///
    /// **只记录，不作为通过条件。** 麦克风能判的仅仅是「有没有达到一定响度的声音」，
    /// 判不了说了什么；教室里多台设备同时训练还会互相串音。
    /// 拿它卡关会造出大量假阳性和假阴性，所以结果只进成绩单，不挡流程。
    ///
    /// 麦克风不可用（权限被拒、设备没有录音输入、被别的程序占用）时
    /// MicAvailable 为 false，整套逻辑静默退化 —— 学员照常靠拍肩通过。
    /// </summary>
    public class ResponseCallVoiceMonitor : MonoBehaviour
    {
        [Tooltip("判定为「喊了」的 RMS 响度阈值。0.06 约相当于正常说话音量以上。")]
        [Range(0.01f, 0.5f)] public float rmsThreshold = 0.06f;

        [Tooltip("响度需要持续多久才算数（秒）。防止一次咳嗽或碰撞噪声就算通过。")]
        public float requiredSeconds = 0.25f;

        [Tooltip("采样率。16kHz 足够算响度，比 44.1kHz 省。")]
        public int sampleRate = 16000;

        /// <summary>本次确认过程中是否检测到有效呼唤。</summary>
        public bool HeardCall { get; private set; }

        /// <summary>麦克风是否可用。false 时本组件不产生任何判定。</summary>
        public bool MicAvailable { get; private set; }

        /// <summary>当前响度，0-1，给 UI 做电平指示用。</summary>
        public float CurrentLevel { get; private set; }

        private const int ClipLengthSeconds = 1;
        private const int SampleWindow = 512;

        private AudioClip _clip;
        private string _device;
        private bool _listening;
        private float _loudSeconds;
        private float[] _samples;

        public void BeginListening()
        {
            HeardCall = false;
            CurrentLevel = 0f;
            _loudSeconds = 0f;

            if (_listening)
                return;

            if (!HasMicPermission())
            {
                RequestMicPermission();
                MicAvailable = false;
                Debug.Log("[ResponseVoice] 麦克风权限未授予，本次只按拍肩判定");
                return;
            }

            if (Microphone.devices == null || Microphone.devices.Length == 0)
            {
                MicAvailable = false;
                Debug.Log("[ResponseVoice] 没有可用的录音设备，本次只按拍肩判定");
                return;
            }

            _device = Microphone.devices[0];
            _clip = Microphone.Start(_device, true, ClipLengthSeconds, sampleRate);

            if (_clip == null)
            {
                MicAvailable = false;
                Debug.LogWarning("[ResponseVoice] Microphone.Start 失败，本次只按拍肩判定");
                return;
            }

            if (_samples == null || _samples.Length != SampleWindow)
                _samples = new float[SampleWindow];

            MicAvailable = true;
            _listening = true;
        }

        public void StopListening()
        {
            if (!_listening)
                return;

            _listening = false;

            if (!string.IsNullOrEmpty(_device))
                Microphone.End(_device);

            _clip = null;
            CurrentLevel = 0f;
        }

        private void OnDisable()
        {
            StopListening();
        }

        private void Update()
        {
            if (!_listening || _clip == null)
                return;

            int position = Microphone.GetPosition(_device) - SampleWindow;
            if (position < 0)
                return;

            if (!_clip.GetData(_samples, position))
                return;

            float sum = 0f;
            for (int i = 0; i < _samples.Length; i++)
                sum += _samples[i] * _samples[i];

            CurrentLevel = Mathf.Sqrt(sum / _samples.Length);

            if (HeardCall)
                return;

            // 要求「连续」达标：单帧尖峰（咳嗽、碰到头显、别人走过）不算。
            if (CurrentLevel >= rmsThreshold)
            {
                _loudSeconds += Time.deltaTime;
                if (_loudSeconds >= requiredSeconds)
                {
                    HeardCall = true;
                    Debug.Log($"[ResponseVoice] 检测到呼唤，RMS={CurrentLevel:F3}");
                }
            }
            else
            {
                _loudSeconds = 0f;
            }
        }

        private static bool HasMicPermission()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return Permission.HasUserAuthorizedPermission(Permission.Microphone);
#else
            return true;
#endif
        }

        private static void RequestMicPermission()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // 申请是异步的，本次训练来不及用上；授予之后下一次进入该阶段就生效。
            // 不在这里阻塞等待：意识确认是个 10 秒级的步骤，
            // 卡在系统权限弹窗上比没有语音检测更糟。
            Permission.RequestUserPermission(Permission.Microphone);
#endif
        }
    }
}
