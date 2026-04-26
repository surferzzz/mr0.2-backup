using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PressData
{
    public float depth;
    public float duration;
    public DateTime timestamp;
    public bool isValidPress;
    
    public PressData(float depth, float duration, bool isValid = true)
    {
        this.depth = depth;
        this.duration = duration;
        this.timestamp = DateTime.Now;
        this.isValidPress = isValid;
    }
}

[System.Serializable]
public class PressSession
{
    public List<PressData> presses = new List<PressData>();
    public DateTime sessionStart;
    public float totalDuration;
    public int validPressCount;
    public float averageDepth;
    public float maxDepth;
    
    public PressSession()
    {
        sessionStart = DateTime.Now;
        presses = new List<PressData>();
    }
    
    public void AddPress(PressData pressData)
    {
        presses.Add(pressData);
        UpdateStatistics();
    }
    
    private void UpdateStatistics()
    {
        validPressCount = 0;
        float totalDepth = 0f;
        maxDepth = 0f;
        
        foreach (var press in presses)
        {
            if (press.isValidPress)
            {
                validPressCount++;
                totalDepth += press.depth;
                if (press.depth > maxDepth)
                    maxDepth = press.depth;
            }
        }
        
        averageDepth = validPressCount > 0 ? totalDepth / validPressCount : 0f;
        totalDuration = (float)(DateTime.Now - sessionStart).TotalSeconds;
    }
}

public class PressDataManager : MonoBehaviour
{
    [Header("=== 按压统计设置 ===")]
    [SerializeField] private float minValidDepth = 0.02f; // 最小有效按压深度
    [SerializeField] private float minValidDuration = 0.5f; // 最小有效按压时长
    [SerializeField] private float maxValidDuration = 3.0f; // 最大有效按压时长
    
    [Header("=== 实时数据 ===")]
    [SerializeField] private int totalPressCount = 0;
    [SerializeField] private int validPressCount = 0;
    [SerializeField] private float currentDepth = 0f;
    [SerializeField] private float maxRecordedDepth = 0f;
    [SerializeField] private float averageDepth = 0f;
    [SerializeField] private float sessionDuration = 0f;
    
    // 事件系统
    public static event Action<PressData> OnPressCompleted;
    public static event Action<int, int> OnPressCountUpdated; // (total, valid)
    public static event Action<float> OnDepthUpdated;
    public static event Action<PressSession> OnSessionUpdated;
    
    private PressSession currentSession;
    private bool isCurrentlyPressed = false;
    private float currentPressStartTime;
    private float currentPressMaxDepth;
    private List<float> recentDepths = new List<float>();
    
    // 单例模式
    public static PressDataManager Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSession();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        Debug.Log("🎯 PressDataManager 初始化完成");
    }
    
    void Update()
    {
        if (currentSession != null)
        {
            sessionDuration = (float)(DateTime.Now - currentSession.sessionStart).TotalSeconds;
        }
    }
    
    private void InitializeSession()
    {
        currentSession = new PressSession();
        totalPressCount = 0;
        validPressCount = 0;
        maxRecordedDepth = 0f;
        Debug.Log("📊 新的按压会话开始");
    }
    
    public void UpdatePressDepth(float depth)
    {
        currentDepth = depth;
        
        // 检测按压开始
        if (!isCurrentlyPressed && depth > 0.001f)
        {
            StartPress();
        }
        // 检测按压结束
        else if (isCurrentlyPressed && depth < 0.001f)
        {
            EndPress();
        }
        
        // 更新当前按压的最大深度
        if (isCurrentlyPressed && depth > currentPressMaxDepth)
        {
            currentPressMaxDepth = depth;
        }
        
        // 更新全局最大深度
        if (depth > maxRecordedDepth)
        {
            maxRecordedDepth = depth;
        }
        
        // 触发深度更新事件
        OnDepthUpdated?.Invoke(depth);
    }
    
    private void StartPress()
    {
        isCurrentlyPressed = true;
        currentPressStartTime = Time.time;
        currentPressMaxDepth = currentDepth;
        
        Debug.Log("🔽 按压开始");
    }
    
    private void EndPress()
    {
        if (!isCurrentlyPressed) return;
        
        isCurrentlyPressed = false;
        float pressDuration = Time.time - currentPressStartTime;
        
        // 创建按压数据
        bool isValid = IsValidPress(currentPressMaxDepth, pressDuration);
        PressData pressData = new PressData(currentPressMaxDepth, pressDuration, isValid);
        
        // 添加到会话
        currentSession.AddPress(pressData);
        totalPressCount++;
        
        if (isValid)
        {
            validPressCount++;
        }
        
        // 更新平均深度
        UpdateAverageDepth();
        
        // 触发事件
        OnPressCompleted?.Invoke(pressData);
        OnPressCountUpdated?.Invoke(totalPressCount, validPressCount);
        OnSessionUpdated?.Invoke(currentSession);
        
        Debug.Log($"🔼 按压结束 - 深度: {currentPressMaxDepth:F3}m, 时长: {pressDuration:F1}s, 有效: {isValid}");
    }
    
    private bool IsValidPress(float depth, float duration)
    {
        return depth >= minValidDepth && 
               duration >= minValidDuration && 
               duration <= maxValidDuration;
    }
    
    private void UpdateAverageDepth()
    {
        if (currentSession.presses.Count > 0)
        {
            averageDepth = currentSession.averageDepth;
        }
    }
    
    // 公共接口
    public int GetTotalPressCount() => totalPressCount;
    public int GetValidPressCount() => validPressCount;
    public float GetCurrentDepth() => currentDepth;
    public float GetMaxDepth() => maxRecordedDepth;
    public float GetAverageDepth() => averageDepth;
    public float GetSessionDuration() => sessionDuration;
    public PressSession GetCurrentSession() => currentSession;
    
    public void ResetSession()
    {
        InitializeSession();
        Debug.Log("🔄 会话已重置");
    }
    
    public string GetSessionSummary()
    {
        if (currentSession == null) return "无数据";
        
        return $"总按压: {totalPressCount} | 有效: {validPressCount} | " +
               $"平均深度: {averageDepth:F3}m | 最大深度: {maxRecordedDepth:F3}m | " +
               $"会话时长: {sessionDuration:F0}s";
    }
    
    // 保存数据到文件
    public void SaveSessionData()
    {
        try
        {
            string json = JsonUtility.ToJson(currentSession, true);
            string fileName = $"PressSession_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            string filePath = Application.persistentDataPath + "/" + fileName;
            System.IO.File.WriteAllText(filePath, json);
            Debug.Log($"💾 会话数据已保存: {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ 保存数据失败: {e.Message}");
        }
    }
    
    void OnDestroy()
    {
        if (Instance == this)
        {
            SaveSessionData();
        }
    }
    
    // 调试用的Gizmos
    void OnDrawGizmos()
    {
        if (isCurrentlyPressed)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.1f);
        }
    }
}

