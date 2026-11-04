# 客観指標（装置側ログ）設計

**问卷之外的另一半：设备自动测量的客观数据**

对照文档：[questionnaire_visual_vs_haptic.md](questionnaire_visual_vs_haptic.md)（主观问卷）

---

## 实装状态（2026-11-02）

| 缺口 | 状态 | 实装 |
|---|---|---|
| 1 数据没落盘 | **已解决** | `Assets/Scripts/Research/TrainingDataLogger.cs` 写两张 CSV |
| 2 没有逐次明细 | **已解决** | `compressions.csv`，并由此算出 `depth_sd_cm` / `rate_sd_bpm` |
| 3 没有被试标识 | **已解决** | `Assets/Scripts/Research/ResearchSession.cs`（PlayerPrefs + `session_config.json`） |
| 4 AED 只有总耗时 | **已解决** | `AEDTrainingRuntime.ResultTimeToPadsSeconds` / `ResultTimeToShockSeconds` |
| 5 中断阈值要声明 | 未改 | 阈值已写进 CSV 的 `interruption_threshold_s` 列，可追溯；常量本身仍是 `3f` |

### 复查后补的五处（2026-11-02 第二轮）

上一轮把采集代码写完了，但有两处会让数据静默失真 —— 画面一切正常，错误只在分析时才显形。

| 问题 | 状态 | 实装 |
|---|---|---|
| A 记录器没挂进场景，**一行都不会落盘** | **已解决** | `TrainingDataLogger` 改为 `RuntimeInitializeOnLoadMethod` 自启动，不再依赖「记得挂」 |
| B 意识确认的数据在写入前被清零 | **已解决** | `ResetSessionMetrics` 不再碰这三个字段，另设 `ResetResponseCheckMetrics()` |
| C `overall_score` 没进 CSV | **已解决** | 新增列；中断回合留空而不是 0 |
| D 中途退出 / 离场不写行 | **已解决** | `FlushUnfinishedSession()` + 新增 `completed` 列 |
| E 实验当天看不到 ID / 条件 | **已解决** | `Assets/Scripts/Research/ResearchHudLabel.cs`，视野左上角常驻 |

**A 的细节**：`TrainingDataLogger` 的 GUID 在全项目所有 `.unity` / `.prefab` 里零命中，
类上也没有自启动入口。结果面板照常显示成绩，所以在设备上看不出任何异常。
现在和 `AEDTrainingRuntime` 用同一套自举方式（`sceneLoaded` 里重试，因为启动场景是
没有训练控制器的 `Scenes/MainMenu.unity`）。

**B 的细节**：`WaitForResponseCheck()` 在按压阶段**之前**跑完并写入
`_responseCheckSeconds` 等三个字段，但随后的 `StartCPRPhase()` → `ResetSessionMetrics()`
把它们归零。三条路径（Story / AED / 纯 CPR）全部中招，
CSV 的 `response_check_s` / `response_voice_heard` / `response_skipped` 恒为 `0.00 / 0 / 0`，
结果面板那一行也永远显示 0.0 秒。

**D 的细节**：中断本身就是所见（嫌疼停下、手部追踪失灵），
丢掉的话分析侧只会看到「这名被试没有体验过」。现在中断回合也留一行，`completed=0`，
`overall_score` 留空。入口有三个：中途退出按钮（`AbortCurrentTraining`）、
重新开始（`RestartTrainingInternal`）、应用退出（`OnApplicationQuit`，`ExitToMainMenu` 也走这里）。
`trainingStartTime` 兼作哨兵，不会重复写。

### 第三轮（2026-11-02）：输入面板与成绩的可见性

| 问题 | 状态 | 实装 |
|---|---|---|
| F 实机上改 ID 只能靠 adb | **已解决** | `Assets/Scripts/Research/ResearchSetupPanel.cs`，启动时先出设置画面 |
| G 被试在填问卷前看到成绩 | **已解决** | `ResearchSession.HideScoreFromParticipant`，结算页换成中性文案 |

