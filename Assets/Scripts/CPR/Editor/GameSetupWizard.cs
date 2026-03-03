using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace CPRSimulation.Editor
{
    /// <summary>
    /// 游戏设置向导 - 一键创建主菜单场景和训练场景
    /// </summary>
    public class GameSetupWizard : EditorWindow
    {
        [MenuItem("Tools/CPR/游戏设置向导", false, 300)]
        public static void ShowWindow()
        {
            GetWindow<GameSetupWizard>("CPR游戏设置");
        }

        [MenuItem("Tools/CPR/快速创建/创建主菜单场景", false, 310)]
        public static void CreateMainMenuScene()
        {
            // 创建新场景
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // 创建Canvas
            var canvas = CreateCanvas("MainMenuCanvas");

            // 创建主菜单面板
            var mainMenuPanel = CreatePanel("MainMenuPanel", canvas.transform);
            
            // 标题
            var title = CreateText("TitleText", mainMenuPanel.transform, "CPR 心肺复苏训练", 48);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0, 150);

            // 开始按钮
            var startBtn = CreateButton("StartButton", mainMenuPanel.transform, "开始训练", new Vector2(0, 30));
            
            // 教程按钮
            var tutorialBtn = CreateButton("TutorialButton", mainMenuPanel.transform, "教程说明", new Vector2(0, -40));
            
            // 退出按钮
            var quitBtn = CreateButton("QuitButton", mainMenuPanel.transform, "退出游戏", new Vector2(0, -110));

            // 创建教程面板（初始隐藏）
            var tutorialPanel = CreatePanel("TutorialPanel", canvas.transform);
            tutorialPanel.SetActive(false);

            var tutorialTitle = CreateText("TutorialTitle", tutorialPanel.transform, "CPR训练教程", 36);
            tutorialTitle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 180);

            var tutorialContent = CreateText("TutorialContent", tutorialPanel.transform, 
                "CPR心肺复苏训练步骤：\n\n" +
                "1. 确认患者意识\n" +
                "2. 呼叫急救 (120)\n" +
                "3. 胸外按压 30次\n" +
                "   - 深度：5-6厘米\n" +
                "   - 频率：100-120次/分钟\n" +
                "4. 人工呼吸 2次\n" +
                "5. 重复以上步骤", 24);
            var contentRect = tutorialContent.GetComponent<RectTransform>();
            contentRect.anchoredPosition = new Vector2(0, 0);
            contentRect.sizeDelta = new Vector2(500, 300);

            var beginBtn = CreateButton("BeginTrainingButton", tutorialPanel.transform, "开始训练", new Vector2(0, -180));
            var backBtn = CreateButton("BackButton", tutorialPanel.transform, "返回", new Vector2(0, -250));

            // 添加MainMenuUI脚本
            var menuUI = canvas.AddComponent<Game.MainMenuUI>();
            menuUI.mainMenuPanel = mainMenuPanel;
            menuUI.tutorialPanel = tutorialPanel;
            menuUI.startButton = startBtn.GetComponent<Button>();
            menuUI.tutorialButton = tutorialBtn.GetComponent<Button>();
            menuUI.quitButton = quitBtn.GetComponent<Button>();
            menuUI.beginTrainingButton = beginBtn.GetComponent<Button>();
            menuUI.backFromTutorialButton = backBtn.GetComponent<Button>();
            menuUI.trainingSceneName = "CPRSimulation";

            // 保存场景
            string scenePath = "Assets/Scenes/MainMenu.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            
            Debug.Log($"[GameSetup] 主菜单场景已创建: {scenePath}");
            EditorUtility.DisplayDialog("完成", "主菜单场景已创建！\n\n路径: " + scenePath, "确定");
        }

        [MenuItem("Tools/CPR/快速创建/为当前场景添加训练UI", false, 320)]
        public static void AddTrainingUIToCurrentScene()
        {
            // 创建Canvas
            var existingCanvas = Object.FindObjectOfType<Canvas>();
            GameObject canvasObj;
            
            if (existingCanvas != null)
            {
                canvasObj = existingCanvas.gameObject;
            }
            else
            {
                canvasObj = CreateCanvas("TrainingCanvas");
            }

            // 创建介绍面板
            var introPanel = CreatePanel("IntroPanel", canvasObj.transform);
            var introText = CreateText("IntroText", introPanel.transform, "CPR心肺复苏训练\n\n准备开始...", 32);

            // 创建开始面板
            var startPanel = CreatePanel("StartTrainingPanel", canvasObj.transform);
            startPanel.SetActive(false);
            var startScenarioBtn = CreateButton("StartScenarioButton", startPanel.transform, "开始情景", new Vector2(0, 0));
            var startCPRBtn = CreateButton("StartCPRButton", startPanel.transform, "开始CPR", new Vector2(0, 0));
            startCPRBtn.SetActive(false);

            // 创建训练HUD
            var hudPanel = CreatePanel("TrainingHUDPanel", canvasObj.transform);
            hudPanel.SetActive(false);
            
            var phaseText = CreateText("PhaseText", hudPanel.transform, "训练中", 28);
            phaseText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 200);
            
            var instructionText = CreateText("InstructionText", hudPanel.transform, "", 24);
            instructionText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 100);
            instructionText.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 150);

            // 创建完成面板
            var completePanel = CreatePanel("CompletionPanel", canvasObj.transform);
            completePanel.SetActive(false);
            var completeText = CreateText("CompleteText", completePanel.transform, "训练完成！", 36);
            completeText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 100);
            
            var restartBtn = CreateButton("RestartButton", completePanel.transform, "重新开始", new Vector2(-100, -100));
            var exitBtn = CreateButton("ExitButton", completePanel.transform, "退出", new Vector2(100, -100));

            // 创建训练控制器
            var controllerObj = new GameObject("CPRTrainingController");
            var controller = controllerObj.AddComponent<Game.CPRTrainingController>();
            
            controller.introPanel = introPanel;
            controller.startTrainingPanel = startPanel;
            controller.trainingHUDPanel = hudPanel;
            controller.completionPanel = completePanel;
            controller.startScenarioButton = startScenarioBtn.GetComponent<Button>();
            controller.startCPRButton = startCPRBtn.GetComponent<Button>();
            controller.restartButton = restartBtn.GetComponent<Button>();
            controller.exitButton = exitBtn.GetComponent<Button>();
            controller.phaseText = phaseText.GetComponent<Text>();
            controller.instructionText = instructionText.GetComponent<Text>();

            // 查找患者模型
            var patient = GameObject.Find("Death From Right");
            if (patient == null) patient = GameObject.Find("Remy@Laying Seizure");
            if (patient != null)
            {
                controller.patientModel = patient;
                controller.patientAnimator = patient.GetComponent<Animator>();
            }

            // 查找CPR检测器
            controller.pressDetector = Object.FindObjectOfType<CPRPressDetector>();
            controller.hapticController = Object.FindObjectOfType<GloveHapticController>();

            Debug.Log("[GameSetup] 训练UI已添加到当前场景");
            EditorUtility.DisplayDialog("完成", 
                "训练UI已添加！\n\n" +
                "请检查CPRTrainingController组件并设置：\n" +
                "- 患者模型引用\n" +
                "- CPR检测器引用", 
                "确定");
        }

        private static GameObject CreateCanvas(string name)
        {
            var canvasObj = new GameObject(name);
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            // 创建EventSystem
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            return canvasObj;
        }

        private static GameObject CreatePanel(string name, Transform parent)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = panel.AddComponent<Image>();
            image.color = new Color(0, 0, 0, 0.7f);

            return panel;
        }

        private static GameObject CreateButton(string name, Transform parent, string text, Vector2 position)
        {
            var btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            var rect = btnObj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(200, 50);

            var image = btnObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.6f, 0.9f);

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
            textComp.text = text;
            textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComp.fontSize = 24;
            textComp.alignment = TextAnchor.MiddleCenter;
            textComp.color = Color.white;

            return btnObj;
        }

        private static GameObject CreateText(string name, Transform parent, string content, int fontSize)
        {
            var textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            var rect = textObj.AddComponent<RectTransform>();
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(400, 100);

            var text = textObj.AddComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            return textObj;
        }

        private void OnGUI()
        {
            GUILayout.Label("CPR游戏设置向导", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "这个向导帮助你创建完整的CPR训练游戏：\n\n" +
                "1. 主菜单场景 - 开始界面\n" +
                "2. 训练场景 - CPR训练流程\n\n" +
                "按顺序点击下面的按钮来设置游戏。",
                MessageType.Info);

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("步骤1：创建主菜单", EditorStyles.boldLabel);
            if (GUILayout.Button("创建主菜单场景", GUILayout.Height(35)))
            {
                CreateMainMenuScene();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("步骤2：设置训练场景", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("请先打开你的训练场景(CPRSimulation)，然后点击下面的按钮", MessageType.Warning);
            if (GUILayout.Button("为当前场景添加训练UI", GUILayout.Height(35)))
            {
                AddTrainingUIToCurrentScene();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("步骤3：添加到Build Settings", EditorStyles.boldLabel);
            if (GUILayout.Button("打开Build Settings", GUILayout.Height(30)))
            {
                EditorWindow.GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
            }
            EditorGUILayout.HelpBox(
                "将以下场景添加到Build Settings:\n" +
                "0: Scenes/MainMenu\n" +
                "1: Scenes/CPRSimulation",
                MessageType.Info);
            EditorGUILayout.EndVertical();
        }
    }
}
