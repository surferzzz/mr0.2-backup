#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// 专门为 Mixamo 模型（如 Remy）配置 CPR 按压功能
/// 自动识别骨骼结构，精确定位胸部
/// </summary>
public class SetupRemyModel : MonoBehaviour
{
    [MenuItem("CPR训练系统/🎯 配置 Remy 模型为患者", false, 3)]
    public static void SetupRemyAsPatient()
    {
        // 查找所有包含 "Remy" 的对象
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        GameObject remyModel = null;
        
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.ToLower().Contains("remy"))
            {
                // 优先选择根对象
                if (obj.transform.parent == null || obj.transform.parent.name.ToLower().Contains("scene"))
                {
                    remyModel = obj;
                    break;
                }
            }
        }
        
        if (remyModel == null)
        {
            // 如果没找到，尝试选中的对象
            if (Selection.activeGameObject != null)
            {
                remyModel = Selection.activeGameObject;
            }
            else
            {
                EditorUtility.DisplayDialog("未找到 Remy 模型", 
                    "请确认：\n\n" +
                    "1. 场景中有 Remy 模型\n" +
                    "2. 或在 Hierarchy 中选中 Remy 模型\n" +
                    "3. 然后重新点击此菜单", 
                    "知道了");
                return;
            }
        }
        
        Debug.Log("╔══════════════════════════════════════════════════════════════╗");
        Debug.Log("║          🎯 配置 Remy 模型为可按压患者                       ║");
        Debug.Log("╚══════════════════════════════════════════════════════════════╝");
        Debug.Log($"找到模型: {remyModel.name}");
        Debug.Log("");
        
        // 配置模型
        SetupMixamoModel(remyModel);
        
        Debug.Log("");
        Debug.Log("╔══════════════════════════════════════════════════════════════╗");
        Debug.Log("║                    ✅ 配置完成！                              ║");
        Debug.Log("╚══════════════════════════════════════════════════════════════╝");
        Debug.Log("");
        Debug.Log("📋 下一步：");
        Debug.Log("  1. 在 Scene 视图中查看绿色胸部区域");
        Debug.Log("  2. 如需调整位置，选中 ChestArea 拖动");
        Debug.Log("  3. 打开 CPR 控制面板测试");
        Debug.Log("");
        
        EditorUtility.DisplayDialog("Remy 模型配置完成！✅", 
            $"模型 {remyModel.name} 已配置完成！\n\n" +
            "✅ 胸部检测区已创建\n" +
            "✅ 按压系统已配置\n" +
            "✅ UI 系统已就绪\n\n" +
            "下一步：\n" +
            "1. 检查绿色胸部区位置\n" +
            "2. 创建测试球测试\n" +
            "3. 或 Build 到 Quest 3", 
            "开始测试！");
        
        // 选中模型并聚焦
        Selection.activeGameObject = remyModel;
        SceneView.FrameLastActiveSceneView();
    }
    
    static void SetupMixamoModel(GameObject model)
    {
        // 1. 查找 Mixamo 骨骼的胸部位置
        Transform chestBone = FindMixamoChestBone(model.transform);
        
        Vector3 chestPosition;
        
        if (chestBone != null)
        {
            Debug.Log($"✅ 找到胸部骨骼: {chestBone.name}");
            chestPosition = chestBone.position;
            
            // Mixamo 的 Spine2 或 Spine1 通常需要向前偏移一点
            // 因为骨骼在身体内部，我们需要检测区在皮肤表面
            chestPosition += chestBone.forward * 0.05f;
        }
        else
        {
            Debug.LogWarning("⚠️ 未找到标准骨骼，使用模型中心估算");
            // 使用模型的渲染边界估算胸部位置
            Renderer renderer = model.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                Bounds bounds = renderer.bounds;
                // 对于躺着的模型，胸部通常在中上部
                chestPosition = new Vector3(
                    bounds.center.x,
                    bounds.center.y + bounds.extents.y * 0.2f,
                    bounds.center.z + bounds.extents.z * 0.2f
                );
            }
            else
            {
                chestPosition = model.transform.position + Vector3.up * 0.5f;
            }
        }
        
        // 2. 创建或更新 ChestArea
        Transform existingChest = model.transform.Find("ChestArea");
        GameObject chestArea;
        
        if (existingChest != null)
        {
            Debug.Log("✅ 找到现有 ChestArea，更新配置");
            chestArea = existingChest.gameObject;
        }
        else
        {
            Debug.Log("✅ 创建新的 ChestArea");
            chestArea = new GameObject("ChestArea");
            chestArea.transform.SetParent(model.transform);
            Undo.RegisterCreatedObjectUndo(chestArea, "Create Chest Area");
        }
        
        chestArea.transform.position = chestPosition;
        
        // 3. 配置碰撞和检测
        SetupChestComponents(chestArea, model);
        
        // 4. 创建视觉标识
        CreateChestVisual(chestArea);
        
        // 5. 设置系统组件
        SetupSystemComponents();
        
        Debug.Log($"✅ ChestArea 位置: {chestPosition}");
    }
    
    static Transform FindMixamoChestBone(Transform root)
    {
        // Mixamo 标准骨骼名称
        string[] mixamoChestNames = new string[]
        {
            "mixamorig:Spine2",
            "mixamorig:Spine1",
            "mixamorig:Spine",
            "Spine2",
            "Spine1",
            "Spine",
            "chest",
            "Chest",
            "UpperChest"
        };
        
        foreach (string boneName in mixamoChestNames)
        {
            Transform bone = FindChildRecursive(root, boneName);
            if (bone != null)
            {
                return bone;
            }
        }
        
        return null;
    }
    
    static Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name == name)
        {
            return parent;
        }
        
        foreach (Transform child in parent)
        {
            Transform result = FindChildRecursive(child, name);
            if (result != null)
            {
                return result;
            }
        }
        
        return null;
    }
    
    static void SetupChestComponents(GameObject chestArea, GameObject model)
    {
        Debug.Log("⚙️ 配置胸部组件...");
        
        // 添加或更新碰撞器
        BoxCollider collider = chestArea.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = chestArea.AddComponent<BoxCollider>();
        }
        
        // 根据模型大小调整碰撞器
        Renderer renderer = model.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            float scale = renderer.bounds.size.magnitude * 0.1f;
            collider.size = new Vector3(scale, scale * 0.3f, scale * 0.6f);
        }
        else
        {
            collider.size = new Vector3(0.3f, 0.1f, 0.2f);
        }
        
        collider.isTrigger = false;
        
        // 添加刚体
        Rigidbody rb = chestArea.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = chestArea.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;
        
        // 添加按压检测脚本
        SimpleMetaXRChest chestScript = chestArea.GetComponent<SimpleMetaXRChest>();
        if (chestScript == null)
        {
            chestScript = chestArea.AddComponent<SimpleMetaXRChest>();
        }
        
        // 设置标签
        chestArea.tag = "Chest";
        
        Debug.Log("✅ 胸部组件配置完成");
    }
    
    static void CreateChestVisual(GameObject chestArea)
    {
        Debug.Log("🎨 创建视觉标识...");
        
        // 查找或创建视觉对象
        Transform existingVisual = chestArea.transform.Find("ChestVisual");
        GameObject visual;
        
        if (existingVisual != null)
        {
            visual = existingVisual.gameObject;
        }
        else
        {
            visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "ChestVisual";
            visual.transform.SetParent(chestArea.transform);
        }
        
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        
        // 使用父对象的碰撞器大小
        BoxCollider parentCollider = chestArea.GetComponent<BoxCollider>();
        if (parentCollider != null)
        {
            visual.transform.localScale = parentCollider.size;
        }
        else
        {
            visual.transform.localScale = new Vector3(0.3f, 0.1f, 0.2f);
        }
        
        // 移除视觉对象的碰撞器
        Collider visualCollider = visual.GetComponent<Collider>();
        if (visualCollider != null)
        {
            Object.DestroyImmediate(visualCollider);
        }
        
        // 设置半透明绿色材质
        Renderer renderer = visual.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.2f, 1f, 0.2f, 0.4f);
            
            // 设置为透明模式
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            
            renderer.material = mat;
        }
        
        Debug.Log("✅ 视觉标识创建完成");
    }
    
    static void SetupSystemComponents()
    {
        Debug.Log("⚙️ 配置系统组件...");
        
        // 查找或创建系统管理器
        GameObject managerGO = GameObject.Find("SystemManager");
        if (managerGO == null)
        {
            managerGO = new GameObject("SystemManager");
            Undo.RegisterCreatedObjectUndo(managerGO, "Create System Manager");
        }
        
        // 添加必要的管理器
        if (managerGO.GetComponent<PressDataManager>() == null)
        {
            managerGO.AddComponent<PressDataManager>();
        }
        
        if (managerGO.GetComponent<CPRTrainingManager>() == null)
        {
            managerGO.AddComponent<CPRTrainingManager>();
        }
        
        if (managerGO.GetComponent<ScenarioManager>() == null)
        {
            managerGO.AddComponent<ScenarioManager>();
        }
        
        // 查找或创建 UI
        SimplePressUI ui = Object.FindObjectOfType<SimplePressUI>();
        if (ui == null)
        {
            GameObject uiGO = new GameObject("SimplePressUI");
            ui = uiGO.AddComponent<SimplePressUI>();
            Undo.RegisterCreatedObjectUndo(uiGO, "Create UI System");
        }
        
        // 查找或创建 VR 手部设置
        VRHandSetup handSetup = Object.FindObjectOfType<VRHandSetup>();
        if (handSetup == null)
        {
            GameObject handGO = new GameObject("VRHandSetup");
            handSetup = handGO.AddComponent<VRHandSetup>();
            Undo.RegisterCreatedObjectUndo(handGO, "Create VR Hand Setup");
        }
        
        // 执行手部设置
        handSetup.SetupVRHands();
        
        // 自动修复问题
        QuickErrorFix errorFix = Object.FindObjectOfType<QuickErrorFix>();
        if (errorFix == null)
        {
            GameObject fixGO = new GameObject("QuickErrorFix");
            errorFix = fixGO.AddComponent<QuickErrorFix>();
            Undo.RegisterCreatedObjectUndo(fixGO, "Create Error Fix");
        }
        
        errorFix.FixAllIssues();
        
        Debug.Log("✅ 系统组件配置完成");
    }
}
#endif

