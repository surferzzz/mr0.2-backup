using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using CPRSimulation;   // ProceduralAudio

/// <summary>
/// Earthquake simulation for the training scene.
/// It combines mild camera shake, visual prop shake, falling rigidbodies, dust, and optional procedural rumble.
/// </summary>
public class EarthquakeController : MonoBehaviour
{
    [Header("Shake Target")]
    [Tooltip("Usually Main Camera or CenterEyeAnchor. Keep VR amplitudes low to avoid discomfort.")]
    public Transform targetCamera;

    [Header("Timing")]
    public float duration = 8f;
    public AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Camera Shake")]
    public bool shakeCamera = true;
    public float amplitudePos = 0.025f;
    public float amplitudeRot = 0.45f;
    public float frequency = 7f;

    [Header("Visual Prop Shake")]
    public bool shakeVisualTargets = true;
    public Transform[] visualShakeTargets;
    public float objectPositionAmplitude = 0.035f;
    public float objectRotationAmplitude = 2.5f;

    [Header("Rigidbodies Impact")]
    public Rigidbody[] affectedBodies;
    public bool enableGravityOnPlay = true;
    public float impulseInterval = 0.45f;
    public float impulseForce = 45f;

    [Header("VFX / SFX")]
    public ParticleSystem dustFX;
    public AudioSource rumbleLoop;
    public AudioSource hitSfx;
    public bool generateProceduralRumble = true;

    [Header("自动绑定")]
    [Tooltip("Play() 时自动把房间里的松散道具填进 visualShakeTargets。" +
             "震动是非破坏性的 —— Stop() 会还原原始位置与旋转。已手动配置则不覆盖。")]
    public bool autoBindVisualTargets = true;

    [Tooltip("房间根物体名，用来查找要震动的道具。")]
    public string roomRootName = "Madduck_Hospital_Training_Room";

    [Tooltip("未指定 dustFX 时自动生成一套扬尘粒子（不需要美术资源）。")]
    public bool autoCreateDustFX = true;

    [Tooltip("未指定 hitSfx 时用代码合成撞击/落屑声。")]
    public bool autoCreateHitSfx = true;

    [Header("緊急放送（紧急播报）")]
    [Tooltip("地震开始时播放紧急播报语音。\n" +
             "音频从 StreamingAssets/earthquake-voice/<语言>/<key>.wav 读取，\n" +
             "用 UnityWebRequest 加载（Android 上 StreamingAssets 在 APK 里，不能用 File.IO）。\n" +
             "文件不存在时退回代码合成的警报音，所以没有音频也能跑。")]
    public bool playEmergencyAnnouncement = true;

    [Tooltip("播报音频的文件名（不含扩展名）。完整路径见上。")]
    public string announcementKey = "earthquake_alert";

    [Tooltip("留空则跟随 LanguageManager 的当前语言（ja / zh / en）。")]
    public string announcementLanguageOverride = "";

    public AudioSource announcementSource;

    [Header("Debug")]
    public bool playOnStart = false;

    public bool IsPlaying => _playing;

    private bool _loggedMissingBodies;
    private float _t;
    private float _nextImpulse;
    private bool _playing;
    private Vector3 _camBasePos;
    private Quaternion _camBaseRot;
    private Vector3[] _visualBasePositions;
    private Quaternion[] _visualBaseRotations;
    private bool[] _bodyBaseGravity;
    private bool[] _bodyBaseKinematic;
    private AudioClip _generatedRumble;
    private AudioClip _announcementClip;
    private AudioClip _fallbackAlert;
    private bool _announcementRequested;

    private void Start()
    {
        if (playOnStart)
        {
            Play();
        }
    }

    private void Update()
    {
        if (!_playing)
        {
            return;
        }

        _t += Time.deltaTime;
        float progress = Mathf.Clamp01(_t / Mathf.Max(0.001f, duration));
        float intensity = intensityCurve.Evaluate(progress);

        UpdateCameraShake(intensity);
        UpdateVisualTargetShake(intensity);
        UpdateRigidBodyImpulses(intensity);

        if (_t >= duration)
        {
            Stop();
        }
    }

