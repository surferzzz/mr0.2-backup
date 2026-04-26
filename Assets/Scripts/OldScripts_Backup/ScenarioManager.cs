using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 场景管理器 - 管理不同的急救场景
/// 支持车祸、地震、家庭意外、公共场所等多种场景
/// </summary>
public class ScenarioManager : MonoBehaviour
{
    [Header("=== 当前场景 ===")]
    [SerializeField] private EmergencyScenario currentScenario = EmergencyScenario.Earthquake;
    
    [Header("=== 场景环境设置 ===")]
    [SerializeField] private bool autoSetupEnvironment = true;
    [SerializeField] private GameObject scenarioContainer;
    
    [Header("=== 场景音效 ===")]
    [SerializeField] private AudioSource ambientAudio;
    [SerializeField] private AudioClip trafficNoise;
    [SerializeField] private AudioClip earthquakeSound;
    [SerializeField] private AudioClip crowdNoise;
    
    [Header("=== 场景灯光 ===")]
    [SerializeField] private Light sceneLight;
    [SerializeField] private float normalLightIntensity = 1f;
    [SerializeField] private float lowLightIntensity = 0.3f;
    
    // 场景对象池
    private Dictionary<EmergencyScenario, GameObject> scenarioObjects = new Dictionary<EmergencyScenario, GameObject>();
    
    // 事件系统
    public static System.Action<EmergencyScenario> OnScenarioChanged;
    
    void Start()
    {
        InitializeScenario();
    }
    
    void InitializeScenario()
    {
        // 创建场景容器
        if (scenarioContainer == null)
        {
            scenarioContainer = new GameObject("ScenarioContainer");
            scenarioContainer.transform.SetParent(transform);
        }
        
        // 获取场景灯光
        if (sceneLight == null)
        {
            sceneLight = FindObjectOfType<Light>();
        }
        
        // 创建音频源
        if (ambientAudio == null)
        {
            GameObject audioGO = new GameObject("AmbientAudio");
            audioGO.transform.SetParent(transform);
            ambientAudio = audioGO.AddComponent<AudioSource>();
            ambientAudio.loop = true;
            ambientAudio.playOnAwake = false;
            ambientAudio.spatialBlend = 0.5f;
        }
        
        // 初始环境布置
        if (autoSetupEnvironment)
        {
            SetupScenarioEnvironment(currentScenario);
        }
        
        Debug.Log($"🎬 场景管理器初始化完成 - 当前场景: {currentScenario}");
    }
    
    [ContextMenu("切换到车祸场景")]
    public void SwitchToCarAccidentScene()
    {
        SetScenario(EmergencyScenario.CarAccident);
    }
    
    [ContextMenu("切换到地震场景")]
    public void SwitchToEarthquakeScene()
    {
        SetScenario(EmergencyScenario.Earthquake);
    }
    
    [ContextMenu("切换到家庭意外场景")]
    public void SwitchToHomeAccidentScene()
    {
        SetScenario(EmergencyScenario.HomeAccident);
    }
    
    [ContextMenu("切换到公共场所场景")]
    public void SwitchToPublicPlaceScene()
    {
        SetScenario(EmergencyScenario.PublicPlace);
    }
    
    [ContextMenu("切换到训练场景")]
    public void SwitchToTrainingScene()
    {
        SetScenario(EmergencyScenario.Training);
    }
    
    public void SetScenario(EmergencyScenario scenario)
    {
        if (currentScenario == scenario)
        {
            Debug.LogWarning($"⚠️ 已经在 {scenario} 场景中");
            return;
        }
        
        Debug.Log($"🎬 切换场景: {currentScenario} → {scenario}");
        
        // 清理当前场景
        CleanupCurrentScenario();
        
        // 设置新场景
        currentScenario = scenario;
        
        // 设置场景环境
        if (autoSetupEnvironment)
        {
            SetupScenarioEnvironment(scenario);
        }
        
        // 触发场景切换事件
        OnScenarioChanged?.Invoke(scenario);
        
        Debug.Log($"✅ 场景切换完成: {scenario}");
    }
    
    void CleanupCurrentScenario()
    {
        // 停止环境音效
        if (ambientAudio != null && ambientAudio.isPlaying)
        {
            ambientAudio.Stop();
        }
        
        // 恢复正常灯光
        SetNormalLighting();
        
        // 清理场景对象（保留容器）
        if (scenarioContainer != null)
        {
            foreach (Transform child in scenarioContainer.transform)
            {
                Destroy(child.gameObject);
            }
        }
    }
    
    void SetupScenarioEnvironment(EmergencyScenario scenario)
    {
        switch (scenario)
        {
            case EmergencyScenario.CarAccident:
                SetupCarAccidentEnvironment();
                break;
                
            case EmergencyScenario.Earthquake:
                SetupEarthquakeEnvironment();
                break;
                
            case EmergencyScenario.HomeAccident:
                SetupHomeAccidentEnvironment();
                break;
                
            case EmergencyScenario.PublicPlace:
                SetupPublicPlaceEnvironment();
                break;
                
            case EmergencyScenario.Training:
                SetupTrainingEnvironment();
                break;
        }
    }
    
