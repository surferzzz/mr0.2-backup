# mr0.2 — VR CPR 训练（Meta Quest 3）

Unity + Meta XR 的心肺复苏训练场景，附带研究用数据采集：每次按压一行的明细，加上与问卷 1:1 对应的 session 记录。

## 在另一台电脑上跑起来

### 1. 装环境

| 需要的东西 | 版本 / 说明 |
| --- | --- |
| Unity Editor | **2022.3.62f2**（revision `7670c08855a9`）。版本要对上——换小版本会让 Meta XR 的包整体重新导入，容易出编译错误 |
| Unity 模块 | 安装时勾选 **Android Build Support**，连同 **OpenJDK** 和 **Android SDK & NDK Tools** |
| Git | 必须在 PATH 上。`Packages/manifest.json` 里有一个 Git 依赖（`com.besty.unity-skills`），Unity 打开工程时要用 git 去拉它 |
| 设备 | Meta Quest 3，开开发者模式 + USB 调试 |

Unity Hub 里装指定版本：Installs → Install Editor → Archive，或者直接打开
`unityhub://2022.3.62f2/7670c08855a9`。

### 2. 克隆

```
git clone https://github.com/surferzzz/mr0.2-backup.git
cd mr0.2-backup
```

仓库里没有 `Library/`。用 Unity Hub 第一次打开会重新导入全部素材，要几分钟到十几分钟，期间别中断。

### 3. 场景

- 主场景：`Assets/demo/0.7.unity`
- 菜单：`Assets/Scenes/MainMenu.unity`

两个都已经在 Build Settings 的场景列表里，顺序也是对的。

### 4. 构建到 Quest

工程里已经配好，不用改：

- Platform: **Android**
- Scripting Backend: **IL2CPP**，Target Architecture: **ARM64**
- Package name: `com.cprtraining.quest3`
- Min SDK 33 / Target SDK 34

File → Build Settings → Android → Switch Platform（第一次切平台会再导入一遍）→ Build And Run。

## 研究数据写在哪

`Assets/Scripts/Research/TrainingDataLogger.cs` 往 `Application.persistentDataPath/research/` 写两个 CSV：

- `sessions.csv` — 每次体验一行，与问卷一一对应
- `compressions.csv` — 每次按压一行，用于学习曲线和离散度分析

Quest 上的实际路径是 `/sdcard/Android/data/com.cprtraining.quest3/files/research/`，取数据：

```
adb pull /sdcard/Android/data/com.cprtraining.quest3/files/research ./data
```

问卷设计和客观指标的定义在 `docs/survey/`。

## 仓库里刻意不放的东西

| 路径 | 原因 |
| --- | --- |
| `Library/` `Temp/` `Logs/` `UserSettings/` | Unity 自己生成，换机器必须本地重建 |
| `Assets/*.zip`（`Hospital_assets.zip` 等） | 解压后的内容已经在 `Assets/ThirdParty/` 里，再存一份只会把历史撑大 |
| `Assets/ThirdParty/MadduckHospital/Hospital_assets/UE5_textures/` | 422 MB 的 UE5 格式源贴图，Unity 不导入；场景用的是同级 `textures/` 下的 PNG |
| `Assets/ThirdParty/MadduckHospital/Hospital_assets/hospital.blend` | FBX 的 Blender 源文件；留在 `Assets/` 下会要求每台机器都装 Blender 才能导入成功 |
| `cpr_v0.7/` `*.apk` | 构建产物 |

主场景实际引用的 `Assets/ThirdParty/` 资源（FBX、材质、贴图，约 113 MB）**在仓库里**，克隆下来就能直接打开，不会出现 missing 引用。

## 素材授权

`Assets/` 下含第三方素材：Madduck Hospital、Sci-Fi Styled Modular Pack、Bhaptics SDK、Mixamo 动作、Kenney UI、Noto 字体。各自的授权归原作者所有，随本仓库一并存放仅为复现研究环境，不构成转授权。
