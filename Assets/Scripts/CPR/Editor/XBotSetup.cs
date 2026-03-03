using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.UI;
using CPRSimulation.Core;

namespace CPRSimulation.Editor
{
    /// <summary>
    /// X Bot 模型一键设置
    /// </summary>
    public class XBotSetup
    {
        private const string MODEL_PATH = "Assets/00/X Bot@Death From Right.fbx";
        
        [MenuItem("Tools/CPR/★ 设置 X Bot 患者模型 ★", false, 50)]
        public static void SetupXBotPatient()
        {
            // 检查模型是否存在
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH);
            if (modelAsset == null)
            {
                EditorUtility.DisplayDialog("错误", 
                    $"未找到模型文件:\n{MODEL_PATH}\n\n请确认文件位置正确。", "确定");
                return;
            }

            Debug.Log("========== 开始设置 X Bot 患者模型 ==========");

            // 0. 删除旧的模型
            CleanupOldModels();

            // 1. 配置模型导入设置
            ConfigureModelImport();

            // 2. 实例化模型到场景
            var patient = InstantiateModel(modelAsset);

            // 3. 创建并配置 Animator Controller
            SetupAnimator(patient);

            // 4. 添加 CPR 检测组件
            AddCPRComponents(patient);

            // 5. 创建状态 UI
            CreateStatusUI();

            // 6. 设置游戏控制器
            SetupGameController(patient);

            Debug.Log("========== X Bot 设置完成 ==========");
            
            EditorUtility.DisplayDialog("完成", 
                "X Bot 患者模型已设置！\n\n" +
                "流程：\n" +
                "1. 点击 Play 运行\n" +
                "2. 点击「开始训练」按钮\n" +
                "3. X Bot 会倒地\n" +
                "4. 按压胸部进行CPR\n\n" +
                "胸部红色区域是检测位置。", "确定");

            Selection.activeGameObject = patient;
        }

        private static void CleanupOldModels()
        {
            Debug.Log("[0/6] 清理旧模型...");
            
            // 删除旧的 Patient 模型
            string[] oldNames = { "Patient", "SimplePatient", "XBot_Patient", "CPR_StatusCanvas" };
            foreach (var name in oldNames)
            {
                var old = GameObject.Find(name);
                if (old != null)
                {
                    Undo.DestroyObjectImmediate(old);
                    Debug.Log($"  已删除: {name}");
                }
            }
            
            Debug.Log("  ✓ 清理完成");
        }

        private static void ConfigureModelImport()
        {
            Debug.Log("[1/6] 配置模型导入设置...");

            var importer = AssetImporter.GetAtPath(MODEL_PATH) as ModelImporter;
            if (importer == null) return;

            bool needsReimport = false;

            // 设置为 Humanoid
            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                needsReimport = true;
            }

            // 动画设置 - 烘焙根运动防止位移
            var clipAnimations = importer.defaultClipAnimations;
            if (clipAnimations.Length > 0)
            {
                foreach (var clip in clipAnimations)
                {
                    clip.lockRootRotation = true;
                    clip.lockRootHeightY = true;
                    clip.lockRootPositionXZ = true;
                    clip.keepOriginalOrientation = true;
                    clip.keepOriginalPositionY = true;
                    clip.keepOriginalPositionXZ = true;
                }
                importer.clipAnimations = clipAnimations;
                needsReimport = true;
            }

