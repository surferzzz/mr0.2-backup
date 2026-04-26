using UnityEngine;

/// <summary>
/// 完整设置向导 - 一次性解决所有问题
/// 在 Inspector 中点击按钮执行
/// </summary>
public class CompleteSetupWizard : MonoBehaviour
{
    [Header("=== 完整设置向导 ===")]
    [Tooltip("点击下面的按钮按顺序执行")]
    public bool ready = true;
    
    [ContextMenu("1️⃣ 第一步：创建VR UI")]
    public void Step1_CreateVRUI()
    {
        Debug.Log("════════════════════════════════════════");
        Debug.Log("1️⃣ VR UIを作成中...");
        Debug.Log("════════════════════════════════════════");
        
        // 检查是否已存在
        VRSimpleUI existing = FindObjectOfType<VRSimpleUI>();
        if (existing != null)
        {
            Debug.Log("✅ VR UI 已存在！");
            return;
        }
        
        // 创建
        GameObject uiGO = new GameObject("VRSimpleUI");
        uiGO.AddComponent<VRSimpleUI>();
        
        Debug.Log("✅ VR UI 创建完成！");
        Debug.Log("   会在 Quest 3 中显示在眼前");
        Debug.Log("");
    }
    
    [ContextMenu("2️⃣ 第二步：查找Remy并配置胸部")]
    public void Step2_SetupRemyChest()
    {
        Debug.Log("════════════════════════════════════════");
        Debug.Log("2️⃣ 配置 Remy 胸部检测区...");
        Debug.Log("════════════════════════════════════════");
        
        // 查找 Remy
        GameObject remy = GameObject.Find("Remy@Laying Seizure");
        if (remy == null)
        {
            Debug.LogError("❌ 未找到 Remy@Laying Seizure！");
            Debug.LogError("   请确认场景中有 Remy 模型");
            return;
        }
        
        Debug.Log($"✅ 找到 Remy: {remy.name}");
        
        // 查找或创建 ChestArea
        Transform chestArea = remy.transform.Find("ChestArea");
        GameObject chestGO;
        
        if (chestArea != null)
        {
            Debug.Log("✅ ChestArea 已存在，更新配置");
            chestGO = chestArea.gameObject;
        }
        else
        {
            Debug.Log("✅ 创建新的 ChestArea");
            chestGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chestGO.name = "ChestArea";
            chestGO.transform.SetParent(remy.transform);
            
            // 设置位置（Remy 躺着的姿势）
            chestGO.transform.localPosition = new Vector3(0, 0.5f, 0);
            chestGO.transform.localScale = new Vector3(0.3f, 0.1f, 0.2f);
        }
        
        // 配置碰撞
        SetupCollision(chestGO);
        
        // 设置颜色
        Renderer rend = chestGO.GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = rend.material;
            mat.color = new Color(0.2f, 1f, 0.2f, 0.5f); // 半透明绿色
        }
        
