#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// 一键设置工具 - 自动配置碰撞检测
/// </summary>
public class OneClickSetup : MonoBehaviour
{
    [MenuItem("CPR训练系统/⚡ 一键配置选中对象为可检测目标", false, 10)]
    public static void SetupSelectedAsTarget()
    {
        GameObject selected = Selection.activeGameObject;
        
        if (selected == null)
        {
            EditorUtility.DisplayDialog("请选择对象", 
                "请先在 Hierarchy 中选择一个对象！\n\n" +
                "操作步骤：\n" +
                "1. 在 Hierarchy 中点击选中对象\n" +
                "2. 再次点击此菜单", 
                "知道了");
            return;
        }
        
        Debug.Log("╔══════════════════════════════════════════════════════════════╗");
        Debug.Log("║         ⚡ 一键配置碰撞检测目标                              ║");
        Debug.Log("╚══════════════════════════════════════════════════════════════╝");
        Debug.Log($"选中对象: {selected.name}");
        Debug.Log("");
        
        Undo.RecordObject(selected, "Setup Collision Target");
        
        // 1. 添加或配置 Collider
        Collider collider = selected.GetComponent<Collider>();
        if (collider == null)
        {
            BoxCollider box = selected.AddComponent<BoxCollider>();
            box.isTrigger = false;
            Debug.Log("✅ 添加 Box Collider");
        }
        else
        {
            collider.isTrigger = false;
            Debug.Log($"✅ 已有 {collider.GetType().Name}，确保不是触发器");
        }
        
        // 2. 添加或配置 Rigidbody
        Rigidbody rb = selected.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = selected.AddComponent<Rigidbody>();
            Debug.Log("✅ 添加 Rigidbody");
        }
        
        rb.isKinematic = true;
        rb.useGravity = false;
        Debug.Log("✅ 配置 Rigidbody（Kinematic + 无重力）");
        
        // 3. 添加检测脚本
        SuperSimpleCollisionTest testScript = selected.GetComponent<SuperSimpleCollisionTest>();
        if (testScript == null)
        {
            testScript = selected.AddComponent<SuperSimpleCollisionTest>();
            testScript.normalColor = Color.green;
            testScript.hitColor = Color.red;
            Debug.Log("✅ 添加 SuperSimpleCollisionTest 脚本");
        }
        else
        {
            Debug.Log("✅ 已有 SuperSimpleCollisionTest 脚本");
        }
        
        Debug.Log("");
        Debug.Log("╔══════════════════════════════════════════════════════════════╗");
        Debug.Log("║                    ✅ 配置完成！                              ║");
        Debug.Log("╚══════════════════════════════════════════════════════════════╝");
        Debug.Log("");
        Debug.Log("📋 现在可以测试碰撞了！");
        Debug.Log("");
        
