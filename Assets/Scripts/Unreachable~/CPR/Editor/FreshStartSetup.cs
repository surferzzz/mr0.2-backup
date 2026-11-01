using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace CPRSimulation.Editor
{
    /// <summary>
    /// 从零开始创建CPR训练系统
    /// </summary>
    public class FreshStartSetup
    {
        [MenuItem("Tools/CPR/★ 全新开始 - 创建VR UI场景 ★", false, -100)]
        public static void CreateFreshScene()
        {
            // 确认
            if (!EditorUtility.DisplayDialog("全新开始", 
                "这将创建一个全新的场景，包含：\n\n" +
                "• CPR训练系统 UI界面\n" +
                "• 开始/退出按钮\n" +
                "• VR手部交互支持\n\n" +
                "是否继续？", "创建", "取消"))
            {
                return;
            }

            // 创建新场景
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            Debug.Log("========== 开始创建 CPR 训练场景 ==========");

            // 1. 设置环境
            SetupEnvironment();

            // 2. 创建VR相机（如果有Meta SDK）
            SetupVRCamera();

            // 3. 创建UI
            CreateMainMenuUI();

            // 4. 添加手部交互支持
            SetupHandInteraction();

            // 5. 保存场景
            string scenePath = "Assets/Scenes/CPRTraining_Main.unity";
            EnsureDirectoryExists("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, scenePath);

            Debug.Log("========== 场景创建完成 ==========");
            Debug.Log($"场景已保存到: {scenePath}");
            
            EditorUtility.DisplayDialog("完成", 
                "CPR训练场景已创建！\n\n" +
                "场景包含：\n" +
                "• 主菜单UI（世界空间Canvas）\n" +
                "• 开始/退出按钮\n" +
                "• VR手部射线交互\n\n" +
                "点击 Play 测试效果。\n" +
                "在VR中可用手指点击按钮。", "确定");
        }

        private static void SetupEnvironment()
        {
            Debug.Log("[1/5] 设置环境...");

            // 删除默认相机（VR会用自己的）
            var mainCam = Camera.main;
            if (mainCam != null)
            {
                // 暂时保留，如果没有VR SDK则需要它
            }

            // 创建简单地面
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(5, 1, 5);
            
            // 设置地面材质颜色
            var groundRenderer = ground.GetComponent<Renderer>();
            groundRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            groundRenderer.material.color = new Color(0.2f, 0.25f, 0.3f);

            // 调整灯光
            var light = Object.FindObjectOfType<Light>();
            if (light != null)
            {
                light.transform.rotation = Quaternion.Euler(50, -30, 0);
                light.intensity = 1.2f;
            }

            Debug.Log("  ✓ 环境设置完成");
        }

        private static void SetupVRCamera()
        {
            Debug.Log("[2/5] 设置VR相机...");

            // 检查是否有Meta XR SDK
            bool hasMetaSDK = System.Type.GetType("OVRCameraRig, Oculus.VR") != null;

            if (hasMetaSDK)
            {
                // 查找OVRCameraRig预制体
                string[] guids = AssetDatabase.FindAssets("OVRCameraRig t:Prefab");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                    {
                        var cameraRig = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                        cameraRig.name = "OVRCameraRig";
                        cameraRig.transform.position = new Vector3(0, 0, -2);
                        
                        // 删除默认相机
                        var mainCam = GameObject.Find("Main Camera");
                        if (mainCam != null)
                        {
                            Object.DestroyImmediate(mainCam);
                        }
                        
                        Debug.Log("  ✓ OVRCameraRig 已添加");
                        return;
                    }
                }
            }

            // 如果没有Meta SDK，使用普通相机
            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0, 1.6f, -2);
                cam.transform.rotation = Quaternion.identity;
            }
            
            Debug.Log("  ✓ 使用标准相机（未检测到Meta XR SDK）");
        }

        private static void CreateMainMenuUI()
        {
            Debug.Log("[3/5] 创建UI界面...");

            // 创建Canvas（世界空间，用于VR）
            var canvasObj = new GameObject("MainMenuCanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            var canvasScaler = canvasObj.AddComponent<CanvasScaler>();
            canvasScaler.dynamicPixelsPerUnit = 10;
            
            canvasObj.AddComponent<GraphicRaycaster>();

            // 设置Canvas位置和大小（面向玩家）
            var canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.position = new Vector3(0, 1.5f, 2);
            canvasRect.sizeDelta = new Vector2(800, 600);
            canvasRect.localScale = new Vector3(0.002f, 0.002f, 0.002f);

            // 创建背景面板
            var panel = CreateUIPanel("BackgroundPanel", canvasObj.transform);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.15f, 0.95f);

            // 标题
            CreateUIText("TitleText", panel.transform, 
                "CPR トレーニングシステム", 
                new Vector2(0, 180), new Vector2(700, 100), 72, Color.white, FontStyle.Bold);

            // 副标题
            CreateUIText("SubtitleText", panel.transform, 
                "心肺蘇生バーチャルリアリティ訓練", 
                new Vector2(0, 100), new Vector2(500, 50), 32, new Color(0.7f, 0.8f, 0.9f));

            // 开始按钮
            var startBtn = CreateUIButton("StartButton", panel.transform, 
                "トレーニング開始", 
                new Vector2(0, -20), new Vector2(350, 80), 
                new Color(0.2f, 0.6f, 0.3f));

            // 退出按钮
            var quitBtn = CreateUIButton("QuitButton", panel.transform, 
                "終 了", 
                new Vector2(0, -120), new Vector2(350, 80), 
                new Color(0.5f, 0.2f, 0.2f));

            // 版本信息
            CreateUIText("VersionText", panel.transform, 
                "v1.0 | Quest 3 + ハンドトラッキング", 
                new Vector2(0, -220), new Vector2(400, 30), 18, new Color(0.5f, 0.5f, 0.6f));

            // 创建EventSystem
            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                var eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
            }

            // 添加菜单控制器
            var menuController = canvasObj.AddComponent<Game.SimpleMenuController>();
            menuController.startButton = startBtn.GetComponent<Button>();
            menuController.quitButton = quitBtn.GetComponent<Button>();

            Debug.Log("  ✓ UI界面创建完成");
        }

        private static void SetupHandInteraction()
        {
            Debug.Log("[4/5] 设置手部交互...");

            // 查找是否有Meta Interaction SDK
            bool hasInteractionSDK = System.Type.GetType("Oculus.Interaction.PointableCanvasModule, Oculus.Interaction") != null;

            if (hasInteractionSDK)
            {
                // 尝试添加交互组件
                var eventSystem = Object.FindObjectOfType<EventSystem>();
                if (eventSystem != null)
                {
                    // 尝试添加 PointableCanvasModule
                    var moduleType = System.Type.GetType("Oculus.Interaction.PointableCanvasModule, Oculus.Interaction");
                    if (moduleType != null && eventSystem.GetComponent(moduleType) == null)
                    {
                        eventSystem.gameObject.AddComponent(moduleType);
                        Debug.Log("  ✓ PointableCanvasModule 已添加");
                    }
                }

                // 查找并添加手部交互预制体
                AddHandInteractionPrefabs();
            }
            else
            {
                Debug.Log("  ℹ 未检测到 Meta Interaction SDK，使用鼠标/控制器交互");
                
                // 添加简单的射线交互说明
                CreateInstructionText();
            }

            Debug.Log("  ✓ 交互设置完成");
        }

        private static void AddHandInteractionPrefabs()
        {
            // 查找 HandInteractorsLeft/Right 预制体
            string[] handGuids = AssetDatabase.FindAssets("HandInteractors t:Prefab");
            
            foreach (var guid in handGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("Left") || path.Contains("Right"))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                    {
                        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                        Debug.Log($"  ✓ 添加了 {instance.name}");
                    }
                }
            }

            // 或者查找 OVRHand 相关
            string[] ovrHandGuids = AssetDatabase.FindAssets("OVRHandPrefab t:Prefab");
            foreach (var guid in ovrHandGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    Debug.Log($"  ℹ 找到手部预制体: {path}");
                }
            }
        }

        private static void CreateInstructionText()
        {
            // 在场景中添加一个简单的说明
            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                var panel = canvas.transform.Find("BackgroundPanel");
                if (panel != null)
                {
                    CreateUIText("InstructionText", panel, 
                        "在编辑器中用鼠标点击按钮测试\n在VR中使用手指或控制器射线交互", 
                        new Vector2(0, -280), new Vector2(600, 60), 16, new Color(0.6f, 0.6f, 0.7f));
                }
            }
        }

        // ============ UI辅助方法 ============

        private static GameObject CreateUIPanel(string name, Transform parent)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            
            var rect = panel.AddComponent<RectTransform>();
            var image = panel.AddComponent<Image>();
            image.color = new Color(0, 0, 0, 0.8f);
            
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
            
            // 按钮颜色过渡
            var colors = button.colors;
            colors.highlightedColor = new Color(bgColor.r + 0.15f, bgColor.g + 0.15f, bgColor.b + 0.15f);
            colors.pressedColor = new Color(bgColor.r * 0.8f, bgColor.g * 0.8f, bgColor.b * 0.8f);
            colors.selectedColor = bgColor;
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
            textComp.fontSize = 36;
            textComp.fontStyle = FontStyle.Bold;
            textComp.alignment = TextAnchor.MiddleCenter;
            textComp.color = Color.white;

            return btnObj;
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
