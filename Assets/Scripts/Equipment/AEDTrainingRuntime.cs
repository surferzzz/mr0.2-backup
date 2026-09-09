using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CPRSimulation;
using CPRSimulation.Game;
using GLTFast;
using Oculus.Interaction;
using Oculus.Interaction.Grab;
using Oculus.Interaction.GrabAPI;
using Oculus.Interaction.HandGrab;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRFirstAid.Equipment
{
    /// <summary>
    /// Adds a lightweight AED workflow on top of the existing CPR training flow.
    /// The component is created automatically at runtime in scenes that use
    /// CPRTrainingController, so the scene does not need manual inspector wiring.
    /// </summary>
    public sealed class AEDTrainingRuntime : MonoBehaviour
    {
        private const string RuntimeRootName = "AEDTrainingRuntime";
        private const string StreamingAssetModelPath = "aed-defibrilator/source/AED_Defibrilator.glb";
        private const float TargetModelSizeMeters = 0.22f;
        private const float InteractionRadius = 0.24f;
        private const float AedBodyInteractionRadius = 0.42f;
        private const float PadInteractionRadius = 0.72f;
        private const float ElectrodePackageInteractionRadius = 0.58f;
        private const float InteractionHoldSeconds = 0.05f;
        private const float MarkerScale = 0.045f;
        private const float MarkerHeightOffset = 0.045f;
        private const float ShockFlashSeconds = 0.8f;
        private const float WorldUiScale = 0.00065f;
        private const float AedWorldUiHeightAboveHead = 0.36f;
        private const float AedWorldUiDistanceTowardViewer = 0.12f;
        private const float PhysicalPadGrabRadius = 0.58f;
        private const float PhysicalPadSnapRadius = 0.30f;
        private const float PhysicalPadWidth = 0.18f;
        private const float PhysicalPadHeight = 0.012f;
        private const float PhysicalPadDepth = 0.12f;
        private static readonly Vector3 AedWorldUiLocalPosition = new Vector3(-0.18f, 0.34f, 0.26f);
        private static readonly Vector3 AedWorldUiLocalEulerAngles = new Vector3(0f, 180f, 0f);
        private static readonly Vector3 AedModelHostLocalPosition = new Vector3(-0.693f, 0.005f, 1.369f);
        private static readonly Vector3 AedModelHostLocalScale = Vector3.one * 0.28639f;
        private static readonly Vector3 AedElectrodeLocalPosition = new Vector3(-0.059f, 0.085f, -0.811f);
        private static readonly Vector3 AedElectrodeLocalEulerAngles = new Vector3(180f, 0f, -180f);

        // 找不到静置家具时的地面兜底位置：房间内(x -4..4, z -3..3.5)、患者与等候区之间。
        private static readonly Vector3 AedFloorFallbackPosition = new Vector3(1.6f, 0f, -1.2f);
        private static readonly Vector3 AedPowerMarkerLocalPosition = new Vector3(-0.921f, 0.112f, 1.14f);

        [Tooltip("调试用：勾选后每次分析都判定为可电击心律，忽略下方概率。")]
        [SerializeField] private bool alwaysAdviseShock = false;
        [Tooltip("分析时判定为可电击心律(室颤/无脉室速)的概率，其余为不可电击心律(心搏停止/PEA)。")]
        [Range(0f, 1f)]
        [SerializeField] private float shockableRhythmProbability = 0.65f;
        [SerializeField] private float analysisDuration = 3f;

        [Header("语音引导")]
        [Tooltip("每次状态切换时播放对应 AED 语音；无真人录音时用程序化提示音兜底。暂时关闭，后续接入真人录音时再开启。")]
        [SerializeField] private bool enableVoicePrompts = false;
        [Range(0f, 1f)]
        [SerializeField] private float voiceVolume = 0.9f;
        [SerializeField] private Vector3 patientSideOffset = new Vector3(0.55f, 0.02f, 0.22f);

        [Header("开局摆放")]
        [Tooltip("训练一开始就把 AED 摆在房间里，而不是等患者倒地后凭空出现。" +
                 "顺带会提前加载模型，消除 AED 阶段切换时的加载卡顿。")]
        [SerializeField] private bool showAedFromStart = true;

        [Tooltip("AED 静置在哪个家具上。留空则退回地面。房间里唯一的桌子是 Waiting_Table；" +
                 "床边只有 Visitor_Bench，没有床头小桌。")]
        [SerializeField] private string restingFurnitureName = "Waiting_Table";

        [Tooltip("进入 AED 阶段时是否把整套场景搬到患者身旁。" +
                 "关闭则 AED 留在原处，学员需要走过去操作（交互距离未经验证，改前先测）。")]
        [SerializeField] private bool relocateToPatientOnAedPhase = true;
        [SerializeField] private Color powerMarkerColor = new Color(0.1f, 0.8f, 1f, 0.95f);
        [SerializeField] private Color padMarkerColor = new Color(1f, 0.85f, 0.15f, 0.95f);
        [SerializeField] private Color completedPadColor = new Color(0.15f, 0.95f, 0.35f, 0.95f);
        [SerializeField] private Color shockMarkerColor = new Color(1f, 0.25f, 0.25f, 0.95f);

        private CPRTrainingController trainingController;
        private CPRTrainingController subscribedTrainingController;
        private HandTrackingAdapter handTrackingAdapter;
        private Transform patientRoot;
        private Transform chestReference;

        // FindChestReference() 里有一次 FindObjectOfType 和最多 5 次骨骼递归遍历。
        // 它原来在 Update() 每帧执行，而且发生在 scenarioVisible 提前返回之前，
        // 所以连「只练 CPR」的场次也在白烧这份开销。
        // 现在：拿到检测器的 chestSurface 之后就不再重算；在此之前每 0.5s 重试一次，
        // 避免把 patientRoot 这个降级结果永久缓存下来。
        private float _nextChestResolveTime;
        private bool _chestReferenceFromDetector;
        private GameObject scenarioRoot;
        private GameObject modelHost;
        private GltfAsset gltfAsset;
        private bool modelPrepared;
        private bool loadRequested;
        private bool leftPadPlaced;
        private bool rightPadPlaced;
        private bool scenarioVisible;
        private bool worldUiPoseFixed;
        private bool electrodePackageOpened;
        private AEDState currentState = AEDState.Off;
        private SimulatedRhythm currentRhythm = SimulatedRhythm.VentricularFibrillation;
        private AudioSource voiceAudioSource;
        private AudioClip cueBeepClip;
        private AudioClip urgentBeepClip;
        private Coroutine subtitlePulseRoutine;
        private readonly Dictionary<string, AudioClip> voiceClipCache = new Dictionary<string, AudioClip>();
        private readonly HashSet<string> voiceClipMissing = new HashSet<string>();
        private float scenarioStartTime;

        // AED 环节评分结果（静态保存，供 CPRTrainingController 结算界面读取）
        public static bool HasResult { get; private set; }
        public static bool ResultPadsPlaced { get; private set; }
        public static bool ResultShockAdvised { get; private set; }
        public static bool ResultShockDelivered { get; private set; }
        public static float ResultDurationSeconds { get; private set; }
        public static SimulatedRhythm ResultRhythm { get; private set; }
        private GameObject powerMarker;
        private GameObject shockMarker;
        private GameObject leftPadTarget;
        private GameObject rightPadTarget;
        private GameObject leftPhysicalPad;
        private GameObject rightPhysicalPad;
        private GameObject electrodePackageTarget;
        private AedPadGrabbable carriedPad;
        private Text instructionText;
        private Image instructionBackground;
        private float targetHoverStartedAt = -1f;
        private GameObject hoveredTarget;
        private Coroutine analysisRoutine;
        private Coroutine shockFlashRoutine;
        private Coroutine activationRoutine;
        private Task<bool> modelLoadTask;
        private Canvas aedWorldCanvas;
        private RectTransform aedWorldCanvasRect;
        private GameObject instructionPanel;
        private GameObject controlPanel;
        private Text controlTitleText;
        private Button powerControlButton;
        private Button packageControlButton;
        private Button leftPadControlButton;
        private Button rightPadControlButton;
        private Button shockControlButton;
        private Button returnControlButton;
        private Button exitControlButton;
        private OVRCameraRig clickRig;
        private OVRHand clickLeftHand;
        private OVRHand clickRightHand;
        private bool wasRightPinch;
        private bool wasLeftPinch;

        /// <summary>
        /// 自启动入口。注意：RuntimeInitializeOnLoadMethod 只在「首个场景加载后」
        /// 执行一次，之后的场景切换不会再触发。
        ///
        /// 原来的实现在这里直接判断「当前场景没有 CPRTrainingController 就 return」，
        /// 而 Build Settings 的启动场景是 Scenes/MainMenu.unity，那里没有训练控制器 ——
        /// 于是构建版里这个方法一次就放弃，AEDTrainingRuntime 永远不会被创建，
        /// 整个 AED 训练模块不会初始化。
        /// 编辑器里直接 Play 训练场景时守卫刚好通过，所以问题只在构建版暴露。
        ///
        /// 修法：把创建逻辑挂到 sceneLoaded 上，每次场景加载后都重试一次。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            // 先减再加：静态事件订阅会跨场景存活，编辑器关闭 Domain Reload 时
            // 还会跨 Play 会话存活，重复订阅会导致每次加载尝试创建多次。
            SceneManager.sceneLoaded -= HandleSceneLoadedForBootstrap;
            SceneManager.sceneLoaded += HandleSceneLoadedForBootstrap;

            TryCreateRuntimeHost();
        }

        private static void HandleSceneLoadedForBootstrap(Scene scene, LoadSceneMode mode)
        {
            TryCreateRuntimeHost();
        }

        private static void TryCreateRuntimeHost()
        {
            if (FindObjectOfType<AEDTrainingRuntime>() != null)
            {
                return;
            }

            // 没有训练控制器的场景（如主菜单）不需要 AED 宿主，静默跳过即可 ——
            // 进入训练场景后 sceneLoaded 会再来一次。
            if (FindObjectOfType<CPRTrainingController>() == null)
            {
                return;
            }

            var go = new GameObject(RuntimeRootName);
            go.AddComponent<AEDTrainingRuntime>();
            Debug.Log("[AEDTrainingRuntime] Runtime host created. AED training will activate when the patient falls in AED mode.");
        }

        private void Awake()
        {
            ResolveSceneReferences();
            EnsureInstructionOverlay();
            HideInstructionOverlay();
        }

        private void OnEnable()
        {
            SceneManager.activeSceneChanged += HandleActiveSceneChanged;
            SubscribeToTrainingEvents();
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
            UnsubscribeFromTrainingEvents();
        }

        private void Start()
        {
            ResolveSceneReferences();

            if (showAedFromStart)
            {
                StartCoroutine(ShowAedAtRestingPlace());
            }
        }

        private void Update()
        {
            ResolveSceneReferences();

            if (!scenarioVisible)
            {
                return;
            }

            HandleDebugShortcuts();
            HandleHandInteraction();
            HandlePhysicalPadProximityInteraction();
            HandleControlPanelClick();
            AnimateMarkers();
        }

        private void HandleActiveSceneChanged(Scene previousScene, Scene nextScene)
        {
            ResetScenario();
            ResolveSceneReferences(force: true);
            EnsureInstructionOverlay();
            HideInstructionOverlay();
            SubscribeToTrainingEvents();
        }

        private void ResolveSceneReferences(bool force = false)
        {
            if (force || trainingController == null)
            {
                trainingController = FindObjectOfType<CPRTrainingController>();
            }

            if (force || handTrackingAdapter == null)
            {
                handTrackingAdapter = FindObjectOfType<HandTrackingAdapter>();
            }

            if (trainingController != null && trainingController.patientModel != null)
            {
                patientRoot = trainingController.patientModel.transform;
            }
            else if (force || patientRoot == null)
            {
                patientRoot = FindPatientRoot();
            }

            if (force
                || chestReference == null
                || (!_chestReferenceFromDetector && Time.unscaledTime >= _nextChestResolveTime))
            {
                _nextChestResolveTime = Time.unscaledTime + 0.5f;
                chestReference = FindChestReference(out _chestReferenceFromDetector);
            }

            if (trainingController != null && trainingController != subscribedTrainingController)
            {
                SubscribeToTrainingEvents();
            }
        }

        private void SubscribeToTrainingEvents()
        {
            UnsubscribeFromTrainingEvents();

            if (trainingController == null)
            {
                return;
            }

            CPRTrainingController.OnPatientFell += HandlePatientFell;
            CPRTrainingController.OnPhaseChanged += HandlePhaseChanged;
            subscribedTrainingController = trainingController;
        }

        private void UnsubscribeFromTrainingEvents()
        {
            CPRTrainingController.OnPatientFell -= HandlePatientFell;
            CPRTrainingController.OnPhaseChanged -= HandlePhaseChanged;
            subscribedTrainingController = null;
        }

        private void HandlePatientFell()
        {
            ResolveSceneReferences();
            if (trainingController == null)
            {
                Debug.LogWarning("[AEDTrainingRuntime] Patient fell but training controller is missing.");
                return;
            }

            if (!trainingController.IsAEDTrainingSelected)
            {
                Debug.Log("[AEDTrainingRuntime] Patient fell in CPR-only mode. AED scenario will stay hidden.");
                return;
            }

            Debug.Log("[AEDTrainingRuntime] Patient fell in AED-capable mode - waiting for AED phase.");
        }

        private void HandlePhaseChanged(CPRTrainingController.TrainingPhase phase)
        {
            if (phase == CPRTrainingController.TrainingPhase.Intro ||
                phase == CPRTrainingController.TrainingPhase.WaitingStart ||
                phase == CPRTrainingController.TrainingPhase.Completed)
            {
                ResetScenario();
            }

            if (phase == CPRTrainingController.TrainingPhase.CPRInProgress)
            {
                ResetScenario();
            }

            if (phase == CPRTrainingController.TrainingPhase.AEDInProgress &&
                trainingController != null &&
                trainingController.IsAEDTrainingSelected &&
                !scenarioVisible)
            {
                RequestScenarioActivation();
            }
        }

        private void RequestScenarioActivation()
        {
            if (activationRoutine != null || scenarioVisible)
            {
                return;
            }

            activationRoutine = StartCoroutine(ActivateScenarioAfterFall());
        }

        private IEnumerator ActivateScenarioAfterFall()
        {
            yield return EnsureModelReady();

            ResolveSceneReferences();

            if (trainingController == null ||
                !trainingController.IsAEDTrainingSelected ||
                scenarioRoot == null ||
                patientRoot == null)
            {
                activationRoutine = null;
                yield break;
            }

            // 关闭这个开关时，AED 留在静置家具上，学员需要走过去操作。
            // 交互半径（InteractionRadius / PadInteractionRadius）是按「AED 就在患者旁」
            // 调过的，远距操作没验证过，所以默认仍然搬到患者身旁。
            if (relocateToPatientOnAedPhase)
            {
                PositionScenarioNearPatient();
            }

            trainingController.HideCprUiForAedMode();
            CreateOrUpdatePadTargets();
            EnsurePhysicalPads();
            leftPadPlaced = false;
            rightPadPlaced = false;
            electrodePackageOpened = false;
            carriedPad = null;
            scenarioVisible = true;
            scenarioStartTime = Time.time;
            ResetAedResult();
            scenarioRoot.SetActive(true);

            if (powerMarker != null)
                powerMarker.SetActive(true);

            if (shockMarker != null)
                shockMarker.SetActive(false);

            if (leftPadTarget != null)
            {
                leftPadTarget.SetActive(false);
                UpdatePadVisual(leftPadTarget, false);
            }

            if (rightPadTarget != null)
            {
                rightPadTarget.SetActive(false);
                UpdatePadVisual(rightPadTarget, false);
            }

            ResetPhysicalPads();
            SetPhysicalPadsActive(false);

            EnsureControlPanel();
            UpdateControlButtons();
            SetState(
                AEDState.Off,
                LanguageManager.Get(
                    "\u0041\u0045\u0044 \u5df2\u5c31\u7eea\uff0c\u8bf7\u5c06\u624b\u79fb\u52a8\u5230\u84dd\u8272\u6807\u8bb0\u5904\u5f00\u673a\u3002",
                    "AED \u306e\u6e96\u5099\u304c\u3067\u304d\u307e\u3057\u305f\u3002\u9752\u3044\u30de\u30fc\u30ab\u30fc\u306b\u624b\u3092\u5408\u308f\u305b\u3066\u96fb\u6e90\u3092\u5165\u308c\u3066\u304f\u3060\u3055\u3044\u3002",
                    "AED is ready. Move your hand to the blue marker to power it on."
                )
            );

            activationRoutine = null;
        }

        private IEnumerator EnsureModelReady()
        {
            if (modelPrepared && scenarioRoot != null)
            {
                yield break;
            }

            if (scenarioRoot == null)
            {
                scenarioRoot = new GameObject("AEDScenarioRoot");
                scenarioRoot.SetActive(false);

                modelHost = new GameObject("AEDModelHost");
                modelHost.transform.SetParent(scenarioRoot.transform, false);
                modelHost.SetActive(false);

                gltfAsset = modelHost.AddComponent<GltfAsset>();
                gltfAsset.LoadOnStartup = false;

                // glTFast 的默认值是 GenerateMipMaps=false、AnisotropicFilterLevel=1，
                // 电极片(1024²)和两个按钮贴图在斜视/远距时会闪烁走样。
                gltfAsset.ImportSettings = new ImportSettings
                {
                    GenerateMipMaps = true,
                    AnisotropicFilterLevel = 4,
                };
            }

            if (!loadRequested)
            {
                loadRequested = true;
                string modelUrl = ResolveModelUrl();
                gltfAsset.Url = modelUrl;
                modelHost.SetActive(true);
                modelLoadTask = gltfAsset.Load(modelUrl);
            }

            const float timeoutSeconds = 20f;
            float deadline = Time.unscaledTime + timeoutSeconds;

            while (Time.unscaledTime < deadline)
            {
                if (modelLoadTask != null)
                {
                    if (modelLoadTask.IsFaulted)
                    {
                        string message = modelLoadTask.Exception?.GetBaseException().Message ?? "Unknown load error.";
                        Debug.LogError($"[AEDTrainingRuntime] Failed to load AED model: {message}");
                        CreateFallbackAedModel(message);
                        yield break;
                    }

                    if (modelLoadTask.IsCanceled)
                    {
                        Debug.LogWarning("[AEDTrainingRuntime] AED model loading was canceled.");
                        CreateFallbackAedModel("Load task was canceled.");
                        yield break;
                    }

                    if (modelLoadTask.IsCompleted && !modelLoadTask.Result)
                    {
                        Debug.LogWarning("[AEDTrainingRuntime] AED model load completed but returned false.");
                        CreateFallbackAedModel("GLTFast returned false.");
                        yield break;
                    }
                }

                if (modelHost.transform.childCount > 0 &&
                    (modelLoadTask == null || modelLoadTask.IsCompleted))
                {
                    PrepareLoadedModel();
                    modelPrepared = true;
                    yield break;
                }

                yield return null;
            }

            Debug.LogWarning("[AEDTrainingRuntime] Timed out while waiting for the AED model to load.");
            CreateFallbackAedModel("Timed out waiting for GLB children.");
        }

        private void CreateFallbackAedModel(string reason)
        {
            if (modelPrepared || scenarioRoot == null || modelHost == null)
            {
                return;
            }

            Debug.LogWarning($"[AEDTrainingRuntime] Using fallback AED model. Reason: {reason}");

            for (int i = modelHost.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(modelHost.transform.GetChild(i).gameObject);
            }

            modelHost.SetActive(true);
            modelHost.transform.localScale = Vector3.one;
            modelHost.transform.localPosition = Vector3.zero;
            modelHost.transform.localRotation = Quaternion.identity;

            CreateFallbackPart("AEDFallbackBody", new Vector3(0f, 0.08f, 0f), new Vector3(0.42f, 0.12f, 0.3f), new Color(0.08f, 0.1f, 0.12f));
            CreateFallbackPart("AEDFallbackLid", new Vector3(0f, 0.17f, 0f), new Vector3(0.38f, 0.04f, 0.26f), new Color(0.18f, 0.22f, 0.28f));
            CreateFallbackPart("AEDFallbackScreen", new Vector3(0f, 0.2f, 0.08f), new Vector3(0.16f, 0.012f, 0.08f), new Color(0.08f, 0.55f, 0.42f));
            CreateFallbackPart("AEDFallbackPowerButton", new Vector3(0.11f, 0.205f, 0.075f), new Vector3(0.045f, 0.014f, 0.045f), powerMarkerColor);
            CreateFallbackPart("AEDFallbackShockButton", new Vector3(-0.11f, 0.205f, 0.075f), new Vector3(0.045f, 0.014f, 0.045f), shockMarkerColor);

            PrepareLoadedModel();
            modelPrepared = true;
        }

        private void CreateFallbackPart(string partName, Vector3 localPosition, Vector3 localScale, Color color)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = partName;
            part.transform.SetParent(modelHost.transform, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;

            var collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            var renderer = part.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateMarkerMaterial(color);
            }
        }

        private string ResolveModelUrl()
        {
            string streamingPath = CombineRuntimePath(Application.streamingAssetsPath, StreamingAssetModelPath);
            if (!IsUrlLike(streamingPath) && File.Exists(streamingPath))
            {
                return new Uri(streamingPath).AbsoluteUri;
            }

            if (IsUrlLike(streamingPath))
            {
                return streamingPath;
            }

#if UNITY_EDITOR
            string editorPath = Path.Combine(
                Application.dataPath,
                StreamingAssetModelPath.Replace('/', Path.DirectorySeparatorChar)
            );

            if (File.Exists(editorPath))
            {
                return new Uri(editorPath).AbsoluteUri;
            }
#endif

            return streamingPath;
        }

        private static string CombineRuntimePath(string rootPath, string relativePath)
        {
            string normalizedRelativePath = relativePath.Replace('\\', '/');

            if (IsUrlLike(rootPath))
            {
                return rootPath.TrimEnd('/') + "/" + normalizedRelativePath;
            }

            return Path.Combine(rootPath, normalizedRelativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static bool IsUrlLike(string value)
        {
            return value.IndexOf("://", StringComparison.Ordinal) >= 0 ||
                   value.StartsWith("jar:", StringComparison.OrdinalIgnoreCase);
        }

        private void PrepareLoadedModel()
        {
            FitModelToTrainingScale();
            ApplyManualAedTransforms();
            TuneLoadedMaterials();
            CreateOrUpdateAedMarkers();
            scenarioRoot.SetActive(false);
        }

        // .glb 里 12 个材质的 roughnessFactor 全是 0.218（一个 0.156），那是抛光塑料的数值。
        // 真实 AED 外壳是磨砂 ABS，应在 0.6-0.8。0.218 会在机身上打出又紧又亮的高光，
        // 而这个模型的机身没有任何贴图去打断高光，结果就是廉价塑料的观感。
        private const float AedHousingRoughness = 0.65f;
        private const float AedRubberRoughness = 0.85f;

        private void TuneLoadedMaterials()
        {
            if (modelHost == null)
                return;

            // 用 sharedMaterials：glTFast 的材质是运行时从 .glb 生成的，没有磁盘资产会被改坏，
            // 而且共享改一次就够，不会像 materials 那样为每个 Renderer 复制一份。
            var seen = new System.Collections.Generic.HashSet<Material>();
            int tuned = 0;

            foreach (var renderer in modelHost.GetComponentsInChildren<Renderer>(true))
            {
                foreach (var mat in renderer.sharedMaterials)
                {
                    if (mat == null || !seen.Add(mat))
                        continue;

                    bool isRubber = mat.name.IndexOf("rubber", System.StringComparison.OrdinalIgnoreCase) >= 0;
                    if (TrySetRoughness(mat, isRubber ? AedRubberRoughness : AedHousingRoughness))
                        tuned++;
                }
            }

            Debug.Log($"[AEDTrainingRuntime] 材质粗糙度已修正 {tuned}/{seen.Count} 个" +
                      $"（机身 {AedHousingRoughness}，橡胶 {AedRubberRoughness}）");
        }

        /// <summary>
        /// glTFast 的 shader 用 glTF 原生命名（roughnessFactor），
        /// Unity Standard 系用 smoothness 语义（= 1 - roughness）。两种都兼容，
        /// 这样以后项目迁到 URP 也不用改这里。
        /// </summary>
        private static bool TrySetRoughness(Material mat, float roughness)
        {
            if (mat.HasProperty("roughnessFactor"))
            {
                mat.SetFloat("roughnessFactor", roughness);
                return true;
            }
            if (mat.HasProperty("_Smoothness"))
            {
                mat.SetFloat("_Smoothness", 1f - roughness);
                return true;
            }
            if (mat.HasProperty("_Glossiness"))
            {
                mat.SetFloat("_Glossiness", 1f - roughness);
                return true;
            }

            Debug.LogWarning($"[AEDTrainingRuntime] 材质「{mat.name}」(shader {mat.shader?.name}) " +
                             "没有可识别的粗糙度属性，已跳过");
            return false;
        }

        private void ApplyManualAedTransforms()
        {
            if (modelHost == null)
            {
                return;
            }

            modelHost.transform.localPosition = AedModelHostLocalPosition;
            modelHost.transform.localRotation = Quaternion.identity;
            modelHost.transform.localScale = AedModelHostLocalScale;

            Transform electrode = FindDescendantByName(modelHost.transform, "electrode");
            if (electrode != null)
            {
                electrode.localPosition = AedElectrodeLocalPosition;
                electrode.localRotation = Quaternion.Euler(AedElectrodeLocalEulerAngles);
                electrode.localScale = Vector3.one;
                electrodePackageTarget = electrode.gameObject;
            }
        }

        private static Transform FindDescendantByName(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            foreach (Transform child in root)
            {
                if (string.Equals(child.name, childName, StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }

                Transform match = FindDescendantByName(child, childName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private void FitModelToTrainingScale()
        {
            modelHost.transform.localScale = Vector3.one;
            modelHost.transform.localPosition = Vector3.zero;

            Bounds bounds = CalculateModelBounds();
            float maxDimension = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            if (maxDimension > 0.0001f)
            {
                float uniformScale = TargetModelSizeMeters / maxDimension;
                modelHost.transform.localScale = Vector3.one * uniformScale;
            }

            bounds = CalculateModelBounds();
            maxDimension = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            if (maxDimension > TargetModelSizeMeters * 1.15f)
            {
                float correction = TargetModelSizeMeters / maxDimension;
                modelHost.transform.localScale *= correction;
                bounds = CalculateModelBounds();
            }

            Vector3 localCenter = scenarioRoot.transform.InverseTransformPoint(bounds.center);
            float bottomY = localCenter.y - bounds.extents.y;

            modelHost.transform.localPosition += new Vector3(-localCenter.x, -bottomY + 0.01f, -localCenter.z);
        }

        private Bounds CalculateModelBounds()
        {
            var renderers = modelHost.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(modelHost.transform.position, Vector3.one * 0.25f);
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private void CreateOrUpdateAedMarkers()
        {
            Bounds bounds = CalculateModelBounds();
            Vector3 localCenter = scenarioRoot.transform.InverseTransformPoint(bounds.center);
            Vector3 topLocal = localCenter + Vector3.up * (bounds.extents.y + MarkerHeightOffset);
            float horizontalOffset = Mathf.Max(bounds.extents.x * 0.4f, 0.08f);

            powerMarker = CreateOrReuseMarker(
                powerMarker,
                "AEDPowerMarker",
                powerMarkerColor,
                AedPowerMarkerLocalPosition
            );

            shockMarker = CreateOrReuseMarker(
                shockMarker,
                "AEDShockMarker",
                shockMarkerColor,
                topLocal - Vector3.right * horizontalOffset
            );

            shockMarker.SetActive(false);
        }

        private GameObject CreateOrReuseMarker(
            GameObject marker,
            string markerName,
            Color color,
            Vector3 localPosition
        )
        {
            if (marker == null)
            {
                marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = markerName;
                marker.transform.SetParent(scenarioRoot.transform, false);
                marker.transform.localScale = Vector3.one * MarkerScale;

                var collider = marker.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }

                var renderer = marker.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = CreateMarkerMaterial(color);
                }
            }

            marker.transform.localPosition = localPosition;
            marker.SetActive(true);
            return marker;
        }

        private Material CreateMarkerMaterial(Color color)
        {
            var material = new Material(Shader.Find("Standard"));
            material.color = color;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 1.8f);
            return material;
        }

        private void CreateOrUpdatePadTargets()
        {
            if (patientRoot == null)
            {
                return;
            }

            Transform chest = chestReference != null ? chestReference : patientRoot;
            Vector3 chestPosition = chest.position;
            Vector3 patientRight = patientRoot.right.normalized;
            Vector3 patientHeadDirection = Vector3.Cross(Vector3.up, patientRight).normalized;

            if (patientHeadDirection.sqrMagnitude < 0.01f)
            {
                patientHeadDirection = patientRoot.forward.normalized;
            }

            leftPadTarget = CreateOrReusePatientMarker(
                leftPadTarget,
                "AEDLeftPadTarget",
                chestPosition + patientRight * 0.12f + patientHeadDirection * 0.10f + Vector3.up * 0.022f,
                padMarkerColor
            );

            rightPadTarget = CreateOrReusePatientMarker(
                rightPadTarget,
                "AEDRightPadTarget",
                chestPosition - patientRight * 0.20f - patientHeadDirection * 0.08f + Vector3.up * 0.022f,
                padMarkerColor
            );

            Quaternion padRotation = Quaternion.LookRotation(patientHeadDirection.normalized, Vector3.up);
            leftPadTarget.transform.rotation = padRotation * Quaternion.Euler(0f, 0f, -12f);
            rightPadTarget.transform.rotation = padRotation * Quaternion.Euler(0f, 0f, 12f);

            leftPadTarget.SetActive(false);
            rightPadTarget.SetActive(false);
            UpdatePadVisual(leftPadTarget, false);
            UpdatePadVisual(rightPadTarget, false);
            EnsurePadTargetTrigger(leftPadTarget, PadKind.Left);
            EnsurePadTargetTrigger(rightPadTarget, PadKind.Right);
        }

        private void EnsurePhysicalPads()
        {
            if (scenarioRoot == null)
            {
                return;
            }

            leftPhysicalPad = CreateOrReusePhysicalPad(leftPhysicalPad, "AEDUpperElectrodePad", PadKind.Left);
            rightPhysicalPad = CreateOrReusePhysicalPad(rightPhysicalPad, "AEDLowerElectrodePad", PadKind.Right);
            ResetPhysicalPads();
        }

        private GameObject CreateOrReusePhysicalPad(GameObject pad, string padName, PadKind padKind)
        {
            if (pad == null)
            {
                pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pad.name = padName;
                pad.transform.SetParent(scenarioRoot.transform, false);
                pad.transform.localScale = new Vector3(PhysicalPadWidth, PhysicalPadHeight, PhysicalPadDepth);

                var renderer = pad.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = CreatePadMaterial(new Color(1f, 0.98f, 0.9f, 1f));
                }

                var rigidbody = pad.AddComponent<Rigidbody>();
                rigidbody.useGravity = false;
                rigidbody.isKinematic = true;
                rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

                var grabInteractable = pad.AddComponent<XRGrabInteractable>();
                grabInteractable.movementType = XRBaseInteractable.MovementType.Kinematic;
                grabInteractable.throwOnDetach = false;
                grabInteractable.useDynamicAttach = true;
                grabInteractable.enabled = false;

                var grabbable = pad.AddComponent<AedPadGrabbable>();
                grabbable.Initialize(this, padKind);

                CreatePadGraphics(pad.transform, padKind);
            }

            var component = pad.GetComponent<AedPadGrabbable>();
            if (component != null)
            {
                component.Initialize(this, padKind);
            }

            var padRigidbody = pad.GetComponent<Rigidbody>();
            if (padRigidbody != null)
            {
                EnsureMetaGrabInteraction(pad, padRigidbody);
            }

            return pad;
        }

        private void CreatePadGraphics(Transform parent, PadKind padKind)
        {
            CreatePadDetail(parent, "YellowAdhesiveArea", new Vector3(-0.03f, 0.56f, 0f), new Vector3(0.72f, 0.12f, 0.78f), new Color(1f, 0.82f, 0.18f, 1f));
            CreatePadDetail(parent, "WhitePeelTab", new Vector3(0.39f, 0.58f, 0f), new Vector3(0.18f, 0.14f, 0.88f), new Color(1f, 1f, 0.96f, 1f));
            CreatePadDetail(parent, "BlackGuideLine", new Vector3(-0.05f, 0.62f, -0.24f), new Vector3(0.76f, 0.08f, 0.035f), Color.black);
            CreatePadDetail(parent, "BodyPlacementIcon", new Vector3(-0.12f, 0.64f, 0.08f), new Vector3(0.26f, 0.09f, 0.28f), new Color(0.95f, 0.72f, 0.28f, 1f));
            CreatePadDetail(parent, "RedAdultLabel", new Vector3(0.31f, 0.66f, 0.24f), new Vector3(0.10f, 0.12f, 0.28f), new Color(0.95f, 0.05f, 0.03f, 1f));
            CreatePadDetail(parent, "CableSocket", new Vector3(-0.45f, 0.64f, -0.30f), new Vector3(0.08f, 0.12f, 0.08f), new Color(0.15f, 0.15f, 0.15f, 1f));

            float side = padKind == PadKind.Left ? -1f : 1f;
            CreatePadDetail(parent, "PadIndexStripe", new Vector3(side * 0.18f, 0.68f, -0.02f), new Vector3(0.035f, 0.10f, 0.62f), new Color(0.08f, 0.08f, 0.08f, 1f));
        }

        private void CreatePadDetail(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Color color)
        {
            var detail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            detail.name = name;
            detail.transform.SetParent(parent, false);
            detail.transform.localPosition = localPosition;
            detail.transform.localScale = localScale;

            var collider = detail.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            var renderer = detail.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreatePadMaterial(color);
            }
        }

        private Material CreatePadMaterial(Color color)
        {
            var material = new Material(Shader.Find("Standard"));
            material.color = color;
            material.SetFloat("_Glossiness", 0.25f);
            return material;
        }

        private void ResetPhysicalPads()
        {
            PositionPhysicalPadsNearAed();
            SetPhysicalPadsActive(electrodePackageOpened);
        }

        private void PositionPhysicalPadsNearAed()
        {
            if (scenarioRoot == null)
            {
                return;
            }

            // 电极贴片出现在 AED 模型旁边（以 AED 模型本地位置为锚点偏移）
            Vector3 aedAnchor = (modelPrepared && modelHost != null)
                ? scenarioRoot.transform.InverseTransformPoint(modelHost.transform.position)
                : AedModelHostLocalPosition;

            if (leftPhysicalPad != null)
            {
                leftPhysicalPad.transform.SetParent(scenarioRoot.transform, false);
                leftPhysicalPad.transform.localPosition = aedAnchor + new Vector3(-0.18f, 0.14f, -0.14f);
                leftPhysicalPad.transform.localRotation = Quaternion.Euler(0f, -18f, 0f);
                PreparePadForGrab(leftPhysicalPad);
            }

            if (rightPhysicalPad != null)
            {
                rightPhysicalPad.transform.SetParent(scenarioRoot.transform, false);
                rightPhysicalPad.transform.localPosition = aedAnchor + new Vector3(0.04f, 0.14f, -0.14f);
                rightPhysicalPad.transform.localRotation = Quaternion.Euler(0f, 18f, 0f);
                PreparePadForGrab(rightPhysicalPad);
            }
        }

        private void SetPhysicalPadActive(GameObject pad, bool active)
        {
            if (pad != null)
            {
                pad.SetActive(active);
            }
        }

        private void SetPhysicalPadsActive(bool active)
        {
            SetPhysicalPadActive(leftPhysicalPad, active);
            SetPhysicalPadActive(rightPhysicalPad, active);
        }

        private void PreparePadForGrab(GameObject pad)
        {
            var grabbable = pad.GetComponent<AedPadGrabbable>();
            if (grabbable != null)
            {
                grabbable.IsPlaced = false;
            }

            var rigidbody = pad.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                rigidbody.isKinematic = true;
                rigidbody.useGravity = false;
                rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            }

            var grabInteractable = pad.GetComponent<XRGrabInteractable>();
            if (grabInteractable != null)
            {
                grabInteractable.enabled = false;
            }

            SetMetaGrabInteractionsEnabled(pad, true);
        }

        private void EnsureMetaGrabInteraction(GameObject pad, Rigidbody rigidbody)
        {
            if (pad == null || rigidbody == null)
            {
                return;
            }

            var grabbable = pad.GetComponent<Grabbable>();
            if (grabbable == null)
            {
                grabbable = pad.AddComponent<Grabbable>();
            }

            grabbable.InjectOptionalRigidbody(rigidbody);
            grabbable.InjectOptionalTargetTransform(pad.transform);
            grabbable.InjectOptionalThrowWhenUnselected(false);
            grabbable.InjectOptionalKinematicWhileSelected(true);
            grabbable.MaxGrabPoints = 1;

            var grabInteractable = pad.GetComponent<GrabInteractable>();
            if (grabInteractable == null)
            {
                grabInteractable = pad.AddComponent<GrabInteractable>();
            }

            grabInteractable.InjectRigidbody(rigidbody);
            grabInteractable.InjectOptionalPointableElement(grabbable);
            grabInteractable.UseClosestPointAsGrabSource = true;
            grabInteractable.InjectOptionalReleaseDistance(0.9f);
            grabInteractable.ResetGrabOnGrabsUpdated = true;

            var handGrabInteractable = pad.GetComponent<HandGrabInteractable>();
            if (handGrabInteractable == null)
            {
                handGrabInteractable = pad.AddComponent<HandGrabInteractable>();
            }

            handGrabInteractable.InjectAllHandGrabInteractable(
                GrabTypeFlags.All,
                rigidbody,
                GrabbingRule.DefaultPinchRule,
                GrabbingRule.DefaultPalmRule
            );
            handGrabInteractable.InjectOptionalPointableElement(grabbable);

            var distanceGrabInteractable = pad.GetComponent<DistanceGrabInteractable>();
            if (distanceGrabInteractable == null)
            {
                distanceGrabInteractable = pad.AddComponent<DistanceGrabInteractable>();
            }

            distanceGrabInteractable.InjectRigidbody(rigidbody);
            distanceGrabInteractable.InjectOptionalPointableElement(grabbable);
            distanceGrabInteractable.InjectOptionalGrabSource(pad.transform);

            var distanceHandGrabInteractable = pad.GetComponent<DistanceHandGrabInteractable>();
            if (distanceHandGrabInteractable == null)
            {
                distanceHandGrabInteractable = pad.AddComponent<DistanceHandGrabInteractable>();
            }

            distanceHandGrabInteractable.InjectAllDistanceHandGrabInteractable(
                GrabTypeFlags.All,
                rigidbody,
                GrabbingRule.DefaultPinchRule,
                GrabbingRule.DefaultPalmRule
            );
            distanceHandGrabInteractable.InjectOptionalPointableElement(grabbable);

            var relay = pad.GetComponent<AedMetaGrabRelay>();
            if (relay == null)
            {
                relay = pad.AddComponent<AedMetaGrabRelay>();
            }

            relay.Initialize(this, pad.GetComponent<AedPadGrabbable>(), grabbable);
        }

        private void SetMetaGrabInteractionsEnabled(GameObject pad, bool enabled)
        {
            if (pad == null)
            {
                return;
            }

            SetComponentEnabled<Grabbable>(pad, enabled);
            SetComponentEnabled<GrabInteractable>(pad, enabled);
            SetComponentEnabled<HandGrabInteractable>(pad, enabled);
            SetComponentEnabled<DistanceGrabInteractable>(pad, enabled);
            SetComponentEnabled<DistanceHandGrabInteractable>(pad, enabled);
            SetComponentEnabled<AedMetaGrabRelay>(pad, enabled);
        }

        private void SetComponentEnabled<T>(GameObject owner, bool enabled) where T : Behaviour
        {
            var component = owner.GetComponent<T>();
            if (component != null)
            {
                component.enabled = enabled;
            }
        }

        private void EnsurePadTargetTrigger(GameObject target, PadKind padKind)
        {
            if (target == null)
            {
                return;
            }

            var collider = target.GetComponent<SphereCollider>();
            if (collider == null)
            {
                collider = target.AddComponent<SphereCollider>();
            }

            collider.isTrigger = true;
            float maxScale = Mathf.Max(
                Mathf.Abs(target.transform.lossyScale.x),
                Mathf.Max(Mathf.Abs(target.transform.lossyScale.y), Mathf.Abs(target.transform.lossyScale.z))
            );
            collider.radius = maxScale > 0.0001f ? PhysicalPadSnapRadius / maxScale : PhysicalPadSnapRadius;

            var rigidbody = target.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = target.AddComponent<Rigidbody>();
            }

            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;

            var trigger = target.GetComponent<AedPadSnapTarget>();
            if (trigger == null)
            {
                trigger = target.AddComponent<AedPadSnapTarget>();
            }

            trigger.Initialize(this, padKind);
        }

        private GameObject CreateOrReusePatientMarker(
            GameObject marker,
            string markerName,
            Vector3 position,
            Color color
        )
        {
            if (marker == null)
            {
                marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = markerName;
                marker.transform.localScale = Vector3.one * (MarkerScale * 0.85f);

                var collider = marker.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }

                var renderer = marker.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = CreateMarkerMaterial(color);
                }
            }

            marker.transform.position = position;
            return marker;
        }

        private void UpdatePadVisual(GameObject marker, bool completed)
        {
            if (marker == null)
            {
                return;
            }

            var renderer = marker.GetComponent<Renderer>();
            if (renderer == null || renderer.sharedMaterial == null)
            {
                return;
            }

            Color color = completed ? completedPadColor : padMarkerColor;
            renderer.sharedMaterial.color = color;
            renderer.sharedMaterial.SetColor("_EmissionColor", color * 1.8f);
        }

        /// <summary>
        /// 开局把 AED 摆到静置家具上。模型是异步从 StreamingAssets 加载的，所以要先等
        /// EnsureModelReady()。这里只让模型本体现身：交互标记、贴片目标和世界 UI 由
        /// ResetScenario 单独关闭，scenarioVisible 保持 false，
        /// 因此 Update() 里的交互处理不会提前启动。
        /// </summary>
        private IEnumerator ShowAedAtRestingPlace()
        {
            yield return EnsureModelReady();

            if (scenarioRoot == null || modelHost == null)
            {
                yield break;
            }

            HideInstructionOverlay();
            scenarioRoot.SetActive(true);   // 先激活，包围盒才读得到
            PlaceScenarioOnRestingSpot();
        }

        private void PlaceScenarioOnRestingSpot()
        {
            Transform furniture = string.IsNullOrWhiteSpace(restingFurnitureName)
                ? null
                : GameObject.Find(restingFurnitureName)?.transform;

            if (!TryGetSurfaceTop(furniture, out Vector3 surfaceTop))
            {
                surfaceTop = AedFloorFallbackPosition;
                Debug.LogWarning($"[AEDTrainingRuntime] 未找到家具「{restingFurnitureName}」，AED 退回地面摆放");
            }

            // 让 AED 面板朝向房间中心，学员一眼能看到
            Vector3 towardRoomCenter = new Vector3(-surfaceTop.x, 0f, -surfaceTop.z);
            scenarioRoot.transform.rotation = towardRoomCenter.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(towardRoomCenter.normalized, Vector3.up)
                : Quaternion.identity;

            // modelHost 在 scenarioRoot 局部空间里有约 1.5m 的偏移，
            // 所以要反推 scenarioRoot 的位置，才能让「模型」而不是「根节点」落在台面上。
            scenarioRoot.transform.position =
                surfaceTop - scenarioRoot.transform.rotation * AedModelHostLocalPosition;

            // 再用模型真实的渲染包围盒把底面精确压到台面，
            // 这样不依赖任何硬编码高度，换模型也不会悬空或陷进去。
            if (TryGetWorldBounds(modelHost, out Bounds modelBounds))
            {
                scenarioRoot.transform.position += Vector3.up * (surfaceTop.y - modelBounds.min.y);
            }

            HideScenarioInteractables();

            worldUiPoseFixed = false;
            Debug.Log($"[AEDTrainingRuntime] AED 静置于「{(furniture != null ? furniture.name : "地面")}」" +
                      $"，台面 y={surfaceTop.y:F3}");
        }

        /// <summary>
        /// 静置状态下只该看到 AED 本体。scenarioRoot.SetActive(true) 会把所有
        /// 没被单独禁用的子物体一并放出来 —— powerMarker/shockMarker 在
        /// PrepareLoadedModel() 阶段就已创建且处于激活状态，会露出一个青色小球。
        /// 贴片和目标点要到 AED 阶段才创建，这里一并做防御性处理。
        /// </summary>
        private void HideScenarioInteractables()
        {
            // 只关那些确实有对应 SetActive(true) 会把它们放回来的对象：
            //   powerMarker  -> BeginScenario
            //   shockMarker  -> 判定建议电击时
            //   *PadTarget   -> 电极包打开后
            // controlPanel 和 *PhysicalPad 全项目都没有 SetActive(true)，
            // 关掉就再也开不回来，所以这里绝对不能碰。
            SetActiveSafe(powerMarker, false);
            SetActiveSafe(shockMarker, false);
            SetActiveSafe(leftPadTarget, false);
            SetActiveSafe(rightPadTarget, false);
            HideInstructionOverlay();
        }

        private static void SetActiveSafe(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active)
            {
                go.SetActive(active);
            }
        }

        private static bool TryGetSurfaceTop(Transform furniture, out Vector3 top)
        {
            top = Vector3.zero;
            if (furniture == null || !TryGetWorldBounds(furniture.gameObject, out Bounds b))
            {
                return false;
            }

            top = new Vector3(b.center.x, b.max.y, b.center.z);
            return true;
        }

        private static bool TryGetWorldBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            if (root == null)
            {
                return false;
            }

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return false;
            }

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            return true;
        }

        private void PositionScenarioNearPatient()
        {
            if (scenarioRoot == null || patientRoot == null)
            {
                return;
            }

            Vector3 worldOffset =
                patientRoot.right * patientSideOffset.x +
                Vector3.up * patientSideOffset.y +
                patientRoot.forward * patientSideOffset.z;

            scenarioRoot.transform.position = patientRoot.position + worldOffset;

            Vector3 forward = Vector3.ProjectOnPlane(patientRoot.position - scenarioRoot.transform.position, Vector3.up);
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.forward;
            }

            scenarioRoot.transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            worldUiPoseFixed = false;
            UpdateWorldUiPose();
        }

        private void UpdateWorldUiPose()
        {
            if (aedWorldCanvasRect == null || worldUiPoseFixed)
            {
                return;
            }

            if (patientRoot != null)
            {
                Vector3 headPosition = GetPatientHeadWorldPosition();
                Transform viewTransform = GetPrimaryViewTransform();
                Vector3 towardViewer = Vector3.zero;
                if (viewTransform != null)
                {
                    towardViewer = Vector3.ProjectOnPlane(viewTransform.position - headPosition, Vector3.up);
                }

                if (towardViewer.sqrMagnitude < 0.001f && patientRoot != null)
                {
                    towardViewer = Vector3.ProjectOnPlane(-patientRoot.forward, Vector3.up);
                }

                Vector3 uiPosition = headPosition + Vector3.up * AedWorldUiHeightAboveHead;
                if (towardViewer.sqrMagnitude > 0.001f)
                {
                    uiPosition += towardViewer.normalized * AedWorldUiDistanceTowardViewer;
                }

                aedWorldCanvasRect.SetParent(transform, true);
                aedWorldCanvasRect.position = uiPosition;

                if (viewTransform != null)
                {
                    Vector3 faceDirection = aedWorldCanvasRect.position - viewTransform.position;
                    if (faceDirection.sqrMagnitude > 0.001f)
                    {
                        aedWorldCanvasRect.rotation = Quaternion.LookRotation(faceDirection.normalized, Vector3.up);
                    }

                    if (aedWorldCanvas != null)
                    {
                        var viewCamera = viewTransform.GetComponent<Camera>();
                        if (viewCamera != null)
                            aedWorldCanvas.worldCamera = viewCamera;
                    }
                }
                else
                {
                    aedWorldCanvasRect.rotation = Quaternion.Euler(AedWorldUiLocalEulerAngles);
                }
            }
            else if (scenarioRoot != null)
            {
                aedWorldCanvasRect.SetParent(scenarioRoot.transform, false);
                aedWorldCanvasRect.localPosition = AedWorldUiLocalPosition;
                aedWorldCanvasRect.localRotation = Quaternion.Euler(AedWorldUiLocalEulerAngles);
            }
            else
            {
                return;
            }

            aedWorldCanvasRect.localScale = Vector3.one * WorldUiScale;
            worldUiPoseFixed = true;
        }

        private Vector3 GetPatientHeadWorldPosition()
        {
            Transform head = FindPatientHeadReference();
            if (head != null)
            {
                return head.position;
            }

            if (TryGetPatientRendererBounds(out Bounds bounds))
            {
                Vector3 chestPosition = chestReference != null ? chestReference.position : bounds.center;
                Vector3 patientRight = patientRoot != null ? patientRoot.right.normalized : Vector3.right;
                Vector3 headDirection = Vector3.Cross(Vector3.up, patientRight).normalized;
                if (headDirection.sqrMagnitude < 0.001f && patientRoot != null)
                    headDirection = patientRoot.forward.normalized;
                if (headDirection.sqrMagnitude < 0.001f)
                    headDirection = Vector3.forward;

                Vector3 towardHead = bounds.center + headDirection * bounds.extents.magnitude;
                return Vector3.Lerp(chestPosition, towardHead, 0.55f);
            }

            return chestReference != null ? chestReference.position : patientRoot.position;
        }

        private Transform FindPatientHeadReference()
        {
            if (patientRoot == null)
            {
                return null;
            }

            string[] headNames = { "Head", "Neck", "頭", "头" };
            foreach (string headName in headNames)
            {
                Transform result = FindChildRecursive(patientRoot, headName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private bool TryGetPatientRendererBounds(out Bounds bounds)
        {
            bounds = default;
            if (patientRoot == null)
            {
                return false;
            }

            var renderers = patientRoot.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;
            foreach (var renderer in renderers)
            {
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds;
        }

        private Transform GetPrimaryViewTransform()
        {
            var rig = FindObjectOfType<OVRCameraRig>();
            if (rig != null && rig.centerEyeAnchor != null)
            {
                return rig.centerEyeAnchor;
            }

            if (Camera.main != null)
            {
                return Camera.main.transform;
            }

            var camera = FindObjectOfType<Camera>();
            return camera != null ? camera.transform : null;
        }

        private void HandleHandInteraction()
        {
            if (!TryGetInteractionPoint(out Vector3 interactionPoint))
            {
                hoveredTarget = null;
                targetHoverStartedAt = -1f;
                return;
            }

            GameObject target = GetCurrentTarget(interactionPoint);
            if (target == null)
            {
                hoveredTarget = null;
                targetHoverStartedAt = -1f;
                return;
            }

            if (hoveredTarget != target)
            {
                hoveredTarget = target;
                targetHoverStartedAt = Time.unscaledTime;
                return;
            }

            if (Time.unscaledTime - targetHoverStartedAt < InteractionHoldSeconds)
            {
                return;
            }

            hoveredTarget = null;
            targetHoverStartedAt = -1f;
            TriggerTarget(target);
        }

        private void HandlePhysicalPadProximityInteraction()
        {
            if (currentState != AEDState.Off && currentState != AEDState.On)
            {
                carriedPad = null;
                return;
            }

            List<Vector3> interactionPoints = GetInteractionPoints();
            if (interactionPoints.Count == 0)
            {
                carriedPad = null;
                return;
            }

            if (TryHandleDirectAedInteraction(interactionPoints))
            {
                carriedPad = null;
                return;
            }

            if (IsAnyMetaPadSelected())
            {
                carriedPad = null;
                return;
            }

            Vector3 interactionPoint = GetBestPointForPadInteraction(interactionPoints);
            if (carriedPad == null || carriedPad.IsPlaced || !carriedPad.gameObject.activeInHierarchy)
            {
                carriedPad = FindNearestAvailablePad(interactionPoint);
                if (carriedPad == null)
                {
                    return;
                }
            }

            carriedPad.transform.position = interactionPoint;
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                Vector3 toCamera = Vector3.ProjectOnPlane(mainCamera.transform.position - carriedPad.transform.position, Vector3.up);
                if (toCamera.sqrMagnitude > 0.001f)
                {
                    carriedPad.transform.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
                }
            }

            GameObject target = GetTargetForPad(carriedPad.Kind);
            if (target != null && target.activeInHierarchy &&
                Vector3.Distance(carriedPad.transform.position, target.transform.position) <= PhysicalPadSnapRadius)
            {
                if (currentState == AEDState.Off)
                {
                    TurnOnAed();
                }

                PlacePhysicalPad(carriedPad, target);
            }
        }

        private bool IsAnyMetaPadSelected()
        {
            return IsMetaPadSelected(leftPhysicalPad) || IsMetaPadSelected(rightPhysicalPad);
        }

        private bool IsMetaPadSelected(GameObject padObject)
        {
            if (padObject == null || !padObject.activeInHierarchy)
            {
                return false;
            }

            var grabbable = padObject.GetComponent<Grabbable>();
            return grabbable != null && grabbable.SelectingPointsCount > 0;
        }

        private Vector3 GetBestPointForPadInteraction(List<Vector3> interactionPoints)
        {
            if (interactionPoints == null || interactionPoints.Count == 0)
            {
                return Vector3.zero;
            }

            Vector3 bestPoint = interactionPoints[0];
            float bestDistance = float.MaxValue;

            foreach (Vector3 point in interactionPoints)
            {
                ConsiderDistanceToPad(point, leftPhysicalPad, ref bestPoint, ref bestDistance);
                ConsiderDistanceToPad(point, rightPhysicalPad, ref bestPoint, ref bestDistance);
                ConsiderDistanceToPad(point, leftPadTarget, ref bestPoint, ref bestDistance);
                ConsiderDistanceToPad(point, rightPadTarget, ref bestPoint, ref bestDistance);
            }

            return bestPoint;
        }

        private void ConsiderDistanceToPad(Vector3 point, GameObject target, ref Vector3 bestPoint, ref float bestDistance)
        {
            if (target == null || !target.activeInHierarchy)
            {
                return;
            }

            float distance = Vector3.Distance(point, target.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestPoint = point;
            }
        }

        private bool TryHandleDirectAedInteraction(List<Vector3> interactionPoints)
        {
            if (currentState != AEDState.On)
            {
                return false;
            }

            foreach (Vector3 point in interactionPoints)
            {
                if (!electrodePackageOpened && IsPointNearElectrodePackage(point))
                {
                    OpenElectrodePackage();
                    return true;
                }

                // Pad placement is intentionally handled by moving the physical pad objects
                // onto the chest targets. Do not auto-place pads just because the hand
                // touches a marker.
            }

            return false;
        }

        private AedPadGrabbable FindNearestAvailablePad(Vector3 interactionPoint)
        {
            AedPadGrabbable bestPad = null;
            float bestDistance = PhysicalPadGrabRadius;

            ConsiderPhysicalPad(leftPhysicalPad, interactionPoint, ref bestPad, ref bestDistance);
            ConsiderPhysicalPad(rightPhysicalPad, interactionPoint, ref bestPad, ref bestDistance);

            return bestPad;
        }

        private void ConsiderPhysicalPad(
            GameObject padObject,
            Vector3 interactionPoint,
            ref AedPadGrabbable bestPad,
            ref float bestDistance
        )
        {
            if (padObject == null || !padObject.activeInHierarchy)
            {
                return;
            }

            var pad = padObject.GetComponent<AedPadGrabbable>();
            if (pad == null || pad.IsPlaced || IsPadKindPlaced(pad.Kind))
            {
                return;
            }

            float distance = Vector3.Distance(interactionPoint, padObject.transform.position);
            if (distance <= bestDistance)
            {
                bestDistance = distance;
                bestPad = pad;
            }
        }

        private bool TryGetInteractionPoint(out Vector3 interactionPoint)
        {
            interactionPoint = Vector3.zero;

            List<Vector3> interactionPoints = GetInteractionPoints();
            if (interactionPoints.Count > 0)
            {
                interactionPoint = interactionPoints[0];
                return true;
            }

            return false;
        }

        private List<Vector3> GetInteractionPoints()
        {
            var points = new List<Vector3>();

            if (TryGetVRCursorPosition(out Vector3 cursorPosition))
            {
                points.Add(cursorPosition);
            }

            if (TryGetBestHandInteractionPoint(out Vector3 bestHandPoint))
            {
                AddInteractionPoint(points, bestHandPoint);
            }

            AddTrackedHandInteractionPoints(points);

            if (handTrackingAdapter != null)
            {
                Vector3 handPosition = handTrackingAdapter.ActiveHandPosition;
                if (handPosition.sqrMagnitude > 0.0001f)
                {
                    AddInteractionPoint(points, handPosition);
                }
            }

            AddRigAnchorInteractionPoints(points);
            AddNamedHandTransformInteractionPoints(points);
            AddCameraFallbackInteractionPoints(points);

            return points;
        }

        private bool TryGetVRCursorPosition(out Vector3 position)
        {
            position = Vector3.zero;
            GameObject cursor = GameObject.Find("VRCursor");
            if (cursor == null || !cursor.activeInHierarchy || cursor.transform.position.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            position = cursor.transform.position;
            return true;
        }

        private void AddRigAnchorInteractionPoints(List<Vector3> points)
        {
            var rig = FindObjectOfType<OVRCameraRig>();
            if (rig == null)
            {
                return;
            }

            if (rig.leftHandAnchor != null)
            {
                AddInteractionPoint(points, rig.leftHandAnchor.position);
            }

            if (rig.rightHandAnchor != null)
            {
                AddInteractionPoint(points, rig.rightHandAnchor.position);
            }

            if (rig.leftControllerAnchor != null)
            {
                AddInteractionPoint(points, rig.leftControllerAnchor.position);
            }

            if (rig.rightControllerAnchor != null)
            {
                AddInteractionPoint(points, rig.rightControllerAnchor.position);
            }
        }

        private void AddTrackedHandInteractionPoints(List<Vector3> points)
        {
            var hands = FindObjectsOfType<OVRHand>(true);
            foreach (var hand in hands)
            {
                if (hand == null || !hand.isActiveAndEnabled)
                {
                    continue;
                }

                AddInteractionPoint(points, hand.transform.position);

                var skeleton = hand.GetComponent<OVRSkeleton>();
                if (skeleton == null || skeleton.Bones == null)
                {
                    continue;
                }

                foreach (var bone in skeleton.Bones)
                {
                    if (bone.Transform == null)
                    {
                        continue;
                    }

                    string boneName = bone.Id.ToString();
                    if (boneName.IndexOf("Tip", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        boneName.IndexOf("Index", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        boneName.IndexOf("Middle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        boneName.IndexOf("WristRoot", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        AddInteractionPoint(points, bone.Transform.position);
                    }
                }
            }
        }

        private void AddNamedHandTransformInteractionPoints(List<Vector3> points)
        {
            var transforms = FindObjectsOfType<Transform>(true);
            foreach (var transform in transforms)
            {
                if (transform == null || !transform.gameObject.activeInHierarchy)
                {
                    continue;
                }

                string name = transform.name;
                bool looksLikeHandPoint =
                    name.IndexOf("IndexTip", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("FingerTip", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("PointerPose", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("HandAnchor", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("ControllerAnchor", StringComparison.OrdinalIgnoreCase) >= 0;

                if (!looksLikeHandPoint)
                {
                    continue;
                }

                AddInteractionPoint(points, transform.position);
            }
        }

        private void AddCameraFallbackInteractionPoints(List<Vector3> points)
        {
            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }

            AddInteractionPoint(points, mainCamera.transform.position + mainCamera.transform.forward * 0.45f);
            AddInteractionPoint(points, mainCamera.transform.position + mainCamera.transform.forward * 0.75f);
            AddInteractionPoint(points, mainCamera.transform.position + mainCamera.transform.forward * 1.05f);
        }

        private void AddInteractionPoint(List<Vector3> points, Vector3 point)
        {
            if (point.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            for (int i = 0; i < points.Count; i++)
            {
                if (Vector3.Distance(points[i], point) < 0.02f)
                {
                    return;
                }
            }

            points.Add(point);
        }

        private bool TryGetBestHandInteractionPoint(out Vector3 position)
        {
            position = Vector3.zero;
            float bestDistance = float.MaxValue;
            bool found = false;

            var hands = FindObjectsOfType<OVRHand>(true);
            foreach (var hand in hands)
            {
                if (hand == null)
                {
                    continue;
                }

                if (hand.IsTracked)
                {
                    var skeleton = hand.GetComponent<OVRSkeleton>();
                    if (TryGetIndexTipFromSkeleton(skeleton, out Vector3 tipPosition))
                    {
                        ConsiderInteractionPoint(tipPosition, ref position, ref bestDistance, ref found);
                        continue;
                    }
                }

                if (hand.transform.position.sqrMagnitude > 0.0001f)
                {
                    ConsiderInteractionPoint(hand.transform.position, ref position, ref bestDistance, ref found);
                }
            }

            var rig = FindObjectOfType<OVRCameraRig>();
            if (rig != null)
            {
                if (rig.leftHandAnchor != null)
                {
                    ConsiderInteractionPoint(rig.leftHandAnchor.position, ref position, ref bestDistance, ref found);
                }

                if (rig.rightHandAnchor != null)
                {
                    ConsiderInteractionPoint(rig.rightHandAnchor.position, ref position, ref bestDistance, ref found);
                }
            }

            return found;
        }

        private bool TryGetIndexTipFromSkeleton(OVRSkeleton skeleton, out Vector3 position)
        {
            position = Vector3.zero;
            if (skeleton == null || skeleton.Bones == null)
            {
                return false;
            }

            Transform indexTip = null;
            foreach (var bone in skeleton.Bones)
            {
                string boneName = bone.Id.ToString();
                if (boneName.IndexOf("IndexTip", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    indexTip = bone.Transform;
                    break;
                }

                if (indexTip == null &&
                    boneName.IndexOf("Index3", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    indexTip = bone.Transform;
                }
            }

            if (indexTip == null || indexTip.position.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            position = indexTip.position;
            return true;
        }

        private void ConsiderInteractionPoint(
            Vector3 candidate,
            ref Vector3 bestPoint,
            ref float bestDistance,
            ref bool found
        )
        {
            if (candidate.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Vector3 reference = scenarioRoot != null ? scenarioRoot.transform.position : candidate;
            float distance = Vector3.Distance(candidate, reference);
            if (!found || distance < bestDistance)
            {
                bestPoint = candidate;
                bestDistance = distance;
                found = true;
            }
        }

        private GameObject GetCurrentTarget(Vector3 interactionPoint)
        {
            switch (currentState)
            {
                case AEDState.Off:
                    if (IsPointInsideTarget(interactionPoint, powerMarker) ||
                        IsPointNearAedBody(interactionPoint))
                    {
                        return powerMarker;
                    }

                    return null;

                case AEDState.On:
                    if (!electrodePackageOpened && IsPointNearElectrodePackage(interactionPoint))
                    {
                        return GetElectrodePackageTarget();
                    }

                    if (!electrodePackageOpened)
                    {
                        return null;
                    }

                    return null;

                case AEDState.ShockAdvised:
                    if (IsPointInsideTarget(interactionPoint, shockMarker) ||
                        IsPointNearAedBody(interactionPoint))
                    {
                        return shockMarker;
                    }

                    return null;

                default:
                    return null;
            }
        }

        private bool IsPointInsideTarget(Vector3 interactionPoint, GameObject target)
        {
            return IsPointInsideTarget(interactionPoint, target, InteractionRadius);
        }

        private bool IsPointInsideTarget(Vector3 interactionPoint, GameObject target, float radius)
        {
            return target != null &&
                   target.activeInHierarchy &&
                   Vector3.Distance(interactionPoint, target.transform.position) <= radius;
        }

        private bool IsPointNearAedBody(Vector3 interactionPoint)
        {
            if (scenarioRoot == null || !scenarioRoot.activeInHierarchy)
            {
                return false;
            }

            return Vector3.Distance(interactionPoint, scenarioRoot.transform.position) <= AedBodyInteractionRadius;
        }

        private bool IsPointNearElectrodePackage(Vector3 interactionPoint)
        {
            GameObject packageTarget = GetElectrodePackageTarget();
            if (packageTarget == null || !packageTarget.activeInHierarchy)
            {
                return false;
            }

            if (Vector3.Distance(interactionPoint, packageTarget.transform.position) <= ElectrodePackageInteractionRadius)
            {
                return true;
            }

            return modelHost != null &&
                   modelHost.activeInHierarchy &&
                   Vector3.Distance(interactionPoint, modelHost.transform.position) <= AedBodyInteractionRadius;
        }

        private GameObject GetElectrodePackageTarget()
        {
            return electrodePackageTarget != null ? electrodePackageTarget : modelHost;
        }

        private void TriggerTarget(GameObject target)
        {
            if (target == powerMarker)
            {
                TurnOnAed();
                return;
            }

            if (target == GetElectrodePackageTarget())
            {
                OpenElectrodePackage();
                return;
            }

            if (target == shockMarker)
                DeliverShock();
        }

        private void TurnOnAed()
        {
            SetState(
                AEDState.On,
                LanguageManager.Get(
                    "\u0041\u0045\u0044 \u5df2\u5f00\u673a\uff0c\u8bf7\u89e6\u78b0\u7535\u6781\u7247\u5305\u88c5\u4ee5\u53d6\u51fa\u4e24\u7247\u8d34\u7247\u3002",
                    "\u0041\u0045\u0044 \u306e\u96fb\u6e90\u304c\u5165\u308a\u307e\u3057\u305f\u3002\u96fb\u6975\u30d1\u30c3\u30c9\u306e\u888b\u306b\u89e6\u308c\u3066\u30012\u679a\u306e\u30d1\u30c3\u30c9\u3092\u53d6\u308a\u51fa\u3057\u3066\u304f\u3060\u3055\u3044\u3002",
                    "AED powered on. Touch the electrode package to take out both pads."
                )
            );

            if (powerMarker != null)
                powerMarker.SetActive(false);

            electrodePackageOpened = false;

            if (leftPadTarget != null)
                leftPadTarget.SetActive(false);

            if (rightPadTarget != null)
                rightPadTarget.SetActive(false);

            ResetPhysicalPads();
            SetPhysicalPadsActive(false);

            UpdateControlButtons();
        }

        private void OpenElectrodePackage()
        {
            if (currentState != AEDState.On || electrodePackageOpened)
            {
                return;
            }

            electrodePackageOpened = true;
            ResetPhysicalPads();

            if (leftPadTarget != null)
                leftPadTarget.SetActive(true);

            if (rightPadTarget != null)
                rightPadTarget.SetActive(true);

            UpdateInstructionOverlay(LanguageManager.Get(
                "\u5df2\u53d6\u51fa\u4e24\u7247\u7535\u6781\u8d34\u7247\u3002\u8bf7\u7528\u624b\u62ff\u8d77\u8d34\u7247\uff0c\u5206\u522b\u62d6\u5230\u60a3\u8005\u80f8\u53e3\u7684\u4e24\u4e2a AED \u6807\u8bb0\u4e0a\u3002",
                "2\u679a\u306e\u96fb\u6975\u30d1\u30c3\u30c9\u3092\u53d6\u308a\u51fa\u3057\u307e\u3057\u305f\u3002\u624b\u3067\u30d1\u30c3\u30c9\u3092\u6301\u3061\u3001\u60a3\u8005\u306e\u80f8\u306b\u3042\u308b2\u3064\u306e AED \u30de\u30fc\u30ab\u30fc\u3078\u904b\u3093\u3067\u304f\u3060\u3055\u3044\u3002",
                "Both electrode pads are out. Pick them up by hand and drag them to the two AED markers on the patient's chest."
            ));

            UpdateControlButtons();
        }

        private void PlacePhysicalPad(AedPadGrabbable pad, GameObject target)
        {
            if (pad == null || target == null)
            {
                return;
            }

            PlacePadByKind(pad.Kind, pad.gameObject, target);
        }

        private void PlacePadByKind(PadKind padKind, GameObject padObject, GameObject target)
        {
            if (currentState != AEDState.On || target == null || IsPadKindPlaced(padKind))
            {
                return;
            }

            if (!electrodePackageOpened)
            {
                electrodePackageOpened = true;
                if (leftPadTarget != null)
                    leftPadTarget.SetActive(true);
                if (rightPadTarget != null)
                    rightPadTarget.SetActive(true);
                SetPhysicalPadsActive(true);
                UpdateControlButtons();
            }

            if (padKind == PadKind.Left)
            {
                leftPadPlaced = true;
                UpdatePadVisual(leftPadTarget, true);
                UpdateInstructionOverlay(LanguageManager.Get(
                    "\u4e0a\u65b9\u7535\u6781\u7247\u5df2\u5438\u9644\u5230\u80f8\u53e3\uff0c\u8bf7\u7ee7\u7eed\u653e\u7f6e\u53e6\u4e00\u7247\u3002",
                    "\u4e0a\u5074\u306e\u30d1\u30c3\u30c9\u304c\u80f8\u306b\u5438\u7740\u3057\u307e\u3057\u305f\u3002\u7d9a\u3051\u3066\u3082\u3046\u4e00\u679a\u3092\u8cbc\u3063\u3066\u304f\u3060\u3055\u3044\u3002",
                    "Upper pad snapped to the chest. Place the second pad."
                ));
            }
            else
            {
                rightPadPlaced = true;
                UpdatePadVisual(rightPadTarget, true);
                UpdateInstructionOverlay(LanguageManager.Get(
                    "\u4e0b\u65b9\u7535\u6781\u7247\u5df2\u5438\u9644\u5230\u80f8\u53e3\uff0c\u6b63\u5728\u7b49\u5f85\u5fc3\u5f8b\u5206\u6790\u3002",
                    "\u4e0b\u5074\u306e\u30d1\u30c3\u30c9\u304c\u80f8\u306b\u5438\u7740\u3057\u307e\u3057\u305f\u3002\u5fc3\u96fb\u56f3\u89e3\u6790\u3092\u958b\u59cb\u3057\u307e\u3059\u3002",
                    "Lower pad snapped to the chest. Waiting for rhythm analysis..."
                ));
            }

            if (padObject != null)
            {
                SnapPadObjectToTarget(padObject, target);
            }

            if (carriedPad != null && carriedPad.gameObject == padObject)
            {
                carriedPad = null;
            }

            StartAnalysisIfReady();
        }

        private void SnapPadObjectToTarget(GameObject padObject, GameObject target)
        {
            var pad = padObject.GetComponent<AedPadGrabbable>();
            if (pad != null)
            {
                pad.IsPlaced = true;
            }

            var grabInteractable = padObject.GetComponent<XRGrabInteractable>();
            if (grabInteractable != null)
            {
                grabInteractable.enabled = false;
            }

            SetMetaGrabInteractionsEnabled(padObject, false);

            var rigidbody = padObject.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                rigidbody.velocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
                rigidbody.isKinematic = true;
            }

            padObject.transform.SetParent(target.transform, true);
            padObject.transform.position = target.transform.position + Vector3.up * 0.008f;
            padObject.transform.rotation = target.transform.rotation;
        }

        private void TrySnapPhysicalPad(AedPadGrabbable pad)
        {
            if (pad == null || pad.IsPlaced || IsPadKindPlaced(pad.Kind))
            {
                return;
            }

            GameObject target = GetTargetForPad(pad.Kind);
            if (target == null || !target.activeInHierarchy)
            {
                return;
            }

            if (Vector3.Distance(pad.transform.position, target.transform.position) > PhysicalPadSnapRadius)
            {
                return;
            }

            if (currentState == AEDState.Off)
            {
                TurnOnAed();
            }

            PlacePhysicalPad(pad, target);
        }

        private GameObject GetTargetForPad(PadKind padKind)
        {
            return padKind == PadKind.Left ? leftPadTarget : rightPadTarget;
        }

        private bool IsPadKindPlaced(PadKind padKind)
        {
            return padKind == PadKind.Left ? leftPadPlaced : rightPadPlaced;
        }

        private void StartAnalysisIfReady()
        {
            if (!leftPadPlaced || !rightPadPlaced || analysisRoutine != null)
            {
                return;
            }

            ResultPadsPlaced = true;
            analysisRoutine = StartCoroutine(AnalyzeRhythm());
            UpdateControlButtons();
        }

        private IEnumerator AnalyzeRhythm()
        {
            SetState(
                AEDState.Analyzing,
                LanguageManager.Get(
                    "\u6b63\u5728\u5206\u6790\u5fc3\u5f8b\uff0c\u8bf7\u4e0d\u8981\u63a5\u89e6\u60a3\u8005\u3002",
                    "\u5fc3\u96fb\u56f3\u3092\u89e3\u6790\u4e2d\u3067\u3059\u3002\u60a3\u8005\u306b\u89e6\u308c\u306a\u3044\u3067\u304f\u3060\u3055\u3044\u3002",
                    "Analyzing heart rhythm. Keep clear of the patient."
                )
            );

            yield return new WaitForSeconds(analysisDuration);
            analysisRoutine = null;

            currentRhythm = PickSimulatedRhythm();
            bool shockable = IsShockableRhythm(currentRhythm);
            ResultRhythm = currentRhythm;
            ResultShockAdvised = shockable;
            Debug.Log($"[AEDTrainingRuntime] Simulated rhythm: {currentRhythm} (shockable={shockable}).");

            if (shockable)
            {
                SetState(
                    AEDState.ShockAdvised,
                    LanguageManager.Get(
                        "\u5efa\u8bae\u7535\u51fb\u3002\u786e\u8ba4\u65e0\u4eba\u63a5\u89e6\u60a3\u8005\u540e\uff0c\u70b9\u51fb\u7ea2\u8272\u6807\u8bb0\u6267\u884c\u7535\u51fb\u3002",
                        "\u30b7\u30e7\u30c3\u30af\u304c\u63a8\u5968\u3055\u308c\u307e\u3059\u3002\u8ab0\u3082\u60a3\u8005\u306b\u89e6\u308c\u3066\u3044\u306a\u3044\u3053\u3068\u3092\u78ba\u8a8d\u3057\u3066\u3001\u8d64\u3044\u30de\u30fc\u30ab\u30fc\u306b\u89e6\u308c\u3066\u304f\u3060\u3055\u3044\u3002",
                        "Shock advised. Make sure nobody touches the patient, then touch the red marker."
                    )
                );

                if (shockMarker != null)
                    shockMarker.SetActive(true);

                UpdateControlButtons();
            }
            else
            {
                SetState(
                    AEDState.NoShockAdvised,
                    LanguageManager.Get(
                        "\u4e0d\u5efa\u8bae\u7535\u51fb\uff0c\u8bf7\u7acb\u5373\u8fdb\u884c\u5fc3\u80ba\u590d\u82cf\u3002",
                        "\u30b7\u30e7\u30c3\u30af\u4e0d\u8981\u3067\u3059\u3002\u3059\u3050\u306b\u5fc3\u80ba\u8607\u751f\u3092\u958b\u59cb\u3057\u3066\u304f\u3060\u3055\u3044\u3002",
                        "No shock advised. Start CPR immediately."
                    )
                );

                FinalizeAedResult(shockDelivered: false);
                StartCoroutine(ResumeCprAfterDelay(1.2f));
                UpdateControlButtons();
            }
        }

        private void ResetAedResult()
        {
            HasResult = false;
            ResultPadsPlaced = false;
            ResultShockAdvised = false;
            ResultShockDelivered = false;
            ResultDurationSeconds = 0f;
        }

        private void FinalizeAedResult(bool shockDelivered)
        {
            ResultShockDelivered = shockDelivered;
            ResultDurationSeconds = Mathf.Max(0f, Time.time - scenarioStartTime);
            HasResult = true;
        }

        /// <summary>
        /// 供 CPRTrainingController 结算界面调用，按当前语言拼接 AED 环节评价文本。
        /// </summary>
        public static string BuildAedResultText()
        {
            if (!HasResult)
            {
                return string.Empty;
            }

            string header = LanguageManager.Get("—— AED 评价 ——", "—— AED 評価 ——", "—— AED Review ——");

            string rhythmName = GetRhythmDisplayName(ResultRhythm);
            string rhythmLabel = LanguageManager.Get("心律判定", "心電図判定", "Rhythm");
            string rhythmLine = $"{rhythmLabel}: {rhythmName}";

            string padLabel = LanguageManager.Get("电极片", "電極パッド", "Pads");
            string padValue = ResultPadsPlaced
                ? LanguageManager.Get("已正确贴放", "正しく装着", "Placed correctly")
                : LanguageManager.Get("未完成", "未完了", "Not placed");
            string padLine = $"{padLabel}: {padValue}";

            string actionLabel = LanguageManager.Get("处置", "処置", "Action");
            string actionValue = ResultShockDelivered
                ? LanguageManager.Get("已执行电击", "ショック実施", "Shock delivered")
                : LanguageManager.Get("未电击，继续 CPR", "ショックなし、CPR継続", "No shock, continued CPR");
            string actionLine = $"{actionLabel}: {actionValue}";

            string timeLabel = LanguageManager.Get("AED 用时", "AED 所要時間", "AED Time");
            string timeUnit = LanguageManager.Get("秒", "秒", "sec");
            string timeLine = $"{timeLabel}: {ResultDurationSeconds:F1} {timeUnit}";

            string gradeLabel = LanguageManager.Get("评级", "評価", "Grade");
            string gradeLine = $"{gradeLabel}: {GetAedGrade(ResultDurationSeconds)}";

            return $"{header}\n{rhythmLine}\n{padLine}\n{actionLine}\n{timeLine}\n{gradeLine}";
        }

        private static string GetRhythmDisplayName(SimulatedRhythm rhythm)
        {
            switch (rhythm)
            {
                case SimulatedRhythm.VentricularFibrillation:
                    return LanguageManager.Get("可电击 · 室颤(VF)", "ショック適応 · 心室細動(VF)", "Shockable · VF");
                case SimulatedRhythm.VentricularTachycardia:
                    return LanguageManager.Get("可电击 · 无脉室速(VT)", "ショック適応 · 無脈性VT", "Shockable · Pulseless VT");
                case SimulatedRhythm.Asystole:
                    return LanguageManager.Get("不可电击 · 心搏停止", "ショック非適応 · 心静止", "Non-shockable · Asystole");
                case SimulatedRhythm.PulselessElectricalActivity:
                    return LanguageManager.Get("不可电击 · 无脉电活动(PEA)", "ショック非適応 · PEA", "Non-shockable · PEA");
                default:
                    return string.Empty;
            }
        }

        private static string GetAedGrade(float durationSeconds)
        {
            if (durationSeconds <= 30f)
            {
                return LanguageManager.Get("优秀", "優秀", "Excellent");
            }

            if (durationSeconds <= 60f)
            {
                return LanguageManager.Get("良好", "良好", "Good");
            }

            return LanguageManager.Get("继续加油", "要改善", "Keep practicing");
        }

        private SimulatedRhythm PickSimulatedRhythm()
        {
            bool shockable = alwaysAdviseShock || UnityEngine.Random.value < shockableRhythmProbability;

            if (shockable)
            {
                return UnityEngine.Random.value < 0.5f
                    ? SimulatedRhythm.VentricularFibrillation
                    : SimulatedRhythm.VentricularTachycardia;
            }

            return UnityEngine.Random.value < 0.5f
                ? SimulatedRhythm.Asystole
                : SimulatedRhythm.PulselessElectricalActivity;
        }

        private static bool IsShockableRhythm(SimulatedRhythm rhythm)
        {
            return rhythm == SimulatedRhythm.VentricularFibrillation ||
                   rhythm == SimulatedRhythm.VentricularTachycardia;
        }

        private void DeliverShock()
        {
            if (currentState != AEDState.ShockAdvised)
                return;

            if (shockMarker != null)
                shockMarker.SetActive(false);

            SetState(
                AEDState.ShockDelivered,
                LanguageManager.Get(
                    "\u7535\u51fb\u5b8c\u6210\uff0c\u8bf7\u7acb\u5373\u8fdb\u884c\u5fc3\u80ba\u590d\u82cf\u3002",
                    "\u30b7\u30e7\u30c3\u30af\u3092\u5b9f\u884c\u3057\u307e\u3057\u305f\u3002\u3059\u3050\u306b\u5fc3\u80ba\u8607\u751f\u3092\u958b\u59cb\u3057\u3066\u304f\u3060\u3055\u3044\u3002",
                    "Shock delivered. Start CPR immediately."
                )
            );

            if (shockFlashRoutine != null)
                StopCoroutine(shockFlashRoutine);

            shockFlashRoutine = StartCoroutine(ShockFlashRoutine());
            TriggerShockHaptic();
            FinalizeAedResult(shockDelivered: true);
            StartCoroutine(ResumeCprAfterDelay(1.2f));
            UpdateControlButtons();
        }

        private void TriggerShockHaptic()
        {
            var haptic = FindObjectOfType<GloveHapticController>();
            if (haptic != null)
            {
                haptic.TriggerShockFeedback();
            }
        }

        private IEnumerator ShockFlashRoutine()
        {
            float elapsed = 0f;
            while (elapsed < ShockFlashSeconds)
            {
                elapsed += Time.deltaTime;
                float pulse = Mathf.PingPong(elapsed * 8f, 1f);
                SetModelEmission(Color.Lerp(Color.white, shockMarkerColor, pulse));
                yield return null;
            }

            SetModelEmission(Color.black);
            shockFlashRoutine = null;
        }

        private void SetModelEmission(Color color)
        {
            var renderers = modelHost.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                var material = renderer.material;
                if (material == null)
                {
                    continue;
                }

                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color);
            }
        }

        private IEnumerator FadeInstructionAfterDelay(float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);

            if (currentState == AEDState.ShockDelivered || currentState == AEDState.NoShockAdvised)
            {
                HideInstructionOverlay();
            }
        }

        private IEnumerator ResumeCprAfterDelay(float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);

            ResolveSceneReferences();
            if (trainingController == null || !scenarioVisible)
            {
                yield break;
            }

            trainingController.StartCPRAfterAED();
        }

        private void SetState(AEDState newState, string message)
        {
            currentState = newState;
            UpdateInstructionOverlay(message);
            UpdateControlButtons();
            PlayVoiceForState(newState);
            Debug.Log($"[AEDTrainingRuntime] State -> {newState}: {message}");
        }

        private void PlayVoiceForState(AEDState state)
        {
            if (!enableVoicePrompts)
            {
                return;
            }

            string key = GetVoiceKeyForState(state);
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            PlayVoice(key, urgent: state == AEDState.ShockAdvised);
        }

        private static string GetVoiceKeyForState(AEDState state)
        {
            switch (state)
            {
                case AEDState.Off: return "ready";
                case AEDState.On: return "apply_pads";
                case AEDState.Analyzing: return "analyzing";
                case AEDState.ShockAdvised: return "shock_advised";
                case AEDState.NoShockAdvised: return "no_shock";
                case AEDState.ShockDelivered: return "shock_delivered";
                default: return null;
            }
        }

        private void PlayVoice(string key, bool urgent)
        {
            EnsureVoiceAudio();
            PulseSubtitle();

            string lang = GetVoiceLanguageFolder();
            string cacheKey = lang + "/" + key;

            if (voiceClipCache.TryGetValue(cacheKey, out AudioClip clip) && clip != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.PlayOneShot(clip, voiceVolume);
                return;
            }

            // 没有真人录音时，先用程序化提示音兜底，保证有声反馈。
            PlayBeep(urgent);

            if (!voiceClipMissing.Contains(cacheKey))
            {
                StartCoroutine(LoadVoiceClip(lang, key, cacheKey));
            }
        }

        private void EnsureVoiceAudio()
        {
            if (voiceAudioSource != null)
            {
                return;
            }

            voiceAudioSource = gameObject.AddComponent<AudioSource>();
            voiceAudioSource.playOnAwake = false;
            voiceAudioSource.spatialBlend = 0f; // 2D，始终清晰可闻
            voiceAudioSource.volume = voiceVolume;
        }

        private string GetVoiceLanguageFolder()
        {
            var mgr = LanguageManager.Instance != null
                ? LanguageManager.Instance
                : FindObjectOfType<LanguageManager>();

            if (mgr != null)
            {
                switch (mgr.CurrentLanguage)
                {
                    case Language.Chinese: return "zh";
                    case Language.Japanese: return "ja";
                    case Language.English: return "en";
                }
            }

            return "ja"; // 与 LanguageManager 默认语言保持一致
        }

        private IEnumerator LoadVoiceClip(string lang, string key, string cacheKey)
        {
            string url = BuildVoiceClipUrl(lang, key);

            using (var request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV))
            {
                yield return request.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
                bool failed = request.result != UnityWebRequest.Result.Success;
#else
                bool failed = request.isNetworkError || request.isHttpError;
#endif
                if (failed)
                {
                    voiceClipMissing.Add(cacheKey);
                    yield break;
                }

                var clip = DownloadHandlerAudioClip.GetContent(request);
                if (clip == null)
                {
                    voiceClipMissing.Add(cacheKey);
                    yield break;
                }

                clip.name = "AedVoice_" + cacheKey;
                voiceClipCache[cacheKey] = clip;
            }
        }

        private string BuildVoiceClipUrl(string lang, string key)
        {
            string relative = $"aed-voice/{lang}/{key}.wav";
            string full = CombineRuntimePath(Application.streamingAssetsPath, relative);
            return IsUrlLike(full) ? full : new Uri(full).AbsoluteUri;
        }

        private void PlayBeep(bool urgent)
        {
            if (voiceAudioSource == null)
            {
                return;
            }

            AudioClip clip = urgent ? GetUrgentBeepClip() : GetCueBeepClip();
            if (clip != null)
            {
                voiceAudioSource.PlayOneShot(clip, voiceVolume);
            }
        }

        private AudioClip GetCueBeepClip()
        {
            if (cueBeepClip == null)
            {
                cueBeepClip = GenerateBeepClip("AedCueBeep", 880f, 0.16f, 1);
            }

            return cueBeepClip;
        }

        private AudioClip GetUrgentBeepClip()
        {
            if (urgentBeepClip == null)
            {
                urgentBeepClip = GenerateBeepClip("AedUrgentBeep", 1320f, 0.6f, 3);
            }

            return urgentBeepClip;
        }

        private static AudioClip GenerateBeepClip(string clipName, float frequency, float duration, int pulses)
        {
            const int sampleRate = 44100;
            int totalSamples = Mathf.Max(1, Mathf.CeilToInt(sampleRate * duration));
            int pulseSamples = Mathf.Max(1, totalSamples / Mathf.Max(1, pulses));
            var samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                int posInPulse = i % pulseSamples;
                float pulseT = (float)posInPulse / pulseSamples;
                // 每个脉冲只在前 60% 发声，形成"嘀—嘀—嘀"的间隔感
                float env = pulseT < 0.6f ? Mathf.Sin(Mathf.Clamp01(pulseT / 0.6f) * Mathf.PI) : 0f;
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * env * 0.5f;
            }

            var clip = AudioClip.Create(clipName, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void PulseSubtitle()
        {
            if (instructionText == null)
            {
                return;
            }

            if (subtitlePulseRoutine != null)
            {
                StopCoroutine(subtitlePulseRoutine);
            }

            subtitlePulseRoutine = StartCoroutine(SubtitlePulseRoutine());
        }

        private IEnumerator SubtitlePulseRoutine()
        {
            if (instructionText == null)
            {
                yield break;
            }

            instructionText.transform.parent.gameObject.SetActive(true);

            Transform target = instructionText.transform;
            const float duration = 0.6f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float p = Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI);
                target.localScale = Vector3.one * (1f + p * 0.08f);
                yield return null;
            }

            target.localScale = Vector3.one;
            subtitlePulseRoutine = null;
        }

        private void EnsureInstructionOverlay()
        {
            if (instructionText != null)
            {
                return;
            }

            GameObject canvasObject = new GameObject(
                "AEDWorldUICanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );
            aedWorldCanvas = canvasObject.GetComponent<Canvas>();
            aedWorldCanvas.renderMode = RenderMode.WorldSpace;
            aedWorldCanvas.sortingOrder = 50;

            aedWorldCanvasRect = canvasObject.GetComponent<RectTransform>();
            aedWorldCanvasRect.sizeDelta = new Vector2(600f, 240f);
            UpdateWorldUiPose();

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 12f;

            instructionPanel = new GameObject("AEDInstructionPanel", typeof(RectTransform), typeof(Image));
            instructionPanel.transform.SetParent(canvasObject.transform, false);

            var panelTransform = instructionPanel.GetComponent<RectTransform>();
            panelTransform.anchorMin = new Vector2(0.5f, 1f);
            panelTransform.anchorMax = new Vector2(0.5f, 1f);
            panelTransform.pivot = new Vector2(0.5f, 1f);
            panelTransform.anchoredPosition = new Vector2(0f, -48f);
            panelTransform.sizeDelta = new Vector2(540f, 88f);

            instructionBackground = instructionPanel.GetComponent<Image>();
            instructionBackground.color = new Color(0f, 0f, 0f, 0f);

            var textObject = new GameObject("AEDInstructionText", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(instructionPanel.transform, false);

            var textTransform = textObject.GetComponent<RectTransform>();
            textTransform.anchorMin = Vector2.zero;
            textTransform.anchorMax = Vector2.one;
            textTransform.offsetMin = new Vector2(24f, 12f);
            textTransform.offsetMax = new Vector2(-24f, -12f);

            instructionText = textObject.GetComponent<Text>();
            instructionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            instructionText.fontSize = 22;
            instructionText.alignment = TextAnchor.MiddleCenter;
            instructionText.resizeTextForBestFit = true;
            instructionText.resizeTextMinSize = 14;
            instructionText.resizeTextMaxSize = 24;
            instructionText.color = Color.white;
        }

        private void EnsureControlPanel()
        {
            if (controlPanel != null)
            {
                return;
            }

            EnsureInstructionOverlay();
            if (aedWorldCanvas == null)
            {
                return;
            }

            controlPanel = new GameObject("AEDControlPanel", typeof(RectTransform), typeof(Image));
            controlPanel.transform.SetParent(aedWorldCanvas.transform, false);

            var panelTransform = controlPanel.GetComponent<RectTransform>();
            panelTransform.anchorMin = new Vector2(0.5f, 0.5f);
            panelTransform.anchorMax = new Vector2(0.5f, 0.5f);
            panelTransform.pivot = new Vector2(0.5f, 0.5f);
            panelTransform.anchoredPosition = Vector2.zero;
            panelTransform.sizeDelta = new Vector2(560f, 210f);

            var panelImage = controlPanel.GetComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.72f);

            var titleObject = new GameObject("AEDControlTitle", typeof(RectTransform), typeof(Text));
            titleObject.transform.SetParent(controlPanel.transform, false);
            var titleTransform = titleObject.GetComponent<RectTransform>();
            titleTransform.anchorMin = new Vector2(0f, 1f);
            titleTransform.anchorMax = new Vector2(1f, 1f);
            titleTransform.pivot = new Vector2(0.5f, 1f);
            titleTransform.anchoredPosition = new Vector2(0f, -10f);
            titleTransform.sizeDelta = new Vector2(-28f, 30f);

            controlTitleText = titleObject.GetComponent<Text>();
            controlTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            controlTitleText.fontSize = 20;
            controlTitleText.alignment = TextAnchor.MiddleCenter;
            controlTitleText.color = Color.white;

            MergeInstructionPanelIntoControlPanel();

            powerControlButton = CreateControlButton("AEDPowerControlButton", new Vector2(-172f, 32f), new Vector2(118f, 46f), TurnOnAed);
            packageControlButton = CreateControlButton("AEDElectrodePackageControlButton", new Vector2(0f, 32f), new Vector2(132f, 46f), OpenElectrodePackage);
            leftPadControlButton = CreateControlButton("AEDLeftPadControlButton", new Vector2(0f, 32f), new Vector2(84f, 46f), () => TriggerTarget(leftPadTarget));
            rightPadControlButton = CreateControlButton("AEDRightPadControlButton", new Vector2(0f, 32f), new Vector2(84f, 46f), () => TriggerTarget(rightPadTarget));
            shockControlButton = CreateControlButton("AEDShockControlButton", new Vector2(172f, 32f), new Vector2(118f, 46f), DeliverShock);

            // 顶部角落：返回初始界面 / 退出（手与手柄均可点）
            returnControlButton = CreateCornerControlButton("AEDReturnControlButton", true, new Vector2(96f, 34f), ReturnToInitialScreen);
            exitControlButton = CreateCornerControlButton("AEDExitControlButton", false, new Vector2(96f, 34f), ExitTraining);

            controlPanel.SetActive(false);

            RegisterControlPanelWithPointer();
        }

        /// <summary>
        /// 自包含的面板点击：手柄扳机或手部捏合时，从对应手/柄发射线命中面板按钮即触发。
        /// 不依赖外部 UI 射线插件，保证「返回/退出/電源/Pads/ショック」按钮手与手柄都能点。
        /// </summary>
        private void HandleControlPanelClick()
        {
            if (controlPanel == null || !controlPanel.activeInHierarchy)
            {
                return;
            }

            if (clickRig == null)
            {
                clickRig = FindObjectOfType<OVRCameraRig>();
            }

            Transform origin = null;

            // 1) 手柄扳机（右/左）
            if (clickRig != null)
            {
                if (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger) && clickRig.rightControllerAnchor != null)
                    origin = clickRig.rightControllerAnchor;
                else if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger) && clickRig.leftControllerAnchor != null)
                    origin = clickRig.leftControllerAnchor;
            }

            // 2) 手部捏合（右/左），取上升沿
            EnsureClickHands();
            bool rightPinch = IsPinching(clickRightHand);
            bool leftPinch = IsPinching(clickLeftHand);
            if (origin == null && rightPinch && !wasRightPinch && clickRig != null && clickRig.rightHandAnchor != null)
                origin = clickRig.rightHandAnchor;
            if (origin == null && leftPinch && !wasLeftPinch && clickRig != null && clickRig.leftHandAnchor != null)
                origin = clickRig.leftHandAnchor;
            wasRightPinch = rightPinch;
            wasLeftPinch = leftPinch;

#if UNITY_EDITOR
            // 编辑器：鼠标左键 / 回车，从主相机经鼠标位置发射线
            if (origin == null && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Return)))
            {
                var cam = Camera.main;
                if (cam != null)
                {
                    TryClickAedButton(cam.ScreenPointToRay(Input.mousePosition));
                }
                return;
            }
#endif

            if (origin != null)
            {
                TryClickAedButton(new Ray(origin.position, origin.forward));
            }
        }

        private void EnsureClickHands()
        {
            if (clickLeftHand != null && clickRightHand != null)
            {
                return;
            }

            var hands = FindObjectsOfType<OVRHand>(true);
            foreach (var hand in hands)
            {
                var skeleton = hand.GetComponent<OVRSkeleton>();
                if (skeleton == null)
                {
                    continue;
                }

                var type = skeleton.GetSkeletonType();
                if ((type == OVRSkeleton.SkeletonType.HandLeft || type == OVRSkeleton.SkeletonType.XRHandLeft) && clickLeftHand == null)
                    clickLeftHand = hand;
                else if ((type == OVRSkeleton.SkeletonType.HandRight || type == OVRSkeleton.SkeletonType.XRHandRight) && clickRightHand == null)
                    clickRightHand = hand;
            }
        }

        private static bool IsPinching(OVRHand hand)
        {
            return hand != null && hand.IsDataValid && hand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        }

        private void TryClickAedButton(Ray ray)
        {
            Button best = null;
            float bestDistance = float.MaxValue;

            foreach (var button in EnumerateControlButtons())
            {
                if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
                {
                    continue;
                }

                var rect = button.GetComponent<RectTransform>();
                if (rect == null)
                {
                    continue;
                }

                if (RayHitsRect(ray, rect, out float distance) && distance < bestDistance)
                {
                    bestDistance = distance;
                    best = button;
                }
            }

            if (best != null)
            {
                best.onClick.Invoke();
            }
        }

        private IEnumerable<Button> EnumerateControlButtons()
        {
            yield return powerControlButton;
            yield return packageControlButton;
            yield return leftPadControlButton;
            yield return rightPadControlButton;
            yield return shockControlButton;
            yield return returnControlButton;
            yield return exitControlButton;
        }

        private static bool RayHitsRect(Ray ray, RectTransform rect, out float distance)
        {
            distance = 0f;

            var corners = new Vector3[4]; // 0=BL 1=TL 2=TR 3=BR (world)
            rect.GetWorldCorners(corners);

            Vector3 right = corners[3] - corners[0];
            Vector3 up = corners[1] - corners[0];
            Vector3 normal = Vector3.Cross(right, up).normalized;
            if (normal.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            var plane = new Plane(normal, corners[0]);
            if (!plane.Raycast(ray, out distance))
            {
                // 面板背面也允许命中
                plane = new Plane(-normal, corners[0]);
                if (!plane.Raycast(ray, out distance))
                {
                    return false;
                }
            }

            if (distance <= 0f)
            {
                return false;
            }

            Vector3 hit = ray.GetPoint(distance);
            Vector3 rel = hit - corners[0];
            float u = Vector3.Dot(rel, right) / right.sqrMagnitude;
            float v = Vector3.Dot(rel, up) / up.sqrMagnitude;
            return u >= 0f && u <= 1f && v >= 0f && v <= 1f;
        }

        /// <summary> 让 AED 控制面板可被手部射线/手柄射线点击。 </summary>
        private void RegisterControlPanelWithPointer()
        {
            if (aedWorldCanvas == null)
            {
                return;
            }

            var pointer = FindObjectOfType<VRUIPointer>();
            if (pointer == null)
            {
                // 场景没有射线指针时，尝试在 OVRCameraRig 上补一个，保证手/柄都能点 UI
                var rig = FindObjectOfType<OVRCameraRig>();
                if (rig != null)
                {
                    pointer = rig.gameObject.AddComponent<VRUIPointer>();
                    Debug.Log("[AEDTrainingRuntime] 未找到 VRUIPointer，已在 OVRCameraRig 上自动添加。");
                }
            }

            if (pointer != null)
            {
                pointer.RegisterWorldSpaceCanvas(aedWorldCanvas);
            }
            // 即使此刻无指针，VRUIPointer 在其 Update 中会周期性扫描并注册本面板。
        }

        private void MergeInstructionPanelIntoControlPanel()
        {
            if (instructionPanel == null || controlPanel == null)
            {
                return;
            }

            instructionPanel.transform.SetParent(controlPanel.transform, false);
            var panelTransform = instructionPanel.GetComponent<RectTransform>();
            if (panelTransform != null)
            {
                panelTransform.anchorMin = new Vector2(0.5f, 1f);
                panelTransform.anchorMax = new Vector2(0.5f, 1f);
                panelTransform.pivot = new Vector2(0.5f, 1f);
                panelTransform.anchoredPosition = new Vector2(0f, -46f);
                panelTransform.sizeDelta = new Vector2(530f, 96f);
            }

            if (instructionBackground != null)
            {
                instructionBackground.color = new Color(0f, 0f, 0f, 0f);
            }
        }

        private Button CreateControlButton(string buttonName, Vector2 anchoredPosition, Vector2 size, UnityAction onClick)
        {
            var buttonObject = new GameObject(buttonName, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(controlPanel.transform, false);

            var rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0f);
            rectTransform.anchorMax = new Vector2(0.5f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.92f);

            var button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(onClick);
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0.92f);
            colors.highlightedColor = new Color(0.28f, 0.82f, 1f, 1f);
            colors.pressedColor = new Color(0.08f, 0.56f, 1f, 1f);
            colors.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.55f);
            colors.colorMultiplier = 1.2f;
            button.colors = colors;

            var labelObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(buttonObject.transform, false);
            var labelTransform = labelObject.GetComponent<RectTransform>();
            labelTransform.anchorMin = Vector2.zero;
            labelTransform.anchorMax = Vector2.one;
            labelTransform.offsetMin = new Vector2(8f, 4f);
            labelTransform.offsetMax = new Vector2(-8f, -4f);

            var label = labelObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 16;
            label.alignment = TextAnchor.MiddleCenter;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 10;
            label.resizeTextMaxSize = 18;
            label.color = Color.black;

            return button;
        }

        /// <summary> 在面板顶部左/右角创建小按钮（返回 / 退出）。 </summary>
        private Button CreateCornerControlButton(string buttonName, bool topLeft, Vector2 size, UnityAction onClick)
        {
            var buttonObject = new GameObject(buttonName, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(controlPanel.transform, false);

            var rectTransform = buttonObject.GetComponent<RectTransform>();
            Vector2 cornerAnchor = topLeft ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
            rectTransform.anchorMin = cornerAnchor;
            rectTransform.anchorMax = cornerAnchor;
            rectTransform.pivot = cornerAnchor;
            float xOffset = topLeft ? 10f : -10f;
            rectTransform.anchoredPosition = new Vector2(xOffset, -8f);
            rectTransform.sizeDelta = size;

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.92f, 0.92f, 0.95f, 0.85f);

            var button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(onClick);
            var colors = button.colors;
            colors.normalColor = new Color(0.92f, 0.92f, 0.95f, 0.85f);
            colors.highlightedColor = new Color(0.28f, 0.82f, 1f, 1f);
            colors.pressedColor = new Color(0.08f, 0.56f, 1f, 1f);
            colors.colorMultiplier = 1.2f;
            button.colors = colors;

            var labelObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(buttonObject.transform, false);
            var labelTransform = labelObject.GetComponent<RectTransform>();
            labelTransform.anchorMin = Vector2.zero;
            labelTransform.anchorMax = Vector2.one;
            labelTransform.offsetMin = new Vector2(4f, 2f);
            labelTransform.offsetMax = new Vector2(-4f, -2f);

            var label = labelObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 14;
            label.alignment = TextAnchor.MiddleCenter;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 9;
            label.resizeTextMaxSize = 16;
            label.color = Color.black;

            return button;
        }

        /// <summary> 返回初始（开始训练）界面：收起 AED 场景并回到主菜单。 </summary>
        private void ReturnToInitialScreen()
        {
            ResetScenario();

            if (controlPanel != null)
                controlPanel.SetActive(false);

            ResolveSceneReferences();
            if (trainingController != null)
            {
                trainingController.ReturnToStartMenu();
            }
            else
            {
                Debug.LogWarning("[AEDTrainingRuntime] Return requested but training controller is missing.");
            }
        }

        /// <summary> 退出训练（编辑器停止播放 / 打包后退出应用）。 </summary>
        private void ExitTraining()
        {
            ResolveSceneReferences();
            if (trainingController != null)
            {
                trainingController.ExitToMainMenu();
                return;
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void UpdateControlButtons()
        {
            if (controlPanel == null)
            {
                return;
            }

            controlPanel.SetActive(scenarioVisible);

            if (!scenarioVisible)
            {
                return;
            }

            SetButtonText(powerControlButton, LanguageManager.Get("\u5f00\u673a", "\u96fb\u6e90", "Power"));
            SetButtonText(packageControlButton, LanguageManager.Get("\u53d6\u51fa\u8d34\u7247", "Pads", "Pads"));
            SetButtonText(shockControlButton, LanguageManager.Get("\u7535\u51fb", "\u30b7\u30e7\u30c3\u30af", "Shock"));
            SetButtonText(returnControlButton, LanguageManager.Get("\u8fd4\u56de", "\u623b\u308b", "Back"));
            SetButtonText(exitControlButton, LanguageManager.Get("\u9000\u51fa", "\u7d42\u4e86", "Exit"));

            if (leftPadControlButton != null)
                leftPadControlButton.gameObject.SetActive(false);

            if (rightPadControlButton != null)
                rightPadControlButton.gameObject.SetActive(false);

            if (controlTitleText != null)
            {
                controlTitleText.text = LanguageManager.Get("\u0041\u0045\u0044 \u6551\u63f4\u6d41\u7a0b", "\u0041\u0045\u0044 \u30ec\u30b9\u30ad\u30e5\u30fc", "AED Rescue Flow");
            }

            if (powerControlButton != null)
                powerControlButton.interactable = currentState == AEDState.Off;

            if (packageControlButton != null)
                packageControlButton.interactable = currentState == AEDState.On && !electrodePackageOpened;

            if (shockControlButton != null)
                shockControlButton.interactable = currentState == AEDState.ShockAdvised;
        }

        private void SetButtonText(Button button, string text)
        {
            if (button == null)
            {
                return;
            }

            var label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.text = text;
            }
        }

        private void UpdateInstructionOverlay(string message)
        {
            EnsureInstructionOverlay();

            if (instructionText != null)
            {
                instructionText.text = message;
                instructionText.transform.parent.gameObject.SetActive(true);
            }
        }

        private void HideInstructionOverlay()
        {
            if (instructionText != null)
            {
                instructionText.transform.parent.gameObject.SetActive(false);
            }
        }

        private void AnimateMarkers()
        {
            AnimateMarker(powerMarker, MarkerScale);
            AnimateMarker(shockMarker, MarkerScale);
            AnimateMarker(leftPadTarget, MarkerScale * 0.85f);
            AnimateMarker(rightPadTarget, MarkerScale * 0.85f);
        }

        private void AnimateMarker(GameObject marker, float baseScale)
        {
            if (marker == null || !marker.activeSelf)
            {
                return;
            }

            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 4f) * 0.08f;
            marker.transform.localScale = Vector3.one * baseScale * pulse;
        }

        private void HandleDebugShortcuts()
        {
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.P) && currentState == AEDState.Off)
            {
                TurnOnAed();
            }

            if (Input.GetKeyDown(KeyCode.Alpha1) && currentState == AEDState.On && !leftPadPlaced)
            {
                TriggerTarget(leftPadTarget);
            }

            if (Input.GetKeyDown(KeyCode.Alpha2) && currentState == AEDState.On && !rightPadPlaced)
            {
                TriggerTarget(rightPadTarget);
            }

            if (Input.GetKeyDown(KeyCode.K) && currentState == AEDState.ShockAdvised)
            {
                DeliverShock();
            }
#endif
        }

        private void ResetScenario()
        {
            scenarioVisible = false;
            leftPadPlaced = false;
            rightPadPlaced = false;
            electrodePackageOpened = false;
            worldUiPoseFixed = false;
            hoveredTarget = null;
            targetHoverStartedAt = -1f;
            currentState = AEDState.Off;

            if (analysisRoutine != null)
            {
                StopCoroutine(analysisRoutine);
                analysisRoutine = null;
            }

            if (activationRoutine != null)
            {
                StopCoroutine(activationRoutine);
                activationRoutine = null;
            }

            if (shockFlashRoutine != null)
            {
                StopCoroutine(shockFlashRoutine);
                shockFlashRoutine = null;
            }

            if (subtitlePulseRoutine != null)
            {
                StopCoroutine(subtitlePulseRoutine);
                subtitlePulseRoutine = null;
            }

            if (instructionText != null)
            {
                instructionText.transform.localScale = Vector3.one;
            }

            if (voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
            }

            if (scenarioRoot != null)
            {
                scenarioRoot.SetActive(false);
            }

            if (leftPadTarget != null)
            {
                UpdatePadVisual(leftPadTarget, false);
                leftPadTarget.SetActive(false);
            }

            if (rightPadTarget != null)
            {
                UpdatePadVisual(rightPadTarget, false);
                rightPadTarget.SetActive(false);
            }

            if (powerMarker != null)
            {
                powerMarker.SetActive(false);
            }

            if (shockMarker != null)
            {
                shockMarker.SetActive(false);
            }

            carriedPad = null;
            ResetPhysicalPads();

            HideInstructionOverlay();
            UpdateControlButtons();

            // 复位后 AED 应该回到静置家具上，而不是整个消失 —— 它是房间的一部分，
            // 训练开始前和结束后都该看得见。此时模型已加载完，不需要再走协程。
            if (showAedFromStart && modelPrepared && scenarioRoot != null)
            {
                scenarioRoot.SetActive(true);
                PlaceScenarioOnRestingSpot();
            }
        }

        private Transform FindPatientRoot()
        {
            var animators = FindObjectsOfType<Animator>(true);
            foreach (var animator in animators)
            {
                string name = animator.gameObject.name;
                if (name.IndexOf("OVRCameraRig", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }

                if (name.IndexOf("Standing React Death Backward", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Patient", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Remy", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return animator.transform;
                }
            }

            return null;
        }

        /// <param name="fromDetector">
        /// true 表示拿到的是 CPRPressDetector.chestSurface —— 这是最准的来源，
        /// 调用方据此停止重算。其余结果都是降级的，需要继续重试。
        /// </param>
        private Transform FindChestReference(out bool fromDetector)
        {
            fromDetector = false;

            CPRPressDetector pressDetector = FindObjectOfType<CPRPressDetector>();
            if (pressDetector != null && pressDetector.chestSurface != null)
            {
                fromDetector = true;
                return pressDetector.chestSurface;
            }

            if (patientRoot == null)
            {
                return null;
            }

            string[] chestNames = { "Spine2", "UpperChest", "Chest", "Spine1", "Spine" };
            foreach (string chestName in chestNames)
            {
                Transform result = FindChildRecursive(patientRoot, chestName);
                if (result != null)
                {
                    return result;
                }
            }

            return patientRoot;
        }

        private Transform FindChildRecursive(Transform parent, string nameContains)
        {
            foreach (Transform child in parent)
            {
                if (child.name.IndexOf(nameContains, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return child;
                }

                Transform nested = FindChildRecursive(child, nameContains);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private enum PadKind
        {
            Left,
            Right
        }

        public enum SimulatedRhythm
        {
            VentricularFibrillation,     // 室颤 - 可电击
            VentricularTachycardia,      // 无脉性室速 - 可电击
            Asystole,                    // 心搏停止 - 不可电击
            PulselessElectricalActivity  // 无脉电活动(PEA) - 不可电击
        }

        private sealed class AedPadGrabbable : MonoBehaviour
        {
            private AEDTrainingRuntime runtime;

            public PadKind Kind { get; private set; }
            public bool IsPlaced { get; set; }

            public void Initialize(AEDTrainingRuntime owner, PadKind padKind)
            {
                runtime = owner;
                Kind = padKind;
            }

            private void OnTriggerEnter(Collider other)
            {
                if (runtime == null || IsPlaced)
                {
                    return;
                }

                var target = other.GetComponent<AedPadSnapTarget>();
                if (target != null && target.Kind == Kind)
                {
                    runtime.PlacePhysicalPad(this, target.gameObject);
                }
            }
        }

        private sealed class AedMetaGrabRelay : MonoBehaviour
        {
            private AEDTrainingRuntime runtime;
            private AedPadGrabbable pad;
            private Grabbable grabbable;

            public void Initialize(AEDTrainingRuntime owner, AedPadGrabbable padComponent, Grabbable metaGrabbable)
            {
                if (grabbable != null)
                {
                    grabbable.WhenPointerEventRaised -= HandlePointerEvent;
                }

                runtime = owner;
                pad = padComponent;
                grabbable = metaGrabbable;

                if (grabbable != null)
                {
                    grabbable.WhenPointerEventRaised += HandlePointerEvent;
                }
            }

            private void OnEnable()
            {
                if (grabbable != null)
                {
                    grabbable.WhenPointerEventRaised -= HandlePointerEvent;
                    grabbable.WhenPointerEventRaised += HandlePointerEvent;
                }
            }

            private void OnDisable()
            {
                if (grabbable != null)
                {
                    grabbable.WhenPointerEventRaised -= HandlePointerEvent;
                }
            }

            private void OnDestroy()
            {
                if (grabbable != null)
                {
                    grabbable.WhenPointerEventRaised -= HandlePointerEvent;
                }
            }

            private void HandlePointerEvent(PointerEvent pointerEvent)
            {
                if (runtime == null || pad == null)
                {
                    return;
                }

                if (pointerEvent.Type == PointerEventType.Unselect ||
                    pointerEvent.Type == PointerEventType.Move)
                {
                    runtime.TrySnapPhysicalPad(pad);
                }
            }
        }

        private sealed class AedPadSnapTarget : MonoBehaviour
        {
            private AEDTrainingRuntime runtime;

            public PadKind Kind { get; private set; }

            public void Initialize(AEDTrainingRuntime owner, PadKind padKind)
            {
                runtime = owner;
                Kind = padKind;
            }

            private void OnTriggerEnter(Collider other)
            {
                if (runtime == null)
                {
                    return;
                }

                var pad = other.GetComponent<AedPadGrabbable>();
                if (pad != null && pad.Kind == Kind && !pad.IsPlaced)
                {
                    runtime.PlacePhysicalPad(pad, gameObject);
                }
            }
        }
    }
}
