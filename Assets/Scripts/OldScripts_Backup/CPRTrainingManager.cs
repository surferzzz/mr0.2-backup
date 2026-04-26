using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum CPRPhase
{
    Assessment,
    Positioning,
    ChestCompression,
    AirwayOpening,
    Ventilation,
    Completed
}

public enum EmergencyScenario
{
    CarAccident,
    Earthquake,
    HomeAccident,
    PublicPlace,
    Training
}

[System.Serializable]
public class CPRTrainingData
{
    [Header("=== 训练基础信息 ===")]
    public EmergencyScenario scenario;
    public float totalTrainingTime;
    public CPRPhase currentPhase;
    public bool isCompleted;

    [Header("=== 胸外按压数据 ===")]
    public int totalCompressions;
    public int validCompressions;
    public float averageDepth;
    public float averageRate;
    public float compressionScore;

    [Header("=== 人工呼吸数据 ===")]
    public int totalVentilations;
    public int validVentilations;
    public float ventilationScore;

    [Header("=== 综合评分 ===")]
    public float overallScore;
    public string performanceLevel;
    public List<string> feedbackMessages;
}

public class CPRTrainingManager : MonoBehaviour
{
    [Header("=== 训练设置 ===")]
    [SerializeField] private EmergencyScenario currentScenario = EmergencyScenario.Training;
    [SerializeField] private bool enableVoiceGuidance = true;
    [SerializeField] private bool enableHapticFeedback = true;
    [SerializeField] private float trainingDuration = 300f; // 5 minutes

    [Header("=== 场景组件 ===")]
    [SerializeField] private Transform patientModel;
    [SerializeField] private Transform chestArea;
    [SerializeField] private Transform headArea;
    [SerializeField] private AudioSource voiceGuidance;
    [SerializeField] private AudioSource ambientAudio;

    [Header("=== 地震效果 ===")]
    [SerializeField] private EarthquakeEffect earthquakeEffect;
    [SerializeField] private float quakeDuration = 1.2f;
    [SerializeField] private float quakePosAmplitude = 0.06f;
    [SerializeField] private float quakeRotAmplitude = 2.5f;
    [SerializeField] private float quakeFrequency = 9f;
    [SerializeField] private float quakeHapticsAmplitude = 0.6f;

    [Header("=== CPR标准参数 ===")]
    [SerializeField] private float targetCompressionDepth = 0.05f; // 5-6 cm
    [SerializeField] private float targetCompressionRate = 100f;   // 100-120 per minute
    [SerializeField] private float compressionReleaseRatio = 0.5f;

    [Header("=== 训练数据 ===")]
    [SerializeField] private CPRTrainingData trainingData;

    private PressDataManager pressDataManager;
    private ScenarioManager scenarioManager;
    private bool isTrainingActive = false;
    private float trainingStartTime;
    private float lastCompressionTime;
    private readonly Queue<float> compressionTimes = new Queue<float>();

    public static System.Action<CPRPhase> OnPhaseChanged;
    public static System.Action<CPRTrainingData> OnTrainingCompleted;
    public static System.Action<string> OnGuidanceMessage;

    void Start()
    {
        InitializeTraining();
        SetupEventListeners();
    }

    void InitializeTraining()
    {
        trainingData = new CPRTrainingData
        {
            scenario = currentScenario,
            currentPhase = CPRPhase.Assessment,
            feedbackMessages = new List<string>()
        };

        pressDataManager = FindObjectOfType<PressDataManager>();
        scenarioManager = FindObjectOfType<ScenarioManager>();

        if (voiceGuidance == null)
        {
            GameObject voiceGO = new GameObject("VoiceGuidance");
            voiceGuidance = voiceGO.AddComponent<AudioSource>();
        }

        LogResult("InitializeTraining", true, $"场景: {currentScenario}");
    }

    void SetupEventListeners()
    {
        if (pressDataManager != null)
        {
            PressDataManager.OnPressCompleted += OnCompressionCompleted;
            PressDataManager.OnDepthUpdated += OnCompressionDepthUpdated;
        }
    }

    [ContextMenu("开始CPR训练")]
    public void StartCPRTraining()
    {
        if (isTrainingActive)
        {
            Debug.LogWarning("⚠️ 训练已在进行中");
            return;
        }

        if (!ValidateDependencies())
        {
            LogResult("StartCPRTraining", false, "依赖未就绪，请检查上方报警");
            return;
        }

        isTrainingActive = true;
        trainingStartTime = Time.time;
        trainingData.currentPhase = CPRPhase.Assessment;

        SetupScenarioEnvironment();
        StartAssessmentPhase();

        LogResult("StartCPRTraining", true, "训练已启动");
    }

