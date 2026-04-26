#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// CPR训练系统 - Unity编辑器菜单
/// 提供一键式操作，方便Unity新手使用
/// </summary>
public class CPRSystemMenu : EditorWindow
{
    private static Color headerColor = new Color(0.2f, 0.6f, 0.9f);
    private static Color successColor = new Color(0.2f, 0.8f, 0.2f);
    private static Color warningColor = new Color(0.9f, 0.7f, 0.2f);
    
    private Vector2 scrollPosition;
    
    // 设置选项
    private bool createPatient = true;
    private bool createManagers = true;
    private bool createUI = true;
    private bool setupVRHands = true;
    private Vector3 patientPosition = new Vector3(0, 0.5f, 2f);
    
    [MenuItem("CPR训练系统/📋 打开控制面板", false, 0)]
    public static void ShowWindow()
    {
        CPRSystemMenu window = GetWindow<CPRSystemMenu>("CPR控制面板");
        window.minSize = new Vector2(400, 600);
        window.Show();
    }
    
    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // 标题
        DrawHeader("🚑 CPR训练系统控制面板");
        
        GUILayout.Space(10);
        
        // 快速设置区域
        DrawQuickSetupSection();
        
        GUILayout.Space(10);
        
        // ⭐ 快速测试区域（新增）
        DrawQuickTestSection();
        
        GUILayout.Space(10);
        
        // 场景管理区域
        DrawScenarioSection();
        
        GUILayout.Space(10);
        
        // 测试工具区域
        DrawTestingSection();
        
        GUILayout.Space(10);
        
        // 问题修复区域
        DrawFixSection();
        
        GUILayout.Space(10);
        
        // 系统状态区域
        DrawStatusSection();
        
        GUILayout.Space(10);
        
        // 文档区域
        DrawDocumentationSection();
        