    public void Play()
    {
        EnsureBindings();
        CacheBaseTransforms();
        PrepareRumbleAudio();
        PrepareRigidbodiesForEarthquake();

        _t = 0f;
        _nextImpulse = 0f;
        _loggedMissingBodies = false;
        _playing = true;

        if (rumbleLoop != null)
        {
            rumbleLoop.loop = true;
            rumbleLoop.Play();
        }

        if (dustFX != null)
        {
            dustFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            dustFX.Play();
        }

        PlayEmergencyAnnouncement();
    }

    public void Stop()
    {
        _playing = false;
        RestoreCamera();
        RestoreVisualTargets();
        RestoreRigidbodies();

        if (rumbleLoop != null)
        {
            rumbleLoop.Stop();
        }

        if (dustFX != null)
        {
            dustFX.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private void UpdateCameraShake(float intensity)
    {
        if (!shakeCamera || targetCamera == null)
        {
            return;
        }

        float n1 = Mathf.PerlinNoise(Time.time * frequency, 0f) * 2f - 1f;
        float n2 = Mathf.PerlinNoise(0f, Time.time * frequency) * 2f - 1f;

        targetCamera.localPosition = _camBasePos + new Vector3(n1, n2 * 0.55f, 0f) * (amplitudePos * intensity);
        targetCamera.localRotation = _camBaseRot *
                                     Quaternion.Euler(n2 * amplitudeRot * intensity,
                                                      n1 * amplitudeRot * intensity,
                                                      n1 * amplitudeRot * 0.35f * intensity);
    }

    private void UpdateVisualTargetShake(float intensity)
    {
        if (!shakeVisualTargets || visualShakeTargets == null || _visualBasePositions == null)
        {
            return;
        }

        for (int i = 0; i < visualShakeTargets.Length; i++)
        {
            Transform target = visualShakeTargets[i];
            if (target == null)
            {
                continue;
            }

            float seed = i * 11.73f;
            float nx = Mathf.PerlinNoise(seed, Time.time * frequency) * 2f - 1f;
            float nz = Mathf.PerlinNoise(Time.time * frequency, seed) * 2f - 1f;
            float yaw = Mathf.PerlinNoise(seed + 3.7f, Time.time * frequency) * 2f - 1f;

            target.localPosition = _visualBasePositions[i] +
                                   new Vector3(nx, 0f, nz) * (objectPositionAmplitude * intensity);
            target.localRotation = _visualBaseRotations[i] *
                                   Quaternion.Euler(0f, yaw * objectRotationAmplitude * intensity, nx * objectRotationAmplitude * intensity);
        }
    }

    private void UpdateRigidBodyImpulses(float intensity)
    {
        if (_t < _nextImpulse)
        {
            return;
        }

        _nextImpulse = _t + impulseInterval;
        Rigidbody[] bodies = affectedBodies;
        if (bodies != null && bodies.Length > 0)
        {
            foreach (Rigidbody rb in bodies)
            {
                if (rb == null)
                {
                    continue;
                }

                rb.WakeUp();
                Vector3 dir = new Vector3(Random.Range(-1f, 1f), Random.Range(0.1f, 0.45f), Random.Range(-1f, 1f)).normalized;
                rb.AddForce(dir * impulseForce * intensity, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * impulseForce * 0.45f * intensity, ForceMode.Impulse);
            }
        }
        else if (!_loggedMissingBodies)
        {
            Debug.LogWarning("[Earthquake] No affected bodies assigned; only shake effects will play.");
            _loggedMissingBodies = true;
        }

        if (hitSfx != null && hitSfx.clip != null)
        {
            // 每 impulseInterval 都是同一声闷响会很单调，随机音高拉开差异
            hitSfx.pitch = Random.Range(0.82f, 1.22f);
            hitSfx.PlayOneShot(hitSfx.clip, Mathf.Lerp(0.45f, 1f, intensity));
        }
    }

    private void EnsureBindings()
    {
        // 纯镜头晃动：抖动必须加在 OVRCameraRig 的 TrackingSpace 上。
        // CenterEyeAnchor 的 localPosition/localRotation 每帧会被头显追踪覆盖，
        // 加在它上面看不到任何晃动；而 TrackingSpace 不会被覆盖，移动它会让整个视角一起晃。
        Transform resolved = ResolveShakeTarget();
        if (resolved != null)
        {
            targetCamera = resolved;
        }
        else if (targetCamera == null && Camera.main != null)
        {
            targetCamera = Camera.main.transform;
        }

        AutoBindVisualShakeTargets();
        EnsureDustFX();
        EnsureHitSfx();
        EnsureAnnouncementSource();
    }

    private void EnsureAnnouncementSource()
    {
        if (!playEmergencyAnnouncement || announcementSource != null)
            return;

        var host = new GameObject("EarthquakeAnnouncement");
        host.transform.SetParent(transform, false);
        announcementSource = host.AddComponent<AudioSource>();
        announcementSource.playOnAwake = false;
        announcementSource.spatialBlend = 0f;   // 广播是全场的，不做定位
        announcementSource.volume = 0.9f;
        announcementSource.priority = 0;        // 压过轰鸣和落屑声
    }

    /// <summary>
    /// 播放紧急播报。优先用 StreamingAssets 里的授权音频，没有就退回合成警报音。
    ///
    /// 刻意不内置任何真实的緊急地震速報音源：那段旋律有著作权，
    /// 而且日本对它在非紧急场合的使用有明确的社会规范 —— 人们被训练成
    /// 听到就立即反应，随便播放是有害的。这里合成的是一个明显不同的双音警报，
    /// 只作占位；正式用途请放入你自己取得授权的音频（见 README）。
    /// </summary>
    private void PlayEmergencyAnnouncement()
    {
        if (!playEmergencyAnnouncement || announcementSource == null)
            return;

        if (_announcementClip != null)
        {
            announcementSource.PlayOneShot(_announcementClip);
            return;
        }

        // 先放占位警报音，真实音频异步加载完之后的那次地震才会用上它
        announcementSource.PlayOneShot(GetFallbackAlertClip());

        if (!_announcementRequested)
        {
            _announcementRequested = true;
            StartCoroutine(LoadAnnouncementClip());
        }
    }

    private AudioClip GetFallbackAlertClip()
    {
        if (_fallbackAlert == null)
        {
            // 双音交替，重复三轮。刻意避开真实速报音的音程关系。
            _fallbackAlert = ProceduralAudio.CreateSequence(
                "EarthquakeAlertFallback",
                new[] { 880f, 660f, 880f, 660f, 880f, 660f },
                noteSeconds: 0.22f,
                volume: 0.5f);
        }

        return _fallbackAlert;
    }

    private string ResolveAnnouncementLanguage()
    {
        if (!string.IsNullOrWhiteSpace(announcementLanguageOverride))
            return announcementLanguageOverride.Trim();

        // LanguageManager 是 MonoBehaviour 单例，可能还没建起来 —— 默认日语，
        // 和 LanguageManager.Get 无实例时的回退保持一致。
        var mgr = LanguageManager.Instance;
        if (mgr == null)
            return "ja";

        switch (mgr.CurrentLanguage)
        {
            case Language.Chinese: return "zh";
            case Language.English: return "en";
            default: return "ja";
        }
    }

    private IEnumerator LoadAnnouncementClip()
    {
        string lang = ResolveAnnouncementLanguage();
        string relative = $"earthquake-voice/{lang}/{announcementKey}.wav";

        // Android 上 StreamingAssets 打在 APK 里，路径是 jar:file:///...，
        // 不能用 System.IO 读，必须走 UnityWebRequest。这和 AED 语音同一套做法。
        string root = Application.streamingAssetsPath;
        string full = root.Contains("://") || root.StartsWith("jar:")
            ? root.TrimEnd('/') + "/" + relative
            : new System.Uri(System.IO.Path.Combine(root, relative)).AbsoluteUri;

        using (var request = UnityWebRequestMultimedia.GetAudioClip(full, AudioType.WAV))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.Log($"[Earthquake] 未找到紧急播报音频（{relative}），继续使用合成警报音。" +
                          "放入授权音频后会自动改用它。");
                yield break;
            }

            _announcementClip = DownloadHandlerAudioClip.GetContent(request);
            if (_announcementClip != null)
            {
                _announcementClip.name = announcementKey;
                Debug.Log($"[Earthquake] 已载入紧急播报音频：{relative}");
            }
        }
    }

