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
    /// <summary>
    /// 按压实时提示。这个方法在 CPRTrainingController.Update() 里每帧调用，
    /// 所以不能用「三个实参都插值、Get 只留一个」的写法 ——
    /// LanguageManager.Get 是普通方法，那样每帧会构建 3 个约 150 字符的长串、丢掉 2 个。
    /// 先取格式串（纯字面量，不分配）再格式化一次。
    /// </summary>
    public static string CPRProgress(int count, int target, float depth, float rate, bool handInContact)
    {
        string handStatus = handInContact ? HandStatus_Contact : HandStatus_NoContact;
        return string.Format(
            LanguageManager.Get(
                "按压次数: {0} / {1}\n实时深度: {2:F1} cm    实时频率: {3:F0} 次/分\n参考标准: 深度约 5–6 cm，频率 100–120 次/分\n{4}",
                "圧迫回数: {0} / {1}\nリアルタイム深度: {2:F1} cm    テンポ: {3:F0} 回/分\n目標: 深さ 5–6 cm、テンポ 100–120 回/分\n{4}",
                "Compressions: {0} / {1}\nDepth: {2:F1} cm    Rate: {3:F0}/min\nTarget: Depth 5–6 cm, Rate 100–120/min\n{4}"),
            count, target, depth, rate, handStatus);
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

    /// <summary>
    /// 研究モードで被験者に見せる結果欄。成績の数字はいっさい出さない。
    /// 採点も CSV への記録も通常どおり行われていて、表示だけを差し替えている。
    /// </summary>
    public static string ResultHiddenForParticipant =>
        LanguageManager.Get(
            "体验结束。\n请摘下头显，然后填写问卷。",
            "体験は終了です。\nヘッドセットを外して、アンケートにお進みください。",
            "Session complete.\nPlease remove the headset and continue to the questionnaire.");

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

    // ---------- 操作说明 ----------
    public static string ControlsGuideTitle =>
        LanguageManager.Get("操作方法", "操作方法", "How to Use");

    /// <summary>
    /// 开场操作说明正文。手部追踪和手柄两条输入路径在代码里是并存的
    /// （VRUIPointer 每帧按 OVRInput.IsControllerConnected 自动切换射线起点，
    /// CPRPressDetector 的 IsHandCollider 同时接受手骨和 HandAnchor），
    /// 但之前没有任何界面告诉用户这件事。
    /// </summary>
    public static string ControlsGuideBody =>
        LanguageManager.Get(
            "■ 菜单操作（手势 / 手柄都可以）\n" +
            "  ・手势：用食指直接按按钮，或捏合拇指和食指点击射线指向处\n" +
            "  ・手柄：按右手扳机键\n" +
            "  ・放下手柄会自动切回手势，无需重启\n" +
            "\n" +
            "■ 胸外按压\n" +
            "  ・把掌根放在患者胸部中央的检测区，垂直向下按\n" +
            "  ・建议用手势按压：手柄的握持点和掌根有偏差，深度与回弹判定会不准\n" +
            "  ・目标：深度 5–6 cm，频率 100–120 次/分，每次压完让胸部完全回弹",

            "■ メニュー操作（ハンド / コントローラ どちらでも可）\n" +
            "  ・ハンド：人差し指でボタンを直接押す、または親指と人差し指でつまむ（ピンチ）\n" +
            "  ・コントローラ：右手の人差し指トリガー\n" +
            "  ・コントローラを置くと自動的にハンドに切り替わります（再起動不要）\n" +
            "\n" +
            "■ 胸骨圧迫\n" +
            "  ・手のひらの付け根を患者の胸の中央（検出エリア）に当て、まっすぐ押し下げます\n" +
            "  ・圧迫はハンドトラッキング推奨：コントローラでは握り位置がずれ、深さとリコイルの判定が不正確になります\n" +
            "  ・目標：深さ 5〜6 cm、テンポ 100〜120 回/分、1回ごとに胸を完全に戻す",

            "■ Menus (hands or controller - both work)\n" +
            "  - Hands: poke a button with your index finger, or pinch thumb + index to click\n" +
            "  - Controller: right index trigger\n" +
            "  - Put the controller down and it switches back to hands automatically (no restart)\n" +
            "\n" +
            "■ Chest compressions\n" +
            "  - Place the heel of your palm on the centre of the chest and press straight down\n" +
            "  - Hand tracking is recommended: a held controller offsets depth and recoil readings\n" +
            "  - Target: 5-6 cm deep, 100-120/min, let the chest recoil fully each time");

    public static string BtnControlsGuide =>
        LanguageManager.Get("操作说明", "操作方法", "How to Use");

    public static string BtnGotIt =>
        LanguageManager.Get("知道了", "確認", "Got it");

    // ---------- 人工呼吸（30:2） ----------
    public static string BtnGiveBreath =>
        LanguageManager.Get("完成一次吹气", "1 回送気した", "Breath given");

    /// <summary>
    /// 通气阶段的提示。每帧调用（要刷新已停顿秒数），所以先取格式串再格式化一次，
    /// 不能让 LanguageManager.Get 的三个实参各插值一遍。
    /// </summary>
    public static string VentilationPrompt(int perCycle, int breaths, int remaining, float elapsedSec)
    {
        return string.Format(
            LanguageManager.Get(
                "已完成 {0} 次按压 —— 现在做 {1} 次人工呼吸\n" +
                "・仰头抬颏打开气道，每次吹气约 1 秒，见到胸廓抬起即可\n" +
                "・还需 {2} 次\n" +
                "・已中断 {3:F1} 秒（指南要求 10 秒以内，然后立刻恢复按压）",

                "{0} 回の圧迫が完了 —— 人工呼吸を {1} 回行ってください\n" +
                "・頭部後屈あご先挙上で気道を確保し、1 回約 1 秒、胸の上がりが見える程度に\n" +
                "・あと {2} 回\n" +
                "・中断 {3:F1} 秒（ガイドラインは 10 秒未満。終わったらすぐ圧迫を再開）",

                "{0} compressions done - now give {1} rescue breaths\n" +
                "- Head tilt / chin lift, about 1 second per breath, just enough for chest rise\n" +
                "- {2} to go\n" +
                "- Paused {3:F1} s (guidelines say under 10 s, then resume compressions immediately)"),
            perCycle, breaths, remaining, elapsedSec);
    }

    // ---------- 意识确认（反応の確認） ----------
    public static string ResponseCheckTitle =>
        LanguageManager.Get("确认意识", "反応の確認", "Check for a Response");

    public static string BtnSkipResponseCheck =>
        LanguageManager.Get("跳过这一步", "このステップを飛ばす", "Skip this step");

    /// <summary>
    /// 意识确认阶段的提示。每帧调用（要刷新肩部进度和秒数），
    /// 所以先取格式串再格式化一次，不能让 LanguageManager.Get 的实参各插值一遍。
    /// </summary>
    public static string ResponseCheckPrompt(string leftMark, string rightMark, string voiceLine, float elapsedSec)
    {
        return string.Format(
            LanguageManager.Get(
                "轻拍患者双肩并大声呼唤「你没事吧？」\n" +
                "・左肩 {0}　　右肩 {1}\n" +
                "{2}" +
                "・已用时 {3:F1} 秒（指南要求迅速判断，不超过 10 秒）",

                "患者の両肩を軽くたたき、大声で「大丈夫ですか？」と呼びかけてください\n" +
                "・左肩 {0}　　右肩 {1}\n" +
                "{2}" +
                "・経過 {3:F1} 秒（ガイドラインは 10 秒以内での迅速な判断）",

                "Tap both shoulders and shout \"Are you all right?\"\n" +
                "- Left {0}   Right {1}\n" +
                "{2}" +
                "- {3:F1} s elapsed (guidelines call for a check within 10 s)"),
            leftMark, rightMark, voiceLine, elapsedSec);
    }

    /// <summary>呼唤检测的那一行。麦克风不可用时返回空串，整行不显示。</summary>
    public static string ResponseCheckVoiceLine(bool micAvailable, bool heard)
    {
        if (!micAvailable)
            return string.Empty;

        return heard
            ? LanguageManager.Get("・已听到呼唤 ✓\n", "・呼びかけを確認 ✓\n", "- Call heard ✓\n")
            : LanguageManager.Get("・还没听到呼唤\n", "・呼びかけがまだ聞こえません\n", "- No call heard yet\n");
    }

    public static string ResponseCheckMarkDone =>
        LanguageManager.Get("✓", "✓", "✓");

    public static string ResponseCheckMarkPending =>
        LanguageManager.Get("—", "—", "—");

    // ---------- 患者放置 ----------
    public static string BtnPlacePatient =>
        LanguageManager.Get("调整患者位置", "患者の位置を調整", "Move Patient");

    public static string PlacementTitle =>
        LanguageManager.Get("选择放置位置", "設置場所を選択", "Choose a Spot");

    public static string PlacementBody =>
        LanguageManager.Get(
            "用手（或手柄）指向地面上的一块空地，绿色轮廓就是患者躺下后的占地范围。\n" +
            "・请选一块真实房间里没有家具的地面 —— 否则按压时会碰到桌椅\n" +
            "・右摇杆左右拨可以旋转朝向\n" +
            "・轮廓变红表示超出可用范围\n" +
            "选好后按「确定」。",

            "手（またはコントローラ）で床の空いている場所を指してください。緑の枠が患者の占有範囲です。\n" +
            "・実際の部屋で家具のない床を選んでください（圧迫時に机や椅子に当たるのを防ぐため）\n" +
            "・右スティックを左右に倒すと向きを回転できます\n" +
            "・枠が赤い場合は範囲外です\n" +
            "決まったら「確定」を押してください。",

            "Point at an empty patch of floor with your hand (or controller). The green outline is the space the patient will take up.\n" +
            "- Pick floor that is clear of real furniture, or you will hit it while compressing\n" +
            "- Push the right thumbstick left/right to rotate\n" +
            "- A red outline means the spot is out of range\n" +
            "Press Confirm when you are happy with it.");

    public static string BtnConfirmPlacement =>
        LanguageManager.Get("确定", "確定", "Confirm");

    public static string BtnCancelPlacement =>
        LanguageManager.Get("取消", "キャンセル", "Cancel");

    // ---------- 地震场景世界UI ----------
    // 这块白板是整个训练的入口：按下去 → 地震 → 有人倒地 → 自动进入急救流程。
    // 原来标签只写「地震開始」，学员看不出这是训练的开始按钮，
    // 实机反馈直接问「这个地震按钮是干什么的」。标签和说明一起改成讲清楚用途的写法。
    public static string BtnStartEarthquake =>
        LanguageManager.Get("开始训练（地震情景）", "訓練開始（地震想定）", "Start Drill (Earthquake)");

    public static string EarthquakeReady =>
        LanguageManager.Get(
            "训练情景：地震发生后现场急救\n按下按钮后会发生地震，随后出现伤者\n请对倒地者实施心肺复苏",
            "訓練シナリオ：地震発生後の救命処置\nボタンを押すと地震が起こり、負傷者が発生します\n倒れた人に心肺蘇生を行ってください",
            "Scenario: first aid after an earthquake\nPressing the button triggers a quake, then a casualty appears\nPerform CPR on the collapsed person");

    public static string EarthquakeShaking =>
        LanguageManager.Get(
            "地震发生中\n请压低身体，注意周围坠落物",
            "地震発生中\n姿勢を低く保ち、周囲の危険物に注意",
            "Earthquake in progress\nStay low and watch for falling objects");

    public static string EarthquakeFinished =>
        LanguageManager.Get(
            "地震结束\n发现伤者，开始急救处置",
            "地震終了\n負傷者を発見、救命処置を開始します",
            "Earthquake over\nA casualty is found - begin first aid");

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
