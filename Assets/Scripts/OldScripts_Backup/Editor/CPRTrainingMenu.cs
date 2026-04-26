using UnityEngine;
using UnityEditor;

/// <summary>
/// CPR训练系统的编辑器菜单
/// </summary>
public class CPRTrainingMenu
{
    [MenuItem("CPR训练/1. 快速设置整个系统 ⚡", false, 1)]
    public static void QuickSetupAll()
    {
        Debug.Log("🚀 开始自动设置CPR训练系统...");
        
        // 创建或查找CPRSetup对象
        GameObject setupGO = GameObject.Find("CPRSetup");
        if (setupGO == null)
        {
            setupGO = new GameObject("CPRSetup");
            Debug.Log("✅ 创建了 CPRSetup 对象");
        }
        
        // 添加QuickSetupCPR组件
        QuickSetupCPR quickSetup = setupGO.GetComponent<QuickSetupCPR>();
        if (quickSetup == null)
        {
            quickSetup = setupGO.AddComponent<QuickSetupCPR>();
            Debug.Log("✅ 添加了 QuickSetupCPR 组件");
        }
        
        // 执行快速设置
        quickSetup.QuickSetup();
        
        // 选中创建的对象
        Selection.activeGameObject = setupGO;
        
        Debug.Log("✅ 自动设置完成！请查看Hierarchy中的生成对象");
        Debug.Log("📝 下一步：点击播放按钮测试系统");
    }
    
    [MenuItem("CPR训练/2. 只创建患者模型 👤", false, 2)]
    public static void CreatePatientOnly()
    {
        Debug.Log("👤 创建患者模型...");
        
        // 创建患者根对象
        GameObject patient = new GameObject("Patient");
        patient.transform.position = new Vector3(0, 0.5f, 2f);
        patient.transform.rotation = Quaternion.Euler(0, 0, 90);
        
        // 创建身体
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(patient.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(0.5f, 0.8f, 0.5f);
        
        // 设置材质
        Renderer bodyRenderer = body.GetComponent<Renderer>();
        if (bodyRenderer != null)
        {
            Material bodyMat = new Material(Shader.Find("Standard"));
            bodyMat.color = new Color(0.9f, 0.8f, 0.7f);
            bodyRenderer.material = bodyMat;
        }
        
        // 创建头部
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(patient.transform);
        head.transform.localPosition = new Vector3(0, 0.9f, 0);
        head.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        
        // 创建胸部区域
        GameObject chest = GameObject.CreatePrimitive(PrimitiveType.Cube);
        chest.name = "ChestArea";
        chest.transform.SetParent(patient.transform);
        chest.transform.localPosition = new Vector3(0, 0.2f, 0);
        chest.transform.localScale = new Vector3(0.3f, 0.1f, 0.2f);
        
        // 添加组件
        Rigidbody rb = chest.AddComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeAll;
        
        SimpleMetaXRChest chestScript = chest.AddComponent<SimpleMetaXRChest>();
        chestScript.chestTarget = chest.transform;
        
        // 设置材质
        Renderer chestRenderer = chest.GetComponent<Renderer>();
        if (chestRenderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.5f, 1f, 0.5f, 0.5f);
            chestRenderer.material = mat;
        }
        
        Selection.activeGameObject = patient;
        Undo.RegisterCreatedObjectUndo(patient, "Create Patient Model");
        Debug.Log("✅ 患者模型创建完成！");
    }
    
    [MenuItem("CPR训练/3. 只创建UI系统 🎨", false, 3)]
    public static void CreateUIOnly()
    {
        Debug.Log("🎨 创建UI系统...");
        
        GameObject uiGO = GameObject.Find("SimplePressUI");
        if (uiGO == null)
        {
            uiGO = new GameObject("SimplePressUI");
            Undo.RegisterCreatedObjectUndo(uiGO, "Create UI System");
        }
        
        SimplePressUI ui = uiGO.GetComponent<SimplePressUI>();
        if (ui == null)
        {
            ui = uiGO.AddComponent<SimplePressUI>();
        }
        
        Selection.activeGameObject = uiGO;
        Debug.Log("✅ UI系统创建完成！");
    }
    
    [MenuItem("CPR训练/4. 只创建系统管理器 ⚙️", false, 4)]
    public static void CreateManagersOnly()
    {
        Debug.Log("⚙️ 创建系统管理器...");
        
        GameObject managerGO = GameObject.Find("SystemManager");
        if (managerGO == null)
        {
            managerGO = new GameObject("SystemManager");
            Undo.RegisterCreatedObjectUndo(managerGO, "Create System Manager");
        }
        
        // 添加所有管理器组件
        if (managerGO.GetComponent<PressDataManager>() == null)
        {
            managerGO.AddComponent<PressDataManager>();
            Debug.Log("✅ 添加了 PressDataManager");
        }
        
        if (managerGO.GetComponent<CPRTrainingManager>() == null)
        {
            managerGO.AddComponent<CPRTrainingManager>();
            Debug.Log("✅ 添加了 CPRTrainingManager");
        }
        
        if (managerGO.GetComponent<ScenarioManager>() == null)
        {
            managerGO.AddComponent<ScenarioManager>();
            Debug.Log("✅ 添加了 ScenarioManager");
        }
        
        if (managerGO.GetComponent<MetaXRGestureDetector>() == null)
        {
            managerGO.AddComponent<MetaXRGestureDetector>();
            Debug.Log("✅ 添加了 MetaXRGestureDetector");
        }
        
        Selection.activeGameObject = managerGO;
        Debug.Log("✅ 系统管理器创建完成！");
    }
    