    // 结构件不该跟着晃：墙、地、天花板、门、装饰条、灯、标识、地垫、边界。
    // 用排除法而不是白名单，这样房间以后加了新道具也会自动跟着震。
    private static readonly string[] StructuralNameParts =
    {
        "Floor", "Wall", "Ceiling", "Trim", "Entrance", "Door",
        "Mat", "Backplate", "Seal", "Boundary", "Light", "Sign", "Frame"
    };

    private void AutoBindVisualShakeTargets()
    {
        if (!autoBindVisualTargets)
            return;

        // 已经手动配置过就不覆盖
        if (visualShakeTargets != null && visualShakeTargets.Length > 0)
            return;

        GameObject room = string.IsNullOrWhiteSpace(roomRootName) ? null : GameObject.Find(roomRootName);
        if (room == null)
        {
            Debug.LogWarning($"[Earthquake] 未找到房间根物体「{roomRootName}」，道具震动跳过");
            return;
        }

        var props = new List<Transform>();
        foreach (Transform child in room.transform)
        {
            if (IsStructural(child.name))
                continue;

            props.Add(child);
        }

        visualShakeTargets = props.ToArray();
        Debug.Log($"[Earthquake] 自动绑定 {props.Count} 个道具参与震动：" +
                  string.Join("、", props.ConvertAll(p => p.name)));
    }

