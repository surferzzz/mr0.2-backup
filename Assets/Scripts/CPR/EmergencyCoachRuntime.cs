using CPRSimulation.Game;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CPRSimulation
{
    public sealed class EmergencyCoachRuntime : MonoBehaviour
    {
        private const float CanvasWidth = 520f;
        private const float CanvasHeight = 210f;
        private const float FillMaxWidth = 430f;
        private const float DefaultFixedHeightAbovePatient = 0.46f;
        private const float DefaultFixedDistanceTowardCamera = 0.10f;
        private const float DefaultFixedSideOffset = 0f;
        private const float DefaultFixedCanvasScale = 0.0011f;

        private static EmergencyCoachRuntime instance;

        private CPRTrainingController controller;
        private Canvas canvas;
        private RectTransform canvasRect;
        private GameObject panel;
        private Text titleText;
        private Text bodyText;
        private Text progressText;
        private RectTransform progressFill;
        private int currentCompressions;
        private int targetCompressions = 30;
        private bool fixedPoseApplied;

        [Header("World Pose")]
        [SerializeField] private bool followCamera = false;
        [SerializeField] private float fixedHeightAbovePatient = DefaultFixedHeightAbovePatient;
        [SerializeField] private float fixedDistanceTowardCamera = DefaultFixedDistanceTowardCamera;
        [SerializeField] private float fixedSideOffset = DefaultFixedSideOffset;
        [SerializeField] private float fixedCanvasScale = DefaultFixedCanvasScale;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            EnsureInstance();
        }

        private static void EnsureInstance()
        {
            if (instance != null)
            {
                return;
            }

            var host = new GameObject("EmergencyCoachRuntime");
            DontDestroyOnLoad(host);
            instance = host.AddComponent<EmergencyCoachRuntime>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            CPRTrainingController.OnPhaseChanged += HandlePhaseChanged;
            CPRTrainingController.OnCPRStarted += HandleCprStarted;
            CPRTrainingController.OnCompressionProgress += HandleCompressionProgress;
            ResolveController();
            EnsureCanvas();
            Hide();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }

            SceneManager.sceneLoaded -= HandleSceneLoaded;
            CPRTrainingController.OnPhaseChanged -= HandlePhaseChanged;
            CPRTrainingController.OnCPRStarted -= HandleCprStarted;
            CPRTrainingController.OnCompressionProgress -= HandleCompressionProgress;
        }

        private void LateUpdate()
        {
            if (panel == null || !panel.activeSelf || canvasRect == null)
            {
                return;
            }

            if (!followCamera)
            {
                ApplyFixedCoachPoseIfNeeded();
                return;
            }

            var cameraTransform = GetCameraTransform();
            if (cameraTransform == null)
            {
                return;
            }

            canvasRect.position = cameraTransform.position +
                                  cameraTransform.forward * 1.15f +
                                  cameraTransform.up * -0.28f;
            canvasRect.rotation = Quaternion.LookRotation(canvasRect.position - cameraTransform.position, Vector3.up);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ResolveController();
            fixedPoseApplied = false;
            Hide();
        }

        private void ResolveController()
        {
            controller = FindObjectOfType<CPRTrainingController>();
        }

        private void HandlePhaseChanged(CPRTrainingController.TrainingPhase phase)
        {
            ResolveController();

            switch (phase)
            {
                case CPRTrainingController.TrainingPhase.AEDInProgress:
                    followCamera = false;
                    fixedPoseApplied = false;
                    currentCompressions = 0;
                    SetProgress(0, targetCompressions);
                    Hide();
                    break;

                case CPRTrainingController.TrainingPhase.CPRInProgress:
                    fixedPoseApplied = false;
                    Hide();
                    break;

                case CPRTrainingController.TrainingPhase.Completed:
                    fixedPoseApplied = false;
                    Hide();
                    break;

                case CPRTrainingController.TrainingPhase.Intro:
                case CPRTrainingController.TrainingPhase.WaitingStart:
                case CPRTrainingController.TrainingPhase.PatientFalling:
                case CPRTrainingController.TrainingPhase.ReadyForCPR:
                    Hide();
                    break;
            }
        }

        private void HandleCprStarted()
        {
            fixedPoseApplied = false;
            currentCompressions = 0;
            Hide();
        }

        private void HandleCompressionProgress(int current, int target)
        {
            currentCompressions = current;
            targetCompressions = Mathf.Max(1, target);
        }

        private void ShowCprCoach()
        {
            SetProgress(currentCompressions, targetCompressions);

            string tip;
            if (currentCompressions < 10)
            {
                tip = LanguageManager.Get(
                    "\u524d10\u6b21\uff1a\u53cc\u80a9\u5728\u624b\u638c\u6b63\u4e0a\u65b9\uff0c\u5148\u627e\u5230\u7a33\u5b9a\u8282\u594f\u3002",
                    "\u6700\u521d\u306e10\u56de\uff1a\u80a9\u3092\u624b\u306e\u771f\u4e0a\u306b\u7f6e\u304d\u3001\u30ea\u30ba\u30e0\u3092\u5b89\u5b9a\u3055\u305b\u307e\u3059\u3002",
                    "First 10: keep shoulders above hands and establish a steady rhythm."
                );
            }
            else if (currentCompressions < 20)
            {
                tip = LanguageManager.Get(
                    "\u4e2d\u6bb5\uff1a\u6bcf\u6b21\u6309\u538b\u540e\u8ba9\u80f8\u90e8\u56de\u5f39\uff0c\u4e0d\u8981\u505c\u987f\u3002",
                    "\u4e2d\u76e4\uff1a\u6bce\u56de\u306e\u5727\u8feb\u5f8c\u306b\u80f8\u3092\u623b\u3057\u3001\u6b62\u307e\u3089\u306a\u3044\u3002",
                    "Middle set: let the chest recoil after each compression; do not pause."
                );
            }
            else
            {
                tip = LanguageManager.Get(
                    "\u6700\u540e\u9636\u6bb5\uff1a\u4fdd\u6301\u8282\u594f\uff0c\u5b8c\u621030\u6b21\u540e\u67e5\u770b\u590d\u76d8\u3002",
                    "\u6700\u7d42\u6bb5\u968e\uff1a\u30ea\u30ba\u30e0\u3092\u4fdd\u3061\u300130\u56de\u5b8c\u4e86\u5f8c\u306b\u632f\u308a\u8fd4\u308a\u307e\u3059\u3002",
                    "Final set: hold the rhythm and review results after 30 compressions."
                );
            }

            Show(
                LanguageManager.Get("\u5fc3\u80ba\u590d\u82cf\u6559\u7ec3", "\u0043\u0050\u0052 \u30b3\u30fc\u30c1", "CPR Coach"),
                tip
            );
        }

        private void EnsureCanvas()
        {
            if (canvas != null)
            {
                return;
            }

            var canvasObject = new GameObject("EmergencyCoachCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 90;

            canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);
            canvasRect.localScale = Vector3.one * fixedCanvasScale;

            panel = new GameObject("CoachPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvasObject.transform, false);

            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var panelImage = panel.GetComponent<Image>();
            panelImage.color = new Color(0.02f, 0.04f, 0.05f, 0.78f);

            titleText = CreateText("Title", panel.transform, new Vector2(0f, -22f), new Vector2(-36f, 42f), 28, TextAnchor.MiddleLeft);
            bodyText = CreateText("Body", panel.transform, new Vector2(0f, -82f), new Vector2(-36f, 72f), 20, TextAnchor.UpperLeft);
            progressText = CreateText("Progress", panel.transform, new Vector2(0f, -168f), new Vector2(-36f, 26f), 18, TextAnchor.MiddleRight);

            var track = new GameObject("ProgressTrack", typeof(RectTransform), typeof(Image));
            track.transform.SetParent(panel.transform, false);
            var trackRect = track.GetComponent<RectTransform>();
            trackRect.anchorMin = new Vector2(0f, 0f);
            trackRect.anchorMax = new Vector2(0f, 0f);
            trackRect.pivot = new Vector2(0f, 0.5f);
            trackRect.anchoredPosition = new Vector2(24f, 26f);
            trackRect.sizeDelta = new Vector2(FillMaxWidth, 12f);
            track.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.18f);

            var fill = new GameObject("ProgressFill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(track.transform, false);
            progressFill = fill.GetComponent<RectTransform>();
            progressFill.anchorMin = new Vector2(0f, 0f);
            progressFill.anchorMax = new Vector2(0f, 1f);
            progressFill.pivot = new Vector2(0f, 0.5f);
            progressFill.anchoredPosition = Vector2.zero;
            progressFill.sizeDelta = new Vector2(0f, 0f);
            fill.GetComponent<Image>().color = new Color(0.12f, 0.82f, 0.52f, 0.95f);
        }

        private static Text CreateText(string name, Transform parent, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);

            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 12;
            text.resizeTextMaxSize = fontSize;
            return text;
        }

        private void Show(string title, string body)
        {
            EnsureCanvas();
            panel.SetActive(true);
            titleText.text = title;
            bodyText.text = body;
            ApplyFixedCoachPoseIfNeeded();
        }

        private void Hide()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }

            fixedPoseApplied = false;
        }

        private void SetProgress(int current, int target)
        {
            EnsureCanvas();
            target = Mathf.Max(1, target);
            float t = Mathf.Clamp01((float)current / target);
            if (progressFill != null)
            {
                progressFill.sizeDelta = new Vector2(FillMaxWidth * t, 0f);
            }

            if (progressText != null)
            {
                progressText.text = $"{current} / {target}";
            }
        }

        private static Transform GetCameraTransform()
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

        private void ApplyFixedCoachPoseIfNeeded()
        {
            if (fixedPoseApplied || followCamera || canvasRect == null)
            {
                return;
            }

            Transform cameraTransform = GetCameraTransform();
            Transform patientTransform = ResolvePatientHeadTransform();

            Vector3 position;
            if (patientTransform != null)
            {
                Vector3 towardCamera = Vector3.zero;
                if (cameraTransform != null)
                {
                    towardCamera = Vector3.ProjectOnPlane(
                        cameraTransform.position - patientTransform.position,
                        Vector3.up
                    );
                }

                if (towardCamera.sqrMagnitude < 0.001f)
                {
                    towardCamera = Vector3.ProjectOnPlane(-patientTransform.forward, Vector3.up);
                }

                if (towardCamera.sqrMagnitude < 0.001f)
                {
                    towardCamera = Vector3.forward;
                }

                towardCamera.Normalize();
                Vector3 side = Vector3.Cross(Vector3.up, towardCamera).normalized;
                position = patientTransform.position +
                           Vector3.up * fixedHeightAbovePatient +
                           towardCamera * fixedDistanceTowardCamera +
                           side * fixedSideOffset;
            }
            else if (cameraTransform != null)
            {
                position = cameraTransform.position +
                           cameraTransform.forward * 1.05f +
                           Vector3.up * -0.12f;
            }
            else
            {
                position = new Vector3(-0.28f, 1.15f, 2.45f);
            }

            canvasRect.SetParent(transform, true);
            canvasRect.position = position;
            canvasRect.localScale = Vector3.one * fixedCanvasScale;

            if (cameraTransform != null)
            {
                Vector3 forward = canvasRect.position - cameraTransform.position;
                if (forward.sqrMagnitude > 0.001f)
                {
                    canvasRect.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
                }
            }
            else
            {
                canvasRect.rotation = Quaternion.Euler(18f, 180f, 0f);
            }

            fixedPoseApplied = true;
        }

        private Transform ResolvePatientTransform()
        {
            if (controller == null)
            {
                ResolveController();
            }

            if (controller != null && controller.patientModel != null)
            {
                return controller.patientModel.transform;
            }

            var animators = FindObjectsOfType<Animator>(true);
            foreach (var animator in animators)
            {
                string objectName = animator.gameObject.name;
                if (objectName.IndexOf("OVRCameraRig", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }

                if (objectName.IndexOf("Patient", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    objectName.IndexOf("Remy", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    objectName.IndexOf("Standing React Death Backward", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return animator.transform;
                }
            }

            return null;
        }

        private Transform ResolvePatientHeadTransform()
        {
            Transform patientTransform = ResolvePatientTransform();
            if (patientTransform == null)
            {
                return null;
            }

            string[] headNames = { "Head", "Neck", "頭", "头" };
            foreach (string headName in headNames)
            {
                Transform result = FindChildRecursive(patientTransform, headName);
                if (result != null)
                {
                    return result;
                }
            }

            return patientTransform;
        }

        private static Transform FindChildRecursive(Transform parent, string nameContains)
        {
            if (parent == null)
            {
                return null;
            }

            foreach (Transform child in parent)
            {
                if (child.name.IndexOf(nameContains, System.StringComparison.OrdinalIgnoreCase) >= 0)
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
