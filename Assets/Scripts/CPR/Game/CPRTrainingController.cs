using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using CPRSimulation;

namespace CPRSimulation.Game
{
    /// <summary>
    /// CPR璁粌娴佺▼鎺у埗鍣?- 鎺у埗璁粌鍦烘櫙涓殑瀹屾暣娴佺▼
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
        public GameObject introPanel;           // 浠嬬粛闈㈡澘
        public GameObject startTrainingPanel;   // 寮€濮嬭缁冮潰鏉?
        public GameObject trainingHUDPanel;     // 璁粌涓殑HUD
        public GameObject completionPanel;      // 瀹屾垚闈㈡澘

        [Header("UI\u6309\u94ae")]
        public Button startScenarioButton;      // 寮€濮嬫儏鏅寜閽?
        public Button startCPRButton;           // 寮€濮婥PR鎸夐挳
        public Button restartButton;            // 閲嶆柊寮€濮嬫寜閽?
        public Button exitButton;               // 閫€鍑烘寜閽紙瀹屾垚闈㈡澘锛?
        [Tooltip("\u5f00\u59cb\u8bad\u7ec3\u9762\u677f\u4e0a\u7684\u7d42\u4e86\u6309\u94ae\uff0c\u70b9\u51fb\u8fd4\u56de\u4e3b\u83dc\u5355")]
        public Button exitTrainingButton;       // 绲備簡鎸夐挳锛堝紑濮嬮潰鏉匡級
        [Tooltip("CPR \u8fdb\u884c\u4e2d HUD \u4e0a\u7684\u4e2d\u9014\u9000\u51fa\u6309\u94ae\uff0c\u70b9\u51fb\u8fd4\u56de\u5f00\u59cb\u754c\u9762")]
        public Button hudExitButton;            // HUD 涓€旈€€鍑烘寜閽?

        [Header("UI\u6587\u672c")]
        public Text phaseText;                  // 闃舵鎻愮ず鏂囨湰
        public Text instructionText;            // 鎸囧鏂囨湰
        public Text timerText;                  // 璁℃椂鍣?
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
        public Vector3 teleportTargetPosition = new Vector3(-0.26f, 0.103f, 2.5f);
        [Tooltip("\u4f20\u9001\u540e\u7684\u56fa\u5b9a\u65cb\u8f6c\uff08\u6b27\u62c9\u89d2 X,Y,Z\uff09")]
        public Vector3 teleportTargetRotation = new Vector3(15.941f, 0f, 0f);

        public enum TrainingMode
        {
            None,
            CPR,
            AED
        }

        public enum TrainingPhase
        {
            Intro,
            WaitingStart,
            PatientFalling,
            AEDInProgress,
            ReadyForCPR,
            CPRInProgress,
            Completed
        }

        public TrainingMode SelectedTrainingMode => selectedTrainingMode;
        public bool IsAEDTrainingSelected => ResolveSelectedTrainingMode() == TrainingMode.AED;

        // 浜嬩欢
        public static event System.Action<TrainingPhase> OnPhaseChanged;
        public static event System.Action OnPatientFell;
        public static event System.Action OnCPRStarted;
        public static event System.Action<int, int> OnCompressionProgress; // (current, target)

        private float trainingStartTime;
        private int compressionCount;
        // 鎸夊帇璐ㄩ噺缁熻锛堢敤浜庡畬鎴愮晫闈㈣瘎浠凤級
        private float _sumDepthCm;
        private float _sumRateBpm;
        private int _compressionSamples;
        private bool lockPatientPosition = false;
        private Vector3 lockedPosition;

        // 瀹屾垚鐣岄潰杩愯鏃舵瀯寤虹殑 UI 寮曠敤
        private bool _completionUIBuilt;
        private Text _completionTextRuntime;
        private Button _completionRestartButtonRuntime;
        private Button _completionExitButtonRuntime;

        private void Awake()
        {
            // 鏈€鏃╃鐢ㄥ叾浠栧啿绐佺殑鎺у埗鍣?
            DisableOtherControllers();
        }

        private void Start()
        {
            EnsureLanguageManager();
            AutoFindAndFixPatient();
            AutoFindUI();
            CacheUIRootInitialTransform();
            SetupButtons();
            BuildLanguageSelectButtons();   // 鍦ㄥ紑濮嬮潰鏉夸笂鍒涘缓璇█鍒囨崲鎸夐挳
            SetupCPRDetector();
            EnsureHapticController();

            if (LanguageManager.Instance != null)
                LanguageManager.Instance.OnLanguageChanged += RefreshAllTrainingUI;

            StartCoroutine(StartIntroSequence());
        }

