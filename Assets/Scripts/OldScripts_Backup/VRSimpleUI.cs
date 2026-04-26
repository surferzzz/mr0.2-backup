using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// VR 简易世界空间 UI（可配置跟随模式 + 简洁面板样式）
/// </summary>
public class VRSimpleUI : MonoBehaviour
{
    public enum VRUIFollowMode
    {
        FixedWorld,   // 固定在世界中的某个位置，不随相机运动
        Billboard,    // 只朝向相机（看板效果），不改变位置
        HeadLocked    // 跟随相机前方（头部锁定）
    }

    [Header("=== VR UI 设置 ===")]
    [Tooltip("UI 距离摄像机的距离（HeadLocked 模式）")]
    public float distanceFromCamera = 2f;

    [Tooltip("UI 的整体缩放")]
    public float uiScale = 0.001f;

    [Tooltip("UI 跟随模式")]
    public VRUIFollowMode followMode = VRUIFollowMode.Billboard;

    [Tooltip("世界锚点：在 Billboard/FixedWorld 模式下的放置参考点")]
    public Transform anchor;

    [Tooltip("相对锚点的偏移（世界空间）")]
    public Vector3 anchorOffset = new Vector3(0f, 1.6f, 2f);

    [Header("=== 跟随与朝向平滑 ===")]
    [Tooltip("位置插值速度（越大越跟手）")]
    public float positionLerp = 10f;

    [Tooltip("旋转插值速度（越大越跟手）")]
    public float rotationLerp = 12f;

    [Tooltip("仅对齐 Yaw（水平方向），减少仰俯导致的抖动")]
    public bool lockYawOnly = true;

    [Header("=== 显示信息 ===")]
    public int hitCount = 0;
    public string lastHitObject = "-";

    private Canvas canvas;
    private Text infoText;
    private GameObject canvasObject;
    private RectTransform canvasRect;

    void Start()
    {
        CreateVRUI();
        FindAndSubscribeToCollisionEvents();
    }

    void CreateVRUI()
    {
        Debug.Log("[VRSimpleUI] 构建世界空间 UI...");

        // Canvas
        canvasObject = new GameObject("VR_UI_Canvas");
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;
        canvasObject.AddComponent<GraphicRaycaster>();

        // 尺寸
        canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1000, 560);

        // 背景 Panel + 阴影
        var panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(canvasObject.transform, false);
        var panelImage = panelGO.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.75f);
        var panelShadow = panelGO.AddComponent<Shadow>();
        panelShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
        panelShadow.effectDistance = new Vector2(8, -8);

        var panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;

        // 布局
        var layout = panelGO.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.padding = new RectOffset(24, 24, 20, 24);
        layout.spacing = 16f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;

        // 头部条
        var headerGO = new GameObject("Header");
        headerGO.transform.SetParent(panelGO.transform, false);
        var headerImage = headerGO.AddComponent<Image>();
        headerImage.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
        var headerRect = headerGO.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 1);
        headerRect.anchorMax = new Vector2(1, 1);
        headerRect.sizeDelta = new Vector2(0, 72);

        var headerTextGO = new GameObject("HeaderText");
        headerTextGO.transform.SetParent(headerGO.transform, false);
        var headerText = headerTextGO.AddComponent<Text>();
        // Unity 2023+ 不再内置 Arial；使用 LegacyRuntime.ttf 作为内置字体
        headerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        headerText.fontSize = 42;
        headerText.color = Color.white;
        headerText.alignment = TextAnchor.MiddleLeft;
        headerText.text = "胸外按压训练 - 状态面板";
        var headerTextRect = headerTextGO.GetComponent<RectTransform>();
        headerTextRect.anchorMin = Vector2.zero;
        headerTextRect.anchorMax = Vector2.one;
        headerTextRect.sizeDelta = new Vector2(-24, 0);
        headerTextRect.anchoredPosition = new Vector2(12, 0);

        // 信息文本
        var textGO = new GameObject("InfoText");
        textGO.transform.SetParent(panelGO.transform, false);
        infoText = textGO.AddComponent<Text>();
        infoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        infoText.fontSize = 36;
        infoText.color = Color.white;
        infoText.alignment = TextAnchor.UpperLeft;
        infoText.text = "初始化中...";
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.sizeDelta = new Vector2(0, -100);

        Debug.Log("[VRSimpleUI] UI 创建完成");
    }

    void Update()
    {
        // 跟随与朝向（可配置）
        var cam = Camera.main;
        if (cam != null && canvasObject != null)
        {
            Vector3 desiredPos = canvasObject.transform.position;

            if (followMode == VRUIFollowMode.HeadLocked)
            {
                desiredPos = cam.transform.position + cam.transform.forward * distanceFromCamera;
            }
            else if (anchor != null)
            {
                desiredPos = anchor.TransformPoint(anchorOffset);
            }

            // 平滑位置
            canvasObject.transform.position = Vector3.Lerp(
                canvasObject.transform.position,
                desiredPos,
                1f - Mathf.Exp(-positionLerp * Time.deltaTime)
            );

            // 旋转：FixedWorld 不旋转；其余模式朝向相机
            if (followMode != VRUIFollowMode.FixedWorld)
            {
                Quaternion targetRot;
                if (lockYawOnly)
                {
                    Vector3 toCam = cam.transform.position - canvasObject.transform.position;
                    toCam.y = 0f;
                    if (toCam.sqrMagnitude < 0.0001f)
                    {
                        toCam = cam.transform.forward; toCam.y = 0f;
                    }
                    targetRot = Quaternion.LookRotation(-toCam.normalized);
                }
                else
                {
                    targetRot = Quaternion.LookRotation(
                        canvasObject.transform.position - cam.transform.position
                    );
                }

                canvasObject.transform.rotation = Quaternion.Slerp(
                    canvasObject.transform.rotation,
                    targetRot,
                    1f - Mathf.Exp(-rotationLerp * Time.deltaTime)
                );
            }

            // 缩放
            canvasObject.transform.localScale = Vector3.one * uiScale;
        }

        UpdateText();
    }

    void UpdateText()
    {
        if (infoText != null)
        {
            infoText.text =
                $"统计\n" +
                $"- 按压次数: {hitCount}\n" +
                $"- 最后碰撞: {lastHitObject}\n\n" +
                $"显示\n" +
                $"- 模式: {followMode}\n" +
                $"- 锁定Yaw: {lockYawOnly}\n";
        }
    }

    void FindAndSubscribeToCollisionEvents()
    {
        // 若需要，可在此订阅其他脚本的事件
        var testScripts = FindObjectsOfType<SuperSimpleCollisionTest>();
        if (testScripts.Length == 0)
        {
            Debug.LogWarning("[VRSimpleUI] 未找到 SuperSimpleCollisionTest，UI 将仅显示基本信息");
        }
    }

    // 公开方法，供其他脚本调用
    public void OnCollisionDetected(string objectName)
    {
        hitCount++;
        lastHitObject = objectName;
        Debug.Log($"[VRSimpleUI] 碰撞次数 {hitCount}, 对象 {objectName}");
    }

    [ContextMenu("切换为固定世界(锚点)")]
    public void SetFixedWorld()
    {
        followMode = VRUIFollowMode.FixedWorld;
    }

    [ContextMenu("切换为只面向相机")]
    public void SetBillboard()
    {
        followMode = VRUIFollowMode.Billboard;
    }

    [ContextMenu("切换为头部跟随")]
    public void SetHeadLocked()
    {
        followMode = VRUIFollowMode.HeadLocked;
    }
}
