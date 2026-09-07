using UnityEngine;

/// <summary>
/// 快速设置CPR训练系统的辅助工具
/// 在Unity编辑器中右键点击此脚本，选择"快速设置CPR系统"
/// </summary>
public class QuickSetupCPR : MonoBehaviour
{
    [Header("=== 快速设置选项 ===")]
    [SerializeField] private bool createPatientModel = true;
    [SerializeField] private bool createSystemManagers = true;
    [SerializeField] private bool createUI = true;
    [SerializeField] private bool setupXRHands = true;
    
    [Header("=== 患者模型设置 ===")]
    [SerializeField] private Vector3 patientPosition = new Vector3(0, 0.5f, 2f);
    [SerializeField] private Vector3 patientRotation = new Vector3(0, 0, 90);
    
    [Header("=== 生成的对象引用 ===")]
    public GameObject patientModel;
    public GameObject chestArea;
    public GameObject systemManager;
    
    [ContextMenu("🚀 快速设置CPR系统")]
    public void QuickSetup()
    {
        Debug.Log("🚀 开始快速设置CPR训练系统...");
        
        if (createPatientModel)
        {
            CreatePatientModel();
        }
        
        if (createSystemManagers)
        {
            CreateSystemManagers();
        }
        
        if (createUI)
        {
            SetupUISystem();
        }
        
        if (setupXRHands)
        {
            SetupXRHandTracking();
        }
        
        Debug.Log("✅ CPR训练系统设置完成！");
        Debug.Log("📝 下一步：运行场景开始训练");
    }
    
    void CreatePatientModel()
    {
        Debug.Log("👤 创建患者模型...");
        
        // 创建患者根对象
        patientModel = new GameObject("Patient");
        patientModel.transform.position = patientPosition;
        patientModel.transform.rotation = Quaternion.Euler(patientRotation);
        
        // 创建身体（胶囊体）
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(patientModel.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localRotation = Quaternion.identity;
        body.transform.localScale = new Vector3(0.5f, 0.8f, 0.5f);
        
        // 设置身体材质
        Renderer bodyRenderer = body.GetComponent<Renderer>();
        if (bodyRenderer != null)
        {
            Material bodyMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            bodyMat.color = new Color(0.9f, 0.8f, 0.7f); // 肤色
            bodyRenderer.material = bodyMat;
        }
        
        // 创建头部
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(patientModel.transform);
        head.transform.localPosition = new Vector3(0, 0.9f, 0);
        head.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        
        // 设置头部材质
        Renderer headRenderer = head.GetComponent<Renderer>();
        if (headRenderer != null)
        {
            Material headMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            headMat.color = new Color(0.9f, 0.8f, 0.7f);
            headRenderer.material = headMat;
        }
        
        // 创建胸部按压区域
        CreateChestArea();
        
        Debug.Log("✅ 患者模型创建完成");
    }
    
    void CreateChestArea()
    {
        Debug.Log("🫁 创建胸部按压区域...");
        
        chestArea = GameObject.CreatePrimitive(PrimitiveType.Cube);
        chestArea.name = "ChestArea";
        chestArea.transform.SetParent(patientModel.transform);
        chestArea.transform.localPosition = new Vector3(0, 0.2f, 0);
        chestArea.transform.localScale = new Vector3(0.3f, 0.1f, 0.2f);
        
        // 设置材质（半透明绿色，用于调试）
        Renderer chestRenderer = chestArea.GetComponent<Renderer>();
        if (chestRenderer != null)
        {
            Material chestMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            chestMat.color = new Color(0.5f, 1f, 0.5f, 0.5f);
            chestMat.SetFloat("_Surface", 1); // Transparent
            chestRenderer.material = chestMat;
        }
        
        // 添加Rigidbody
        Rigidbody rb = chestArea.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = chestArea.AddComponent<Rigidbody>();
        }
        rb.isKinematic = false;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeAll;
        
        // 确保有BoxCollider
        BoxCollider collider = chestArea.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = chestArea.AddComponent<BoxCollider>();
        }
        
        // 添加按压检测脚本
        SimpleMetaXRChest chestScript = chestArea.GetComponent<SimpleMetaXRChest>();
        if (chestScript == null)
        {
            chestScript = chestArea.AddComponent<SimpleMetaXRChest>();
        }
        
        // 配置脚本参数
        chestScript.chestTarget = chestArea.transform;
        chestScript.maxPressDepth = 0.06f; // 6cm
        chestScript.recoverySpeed = 5f;
        
