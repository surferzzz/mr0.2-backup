using UnityEngine;

/// <summary>
/// 超级简单的CPR设置助手
/// 直接添加到场景中的任何GameObject上，然后在Inspector中点击按钮即可
/// </summary>
public class EasySetup : MonoBehaviour
{
    [Header("=== 点击下方按钮开始设置 ===")]
    [Space(10)]
    
    [Tooltip("点击这个按钮自动创建所有CPR训练组件")]
    public bool setupButton = false;
    
    [Header("=== 设置选项 ===")]
    public bool createPatient = true;
    public bool createManagers = true;
    public bool createUI = true;
    public bool setupVRHands = true;
    
    [Header("=== 患者位置 ===")]
    public Vector3 patientPosition = new Vector3(0, 0.5f, 2f);
    
    [Header("=== 生成的对象（自动填充） ===")]
    public GameObject patient;
    public GameObject systemManager;
    public GameObject uiManager;
    
    [ContextMenu("🚀 一键设置CPR系统")]
    public void SetupCPRSystem()
    {
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Debug.Log("🚀 开始设置CPR训练系统...");
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━");
        
        if (createPatient)
        {
            CreatePatientModel();
        }
        
        if (createManagers)
        {
            CreateSystemManagers();
        }
        
        if (createUI)
        {
            CreateUISystem();
        }
        
        if (setupVRHands)
        {
            SetupVRHandColliders();
        }
        
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Debug.Log("✅ CPR训练系统设置完成！");
        Debug.Log("📝 下一步：点击播放按钮 ▶️ 测试系统");
        Debug.Log("🎮 如果在Quest设备上，手部现在可以触碰胸部了！");
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━");
    }
    
    void CreatePatientModel()
    {
        Debug.Log("👤 正在创建患者模型...");
        
        // 检查是否已存在
        patient = GameObject.Find("Patient");
        if (patient != null)
        {
            Debug.LogWarning("⚠️ 患者模型已存在，跳过创建");
            return;
        }
        
        // 创建患者根对象
        patient = new GameObject("Patient");
        patient.transform.position = patientPosition;
        patient.transform.rotation = Quaternion.Euler(0, 0, 90); // 躺平
        
        // 创建身体
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(patient.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(0.5f, 0.8f, 0.5f);
        
        // 设置身体材质
        Renderer bodyRenderer = body.GetComponent<Renderer>();
        if (bodyRenderer != null && bodyRenderer.material != null)
        {
            bodyRenderer.material.color = new Color(0.9f, 0.8f, 0.7f); // 肤色
        }
        
        // 创建头部
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(patient.transform);
        head.transform.localPosition = new Vector3(0, 0.9f, 0);
        head.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        
        // 设置头部材质
        Renderer headRenderer = head.GetComponent<Renderer>();
        if (headRenderer != null && headRenderer.material != null)
        {
            headRenderer.material.color = new Color(0.9f, 0.8f, 0.7f);
        }
        
        // 创建胸部按压区域
        GameObject chest = GameObject.CreatePrimitive(PrimitiveType.Cube);
        chest.name = "ChestArea";
        chest.transform.SetParent(patient.transform);
        chest.transform.localPosition = new Vector3(0, 0.2f, 0);
        chest.transform.localScale = new Vector3(0.3f, 0.1f, 0.2f);
        
        // 设置胸部材质（半透明绿色）
        Renderer chestRenderer = chest.GetComponent<Renderer>();
        if (chestRenderer != null && chestRenderer.material != null)
        {
            chestRenderer.material.color = new Color(0.5f, 1f, 0.5f, 0.5f);
        }
        
        // 添加Rigidbody
        Rigidbody rb = chest.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = chest.AddComponent<Rigidbody>();
        }
        rb.isKinematic = false;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeAll;
        
        // 添加按压检测脚本
        SimpleMetaXRChest chestScript = chest.GetComponent<SimpleMetaXRChest>();
        if (chestScript == null)
        {
            chestScript = chest.AddComponent<SimpleMetaXRChest>();
        }
        chestScript.chestTarget = chest.transform;
        chestScript.maxPressDepth = 0.06f;
        chestScript.recoverySpeed = 5f;
        
        Debug.Log("✅ 患者模型创建完成");
    }
    
