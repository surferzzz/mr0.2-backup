using UnityEngine;

/// <summary>
/// 场景诊断工具 - 帮助检查场景配置问题
/// </summary>
public class SceneDiagnostics : MonoBehaviour
{
    [Header("诊断设置")]
    [Tooltip("启动时自动运行诊断")]
    public bool runOnStart = true;

    private void Start()
    {
        if (runOnStart)
        {
            RunDiagnostics();
        }
    }

    [ContextMenu("运行诊断")]
    public void RunDiagnostics()
    {
        Debug.Log("===== 场景诊断开始 =====");

        CheckCamera();
        CheckEarthquakeController();
        CheckUI();
        CheckPhysics();
        CheckScripts();

        Debug.Log("===== 场景诊断完成 =====");
    }

    private void CheckCamera()
    {
        Debug.Log("\n--- 检查摄像机 ---");

        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("未找到主摄像机！请确保场景中有Camera组件且Tag为MainCamera");
        }
        else
        {
            Debug.Log($"✓ 找到主摄像机: {mainCam.name}");
            Debug.Log($"  位置: {mainCam.transform.position}");
            Debug.Log($"  旋转: {mainCam.transform.eulerAngles}");
        }

        Camera[] allCameras = FindObjectsOfType<Camera>();
        Debug.Log($"场景中共有 {allCameras.Length} 个摄像机");
    }

    private void CheckEarthquakeController()
    {
        Debug.Log("\n--- 检查地震控制器 ---");

        EarthquakeController earthquake = FindObjectOfType<EarthquakeController>();
        if (earthquake == null)
        {
            Debug.LogError("未找到EarthquakeController！");
        }
        else
        {
            Debug.Log($"✓ 找到EarthquakeController: {earthquake.name}");

            if (earthquake.targetCamera == null)
            {
                Debug.LogError("EarthquakeController.targetCamera 未设置！");
            }
            else
            {
                Debug.Log($"✓ targetCamera已设置: {earthquake.targetCamera.name}");
            }

            Debug.Log($"  持续时间: {earthquake.duration}秒");
            Debug.Log($"  受影响的刚体数量: {(earthquake.affectedBodies != null ? earthquake.affectedBodies.Length : 0)}");
        }
    }

    private void CheckUI()
    {
        Debug.Log("\n--- 检查UI系统 ---");

        Canvas[] canvases = FindObjectsOfType<Canvas>();
        Debug.Log($"场景中共有 {canvases.Length} 个Canvas");

        foreach (Canvas canvas in canvases)
        {
            Debug.Log($"  Canvas: {canvas.name}, 渲染模式: {canvas.renderMode}");
        }

        UnityEngine.UI.Button[] buttons = FindObjectsOfType<UnityEngine.UI.Button>();
        Debug.Log($"场景中共有 {buttons.Length} 个Button");

        UnityEngine.EventSystems.EventSystem eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null)
        {
            Debug.LogWarning("未找到EventSystem！UI可能无法交互");
        }
        else
        {
            Debug.Log("✓ EventSystem存在");
        }
    }

    private void CheckPhysics()
    {
        Debug.Log("\n--- 检查物理系统 ---");

        Rigidbody[] rigidbodies = FindObjectsOfType<Rigidbody>();
        Debug.Log($"场景中共有 {rigidbodies.Length} 个Rigidbody");

        int activeRBs = 0;
        foreach (Rigidbody rb in rigidbodies)
        {
            if (rb.gameObject.activeInHierarchy)
            {
                activeRBs++;
            }
        }
        Debug.Log($"  激活的刚体: {activeRBs}");
    }

    private void CheckScripts()
    {
        Debug.Log("\n--- 检查脚本组件 ---");

        // 检查场景控制器
        var sceneController = FindObjectOfType<VRFirstAid.Core.EarthquakeSceneController>();
        if (sceneController == null)
        {
            Debug.LogWarning("未找到EarthquakeSceneController");
        }
        else
        {
            Debug.Log($"✓ 找到EarthquakeSceneController: {sceneController.name}");

            if (sceneController.earthquakeController == null)
                Debug.LogError("  earthquakeController引用未设置！");
            if (sceneController.triggerButton == null)
                Debug.LogError("  triggerButton引用未设置！");
            if (sceneController.instructionText == null)
                Debug.LogWarning("  instructionText引用未设置");
        }

        // 检查CPR管理器
        var cprManager = FindObjectOfType<VRFirstAid.CPR.CPRManager>();
        if (cprManager == null)
        {
            Debug.Log("未找到CPRManager（这是正常的，如果还没添加CPR系统）");
        }
        else
        {
            Debug.Log($"✓ 找到CPRManager: {cprManager.name}");
        }
    }

    [ContextMenu("列出所有GameObject")]
    public void ListAllGameObjects()
    {
        Debug.Log("===== 场景层次结构 =====");

        GameObject[] allObjects = FindObjectsOfType<GameObject>();

        // 只列出根对象
        foreach (GameObject obj in allObjects)
        {
            if (obj.transform.parent == null)
            {
                LogHierarchy(obj.transform, 0);
            }
        }
    }

    private void LogHierarchy(Transform t, int depth)
    {
        string indent = new string(' ', depth * 2);
        string active = t.gameObject.activeInHierarchy ? "" : " [INACTIVE]";
        Debug.Log($"{indent}- {t.name}{active}");

        foreach (Transform child in t)
        {
            LogHierarchy(child, depth + 1);
        }
    }
}