        EditorUtility.DisplayDialog("配置完成！✅", 
            $"对象 '{selected.name}' 已配置完成！\n\n" +
            "✅ Collider（碰撞器）\n" +
            "✅ Rigidbody（刚体）\n" +
            "✅ SuperSimpleCollisionTest（检测脚本）\n\n" +
            "下一步：\n" +
            "• 配置手部对象\n" +
            "• 或创建测试球测试", 
            "好的");
    }
    
    [MenuItem("CPR训练系统/🤚 一键配置选中对象为VR手部", false, 11)]
    public static void SetupSelectedAsHand()
    {
        GameObject selected = Selection.activeGameObject;
        
        if (selected == null)
        {
            EditorUtility.DisplayDialog("请选择对象", 
                "请先在 Hierarchy 中选择手部对象！\n\n" +
                "通常是：\n" +
                "• LeftHand / RightHand\n" +
                "• LeftHandAnchor / RightHandAnchor\n" +
                "• Controller 等", 
                "知道了");
            return;
        }
        
        Debug.Log("╔══════════════════════════════════════════════════════════════╗");
        Debug.Log("║         🤚 一键配置 VR 手部碰撞                              ║");
        Debug.Log("╚══════════════════════════════════════════════════════════════╝");
        Debug.Log($"选中对象: {selected.name}");
        Debug.Log("");
        
        Undo.RecordObject(selected, "Setup VR Hand");
        
        // 1. 添加 Sphere Collider
        SphereCollider sphereCol = selected.GetComponent<SphereCollider>();
        if (sphereCol == null)
        {
            sphereCol = selected.AddComponent<SphereCollider>();
            Debug.Log("✅ 添加 Sphere Collider");
        }
        
        sphereCol.radius = 0.05f;
        sphereCol.isTrigger = false;
        Debug.Log($"✅ 配置 Sphere Collider（半径: {sphereCol.radius}）");
        
        // 2. 添加 Rigidbody
        Rigidbody rb = selected.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = selected.AddComponent<Rigidbody>();
            Debug.Log("✅ 添加 Rigidbody");
        }
        
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        Debug.Log("✅ 配置 Rigidbody（Kinematic + 连续碰撞检测）");
        
        Debug.Log("");
        Debug.Log("╔══════════════════════════════════════════════════════════════╗");
        Debug.Log("║                    ✅ 手部配置完成！                          ║");
        Debug.Log("╚══════════════════════════════════════════════════════════════╝");
        Debug.Log("");
        Debug.Log("💡 提示：如果有左右两只手，请对另一只手也执行此操作");
        Debug.Log("");
        
        EditorUtility.DisplayDialog("手部配置完成！✅", 
            $"手部对象 '{selected.name}' 已配置完成！\n\n" +
            "✅ Sphere Collider（半径: 0.05）\n" +
            "✅ Rigidbody（Kinematic 模式）\n" +
            "✅ 连续碰撞检测\n\n" +
            "提示：\n" +
            "如果有两只手，请对另一只手\n" +
            "也执行相同操作", 
            "好的");
    }
    
    [MenuItem("CPR训练系统/🎾 创建测试球", false, 12)]
    public static void CreateTestBall()
    {
        // 创建球体
        GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "TestBall";
        ball.transform.position = new Vector3(0, 2f, 0);
        ball.transform.localScale = Vector3.one * 0.2f;
        
        // 配置 Rigidbody
        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = ball.AddComponent<Rigidbody>();
        }
        rb.mass = 1f;
        rb.useGravity = true;
        
        // 设置材质
        Renderer renderer = ball.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(1f, 0.5f, 0f); // 橙色
            renderer.material = mat;
        }
        
        Undo.RegisterCreatedObjectUndo(ball, "Create Test Ball");
        Selection.activeGameObject = ball;
        
        Debug.Log("✅ 测试球创建完成！");
        Debug.Log($"   位置: {ball.transform.position}");
        Debug.Log("   提示：调整位置到目标对象上方");
        Debug.Log("   然后点击播放按钮 ▶️ 测试");
        Debug.Log("");
        
        EditorUtility.DisplayDialog("测试球创建完成！✅", 
            "橙色测试球已创建！\n\n" +
            "位置：(0, 2, 0)\n\n" +
            "下一步：\n" +
            "1. 调整球的位置到目标上方\n" +
            "2. 点击播放按钮 ▶️\n" +
            "3. 观察球掉落并触发碰撞检测", 
            "开始测试");
    }
    
    [MenuItem("CPR训练系统/🔍 自动查找并配置所有手部", false, 13)]
    public static void AutoSetupAllHands()
    {
        Debug.Log("╔══════════════════════════════════════════════════════════════╗");
        Debug.Log("║         🔍 自动查找并配置所有手部                            ║");
        Debug.Log("╚══════════════════════════════════════════════════════════════╝");
        Debug.Log("");
        
        int setupCount = 0;
        
        // 查找所有可能的手部对象
        string[] handNames = new string[]
        {
            "lefthand", "righthand",
            "left hand", "right hand",
            "lefthandanchor", "righthandanchor",
            "left_hand", "right_hand",
            "controller_left", "controller_right"
        };
        
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            string nameLower = obj.name.ToLower();
            
            foreach (string handName in handNames)
            {
                if (nameLower.Contains(handName))
                {
                    Debug.Log($"🔍 找到手部对象: {obj.name}");
                    
                    // 配置这个手部
                    Undo.RecordObject(obj, "Auto Setup Hand");
                    
                    // Sphere Collider
                    SphereCollider col = obj.GetComponent<SphereCollider>();
                    if (col == null)
                    {
                        col = obj.AddComponent<SphereCollider>();
                    }
                    col.radius = 0.05f;
                    col.isTrigger = false;
                    
                    // Rigidbody
                    Rigidbody rb = obj.GetComponent<Rigidbody>();
                    if (rb == null)
                    {
                        rb = obj.AddComponent<Rigidbody>();
                    }
                    rb.isKinematic = true;
                    rb.useGravity = false;
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                    
                    Debug.Log($"   ✅ 已配置: {obj.name}");
                    setupCount++;
                    
                    break; // 找到一个匹配的名称就跳出内循环
                }
            }
        }
        
        Debug.Log("");
        Debug.Log("╔══════════════════════════════════════════════════════════════╗");
        Debug.Log($"║         ✅ 完成！共配置 {setupCount} 个手部对象                    ║");
        Debug.Log("╚══════════════════════════════════════════════════════════════╝");
        Debug.Log("");
        
        if (setupCount > 0)
        {
            EditorUtility.DisplayDialog("自动配置完成！✅", 
                $"找到并配置了 {setupCount} 个手部对象！\n\n" +
                "✅ 已添加 Sphere Collider\n" +
                "✅ 已添加 Rigidbody\n" +
                "✅ 已设置碰撞检测模式\n\n" +
                "现在可以测试碰撞了！", 
                "太好了！");
        }
        else
        {
            EditorUtility.DisplayDialog("未找到手部对象", 
                "没有找到常见的手部对象名称。\n\n" +
                "请手动选择手部对象，然后使用：\n" +
                "CPR训练系统 → 🤚 一键配置选中对象为VR手部", 
                "知道了");
        }
    }
    
    [MenuItem("CPR训练系统/📺 创建VR UI显示", false, 14)]
    public static void CreateVRUI()
    {
        // 检查是否已存在
        VRSimpleUI existing = Object.FindObjectOfType<VRSimpleUI>();
        if (existing != null)
        {
            EditorUtility.DisplayDialog("VR UI 已存在", 
                "场景中已经有 VR UI 了！\n\n" +
                "如果UI不显示，请检查：\n" +
                "1. 场景中有Main Camera\n" +
                "2. 点击播放按钮测试", 
                "知道了");
            Selection.activeGameObject = existing.gameObject;
            return;
        }
        
        Debug.Log("╔══════════════════════════════════════════════════════════════╗");
        Debug.Log("║              📺 创建 VR UI                                    ║");
        Debug.Log("╚══════════════════════════════════════════════════════════════╝");
        Debug.Log("");
        
        // 创建UI对象
        GameObject uiGO = new GameObject("VRSimpleUI");
        VRSimpleUI vrUI = uiGO.AddComponent<VRSimpleUI>();
        
        Undo.RegisterCreatedObjectUndo(uiGO, "Create VR UI");
        Selection.activeGameObject = uiGO;
        
        Debug.Log("✅ VR UI 创建完成");
        Debug.Log("   UI 会自动显示在 Quest 3 视野中");
        Debug.Log("   距离：摄像机前方 2 米");
        Debug.Log("");
        Debug.Log("╔══════════════════════════════════════════════════════════════╗");
        Debug.Log("║                    ✅ 完成！                                  ║");
        Debug.Log("╚══════════════════════════════════════════════════════════════╝");
        Debug.Log("");
        
        EditorUtility.DisplayDialog("VR UI 创建完成！✅", 
            "VR UI 已创建！\n\n" +
            "✅ 自动跟随摄像机\n" +
            "✅ 显示在视野中央\n" +
            "✅ 实时显示碰撞信息\n\n" +
            "在 Quest 3 中：\n" +
            "UI 会显示在您眼前 2 米处\n" +
            "显示碰撞次数和对象名称\n\n" +
            "现在可以 Build And Run 测试！", 
            "太好了！");
    }
    
    [MenuItem("CPR训练系统/🚀 完整设置向导（推荐！）", false, 15)]
    public static void OpenSetupWizard()
    {
        // 查找或创建向导对象
        CompleteSetupWizard wizard = Object.FindObjectOfType<CompleteSetupWizard>();
        if (wizard == null)
        {
            GameObject wizardGO = new GameObject("CompleteSetupWizard");
            wizard = wizardGO.AddComponent<CompleteSetupWizard>();
            Undo.RegisterCreatedObjectUndo(wizardGO, "Create Setup Wizard");
        }
        
        // 选中向导对象
        Selection.activeGameObject = wizard.gameObject;
        
        // 执行完整设置
        wizard.CompleteSetup();
        
        EditorUtility.DisplayDialog("完整设置完成！✅", 
            "所有组件已自动配置！\n\n" +
            "✅ VR UI 已创建\n" +
            "✅ Remy 胸部检测区已配置\n" +
            "✅ 手部碰撞已检查\n\n" +
            "查看 Console 了解详细信息\n\n" +
            "下一步：\n" +
            "1. 保存场景 (Ctrl+S)\n" +
            "2. Build And Run 到 Quest 3\n" +
            "3. 测试！", 
            "太好了！");
    }
    
    [MenuItem("CPR训练系统/📋 显示完整设置指南", false, 20)]
    public static void ShowGuide()
    {
        string guide = 
            "═══════════════════════════════════════════════\n" +
            "🎯 CPR 训练系统 - 完整设置指南\n" +
            "═══════════════════════════════════════════════\n\n" +
            
            "【第1步】配置目标对象（如：桌子、方块）\n" +
            "──────────────────────────────────\n" +
            "1. 在 Hierarchy 中选中目标对象\n" +
            "2. 点击菜单：\n" +
            "   CPR训练系统 → ⚡ 一键配置选中对象为可检测目标\n\n" +
            
            "【第2步】配置手部对象\n" +
            "──────────────────────────────────\n" +
            "方法A - 自动配置（推荐）：\n" +
            "   CPR训练系统 → 🔍 自动查找并配置所有手部\n\n" +
            
            "方法B - 手动配置：\n" +
            "   1. 选中左手对象\n" +
            "   2. CPR训练系统 → 🤚 一键配置选中对象为VR手部\n" +
            "   3. 对右手重复\n\n" +
            
            "【第3步】测试\n" +
            "──────────────────────────────────\n" +
            "方法A - 使用测试球：\n" +
            "   1. CPR训练系统 → 🎾 创建测试球\n" +
            "   2. 调整球到目标上方\n" +
            "   3. 点击播放 ▶️\n\n" +
            
            "方法B - 使用VR手部：\n" +
            "   1. 点击播放 ▶️\n" +
            "   2. 用手触碰目标对象\n" +
            "   3. 观察 Console 日志\n\n" +
            
            "【期望结果】\n" +
            "──────────────────────────────────\n" +
            "Console 应该显示：\n" +
            "════════════════════════════════════\n" +
            "🎯🎯🎯 碰撞检测！第 1 次\n" +
            "   我是: desk\n" +
            "   对方是: TestBall\n" +
            "════════════════════════════════════\n\n" +
            
            "目标对象应该从绿色变为红色\n\n" +
            
            "【需要帮助？】\n" +
            "──────────────────────────────────\n" +
            "查看文档：\n" +
            "• ⭐修复手部碰撞问题.txt\n" +
            "• 从零开始制作指南.txt\n\n" +
            
            "═══════════════════════════════════════════════";
        
        Debug.Log(guide);
        
        EditorUtility.DisplayDialog("完整设置指南", 
            "详细指南已显示在 Console 中！\n\n" +
            "快速步骤：\n\n" +
            "1️⃣ 选中目标 → 一键配置为可检测目标\n" +
            "2️⃣ 自动查找并配置所有手部\n" +
            "3️⃣ 创建测试球 → 播放测试\n\n" +
            "完成！", 
            "知道了");
    }
}
#endif