    void SetupCarAccidentEnvironment()
    {
        Debug.Log("🚗 设置车祸场景环境...");
        
        // 创建道路标记
        CreateRoadMarkers();
        
        // 创建车辆残骸（简单方块表示）
        CreateCarWreckage();
        
        // 播放交通噪音
        if (trafficNoise != null && ambientAudio != null)
        {
            ambientAudio.clip = trafficNoise;
            ambientAudio.Play();
        }
        
        // 设置户外灯光
        SetNormalLighting();
        
        Debug.Log("✅ 车祸场景环境设置完成");
    }
    
    void SetupEarthquakeEnvironment()
    {
        Debug.Log("⚡ 设置地震场景环境...");
        
        // 创建废墟效果
        CreateDebris();
        
        // 播放地震余音
        if (earthquakeSound != null && ambientAudio != null)
        {
            ambientAudio.clip = earthquakeSound;
            ambientAudio.volume = 0.3f;
            ambientAudio.Play();
        }
        
        // 设置昏暗灯光
        SetLowLighting();
        
        // 添加灰尘效果（可选）
        CreateDustEffect();

        // 触发地震震动效果
        var quake = FindObjectOfType<EarthquakeEffect>();
        if (quake == null && Camera.main != null)
        {
            quake = Camera.main.gameObject.AddComponent<EarthquakeEffect>();
        }
        if (quake != null)
        {
            quake.PlayQuake();
        }
        
        Debug.Log("✅ 地震场景环境设置完成");
    }
    
    void SetupHomeAccidentEnvironment()
    {
        Debug.Log("🏠 设置家庭意外场景环境...");
        
        // 创建家具（简单表示）
        CreateHomeFurniture();
        
        // 设置室内灯光
        SetNormalLighting();
        
        // 添加一些家居物品
        CreateHomeObjects();
        
        Debug.Log("✅ 家庭意外场景环境设置完成");
    }
    
    void SetupPublicPlaceEnvironment()
    {
        Debug.Log("🏢 设置公共场所场景环境...");
        
        // 创建围观人群（简单圆柱表示）
        CreateCrowdCircle();
        
        // 播放人群噪音
        if (crowdNoise != null && ambientAudio != null)
        {
            ambientAudio.clip = crowdNoise;
            ambientAudio.Play();
        }
        
        // 设置公共场所灯光
        SetNormalLighting();
        
        Debug.Log("✅ 公共场所场景环境设置完成");
    }
    
    void SetupTrainingEnvironment()
    {
        Debug.Log("📚 设置训练场景环境...");
        
        // 简洁的训练环境，不添加额外物体
        SetNormalLighting();
        
        // 停止所有环境音效
        if (ambientAudio != null)
        {
            ambientAudio.Stop();
        }
        
        Debug.Log("✅ 训练场景环境设置完成");
    }
    
    // === 环境创建辅助方法 ===
    