**F 的细节**：原先唯一的办法是 `adb push session_config.json`。戴着头显改不了，
所以「前一个被试的设定原样记了下一场」这件事，实验者没有任何手段挡住。
现在主菜单之前先出一块设置画面：数字键盘输编号、A/B 选组别、1/2/3 选轮次，
确认之后才放行菜单（确认前主菜单的 Canvas 整个 `SetActive(false)` —— 只关 `Canvas`
组件不够，`HandPokeButtonInteractor` 是从「启用的 GameObject」上找 `Button` 的，
看不见也照样能按到）。初始值显示的是**当前设定**而不是空白，因为要让人在这里
认出「这还是上一场的编号」。编号为空时 `START` 不可按。
`session_config.json` 仍然有效，适合批量预设。

**G 的细节**：成绩照常计算、照常写进 `sessions.csv`，只是不显示给被试，
结算页换成「体験は終了です。ヘッドセットを外して、アンケートにお進みください。」。
理由是紧接着要填的自我效能感和真实感量表会被分数带着走 —— 想测的是体验本身的效果，
不是「被告知 80 分之后的心情」。**压迫过程中的实时反馈不动**，那是实验操作本身。
设置画面上有 `SCORE: HIDE / SHOW` 开关，当教学用时切回 `SHOW`；
`session_config.json` 里也可以写 `"hideScore": "0"`。默认是隐藏。

未做：知识测验（`KnowledgeQuizView`）结束时仍然显示对错与得分，这次没有动。

### 第四轮（2026-11-04）：自变量本身没有实装

| 问题 | 状态 | 实装 |
|---|---|---|
| H 面板输入的值被旧的 `session_config.json` 覆盖 | **已解决** | `ResearchSession.MarkConfiguredInThisLaunch()` |
| I **组别 A/B 不驱动任何代码，两组跑的程序一样** | **已解决** | `GloveHapticController.HapticsAllowed`，并新增两列记录实际状态 |
| J CCF 把最后一次通气停顿算了两次 | **已解决** | `EndVentilation` 里 `_lastCompressionAtTime` 挪到完成判定之前 |

**I 的细节（最要紧的一条）**：`ResearchSession.Condition` 在 Research 命名空间之外
只被读过一次，而且是用来决定隐不隐藏成绩的。它从来没有被用来开关手套震动。
`CPRTrainingController` 反过来还会在场景里找不到 `GloveHapticController` 时**自动建一个**。
也就是说 A 组和 B 组跑的是完全相同的程序，`condition` 列只是一个标签，
不对应任何实际差异 —— 被试间设计的自变量没有实装。

现在 `GloveHapticController.HapticsAllowed` 直接读 `ResearchSession.Condition`，
三个出口（`TryPlayHapticEvent` / `PlayBothGloves` / `TriggerShockFeedback`）各有一道关，
项目里全部 5 处 `BhapticsLibrary.PlayMotors` / `Play` 调用都在关后面。
故意不做成「由外部传入的开关」：那样只要有一处忘了设，A 和 B 又会变成同一个东西。

还有第二条触觉通路：`Assets/Scripts/CPR/Core/GloveHapticFeedback.cs`。它的 GUID 在场景和
预制体里零命中，只有 4 个编辑器向导（`OneClickFix` / `AddTrainingSetup` /
`SimpleXBotSetup` / `XBotSetup`）会 `AddComponent` 它。实验前谁跑一次向导，
场景里就会多一个不受组别控制的震动源。所以同样的关也加在了它的两个播放入口上。

**连带的行为变化**：`Condition` 的默认值是 A，所以装上新版之后，
不在设置画面上选 B 就**完全没有手套震动**。当教学用时记得选 B。

**新增两列**，用来事后验证条件是否真的成立：

| 列 | 来源 | 怎么用 |
|---|---|---|
| `haptic_enabled` | `GloveHapticController.HapticsAllowed` | 软件这一侧是否放行。应当和 `condition` 完全一致，不一致说明有 bug |
| `haptic_pulses` | `GloveHapticController.PulsesThisSession` | 这一场实际往手套发了多少次。**B 组为 0 就是手套没在动**，那一场不能当「触觉有」来分析 |

