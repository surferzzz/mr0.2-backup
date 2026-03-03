using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace CPRSimulation.Editor
{
    /// <summary>
    /// CPR场景快速设置工具
    /// 基于Meta XR Interaction SDK的PokeExamples场景
    /// </summary>
    public class CPRSimpleSetup : EditorWindow
    {
        [MenuItem("Tools/CPR/快速设置场景", false, 100)]
        public static void ShowWindow()
        {
            GetWindow<CPRSimpleSetup>("CPR场景设置");
        }

        [MenuItem("Tools/CPR/打开CPR模拟场景", false, 50)]
        public static void OpenCPRScene()
        {
            string scenePath = "Assets/Scenes/CPRSimulation.unity";
            if (System.IO.File.Exists(Application.dataPath.Replace("Assets", "") + scenePath))
            {
                EditorSceneManager.OpenScene(scenePath);
            }
            else
            {
                Debug.LogWarning("[CPR Setup] CPRSimulation.unity 不存在，请先创建场景");
            }
        }

        [MenuItem("Tools/CPR/创建胸部检测点", false, 200)]
        public static void CreateChestDetector()
        {
            // 创建胸部检测器对象
            var chest = new GameObject("CPR_ChestDetector");
            chest.transform.position = new Vector3(0, 1f, 0.5f);
            
            // 添加CPR检测器组件
            var detector = chest.AddComponent<CPRPressDetector>();
            detector.useCollisionDetection = true;
            detector.detectionAreaSize = new Vector3(0.15f, 0.1f, 0.15f);
            
            // 添加可视化指示器
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "VisualIndicator";
            visual.transform.SetParent(chest.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = detector.detectionAreaSize;
            
            // 移除碰撞体（避免与检测器冲突）
            DestroyImmediate(visual.GetComponent<Collider>());
            
            // 设置半透明材质
            var renderer = visual.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.2f, 0.8f, 0.2f, 0.3f);
            SetupTransparentMaterial(mat);
            renderer.material = mat;

            Selection.activeGameObject = chest;
            Undo.RegisterCreatedObjectUndo(chest, "Create CPR Chest Detector");
            
            Debug.Log("[CPR Setup] 创建胸部检测点 - 请将其放置到虚拟病人胸部位置");
        }

        [MenuItem("Tools/CPR/添加触觉反馈控制器", false, 201)]
        public static void AddHapticController()
        {
            var detector = FindObjectOfType<CPRPressDetector>();
            if (detector == null)
            {
                Debug.LogWarning("[CPR Setup] 请先创建胸部检测点");
                return;
            }

            // 检查是否已存在
            var existing = FindObjectOfType<GloveHapticController>();
            if (existing != null)
            {
                Debug.Log("[CPR Setup] 触觉反馈控制器已存在");
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            var haptic = detector.gameObject.AddComponent<GloveHapticController>();
            haptic.pressDetector = detector;

            Selection.activeGameObject = detector.gameObject;
            Debug.Log("[CPR Setup] 已添加触觉反馈控制器");
        }

        [MenuItem("Tools/CPR/添加手部追踪适配器", false, 202)]
        public static void AddHandTrackingAdapter()
        {
            var detector = FindObjectOfType<CPRPressDetector>();
            if (detector == null)
            {
                Debug.LogWarning("[CPR Setup] 请先创建胸部检测点");
                return;
            }

            var existing = FindObjectOfType<HandTrackingAdapter>();
            if (existing != null)
            {
                Debug.Log("[CPR Setup] 手部追踪适配器已存在");
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            var adapter = detector.gameObject.AddComponent<HandTrackingAdapter>();
            adapter.pressDetector = detector;
            adapter.autoFindOVRHands = true;

            Selection.activeGameObject = detector.gameObject;
            Debug.Log("[CPR Setup] 已添加手部追踪适配器");
        }

        [MenuItem("Tools/CPR/添加bHaptics SDK2", false, 300)]
        public static void AddBhapticsSDK()
        {
            var existing = FindObjectOfType<Bhaptics.SDK2.BhapticsSDK2>();
            if (existing != null)
            {
                Debug.Log("[CPR Setup] 场景中已存在BhapticsSDK2");
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            var bhaptics = new GameObject("BhapticsSDK2");
            bhaptics.AddComponent<Bhaptics.SDK2.BhapticsSDK2>();
            
            Selection.activeGameObject = bhaptics;
            Undo.RegisterCreatedObjectUndo(bhaptics, "Add BhapticsSDK2");
            
            Debug.Log("[CPR Setup] 添加BhapticsSDK2 - 请在bHaptics设置中配置AppId");
        }

        private static void SetupTransparentMaterial(Material mat)
        {
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }

        private void OnGUI()
        {
            GUILayout.Label("CPR触觉反馈模拟 - 快速设置", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            // 场景状态检查
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("当前状态", EditorStyles.boldLabel);
            
            var detector = FindObjectOfType<CPRPressDetector>();
            var haptic = FindObjectOfType<GloveHapticController>();
            var adapter = FindObjectOfType<HandTrackingAdapter>();
            var bhaptics = FindObjectOfType<Bhaptics.SDK2.BhapticsSDK2>();

            var patient = GameObject.Find("Patient_Simple");
            
            DrawStatusLine("患者模型", patient != null);
            DrawStatusLine("胸部检测器", detector != null);
            DrawStatusLine("触觉反馈控制器", haptic != null);
            DrawStatusLine("手部追踪适配器", adapter != null);
            DrawStatusLine("bHaptics SDK", bhaptics != null);
            
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // 设置按钮
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("设置步骤", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "1. 先打开CPR模拟场景\n" +
                "2. 创建患者模型\n" +
                "3. 创建胸部检测点\n" +
                "4. 添加触觉反馈和手部追踪组件\n" +
                "5. 添加bHaptics SDK进行初始化",
                MessageType.Info);

            EditorGUILayout.Space(5);

            if (GUILayout.Button("打开 CPRSimulation 场景", GUILayout.Height(25)))
            {
                OpenCPRScene();
            }

            EditorGUILayout.Space(5);

            using (new EditorGUI.DisabledScope(patient != null))
            {
                if (GUILayout.Button("1. 创建患者模型", GUILayout.Height(30)))
                {
                    PatientModelCreator.CreateSimplePatient();
                }
            }

            using (new EditorGUI.DisabledScope(detector != null))
            {
                if (GUILayout.Button("2. 创建胸部检测点", GUILayout.Height(30)))
                {
                    CreateChestDetector();
                }
            }

            using (new EditorGUI.DisabledScope(detector == null || haptic != null))
            {
                if (GUILayout.Button("3. 添加触觉反馈控制器", GUILayout.Height(30)))
                {
                    AddHapticController();
                }
            }

            using (new EditorGUI.DisabledScope(detector == null || adapter != null))
            {
                if (GUILayout.Button("4. 添加手部追踪适配器", GUILayout.Height(30)))
                {
                    AddHandTrackingAdapter();
                }
            }

            using (new EditorGUI.DisabledScope(bhaptics != null))
            {
                if (GUILayout.Button("5. 添加bHaptics SDK2", GUILayout.Height(30)))
                {
                    AddBhapticsSDK();
                }
            }

            EditorGUILayout.Space(5);
            
            if (patient != null && detector != null)
            {
                if (GUILayout.Button("对齐检测器到患者胸部", GUILayout.Height(25)))
                {
                    PatientModelCreator.AlignDetectorToChest();
                }
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // 配置提醒
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("重要配置", EditorStyles.boldLabel);
            
            EditorGUILayout.HelpBox(
                "确保已添加预处理符号:\n" +
                "Project Settings > Player > Scripting Define Symbols\n" +
                "添加: BHAPTICS_SDK2",
                MessageType.Warning);

            if (GUILayout.Button("打开 Player Settings"))
            {
                SettingsService.OpenProjectSettings("Project/Player");
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawStatusLine(string label, bool isOk)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(isOk ? "✓" : "✗", GUILayout.Width(20));
            GUILayout.Label(label);
            GUILayout.FlexibleSpace();
            GUILayout.Label(isOk ? "已配置" : "未配置", 
                isOk ? EditorStyles.boldLabel : EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }
    }
}
