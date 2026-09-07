#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// 在 Remy 模型的上衣/身体网格上添加按压交互
/// 让手部可以直接与衣服/身体碰撞，触发按压检测
/// </summary>
public class SetupBodyMeshInteraction : MonoBehaviour
{
    [MenuItem("CPR训练系统/🎯 配置 Remy 身体网格交互", false, 4)]
    public static void SetupBodyMeshForInteraction()
    {
        // 查找 Remy 模型
        GameObject remyModel = FindRemyModel();
        
        if (remyModel == null)
        {
            EditorUtility.DisplayDialog("未找到 Remy 模型", 
                "请确认场景中有 Remy 模型，或选中 Remy 模型后再试。", 
                "知道了");
            return;
        }
        
        Debug.Log("╔══════════════════════════════════════════════════════════════╗");
        Debug.Log("║          🎯 配置 Remy 身体网格交互                           ║");
        Debug.Log("╚══════════════════════════════════════════════════════════════╝");
        Debug.Log($"找到模型: {remyModel.name}");
        Debug.Log("");
        
        // 查找所有身体网格
        List<Renderer> bodyMeshes = FindBodyMeshes(remyModel);
        
        if (bodyMeshes.Count == 0)
        {
            Debug.LogWarning("⚠️ 未找到身体网格！");
            EditorUtility.DisplayDialog("未找到网格", 
                "未在模型中找到身体网格（Renderer）。\n" +
                "请确认模型有可见的网格组件。", 
                "知道了");
            return;
        }
        
        Debug.Log($"✅ 找到 {bodyMeshes.Count} 个身体网格");
        
        // 为每个身体网格添加碰撞和按压检测
        int setupCount = 0;
        foreach (Renderer mesh in bodyMeshes)
        {
            if (IsChestArea(mesh.transform, remyModel.transform))
            {
                SetupMeshCollider(mesh.gameObject);
                setupCount++;
            }
        }
        
        Debug.Log($"✅ 已配置 {setupCount} 个胸部区域网格");
        Debug.Log("");
        
        // 确保系统组件存在
        EnsureSystemComponents();
        
        Debug.Log("");
        Debug.Log("╔══════════════════════════════════════════════════════════════╗");
        Debug.Log("║                    ✅ 配置完成！                              ║");
        Debug.Log("╚══════════════════════════════════════════════════════════════╝");
        Debug.Log("");
        Debug.Log("📋 现在您可以：");
        Debug.Log("  1. 直接触碰 Remy 的身体/上衣");
        Debug.Log("  2. 不需要额外的绿色方块");
        Debug.Log("  3. 更真实的交互体验！");
        Debug.Log("");
        
        EditorUtility.DisplayDialog("身体网格交互配置完成！✅", 
            $"已配置 {setupCount} 个身体网格！\n\n" +
            "✅ 可以直接触碰 Remy 的身体\n" +
            "✅ 不需要绿色方块\n" +
            "✅ 更真实的交互\n\n" +
            "下一步：\n" +
            "1. 测试按压功能\n" +
            "2. Build 到 Quest 3\n" +
            "3. 用手触碰 Remy 上衣", 
            "开始测试！");
    }
    
