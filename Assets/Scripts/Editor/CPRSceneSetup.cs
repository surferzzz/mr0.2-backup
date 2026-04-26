using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace VRFirstAid.Editor
{
    /// <summary>
    /// CPR场景自动设置工具
    /// </summary>
    public class CPRSceneSetup : EditorWindow
    {
        [MenuItem("VR First Aid/Setup CPR Training Scene")]
        public static void ShowWindow()
        {
            GetWindow<CPRSceneSetup>("CPR Scene Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("CPR训练场景自动设置工具", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("创建基础场景结构", GUILayout.Height(30)))
            {
                CreateBasicSceneStructure();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("添加XR Origin", GUILayout.Height(30)))
            {
                AddXROrigin();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("创建CPR假人", GUILayout.Height(30)))
            {
                CreateCPRDummy();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("创建UI Canvas", GUILayout.Height(30)))
            {
                CreateUICanvas();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("添加所有管理器", GUILayout.Height(30)))
            {
                AddManagers();
            }

            GUILayout.Space(20);
            EditorGUILayout.HelpBox("注意：某些设置需要手动配置引用关系", MessageType.Info);
        }

        private static void CreateBasicSceneStructure()
        {
            // 创建地面
            GameObject floor = GameObject.Find("Floor");
            if (floor == null)
            {
                floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "Floor";
                floor.transform.position = Vector3.zero;
                floor.transform.localScale = new Vector3(10, 1, 10);
                
                // 设置材质颜色
                Renderer renderer = floor.GetComponent<Renderer>();
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.5f, 0.5f, 0.5f);
                renderer.material = mat;
            }

            // 创建环境父对象
            GameObject environment = GameObject.Find("Environment");
            if (environment == null)
            {
                environment = new GameObject("Environment");
            }

            if (floor.transform.parent != environment.transform)
            {
                floor.transform.SetParent(environment.transform);
            }

            EditorUtility.SetDirty(floor);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("基础场景结构已创建");
        }

        private static void AddXROrigin()
        {
            // 检查是否已存在XR Origin
            GameObject xrOrigin = GameObject.Find("XR Origin (XR Rig)");
            if (xrOrigin != null)
            {
                EditorUtility.DisplayDialog("提示", "XR Origin已存在于场景中", "确定");
                return;
            }

            // 尝试从预制体加载
            string prefabPath = "Assets/Samples/XRI/Prefabs/XR Origin (XR Rig).prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (prefab != null)
            {
                GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                instance.name = "XR Origin (XR Rig)";
                instance.transform.position = Vector3.zero;
                EditorUtility.SetDirty(instance);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                Debug.Log("XR Origin已添加");
            }
            else
            {
                EditorUtility.DisplayDialog("错误", $"找不到XR Origin预制体：{prefabPath}\n请手动从Assets/Samples/XRI/Prefabs/拖拽到场景", "确定");
            }
        }

        private static void CreateCPRDummy()
        {
            GameObject dummy = GameObject.Find("CPR Dummy");
            if (dummy != null)
            {
                EditorUtility.DisplayDialog("提示", "CPR Dummy已存在于场景中", "确定");
                return;
            }

            // 创建假人根对象
            dummy = new GameObject("CPR Dummy");
            dummy.transform.position = new Vector3(0, 0.5f, 2f);

            // 添加CPRManager脚本
            var cprManager = dummy.AddComponent<VRFirstAid.CPR.CPRManager>();
            var cprFeedback = dummy.AddComponent<VRFirstAid.CPR.CPRFeedback>();

            // 创建胸部区域
            GameObject chest = new GameObject("Chest");
            chest.transform.SetParent(dummy.transform);
            chest.transform.localPosition = new Vector3(0, 0.2f, 0);
            
            BoxCollider chestCollider = chest.AddComponent<BoxCollider>();
            chestCollider.size = new Vector3(0.3f, 0.2f, 0.15f);
            
            var compressionDetector = chest.AddComponent<VRFirstAid.CPR.CompressionDetector>();

            // 创建按压区域标记点
            GameObject compressionArea = new GameObject("CompressionArea");
            compressionArea.transform.SetParent(chest.transform);
            compressionArea.transform.localPosition = Vector3.zero;

            // 创建头部区域
            GameObject head = new GameObject("Head");
            head.transform.SetParent(dummy.transform);
            head.transform.localPosition = new Vector3(0, 0.6f, 0.1f);
            
            SphereCollider headCollider = head.AddComponent<SphereCollider>();
            headCollider.radius = 0.12f;
            
            var breathingDetector = head.AddComponent<VRFirstAid.CPR.BreathingDetector>();

            // 创建头部区域标记点
            GameObject headArea = new GameObject("HeadArea");
            headArea.transform.SetParent(head.transform);
            headArea.transform.localPosition = Vector3.zero;

            // 创建临时模型（使用基本几何体）
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(dummy.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.4f, 0.8f, 0.3f);

            GameObject headModel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            headModel.name = "HeadModel";
            headModel.transform.SetParent(head.transform);
            headModel.transform.localPosition = Vector3.zero;
            headModel.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);

            // 配置引用
            cprManager.compressionDetector = compressionDetector;
            cprManager.breathingDetector = breathingDetector;
            cprManager.feedback = cprFeedback;

            compressionDetector.compressionArea = compressionArea.transform;
            breathingDetector.headArea = headArea.transform;

            EditorUtility.SetDirty(dummy);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("CPR假人已创建，请手动配置Hand Controller引用");
        }

        private static void CreateUICanvas()
        {
            GameObject canvasObj = GameObject.Find("CPR Training UI");
            if (canvasObj != null)
            {
                EditorUtility.DisplayDialog("提示", "CPR Training UI已存在于场景中", "确定");
                return;
            }

            // 创建Canvas
            canvasObj = new GameObject("CPR Training UI");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObj.transform.position = new Vector3(0, 1.8f, 1.5f);
            canvasObj.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);

            CanvasScaler scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;

            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // 创建文本对象（需要TextMeshPro）
            CreateTextObject(canvasObj.transform, "InstructionText", "准备开始CPR训练");
            CreateTextObject(canvasObj.transform, "CompressionCountText", "按压: 0/30");
            CreateTextObject(canvasObj.transform, "BreathCountText", "呼吸: 0/2");
            CreateTextObject(canvasObj.transform, "CycleCountText", "循环: 0/5");

            // 添加CPRFeedback脚本引用
            GameObject dummy = GameObject.Find("CPR Dummy");
            if (dummy != null)
            {
                var feedback = dummy.GetComponent<VRFirstAid.CPR.CPRFeedback>();
                if (feedback != null)
                {
                    // 尝试设置文本引用（需要TextMeshPro组件）
                    var instructionText = canvasObj.transform.Find("InstructionText");
                    if (instructionText != null)
                    {
                        var tmp = instructionText.GetComponent<TMPro.TextMeshProUGUI>();
                        if (tmp != null)
                        {
                            feedback.instructionText = tmp;
                        }
                    }
                }
            }

            EditorUtility.SetDirty(canvasObj);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("UI Canvas已创建");
        }

        private static void CreateTextObject(Transform parent, string name, string text)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent);
            textObj.transform.localPosition = Vector3.zero;
            textObj.transform.localScale = Vector3.one;

            // 尝试添加TextMeshPro组件
            var tmp = textObj.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 36;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;

            RectTransform rectTransform = textObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(1000, 100);
        }

        private static void AddManagers()
        {
            // Game Manager
            GameObject gameManager = GameObject.Find("Game Manager");
            if (gameManager == null)
            {
                gameManager = new GameObject("Game Manager");
                gameManager.AddComponent<VRFirstAid.Core.GameManager>();
            }

            // Score System
            GameObject scoreSystem = GameObject.Find("Score System");
            if (scoreSystem == null)
            {
                scoreSystem = new GameObject("Score System");
                scoreSystem.AddComponent<VRFirstAid.Core.ScoreSystem>();
            }

            // Performance Optimizer
            GameObject perfOptimizer = GameObject.Find("Performance Optimizer");
            if (perfOptimizer == null)
            {
                perfOptimizer = new GameObject("Performance Optimizer");
                perfOptimizer.AddComponent<VRFirstAid.Core.PerformanceOptimizer>();
            }

            // VR Hand Controller
            GameObject vrHandController = GameObject.Find("VR Hand Controller");
            if (vrHandController == null)
            {
                vrHandController = new GameObject("VR Hand Controller");
                vrHandController.AddComponent<VRFirstAid.VR.VRHandController>();
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("所有管理器已添加，请手动配置引用关系");
        }
    }
}
