# 待验证 / 待决定清单

本次会话的改动里，有一部分我无法自己确认（看不到画面、跑不了构建、没有你的实测数据）。
这里按「必须做」「需要你决定」「可选」分组记录，每条都写清了怎么做和期望结果。

---

## A. 必须在**构建版**验证（编辑器里看不出差别）

### A1. AED 训练是否真的初始化了 —— 最重要

**背景**：`AEDTrainingRuntime.cs`（3666 行）的自启动用的是
`[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]`，它**只在首个场景加载后执行一次**。

Build Settings 的顺序是：

```
[0] Assets/Scenes/MainMenu.unity     ← 启动场景，里面没有 CPRTrainingController
[1] Assets/demo/0.7.unity            ← 训练场景，有
```

旧代码在启动时发现「当前场景没有 CPRTrainingController」就直接 return，
而该特性方法不会再执行第二次 → **构建版里 AED 模块从不初始化**。
编辑器里直接 Play 训练场景时守卫刚好通过，所以问题只在构建版暴露。

已改成挂 `SceneManager.sceneLoaded`，每次场景加载后重试。

**怎么验证**：打包 → 从 MainMenu 进入训练 → 选 AED 模式 → 看 Console/logcat：

- 期望出现：`[AEDTrainingRuntime] Runtime host created.`
- 修复前会出现：`No CPRTrainingController detected in active scene - AED bootstrap skipped.`

---

## B. 需要在编辑器里 Play 一次确认

### B1. Console 里那 6 条 error

Console 右上角显示 `⊘ 6`，但错误过滤按钮是关的所以列表里看不到。
**点一下那个红色圆圈图标**让它们显示出来。我提过三次都没看到内容，
如果里面有本次改动引入的问题，越往后越难定位。

### B2. TMP 迁移后的字号是否偏移

TMP 和 legacy `Text` 的字号度量不同，同一个数值渲染出的视觉大小不完全一样。
迁移涉及：深度值、频率值、按压计数、质量评分、节拍提示、手位提示、教练提示、
知识测试文本、呼救提示、`PhaseText`、`InstructionText`。

偏大或偏小告诉我方向，布局数字现在集中在 `CPRTrainingController.HudLayout` 一处，好调。

### B3. `PhaseText` 升级后颜色是否还对

它原来是场景里配的 **60 号、黄色 (1, 0.92, 0.05)**。
运行时从 legacy Text 升级成 TMP 时我把字号和颜色带过去了，但没法自己确认。
Console 里会有：`[CPRTraining]「PhaseText」已从 legacy Text 升级为 TMP（60号）`

### B4. 回弹容差需要实测校准 ← 需要你提供数据

`CPRPressDetector.recoilToleranceCm` 默认 `1.0`、`maxPressDurationSec` 默认 `1.5`，
是我按 AHA 标准和 100–120/min 的节奏推的，但你的触发区大小和手部追踪抖动会影响实际值。

**做法**：正常按压十几次，看 Console 里每次的日志：

```
[CPRPressDetector] 按压#12: 深度=5.3cm, 速率=108BPM, 回弹残余=0.4cm
```

如果**正常按压**也经常出现「回弹残余 > 1.0cm ← 回弹不足」，说明容差要放宽，
或者 `compressionApproachHeight`（默认 0.12m）需要调。把典型值告诉我。

### B5. HUD 观感

- 面板是否已是圆角（`ApplyPanel` 换了 Kenney 九宫格精灵，之前是硬直角）
- 深度条/频率条上是否有半透明绿色的**合格区间色带**
  （深度 5–6cm → 条上 350–420px；频率 100–120 → 231–347px）
- 标题和数值的字体是否一致（phase 2 迁移后应该全是 TMP，不再混排）

### B6. 地震效果

点「地震開始」，期望：相机震动 + 轰鸣 + **12 个道具跟着晃** + 扬尘飘落 + 随强度起伏的落屑声。
Console 会有：

```
[Earthquake] 自动绑定 12 个道具参与震动：Hospital_Bed、Wheelchair、...
[Earthquake] 已生成扬尘粒子（80 上限 / 30 每秒）
```

震动幅度（`objectPositionAmplitude = 0.035`、`objectRotationAmplitude = 2.5`）
觉得太轻或太夸张就直接在 Inspector 调。

### B7. 知识测验流程

菜单点 `[?] 知識テスト`。期望 Console：

```
[QuizBank] 题库可用，但有以下提醒：
  · 有 2 道题 reviewed=false（医学内容未经核实）...
[QuizBank] 载入 2 道题 / 2 个主题（version 1）
```

流程：题干 + 选项 → 点选 → 对的染绿/错的染红 + 解析 + 「下一题」→ 第二题 → 结果页。

