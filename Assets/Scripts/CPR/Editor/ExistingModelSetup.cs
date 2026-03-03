using UnityEngine;
using UnityEditor;

namespace CPRSimulation.Editor
{
    /// <summary>
    /// 配置现有人物模型用于CPR训练
    /// </summary>
    public class ExistingModelSetup : EditorWindow
    {
        private Transform selectedChestArea;
        private string[] searchNames = { "ChestArea", "Chest", "胸", "Torso", "Spine" };

        [MenuItem("Tools/CPR/配置现有人物模型", false, 210)]
        public static void ShowWindow()
        {
            GetWindow<ExistingModelSetup>("配置CPR模型");
        }

        [MenuItem("Tools/CPR/快速配置选中的胸部对象", false, 211)]
        public static void QuickSetupSelectedAsChest()
        {
            if (Selection.activeTransform == null)
            {
                Debug.LogWarning("[CPR Setup] 请先选择胸部区域对象");
                return;
            }

            SetupChestDetector(Selection.activeTransform);
        }

        [MenuItem("Tools/CPR/自动查找并配置胸部", false, 212)]
        public static void AutoFindAndSetupChest()
        {
            // 查找常见的胸部对象名称
            string[] searchNames = { "ChestArea", "ChestInteraction", "Chest", "胸部", "Torso" };
            
            foreach (var name in searchNames)
            {
                var chest = GameObject.Find(name);
                if (chest != null)
                {
                    SetupChestDetector(chest.transform);
                    return;
                }
            }

            // 尝试在Remy模型下查找
            var remy = GameObject.Find("Remy@Laying Seizure");
            if (remy != null)
            {
                var chestArea = remy.transform.Find("ChestArea");
                if (chestArea != null)
                {
                    SetupChestDetector(chestArea);
                    return;
                }

                // 查找骨骼中的Spine
                var spine = FindChildRecursive(remy.transform, "Spine");
                if (spine != null)
                {
                    SetupChestDetector(spine);
                    return;
                }
            }

            Debug.LogWarning("[CPR Setup] 未找到胸部对象，请手动选择并使用'快速配置选中的胸部对象'");
        }

        private static Transform FindChildRecursive(Transform parent, string nameContains)
        {
            foreach (Transform child in parent)
            {
                if (child.name.ToLower().Contains(nameContains.ToLower()))
                    return child;

                var found = FindChildRecursive(child, nameContains);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static void SetupChestDetector(Transform chestTransform)
        {
            // 查找或创建CPR检测器
            var detector = Object.FindObjectOfType<CPRPressDetector>();
            
            if (detector == null)
            {
                // 创建新的检测器对象
                var detectorObj = new GameObject("CPR_ChestDetector");
                detector = detectorObj.AddComponent<CPRPressDetector>();
                detector.useCollisionDetection = true;
                
                Undo.RegisterCreatedObjectUndo(detectorObj, "Create CPR Detector");
            }

            // 配置检测器位置
            detector.transform.position = chestTransform.position + Vector3.up * 0.05f;
            detector.chestSurface = chestTransform;
            detector.detectionAreaSize = new Vector3(0.2f, 0.15f, 0.2f);

            // 确保有触觉反馈控制器
            var haptic = detector.GetComponent<GloveHapticController>();
            if (haptic == null)
            {
                haptic = detector.gameObject.AddComponent<GloveHapticController>();
                haptic.pressDetector = detector;
            }

            // 确保有手部追踪适配器
            var adapter = detector.GetComponent<HandTrackingAdapter>();
            if (adapter == null)
            {
                adapter = detector.gameObject.AddComponent<HandTrackingAdapter>();
                adapter.pressDetector = detector;
                adapter.autoFindOVRHands = true;
            }

            // 重新初始化检测器
            detector.Initialize();

            Selection.activeGameObject = detector.gameObject;
            
            Debug.Log($"[CPR Setup] CPR检测器已配置到: {chestTransform.name}\n" +
                      $"位置: {detector.transform.position}\n" +
                      $"胸部参考: {chestTransform.name}");
        }

        private void OnGUI()
        {
            GUILayout.Label("配置现有人物模型用于CPR", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            // 检测场景中的模型
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("场景中找到的对象", EditorStyles.boldLabel);

            var remy = GameObject.Find("Remy@Laying Seizure");
            var chestArea = GameObject.Find("ChestArea");
            var chestInteraction = GameObject.Find("ChestInteraction");
            var detector = Object.FindObjectOfType<CPRPressDetector>();

            DrawStatusLine("Remy模型", remy != null);
            DrawStatusLine("ChestArea", chestArea != null);
            DrawStatusLine("ChestInteraction", chestInteraction != null);
            DrawStatusLine("CPR检测器", detector != null);

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // 快速设置按钮
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("快速设置", EditorStyles.boldLabel);

            if (GUILayout.Button("自动查找并配置胸部", GUILayout.Height(35)))
            {
                AutoFindAndSetupChest();
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "如果自动查找失败，请：\n" +
                "1. 在Hierarchy中选择ChestArea或ChestInteraction对象\n" +
                "2. 点击下方按钮",
                MessageType.Info);

            if (GUILayout.Button("配置选中对象为胸部检测点", GUILayout.Height(30)))
            {
                QuickSetupSelectedAsChest();
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // 手动选择
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("手动选择", EditorStyles.boldLabel);

            selectedChestArea = EditorGUILayout.ObjectField(
                "胸部对象", selectedChestArea, typeof(Transform), true) as Transform;

            using (new EditorGUI.DisabledScope(selectedChestArea == null))
            {
                if (GUILayout.Button("配置此对象为胸部检测点", GUILayout.Height(30)))
                {
                    SetupChestDetector(selectedChestArea);
                }
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // bHaptics
            var bhaptics = Object.FindObjectOfType<Bhaptics.SDK2.BhapticsSDK2>();
            if (bhaptics == null)
            {
                EditorGUILayout.BeginVertical("box");
                GUILayout.Label("触觉反馈", EditorStyles.boldLabel);
                
                if (GUILayout.Button("添加bHaptics SDK2", GUILayout.Height(30)))
                {
                    var obj = new GameObject("BhapticsSDK2");
                    obj.AddComponent<Bhaptics.SDK2.BhapticsSDK2>();
                    Undo.RegisterCreatedObjectUndo(obj, "Add BhapticsSDK2");
                }
                
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawStatusLine(string label, bool isOk)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(isOk ? "✓" : "✗", GUILayout.Width(20));
            GUILayout.Label(label);
            GUILayout.FlexibleSpace();
            if (isOk)
            {
                if (GUILayout.Button("选择", GUILayout.Width(50)))
                {
                    var obj = GameObject.Find(label);
                    if (obj != null) Selection.activeGameObject = obj;
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
