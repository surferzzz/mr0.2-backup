using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using CPRSimulation;
using CPRSimulation.Game;
using GLTFast;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
        private const float PadInteractionRadius = 0.32f;
        private const float InteractionHoldSeconds = 0.05f;
        private const float MarkerScale = 0.045f;
        private const float MarkerHeightOffset = 0.045f;
        private const float ShockFlashSeconds = 0.8f;
        private const float WorldUiScale = 0.00055f;

        [SerializeField] private bool alwaysAdviseShock = true;
        [SerializeField] private float analysisDuration = 3f;
        [SerializeField] private Vector3 patientSideOffset = new Vector3(0.55f, 0.02f, 0.22f);
        [SerializeField] private Color powerMarkerColor = new Color(0.1f, 0.8f, 1f, 0.95f);
        [SerializeField] private Color padMarkerColor = new Color(1f, 0.85f, 0.15f, 0.95f);
        [SerializeField] private Color completedPadColor = new Color(0.15f, 0.95f, 0.35f, 0.95f);
        [SerializeField] private Color shockMarkerColor = new Color(1f, 0.25f, 0.25f, 0.95f);

        private CPRTrainingController trainingController;
        private CPRTrainingController subscribedTrainingController;
        private HandTrackingAdapter handTrackingAdapter;
        private Transform patientRoot;
        private Transform chestReference;
        private GameObject scenarioRoot;
        private GameObject modelHost;
        private GltfAsset gltfAsset;
        private bool modelPrepared;
        private bool loadRequested;
        private bool leftPadPlaced;
        private bool rightPadPlaced;
        private bool scenarioVisible;
        private AEDState currentState = AEDState.Off;
        private GameObject powerMarker;
        private GameObject shockMarker;
        private GameObject leftPadTarget;
        private GameObject rightPadTarget;
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
        private Button leftPadControlButton;
        private Button rightPadControlButton;
        private Button shockControlButton;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindObjectOfType<AEDTrainingRuntime>() != null)
            {
                return;
            }

            if (FindObjectOfType<CPRTrainingController>() == null)
            {
                Debug.Log("[AEDTrainingRuntime] No CPRTrainingController detected in active scene - AED bootstrap skipped.");
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
            AnimateMarkers();
            UpdateWorldUiPose();
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

            chestReference = FindChestReference();

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

            Debug.Log("[AEDTrainingRuntime] Patient fell in AED mode - spawning AED scenario.");
            RequestScenarioActivation();
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
                HideInstructionOverlay();
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

            PositionScenarioNearPatient();
            CreateOrUpdatePadTargets();
            leftPadPlaced = false;
            rightPadPlaced = false;
            scenarioVisible = true;
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
            CreateOrUpdateAedMarkers();
            scenarioRoot.SetActive(false);
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
                topLocal + Vector3.right * horizontalOffset
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
            Vector3 patientForward = Vector3.Cross(Vector3.up, patientRight).normalized;

            if (patientForward.sqrMagnitude < 0.01f)
            {
                patientForward = patientRoot.forward.normalized;
            }

            leftPadTarget = CreateOrReusePatientMarker(
                leftPadTarget,
                "AEDLeftPadTarget",
                chestPosition + patientRight * 0.18f + patientForward * 0.08f + Vector3.up * 0.04f,
                padMarkerColor
            );

            rightPadTarget = CreateOrReusePatientMarker(
                rightPadTarget,
                "AEDRightPadTarget",
                chestPosition - patientRight * 0.14f - patientForward * 0.12f + Vector3.up * 0.03f,
                padMarkerColor
            );

            leftPadTarget.SetActive(false);
            rightPadTarget.SetActive(false);
            UpdatePadVisual(leftPadTarget, false);
            UpdatePadVisual(rightPadTarget, false);
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
            UpdateWorldUiPose();
        }

        private void UpdateWorldUiPose()
        {
            if (aedWorldCanvasRect == null || scenarioRoot == null)
            {
                return;
            }

            aedWorldCanvasRect.SetParent(null, true);

            Vector3 anchorPosition = scenarioRoot.transform.position + Vector3.up * 0.38f;
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                Vector3 toCamera = Vector3.ProjectOnPlane(mainCamera.transform.position - anchorPosition, Vector3.up);
                if (toCamera.sqrMagnitude < 0.001f)
                {
                    toCamera = -scenarioRoot.transform.forward;
                }

                aedWorldCanvasRect.position = anchorPosition + toCamera.normalized * 0.16f;
                aedWorldCanvasRect.rotation = Quaternion.LookRotation(aedWorldCanvasRect.position - mainCamera.transform.position, Vector3.up);
            }
            else
            {
                aedWorldCanvasRect.position = anchorPosition - scenarioRoot.transform.forward * 0.16f;
                aedWorldCanvasRect.rotation = Quaternion.LookRotation(-scenarioRoot.transform.forward, Vector3.up);
            }

            aedWorldCanvasRect.localScale = Vector3.one * WorldUiScale;
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

        private bool TryGetInteractionPoint(out Vector3 interactionPoint)
        {
            interactionPoint = Vector3.zero;

            if (TryGetBestHandInteractionPoint(out interactionPoint))
            {
                return true;
            }

            if (handTrackingAdapter != null)
            {
                Vector3 handPosition = handTrackingAdapter.ActiveHandPosition;
                if (handPosition.sqrMagnitude > 0.0001f)
                {
                    interactionPoint = handPosition;
                    return true;
                }
            }

            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                interactionPoint = mainCamera.transform.position + mainCamera.transform.forward * 0.75f;
                return true;
            }

            return false;
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
                    if (!leftPadPlaced && IsPointInsideTarget(interactionPoint, leftPadTarget, PadInteractionRadius))
                    {
                        return leftPadTarget;
                    }

                    if (!rightPadPlaced && IsPointInsideTarget(interactionPoint, rightPadTarget, PadInteractionRadius))
                    {
                        return rightPadTarget;
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

        private void TriggerTarget(GameObject target)
        {
            if (target == powerMarker)
            {
                TurnOnAed();
                return;
            }

            if (target == leftPadTarget)
            {
                leftPadPlaced = true;
                UpdatePadVisual(leftPadTarget, true);
                UpdateInstructionOverlay(LanguageManager.Get(
                    "\u4e0a\u65b9\u7535\u6781\u7247\u5df2\u8d34\u597d\uff0c\u8bf7\u7ee7\u7eed\u8d34\u653e\u53e6\u4e00\u7247\u3002",
                    "\u4e0a\u5074\u306e\u30d1\u30c3\u30c9\u3092\u88c5\u7740\u3057\u307e\u3057\u305f\u3002\u7d9a\u3051\u3066\u3082\u3046\u4e00\u679a\u306e\u30d1\u30c3\u30c9\u3092\u8cbc\u3063\u3066\u304f\u3060\u3055\u3044\u3002",
                    "First pad placed. Place the second pad on the remaining marker."
                ));
                StartAnalysisIfReady();
                return;
            }

            if (target == rightPadTarget)
            {
                rightPadPlaced = true;
                UpdatePadVisual(rightPadTarget, true);
                UpdateInstructionOverlay(LanguageManager.Get(
                    "\u7b2c\u4e8c\u7247\u7535\u6781\u7247\u5df2\u8d34\u597d\uff0c\u6b63\u5728\u7b49\u5f85\u5fc3\u5f8b\u5206\u6790\u3002",
                    "2\u679a\u76ee\u306e\u30d1\u30c3\u30c9\u3092\u88c5\u7740\u3057\u307e\u3057\u305f\u3002\u5fc3\u96fb\u56f3\u89e3\u6790\u3092\u958b\u59cb\u3057\u307e\u3059\u3002",
                    "Second pad placed. Waiting for rhythm analysis..."
                ));
                StartAnalysisIfReady();
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
                    "\u0041\u0045\u0044 \u5df2\u5f00\u673a\uff0c\u8bf7\u5c06\u4e24\u7247\u7535\u6781\u7247\u8d34\u5230\u60a3\u8005\u6807\u8bb0\u4f4d\u7f6e\u3002",
                    "AED \u306e\u96fb\u6e90\u304c\u5165\u308a\u307e\u3057\u305f\u30022\u679a\u306e\u30d1\u30c3\u30c9\u3092\u60a3\u8005\u306e\u30de\u30fc\u30ab\u30fc\u4f4d\u7f6e\u306b\u8cbc\u3063\u3066\u304f\u3060\u3055\u3044\u3002",
                    "AED powered on. Place both pads on the patient markers."
                )
            );

            if (powerMarker != null)
                powerMarker.SetActive(false);

            if (leftPadTarget != null)
                leftPadTarget.SetActive(true);

            if (rightPadTarget != null)
                rightPadTarget.SetActive(true);

            UpdateControlButtons();
        }

        private void StartAnalysisIfReady()
        {
            if (!leftPadPlaced || !rightPadPlaced || analysisRoutine != null)
            {
                return;
            }

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

            if (alwaysAdviseShock)
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
                        "\u4e0d\u5efa\u8bae\u7535\u51fb\uff0c\u8bf7\u7acb\u5373\u7ee7\u7eed\u8fdb\u884c CPR \u6309\u538b\u3002",
                        "\u30b7\u30e7\u30c3\u30af\u4e0d\u8981\u3067\u3059\u3002\u3059\u3050\u306b CPR \u3092\u518d\u958b\u3057\u3066\u304f\u3060\u3055\u3044\u3002",
                        "No shock advised. Continue CPR immediately."
                    )
                );

                StartCoroutine(ResumeCprAfterDelay(1.2f));
                StartCoroutine(FadeInstructionAfterDelay(4f));
                UpdateControlButtons();
            }
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
                    "\u7535\u51fb\u5b8c\u6210\uff0c\u8bf7\u7acb\u5373\u6062\u590d\u80f8\u5916\u6309\u538b\u3002",
                    "\u30b7\u30e7\u30c3\u30af\u3092\u5b9f\u884c\u3057\u307e\u3057\u305f\u3002\u3059\u3050\u306b\u80f8\u9aa8\u5727\u8feb\u3092\u518d\u958b\u3057\u3066\u304f\u3060\u3055\u3044\u3002",
                    "Shock delivered. Resume chest compressions now."
                )
            );

            if (shockFlashRoutine != null)
                StopCoroutine(shockFlashRoutine);

            shockFlashRoutine = StartCoroutine(ShockFlashRoutine());
            StartCoroutine(ResumeCprAfterDelay(1.2f));
            StartCoroutine(FadeInstructionAfterDelay(4f));
            UpdateControlButtons();
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
            Debug.Log($"[AEDTrainingRuntime] State -> {newState}: {message}");
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
            aedWorldCanvasRect.sizeDelta = new Vector2(720f, 300f);
            UpdateWorldUiPose();

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 12f;

            instructionPanel = new GameObject("AEDInstructionPanel", typeof(RectTransform), typeof(Image));
            instructionPanel.transform.SetParent(canvasObject.transform, false);

            var panelTransform = instructionPanel.GetComponent<RectTransform>();
            panelTransform.anchorMin = new Vector2(0.5f, 1f);
            panelTransform.anchorMax = new Vector2(0.5f, 1f);
            panelTransform.pivot = new Vector2(0.5f, 1f);
            panelTransform.anchoredPosition = new Vector2(0f, -10f);
            panelTransform.sizeDelta = new Vector2(690f, 105f);

            instructionBackground = instructionPanel.GetComponent<Image>();
            instructionBackground.color = new Color(0f, 0f, 0f, 0.65f);

            var textObject = new GameObject("AEDInstructionText", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(instructionPanel.transform, false);

            var textTransform = textObject.GetComponent<RectTransform>();
            textTransform.anchorMin = Vector2.zero;
            textTransform.anchorMax = Vector2.one;
            textTransform.offsetMin = new Vector2(24f, 12f);
            textTransform.offsetMax = new Vector2(-24f, -12f);

            instructionText = textObject.GetComponent<Text>();
            instructionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            instructionText.fontSize = 32;
            instructionText.alignment = TextAnchor.MiddleCenter;
            instructionText.resizeTextForBestFit = true;
            instructionText.resizeTextMinSize = 20;
            instructionText.resizeTextMaxSize = 36;
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
            panelTransform.anchorMin = new Vector2(0.5f, 0f);
            panelTransform.anchorMax = new Vector2(0.5f, 0f);
            panelTransform.pivot = new Vector2(0.5f, 0f);
            panelTransform.anchoredPosition = new Vector2(0f, 18f);
            panelTransform.sizeDelta = new Vector2(690f, 160f);

            var panelImage = controlPanel.GetComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.62f);

            var titleObject = new GameObject("AEDControlTitle", typeof(RectTransform), typeof(Text));
            titleObject.transform.SetParent(controlPanel.transform, false);
            var titleTransform = titleObject.GetComponent<RectTransform>();
            titleTransform.anchorMin = new Vector2(0f, 1f);
            titleTransform.anchorMax = new Vector2(1f, 1f);
            titleTransform.pivot = new Vector2(0.5f, 1f);
            titleTransform.anchoredPosition = new Vector2(0f, -8f);
            titleTransform.sizeDelta = new Vector2(-32f, 30f);

            controlTitleText = titleObject.GetComponent<Text>();
            controlTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            controlTitleText.fontSize = 22;
            controlTitleText.alignment = TextAnchor.MiddleCenter;
            controlTitleText.color = Color.white;

            powerControlButton = CreateControlButton("AEDPowerControlButton", new Vector2(-252f, 50f), new Vector2(136f, 66f), TurnOnAed);
            leftPadControlButton = CreateControlButton("AEDLeftPadControlButton", new Vector2(-84f, 50f), new Vector2(136f, 66f), () => TriggerTarget(leftPadTarget));
            rightPadControlButton = CreateControlButton("AEDRightPadControlButton", new Vector2(84f, 50f), new Vector2(136f, 66f), () => TriggerTarget(rightPadTarget));
            shockControlButton = CreateControlButton("AEDShockControlButton", new Vector2(252f, 50f), new Vector2(136f, 66f), DeliverShock);

            controlPanel.SetActive(false);
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
            label.fontSize = 22;
            label.alignment = TextAnchor.MiddleCenter;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 14;
            label.resizeTextMaxSize = 24;
            label.color = Color.black;

            return button;
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
            SetButtonText(leftPadControlButton, LanguageManager.Get("\u8d34\u4e0a\u7535\u6781", "\u4e0a\u30d1\u30c3\u30c9", "Upper Pad"));
            SetButtonText(rightPadControlButton, LanguageManager.Get("\u8d34\u4e0b\u7535\u6781", "\u4e0b\u30d1\u30c3\u30c9", "Lower Pad"));
            SetButtonText(shockControlButton, LanguageManager.Get("\u7535\u51fb", "\u30b7\u30e7\u30c3\u30af", "Shock"));

            if (controlTitleText != null)
            {
                controlTitleText.text = LanguageManager.Get("\u0041\u0045\u0044 \u64cd\u4f5c", "\u0041\u0045\u0044 \u64cd\u4f5c", "AED Controls");
            }

            if (powerControlButton != null)
                powerControlButton.interactable = currentState == AEDState.Off;

            if (leftPadControlButton != null)
                leftPadControlButton.interactable = currentState == AEDState.On && !leftPadPlaced;

            if (rightPadControlButton != null)
                rightPadControlButton.interactable = currentState == AEDState.On && !rightPadPlaced;

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

            HideInstructionOverlay();
            UpdateControlButtons();
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

        private Transform FindChestReference()
        {
            CPRPressDetector pressDetector = FindObjectOfType<CPRPressDetector>();
            if (pressDetector != null && pressDetector.chestSurface != null)
            {
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
    }
}