        /// <summary>
        /// 璁板綍褰撳墠 UI 鏍硅妭鐐圭殑鍒濆浣嶇疆涓庢棆杞紙鐢ㄤ簬銆屽啀鏉ヤ竴娆°€嶆椂鎭㈠锛夈€?
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
        /// 淇濆瓨鎽勫儚澶村綋鍓嶄綅缃綔涓恒€屽垵濮嬩綅缃€嶏紝渚涖€屽啀鏉ヤ竴娆°€嶆椂鎭㈠銆?
        /// forceOverwrite: 涓?true 鏃跺湪浼犻€佸墠瑕嗙洊锛堟洿鍑嗙‘锛夛紱鍚﹀垯浠呴娆′繚瀛樸€?
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
                Debug.Log($"[CPRTraining] 宸茶褰?OVRCameraRig 鍒濆浣嶇疆: {_initialCameraRigPosition}");
            }

            var mainCam = Camera.main;
            if (mainCam != null && mainCam.gameObject.name == "Main Camera")
            {
                _initialMainCamPosition = mainCam.transform.position;
                _initialMainCamRotation = mainCam.transform.rotation;
                Debug.Log($"[CPRTraining] 宸茶褰?Main Camera 鍒濆浣嶇疆: {_initialMainCamPosition}");
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
                Debug.Log($"[CPRTraining] OVRCameraRig 宸叉仮澶? {_initialCameraRigPosition}");
            }

            var mainCam = Camera.main;
            if (mainCam != null && mainCam.gameObject.name == "Main Camera")
            {
                mainCam.transform.position = _initialMainCamPosition;
                mainCam.transform.rotation = _initialMainCamRotation;
                Debug.Log($"[CPRTraining] Main Camera 宸叉仮澶? {_initialMainCamPosition}");
            }
        }

        /// <summary>
        /// 灏?UI 鏍?Canvas 鎭㈠鍒板垵濮嬩綅缃笌鏃嬭浆锛屽苟绂佺敤缁撴灉闈㈡澘鐨勫浐瀹氭憜鏀捐剼鏈€?
        /// </summary>
        private void RestoreUIRootToInitial()
        {
            if (_uiRoot == null)
                CacheUIRootInitialTransform();
            if (_uiRoot == null) return;

            _uiRoot.position = _uiRootInitialPosition;
            _uiRoot.rotation = _uiRootInitialRotation;

            var placer = _uiRoot.GetComponent<CPRResultPanelPlacer>();
            if (placer != null)
                placer.enabled = false;
        }

        private void EnsureLanguageManager()
        {
            if (LanguageManager.Instance == null)
            {
                var go = new GameObject("LanguageManager");
                go.AddComponent<LanguageManager>();
                Debug.Log("[CPRTraining] 鑷姩鍒涘缓 LanguageManager");
            }
        }

        // 鈹€鈹€鈹€ 璇█鍒囨崲鎸夐挳锛堟樉绀哄湪寮€濮嬭缁冮潰鏉夸笂鏂癸紝瀹屾垚鐣岄潰鏃堕殣钘忛伩鍏嶉伄鎸★級鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€
        private Button _langCNBtn, _langJPBtn, _langENBtn;
        private GameObject _languageSelectPanel;

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
            panelRt.anchoredPosition = new Vector2(0f, -18f);
            panelRt.sizeDelta = new Vector2(1140f, 136f);

            var bg = container.AddComponent<UnityEngine.UI.Image>();
            bg.color = new Color(0f, 0f, 0f, 0.5f);

            var langs = new[] { Language.Chinese, Language.Japanese, Language.English };
            var labels = new[] { "\u4e2d\u6587", "\u65e5\u672c\u8a9e", "English" };
            var colors = new[]
            {
                new Color(0.8f, 0.15f, 0.15f, 0.85f),
                new Color(0.1f, 0.35f, 0.85f, 0.85f),
                new Color(0.1f, 0.5f, 0.2f, 0.85f)
            };
            var xPos = new[] { -370f, 0f, 370f };

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
                rt.sizeDelta = new Vector2(336f, 104f);
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
                outline.effectColor = new Color(1f, 1f, 1f, 0.4f);
                outline.effectDistance = new Vector2(2f, -2f);

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
                ltxt.fontSize = 52;
                ltxt.alignment = TextAnchor.MiddleCenter;
                ltxt.color = Color.white;

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
                ol.effectColor = active ? new Color(1f,1f,0.2f,1f) : new Color(1f,1f,1f,0.3f);
        }

        /// <summary>璇█鍒囨崲鏃跺埛鏂版暣涓缁冪晫闈㈡枃瀛楋紙闃舵銆佹彁绀恒€佹寜閽瓑锛?/summary>
        private void RefreshAllTrainingUI()
        {
            // 1. 闃舵鍚嶇О
            if (phaseText != null)
                phaseText.text = GetPhaseDisplayName(currentPhase);

            // 2. 鎸囧鎻愮ず锛堟牴鎹綋鍓嶉樁娈垫樉绀哄搴旀枃妗堬級
            if (instructionText != null)
                instructionText.text = GetInstructionForCurrentPhase();

            // 3. 寮€濮嬭缁冮潰鏉夸笂鐨勬寜閽?
            SetButtonLabel(startScenarioButton, GetCPRTrainingButtonLabel());
            SetButtonLabel(startCPRButton, GetAEDTrainingButtonLabel());
            SetButtonLabel(exitTrainingButton, CPRTextLibrary.BtnExit);
            SetButtonLabel(hudExitButton, CPRTextLibrary.BtnExit);

            // 4. 寮€濮嬮潰鏉垮唴鐨勩€屽噯澶囧氨缁€嶇被鐘舵€佹枃瀛楋紙闈炴寜閽唴鐨?Text锛?
            if (startTrainingPanel != null && currentPhase == TrainingPhase.WaitingStart)
            {
                bool found = false;
                var texts = startTrainingPanel.GetComponentsInChildren<Text>(true);
                foreach (var t in texts)
                {
                    if (t.GetComponentInParent<Button>() != null) continue;
                    if (t == instructionText) continue;
                    string n = t.gameObject.name.ToLower();
                    if (n.Contains("title") || n.Contains("status") || n.Contains("ready") || n.Contains("phase")
                        || n.Contains("label") || n.Contains("main") || n.Contains("header"))
                    {
                        t.text = GetTrainingModeSelectionTitle();
                        found = true;
                        break;
                    }
                }
                if (!found && texts.Length > 0)
                {
                    foreach (var t in texts)
                    {
                        if (t.GetComponentInParent<Button>() != null) continue;
                        if (t == instructionText) continue;
                        t.text = GetTrainingModeSelectionTitle();
                        break;  // 鍙敼绗竴涓尮閰嶇殑
                    }
                }
            }

            // 5. 浠嬬粛闈㈡澘鐨勬杩庤
            if (introPanel != null && currentPhase == TrainingPhase.Intro)
            {
                var texts = introPanel.GetComponentsInChildren<Text>(true);
                foreach (var t in texts)
                {
                    if (t.GetComponentInParent<Button>() != null) continue;
                    if (t == instructionText) continue;
                    string n = t.gameObject.name.ToLower();
                    if (n.Contains("title") || n.Contains("welcome") || n.Contains("intro"))
                    {
                        t.text = CPRTextLibrary.Welcome;
                        break;
                    }
                }
            }

            Debug.Log("[CPRTraining] UI 鏂囧瓧宸叉寜褰撳墠璇█鍒锋柊");
        }

        private string GetInstructionForCurrentPhase()
        {
            return currentPhase switch
            {
                TrainingPhase.Intro          => CPRTextLibrary.Welcome,
                TrainingPhase.WaitingStart   => GetTrainingModeSelectionInstruction(),
                TrainingPhase.PatientFalling => CPRTextLibrary.PatientFell,
                TrainingPhase.AEDInProgress  => GetAEDPhaseInstruction(),
                TrainingPhase.ReadyForCPR    => CPRTextLibrary.FlipPatient,
                TrainingPhase.CPRInProgress  => CPRTextLibrary.StartCPRNow,
                TrainingPhase.Completed      => CPRTextLibrary.CompletionTitle,
                _                            => ""
            };
        }

        private string GetTrainingModeSelectionTitle()
        {
            return LanguageManager.Get(
                "请选择训练方式",
                "トレーニング方式を選択してください",
                "Choose a training mode"
            );
        }

        private string GetTrainingModeSelectionInstruction()
        {
            return LanguageManager.Get(
                "请选择 CPR 训练 或 AED 训练",
                "CPRトレーニング または AEDトレーニング を選択してください",
                "Choose CPR Training or AED Training"
            );
        }

        private string GetCPRTrainingButtonLabel()
        {
            return LanguageManager.Get(
                "CPR训练",
                "CPRトレーニング",
                "CPR Training"
            );
        }

        private string GetAEDTrainingButtonLabel()
        {
            return LanguageManager.Get(
                "AED训练",
                "AEDトレーニング",
                "AED Training"
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
            if (startScenarioButton != null)
                startScenarioButton.gameObject.SetActive(true);

            if (startCPRButton != null)
                startCPRButton.gameObject.SetActive(true);
        }

        private void PrepareTrainingSelectionUI()
        {
            selectedTrainingMode = TrainingMode.None;
            if (startTrainingPanel != null)
                startTrainingPanel.SetActive(true);
            EnsureTrainingModeButtonsVisible();
            AdjustStartMenuLayoutAndStyle();
            RefreshAllTrainingUI();
        }

        private TrainingMode ResolveSelectedTrainingMode()
        {
            return selectedTrainingMode == TrainingMode.None
                ? TrainingMode.CPR
                : selectedTrainingMode;
        }

        /// <summary>璁剧疆鎸夐挳鐨勫瓙 Text 鏂囧瓧锛岃嫢鏃犲垯鍒涘缓</summary>
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
                Debug.Log("[CPRTraining] 鍦烘櫙涓湭鎵惧埌 GloveHapticController锛屽凡鑷姩鍒涘缓");
            }
            Debug.Log($"[CPRTraining] hapticController={hapticController != null} ({(hapticController != null ? hapticController.gameObject.name : "null")})");
        }

        private float _diagTimer;
        private float _lastRealtimeHapticDepth;

        private void Update()
        {
            // 姣忓抚寮哄埗閿佸畾鎮ｈ€呬綅缃紙闃叉鎺夊湴涓嬶級
            if (lockPatientPosition && patientModel != null)
            {
                Vector3 pos = patientModel.transform.position;
                if (Mathf.Abs(pos.y - lockedPosition.y) > 0.01f || pos.y < 0f)
                {
                    patientModel.transform.position = lockedPosition;
                }
            }

            // === 姣?3 绉掓墦鍗颁竴娆¤瘖鏂俊鎭?===
            _diagTimer += Time.deltaTime;
            if (_diagTimer >= 3f)
            {
                _diagTimer = 0f;
                bool hasDetector = pressDetector != null;
                bool hasHaptic = hapticController != null;
                bool detectorInit = hasDetector && pressDetector.IsInitialized;
                float depth = hasDetector ? pressDetector.CurrentDepthCm : -1f;
                bool handIn = hasDetector && pressDetector.IsHandInContact;
                Debug.Log($"[CPR璇婃柇] phase={currentPhase} detector={hasDetector}(init={detectorInit}) haptic={hasHaptic} depth={depth:F1}cm hand={handIn} count={compressionCount}");

                // 鑷姩淇锛氬鏋?CPR 闃舵浣嗙己灏戠粍浠?
                if (currentPhase == TrainingPhase.CPRInProgress)
                {
                    if (!hasHaptic)
                    {
                        hapticController = FindObjectOfType<GloveHapticController>();
                        if (hapticController == null)
                        {
                            var go = new GameObject("AutoGloveHapticController");
                            hapticController = go.AddComponent<GloveHapticController>();
                            Debug.Log("[CPR璇婃柇] 鑷姩鍒涘缓浜?GloveHapticController");
                        }
                    }
                    if (!hasDetector)
                    {
                        pressDetector = FindObjectOfType<CPRPressDetector>();
                        if (pressDetector != null)
                        {
                            pressDetector.OnCompressionComplete -= OnCompressionDetected;
                            pressDetector.OnCompressionComplete += OnCompressionDetected;
                            Debug.Log("[CPR璇婃柇] 鑷姩鎵惧埌骞剁粦瀹氫簡 CPRPressDetector");
                        }
                    }
                }
            }

            // CPR 闃舵锛氬疄鏃舵洿鏂版寜鍘嬫繁搴︿笌棰戠巼鏄剧ず
            if (currentPhase == TrainingPhase.CPRInProgress && pressDetector != null)
            {
                float depth = pressDetector.CurrentDepthCm;
                float rate = pressDetector.CurrentRateBPM;
                bool handIn = pressDetector.IsHandInContact;
                if (instructionText != null)
                    instructionText.text = CPRTextLibrary.CPRProgress(
                        compressionCount, targetCompressions, depth, rate, handIn);

                // 瀹炴椂瑙﹁淇濆簳锛氭繁搴?> 0.5cm 灏辫Е鍙戦渿鍔紙涓嶇瓑 OnCompressionComplete锛?
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

        // 姝ゆ柟娉曞凡鐢?CPRTextLibrary + LanguageManager 鏇夸唬锛屼繚鐣欑┖鏂规硶涓嶅奖鍝嶈皟鐢ㄦ爤
        private void ReplaceUITextToJapanese() { }

        private Vector3 patientStandPosition;
        private Quaternion patientStandRotation;

        // 鎽勫儚澶村垵濮嬩綅缃紙鐢ㄤ簬銆屽啀鏉ヤ竴娆°€嶆椂鎭㈠锛?
        private Vector3 _initialCameraRigPosition;
        private Quaternion _initialCameraRigRotation;
        private Vector3 _initialMainCamPosition;
        private Quaternion _initialMainCamRotation;
        private bool _initialCameraPositionSaved;

        // UI 鏍硅妭鐐瑰垵濮嬩綅缃紙鎵€鏈夐潰鏉垮叡鐢ㄧ殑 Canvas 鏍癸級
        private Transform _uiRoot;
        private Vector3 _uiRootInitialPosition;
        private Quaternion _uiRootInitialRotation;

        private void AutoFindAndFixPatient()
        {
            // 浼樺厛褰撳墠椤圭洰浣跨敤鐨勪汉鐗╋細Standing React Death Backward锛堜笂鍗婅韩涓?Ch38_Shirt锛?
            if (patientModel == null)
                patientModel = GameObject.Find("Standing React Death Backward");
            if (patientModel == null)
                patientModel = GameObject.Find("XBot_Patient");
            if (patientModel == null)
                patientModel = GameObject.Find("Patient");
            if (patientModel == null)
                patientModel = GameObject.Find("X Bot");
            if (patientModel == null)
                patientModel = FindPatientByModelHierarchy(); // 鎸?Ch38_Body / mixamorig 绛夊瓙鑺傜偣鎵?
            if (patientModel == null)
                patientModel = FindPatientByAnimator();

            if (patientModel == null)
            {
                Debug.LogWarning("[CPRTraining] Patient model not found automatically. Assign patientModel in the inspector or use a model named Patient / XBot_Patient / Ch38_Body.");
                return;
            }

            // 淇 Scale
            if (patientModel.transform.localScale.x < 0.5f)
            {
                patientModel.transform.localScale = Vector3.one;
                Debug.Log("[CPRTraining] 淇 Scale -> (1,1,1)");
            }

            // 淇 Position锛堢‘淇濅笉鍦ㄥ師鐐癸紝Z 绾?1 浣挎偅鑰呯鐜╁鏇磋繎锛?
            if (patientModel.transform.position.magnitude < 0.1f)
            {
                patientModel.transform.position = new Vector3(0, 0, 1f);
                Debug.Log("[CPRTraining] 淇 Position -> (0,0,1)");
            }

            // 闈㈠悜鐩告満
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

                // 鍏抽敭锛氱鐢?Root Motion锛岄槻姝㈠姩鐢绘妸妯″瀷绉诲埌鍦颁笅
                patientAnimator.applyRootMotion = false;
                patientAnimator.speed = 0f;

                bool hasController = patientAnimator.runtimeAnimatorController != null;
                Debug.Log($"[CPRTraining] Animator: controller={hasController}, applyRootMotion=false");
            }

            // 娓呯悊鍦烘櫙涓棫鐨勬娴嬪尯
            var oldDetector = GameObject.Find("CPR_ChestDetector");
            if (oldDetector != null)
            {
                Destroy(oldDetector);
                Debug.Log("[CPRTraining] 娓呯悊鏃х殑妫€娴嬪尯");
            }

            Debug.Log($"[CPRTraining] 鎮ｈ€呭氨缁?Pos:{patientModel.transform.position} Scale:{patientModel.transform.localScale}");
        }

        /// <summary> 鎸夋ā鍨嬪眰绾ф煡鎵撅細鏍圭墿浣撲笅鍚湁 Ch38_Body銆乵ixamorig 绛夊吀鍨嬩汉褰㈤楠?缃戞牸鐨勮涓烘偅鑰?</summary>
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
                if (startCPRButton == null) startCPRButton = FindButtonByName("AEDTrainingButton");
                if (startCPRButton == null) startCPRButton = FindButtonByName("AEDButton");
            }

            if (restartButton == null)
                restartButton = FindButtonByName("RestartButton");

            if (exitButton == null)
                exitButton = FindButtonByName("ExitButton") ?? FindButtonByName("QuitButton");

            if (exitTrainingButton == null)
                exitTrainingButton = FindButtonByName("ExitTrainingButton");

            if (hudExitButton == null)
                hudExitButton = FindButtonByName("HUDExitButton") ?? FindButtonByName("HudExitButton");

            if (phaseText == null || instructionText == null)
            {
                var allTexts = FindObjectsOfType<Text>(true);
                foreach (var t in allTexts)
                {
                    if (phaseText == null &&
                        (t.text.Contains("CPR") ||
                         t.text.Contains("\u8bad\u7ec3") ||
                         t.text.Contains("\u30c8\u30ec\u30fc\u30cb\u30f3\u30b0")))
                    {
                        phaseText = t;
                    }

                    if (instructionText == null &&
                        (t.text.Contains("\u6309\u538b") ||
                         t.text.Contains("\u5f00\u59cb") ||
                         t.text.Contains("\u958b\u59cb") ||
                         t.text.Contains("\u6e96\u5099") ||
                         t.text.Contains("\u5727\u8feb")))
                    {
                        instructionText = t;
                    }
                }
            }

            Debug.Log($"[CPRTraining] UI: startBtn={startScenarioButton != null}, exitBtn={exitButton != null}");
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
                Debug.Log("[CPRTraining] 绂佺敤 SimpleMenuController");
            }
            var gameController = FindObjectOfType<CPRGameController>();
            if (gameController != null)
            {
                gameController.enabled = false;
                Debug.Log("[CPRTraining] 绂佺敤 CPRGameController");
            }
            // 绉婚櫎 PatientFallController锛堝畠浼氳鐩?Animator 璁剧疆锛?
            var patient = GameObject.Find("XBot_Patient") ?? GameObject.Find("Patient") ?? GameObject.Find("Standing React Death Backward") ?? GameObject.Find("X Bot");
            if (patient != null)
            {
                var fallCtrl = patient.GetComponent<Core.PatientFallController>();
                if (fallCtrl != null)
                {
                    Destroy(fallCtrl);
                    Debug.Log("[CPRTraining] 绉婚櫎鍐茬獊鐨?PatientFallController");
                }
            }
        }

        private void SetupButtons()
        {
            if (startScenarioButton != null)
            {
                startScenarioButton.onClick.RemoveAllListeners();
                startScenarioButton.onClick.AddListener(OnStartScenarioClicked);
                Debug.Log("[CPRTraining] 寮€濮嬫寜閽凡缁戝畾鍒板€掑湴娴佺▼");
            }

            if (startCPRButton != null)
            {
                startCPRButton.onClick.RemoveAllListeners();
                startCPRButton.onClick.AddListener(OnStartAEDClicked);
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

            // CPR 杩涜涓?HUD 涓婄殑涓€旈€€鍑烘寜閽細涓嶄細閫€鍑哄簲鐢紝鍙槸缁撴潫鏈璁粌骞跺洖鍒板紑濮嬬晫闈?
            if (hudExitButton != null)
            {
                hudExitButton.onClick.RemoveAllListeners();
                hudExitButton.onClick.AddListener(AbortCurrentTraining);
            }

            // 璋冩暣寮€濮嬬晫闈㈢殑鎸夐挳甯冨眬涓庢牱寮忥紝璁╂墜閮ㄥ皠绾块€夋嫨鏃舵洿鏄庢樉
            EnsureTrainingModeButtonsVisible();
            AdjustStartMenuLayoutAndStyle();
        }

        /// <summary>
        /// 灏嗗紑濮嬬晫闈㈢殑鎸夐挳閫傚綋鎷夊紑锛屽苟澧炲己楂樹寒鏁堟灉锛屾柟渚挎墜閮ㄥ皠绾块€夋嫨銆?
        /// </summary>
        private void AdjustStartMenuLayoutAndStyle()
        {
            Button[] btns = new Button[] { startScenarioButton, startCPRButton, exitTrainingButton };

            foreach (var btn in btns)
            {
                if (btn == null) continue;

                var colors = btn.colors;
                colors.normalColor = new Color(1f, 1f, 1f, 0.9f);
                colors.highlightedColor = new Color(0.15f, 0.8f, 1f, 1f);
                colors.pressedColor = new Color(0.05f, 0.5f, 1f, 1f);
                colors.selectedColor = colors.highlightedColor;
                colors.colorMultiplier = 1.3f;
                btn.colors = colors;
                btn.transition = UnityEngine.UI.Selectable.Transition.ColorTint;

                var image = btn.GetComponent<UnityEngine.UI.Image>();
                if (image != null && btn.GetComponent<UnityEngine.UI.Outline>() == null)
                {
                    var outline = btn.gameObject.AddComponent<UnityEngine.UI.Outline>();
                    outline.effectColor = new Color(0.1f, 0.8f, 1f, 0.9f);
                    outline.effectDistance = new Vector2(3f, -3f);
                }
            }

            LayoutStartMenuButton(startScenarioButton, new Vector2(-190f, -60f), new Vector2(300f, 90f));
            LayoutStartMenuButton(startCPRButton, new Vector2(190f, -60f), new Vector2(300f, 90f));
            LayoutStartMenuButton(exitTrainingButton, new Vector2(0f, -190f), new Vector2(240f, 72f));

            if (startTrainingPanel != null)
            {
                var panelImage = startTrainingPanel.GetComponent<Image>();
                if (panelImage != null)
                {
                    panelImage.color = new Color(0f, 0f, 0f, 0.18f);
                }
            }
        }

        private void LayoutStartMenuButton(Button button, Vector2 anchoredPosition, Vector2 size)
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
            // 鍚屾缁戝畾瑙﹁鎺у埗鍣?
            if (hapticController == null)
                hapticController = FindObjectOfType<GloveHapticController>();
            if (hapticController != null && pressDetector != null)
                hapticController.BindDetector(pressDetector);
        }

        private IEnumerator StartIntroSequence()
        {
            bool hasTrainingSelection = startScenarioButton != null || startCPRButton != null;

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
            BeginTraining(TrainingMode.CPR);
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
                Debug.Log($"[CPRTraining] 浼犻€佸皢鍦?{teleportDelay} 绉掑悗鎵ц");
                StartCoroutine(TeleportAfterDelay(teleportDelay));
            }

            if (patientModel != null)
            {
                yield return StartCoroutine(PlayDeathAnimation());
            }

            yield return new WaitForSeconds(0.5f);
            OnPatientFell?.Invoke();

            if (ResolveSelectedTrainingMode() == TrainingMode.AED)
            {
                ChangePhase(TrainingPhase.AEDInProgress);
                EnsureHUDFollowsCamera();
                ShowPanel(trainingHUDPanel);
                UpdateInstruction(GetAEDPhaseInstruction());
                yield break;
            }

            if (skipFlipInteraction)
            {
                // 璺宠繃缈昏浆浜や簰锛岀洿鎺ュ紑濮?CPR锛堥槻姝㈢炕杞椂鎺夊湴涓嬶級
                Debug.Log("[CPRTraining] 璺宠繃缈昏浆浜や簰锛岀洿鎺ュ紑濮?CPR");

                // 鐩存帴鍒涘缓妫€娴嬪尯
                CreateCPRDetectionZone();

                yield return new WaitForSeconds(0.5f);
                StartCPRPhase();
            }
            else
            {
                ChangePhase(TrainingPhase.ReadyForCPR);
                UpdateInstruction(CPRTextLibrary.FlipPatient);

                // 鍒涘缓缈昏浆鎻愮ず UI 鍜屼氦浜掓娴?
                yield return StartCoroutine(WaitForFlipInteraction());

                // 缈昏浆瀹屾垚鍚庡垱寤烘娴嬪尯
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
            // 浼犻€佸墠淇濆瓨褰撳墠浣嶇疆锛屼綔涓恒€屽啀鏉ヤ竴娆°€嶆椂鐨勬仮澶嶇洰鏍?
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

            // CharacterController 浼氳鐩栫洿鎺ヨ缃殑 transform.position锛岄渶鍏堢鐢ㄥ啀璁剧疆
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

            Debug.Log($"[CPRTraining] 宸蹭紶閫佸埌鎮ｈ€呰韩鏃?Pos:{targetPos}");
        }

        private void EnsureHUDFollowsCamera()
        {
            if (trainingHUDPanel == null) return;
            Transform root = trainingHUDPanel.transform;
            while (root.parent != null) root = root.parent;
            var follow = root.GetComponent<CPRHUDFollowCamera>();
            var resultPlacer = root.GetComponent<CPRResultPanelPlacer>();
            if (resultPlacer != null) resultPlacer.enabled = false; // 璁粌闃舵鍏抽棴缁撴灉甯冨眬
            if (follow == null)
            {
                follow = root.gameObject.AddComponent<CPRHUDFollowCamera>();
                // 璁?HUD 鏇撮潬杩戜汉鐗╁拰瑙嗙嚎
                follow.distanceInFront = 0.5f;
                follow.placeUINearPatient = true;
                // 鏇村亸鍚戣创杩戞偅鑰咃紙鏁板€艰秺灏忚秺闈犺繎浜虹墿锛?
                follow.patientToCameraBlend = 0.25f;
                follow.faceCamera = true;
                follow.onlyWhenActive = false;
                follow.groundLevel = 0f;
                // 璋冧綆鏈€浣庨珮搴︼紝璁?UI 鏇撮潬杩戝湴闈竴浜?
                follow.minHeightAboveGround = 0.4f;
            }
            else
            {
                follow.enabled = true;
            }
        }

        /// <summary>
        /// 鐢ㄦ埛鍦ㄦ寜鍘嬭繃绋嬩腑涓€旈€€鍑哄綋鍓嶈缁冿細娓呯┖鏈缁熻锛屽洖鍒板紑濮嬭缁冪晫闈€?
        /// </summary>
        private void AbortCurrentTraining()
        {
            Debug.Log("[CPRTraining] Training aborted by user");

            StopAllCoroutines();

            compressionCount = 0;
            trainingStartTime = 0f;
            _sumDepthCm = 0f;
            _sumRateBpm = 0f;
            _compressionSamples = 0;
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
            // 鍒涘缓涓€涓棆杞澶存彁绀?
            flipPromptUI = new GameObject("FlipArrowIndicator");
            flipPromptUI.transform.position = position + Vector3.up * 0.5f;

            // 浣跨敤涓€涓畝鍗曠殑鍦嗘煴浣撲綔涓虹澶存寚绀?
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

            // 娣诲姞鏃嬭浆鍔ㄧ敾
            var rotator = flipPromptUI.AddComponent<SpinAnimation>();
        }

        private IEnumerator PlayDeathAnimation()
        {
            if (patientModel == null) yield break;

            bool hasAnimator = patientAnimator != null
                && patientAnimator.runtimeAnimatorController != null;

            if (hasAnimator)
            {
                Debug.Log("[CPRTraining] ===== 寮€濮嬫挱鏀?FBX 姝讳骸鍔ㄧ敾 =====");

                patientAnimator.enabled = true;
                patientAnimator.applyRootMotion = false;  // 鍏抽敭锛氱鐢?Root Motion锛岄槻姝㈡帀鍦颁笅
                patientAnimator.speed = 1f;

                // 鎾斁鍊掑湴鐘舵€侊紙Inspector 鍙涓?DeathFromRight 鎴?Standing React Death Backward锛?
                string stateToPlay = !string.IsNullOrEmpty(fallDownStateName) ? fallDownStateName : "DeathFromRight";
                patientAnimator.Play(stateToPlay, 0, 0f);
                Debug.Log($"[CPRTraining] Play({stateToPlay}) 宸茶皟鐢紝applyRootMotion=false");

                // 璁板綍鍒濆浣嶇疆锛岀敤浜庨槻姝㈡帀鍦颁笅
                Vector3 startPos = patientModel.transform.position;

                yield return null;

                // 鑾峰彇鍔ㄧ敾鐗囨闀垮害
                float animLength = 3.5f;
                var clipInfo = patientAnimator.GetCurrentAnimatorClipInfo(0);
                if (clipInfo.Length > 0)
                {
                    animLength = clipInfo[0].clip.length;
                    Debug.Log($"[CPRTraining] 鍔ㄧ敾鐗囨: {clipInfo[0].clip.name}, 鏃堕暱: {animLength:F2}s");
                }
                else
                {
                    var stateInfo = patientAnimator.GetCurrentAnimatorStateInfo(0);
                    if (stateInfo.length > 0)
                        animLength = stateInfo.length;
                    Debug.Log($"[CPRTraining] 浣跨敤 stateInfo 鏃堕暱: {animLength:F2}s");
                }

                // 绛夊緟鍔ㄧ敾瀹屾垚锛屽悓鏃堕攣瀹?Y 杞撮槻姝㈡帀鍦颁笅
                float elapsed = 0f;
                while (elapsed < animLength + 0.2f)
                {
                    elapsed += Time.deltaTime;

                    // 姣忓抚閿佸畾 Y 杞达紙涓嶈鍔ㄧ敾鎶婃ā鍨嬬Щ鍒板湴涓嬶級
                    Vector3 pos = patientModel.transform.position;
                    pos.y = Mathf.Max(pos.y, 0f);  // 涓嶄綆浜庡湴闈?
                    patientModel.transform.position = pos;

                    yield return null;
                }

                // 鍔ㄧ敾瀹屾垚鍚庯細瀹屽叏绂佺敤 Animator 骞堕攣瀹氫綅缃?
                Vector3 finalPos = patientModel.transform.position;
                Quaternion finalRot = patientModel.transform.rotation;

                // 鍏抽敭锛氬厛绂佺敤 Animator锛屽啀璁剧疆浣嶇疆锛堥槻姝?Animator 瑕嗙洊锛?
                patientAnimator.enabled = false;

                // 寮哄埗閿佸畾浣嶇疆锛圷 杞翠笉浣庝簬 0锛?
                finalPos.y = Mathf.Max(finalPos.y, 0f);
                patientModel.transform.position = finalPos;
                patientModel.transform.rotation = finalRot;

                // 娣诲姞 Rigidbody 骞剁鐢ㄩ噸鍔涳紙濡傛灉杩樻病鏈夛級
                var rb = patientModel.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = patientModel.AddComponent<Rigidbody>();
                    Debug.Log("[CPRTraining] 宸叉坊鍔?Rigidbody");
                }
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;  // 鍐荤粨 Y 杞村拰鏃嬭浆

                // 鍚姩 Update() 姣忓抚浣嶇疆閿佸畾
                lockedPosition = finalPos;
                lockPatientPosition = true;

                Debug.Log($"[CPRTraining] 鍔ㄧ敾瀹屾垚骞跺喕缁?Pos:{finalPos} Rot:{finalRot.eulerAngles}锛屽凡鍚姩姣忓抚浣嶇疆閿佸畾");
            }
            else
            {
                Debug.LogWarning("[CPRTraining] 鏃?AnimatorController锛佽 Ctrl+R 閲嶆柊缂栬瘧璁?AutoAnimatorSetup 鑷姩鍒涘缓");
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

            // 鎵捐兏閮ㄥ弬鑰冿細浼樺厛楠ㄩ锛屽叾娆′笂鍗婅韩 Ch38_Shirt锛圫tanding React Death Backward 鐨勪笂鍗婅韩锛?
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
                localPos = new Vector3(0f, 0.05f, 0f);
                Debug.Log($"[CPRTraining] 鎵惧埌鑳搁儴楠ㄩ: {chestBone.name}锛屾娴嬪尯灏嗛殢楠ㄩ绉诲姩");
            }
            else if (upperBody != null)
            {
                zoneParent = patientModel.transform;
                var shirtRenderer = upperBody.GetComponent<SkinnedMeshRenderer>();
                if (shirtRenderer != null && shirtRenderer.bounds.size.sqrMagnitude > 0.001f)
                {
                    Vector3 chestWorld = shirtRenderer.bounds.center;
                    localPos = zoneParent.InverseTransformPoint(chestWorld);
                    Debug.Log($"[CPRTraining] 浣跨敤 Ch38_Shirt 缃戞牸涓績浣滀负鑳搁儴锛屼笘鐣屼綅缃? {chestWorld}");
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
                Debug.Log("[CPRTraining] 鏈壘鍒拌兏閮ㄩ楠?涓婂崐韬紝浣跨敤妯″瀷涓績鍋忎笂浣滀负鑳搁儴");
            }

            // 鍒涘缓妫€娴嬪尯鍩燂紙绾㈣壊鍗婇€忔槑鐞冧綋锛夛紝鎸傚埌鑳搁儴/鎮ｈ€呬笅浠ヤ究鍊掑湴鍚庝粛瀵瑰噯鑳搁儴
            chestDetectionZone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            chestDetectionZone.name = "CPR_ChestDetector";
            chestDetectionZone.transform.SetParent(zoneParent, false);
            chestDetectionZone.transform.localPosition = localPos;
            chestDetectionZone.transform.localRotation = Quaternion.identity;
            chestDetectionZone.transform.localScale = new Vector3(0.2f, 0.08f, 0.2f);

            // 绾㈣壊鍗婇€忔槑鏉愯川锛堝吋瀹?URP 鍜?Built-in锛?
            var renderer = chestDetectionZone.GetComponent<Renderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            var mat = new Material(shader);
            mat.color = new Color(1f, 0.15f, 0.15f, 0.7f);

            // 灏濊瘯璁剧疆閫忔槑妯″紡锛圲RP锛?
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1);
                mat.SetFloat("_Blend", 0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            // 閫氱敤閫忔槑璁剧疆
            mat.SetOverrideTag("RenderType", "Transparent");
            if (mat.HasProperty("_SrcBlend"))
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend"))
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite"))
                mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
            renderer.material = mat;

            // 瑙﹀彂鍣紙鐢ㄤ簬妫€娴嬫墜閮ㄧ鎾烇級
            var collider = chestDetectionZone.GetComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.5f;

            // 娣诲姞鑴夊啿鍙戝厜鍔ㄧ敾
            chestDetectionZone.AddComponent<DetectionZonePulse>();

            // 纭繚鏈夋墜閮ㄨ拷韪€傞厤鍣紙璺濈澶囩敤妫€娴嬮渶瑕侊級
            EnsureHandTrackingAdapter();

            // 鑻ユ棤妫€娴嬪櫒鍒欐坊鍔狅紝骞剁粦瀹氬埌鏈帶鍒跺櫒涓庤Е瑙?
            var detector = chestDetectionZone.GetComponent<CPRPressDetector>();
            if (detector == null)
            {
                detector = chestDetectionZone.AddComponent<CPRPressDetector>();
                detector.useCollisionDetection = true;
                detector.detectionAreaSize = new Vector3(0.2f, 0.08f, 0.2f);
                detector.useDistanceFallback = true;
                detector.palmDistanceThreshold = 0.5f;
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
                hapticController.BindDetector(detector); // 璁㈤槄浜嬩欢锛屽繀椤诲湪 Start() 涔嬪悗璋冪敤
            }
            else
            {
                hapticController = FindObjectOfType<GloveHapticController>();
                if (hapticController != null) hapticController.BindDetector(detector);
            }

            Debug.Log($"[CPRTraining] CPR 妫€娴嬪尯宸插垱寤猴紝鎸傝浇浜?{zoneParent.name}锛屼笘鐣屼綅缃? {chestDetectionZone.transform.position}锛宧aptic={hapticController != null}");
        }

        private void EnsureHandTrackingAdapter()
        {
            if (FindObjectOfType<HandTrackingAdapter>() != null) return;
            var rig = FindObjectOfType<OVRCameraRig>();
            GameObject target = rig != null ? rig.gameObject : new GameObject("HandTrackingAdapter");
            if (rig == null) Debug.Log("[CPRTraining] 鏈壘鍒?OVRCameraRig锛屽凡鍒涘缓鐙珛 HandTrackingAdapter 鐗╀綋");
            target.AddComponent<HandTrackingAdapter>();
            Debug.Log("[CPRTraining] 宸茶嚜鍔ㄦ坊鍔?HandTrackingAdapter锛堢敤浜庤窛绂诲鐢ㄦ娴嬶級");
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
            StartCoroutine(PatientFallSequence());
        }

        private void OnStartAEDClicked()
        {
            BeginTraining(TrainingMode.AED);
        }

        private void StartCPRPhase()
        {
            if (chestDetectionZone == null)
            {
                CreateCPRDetectionZone();
            }

            ChangePhase(TrainingPhase.CPRInProgress);
            EnsureHUDFollowsCamera();
            ShowPanel(trainingHUDPanel);

            trainingStartTime = Time.time;
            compressionCount = 0;
            _sumDepthCm = 0f;
            _sumRateBpm = 0f;
            _compressionSamples = 0;

            if (hapticController != null)
            {
                hapticController.ResetForNewSession();
            }

            // 鍚姩CPR妫€娴?
            if (pressDetector != null)
            {
                pressDetector.ResetStats();
                pressDetector.Initialize();
            }

            OnCPRStarted?.Invoke();
            PlaySound(startCPRSound);

            UpdateInstruction(CPRTextLibrary.StartCPRNow);
        }

        public void StartCPRAfterAED()
        {
            if (currentPhase == TrainingPhase.CPRInProgress || currentPhase == TrainingPhase.Completed)
            {
                return;
            }

            StartCPRPhase();
        }

        private void OnCompressionDetected(float depth, float rate)
        {
            if (currentPhase != TrainingPhase.CPRInProgress) return;

            compressionCount++;
            // 绱Н鐢ㄤ簬骞冲潎鍊肩粺璁?
            _sumDepthCm += depth;
            _sumRateBpm += rate;
            _compressionSamples++;

            OnCompressionProgress?.Invoke(compressionCount, targetCompressions);

            UpdateInstruction(CPRTextLibrary.CPRAbortProgress(compressionCount, targetCompressions, depth, rate));

            // 鐩存帴瑙﹀彂瑙﹁锛堜笉渚濊禆浜嬩欢璁㈤槄锛岀‘淇濇瘡娆℃寜鍘嬮兘鏈夊弽棣堬級
            if (hapticController == null)
                hapticController = FindObjectOfType<GloveHapticController>();
            if (hapticController != null)
                hapticController.TriggerByDepth(depth);

            if (compressionCount >= targetCompressions)
            {
                CompleteTraining();
            }
        }

        private void CompleteTraining()
        {
            EnsureLanguageManager();  // 纭繚鏈?LanguageManager锛岀粨鏋滅晫闈㈡寜褰撳墠璇█鏄剧ず
            ChangePhase(TrainingPhase.Completed);
            ShowPanel(completionPanel);
            BuildCompletionUIIfNeeded();

            // 瀹屾垚闃舵锛氬仠姝?HUD 璺熼殢锛涜嫢闈炲浐瀹氭ā寮忥紝灏嗙粺璁?UI 鏀惧埌浜虹墿鑳搁儴鏃佽竟鐨勫湴闈笂
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
            float avgRate = _compressionSamples > 0 ? _sumRateBpm / _compressionSamples : 0f;

            // 鏍规嵁鍖诲鎺ㄨ崘鑼冨洿璇勪环鎸夊帇璐ㄩ噺锛堟牴鎹綋鍓嶈瑷€杩斿洖瀵瑰簲鏂囧瓧锛?
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

            string result = CPRTextLibrary.BuildResultText(
                compressionCount, totalTime, avgDepth, avgRate, depthEval, rateEval);

            // 鍙洿鏂颁笓鐢ㄧ殑缁撴灉 Text锛屼笉瑕嗙洊鏍囬绛夊叾浠栨枃瀛?
            Text targetText = _completionTextRuntime;
            if (targetText == null) targetText = completionResultText;
            // 鎸夊悕瀛楁壘 ResultText锛堜笉鐢?GetComponentInChildren 浠ュ厤鎷垮埌鏍囬锛?
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
            if (targetText == null) targetText = instructionText;
            if (targetText != null) targetText.text = result;

            // 鎸夊綋鍓嶈瑷€鍒锋柊瀹屾垚鐣岄潰鐨勬爣棰樺拰鎸夐挳锛堜笌寮€濮嬬晫闈㈤€夌殑璇█涓€鑷达級
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
            }

            // 璁粌瀹屾垚鍚庣珛鍗冲仠姝㈣Е瑙夊弽棣?
            if (hapticController != null)
            {
                hapticController.StopAllFeedback();
            }

            PlaySound(completeSound);

            Debug.Log($"[CPRTraining] 瀹屾垚 - 鎸夊帇:{compressionCount}, 鐢ㄦ椂:{totalTime:F1}s");
        }

        /// <summary>
        /// 绗竴娆¤繘鍏ュ畬鎴愰樁娈垫椂锛屼紭鍏堜娇鐢ㄥ満鏅腑宸叉湁鐨?ResultText/RestartButton/ExitButton锛?
        /// 鑻ユ棤鍒欒嚜鍔ㄥ垱寤猴紝纭繚缁撴灉鏄剧ず鍦?UI 涓婁笖鏃犻噸澶嶉伄鎸°€?
        /// </summary>
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

            // 浼樺厛浣跨敤鍦烘櫙涓凡鏈夌殑缁撴灉鏂囨湰锛堥伩鍏嶅垱寤洪噸澶嶇殑 CPR_ResultText 閫犳垚閬尅锛?
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
                    txt.text = "銉堛儸銉笺儖銉炽偘绲愭灉 / 璁粌缁撴灉";
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

            // 浼樺厛浣跨敤鍦烘櫙涓凡鏈夌殑鎸夐挳锛堥伩鍏嶅垱寤?CPR_ReplayButton/CPR_ExitButton 閫犳垚閲嶅鑹插潡閬尅锛?
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
                    _completionRestartButtonRuntime.onClick.AddListener(RestartTraining);
                }
                else
                {
                    var btnGo = CreateCompletionButton(panelRt, "CPR_ReplayButton", CPRTextLibrary.BtnRestart,
                        new Vector2(-220f, -190f), new Color(0.1f, 0.6f, 0.9f, 0.9f), RestartTraining);
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
                }
                else
                {
                    var btnGo = CreateCompletionButton(panelRt, "CPR_ExitButton", CPRTextLibrary.BtnExit,
                        new Vector2(220f, -190f), new Color(0.2f, 0.2f, 0.2f, 0.95f), ExitToMainMenu);
                    _completionExitButtonRuntime = btnGo.GetComponent<Button>();
                }
            }

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
            return btnGo;
        }

        private void ChangePhase(TrainingPhase newPhase)
        {
            currentPhase = newPhase;
            OnPhaseChanged?.Invoke(newPhase);

            if (phaseText != null)
            {
                phaseText.text = GetPhaseDisplayName(newPhase);
            }

            Debug.Log($"[CPRTraining] 闃舵: {newPhase}");
        }

        private string GetPhaseDisplayName(TrainingPhase phase)
        {
            return phase switch
            {
                TrainingPhase.Intro          => CPRTextLibrary.PhaseIntro,
                TrainingPhase.WaitingStart   => GetTrainingModeSelectionTitle(),
                TrainingPhase.PatientFalling => CPRTextLibrary.PhaseFalling,
                TrainingPhase.AEDInProgress  => GetAEDPhaseDisplayName(),
                TrainingPhase.ReadyForCPR    => CPRTextLibrary.PhaseReadyCPR,
                TrainingPhase.CPRInProgress  => CPRTextLibrary.PhaseCPRInProgress,
                TrainingPhase.Completed      => CPRTextLibrary.PhaseCompleted,
                _                            => ""
            };
        }

        private void UpdateInstruction(string text)
        {
            if (instructionText != null)
                instructionText.text = text;
        }

        private void ShowPanel(GameObject panel)
        {
            HideAllPanels();
            if (panel != null)
                panel.SetActive(true);
            // 瀹屾垚鐣岄潰鏃堕殣钘忚瑷€閫夋嫨鎸夐挳锛堥伩鍏嶉伄鎸＄粨鏋滄樉绀哄拰鎿嶄綔鎸夐挳锛?
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
        /// 閲嶆柊寮€濮嬭缁冿細娓呯悊褰撳墠鐘舵€侊紝灏嗘偅鑰呮仮澶嶄负绔欑珛锛屽洖鍒?Intro 娴佺▼銆?
        /// </summary>
        public void RestartTraining()
        {
            Debug.Log("[CPRTraining] Restart training");

            StopAllCoroutines();

            compressionCount = 0;
            trainingStartTime = 0f;
            _sumDepthCm = 0f;
            _sumRateBpm = 0f;
            _compressionSamples = 0;
            selectedTrainingMode = TrainingMode.None;

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

            StartCoroutine(StartIntroSequence());
        }

        public void ExitToMainMenu()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
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
