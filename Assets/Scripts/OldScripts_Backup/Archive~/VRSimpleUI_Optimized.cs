using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// VR専用シンプルUI - Quest 3最適化版（更新頻度を削減）
/// </summary>
public class VRSimpleUI_Optimized : MonoBehaviour
{
    [Header("=== VR UI 設定 ===")]
    [Tooltip("UIとカメラの距離（メートル）")]
    public float distanceFromCamera = 2f;
    
    [Tooltip("UIのサイズ")]
    public float uiScale = 0.001f;
    
    [Header("=== パフォーマンス設定 ===")]
    [Tooltip("UI更新間隔（秒）- 値を大きくすると軽くなる")]
    public float updateInterval = 0.1f; // 0.1秒ごとに更新（10fps）
    
    [Tooltip("位置更新間隔（秒）")]
    public float positionUpdateInterval = 0.05f; // 20fps
    
    [Header("=== 表示情報 ===")]
    public int hitCount = 0;
    public string lastHitObject = "なし";
    
    private Canvas canvas;
    private Text infoText;
    private GameObject canvasObject;
    
    private float lastUpdateTime = 0f;
    private float lastPositionUpdateTime = 0f;
    
    void Start()
    {
        CreateVRUI();
        FindAndSubscribeToCollisionEvents();
    }
    
    void CreateVRUI()
    {
        Debug.Log("✅ VR UI（最適化版）を作成中...");
        
        // Canvas作成
        canvasObject = new GameObject("VR_UI_Canvas_Optimized");
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;
        
        canvasObject.AddComponent<GraphicRaycaster>();
        
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1000, 500);
        
        // 背景パネル
        GameObject panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(canvasObject.transform, false);
        
        Image panelImage = panelGO.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.7f); // 少し透明度を上げて軽量化
        
        RectTransform panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        
        // テキスト作成
        GameObject textGO = new GameObject("InfoText");
        textGO.transform.SetParent(panelGO.transform, false);
        
        infoText = textGO.AddComponent<Text>();
        infoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        infoText.fontSize = 36; // フォントサイズを少し小さく
        infoText.color = Color.white;
        infoText.alignment = TextAnchor.MiddleCenter;
        infoText.text = "🎯 CPR訓練システム\n\n準備完了...\n\n胸部を圧迫してください";
        
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        
        Debug.Log("✅ VR UI作成完了（最適化版）");
    }
    
    void Update()
    {
        // 位置更新（高頻度だが軽量）
        if (Time.time - lastPositionUpdateTime >= positionUpdateInterval)
        {
            UpdatePosition();
            lastPositionUpdateTime = Time.time;
        }
        
        // テキスト更新（低頻度で重い処理）
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateText();
            lastUpdateTime = Time.time;
        }
    }
    
    void UpdatePosition()
    {
        if (Camera.main != null && canvasObject != null)
        {
            Vector3 targetPosition = Camera.main.transform.position + 
                                   Camera.main.transform.forward * distanceFromCamera;
            canvasObject.transform.position = targetPosition;
            
            canvasObject.transform.rotation = Quaternion.LookRotation(
                canvasObject.transform.position - Camera.main.transform.position);
            
            canvasObject.transform.localScale = Vector3.one * uiScale;
        }
    }
    
    void UpdateText()
    {
        if (infoText != null)
        {
            // シンプルな表示で負荷を軽減
            infoText.text = $"🎯 CPR訓練システム\n\n" +
                          $"圧迫回数: {hitCount}\n" +
                          $"最後の衝突: {lastHitObject}\n\n" +
                          $"ステータス: 実行中";
        }
    }
    
    void FindAndSubscribeToCollisionEvents()
    {
        SuperSimpleCollisionTest[] testScripts = FindObjectsOfType<SuperSimpleCollisionTest>();
        
        if (testScripts.Length > 0)
        {
            Debug.Log($"✅ {testScripts.Length}個の検出スクリプトを発見");
        }
        else
        {
            Debug.LogWarning("⚠️ 検出スクリプトが見つかりません");
        }
    }
    
    public void OnCollisionDetected(string objectName)
    {
        hitCount++;
        lastHitObject = objectName;
        
        // 即座にテキスト更新（衝突時のみ）
        UpdateText();
    }
}