        Debug.Log("✅ Remy 胸部配置完成！");
        Debug.Log("   在 Scene 视图中可以看到绿色方块");
        Debug.Log("");
    }
    
    void SetupCollision(GameObject obj)
    {
        // Collider
        BoxCollider col = obj.GetComponent<BoxCollider>();
        if (col == null)
        {
            col = obj.AddComponent<BoxCollider>();
        }
        col.isTrigger = false;
        
        // Rigidbody
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = obj.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;
        
        // 检测脚本
        SuperSimpleCollisionTest test = obj.GetComponent<SuperSimpleCollisionTest>();
        if (test == null)
        {
            test = obj.AddComponent<SuperSimpleCollisionTest>();
        }
        test.normalColor = Color.green;
        test.hitColor = Color.red;
    }
    
    [ContextMenu("3️⃣ 第三步：检查手部配置")]
    public void Step3_CheckHands()
    {
        Debug.Log("════════════════════════════════════════");
        Debug.Log("3️⃣ 检查手部配置...");
        Debug.Log("════════════════════════════════════════");
        
        // 查找所有可能的手部对象
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int handCount = 0;
        
        foreach (GameObject obj in allObjects)
        {
            string name = obj.name.ToLower();
            if (name.Contains("hand") && !name.Contains("anchor"))
            {
                Debug.Log($"🔍 找到手部: {obj.name}");
                
                // 检查是否有碰撞器
                Collider col = obj.GetComponent<Collider>();
                if (col == null)
                {
                    Debug.LogWarning($"   ⚠️ {obj.name} 没有 Collider，添加中...");
                    SphereCollider sphere = obj.AddComponent<SphereCollider>();
                    sphere.radius = 0.05f;
                    sphere.isTrigger = false;
                }
                else
                {
                    Debug.Log($"   ✅ 有 Collider: {col.GetType().Name}");
                }
                
                // 检查 Rigidbody
                Rigidbody rb = obj.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    Debug.LogWarning($"   ⚠️ {obj.name} 没有 Rigidbody，添加中...");
                    rb = obj.AddComponent<Rigidbody>();
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }
                else
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                    Debug.Log($"   ✅ 有 Rigidbody");
                }
                
                handCount++;
            }
        }
        
        Debug.Log($"✅ 共检查 {handCount} 个手部对象");
        Debug.Log("");
    }
    
    [ContextMenu("🎯 一键完成所有设置")]
    public void CompleteSetup()
    {
        Debug.Log("╔══════════════════════════════════════════════════════════╗");
        Debug.Log("║                                                          ║");
        Debug.Log("║           🚀 开始完整设置                                 ║");
        Debug.Log("║                                                          ║");
        Debug.Log("╚══════════════════════════════════════════════════════════╝");
        Debug.Log("");
        
        Step1_CreateVRUI();
        Step2_SetupRemyChest();
        Step3_CheckHands();
        
        Debug.Log("╔══════════════════════════════════════════════════════════╗");
        Debug.Log("║                                                          ║");
        Debug.Log("║           ✅ 所有设置完成！                               ║");
        Debug.Log("║                                                          ║");
        Debug.Log("╚══════════════════════════════════════════════════════════╝");
        Debug.Log("");
        Debug.Log("📋 下一步：");
        Debug.Log("   1. 保存场景 (Ctrl+S)");
        Debug.Log("   2. File → Build Settings → Build And Run");
        Debug.Log("   3. 在 Quest 3 中测试");
        Debug.Log("");
        Debug.Log("💡 期望结果：");
        Debug.Log("   • 眼前看到 UI");
        Debug.Log("   • Remy 胸部有绿色方块");
        Debug.Log("   • 手触碰方块 → UI 更新");
        Debug.Log("");
    }
    
    [ContextMenu("📊 显示当前状态")]
    public void ShowStatus()
    {
        Debug.Log("════════════════════════════════════════");
        Debug.Log("📊 当前系统状态");
        Debug.Log("════════════════════════════════════════");
        
        // 检查 VR UI
        VRSimpleUI vrUI = FindObjectOfType<VRSimpleUI>();
        Debug.Log($"VR UI: {(vrUI != null ? "✅ 已创建" : "❌ 未创建")}");
        
        // 检查 Remy
        GameObject remy = GameObject.Find("Remy@Laying Seizure");
        Debug.Log($"Remy 模型: {(remy != null ? "✅ 找到" : "❌ 未找到")}");
        
        if (remy != null)
        {
            Transform chest = remy.transform.Find("ChestArea");
            Debug.Log($"胸部检测区: {(chest != null ? "✅ 已配置" : "❌ 未配置")}");
        }
        
        // 检查手部
        int handCount = 0;
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.ToLower().Contains("hand") && !obj.name.ToLower().Contains("anchor"))
            {
                handCount++;
            }
        }
        Debug.Log($"手部对象: {handCount} 个");
        
        Debug.Log("════════════════════════════════════════");
    }
}