解析前的筛查因此多一条：`condition=B` 且 `haptic_pulses=0` 的行要剔除或单独处理。

**H 的细节**：`TrainingDataLogger.Awake()` 读 `session_config.json`，而记录器要等训练场景
加载后才诞生，设置面板却在主菜单里就写完了 PlayerPrefs。顺序是
「面板确认 → 场景切换 → 记录器读 JSON → 覆盖」，设备上只要残留过一个旧 JSON，
面板就白填了，而且是静默的 —— 恰恰是面板要防的那类事故。
现在面板确认时打一个**只存在于本次启动**的标记，有标记就不读文件；
文件退化成「没人手输时的兜底」，批量预设仍然可用。

**J 的细节**：`EndVentilation` 先把通气停顿计进 `_totalHandsOffSeconds`，
然后在最后一个循环里 `CompleteTraining(); return;` —— 把后面那行
`_lastCompressionAtTime = Time.time;` 跳过去了。于是
`CalculateChestCompressionFraction` 算出的 `trailing` 仍然是「最后一次按压到现在」，
正好又把这段停顿包进去，和已经计入的那份一起被扣两次。
2 轮 × 30 次、整场约 75 秒的话，CCF 系统性偏低约 7–10 个百分点。
对两组是同等偏差，不会制造假的组间差异，但报出来的绝对值是错的，
而指南的 60% 是个绝对阈值。把赋值挪到完成判定之前就好了。

---

## 1. 现状：代码里已经在算什么

好消息是，**AHA/JRC 要求的按压质量指标基本都已经在测了**，而且精度到每一次按压。

### CPRPressDetector.cs — 逐次按压的原始测量

| 字段 / 事件 | 含义 | 判定阈值（Inspector 可改） |
|---|---|---|
| `CurrentDepthCm` | 当前按压深度 | — |
| `CurrentRateBPM` | 当前按压频率 | — |
| `CompressionCount` | 累计按压次数 | — |
| `DepthStatus` | TooShallow / Good / TooDeep | `minGoodDepthCm 5` 〜 `maxGoodDepthCm 6` |
| `RateStatus` | TooSlow / Good / TooFast | `minGoodRateBPM 100` 〜 `maxGoodRateBPM 120` |
| `LastRecoilDeficitCm` / `LastRecoilIncomplete` | 回弹不足量（残留下压） | `recoilToleranceCm 1.0` |
| `LastOffsetCm` / `LastOffTarget` | 手掌偏离胸骨中点的距离 | `positionToleranceCm 4` |
| `AbsoluteDepthCm` | 未平滑的绝对深度 | — |
| `OnCompressionComplete(depth, rate)` | **每次按压结束时触发** | `minCompressionInterval 0.2` |
| `OnRecoilEvaluated(deficit, incomplete)` | 每次回弹评价 | — |
| `OnPositionEvaluated(offset, offTarget)` | 每次位置评价 | — |

### CPRTrainingController.cs — 整场汇总

| 变量 | 含义 |
|---|---|
| `_compressionSamples` / `_rateSamples` | 有效样本数 |
| `_sumDepthCm` / `_sumRateBpm` | 深度・频率合计（÷样本数 = 平均） |
| `_goodDepthSamples` / `_goodRateSamples` / `_goodCombinedSamples` | 深度・频率・两者同时合格的次数 |
| `_tooShallowSamples` / `_tooDeepSamples` / `_tooSlowSamples` / `_tooFastSamples` | 各类错误次数 |
| `_fullRecoilSamples` / `_sumRecoilDeficitCm` | 完全回弹次数・平均残留 |
| `_onTargetSamples` / `_sumOffsetCm` | 位置合格次数・平均偏移 |
| `_totalHandsOffSeconds` / `_longestHandsOffSeconds` / `_interruptionCount` | 手离胸总时长・最长中断・中断次数（阈值 `InterruptionThresholdSec = 3f`） |
| CCF | 胸骨压迫比，由上面的 hands-off 算出 |
| `_currentQualityStreak` / `_bestQualityStreak` | 连续合格次数・最佳连续 |
| `_ventilationCyclesDone` / `_totalBreathsGiven` / `_longestVentilationPauseSec` | 人工呼吸周期・次数・最长中断 |
| `_responseCheckSeconds` / `_responseCheckVoiceHeard` / `_responseCheckSkipped` | 反应确认耗时・是否出声・是否跳过 |
| `overallScore` | 加权总分（深度 25 / 频率 20 / 回弹 15 / 位置 15 / CCF 10 / 一致性 5 / 次数 10；含 AED 时重新加权） |