    void CreateSystemManagers()
    {
        Debug.Log("⚙️ 正在创建系统管理器...");
        
        // 检查是否已存在
        systemManager = GameObject.Find("SystemManager");
        if (systemManager == null)
        {
            systemManager = new GameObject("SystemManager");
            Debug.Log("✅ 创建了 SystemManager 对象");
        }
        else
        {
            Debug.LogWarning("⚠️ SystemManager 已存在");
        }
        
        // 添加数据管理器
        if (systemManager.GetComponent<PressDataManager>() == null)
        {
            systemManager.AddComponent<PressDataManager>();
            Debug.Log("✅ 添加了 PressDataManager");
        }
        
        // 添加CPR训练管理器
        if (systemManager.GetComponent<CPRTrainingManager>() == null)
        {
            systemManager.AddComponent<CPRTrainingManager>();
            Debug.Log("✅ 添加了 CPRTrainingManager");
        }
        
        // 添加场景管理器
        if (systemManager.GetComponent<ScenarioManager>() == null)
        {
            systemManager.AddComponent<ScenarioManager>();
            Debug.Log("✅ 添加了 ScenarioManager");
        }
        
        // 添加手势检测器
        if (systemManager.GetComponent<MetaXRGestureDetector>() == null)
        {
            MetaXRGestureDetector detector = systemManager.AddComponent<MetaXRGestureDetector>();
            Debug.Log("✅ 添加了 MetaXRGestureDetector");
            
            // 如果胸部区域存在，设置引用
            if (patient != null)
            {
                Transform chestArea = patient.transform.Find("ChestArea");
                if (chestArea != null)
                {
                    detector.SetChestTarget(chestArea);
                    Debug.Log("✅ 已连接手势检测器和胸部区域");
                }
            }
        }
        
        Debug.Log("✅ 系统管理器创建完成");
    }
    
    void CreateUISystem()
    {
        Debug.Log("🎨 正在创建UI系统...");
        
        // 检查是否已存在
        uiManager = GameObject.Find("SimplePressUI");
        if (uiManager == null)
        {
            uiManager = new GameObject("SimplePressUI");
            Debug.Log("✅ 创建了 SimplePressUI 对象");
        }
        else
        {
            Debug.LogWarning("⚠️ SimplePressUI 已存在");
        }
        
        // 添加SimplePressUI组件
        if (uiManager.GetComponent<SimplePressUI>() == null)
        {
            uiManager.AddComponent<SimplePressUI>();
            Debug.Log("✅ 添加了 SimplePressUI 组件");
        }
        
        Debug.Log("✅ UI系统创建完成");
    }
    
    [ContextMenu("🧹 清理所有CPR对象")]
    public void CleanupAll()
    {
        Debug.Log("🧹 开始清理CPR训练对象...");
        
        // 清理患者
        GameObject patientObj = GameObject.Find("Patient");
        if (patientObj != null)
        {
            DestroyImmediate(patientObj);
            Debug.Log("🧹 已删除 Patient");
        }
        
        // 清理系统管理器
        GameObject managerObj = GameObject.Find("SystemManager");
        if (managerObj != null)
        {
            DestroyImmediate(managerObj);
            Debug.Log("🧹 已删除 SystemManager");
        }
        
        // 清理UI
        GameObject uiObj = GameObject.Find("SimplePressUI");
        if (uiObj != null)
        {
            DestroyImmediate(uiObj);
            Debug.Log("🧹 已删除 SimplePressUI");
        }
        
        // 清理UI Canvas
        GameObject canvasObj = GameObject.Find("SimplePressUI_Canvas");
        if (canvasObj != null)
        {
            DestroyImmediate(canvasObj);
            Debug.Log("🧹 已删除 SimplePressUI_Canvas");
        }
        
        // 清理测试球体
        GameObject testObj = GameObject.Find("TestHand");
        if (testObj != null)
        {
            DestroyImmediate(testObj);
            Debug.Log("🧹 已删除 TestHand");
        }
        
        // 重置引用
        patient = null;
        systemManager = null;
        uiManager = null;
        
        Debug.Log("✅ 清理完成");
    }
    