    private static bool IsStructural(string objectName)
    {
        foreach (string part in StructuralNameParts)
        {
            if (objectName.IndexOf(part, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 代码生成扬尘粒子，不依赖任何美术资源。
    /// 从天花板高度的一个薄盒里向下缓慢飘落，粒子数刻意压低以照顾 Quest 3。
    /// </summary>
    private void EnsureDustFX()
    {
        if (dustFX != null || !autoCreateDustFX)
            return;

        var host = new GameObject("EarthquakeDustFX");
        host.transform.SetParent(transform, false);
        host.transform.position = new Vector3(0f, 2.7f, 0.25f);   // 房间中心的天花板附近

        var ps = host.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.maxParticles = 80;                     // Quest 3 上刻意压低
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
        main.gravityModifier = 0.08f;               // 缓慢飘落而不是砸下来
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.72f, 0.68f, 0.60f, 0.30f),
            new Color(0.60f, 0.56f, 0.50f, 0.16f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 30f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(7.6f, 0.4f, 6.2f);   // 略小于房间，避免穿墙

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.35f));

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(
            new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                alphaKeys = new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.2f),
                    new GradientAlphaKey(0f, 1f)
                }
            });

        var renderer = host.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = CreateDustMaterial();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        dustFX = ps;
        Debug.Log("[Earthquake] 已生成扬尘粒子（80 上限 / 30 每秒）");
    }

    private static Material CreateDustMaterial()
    {
        // Built-in RP 下按可用性依次退让，找不到就返回 null 让 Unity 用默认粒子材质
        Shader shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended")
                        ?? Shader.Find("Particles/Standard Unlit")
                        ?? Shader.Find("Sprites/Default");

        if (shader == null)
        {
            Debug.LogWarning("[Earthquake] 找不到可用的粒子着色器，扬尘将使用默认材质");
            return null;
        }

        return new Material(shader) { color = new Color(0.70f, 0.66f, 0.58f, 0.28f) };
    }

    /// <summary>
    /// hitSfx 每 impulseInterval 播一次，与有没有刚体无关，
    /// 所以单独接上它就能有周期性的落屑/撞击声。
    /// </summary>
    private void EnsureHitSfx()
    {
        if (!autoCreateHitSfx)
            return;

        if (hitSfx == null)
        {
            var host = new GameObject("EarthquakeHitSfx");
            host.transform.SetParent(transform, false);
            hitSfx = host.AddComponent<AudioSource>();
            hitSfx.playOnAwake = false;
            hitSfx.spatialBlend = 0f;   // 整个房间都在震，不做定位
            hitSfx.volume = 0.5f;
        }

        if (hitSfx.clip == null)
        {
            // 低频闷响 + 慢衰减 ≈ 建材受力/落屑
            hitSfx.clip = ProceduralAudio.CreateClick("EarthquakeDebris", 95f, 0.45f, 7f, 0.85f);
        }
    }

    private Transform ResolveShakeTarget()
    {
        var rig = FindObjectOfType<OVRCameraRig>();
        if (rig != null)
        {
            if (rig.trackingSpace != null)
                return rig.trackingSpace;
            return rig.transform;
        }

        // 非 VR 兜底：Main Camera
        return Camera.main != null ? Camera.main.transform : null;
    }

    private void CacheBaseTransforms()
    {
        if (targetCamera != null)
        {
            _camBasePos = targetCamera.localPosition;
            _camBaseRot = targetCamera.localRotation;
        }

        if (visualShakeTargets != null)
        {
            _visualBasePositions = new Vector3[visualShakeTargets.Length];
            _visualBaseRotations = new Quaternion[visualShakeTargets.Length];
            for (int i = 0; i < visualShakeTargets.Length; i++)
            {
                Transform target = visualShakeTargets[i];
                if (target == null)
                {
                    continue;
                }

                _visualBasePositions[i] = target.localPosition;
                _visualBaseRotations[i] = target.localRotation;
            }
        }

        if (affectedBodies != null)
        {
            _bodyBaseGravity = new bool[affectedBodies.Length];
            _bodyBaseKinematic = new bool[affectedBodies.Length];
            for (int i = 0; i < affectedBodies.Length; i++)
            {
                Rigidbody rb = affectedBodies[i];
                if (rb == null)
                {
                    continue;
                }

                _bodyBaseGravity[i] = rb.useGravity;
                _bodyBaseKinematic[i] = rb.isKinematic;
            }
        }
    }

    /// <summary>
    /// _bodyBaseGravity / _bodyBaseKinematic 在 CacheBaseTransforms 里存了，
    /// 但原来没有任何地方还原 —— 刚体一旦被 Play() 变成动态就永远保持动态，
    /// 地震结束后仍会继续受力漂移。这里补上收尾。
    /// 注意：位置不还原，掉落的物件会留在原地（符合"地震过后"的观感）。
    /// </summary>
    private void RestoreRigidbodies()
    {
        if (affectedBodies == null || _bodyBaseGravity == null || _bodyBaseKinematic == null)
        {
            return;
        }

        int count = Mathf.Min(affectedBodies.Length,
                              Mathf.Min(_bodyBaseGravity.Length, _bodyBaseKinematic.Length));
        for (int i = 0; i < count; i++)
        {
            Rigidbody rb = affectedBodies[i];
            if (rb == null)
            {
                continue;
            }

            rb.useGravity = _bodyBaseGravity[i];
            rb.isKinematic = _bodyBaseKinematic[i];
        }
    }

    private void PrepareRigidbodiesForEarthquake()
    {
        if (affectedBodies == null)
        {
            return;
        }

        foreach (Rigidbody rb in affectedBodies)
        {
            if (rb == null)
            {
                continue;
            }

            rb.isKinematic = false;
            if (enableGravityOnPlay)
            {
                rb.useGravity = true;
            }
            rb.WakeUp();
        }
    }

    private void PrepareRumbleAudio()
    {
        if (!generateProceduralRumble)
        {
            return;
        }

        if (rumbleLoop == null)
        {
            rumbleLoop = gameObject.AddComponent<AudioSource>();
            rumbleLoop.spatialBlend = 0.2f;
            rumbleLoop.volume = 0.35f;
        }

        if (rumbleLoop.clip == null)
        {
            if (_generatedRumble == null)
            {
                _generatedRumble = GenerateRumbleClip();
            }

            rumbleLoop.clip = _generatedRumble;
        }
    }

    private AudioClip GenerateRumbleClip()
    {
        const int sampleRate = 22050;
        const float clipSeconds = 2f;
        int samples = Mathf.CeilToInt(sampleRate * clipSeconds);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float low = Mathf.Sin(2f * Mathf.PI * 42f * t) * 0.35f;
            float sub = Mathf.Sin(2f * Mathf.PI * 18f * t) * 0.25f;
            float noise = Random.Range(-0.08f, 0.08f);
            data[i] = Mathf.Clamp(low + sub + noise, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("ProceduralEarthquakeRumble", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private void RestoreCamera()
    {
        if (targetCamera == null)
        {
            return;
        }

        targetCamera.localPosition = _camBasePos;
        targetCamera.localRotation = _camBaseRot;
    }

    private void RestoreVisualTargets()
    {
        if (visualShakeTargets == null || _visualBasePositions == null || _visualBaseRotations == null)
        {
            return;
        }

        for (int i = 0; i < visualShakeTargets.Length; i++)
        {
            Transform target = visualShakeTargets[i];
            if (target == null)
            {
                continue;
            }

            target.localPosition = _visualBasePositions[i];
            target.localRotation = _visualBaseRotations[i];
        }
    }
}
