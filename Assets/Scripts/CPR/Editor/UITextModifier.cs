using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

namespace CPRSimulation.Editor
{
    /// <summary>
    /// 修改现有UI文字为游戏开始界面
    /// </summary>
    public class UITextModifier
    {
        [MenuItem("Tools/CPR/修改UI/设置为主菜单界面", false, 400)]
        public static void SetupAsMainMenu()
        {
            // 查找场景中的所有Text组件
            var allTexts = Object.FindObjectsOfType<Text>(true);
            var allTMPTexts = Object.FindObjectsOfType<TextMeshProUGUI>(true);

            Debug.Log($"[UIModifier] 找到 {allTexts.Length} 个Text组件, {allTMPTexts.Length} 个TMP组件");

            // 查找Canvas
            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[UIModifier] 场景中没有Canvas，请先创建Canvas");
                return;
            }

            // 清除现有UI内容，创建新的主菜单
            CreateMainMenuUI(canvas.transform);
            
            Debug.Log("[UIModifier] 主菜单UI已设置");
        }

        [MenuItem("Tools/CPR/修改UI/清空并创建简洁主菜单", false, 401)]
        public static void CreateCleanMainMenu()
        {
            // 查找或创建Canvas
            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                var canvasObj = new GameObject("GameCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // 删除Canvas下所有子对象
            for (int i = canvas.transform.childCount - 1; i >= 0; i--)
            {
                Undo.DestroyObjectImmediate(canvas.transform.GetChild(i).gameObject);
            }

            CreateMainMenuUI(canvas.transform);
            
            // 添加控制脚本
            var menuController = canvas.gameObject.GetComponent<Game.MainMenuUI>();
            if (menuController == null)
            {
                menuController = canvas.gameObject.AddComponent<Game.MainMenuUI>();
            }

            // 设置引用
            menuController.mainMenuPanel = canvas.transform.Find("MainMenuPanel")?.gameObject;
            menuController.tutorialPanel = canvas.transform.Find("TutorialPanel")?.gameObject;
            
            var startBtn = canvas.transform.Find("MainMenuPanel/StartButton");
            var tutorialBtn = canvas.transform.Find("MainMenuPanel/TutorialButton");
            var quitBtn = canvas.transform.Find("MainMenuPanel/QuitButton");
            var beginBtn = canvas.transform.Find("TutorialPanel/BeginButton");
            var backBtn = canvas.transform.Find("TutorialPanel/BackButton");

            if (startBtn) menuController.startButton = startBtn.GetComponent<Button>();
            if (tutorialBtn) menuController.tutorialButton = tutorialBtn.GetComponent<Button>();
            if (quitBtn) menuController.quitButton = quitBtn.GetComponent<Button>();
            if (beginBtn) menuController.beginTrainingButton = beginBtn.GetComponent<Button>();
            if (backBtn) menuController.backFromTutorialButton = backBtn.GetComponent<Button>();

            menuController.trainingSceneName = "CPRSimulation";

            Debug.Log("[UIModifier] 简洁主菜单已创建并配置完成");
            EditorUtility.DisplayDialog("完成", "主菜单UI已创建！\n点击Play测试效果。", "确定");
        }

        private static void CreateMainMenuUI(Transform parent)
        {
            // === 主菜单面板 ===
            var mainPanel = CreatePanel("MainMenuPanel", parent);
            
            // 背景
            var bgImage = mainPanel.GetComponent<Image>();
            bgImage.color = new Color(0.05f, 0.08f, 0.15f, 0.95f);

            // 标题
            CreateUIText("TitleText", mainPanel.transform, 
                "CPR 心肺复苏训练", 
                new Vector2(0, 180), new Vector2(800, 100), 58, Color.white, FontStyle.Bold);

            // 副标题
            CreateUIText("SubtitleText", mainPanel.transform, 
                "Quest 3 VR + bHaptics 触觉反馈", 
                new Vector2(0, 110), new Vector2(600, 40), 24, new Color(0.6f, 0.7f, 0.8f));

            // 开始训练按钮
            CreateUIButton("StartButton", mainPanel.transform, 
                "开 始 训 练", 
                new Vector2(0, 10), new Vector2(320, 65), new Color(0.2f, 0.65f, 0.35f));

            // 训练教程按钮
            CreateUIButton("TutorialButton", mainPanel.transform, 
                "训 练 教 程", 
                new Vector2(0, -70), new Vector2(320, 65), new Color(0.25f, 0.45f, 0.75f));

            // 退出按钮
            CreateUIButton("QuitButton", mainPanel.transform, 
                "退 出", 
                new Vector2(0, -150), new Vector2(320, 65), new Color(0.4f, 0.4f, 0.45f));

            // 版本信息
            CreateUIText("VersionText", mainPanel.transform, 
                "v1.0 - VR First Aid Training", 
                new Vector2(0, -250), new Vector2(400, 30), 16, new Color(0.4f, 0.4f, 0.5f));

            // === 教程面板 ===
            var tutorialPanel = CreatePanel("TutorialPanel", parent);
            tutorialPanel.SetActive(false);
            
            var tutorialBg = tutorialPanel.GetComponent<Image>();
            tutorialBg.color = new Color(0.05f, 0.05f, 0.1f, 0.97f);

            // 教程标题
            CreateUIText("TutorialTitle", tutorialPanel.transform, 
                "CPR 心肺复苏 训练教程", 
                new Vector2(0, 230), new Vector2(700, 70), 42, Color.white, FontStyle.Bold);

            // 教程内容
            string content = 
                "【心肺复苏标准步骤】\n\n" +
                "1. 确认环境安全\n\n" +
                "2. 检查患者意识\n" +
                "   · 轻拍双肩，大声呼唤\n\n" +
                "3. 呼叫急救 (拨打120)\n\n" +
                "4. 胸外按压 (30次)\n" +
                "   · 位置：胸骨下半部中央\n" +
                "   · 深度：5 - 6 厘米\n" +
                "   · 频率：100 - 120 次/分钟\n\n" +
                "5. 开放气道，人工呼吸 (2次)\n\n" +
                "【触觉反馈说明】\n" +
                "手套震动强度反映按压质量";

            var contentText = CreateUIText("TutorialContent", tutorialPanel.transform, 
                content, 
                new Vector2(0, -30), new Vector2(650, 380), 22, Color.white);
            contentText.GetComponent<Text>().alignment = TextAnchor.UpperLeft;

            // 开始按钮
            CreateUIButton("BeginButton", tutorialPanel.transform, 
                "开 始 训 练", 
                new Vector2(120, -250), new Vector2(250, 55), new Color(0.2f, 0.7f, 0.3f));

            // 返回按钮
            CreateUIButton("BackButton", tutorialPanel.transform, 
                "返 回", 
                new Vector2(-120, -250), new Vector2(250, 55), new Color(0.45f, 0.45f, 0.5f));
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
            image.color = new Color(0, 0, 0, 0.9f);

            Undo.RegisterCreatedObjectUndo(panel, "Create Panel");
            return panel;
        }

        private static GameObject CreateUIText(string name, Transform parent, string content,
            Vector2 position, Vector2 size, int fontSize, Color color, FontStyle style = FontStyle.Normal)
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
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;

            Undo.RegisterCreatedObjectUndo(textObj, "Create Text");
            return textObj;
        }

        private static GameObject CreateUIButton(string name, Transform parent, string label,
            Vector2 position, Vector2 size, Color bgColor)
        {
            var btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            var rect = btnObj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var image = btnObj.AddComponent<Image>();
            image.color = bgColor;

            var button = btnObj.AddComponent<Button>();
            button.targetGraphic = image;
            
            // 设置按钮颜色过渡
            var colors = button.colors;
            colors.highlightedColor = new Color(bgColor.r + 0.1f, bgColor.g + 0.1f, bgColor.b + 0.1f);
            colors.pressedColor = new Color(bgColor.r - 0.1f, bgColor.g - 0.1f, bgColor.b - 0.1f);
            button.colors = colors;

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
            textComp.fontSize = 28;
            textComp.fontStyle = FontStyle.Bold;
            textComp.alignment = TextAnchor.MiddleCenter;
            textComp.color = Color.white;

            Undo.RegisterCreatedObjectUndo(btnObj, "Create Button");
            return btnObj;
        }
    }
}
