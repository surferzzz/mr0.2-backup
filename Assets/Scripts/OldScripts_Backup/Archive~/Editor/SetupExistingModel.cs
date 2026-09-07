#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// 为现有的人物模型快速配置CPR按压功能
/// 使用方法：选中您的人物模型 → 右键 → CPR设置 → 配置为可按压患者
/// </summary>
public class SetupExistingModel : MonoBehaviour
{
    [MenuItem("GameObject/CPR设置/⚡ 配置选中模型为可按压患者", false, 0)]
    public static void SetupSelectedModelAsPatient()
    {
        GameObject selectedObject = Selection.activeGameObject;
        
        if (selectedObject == null)
        {
            EditorUtility.DisplayDialog("提示", 
                "请先在 Hierarchy 中选中您的人物模型！\n\n" +
                "步骤：\n" +
                "1. 在 Hierarchy 中点击选中人物模型\n" +
                "2. 右键 → CPR设置 → 配置选中模型为可按压患者", 
                "知道了");
            return;
        }
        
        Debug.Log("╔══════════════════════════════════════════════════════════════╗");
        Debug.Log("║          🚀 开始配置现有模型为可按压患者                     ║");
        Debug.Log("╚══════════════════════════════════════════════════════════════╝");
        Debug.Log($"选中的对象: {selectedObject.name}");
        Debug.Log("");
        
        // 1. 创建或找到胸部区域
        Transform chestTransform = FindOrCreateChestArea(selectedObject.transform);
        
        // 2. 配置胸部碰撞和按压检测
        SetupChestCollision(chestTransform.gameObject);
        
        // 3. 创建系统管理器
        SetupManagers();
        
        // 4. 创建UI系统
        SetupUI();
        
        // 5. 设置VR手部（如果需要）
        SetupVRHands();
        
        // 6. 自动修复问题
        AutoFixIssues();
        
        Debug.Log("");
        Debug.Log("╔══════════════════════════════════════════════════════════════╗");
        Debug.Log("║                    ✅ 配置完成！                              ║");
        Debug.Log("╚══════════════════════════════════════════════════════════════╝");
        Debug.Log("");
        Debug.Log("📋 下一步操作：");
        Debug.Log("  1. 点击 Unity 播放按钮 ▶️");
        Debug.Log("  2. 在 Scene 视图中找到绿色的胸部区域");
        Debug.Log("  3. 使用测试球或VR手部触碰胸部");
        Debug.Log("  4. 观察UI显示按压信息");
        Debug.Log("");
        Debug.Log("💡 快速测试：");
        Debug.Log("  打开 CPR控制面板 → 点击 '🎾 创建测试球体'");
        Debug.Log("");
        
        EditorUtility.DisplayDialog("配置完成！✅", 
            "您的人物模型已配置为可按压患者！\n\n" +
            "绿色区域 = 胸部按压区\n\n" +
            "下一步：\n" +
            "1. 点击播放按钮 ▶️ 测试\n" +
            "2. 或打开 CPR控制面板 创建测试球", 
            "开始测试！");
    }
    
    static Transform FindOrCreateChestArea(Transform modelRoot)
    {
        Debug.Log("🔍 查找或创建胸部区域...");
        
        // 尝试查找现有的胸部骨骼
        Transform chest = FindChestBone(modelRoot);
        
        if (chest != null)
        {
            Debug.Log($"✅ 找到胸部骨骼: {chest.name}");
            return chest;
        }
        
        // 如果没找到，创建一个胸部区域
        GameObject chestArea = new GameObject("ChestArea");
        chestArea.transform.SetParent(modelRoot);
        
        // 根据模型的大小自动定位胸部
        Renderer renderer = modelRoot.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            Bounds bounds = renderer.bounds;
            // 胸部通常在模型中上部
            chestArea.transform.position = new Vector3(
                bounds.center.x,
                bounds.center.y + bounds.extents.y * 0.3f,
                bounds.center.z
            );
        }
        else
        {
            chestArea.transform.localPosition = new Vector3(0, 1.2f, 0);
        }
        
        Debug.Log($"✅ 创建胸部区域: {chestArea.name} at {chestArea.transform.position}");
        
        Undo.RegisterCreatedObjectUndo(chestArea, "Create Chest Area");
        
