using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using CPRSimulation.Core;

namespace CPRSimulation.Editor
{
    /// <summary>
    /// 添加CPR训练组件到场景
    /// </summary>
    public class AddTrainingSetup
    {
        [MenuItem("Tools/CPR/添加训练组件/添加完整训练系统", false, 200)]
        public static void AddFullTrainingSystem()
        {
            Debug.Log("========== 添加CPR训练系统 ==========");
            
            // 1. 创建患者模型
            var patient = CreatePatientModel();
            
            // 2. 添加按压检测器
            var detector = AddPressureDetector(patient);
            
            // 3. 添加震动反馈
            AddHapticFeedback(detector);
            
            // 4. 创建状态显示UI
            CreateStatusUI(detector);
            
            Debug.Log("========== 训练系统添加完成 ==========");
            EditorUtility.DisplayDialog("完成", 
                "CPR训练系统已添加！\n\n" +
                "包含：\n" +
                "• 患者模型（躺在地上）\n" +
                "• 胸部按压检测区域\n" +
                "• bHaptics震动反馈\n" +
                "• 实时状态显示UI\n\n" +
                "点击Play测试，用手按压患者胸部区域。", "确定");
        }

        [MenuItem("Tools/CPR/添加训练组件/只添加患者模型", false, 201)]
        public static void AddPatientOnly()
        {
            CreatePatientModel();
        }

        [MenuItem("Tools/CPR/添加训练组件/只添加按压检测器", false, 202)]
        public static void AddDetectorOnly()
        {
            var patient = GameObject.Find("Patient");
            if (patient != null)
            {
                AddPressureDetector(patient);
            }
            else
            {
                var detector = new GameObject("CPR_PressureDetector");
                detector.transform.position = new Vector3(0, 0.15f, 0);
                detector.AddComponent<CPRPressureDetector>();
                Selection.activeGameObject = detector;
            }
        }

        private static GameObject CreatePatientModel()
        {
            Debug.Log("[1/4] 创建患者模型...");

            // 创建患者根对象
            var patient = new GameObject("Patient");
            patient.transform.position = new Vector3(0, 0, 2);

            // 身体 - 使用胶囊体
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(patient.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = Quaternion.Euler(0, 0, 90); // 躺下
            body.transform.localScale = new Vector3(0.35f, 0.5f, 0.35f);
            SetMaterialColor(body, new Color(0.8f, 0.7f, 0.6f)); // 肤色

            // 头部
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(patient.transform);
            head.transform.localPosition = new Vector3(-0.6f, 0.1f, 0);
            head.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            SetMaterialColor(head, new Color(0.8f, 0.7f, 0.6f));

            // 胸部标记区域（可视化按压位置）
            var chestMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            chestMarker.name = "ChestMarker";
            chestMarker.transform.SetParent(patient.transform);
            chestMarker.transform.localPosition = new Vector3(0, 0.18f, 0);
            chestMarker.transform.localScale = new Vector3(0.12f, 0.01f, 0.12f);
            SetMaterialColor(chestMarker, new Color(1f, 0.3f, 0.3f, 0.7f)); // 红色半透明
            
            // 移除碰撞器（避免干扰检测）
            Object.DestroyImmediate(chestMarker.GetComponent<Collider>());

            Undo.RegisterCreatedObjectUndo(patient, "Create Patient");
            Selection.activeGameObject = patient;
            
            Debug.Log("  ✓ 患者模型创建完成");
            return patient;
        }

        private static CPRPressureDetector AddPressureDetector(GameObject patient)
        {
            Debug.Log("[2/4] 添加按压检测器...");

            // 创建检测器
            var detectorObj = new GameObject("CPR_PressureDetector");
            detectorObj.transform.SetParent(patient.transform);
            detectorObj.transform.localPosition = new Vector3(0, 0.2f, 0); // 胸部位置

            var detector = detectorObj.AddComponent<CPRPressureDetector>();
            detector.detectionSize = new Vector3(0.15f, 0.15f, 0.15f);
            detector.showDebugGizmos = true;
            detector.showDebugLog = true;

            Undo.RegisterCreatedObjectUndo(detectorObj, "Create Pressure Detector");
            
            Debug.Log("  ✓ 按压检测器添加完成");
            return detector;
        }

        private static void AddHapticFeedback(CPRPressureDetector detector)
        {
            Debug.Log("[3/4] 添加震动反馈...");

            // 在检测器同一对象上添加震动反馈
            var haptic = detector.gameObject.AddComponent<GloveHapticFeedback>();
            haptic.pressureDetector = detector;
            haptic.showDebugLog = true;
            haptic.testModeWithoutDevice = true; // 默认测试模式
            
            Debug.Log("  ✓ 震动反馈添加完成（测试模式）");
        }

        private static void CreateStatusUI(CPRPressureDetector detector)
        {
            Debug.Log("[4/4] 创建状态显示UI...");

            // 创建Canvas（世界空间）
            var canvasObj = new GameObject("CPR_StatusCanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            // 设置位置（在患者旁边）
            var canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.position = new Vector3(-0.8f, 1.2f, 2);
            canvasRect.sizeDelta = new Vector2(400, 300);
            canvasRect.localScale = new Vector3(0.002f, 0.002f, 0.002f);
            canvasRect.rotation = Quaternion.Euler(0, 30, 0); // 面向玩家

            // 背景面板
            var panel = new GameObject("Panel");
            panel.transform.SetParent(canvasObj.transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);

            // 标题
            var title = CreateText("CPR 状态监测", panel.transform, 
                new Vector2(0, 110), new Vector2(350, 50), 32, Color.white, FontStyle.Bold);

            // 深度显示
            var depthText = CreateText("深度: 0.0 cm", panel.transform,
                new Vector2(0, 50), new Vector2(300, 40), 28, Color.white);

            // 频率显示
            var rateText = CreateText("频率: 0/分钟", panel.transform,
                new Vector2(0, 0), new Vector2(300, 40), 28, Color.white);

            // 计数显示
            var countText = CreateText("按压次数: 0", panel.transform,
                new Vector2(0, -50), new Vector2(300, 40), 28, Color.white);

            // 状态显示
            var statusText = CreateText("等待按压...", panel.transform,
                new Vector2(0, -100), new Vector2(300, 50), 36, new Color(0.5f, 0.5f, 0.6f), FontStyle.Bold);

            // 添加状态显示组件
            var statusDisplay = canvasObj.AddComponent<CPRStatusDisplay>();
            statusDisplay.detector = detector;
            statusDisplay.depthText = depthText.GetComponent<Text>();
            statusDisplay.rateText = rateText.GetComponent<Text>();
            statusDisplay.countText = countText.GetComponent<Text>();
            statusDisplay.statusText = statusText.GetComponent<Text>();

            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Status UI");
            
            Debug.Log("  ✓ 状态显示UI创建完成");
        }

        private static GameObject CreateText(string content, Transform parent, 
            Vector2 position, Vector2 size, int fontSize, Color color, FontStyle style = FontStyle.Normal)
        {
            var obj = new GameObject(content.Split(':')[0].Trim());
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var text = obj.AddComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;

            return obj;
        }

        private static void SetMaterialColor(GameObject obj, Color color)
        {
            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                material.color = color;
                renderer.material = material;
            }
        }
    }
}