    void CreateRoadMarkers()
    {
        // 创建道路标线
        GameObject roadLine = GameObject.CreatePrimitive(PrimitiveType.Plane);
        roadLine.name = "RoadLine";
        roadLine.transform.SetParent(scenarioContainer.transform);
        roadLine.transform.position = new Vector3(0, 0, 0);
        roadLine.transform.localScale = new Vector3(1, 1, 3);
        
        Renderer renderer = roadLine.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(0.3f, 0.3f, 0.3f); // 深灰色
        }
    }
    
    void CreateCarWreckage()
    {
        // 创建简单的车辆残骸
        GameObject car = GameObject.CreatePrimitive(PrimitiveType.Cube);
        car.name = "CarWreckage";
        car.transform.SetParent(scenarioContainer.transform);
        car.transform.position = new Vector3(2f, 0.5f, 1f);
        car.transform.localScale = new Vector3(1.5f, 1f, 3f);
        car.transform.rotation = Quaternion.Euler(0, 30f, 0);
        
        Renderer renderer = car.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(0.8f, 0.2f, 0.2f); // 红色
        }
        
        // 移除碰撞器避免干扰
        Collider collider = car.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }
    }
    
    void CreateDebris()
    {
        // 创建随机散落的碎片
        for (int i = 0; i < 5; i++)
        {
            GameObject debris = GameObject.CreatePrimitive(PrimitiveType.Cube);
            debris.name = $"Debris_{i}";
            debris.transform.SetParent(scenarioContainer.transform);
            
            Vector3 randomPos = new Vector3(
                Random.Range(-3f, 3f),
                Random.Range(0f, 0.5f),
                Random.Range(-3f, 3f)
            );
            debris.transform.position = randomPos;
            debris.transform.localScale = Vector3.one * Random.Range(0.2f, 0.5f);
            debris.transform.rotation = Random.rotation;
            
            Renderer renderer = debris.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.6f, 0.5f, 0.4f); // 灰褐色
            }
            
            // 移除碰撞器
            Collider collider = debris.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }
    }
    
    void CreateDustEffect()
    {
        // 简单的灰尘粒子效果可以在这里添加
        // 需要ParticleSystem支持
        Debug.Log("💨 灰尘效果（待实现）");
    }
    
    void CreateHomeFurniture()
    {
        // 创建沙发
        GameObject sofa = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sofa.name = "Sofa";
        sofa.transform.SetParent(scenarioContainer.transform);
        sofa.transform.position = new Vector3(-2f, 0.4f, -1f);
        sofa.transform.localScale = new Vector3(2f, 0.8f, 1f);
        
        Renderer sofaRenderer = sofa.GetComponent<Renderer>();
        if (sofaRenderer != null)
        {
            sofaRenderer.material.color = new Color(0.5f, 0.3f, 0.2f); // 棕色
        }
        
        // 移除碰撞器
        Destroy(sofa.GetComponent<Collider>());
        
        // 创建桌子
        GameObject table = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        table.name = "Table";
        table.transform.SetParent(scenarioContainer.transform);
        table.transform.position = new Vector3(2f, 0.5f, 0);
        table.transform.localScale = new Vector3(1f, 0.1f, 1f);
        
        Renderer tableRenderer = table.GetComponent<Renderer>();
        if (tableRenderer != null)
        {
            tableRenderer.material.color = new Color(0.4f, 0.3f, 0.2f);
        }
        
        Destroy(table.GetComponent<Collider>());
    }
    
    void CreateHomeObjects()
    {
        // 创建一些小装饰物
        for (int i = 0; i < 3; i++)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = $"HomeObject_{i}";
            obj.transform.SetParent(scenarioContainer.transform);
            obj.transform.position = new Vector3(
                Random.Range(-2f, 2f),
                0.2f,
                Random.Range(-2f, 2f)
            );
            obj.transform.localScale = Vector3.one * 0.2f;
            
            Destroy(obj.GetComponent<Collider>());
        }
    }
    
    void CreateCrowdCircle()
    {
        // 创建围观人群（圆形排列的圆柱体）
        int crowdCount = 8;
        float radius = 4f;
        
        for (int i = 0; i < crowdCount; i++)
        {
            float angle = i * (360f / crowdCount) * Mathf.Deg2Rad;
            Vector3 position = new Vector3(
                Mathf.Cos(angle) * radius,
                1f,
                Mathf.Sin(angle) * radius
            );
            
            GameObject person = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            person.name = $"Crowd_{i}";
            person.transform.SetParent(scenarioContainer.transform);
            person.transform.position = position;
            person.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
            person.transform.LookAt(Vector3.zero);
            
            Renderer renderer = person.GetComponent<Renderer>();
            if (renderer != null)
            {
                // 随机衣服颜色
                renderer.material.color = new Color(
                    Random.Range(0.3f, 0.8f),
                    Random.Range(0.3f, 0.8f),
                    Random.Range(0.3f, 0.8f)
                );
            }
            
            // 移除碰撞器
            Destroy(person.GetComponent<Collider>());
        }
    }
    
    void SetNormalLighting()
    {
        if (sceneLight != null)
        {
            sceneLight.intensity = normalLightIntensity;
            sceneLight.color = Color.white;
        }
        
        RenderSettings.ambientIntensity = 1f;
    }
    
    void SetLowLighting()
    {
        if (sceneLight != null)
        {
            sceneLight.intensity = lowLightIntensity;
            sceneLight.color = new Color(0.8f, 0.8f, 0.9f); // 略带蓝色
        }
        
        RenderSettings.ambientIntensity = 0.3f;
    }
    
    // === 公共接口 ===
    
    public EmergencyScenario GetCurrentScenario()
    {
        return currentScenario;
    }
    
    public string GetScenarioName()
    {
        return GetScenarioDisplayName(currentScenario);
    }
    
    public static string GetScenarioDisplayName(EmergencyScenario scenario)
    {
        switch (scenario)
        {
            case EmergencyScenario.CarAccident:
                return "车祸现场";
            case EmergencyScenario.Earthquake:
                return "地震灾害";
            case EmergencyScenario.HomeAccident:
                return "家庭意外";
            case EmergencyScenario.PublicPlace:
                return "公共场所";
            case EmergencyScenario.Training:
                return "标准训练";
            default:
                return "未知场景";
        }
    }
    
    [ContextMenu("显示场景信息")]
    public void ShowScenarioInfo()
    {
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Debug.Log($"🎬 当前场景: {GetScenarioName()}");
        Debug.Log($"📍 场景类型: {currentScenario}");
        Debug.Log($"🎵 环境音效: {(ambientAudio != null && ambientAudio.isPlaying ? "播放中" : "未播放")}");
        Debug.Log($"💡 灯光强度: {(sceneLight != null ? sceneLight.intensity.ToString("F2") : "未设置")}");
        Debug.Log($"📦 场景物体数量: {(scenarioContainer != null ? scenarioContainer.transform.childCount : 0)}");
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━");
    }
    
    void OnDestroy()
    {
        CleanupCurrentScenario();
    }
}

