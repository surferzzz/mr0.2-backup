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

未做：HUD 角落常驻显示 ID / 条件 / 轮次，以及 VR 内的 ID 输入面板（目前靠 `session_config.json`）。

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
| `trial_index` | 会话开始时选择 | 被験者間なので通常 1。再実施したときだけ 2 以上 |
| `app_version` | `Application.version` | 中途改过参数时能追溯 |
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
| `overall_score` | 既有加权总分 | 自家算法，**不要当主要终点**，当描述统计用 |
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

挂在和 `CPRTrainingController` 同一个物体上，职责只有三件：

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
面板设好后把当前 ID / 条件 / 轮次**常驻显示在 HUD 角落**，实验者随时能看见有没有设错。

### 5.3 导出

- **adb pull**：`adb pull /sdcard/Android/data/<包名>/files/sessions.csv`
  （Quest 上 `persistentDataPath` 就是这个路径）。最简单，离线也能干。
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
| 次要（主观） | SUS / Raw NASA-TLX / リアルさ / 自信变化 | 问卷 |
| 次要（主观） | 振動そのものの評価（HP1〜HP9） | 问卷（**B 群のみ**） |

为什么只留一个主要终点：每组 25 人时，哪怕只立 2 个主要终点，
Bonferroni 校正后 α 降到 .025，可检出的 d 从 0.79 升到约 0.87——
本来就紧张的检出力又被削一截。深度合格率是 CPR 研究最通行的主要终点，
也是实时反馈设备效应最大的地方，选它。其余全部按**探索性**次要终点报告。

**被験者間なので検定はすべて独立標本**（t 検定 / Mann-Whitney U）。
対応のある検定を使ってよいのは、同一人物の事前→事後を比べる自己効力感だけ。
群間比較では、その「変化量」を独立標本で比べる。

### 50 人で検出力を稼ぐ 2 つの手

**ANCOVA**：主要终点の解析に、事前の自己効力感と CPR 経験の有無を共変量として入れる。
結果と r≈0.5 の相関があれば誤差分散が 25% 減り、検出可能な d が 0.79 → 0.69 になる。
解析計画に**事前に**書いておくこと（結果を見てから足すと後付けになる）。

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