            if (needsReimport)
            {
                importer.SaveAndReimport();
                Debug.Log("  ✓ 模型已重新导入为 Humanoid");
            }
            else
            {
                Debug.Log("  ✓ 模型导入设置正确");
            }
        }

        private static GameObject InstantiateModel(GameObject modelAsset)
        {
            Debug.Log("[2/6] 实例化模型...");

            // 实例化模型
            var patient = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
            patient.name = "XBot_Patient";

            // 设置位置（站立位置，动画会让它倒下）
            patient.transform.position = new Vector3(0, 0, 2.5f);
            patient.transform.rotation = Quaternion.Euler(0, 180, 0); // 面向相机
            patient.transform.localScale = Vector3.one;

            // 保持可见状态
            patient.SetActive(true);

            Undo.RegisterCreatedObjectUndo(patient, "Create XBot Patient");
            
            Debug.Log("  ✓ 模型已添加到场景");
            return patient;
        }

        private static void SetupAnimator(GameObject patient)
        {
            Debug.Log("[3/6] 设置倒地控制器（代码动画）...");

            // 禁用 Animator（使用代码动画代替，避免FBX动画位移问题）
            var animator = patient.GetComponent<Animator>();
            if (animator != null)
            {
                animator.enabled = false;
            }

            // 添加代码动画控制器
            var fallController = patient.GetComponent<Core.PatientFallController>();
            if (fallController == null)
            {
                fallController = patient.AddComponent<Core.PatientFallController>();
            }
            fallController.fallDuration = 2f;
            fallController.finalRotationX = -90f;
            
            Debug.Log("  ✓ 倒地控制器已添加（使用代码动画）");
        }

        private static void AddCPRComponents(GameObject patient)
        {
            Debug.Log("[4/6] 添加 CPR 组件...");

            // 查找胸部骨骼
            Transform chestBone = FindBone(patient.transform, "Spine", "Chest", "Spine1", "Spine2");
            
            Vector3 chestOffset;
            if (chestBone != null)
            {
                chestOffset = new Vector3(0, 0.05f, 0.1f); // 相对于模型的胸部位置
                Debug.Log($"  找到胸部骨骼: {chestBone.name}");
            }
            else
            {
                chestOffset = new Vector3(0, 0.15f, 0);
            }

            // 创建检测器对象
            var detectorObj = new GameObject("CPR_ChestDetector");
            detectorObj.transform.SetParent(patient.transform);
            detectorObj.transform.localPosition = chestOffset;
            detectorObj.transform.localRotation = Quaternion.identity;

            // 添加检测器
            var detector = detectorObj.AddComponent<CPRPressureDetector>();
            detector.detectionSize = new Vector3(0.2f, 0.2f, 0.2f);
            detector.showDebugGizmos = true;
            detector.showDebugLog = true;

            // 添加震动反馈
            var haptic = detectorObj.AddComponent<GloveHapticFeedback>();
            haptic.pressureDetector = detector;
            haptic.testModeWithoutDevice = true;
            haptic.showDebugLog = true;

            // 添加胸部可视化标记
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "ChestMarker";
            marker.transform.SetParent(detectorObj.transform);
            marker.transform.localPosition = Vector3.zero;
            marker.transform.localScale = new Vector3(0.15f, 0.05f, 0.15f);
            
            var markerRenderer = marker.GetComponent<Renderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = new Color(1f, 0.2f, 0.2f, 0.6f);
            markerRenderer.material = mat;
            
            Object.DestroyImmediate(marker.GetComponent<Collider>());

            Debug.Log("  ✓ CPR 组件添加完成");
        }

        private static void CreateStatusUI()
        {
            Debug.Log("[5/6] 创建状态 UI...");

            var canvasObj = new GameObject("CPR_StatusCanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            var canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.position = new Vector3(-1.2f, 1.0f, 2.5f);
            canvasRect.sizeDelta = new Vector2(400, 300);
            canvasRect.localScale = new Vector3(0.002f, 0.002f, 0.002f);
            canvasRect.rotation = Quaternion.Euler(0, 30, 0);

            // 背景
            var panel = new GameObject("TrainingPanel");
            panel.transform.SetParent(canvasObj.transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.08f, 0.1f, 0.15f, 0.92f);

            // 标题
            CreateText("标题", "CPR 训练中", panel.transform, new Vector2(0, 110), 34, Color.white, FontStyle.Bold);
            
            // 数据显示
            var depthText = CreateText("深度", "深度: 0.0 cm", panel.transform, new Vector2(0, 50), 28, Color.white);
            var rateText = CreateText("频率", "频率: 0/分钟", panel.transform, new Vector2(0, 5), 28, Color.white);
            var countText = CreateText("计数", "按压次数: 0 / 30", panel.transform, new Vector2(0, -40), 28, Color.white);
            var statusText = CreateText("状态", "请按压胸部", panel.transform, new Vector2(0, -95), 32, new Color(0.9f, 0.8f, 0.3f), FontStyle.Bold);

            // 初始隐藏训练面板
            panel.SetActive(false);

            // 添加显示组件
            var statusDisplay = canvasObj.AddComponent<CPRStatusDisplay>();
            // 组件会在运行时自动查找检测器

            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Status UI");
            Debug.Log("  ✓ 状态 UI 创建完成");
        }

        private static void SetupGameController(GameObject patient)
        {
            Debug.Log("[6/6] 设置游戏控制器...");

            // 查找主菜单 Canvas
            var menuCanvas = GameObject.Find("MainMenuCanvas");
            if (menuCanvas == null)
            {
                Debug.LogWarning("  未找到 MainMenuCanvas，请先运行「全新开始」创建场景");
                return;
            }

            // 添加游戏控制器
            var controller = menuCanvas.GetComponent<Game.CPRGameController>();
            if (controller == null)
            {
                controller = menuCanvas.AddComponent<Game.CPRGameController>();
            }

            // 设置引用
            controller.patientModel = patient;
            controller.fallController = patient.GetComponent<Core.PatientFallController>();
            controller.delayBeforeCPR = 2.5f;
            controller.targetCompressions = 30;

            // 查找按钮
            var startBtn = GameObject.Find("StartButton");
            var quitBtn = GameObject.Find("QuitButton");
            
            if (startBtn != null)
                controller.startButton = startBtn.GetComponent<Button>();
            if (quitBtn != null)
                controller.quitButton = quitBtn.GetComponent<Button>();

            // 查找面板
            var menuPanel = menuCanvas.transform.Find("BackgroundPanel");
            if (menuPanel != null)
                controller.menuPanel = menuPanel.gameObject;

            var statusCanvas = GameObject.Find("CPR_StatusCanvas");
            if (statusCanvas != null)
            {
                var trainingPanel = statusCanvas.transform.Find("TrainingPanel");
                if (trainingPanel != null)
                    controller.trainingPanel = trainingPanel.gameObject;
            }

            // 查找检测器
            controller.pressureDetector = Object.FindObjectOfType<CPRPressureDetector>();

            Debug.Log("  ✓ 游戏控制器设置完成");
        }

        private static Transform FindBone(Transform root, params string[] boneNames)
        {
            foreach (var boneName in boneNames)
            {
                var bone = FindChildRecursive(root, boneName);
                if (bone != null) return bone;
            }
            return null;
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name.ToLower().Contains(name.ToLower()))
                    return child;
                
                var result = FindChildRecursive(child, name);
                if (result != null) return result;
            }
            return null;
        }

        private static GameObject CreateText(string objName, string content, Transform parent, 
            Vector2 position, int fontSize, Color color, FontStyle style = FontStyle.Normal)
        {
            var obj = new GameObject(objName);
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(360, 50);

            var text = obj.AddComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;

            return obj;
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                var parts = path.Split('/');
                string current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                    {
                        AssetDatabase.CreateFolder(current, parts[i]);
                    }
                    current = next;
                }
            }
        }
    }
}