        return chestArea.transform;
    }
    
    static Transform FindChestBone(Transform root)
    {
        // 常见的胸部骨骼名称
        string[] chestNames = new string[]
        {
            "chest", "Chest", "CHEST",
            "spine2", "Spine2", "SPINE2",
            "spine3", "Spine3", "SPINE3",
            "upperchest", "UpperChest", "UPPERCHEST",
            "mixamorig:Spine2", "mixamorig:Spine1"
        };
        
        // 递归查找
        foreach (string name in chestNames)
        {
            Transform found = root.Find(name);
            if (found != null) return found;
            
            // 深度搜索
            found = FindTransformByName(root, name);
            if (found != null) return found;
        }
        
        return null;
    }
    
    static Transform FindTransformByName(Transform parent, string name)
    {
        if (parent.name.ToLower().Contains(name.ToLower()))
        {
            return parent;
        }
        
        foreach (Transform child in parent)
        {
            Transform result = FindTransformByName(child, name);
            if (result != null) return result;
        }
        
        return null;
    }
    
    static void SetupChestCollision(GameObject chestObject)
    {
        Debug.Log("⚙️ 配置胸部碰撞和按压检测...");
        
        // 添加碰撞器
        BoxCollider collider = chestObject.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = chestObject.AddComponent<BoxCollider>();
        }
        collider.size = new Vector3(0.3f, 0.1f, 0.2f);
        collider.isTrigger = false;
        
        // 添加刚体
        Rigidbody rb = chestObject.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = chestObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true; // 不受物理影响，但可以检测碰撞
        rb.useGravity = false;
        
        // 添加按压检测脚本
        SimpleMetaXRChest chestScript = chestObject.GetComponent<SimpleMetaXRChest>();
        if (chestScript == null)
        {
            chestScript = chestObject.AddComponent<SimpleMetaXRChest>();
        }
        
        // 设置视觉标识（绿色材质）
        Renderer renderer = chestObject.GetComponent<Renderer>();
        if (renderer == null)
        {
            // 添加视觉对象
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "ChestVisual";
            visual.transform.SetParent(chestObject.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = new Vector3(0.3f, 0.1f, 0.2f);
            
            // 移除碰撞器（父对象已有）
            Collider visualCollider = visual.GetComponent<Collider>();
            if (visualCollider != null)
            {
                Object.DestroyImmediate(visualCollider);
            }
            
            renderer = visual.GetComponent<Renderer>();
        }
        
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.2f, 1f, 0.2f, 0.5f); // 半透明绿色
            mat.SetFloat("_Mode", 3); // 透明模式
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            renderer.material = mat;
        }
        
        // 添加标签
        chestObject.tag = "Chest";
        
        Debug.Log("✅ 胸部碰撞和按压检测配置完成");
    }
    
    static void SetupManagers()
    {
        Debug.Log("⚙️ 配置系统管理器...");
        
        // 查找或创建 SystemManager
        GameObject managerGO = GameObject.Find("SystemManager");
        if (managerGO == null)
        {
            managerGO = new GameObject("SystemManager");
            Undo.RegisterCreatedObjectUndo(managerGO, "Create System Manager");
        }
        
        // 添加 PressDataManager
        if (managerGO.GetComponent<PressDataManager>() == null)
        {
            managerGO.AddComponent<PressDataManager>();
        }
        
        // 添加 CPRTrainingManager
        if (managerGO.GetComponent<CPRTrainingManager>() == null)
        {
            managerGO.AddComponent<CPRTrainingManager>();
        }
        
        // 添加 ScenarioManager
        if (managerGO.GetComponent<ScenarioManager>() == null)
        {
            managerGO.AddComponent<ScenarioManager>();
        }
        
        Debug.Log("✅ 系统管理器配置完成");
    }
    
    static void SetupUI()
    {
        Debug.Log("⚙️ 配置UI系统...");
        
        // 查找或创建 SimplePressUI
        SimplePressUI ui = Object.FindObjectOfType<SimplePressUI>();
        if (ui == null)
        {
            GameObject uiGO = new GameObject("SimplePressUI");
            ui = uiGO.AddComponent<SimplePressUI>();
            Undo.RegisterCreatedObjectUndo(uiGO, "Create UI System");
        }
        
        Debug.Log("✅ UI系统配置完成");
    }
    
    static void SetupVRHands()
    {
        Debug.Log("⚙️ 配置VR手部...");
        
        // 查找或创建 VRHandSetup
        VRHandSetup handSetup = Object.FindObjectOfType<VRHandSetup>();
        if (handSetup == null)
        {
            GameObject handGO = new GameObject("VRHandSetup");
            handSetup = handGO.AddComponent<VRHandSetup>();
            Undo.RegisterCreatedObjectUndo(handGO, "Create VR Hand Setup");
        }
        
        // 执行手部设置
        handSetup.SetupVRHands();
        
        Debug.Log("✅ VR手部配置完成");
    }
    
    static void AutoFixIssues()
    {
        Debug.Log("⚙️ 自动修复常见问题...");
        
        QuickErrorFix errorFix = Object.FindObjectOfType<QuickErrorFix>();
        if (errorFix == null)
        {
            GameObject fixGO = new GameObject("QuickErrorFix");
            errorFix = fixGO.AddComponent<QuickErrorFix>();
            Undo.RegisterCreatedObjectUndo(fixGO, "Create Error Fix");
        }
        
        errorFix.FixAllIssues();
        
        Debug.Log("✅ 问题修复完成");
    }
    
    // 在菜单中添加快捷方式
    [MenuItem("CPR训练系统/🎯 配置现有模型为患者", false, 2)]
    public static void MenuSetupExistingModel()
    {
        if (Selection.activeGameObject != null)
        {
            SetupSelectedModelAsPatient();
        }
        else
        {
            EditorUtility.DisplayDialog("请选择模型", 
                "请先在 Hierarchy 中选中您的人物模型！\n\n" +
                "步骤：\n" +
                "1. 在 Hierarchy 中点击选中人物模型\n" +
                "2. 再次点击这个菜单", 
                "知道了");
        }
    }
}
#endif