### AEDTrainingRuntime.cs — AED 段

| 字段 | 含义 |
|---|---|
| `ResultPadsPlaced` | 电极片是否贴好 |
| `ResultShockAdvised` | 是否建议电击 |
| `ResultShockDelivered` | 是否实际放电 |
| `ResultDurationSeconds` | **AED 段总耗时** |
| `ResultRhythm` | 模拟心律 |

### KnowledgeQuizView.cs — 知识测验

`QuizResult { correct, total, wrongTopics, suggestedPractice }`

---

## 2. 缺口：五个必须补的洞

### 缺口 1：一条都没存下来（最致命）

全部指标只拼成一个字符串喂给 `completionResultText` 显示在完成面板上，**退出场景即丢失**。
全项目没有任何 `File.Write` / `StreamWriter` / 上传代码（`PlayerPrefs` 只用来存语言和训练模式）。
现在的状态是：**数据在算，但没人收**。

### 缺口 2：只有汇总，没有逐次明细

`OnCompressionComplete` 每次都触发，但只被累加进 `_sumDepthCm` 这类合计变量，
**单次按压的时间戳、深度、频率、回弹、偏移没有保存**。

这件事对本研究特别要命。触觉反馈的效应，经验上往往**不体现在平均值上，而体现在**：

- **变异度下降**（深度 SD 变小 = 按得更稳）
- **收敛更快**（学习曲线：第几次按压开始稳定进入 5–6 cm）
- **纠错更快**（压浅之后，下一次是否立刻修正回来）

只有汇总值的话，这三个全都算不出来，很可能得到「平均深度 A 5.2 cm vs B 5.3 cm，n.s.」这种
什么都没说明的结果，而真实差异被平均掉了。**逐次明细是本实验的主要数据，不是附加项。**

### 缺口 3：没有被试・条件・轮次的标识

没有 participant ID、条件 A/B、第几轮的字段。不加这三个，数据无法和问卷对齐。

### 缺口 4：AED 只有总耗时

`ResultDurationSeconds` 是整段时长（`Time.time - scenarioStartTime`）。CPR 研究里 AED 的标准终点是
**time-to-first-shock**（从开始到首次放电）和 **time-to-pads**（到贴好电极片），
以及放电前后的 hands-off 时间。现在这三个都没有分段计时。

### 缺口 5：中断阈值的定义要写进论文

`InterruptionThresholdSec = 3f` —— 超过 3 秒才算一次「中断」。
指南里常见的是 >5 s 或 >10 s。这不是 bug，但**是一个要在方法部分明确声明的参数**，
否则「中断次数」没法和别的研究比较。建议改成 Inspector 可配置，并把实际取值写进日志。

---

## 3. 测量效度：必须在论文里写清楚的事

**本系统的「深度」是手部位置推算值，不是力学传感值。**
`CPRPressDetector` 用 `handPalm` 与 `chestSurface` 两个 Transform 的相对位移算深度
（`useChestAxis` 时沿胸廓法线投影），并带动态静止位校准（`useDynamicRestCalibration`）。

所以：

