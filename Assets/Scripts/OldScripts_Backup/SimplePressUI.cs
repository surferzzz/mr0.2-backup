using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using UnityEngine.EventSystems;

/// <summary>
/// 简化版按压UI管理器 - 使用Unity标准UI组件
/// 适用于快速测试和基础功能展示
/// </summary>
public class SimplePressUI : MonoBehaviour
{
    [Header("=== UI设置 ===")]
    [SerializeField] private bool autoCreateUI = true;
    [SerializeField] private Vector3 uiPosition = new Vector3(0, 2f, 3f);
    [SerializeField] private float uiScale = 0.005f;
    
    [Header("=== UI组件 ===")]
    private Canvas canvas;
    private GameObject panel;
    private Text statusText;
    private Text dataText;
    private Slider depthSlider;
    private Image qualityIndicator;
    private Button toggleUIButton;
    private Button quakeButton;
    private EarthquakeEffect quakeEffect;
    private bool isUIVisible = true;
    
    [Header("=== 数据显示 ===")]
    private int totalPress = 0;
    private int validPress = 0;
    private float currentDepth = 0f;
    private float maxDepth = 0f;
    private string qualityStatus = "検出待ち";
    private ScenarioManager scenarioManager;
    
    void Start()
    {
        if (autoCreateUI)
        {
            CreateSimpleUI();
        }
        SetupEarthquakeEffect();
        EnsureEventSystemSupportsXR();
        scenarioManager = FindObjectOfType<ScenarioManager>();
        
        // 订阅数据事件
        PressDataManager.OnPressCountUpdated += OnPressCountUpdated;
        PressDataManager.OnDepthUpdated += OnDepthUpdated;
        
        StartCoroutine(UpdateUILoop());
        
        Debug.Log("✅ SimplePressUI 初始化完成");
    }
    
    void CreateSimpleUI()
    {
        // 创建Canvas
        GameObject canvasGO = new GameObject("SimplePressUI_Canvas");
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        // 设置Canvas位置（相对于摄像机）
        if (Camera.main != null)
        {
            canvasGO.transform.SetParent(Camera.main.transform);
            canvasGO.transform.localPosition = new Vector3(0, 0, 2f); // 摄像机前方2米
            canvasGO.transform.localRotation = Quaternion.identity;
            canvasGO.transform.localScale = Vector3.one * uiScale;
        }
        else
        {
            canvasGO.transform.position = uiPosition;
            canvasGO.transform.localScale = Vector3.one * uiScale;
        }
        
        // 添加CanvasScaler
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;
        
        // 添加GraphicRaycaster
        canvasGO.AddComponent<GraphicRaycaster>();
        AddXRRaycasters(canvasGO);
        
        // 创建主面板
        CreatePanel();
        CreateTexts();
        CreateSlider();
        CreateQualityIndicator();
        CreateQuakeButton();
        CreateToggleButton();
        
        Debug.Log("🎨 简化UI创建完成");
    }
    
