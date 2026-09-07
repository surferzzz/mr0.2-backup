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
    public float frontOffset = 0.06f;

    private bool _earthquakeStarted;
    private bool _earthquakeFinished;
    private GameObject _trainingCanvasGO;

    private void Awake()
    {
        AutoAssign();
        WireButton();
    }

    private void Start()
    {
        StartCoroutine(InitializeAfterUiReady());
    }

    private IEnumerator InitializeAfterUiReady()
    {
        yield return null;

        AutoAssign();
        PlaceEarthquakeUiOnWhiteboard();

        if (earthquakePanel != null)
            earthquakePanel.SetActive(true);

        SetButtonLabel("地震開始");
        SetStatus("地震シミュレーション準備完了\nボタンを押して地震訓練を開始");
        StyleEarthquakeUi();
    }

    private void Update()
    {
        if (Input.GetKeyDown(editorTriggerKey))
            StartEarthquake();
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

        SetStatus("地震発生中\n姿勢を低く保ち、周囲の危険物に注意");
        StyleEarthquakeUi();

        earthquakeController.Play();
        CancelInvoke();
        Invoke(nameof(CompleteEarthquake), earthquakeController.duration);
    }

    public void CompleteEarthquake()
    {
        _earthquakeFinished = true;
        SetStatus("地震終了\n負傷者を発見、救命処置を開始します");

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
                statusRect.anchoredPosition = new Vector2(0f, 120f);
                statusRect.sizeDelta = new Vector2(1280f, 180f);
                statusRect.localScale = Vector3.one;
            }

            statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statusText.fontSize = 42;
            statusText.fontStyle = FontStyle.Bold;
            statusText.alignment = TextAnchor.MiddleCenter;
            statusText.resizeTextForBestFit = true;
            statusText.resizeTextMinSize = 28;
            statusText.resizeTextMaxSize = 42;
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
                buttonRect.sizeDelta = new Vector2(540f, 126f);
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
                label.fontSize = 38;
                label.fontStyle = FontStyle.Bold;
                label.alignment = TextAnchor.MiddleCenter;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 24;
                label.resizeTextMaxSize = 38;
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

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;

        Debug.Log("[EarthquakeTrainingDirector] " + message.Replace('\n', ' '));
    }

    private void OnDestroy()
    {
        CancelInvoke();

        if (startEarthquakeButton != null)
            startEarthquakeButton.onClick.RemoveListener(StartEarthquake);
    }
}
