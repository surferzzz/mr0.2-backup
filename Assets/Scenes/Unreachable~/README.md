# Unreachable~ —— 运行时不可达的场景（已隔离，未删除）

Unity 会忽略以 `~` 结尾的文件夹，所以这里的场景**不会被导入、也进不了构建**，
但文件和 .meta（含原 guid）都完整保留，随时可以移回去。

## 为什么隔离而不是删除

和 `Assets/Scripts/Unreachable~/` 同一个理由：**防止在死场景里白改**。

这些场景里的对象名和线上场景一模一样 —— 同样叫 `TrainingCanvas`、
`CPRTrainingController`、同一个患者模型。打开 `CPRTraining.unity` 调半天 UI 摆位，
保存、打包、戴上头显，什么都没变，因为跑的根本不是这个场景。
`CPRTraining_Setup_Guide.md` 更直接，第一句就让人去打开这个死场景。

## 判定依据

实际运行链路只有两个场景，闭环如下：

```
Assets/Scenes/MainMenu.unity
  └─ MainMenuUI.trainingSceneName = "Assets/demo/0.7.unity"
       └─ Assets/demo/0.7.unity   ← 构建首场景，CPRTrainingController 挂在这里
            └─ CPRTrainingController.mainMenuSceneName = "MainMenu"（回到上面）
```

同时确认：

- `ProjectSettings/EditorBuildSettings.asset` 里只有 `Assets/demo/0.7.unity`
  和 `Assets/Scenes/MainMenu.unity` 两条
- 这七个场景的 guid 在整个 `Assets` / `ProjectSettings` / `Packages` 下引用数为 0
- 唯一挂在场景里的菜单控制器是 `MainMenuUI`；
  `GameManager`、`GameFlowManager`、`SimpleMenuController` 也都不在任何场景里
  （它们同样是不可达的，但属于脚本层面的清理，未在本次一并处理）

## 清单

| 文件 | 原位置 | 最后修改 |
|---|---|---|
| `CPRSimulation.unity` | `Assets/Scenes/` | 2026-03-03 |
| `CPRTraining.unity` | `Assets/Scenes/` | 2026-04-24 |
| `CPRTraining_Setup_Guide.md` | `Assets/Scenes/` | 上面那个场景的搭建说明 |
| `0.3.unity` | `Assets/demo/` | 2026-04-20 |
| `0.4.unity` | `Assets/demo/` | 2026-04-20 |
| `0.5.unity` | `Assets/demo/` | 2026-04-20 |
| `0.6.unity` | `Assets/demo/` | 2026-04-20 |
| `mr0.7.unity` | `Assets/0.7/`（移走后该文件夹已空，连同 .meta 一并删除） | 2026-04-27 |

作为对照，线上的 `Assets/demo/0.7.unity` 一直在改。
`Assets/demo/0.7/`（烘焙光照数据）属于线上场景，**没有**被移走。

## 怎么恢复

把对应的 `.unity` 和 `.unity.meta` 一起移回表里的原位置即可，guid 不变，
所有引用关系照旧。要进构建还需要在 Build Settings 里手动加回去。
