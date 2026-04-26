using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 简单的 VR 系统菜单（世界空间）：
/// - 开关按钮：切换内容可见性（保留头部栏以便再打开）
/// - 退出按钮：调用 Application.Quit（编辑器里停止播放）
/// 自动生成世界空间 Canvas，面向主摄像机。
/// </summary>
public class VRPowerMenu : MonoBehaviour
{
    [Header("UI 构建")]
    [SerializeField] private bool autoCreateUI = true;
    [SerializeField] private Vector3 worldPosition = new Vector3(0f, 1.6f, 2f);
    [SerializeField] private Vector3 uiScale = Vector3.one * 0.0012f;
    [SerializeField] private Vector2 panelSize = new Vector2(800f, 360f);

    [Header("朝向")]
    [SerializeField] private bool faceCamera = true;

    [Header("引用（自动生成）")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject contentArea;
    [SerializeField] private Button toggleButton;
    [SerializeField] private Button exitButton;

    private bool contentVisible = true;

    void Start()
    {
        if (autoCreateUI || canvas == null)
        {
            BuildUI();
        }

        UpdateButtonLabels();
        Debug.Log("[VRPowerMenu] 初始化完成，世界菜单就绪");
    }

    void Update()
    {
        if (faceCamera && Camera.main != null && canvas != null)
        {
            var cam = Camera.main.transform;
            canvas.transform.position = worldPosition;
            canvas.transform.rotation = Quaternion.LookRotation(canvas.transform.position - cam.position);
            canvas.transform.localScale = uiScale;
        }
    }

    private void BuildUI()
    {
        Debug.Log("[VRPowerMenu] 自动构建 UI");

        GameObject canvasGO = new GameObject("VRPowerMenu_Canvas");
        canvasGO.transform.SetParent(transform, false);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;
        canvasGO.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = panelSize;
        canvasRect.position = worldPosition;
        canvasRect.localScale = uiScale;

        // 背景
        GameObject panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        var panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.8f);
        var panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;

        // 竖直布局
        var vLayout = panelGO.AddComponent<VerticalLayoutGroup>();
        vLayout.childAlignment = TextAnchor.UpperLeft;
        vLayout.padding = new RectOffset(20, 20, 20, 20);
        vLayout.spacing = 16;
        vLayout.childControlHeight = true;
        vLayout.childControlWidth = true;

        // 头部
        var header = CreateHeader(panelGO.transform, "系统菜单");

        // 内容容器
        contentArea = new GameObject("Content");
        contentArea.transform.SetParent(panelGO.transform, false);
        var contentLayout = contentArea.AddComponent<VerticalLayoutGroup>();
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.spacing = 12;
        contentLayout.childControlHeight = true;
        contentLayout.childControlWidth = true;

        // 开关按钮
        toggleButton = CreateButton(contentArea.transform, "关闭内容");
        toggleButton.onClick.AddListener(ToggleContentVisible);

        // 退出按钮
        exitButton = CreateButton(contentArea.transform, "退出程序");
        exitButton.onClick.AddListener(ExitApplication);
    }

    private GameObject CreateHeader(Transform parent, string title)
    {
        var go = new GameObject("Header");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 72);

        var textGO = new GameObject("Title");
        textGO.transform.SetParent(go.transform, false);
        var text = textGO.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 42;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;
        text.text = title;
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(16, 0);
        textRect.offsetMax = new Vector2(-16, 0);

        return go;
    }

    private Button CreateButton(Transform parent, string label)
    {
        var go = new GameObject(label);
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 0.85f);
        var btn = go.AddComponent<Button>();

        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 70);

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var txt = textGO.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 34;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.text = label;
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        return btn;
    }

    private void ToggleContentVisible()
    {
        contentVisible = !contentVisible;
        if (contentArea != null)
        {
            contentArea.SetActive(contentVisible);
        }
        UpdateButtonLabels();
        Debug.Log($"[VRPowerMenu] 开关切换 -> {(contentVisible ? "开启" : "关闭")}");
    }

    private void UpdateButtonLabels()
    {
        if (toggleButton != null)
        {
            var txt = toggleButton.GetComponentInChildren<Text>();
            if (txt != null)
            {
                txt.text = contentVisible ? "关闭内容" : "开启内容";
            }
        }
    }

    private void ExitApplication()
    {
        Debug.Log("[VRPowerMenu] 退出程序：Application.Quit()");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
