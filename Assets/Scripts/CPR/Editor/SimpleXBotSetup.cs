using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using CPRSimulation.Core;

namespace CPRSimulation.Editor
{
    /// <summary>
    /// 简单 X Bot 设置 - 使用代码动画避免FBX动画问题
    /// </summary>
    public class SimpleXBotSetup
    {
        private const string MODEL_PATH = "Assets/00/X Bot@Death From Right.fbx";

        [MenuItem("Tools/CPR/★★ 简单设置 X Bot (推荐) ★★", false, 40)]
        public static void Setup()
        {
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH);
            if (modelAsset == null)
            {
                EditorUtility.DisplayDialog("错误", $"未找到模型:\n{MODEL_PATH}", "确定");
                return;
            }

            Debug.Log("===== 简单 X Bot 设置开始 =====");

            // 1. 清理旧对象
            Cleanup();

            // 2. 添加模型
            var patient = AddModel(modelAsset);

            // 3. 添加倒地控制器（代码动画）
            AddFallController(patient);

            // 4. 添加CPR检测
            AddCPRDetection(patient);

            // 5. 设置游戏控制器
            SetupGameController(patient);

            Debug.Log("===== 设置完成 =====");

            EditorUtility.DisplayDialog("完成",
                "X Bot 已设置完成！\n\n" +
                "使用代码动画控制倒地，避免FBX动画位移问题。\n\n" +
                "1. 点击 Play\n" +
                "2. 点击「开始训练」\n" +
                "3. X Bot 会倒地\n" +
                "4. 按压胸部区域", "确定");

            Selection.activeGameObject = patient;
        }

        private static void Cleanup()
        {
            string[] names = { "XBot_Patient", "Patient", "CPR_StatusCanvas" };
            foreach (var name in names)
            {
                var obj = GameObject.Find(name);
                if (obj != null)
                {
                    Undo.DestroyObjectImmediate(obj);
                }
            }
        }

        private static GameObject AddModel(GameObject modelAsset)
        {
            Debug.Log("[1/5] 添加模型...");

            var patient = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
            patient.name = "XBot_Patient";

            // 设置初始位置（站立）
            patient.transform.position = new Vector3(0, 0, 2.5f);
            patient.transform.rotation = Quaternion.Euler(0, 180, 0);
            patient.transform.localScale = Vector3.one;

            // 保留 Animator 但禁用根运动
            var animator = patient.GetComponent<Animator>();
            if (animator != null)
            {
                animator.applyRootMotion = false;
            }

            Undo.RegisterCreatedObjectUndo(patient, "Create XBot");
            Debug.Log("  ✓ 模型已添加");
            return patient;
        }

        private static void AddFallController(GameObject patient)
        {
            Debug.Log("[2/5] 添加倒地控制器...");

            var fallController = patient.AddComponent<PatientFallController>();
            fallController.fallDuration = 2f;
            fallController.finalRotationX = -90f;

            Debug.Log("  ✓ 倒地控制器已添加（代码动画）");
        }

        private static void AddCPRDetection(GameObject patient)
        {
            Debug.Log("[3/5] 添加CPR检测...");

            // 创建检测器
            var detectorObj = new GameObject("CPR_ChestDetector");
            detectorObj.transform.SetParent(patient.transform);
            detectorObj.transform.localPosition = new Vector3(0, 0.12f, 0.05f);

            var detector = detectorObj.AddComponent<CPRPressureDetector>();
            detector.detectionSize = new Vector3(0.2f, 0.2f, 0.2f);
            detector.showDebugGizmos = true;
            detector.showDebugLog = true;

            var haptic = detectorObj.AddComponent<GloveHapticFeedback>();
            haptic.pressureDetector = detector;
            haptic.testModeWithoutDevice = true;

            // 胸部标记
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "ChestMarker";
            marker.transform.SetParent(detectorObj.transform);
            marker.transform.localPosition = Vector3.zero;
            marker.transform.localScale = new Vector3(0.12f, 0.04f, 0.12f);

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = new Color(1f, 0.2f, 0.2f, 0.5f);
            marker.GetComponent<Renderer>().material = mat;
            Object.DestroyImmediate(marker.GetComponent<Collider>());

            Debug.Log("  ✓ CPR检测已添加");
        }

        private static void SetupGameController(GameObject patient)
        {
            Debug.Log("[4/5] 设置游戏控制器...");

            var menuCanvas = GameObject.Find("MainMenuCanvas");
            if (menuCanvas == null)
            {
                Debug.LogWarning("  未找到 MainMenuCanvas");
                return;
            }

            // 移除旧的控制器
            var oldController = menuCanvas.GetComponent<Game.SimpleMenuController>();
            if (oldController != null)
            {
                Object.DestroyImmediate(oldController);
            }

            // 添加游戏控制器
            var controller = menuCanvas.GetComponent<Game.CPRGameController>();
            if (controller == null)
            {
                controller = menuCanvas.AddComponent<Game.CPRGameController>();
            }

            controller.patientModel = patient;
            controller.fallController = patient.GetComponent<PatientFallController>();
            controller.pressureDetector = patient.GetComponentInChildren<CPRPressureDetector>();

            // 查找UI
            var startBtn = menuCanvas.transform.Find("BackgroundPanel/StartButton");
            var quitBtn = menuCanvas.transform.Find("BackgroundPanel/QuitButton");
            var menuPanel = menuCanvas.transform.Find("BackgroundPanel");

            if (startBtn != null) controller.startButton = startBtn.GetComponent<Button>();
            if (quitBtn != null) controller.quitButton = quitBtn.GetComponent<Button>();
            if (menuPanel != null) controller.menuPanel = menuPanel.gameObject;

            Debug.Log("  ✓ 游戏控制器已设置");
        }
    }
}
