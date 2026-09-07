using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using System.IO;

namespace CPRSimulation.Editor
{
    /// <summary>
    /// 一键完成所有游戏设置 - 无需手动操作
    /// </summary>
    public class OneClickGameSetup
    {
        [MenuItem("Tools/CPR/★ 一键完成所有设置 ★", false, 0)]
        public static void DoEverything()
        {
            Debug.Log("========== 开始一键设置 ==========");

            // 1. 创建主菜单场景
            Debug.Log("[1/4] 创建主菜单场景...");
            CreateMainMenuScene();

            // 2. 创建训练场景
            Debug.Log("[2/4] 创建训练场景...");
            CreateTrainingScene();

            // 3. 添加到Build Settings
            Debug.Log("[3/4] 配置Build Settings...");
            SetupBuildSettings();

            // 4. 打开训练场景
            Debug.Log("[4/4] 打开训练场景...");
            EditorSceneManager.OpenScene("Assets/Scenes/CPRTraining.unity");

            Debug.Log("========== 一键设置完成 ==========");
            
            EditorUtility.DisplayDialog("设置完成！", 
                "已自动创建：\n\n" +
                "✓ 主菜单场景 (MainMenu.unity)\n" +
                "✓ 训练场景 (CPRTraining.unity)\n" +
                "✓ Build Settings已配置\n\n" +
                "点击Play按钮即可开始测试！\n" +
                "从MainMenu场景开始运行以测试完整流程。",
                "开始使用");
        }

        private static void CreateMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // 设置相机背景
            var camera = Camera.main;
            if (camera != null)
            {
                camera.backgroundColor = new Color(0.1f, 0.1f, 0.2f);
                camera.clearFlags = CameraClearFlags.SolidColor;
            }

            // 创建Canvas
            var canvasObj = new GameObject("MainMenuCanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            // EventSystem
            var eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // === 主菜单面板 ===
            var mainMenuPanel = CreatePanel("MainMenuPanel", canvasObj.transform, new Color(0, 0, 0, 0.5f));

            // 标题
            CreateText("Title", mainMenuPanel.transform, "CPR 心肺蘇生トレーニング", 72, 
                new Vector2(0, 200), new Vector2(800, 100), Color.white, FontStyle.Bold);

            // 副标题
            CreateText("Subtitle", mainMenuPanel.transform, "Quest 3 + bHaptics TactGlove", 28, 
                new Vector2(0, 130), new Vector2(600, 50), new Color(0.7f, 0.7f, 0.7f));

            // 按钮
            var startBtn = CreateButton("StartButton", mainMenuPanel.transform, "开始训练", 
                new Vector2(0, 0), new Vector2(300, 60), new Color(0.2f, 0.7f, 0.3f));
            
            var tutorialBtn = CreateButton("TutorialButton", mainMenuPanel.transform, "训练教程", 
                new Vector2(0, -80), new Vector2(300, 60), new Color(0.3f, 0.5f, 0.8f));
            
            var quitBtn = CreateButton("QuitButton", mainMenuPanel.transform, "退出", 
                new Vector2(0, -160), new Vector2(300, 60), new Color(0.5f, 0.5f, 0.5f));

            // === 教程面板 ===
            var tutorialPanel = CreatePanel("TutorialPanel", canvasObj.transform, new Color(0, 0, 0, 0.85f));
            tutorialPanel.SetActive(false);

            CreateText("TutorialTitle", tutorialPanel.transform, "CPR トレーニングガイド", 48, 
                new Vector2(0, 250), new Vector2(600, 80), Color.white, FontStyle.Bold);

            string tutorialContent = 
                "心肺蘇生 (CPR) 標準手順：\n\n" +
                "1. 現場の安全確認\n" +
                "2. 患者の意識確認 - 肩を軽く叩く、大声で呼ぶ\n" +
                "3. 救急通報 - 119番\n" +
                "4. 胸骨圧迫\n" +
                "   • 圧迫位置：胸骨下半部\n" +
                "   • 圧迫深さ：5-6 cm\n" +
                "   • 圧迫テンポ：100-120 回/分\n" +
                "5. 30回圧迫後に2回人工呼吸\n\n" +
                "トレーニング中、グローブが振動で圧迫品質をフィードバック";

            CreateText("TutorialContent", tutorialPanel.transform, tutorialContent, 26, 
                new Vector2(0, -20), new Vector2(700, 400), Color.white, FontStyle.Normal, TextAnchor.UpperLeft);

            var beginBtn = CreateButton("BeginButton", tutorialPanel.transform, "トレーニング開始", 
                new Vector2(100, -280), new Vector2(250, 55), new Color(0.2f, 0.7f, 0.3f));
            
            var backBtn = CreateButton("BackButton", tutorialPanel.transform, "戻る", 
                new Vector2(-100, -280), new Vector2(250, 55), new Color(0.5f, 0.5f, 0.5f));

            // 添加MainMenuUI脚本
            var menuUI = canvasObj.AddComponent<Game.MainMenuUI>();
            menuUI.mainMenuPanel = mainMenuPanel;
            menuUI.tutorialPanel = tutorialPanel;
            menuUI.startButton = startBtn.GetComponent<Button>();
            menuUI.tutorialButton = tutorialBtn.GetComponent<Button>();
            menuUI.quitButton = quitBtn.GetComponent<Button>();
            menuUI.beginTrainingButton = beginBtn.GetComponent<Button>();
            menuUI.backFromTutorialButton = backBtn.GetComponent<Button>();
            menuUI.trainingSceneName = "CPRTraining";

            // 确保目录存在
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");
            Debug.Log("  → 主菜单场景已保存: Assets/Scenes/MainMenu.unity");
        }

