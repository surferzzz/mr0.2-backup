# Unreachable~ —— 运行时不可达的脚本（已隔离，未删除）

Unity 会忽略以 `~` 结尾的文件夹，所以这里的脚本**不参与编译**，
但文件和 .meta（含原 guid）都完整保留，随时可以移回去。

## 为什么隔离而不是删除

这些脚本都是早期实现或被现有系统取代的方案，但里面有完整的工作
（分步引导、评分系统、面板动画等），删掉就找不回来了。

隔离的实际作用是**防止在死代码里白改**：本次审计中就发生过两次
—— 在 `TutorialUI` 和 `CPRGameController` 里认真修了 bug，
之后才发现它们不在任何场景里、改动不产生任何效果。

## 判定依据

从「场景/预制体挂载的组件」和「`[RuntimeInitializeOnLoadMethod]` 自启动的组件」
两类根出发做传递可达性分析，这些文件不可达；同时确认：

- 不在任何 .unity / .prefab / .asset 里（移走不会留下 missing script）
- 没有任何仍在编译的代码引用它们声明的类型

## 没有被隔离的近亲

以下脚本同样运行时不可达，但**编辑器搭建工具还在引用**，移走会让向导编译失败：

| 文件 | 被谁引用 |
|---|---|
| `CPR/Core/GloveHapticFeedback.cs` | AddTrainingSetup, OneClickFix, SimpleXBotSetup |
| `CPR/Core/CPRPressureDetector.cs` | AddTrainingSetup, OneClickFix, SimpleXBotSetup |
| `CPR/Core/CPRStatusDisplay.cs` | AddTrainingSetup, XBotSetup |
| `Core/EarthquakeSceneController.cs` | EarthquakeRebuild |
| `UI/EarthquakeUIStyler.cs` | EarthquakeSceneController（上一行那个） |

**注意命名陷阱**：`CPRPressureDetector`（不可达）和 `CPRPressDetector`（活跃）
只差 `ure` 三个字母。改错文件不会报任何错，只是没有效果。

## 如何恢复某个文件

把 .cs 和 .meta 一起移回 `Assets/Scripts/<原相对路径>` 即可。
.meta 里的 guid 没变，所以历史引用（如果有）仍然有效。

## 本次隔离清单

- `UI/TutorialUI.cs` (284 行)
- `Equipment/FirstAidKit.cs` (190 行)
- `Core/SceneDiagnostics.cs` (188 行)
- `CPR/CPRSimpleUI.cs` (178 行)
- `CPR/UIPanelAnimator.cs` (171 行)
- `UI/MenuUI.cs` (168 行)
- `Core/ScoreSystem.cs` (164 行)
- `UI/ScoreUI.cs` (156 行)
- `Core/PerformanceOptimizer.cs` (115 行)
- `Core/GameManager.cs` (92 行)
- `VR/VRCanvasSetup.cs` (80 行)
- `VR/VRHandController.cs` (79 行)
- `Core/SimpleEarthquakeTrigger.cs` (76 行)
- `VR/VRInteractionHelper.cs` (65 行)
- `Core/SceneLoader.cs` (46 行)
- `Compat/MRUKCompat.cs` (21 行)

合计 16 个文件 / 2073 行

---

# 追加隔离：搭建向导（2026-09-16）

起因是同一天把七个不可达场景移进了 `Assets/Scenes/Unreachable~/`。
`Assets/Scripts/CPR/Editor/` 下有一批搭建向导按**字符串路径**引用那些场景 ——
它们照常编译（路径只是字符串），但菜单点下去会失败，其中一个还会把死场景
重新写回 Build Settings，等于把清理成果抵消掉。

## 本次隔离清单

| 文件 | 行数 | 为什么不可达 |
|---|---|---|
| `CPR/Editor/RealisticEmergencySceneBuilder.cs` | 652 | 建的根物体 `Realistic_Emergency_Scene` **不在**线上场景里，已被 Madduck 版取代；`PrimaryScenePath` 指向已归档的 `Assets/0.7/mr0.7.unity`，且开场景时**没有** `File.Exists` 保护，点了直接抛异常 |
| `CPR/Editor/OneClickGameSetup.cs` | 468 | 打开并保存到已归档的 `Assets/Scenes/CPRTraining.unity`；最要命的是第 386 行 `new EditorBuildSettingsScene("Assets/Scenes/CPRTraining.unity", true)` —— 跑一次就把死场景塞回构建 |
| `CPR/Editor/FreshStartSetup.cs` | 394 | 保存到 `Assets/Scenes/CPRTraining_Main.unity`，**这个场景从来就没存在过**，隔离前就已经是坏的 |

合计 3 个文件 / 1514 行。隔离后 `Assets/Scripts/**/Editor/` 下剩余 35 个脚本全部编译通过。

## 同批排查过、但**没有**隔离的

这三个也引用了死场景路径，但主体功能还活着，整份移走会误伤：

| 文件 | 为什么留下 |
|---|---|
| `CPR/Editor/MadduckHospitalSceneBuilder.cs` | 它建的 `Madduck_Hospital_Training_Room` **就在线上场景 `demo/0.7.unity` 里**，是现在这间训练室的来源，而且按名字查找并替换旧根、可重复执行。另外两个菜单项（`Build / Fix ... In Current Scene`）作用于当前场景，完全可用。只有 `PrimaryScenePath` 指向已归档的 `mr0.7.unity`，但那条路径外面包着 `File.Exists` + `DisplayDialog`，会礼貌地报错而不是抛异常 —— 所以**保持原样**，没有把它改指向 `demo/0.7.unity`：那会把一个现在无害的菜单项变成「一点就重写并保存线上场景」 |
| `CPR/Editor/CPRSimpleSetup.cs` | 六个菜单项里只有「打开CPR模拟场景」指向死场景，而且已有 `File.Exists` 保护，失败时只打一条 warning。其余五项（创建胸部检测点、添加触觉反馈控制器、添加手部追踪适配器、添加 bHaptics SDK2 等）都作用于当前场景 |
| `CPR/Editor/GameSetupWizard.cs` | 唯一涉及死场景的地方是第 308 行提示文案里的字符串 `"1: Scenes/CPRSimulation"`，不影响任何行为 |

## 顺带确认

`GameManager`、`GameFlowManager`、`SimpleMenuController` 同样不挂在任何场景里
（唯一活着的菜单控制器是 `MainMenuUI`）。`GameManager` 上一轮已经隔离，
另外两个仍在 `Assets/Scripts/CPR/Game/` 下，本次未处理。

---

# 追加隔离：GameFlowManager（2026-10-18）

| 文件 | 行数 | 判定 |
|---|---|---|
| `CPR/Game/GameFlowManager.cs` | 121 | 不在任何场景里，且全项目没有任何仍在编译的代码引用它声明的类型 |

## 同批排查过、但**不能**隔离的

| 文件 | 为什么留下 |
|---|---|
| `CPR/Game/SimpleMenuController.cs` | 虽然同样不挂在任何场景里，但 `CPRTrainingController.DisableOtherControllers()` 会 `FindObjectOfType<SimpleMenuController>()` 把它关掉，另有 3 个 Editor 向导引用。移走会直接编译失败 |
| `CPR/Game/CPRGameController.cs` | 同上，另有 4 个 Editor 向导引用 |

这两个属于本文件开头说的「没有被隔离的近亲」那一类：运行时不可达，
但仍被编译期引用拴着。要隔离得先把 `DisableOtherControllers()` 里那两段
防御性关闭删掉，而那两段的存在本身说明曾经踩过「多个控制器互相打架」的坑，
不宜顺手拆。