**想验证校验真的管用**：把 `Assets/Resources/VRTrainingUI/Quiz/questions.json` 里
`sample_single` 的 `"answer"` 改成 `9`，应该看到
「answer=9 越界（选项共 4 个，合法范围 0..3）」并拒绝加载。

### B8. 按钮按压进度覆盖层

5 个按钮的 `VRPressButton.progressImage` 原来全是 None ——
按钮要按住 0.6 秒才触发，而这是唯一的进度提示，VR 里没有触觉反馈。
现在会自动生成一个横向扫过的覆盖层（复用按钮自己的精灵）。看按下去有没有填充动画。

---

## C. 需要你决定

### C1. `VRPressButton.requireHoldToActivate` 要不要开

**当前默认 `false`**，保持你现在观察到的行为。

原因：这个组件原来在按满 `holdTime` 时手动调 `onClick`，但它没实现
`IPointerClickHandler`，Button 自己的点击照样发生（Unity 顺序是
`OnPointerDown → OnPointerUp → OnPointerClick`）——**按住满 0.6 秒会触发两次**。
实际使用中很少按到 0.6 秒，所以平时只有 Button 那一次、看不出问题。

`false` = 去掉重复触发、保持原表现（快速点击有效，`holdTime` 只驱动进度显示）。
`true` = 真正的「按住才激活」，可防 VR 射线误触，但**快速点击会失效**，
这是可见的行为改变，建议配合 B8 的进度条一起在设备上试过再定。

### C2. 知识测验的真实题目

数据层和流程已跑通，题库现在是 2 道**明确标注的占位题**（题干写着「示例题·非医学内容」）。

题目内容准确性完全在你/教员手里。每道题有 `"reviewed": false` 字段，
加载时会汇总提醒有多少道未核实。逐条核对后改成 `true`。

文件：`Assets/Resources/VRTrainingUI/Quiz/questions.json`
格式说明和校验规则见 `Assets/Scripts/CPR/Knowledge/QuizModel.cs` 的注释。

### C3. 是否开启 Unity REST 服务（强烈建议）

**Window ▸ UnitySkills ▸ Start Server**

你装了 UnitySkills 但服务没启动。开了之后我能自己完成 B2/B3/B5 这类验证：

| 能力 | 用途 |
|---|---|
| `editor_execute_menu` | 直接跑烘字体这类菜单命令 |
| `console_get_logs` | 自己读 Console，不用你转述 |
| **`scene_screenshot`** | **真正看到 UI** |

最后一项是关键。目前 UI 相关的改动我全是盲改——布局重构、字号、颜色都只能靠你反馈。
有了截图，`VerticalLayoutGroup` 那种改层级的重构才有把握做。

---

## D. 已知可做但没做（按价值排序）

| 项 | 说明 |
|---|---|
| **地震掉落物** `affectedBodies` | 需逐个确认道具有碰撞体 + 设计「重置房间」时机。`Unreachable~` 隔离说明里提到，`HospitalEarthquakeSetup.CreateFallingProps` 有原作者写的编辑器版参考 |
| **TMP phase 3** | 结算面板还有 3 个 legacy `Text`（`timerText`、`completionResultText`、`_completionTextRuntime`）。跨画面不刺眼，所以放最后 |
| **Canvas 像素密度** | `TrainingCanvas` 是 1920×1080 @ scale 0.001 = **1000 px/m**。Quest 3 约 25 PPD，算下来面板距离 **< 1.43m 时 Canvas 是清晰度瓶颈**，光换 TMP 只解决一半 |
| **Layout Group 重构** | 坐标已收敛到 `HudLayout` 一张表（前置步骤已完成）。真正改成 `VerticalLayoutGroup` + `HorizontalLayoutGroup` 需要插 row 容器、改 GameObject 层级，而 `FindChildGameObject` 依赖层级 —— 建议等 C3 能截图后再做 |
| **HUD 抽成 Prefab** | 长期收益最大：编辑器里所见即所得，你能自己调 |
| **AED 模块余下 3400 行** | 只审了开头的启动部分就找到 A1 那个 bug，剩下 101 个方法未审 |

---

## E. 参考：本次会话的验证方式

UI 类改动我看不到结果，所以凡是「行为应当不变」的重构都做了机械对账，例如：

- **布局表提取**：16 项坐标/尺寸/字号逐项比对，全部 IDENTICAL
- **字符串插值优化**：三语文案的静态文本和格式说明符逐项比对 SAME
  （依据是 C# 插值字符串编译后就是 `string.Format`，格式说明符原样保留）
- **代码隔离**：验证无残留引用 + `.meta` 已随文件移动（不会留下 missing script）

如果发现哪里行为变了，这些对账脚本的逻辑写在提交说明里，可以回溯。
