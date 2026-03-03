using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace CPRSimulation.Game
{
    /// <summary>
    /// CPR训练流程控制器 - 控制训练场景中的完整流程
    /// </summary>
    public class CPRTrainingController : MonoBehaviour
    {
        [Header("训练阶段")]
        public TrainingPhase currentPhase = TrainingPhase.Intro;

        [Header("患者模型")]
        public GameObject patientModel;
        public Animator patientAnimator;
        
        [Header("动画参数")]
        public string fallDownTrigger = "FallDown";
        public string lyingIdleTrigger = "LyingIdle";

        [Header("UI面板")]
        public GameObject introPanel;           // 介绍面板
        public GameObject startTrainingPanel;   // 开始训练面板
        public GameObject trainingHUDPanel;     // 训练中的HUD
        public GameObject completionPanel;      // 完成面板

        [Header("UI按钮")]
        public Button startScenarioButton;      // 开始情景按钮
        public Button startCPRButton;           // 开始CPR按钮
        public Button restartButton;            // 重新开始按钮
        public Button exitButton;               // 退出按钮

        [Header("UI文本")]
        public Text phaseText;                  // 阶段提示文本
        public Text instructionText;            // 指导文本
        public Text timerText;                  // 计时器

        [Header("CPR组件")]
        public CPRPressDetector pressDetector;
        public GloveHapticController hapticController;

        [Header("音效")]
        public AudioSource audioSource;
        public AudioClip introSound;
        public AudioClip fallSound;
        public AudioClip startCPRSound;
        public AudioClip completeSound;

        [Header("设置")]
        public float introDuration = 3f;
        public float fallAnimationDelay = 1f;
        public int targetCompressions = 30;

        public enum TrainingPhase
        {
            Intro,          // 介绍
            WaitingStart,   // 等待开始
            PatientFalling, // 患者倒地中
            ReadyForCPR,    // 准备CPR
            CPRInProgress,  // CPR进行中
            Completed       // 完成
        }

        // 事件
        public static event System.Action<TrainingPhase> OnPhaseChanged;
        public static event System.Action OnPatientFell;
        public static event System.Action OnCPRStarted;
        public static event System.Action<int, int> OnCompressionProgress; // (current, target)

        private float trainingStartTime;
        private int compressionCount;

        private void Awake()
        {
            // 最早禁用其他冲突的控制器
            DisableOtherControllers();
        }

        private void Start()
        {
            AutoFindAndFixPatient();
            ReplaceUITextToJapanese();
            AutoFindUI();
            SetupButtons();
            SetupCPRDetector();
            StartCoroutine(StartIntroSequence());
        }

        private void ReplaceUITextToJapanese()
        {
            var allTexts = FindObjectsOfType<Text>(true);
            foreach (var t in allTexts)
            {
                string original = t.text;
                t.text = original
                    .Replace("CPR 训练系统", "CPR トレーニングシステム")
                    .Replace("CPR 心肺复苏训练", "CPR トレーニングシステム")
                    .Replace("CPR训练系统", "CPR トレーニングシステム")
                    .Replace("心肺复苏虚拟现实训练", "心肺蘇生バーチャルリアリティ訓練")
                    .Replace("心肺复苏训练", "心肺蘇生トレーニング")
                    .Replace("开 始 训 练", "トレーニング開始")
                    .Replace("开始训练", "トレーニング開始")
                    .Replace("开始情景", "シナリオ開始")
                    .Replace("开始CPR", "CPR開始")
                    .Replace("退 出", "終 了")
                    .Replace("退出", "終了")
                    .Replace("重新开始", "やり直す")
                    .Replace("准备开始", "準備中")
                    .Replace("Quest 3 + 手部追踪", "Quest 3 + ハンドトラッキング")
                    .Replace("CPR进行中", "CPR実施中")
                    .Replace("CPR 进行中", "CPR実施中")
                    .Replace("训练完成", "トレーニング完了")
                    .Replace("开始按压", "圧迫開始")
                    .Replace("按压次数", "圧迫回数")
                    .Replace("按压深度", "圧迫深度")
                    .Replace("按压频率", "圧迫テンポ")
                    .Replace("目标", "目標")
                    .Replace("深度", "深さ")
                    .Replace("频率", "テンポ")
                    .Replace("次/分钟", "回/分")
                    .Replace("次/分", "回/分")
                    .Replace("按压", "圧迫")
                    .Replace("开始", "開始")
                    .Replace("准备", "準備");

                if (original != t.text)
                    Debug.Log($"[CPRTraining] UI翻訳: '{original}' → '{t.text}'");
            }
        }

        private Vector3 patientStandPosition;

        private void AutoFindAndFixPatient()
        {
            if (patientModel == null)
                patientModel = GameObject.Find("XBot_Patient");

            if (patientModel == null)
            {
                Debug.LogWarning("[CPRTraining] 未找到 XBot_Patient！");
                return;
            }

            // 修复 Scale
            if (patientModel.transform.localScale.x < 0.5f)
            {
                patientModel.transform.localScale = Vector3.one;
                Debug.Log("[CPRTraining] 修复 Scale -> (1,1,1)");
            }

            // 修复 Position（确保不在原点）
            if (patientModel.transform.position.magnitude < 0.1f)
            {
                patientModel.transform.position = new Vector3(0, 0, 2f);
                Debug.Log("[CPRTraining] 修复 Position -> (0,0,2)");
            }

            // 面向相机
            patientModel.transform.rotation = Quaternion.Euler(0, 180, 0);
            patientModel.SetActive(true);
            patientStandPosition = patientModel.transform.position;

            patientAnimator = patientModel.GetComponent<Animator>();
            if (patientAnimator != null)
            {
                // 尝试加载 Controller（如果还没有的话）
                if (patientAnimator.runtimeAnimatorController == null)
                {
                    var ctrl = Resources.Load<RuntimeAnimatorController>("XBot_DeathController");
                    if (ctrl != null)
                        patientAnimator.runtimeAnimatorController = ctrl;
                }

                patientAnimator.applyRootMotion = true;

                bool hasController = patientAnimator.runtimeAnimatorController != null;
                Debug.Log($"[CPRTraining] Animator: controller={hasController}");

                if (!hasController)
                {
                    Debug.LogWarning("[CPRTraining] Animator 没有 Controller！请在 Unity 中运行 Tools > CPR > 创建动画控制器");
                }
            }

            Debug.Log($"[CPRTraining] 患者就绪 Pos:{patientModel.transform.position} Scale:{patientModel.transform.localScale}");
        }

        private void AutoFindUI()
        {
            // 尝试从场景中自动查找按钮
            if (startScenarioButton == null)
            {
                var btn = FindButtonByName("StartButton");
                if (btn != null) startScenarioButton = btn;
            }
            if (exitButton == null)
            {
                var btn = FindButtonByName("QuitButton");
                if (btn != null) exitButton = btn;
            }

            // 查找文本（用于显示状态）
            if (phaseText == null || instructionText == null)
            {
                var allTexts = FindObjectsOfType<Text>(true);
                foreach (var t in allTexts)
                {
                    if (t.text.Contains("CPR") || t.text.Contains("训练") || t.text.Contains("トレーニング"))
                    {
                        if (phaseText == null) phaseText = t;
                    }
                    if (t.text.Contains("按压") || t.text.Contains("开始") || t.text.Contains("開始") || t.text.Contains("圧迫"))
                    {
                        if (instructionText == null) instructionText = t;
                    }
                }
            }

            Debug.Log($"[CPRTraining] UI: startBtn={startScenarioButton != null}, exitBtn={exitButton != null}");
        }

        private Button FindButtonByName(string name)
        {
            var obj = GameObject.Find(name);
            if (obj != null) return obj.GetComponent<Button>();
            // 递归搜索
            var allButtons = FindObjectsOfType<Button>(true);
            foreach (var b in allButtons)
            {
                if (b.gameObject.name == name) return b;
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
            // 移除 PatientFallController（它会覆盖 Animator 设置）
            var patient = GameObject.Find("XBot_Patient");
            if (patient != null)
            {
                var fallCtrl = patient.GetComponent<Core.PatientFallController>();
                if (fallCtrl != null)
                {
                    Destroy(fallCtrl);
                    Debug.Log("[CPRTraining] 移除冲突的 PatientFallController");
                }
            }
        }

        private void SetupButtons()
        {
            if (startScenarioButton != null)
            {
                startScenarioButton.onClick.RemoveAllListeners();
                startScenarioButton.onClick.AddListener(OnStartScenarioClicked);
                Debug.Log("[CPRTraining] 开始按钮已绑定到倒地流程");
            }
            
            if (startCPRButton != null)
                startCPRButton.onClick.AddListener(OnStartCPRClicked);
            
            if (restartButton != null)
                restartButton.onClick.AddListener(RestartTraining);
            
            if (exitButton != null)
            {
                exitButton.onClick.RemoveAllListeners();
                exitButton.onClick.AddListener(ExitToMainMenu);
            }
        }

        private void SetupCPRDetector()
        {
            if (pressDetector == null)
                pressDetector = FindObjectOfType<CPRPressDetector>();

            if (pressDetector != null)
            {
                pressDetector.OnCompressionComplete += OnCompressionDetected;
            }
        }

        private IEnumerator StartIntroSequence()
        {
            ChangePhase(TrainingPhase.Intro);
            
            // 如果有面板就显示，没有就跳过
            if (introPanel != null)
                ShowPanel(introPanel);
            
            UpdateInstruction("CPR心肺蘇生トレーニングへようこそ");
            PlaySound(introSound);

            yield return new WaitForSeconds(introDuration);

            // 如果有开始按钮，等待用户点击
            if (startScenarioButton != null)
            {
                ChangePhase(TrainingPhase.WaitingStart);
                if (startTrainingPanel != null)
                    ShowPanel(startTrainingPanel);
                UpdateInstruction("「トレーニング開始」ボタンを押してください");
                Debug.Log("[CPRTraining] 等待用户点击开始按钮...");
            }
            else
            {
                // 没有按钮，自动开始倒地
                Debug.Log("[CPRTraining] 没有开始按钮，自动触发倒地");
                StartCoroutine(PatientFallSequence());
            }
        }

        private void OnStartScenarioClicked()
        {
            StartCoroutine(PatientFallSequence());
        }

        private IEnumerator PatientFallSequence()
        {
            ChangePhase(TrainingPhase.PatientFalling);
            HideAllPanels();
            
            UpdateInstruction("注意！人が倒れました！");
            PlaySound(fallSound);

            if (patientModel != null)
            {
                yield return StartCoroutine(PlayDeathAnimation());
                CreateCPRDetectionZone();
            }

            yield return new WaitForSeconds(0.5f);

            OnPatientFell?.Invoke();
            
            // 直接进入 CPR 阶段
            ChangePhase(TrainingPhase.CPRInProgress);
            UpdateInstruction($"患者が倒れました！胸部の赤いエリアを押してください\n目標：{targetCompressions}回");
            trainingStartTime = Time.time;
            compressionCount = 0;
        }

        private IEnumerator PlayDeathAnimation()
        {
            if (patientModel == null) yield break;

            bool hasAnimator = patientAnimator != null 
                && patientAnimator.runtimeAnimatorController != null;

            if (hasAnimator)
            {
                Debug.Log("[CPRTraining] ===== 开始播放 FBX 死亡动画 =====");
                
                patientAnimator.enabled = true;
                patientAnimator.applyRootMotion = true;
                patientAnimator.speed = 1f;

                // 直接播放 DeathFromRight 状态（不依赖 Trigger）
                patientAnimator.Play("DeathFromRight", 0, 0f);
                Debug.Log("[CPRTraining] Play(DeathFromRight) 已调用");

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

                // 等待动画完成
                float elapsed = 0f;
                while (elapsed < animLength + 0.2f)
                {
                    elapsed += Time.deltaTime;
                    yield return null;

                    Vector3 pos = patientModel.transform.position;
                    if (pos.y < 0f)
                        patientModel.transform.position = new Vector3(pos.x, 0f, pos.z);
                }

                // 冻结最终姿势
                patientAnimator.speed = 0f;

                Vector3 finalPos = patientModel.transform.position;
                finalPos.y = Mathf.Max(finalPos.y, 0f);
                patientModel.transform.position = finalPos;

                Debug.Log($"[CPRTraining] 动画完成 Pos:{finalPos} Rot:{patientModel.transform.rotation.eulerAngles}");
            }
            else
            {
                Debug.LogWarning("[CPRTraining] 无 AnimatorController！请 Ctrl+R 重新编译让 AutoAnimatorSetup 自动创建");
                yield return new WaitForSeconds(1f);
            }
        }

        private GameObject chestDetectionZone;

        private void CreateCPRDetectionZone()
        {
            if (chestDetectionZone != null) return;

            // 找胸部骨骼
            Transform chestBone = FindBoneRecursive(patientModel.transform, "Spine2");
            if (chestBone == null) chestBone = FindBoneRecursive(patientModel.transform, "Spine1");
            if (chestBone == null) chestBone = FindBoneRecursive(patientModel.transform, "Chest");
            if (chestBone == null) chestBone = FindBoneRecursive(patientModel.transform, "Spine");

            Vector3 chestWorldPos;
            if (chestBone != null)
            {
                chestWorldPos = chestBone.position;
                // 胸骨位置向上偏移一点（面向上方 = CPR 按压面）
                chestWorldPos.y = Mathf.Max(chestWorldPos.y, 0.1f) + 0.05f;
                Debug.Log($"[CPRTraining] 找到胸部骨骼: {chestBone.name} 位置: {chestWorldPos}");
            }
            else
            {
                // 估算位置：模型中心偏上
                chestWorldPos = patientModel.transform.position + Vector3.up * 0.2f;
                Debug.Log("[CPRTraining] 未找到胸部骨骼，使用估算位置");
            }

            // 创建检测区域（红色半透明球体）
            chestDetectionZone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            chestDetectionZone.name = "CPR_ChestDetector";
            chestDetectionZone.transform.position = chestWorldPos;
            chestDetectionZone.transform.localScale = new Vector3(0.2f, 0.08f, 0.2f);

            // 红色半透明材质（兼容 URP 和 Built-in）
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
            var collider = chestDetectionZone.GetComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.5f;

            // 添加脉冲发光动画
            var pulser = chestDetectionZone.AddComponent<DetectionZonePulse>();

            Debug.Log($"[CPRTraining] CPR 检测区已创建在 {chestWorldPos}");
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

        private void OnStartCPRClicked()
        {
            StartCPRPhase();
        }

        private void StartCPRPhase()
        {
            ChangePhase(TrainingPhase.CPRInProgress);
            ShowPanel(trainingHUDPanel);
            
            trainingStartTime = Time.time;
            compressionCount = 0;

            // 启动CPR检测
            if (pressDetector != null)
            {
                pressDetector.ResetStats();
                pressDetector.Initialize();
            }

            OnCPRStarted?.Invoke();
            PlaySound(startCPRSound);
            
            UpdateInstruction($"胸骨圧迫を開始！目標：{targetCompressions}回\n圧迫深度：5-6cm、テンポ：100-120回/分");
        }

        private void OnCompressionDetected(float depth, float rate)
        {
            if (currentPhase != TrainingPhase.CPRInProgress) return;

            compressionCount++;
            OnCompressionProgress?.Invoke(compressionCount, targetCompressions);
            
            UpdateInstruction($"圧迫回数: {compressionCount} / {targetCompressions}\n深さ: {depth:F1}cm  テンポ: {rate:F0}/分");

            if (compressionCount >= targetCompressions)
            {
                CompleteTraining();
            }
        }

        private void CompleteTraining()
        {
            ChangePhase(TrainingPhase.Completed);
            ShowPanel(completionPanel);
            
            float totalTime = Time.time - trainingStartTime;
            float avgDepth = pressDetector != null ? pressDetector.CurrentDepthCm : 0;
            float avgRate = pressDetector != null ? pressDetector.CurrentRateBPM : 0;

            string result = $"トレーニング完了！\n\n" +
                           $"総圧迫回数: {compressionCount}\n" +
                           $"所要時間: {totalTime:F1}秒\n" +
                           $"平均テンポ: {avgRate:F0}回/分";

            UpdateInstruction(result);
            PlaySound(completeSound);
            
            Debug.Log($"[CPRTraining] 完成 - 按压:{compressionCount}, 用时:{totalTime:F1}s");
        }

        private void ChangePhase(TrainingPhase newPhase)
        {
            currentPhase = newPhase;
            OnPhaseChanged?.Invoke(newPhase);
            
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
                TrainingPhase.Intro => "紹介",
                TrainingPhase.WaitingStart => "準備中",
                TrainingPhase.PatientFalling => "緊急事態",
                TrainingPhase.ReadyForCPR => "CPR準備",
                TrainingPhase.CPRInProgress => "CPR実施中",
                TrainingPhase.Completed => "トレーニング完了",
                _ => ""
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

        public void RestartTraining()
        {
            compressionCount = 0;
            
            // 重置患者位置
            if (patientModel != null)
            {
                patientModel.transform.rotation = Quaternion.identity;
            }

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
            if (pressDetector != null)
            {
                pressDetector.OnCompressionComplete -= OnCompressionDetected;
            }
        }
    }
}
