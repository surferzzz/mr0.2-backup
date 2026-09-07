using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quest 3 パフォーマンスモニター - FPS/メモリ使用量をリアルタイム表示
/// </summary>
public class Quest3PerformanceMonitor : MonoBehaviour
{
    [Header("=== 表示設定 ===")]
    public bool showFPS = true;
    public bool showMemory = true;
    public KeyCode toggleKey = KeyCode.F1;
    
    [Header("=== 警告設定 ===")]
    public float warningFPS = 60f; // 60fps以下で警告
    public float criticalFPS = 45f; // 45fps以下で重大警告
    
    private float deltaTime = 0f;
    private float fps = 0f;
    private int frameCount = 0;
    private float lastUpdateTime = 0f;
    private float updateInterval = 0.5f; // 0.5秒ごとに更新
    
    private GameObject uiObject;
    private Text fpsText;
    private bool isVisible = true;
    
    void Start()
    {
        CreatePerformanceUI();
        Application.targetFrameRate = 72; // Quest 3推奨フレームレート
    }
    
    void CreatePerformanceUI()
    {
        // Canvas作成
        uiObject = new GameObject("PerformanceMonitor_UI");
        Canvas canvas = uiObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        CanvasScaler scaler = uiObject.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;
        
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(400, 200);
        
        // 背景
        GameObject panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(uiObject.transform, false);
        
        Image panelImage = panelGO.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.5f);
        
        RectTransform panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        
        // テキスト
        GameObject textGO = new GameObject("FPSText");
        textGO.transform.SetParent(panelGO.transform, false);
        
        fpsText = textGO.AddComponent<Text>();
        fpsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        fpsText.fontSize = 28;
        fpsText.color = Color.green;
        fpsText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        
        Debug.Log("✅ パフォーマンスモニター作成完了");
    }
    
    void Update()
    {
        // トグル
        if (Input.GetKeyDown(toggleKey))
        {
            isVisible = !isVisible;
            uiObject.SetActive(isVisible);
        }
        
        // FPS計算
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        frameCount++;
        
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            fps = frameCount / (Time.time - lastUpdateTime);
            frameCount = 0;
            lastUpdateTime = Time.time;
            
            UpdateUI();
        }
        
        // UIをカメラに追従
        if (Camera.main != null && uiObject != null)
        {
            Vector3 targetPosition = Camera.main.transform.position + 
                                   Camera.main.transform.forward * 3f +
                                   Camera.main.transform.up * 1f;
            uiObject.transform.position = targetPosition;
            
            uiObject.transform.rotation = Quaternion.LookRotation(
                uiObject.transform.position - Camera.main.transform.position);
            
            uiObject.transform.localScale = Vector3.one * 0.0015f;
        }
    }
    
    void UpdateUI()
    {
        if (fpsText == null) return;
        
        string text = "";
        
        if (showFPS)
        {
            text += $"FPS: {fps:F1}\n";
            text += $"MS: {deltaTime * 1000f:F1}ms\n";
        }
        
        if (showMemory)
        {
            float memoryMB = (System.GC.GetTotalMemory(false) / 1048576f);
            text += $"メモリ: {memoryMB:F1}MB\n";
        }
        
        // パフォーマンス評価
        if (fps < criticalFPS)
        {
            text += "\n⚠️ 重大: FPS低下！";
            fpsText.color = Color.red;
        }
        else if (fps < warningFPS)
        {
            text += "\n⚠️ 警告: 最適化推奨";
            fpsText.color = new Color(1f, 0.5f, 0f); // オレンジ
        }
        else
        {
            text += "\n✅ 正常";
            fpsText.color = Color.green;
        }
        
        fpsText.text = text;
    }
    
    void OnGUI()
    {
        // デバッグ用画面左上表示
        if (!isVisible) return;
        
        GUIStyle style = new GUIStyle();
        style.fontSize = 24;
        style.normal.textColor = fps < criticalFPS ? Color.red : 
                                 fps < warningFPS ? Color.yellow : Color.green;
        
        Rect rect = new Rect(10, 10, 300, 100);
        string text = $"FPS: {fps:F1}\n" +
                     $"MS: {deltaTime * 1000f:F1}ms\n" +
                     $"Target: 72fps";
        
        GUI.Label(rect, text, style);
    }
}






