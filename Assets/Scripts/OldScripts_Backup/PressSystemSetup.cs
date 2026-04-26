using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class PressSystemComponents
{
    [Header("=== 核心组件 ===")]
    public PressDataManager dataManager;
    public PressUIManager uiManager;
    public SimpleMetaXRChest chestController;
    public MetaXRGestureDetector gestureDetector;
    
    [Header("=== 胸部模型 ===")]
    public Transform chestModel;
    public Transform chestTarget;
    
    [Header("=== XR 组件 ===")]
    public Transform cameraRig;
    public Transform leftHandAnchor;
    public Transform rightHandAnchor;
}

public class PressSystemSetup : MonoBehaviour
{
    [Header("=== 系统组件 ===")]
    [SerializeField] private PressSystemComponents components;
    
    [Header("=== 自动设置选项 ===")]
    [SerializeField] private bool autoSetupOnStart = true;
    [SerializeField] private bool createMissingComponents = true;
    [SerializeField] private bool setupUI = true;
    [SerializeField] private bool setupChestModel = true;
    
    [Header("=== 胸部模型设置 ===")]
    [SerializeField] private Vector3 chestModelPosition = new Vector3(0, 1f, 1f);
    [SerializeField] private Vector3 chestModelScale = Vector3.one;
    [SerializeField] private Material chestMaterial;
    
    [Header("=== 调试选项 ===")]
    [SerializeField] private bool showSetupLog = true;
    [SerializeField] private bool validateSetup = true;
    
    void Start()
    {
        if (autoSetupOnStart)
        {
            SetupPressSystem();
        }
    }
    
    [ContextMenu("设置按压系统")]
    public void SetupPressSystem()
    {
        Log("🚀 开始设置胸外按压训练系统...");
        
        // 1. 查找或创建核心组件
        SetupCoreComponents();
        
        // 2. 设置胸部模型
        if (setupChestModel)
        {
            SetupChestModel();
        }
        
        // 3. 设置XR组件引用
        SetupXRComponents();
        
        // 4. 设置UI系统
        if (setupUI)
        {
            SetupUISystem();
        }
        
        // 5. 连接所有组件
        ConnectComponents();
        
        // 6. 验证设置
        if (validateSetup)
        {
            ValidateSystemSetup();
        }
        
        Log("✅ 胸外按压训练系统设置完成！");
    }
    
    void SetupCoreComponents()
    {
        Log("📦 设置核心组件...");
        
        // 数据管理器
        if (components.dataManager == null)
        {
            components.dataManager = FindObjectOfType<PressDataManager>();
            if (components.dataManager == null && createMissingComponents)
            {
                GameObject dataManagerGO = new GameObject("PressDataManager");
                components.dataManager = dataManagerGO.AddComponent<PressDataManager>();
                Log("✨ 创建了 PressDataManager");
            }
        }
        
        // UI管理器
        if (components.uiManager == null)
        {
            components.uiManager = FindObjectOfType<PressUIManager>();
            if (components.uiManager == null && createMissingComponents)
            {
                GameObject uiManagerGO = new GameObject("PressUIManager");
                components.uiManager = uiManagerGO.AddComponent<PressUIManager>();
                Log("✨ 创建了 PressUIManager");
            }
        }
        
        // 手势检测器
        if (components.gestureDetector == null)
        {
            components.gestureDetector = FindObjectOfType<MetaXRGestureDetector>();
            if (components.gestureDetector == null && createMissingComponents)
            {
                GameObject gestureDetectorGO = new GameObject("MetaXRGestureDetector");
                components.gestureDetector = gestureDetectorGO.AddComponent<MetaXRGestureDetector>();
                Log("✨ 创建了 MetaXRGestureDetector");
            }
        }
    }
    
    void SetupChestModel()
    {
        Log("🫁 设置胸部模型...");
        
        if (components.chestModel == null)
        {
            // 查找现有的胸部模型
            GameObject existingChest = GameObject.Find("ChestModel");
            if (existingChest == null)
            {
                existingChest = GameObject.Find("Chest");
            }
            
            if (existingChest != null)
            {
                components.chestModel = existingChest.transform;
            }
            else if (createMissingComponents)
            {
                // 创建简单的胸部模型
                GameObject chestGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chestGO.name = "ChestModel";
                chestGO.transform.position = chestModelPosition;
                chestGO.transform.localScale = new Vector3(0.3f, 0.1f, 0.2f);
                
                // 设置材质
                if (chestMaterial != null)
                {
                    Renderer renderer = chestGO.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material = chestMaterial;
                    }
                }
                
                components.chestModel = chestGO.transform;
                Log("✨ 创建了简单的胸部模型");
            }
        }
        
