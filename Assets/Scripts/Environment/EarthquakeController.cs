using UnityEngine;

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
    }

    public void Stop()
    {
        _playing = false;
        RestoreCamera();
        RestoreVisualTargets();

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
            hitSfx.PlayOneShot(hitSfx.clip);
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
