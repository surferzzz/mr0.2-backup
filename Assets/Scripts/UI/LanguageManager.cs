using UnityEngine;
using System;

/// <summary>
/// 全局语言管理器 - 单例，跨场景持久化
/// </summary>
public enum Language
{
    Chinese  = 0,
    Japanese = 1,
    English  = 2
}

public class LanguageManager : MonoBehaviour
{
    public static LanguageManager Instance { get; private set; }

    public Language CurrentLanguage { get; private set; } = Language.Japanese;

    public event Action OnLanguageChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 读取上次选择的语言（默认日语）
        CurrentLanguage = (Language)PlayerPrefs.GetInt("CPR_Language", (int)Language.Japanese);
    }

    public void SetLanguage(Language language)
    {
        if (CurrentLanguage == language) return;
        CurrentLanguage = language;
        PlayerPrefs.SetInt("CPR_Language", (int)language);
        PlayerPrefs.Save();
        OnLanguageChanged?.Invoke();
        Debug.Log($"[LanguageManager] 语言切换为: {language}");
    }

    // Inspector / Button.OnClick() 直接调用
    public void SetChinese()  => SetLanguage(Language.Chinese);
    public void SetJapanese() => SetLanguage(Language.Japanese);
    public void SetEnglish()  => SetLanguage(Language.English);

    /// <summary>返回三语言之一的字符串（快捷方法）</summary>
    public static string Get(string zh, string ja, string en)
    {
        var mgr = Instance;
        if (mgr == null)
        {
            mgr = UnityEngine.Object.FindObjectOfType<LanguageManager>();
            if (mgr != null) Instance = mgr;
        }
        if (mgr == null) return ja;
        return mgr.CurrentLanguage switch
        {
            Language.Chinese  => zh,
            Language.Japanese => ja,
            Language.English  => en,
            _                 => ja
        };
    }
}