        EditorGUILayout.EndScrollView();
    }
    
    void DrawHeader(string title)
    {
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
        headerStyle.fontSize = 16;
        headerStyle.alignment = TextAnchor.MiddleCenter;
        
        GUI.backgroundColor = headerColor;
        GUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Space(5);
        GUILayout.Label(title, headerStyle);
        GUILayout.Space(5);
        GUILayout.EndVertical();
        GUI.backgroundColor = Color.white;
    }
    
    void DrawQuickSetupSection()
    {
        GUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("🚀 快速设置", EditorStyles.boldLabel);
        GUILayout.Space(5);
        
        createPatient = EditorGUILayout.Toggle("创建患者模型", createPatient);
        createManagers = EditorGUILayout.Toggle("创建系统管理器", createManagers);
        createUI = EditorGUILayout.Toggle("创建UI界面", createUI);
        setupVRHands = EditorGUILayout.Toggle("设置VR手部", setupVRHands);
        
        GUILayout.Space(5);
        patientPosition = EditorGUILayout.Vector3Field("患者位置", patientPosition);
        
        GUILayout.Space(10);
        
        GUI.backgroundColor = successColor;
        if (GUILayout.Button("✨ 一键创建完整系统", GUILayout.Height(40)))
        {
            QuickSetupCPRSystem();
        }
        GUI.backgroundColor = Color.white;
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("🧹 清理所有对象"))
        {
            CleanupAllObjects();
        }
        
        GUILayout.EndVertical();
    }
    
    void DrawQuickTestSection()
    {
        GUILayout.BeginVertical(EditorStyles.helpBox);
        
        // 标题
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
        titleStyle.fontSize = 14;
        titleStyle.normal.textColor = new Color(1f, 0.8f, 0f);
        GUILayout.Label("⚡ 立即测试（不需要Quest 3）", titleStyle);
        
        GUILayout.Space(5);
        
        // 说明文字
        EditorGUILayout.HelpBox(
            "点击下面的按钮，创建测试球体，然后点击播放按钮 ▶️ 即可看到效果！", 
            MessageType.Info
        );
        
        GUILayout.Space(5);
        
        // 大按钮 - 创建测试球
        GUI.backgroundColor = new Color(1f, 0.6f, 0f); // 橙色
        if (GUILayout.Button("🎾 创建测试球体 → 立即测试！", GUILayout.Height(35)))
        {
            CreateTestBall();
        }
        GUI.backgroundColor = Color.white;
        
        GUILayout.Space(5);
        
        // 步骤提示
        GUIStyle stepStyle = new GUIStyle(EditorStyles.label);
        stepStyle.fontSize = 10;
        stepStyle.normal.textColor = Color.gray;
        GUILayout.Label("步骤：1️⃣ 点击上面按钮  2️⃣ 点击播放按钮 ▶️  3️⃣ 观察效果", stepStyle);
        
        GUILayout.EndVertical();
    }
    
    void DrawScenarioSection()
    {
        GUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("🎬 场景切换", EditorStyles.boldLabel);
        GUILayout.Space(5);
        
        EditorGUILayout.HelpBox("切换不同的急救训练场景", MessageType.Info);
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("📚 标准训练"))
        {
            SwitchScenario(EmergencyScenario.Training);
        }
        if (GUILayout.Button("🚗 车祸现场"))
        {
            SwitchScenario(EmergencyScenario.CarAccident);
        }
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("⚡ 地震灾害"))
        {
            SwitchScenario(EmergencyScenario.Earthquake);
        }
        if (GUILayout.Button("🏠 家庭意外"))
        {
            SwitchScenario(EmergencyScenario.HomeAccident);
        }
        GUILayout.EndHorizontal();
        
        if (GUILayout.Button("🏢 公共场所"))
        {
            SwitchScenario(EmergencyScenario.PublicPlace);
        }
        
        GUILayout.EndVertical();
    }
    
    void DrawTestingSection()
    {
        GUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("🧪 测试工具", EditorStyles.boldLabel);
        GUILayout.Space(5);
        
        EditorGUILayout.HelpBox("在Unity编辑器中测试按压功能", MessageType.Info);
        
        if (GUILayout.Button("🎾 创建测试球体"))
        {
            CreateTestBall();
        }
        
        if (GUILayout.Button("🤚 单独设置VR手部"))
        {
            SetupVRHandsOnly();
        }
        
        if (GUILayout.Button("🎮 开始CPR训练"))
        {
            StartCPRTraining();
        }
        
        GUILayout.EndVertical();
    }
    
    void DrawFixSection()
    {
        GUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("🔧 问题修复", EditorStyles.boldLabel);
        GUILayout.Space(5);
        
        EditorGUILayout.HelpBox("自动修复Quest 3常见问题", MessageType.Warning);
        
        GUI.backgroundColor = warningColor;
        if (GUILayout.Button("⚡ 修复所有Quest 3问题", GUILayout.Height(35)))
        {
            FixAllQuest3Issues();
        }
        GUI.backgroundColor = Color.white;
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("📊 诊断当前问题"))
        {
            DiagnoseIssues();
        }
        
        if (GUILayout.Button("♻️ 快速重启系统"))
        {
            QuickRestartSystem();
        }
        
        if (GUILayout.Button("🧹 清理错误日志"))
        {
            Debug.ClearDeveloperConsole();
            Debug.Log("✅ 错误日志已清理");
        }
        
        GUILayout.EndVertical();
    }
    
    void DrawStatusSection()
    {
        GUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("📊 系统状态", EditorStyles.boldLabel);
        GUILayout.Space(5);
        
        if (GUILayout.Button("🔍 检查系统状态"))
        {
            ShowSystemStatus();
        }
        
        if (GUILayout.Button("📝 显示场景信息"))
        {
            ShowScenarioInfo();
        }
        
        // 实时状态显示
        GUILayout.Space(10);
        DrawRealTimeStatus();
        
        GUILayout.EndVertical();
    }
    
    void DrawRealTimeStatus()
    {
        EditorGUILayout.LabelField("实时状态:", EditorStyles.boldLabel);
        
        // 只在Layout事件中查找对象，避免Repaint时的错误
        if (Event.current.type == EventType.Layout)
        {
            GameObject patient = GameObject.Find("Patient");
            DrawStatusLine("患者模型", patient != null);
            
            PressDataManager dataManager = Object.FindObjectOfType<PressDataManager>();
            DrawStatusLine("数据管理器", dataManager != null);
            
            CPRTrainingManager cprManager = Object.FindObjectOfType<CPRTrainingManager>();
            DrawStatusLine("训练管理器", cprManager != null);
            
            ScenarioManager scenarioManager = Object.FindObjectOfType<ScenarioManager>();
            DrawStatusLine("场景管理器", scenarioManager != null);
            
            SimplePressUI ui = Object.FindObjectOfType<SimplePressUI>();
            DrawStatusLine("UI系统", ui != null);
            
            VRHandSetup handSetup = Object.FindObjectOfType<VRHandSetup>();
            DrawStatusLine("VR手部设置", handSetup != null);
        }
        else
        {
            // 在其他事件中显示提示
            EditorGUILayout.HelpBox("点击 '🔍 检查系统状态' 按钮查看详细状态", MessageType.Info);
        }
    }
    
    void DrawStatusLine(string label, bool exists)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label + ":", GUILayout.Width(120));
        
        GUIStyle statusStyle = new GUIStyle(EditorStyles.label);
        statusStyle.fontStyle = FontStyle.Bold;
        
        if (exists)
        {
            GUI.contentColor = Color.green;
            GUILayout.Label("✅ 存在", statusStyle);
        }
        else
        {
            GUI.contentColor = Color.red;
            GUILayout.Label("❌ 不存在", statusStyle);
        }
        GUI.contentColor = Color.white;
        
        GUILayout.EndHorizontal();
    }
    
    void DrawDocumentationSection()
    {
        GUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("📚 帮助文档", EditorStyles.boldLabel);
        GUILayout.Space(5);
        
        EditorGUILayout.HelpBox("点击按钮打开相应的使用文档", MessageType.Info);
        
        if (GUILayout.Button("📖 开始使用 (README_START_HERE)"))
        {
            OpenDocumentation("README_START_HERE.md");
        }
        
        if (GUILayout.Button("🎮 Quest 3问题解决"))
        {
            OpenDocumentation("README_解决Quest3问题.md");
        }
        
        if (GUILayout.Button("⚙️ 超简单使用指南"))
        {
            OpenDocumentation("README_超简单使用.md");
        }
        
        if (GUILayout.Button("🔧 Quest 3错误修复详解"))
        {
            OpenDocumentation("README_Quest3错误修复.md");
        }
        
        GUILayout.EndVertical();
    }
    
    // === 功能实现方法 ===
    
    void QuickSetupCPRSystem()
    {
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Debug.Log("🚀 开始一键设置CPR系统...");
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━");
        
        // 查找或创建EasySetup对象
        EasySetup easySetup = Object.FindObjectOfType<EasySetup>();
        GameObject setupGO = null;
        
        if (easySetup == null)
        {
            setupGO = new GameObject("CPRSetup");
            easySetup = setupGO.AddComponent<EasySetup>();
            Undo.RegisterCreatedObjectUndo(setupGO, "Create CPR Setup");
            Debug.Log("✅ 创建了 CPRSetup 对象");
        }
        else
        {
            setupGO = easySetup.gameObject;
            Debug.Log("✅ 使用现有 CPRSetup 对象");
        }
        
        // 设置选项
        SerializedObject so = new SerializedObject(easySetup);
        so.FindProperty("createPatient").boolValue = createPatient;
        so.FindProperty("createManagers").boolValue = createManagers;
        so.FindProperty("createUI").boolValue = createUI;
        so.FindProperty("setupVRHands").boolValue = setupVRHands;
        so.FindProperty("patientPosition").vector3Value = patientPosition;
        so.ApplyModifiedProperties();
        
        // 执行设置
        easySetup.SetupCPRSystem();
        
        // 自动添加QuickErrorFix组件
        QuickErrorFix errorFix = setupGO.GetComponent<QuickErrorFix>();
        if (errorFix == null)
        {
            errorFix = setupGO.AddComponent<QuickErrorFix>();
            Debug.Log("✅ 添加了 QuickErrorFix 组件");
        }
        
        // 自动修复问题
        errorFix.FixAllIssues();
        
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Debug.Log("✅ CPR系统设置完成！");
        Debug.Log("🎮 现在可以点击播放按钮测试了");
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━");
        
        // 选中创建的对象
        Selection.activeGameObject = setupGO;
        EditorGUIUtility.PingObject(setupGO);
    }
    
    void CleanupAllObjects()
    {
        if (EditorUtility.DisplayDialog("确认清理", 
            "这将删除所有CPR系统创建的对象。\n确定要继续吗？", 
            "确定", "取消"))
        {
            EasySetup easySetup = Object.FindObjectOfType<EasySetup>();
            if (easySetup != null)
            {
                easySetup.CleanupAll();
            }
            else
            {
                // 手动清理
                DeleteIfExists("Patient");
                DeleteIfExists("SystemManager");
                DeleteIfExists("SimplePressUI");
                DeleteIfExists("SimplePressUI_Canvas");
                DeleteIfExists("VRHandSetup");
                DeleteIfExists("TestHand");
                DeleteIfExists("ScenarioContainer");
                
                Debug.Log("✅ 手动清理完成");
            }
        }
    }
    
    void DeleteIfExists(string objectName)
    {
        GameObject obj = GameObject.Find(objectName);
        if (obj != null)
        {
            Undo.DestroyObjectImmediate(obj);
            Debug.Log($"🧹 已删除 {objectName}");
        }
    }
    
    void SwitchScenario(EmergencyScenario scenario)
    {
        ScenarioManager scenarioManager = Object.FindObjectOfType<ScenarioManager>();
        
        if (scenarioManager == null)
        {
            EditorUtility.DisplayDialog("错误", 
                "未找到场景管理器！\n请先运行 '一键创建完整系统'", 
                "确定");
            return;
        }
        
        scenarioManager.SetScenario(scenario);
        Debug.Log($"✅ 已切换到场景: {ScenarioManager.GetScenarioDisplayName(scenario)}");
    }
    
    void CreateTestBall()
    {
        EasySetup easySetup = Object.FindObjectOfType<EasySetup>();
        if (easySetup != null)
        {
            easySetup.CreateTestBall();
        }
        else
        {
            // 手动创建
            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "TestHand";
            ball.transform.position = new Vector3(0, 2f, 2f);
            ball.transform.localScale = Vector3.one * 0.2f;
            
            Rigidbody rb = ball.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.useGravity = true;
            
            Undo.RegisterCreatedObjectUndo(ball, "Create Test Ball");
            
            Debug.Log("✅ 测试球体创建完成");
            Debug.Log("📝 点击播放按钮，球体会掉落并触发按压检测");
            
            Selection.activeGameObject = ball;
        }
    }
    
    void SetupVRHandsOnly()
    {
        EasySetup easySetup = Object.FindObjectOfType<EasySetup>();
        if (easySetup != null)
        {
            easySetup.SetupVRHandsOnly();
        }
        else
        {
            // 手动设置
            GameObject vrHandSetupGO = GameObject.Find("VRHandSetup");
            if (vrHandSetupGO == null)
            {
                vrHandSetupGO = new GameObject("VRHandSetup");
                Undo.RegisterCreatedObjectUndo(vrHandSetupGO, "Create VR Hand Setup");
            }
            
            VRHandSetup handSetup = vrHandSetupGO.GetComponent<VRHandSetup>();
            if (handSetup == null)
            {
                handSetup = vrHandSetupGO.AddComponent<VRHandSetup>();
            }
            
            handSetup.SetupVRHands();
        }
    }
    
    void StartCPRTraining()
    {
        CPRTrainingManager cprManager = Object.FindObjectOfType<CPRTrainingManager>();
        
        if (cprManager == null)
        {
            EditorUtility.DisplayDialog("错误", 
                "未找到CPR训练管理器！\n请先运行 '一键创建完整系统'", 
                "确定");
            return;
        }
        
        if (!EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("提示", 
                "请先进入播放模式！\n点击编辑器顶部的播放按钮 ▶️", 
                "确定");
            return;
        }
        
        cprManager.StartCPRTraining();
    }
    
    void FixAllQuest3Issues()
    {
        QuickErrorFix errorFix = Object.FindObjectOfType<QuickErrorFix>();
        
        if (errorFix == null)
        {
            // 创建QuickErrorFix对象
            GameObject fixGO = GameObject.Find("CPRSetup");
            if (fixGO == null)
            {
                fixGO = new GameObject("CPRSetup");
                Undo.RegisterCreatedObjectUndo(fixGO, "Create CPR Setup");
            }
            
            errorFix = fixGO.AddComponent<QuickErrorFix>();
            Debug.Log("✅ 创建了 QuickErrorFix 组件");
        }
        
        errorFix.FixAllIssues();
        Debug.Log("✅ Quest 3问题修复完成");
    }
    
    void DiagnoseIssues()
    {
        QuickErrorFix errorFix = Object.FindObjectOfType<QuickErrorFix>();
        
        if (errorFix == null)
        {
            EditorUtility.DisplayDialog("提示", 
                "未找到QuickErrorFix组件！\n将自动创建...", 
                "确定");
            FixAllQuest3Issues();
            return;
        }
        
        errorFix.DiagnoseIssues();
    }
    
    void QuickRestartSystem()
    {
        if (EditorUtility.DisplayDialog("确认重启", 
            "这将清理并重新创建整个CPR系统。\n确定要继续吗？", 
            "确定", "取消"))
        {
            CleanupAllObjects();
            EditorApplication.delayCall += () => {
                QuickSetupCPRSystem();
            };
        }
    }
    
    void ShowSystemStatus()
    {
        EasySetup easySetup = Object.FindObjectOfType<EasySetup>();
        if (easySetup != null)
        {
            easySetup.ShowStatus();
        }
        else
        {
            // 手动显示状态
            Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Debug.Log("📊 CPR训练系统状态");
            Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━");
            
            GameObject patient = GameObject.Find("Patient");
            Debug.Log($"患者模型: {(patient != null ? "✅ 存在" : "❌ 不存在")}");
            
            PressDataManager dataManager = Object.FindObjectOfType<PressDataManager>();
            Debug.Log($"数据管理器: {(dataManager != null ? "✅ 存在" : "❌ 不存在")}");
            
            CPRTrainingManager cprManager = Object.FindObjectOfType<CPRTrainingManager>();
            Debug.Log($"训练管理器: {(cprManager != null ? "✅ 存在" : "❌ 不存在")}");
            
            ScenarioManager scenarioManager = Object.FindObjectOfType<ScenarioManager>();
            Debug.Log($"场景管理器: {(scenarioManager != null ? "✅ 存在" : "❌ 不存在")}");
            
            SimplePressUI ui = Object.FindObjectOfType<SimplePressUI>();
            Debug.Log($"UI系统: {(ui != null ? "✅ 存在" : "❌ 不存在")}");
            
            Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━");
        }
    }
    
    void ShowScenarioInfo()
    {
        ScenarioManager scenarioManager = Object.FindObjectOfType<ScenarioManager>();
        
        if (scenarioManager != null)
        {
            scenarioManager.ShowScenarioInfo();
        }
        else
        {
            Debug.LogWarning("⚠️ 未找到场景管理器");
        }
    }
    
    void OpenDocumentation(string filename)
    {
        string path = System.IO.Path.Combine(Application.dataPath, "model mixamo", "scripts", filename);
        
        if (System.IO.File.Exists(path))
        {
            System.Diagnostics.Process.Start(path);
            Debug.Log($"📖 打开文档: {filename}");
        }
        else
        {
            EditorUtility.DisplayDialog("文档未找到", 
                $"找不到文档: {filename}\n路径: {path}", 
                "确定");
        }
    }
}

