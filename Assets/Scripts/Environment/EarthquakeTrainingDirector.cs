using CPRSimulation;
using CPRSimulation.Game;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Connects the earthquake effect to the CPR training flow.
/// </summary>
public class EarthquakeTrainingDirector : MonoBehaviour
{
    [Header("References")]
    public EarthquakeController earthquakeController;
    public CPRTrainingController trainingController;
    public Button startEarthquakeButton;
    public Text statusText;
    public GameObject earthquakePanel;

    [Header("Flow")]
    public bool autoStartCPR = true;
    public float startCPRDelay = 1.5f;
    public bool hidePanelWhenCPRStarts = true;
    public bool hideTrainingMenuUntilEarthquakeDone = true;
    public KeyCode editorTriggerKey = KeyCode.E;

    [Header("Whiteboard Placement")]
    public Vector3 whiteboardPosition = new Vector3(0f, 1.69f, 3.44f);
    public float whiteboardRotationY = 0f;
    public float whiteboardCanvasScale = 0.001f;

    // 白板固定在 z=3.44 的墙上，画布 scale 0.001 => 1000 px/m，即 1px = 1mm。
    // VR 正文可读性经验值是视角 >= 1.5°；在 3.44m 处换算：
    //   需要高度 = 2 * 3.44 * tan(0.75°) ≈ 0.090 m => 90 px
    // 原来的 42px 只有 0.70°、按钮 38px 只有 0.63°，都远低于可读线。
    private const int WhiteboardFontSize = 90;
    private const int WhiteboardFontMinSize = 52;   // 下限 52px ≈ 0.87°，Best Fit 收缩时的兜底
    public float frontOffset = 0.06f;

    private bool _earthquakeStarted;
    private bool _earthquakeFinished;
    private GameObject _trainingCanvasGO;

    /// <summary>
    /// 白板上当前显示的是哪一条状态文案。语言切换时要按同一条重新取文字，
    /// 所以只能记状态、不能记已经拼好的字符串。
    /// </summary>
    private enum EarthquakeStatus { None, Ready, Shaking, Finished }
    private EarthquakeStatus _status = EarthquakeStatus.None;

    private void Awake()
    {
        AutoAssign();
        WireButton();
    }

    private void Start()
    {
        StartCoroutine(InitializeAfterUiReady());
    }

    private void OnDestroy()
    {
        CancelInvoke();

        if (LanguageManager.Instance != null)
            LanguageManager.Instance.OnLanguageChanged -= RefreshLocalizedUi;

        if (startEarthquakeButton != null)
            startEarthquakeButton.onClick.RemoveListener(StartEarthquake);
    }

    /// <summary>语言切换时把按钮标签和当前状态文案按新语言重新写一遍。</summary>
    private void RefreshLocalizedUi()
    {
        SetButtonLabel(CPRTextLibrary.BtnStartEarthquake);
        if (_status != EarthquakeStatus.None)
            SetStatus(_status);
    }

    private IEnumerator InitializeAfterUiReady()
    {
        yield return null;

        AutoAssign();
        PlaceEarthquakeUiOnWhiteboard();

        if (earthquakePanel != null)
            earthquakePanel.SetActive(true);

        SetButtonLabel(CPRTextLibrary.BtnStartEarthquake);
        SetStatus(EarthquakeStatus.Ready);
        StyleEarthquakeUi();

        // 订阅放在这里而不是 Start()：LanguageManager 由 CPRTrainingController.Start()
        // 创建，脚本执行顺序不保证它先跑，Start() 里取 Instance 可能还是 null。
        // 这个协程已经让过一帧，此时一定建好了。
        if (LanguageManager.Instance != null)
            LanguageManager.Instance.OnLanguageChanged += RefreshLocalizedUi;
    }

    private void Update()
    {
#if UNITY_EDITOR
        // 快捷键只在编辑器里留着方便反复试流程。打包进头显后它是一个
        // 谁也不知道存在、却能随时重新触发地震的隐藏入口，没有保留的理由。
        if (Input.GetKeyDown(editorTriggerKey))
            StartEarthquake();
#endif
    }

