/// <summary>
/// CPR训练系统 - 多语言文本库（中文 / 日语 / 英语）
/// 所有界面字符串集中在此处管理，便于后续扩充翻译。
/// </summary>
public static class CPRTextLibrary
{
    // ---------- 通用 ----------
    public static string Welcome =>
        LanguageManager.Get(
            "欢迎使用 CPR 心肺复苏训练系统",
            "CPR 心肺蘇生トレーニングへようこそ",
            "Welcome to CPR Training System");

    public static string PressStart =>
        LanguageManager.Get(
            "请按「开始训练」按钮",
            "「トレーニング開始」ボタンを押してください",
            "Press the Start Training button");

    // ---------- 阶段名称 ----------
    public static string PhaseIntro =>
        LanguageManager.Get("介绍", "紹介", "Introduction");

    public static string PhaseWaiting =>
        LanguageManager.Get("准备中", "準備中", "Ready");

    public static string PhaseFalling =>
        LanguageManager.Get("紧急情况", "緊急事態", "Emergency");

    public static string PhaseReadyCPR =>
        LanguageManager.Get("CPR 准备", "CPR準備", "CPR Ready");

    public static string PhaseCPRInProgress =>
        LanguageManager.Get("CPR 进行中", "CPR実施中", "CPR In Progress");

    public static string PhaseCompleted =>
        LanguageManager.Get("训练完成", "トレーニング完了", "Training Complete");

    // ---------- 指导文本 ----------
    public static string PatientFell =>
        LanguageManager.Get(
            "注意！有人倒下了！",
            "注意！人が倒れました！",
            "Alert! A person has collapsed!");

    public static string FlipPatient =>
        LanguageManager.Get(
            "请将患者翻转为仰卧位\n请触碰患者身体",
            "患者を仰向けにしてください\n患者の体に触れてください",
            "Flip the patient to supine position\nTouch the patient's body");

    public static string Flipping =>
        LanguageManager.Get(
            "正在翻转患者...",
            "患者を仰向けにしています...",
            "Flipping patient...");

    public static string FlipDone =>
        LanguageManager.Get(
            "翻转完成 - 仰卧位",
            "翻転完了 - 仰臥位",
            "Flip complete - Supine position");

    public static string StartCPRNow =>
        LanguageManager.Get(
            "请迅速进行 CPR 按压\n目标：深度 5–6 cm，频率 100–120 次/分",
            "CPRを迅速に実施してください\n目標: 深さ 5–6 cm、テンポ 100–120 回/分",
            "Perform CPR compressions now\nTarget: Depth 5–6 cm, Rate 100–120/min");

    public static string HandStatus_Contact =>
        LanguageManager.Get("手部：检测中", "手: 検出中", "Hand: Detected");

    public static string HandStatus_NoContact =>
        LanguageManager.Get("手部：请将手靠近胸部", "手: 胸に近づけてください", "Hand: Place hand on chest");

    // ---------- 按压实时提示（格式化） ----------
    public static string CPRProgress(int count, int target, float depth, float rate, bool handInContact)
    {
        string handStatus = handInContact ? HandStatus_Contact : HandStatus_NoContact;
        return LanguageManager.Get(
            $"按压次数: {count} / {target}\n实时深度: {depth:F1} cm    实时频率: {rate:F0} 次/分\n参考标准: 深度约 5–6 cm，频率 100–120 次/分\n{handStatus}",
            $"圧迫回数: {count} / {target}\nリアルタイム深度: {depth:F1} cm    テンポ: {rate:F0} 回/分\n目標: 深さ 5–6 cm、テンポ 100–120 回/分\n{handStatus}",
            $"Compressions: {count} / {target}\nDepth: {depth:F1} cm    Rate: {rate:F0}/min\nTarget: Depth 5–6 cm, Rate 100–120/min\n{handStatus}");
    }

    public static string CPRAbortProgress(int count, int target, float depth, float rate)
    {
        return LanguageManager.Get(
            $"按压次数: {count} / {target}\n深度: {depth:F1} cm  频率: {rate:F0} 次/分",
            $"圧迫回数: {count} / {target}\n深さ: {depth:F1} cm  テンポ: {rate:F0} 回/分",
            $"Compressions: {count} / {target}\nDepth: {depth:F1} cm  Rate: {rate:F0}/min");
    }

    // ---------- 完成界面 ----------
    public static string CompletionTitle =>
        LanguageManager.Get(" ", " ", " ");

    public static string ResultTitle =>
        LanguageManager.Get("训练结果", "トレーニング結果", "Training Results");

    public static string LabelCount =>
        LanguageManager.Get("总按压次数", "総圧迫回数", "Total Compressions");

    public static string LabelTime =>
        LanguageManager.Get("所用时间", "所要時間", "Time Taken");

    public static string LabelAvgRate =>
        LanguageManager.Get("平均频率", "平均テンポ", "Average Rate");

    public static string UnitSeconds =>
        LanguageManager.Get("秒", "秒", "sec");

