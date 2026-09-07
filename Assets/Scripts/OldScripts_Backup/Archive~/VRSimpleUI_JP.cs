using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// VR専用シンプルUI - Quest 3で自動表示（日本語版）
/// </summary>
public class VRSimpleUI_JP : MonoBehaviour
{
    [Header("=== VR UI 設定 ===")]
    [Tooltip("UIとカメラの距離（メートル）")]
    public float distanceFromCamera = 2f;
    
    [Tooltip("UIのサイズ")]
    public float uiScale = 0.001f;
    
    [Header("=== 表示情報 ===")]
    public int hitCount = 0;
    public string lastHitObject = "なし";
    
    private Canvas canvas;
    private Text infoText;
    private GameObject canvasObject;
    
    void Start()
    {
        CreateVRUI();
        FindAndSubscribeToCollisionEvents();
    }
    
    void CreateVRUI()
    {
        Debug.Log("✅ VR UIを作成中...");
        
        // Canvas作成
        canvasObject = new GameObject("VR_UI_Canvas_JP");
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
        panelImage.color = new Color(0, 0, 0, 0.8f);
        
        RectTransform panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        
        // テキスト作成
        GameObject textGO = new GameObject("InfoText");
        textGO.transform.SetParent(panelGO.transform, false);
        
        infoText = textGO.AddComponent<Text>();
        infoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        infoText.fontSize = 40;
        infoText.color = Color.white;
        infoText.alignment = TextAnchor.MiddleCenter;
        infoText.text = "🎯 CPR訓練システム\n\n準備完了...\n\n胸部を圧迫してください";
        
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        
        Debug.Log("✅ VR UI作成完了");
    }
    
    void Update()
    {
        // UIをカメラに追従
        if (Camera.main != null && canvasObject != null)
        {
            Vector3 targetPosition = Camera.main.transform.position + 
                                   Camera.main.transform.forward * distanceFromCamera;
            canvasObject.transform.position = targetPosition;
            
            canvasObject.transform.rotation = Quaternion.LookRotation(
                canvasObject.transform.position - Camera.main.transform.position);
            
            canvasObject.transform.localScale = Vector3.one * uiScale;
        }
        
        UpdateText();
    }
    
    void UpdateText()
    {
        if (infoText != null)
        {
            infoText.text = $"🎯 CPR訓練システム\n\n" +
                          $"総按圧回数: {hitCount}\n" +
                          $"有効按圧: 0\n" +
                          $"現在深度: 0.000m\n" +
                          $"最大深度: 0.001m\n\n" +
                          $"按圧品質: 検出待ち\n\n" +
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
        
        Debug.Log($"🎯 VR UI更新: 圧迫回数 {hitCount}, 対象 {objectName}");
    }
}