    void CreatePanel()
    {
        panel = new GameObject("Panel");
        panel.transform.SetParent(canvas.transform, false);
        
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(600, 400);
        rect.anchoredPosition = Vector2.zero;
        
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0, 0, 0, 0.8f);
    }
    
    void CreateTexts()
    {
        // 标题
        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(panel.transform, false);
        
        RectTransform titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(500, 60);
        titleRect.anchoredPosition = new Vector2(0, 150);
        
        Text titleText = titleGO.AddComponent<Text>();
        titleText.text = "CPR訓練システム";
        titleText.font = GetDefaultFont();
        titleText.fontSize = 32;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;
        
        // 状态文本
        GameObject statusGO = new GameObject("StatusText");
        statusGO.transform.SetParent(panel.transform, false);
        
        RectTransform statusRect = statusGO.AddComponent<RectTransform>();
        statusRect.sizeDelta = new Vector2(500, 40);
        statusRect.anchoredPosition = new Vector2(0, 80);
        
        statusText = statusGO.AddComponent<Text>();
        statusText.text = "準備完了...";
        statusText.font = GetDefaultFont();
        statusText.fontSize = 20;
        statusText.alignment = TextAnchor.MiddleCenter;
        statusText.color = Color.yellow;
        
        // 数据文本
        GameObject dataGO = new GameObject("DataText");
        dataGO.transform.SetParent(panel.transform, false);
        
        RectTransform dataRect = dataGO.AddComponent<RectTransform>();
        dataRect.sizeDelta = new Vector2(500, 150);
        dataRect.anchoredPosition = new Vector2(0, -20);
        
        dataText = dataGO.AddComponent<Text>();
        dataText.text = GetDataString();
        dataText.font = GetDefaultFont();
        dataText.fontSize = 18;
        dataText.alignment = TextAnchor.UpperLeft;
        dataText.color = Color.white;
    }
    
    void CreateSlider()
    {
        GameObject sliderGO = new GameObject("DepthSlider");
        sliderGO.transform.SetParent(panel.transform, false);
        
        RectTransform sliderRect = sliderGO.AddComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(500, 30);
        sliderRect.anchoredPosition = new Vector2(0, -120);
        
        depthSlider = sliderGO.AddComponent<Slider>();
        depthSlider.minValue = 0f;
        depthSlider.maxValue = 0.1f;
        depthSlider.value = 0f;
        
        // 背景
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(sliderGO.transform, false);
        RectTransform bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f);
        
        // 填充区域
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGO.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;
        
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = Color.green;
        
        depthSlider.fillRect = fillRect;
    }
    
    void CreateQualityIndicator()
    {
        GameObject indicatorGO = new GameObject("QualityIndicator");
        indicatorGO.transform.SetParent(panel.transform, false);
        
        RectTransform indicatorRect = indicatorGO.AddComponent<RectTransform>();
        indicatorRect.sizeDelta = new Vector2(40, 40);
        indicatorRect.anchoredPosition = new Vector2(0, -170);
        
        qualityIndicator = indicatorGO.AddComponent<Image>();
        qualityIndicator.color = Color.gray;
        
        // 质量文本
        GameObject qualityTextGO = new GameObject("QualityText");
        qualityTextGO.transform.SetParent(indicatorGO.transform, false);
        
        RectTransform qualityTextRect = qualityTextGO.AddComponent<RectTransform>();
        qualityTextRect.sizeDelta = new Vector2(200, 30);
        qualityTextRect.anchoredPosition = new Vector2(130, 0);
        
        Text qualityText = qualityTextGO.AddComponent<Text>();
        qualityText.text = "圧迫品質: 検出待ち";
        qualityText.font = GetDefaultFont();
        qualityText.fontSize = 16;
        qualityText.alignment = TextAnchor.MiddleLeft;
        qualityText.color = Color.white;
    }

    void CreateQuakeButton()
    {
        // 地震シミュレーションボタン（下部中央寄せ）
        GameObject quakeGO = CreateButton("QuakeButton", "⚡ 地震シミュレーション", new Vector2(0, -170));
        quakeButton = quakeGO.GetComponent<Button>();
        quakeButton.onClick.AddListener(TriggerEarthquake);
    }

    void CreateToggleButton()
    {
        // 单独的切换按钮，隐藏/显示主面板
        // UI 表示切替ボタン（右下寄せ）
        GameObject toggleGO = CreateButton("ToggleUIButton", "UIを非表示", new Vector2(220, -170));
        toggleGO.transform.SetParent(canvas.transform, false);
        toggleUIButton = toggleGO.GetComponent<Button>();
        toggleUIButton.onClick.AddListener(ToggleUIVisibility);
    }
    
    IEnumerator UpdateUILoop()
    {
        while (true)
        {
            UpdateUI();
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    void UpdateUI()
    {
        if (PressDataManager.Instance != null)
        {
            totalPress = PressDataManager.Instance.GetTotalPressCount();
            validPress = PressDataManager.Instance.GetValidPressCount();
            currentDepth = PressDataManager.Instance.GetCurrentDepth();
            maxDepth = PressDataManager.Instance.GetMaxDepth();
            
            // 更新数据文本
            if (dataText != null)
            {
                dataText.text = GetDataString();
            }
            
            // 更新滑块
            if (depthSlider != null)
            {
                depthSlider.value = currentDepth;
                
                // 更新滑块颜色
                Image fillImage = depthSlider.fillRect?.GetComponent<Image>();
                if (fillImage != null)
                {
                    if (currentDepth < 0.02f)
                        fillImage.color = Color.red;
                    else if (currentDepth > 0.06f)
                        fillImage.color = Color.yellow;
                    else
                        fillImage.color = Color.green;
                }
            }
            
            // 更新质量指示器
            UpdateQualityIndicator();
            
            // 更新状态文本
            if (statusText != null)
            {
                if (currentDepth > 0.001f)
                {
                    statusText.text = "圧迫中...";
                    statusText.color = Color.green;
                }
                else if (totalPress > 0)
                {
                    statusText.text = "次の圧迫を待機中";
                    statusText.color = Color.yellow;
                }
                else
                {
                    statusText.text = "準備完了...";
                    statusText.color = Color.white;
                }
            }
        }
    }
    
    void UpdateQualityIndicator()
    {
        if (qualityIndicator == null) return;
        
        if (currentDepth < 0.001f)
        {
            qualityIndicator.color = Color.gray;
            qualityStatus = "検出待ち";
        }
        else if (currentDepth < 0.02f)
        {
            qualityIndicator.color = Color.red;
            qualityStatus = "浅すぎる";
        }
        else if (currentDepth > 0.06f)
        {
            qualityIndicator.color = new Color(1f, 0.5f, 0f); // 橙色
            qualityStatus = "深すぎる";
        }
        else
        {
            qualityIndicator.color = Color.green;
            qualityStatus = "良好";
        }
    }
    
    string GetDataString()
    {
        return $"総圧迫回数: {totalPress}\n" +
               $"有効圧迫: {validPress}\n" +
               $"現在深度: {currentDepth:F3}m\n" +
               $"最大深度: {maxDepth:F3}m\n" +
               $"圧迫品質: {qualityStatus}";
    }
    
    void OnPressCountUpdated(int total, int valid)
    {
        totalPress = total;
        validPress = valid;
    }
    
    void OnDepthUpdated(float depth)
    {
        currentDepth = depth;
    }
    
    void OnDestroy()
    {
        PressDataManager.OnPressCountUpdated -= OnPressCountUpdated;
        PressDataManager.OnDepthUpdated -= OnDepthUpdated;
    }
    
    // 公共方法
    public void SetUIPosition(Vector3 position)
    {
        if (canvas != null)
        {
            canvas.transform.position = position;
        }
    }
    
    public void SetUIScale(float scale)
    {
        if (canvas != null)
        {
            canvas.transform.localScale = Vector3.one * scale;
        }
    }

    private void AddXRRaycasters(GameObject canvasGO)
    {
        // 尝试添加 XR 射线器，让手柄射线可以点击 UI
        Type xrRaycasterType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
        if (xrRaycasterType != null && canvasGO.GetComponent(xrRaycasterType) == null)
        {
            canvasGO.AddComponent(xrRaycasterType);
        }
    }

    private void EnsureEventSystemSupportsXR()
    {
        var es = FindObjectOfType<EventSystem>();
        if (es == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            es = esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
        }

        Type xrModuleType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule, Unity.XR.Interaction.Toolkit");
        if (xrModuleType != null && es.GetComponent(xrModuleType) == null)
        {
            // 避免多输入模块冲突，移除旧 StandaloneInputModule
            var legacy = es.GetComponent<StandaloneInputModule>();
            if (legacy != null)
            {
                Destroy(legacy);
            }
            es.gameObject.AddComponent(xrModuleType);
        }
    }

    private static Font GetDefaultFont()
    {
        // Unity 2023+ removes Arial as a built-in font; LegacyRuntime.ttf stays available.
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private GameObject CreateButton(string name, string label, Vector2 anchoredPos)
    {
        GameObject buttonGO = new GameObject(name);
        buttonGO.transform.SetParent(panel.transform, false);

        RectTransform rect = buttonGO.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(180, 40);
        rect.anchoredPosition = anchoredPos;

        Image bg = buttonGO.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.5f, 0.15f, 0.85f);

        Button button = buttonGO.AddComponent<Button>();

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        Text text = textGO.AddComponent<Text>();
        text.text = label;
        text.font = GetDefaultFont();
        text.fontSize = 18;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        return buttonGO;
    }

    private void ToggleUIVisibility()
    {
        isUIVisible = !isUIVisible;
        if (panel != null)
        {
            panel.SetActive(isUIVisible);
        }

        if (toggleUIButton != null)
        {
            Text btnText = toggleUIButton.GetComponentInChildren<Text>();
            if (btnText != null)
            {
                btnText.text = isUIVisible ? "UIを非表示" : "UIを表示";
            }
        }
    }

    private void SetupEarthquakeEffect()
    {
        quakeEffect = FindObjectOfType<EarthquakeEffect>();
        if (quakeEffect == null && Camera.main != null)
        {
            quakeEffect = Camera.main.gameObject.AddComponent<EarthquakeEffect>();
        }
    }

    private void TriggerEarthquake()
    {
        if (quakeEffect == null)
        {
            Debug.LogWarning("未找到地震效果组件，已尝试自动添加但仍为空。");
            SetupEarthquakeEffect();
        }

        if (quakeEffect != null)
        {
            quakeEffect.PlayQuake();
        }
        else
        {
            Debug.LogWarning("地震效果不可用，无法播放。");
        }
    }
}
