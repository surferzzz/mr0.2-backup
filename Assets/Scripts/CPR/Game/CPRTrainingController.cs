using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;
using CPRSimulation;
using CPRSimulation.Knowledge;

namespace CPRSimulation.Game
{
    /// <summary>
    /// CPR训练流程控制器- 控制训练场景中的完整流程
    /// </summary>
    public class CPRTrainingController : MonoBehaviour
    {
        [Header("\u8bad\u7ec3\u6a21\u5f0f")]
        [SerializeField] private TrainingMode selectedTrainingMode = TrainingMode.None;
        public TrainingPhase currentPhase = TrainingPhase.Intro;

        [Header("\u60a3\u8005\u6a21\u578b")]
        public GameObject patientModel;
        public Animator patientAnimator;

        [Header("\u52a8\u753b\u53c2\u6570")]
        public string fallDownTrigger = "FallDown";
        public string lyingIdleTrigger = "LyingIdle";
        [Tooltip("\u5012\u5730\u52a8\u753b\u72b6\u6001\u540d\uff0c\u53ef\u586b DeathFromRight \u6216 Standing React Death Backward \u7b49")]
        public string fallDownStateName = "DeathFromRight";
        [Tooltip("\u5907\u9009\u5012\u5730\u72b6\u6001\u540d\uff08\u82e5\u4e3b\u72b6\u6001\u4e0d\u5b58\u5728\u5219\u4f9d\u6b21\u5c1d\u8bd5\uff09")]
        public string[] fallDownStateNameFallbacks = new[] { "Standing React Death Backward", "DeathFromLeft" };

        [Header("UI\u9762\u677f")]
        public GameObject introPanel;           // 介绍面板
        public GameObject startTrainingPanel;   // 开始训练面板
        public GameObject trainingHUDPanel;     // 训练中的HUD
        public GameObject completionPanel;      // 完成面板

        [Header("UI\u6309\u94ae")]
        public Button startScenarioButton;      // 开始情景按钮
        public Button startCPRButton;           // 开始CPR按钮
        public Button startAEDButton;
        public Button quizButton;
        public Button backTrainingButton;
        public Button restartButton;            // 重新开始按钮
        public Button exitButton;               // 退出按钮（完成面板）
        [Tooltip("\u5f00\u59cb\u8bad\u7ec3\u9762\u677f\u4e0a\u7684\u7d42\u4e86\u6309\u94ae\uff0c\u70b9\u51fb\u8fd4\u56de\u4e3b\u83dc\u5355")]
        public Button exitTrainingButton;       // 終了按钮（开始面板）
        [Tooltip("CPR \u8fdb\u884c\u4e2d HUD \u4e0a\u7684\u4e2d\u9014\u9000\u51fa\u6309\u94ae\uff0c\u70b9\u51fb\u8fd4\u56de\u5f00\u59cb\u754c\u9762")]
        public Button hudExitButton;            // HUD 中途退出按钮

        [Header("UI\u6587\u672c")]
        public TMP_Text phaseText;                  // 阶段提示文本
        public TMP_Text instructionText;            // 指导文本
        public Text timerText;                  // 计时器
        [Tooltip("\u8bad\u7ec3\u5b8c\u6210\u9762\u677f\u4e0a\u7528\u4e8e\u663e\u793a\u7ed3\u679c\u548c\u8bc4\u4ef7\u7684 Text\uff0c\u4e0d\u586b\u5219\u81ea\u52a8\u4f7f\u7528 completionPanel \u4e0b\u7684\u7b2c\u4e00\u4e2a Text")]
        public Text completionResultText;

        [Header("CPR\u7ec4\u4ef6")]
        public CPRPressDetector pressDetector;
        public GloveHapticController hapticController;

        [Header("\u97f3\u6548")]
        public AudioSource audioSource;
        public AudioClip introSound;
        public AudioClip fallSound;
        public AudioClip startCPRSound;
        public AudioClip completeSound;

        [Header("\u8bbe\u7f6e")]
        public float introDuration = 3f;
        public float fallAnimationDelay = 1f;
        public int targetCompressions = 30;
        public string mainMenuSceneName = "MainMenu";
        [Tooltip("\u8df3\u8fc7\u7ffb\u8f6c\u4ea4\u4e92\uff0c\u5012\u5730\u540e\u76f4\u63a5\u5f00\u59cb CPR\uff08\u9632\u6b62\u7ffb\u8f6c\u65f6\u6389\u5730\u4e0b\uff09")]
        public bool skipFlipInteraction = true;
        [Tooltip("\u52fe\u9009\u65f6\uff1a\u5b8c\u6210\u9762\u677f\u56fa\u5b9a\u5728\u4e16\u754c\u4f4d\u7f6e\u4e0d\u968f\u89c6\u89d2\uff1b\u6309\u538b\u65f6\u7684 HUD \u4ecd\u8ddf\u968f\u89c6\u89d2")]
        public bool completionPanelFixedPosition = true;
        [Tooltip("\u52fe\u9009\u65f6\uff1a\u6309\u4e0b\u5f00\u59cb\u8bad\u7ec3\u540e\u5ef6\u8fdf\u6307\u5b9a\u79d2\u6570\u81ea\u52a8\u4f20\u9001\u5230\u60a3\u8005\u8eab\u65c1\uff08\u65e0\u9700\u624b\u67c4\u79fb\u52a8\uff09")]
        public bool teleportAfterFall = true;
        [Tooltip("\u6309\u4e0b\u5f00\u59cb\u8bad\u7ec3\u540e\u5ef6\u8fdf\u591a\u5c11\u79d2\u4f20\u9001")]
        public float teleportDelay = 4f;
        [Tooltip("\u4f20\u9001\u5230\u60a3\u8005\u8eab\u65c1\u65f6\u7684\u8ddd\u79bb\uff08\u7c73\uff09\uff0c\u8d8a\u5c0f\u8d8a\u8fd1\uff08\u4ec5 useFixedTeleportPosition \u4e3a false \u65f6\u751f\u6548\uff09")]
        public float teleportOffsetDistance = 0.2f;
        [Tooltip("\u52fe\u9009\u65f6\uff1a\u4f20\u9001\u5230\u56fa\u5b9a\u5750\u6807\uff1b\u4e0d\u52fe\u9009\uff1a\u6309\u60a3\u8005\u4f4d\u7f6e+\u8ddd\u79bb\u8ba1\u7b97")]
        public bool useFixedTeleportPosition = true;
        [Tooltip("\u4f20\u9001\u540e\u7684\u56fa\u5b9a\u5750\u6807\uff08\u5f53 useFixedTeleportPosition \u4e3a true \u65f6\u4f7f\u7528\uff09")]
        public Vector3 teleportTargetPosition = new Vector3(-0.26f, 0.103f, 0.9f);
        [Tooltip("\u4f20\u9001\u540e\u7684\u56fa\u5b9a\u65cb\u8f6c\uff08\u6b27\u62c9\u89d2 X,Y,Z\uff09")]
        public Vector3 teleportTargetRotation = new Vector3(15.941f, 0f, 0f);

        [Header("CPR 检测范围（运行时创建检测区时使用，可在此调整）")]
        [Tooltip("检测区相对胸骨(Spine2)的局部偏移（已按标记球校准到胸口）")]
        public Vector3 detectionLocalOffset = new Vector3(-0.0218f, 0.0148f, 0.0702f);
        [Tooltip("检测区可视球体的局部缩放（仅影响外观大小）")]
        public Vector3 detectionZoneScale = new Vector3(0.2f, 0.08f, 0.2f);
        [Tooltip("触发检测的球形碰撞体半径（局部值，×0.2≈世界半径米；0.5≈世界10cm）")]
        public float detectionTriggerRadius = 0.5f;
        [Tooltip("距离备用检测阈值（米）：手掌到胸口小于此值才算贴近")]
        public float detectionPalmThreshold = 0.2f;
        [Tooltip("AABB 备用检测尺寸（米）")]
        public Vector3 detectionAreaSize = new Vector3(0.2f, 0.08f, 0.2f);
        [Tooltip("勾选后在 Scene 视图绘制检测范围 Gizmo（需选中本物体）")]
        public bool drawDetectionGizmo = true;

        public enum TrainingMode
        {
            None,
            Story,
            CPR,
            AED,
            Quiz
        }

        public enum TrainingPhase
        {
            Intro,
            WaitingStart,
            PatientFalling,
            CallingHelp,
            AEDInProgress,
            Quiz,
            ReadyForCPR,
            CPRInProgress,
            Completed
        }

        public TrainingMode SelectedTrainingMode => selectedTrainingMode;
        public bool IsAEDTrainingSelected => UsesAedFlow();
        public const string RequestedTrainingModePrefKey = "CPRTraining.RequestedTrainingMode";

        public static void SetRequestedTrainingMode(TrainingMode mode)
        {
            if (mode == TrainingMode.None)
                PlayerPrefs.DeleteKey(RequestedTrainingModePrefKey);
            else
                PlayerPrefs.SetString(RequestedTrainingModePrefKey, mode.ToString());

            PlayerPrefs.Save();
        }

        // 事件
        public static event System.Action<TrainingPhase> OnPhaseChanged;
        public static event System.Action OnPatientFell;
        public static event System.Action OnCPRStarted;
        public static event System.Action<int, int> OnCompressionProgress; // (current, target)

        private float trainingStartTime;
        private int compressionCount;
        // 按压质量统计（用于完成界面评价）
        private float _sumDepthCm;
        private float _sumRateBpm;
        private int _compressionSamples;
        private int _rateSamples;
        private int _goodDepthSamples;
        private int _goodRateSamples;
        private int _goodCombinedSamples;
        private int _tooShallowSamples;
        private int _tooDeepSamples;
        private int _tooSlowSamples;
        private int _tooFastSamples;
        private int _currentQualityStreak;
        private int _bestQualityStreak;
        private bool lockPatientPosition = false;
        private Vector3 lockedPosition;

        // 完成界面运行时构建的 UI 引用
        private bool _completionUIBuilt;
        private Text _completionTextRuntime;
        private Button _completionRestartButtonRuntime;
        private Button _completionExitButtonRuntime;
        private TrainingResultPanelView _completionViewRuntime;
        private TrainingMode _lastCompletedTrainingMode = TrainingMode.None;

        private const float MergedCprDepthBarWidth = 560f;
        private const float MergedCprDepthBarMaxCm = 8f;
        private const float MergedCprRateBarWidth = 520f;
        private const float MergedCprMinRateDisplay = 60f;
        private const float MergedCprMaxRateDisplay = 150f;
        private const float FixedCprHudHeightAboveChest = 0.82f;
        private const float FixedCprHudDistanceTowardViewer = 0.38f;
        private bool _mergedCprHudBuilt;
        private TMP_Text _mergedCprCoachTextRuntime;
        private TMP_Text _mergedCprDepthValueRuntime;
        private TMP_Text _mergedCprRateValueRuntime;
        private TMP_Text _mergedCprCountValueRuntime;
        private TMP_Text _mergedCprHandCueRuntime;
        private TMP_Text _mergedCprMetronomeRuntime;
        private TMP_Text _mergedCprQualityValueRuntime;
        private RectTransform _mergedCprDepthFillRuntime;
        private RectTransform _mergedCprRateFillRuntime;
        private Image _mergedCprDepthFillImageRuntime;
        private Image _mergedCprRateFillImageRuntime;

        private void Awake()
        {
            // 最早禁用其他冲突的控制器
            DisableOtherControllers();
        }

        private void Start()
        {
            EnsureLanguageManager();
            AutoFindAndFixPatient();
            AutoFindUI();
            CacheUIRootInitialTransform();
            EnsureUIRootActive();
            SetupButtons();
            BuildLanguageSelectButtons();   // 在开始面板上创建语言切换按钮
            SetupCPRDetector();
            EnsureHapticController();
            EnsureAudio();

            if (LanguageManager.Instance != null)
                LanguageManager.Instance.OnLanguageChanged += RefreshAllTrainingUI;

            StartCoroutine(StartIntroSequence());
        }

        /// <summary>
        /// 记录当前 UI 根节点的初始位置与旋转（用于「再来一次」时恢复）。
        /// </summary>
        private void CacheUIRootInitialTransform()
        {
            if (_uiRoot != null) return;

            GameObject anyPanel = introPanel ?? startTrainingPanel ?? trainingHUDPanel ?? completionPanel;
            if (anyPanel == null) return;

            Transform root = anyPanel.transform;
            while (root.parent != null) root = root.parent;

            _uiRoot = root;
            _uiRootInitialPosition = root.position;
            _uiRootInitialRotation = root.rotation;
        }

        /// <summary>
        /// 保存摄像头当前位置作为「初始位置」，供「再来一次」时恢复。
        /// forceOverwrite: 为true 时在传送前覆盖（更准确）；否则仅首次保存。
        /// </summary>
        private void SaveInitialCameraPositionIfNeeded(bool forceOverwrite = false)
        {
            if (!forceOverwrite && _initialCameraPositionSaved) return;
            _initialCameraPositionSaved = true;

            var cameraRig = FindObjectOfType<OVRCameraRig>();
            if (cameraRig != null)
            {
                _initialCameraRigPosition = cameraRig.transform.position;
                _initialCameraRigRotation = cameraRig.transform.rotation;
                Debug.Log($"[CPRTraining] 已记录OVRCameraRig 初始位置: {_initialCameraRigPosition}");
            }

            var mainCam = Camera.main;
            if (mainCam != null && mainCam.gameObject.name == "Main Camera")
            {
                _initialMainCamPosition = mainCam.transform.position;
                _initialMainCamRotation = mainCam.transform.rotation;
                Debug.Log($"[CPRTraining] 已记录Main Camera 初始位置: {_initialMainCamPosition}");
            }
        }

        private void RestoreCameraToInitial()
        {
            var cameraRig = FindObjectOfType<OVRCameraRig>();
            if (cameraRig != null)
            {
                var cc = cameraRig.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                cameraRig.transform.position = _initialCameraRigPosition;
                cameraRig.transform.rotation = _initialCameraRigRotation;

                if (cc != null) cc.enabled = true;
                Debug.Log($"[CPRTraining] OVRCameraRig 已恢复 {_initialCameraRigPosition}");
            }

            var mainCam = Camera.main;
            if (mainCam != null && mainCam.gameObject.name == "Main Camera")
            {
                mainCam.transform.position = _initialMainCamPosition;
                mainCam.transform.rotation = _initialMainCamRotation;
                Debug.Log($"[CPRTraining] Main Camera 已恢复 {_initialMainCamPosition}");
            }
        }

        /// <summary>
        /// 将UI 根Canvas 恢复到初始位置与旋转，并禁用结果面板的固定摆放脚本。
        /// </summary>
        private void RestoreUIRootToInitial()
        {
            if (_uiRoot == null)
                CacheUIRootInitialTransform();
            if (_uiRoot == null) return;

            EnsureUIRootActive();
            _uiRoot.position = _uiRootInitialPosition;
            _uiRoot.rotation = _uiRootInitialRotation;

            var placer = _uiRoot.GetComponent<CPRResultPanelPlacer>();
            if (placer != null)
                placer.enabled = false;
        }

        private void EnsureUIRootActive()
        {
            if (_uiRoot == null)
                CacheUIRootInitialTransform();

            if (_uiRoot != null && !_uiRoot.gameObject.activeSelf)
                _uiRoot.gameObject.SetActive(true);
        }

        private void EnsureLanguageManager()
        {
            if (LanguageManager.Instance == null)
            {
                var go = new GameObject("LanguageManager");
                go.AddComponent<LanguageManager>();
                Debug.Log("[CPRTraining] 自动创建 LanguageManager");
            }
        }

        // ─── 语言切换按钮（显示在开始训练面板上方，完成界面时隐藏避免遮挡）────────────────────────────
        private Button _langCNBtn, _langJPBtn, _langENBtn;
        private GameObject _languageSelectPanel;
        private GameObject _knowledgeQuizPanel;
        private TMP_Text _knowledgeQuizText;
        private KnowledgeQuizView _knowledgeQuizView;

        // UpdateMergedCprHud 每帧都跑（CPR 进行中）。下面这几个缓存用来跳过
        // 「数值没变就不重建字符串」—— 它们只在每次按压后变，约 2 次/秒，
        // 而 Quest 是 72-90 帧/秒。初值故意取 NaN / -1，保证首帧一定刷新。
        private float _lastQualityDepthPct = float.NaN;
        private float _lastQualityRatePct = float.NaN;
        private int _lastQualityStreak = -1;
        private bool _lastQualityHadRate;
        private int _lastCountValue = -1;
        private int _lastCountTarget = -1;
        private GameObject _storyCallHelpPanel;
        private TMP_Text _storyCallHelpText;
        private Button _storyCallHelpButton;
        private bool _storyCallHelpConfirmed;

        private void BuildLanguageSelectButtons()
        {
            Canvas canvas = null;
            if (startTrainingPanel != null)
                canvas = startTrainingPanel.GetComponentInParent<Canvas>();
            if (canvas == null)
                canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
                return;

            var container = new GameObject("LanguageSelectPanel", typeof(RectTransform));
            _languageSelectPanel = container;
            container.transform.SetParent(canvas.transform, false);
            container.transform.SetAsLastSibling();

            var panelRt = container.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 1f);
            panelRt.anchorMax = new Vector2(0.5f, 1f);
            panelRt.pivot = new Vector2(0.5f, 1f);
            panelRt.anchoredPosition = new Vector2(0f, -54f);
            panelRt.sizeDelta = new Vector2(780f, 74f);

            var bg = container.AddComponent<UnityEngine.UI.Image>();
            bg.color = new Color(0.025f, 0.055f, 0.070f, 0.34f);
            VRTrainingUIStyle.ApplyPanel(container, false);

            var langs = new[] { Language.Chinese, Language.Japanese, Language.English };
            var labels = new[] { "\u4e2d\u6587", "\u65e5\u672c\u8a9e", "English" };
            var colors = new[]
            {
                new Color(0.8f, 0.15f, 0.15f, 0.85f),
                new Color(0.1f, 0.35f, 0.85f, 0.85f),
                new Color(0.1f, 0.5f, 0.2f, 0.85f)
            };
            var xPos = new[] { -255f, 0f, 255f };

            for (int i = 0; i < 3; i++)
            {
                var btnGo = new GameObject(
                    $"LangBtn_{langs[i]}",
                    typeof(RectTransform),
                    typeof(UnityEngine.UI.Image),
                    typeof(UnityEngine.UI.Button)
                );
                btnGo.transform.SetParent(container.transform, false);

                var rt = btnGo.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(210f, 56f);
                rt.anchoredPosition = new Vector2(xPos[i], 0f);

                var img = btnGo.GetComponent<UnityEngine.UI.Image>();
                img.color = colors[i];

                var btn = btnGo.GetComponent<UnityEngine.UI.Button>();
                btn.targetGraphic = img;
                var bc = btn.colors;
                bc.highlightedColor = new Color(0.2f, 0.9f, 1f, 1f);
                bc.pressedColor = new Color(0.05f, 0.5f, 1f, 1f);
                bc.selectedColor = bc.highlightedColor;
                btn.colors = bc;

                var outline = btnGo.AddComponent<UnityEngine.UI.Outline>();
                outline.effectColor = new Color(1f, 1f, 1f, 0.12f);
                outline.effectDistance = new Vector2(1f, -1f);

                var labelGo = new GameObject("Label", typeof(RectTransform), typeof(UnityEngine.UI.Text));
                labelGo.transform.SetParent(btnGo.transform, false);

                var lrt = labelGo.GetComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = Vector2.zero;
                lrt.offsetMax = Vector2.zero;

                var ltxt = labelGo.GetComponent<UnityEngine.UI.Text>();
                ltxt.text = labels[i];
                ltxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                ltxt.fontSize = 30;
                ltxt.resizeTextForBestFit = true;
                ltxt.resizeTextMinSize = 18;
                ltxt.resizeTextMaxSize = 30;
                ltxt.alignment = TextAnchor.MiddleCenter;
                ltxt.color = Color.white;

                VRTrainingUIStyle.ApplyButton(btn, colors[i]);
                VRTrainingUIStyle.ApplyText(ltxt, UIColorScheme.TextPrimary);

                Language captured = langs[i];
                btn.onClick.AddListener(() =>
                {
                    EnsureLanguageManager();
                    if (LanguageManager.Instance != null)
                        LanguageManager.Instance.SetLanguage(captured);
                    UpdateLangButtonHighlight();
                    RefreshAllTrainingUI();
                });

                if (langs[i] == Language.Chinese) _langCNBtn = btn;
                if (langs[i] == Language.Japanese) _langJPBtn = btn;
                if (langs[i] == Language.English) _langENBtn = btn;
            }

            UpdateLangButtonHighlight();
            Debug.Log("[CPRTraining] Language buttons created");
        }

        private void UpdateLangButtonHighlight()
        {
            if (LanguageManager.Instance == null) return;
            var cur = LanguageManager.Instance.CurrentLanguage;
            HighlightLangBtn(_langCNBtn, cur == Language.Chinese);
            HighlightLangBtn(_langJPBtn, cur == Language.Japanese);
            HighlightLangBtn(_langENBtn, cur == Language.English);
        }

        private void HighlightLangBtn(UnityEngine.UI.Button btn, bool active)
        {
            if (btn == null) return;
            var img = btn.GetComponent<UnityEngine.UI.Image>();
            if (img == null) return;
            var c = img.color;
            img.color = active
                ? new Color(c.r, c.g, c.b, 1f)
                : new Color(c.r * 0.55f, c.g * 0.55f, c.b * 0.55f, 0.65f);
            var ol = btn.GetComponent<UnityEngine.UI.Outline>();
            if (ol != null)
                ol.effectColor = active ? new Color(1f,1f,0.2f,0.75f) : new Color(1f,1f,1f,0.10f);
        }

        /// <summary>语言切换时刷新整个训练界面文字（阶段、提示、按钮等）/summary>
        /// <summary>
        /// 让 HUD 文字缓存失效，下一帧强制重建。
        /// 语言切换、重开训练等场景都需要，因为缓存键只看数值。
        /// </summary>
        private void InvalidateMergedHudTextCache()
        {
            _lastQualityDepthPct = float.NaN;
            _lastQualityRatePct = float.NaN;
            _lastQualityStreak = -1;
            _lastCountValue = -1;
            _lastCountTarget = -1;
        }