    public void StartEarthquake()
    {
        AutoAssign();

        if (_earthquakeStarted && !_earthquakeFinished)
            return;

        if (earthquakeController == null)
        {
            Debug.LogWarning("[EarthquakeTrainingDirector] Missing EarthquakeController.");
            return;
        }

        _earthquakeStarted = true;
        _earthquakeFinished = false;

        if (hideTrainingMenuUntilEarthquakeDone)
            SetTrainingCanvasVisible(false);

        if (startEarthquakeButton != null)
            startEarthquakeButton.interactable = false;

        SetStatus(EarthquakeStatus.Shaking);
        StyleEarthquakeUi();

        earthquakeController.Play();
        CancelInvoke();
        Invoke(nameof(CompleteEarthquake), earthquakeController.duration);
    }

    public void CompleteEarthquake()
    {
        _earthquakeFinished = true;
        SetStatus(EarthquakeStatus.Finished);

        if (startEarthquakeButton != null)
            startEarthquakeButton.interactable = true;

        StyleEarthquakeUi();

        if (autoStartCPR)
            Invoke(nameof(StartCPR), startCPRDelay);
        else if (hideTrainingMenuUntilEarthquakeDone)
            SetTrainingCanvasVisible(true);
    }

    public void StartCPR()
    {
        AutoAssign();

        if (hidePanelWhenCPRStarts && earthquakePanel != null)
            earthquakePanel.SetActive(false);

        SetTrainingCanvasVisible(true);

        if (trainingController != null)
            trainingController.StartCPRTrainingFromEmergency();
        else
            Debug.LogWarning("[EarthquakeTrainingDirector] Missing CPRTrainingController; CPR flow was not started.");
    }

    private void AutoAssign()
    {
        if (earthquakeController == null)
            earthquakeController = FindObjectOfType<EarthquakeController>(true);

        if (trainingController == null)
            trainingController = FindObjectOfType<CPRTrainingController>(true);

        if (earthquakePanel == null && startEarthquakeButton != null)
            earthquakePanel = startEarthquakeButton.GetComponentInParent<Canvas>(true)?.gameObject;
    }

    private void WireButton()
    {
        if (startEarthquakeButton == null)
            return;

        startEarthquakeButton.onClick.RemoveListener(StartEarthquake);
        startEarthquakeButton.onClick.AddListener(StartEarthquake);
    }

