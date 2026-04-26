using System.Linq;
using UnityEngine;

/// <summary>
/// 简易地震模拟：摄像机抖动 + 物体冲击 + 尘土/音效。
/// 通过调用 Play() 触发；Stop() 可提前结束。
/// </summary>
public class EarthquakeController : MonoBehaviour
{
    [Header("Shake Target")]
    [Tooltip("通常填 XR Rig 下的 Camera 或主摄像机 Transform。")]
    public Transform targetCamera;

    [Header("Timing")]
    [Tooltip("震动总时长（秒）。")]
    public float duration = 8f;
    [Tooltip("抖动强度随时间的曲线，横轴 0-1 为归一化进度。")]
    public AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Camera Shake")]
    [Tooltip("位置抖动幅度（米）。")]
    public float amplitudePos = 0.08f;
    [Tooltip("旋转抖动幅度（度）。")]
    public float amplitudeRot = 1.5f;
    [Tooltip("Perlin 噪声频率。")]
    public float frequency = 6f;

    [Header("Rigidbodies Impact")]
    [Tooltip("需要被震动的物体（需带 Rigidbody）。")]
    public Rigidbody[] affectedBodies;
    [Tooltip("冲击间隔（秒）。")]
    public float impulseInterval = 0.6f;
    [Tooltip("冲击力度。")]
    public float impulseForce = 120f;

    [Header("VFX / SFX（可选）")]
    public ParticleSystem dustFX;
    public AudioSource rumbleLoop;   // 低频隆隆，勾选 Loop
    public AudioSource hitSfx;       // 短促撞击音

    [Header("Debug")]
    public bool playOnStart = false;

    private bool _loggedMissingBodies;
    private float _t;
    private float _nextImpulse;
    private bool _playing;
    private Vector3 _camBasePos;
    private Quaternion _camBaseRot;

    private void Start()
    {
        if (playOnStart) Play();
    }

    private void Update()
    {
        if (!_playing) return;

        _t += Time.deltaTime;
        var progress = Mathf.Clamp01(_t / Mathf.Max(0.001f, duration));
        var intensity = intensityCurve.Evaluate(progress);

        // 摄像机抖动
        if (targetCamera != null)
        {
            float n1 = Mathf.PerlinNoise(Time.time * frequency, 0f) * 2f - 1f;
            float n2 = Mathf.PerlinNoise(0f, Time.time * frequency) * 2f - 1f;

            targetCamera.localPosition = _camBasePos + new Vector3(n1, n2, 0f) * (amplitudePos * intensity);
            targetCamera.localRotation = _camBaseRot *
                                         Quaternion.Euler(n2 * amplitudeRot * intensity,
                                                          n1 * amplitudeRot * intensity,
                                                          0f);
        }

        // 冲击刚体
        if (_t >= _nextImpulse)
        {
            _nextImpulse = _t + impulseInterval;
            var bodies = affectedBodies;
            if (bodies != null && bodies.Length > 0)
            {
                foreach (var rb in bodies)
                {
                    if (rb == null) continue;
                    var dir = new Vector3(Random.Range(-1f, 1f), 0.25f, Random.Range(-1f, 1f)).normalized;
                    rb.AddForce(dir * impulseForce * intensity, ForceMode.Impulse);
                }
            }
            else if (!_loggedMissingBodies)
            {
                Debug.LogWarning("[Earthquake] No affected bodies assigned; only camera shake will play.");
                _loggedMissingBodies = true;
            }

            if (hitSfx != null && hitSfx.clip != null) hitSfx.PlayOneShot(hitSfx.clip);
            if (dustFX != null && !dustFX.isPlaying) dustFX.Play();
        }

        // 结束
        if (_t >= duration)
        {
            Stop();
        }
    }

    /// <summary>触发地震。</summary>
    public void Play()
    {
        EnsureBindings();

        if (targetCamera != null)
        {
            _camBasePos = targetCamera.localPosition;
            _camBaseRot = targetCamera.localRotation;
        }
        _t = 0f;
        _nextImpulse = 0f;
        _loggedMissingBodies = false;
        _playing = true;

        if (rumbleLoop != null)
        {
            rumbleLoop.Play();
        }
        if (dustFX != null) dustFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    /// <summary>提前结束并复位摄像机。</summary>
    public void Stop()
    {
        _playing = false;
        if (targetCamera != null)
        {
            targetCamera.localPosition = _camBasePos;
            targetCamera.localRotation = _camBaseRot;
        }

        if (rumbleLoop != null) rumbleLoop.Stop();
    }

    private void EnsureBindings()
    {
        // Auto-assign camera if inspector reference is missing
        if (targetCamera == null)
        {
            var mainCam = Camera.main;
            if (mainCam != null) targetCamera = mainCam.transform;
        }

        // Fill affected bodies lazily to avoid null refs
        if (affectedBodies == null || affectedBodies.Length == 0)
        {
            affectedBodies = FindObjectsOfType<Rigidbody>()
                .Where(rb => rb != null && rb.gameObject.activeInHierarchy && !rb.isKinematic)
                .ToArray();
        }
    }
}