        private void RefreshAllTrainingUI()
        {
            // 本方法是 OnLanguageChanged 的处理器。HUD 里「达标」和「进度」两行
            // 用了跳过重建的缓存，而它们的缓存键只看数值 —— 切语言时数值没变，
            // 文字就不会重建，会停在旧语言。所以任何一次显式刷新都先让缓存失效。
            InvalidateMergedHudTextCache();

            // 1. 阶段名称
            if (phaseText != null)
                phaseText.text = GetPhaseDisplayName(currentPhase);

            // 2. 指导提示（根据当前阶段显示对应文案）
            if (instructionText != null)
                instructionText.text = GetInstructionForCurrentPhase();

            // 3. 开始训练面板上的按钮
            ApplyTrainingModeCard(startScenarioButton, TrainingMode.Story);
            ApplyTrainingModeCard(startCPRButton, TrainingMode.CPR);
            ApplyTrainingModeCard(startAEDButton, TrainingMode.AED);
            ApplyTrainingModeCard(quizButton, TrainingMode.Quiz);
            SetButtonLabel(backTrainingButton, GetBackButtonLabel());
            SetButtonLabel(exitTrainingButton, GetExitButtonLabel());
            SetButtonLabel(hudExitButton, CPRTextLibrary.BtnExit);
            if (_knowledgeQuizText != null)
                _knowledgeQuizText.text = GetQuizInstruction();
            if (_storyCallHelpText != null)
                _storyCallHelpText.text = GetCallingHelpInstruction();

            // 4. 开始面板内的「准备就绪」类状态文字（非按钮内的Text）
            if (startTrainingPanel != null && currentPhase == TrainingPhase.WaitingStart)
            {
                bool found = false;
                var texts = startTrainingPanel.GetComponentsInChildren<Text>(true);
                foreach (var t in texts)
                {
                    if (t.GetComponentInParent<Button>() != null) continue;
                    string n = t.gameObject.name.ToLower();
                    if (n.Contains("title") || n.Contains("status") || n.Contains("ready") || n.Contains("phase")
                        || n.Contains("label") || n.Contains("main") || n.Contains("header"))
                    {
                        t.text = GetTrainingModeSelectionTitleV2();
                        found = true;
                        break;
                    }
                }
                if (!found && texts.Length > 0)
                {
                    foreach (var t in texts)
                    {
                        if (t.GetComponentInParent<Button>() != null) continue;
                            t.text = GetTrainingModeSelectionTitleV2();
                        break;  // 只改第一个匹配的
                    }
                }
            }

            // 5. 介绍面板的欢迎语
            if (introPanel != null && currentPhase == TrainingPhase.Intro)
            {
                var texts = introPanel.GetComponentsInChildren<Text>(true);
                foreach (var t in texts)
                {
                    if (t.GetComponentInParent<Button>() != null) continue;
                    string n = t.gameObject.name.ToLower();
                    if (n.Contains("title") || n.Contains("welcome") || n.Contains("intro"))
                    {
                        t.text = CPRTextLibrary.Welcome;
                        break;
                    }
                }
            }

            Debug.Log("[CPRTraining] UI 文字已按当前语言刷新");
        }

        private void ApplyTrainingModeCard(Button button, TrainingMode mode)
        {
            if (button == null)
                return;

            var cardView = button.GetComponent<TrainingModeCardView>();
            if (cardView == null)
                cardView = button.gameObject.AddComponent<TrainingModeCardView>();
            cardView.Apply(TrainingModeCatalog.Get(mode));
        }

        private string GetInstructionForCurrentPhase()
        {
            return currentPhase switch
            {
                TrainingPhase.Intro          => CPRTextLibrary.Welcome,
                TrainingPhase.WaitingStart   => GetTrainingModeSelectionInstructionV2(),
                TrainingPhase.PatientFalling => CPRTextLibrary.PatientFell,
                TrainingPhase.CallingHelp    => GetCallingHelpInstruction(),
                TrainingPhase.AEDInProgress  => ResolveSelectedTrainingMode() == TrainingMode.Story ? GetStoryAEDPhaseInstruction() : GetAEDPhaseInstruction(),
                TrainingPhase.Quiz           => GetQuizInstruction(),
                TrainingPhase.ReadyForCPR    => CPRTextLibrary.FlipPatient,
                TrainingPhase.CPRInProgress  => CPRTextLibrary.StartCPRNow,
                TrainingPhase.Completed      => CPRTextLibrary.CompletionTitle,
                _                            => ""
            };
        }





        private string GetTrainingModeSelectionTitleV2()
        {
            return TrainingModeCatalog.SelectionTitle;
        }

        private string GetTrainingModeSelectionInstructionV2()
        {
            return LanguageManager.Get(
                "\u9009\u62e9\u4e00\u4e2a\u6a21\u5f0f\u5f00\u59cb\uff1a\u5267\u60c5\u6025\u6551\u3001CPR\u4e13\u9879\u3001AED\u4e13\u9879\u6216\u77e5\u8bc6\u6d4b\u9a8c\u3002",
                "\u30b7\u30ca\u30ea\u30aa\u3001CPR\u3001AED\u3001\u77e5\u8b58\u30c6\u30b9\u30c8\u304b\u3089\u9078\u3093\u3067\u958b\u59cb\u3057\u3066\u304f\u3060\u3055\u3044\u3002",
                "Choose Story, CPR, AED, or Knowledge Quiz to begin."
            );
        }

        private string GetStoryTrainingButtonLabel()
        {
            return TrainingModeCatalog.GetTitle(TrainingMode.Story);
        }






        private string GetCPRTrainingButtonLabelV2()
        {
            return TrainingModeCatalog.GetTitle(TrainingMode.CPR);
        }

        private string GetAEDTrainingButtonLabelV2()
        {
            return TrainingModeCatalog.GetTitle(TrainingMode.AED);
        }

        private string GetQuizButtonLabel()
        {
            return TrainingModeCatalog.GetTitle(TrainingMode.Quiz);
        }

        private string GetBackButtonLabel()
        {
            return LanguageManager.Get(
                "\u8fd4\u56de",
                "\u623b\u308b",
                "Back"
            );
        }

        private string GetExitButtonLabel()
        {
            return LanguageManager.Get(
                "\u9000\u51fa",
                "\u7d42\u4e86",
                "Exit"
            );
        }

        private string GetQuizInstruction()
        {
            return LanguageManager.Get(
                "\u77e5\u8bc6\u6d4b\u9a8c\u6a21\u5757\u5df2\u63a5\u5165\u4e3b\u83dc\u5355\u3002\u540e\u7eed\u53ef\u5728\u8fd9\u91cc\u653e\u5165 CPR/AED \u9009\u62e9\u9898\u548c\u7ed3\u679c\u8bc4\u5206\u3002",
                "\u77e5\u8b58\u30c6\u30b9\u30c8\u3092\u30e1\u30a4\u30f3\u30e1\u30cb\u30e5\u30fc\u306b\u63a5\u7d9a\u3057\u307e\u3057\u305f\u3002\u5f8c\u3067 CPR/AED \u306e\u554f\u984c\u3068\u63a1\u70b9\u3092\u8ffd\u52a0\u3067\u304d\u307e\u3059\u3002",
                "Knowledge Quiz is now wired into the main menu. CPR/AED questions and scoring can be added here next."
            );
        }

        private string GetAEDPhaseInstruction()
        {
            return LanguageManager.Get(
                "请先打开 AED，并按提示贴放电极片后完成电击操作",
                "AED の電源を入れ、案内に従ってパッドを貼り、必要なら電気ショックを実行してください",
                "Power on the AED, place both pads, and deliver a shock if advised"
            );
        }

        private string GetStoryAEDPhaseInstruction()
        {
            return LanguageManager.Get(
                "\u5267\u60c5\u76ee\u6807\uff1a\u53d1\u73b0\u60a3\u8005\u5df2\u5012\u5730\uff0c\u5148\u547c\u6551\u5e76\u53d6\u6765 AED\u3002\u6309 AED \u63d0\u793a\u5b8c\u6210\u8d34\u7247\u548c\u7535\u51fb\u5224\u65ad\uff0c\u968f\u540e\u7acb\u5373\u8fdb\u5165 CPR\u3002",
                "\u30b7\u30ca\u30ea\u30aa\u76ee\u6a19\uff1a\u5012\u308c\u305f\u60a3\u8005\u3092\u767a\u898b\u3057\u305f\u3089\u3001\u52a9\u3051\u3092\u547c\u3073 AED \u3092\u6e96\u5099\u3057\u307e\u3059\u3002AED \u306e\u6307\u793a\u306b\u5f93\u3063\u3066\u30d1\u30c3\u30c9\u8cbc\u4ed8\u3068\u30b7\u30e7\u30c3\u30af\u5224\u65ad\u3092\u5b8c\u4e86\u3057\u3001\u3059\u3050 CPR \u3092\u958b\u59cb\u3057\u3066\u304f\u3060\u3055\u3044\u3002",
                "Story objective: patient collapsed. Call for help, get the AED, follow pad and shock prompts, then start CPR immediately."
            );
        }

        private string GetCallingHelpPhaseDisplayName()
        {
            return LanguageManager.Get(
                "\u547c\u6551\u4e0e\u53d6 AED",
                "\u52a9\u3051\u3092\u547c\u3073 AED \u3092\u6e96\u5099",
                "Call For Help"
            );
        }

        private string GetCallingHelpInstruction()
        {
            return LanguageManager.Get(
                "\u5267\u60c5\u6b65\u9aa4\uff1a\u786e\u8ba4\u73b0\u573a\u5b89\u5168\uff0c\u547c\u53eb\u5468\u56f4\u4eba\u5e2e\u52a9\u5e76\u8bf7\u4eba\u53d6 AED\u3002",
                "\u30b7\u30ca\u30ea\u30aa\u624b\u9806\uff1a\u73fe\u5834\u306e\u5b89\u5168\u3092\u78ba\u8a8d\u3057\u3001\u5468\u56f2\u306b\u52a9\u3051\u3092\u6c42\u3081\u3001AED \u3092\u6301\u3063\u3066\u304d\u3066\u3082\u3089\u3044\u307e\u3059\u3002",
                "Story step: confirm scene safety, call for help, and ask someone to bring the AED."
            );
        }

        private string GetCallHelpConfirmButtonLabel()
        {
            return LanguageManager.Get(
                "\u5df2\u547c\u6551\uff0c\u53d6 AED",
                "\u52a9\u3051\u3092\u547c\u3073 AED \u6e96\u5099",
                "Help Called, Get AED"
            );
        }

        private string GetAEDPhaseDisplayName()
        {
            return LanguageManager.Get(
                "AED急救中",
                "AED処置中",
                "AED In Progress"
            );
        }

        private void EnsureTrainingModeButtonsVisible()
        {
            EnsureTrainingModeButtonReferences();

            if (startScenarioButton != null)
                startScenarioButton.gameObject.SetActive(true);

            if (startCPRButton != null)
                startCPRButton.gameObject.SetActive(true);

            if (startAEDButton != null)
                startAEDButton.gameObject.SetActive(true);

            if (quizButton != null)
                quizButton.gameObject.SetActive(true);

            if (backTrainingButton != null)
                backTrainingButton.gameObject.SetActive(true);

            if (exitTrainingButton != null)
                exitTrainingButton.gameObject.SetActive(true);
        }

        private void EnsureTrainingModeButtonReferences()
        {
            Transform parent = null;
            if (startTrainingPanel != null)
                parent = startTrainingPanel.transform;
            if (parent == null && startScenarioButton != null)
                parent = startScenarioButton.transform.parent;
            if (parent == null && startCPRButton != null)
                parent = startCPRButton.transform.parent;
            if (parent == null && startAEDButton != null)
                parent = startAEDButton.transform.parent;
            if (parent == null && quizButton != null)
                parent = quizButton.transform.parent;
            if (parent == null && backTrainingButton != null)
                parent = backTrainingButton.transform.parent;
            if (parent == null && exitTrainingButton != null)
                parent = exitTrainingButton.transform.parent;
            if (parent == null)
                return;

            if (startScenarioButton == null)
                startScenarioButton = CreateTrainingModeButton("StartScenarioButton", parent, TrainingModeCatalog.Get(TrainingMode.Story).AccentColor, OnStartScenarioClicked);

            if (startCPRButton == null || startCPRButton == startScenarioButton)
                startCPRButton = CreateTrainingModeButton("CPRTrainingButton", parent, TrainingModeCatalog.Get(TrainingMode.CPR).AccentColor, OnStartCPRClicked);

            if (startAEDButton == null || startAEDButton == startScenarioButton || startAEDButton == startCPRButton)
                startAEDButton = CreateTrainingModeButton("AEDTrainingButton", parent, TrainingModeCatalog.Get(TrainingMode.AED).AccentColor, OnStartAEDClicked);

            if (quizButton == null)
                quizButton = CreateTrainingModeButton("KnowledgeQuizButton", parent, TrainingModeCatalog.Get(TrainingMode.Quiz).AccentColor, OnStartQuizClicked);

            if (backTrainingButton == null)
                backTrainingButton = CreateTrainingModeButton("BackTrainingButton", parent, UIColorScheme.PrimaryDark, OnBackTrainingClicked);

            if (exitTrainingButton == null)
                exitTrainingButton = CreateTrainingModeButton("ExitTrainingButton", parent, UIColorScheme.PrimaryDark, ExitToMainMenu);
        }

        private Button CreateTrainingModeButton(string name, Transform parent, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            var image = buttonObject.GetComponent<Image>();
            image.color = color;

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(buttonObject.transform, false);

            var labelTransform = labelObject.GetComponent<RectTransform>();
            labelTransform.anchorMin = Vector2.zero;
            labelTransform.anchorMax = Vector2.one;
            labelTransform.offsetMin = Vector2.zero;
            labelTransform.offsetMax = Vector2.zero;

            var label = labelObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 28;
            label.alignment = TextAnchor.MiddleCenter;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 16;
            label.resizeTextMaxSize = 30;
            label.color = Color.white;

            VRTrainingUIStyle.ApplyText(label, UIColorScheme.TextPrimary);
            VRTrainingUIStyle.ApplyButton(button, color);
            buttonObject.AddComponent<TrainingModeCardView>();
            return button;
        }

        private void PrepareTrainingSelectionUI()
        {
            selectedTrainingMode = TrainingMode.None;
            if (currentPhase != TrainingPhase.WaitingStart)
                ChangePhase(TrainingPhase.WaitingStart);
            EnsureUIRootActive();
            RestoreUIRootToInitial();
            if (startTrainingPanel != null)
                startTrainingPanel.SetActive(true);
            HideKnowledgeQuizPanel();
            HideStoryCallHelpPanel();
            EnsureTrainingModeButtonsVisible();
            AdjustStartMenuLayoutAndStyle();
            RefreshAllTrainingUI();
            HideStartReadyLabels();
        }

        private void SetTrainingModeButtonsActive(bool active)
        {
            if (startScenarioButton != null)
                startScenarioButton.gameObject.SetActive(active);

            if (startCPRButton != null)
                startCPRButton.gameObject.SetActive(active);

            if (startAEDButton != null)
                startAEDButton.gameObject.SetActive(active);

            if (quizButton != null)
                quizButton.gameObject.SetActive(active);
        }

        private void HideKnowledgeQuizPanel()
        {
            if (_knowledgeQuizPanel != null)
                _knowledgeQuizPanel.SetActive(false);
        }

        private void ShowKnowledgeQuizPanel()
        {
            selectedTrainingMode = TrainingMode.Quiz;
            ChangePhase(TrainingPhase.Quiz);
            EnsureUIRootActive();
            RestoreUIRootToInitial();
            ShowPanel(startTrainingPanel);
            EnsureTrainingModeButtonReferences();
            AdjustStartMenuLayoutAndStyle();
            SetTrainingModeButtonsActive(false);

            if (backTrainingButton != null)
                backTrainingButton.gameObject.SetActive(true);
            if (exitTrainingButton != null)
                exitTrainingButton.gameObject.SetActive(true);

            EnsureKnowledgeQuizPanel();
            if (_knowledgeQuizPanel != null)
                _knowledgeQuizPanel.SetActive(true);

            StartKnowledgeQuiz();

            RefreshAllTrainingUI();
            HideStartReadyLabels();
        }

        private void EnsureKnowledgeQuizPanel()
        {
            if (startTrainingPanel == null)
                return;

            if (_knowledgeQuizPanel == null)
            {
                _knowledgeQuizPanel = FindChildGameObject(startTrainingPanel.transform, "KnowledgeQuizPanel");
                if (_knowledgeQuizPanel == null)
                {
                    _knowledgeQuizPanel = new GameObject("KnowledgeQuizPanel", typeof(RectTransform), typeof(Image), typeof(Outline));
                    _knowledgeQuizPanel.transform.SetParent(startTrainingPanel.transform, false);
                }
            }

            // 原来是 920x240，只够放一行占位文字。做题要放题干+选项+解析+按钮，
            // 尺寸由 KnowledgeQuizView 的布局常量给出，避免两处各写一个数字。
            RectTransform panelRect = _knowledgeQuizPanel.GetComponent<RectTransform>();
            SetRuntimeRect(panelRect, new Vector2(0f, -30f),
                           new Vector2(KnowledgeQuizView.PanelWidth, KnowledgeQuizView.PanelHeight));

            Image image = _knowledgeQuizPanel.GetComponent<Image>();
            if (image != null)
                image.color = new Color(0.02f, 0.04f, 0.05f, 0.72f);

            Outline outline = _knowledgeQuizPanel.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = new Color(0.2f, 0.85f, 1f, 0.18f);
                outline.effectDistance = new Vector2(2f, -2f);
            }

            VRTrainingUIStyle.ApplyPanel(_knowledgeQuizPanel, true);