        private static void CreateTrainingScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // 设置相机
            var camera = Camera.main;
            if (camera != null)
            {
                camera.backgroundColor = new Color(0.2f, 0.2f, 0.25f);
                camera.transform.position = new Vector3(0, 1.5f, -2f);
            }

            // 创建地面
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(2, 1, 2);
            var groundMat = new Material(Shader.Find("Standard"));
            groundMat.color = new Color(0.3f, 0.3f, 0.35f);
            ground.GetComponent<MeshRenderer>().material = groundMat;

            // 创建简易患者模型
            var patient = CreateSimplePatient();

            // 创建灯光：场景里已有方向光就复用，否则每次运行都会多出一盏，
            // 角度还完全相同，叠成双倍太阳。
            var lightComp = System.Array.Find(
                UnityEngine.Object.FindObjectsOfType<Light>(true),
                l => l.type == LightType.Directional);
            if (lightComp == null)
            {
                var light = new GameObject("DirectionalLight");
                lightComp = light.AddComponent<Light>();
                lightComp.type = LightType.Directional;
            }
            lightComp.intensity = 1f;
            lightComp.transform.rotation = Quaternion.Euler(50, -30, 0);

            // 创建CPR检测器
            var detectorObj = new GameObject("CPR_ChestDetector");
            detectorObj.transform.position = new Vector3(0, 0.25f, 0);
            var detector = detectorObj.AddComponent<CPRPressDetector>();
            detector.useCollisionDetection = true;
            detector.chestSurface = patient.transform.Find("Chest");

            // 添加触觉反馈
            var haptic = detectorObj.AddComponent<GloveHapticController>();
            haptic.pressDetector = detector;

            // 添加手部追踪适配器
            var handAdapter = detectorObj.AddComponent<HandTrackingAdapter>();
            handAdapter.pressDetector = detector;