    private void PlaceEarthquakeUiOnWhiteboard()
    {
        if (earthquakePanel == null)
            return;

        Quaternion rotation = Quaternion.Euler(0f, whiteboardRotationY, 0f);
        Vector3 towardRoom = rotation * Vector3.back;
        Vector3 position = whiteboardPosition + towardRoom * frontOffset;

        earthquakePanel.transform.SetParent(null, true);
        earthquakePanel.transform.position = position;
        earthquakePanel.transform.rotation = rotation;
        earthquakePanel.transform.localScale = Vector3.one * whiteboardCanvasScale;

        Canvas canvas = earthquakePanel.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;
        }
    }

    private void StyleEarthquakeUi()
    {
        if (earthquakePanel == null)
            return;

        Transform panelTransform = earthquakePanel.transform.Find("Panel");
        GameObject panelObject = panelTransform != null ? panelTransform.gameObject : earthquakePanel;
        VRTrainingUIStyle.ApplyPanel(panelObject, true);

        if (statusText != null)
        {
            RectTransform statusRect = statusText.transform as RectTransform;
            if (statusRect != null)
            {
                statusRect.anchorMin = new Vector2(0.5f, 0.5f);
                statusRect.anchorMax = new Vector2(0.5f, 0.5f);
                statusRect.pivot = new Vector2(0.5f, 0.5f);
                statusRect.anchoredPosition = new Vector2(0f, 150f);
                // 说明文案从 2 行加到 3 行（讲清楚「这是什么训练、按下去会发生什么」），
                // 按最小字号 52px × 4 行（含换行）× 1.2 行距 ≈ 250 留出余量，
                // 同时加宽到 1480 减少中途折行。面板半高 480，顶边 315 仍在板内。
                statusRect.sizeDelta = new Vector2(1480f, 330f);
                statusRect.localScale = Vector3.one;
            }

            statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statusText.fontSize = WhiteboardFontSize;
            statusText.fontStyle = FontStyle.Bold;
            statusText.alignment = TextAnchor.MiddleCenter;
            statusText.resizeTextForBestFit = true;
            // Best Fit 开启时 fontSize 被忽略，实际字号由 min/max 决定 —— 必须一起抬高
            statusText.resizeTextMinSize = WhiteboardFontMinSize;
            statusText.resizeTextMaxSize = WhiteboardFontSize;
            statusText.horizontalOverflow = HorizontalWrapMode.Wrap;
            statusText.verticalOverflow = VerticalWrapMode.Overflow;
            VRTrainingUIStyle.ApplyText(statusText, UIColorScheme.TextPrimary, true);
        }

        if (startEarthquakeButton != null)
        {
            RectTransform buttonRect = startEarthquakeButton.transform as RectTransform;
            if (buttonRect != null)
            {
                buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
                buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
                buttonRect.pivot = new Vector2(0.5f, 0.5f);
                buttonRect.anchoredPosition = new Vector2(0f, -135f);
                // 标签从「地震開始」变成「訓練開始（地震想定）」，
                // 11 字 × 90px ≈ 990，原来的 620 宽会把字压到最小号。
                buttonRect.sizeDelta = new Vector2(1060f, 160f);
                buttonRect.localScale = Vector3.one;
            }

            VRTrainingUIStyle.ApplyButton(startEarthquakeButton, UIColorScheme.Danger);

            Text label = startEarthquakeButton.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                RectTransform labelRect = label.transform as RectTransform;
                if (labelRect != null)
                {
                    labelRect.anchorMin = Vector2.zero;
                    labelRect.anchorMax = Vector2.one;
                    labelRect.offsetMin = new Vector2(18f, 8f);
                    labelRect.offsetMax = new Vector2(-18f, -8f);
                    labelRect.localScale = Vector3.one;
                }

                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.fontSize = WhiteboardFontSize;
                label.fontStyle = FontStyle.Bold;
                label.alignment = TextAnchor.MiddleCenter;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = WhiteboardFontMinSize;
                label.resizeTextMaxSize = WhiteboardFontSize;
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.verticalOverflow = VerticalWrapMode.Overflow;
                VRTrainingUIStyle.ApplyText(label, UIColorScheme.TextPrimary, true);
            }
        }
    }

    private void SetTrainingCanvasVisible(bool visible)
    {
        if (_trainingCanvasGO == null)
            _trainingCanvasGO = FindTrainingCanvas();

        if (_trainingCanvasGO != null)
            _trainingCanvasGO.SetActive(visible);
    }

    private GameObject FindTrainingCanvas()
    {
        GameObject activeCanvas = GameObject.Find("TrainingCanvas");
        if (activeCanvas != null)
            return activeCanvas;

        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        foreach (Canvas canvas in canvases)
        {
            if (canvas != null && canvas.gameObject.name == "TrainingCanvas")
                return canvas.gameObject;
        }

        if (trainingController != null && trainingController.startTrainingPanel != null)
        {
            Canvas canvas = trainingController.startTrainingPanel.GetComponentInParent<Canvas>(true);
            if (canvas != null)
                return canvas.gameObject;
        }

        return null;
    }

    private void SetButtonLabel(string label)
    {
        if (startEarthquakeButton == null)
            return;

        Text text = startEarthquakeButton.GetComponentInChildren<Text>(true);
        if (text != null)
            text.text = label;
    }

    private void SetStatus(EarthquakeStatus status)
    {
        _status = status;

        string message = status switch
        {
            EarthquakeStatus.Ready    => CPRTextLibrary.EarthquakeReady,
            EarthquakeStatus.Shaking  => CPRTextLibrary.EarthquakeShaking,
            EarthquakeStatus.Finished => CPRTextLibrary.EarthquakeFinished,
            _                         => string.Empty
        };

        if (statusText != null)
            statusText.text = message;

        Debug.Log("[EarthquakeTrainingDirector] " + message.Replace('\n', ' '));
    }
}