// === 顶部菜单项 ===

public class CPRSystemMenuItems
{
    [MenuItem("CPR训练系统/🚀 一键创建完整系统", false, 1)]
    public static void QuickSetupSystem()
    {
        CPRSystemMenu window = EditorWindow.GetWindow<CPRSystemMenu>("CPR控制面板");
        window.Show();
        
        // 延迟执行以确保窗口已显示
        EditorApplication.delayCall += () => {
            EasySetup easySetup = Object.FindObjectOfType<EasySetup>();
            GameObject setupGO = null;
            
            if (easySetup == null)
            {
                setupGO = new GameObject("CPRSetup");
                easySetup = setupGO.AddComponent<EasySetup>();
                Undo.RegisterCreatedObjectUndo(setupGO, "Create CPR Setup");
            }
            else
            {
                setupGO = easySetup.gameObject;
            }
            
            easySetup.SetupCPRSystem();
            
            // 添加QuickErrorFix
            QuickErrorFix errorFix = setupGO.GetComponent<QuickErrorFix>();
            if (errorFix == null)
            {
                errorFix = setupGO.AddComponent<QuickErrorFix>();
            }
            errorFix.FixAllIssues();
            
            Selection.activeGameObject = setupGO;
        };
    }
    
    [MenuItem("CPR训练系统/🔧 修复Quest 3问题", false, 2)]
    public static void FixQuest3Issues()
    {
        QuickErrorFix errorFix = Object.FindObjectOfType<QuickErrorFix>();
        
        if (errorFix == null)
        {
            GameObject fixGO = GameObject.Find("CPRSetup");
            if (fixGO == null)
            {
                fixGO = new GameObject("CPRSetup");
                Undo.RegisterCreatedObjectUndo(fixGO, "Create CPR Setup");
            }
            errorFix = fixGO.AddComponent<QuickErrorFix>();
        }
        
        errorFix.FixAllIssues();
    }
    