    [MenuItem("CPR训练/5. 显示系统状态 📊", false, 5)]
    public static void ShowSystemStatus()
    {
        Debug.Log("=== CPR训练系统状态 ===");
        
        GameObject patient = GameObject.Find("Patient");
        Debug.Log($"患者模型: {(patient != null ? "✅ 存在" : "❌ 不存在")}");
        
        PressDataManager dataManager = Object.FindObjectOfType<PressDataManager>();
        Debug.Log($"数据管理器: {(dataManager != null ? "✅ 存在" : "❌ 不存在")}");
        
        SimplePressUI ui = Object.FindObjectOfType<SimplePressUI>();
        Debug.Log($"UI系统: {(ui != null ? "✅ 存在" : "❌ 不存在")}");
        
        CPRTrainingManager cprManager = Object.FindObjectOfType<CPRTrainingManager>();
        Debug.Log($"CPR训练管理器: {(cprManager != null ? "✅ 存在" : "❌ 不存在")}");
        
        GameObject leftHand = GameObject.Find("LeftHandAnchor");
        GameObject rightHand = GameObject.Find("RightHandAnchor");
        Debug.Log($"XR手部锚点: {(leftHand != null && rightHand != null ? "✅ 存在" : "❌ 不存在")}");
        
        Debug.Log("======================");
    }
    
    [MenuItem("CPR训练/6. 清理所有生成对象 🧹", false, 6)]
    public static void CleanupAll()
    {
        if (EditorUtility.DisplayDialog("确认清理", 
            "这将删除所有自动生成的CPR训练对象。\n是否继续？", 
            "确定", "取消"))
        {
            Debug.Log("🧹 开始清理...");
            
            // 清理患者模型
            GameObject patient = GameObject.Find("Patient");
            if (patient != null)
            {
                Undo.DestroyObjectImmediate(patient);
                Debug.Log("🧹 已删除患者模型");
            }
            
            // 清理系统管理器
            GameObject manager = GameObject.Find("SystemManager");
            if (manager != null)
            {
                Undo.DestroyObjectImmediate(manager);
                Debug.Log("🧹 已删除系统管理器");
            }
            
            // 清理UI
            GameObject ui = GameObject.Find("SimplePressUI");
            if (ui != null)
            {
                Undo.DestroyObjectImmediate(ui);
                Debug.Log("🧹 已删除UI管理器");
            }
            
            GameObject canvas = GameObject.Find("SimplePressUI_Canvas");
            if (canvas != null)
            {
                Undo.DestroyObjectImmediate(canvas);
                Debug.Log("🧹 已删除UI Canvas");
            }
            
            // 清理设置对象
            GameObject setup = GameObject.Find("CPRSetup");
            if (setup != null)
            {
                Undo.DestroyObjectImmediate(setup);
                Debug.Log("🧹 已删除CPRSetup");
            }
            
            // 清理测试球体
            GameObject testSphere = GameObject.Find("TestHand");
            if (testSphere != null)
            {
                Undo.DestroyObjectImmediate(testSphere);
                Debug.Log("🧹 已删除测试球体");
            }
            
            Debug.Log("✅ 清理完成！");
        }
    }
    
    [MenuItem("CPR训练/7. 创建测试球体 🎾", false, 7)]
    public static void CreateTestSphere()
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "TestHand";
        sphere.transform.position = new Vector3(0, 2f, 2f);
        sphere.transform.localScale = Vector3.one * 0.2f;
        
        Rigidbody rb = sphere.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.useGravity = true;
        
        Selection.activeGameObject = sphere;
        Undo.RegisterCreatedObjectUndo(sphere, "Create Test Sphere");
        Debug.Log("✅ 测试球体创建完成！运行场景后它会掉落并触发按压");
    }
    
    [MenuItem("CPR训练/帮助文档 📚", false, 100)]
    public static void OpenDocumentation()
    {
        string docPath = Application.dataPath + "/model mixamo/scripts/README_快速开始.md";
        if (System.IO.File.Exists(docPath))
        {
            Application.OpenURL("file:///" + docPath);
            Debug.Log("📚 打开帮助文档");
        }
        else
        {
            Debug.LogWarning("⚠️ 未找到帮助文档");
        }
    }
}