- 它测的是**手移动了多少**，不是**胸廓被压下去多少**。被试手悬空按压也会被记成深度。
- 绝对值依赖校准质量，**跨被试的绝对深度比较要谨慎**。
- **被験者間设计下这一条更要紧**：A 群和 B 群是不同的人，校准误差不会被组内差分抵消，
  它直接进了组间误差。好处是校准误差与分组无关（随机分配），不会造成系统性偏倚，
  只是让检验更保守；代价是在各群 25 人的条件下进一步吃掉本就不多的检出力。
- 缓解办法：每场开始前用同一套流程做校准，并把校准时的静止位记进日志，
  分析时作为协变量或异常值筛选的依据。

论文里按 "simulated compression depth derived from hand tracking" 来描述，
不要写成 "compression depth" 让人误以为是带力学传感器的训练模型。
如果审稿要求效度证据，最省事的做法是：拿一台带反馈的训练模型做 10〜20 次同步测量，
报告一条相关系数。

---

## 4. 建议的输出：两张表

### 表 1 `sessions.csv` — 每次体验一行（和问卷 1:1 对齐）

| 列名 | 来源 | 说明 |
|---|---|---|
| `timestamp` | `DateTime.Now` ISO8601 | |
| `participant_id` | 会话开始时输入 | **和问卷的 P1 用同一个值** |
| `condition` | 会话开始时选择 | `A`（画面のみ）/ `B`（画面＋振动） |
| `haptic_enabled` | `GloveHapticController.HapticsAllowed` | 软件侧是否放行震动。应与 `condition` 一致 |
| `haptic_pulses` | `GloveHapticController.PulsesThisSession` | 实际发往手套的次数。**B 组为 0＝手套没动**，该场不成立 |
| `trial_index` | 会话开始时选择 | 被験者間なので通常 1。再実施したときだけ 2 以上 |
| `app_version` | `Application.version` | 中途改过参数时能追溯 |
| `completed` | `SessionCompletedNormally` | `1` 规定次数走完，`0` 中途退出。分析前先按这一列筛 |
| `guideline_depth_min` / `guideline_depth_max` | `minGoodDepthCm` / `maxGoodDepthCm` | 判定阈值一起存，后期才能复现 |
| `guideline_rate_min` / `guideline_rate_max` | `minGoodRateBPM` / `maxGoodRateBPM` | 同上 |
| `interruption_threshold_s` | `InterruptionThresholdSec` | 同上（缺口 5） |
| `compression_count` | `_compressionSamples` | |
| `depth_mean_cm` / `depth_sd_cm` | 逐次明细算出 | **SD 是新增，现在算不出来** |
| `depth_in_range_pct` | `_goodDepthSamples / _compressionSamples` | 主要终点候选 |
| `too_shallow_n` / `too_deep_n` | 既有 | |
| `rate_mean_bpm` / `rate_sd_bpm` | 逐次明细算出 | |
| `rate_in_range_pct` | `_goodRateSamples / _rateSamples` | 主要终点候选 |
| `too_slow_n` / `too_fast_n` | 既有 | |
| `both_in_range_pct` | `_goodCombinedSamples / _rateSamples` | |
| `full_recoil_pct` / `recoil_deficit_mean_cm` | 既有 | |
| `on_target_pct` / `offset_mean_cm` | 既有 | |
| `ccf_pct` | 既有 | |
| `hands_off_total_s` / `longest_interruption_s` / `interruption_count` | 既有 | |
| `best_streak` | `_bestQualityStreak` | |
| `response_check_s` / `response_voice_heard` / `response_skipped` | 既有 | |
| `ventilation_cycles` / `breaths_total` / `longest_vent_pause_s` | 既有 | |
| `aed_pads_placed` / `aed_shock_advised` / `aed_shock_delivered` / `aed_rhythm` | 既有 | |
| `aed_total_s` | `ResultDurationSeconds` | |
| `aed_time_to_pads_s` / `aed_time_to_shock_s` | **要新增**（缺口 4） | |
| `quiz_correct` / `quiz_total` / `quiz_wrong_topics` | `QuizResult` | |
| `overall_score` | `LastOverallScore` | 自家算法，**不要当主要终点**，当描述统计用。中断回合为空 |
| `session_duration_s` | 既有 | |