    [MenuItem("CPR训练系统/📊 系统状态检查", false, 3)]
    public static void CheckSystemStatus()
    {
        EasySetup easySetup = Object.FindObjectOfType<EasySetup>();
        if (easySetup != null)
        {
            easySetup.ShowStatus();
        }
        else
        {
            Debug.LogWarning("⚠️ 请先创建CPR系统");
        }
    }
    
    [MenuItem("CPR训练系统/🎬 场景切换/📚 标准训练", false, 11)]
    public static void SwitchToTraining()
    {
        SwitchScenario(EmergencyScenario.Training);
    }
    
    [MenuItem("CPR训练系统/🎬 场景切换/🚗 车祸现场", false, 12)]
    public static void SwitchToCarAccident()
    {
        SwitchScenario(EmergencyScenario.CarAccident);
    }
    
    [MenuItem("CPR训练系统/🎬 场景切换/⚡ 地震灾害", false, 13)]
    public static void SwitchToEarthquake()
    {
        SwitchScenario(EmergencyScenario.Earthquake);
    }
    
    [MenuItem("CPR训练系统/🎬 场景切换/🏠 家庭意外", false, 14)]
    public static void SwitchToHomeAccident()
    {
        SwitchScenario(EmergencyScenario.HomeAccident);
    }
    