        Debug.Log("✅ 胸部按压区域创建完成");
    }
    
    void CreateSystemManagers()
    {
        Debug.Log("⚙️ 创建系统管理器...");
        
        // 查找或创建SystemManager
        systemManager = GameObject.Find("SystemManager");
        if (systemManager == null)
        {
            systemManager = new GameObject("SystemManager");
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
            CPRTrainingManager cprManager = systemManager.AddComponent<CPRTrainingManager>();
            Debug.Log("✅ 添加了 CPRTrainingManager");
            
            // 设置引用
            if (patientModel != null)
            {
                // 通过反射或公共方法设置引用
            }
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
            MetaXRGestureDetector gestureDetector = systemManager.AddComponent<MetaXRGestureDetector>();
            Debug.Log("✅ 添加了 MetaXRGestureDetector");
            
            // 设置胸部目标
            if (chestArea != null)
            {
                gestureDetector.SetChestTarget(chestArea.transform);
            }
        }
        
        Debug.Log("✅ 系统管理器创建完成");
    }
    
    void SetupUISystem()
    {
        Debug.Log("🎨 设置UI系统...");
        
        // 查找或创建UI管理器
        GameObject uiManagerGO = GameObject.Find("SimplePressUI");
        if (uiManagerGO == null)
        {
            uiManagerGO = new GameObject("SimplePressUI");
        }
        
        // 添加SimplePressUI组件
        SimplePressUI simpleUI = uiManagerGO.GetComponent<SimplePressUI>();
        if (simpleUI == null)
        {
            simpleUI = uiManagerGO.AddComponent<SimplePressUI>();
            Debug.Log("✅ 添加了 SimplePressUI");
        }
        
        Debug.Log("✅ UI系统设置完成");
    }
    
    void SetupXRHandTracking()
    {
        Debug.Log("🤚 设置XR手部跟踪...");
        
        // 查找XR相关对象
        GameObject leftHandAnchor = GameObject.Find("LeftHandAnchor");
        GameObject rightHandAnchor = GameObject.Find("RightHandAnchor");
        
        if (leftHandAnchor != null && rightHandAnchor != null)
        {
            Debug.Log($"✅ 找到手部锚点: {leftHandAnchor.name}, {rightHandAnchor.name}");
            
            // 如果有手势检测器，设置引用
            MetaXRGestureDetector gestureDetector = FindObjectOfType<MetaXRGestureDetector>();
            if (gestureDetector != null)
            {
                gestureDetector.SetHandReferences(leftHandAnchor.transform, rightHandAnchor.transform);
                Debug.Log("✅ 手部引用已设置到手势检测器");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ 未找到手部锚点，请确保场景中有XR Camera Rig");
        }
        
        Debug.Log("✅ XR手部跟踪设置完成");
    }
    
    [ContextMenu("🧹 清理生成的对象")]
    public void CleanupGeneratedObjects()
    {
        if (patientModel != null)
        {
            DestroyImmediate(patientModel);
            Debug.Log("🧹 已删除患者模型");
        }
        
        if (systemManager != null)
        {
            DestroyImmediate(systemManager);
            Debug.Log("🧹 已删除系统管理器");
        }
        
        GameObject uiManager = GameObject.Find("SimplePressUI");
        if (uiManager != null)
        {
            DestroyImmediate(uiManager);
            Debug.Log("🧹 已删除UI管理器");
        }
        
        // 同时清理可能存在的Canvas
        GameObject uiCanvas = GameObject.Find("SimplePressUI_Canvas");
        if (uiCanvas != null)
        {
            DestroyImmediate(uiCanvas);
            Debug.Log("🧹 已删除UI Canvas");
        }
        
        Debug.Log("✅ 清理完成");
    }
    
    [ContextMenu("📊 显示系统状态")]
    public void ShowSystemStatus()
    {
        Debug.Log("=== CPR训练系统状态 ===");
        
        // 检查患者模型
        GameObject patient = GameObject.Find("Patient");
        Debug.Log($"患者模型: {(patient != null ? "✅ 存在" : "❌ 不存在")}");
        
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
        SimplePressUI simpleUI = FindObjectOfType<SimplePressUI>();
        Debug.Log($"UI管理器: {(simpleUI != null ? "✅ 存在" : "❌ 不存在")}");
        
        // 检查XR设置
        GameObject leftHand = GameObject.Find("LeftHandAnchor");
        GameObject rightHand = GameObject.Find("RightHandAnchor");
        Debug.Log($"XR手部锚点: {(leftHand != null && rightHand != null ? "✅ 存在" : "❌ 不存在")}");
        
        Debug.Log("======================");
    }
    
    [ContextMenu("🎮 开始训练")]
    public void StartTraining()
    {
        CPRTrainingManager cprManager = FindObjectOfType<CPRTrainingManager>();
        if (cprManager != null)
        {
            cprManager.StartCPRTraining();
            Debug.Log("🎮 CPR训练已开始！");
        }
        else
        {
            Debug.LogError("❌ 未找到CPR训练管理器，请先运行快速设置");
        }
    }
}
