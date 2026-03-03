using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace CPRSimulation.Game
{
    /// <summary>
    /// 简单的主菜单控制器
    /// </summary>
    public class SimpleMenuController : MonoBehaviour
    {
        [Header("按钮引用")]
        public Button startButton;
        public Button quitButton;

        [Header("场景设置")]
        public string trainingSceneName = "CPRTraining";

        [Header("调试")]
        public bool showDebugLog = true;

        private void Start()
        {
            // 自动查找按钮
            if (startButton == null)
            {
                var startObj = transform.Find("BackgroundPanel/StartButton");
                if (startObj != null)
                    startButton = startObj.GetComponent<Button>();
            }

            if (quitButton == null)
            {
                var quitObj = transform.Find("BackgroundPanel/QuitButton");
                if (quitObj != null)
                    quitButton = quitObj.GetComponent<Button>();
            }

            // 绑定按钮事件
            if (startButton != null)
            {
                startButton.onClick.AddListener(OnStartClicked);
                if (showDebugLog) Debug.Log("[Menu] 开始按钮已绑定");
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(OnQuitClicked);
                if (showDebugLog) Debug.Log("[Menu] 退出按钮已绑定");
            }
        }

        public void OnStartClicked()
        {
            if (showDebugLog) Debug.Log("[Menu] 点击了开始训练！");
            
            // 检查训练场景是否存在
            if (Application.CanStreamedLevelBeLoaded(trainingSceneName))
            {
                SceneManager.LoadScene(trainingSceneName);
            }
            else
            {
                Debug.Log("[Menu] 训练场景尚未创建，显示提示...");
                ShowStartMessage();
            }
        }

        public void OnQuitClicked()
        {
            if (showDebugLog) Debug.Log("[Menu] 点击了退出！");
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        private void ShowStartMessage()
        {
            // 在UI上显示消息
            var messageObj = new GameObject("TempMessage");
            messageObj.transform.SetParent(transform, false);
            
            var rect = messageObj.AddComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(0, 0);
            rect.sizeDelta = new Vector2(600, 100);
            
            var image = messageObj.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.1f, 0.4f, 0.2f, 0.95f);
            
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(messageObj.transform, false);
            
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            var text = textObj.AddComponent<Text>();
            text.text = "✓ 系统已准备就绪！\n训练场景正在构建中...";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 28;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            
            // 3秒后销毁消息
            Destroy(messageObj, 3f);
        }
    }
}
