using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PressUIManager : MonoBehaviour
{
    [Header("=== UI 组件引用 ===")]
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private GameObject uiPanel;
    
    [Header("=== 数据显示 ===")]
    [SerializeField] private TextMeshProUGUI totalPressText;
    [SerializeField] private TextMeshProUGUI validPressText;
    [SerializeField] private TextMeshProUGUI currentDepthText;
    [SerializeField] private TextMeshProUGUI maxDepthText;
    [SerializeField] private TextMeshProUGUI averageDepthText;
    [SerializeField] private TextMeshProUGUI sessionTimeText;
    
    [Header("=== 深度可视化 ===")]
    [SerializeField] private Slider depthSlider;
    [SerializeField] private Image depthFillImage;
    [SerializeField] private Gradient depthColorGradient;
    
    [Header("=== 按压质量指示器 ===")]
    [SerializeField] private Image pressQualityIndicator;
    [SerializeField] private TextMeshProUGUI pressQualityText;
    
    [Header("=== 控制按钮 ===")]
    [SerializeField] private Button resetButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button toggleUIButton;
    
    [Header("=== 设置 ===")]
    [SerializeField] private float maxDisplayDepth = 0.1f;
    [SerializeField] private bool autoCreateUI = true;
    [SerializeField] private Vector3 uiWorldPosition = new Vector3(0, 2f, 2f);
    [SerializeField] private Vector3 uiScale = Vector3.one * 0.01f;
    
    private bool isUIVisible = true;
    private Coroutine updateCoroutine;
    
    void Start()
    {
        if (autoCreateUI && uiCanvas == null)
        {
            CreateUI();
        }
        
        InitializeUI();
        StartUpdateCoroutine();
        
        // 订阅事件
        PressDataManager.OnPressCountUpdated += OnPressCountUpdated;
        PressDataManager.OnDepthUpdated += OnDepthUpdated;
        PressDataManager.OnPressCompleted += OnPressCompleted;
        
        Debug.Log("🎨 PressUIManager 初始化完成");
    }
    
    void CreateUI()
    {
        // 创建Canvas
        GameObject canvasGO = new GameObject("PressUI_Canvas");
        uiCanvas = canvasGO.AddComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.WorldSpace;
        uiCanvas.worldCamera = Camera.main;
        
        // 设置Canvas位置和缩放
        canvasGO.transform.position = uiWorldPosition;
        canvasGO.transform.localScale = uiScale;
        canvasGO.transform.LookAt(Camera.main.transform);
        canvasGO.transform.Rotate(0, 180, 0);
        
        // 添加CanvasScaler
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // 添加GraphicRaycaster
        canvasGO.AddComponent<GraphicRaycaster>();
        
        // 创建主面板
        CreateMainPanel();
        
        Debug.Log("🎨 UI界面自动创建完成");
    }
    
    void CreateMainPanel()
    {
        // 主面板
        GameObject panelGO = new GameObject("MainPanel");
        panelGO.transform.SetParent(uiCanvas.transform, false);
        uiPanel = panelGO;
        
        Image panelImage = panelGO.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.8f);
        
        RectTransform panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(800, 600);
        
        // 添加垂直布局组
        VerticalLayoutGroup layoutGroup = panelGO.AddComponent<VerticalLayoutGroup>();
        layoutGroup.spacing = 20;
        layoutGroup.padding = new RectOffset(30, 30, 30, 30);
        layoutGroup.childControlHeight = false;
        layoutGroup.childControlWidth = true;
        layoutGroup.childForceExpandWidth = true;
        
        // 创建标题
        CreateTitle();
        
        // 创建数据显示区域
        CreateDataDisplayArea();
        
        // 创建深度可视化
        CreateDepthVisualization();
        
        // 创建按压质量指示器
        CreateQualityIndicator();
        
        // 创建控制按钮
        CreateControlButtons();
    }
    
    void CreateTitle()
    {
        GameObject titleGO = CreateTextElement("Title", "胸外按压训练系统", 36, Color.white);
        titleGO.transform.SetParent(uiPanel.transform, false);
        
        RectTransform titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(0, 60);
    }
    
    void CreateDataDisplayArea()
    {
        // 数据容器
        GameObject dataContainer = new GameObject("DataContainer");
        dataContainer.transform.SetParent(uiPanel.transform, false);
        
        GridLayoutGroup gridLayout = dataContainer.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(240, 40);
        gridLayout.spacing = new Vector2(10, 10);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 3;
        
        RectTransform dataRect = dataContainer.GetComponent<RectTransform>();
        dataRect.sizeDelta = new Vector2(0, 120);
        
        // 创建数据文本
        totalPressText = CreateDataText(dataContainer, "总按压次数: 0");
        validPressText = CreateDataText(dataContainer, "有效按压: 0");
        sessionTimeText = CreateDataText(dataContainer, "训练时长: 0s");
        currentDepthText = CreateDataText(dataContainer, "当前深度: 0.000m");
        maxDepthText = CreateDataText(dataContainer, "最大深度: 0.000m");
        averageDepthText = CreateDataText(dataContainer, "平均深度: 0.000m");
    }
    
    void CreateDepthVisualization()
    {
        // 深度滑块容器
        GameObject sliderContainer = new GameObject("DepthSliderContainer");
        sliderContainer.transform.SetParent(uiPanel.transform, false);
        
        RectTransform sliderRect = sliderContainer.GetComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(0, 80);
        
        // 标签
        GameObject labelGO = CreateTextElement("DepthLabel", "按压深度实时显示", 20, Color.white);
        labelGO.transform.SetParent(sliderContainer.transform, false);
        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchoredPosition = new Vector2(0, 20);
        labelRect.sizeDelta = new Vector2(0, 30);
        
        // 滑块
        GameObject sliderGO = new GameObject("DepthSlider");
        sliderGO.transform.SetParent(sliderContainer.transform, false);
        
        depthSlider = sliderGO.AddComponent<Slider>();
        depthSlider.minValue = 0f;
        depthSlider.maxValue = maxDisplayDepth;
        depthSlider.interactable = false;
        
        RectTransform sliderRectTransform = sliderGO.GetComponent<RectTransform>();
        sliderRectTransform.anchoredPosition = new Vector2(0, -20);
        sliderRectTransform.sizeDelta = new Vector2(0, 30);
        
        // 滑块背景
        GameObject background = new GameObject("Background");
        background.transform.SetParent(sliderGO.transform, false);
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        RectTransform bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;
        
        // 填充区域
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGO.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;
        fillAreaRect.anchoredPosition = Vector2.zero;
        
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        depthFillImage = fill.AddComponent<Image>();
        depthFillImage.color = Color.green;
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;
        
        depthSlider.fillRect = fillRect;
        
        // 初始化颜色渐变
        if (depthColorGradient == null)
        {
            depthColorGradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[3];
            colorKeys[0] = new GradientColorKey(Color.green, 0.0f);
            colorKeys[1] = new GradientColorKey(Color.yellow, 0.5f);
            colorKeys[2] = new GradientColorKey(Color.red, 1.0f);
            
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f);
            alphaKeys[1] = new GradientAlphaKey(1.0f, 1.0f);
            
            depthColorGradient.SetKeys(colorKeys, alphaKeys);
        }
    }
    
    void CreateQualityIndicator()
    {
        GameObject qualityContainer = new GameObject("QualityContainer");
        qualityContainer.transform.SetParent(uiPanel.transform, false);
        
        RectTransform qualityRect = qualityContainer.GetComponent<RectTransform>();
        qualityRect.sizeDelta = new Vector2(0, 60);
        
        HorizontalLayoutGroup hLayout = qualityContainer.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing = 20;
        hLayout.childControlWidth = false;
        hLayout.childControlHeight = true;
        hLayout.childForceExpandHeight = true;
        
        // 质量指示器圆圈
        GameObject indicatorGO = new GameObject("QualityIndicator");
        indicatorGO.transform.SetParent(qualityContainer.transform, false);
        
        pressQualityIndicator = indicatorGO.AddComponent<Image>();
        pressQualityIndicator.color = Color.gray;
        
        RectTransform indicatorRect = indicatorGO.GetComponent<RectTransform>();
        indicatorRect.sizeDelta = new Vector2(50, 50);
        
        // 质量文本
        GameObject qualityTextGO = CreateTextElement("QualityText", "按压质量: 待检测", 18, Color.white);
        qualityTextGO.transform.SetParent(qualityContainer.transform, false);
        pressQualityText = qualityTextGO.GetComponent<TextMeshProUGUI>();
        
        LayoutElement textLayout = qualityTextGO.AddComponent<LayoutElement>();
        textLayout.flexibleWidth = 1;
    }
    
    void CreateControlButtons()
    {
        GameObject buttonContainer = new GameObject("ButtonContainer");
        buttonContainer.transform.SetParent(uiPanel.transform, false);
        
        RectTransform buttonRect = buttonContainer.GetComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(0, 60);
        
        HorizontalLayoutGroup hLayout = buttonContainer.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing = 20;
        hLayout.childControlWidth = true;
        hLayout.childControlHeight = true;
        hLayout.childForceExpandWidth = true;
        hLayout.childForceExpandHeight = true;
        
        // 重置按钮
        resetButton = CreateButton(buttonContainer, "重置数据", Color.red);
        resetButton.onClick.AddListener(ResetData);
        
        // 保存按钮
        saveButton = CreateButton(buttonContainer, "保存数据", Color.blue);
        saveButton.onClick.AddListener(SaveData);
        
        // 切换UI按钮
        toggleUIButton = CreateButton(buttonContainer, "隐藏UI", Color.gray);
        toggleUIButton.onClick.AddListener(ToggleUI);
    }
    
    GameObject CreateTextElement(string name, string text, int fontSize, Color color)
    {
        GameObject textGO = new GameObject(name);
        TextMeshProUGUI textComponent = textGO.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.color = color;
        textComponent.alignment = TextAlignmentOptions.Center;
        
        return textGO;
    }
    
    TextMeshProUGUI CreateDataText(GameObject parent, string initialText)
    {
        GameObject textGO = CreateTextElement("DataText", initialText, 16, Color.white);
        textGO.transform.SetParent(parent.transform, false);
        
        // 添加背景
        Image bgImage = textGO.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.7f);
        
        return textGO.GetComponent<TextMeshProUGUI>();
    }
    
    Button CreateButton(GameObject parent, string text, Color color)
    {
        GameObject buttonGO = new GameObject("Button_" + text);
        buttonGO.transform.SetParent(parent.transform, false);
        
        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = color;
        
        Button button = buttonGO.AddComponent<Button>();
        
        GameObject textGO = CreateTextElement("Text", text, 16, Color.white);
        textGO.transform.SetParent(buttonGO.transform, false);
        
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        
        return button;
    }
    
    void InitializeUI()
    {
        if (uiPanel != null)
        {
            uiPanel.SetActive(isUIVisible);
        }
    }
    
    void StartUpdateCoroutine()
    {
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
        }
        updateCoroutine = StartCoroutine(UpdateUICoroutine());
    }
    
    IEnumerator UpdateUICoroutine()
    {
        while (true)
        {
            UpdateUI();
            yield return new WaitForSeconds(0.1f); // 10fps更新
        }
    }
    
    void UpdateUI()
    {
        if (PressDataManager.Instance == null) return;
        
        // 更新数据文本
        if (totalPressText != null)
            totalPressText.text = $"总按压次数: {PressDataManager.Instance.GetTotalPressCount()}";
        
        if (validPressText != null)
            validPressText.text = $"有效按压: {PressDataManager.Instance.GetValidPressCount()}";
        
        if (sessionTimeText != null)
            sessionTimeText.text = $"训练时长: {PressDataManager.Instance.GetSessionDuration():F0}s";
        
        if (currentDepthText != null)
            currentDepthText.text = $"当前深度: {PressDataManager.Instance.GetCurrentDepth():F3}m";
        
        if (maxDepthText != null)
            maxDepthText.text = $"最大深度: {PressDataManager.Instance.GetMaxDepth():F3}m";
        
        if (averageDepthText != null)
            averageDepthText.text = $"平均深度: {PressDataManager.Instance.GetAverageDepth():F3}m";
        
        // 更新深度滑块
        UpdateDepthSlider();
        
        // 更新质量指示器
        UpdateQualityIndicator();
    }
    
    void UpdateDepthSlider()
    {
        if (depthSlider != null && PressDataManager.Instance != null)
        {
            float currentDepth = PressDataManager.Instance.GetCurrentDepth();
            depthSlider.value = currentDepth;
            
            // 更新颜色
            if (depthFillImage != null && depthColorGradient != null)
            {
                float normalizedDepth = currentDepth / maxDisplayDepth;
                depthFillImage.color = depthColorGradient.Evaluate(normalizedDepth);
            }
        }
    }
    
    void UpdateQualityIndicator()
    {
        if (pressQualityIndicator != null && pressQualityText != null && PressDataManager.Instance != null)
        {
            float currentDepth = PressDataManager.Instance.GetCurrentDepth();
            
            if (currentDepth < 0.001f)
            {
                pressQualityIndicator.color = Color.gray;
                pressQualityText.text = "按压质量: 待检测";
            }
            else if (currentDepth < 0.02f)
            {
                pressQualityIndicator.color = Color.red;
                pressQualityText.text = "按压质量: 太浅";
            }
            else if (currentDepth > 0.06f)
            {
                pressQualityIndicator.color = new Color(1f, 0.5f, 0f); // 橙色
                pressQualityText.text = "按压质量: 太深";
            }
            else
            {
                pressQualityIndicator.color = Color.green;
                pressQualityText.text = "按压质量: 良好";
            }
        }
    }
    
    // 事件处理
    void OnPressCountUpdated(int total, int valid)
    {
        // UI会在UpdateUI中自动更新
    }
    
    void OnDepthUpdated(float depth)
    {
        // UI会在UpdateUI中自动更新
    }
    
    void OnPressCompleted(PressData pressData)
    {
        // 可以在这里添加按压完成的视觉反馈
        if (pressData.isValidPress)
        {
            StartCoroutine(FlashGreenIndicator());
        }
        else
        {
            StartCoroutine(FlashRedIndicator());
        }
    }
    
    IEnumerator FlashGreenIndicator()
    {
        if (pressQualityIndicator != null)
        {
            Color originalColor = pressQualityIndicator.color;
            pressQualityIndicator.color = Color.green;
            yield return new WaitForSeconds(0.3f);
            pressQualityIndicator.color = originalColor;
        }
    }
    
    IEnumerator FlashRedIndicator()
    {
        if (pressQualityIndicator != null)
        {
            Color originalColor = pressQualityIndicator.color;
            pressQualityIndicator.color = Color.red;
            yield return new WaitForSeconds(0.3f);
            pressQualityIndicator.color = originalColor;
        }
    }
    
    // 按钮事件
    void ResetData()
    {
        if (PressDataManager.Instance != null)
        {
            PressDataManager.Instance.ResetSession();
            Debug.Log("🔄 数据已重置");
        }
    }
    
    void SaveData()
    {
        if (PressDataManager.Instance != null)
        {
            PressDataManager.Instance.SaveSessionData();
            Debug.Log("💾 数据已保存");
        }
    }
    
    void ToggleUI()
    {
        isUIVisible = !isUIVisible;
        if (uiPanel != null)
        {
            uiPanel.SetActive(isUIVisible);
        }
        
        if (toggleUIButton != null)
        {
            TextMeshProUGUI buttonText = toggleUIButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = isUIVisible ? "隐藏UI" : "显示UI";
            }
        }
    }
    
    void OnDestroy()
    {
        // 取消订阅事件
        PressDataManager.OnPressCountUpdated -= OnPressCountUpdated;
        PressDataManager.OnDepthUpdated -= OnDepthUpdated;
        PressDataManager.OnPressCompleted -= OnPressCompleted;
        
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
        }
    }
}