        // 设置胸部控制器
        if (components.chestModel != null)
        {
            components.chestController = components.chestModel.GetComponent<SimpleMetaXRChest>();
            if (components.chestController == null && createMissingComponents)
            {
                components.chestController = components.chestModel.gameObject.AddComponent<SimpleMetaXRChest>();
                Log("✨ 添加了 SimpleMetaXRChest 组件");
            }
            
            // 设置胸部目标
            if (components.chestTarget == null)
            {
                components.chestTarget = components.chestModel;
            }
        }
    }
    
    void SetupXRComponents()
    {
        Log("🥽 设置XR组件...");
        
        // 查找Camera Rig
        if (components.cameraRig == null)
        {
            GameObject cameraRigGO = GameObject.Find("[BuildingBlock] Camera Rig");
            if (cameraRigGO == null) cameraRigGO = GameObject.Find("OVRCameraRig");
            if (cameraRigGO == null) cameraRigGO = GameObject.Find("CameraRig");
            
            if (cameraRigGO != null)
            {
                components.cameraRig = cameraRigGO.transform;
                Log($"🎯 找到Camera Rig: {cameraRigGO.name}");
            }
        }
        
        // 查找手部锚点
        if (components.leftHandAnchor == null)
        {
            GameObject leftHandGO = GameObject.Find("LeftHandAnchor");
            if (leftHandGO == null) leftHandGO = GameObject.Find("Left Hand");
            if (leftHandGO != null)
            {
                components.leftHandAnchor = leftHandGO.transform;
                Log($"🤚 找到左手锚点: {leftHandGO.name}");
            }
        }
        
        if (components.rightHandAnchor == null)
        {
            GameObject rightHandGO = GameObject.Find("RightHandAnchor");
            if (rightHandGO == null) rightHandGO = GameObject.Find("Right Hand");
            if (rightHandGO != null)
            {
                components.rightHandAnchor = rightHandGO.transform;
                Log($"🤚 找到右手锚点: {rightHandGO.name}");
            }
        }
    }
    
    void SetupUISystem()
    {
        Log("🎨 设置UI系统...");
        
        if (components.uiManager != null)
        {
            // UI管理器会自动创建UI，这里不需要额外设置
            Log("✅ UI管理器已就绪");
        }
    }
    
    void ConnectComponents()
    {
        Log("🔗 连接组件...");
        
        // 连接手势检测器
        if (components.gestureDetector != null)
        {
            if (components.leftHandAnchor != null && components.rightHandAnchor != null)
            {
                components.gestureDetector.SetHandReferences(
                    components.leftHandAnchor, 
                    components.rightHandAnchor
                );
            }
            
            if (components.chestTarget != null)
            {
                components.gestureDetector.SetChestTarget(components.chestTarget);
            }
        }
        
        // 设置胸部控制器的目标
        if (components.chestController != null && components.chestTarget != null)
        {
            components.chestController.chestTarget = components.chestTarget;
        }
        
        Log("✅ 组件连接完成");
    }
    
    void ValidateSystemSetup()
    {
        Log("🔍 验证系统设置...");
        
        bool isValid = true;
        
        // 检查核心组件
        if (components.dataManager == null)
        {
            LogError("❌ 缺少 PressDataManager");
            isValid = false;
        }
        
        if (components.uiManager == null)
        {
            LogError("❌ 缺少 PressUIManager");
            isValid = false;
        }
        
        if (components.chestController == null)
        {
            LogError("❌ 缺少 SimpleMetaXRChest");
            isValid = false;
        }
        
        if (components.gestureDetector == null)
        {
            LogError("❌ 缺少 MetaXRGestureDetector");
            isValid = false;
        }
        
        // 检查XR组件
        if (components.leftHandAnchor == null || components.rightHandAnchor == null)
        {
            LogWarning("⚠️ 手部锚点未完全设置，可能影响手部跟踪");
        }
        
        if (components.chestTarget == null)
        {
            LogError("❌ 缺少胸部目标");
            isValid = false;
        }
        
        if (isValid)
        {
            Log("✅ 系统验证通过！");
        }
        else
        {
            LogError("❌ 系统验证失败，请检查缺少的组件");
        }
    }
    
    [ContextMenu("重置系统")]
    public void ResetSystem()
    {
        Log("🔄 重置按压系统...");
        
        if (components.dataManager != null)
        {
            components.dataManager.ResetSession();
        }
        
        Log("✅ 系统重置完成");
    }
    
    [ContextMenu("测试系统")]
    public void TestSystem()
    {
        Log("🧪 测试系统功能...");
        
        if (components.dataManager != null)
        {
            // 模拟一些按压数据
            for (int i = 0; i < 5; i++)
            {
                components.dataManager.UpdatePressDepth(Random.Range(0.02f, 0.05f));
                System.Threading.Thread.Sleep(100);
                components.dataManager.UpdatePressDepth(0f);
                System.Threading.Thread.Sleep(100);
            }
            
            Log($"📊 测试完成 - {components.dataManager.GetSessionSummary()}");
        }
    }
    
    // 公共接口
    public PressSystemComponents GetComponents()
    {
        return components;
    }
    
    public bool IsSystemReady()
    {
        return components.dataManager != null && 
               components.uiManager != null && 
               components.chestController != null && 
               components.gestureDetector != null;
    }
    
    // 日志方法
    void Log(string message)
    {
        if (showSetupLog)
        {
            Debug.Log($"[PressSystemSetup] {message}");
        }
    }
    
    void LogWarning(string message)
    {
        if (showSetupLog)
        {
            Debug.LogWarning($"[PressSystemSetup] {message}");
        }
    }
    
    void LogError(string message)
    {
        if (showSetupLog)
        {
            Debug.LogError($"[PressSystemSetup] {message}");
        }
    }
    
    // 在Inspector中显示设置按钮
    void OnValidate()
    {
        // 这个方法在Inspector值改变时调用
    }
}

