using UnityEngine;

/// <summary>
/// 简单的地震触发器 - 使用空格键触发地震
/// 这个脚本没有任何依赖，可以独立工作
/// </summary>
public class SimpleEarthquakeTrigger : MonoBehaviour
{
    [Header("References")]
    [Tooltip("地震控制器引用")]
    public EarthquakeController earthquakeController;

    [Header("Settings")]
    [Tooltip("触发按键")]
    public KeyCode triggerKey = KeyCode.Space;

    [Tooltip("启动时自动查找EarthquakeController")]
    public bool autoFindController = true;

    private void Start()
    {
        // 自动查找EarthquakeController
        if (earthquakeController == null && autoFindController)
        {
            earthquakeController = FindObjectOfType<EarthquakeController>();

            if (earthquakeController != null)
            {
                Debug.Log($"[SimpleEarthquakeTrigger] 找到EarthquakeController: {earthquakeController.name}");
            }
            else
            {
                Debug.LogWarning("[SimpleEarthquakeTrigger] 未找到EarthquakeController！");
            }
        }

        Debug.Log($"[SimpleEarthquakeTrigger] 按 {triggerKey} 键触发地震");
    }

    private void Update()
    {
        // 检测按键
        if (Input.GetKeyDown(triggerKey))
        {
            TriggerEarthquake();
        }
    }

    /// <summary>
    /// 触发地震
    /// </summary>
    public void TriggerEarthquake()
    {
        if (earthquakeController == null)
        {
            Debug.LogError("[SimpleEarthquakeTrigger] EarthquakeController未设置！");
            return;
        }

        Debug.Log("[SimpleEarthquakeTrigger] 触发地震！");
        earthquakeController.Play();
    }

    /// <summary>
    /// 停止地震
    /// </summary>
    public void StopEarthquake()
    {
        if (earthquakeController != null)
        {
            Debug.Log("[SimpleEarthquakeTrigger] 停止地震");
            earthquakeController.Stop();
        }
    }
}