            // 创建Canvas
            var canvasObj = new GameObject("TrainingCanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            // EventSystem
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // === 介绍面板 ===
            var introPanel = CreatePanel("IntroPanel", canvasObj.transform, new Color(0, 0, 0, 0.7f));
            CreateText("IntroText", introPanel.transform, "CPR 心肺蘇生トレーニング\n\n準備中...", 42, 
                Vector2.zero, new Vector2(600, 200), Color.white);

            // === 开始面板 ===
            var startPanel = CreatePanel("StartTrainingPanel", canvasObj.transform, new Color(0, 0, 0, 0.5f));
            startPanel.SetActive(false);
            
            CreateText("StartHint", startPanel.transform, "準備完了", 36, 
                new Vector2(0, 100), new Vector2(400, 60), Color.white);
            
            var startScenarioBtn = CreateButton("StartScenarioButton", startPanel.transform, "CPR開始", 
                new Vector2(0, 0), new Vector2(280, 60), new Color(0.9f, 0.6f, 0.2f));
            
            var startCPRBtn = CreateButton("StartCPRButton", startPanel.transform, "AED開始", 
                new Vector2(0, 0), new Vector2(280, 60), new Color(0.2f, 0.8f, 0.3f));
            startCPRBtn.SetActive(true);

            var exitTrainingBtn = CreateButton("ExitTrainingButton", startPanel.transform, "終了", 
                new Vector2(0, -80), new Vector2(220, 55), new Color(0.5f, 0.5f, 0.5f));

            // === 训练HUD ===
            var hudPanel = CreatePanel("TrainingHUDPanel", canvasObj.transform, new Color(0, 0, 0, 0));
            hudPanel.SetActive(false);

            var phaseText = CreateText("PhaseText", hudPanel.transform, "CPR 実施中", 32, 
                new Vector2(0, 280), new Vector2(400, 50), Color.yellow);
            
            var instructionText = CreateText("InstructionText", hudPanel.transform, "", 26, 
                new Vector2(0, 200), new Vector2(700, 120), Color.white);

            // 深度指示器背景
            var depthBg = CreatePanel("DepthBarBg", hudPanel.transform, new Color(0.2f, 0.2f, 0.2f, 0.8f));
            var depthBgRect = depthBg.GetComponent<RectTransform>();
            depthBgRect.anchorMin = depthBgRect.anchorMax = new Vector2(0.5f, 0.5f);
            depthBgRect.anchoredPosition = new Vector2(-350, 0);
            depthBgRect.sizeDelta = new Vector2(30, 200);

            // === 完成面板 ===
            var completePanel = CreatePanel("CompletionPanel", canvasObj.transform, new Color(0, 0.1f, 0, 0.85f));
            completePanel.SetActive(false);

            CreateText("CompleteTitle", completePanel.transform, "トレーニング完了！", 48, 
                new Vector2(0, 150), new Vector2(500, 80), Color.green, FontStyle.Bold);
            
            var resultText = CreateText("ResultText", completePanel.transform, "", 28, 
                new Vector2(0, 20), new Vector2(500, 200), Color.white);

            var restartBtn = CreateButton("RestartButton", completePanel.transform, "やり直す", 
                new Vector2(-120, -150), new Vector2(220, 55), new Color(0.3f, 0.6f, 0.9f));
            
            var exitBtn = CreateButton("ExitButton", completePanel.transform, "メニューに戻る", 
                new Vector2(120, -150), new Vector2(220, 55), new Color(0.5f, 0.5f, 0.5f));

            // === 创建训练控制器 ===
            var controllerObj = new GameObject("CPRTrainingController");
            var controller = controllerObj.AddComponent<Game.CPRTrainingController>();

            controller.patientModel = patient;
            controller.introPanel = introPanel;
            controller.startTrainingPanel = startPanel;
            controller.trainingHUDPanel = hudPanel;
            controller.completionPanel = completePanel;
            controller.startScenarioButton = startScenarioBtn.GetComponent<Button>();
            controller.startCPRButton = startCPRBtn.GetComponent<Button>();
            controller.restartButton = restartBtn.GetComponent<Button>();
            controller.exitButton = exitBtn.GetComponent<Button>();
            controller.exitTrainingButton = exitTrainingBtn.GetComponent<Button>();
            controller.phaseText = phaseText.GetComponent<Text>();
            controller.instructionText = instructionText.GetComponent<Text>();
            controller.pressDetector = detector;
            controller.hapticController = haptic;

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/CPRTraining.unity");
            Debug.Log("  → 训练场景已保存: Assets/Scenes/CPRTraining.unity");
        }