    public static string UnitPerMin =>
        LanguageManager.Get("次/分", "回/分", "/min");

    // ---------- 深度评价 ----------
    public static string DepthNoData =>
        LanguageManager.Get(
            "按压深度：未检测到有效按压",
            "圧迫深度: データなし",
            "Depth: No valid compression detected");

    public static string DepthTooShallow(float min, float max) =>
        LanguageManager.Get(
            $"按压深度：偏浅（目标 {min:F0}–{max:F0} cm）",
            $"圧迫深度: 浅い（目標 {min:F0}–{max:F0} cm）",
            $"Depth: Too shallow (target {min:F0}–{max:F0} cm)");

    public static string DepthTooDeep(float min, float max) =>
        LanguageManager.Get(
            $"按压深度：偏深（目标 {min:F0}–{max:F0} cm）",
            $"圧迫深度: 深すぎる（目標 {min:F0}–{max:F0} cm）",
            $"Depth: Too deep (target {min:F0}–{max:F0} cm)");

    public static string DepthGood(float avg) =>
        LanguageManager.Get(
            $"按压深度：良好（平均 {avg:F1} cm）",
            $"圧迫深度: 良好（平均 {avg:F1} cm）",
            $"Depth: Good (avg {avg:F1} cm)");

    // ---------- 频率评价 ----------
    public static string RateNoData =>
        LanguageManager.Get(
            "按压频率：未检测到有效按压",
            "圧迫テンポ: データなし",
            "Rate: No valid compression detected");

    public static string RateTooSlow(float min, float max) =>
        LanguageManager.Get(
            $"按压频率：偏慢（目标 {min:F0}–{max:F0} 次/分）",
            $"圧迫テンポ: 遅い（目標 {min:F0}–{max:F0} 回/分）",
            $"Rate: Too slow (target {min:F0}–{max:F0}/min)");

    public static string RateTooFast(float min, float max) =>
        LanguageManager.Get(
            $"按压频率：偏快（目标 {min:F0}–{max:F0} 次/分）",
            $"圧迫テンポ: 速すぎる（目標 {min:F0}–{max:F0} 回/分）",
            $"Rate: Too fast (target {min:F0}–{max:F0}/min)");

    public static string RateGood(float avg) =>
        LanguageManager.Get(
            $"按压频率：良好（平均 {avg:F0} 次/分）",
            $"圧迫テンポ: 良好（平均 {avg:F0} 回/分）",
            $"Rate: Good (avg {avg:F0}/min)");

    // ---------- 按钮文字 ----------
    public static string BtnStartTraining =>
        LanguageManager.Get("开始训练", "トレーニング開始", "Start Training");

    /// <summary>「CPR开始」按钮</summary>
    public static string BtnStartScenario =>
        LanguageManager.Get("CPR开始", "CPR開始", "Start CPR");

    /// <summary>「准备就绪」状态提示</summary>
    public static string LabelReady =>
        LanguageManager.Get("准备就绪", "準備就緒", "Ready");

    public static string BtnRestart =>
        LanguageManager.Get("再来一次", "もう一度", "Try Again");

    public static string BtnExit =>
        LanguageManager.Get("退出训练", "終了", "Exit");

    public static string BtnQuit =>
        LanguageManager.Get("退出", "終了", "Quit");

    public static string BtnTutorial =>
        LanguageManager.Get("训练教程", "トレーニング教程", "Tutorial");

    public static string BtnBack =>
        LanguageManager.Get("返回", "戻る", "Back");

    // ---------- 主菜单 ----------
    public static string MainTitle =>
        LanguageManager.Get(
            "CPR 心肺复苏训练",
            "CPR 心肺蘇生トレーニング",
            "CPR Training System");

    public static string SubTitle =>
        LanguageManager.Get(
            "Quest 3 + 手部追踪",
            "Quest 3 + ハンドトラッキング",
            "Quest 3 + Hand Tracking");

    // ---------- 语言选择按钮标签（固定，不随语言变化） ----------
    public const string LangBtnChinese  = "中文";
    public const string LangBtnJapanese = "日本語";
    public const string LangBtnEnglish  = "English";

    // ---------- 完成结果完整文本 ----------
    public static string BuildResultText(int count, float totalTime, float avgDepth, float avgRate,
                                         string depthEval, string rateEval)
    {
        return LanguageManager.Get(
            $" \n\n总按压次数: {count}\n所用时间: {totalTime:F1} 秒\n平均频率: {avgRate:F0} 次/分\n\n{depthEval}\n{rateEval}",
            $" \n\n総圧迫回数: {count}\n所要時間: {totalTime:F1} 秒\n平均テンポ: {avgRate:F0} 回/分\n\n{depthEval}\n{rateEval}",
            $" \n\nTotal Compressions: {count}\nTime: {totalTime:F1} sec\nAvg Rate: {avgRate:F0}/min\n\n{depthEval}\n{rateEval}");
    }
}