    void SetupScenarioEnvironment()
    {
        switch (currentScenario)
        {
            case EmergencyScenario.CarAccident:
                SetupCarAccidentScene();
                break;
            case EmergencyScenario.Earthquake:
                SetupEarthquakeScene();
                break;
            case EmergencyScenario.HomeAccident:
                SetupHomeAccidentScene();
                break;
            case EmergencyScenario.PublicPlace:
                SetupPublicPlaceScene();
                break;
            default:
                SetupTrainingScene();
                break;
        }

        LogResult("SetupScenarioEnvironment", true, $"已切换场景: {currentScenario}");
    }

    void SetupCarAccidentScene()
    {
        PlayGuidanceMessage("您来到了车祸现场，请首先确保现场安全，然后评估伤员状况");
        StartCoroutine(SimulateTrafficNoise());
        StartCoroutine(SimulateCrowdDistraction());
    }

    void SetupEarthquakeScene()
    {
        PlayGuidanceMessage("地震刚刚结束，您在废墟中发现了伤员，请小心余震并开始急救");

        if (earthquakeEffect != null)
        {
            earthquakeEffect.PlayQuake(quakeDuration, quakePosAmplitude, quakeRotAmplitude, quakeFrequency, quakeHapticsAmplitude);
            LogResult("EarthquakeScene", true, "主震触发 (EarthquakeEffect)");
        }
        else
        {
            StartCoroutine(CameraShake(quakeDuration, quakePosAmplitude));
            LogResult("EarthquakeScene", false, "未绑定 EarthquakeEffect，已使用 CameraShake 备用");
        }

        StartCoroutine(SimulateAftershocks());
        SetLowLightEnvironment();
    }

    void SetupHomeAccidentScene()
    {
        PlayGuidanceMessage("家中有人突然倒地，请立即开始急救并呼叫 120");
    }

    void SetupPublicPlaceScene()
    {
        PlayGuidanceMessage("公共场所有人倒地，请在确保安全的前提下进行急救");
    }

    void SetupTrainingScene()
    {
        PlayGuidanceMessage("欢迎进入CPR标准训练，请按照指导完成各个步骤");
    }

    void StartAssessmentPhase()
    {
        trainingData.currentPhase = CPRPhase.Assessment;
        OnPhaseChanged?.Invoke(CPRPhase.Assessment);
        PlayGuidanceMessage("首先检查患者意识：轻拍肩膀并大声呼喊");
        StartCoroutine(WaitForAssessmentCompletion());
    }

    IEnumerator WaitForAssessmentCompletion()
    {
        yield return new WaitForSeconds(5f);
        PlayGuidanceMessage("患者无反应，请检查呼吸");
        yield return new WaitForSeconds(3f);

        PlayGuidanceMessage("患者呼吸异常，立即开始胸外按压！");
        StartCompressionPhase();
    }

    void StartCompressionPhase()
    {
        trainingData.currentPhase = CPRPhase.ChestCompression;
        OnPhaseChanged?.Invoke(CPRPhase.ChestCompression);

        PlayGuidanceMessage("将手掌根部放在胸骨下半部，开始按压");
        PlayGuidanceMessage("按压深度 5-6 厘米，频率 100-120 次每分钟");

        if (pressDataManager != null)
        {
            pressDataManager.ResetSession();
        }
    }

    void OnCompressionCompleted(PressData pressData)
    {
        trainingData.totalCompressions++;

        if (pressData.isValidPress)
        {
            trainingData.validCompressions++;
        }

        float currentTime = Time.time;
        compressionTimes.Enqueue(currentTime);
        if (compressionTimes.Count > 10)
        {
            compressionTimes.Dequeue();
        }

        if (compressionTimes.Count >= 2)
        {
            float[] times = compressionTimes.ToArray();
            float avgInterval = 0f;
            for (int i = 1; i < times.Length; i++)
            {
                avgInterval += times[i] - times[i - 1];
            }
            avgInterval /= (times.Length - 1);

            float currentRate = 60f / avgInterval;
            trainingData.averageRate = currentRate;
            ProvidePressureFeedback(pressData.depth, currentRate);
        }

        lastCompressionTime = currentTime;
    }

    void OnCompressionDepthUpdated(float depth)
    {
        // Real-time depth feedback hook (reserved)
    }

    void ProvidePressureFeedback(float depth, float rate)
    {
        if (depth < targetCompressionDepth * 0.8f)
        {
            PlayGuidanceMessage("按压太浅，请用力一些");
            trainingData.feedbackMessages.Add("按压深度不足");
        }
        else if (depth > targetCompressionDepth * 1.3f)
        {
            PlayGuidanceMessage("按压过深，请减轻力度");
            trainingData.feedbackMessages.Add("按压过深");
        }

        if (rate < 90f)
        {
            PlayGuidanceMessage("按压频率太慢，请加快速度");
        }
        else if (rate > 130f)
        {
            PlayGuidanceMessage("按压频率太快，请放慢一些");
        }
        else if (rate >= 100f && rate <= 120f)
        {
            if (Random.Range(0f, 1f) < 0.1f)
            {
                PlayGuidanceMessage("很好，保持这个频率");
            }
        }
    }