        private static GameObject CreateSimplePatient()
        {
            var patient = new GameObject("Patient");
            patient.transform.position = new Vector3(0, 0.1f, 0);

            Color skinColor = new Color(0.96f, 0.8f, 0.7f);
            Color clothColor = new Color(0.2f, 0.4f, 0.6f);
            Color pantsColor = new Color(0.15f, 0.15f, 0.2f);

            // 身体
            var torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            torso.name = "Torso";
            torso.transform.SetParent(patient.transform);
            torso.transform.localPosition = new Vector3(0, 0.08f, 0);
            torso.transform.localRotation = Quaternion.Euler(90, 0, 0);
            torso.transform.localScale = new Vector3(0.35f, 0.45f, 0.22f);
            SetMaterialColor(torso, clothColor);
            Object.DestroyImmediate(torso.GetComponent<Collider>());

            // 胸部标记
            var chest = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            chest.name = "Chest";
            chest.transform.SetParent(patient.transform);
            chest.transform.localPosition = new Vector3(0, 0.15f, 0.1f);
            chest.transform.localScale = new Vector3(0.1f, 0.01f, 0.1f);
            SetMaterialColor(chest, new Color(1f, 0.3f, 0.3f, 0.7f));

            // 头
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(patient.transform);
            head.transform.localPosition = new Vector3(0, 0.1f, 0.55f);
            head.transform.localScale = new Vector3(0.2f, 0.22f, 0.2f);
            SetMaterialColor(head, skinColor);
            Object.DestroyImmediate(head.GetComponent<Collider>());

            // 左臂
            var leftArm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            leftArm.name = "LeftArm";
            leftArm.transform.SetParent(patient.transform);
            leftArm.transform.localPosition = new Vector3(-0.28f, 0.05f, 0.1f);
            leftArm.transform.localRotation = Quaternion.Euler(0, 0, 20);
            leftArm.transform.localScale = new Vector3(0.07f, 0.22f, 0.07f);
            SetMaterialColor(leftArm, skinColor);
            Object.DestroyImmediate(leftArm.GetComponent<Collider>());

            // 右臂
            var rightArm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            rightArm.name = "RightArm";
            rightArm.transform.SetParent(patient.transform);
            rightArm.transform.localPosition = new Vector3(0.28f, 0.05f, 0.1f);
            rightArm.transform.localRotation = Quaternion.Euler(0, 0, -20);
            rightArm.transform.localScale = new Vector3(0.07f, 0.22f, 0.07f);
            SetMaterialColor(rightArm, skinColor);
            Object.DestroyImmediate(rightArm.GetComponent<Collider>());

            // 左腿
            var leftLeg = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            leftLeg.name = "LeftLeg";
            leftLeg.transform.SetParent(patient.transform);
            leftLeg.transform.localPosition = new Vector3(-0.1f, 0.07f, -0.4f);
            leftLeg.transform.localRotation = Quaternion.Euler(90, 0, 0);
            leftLeg.transform.localScale = new Vector3(0.09f, 0.3f, 0.09f);
            SetMaterialColor(leftLeg, pantsColor);
            Object.DestroyImmediate(leftLeg.GetComponent<Collider>());

            // 右腿
            var rightLeg = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            rightLeg.name = "RightLeg";
            rightLeg.transform.SetParent(patient.transform);
            rightLeg.transform.localPosition = new Vector3(0.1f, 0.07f, -0.4f);
            rightLeg.transform.localRotation = Quaternion.Euler(90, 0, 0);
            rightLeg.transform.localScale = new Vector3(0.09f, 0.3f, 0.09f);
            SetMaterialColor(rightLeg, pantsColor);
            Object.DestroyImmediate(rightLeg.GetComponent<Collider>());

            return patient;
        }

        private static void SetMaterialColor(GameObject obj, Color color)
        {
            var renderer = obj.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Standard"));
                mat.color = color;
                renderer.material = mat;
            }
        }

        private static void SetupBuildSettings()
        {
            var scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/CPRTraining.unity", true)
            };
            EditorBuildSettings.scenes = scenes;
            Debug.Log("  → Build Settings已配置");
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = panel.AddComponent<Image>();
            image.color = color;

            return panel;
        }

        private static GameObject CreateText(string name, Transform parent, string content, int fontSize,
            Vector2 position, Vector2 size, Color color, FontStyle style = FontStyle.Normal, 
            TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            var rect = textObj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var text = textObj.AddComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = color;

            return textObj;
        }

        private static GameObject CreateButton(string name, Transform parent, string label,
            Vector2 position, Vector2 size, Color color)
        {
            var btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            var rect = btnObj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var image = btnObj.AddComponent<Image>();
            image.color = color;

            var button = btnObj.AddComponent<Button>();
            button.targetGraphic = image;

            // 按钮文字
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var textComp = textObj.AddComponent<Text>();
            textComp.text = label;
            textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComp.fontSize = 26;
            textComp.fontStyle = FontStyle.Bold;
            textComp.alignment = TextAnchor.MiddleCenter;
            textComp.color = Color.white;

            return btnObj;
        }
    }
}