### 表 2 `compressions.csv` — 每次按压一行（学习曲线・变异度分析用）

| 列名 | 来源 |
|---|---|
| `participant_id` / `condition` / `trial_index` | 和表 1 对齐 |
| `seq` | 第几次按压（1, 2, 3…） |
| `t_since_cpr_start_s` | 相对 CPR 开始的时刻 |
| `depth_cm` | `OnCompressionComplete` 第 1 参数 |
| `rate_bpm` | `OnCompressionComplete` 第 2 参数 |
| `recoil_deficit_cm` / `recoil_incomplete` | `OnRecoilEvaluated` |
| `offset_cm` / `off_target` | `OnPositionEvaluated` |
| `depth_ok` / `rate_ok` | 按当场阈值判定 |

一场 100 次按压 × 每人 2 条件 × 30 人 = 6000 行，CSV 完全扛得住。

---

## 5. 落地方案

### 5.1 一个新组件 `TrainingDataLogger`

**不需要挂在场景里**（最初的方案是挂在 `CPRTrainingController` 同一个物体上，
但那样漏挂一次就整场数据为零，而且当场看不出来）。改成 `RuntimeInitializeOnLoadMethod`
自启动：场景加载后若存在 `CPRTrainingController` 而没有记录器，就自己建一个。
职责只有三件：

1. 订阅 `OnCompressionComplete` / `OnRecoilEvaluated` / `OnPositionEvaluated`，把逐次明细攒在 `List` 里
2. 训练结束时，从 controller 读汇总字段，拼成 `sessions.csv` 一行
3. 两张表都追加写到 `Application.persistentDataPath`

**不改动 `CPRTrainingController` 的既有逻辑**，只把需要的统计字段暴露成只读属性
（现在全是 `private`，这是唯一需要动它的地方）。这样日志坏了也不会影响训练流程。

### 5.2 participant ID / 条件 / 轮次 怎么输入

VR 里打字非常痛苦。按省事程度排序：

1. **开始界面加一个极简数字面板**（0–9 按钮 + A/B 切换 + 1/2 切换）—— 实验者戴上头显前点三下。最稳。
2. **`persistentDataPath/session_config.json`**，实验者用 `adb push` 每场换一次 —— 不用改 UI，但每场都要连线。
3. **`PlayerPrefs` + adb 传参** —— 最省事但最容易忘记改。

推荐 1。实验当天最怕的就是「这场的 ID 忘了改，两个人的数据写到同一个编号下」。

**两半都已实装。**

**输入面板**：`Assets/Scripts/Research/ResearchSetupPanel.cs`。自启动，只在有 `MainMenuUI`
的场景（即主菜单）出现，训练场景不出。布局：

```
                RESEARCH SETUP
      Check against the allocation sheet before each run.
              ┌───────────────────┐
              │        P07        │
              └───────────────────┘
   [1][2][3]        GROUP  [ A ][ B ]
   [4][5][6]        TRIAL  [1][2][3]
   [7][8][9]        SCORE  [HIDE][SHOW]
   [DEL][0][CLR]
                  [  START  ]
```

确认后写进 `ResearchSession`（PlayerPrefs），画面销毁，主菜单恢复。
纯 ASCII，不依赖 CJK 字体图集。按钮是普通的 `Button`，
所以 `HandPokeButtonInteractor` 不用改就能戳。

**常驻显示**：`Assets/Scripts/Research/ResearchHudLabel.cs`，同样是自启动，
在视野左上角贴一块小牌子，内容是 `REC/IDLE  P07 / B / 2` —— 当前 ID / 群 / 轮次，
以及此刻是否正在记录（记录中变绿）。设置画面确认之后仍然看得见，
所以开场前还能再核对一次。
纯 ASCII；`raycastTarget` 全关，不会抢手部的 poke 判定。

### 5.3 导出