    void PlayGuidanceMessage(string message)
    {
        if (!enableVoiceGuidance) return;

        Debug.Log($"[Guidance] {message}");
        OnGuidanceMessage?.Invoke(message);
    }

    IEnumerator SimulateTrafficNoise()
    {
        while (isTrainingActive && currentScenario == EmergencyScenario.CarAccident)
        {
            yield return new WaitForSeconds(Random.Range(3f, 8f));
            Debug.Log("🚗 车辆经过声音");
        }
    }

    IEnumerator SimulateCrowdDistraction()
    {
        while (isTrainingActive && currentScenario == EmergencyScenario.CarAccident)
        {
            yield return new WaitForSeconds(Random.Range(10f, 20f));
            PlayGuidanceMessage("请围观群众保持距离，给急救留出空间");
        }
    }

    IEnumerator SimulateAftershocks()
    {
        while (isTrainingActive && currentScenario == EmergencyScenario.Earthquake)
        {
            yield return new WaitForSeconds(Random.Range(30f, 60f));

            PlayGuidanceMessage("注意余震！保护好自己和患者");
            LogResult("Aftershock", true, "余震触发");

            if (earthquakeEffect != null)
            {
                earthquakeEffect.PlayQuake(quakeDuration, quakePosAmplitude, quakeRotAmplitude, quakeFrequency, quakeHapticsAmplitude);
            }
            else
            {
                StartCoroutine(CameraShake(quakeDuration, quakePosAmplitude));
            }
        }
    }

    IEnumerator CameraShake(float duration, float magnitude)
    {
        Vector3 originalPos = Camera.main.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            Camera.main.transform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        Camera.main.transform.localPosition = originalPos;
    }

    void SetLowLightEnvironment()
    {
        RenderSettings.ambientIntensity = 0.3f;
        Debug.Log("💡 环境光线昏暗，建议使用手电筒");
    }

    public void CompleteTraining()
    {
        if (!isTrainingActive) return;

        isTrainingActive = false;
        trainingData.totalTrainingTime = Time.time - trainingStartTime;
        trainingData.isCompleted = true;

        CalculateFinalScore();
        OnTrainingCompleted?.Invoke(trainingData);

        LogResult("CompleteTraining", true, $"总分: {trainingData.overallScore:F1}, 等级: {trainingData.performanceLevel}");
    }

    void CalculateFinalScore()
    {
        float compressionQuality = 0f;
        if (trainingData.totalCompressions > 0)
        {
            float validRatio = (float)trainingData.validCompressions / trainingData.totalCompressions;
            compressionQuality = validRatio * 100f;
        }

        trainingData.compressionScore = compressionQuality;
        trainingData.overallScore = compressionQuality;

        if (trainingData.overallScore >= 90f)
            trainingData.performanceLevel = "优秀";
        else if (trainingData.overallScore >= 80f)
            trainingData.performanceLevel = "良好";
        else if (trainingData.overallScore >= 70f)
            trainingData.performanceLevel = "及格";
        else
            trainingData.performanceLevel = "需要改进";
    }

    public CPRTrainingData GetTrainingData() => trainingData;
    public bool IsTrainingActive() => isTrainingActive;
    public EmergencyScenario GetCurrentScenario() => currentScenario;

    public void SetScenario(EmergencyScenario scenario)
    {
        currentScenario = scenario;
        LogResult("SetScenario", true, $"切换至 {scenario}");
    }

    void OnDestroy()
    {
        if (pressDataManager != null)
        {
            PressDataManager.OnPressCompleted -= OnCompressionCompleted;
            PressDataManager.OnDepthUpdated -= OnCompressionDepthUpdated;
        }

        LogResult("CPRTrainingManager.OnDestroy", true, "事件解绑完成");
    }

    void Update()
    {
        if (isTrainingActive && Time.time - trainingStartTime > trainingDuration)
        {
            CompleteTraining();
        }
    }

    private bool ValidateDependencies()
    {
        bool ok = true;

        if (pressDataManager == null)
        {
            pressDataManager = FindObjectOfType<PressDataManager>();
        }

        if (pressDataManager == null)
        {
            Debug.LogError("❌ 缺少 PressDataManager，无法记录按压数据");
            ok = false;
        }

        if (currentScenario == EmergencyScenario.Earthquake && earthquakeEffect == null)
        {
            Debug.LogWarning("⚠️ 地震场景未绑定 EarthquakeEffect，将使用 CameraShake 备用方案");
        }

        if (Camera.main == null)
        {
            Debug.LogWarning("⚠️ 场景未找到 Camera.main，部分效果可能失效");
        }

        return ok;
    }

    private void LogResult(string context, bool success, string detail)
    {
        string status = success ? "OK" : "FAIL";
        Debug.Log($"[{status}] {context} - {detail}");
    }
}