    [MenuItem("CPR训练系统/🎬 场景切换/🏢 公共场所", false, 15)]
    public static void SwitchToPublicPlace()
    {
        SwitchScenario(EmergencyScenario.PublicPlace);
    }
    
    [MenuItem("CPR训练系统/🧪 测试工具/🎾 创建测试球体", false, 21)]
    public static void CreateTestBall()
    {
        GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "TestHand";
        ball.transform.position = new Vector3(0, 2f, 2f);
        ball.transform.localScale = Vector3.one * 0.2f;
        
        Rigidbody rb = ball.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.useGravity = true;
        
        Undo.RegisterCreatedObjectUndo(ball, "Create Test Ball");
        Selection.activeGameObject = ball;
        
        Debug.Log("✅ 测试球体创建完成！点击播放按钮测试");
    }
    
    [MenuItem("CPR训练系统/🧪 测试工具/🤚 设置VR手部", false, 22)]
    public static void SetupVRHands()
    {
        EasySetup easySetup = Object.FindObjectOfType<EasySetup>();
        if (easySetup != null)
        {
            easySetup.SetupVRHandsOnly();
        }
        else
        {
            Debug.LogWarning("⚠️ 请先创建CPR系统");
        }
    }
    
    [MenuItem("CPR训练系统/🧹 清理所有对象", false, 31)]
    public static void CleanupAll()
    {
        if (EditorUtility.DisplayDialog("确认清理", 
            "这将删除所有CPR系统对象。确定吗？", 
            "确定", "取消"))
        {
            EasySetup easySetup = Object.FindObjectOfType<EasySetup>();
            if (easySetup != null)
            {
                easySetup.CleanupAll();
            }
        }
    }
    