- **adb pull**：`adb pull /sdcard/Android/data/com.cprtraining.quest3/files/research/ ./research`
  （`persistentDataPath` 是 `/sdcard/Android/data/<包名>/files`，两张 CSV 在它下面的
  `research/` 子目录里 —— `TrainingDataLogger.folderName` 的默认值）。最简单，离线也能干。
- **自动上传 Google**：按之前讲的 Apps Script Web 应用方案，把 `sessions.csv` 的一行 POST 上去，
  失败就留在本地待补传。`compressions.csv` 行数多，建议还是 adb pull，不要每次按压都发请求。

**注意**：Player Settings → Android → **Internet Access = Require**
（现在 `Assets/Plugins/Android/AndroidManifest.xml` 里只有 `RECORD_AUDIO`，没有 INTERNET）。

---

## 6. 建议的终点设定（写进论文的 outcome 一览）

**参加者 50 人（各群 25 人）**。80% の検出力が出るのは d ≈ 0.8 以上なので、
主要终点は欲張らず **1 つだけ**にする。

| 区分 | 指标 | 来源 |
|---|---|---|
| **主要终点（1 つだけ）** | 深度合格率（5–6 cm の割合） | `depth_in_range_pct` |
| 次要（客观） | 频率合格率（100–120/min の割合） | `rate_in_range_pct` |
| 次要（客观） | 深度 SD、频率 SD（稳定性） | `compressions.csv` |
| 次要（客观） | 完全回弹率、位置合格率、CCF、最长中断 | 既有 |
| 次要（客观） | 学习曲线：进入合格区间所需按压次数 | `compressions.csv` |
| 次要（客观） | AED time-to-shock | **要新增** |
| 次要（客观） | 知识测验正答率 | `QuizResult` |
| 次要（主观） | SUS / Raw NASA-TLX / リアルさ / 自信（体験後の水準） | 问卷 |
| 次要（主观） | 振動そのものの評価（HP1〜HP9） | 问卷（**B 群のみ**） |

为什么只留一个主要终点：每组 25 人时，哪怕只立 2 个主要终点，
Bonferroni 校正后 α 降到 .025，可检出的 d 从 0.79 升到约 0.87——
本来就紧张的检出力又被削一截。深度合格率是 CPR 研究最通行的主要终点，
也是实时反馈设备效应最大的地方，选它。其余全部按**探索性**次要终点报告。

**被験者間なので検定はすべて独立標本**（t 検定 / Mann-Whitney U）。
事前アンケートを廃止したので、対応のある検定を使う場面は無い。
自己効力感も「体験後の水準」を群間で比べるだけになる。

### 50 人で検出力を稼ぐ 2 つの手

**ANCOVA**：主要终点の解析に、CPR 経験の有無と VR 経験を共変量として入れる。
r≈0.5 の相関があれば誤差分散が 25% 減り、検出可能な d が 0.79 → 0.69 になるが、
**事前アンケートを廃止してベースライン自己効力感が使えなくなった**ぶん、
実際の相関はこれより低いと見ておくこと。解析計画には**事前に**書いておく
（結果を見てから足すと後付けになる）。

**混合効果モデル**：`compressions.csv` は 1 人あたり約 100 行ある。
「この圧迫が適切な深さだったか」を圧迫 1 回ごとの二値アウトカムとし、
参加者をランダム切片にしたロジスティック混合モデルを組むと、
1 人 1 個の要約値を比べるより情報を使い切れる。
ただし同一人物内の圧迫は強く相関する（級内相関が高い）ので、
**行数が 100 倍でも検出力が 100 倍になるわけではない**。
過大な期待はせず、主解析は要約値で行い、こちらは補助的な位置づけにする。

> 逐次明细がこの設計で一番望みがあるのは変わらない。深度 SD と学習曲線は
> 平均値より群間差が出やすい。ただし SD も学習曲線の指標も
> 「1 人 1 個の数値」なので、n はやはり 25 対 25 であることは忘れないこと。