    [ContextMenu("📊 显示系统状态")]
    public void ShowStatus()
    {
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Debug.Log("📊 CPR训练系统状态");
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━");
        
        // 检查患者模型
        GameObject patientObj = GameObject.Find("Patient");
        Debug.Log($"患者模型: {(patientObj != null ? "✅ 存在" : "❌ 不存在")}");
        
        // 检查系统管理器
        PressDataManager dataManager = FindObjectOfType<PressDataManager>();
        Debug.Log($"数据管理器: {(dataManager != null ? "✅ 存在" : "❌ 不存在")}");
        
        CPRTrainingManager cprManager = FindObjectOfType<CPRTrainingManager>();
        Debug.Log($"CPR训练管理器: {(cprManager != null ? "✅ 存在" : "❌ 不存在")}");
        
        ScenarioManager scenarioManager = FindObjectOfType<ScenarioManager>();
        Debug.Log($"场景管理器: {(scenarioManager != null ? "✅ 存在" : "❌ 不存在")}");
        
        MetaXRGestureDetector gestureDetector = FindObjectOfType<MetaXRGestureDetector>();
        Debug.Log($"手势检测器: {(gestureDetector != null ? "✅ 存在" : "❌ 不存在")}");
        
        // 检查UI
        SimplePressUI ui = FindObjectOfType<SimplePressUI>();
        Debug.Log($"UI系统: {(ui != null ? "✅ 存在" : "❌ 不存在")}");
        
        // 检查XR手部
        GameObject leftHand = GameObject.Find("LeftHandAnchor");
        GameObject rightHand = GameObject.Find("RightHandAnchor");
        Debug.Log($"XR手部锚点: {(leftHand != null && rightHand != null ? "✅ 存在" : "❌ 不存在")}");
        
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━");
        
        if (patientObj != null && dataManager != null && ui != null)
        {
            Debug.Log("✅ 系统已就绪，可以开始测试！");
        }
        else
        {
            Debug.LogWarning("⚠️ 系统未完全设置，请运行 '一键设置CPR系统'");
        }
    }
    
    [ContextMenu("🎾 创建测试球体")]
    public void CreateTestBall()
    {
        GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "TestHand";
        ball.transform.position = new Vector3(0, 2f, 2f);
        ball.transform.localScale = Vector3.one * 0.2f;
        
        Rigidbody rb = ball.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.useGravity = true;
        
        Debug.Log("✅ 测试球体创建完成！");
        Debug.Log("📝 点击播放按钮，球体会掉落并触发按压检测");
    }
    
    void SetupVRHandColliders()
    {
        Debug.Log("🤚 正在设置VR手部碰撞器...");
        
        // 查找或创建VRHandSetup对象
        GameObject vrHandSetupGO = GameObject.Find("VRHandSetup");
        if (vrHandSetupGO == null)
        {
            vrHandSetupGO = new GameObject("VRHandSetup");
        }
        
        VRHandSetup handSetup = vrHandSetupGO.GetComponent<VRHandSetup>();
        if (handSetup == null)
        {
            handSetup = vrHandSetupGO.AddComponent<VRHandSetup>();
        }
        
        // 执行手部设置
        handSetup.SetupVRHands();
        
        Debug.Log("✅ VR手部碰撞器设置完成");
    }
    
    [ContextMenu("🤚 单独设置VR手部")]
    public void SetupVRHandsOnly()
    {
        SetupVRHandColliders();
    }
}