    [MenuItem("CPR训练系统/📚 帮助文档/📖 开始使用", false, 41)]
    public static void OpenStartGuide()
    {
        OpenDoc("README_START_HERE.md");
    }
    
    [MenuItem("CPR训练系统/📚 帮助文档/🎮 Quest 3问题解决", false, 42)]
    public static void OpenQuest3Guide()
    {
        OpenDoc("README_解决Quest3问题.md");
    }
    
    [MenuItem("CPR训练系统/📚 帮助文档/⚙️ 超简单使用", false, 43)]
    public static void OpenEasyGuide()
    {
        OpenDoc("README_超简单使用.md");
    }
    
    // 辅助方法
    private static void SwitchScenario(EmergencyScenario scenario)
    {
        ScenarioManager manager = Object.FindObjectOfType<ScenarioManager>();
        if (manager != null)
        {
            manager.SetScenario(scenario);
        }
        else
        {
            EditorUtility.DisplayDialog("错误", "未找到场景管理器！请先创建系统。", "确定");
        }
    }
    
    private static void OpenDoc(string filename)
    {
        string path = System.IO.Path.Combine(Application.dataPath, "model mixamo", "scripts", filename);
        if (System.IO.File.Exists(path))
        {
            System.Diagnostics.Process.Start(path);
        }
        else
        {
            EditorUtility.DisplayDialog("文档未找到", $"找不到: {filename}", "确定");
        }
    }
}

#endif