    static GameObject FindRemyModel()
    {
        // 优先使用选中的对象
        if (Selection.activeGameObject != null)
        {
            return Selection.activeGameObject;
        }
        
        // 查找所有包含 "Remy" 的对象
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.ToLower().Contains("remy"))
            {
                if (obj.transform.parent == null || 
                    obj.transform.parent.name.ToLower().Contains("scene"))
                {
                    return obj;
                }
            }
        }
        
        return null;
    }
    
    static List<Renderer> FindBodyMeshes(GameObject model)
    {
        List<Renderer> meshes = new List<Renderer>();
        
        // 获取所有 Renderer（包括 SkinnedMeshRenderer 和 MeshRenderer）
        Renderer[] allRenderers = model.GetComponentsInChildren<Renderer>();
        
        foreach (Renderer renderer in allRenderers)
        {
            // 排除一些不需要的对象
            string name = renderer.gameObject.name.ToLower();
            
            // 跳过这些对象
            if (name.Contains("eye") || name.Contains("hair") || 
                name.Contains("eyelash") || name.Contains("teeth") ||
                name.Contains("visual"))
            {
                continue;
            }
            
            // 添加身体网格
            meshes.Add(renderer);
            Debug.Log($"  📦 找到网格: {renderer.gameObject.name}");
        }
        
        return meshes;
    }
    
    static bool IsChestArea(Transform meshTransform, Transform modelRoot)
    {
        // 检查网格是否在胸部区域
        // 通过名称或位置判断
        
        string name = meshTransform.name.ToLower();
        
        // 通过名称判断（常见的身体/躯干名称）
        if (name.Contains("body") || name.Contains("torso") || 
            name.Contains("chest") || name.Contains("spine") ||
            name.Contains("upper") || name.Contains("shirt") ||
            name == "remy" || name.Contains("alpha"))
        {
            return true;
        }
        
        // 通过位置判断（相对于模型根部）
        Vector3 relativePos = meshTransform.position - modelRoot.position;
        float height = relativePos.y;
        
        // 假设胸部在模型高度的 40%-70% 范围
        Renderer rootRenderer = modelRoot.GetComponentInChildren<Renderer>();
        if (rootRenderer != null)
        {
            float modelHeight = rootRenderer.bounds.size.y;
            float normalizedHeight = height / modelHeight;
            
            if (normalizedHeight > 0.3f && normalizedHeight < 0.8f)
            {
                return true;
            }
        }
        
        return true; // 默认返回 true，让所有身体网格都可交互
    }
    
    static void SetupMeshCollider(GameObject meshObject)
    {
        Debug.Log($"⚙️ 配置网格: {meshObject.name}");
        
        // 添加 MeshCollider（更精确的碰撞检测）
        MeshCollider meshCollider = meshObject.GetComponent<MeshCollider>();
        if (meshCollider == null)
        {
            meshCollider = meshObject.AddComponent<MeshCollider>();
        }
        
        // 配置 MeshCollider
        meshCollider.convex = true; // 必须为 convex 才能与 Rigidbody 交互
        meshCollider.isTrigger = false; // 使用物理碰撞，不是触发器
        
        // 尝试获取网格
        SkinnedMeshRenderer skinnedMesh = meshObject.GetComponent<SkinnedMeshRenderer>();
        MeshFilter meshFilter = meshObject.GetComponent<MeshFilter>();
        
        if (skinnedMesh != null)
        {
            // 对于 SkinnedMeshRenderer，使用 sharedMesh
            meshCollider.sharedMesh = skinnedMesh.sharedMesh;
        }
        else if (meshFilter != null)
        {
            meshCollider.sharedMesh = meshFilter.sharedMesh;
        }
        
        // 添加或更新 Rigidbody
        Rigidbody rb = meshObject.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = meshObject.AddComponent<Rigidbody>();
        }
        
        rb.isKinematic = true; // 不受物理影响，但可以检测碰撞
        rb.useGravity = false;
        
        // 添加按压检测脚本
        SimpleMetaXRChest chestScript = meshObject.GetComponent<SimpleMetaXRChest>();
        if (chestScript == null)
        {
            chestScript = meshObject.AddComponent<SimpleMetaXRChest>();
        }
        
        // 设置标签
        if (meshObject.tag == "Untagged")
        {
            meshObject.tag = "Chest";
        }
        
        Debug.Log($"  ✅ {meshObject.name} 配置完成");
    }
    
    static void EnsureSystemComponents()
    {
        Debug.Log("⚙️ 确保系统组件存在...");
        
        // 查找或创建系统管理器
        GameObject managerGO = GameObject.Find("SystemManager");
        if (managerGO == null)
        {
            managerGO = new GameObject("SystemManager");
            Undo.RegisterCreatedObjectUndo(managerGO, "Create System Manager");
        }
        
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
        
        // UI
        SimplePressUI ui = Object.FindObjectOfType<SimplePressUI>();
        if (ui == null)
        {
            GameObject uiGO = new GameObject("SimplePressUI");
            ui = uiGO.AddComponent<SimplePressUI>();
            Undo.RegisterCreatedObjectUndo(uiGO, "Create UI");
        }
        
        // VR 手部
        VRHandSetup handSetup = Object.FindObjectOfType<VRHandSetup>();
        if (handSetup == null)
        {
            GameObject handGO = new GameObject("VRHandSetup");
            handSetup = handGO.AddComponent<VRHandSetup>();
            Undo.RegisterCreatedObjectUndo(handGO, "Create VR Hand Setup");
        }
        
        handSetup.SetupVRHands();
        
        // 错误修复
        QuickErrorFix errorFix = Object.FindObjectOfType<QuickErrorFix>();
        if (errorFix == null)
        {
            GameObject fixGO = new GameObject("QuickErrorFix");
            errorFix = fixGO.AddComponent<QuickErrorFix>();
            Undo.RegisterCreatedObjectUndo(fixGO, "Create Error Fix");
        }
        
        errorFix.FixAllIssues();
        
        Debug.Log("✅ 系统组件已就绪");
    }
    
    [MenuItem("CPR训练系统/🧹 清理身体网格碰撞器", false, 5)]
    public static void CleanupBodyMeshColliders()
    {
        GameObject remyModel = FindRemyModel();
        if (remyModel == null)
        {
            EditorUtility.DisplayDialog("未找到模型", "请选中 Remy 模型", "知道了");
            return;
        }
        
        int cleanedCount = 0;
        
        // 移除所有添加的碰撞组件
        MeshCollider[] colliders = remyModel.GetComponentsInChildren<MeshCollider>();
        foreach (MeshCollider collider in colliders)
        {
            if (collider.gameObject.GetComponent<SimpleMetaXRChest>() != null)
            {
                Object.DestroyImmediate(collider.gameObject.GetComponent<SimpleMetaXRChest>());
                Object.DestroyImmediate(collider);
                cleanedCount++;
            }
        }
        
        Debug.Log($"✅ 已清理 {cleanedCount} 个碰撞器");
        EditorUtility.DisplayDialog("清理完成", $"已清理 {cleanedCount} 个身体网格碰撞器", "确定");
    }
}
#endif

