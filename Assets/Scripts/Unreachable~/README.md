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