            // 这里原来手写了一遍「找子物体 / 建 GameObject / GetComponent / AddComponent」，
            // 和 EnsureRuntimeText 完全重复。直接调它，顺带一起迁到 TMP。
            _knowledgeQuizText = EnsureRuntimeText(
                _knowledgeQuizPanel.transform,
                "KnowledgeQuizText",
                Vector2.zero,
                new Vector2(820f, 150f),
                30,
                TextAnchor.MiddleCenter,
                UIColorScheme.TextPrimary
            );
        }

        /// <summary>
        /// 开一轮知识测验。题库缺失或校验不通过时退回一行说明文字，
        /// 而不是留一个空面板让人以为是卡住了。
        /// </summary>
        private void StartKnowledgeQuiz()
        {
            if (_knowledgeQuizPanel == null)
                return;

            if (_knowledgeQuizView == null)
            {
                _knowledgeQuizView = _knowledgeQuizPanel.GetComponent<KnowledgeQuizView>();
                if (_knowledgeQuizView == null)
                    _knowledgeQuizView = _knowledgeQuizPanel.AddComponent<KnowledgeQuizView>();
            }

            QuizBank bank = QuizBankLoader.Load();
            if (bank == null)
            {
                _knowledgeQuizView.SetUIVisible(false);
                ShowKnowledgeQuizMessage(GetQuizUnavailableText());
                return;
            }

            // 题目 UI 起来了就把说明文字让开，两者都是面板的直接子物体
            if (_knowledgeQuizText != null)
                _knowledgeQuizText.gameObject.SetActive(false);

            if (!_knowledgeQuizView.Begin(bank, OnKnowledgeQuizFinished))
                ShowKnowledgeQuizMessage(GetQuizUnavailableText());
        }

        private void OnKnowledgeQuizFinished(QuizResult result)
        {
            string text = _knowledgeQuizView != null
                ? _knowledgeQuizView.BuildResultText(result)
                : $"{result.correct}/{result.total}";

            ShowKnowledgeQuizMessage(text);
        }

        private void ShowKnowledgeQuizMessage(string message)
        {
            if (_knowledgeQuizText == null)
                return;

            _knowledgeQuizText.gameObject.SetActive(true);
            _knowledgeQuizText.text = message;
        }

        private string GetQuizUnavailableText()
        {
            return LanguageManager.Get(
                "题库未能载入。请检查 Resources/VRTrainingUI/Quiz/questions.json，Console 里有具体原因。",
                "問題集を読み込めませんでした。Resources/VRTrainingUI/Quiz/questions.json を確認してください。詳細は Console に出ています。",
                "Could not load the question bank. Check Resources/VRTrainingUI/Quiz/questions.json; the Console has details.");
        }

        private IEnumerator WaitForStoryCallHelpConfirmation()
        {
            _storyCallHelpConfirmed = false;
            ChangePhase(TrainingPhase.CallingHelp);
            ShowStoryCallHelpPanel();

            while (!_storyCallHelpConfirmed)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                    _storyCallHelpConfirmed = true;

                yield return null;
            }

            HideStoryCallHelpPanel();
        }

        private void ShowStoryCallHelpPanel()
        {
            EnsureUIRootActive();
            RestoreUIRootToInitial();
            ShowPanel(startTrainingPanel);
            EnsureTrainingModeButtonReferences();
            AdjustStartMenuLayoutAndStyle();
            SetTrainingModeButtonsActive(false);

            if (backTrainingButton != null)
                backTrainingButton.gameObject.SetActive(true);
            if (exitTrainingButton != null)
                exitTrainingButton.gameObject.SetActive(true);

            EnsureStoryCallHelpPanel();
            if (_storyCallHelpPanel != null)
                _storyCallHelpPanel.SetActive(true);
            if (_storyCallHelpText != null)
                _storyCallHelpText.text = GetCallingHelpInstruction();

            RefreshAllTrainingUI();
            HideStartReadyLabels();
        }

        private void HideStoryCallHelpPanel()
        {
            if (_storyCallHelpPanel != null)
                _storyCallHelpPanel.SetActive(false);
        }

        private void OnStoryCallHelpConfirmed()
        {
            _storyCallHelpConfirmed = true;
        }

        private void EnsureStoryCallHelpPanel()
        {
            if (startTrainingPanel == null)
                return;

            if (_storyCallHelpPanel == null)
            {
                _storyCallHelpPanel = FindChildGameObject(startTrainingPanel.transform, "StoryCallHelpPanel");
                if (_storyCallHelpPanel == null)
                {
                    _storyCallHelpPanel = new GameObject("StoryCallHelpPanel", typeof(RectTransform), typeof(Image), typeof(Outline));
                    _storyCallHelpPanel.transform.SetParent(startTrainingPanel.transform, false);
                }
            }

            RectTransform panelRect = _storyCallHelpPanel.GetComponent<RectTransform>();
            SetRuntimeRect(panelRect, new Vector2(0f, -20f), new Vector2(920f, 270f));

            Image image = _storyCallHelpPanel.GetComponent<Image>();
            if (image != null)
                image.color = new Color(0.02f, 0.04f, 0.05f, 0.74f);

            Outline outline = _storyCallHelpPanel.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = new Color(1f, 0.78f, 0.18f, 0.24f);
                outline.effectDistance = new Vector2(2f, -2f);
            }

            VRTrainingUIStyle.ApplyPanel(_storyCallHelpPanel, true);

            _storyCallHelpText = EnsureRuntimeText(
                _storyCallHelpPanel.transform,
                "StoryCallHelpText",
                new Vector2(0f, 45f),
                new Vector2(820f, 128f),
                30,
                TextAnchor.MiddleCenter,
                UIColorScheme.TextPrimary
            );

            if (_storyCallHelpButton == null)
            {
                Button existing = null;
                Button[] buttons = _storyCallHelpPanel.GetComponentsInChildren<Button>(true);
                foreach (Button button in buttons)
                {
                    if (button != null && button.gameObject.name == "StoryCallHelpButton")
                    {
                        existing = button;
                        break;
                    }
                }

                _storyCallHelpButton = existing ?? CreateTrainingModeButton("StoryCallHelpButton", _storyCallHelpPanel.transform, UIColorScheme.Warning, OnStoryCallHelpConfirmed);
            }

            _storyCallHelpButton.onClick.RemoveAllListeners();
            _storyCallHelpButton.onClick.AddListener(OnStoryCallHelpConfirmed);
            SetButtonLabel(_storyCallHelpButton, GetCallHelpConfirmButtonLabel());
            LayoutStartMenuButton(_storyCallHelpButton, new Vector2(0f, -85f), new Vector2(420f, 76f), 28);
        }

        private void HideStartReadyLabels()
        {
            if (startTrainingPanel == null)
            {
                return;
            }

            var texts = startTrainingPanel.GetComponentsInChildren<Text>(true);
            foreach (var text in texts)
            {
                if (text == null || text.GetComponentInParent<Button>() != null)
                {
                    continue;
                }

                string value = text.text ?? string.Empty;
                string name = text.gameObject.name.ToLowerInvariant();
                if (name.Contains("starthint") ||
                    name.Contains("ready") ||
                    value.Contains("准备就绪") ||
                    value.Contains("準備就緒") ||
                    value.Contains("準備完了") ||
                    value.Equals("Ready", System.StringComparison.OrdinalIgnoreCase))
                {
                    text.text = string.Empty;
                    text.gameObject.SetActive(false);
                }
            }
        }

        private TrainingMode ResolveSelectedTrainingMode()
        {
            return selectedTrainingMode == TrainingMode.None
                ? TrainingMode.CPR
                : selectedTrainingMode;
        }

        private bool TryConsumeRequestedTrainingMode(out TrainingMode mode)
        {
            mode = TrainingMode.None;

            if (!PlayerPrefs.HasKey(RequestedTrainingModePrefKey))
                return false;

            string value = PlayerPrefs.GetString(RequestedTrainingModePrefKey, string.Empty);
            PlayerPrefs.DeleteKey(RequestedTrainingModePrefKey);
            PlayerPrefs.Save();

            if (value == TrainingMode.Story.ToString())
                mode = TrainingMode.Story;
            else if (value == TrainingMode.CPR.ToString())
                mode = TrainingMode.CPR;
            else if (value == TrainingMode.AED.ToString())
                mode = TrainingMode.AED;
            else if (value == TrainingMode.Quiz.ToString())
                mode = TrainingMode.Quiz;

            return mode != TrainingMode.None;
        }

        private bool UsesCprFlow()
        {
            TrainingMode mode = ResolveSelectedTrainingMode();
            return mode == TrainingMode.Story || mode == TrainingMode.CPR;
        }

        private bool UsesAedFlow()
        {
            TrainingMode mode = ResolveSelectedTrainingMode();
            return mode == TrainingMode.Story || mode == TrainingMode.AED;
        }

        /// <summary>设置按钮的子 Text 文字，若无则创建</summary>
        private void SetButtonLabel(Button btn, string label)
        {
            if (btn == null) return;
            var txt = btn.GetComponentInChildren<Text>(true);
            if (txt == null)
            {
                var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
                labelGo.transform.SetParent(btn.transform, false);
                var lrt = labelGo.GetComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
                lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
                txt = labelGo.GetComponent<Text>();
                txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.fontSize  = 28;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color     = Color.white;
            }
            txt.supportRichText = true;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.text = label;
        }

        private void EnsureHapticController()
        {
            if (hapticController == null)
                hapticController = FindObjectOfType<GloveHapticController>();
            if (hapticController == null)
            {
                var go = new GameObject("AutoGloveHapticController");
                hapticController = go.AddComponent<GloveHapticController>();
                Debug.Log("[CPRTraining] 场景中未找到 GloveHapticController，已自动创建");
            }
            Debug.Log($"[CPRTraining] hapticController={hapticController != null} ({(hapticController != null ? hapticController.gameObject.name : "null")})");
        }

        private CPRMetronome _metronome;

        /// <summary>
        /// 项目里 Assets/Audio 是空的，audioSource 和 4 个 AudioClip 在场景中也都没赋值，
        /// 于是 PlaySound() 一直是空操作 —— 训练全程无声。
        /// 这里补上 AudioSource 并为未指定的音效生成合成音；
        /// 以后若在 Inspector 里挂上真实素材，会自动优先使用素材。
        /// </summary>
        private void EnsureAudio()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;   // 提示音是 2D 的，不随头部朝向衰减

            if (introSound == null)
                introSound = ProceduralAudio.CreateSequence("CPR_Intro", new[] { 523.25f, 659.25f });
            if (fallSound == null)
                fallSound = ProceduralAudio.CreateClick("CPR_Fall", 180f, 0.35f, 9f);
            if (startCPRSound == null)
                startCPRSound = ProceduralAudio.CreateSequence("CPR_Start", new[] { 659.25f, 880f });
            if (completeSound == null)
                completeSound = ProceduralAudio.CreateSequence(
                    "CPR_Complete", new[] { 523.25f, 659.25f, 783.99f, 1046.5f });

            _metronome = FindObjectOfType<CPRMetronome>();
            if (_metronome == null)
            {
                var go = new GameObject("CPRMetronome");
                go.transform.SetParent(transform, false);
                _metronome = go.AddComponent<CPRMetronome>();
            }

            // 节拍取合格区间中值，保证「听到的节奏」和「评分标准」是同一个。
            float minRate = pressDetector != null ? pressDetector.minGoodRateBPM : 100f;
            float maxRate = pressDetector != null ? pressDetector.maxGoodRateBPM : 120f;
            _metronome.bpm = Mathf.Clamp((minRate + maxRate) * 0.5f, 60f, 140f);

            Debug.Log($"[CPRTraining] 音频就绪：节拍器 {_metronome.bpm:F0} BPM");
        }

        private float _diagTimer;
        private float _lastRealtimeHapticDepth;

        private void Update()
        {
            // 每帧强制锁定患者位置（防止掉地下）
            if (lockPatientPosition && patientModel != null)
            {
                Vector3 pos = patientModel.transform.position;
                if (Mathf.Abs(pos.y - lockedPosition.y) > 0.01f || pos.y < 0f)
                {
                    patientModel.transform.position = lockedPosition;
                }
            }

            // === 每3 秒打印一次诊断信息===
            _diagTimer += Time.deltaTime;
            if (_diagTimer >= 3f)
            {
                _diagTimer = 0f;
                bool hasDetector = pressDetector != null;
                bool hasHaptic = hapticController != null;
                bool detectorInit = hasDetector && pressDetector.IsInitialized;
                float depth = hasDetector ? pressDetector.CurrentDepthCm : -1f;
                bool handIn = hasDetector && pressDetector.IsHandInContact;
                Debug.Log($"[CPR诊断] phase={currentPhase} detector={hasDetector}(init={detectorInit}) haptic={hasHaptic} depth={depth:F1}cm hand={handIn} count={compressionCount}");

                // 自动修复：如果CPR 阶段但缺少组件
                if (currentPhase == TrainingPhase.CPRInProgress)
                {
                    if (!hasHaptic)
                    {
                        hapticController = FindObjectOfType<GloveHapticController>();
                        if (hapticController == null)
                        {
                            var go = new GameObject("AutoGloveHapticController");
                            hapticController = go.AddComponent<GloveHapticController>();
                            Debug.Log("[CPR诊断] 自动创建了GloveHapticController");
                        }
                    }
                    if (!hasDetector)
                    {
                        pressDetector = FindObjectOfType<CPRPressDetector>();
                        if (pressDetector != null)
                        {
                            pressDetector.OnCompressionComplete -= OnCompressionDetected;
                            pressDetector.OnCompressionComplete += OnCompressionDetected;
                            Debug.Log("[CPR诊断] 自动找到并绑定了 CPRPressDetector");
                        }
                    }
                }
            }

            // CPR 阶段：实时更新按压深度与频率显示
            if (currentPhase == TrainingPhase.CPRInProgress && pressDetector != null)
            {
                float depth = pressDetector.CurrentDepthCm;
                float rate = pressDetector.CurrentRateBPM;
                bool handIn = pressDetector.IsHandInContact;
                if (instructionText != null)
                    instructionText.text = CPRTextLibrary.CPRProgress(
                        compressionCount, targetCompressions, depth, rate, handIn);
                UpdateMergedCprHud(depth, rate, handIn);

                // 实时触觉保底：深度> 0.5cm 就触发震动（不等 OnCompressionComplete）
                if (depth > 0.5f && hapticController != null)
                {
                    if (Mathf.Abs(depth - _lastRealtimeHapticDepth) > 0.3f)
                    {
                        _lastRealtimeHapticDepth = depth;
                        hapticController.TriggerByDepth(depth);
                    }
                }
                else
                {
                    _lastRealtimeHapticDepth = 0f;
                }
            }
        }

        // 此方法已由CPRTextLibrary + LanguageManager 替代，保留空方法不影响调用栈
        private void ReplaceUITextToJapanese() { }

        private Vector3 patientStandPosition;
        private Quaternion patientStandRotation;

        // 摄像头初始位置（用于「再来一次」时恢复）
        private Vector3 _initialCameraRigPosition;
        private Quaternion _initialCameraRigRotation;
        private Vector3 _initialMainCamPosition;
        private Quaternion _initialMainCamRotation;
        private bool _initialCameraPositionSaved;

        // UI 根节点初始位置（所有面板共用的 Canvas 根）
        private Transform _uiRoot;
        private Vector3 _uiRootInitialPosition;
        private Quaternion _uiRootInitialRotation;

        private void AutoFindAndFixPatient()
        {
            // 优先当前项目使用的人物：Standing React Death Backward（上半身为Ch38_Shirt）
            if (patientModel == null)
                patientModel = GameObject.Find("Standing React Death Backward");
            if (patientModel == null)
                patientModel = GameObject.Find("XBot_Patient");
            if (patientModel == null)
                patientModel = GameObject.Find("Patient");
            if (patientModel == null)
                patientModel = GameObject.Find("X Bot");
            if (patientModel == null)
                patientModel = FindPatientByModelHierarchy(); // 按Ch38_Body / mixamorig 等子节点找
            if (patientModel == null)
                patientModel = FindPatientByAnimator();

            if (patientModel == null)
            {
                Debug.LogWarning("[CPRTraining] Patient model not found automatically. Assign patientModel in the inspector or use a model named Patient / XBot_Patient / Ch38_Body.");
                return;
            }

            // 修复 Scale
            if (patientModel.transform.localScale.x < 0.5f)
            {
                patientModel.transform.localScale = Vector3.one;
                Debug.Log("[CPRTraining] 修复 Scale -> (1,1,1)");
            }

            // 修复 Position（确保不在原点，Z 约1 使患者离玩家更近）
            if (patientModel.transform.position.magnitude < 0.1f)
            {
                patientModel.transform.position = new Vector3(0, 0, 1f);
                Debug.Log("[CPRTraining] 修复 Position -> (0,0,1)");
            }

            // 面向相机
            patientModel.transform.rotation = Quaternion.Euler(0, 180, 0);
            patientModel.SetActive(true);
            patientStandPosition = patientModel.transform.position;
            patientStandRotation = patientModel.transform.rotation;

            patientAnimator = patientModel.GetComponent<Animator>();
            if (patientAnimator != null)
            {
                if (patientAnimator.runtimeAnimatorController == null)
                {
                    var ctrl = Resources.Load<RuntimeAnimatorController>("XBot_DeathController");
                    if (ctrl != null)
                        patientAnimator.runtimeAnimatorController = ctrl;
                }

                // 关键：禁用Root Motion，防止动画把模型移到地下
                patientAnimator.applyRootMotion = false;
                patientAnimator.speed = 0f;

                bool hasController = patientAnimator.runtimeAnimatorController != null;
                Debug.Log($"[CPRTraining] Animator: controller={hasController}, applyRootMotion=false");
            }

            // 清理场景中旧的检测区
            var oldDetector = GameObject.Find("CPR_ChestDetector");
            if (oldDetector != null)
            {
                Destroy(oldDetector);
                Debug.Log("[CPRTraining] 清理旧的检测区");
            }

            Debug.Log($"[CPRTraining] 患者就绪Pos:{patientModel.transform.position} Scale:{patientModel.transform.localScale}");
        }

        /// <summary> 按模型层级查找：根物体下含有 Ch38_Body、mixamorig 等典型人形骨骼网格的视为患者</summary>
        private GameObject FindPatientByModelHierarchy()
        {
            var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in roots)
            {
                var candidate = FindPatientRootRecursive(root.transform);
                if (candidate != null) return candidate;
            }
            return null;
        }

        private GameObject FindPatientRootRecursive(Transform t)
        {
            if (t.name.IndexOf("Ch38_Body", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                (t.name.IndexOf("mixamorig", System.StringComparison.OrdinalIgnoreCase) >= 0 && t.name.IndexOf("Hips", System.StringComparison.OrdinalIgnoreCase) >= 0))
            {
                Transform p = t;
                while (p != null)
                {
                    if (p.GetComponent<Animator>() != null)
                        return p.gameObject;
                    p = p.parent;
                }
                return t.root.gameObject;
            }
            foreach (Transform child in t)
            {
                var found = FindPatientRootRecursive(child);
                if (found != null) return found;
            }
            return null;
        }

        private GameObject FindPatientByAnimator()
        {
            var animators = FindObjectsOfType<Animator>(true);
            foreach (var a in animators)
            {
                if (a.avatar != null && a.avatar.isHuman && a.runtimeAnimatorController != null)
                {
                    if (a.gameObject.name.Contains("Patient") || a.gameObject.name.Contains("Bot") || a.gameObject.name.Contains("X Bot"))
                        return a.gameObject;
                }
            }
            foreach (var a in animators)
            {
                if (a.avatar != null && a.avatar.isHuman)
                {
                    if (FindBoneRecursive(a.transform, "Spine") != null || FindBoneRecursive(a.transform, "Spine1") != null)
                        return a.gameObject;
                }
            }
            return animators.Length > 0 ? animators[0].gameObject : null;
        }

        private void AutoFindUI()
        {
            if (startScenarioButton == null)
            {
                startScenarioButton = FindButtonByName("StartButton");
                if (startScenarioButton == null) startScenarioButton = FindButtonByName("StartScenarioButton");
                if (startScenarioButton == null)
                {
                    startScenarioButton = FindButtonByText(
                        "CPR",
                        "Scenario",
                        "\u5f00\u59cb",
                        "\u8bad\u7ec3",
                        "\u30b7\u30ca\u30ea\u30aa",
                        "\u30c8\u30ec\u30fc\u30cb\u30f3\u30b0"
                    );
                }
            }

            if (startCPRButton == null)
            {
                startCPRButton = FindButtonByName("StartCPRButton");
                if (startCPRButton == null) startCPRButton = FindButtonByName("CPRTrainingButton");
            }

            if (startAEDButton == null)
            {
                startAEDButton = FindButtonByName("AEDTrainingButton");
                if (startAEDButton == null) startAEDButton = FindButtonByName("AEDButton");
            }

            if (quizButton == null)
            {
                quizButton = FindButtonByName("KnowledgeQuizButton");
                if (quizButton == null) quizButton = FindButtonByName("QuizButton");
            }

            if (backTrainingButton == null)
            {
                backTrainingButton = FindButtonByName("BackTrainingButton");
                if (backTrainingButton == null) backTrainingButton = FindButtonByName("BackButton");
            }

            if (restartButton == null)
                restartButton = FindButtonByName("RestartButton");

            if (exitButton == null)
                exitButton = FindButtonByName("ExitButton") ?? FindButtonByName("QuitButton");

            if (exitTrainingButton == null)
                exitTrainingButton = FindButtonByName("ExitTrainingButton");

            if (hudExitButton == null)
                hudExitButton = FindButtonByName("HUDExitButton") ?? FindButtonByName("HudExitButton");

            // \u8fd9\u4e24\u4e2a\u5b57\u6bb5\u539f\u6765\u662f legacy Text\u3001\u5e76\u5728 7 \u4e2a\u573a\u666f\u91cc\u63a5\u4e86\u7ebf\u3002\u6539\u6210 TMP_Text \u540e
            // Unity \u4f1a\u4e22\u6389\u90a3\u4e9b\u5e8f\u5217\u5316\u5f15\u7528\uff08\u7c7b\u578b\u4e0d\u5339\u914d\u65e0\u6cd5\u4fdd\u7559\uff09\uff0c\u6240\u4ee5\u8fd9\u91cc\u6309\u7269\u4f53\u540d
            // \u91cd\u65b0\u89e3\u6790\uff0c\u5e76\u628a\u627e\u5230\u7684 legacy Text \u5c31\u5730\u5347\u7ea7\u6210 TMP\u3002
            //
            // \u987a\u5e26\u6362\u6389\u4e86\u539f\u6765\u7684\u515c\u5e95\u65b9\u5f0f\uff1a\u5b83\u662f\u6309\u6587\u6848\u5185\u5bb9\u5339\u914d\u7684\uff08\u641c\u542b
            // "CPR" / "\u8bad\u7ec3" / "\u6309\u538b" \u7b49\u5b57\u6837\u7684\u6587\u672c\uff09\uff0c\u6362\u4e2a\u8bed\u8a00\u6216\u6539\u4e00\u53e5\u6587\u6848\u5c31\u4f1a\u5931\u7075\u3002
            // \u7269\u4f53\u540d\u662f\u7a33\u5b9a\u7684\u3002
            if (phaseText == null)
                phaseText = ResolveAndUpgradeText("PhaseText");

            if (instructionText == null)
                instructionText = ResolveAndUpgradeText("InstructionText");

            Debug.Log($"[CPRTraining] UI: startBtn={startScenarioButton != null}, exitBtn={exitButton != null}");
        }

        /// <summary>
        /// 按物体名找到文本物体，并保证它带的是 TMP 组件。
        /// GameObject.Find 找不到非激活物体，所以再兜一层全量搜索。
        /// </summary>
        private TMP_Text ResolveAndUpgradeText(string objectName)
        {
            GameObject go = GameObject.Find(objectName);

            if (go == null)
            {
                foreach (TMP_Text t in FindObjectsOfType<TMP_Text>(true))
                {
                    if (t.gameObject.name == objectName)
                        return t;   // 已经升级过了
                }

                foreach (Text t in FindObjectsOfType<Text>(true))
                {
                    if (t.gameObject.name == objectName)
                    {
                        go = t.gameObject;
                        break;
                    }
                }
            }

            if (go == null)
            {
                Debug.LogWarning($"[CPRTraining] 找不到文本物体「{objectName}」，相关提示文字不会显示");
                return null;
            }

            return UpgradeToTmp(go);
        }

        /// <summary>
        /// 把物体上的 legacy Text 换成 TMP，并把场景里已经调好的排版参数带过去
        /// （PhaseText 是 60 号黄色、InstructionText 是 42 号近白，丢了就得重新调）。
        /// </summary>
        private TMP_Text UpgradeToTmp(GameObject go)
        {
            TMP_Text existing = go.GetComponent<TMP_Text>();
            if (existing != null)
                return existing;

            int fontSize = 32;
            Color color = UIColorScheme.TextPrimary;
            TextAnchor anchor = TextAnchor.MiddleCenter;
            string content = string.Empty;
            bool bestFit = false;

            Text legacy = go.GetComponent<Text>();
            if (legacy != null)
            {
                fontSize = legacy.fontSize;
                color = legacy.color;
                anchor = legacy.alignment;
                content = legacy.text;
                bestFit = legacy.resizeTextForBestFit;

                // Destroy 要到帧末才真正移除，期间同物体上会同时存在两个 Graphic
                // 而导致文字重影 —— 先 disable 让它立刻停止渲染。
                legacy.enabled = false;
                Destroy(legacy);
            }

            TMP_Text tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.alignment = ToTmpAlignment(anchor);
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Overflow;

            if (bestFit)
            {
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = Mathf.Max(10, fontSize - 8);
                tmp.fontSizeMax = fontSize;
            }

            VRTrainingUIStyle.ApplyText(tmp, color, fontSize >= 30);

            Debug.Log($"[CPRTraining] 「{go.name}」已从 legacy Text 升级为 TMP（{fontSize}号）");
            return tmp;
        }

        private Button FindButtonByName(string name)
        {
            var obj = GameObject.Find(name);
            if (obj != null) return obj.GetComponent<Button>();
            var allButtons = FindObjectsOfType<Button>(true);
            foreach (var b in allButtons)
            {
                if (b.gameObject.name == name) return b;
            }
            return null;
        }

        private Button FindButtonByText(params string[] keywords)
        {
            var allButtons = FindObjectsOfType<Button>(true);
            foreach (var b in allButtons)
            {
                var text = b.GetComponentInChildren<Text>(true);
                if (text == null) continue;
                foreach (var kw in keywords)
                {
                    if (!string.IsNullOrEmpty(kw) && text.text != null && text.text.Contains(kw))
                        return b;
                }
            }
            return null;
        }

        private void DisableOtherControllers()
        {
            var simpleMenu = FindObjectOfType<SimpleMenuController>();
            if (simpleMenu != null)
            {
                simpleMenu.enabled = false;
                Debug.Log("[CPRTraining] 禁用 SimpleMenuController");
            }
            var gameController = FindObjectOfType<CPRGameController>();
            if (gameController != null)
            {
                gameController.enabled = false;
                Debug.Log("[CPRTraining] 禁用 CPRGameController");
            }
            // 移除 PatientFallController（它会覆盖Animator 设置）
            var patient = GameObject.Find("XBot_Patient") ?? GameObject.Find("Patient") ?? GameObject.Find("Standing React Death Backward") ?? GameObject.Find("X Bot");
            if (patient != null)
            {
                var fallCtrl = patient.GetComponent<Core.PatientFallController>();
                if (fallCtrl != null)
                {
                    Destroy(fallCtrl);
                    Debug.Log("[CPRTraining] 移除冲突的PatientFallController");
                }
            }
        }

        private void SetupButtons()
        {
            EnsureTrainingModeButtonsVisible();

            if (startScenarioButton != null)
            {
                startScenarioButton.onClick.RemoveAllListeners();
                startScenarioButton.onClick.AddListener(OnStartScenarioClicked);
                Debug.Log("[CPRTraining] 开始按钮已绑定到倒地流程");
            }

            if (startCPRButton != null)
            {
                startCPRButton.onClick.RemoveAllListeners();
                startCPRButton.onClick.AddListener(OnStartCPRClicked);
            }

            if (startAEDButton != null)
            {
                startAEDButton.onClick.RemoveAllListeners();
                startAEDButton.onClick.AddListener(OnStartAEDClicked);
            }

            if (quizButton != null)
            {
                quizButton.onClick.RemoveAllListeners();
                quizButton.onClick.AddListener(OnStartQuizClicked);
            }

            if (backTrainingButton != null)
            {
                backTrainingButton.onClick.RemoveAllListeners();
                backTrainingButton.onClick.AddListener(OnBackTrainingClicked);
            }

            if (restartButton != null)
                restartButton.onClick.AddListener(RestartTraining);

            if (exitButton != null)
            {
                exitButton.onClick.RemoveAllListeners();
                exitButton.onClick.AddListener(ExitToMainMenu);
            }

            if (exitTrainingButton != null)
            {
                exitTrainingButton.onClick.RemoveAllListeners();
                exitTrainingButton.onClick.AddListener(ExitToMainMenu);
            }

            // CPR 进行中HUD 上的中途退出按钮：不会退出应用，只是结束本次训练并回到开始界面
            if (hudExitButton != null)
            {
                hudExitButton.onClick.RemoveAllListeners();
                hudExitButton.onClick.AddListener(AbortCurrentTraining);
            }

            // 调整开始界面的按钮布局与样式，让手部射线选择时更明显
            EnsureTrainingModeButtonsVisible();
            AdjustStartMenuLayoutAndStyle();
        }

        /// <summary>
        /// 将开始界面的按钮适当拉开，并增强高亮效果，方便手部射线选择。
        /// </summary>
        private void AdjustStartMenuLayoutAndStyle()
        {
            Button[] btns = new Button[]
            {
                startScenarioButton,
                startCPRButton,
                startAEDButton,
                quizButton,
                backTrainingButton,
                exitTrainingButton
            };

            foreach (var btn in btns)
            {
                if (btn == null) continue;

                Color accent = btn == startScenarioButton
                    ? UIColorScheme.Warning
                    : btn == startCPRButton
                        ? UIColorScheme.Success
                        : btn == startAEDButton
                            ? UIColorScheme.Danger
                            : btn == quizButton
                                ? UIColorScheme.Info
                                : UIColorScheme.PrimaryDark;
                VRTrainingUIStyle.ApplyButton(btn, accent);

                var labels = btn.GetComponentsInChildren<Text>(true);
                foreach (var label in labels)
                {
                    VRTrainingUIStyle.ApplyText(label, UIColorScheme.TextPrimary);
                }
            }

            LayoutStartMenuButton(startScenarioButton, new Vector2(-290f, 60f), new Vector2(490f, 124f), 26);
            LayoutStartMenuButton(startCPRButton, new Vector2(290f, 60f), new Vector2(490f, 124f), 26);
            LayoutStartMenuButton(startAEDButton, new Vector2(-290f, -95f), new Vector2(490f, 124f), 26);
            LayoutStartMenuButton(quizButton, new Vector2(290f, -95f), new Vector2(490f, 124f), 26);
            LayoutStartMenuButton(backTrainingButton, new Vector2(-190f, -235f), new Vector2(300f, 76f), 30);
            LayoutStartMenuButton(exitTrainingButton, new Vector2(190f, -235f), new Vector2(300f, 76f), 30);

            if (startTrainingPanel != null)
            {
                VRTrainingUIStyle.ApplyPanel(startTrainingPanel, false);
                if (phaseText != null && phaseText.transform.IsChildOf(startTrainingPanel.transform))
                {
                    ConfigureRuntimeText(
                        phaseText,
                        new Vector2(0f, 245f),
                        new Vector2(1180f, 72f),
                        44,
                        TextAnchor.MiddleCenter,
                        UIColorScheme.TextPrimary
                    );
                }

                if (instructionText != null && instructionText.transform.IsChildOf(startTrainingPanel.transform))
                {
                    ConfigureRuntimeText(
                        instructionText,
                        new Vector2(0f, 150f),
                        new Vector2(1180f, 82f),
                        28,
                        TextAnchor.MiddleCenter,
                        UIColorScheme.TextSecondary
                    );
                }
            }
        }

        private void LayoutStartMenuButton(Button button, Vector2 anchoredPosition, Vector2 size, int fontSize)
        {
            if (button == null) return;

            button.gameObject.SetActive(true);
            var rt = button.transform as RectTransform;
            if (rt == null) return;

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = size;

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = true;
            }

            Text[] labels = button.GetComponentsInChildren<Text>(true);
            foreach (Text label in labels)
            {
                if (label == null)
                    continue;

                RectTransform labelRt = label.transform as RectTransform;
                if (labelRt != null)
                {
                    labelRt.anchorMin = Vector2.zero;
                    labelRt.anchorMax = Vector2.one;
                    labelRt.offsetMin = new Vector2(18f, 8f);
                    labelRt.offsetMax = new Vector2(-18f, -8f);
                    labelRt.localScale = Vector3.one;
                }

                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.fontSize = fontSize;
                label.fontStyle = FontStyle.Bold;
                label.supportRichText = true;
                label.alignment = TextAnchor.MiddleCenter;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = Mathf.Max(14, fontSize - 12);
                label.resizeTextMaxSize = fontSize;
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.verticalOverflow = VerticalWrapMode.Overflow;
                VRTrainingUIStyle.ApplyText(label, UIColorScheme.TextPrimary, fontSize >= 38);
            }
        }

        private void SetupCPRDetector()
        {
            if (pressDetector == null)
                pressDetector = FindObjectOfType<CPRPressDetector>();

            if (pressDetector != null)
            {
                pressDetector.OnCompressionComplete -= OnCompressionDetected;
                pressDetector.OnCompressionComplete += OnCompressionDetected;
            }
            // 同步绑定触觉控制器
            if (hapticController == null)
                hapticController = FindObjectOfType<GloveHapticController>();
            if (hapticController != null && pressDetector != null)
                hapticController.BindDetector(pressDetector);
        }

        private IEnumerator StartIntroSequence()
        {
            if (TryConsumeRequestedTrainingMode(out TrainingMode requestedMode))
            {
                BeginTraining(requestedMode);
                yield break;
            }

            bool hasTrainingSelection = startScenarioButton != null ||
                                        startCPRButton != null ||
                                        startAEDButton != null ||
                                        quizButton != null;

            if (hasTrainingSelection)
            {
                HideAllPanels();
                if (introPanel != null) introPanel.SetActive(true);
                if (startTrainingPanel != null) startTrainingPanel.SetActive(true);

                ChangePhase(TrainingPhase.WaitingStart);
                PrepareTrainingSelectionUI();
                SaveInitialCameraPositionIfNeeded(false);
                PlaySound(introSound);
                Debug.Log("[CPRTraining] Waiting for training mode selection.");
                yield break;
            }

            ChangePhase(TrainingPhase.Intro);

            if (introPanel != null)
                ShowPanel(introPanel);

            RefreshAllTrainingUI();
            PlaySound(introSound);

            yield return new WaitForSecondsRealtime(introDuration);

            Debug.Log("[CPRTraining] No start menu buttons found, beginning CPR training directly.");
            BeginTraining(TrainingMode.CPR);
        }

        private void OnStartScenarioClicked()
        {
            BeginTraining(TrainingMode.Story);
        }

        private void OnStartCPRClicked()
        {
            BeginTraining(TrainingMode.CPR);
        }

        private void OnStartQuizClicked()
        {
            BeginTraining(TrainingMode.Quiz);
        }

        private void OnBackTrainingClicked()
        {
            if (currentPhase == TrainingPhase.Quiz)
            {
                PrepareTrainingSelectionUI();
                return;
            }

            if (currentPhase != TrainingPhase.WaitingStart)
            {
                AbortCurrentTraining();
                return;
            }

            if (!string.IsNullOrWhiteSpace(mainMenuSceneName) &&
                Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                SceneManager.LoadScene(mainMenuSceneName);
                return;
            }

            PrepareTrainingSelectionUI();
        }

        private bool waitingForFlip = false;
        private GameObject flipPromptUI;

        private IEnumerator PatientFallSequence()
        {
            ChangePhase(TrainingPhase.PatientFalling);
            HideAllPanels();

            UpdateInstruction(CPRTextLibrary.PatientFell);
            PlaySound(fallSound);

            if (teleportAfterFall && teleportDelay > 0f)
            {
                Debug.Log($"[CPRTraining] 传送将在{teleportDelay} 秒后执行");
                StartCoroutine(TeleportAfterDelay(teleportDelay));
            }

            if (patientModel != null)
            {
                yield return StartCoroutine(PlayDeathAnimation());
            }

            yield return new WaitForSeconds(0.5f);
            OnPatientFell?.Invoke();

            if (ResolveSelectedTrainingMode() == TrainingMode.Story)
            {
                yield return StartCoroutine(WaitForStoryCallHelpConfirmation());
                ChangePhase(TrainingPhase.AEDInProgress);
                HideStoryCallHelpPanel();
                HideCprUiForAedMode();
                UpdateInstruction(GetStoryAEDPhaseInstruction());
                yield break;
            }

            if (ResolveSelectedTrainingMode() == TrainingMode.AED)
            {
                ChangePhase(TrainingPhase.AEDInProgress);
                HideCprUiForAedMode();
                UpdateInstruction(GetAEDPhaseInstruction());
                yield break;
            }

            if (skipFlipInteraction)
            {
                // 跳过翻转交互，直接开始CPR（防止翻转时掉地下）
                Debug.Log("[CPRTraining] 跳过翻转交互，直接开始CPR");

                // 直接创建检测区
                CreateCPRDetectionZone();

                yield return new WaitForSeconds(0.5f);
                StartCPRPhase();
            }
            else
            {
                ChangePhase(TrainingPhase.ReadyForCPR);
                UpdateInstruction(CPRTextLibrary.FlipPatient);

                // 创建翻转提示 UI 和交互检测
                yield return StartCoroutine(WaitForFlipInteraction());

                // 翻转完成后创建检测区
                CreateCPRDetectionZone();

                yield return new WaitForSeconds(0.5f);
                StartCPRPhase();
            }
        }

        private IEnumerator TeleportAfterDelay(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            TeleportPlayerToPatient();
        }

        private void TeleportPlayerToPatient()
        {
            // 传送前保存当前位置，作为「再来一次」时的恢复目标
            SaveInitialCameraPositionIfNeeded(forceOverwrite: true);

            var cameraRig = FindObjectOfType<OVRCameraRig>();
            if (cameraRig == null) return;

            Vector3 targetPos;
            Quaternion targetRot;

            if (useFixedTeleportPosition)
            {
                targetPos = teleportTargetPosition;
                targetRot = Quaternion.Euler(teleportTargetRotation);
            }
            else
            {
                if (patientModel == null) return;
                Vector3 patientPos = patientModel.transform.position;
                Vector3 patientForward = patientModel.transform.forward;
                patientForward.y = 0f;
                if (patientForward.sqrMagnitude < 0.01f)
                    patientForward = Vector3.forward;
                patientForward.Normalize();
                Vector3 offset = patientForward * teleportOffsetDistance + Vector3.up * 0.1f;
                targetPos = patientPos + offset;
                targetRot = Quaternion.LookRotation(patientPos - targetPos);
            }

            // CharacterController 会覆盖直接设置的 transform.position，需先禁用再设置
            var cc = cameraRig.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
            }

            cameraRig.transform.position = targetPos;
            cameraRig.transform.rotation = targetRot;

            if (cc != null)
            {
                cc.enabled = true;
            }

            Debug.Log($"[CPRTraining] 已传送到患者身旁Pos:{targetPos}");
        }

        private void EnsureHUDFollowsCamera()
        {
            if (trainingHUDPanel == null) return;
            Transform root = trainingHUDPanel.transform;
            while (root.parent != null) root = root.parent;
            var follow = root.GetComponent<CPRHUDFollowCamera>();
            var resultPlacer = root.GetComponent<CPRResultPanelPlacer>();
            if (resultPlacer != null) resultPlacer.enabled = false; // 训练阶段关闭结果布局
            if (follow == null)
            {
                follow = root.gameObject.AddComponent<CPRHUDFollowCamera>();
                // 让HUD 更靠近人物和视线
                follow.placeUINearPatient = true;
                follow.anchorOverride = patientModel != null
                    ? FindBoneRecursive(patientModel.transform, "Head")
                    : null;
                follow.heightAboveAnchor = 0.25f;
                follow.distanceInFront = 0.5f;
                follow.followSmoothing = 0.08f;
                // 更偏向贴近患者（数值越小越靠近人物）
                follow.faceCamera = true;
                follow.onlyWhenActive = false;
                follow.groundLevel = 0f;
                // 调低最低高度，让UI 更靠近地面一些
                follow.minHeightAboveGround = 0.4f;
            }
            else
            {
                follow.enabled = true;
            }
        }

        private void EnsureHUDFixedNearPatient()
        {
            if (trainingHUDPanel == null) return;

            Transform root = trainingHUDPanel.transform;
            while (root.parent != null) root = root.parent;

            var follow = root.GetComponent<CPRHUDFollowCamera>();
            if (follow != null)
                follow.enabled = false;

            var resultPlacer = root.GetComponent<CPRResultPanelPlacer>();
            if (resultPlacer != null)
                resultPlacer.enabled = false;

            var canvas = root.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.WorldSpace;
                if (canvas.worldCamera == null && Camera.main != null)
                    canvas.worldCamera = Camera.main;
            }

            Transform reference = chestDetectionZone != null
                ? chestDetectionZone.transform
                : (patientModel != null ? patientModel.transform : null);
            if (reference == null)
                return;

            Transform viewer = GetPrimaryViewTransform();
            Vector3 fixedPosition = reference.position + Vector3.up * FixedCprHudHeightAboveChest;
            Vector3 towardViewer = Vector3.zero;
            if (viewer != null)
                towardViewer = Vector3.ProjectOnPlane(viewer.position - reference.position, Vector3.up);
            if (towardViewer.sqrMagnitude < 0.001f && patientModel != null)
                towardViewer = -patientModel.transform.forward;
            if (towardViewer.sqrMagnitude > 0.001f)
            {
                towardViewer.Normalize();
                fixedPosition += towardViewer * FixedCprHudDistanceTowardViewer;
            }

            root.position = fixedPosition;
            root.localScale = Vector3.one * 0.001f;

            if (viewer != null)
            {
                Vector3 faceDirection = Vector3.ProjectOnPlane(root.position - viewer.position, Vector3.up);
                if (faceDirection.sqrMagnitude > 0.001f)
                    root.rotation = Quaternion.LookRotation(faceDirection.normalized, Vector3.up);
            }
        }

        private Transform GetPrimaryViewTransform()
        {
            var cameraRig = FindObjectOfType<OVRCameraRig>();
            if (cameraRig != null && cameraRig.centerEyeAnchor != null)
                return cameraRig.centerEyeAnchor;

            return Camera.main != null ? Camera.main.transform : null;
        }

        /// <summary>
        /// 用户在按压过程中中途退出当前训练：清空本次统计，回到开始训练界面。
        /// </summary>
        private void AbortCurrentTraining()
        {
            Debug.Log("[CPRTraining] Training aborted by user");

            StopAllCoroutines();

            ResetSessionMetrics();
            selectedTrainingMode = TrainingMode.None;
            ChangePhase(TrainingPhase.WaitingStart);

            if (pressDetector != null)
                pressDetector.ResetStats();

            if (chestDetectionZone != null)
            {
                Destroy(chestDetectionZone);
                chestDetectionZone = null;
            }

            lockPatientPosition = false;

            if (hapticController != null)
                hapticController.StopAllFeedback();

            HideAllPanels();
            if (startTrainingPanel != null)
            {
                startTrainingPanel.SetActive(true);
                PrepareTrainingSelectionUI();
            }
            else if (introPanel != null)
            {
                introPanel.SetActive(true);
            }
        }

        private IEnumerator WaitForFlipInteraction()
        {
            waitingForFlip = true;

            var flipTrigger = new GameObject("FlipTrigger");
            Vector3 bodyCenter = patientModel.transform.position + Vector3.up * 0.3f;
            flipTrigger.transform.position = bodyCenter;

            var col = flipTrigger.AddComponent<BoxCollider>();
            col.size = new Vector3(1f, 0.6f, 2f);
            col.isTrigger = true;

            var rb = flipTrigger.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            var detector = flipTrigger.AddComponent<FlipInteractionDetector>();
            detector.onInteracted = () => { waitingForFlip = false; };

            CreateFlipArrowIndicator(bodyCenter);
            Debug.Log("[CPRTraining] Waiting for flip interaction");

            float timeout = 15f;
            float elapsed = 0f;
            while (waitingForFlip && elapsed < timeout)
            {
                elapsed += Time.deltaTime;

                if (Input.GetKeyDown(KeyCode.F))
                    waitingForFlip = false;

                yield return null;
            }

            if (flipPromptUI != null)
                Destroy(flipPromptUI);
            Destroy(flipTrigger);

            UpdateInstruction(CPRTextLibrary.Flipping);
            yield return StartCoroutine(FlipToSupine());

            Debug.Log("[CPRTraining] Flip complete");
        }

        private IEnumerator FlipToSupine()
        {
            if (patientModel == null)
                yield break;

            Vector3 startPos = patientModel.transform.position;
            startPos.y = Mathf.Max(startPos.y, 0f);
            patientModel.transform.position = startPos;

            lockedPosition = startPos;
            lockPatientPosition = true;

            if (patientAnimator != null)
                patientAnimator.enabled = false;

            Quaternion startRot = patientModel.transform.rotation;
            float currentX = NormalizeAngle(startRot.eulerAngles.x);
            float yAngle = startRot.eulerAngles.y;

            Quaternion targetRot;
            if (currentX > 0f)
            {
                targetRot = Quaternion.Euler(-90f, yAngle, 0f);
            }
            else
            {
                Vector3 modelForward = patientModel.transform.forward;
                targetRot = modelForward.y > 0f
                    ? startRot
                    : startRot * Quaternion.Euler(180f, 0f, 0f);
            }

            Debug.Log($"[CPRTraining] Flip target rotation: {targetRot.eulerAngles}");

            float duration = 1.5f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = t * t * (3f - 2f * t);

                patientModel.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);

                Vector3 pos = patientModel.transform.position;
                pos.y = 0f;
                patientModel.transform.position = pos;
                lockedPosition = pos;
                yield return null;
            }

            patientModel.transform.rotation = targetRot;
            Vector3 finalPos = patientModel.transform.position;
            finalPos.y = 0f;
            patientModel.transform.position = finalPos;
            lockedPosition = finalPos;

            Debug.Log($"[CPRTraining] Patient flipped to supine at {finalPos}");
        }

        private float NormalizeAngle(float angle)
        {
            while (angle > 180f) angle -= 360f;
            while (angle < -180f) angle += 360f;
            return angle;
        }

        private void CreateFlipArrowIndicator(Vector3 position)
        {
            // 创建一个旋转箭头提示
            flipPromptUI = new GameObject("FlipArrowIndicator");
            flipPromptUI.transform.position = position + Vector3.up * 0.5f;

            // 使用一个简单的圆柱体作为箭头指示
            var arrow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            arrow.name = "Arrow";
            arrow.transform.SetParent(flipPromptUI.transform);
            arrow.transform.localPosition = Vector3.zero;
            arrow.transform.localScale = new Vector3(0.3f, 0.02f, 0.3f);
            arrow.transform.localRotation = Quaternion.Euler(0, 0, 90f);

            Destroy(arrow.GetComponent<Collider>());

            var renderer = arrow.GetComponent<Renderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            mat.color = new Color(0.2f, 0.8f, 1f, 0.8f);
            renderer.material = mat;

            // 添加旋转动画
            var rotator = flipPromptUI.AddComponent<SpinAnimation>();
        }

        private IEnumerator PlayDeathAnimation()
        {
            if (patientModel == null) yield break;

            bool hasAnimator = patientAnimator != null
                && patientAnimator.runtimeAnimatorController != null;

            if (hasAnimator)
            {
                Debug.Log("[CPRTraining] ===== 开始播放FBX 死亡动画 =====");

                patientAnimator.enabled = true;
                patientAnimator.applyRootMotion = false;  // 关键：禁用Root Motion，防止掉地下
                patientAnimator.speed = 1f;

                // 播放倒地状态（Inspector 可设为DeathFromRight 或Standing React Death Backward）
                string stateToPlay = !string.IsNullOrEmpty(fallDownStateName) ? fallDownStateName : "DeathFromRight";
                patientAnimator.Play(stateToPlay, 0, 0f);
                Debug.Log($"[CPRTraining] Play({stateToPlay}) 已调用，applyRootMotion=false");

                // 记录初始位置，用于防止掉地下
                Vector3 startPos = patientModel.transform.position;

                yield return null;

                // 获取动画片段长度
                float animLength = 3.5f;
                var clipInfo = patientAnimator.GetCurrentAnimatorClipInfo(0);
                if (clipInfo.Length > 0)
                {
                    animLength = clipInfo[0].clip.length;
                    Debug.Log($"[CPRTraining] 动画片段: {clipInfo[0].clip.name}, 时长: {animLength:F2}s");
                }
                else
                {
                    var stateInfo = patientAnimator.GetCurrentAnimatorStateInfo(0);
                    if (stateInfo.length > 0)
                        animLength = stateInfo.length;
                    Debug.Log($"[CPRTraining] 使用 stateInfo 时长: {animLength:F2}s");
                }

                // 等待动画完成，同时锁定Y 轴防止掉地下
                float elapsed = 0f;
                while (elapsed < animLength + 0.2f)
                {
                    elapsed += Time.deltaTime;

                    // 每帧锁定 Y 轴（不让动画把模型移到地下）
                    Vector3 pos = patientModel.transform.position;
                    pos.y = Mathf.Max(pos.y, 0f);  // 不低于地面
                    patientModel.transform.position = pos;

                    yield return null;
                }

                // 动画完成后：完全禁用 Animator 并锁定位置
                Vector3 finalPos = patientModel.transform.position;
                Quaternion finalRot = patientModel.transform.rotation;

                // 关键：先禁用 Animator，再设置位置（防止Animator 覆盖）
                patientAnimator.enabled = false;

                // 强制锁定位置（Y 轴不低于 0）
                finalPos.y = Mathf.Max(finalPos.y, 0f);
                patientModel.transform.position = finalPos;
                patientModel.transform.rotation = finalRot;

                // 添加 Rigidbody 并禁用重力（如果还没有）
                var rb = patientModel.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = patientModel.AddComponent<Rigidbody>();
                    Debug.Log("[CPRTraining] 已添加Rigidbody");
                }
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;  // 冻结 Y 轴和旋转

                // 启动 Update() 每帧位置锁定
                lockedPosition = finalPos;
                lockPatientPosition = true;

                Debug.Log($"[CPRTraining] 动画完成并冻结Pos:{finalPos} Rot:{finalRot.eulerAngles}，已启动每帧位置锁定");
            }
            else
            {
                Debug.LogWarning("[CPRTraining] 无AnimatorController！请 Ctrl+R 重新编译让AutoAnimatorSetup 自动创建");
                yield return new WaitForSeconds(1f);
            }
        }

        private GameObject chestDetectionZone;

        private void CreateCPRDetectionZone()
        {
            if (chestDetectionZone != null) return;
            if (patientModel == null)
            {
                AutoFindAndFixPatient();
                if (patientModel == null)
                {
                    Debug.LogError("[CPRTraining] Failed to create chest detection zone: Patient Model is not assigned.");
                    return;
                }
            }

            // 找胸部参考：优先骨骼，其次上半身 Ch38_Shirt（Standing React Death Backward 的上半身）
            Transform chestBone = FindBoneRecursive(patientModel.transform, "Spine2");
            if (chestBone == null) chestBone = FindBoneRecursive(patientModel.transform, "Spine1");
            if (chestBone == null) chestBone = FindBoneRecursive(patientModel.transform, "UpperChest");
            if (chestBone == null) chestBone = FindBoneRecursive(patientModel.transform, "Chest");
            if (chestBone == null) chestBone = FindBoneRecursive(patientModel.transform, "Spine");

            Transform upperBody = patientModel.transform.Find("Ch38_Shirt");
            if (upperBody == null)
                upperBody = FindChildRecursiveByName(patientModel.transform, "Ch38_Shirt");
            if (upperBody == null)
                upperBody = FindChildRecursiveByName(patientModel.transform, "Shirt");

            Transform zoneParent;
            Vector3 localPos;
            if (chestBone != null)
            {
                zoneParent = chestBone;
                localPos = detectionLocalOffset;
                Debug.Log($"[CPRTraining] 找到胸部骨骼: {chestBone.name}，检测区将随骨骼移动");
            }
            else if (upperBody != null)
            {
                zoneParent = patientModel.transform;
                var shirtRenderer = upperBody.GetComponent<SkinnedMeshRenderer>();
                if (shirtRenderer != null && shirtRenderer.bounds.size.sqrMagnitude > 0.001f)
                {
                    Vector3 chestWorld = shirtRenderer.bounds.center;
                    localPos = zoneParent.InverseTransformPoint(chestWorld);
                    Debug.Log($"[CPRTraining] 使用 Ch38_Shirt 网格中心作为胸部，世界位置 {chestWorld}");
                }
                else
                {
                    localPos = zoneParent.InverseTransformPoint(upperBody.position);
                    Debug.Log("[CPRTraining] Using Ch38_Shirt transform as chest fallback position");
                }
            }
            else
            {
                zoneParent = patientModel.transform;
                localPos = new Vector3(0f, 0.25f, 0f);
                Debug.Log("[CPRTraining] 未找到胸部骨骼上半身，使用模型中心偏上作为胸部");
            }

            // 创建检测区域（红色半透明球体），挂到胸部/患者下以便倒地后仍对准胸部
            chestDetectionZone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            chestDetectionZone.name = "CPR_ChestDetector";
            chestDetectionZone.transform.SetParent(zoneParent, false);
            chestDetectionZone.transform.localPosition = localPos;
            chestDetectionZone.transform.localRotation = Quaternion.identity;
            chestDetectionZone.transform.localScale = detectionZoneScale;
            // 关键：强制世界旋转为单位旋转，让 chestSurface.up = 世界上方 = 真实按压法线。
            // 否则检测区继承胸骨(Spine2)旋转，按压轴投影会算错深度。患者动画已冻结(speed=0)，此旋转保持稳定。
            chestDetectionZone.transform.rotation = Quaternion.identity;

            // 红色半透明材质（兼容URP 和Built-in）
            var renderer = chestDetectionZone.GetComponent<Renderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            var mat = new Material(shader);
            mat.color = new Color(1f, 0.15f, 0.15f, 0.7f);

            // 尝试设置透明模式（URP）
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1);
                mat.SetFloat("_Blend", 0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            // 通用透明设置
            mat.SetOverrideTag("RenderType", "Transparent");
            if (mat.HasProperty("_SrcBlend"))
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend"))
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite"))
                mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
            renderer.material = mat;

            // 触发器（用于检测手部碰撞）
            // 半径 0.7(本地) × 缩放 0.2 ≈ 0.14m 世界半径：覆盖手悬停-接触全程，
            // 让动态基准在真正的"手悬停于胸口上方"处校准，又不把远处的手误判为接触。
            var collider = chestDetectionZone.GetComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = detectionTriggerRadius;

            // 添加脉冲发光动画
            chestDetectionZone.AddComponent<DetectionZonePulse>();

            // 确保有手部追踪适配器（距离备用检测需要）
            EnsureHandTrackingAdapter();

            // 若无检测器则添加，并绑定到本控制器与触觉
            var detector = chestDetectionZone.GetComponent<CPRPressDetector>();
            if (detector == null)
            {
                detector = chestDetectionZone.AddComponent<CPRPressDetector>();
                detector.useCollisionDetection = true;
                detector.detectionAreaSize = detectionAreaSize;
                detector.useDistanceFallback = true;
                // 距离备用阈值：只有手真正贴近胸口才驱动检测，提升区域识别精度
                detector.palmDistanceThreshold = detectionPalmThreshold;
                // 精度增强参数：世界上方为按压轴、双阈值滞回、去抖
                detector.useChestAxis = true;
                detector.pressStartDepthCm = 2f;
                detector.releaseDepthCm = 1.5f;
                detector.minCompressionInterval = 0.2f;
            }
            detector.chestSurface = chestDetectionZone.transform;
            if (pressDetector == null)
            {
                pressDetector = detector;
                SetupCPRDetector();
            }
            else
            {
                pressDetector = detector;
                if (pressDetector != null)
                {
                    pressDetector.OnCompressionComplete -= OnCompressionDetected;
                    pressDetector.OnCompressionComplete += OnCompressionDetected;
                }
            }
            if (hapticController != null)
            {
                hapticController.BindDetector(detector); // 订阅事件，必须在 Start() 之后调用
            }
            else
            {
                hapticController = FindObjectOfType<GloveHapticController>();
                if (hapticController != null) hapticController.BindDetector(detector);
            }

            Debug.Log($"[CPRTraining] CPR 检测区已创建，挂载于{zoneParent.name}，世界位置 {chestDetectionZone.transform.position}，haptic={hapticController != null}");
        }

        // 在 Scene 视图绘制 CPR 检测范围，便于编辑期调整（需选中挂有本脚本的物体）
        private void OnDrawGizmosSelected()
        {
            if (!drawDetectionGizmo) return;

            GameObject model = patientModel != null
                ? patientModel
                : GameObject.Find("Standing React Death Backward");
            if (model == null) return;

            Transform chestBone = FindBoneRecursive(model.transform, "Spine2");
            if (chestBone == null) chestBone = FindBoneRecursive(model.transform, "Spine1");
            if (chestBone == null) chestBone = FindBoneRecursive(model.transform, "Spine");
            Transform parent = chestBone != null ? chestBone : model.transform;

            Vector3 center = parent.TransformPoint(detectionLocalOffset);

            // 红：触发球范围（世界半径 ≈ 本地半径 × 检测区世界缩放最大轴）
            Vector3 zoneLossy = Vector3.Scale(parent.lossyScale, detectionZoneScale);
            float worldRadius = detectionTriggerRadius *
                Mathf.Max(Mathf.Abs(zoneLossy.x), Mathf.Abs(zoneLossy.y), Mathf.Abs(zoneLossy.z));
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);
            Gizmos.DrawWireSphere(center, worldRadius);

            // 蓝：AABB 备用检测尺寸
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
            Gizmos.DrawWireCube(center, detectionAreaSize);

            // 黄：距离备用阈值
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.5f);
            Gizmos.DrawWireSphere(center, detectionPalmThreshold);
        }

        private void EnsureHandTrackingAdapter()
        {
            if (FindObjectOfType<HandTrackingAdapter>() != null) return;
            var rig = FindObjectOfType<OVRCameraRig>();
            GameObject target = rig != null ? rig.gameObject : new GameObject("HandTrackingAdapter");
            if (rig == null) Debug.Log("[CPRTraining] 未找到OVRCameraRig，已创建独立 HandTrackingAdapter 物体");
            target.AddComponent<HandTrackingAdapter>();
            Debug.Log("[CPRTraining] 已自动添加HandTrackingAdapter（用于距离备用检测）");
        }

        private Transform FindBoneRecursive(Transform parent, string nameContains)
        {
            foreach (Transform child in parent)
            {
                if (child.name.IndexOf(nameContains, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return child;
                var result = FindBoneRecursive(child, nameContains);
                if (result != null) return result;
            }
            return null;
        }

        private Transform FindChildRecursiveByName(Transform parent, string nameContains)
        {
            foreach (Transform child in parent)
            {
                if (child.name.IndexOf(nameContains, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return child;
                var result = FindChildRecursiveByName(child, nameContains);
                if (result != null) return result;
            }
            return null;
        }

        private void BeginTraining(TrainingMode mode)
        {
            selectedTrainingMode = mode;

            if (mode == TrainingMode.Quiz)
            {
                StopAllCoroutines();
                ShowKnowledgeQuizPanel();
                return;
            }

            HideKnowledgeQuizPanel();
            HideStoryCallHelpPanel();
            StartCoroutine(PatientFallSequence());
        }

        private void OnStartAEDClicked()
        {
            BeginTraining(TrainingMode.AED);
        }

        public void StartCPRTrainingFromEmergency()
        {
            if (currentPhase == TrainingPhase.PatientFalling ||
                currentPhase == TrainingPhase.CPRInProgress ||
                currentPhase == TrainingPhase.AEDInProgress)
            {
                return;
            }

            BeginTraining(TrainingMode.CPR);
        }

        public void StartAEDTrainingFromEmergency()
        {
            if (currentPhase == TrainingPhase.PatientFalling ||
                currentPhase == TrainingPhase.CPRInProgress ||
                currentPhase == TrainingPhase.AEDInProgress)
            {
                return;
            }

            BeginTraining(TrainingMode.AED);
        }

        private void StartCPRPhase(bool allowAfterAed = false)
        {
            if (!UsesCprFlow() && !allowAfterAed)
            {
                HideCprUiForAedMode();
                return;
            }

            if (chestDetectionZone == null)
            {
                CreateCPRDetectionZone();
            }

            ChangePhase(TrainingPhase.CPRInProgress);
            EnsureHUDFollowsCamera();
            ShowPanel(trainingHUDPanel);
            BuildMergedCprHudIfNeeded();

            ResetSessionMetrics();
            trainingStartTime = Time.time;

            if (hapticController != null)
            {
                hapticController.ResetForNewSession();
            }

            // 启动CPR检测
            if (pressDetector != null)
            {
                pressDetector.ResetStats();
                pressDetector.Initialize();
            }

            OnCPRStarted?.Invoke();
            PlaySound(startCPRSound);

            UpdateInstruction(CPRTextLibrary.StartCPRNow);
            UpdateMergedCprHud(0f, 0f, false);
        }

        public void StartCPRAfterAED()
        {
            StartCPRPhase(true);
        }

        public void HideCprUiForAedMode()
        {
            HideAllPanels();
            if (_languageSelectPanel != null)
                _languageSelectPanel.SetActive(false);
        }

        private void ResetSessionMetrics()
        {
            compressionCount = 0;
            trainingStartTime = 0f;
            _sumDepthCm = 0f;
            _sumRateBpm = 0f;
            _compressionSamples = 0;
            _rateSamples = 0;
            _goodDepthSamples = 0;
            _goodRateSamples = 0;
            _goodCombinedSamples = 0;
            _tooShallowSamples = 0;
            _tooDeepSamples = 0;
            _tooSlowSamples = 0;
            _tooFastSamples = 0;
            _currentQualityStreak = 0;
            _bestQualityStreak = 0;
        }

        private void OnCompressionDetected(float depth, float rate)
        {
            if (currentPhase != TrainingPhase.CPRInProgress) return;

            compressionCount++;
            // 累积用于平均值统计
            RecordCompressionSample(depth, rate);

            OnCompressionProgress?.Invoke(compressionCount, targetCompressions);

            UpdateInstruction(CPRTextLibrary.CPRAbortProgress(compressionCount, targetCompressions, depth, rate));
            UpdateMergedCprHud(depth, rate, true);

            // 直接触发触觉（不依赖事件订阅，确保每次按压都有反馈）
            if (hapticController == null)
                hapticController = FindObjectOfType<GloveHapticController>();
            if (hapticController != null && !hapticController.IsBoundTo(pressDetector))
                hapticController.TriggerByDepth(depth);

            if (compressionCount >= targetCompressions)
            {
                CompleteTraining();
            }
        }

        private void RecordCompressionSample(float depth, float rate)
        {
            float minDepth = pressDetector != null ? pressDetector.minGoodDepthCm : 5f;
            float maxDepth = pressDetector != null ? pressDetector.maxGoodDepthCm : 6f;
            float minRate = pressDetector != null ? pressDetector.minGoodRateBPM : 100f;
            float maxRate = pressDetector != null ? pressDetector.maxGoodRateBPM : 120f;

            _sumDepthCm += depth;
            _compressionSamples++;

            bool depthGood = depth >= minDepth && depth <= maxDepth;
            if (depthGood)
                _goodDepthSamples++;
            else if (depth < minDepth)
                _tooShallowSamples++;
            else
                _tooDeepSamples++;

            bool hasRate = rate > 0.1f;
            bool rateGood = false;
            if (hasRate)
            {
                _sumRateBpm += rate;
                _rateSamples++;
                rateGood = rate >= minRate && rate <= maxRate;

                if (rateGood)
                    _goodRateSamples++;
                else if (rate < minRate)
                    _tooSlowSamples++;
                else
                    _tooFastSamples++;
            }

            if (depthGood && rateGood)
            {
                _goodCombinedSamples++;
                _currentQualityStreak++;
                _bestQualityStreak = Mathf.Max(_bestQualityStreak, _currentQualityStreak);
            }
            else if (hasRate)
            {
                _currentQualityStreak = 0;
            }
        }

        private void CompleteTraining()
        {
            EnsureLanguageManager();  // 确保有LanguageManager，结果界面按当前语言显示
            _lastCompletedTrainingMode = ResolveSelectedTrainingMode();
            ChangePhase(TrainingPhase.Completed);
            ShowPanel(completionPanel);
            BuildCompletionUIIfNeeded();

            // 完成阶段：停止HUD 跟随；若非固定模式，将统计UI 放到人物胸部旁边的地面上
            if (completionPanel != null)
            {
                Transform root = completionPanel.transform;
                while (root.parent != null) root = root.parent;
                var follow = root.GetComponent<CPRHUDFollowCamera>();
                if (follow != null) follow.enabled = false;
                var placer = root.GetComponent<CPRResultPanelPlacer>();
                if (completionPanelFixedPosition)
                {
                    if (placer != null) placer.enabled = false;
                }
                else
                {
                    if (placer == null)
                    {
                        placer = root.gameObject.AddComponent<CPRResultPanelPlacer>();
                        placer.sideOffset = 0.6f;
                        placer.groundY = 0f;
                        placer.faceCamera = true;
                    }
                    else
                    {
                        placer.enabled = true;
                    }
                }
            }

            float totalTime = Time.time - trainingStartTime;
            float avgDepth = _compressionSamples > 0 ? _sumDepthCm / _compressionSamples : 0f;
            float avgRate = _rateSamples > 0 ? _sumRateBpm / _rateSamples : 0f;

            // 根据医学推荐范围评价按压质量（根据当前语言返回对应文字）
            string depthEval = CPRTextLibrary.DepthNoData;
            string rateEval  = CPRTextLibrary.RateNoData;

            if (pressDetector != null && _compressionSamples > 0)
            {
                float minDepth = pressDetector.minGoodDepthCm;
                float maxDepth = pressDetector.maxGoodDepthCm;
                float minRate  = pressDetector.minGoodRateBPM;
                float maxRate  = pressDetector.maxGoodRateBPM;

                if (avgDepth <= 0.1f)
                    depthEval = CPRTextLibrary.DepthNoData;
                else if (avgDepth < minDepth)
                    depthEval = CPRTextLibrary.DepthTooShallow(minDepth, maxDepth);
                else if (avgDepth > maxDepth)
                    depthEval = CPRTextLibrary.DepthTooDeep(minDepth, maxDepth);
                else
                    depthEval = CPRTextLibrary.DepthGood(avgDepth);

                if (avgRate <= 0.1f)
                    rateEval = CPRTextLibrary.RateNoData;
                else if (avgRate < minRate)
                    rateEval = CPRTextLibrary.RateTooSlow(minRate, maxRate);
                else if (avgRate > maxRate)
                    rateEval = CPRTextLibrary.RateTooFast(minRate, maxRate);
                else
                    rateEval = CPRTextLibrary.RateGood(avgRate);
            }

            string result = BuildEnhancedTrainingResultText(
                compressionCount,
                targetCompressions,
                totalTime,
                avgDepth,
                avgRate,
                depthEval,
                rateEval);

            // AED 模式下追加 AED 环节评价
            // 只更新专用的结果 Text，不覆盖标题等其他文字
            Text targetText = _completionTextRuntime;
            if (targetText == null) targetText = completionResultText;
            // 按名字找 ResultText（不用GetComponentInChildren 以免拿到标题）
            if (targetText == null && completionPanel != null)
            {
                var allTexts = completionPanel.GetComponentsInChildren<Text>(true);
                foreach (var t in allTexts)
                {
                    string n = t.gameObject.name.ToLower();
                    if (n.Contains("result") || n.Contains("cpr_result"))
                    { targetText = t; break; }
                }
            }
            if (_completionViewRuntime != null)
                _completionViewRuntime.SetResult(result);
            else if (targetText != null)
                targetText.text = result;
            else if (instructionText != null)
                // instructionText 现在是 TMP_Text，不能再赋给 Text 类型的 targetText，
                // 所以把「退回指导文本」这一步单独写出来，行为不变
                instructionText.text = result;

            // 按当前语言刷新完成界面的标题和按钮（与开始界面选的语言一致）
            if (completionPanel != null)
            {
                var allTexts = completionPanel.GetComponentsInChildren<Text>(true);
                foreach (var t in allTexts)
                {
                    string n = t.gameObject.name.ToLower();
                    if ((n.Contains("complete") && n.Contains("title")) || n.Contains("completetitle"))
                    {
                        t.text = CPRTextLibrary.CompletionTitle;
                        break;
                    }
                }
                SetButtonLabel(_completionRestartButtonRuntime, CPRTextLibrary.BtnRestart);
                SetButtonLabel(_completionExitButtonRuntime, CPRTextLibrary.BtnExit);
                if (_completionViewRuntime != null)
                    _completionViewRuntime.RefreshLabels(CPRTextLibrary.BtnRestart, CPRTextLibrary.BtnExit);
            }

            // 训练完成后立即停止触觉反馈
            if (hapticController != null)
            {
                hapticController.StopAllFeedback();
            }

            PlaySound(completeSound);

            Debug.Log($"[CPRTraining] 完成 - 按压:{compressionCount}, 用时:{totalTime:F1}s");
        }

        /// <summary>
        /// 第一次进入完成阶段时，优先使用场景中已有的ResultText/RestartButton/ExitButton；
        /// 若无则自动创建，确保结果显示在UI 上且无重复遮挡。
        /// </summary>
        private string BuildEnhancedTrainingResultText(
            int count,
            int target,
            float totalTime,
            float avgDepth,
            float avgRate,
            string depthEval,
            string rateEval)
        {
            float minDepth = pressDetector != null ? pressDetector.minGoodDepthCm : 5f;
            float maxDepth = pressDetector != null ? pressDetector.maxGoodDepthCm : 6f;
            float minRate = pressDetector != null ? pressDetector.minGoodRateBPM : 100f;
            float maxRate = pressDetector != null ? pressDetector.maxGoodRateBPM : 120f;

            float depthScore = CalculateHitPercent(_goodDepthSamples, _compressionSamples);
            float rateScore = _rateSamples > 0 ? CalculateHitPercent(_goodRateSamples, _rateSamples) : CalculateRangeScore(avgRate, minRate, maxRate);
            float consistencyScore = CalculateConsistencyScore();
            float countScore = Mathf.Clamp01(target > 0 ? count / (float)target : 0f) * 100f;
            bool includeAed = RequiresAedFlowForResult();
            float aedScore = includeAed ? CalculateAedScore() : 100f;
            float overallScore = includeAed
                ? depthScore * 0.25f + rateScore * 0.25f + consistencyScore * 0.10f + countScore * 0.15f + aedScore * 0.25f
                : depthScore * 0.35f + rateScore * 0.35f + consistencyScore * 0.10f + countScore * 0.20f;

            string separator = "\n";
            string result =
                $"{GetResultHeaderLabel()} - {GetResultModeLabel()}{separator}" +
                $"{GetOverallScoreLabel()}: {overallScore:F0}/100  {GetTrainingGrade(overallScore)}{separator}{separator}" +
                $"{GetScoreBreakdownLabel()}{separator}" +
                $"{GetDepthScoreLabel()}: {depthScore:F0}/100  ({avgDepth:F1} cm){separator}" +
                $"{GetRateScoreLabel()}: {rateScore:F0}/100  ({avgRate:F0}/min){separator}" +
                $"{GetConsistencyScoreLabel()}: {consistencyScore:F0}/100  ({GetBestStreakLabel()}: {_bestQualityStreak}){separator}" +
                $"{GetCompressionCountLabel()}: {count}/{target}  ({countScore:F0}/100){separator}" +
                $"{GetAedStepScoreLabel()}: {(includeAed ? $"{aedScore:F0}/100" : GetNotIncludedLabel())}{separator}{separator}" +
                $"{GetGuidelineHitRateLabel()}{separator}" +
                $"{GetDepthInRangeLabel()}: {FormatPercent(_goodDepthSamples, _compressionSamples)}  ({_goodDepthSamples}/{_compressionSamples}){separator}" +
                $"{GetRateInRangeLabel()}: {FormatPercent(_goodRateSamples, _rateSamples)}  ({_goodRateSamples}/{_rateSamples}){separator}" +
                $"{GetBestStreakLabel()}: {_bestQualityStreak}{separator}{separator}" +
                $"{GetProcessDataLabel()}{separator}" +
                $"{GetTimeUsedLabel()}: {totalTime:F1} {GetSecondsUnitLabel()}{separator}" +
                $"{depthEval}{separator}" +
                $"{rateEval}{separator}{separator}" +
                $"{GetNextFocusLabel()}: {BuildNextFocus()}{separator}{separator}" +
                $"{GetMistakeReviewLabel()}{separator}" +
                $"{BuildMistakeReview(avgDepth, avgRate, minDepth, maxDepth, minRate, maxRate, includeAed)}";

            if (includeAed)
            {
                result += separator + separator + BuildAedStepReview();
            }

            return result;
        }

        private float CalculateRangeScore(float value, float min, float max)
        {
            if (value <= 0.1f)
                return 0f;

            if (value >= min && value <= max)
                return 100f;

            if (value < min)
                return Mathf.Clamp01(value / Mathf.Max(0.01f, min)) * 100f;

            float over = value - max;
            float tolerance = Mathf.Max(1f, max * 0.35f);
            return Mathf.Clamp01(1f - over / tolerance) * 100f;
        }

        private float CalculateHitPercent(int good, int total)
        {
            if (total <= 0)
                return 0f;

            return Mathf.Clamp01(good / (float)total) * 100f;
        }

        private string FormatPercent(int good, int total)
        {
            return total > 0 ? $"{CalculateHitPercent(good, total):F0}%" : "--";
        }

        private float CalculateConsistencyScore()
        {
            if (_rateSamples <= 0)
                return 0f;

            float combinedHitRate = CalculateHitPercent(_goodCombinedSamples, _rateSamples);
            float streakTarget = Mathf.Clamp(targetCompressions > 0 ? targetCompressions * 0.35f : 8f, 6f, 12f);
            float streakScore = Mathf.Clamp01(_bestQualityStreak / streakTarget) * 100f;
            return combinedHitRate * 0.70f + streakScore * 0.30f;
        }

        private string BuildNextFocus()
        {
            if (_compressionSamples <= 0)
            {
                return LanguageManager.Get(
                    "\u5148\u786e\u8ba4\u624b\u638c\u63a5\u89e6\u80f8\u9aa8\u4e2d\u592e\u5e76\u5b8c\u6210\u6709\u6548\u56de\u5f39\u3002",
                    "\u307e\u305a\u624b\u306e\u63a5\u89e6\u3068\u80f8\u9aa8\u4e2d\u592e\u306e\u4f4d\u7f6e\u3092\u78ba\u8a8d\u3057\u3001\u6709\u52b9\u306a\u623b\u308a\u307e\u3067\u884c\u3044\u307e\u3059\u3002",
                    "Confirm hand contact at the center of the sternum and complete recoil first."
                );
            }

            float depthHitRate = CalculateHitPercent(_goodDepthSamples, _compressionSamples);
            if (depthHitRate < 75f)
            {
                if (_tooDeepSamples > _tooShallowSamples)
                {
                    return LanguageManager.Get(
                        "\u529b\u5ea6\u504f\u5927\uff0c\u4e0b\u4e00\u8f6e\u628a\u5cf0\u503c\u63a7\u5236\u5728 5-6 cm\u5185\u3002",
                        "\u529b\u304c\u5f37\u3081\u3067\u3059\u3002\u6b21\u306f\u30d4\u30fc\u30af\u3092 5-6 cm \u306b\u6291\u3048\u3066\u304f\u3060\u3055\u3044\u3002",
                        "Ease pressure and keep peak depth inside 5-6 cm."
                    );
                }

                return LanguageManager.Get(
                    "\u6df1\u5ea6\u4e0d\u8db3\uff0c\u4e0b\u4e00\u8f6e\u4fdd\u6301\u624b\u8098\u4f38\u76f4\u5e76\u538b\u5230 5-6 cm\u3002",
                    "\u6df1\u3055\u304c\u4e0d\u8db3\u3057\u3066\u3044\u307e\u3059\u3002\u6b21\u306f\u8098\u3092\u4f38\u3070\u3057\u3066 5-6 cm \u307e\u3067\u5727\u8feb\u3057\u307e\u3059\u3002",
                    "Press deeper: keep elbows straight and reach 5-6 cm."
                );
            }

            if (_rateSamples <= 0)
            {
                return LanguageManager.Get(
                    "\u5148\u5b8c\u6210\u81f3\u5c11\u4e24\u6b21\u8fde\u7eed\u6309\u538b\uff0c\u8ba9\u7cfb\u7edf\u83b7\u5f97\u8282\u594f\u6570\u636e\u3002",
                    "\u30c6\u30f3\u30dd\u8a08\u6e2c\u306e\u305f\u3081\u3001\u307e\u305a 2 \u56de\u4ee5\u4e0a\u9023\u7d9a\u3067\u5727\u8feb\u3057\u3066\u304f\u3060\u3055\u3044\u3002",
                    "Do at least two compressions so the system can measure rhythm."
                );
            }

            float rateHitRate = CalculateHitPercent(_goodRateSamples, _rateSamples);
            if (rateHitRate < 75f)
            {
                if (_tooFastSamples > _tooSlowSamples)
                {
                    return LanguageManager.Get(
                        "\u8282\u594f\u504f\u5feb\uff0c\u6309\u8282\u62cd\u5668\u6162\u5230 100-120 \u6b21/\u5206\u5e76\u7ed9\u80f8\u90e8\u56de\u5f39\u65f6\u95f4\u3002",
                        "\u30c6\u30f3\u30dd\u304c\u901f\u3081\u3067\u3059\u3002\u30e1\u30c8\u30ed\u30ce\u30fc\u30e0\u306b\u5408\u308f\u305b\u3066 100-120 \u56de/\u5206\u3078\u843d\u3068\u3057\u3001\u623b\u308a\u306e\u6642\u9593\u3092\u78ba\u4fdd\u3057\u307e\u3059\u3002",
                        "Slow to 100-120/min with the metronome and allow recoil time."
                    );
                }

                return LanguageManager.Get(
                    "\u8282\u594f\u504f\u6162\uff0c\u5148\u8ddf\u968f 110/min \u8282\u62cd\u4fdd\u6301\u8fde\u7eed\u6309\u538b\u3002",
                    "\u30c6\u30f3\u30dd\u304c\u9045\u3081\u3067\u3059\u3002\u307e\u305a 110/\u5206 \u306e\u62cd\u5b50\u306b\u5408\u308f\u305b\u3066\u9023\u7d9a\u5727\u8feb\u3057\u307e\u3059\u3002",
                    "Follow the 110/min beat and keep compressions continuous."
                );
            }

            if (_bestQualityStreak < 8)
            {
                return LanguageManager.Get(
                    "\u5355\u6b21\u8d28\u91cf\u53ef\u4ee5\uff0c\u4e0b\u4e00\u8f6e\u628a\u8fde\u7eed\u8fbe\u6807\u63d0\u5230 8 \u6b21\u4ee5\u4e0a\u3002",
                    "\u5358\u56de\u306e\u8cea\u306f\u826f\u597d\u3067\u3059\u3002\u6b21\u306f\u9023\u7d9a\u5408\u683c\u3092 8 \u56de\u4ee5\u4e0a\u306b\u4f38\u3070\u3057\u307e\u3059\u3002",
                    "Quality is close. Build a streak of 8+ correct compressions."
                );
            }

            return LanguageManager.Get(
                "\u57fa\u7840\u6309\u538b\u5df2\u7a33\u5b9a\uff0c\u4e0b\u4e00\u6b65\u7ec3\u4e60\u5267\u60c5\u6216 AED \u6d41\u7a0b\u4e0b\u7684\u5feb\u901f\u51b3\u7b56\u3002",
                "\u57fa\u672c\u306e\u5727\u8feb\u306f\u5b89\u5b9a\u3057\u3066\u3044\u307e\u3059\u3002\u6b21\u306f\u30b7\u30ca\u30ea\u30aa\u307e\u305f\u306f AED \u6d41\u308c\u306e\u5224\u65ad\u3092\u7df4\u7fd2\u3057\u307e\u3059\u3002",
                "Core compression is stable. Move to Story or AED flow for decision-making."
            );
        }

        private bool RequiresAedFlowForResult()
        {
            TrainingMode mode = ResolveSelectedTrainingMode();
            return mode == TrainingMode.Story || mode == TrainingMode.AED;
        }

        private float CalculateAedScore()
        {
            if (!VRFirstAid.Equipment.AEDTrainingRuntime.HasResult)
                return 0f;

            float score = 0f;
            if (VRFirstAid.Equipment.AEDTrainingRuntime.ResultPadsPlaced)
                score += 45f;

            bool shockAdvised = VRFirstAid.Equipment.AEDTrainingRuntime.ResultShockAdvised;
            bool shockDelivered = VRFirstAid.Equipment.AEDTrainingRuntime.ResultShockDelivered;
            if ((shockAdvised && shockDelivered) || (!shockAdvised && !shockDelivered))
                score += 35f;

            float duration = VRFirstAid.Equipment.AEDTrainingRuntime.ResultDurationSeconds;
            if (duration <= 30f)
                score += 20f;
            else if (duration <= 60f)
                score += 12f;
            else if (duration <= 90f)
                score += 6f;

            return Mathf.Clamp(score, 0f, 100f);
        }

        private string BuildMistakeReview(float avgDepth, float avgRate, float minDepth, float maxDepth, float minRate, float maxRate, bool includeAed)
        {
            string review = string.Empty;

            AppendReviewLine(ref review, compressionCount <= 0, GetReviewNoCompression());
            AppendReviewLine(ref review, avgDepth > 0.1f && avgDepth < minDepth, GetReviewDepthTooShallow());
            AppendReviewLine(ref review, avgDepth > maxDepth, GetReviewDepthTooDeep());
            AppendReviewLine(ref review, _compressionSamples > 0 && CalculateHitPercent(_goodDepthSamples, _compressionSamples) < 75f, GetReviewDepthInconsistent());
            AppendReviewLine(ref review, compressionCount > 1 && _rateSamples <= 0, GetReviewNoRateData());
            AppendReviewLine(ref review, avgRate > 0.1f && avgRate < minRate, GetReviewRateTooSlow());
            AppendReviewLine(ref review, avgRate > maxRate, GetReviewRateTooFast());
            AppendReviewLine(ref review, _rateSamples > 0 && CalculateHitPercent(_goodRateSamples, _rateSamples) < 75f, GetReviewRateInconsistent());
            AppendReviewLine(ref review, _rateSamples >= 6 && _bestQualityStreak < 8, GetReviewStreakTooShort());

            if (includeAed)
            {
                bool hasAed = VRFirstAid.Equipment.AEDTrainingRuntime.HasResult;
                AppendReviewLine(ref review, !hasAed, GetReviewAedNotCompleted());
                AppendReviewLine(ref review, hasAed && !VRFirstAid.Equipment.AEDTrainingRuntime.ResultPadsPlaced, GetReviewPadsMissing());
                AppendReviewLine(
                    ref review,
                    hasAed &&
                    VRFirstAid.Equipment.AEDTrainingRuntime.ResultShockAdvised &&
                    !VRFirstAid.Equipment.AEDTrainingRuntime.ResultShockDelivered,
                    GetReviewShockMissing());
            }

            return string.IsNullOrEmpty(review) ? GetReviewGoodRun() : review;
        }

        private void AppendReviewLine(ref string review, bool condition, string line)
        {
            if (!condition)
                return;

            if (!string.IsNullOrEmpty(review))
                review += "\n";

            review += "- " + line;
        }

        private string BuildAedStepReview()
        {
            if (!VRFirstAid.Equipment.AEDTrainingRuntime.HasResult)
                return $"{GetAedStepReviewLabel()}: {GetNotCompletedLabel()}";

            string pads = VRFirstAid.Equipment.AEDTrainingRuntime.ResultPadsPlaced ? GetPassedLabel() : GetMissedLabel();
            bool shockAdvised = VRFirstAid.Equipment.AEDTrainingRuntime.ResultShockAdvised;
            bool shockDelivered = VRFirstAid.Equipment.AEDTrainingRuntime.ResultShockDelivered;
            string shock = shockAdvised
                ? (shockDelivered ? GetPassedLabel() : GetMissedLabel())
                : GetNoShockAdvisedLabel();

            return $"{GetAedStepReviewLabel()}\n" +
                   $"{GetAedPadsLabel()}: {pads}\n" +
                   $"{GetAedShockLabel()}: {shock}\n" +
                   $"{GetTimeUsedLabel()}: {VRFirstAid.Equipment.AEDTrainingRuntime.ResultDurationSeconds:F1} {GetSecondsUnitLabel()}";
        }

        private string GetTrainingGrade(float score)
        {
            if (score >= 90f) return "S";
            if (score >= 80f) return "A";
            if (score >= 70f) return "B";
            if (score >= 60f) return "C";
            return "D";
        }

        private string GetResultModeLabel()
        {
            switch (ResolveSelectedTrainingMode())
            {
                case TrainingMode.Story: return GetStoryTrainingButtonLabel();
                case TrainingMode.AED: return GetAEDTrainingButtonLabelV2();
                case TrainingMode.Quiz: return GetQuizButtonLabel();
                case TrainingMode.CPR:
                default:
                    return GetCPRTrainingButtonLabelV2();
            }
        }

        private string GetResultHeaderLabel() => LanguageManager.Get("\u8bad\u7ec3\u7ed3\u679c", "\u30c8\u30ec\u30fc\u30cb\u30f3\u30b0\u7d50\u679c", "Training Result");
        private string GetOverallScoreLabel() => LanguageManager.Get("\u603b\u5206", "\u7dcf\u5408\u70b9", "Overall Score");
        private string GetScoreBreakdownLabel() => LanguageManager.Get("\u5206\u9879\u8bc4\u5206", "\u9805\u76ee\u5225\u30b9\u30b3\u30a2", "Score Breakdown");
        private string GetDepthScoreLabel() => LanguageManager.Get("\u6309\u538b\u6df1\u5ea6", "\u5727\u8feb\u306e\u6df1\u3055", "Compression Depth");
        private string GetRateScoreLabel() => LanguageManager.Get("\u6309\u538b\u9891\u7387", "\u5727\u8feb\u30c6\u30f3\u30dd", "Compression Rate");
        private string GetConsistencyScoreLabel() => LanguageManager.Get("\u7a33\u5b9a\u6027", "\u5b89\u5b9a\u6027", "Consistency");
        private string GetCompressionCountLabel() => LanguageManager.Get("\u6309\u538b\u6b21\u6570", "\u5727\u8feb\u56de\u6570", "Compressions");
        private string GetAedStepScoreLabel() => LanguageManager.Get("AED \u6b65\u9aa4", "AED \u624b\u9806", "AED Steps");
        private string GetGuidelineHitRateLabel() => LanguageManager.Get("\u6307\u5357\u8fbe\u6807\u7387", "\u30ac\u30a4\u30c9\u30e9\u30a4\u30f3\u9054\u6210\u7387", "Guideline Hit Rate");
        private string GetDepthInRangeLabel() => LanguageManager.Get("\u6df1\u5ea6\u8fbe\u6807", "\u6df1\u3055\u9069\u5207", "Depth In Range");
        private string GetRateInRangeLabel() => LanguageManager.Get("\u9891\u7387\u8fbe\u6807", "\u30c6\u30f3\u30dd\u9069\u5207", "Rate In Range");
        private string GetBestStreakLabel() => LanguageManager.Get("\u6700\u957f\u8fde\u7eed\u8fbe\u6807", "\u6700\u9577\u9023\u7d9a\u5408\u683c", "Best Streak");
        private string GetNextFocusLabel() => LanguageManager.Get("\u4e0b\u4e00\u8f6e\u91cd\u70b9", "\u6b21\u306e\u91cd\u70b9", "Next Focus");
        private string GetProcessDataLabel() => LanguageManager.Get("\u8fc7\u7a0b\u6570\u636e", "\u30d7\u30ed\u30bb\u30b9\u30c7\u30fc\u30bf", "Process Data");
        private string GetTimeUsedLabel() => LanguageManager.Get("\u7528\u65f6", "\u6240\u8981\u6642\u9593", "Time");
        private string GetSecondsUnitLabel() => LanguageManager.Get("\u79d2", "\u79d2", "sec");
        private string GetMistakeReviewLabel() => LanguageManager.Get("\u9519\u8bef\u590d\u76d8", "\u6539\u5584\u30ec\u30d3\u30e5\u30fc", "Mistake Review");
        private string GetNotIncludedLabel() => LanguageManager.Get("\u672a\u8ba1\u5165", "\u5bfe\u8c61\u5916", "Not included");
        private string GetNotCompletedLabel() => LanguageManager.Get("\u672a\u5b8c\u6210", "\u672a\u5b8c\u4e86", "Not completed");
        private string GetPassedLabel() => LanguageManager.Get("\u901a\u8fc7", "\u5408\u683c", "Passed");
        private string GetMissedLabel() => LanguageManager.Get("\u672a\u5b8c\u6210", "\u672a\u5b8c\u4e86", "Missed");
        private string GetNoShockAdvisedLabel() => LanguageManager.Get("\u672a\u5efa\u8bae\u7535\u51fb", "\u30b7\u30e7\u30c3\u30af\u4e0d\u8981", "No shock advised");
        private string GetAedStepReviewLabel() => LanguageManager.Get("AED \u6b65\u9aa4\u590d\u76d8", "AED \u624b\u9806\u30ec\u30d3\u30e5\u30fc", "AED Step Review");
        private string GetAedPadsLabel() => LanguageManager.Get("\u8d34\u7247", "\u30d1\u30c3\u30c9", "Pads");
        private string GetAedShockLabel() => LanguageManager.Get("\u7535\u51fb\u5904\u7f6e", "\u30b7\u30e7\u30c3\u30af\u5bfe\u5fdc", "Shock Action");
        private string GetReviewNoCompression() => LanguageManager.Get("\u672a\u68c0\u6d4b\u5230\u6709\u6548\u6309\u538b\uff0c\u9700\u5148\u786e\u8ba4\u624b\u4f4d\u548c\u63a5\u89e6\u3002", "\u6709\u52b9\u306a\u5727\u8feb\u304c\u691c\u51fa\u3055\u308c\u3066\u3044\u307e\u305b\u3093\u3002\u624b\u306e\u4f4d\u7f6e\u3068\u63a5\u89e6\u3092\u78ba\u8a8d\u3057\u3066\u304f\u3060\u3055\u3044\u3002", "No valid compressions detected. Check hand position and contact first.");
        private string GetReviewDepthTooShallow() => LanguageManager.Get("\u6309\u538b\u504f\u6d45\uff0c\u4e0b\u4e00\u8f6e\u8981\u52a0\u6df1\u5230 5-6 cm\u3002", "\u5727\u8feb\u304c\u6d45\u3081\u3067\u3059\u3002\u6b21\u306f 5-6 cm \u3092\u76ee\u6307\u3057\u3066\u304f\u3060\u3055\u3044\u3002", "Compression depth was shallow. Aim for 5-6 cm next run.");
        private string GetReviewDepthTooDeep() => LanguageManager.Get("\u6309\u538b\u504f\u6df1\uff0c\u8981\u63a7\u5236\u529b\u5ea6\u907f\u514d\u8fc7\u538b\u3002", "\u5727\u8feb\u304c\u6df1\u3059\u304e\u307e\u3059\u3002\u529b\u3092\u8abf\u6574\u3057\u3066\u904e\u5727\u3092\u907f\u3051\u3066\u304f\u3060\u3055\u3044\u3002", "Compression depth was too deep. Control force to avoid over-compression.");
        private string GetReviewRateTooSlow() => LanguageManager.Get("\u9891\u7387\u504f\u6162\uff0c\u8ddf\u7740\u8282\u62cd\u5668\u7a33\u5b9a\u5230 100-120 \u6b21/\u5206\u3002", "\u30c6\u30f3\u30dd\u304c\u9045\u3081\u3067\u3059\u3002\u30e1\u30c8\u30ed\u30ce\u30fc\u30e0\u306b\u5408\u308f\u305b\u3066 100-120 \u56de/\u5206\u3092\u7dad\u6301\u3057\u3066\u304f\u3060\u3055\u3044\u3002", "Compression rate was slow. Use the metronome and hold 100-120/min.");
        private string GetReviewRateTooFast() => LanguageManager.Get("\u9891\u7387\u504f\u5feb\uff0c\u4e0d\u8981\u727a\u7272\u56de\u5f39\u548c\u6df1\u5ea6\u3002", "\u30c6\u30f3\u30dd\u304c\u901f\u3059\u304e\u307e\u3059\u3002\u80f8\u306e\u623b\u308a\u3068\u6df1\u3055\u3092\u72a0\u7272\u306b\u3057\u306a\u3044\u3067\u304f\u3060\u3055\u3044\u3002", "Compression rate was fast. Do not sacrifice recoil or depth.");
        private string GetReviewNoRateData() => LanguageManager.Get("\u9891\u7387\u6837\u672c\u4e0d\u8db3\uff0c\u9700\u81f3\u5c11\u4e24\u6b21\u8fde\u7eed\u6309\u538b\u624d\u80fd\u8bc4\u4f30\u8282\u594f\u3002", "\u30c6\u30f3\u30dd\u30c7\u30fc\u30bf\u304c\u4e0d\u8db3\u3067\u3059\u30022 \u56de\u4ee5\u4e0a\u9023\u7d9a\u3067\u5727\u8feb\u3059\u308b\u3068\u8a55\u4fa1\u3067\u304d\u307e\u3059\u3002", "Not enough rate samples. At least two consecutive compressions are needed to evaluate rhythm.");
        private string GetReviewDepthInconsistent() => LanguageManager.Get("\u6df1\u5ea6\u8fbe\u6807\u7387\u504f\u4f4e\uff0c\u4e0d\u8981\u53ea\u770b\u5e73\u5747\u503c\uff1b\u6bcf\u6b21\u90fd\u8981\u843d\u5728 5-6 cm\u3002", "\u6df1\u3055\u306e\u5408\u683c\u7387\u304c\u4f4e\u3081\u3067\u3059\u3002\u5e73\u5747\u5024\u3060\u3051\u3067\u306a\u304f\u3001\u6bce\u56de 5-6 cm \u306b\u5165\u308c\u3066\u304f\u3060\u3055\u3044\u3002", "Depth hit rate was low. Do not rely on the average; keep every compression within 5-6 cm.");
        private string GetReviewRateInconsistent() => LanguageManager.Get("\u9891\u7387\u8fbe\u6807\u7387\u504f\u4f4e\uff0c\u5148\u7ec3 10 \u6b21\u7a33\u5b9a\u8282\u62cd\u518d\u8ffd\u6c42\u901f\u5ea6\u3002", "\u30c6\u30f3\u30dd\u306e\u5408\u683c\u7387\u304c\u4f4e\u3081\u3067\u3059\u3002\u307e\u305a 10 \u56de\u306e\u5b89\u5b9a\u3057\u305f\u62cd\u5b50\u3092\u7df4\u7fd2\u3057\u307e\u3059\u3002", "Rate hit rate was low. Practice 10 steady beats before pushing speed.");
        private string GetReviewStreakTooShort() => LanguageManager.Get("\u8fde\u7eed\u8fbe\u6807\u6bb5\u8f83\u77ed\uff0c\u628a\u76ee\u6807\u8bbe\u4e3a\u8fde\u7eed 8 \u6b21\u6df1\u5ea6\u548c\u9891\u7387\u90fd\u8fbe\u6807\u3002", "\u9023\u7d9a\u5408\u683c\u304c\u77ed\u3081\u3067\u3059\u3002\u6df1\u3055\u3068\u30c6\u30f3\u30dd\u306e\u4e21\u65b9\u3067 8 \u56de\u9023\u7d9a\u5408\u683c\u3092\u76ee\u6307\u3057\u307e\u3059\u3002", "The correct-compression streak was short. Aim for 8 consecutive compressions with both depth and rate in range.");
        private string GetReviewAedNotCompleted() => LanguageManager.Get("AED \u6d41\u7a0b\u672a\u5b8c\u6210\uff0c\u9700\u5b8c\u6210\u5f00\u673a\u3001\u8d34\u7247\u548c\u5206\u6790\u3002", "AED \u624b\u9806\u304c\u672a\u5b8c\u4e86\u3067\u3059\u3002\u96fb\u6e90\u3001\u30d1\u30c3\u30c9\u8cbc\u4ed8\u3001\u89e3\u6790\u3092\u5b8c\u4e86\u3057\u3066\u304f\u3060\u3055\u3044\u3002", "AED flow was not completed. Finish power, pads, and analysis.");
        private string GetReviewPadsMissing() => LanguageManager.Get("AED \u8d34\u7247\u672a\u6b63\u786e\u653e\u7f6e\uff0c\u4f18\u5148\u7ec3\u4e60\u8d34\u7247\u4f4d\u7f6e\u3002", "AED \u30d1\u30c3\u30c9\u304c\u6b63\u3057\u304f\u8cbc\u3089\u308c\u3066\u3044\u307e\u305b\u3093\u3002\u30d1\u30c3\u30c9\u4f4d\u7f6e\u3092\u512a\u5148\u3057\u3066\u7df4\u7fd2\u3057\u3066\u304f\u3060\u3055\u3044\u3002", "AED pads were not placed correctly. Practice pad placement first.");
        private string GetReviewShockMissing() => LanguageManager.Get("\u7cfb\u7edf\u5efa\u8bae\u7535\u51fb\u65f6\u672a\u6267\u884c\u7535\u51fb\uff0c\u9700\u5148\u786e\u8ba4\u65e0\u4eba\u63a5\u89e6\u60a3\u8005\u3002", "\u30b7\u30e7\u30c3\u30af\u63a8\u5968\u6642\u306b\u5b9f\u884c\u3055\u308c\u3066\u3044\u307e\u305b\u3093\u3002\u5468\u56f2\u306e\u5b89\u5168\u78ba\u8a8d\u5f8c\u306b\u5b9f\u884c\u3057\u3066\u304f\u3060\u3055\u3044\u3002", "Shock was advised but not delivered. Confirm nobody is touching the patient first.");
        private string GetReviewGoodRun() => LanguageManager.Get("\u5173\u952e\u6b65\u9aa4\u5b8c\u6210\u826f\u597d\uff0c\u4e0b\u4e00\u8f6e\u7ee7\u7eed\u4fdd\u6301\u8282\u594f\u548c\u6df1\u5ea6\u3002", "\u91cd\u8981\u624b\u9806\u306f\u826f\u597d\u3067\u3059\u3002\u6b21\u3082\u30c6\u30f3\u30dd\u3068\u6df1\u3055\u3092\u7dad\u6301\u3057\u3066\u304f\u3060\u3055\u3044\u3002", "Key steps were solid. Keep the same rhythm and depth next run.");

        private void BuildCompletionUIIfNeeded()
        {
            if (_completionUIBuilt)
                return;

            if (completionPanel == null)
            {
                Debug.LogWarning("[CPRTraining] completionPanel is missing. Falling back to instructionText for completion UI.");
                return;
            }

            var panelRt = completionPanel.GetComponent<RectTransform>();
            if (panelRt == null)
                panelRt = completionPanel.AddComponent<RectTransform>();

            // 优先使用场景中已有的结果文本（避免创建重复的 CPR_ResultText 造成遮挡）
            if (_completionTextRuntime == null)
            {
                _completionTextRuntime = completionResultText;
                if (_completionTextRuntime == null)
                {
                    var allTexts = completionPanel.GetComponentsInChildren<Text>(true);
                    foreach (var t in allTexts)
                    {
                        string n = t.gameObject.name.ToLower();
                        if (n.Contains("result") && !n.Contains("cpr_"))
                        { _completionTextRuntime = t; break; }
                    }
                }
                if (_completionTextRuntime == null)
                {
                    GameObject textGo = new GameObject("CPR_ResultText", typeof(RectTransform), typeof(Text));
                    textGo.transform.SetParent(panelRt, false);
                    var rt = textGo.GetComponent<RectTransform>();
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(900f, 360f);
                    rt.anchoredPosition = new Vector2(0f, 90f);
                    var txt = textGo.GetComponent<Text>();
                    txt.text = "トレーニング結果 / 训练结果";
                    txt.alignment = TextAnchor.UpperCenter;
                    txt.fontSize = 40;
                    txt.resizeTextForBestFit = true;
                    txt.resizeTextMinSize = 22;
                    txt.resizeTextMaxSize = 40;
                    txt.color = Color.white;
                    txt.horizontalOverflow = HorizontalWrapMode.Wrap;
                    txt.verticalOverflow = VerticalWrapMode.Overflow;
                    _completionTextRuntime = txt;
                }
            }

            // 优先使用场景中已有的按钮（避免创建CPR_ReplayButton/CPR_ExitButton 造成重复色块遮挡）
            VRTrainingUIStyle.ApplyPanel(completionPanel, true);
            if (_completionTextRuntime != null)
            {
                ConfigureRuntimeText(
                    _completionTextRuntime,
                    new Vector2(0f, 88f),
                    new Vector2(1040f, 430f),
                    27,
                    TextAnchor.UpperLeft,
                    UIColorScheme.TextPrimary
                );
            }

            if (_completionRestartButtonRuntime == null)
            {
                _completionRestartButtonRuntime = restartButton;
                if (_completionRestartButtonRuntime == null)
                {
                    var btn = FindButtonByNameInPanel("RestartButton");
                    if (btn != null) _completionRestartButtonRuntime = btn;
                }
                if (_completionRestartButtonRuntime != null)
                {
                    _completionRestartButtonRuntime.onClick.RemoveAllListeners();
                    _completionRestartButtonRuntime.onClick.AddListener(ReplayLastTraining);
                    VRTrainingUIStyle.ApplyButton(_completionRestartButtonRuntime, UIColorScheme.Primary);
                }
                else
                {
                    var btnGo = CreateCompletionButton(panelRt, "CPR_ReplayButton", CPRTextLibrary.BtnRestart,
                        new Vector2(-220f, -235f), new Color(0.1f, 0.6f, 0.9f, 0.9f), ReplayLastTraining);
                    _completionRestartButtonRuntime = btnGo.GetComponent<Button>();
                }
            }

            if (_completionExitButtonRuntime == null)
            {
                _completionExitButtonRuntime = exitButton;
                if (_completionExitButtonRuntime == null)
                {
                    var btn = FindButtonByNameInPanel("ExitButton");
                    if (btn != null) _completionExitButtonRuntime = btn;
                }
                if (_completionExitButtonRuntime != null)
                {
                    _completionExitButtonRuntime.onClick.RemoveAllListeners();
                    _completionExitButtonRuntime.onClick.AddListener(ExitToMainMenu);
                    VRTrainingUIStyle.ApplyButton(_completionExitButtonRuntime, UIColorScheme.PrimaryDark);
                }
                else
                {
                    var btnGo = CreateCompletionButton(panelRt, "CPR_ExitButton", CPRTextLibrary.BtnExit,
                        new Vector2(220f, -235f), new Color(0.2f, 0.2f, 0.2f, 0.95f), ExitToMainMenu);
                    _completionExitButtonRuntime = btnGo.GetComponent<Button>();
                }
            }

            _completionViewRuntime = completionPanel.GetComponent<TrainingResultPanelView>();
            if (_completionViewRuntime == null)
                _completionViewRuntime = completionPanel.AddComponent<TrainingResultPanelView>();
            _completionViewRuntime.Bind(_completionTextRuntime, _completionRestartButtonRuntime, _completionExitButtonRuntime);
            _completionViewRuntime.ConfigureActions(ReplayLastTraining, ExitToMainMenu);
            _completionViewRuntime.RefreshLabels(CPRTextLibrary.BtnRestart, CPRTextLibrary.BtnExit);

            _completionUIBuilt = true;
        }

        private Button FindButtonByNameInPanel(string name)
        {
            if (completionPanel == null) return null;
            var btns = completionPanel.GetComponentsInChildren<Button>(true);
            foreach (var b in btns)
            {
                if (b.gameObject.name == name) return b;
            }
            return null;
        }

        private GameObject CreateCompletionButton(Transform parent, string name, string label, Vector2 pos, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);
            var rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(320f, 90f);
            rt.anchoredPosition = pos;
            var img = btnGo.GetComponent<Image>();
            img.color = color;
            var btn = btnGo.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);
            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(btnGo.transform, false);
            var lrt = labelGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            var ltxt = labelGo.GetComponent<Text>();
            ltxt.text = label;
            ltxt.alignment = TextAnchor.MiddleCenter;
            ltxt.fontSize = 28;
            ltxt.color = Color.white;
            VRTrainingUIStyle.ApplyText(ltxt, UIColorScheme.TextPrimary);
            VRTrainingUIStyle.ApplyButton(btn, color);
            return btnGo;
        }

        private void ChangePhase(TrainingPhase newPhase)
        {
            currentPhase = newPhase;
            OnPhaseChanged?.Invoke(newPhase);

            // 集中在这里起停节拍器：所有进入/离开按压阶段的路径都经过 ChangePhase，
            // 包括正常完成、中途退出和重新开始，不会有漏掉的分支。
            if (_metronome != null)
                _metronome.SetRunning(newPhase == TrainingPhase.CPRInProgress);

            if (phaseText != null)
            {
                phaseText.text = GetPhaseDisplayName(newPhase);
            }

            Debug.Log($"[CPRTraining] 阶段: {newPhase}");
        }

        private string GetPhaseDisplayName(TrainingPhase phase)
        {
            return phase switch
            {
                TrainingPhase.Intro          => CPRTextLibrary.PhaseIntro,
                TrainingPhase.WaitingStart   => GetTrainingModeSelectionTitleV2(),
                TrainingPhase.PatientFalling => CPRTextLibrary.PhaseFalling,
                TrainingPhase.CallingHelp    => GetCallingHelpPhaseDisplayName(),
                TrainingPhase.AEDInProgress  => GetAEDPhaseDisplayName(),
                TrainingPhase.Quiz           => GetQuizButtonLabel(),
                TrainingPhase.ReadyForCPR    => CPRTextLibrary.PhaseReadyCPR,
                TrainingPhase.CPRInProgress  => CPRTextLibrary.PhaseCPRInProgress,
                TrainingPhase.Completed      => CPRTextLibrary.PhaseCompleted,
                _                            => ""
            };
        }

        /// <summary>
        /// 合并 CPR HUD 的一个元素在容器里占的位置、大小和字号。
        /// </summary>
        private readonly struct HudSlot
        {
            public readonly float X;
            public readonly float Y;
            public readonly float W;
            public readonly float H;
            public readonly int FontSize;

            public HudSlot(float x, float y, float w, float h, int fontSize)
            {
                X = x; Y = y; W = w; H = h; FontSize = fontSize;
            }

            public Vector2 Pos => new Vector2(X, Y);
            public Vector2 Size => new Vector2(W, H);
        }

        /// <summary>
        /// 合并 CPR HUD 的布局表。
        ///
        /// 这些数字原来散在 13 处 EnsureRuntimeText / ConfigureRuntimeText 调用里，
        /// 想整体上移、改行距、或者换一版排版就得逐个去找。集中到这里之后，
        /// 三段式结构也变得能一眼看出来：
        ///
        ///   页头（标题/指导/教练提示）行距 ~80-100
        ///   指标（深度、频率各自的 标题|数值 / 进度条 / 图例）行距 ~44-46
        ///   页脚（质量、次数|节拍、手位）行距 ~32-38
        ///
        /// 坐标以容器中心为原点，y 向上为正。整宽元素 x=0；
        /// 左右并排的两个元素用 ±x（如深度标题在左、深度数值在右）。
        /// 进度条本身的 y 见 DepthTrackY / RateTrackY，宽度用
        /// MergedCprDepthBarWidth / MergedCprRateBarWidth 两个既有常量。
        /// </summary>
        private static class HudLayout
        {
            //                                    x        y       w      h  font
            // ---- 页头 ----
            public static readonly HudSlot Title       = new HudSlot(     0f,    300f,   800f,   52f, 34);
            public static readonly HudSlot Instruction = new HudSlot(     0f,    218f,   790f,   86f, 22);
            public static readonly HudSlot CoachTip    = new HudSlot(     0f,    118f,   790f,   78f, 22);

            // ---- 指标 ----
            public static readonly HudSlot DepthTitle  = new HudSlot(  -250f,     38f,   220f,   32f, 20);
            public static readonly HudSlot DepthValue  = new HudSlot(   205f,     38f,   360f,   32f, 20);
            public static readonly HudSlot DepthLegend = new HudSlot(     0f,    -52f,   620f,   30f, 17);
            public static readonly HudSlot RateTitle   = new HudSlot(  -250f,    -96f,   220f,   32f, 20);
            public static readonly HudSlot RateValue   = new HudSlot(   205f,    -96f,   360f,   32f, 20);
            public static readonly HudSlot RateLegend  = new HudSlot(     0f,   -186f,   650f,   30f, 17);

            // ---- 页脚 ----
            public static readonly HudSlot Quality     = new HudSlot(     0f,   -218f,   720f,   28f, 18);
            public static readonly HudSlot Count       = new HudSlot(  -255f,   -252f,   320f,   38f, 22);
            public static readonly HudSlot Metronome   = new HudSlot(   175f,   -252f,   420f,   38f, 22);
            public static readonly HudSlot HandCue     = new HudSlot(     0f,   -290f,   760f,   32f, 19);

            // ---- 容器与进度条 ----
            public static readonly Vector2 ContainerPos = new Vector2(0f, 70f);
            public static readonly Vector2 ContainerSize = new Vector2(920f, 620f);
            public const float DepthTrackY = -8f;
            public const float RateTrackY = -142f;
            public const float TrackHeight = 30f;
        }

        private void BuildMergedCprHudIfNeeded()
        {
            if (trainingHUDPanel == null)
                return;

            if (_mergedCprHudBuilt && _mergedCprDepthFillRuntime != null)
                return;

            Transform parent = trainingHUDPanel.transform;

            GameObject container = FindChildGameObject(parent, "MergedCPRCoachContainer");
            if (container == null)
            {
                container = new GameObject("MergedCPRCoachContainer", typeof(RectTransform), typeof(Image), typeof(Outline));
                container.transform.SetParent(parent, false);
            }

            var containerRt = container.GetComponent<RectTransform>();
            SetRuntimeRect(containerRt, HudLayout.ContainerPos, HudLayout.ContainerSize);
            container.transform.SetAsFirstSibling();

            var containerImage = container.GetComponent<Image>();
            if (containerImage != null)
                containerImage.color = new Color(0.02f, 0.04f, 0.05f, 0.70f);

            var containerOutline = container.GetComponent<Outline>();
            if (containerOutline != null)
            {
                containerOutline.effectColor = new Color(0.2f, 0.85f, 1f, 0.18f);
                containerOutline.effectDistance = new Vector2(2f, -2f);
            }
            VRTrainingUIStyle.ApplyPanel(container, true);

            if (phaseText != null && phaseText.transform.IsChildOf(parent))
            {
                ConfigureRuntimeText(
                    phaseText,
                    HudLayout.Title.Pos,
                    HudLayout.Title.Size,
                    HudLayout.Title.FontSize,
                    TextAnchor.MiddleCenter,
                    new Color(1f, 0.92f, 0.05f, 1f)
                );
                phaseText.text = GetMergedCprTitle();
            }

            if (instructionText != null && instructionText.transform.IsChildOf(parent))
            {
                ConfigureRuntimeText(
                    instructionText,
                    HudLayout.Instruction.Pos,
                    HudLayout.Instruction.Size,
                    HudLayout.Instruction.FontSize,
                    TextAnchor.MiddleCenter,
                    Color.white
                );
            }

            _mergedCprCoachTextRuntime = EnsureRuntimeText(
                parent,
                "MergedCPRCoachText",
                HudLayout.CoachTip.Pos,
                HudLayout.CoachTip.Size,
                HudLayout.CoachTip.FontSize,
                TextAnchor.MiddleCenter,
                new Color(0.90f, 0.96f, 1f, 1f)
            );

            TMP_Text depthTitle = EnsureRuntimeText(
                parent,
                "MergedCPRDepthTitle",
                HudLayout.DepthTitle.Pos,
                HudLayout.DepthTitle.Size,
                HudLayout.DepthTitle.FontSize,
                TextAnchor.MiddleLeft,
                new Color(0.72f, 0.88f, 1f, 1f)
            );
            depthTitle.text = LanguageManager.Get(
                "\u6309\u538b\u6df1\u5ea6",
                "\u5727\u8feb\u6df1\u5ea6",
                "Compression depth"
            );

            GameObject track = FindChildGameObject(parent, "DepthBarBg");
            if (track == null)
                track = new GameObject("DepthBarBg", typeof(RectTransform), typeof(Image));
            track.transform.SetParent(parent, false);
            var trackRt = track.GetComponent<RectTransform>();
            SetRuntimeRect(trackRt, new Vector2(0f, HudLayout.DepthTrackY), new Vector2(MergedCprDepthBarWidth, 30f));

            var trackImage = track.GetComponent<Image>();
            if (trackImage == null)
                trackImage = track.AddComponent<Image>();
            VRTrainingUIStyle.ApplyDepthTrack(trackImage);

            // 合格深度区间色带。区间与条上限都取自 pressDetector，
            // 用的是和 UpdateMergedCprHud 完全相同的换算，保证色带和填充对得上。
            {
                float minGoodCm = pressDetector != null ? pressDetector.minGoodDepthCm : 5f;
                float maxGoodCm = pressDetector != null ? pressDetector.maxGoodDepthCm : 6f;
                float barMaxCm = Mathf.Max(MergedCprDepthBarMaxCm, maxGoodCm + 2f);
                EnsureGoodZoneBand(track.transform, "DepthBarGoodZone", MergedCprDepthBarWidth,
                                   minGoodCm / barMaxCm, maxGoodCm / barMaxCm);
            }

            GameObject fill = FindChildGameObject(track.transform, "DepthBarFill");
            if (fill == null)
            {
                fill = new GameObject("DepthBarFill", typeof(RectTransform), typeof(Image));
                fill.transform.SetParent(track.transform, false);
            }

            _mergedCprDepthFillRuntime = fill.GetComponent<RectTransform>();
            _mergedCprDepthFillRuntime.anchorMin = new Vector2(0f, 0f);
            _mergedCprDepthFillRuntime.anchorMax = new Vector2(0f, 1f);
            _mergedCprDepthFillRuntime.pivot = new Vector2(0f, 0.5f);
            _mergedCprDepthFillRuntime.anchoredPosition = Vector2.zero;
            _mergedCprDepthFillRuntime.sizeDelta = new Vector2(0f, 0f);

            _mergedCprDepthFillImageRuntime = fill.GetComponent<Image>();
            if (_mergedCprDepthFillImageRuntime == null)
                _mergedCprDepthFillImageRuntime = fill.AddComponent<Image>();
            VRTrainingUIStyle.ApplyDepthFill(_mergedCprDepthFillImageRuntime);

            TMP_Text depthLegend = EnsureRuntimeText(
                parent,
                "MergedCPRDepthLegend",
                HudLayout.DepthLegend.Pos,
                HudLayout.DepthLegend.Size,
                HudLayout.DepthLegend.FontSize,
                TextAnchor.MiddleCenter,
                new Color(0.82f, 0.88f, 0.92f, 1f)
            );
            depthLegend.text = LanguageManager.Get(
                "\u76ee\u6807\u6df1\u5ea6 5-6cm\u3002\u9ec4=\u8fc7\u6d45\uff0c\u7eff=\u5408\u9002\uff0c\u7ea2=\u8fc7\u6df1\u3002",
                "\u76ee\u6a19\u6df1\u5ea6 5-6cm\u3002\u9ec4=\u6d45\u3044\u3001\u7dd1=\u9069\u5207\u3001\u8d64=\u6df1\u3059\u304e\u3002",
                "Target depth 5-6 cm. Yellow shallow, green good, red too deep."
            );

            _mergedCprDepthValueRuntime = EnsureRuntimeText(
                parent,
                "MergedCPRDepthValue",
                HudLayout.DepthValue.Pos,
                HudLayout.DepthValue.Size,
                HudLayout.DepthValue.FontSize,
                TextAnchor.MiddleRight,
                Color.white
            );

            TMP_Text rateTitle = EnsureRuntimeText(
                parent,
                "MergedCPRRateTitle",
                HudLayout.RateTitle.Pos,
                HudLayout.RateTitle.Size,
                HudLayout.RateTitle.FontSize,
                TextAnchor.MiddleLeft,
                new Color(0.72f, 0.88f, 1f, 1f)
            );
            rateTitle.text = LanguageManager.Get(
                "\u6309\u538b\u9891\u7387",
                "\u5727\u8feb\u30c6\u30f3\u30dd",
                "Compression rate"
            );

            GameObject rateTrack = FindChildGameObject(parent, "RateBarBg");
            if (rateTrack == null)
                rateTrack = new GameObject("RateBarBg", typeof(RectTransform), typeof(Image));
            rateTrack.transform.SetParent(parent, false);
            var rateTrackRt = rateTrack.GetComponent<RectTransform>();
            SetRuntimeRect(rateTrackRt, new Vector2(0f, HudLayout.RateTrackY), new Vector2(MergedCprRateBarWidth, 26f));

            var rateTrackImage = rateTrack.GetComponent<Image>();
            if (rateTrackImage == null)
                rateTrackImage = rateTrack.AddComponent<Image>();
            VRTrainingUIStyle.ApplyDepthTrack(rateTrackImage);

            // 合格频率区间色带（默认 100-120/min）。频率条用的是
            // InverseLerp(60, 150)，这里沿用同一换算。
            {
                float minGoodBpm = pressDetector != null ? pressDetector.minGoodRateBPM : 100f;
                float maxGoodBpm = pressDetector != null ? pressDetector.maxGoodRateBPM : 120f;
                EnsureGoodZoneBand(rateTrack.transform, "RateBarGoodZone", MergedCprRateBarWidth,
                                   Mathf.InverseLerp(MergedCprMinRateDisplay, MergedCprMaxRateDisplay, minGoodBpm),
                                   Mathf.InverseLerp(MergedCprMinRateDisplay, MergedCprMaxRateDisplay, maxGoodBpm));
            }

            GameObject rateFill = FindChildGameObject(rateTrack.transform, "RateBarFill");
            if (rateFill == null)
            {
                rateFill = new GameObject("RateBarFill", typeof(RectTransform), typeof(Image));
                rateFill.transform.SetParent(rateTrack.transform, false);
            }

            _mergedCprRateFillRuntime = rateFill.GetComponent<RectTransform>();
            _mergedCprRateFillRuntime.anchorMin = new Vector2(0f, 0f);
            _mergedCprRateFillRuntime.anchorMax = new Vector2(0f, 1f);
            _mergedCprRateFillRuntime.pivot = new Vector2(0f, 0.5f);
            _mergedCprRateFillRuntime.anchoredPosition = Vector2.zero;
            _mergedCprRateFillRuntime.sizeDelta = new Vector2(0f, 0f);

            _mergedCprRateFillImageRuntime = rateFill.GetComponent<Image>();
            if (_mergedCprRateFillImageRuntime == null)
                _mergedCprRateFillImageRuntime = rateFill.AddComponent<Image>();
            VRTrainingUIStyle.ApplyDepthFill(_mergedCprRateFillImageRuntime);

            TMP_Text rateLegend = EnsureRuntimeText(
                parent,
                "MergedCPRRateLegend",
                HudLayout.RateLegend.Pos,
                HudLayout.RateLegend.Size,
                HudLayout.RateLegend.FontSize,
                TextAnchor.MiddleCenter,
                new Color(0.82f, 0.88f, 0.92f, 1f)
            );
            rateLegend.text = LanguageManager.Get(
                "\u76ee\u6807\u9891\u7387 100-120 \u6b21/\u5206\uff0c\u6309\u8282\u62cd\u4fdd\u6301\u7a33\u5b9a\u56de\u5f39\u3002",
                "\u76ee\u6a19\u30c6\u30f3\u30dd 100-120 \u56de/\u5206\u3002\u30e1\u30c8\u30ed\u30ce\u30fc\u30e0\u306b\u5408\u308f\u305b\u3066\u623b\u308a\u3092\u4fdd\u3061\u307e\u3059\u3002",
                "Target rate 100-120/min. Follow the beat and allow recoil."
            );

            _mergedCprRateValueRuntime = EnsureRuntimeText(
                parent,
                "MergedCPRRateValue",
                HudLayout.RateValue.Pos,
                HudLayout.RateValue.Size,
                HudLayout.RateValue.FontSize,
                TextAnchor.MiddleRight,
                Color.white
            );

            _mergedCprQualityValueRuntime = EnsureRuntimeText(
                parent,
                "MergedCPRQualityValue",
                HudLayout.Quality.Pos,
                HudLayout.Quality.Size,
                HudLayout.Quality.FontSize,
                TextAnchor.MiddleCenter,
                UIColorScheme.TextPrimary
            );

            _mergedCprCountValueRuntime = EnsureRuntimeText(
                parent,
                "MergedCPRCountValue",
                HudLayout.Count.Pos,
                HudLayout.Count.Size,
                HudLayout.Count.FontSize,
                TextAnchor.MiddleLeft,
                UIColorScheme.TextPrimary
            );

            _mergedCprMetronomeRuntime = EnsureRuntimeText(
                parent,
                "MergedCPRMetronome",
                HudLayout.Metronome.Pos,
                HudLayout.Metronome.Size,
                HudLayout.Metronome.FontSize,
                TextAnchor.MiddleRight,
                UIColorScheme.TextPrimary
            );

            _mergedCprHandCueRuntime = EnsureRuntimeText(
                parent,
                "MergedCPRHandCue",
                HudLayout.HandCue.Pos,
                HudLayout.HandCue.Size,
                HudLayout.HandCue.FontSize,
                TextAnchor.MiddleCenter,
                UIColorScheme.TextSecondary
            );

            _mergedCprHudBuilt = true;
        }

        private void UpdateMergedCprHud(float depthCm, float rateBpm, bool handInContact)
        {
            if (currentPhase != TrainingPhase.CPRInProgress || trainingHUDPanel == null)
                return;

            BuildMergedCprHudIfNeeded();

            if (phaseText != null && phaseText.transform.IsChildOf(trainingHUDPanel.transform))
                phaseText.text = GetMergedCprTitle();

            if (_mergedCprCoachTextRuntime != null)
            {
                _mergedCprCoachTextRuntime.text = GetMergedCprCoachTip();
                // 回弹告警要看得出是告警，不能和常规提示一个颜色
                _mergedCprCoachTextRuntime.color = IsRecoilWarningActive
                    ? UIColorScheme.Warning
                    : UIColorScheme.TextSecondary;
            }

            if (_mergedCprDepthFillRuntime != null)
            {
                float maxDepth = pressDetector != null
                    ? Mathf.Max(MergedCprDepthBarMaxCm, pressDetector.maxGoodDepthCm + 2f)
                    : MergedCprDepthBarMaxCm;
                float normalized = Mathf.Clamp01(depthCm / Mathf.Max(0.1f, maxDepth));
                _mergedCprDepthFillRuntime.sizeDelta = new Vector2(MergedCprDepthBarWidth * normalized, 0f);
            }

            Color depthColor = GetMergedCprDepthColor(depthCm, handInContact);
            if (_mergedCprDepthFillImageRuntime != null)
                _mergedCprDepthFillImageRuntime.color = depthColor;

            if (_mergedCprDepthValueRuntime != null)
            {
                _mergedCprDepthValueRuntime.color = depthColor;
                _mergedCprDepthValueRuntime.text = $"{depthCm:F1} cm  {GetMergedCprDepthFeedback(depthCm, handInContact)}";
            }

            if (_mergedCprRateFillRuntime != null)
            {
                float normalizedRate = Mathf.InverseLerp(MergedCprMinRateDisplay, MergedCprMaxRateDisplay, Mathf.Max(0f, rateBpm));
                _mergedCprRateFillRuntime.sizeDelta = new Vector2(MergedCprRateBarWidth * Mathf.Clamp01(normalizedRate), 0f);
            }

            Color rateColor = GetMergedCprRateColor(rateBpm);
            if (_mergedCprRateFillImageRuntime != null)
                _mergedCprRateFillImageRuntime.color = rateColor;

            if (_mergedCprRateValueRuntime != null)
            {
                _mergedCprRateValueRuntime.color = rateColor;
                _mergedCprRateValueRuntime.text = $"{rateBpm:F0}/min  {GetMergedCprRateFeedback(rateBpm)}";
            }

            if (_mergedCprQualityValueRuntime != null)
            {
                float depthHitRate = CalculateHitPercent(_goodDepthSamples, _compressionSamples);
                float rateHitRate = _rateSamples > 0 ? CalculateHitPercent(_goodRateSamples, _rateSamples) : 0f;
                bool strongStreak = _currentQualityStreak >= 5;
                _mergedCprQualityValueRuntime.color = strongStreak ? UIColorScheme.Success : UIColorScheme.TextPrimary;

                // \u8fd9\u51e0\u4e2a\u6570\u5b57\u53ea\u5728\u6bcf\u6b21\u6309\u538b\u7ed3\u675f\u540e\u53d8\uff08\u7ea6 2 \u6b21/\u79d2\uff09\uff0c\u800c\u672c\u65b9\u6cd5\u6bcf\u5e27\u90fd\u8dd1\uff0c
                // Quest \u4e0a\u662f 72-90 \u5e27/\u79d2 \u2014\u2014 \u6ca1\u53d8\u5c31\u4e0d\u91cd\u5efa\u5b57\u7b26\u4e32\u3002
                bool hadRate = _rateSamples > 0;
                if (!Mathf.Approximately(depthHitRate, _lastQualityDepthPct) ||
                    !Mathf.Approximately(rateHitRate, _lastQualityRatePct) ||
                    _currentQualityStreak != _lastQualityStreak ||
                    hadRate != _lastQualityHadRate)
                {
                    _lastQualityDepthPct = depthHitRate;
                    _lastQualityRatePct = rateHitRate;
                    _lastQualityStreak = _currentQualityStreak;
                    _lastQualityHadRate = hadRate;

                    // LanguageManager.Get \u662f\u666e\u901a\u65b9\u6cd5\uff0c\u4e09\u4e2a\u5b9e\u53c2\u90fd\u4f1a\u5148\u6c42\u503c\u3002
                    // \u539f\u6765\u8fd9\u91cc\u4e09\u79cd\u8bed\u8a00\u5404\u63d2\u503c\u4e00\u904d\u3001\u53ea\u7559\u4e00\u4e2a\uff0c\u53e6\u4e24\u4e2a\u76f4\u63a5\u4e22\u6389\u3002
                    // \u6539\u6210\u5148\u53d6\u683c\u5f0f\u4e32\uff08\u7eaf\u5b57\u9762\u91cf\uff0c\u4e0d\u5206\u914d\uff09\u518d\u683c\u5f0f\u5316\u4e00\u6b21\u3002
                    string ratePart = hadRate ? rateHitRate.ToString("F0") + "%" : "--";
                    _mergedCprQualityValueRuntime.text = string.Format(
                        LanguageManager.Get(
                            "\u8fbe\u6807\uff1a\u6df1\u5ea6 {0:F0}% / \u9891\u7387 {1}  \u8fde\u7eed {2}",
                            "\u5408\u683c\uff1a\u6df1\u3055 {0:F0}% / \u30c6\u30f3\u30dd {1}  \u9023\u7d9a {2}",
                            "Hit: depth {0:F0}% / rate {1}  Streak {2}"),
                        depthHitRate, ratePart, _currentQualityStreak);
                }
            }

            if (_mergedCprCountValueRuntime != null)
            {
                // \u540c\u7406\uff1a\u6b21\u6570\u53ea\u5728\u6309\u538b\u65f6\u53d8
                if (compressionCount != _lastCountValue || targetCompressions != _lastCountTarget)
                {
                    _lastCountValue = compressionCount;
                    _lastCountTarget = targetCompressions;

                    _mergedCprCountValueRuntime.text = string.Format(
                        LanguageManager.Get(
                            "\u8fdb\u5ea6\uff1a{0}/{1}",
                            "\u9032\u6357\uff1a{0}/{1}",
                            "Progress: {0}/{1}"),
                        compressionCount, targetCompressions);
                }
            }

            if (_mergedCprMetronomeRuntime != null)
            {
                // \u8282\u62cd\u6807\u8bb0\u8ddf\u7740\u8282\u594f\u52a8\uff0c\u8df3\u8fc7\u68c0\u6d4b\u610f\u4e49\u4e0d\u5927\uff1b\u4f46\u4e09\u8bed\u63d2\u503c\u7684\u6d6a\u8d39\u7167\u6837\u8981\u53bb\u6389
                string beatMarker = GetMergedCprBeatMarker();
                _mergedCprMetronomeRuntime.text = string.Format(
                    LanguageManager.Get(
                        "\u8282\u62cd {0} 100-120/min",
                        "\u62cd\u5b50 {0} 100-120/min",
                        "Beat {0} 100-120/min"),
                    beatMarker);
            }

            if (_mergedCprHandCueRuntime != null)
            {
                _mergedCprHandCueRuntime.color = handInContact ? UIColorScheme.Success : UIColorScheme.Warning;
                _mergedCprHandCueRuntime.text = GetMergedCprHandCue(handInContact);
            }
        }

        private string GetMergedCprTitle()
        {
            return LanguageManager.Get(
                "CPR\u5b9e\u65bd\u4e2d / CPR\u6559\u7ec3",
                "CPR\u5b9f\u65bd\u4e2d / CPR \u30b3\u30fc\u30c1",
                "CPR In Progress / Coach"
            );
        }

        /// <summary>上一次按压回弹不足时，教练提示要让位给回弹告警。</summary>
        private bool IsRecoilWarningActive =>
            pressDetector != null && pressDetector.LastRecoilIncomplete;

        private string GetMergedCprCoachTip()
        {
            // 回弹不足优先。下面那几条按次数推进的提示里本来就写着「让胸部回弹」，
            // 但那只是固定台词，学员有没有真的回弹它并不知道。
            // 检测器现在能测出来了，就该在真的没回弹时才喊，并说清差多少。
            // LastRecoilIncomplete 会保持到下一次按压结束，所以这条提示会自然消失。
            if (IsRecoilWarningActive)
            {
                // 同样避开三语各插值一遍：本方法在回弹告警期间每帧都会被调用
                return string.Format(
                    LanguageManager.Get(
                        "回弹不足（胸部仍下压 {0:F1}cm）：压完把手完全松开，不要压着胸口。",
                        "リコイル不足（胸が {0:F1}cm 沈んだまま）：圧迫後は手を完全に離し、体重を預けない。",
                        "Incomplete recoil ({0:F1}cm still depressed): release fully after each compression; do not lean."),
                    pressDetector.LastRecoilDeficitCm);
            }

            if (compressionCount < 10)
            {
                return LanguageManager.Get(
                    "\u524d10\u6b21\uff1a\u53cc\u80a9\u5728\u624b\u638c\u6b63\u4e0a\u65b9\uff0c\u5148\u627e\u5230\u7a33\u5b9a\u8282\u594f\u3002",
                    "\u6700\u521d\u306e10\u56de\uff1a\u80a9\u3092\u624b\u306e\u771f\u4e0a\u306b\u7f6e\u304d\u3001\u30ea\u30ba\u30e0\u3092\u5b89\u5b9a\u3055\u305b\u307e\u3059\u3002",
                    "First 10: keep shoulders above hands and establish a steady rhythm."
                );
            }

            if (compressionCount < 20)
            {
                return LanguageManager.Get(
                    "\u4e2d\u6bb5\uff1a\u6bcf\u6b21\u6309\u538b\u540e\u8ba9\u80f8\u90e8\u56de\u5f39\uff0c\u4e0d\u8981\u505c\u987f\u3002",
                    "\u4e2d\u76e4\uff1a\u6bce\u56de\u306e\u5727\u8feb\u5f8c\u306b\u80f8\u3092\u623b\u3057\u3001\u6b62\u307e\u3089\u306a\u3044\u3002",
                    "Middle set: let the chest recoil after each compression; do not pause."
                );
            }

            return LanguageManager.Get(
                "\u6700\u540e\u9636\u6bb5\uff1a\u4fdd\u6301\u8282\u594f\uff0c\u5b8c\u621030\u6b21\u540e\u67e5\u770b\u590d\u76d8\u3002",
                "\u6700\u7d42\u6bb5\u968e\uff1a\u30ea\u30ba\u30e0\u3092\u4fdd\u3061\u300130\u56de\u5b8c\u4e86\u5f8c\u306b\u632f\u308a\u8fd4\u308a\u307e\u3059\u3002",
                "Final set: hold the rhythm and review results after 30 compressions."
            );
        }

        private string GetMergedCprDepthFeedback(float depthCm, bool handInContact)
        {
            float minGood = pressDetector != null ? pressDetector.minGoodDepthCm : 5f;
            float maxGood = pressDetector != null ? pressDetector.maxGoodDepthCm : 6f;

            if (!handInContact && depthCm < 0.2f)
            {
                return LanguageManager.Get(
                    "\u624b\u638c\u653e\u5230\u80f8\u53e3",
                    "\u624b\u3092\u80f8\u306b\u7f6e\u304f",
                    "Place hands"
                );
            }

            if (depthCm < minGood)
            {
                return LanguageManager.Get(
                    "\u518d\u6df1\u4e00\u70b9",
                    "\u3082\u3046\u5c11\u3057\u6df1\u304f",
                    "Press deeper"
                );
            }

            if (depthCm <= maxGood)
            {
                return LanguageManager.Get(
                    "\u6df1\u5ea6\u5408\u9002",
                    "\u6df1\u3055\u9069\u5207",
                    "Good depth"
                );
            }

            return LanguageManager.Get(
                "\u592a\u6df1\uff0c\u51cf\u8f7b\u529b\u5ea6",
                "\u6df1\u3059\u304e\u3001\u529b\u3092\u6291\u3048\u308b",
                "Too deep"
            );
        }

        private Color GetMergedCprDepthColor(float depthCm, bool handInContact)
        {
            if (!handInContact && depthCm < 0.2f)
                return new Color(0.65f, 0.72f, 0.78f, 1f);

            float minGood = pressDetector != null ? pressDetector.minGoodDepthCm : 5f;
            float maxGood = pressDetector != null ? pressDetector.maxGoodDepthCm : 6f;

            if (depthCm < minGood)
                return new Color(1f, 0.78f, 0.18f, 1f);

            if (depthCm <= maxGood)
                return new Color(0.18f, 0.88f, 0.36f, 1f);

            return new Color(1f, 0.24f, 0.22f, 1f);
        }

        private string GetMergedCprRateFeedback(float rateBpm)
        {
            float minGood = pressDetector != null ? pressDetector.minGoodRateBPM : 100f;
            float maxGood = pressDetector != null ? pressDetector.maxGoodRateBPM : 120f;

            if (rateBpm <= 0.1f)
            {
                return LanguageManager.Get(
                    "\u7b49\u5f85\u9891\u7387",
                    "\u30c6\u30f3\u30dd\u5f85\u3061",
                    "Waiting"
                );
            }

            if (rateBpm < minGood)
            {
                return LanguageManager.Get(
                    "\u518d\u5feb\u4e00\u70b9",
                    "\u3082\u3046\u5c11\u3057\u901f\u304f",
                    "Faster"
                );
            }

            if (rateBpm <= maxGood)
            {
                return LanguageManager.Get(
                    "\u8282\u594f\u5408\u9002",
                    "\u30c6\u30f3\u30dd\u9069\u5207",
                    "Good rate"
                );
            }

            return LanguageManager.Get(
                "\u7a0d\u6162\u4e00\u70b9",
                "\u5c11\u3057\u3086\u3063\u304f\u308a",
                "Slower"
            );
        }

        private Color GetMergedCprRateColor(float rateBpm)
        {
            if (rateBpm <= 0.1f)
                return new Color(0.65f, 0.72f, 0.78f, 1f);

            float minGood = pressDetector != null ? pressDetector.minGoodRateBPM : 100f;
            float maxGood = pressDetector != null ? pressDetector.maxGoodRateBPM : 120f;

            if (rateBpm < minGood)
                return UIColorScheme.Warning;

            if (rateBpm <= maxGood)
                return UIColorScheme.Success;

            return UIColorScheme.Danger;
        }

        private string GetMergedCprHandCue(bool handInContact)
        {
            return handInContact
                ? LanguageManager.Get(
                    "\u624b\u4f4d\u5df2\u63a5\u89e6\uff1a\u4fdd\u6301\u53cc\u624b\u91cd\u53e0\uff0c\u624b\u8098\u6253\u76f4\u3002",
                    "\u624b\u306e\u63a5\u89e6\u3092\u691c\u51fa\uff1a\u624b\u3092\u91cd\u306d\u3001\u8098\u3092\u4f38\u3070\u3057\u307e\u3059\u3002",
                    "Hand contact detected: stack hands and keep elbows straight."
                )
                : LanguageManager.Get(
                    "\u628a\u53cc\u624b\u53e0\u653e\u5728\u80f8\u9aa8\u4e2d\u592e\uff0c\u5782\u76f4\u5411\u4e0b\u6309\u538b\u3002",
                    "\u4e21\u624b\u3092\u80f8\u9aa8\u306e\u4e2d\u592e\u306b\u91cd\u306d\u3001\u771f\u4e0b\u306b\u5727\u8feb\u3057\u307e\u3059\u3002",
                    "Place stacked hands on the center of the sternum and press straight down."
                );
        }

        private string GetMergedCprBeatMarker()
        {
            float targetBpm = 110f;
            if (pressDetector != null)
                targetBpm = Mathf.Clamp((pressDetector.minGoodRateBPM + pressDetector.maxGoodRateBPM) * 0.5f, 100f, 120f);

            float beatPhase = Mathf.Repeat(Time.time * targetBpm / 60f, 1f);
            return beatPhase < 0.18f ? "●" : "○";
        }

        /// <summary>
        /// runtime HUD 的文本统一出口，现在产出 TextMeshProUGUI。
        ///
        /// 迁到 TMP 的原因：legacy Text 是位图图集，按固定像素尺寸烘一次再缩放，
        /// VR 距离下发虚；TMP 是 SDF，分辨率无关。
        /// 前提是字体资源里必须有 CJK 字形 —— legacy Text 靠 OS 字体回退显示日文，
        /// TMP 没有这个机制，所以先有 Bake CJK TMP Fonts 才能有这一步。
        ///
        /// 已存在的旧 legacy Text 子物体会被就地替换成 TMP（见 SwapLegacyTextComponent），
        /// 这样重复进出训练不会留下混排。
        /// </summary>
        private TMP_Text EnsureRuntimeText(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor alignment, Color color)
        {
            GameObject textObject = FindChildGameObject(parent, name);
            if (textObject == null)
            {
                textObject = new GameObject(name, typeof(RectTransform));
                textObject.transform.SetParent(parent, false);
            }

            TMP_Text text = SwapLegacyTextComponent(textObject);

            ConfigureRuntimeText(text, anchoredPosition, size, fontSize, alignment, color);
            return text;
        }

        /// <summary>
        /// 拿到物体上的 TMP_Text；如果上面还挂着迁移前的 legacy Text，先删掉它。
        /// 同一个物体上同时存在 Text 和 TextMeshProUGUI 会各画一遍，文字会重影。
        /// </summary>
        private TMP_Text SwapLegacyTextComponent(GameObject textObject)
        {
            Text legacy = textObject.GetComponent<Text>();
            if (legacy != null)
                Destroy(legacy);

            TMP_Text text = textObject.GetComponent<TMP_Text>();
            if (text == null)
                text = textObject.AddComponent<TextMeshProUGUI>();

            return text;
        }

        /// <summary>TextAnchor（legacy）到 TextAlignmentOptions（TMP）的映射。</summary>
        private static TextAlignmentOptions ToTmpAlignment(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
                case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
                default: return TextAlignmentOptions.Center;
            }
        }

        /// <summary>
        /// TMP 版。参数仍然收 TextAnchor，这样 21 个调用点一行都不用改。
        /// </summary>
        private void ConfigureRuntimeText(TMP_Text text, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor alignment, Color color)
        {
            if (text == null)
                return;

            SetRuntimeRect(text.transform as RectTransform, anchoredPosition, size);

            text.fontSize = fontSize;
            text.alignment = ToTmpAlignment(alignment);
            text.color = color;
            text.richText = true;

            // legacy 的 resizeTextForBestFit / resizeTextMin|MaxSize 对应 TMP 的自动字号
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(10, fontSize - 8);
            text.fontSizeMax = fontSize;

            // legacy 的 horizontalOverflow=Wrap / verticalOverflow=Overflow
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Overflow;

            VRTrainingUIStyle.ApplyText(text, color, fontSize >= 30);
        }

        private void ConfigureRuntimeText(Text text, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor alignment, Color color)
        {
            if (text == null)
                return;

            RectTransform rt = text.transform as RectTransform;
            SetRuntimeRect(rt, anchoredPosition, size);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.supportRichText = true;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(10, fontSize - 8);
            text.resizeTextMaxSize = fontSize;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            VRTrainingUIStyle.ApplyText(text, color, fontSize >= 30);
        }

        private void SetRuntimeRect(RectTransform rt, Vector2 anchoredPosition, Vector2 size)
        {
            if (rt == null)
                return;

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;
        }

        /// <summary>
        /// 在进度条底板上画出「合格区间」色带。
        /// 之前学员只看到一个数字，并不知道 5cm 和 6cm 的边界落在条的哪个位置；
        /// 有了色带就能直接看出自己在区间内还是外 —— 教学价值大于美观。
        /// 用 SetAsFirstSibling 压到填充条下面，这样填充仍然画在色带之上。
        /// </summary>
        private void EnsureGoodZoneBand(Transform track, string bandName, float barWidth,
                                        float normalizedStart, float normalizedEnd)
        {
            if (track == null)
                return;

            var band = FindChildGameObject(track, bandName);
            if (band == null)
            {
                band = new GameObject(bandName, typeof(RectTransform), typeof(Image));
                band.transform.SetParent(track, false);
            }

            float x0 = barWidth * Mathf.Clamp01(normalizedStart);
            float x1 = barWidth * Mathf.Clamp01(normalizedEnd);

            var rt = band.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(x0, 0f);
            rt.sizeDelta = new Vector2(Mathf.Max(0f, x1 - x0), 0f);

            var img = band.GetComponent<Image>();
            if (img == null)
                img = band.AddComponent<Image>();
            img.sprite = null;
            img.type = Image.Type.Simple;
            // 半透明绿：填充条压上来之后两者仍然分得清
            img.color = new Color(UIColorScheme.Success.r, UIColorScheme.Success.g,
                                  UIColorScheme.Success.b, 0.22f);
            img.raycastTarget = false;

            band.transform.SetAsFirstSibling();
        }

        private GameObject FindChildGameObject(Transform parent, string childName)
        {
            if (parent == null)
                return null;

            foreach (Transform child in parent)
            {
                if (child.name == childName)
                    return child.gameObject;

                GameObject nested = FindChildGameObject(child, childName);
                if (nested != null)
                    return nested;
            }

            return null;
        }

        private void UpdateInstruction(string text)
        {
            if (instructionText != null)
                instructionText.text = text;
        }

        private void ShowPanel(GameObject panel)
        {
            EnsureUIRootActive();
            HideAllPanels();
            if (panel != null)
                panel.SetActive(true);
            // 完成界面时隐藏语言选择按钮（避免遮挡结果显示和操作按钮）
            if (_languageSelectPanel != null)
                _languageSelectPanel.SetActive(panel != completionPanel);
        }

        private void HideAllPanels()
        {
            if (introPanel != null) introPanel.SetActive(false);
            if (startTrainingPanel != null) startTrainingPanel.SetActive(false);
            if (trainingHUDPanel != null) trainingHUDPanel.SetActive(false);
            if (completionPanel != null) completionPanel.SetActive(false);
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        /// <summary>
        /// 重新开始训练：清理当前状态，将患者恢复为站立，回到Intro 流程。
        /// </summary>
        public void RestartTraining()
        {
            RestartTrainingInternal(TrainingMode.None);
        }

        private void ReplayLastTraining()
        {
            TrainingMode mode = _lastCompletedTrainingMode == TrainingMode.None
                ? ResolveSelectedTrainingMode()
                : _lastCompletedTrainingMode;

            RestartTrainingInternal(mode);
        }

        private void RestartTrainingInternal(TrainingMode modeToStart)
        {
            Debug.Log("[CPRTraining] Restart training");

            StopAllCoroutines();

            ResetSessionMetrics();
            selectedTrainingMode = TrainingMode.None;
            _lastCompletedTrainingMode = modeToStart;

            if (pressDetector != null)
                pressDetector.ResetStats();

            if (chestDetectionZone != null)
            {
                Destroy(chestDetectionZone);
                chestDetectionZone = null;
            }

            lockPatientPosition = false;

            if (hapticController != null)
                hapticController.StopAllFeedback();

            if (patientModel != null)
            {
                patientModel.transform.position = patientStandPosition;
                patientModel.transform.rotation = patientStandRotation;

                var rb = patientModel.GetComponent<Rigidbody>();
                if (rb != null)
                    Destroy(rb);

                if (patientAnimator != null)
                {
                    patientAnimator.enabled = true;
                    patientAnimator.speed = 0f;
                    patientAnimator.applyRootMotion = false;
                    patientAnimator.Rebind();
                    patientAnimator.Update(0f);
                    patientAnimator.Play("Idle", 0, 0f);
                }
            }

            RestoreCameraToInitial();
            RestoreUIRootToInitial();
            HideAllPanels();

            if (modeToStart == TrainingMode.None)
                StartCoroutine(StartIntroSequence());
            else
                BeginTraining(modeToStart);
        }

        public void ExitToMainMenu()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        /// <summary>
        /// 公开入口：中止当前训练并返回初始（开始训练）界面。
        /// 供 AED 等其它流程调用。
        /// </summary>
        public void ReturnToStartMenu()
        {
            AbortCurrentTraining();
        }

        private void OnDestroy()
        {
            lockPatientPosition = false;
            if (pressDetector != null)
                pressDetector.OnCompressionComplete -= OnCompressionDetected;
            if (LanguageManager.Instance != null)
                LanguageManager.Instance.OnLanguageChanged -= RefreshAllTrainingUI;
        }
    }
}
