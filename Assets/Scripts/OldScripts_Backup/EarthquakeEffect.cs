using System.Collections;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// シンプルな地震効果：カメラ/ターゲットの揺れ + XR ハプティクス
/// PlayQuake で開始し、StopQuake で停止
/// </summary>
public class EarthquakeEffect : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("揺らしたいターゲット（通常は XR Rig またはメインカメラ）。未設定なら自動で Camera.main を使用")]
    [SerializeField] private Transform targetTransform;

    [Header("Default Params")]
    [SerializeField] private float defaultDuration = 2.0f;
    [SerializeField] private float defaultPositionAmplitude = 0.06f; // meters
    [SerializeField] private float defaultRotationAmplitude = 2.5f;   // degrees
    [SerializeField] private float defaultFrequency = 10f;            // Hz-ish
    [SerializeField] private float defaultHapticsAmplitude = 0.6f;    // 0-1
    [SerializeField] private float hapticsPulseDuration = 0.05f;

    private Coroutine quakeRoutine;
    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;

    void Awake()
    {
        if (targetTransform == null && Camera.main != null)
        {
            targetTransform = Camera.main.transform;
            Debug.Log("[EarthquakeEffect] Camera.main を自動でターゲットに設定");
        }
    }

    /// <summary>
    /// 地震を一度トリガー（負の値はデフォルトを使用）
    /// </summary>
    public void PlayQuake(
        float duration = -1f,
        float positionAmplitude = -1f,
        float rotationAmplitude = -1f,
        float frequency = -1f,
        float hapticsAmplitude = -1f)
    {
        if (targetTransform == null)
        {
            Debug.LogWarning("[EarthquakeEffect] targetTransform が見つからず、再生できません");
            return;
        }

        // デフォルト値に置き換え
        duration = duration > 0f ? duration : defaultDuration;
        positionAmplitude = positionAmplitude > 0f ? positionAmplitude : defaultPositionAmplitude;
        rotationAmplitude = rotationAmplitude > 0f ? rotationAmplitude : defaultRotationAmplitude;
        frequency = frequency > 0f ? frequency : defaultFrequency;
        hapticsAmplitude = hapticsAmplitude >= 0f ? hapticsAmplitude : defaultHapticsAmplitude;

        // 基準姿勢を記録
        baseLocalPosition = targetTransform.localPosition;
        baseLocalRotation = targetTransform.localRotation;

        // 既存コルーチンを停止
        if (quakeRoutine != null)
        {
            StopCoroutine(quakeRoutine);
        }

        Debug.Log($"[EarthquakeEffect] 地震開始: duration={duration:F2}s, posAmp={positionAmplitude:F3}m, rotAmp={rotationAmplitude:F1}°, freq={frequency:F1}Hz, haptics={hapticsAmplitude:F2}");
        quakeRoutine = StartCoroutine(DoQuake(duration, positionAmplitude, rotationAmplitude, frequency, hapticsAmplitude));
    }

    public void StopQuake()
    {
        if (quakeRoutine != null)
        {
            StopCoroutine(quakeRoutine);
            quakeRoutine = null;
        }

        if (targetTransform != null)
        {
            targetTransform.localPosition = baseLocalPosition;
            targetTransform.localRotation = baseLocalRotation;
        }

        // ハプティクスを停止
        SendHapticsPulse(0f, 0.01f);
        Debug.Log("[EarthquakeEffect] 地震停止、姿勢をリセット");
    }

    private IEnumerator DoQuake(float duration, float posAmp, float rotAmp, float freq, float hapticAmp)
    {
        float time = 0f;
        float seed = Random.Range(0f, 1000f);

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time * freq;

            // 平滑ノイズ（-1..1）
            float nx = Mathf.PerlinNoise(seed, t) * 2f - 1f;
            float ny = Mathf.PerlinNoise(seed + 13.37f, t) * 2f - 1f;
            float nz = Mathf.PerlinNoise(seed + 26.74f, t) * 2f - 1f;

            // 位置・回転の揺れ
            Vector3 offsetPos = new Vector3(nx, ny * 0.5f, nz) * posAmp;
            Vector3 offsetRot = new Vector3(ny, nx, nz) * rotAmp; // degrees

            targetTransform.localPosition = baseLocalPosition + offsetPos;
            targetTransform.localRotation = baseLocalRotation * Quaternion.Euler(offsetRot);

            // ハプティクス（毎フレームの小さなパルス）
            float pulseAmp = Mathf.Clamp01(hapticAmp);
            if (pulseAmp > 0f)
            {
                SendHapticsPulse(pulseAmp, hapticsPulseDuration);
            }

            yield return null;
        }

        StopQuake();
    }

    private void SendHapticsPulse(float amplitude, float duration)
    {
        TrySendHaptics(XRNode.LeftHand, amplitude, duration);
        TrySendHaptics(XRNode.RightHand, amplitude, duration);
    }

    private void TrySendHaptics(XRNode node, float amplitude, float duration)
    {
        var device = InputDevices.GetDeviceAtXRNode(node);
        if (!device.isValid)
        {
            return;
        }

        if (device.TryGetHapticCapabilities(out var caps) && caps.supportsImpulse)
        {
            device.SendHapticImpulse(0u, Mathf.Clamp01(amplitude), Mathf.Max(0f, duration));
        }
        else
        {
            Debug.LogWarning($"[EarthquakeEffect] ハプティクス非対応: {device.name}");
        }
    }

    void OnDisable()
    {
        StopQuake();
    }
}
