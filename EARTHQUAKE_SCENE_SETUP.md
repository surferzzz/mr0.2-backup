# 地震场景创建指南

## 目标
基于 `HandOnHandInteraction.unity` 示例场景创建一个模拟地震的VR训练场景，用于灾害中的心肺复苏训练。

## 场景位置
- **源场景**: `Assets/Samples/Meta XR Interaction SDK/78.0.0/Example Scenes/HandOnHandInteraction.unity`
- **目标场景**: `Assets/Scenes/EarthquakeTraining.unity`

---

## 操作步骤

### 第一步：复制示例场景

1. 在Unity编辑器的Project窗口中，导航到：
   ```
   Assets/Samples/Meta XR Interaction SDK/78.0.0/Example Scenes/
   ```

2. 找到 `HandOnHandInteraction.unity` 场景

3. 右键点击该场景 → **Duplicate** (复制)

4. 重命名为 `EarthquakeTraining`

5. 将复制的场景移动到 `Assets/Scenes/` 文件夹（如果该文件夹不存在，先创建）

### 第二步：打开场景并添加地震控制器

1. 双击打开 `EarthquakeTraining.unity` 场景

2. 在Unity顶部菜单栏选择：
   ```
   Tools → CPR → Add Earthquake Controller
   ```

3. 脚本会自动执行以下操作：
   - 创建 `EarthquakeController` GameObject
   - 自动绑定摄像机（优先MainCamera，其次CenterEyeAnchor）
   - 自动为场景中的物体添加Rigidbody组件（最多50个）
   - 配置刚体参数（质量、插值等）

4. 点击确定按钮

---

## 地震控制器配置

### 基础参数设置

选中场景中的 `EarthquakeController` GameObject，在Inspector面板中配置：

#### **Timing（时间控制）**
- **Duration**: `8` 秒（地震持续时长）
- **Intensity Curve**: 默认曲线（控制地震强度随时间变化）

#### **Camera Shake（摄像机抖动）**
- **Target Camera**: 自动绑定到XR Origin下的Camera
- **Amplitude Pos**: `0.08` 米（位置抖动幅度）
- **Amplitude Rot**: `1.5` 度（旋转抖动幅度）
- **Frequency**: `6` Hz（抖动频率）

#### **Rigidbodies Impact（物体冲击）**
- **Affected Bodies**: 自动收集的场景刚体数组
- **Impulse Interval**: `0.6` 秒（冲击间隔）
- **Impulse Force**: `120` 牛顿（冲击力度）

#### **VFX / SFX（视觉和音效，可选）**
- **Dust FX**: 尘土粒子效果（需要手动创建并拖拽）
- **Rumble Loop**: 低频隆隆声音（需要AudioSource组件，勾选Loop）
- **Hit Sfx**: 短促撞击音效

#### **Debug（调试）**
- **Play On Start**: `false`（不自动播放，通过代码或按钮触发）

---

## 添加环境物体（增强地震效果）

### 1. 添加可掉落物体

在场景中添加一些物体来增强地震效果：

```
GameObject → 3D Object → Cube/Sphere/Capsule
```

建议添加的物体：
- 书架
- 花瓶
- 医疗设备
- 椅子
- 桌子

为每个物体添加组件：
1. **Rigidbody** 组件
   - Mass: 5-20
   - Use Gravity: ✓
   - Interpolate: Interpolate

2. **Collider** 组件（通常自动添加）

3. 将这些Rigidbody拖拽到 `EarthquakeController` 的 `Affected Bodies` 数组中

### 2. 添加CPR训练假人

在地震场景中放置CPR假人：

1. 创建空GameObject，命名为 `CPR Dummy`
2. 位置设置为地面上：`(0, 0.2, 2)`
3. 按照CPR场景设置添加相应组件（参考CPRSceneSetup工具）

---

## 添加UI触发按钮

创建一个UI按钮来触发地震：

### 方法1：使用3D UI按钮

1. 创建World Space Canvas：
   ```
   GameObject → UI → Canvas
   ```

2. 设置Canvas：
   - Render Mode: `World Space`
   - Position: `(0, 1.5, 1.5)`
   - Scale: `(0.01, 0.01, 0.01)`

3. 添加Button：
   ```
   右键Canvas → UI → Button - TextMeshPro
   ```

4. 设置Button文本为 "触发地震"

5. 配置Button的OnClick事件：
   - 拖拽 `EarthquakeController` 到目标框
   - 选择函数：`EarthquakeController.Play()`

### 方法2：使用手部交互按钮

利用HandOnHandInteraction中的手部交互功能：

1. 创建一个可抓取的按钮物体
2. 添加碰撞检测脚本
3. 检测到抓取时调用 `EarthquakeController.Play()`

---

## 添加音效和粒子效果

### 音效设置

1. **隆隆声循环音效**：
   - 在 `EarthquakeController` GameObject上添加 `AudioSource` 组件
   - 勾选 `Loop`
   - 设置音量为 0.5-0.7
   - 导入低频隆隆声音文件（可从免费音效网站下载）
   - 将AudioSource拖拽到 `Rumble Loop` 字段

2. **撞击音效**：
   - 添加另一个 `AudioSource` 组件
   - 不勾选Loop
   - 导入短促撞击音效
   - 将AudioSource拖拽到 `Hit Sfx` 字段

### 粒子效果

1. 创建粒子系统：
   ```
   GameObject → Effects → Particle System
   ```

2. 命名为 `DustEffect`

3. 配置粒子参数：
   - Duration: 2
   - Looping: ✗
   - Start Lifetime: 1-3
   - Start Speed: 1-2
   - Start Size: 0.1-0.5
   - Start Color: 灰褐色
   - Emission Rate: 20-50
   - Shape: Box (范围覆盖场景)

4. 将粒子系统拖拽到 `Dust FX` 字段

---

## 测试地震效果

### 方法1：通过Inspector测试

1. 进入Play模式
2. 在Hierarchy中选中 `EarthquakeController`
3. 在Inspector面板中找到 `Play On Start` 勾选框下方
4. 点击右键 → `Debug` 查看组件
5. 调用 `Play()` 方法

### 方法2：通过UI按钮测试

1. 进入Play模式
2. 使用VR手柄点击 "触发地震" 按钮
3. 观察效果

### 预期效果

- ✓ 摄像机开始抖动
- ✓ 场景中的物体开始晃动、掉落
- ✓ 播放隆隆声音效
- ✓ 播放撞击音效
- ✓ 显示尘土粒子效果
- ✓ 8秒后自动停止

---

## 整合CPR训练流程

### 场景流程设计

1. **阶段1：平静状态**
   - 玩家进入场景
   - 展示环境和CPR假人

2. **阶段2：地震发生**
   - 触发地震（自动或手动）
   - 摄像机抖动，物体掉落
   - 持续8秒

3. **阶段3：地震结束**
   - 场景稳定
   - UI提示：发现倒地的伤员

4. **阶段4：CPR训练**
   - 引导玩家检查伤员意识
   - 开始CPR训练流程
   - 使用CPRManager控制训练

### 脚本集成

创建一个场景控制器来管理整个流程：

位置：`Assets/Scripts/Core/EarthquakeSceneController.cs`

```csharp
using UnityEngine;
using VRFirstAid.CPR;

public class EarthquakeSceneController : MonoBehaviour
{
    public EarthquakeController earthquake;
    public CPRManager cprManager;
    public GameObject instructionUI;

    private enum SceneState
    {
        Idle,
        Earthquake,
        PostEarthquake,
        CPRTraining
    }

    private SceneState currentState = SceneState.Idle;

    public void StartScenario()
    {
        // 触发地震
        currentState = SceneState.Earthquake;
        earthquake.Play();
        Invoke("OnEarthquakeEnded", earthquake.duration);
    }

    private void OnEarthquakeEnded()
    {
        currentState = SceneState.PostEarthquake;
        // 显示提示UI
        instructionUI.SetActive(true);
        // 等待玩家靠近假人
    }

    public void StartCPRTraining()
    {
        currentState = SceneState.CPRTraining;
        instructionUI.SetActive(false);
        // 启动CPR训练
        cprManager.gameObject.SetActive(true);
    }
}
```

---

## 场景优化建议

### 性能优化

1. **限制受影响的刚体数量**
   - 只为重要物体添加Rigidbody
   - 建议不超过30个

2. **使用简化碰撞体**
   - 优先使用Box/Sphere Collider
   - 避免Mesh Collider

3. **降低粒子数量**
   - 在Quest设备上限制粒子发射率

### 视觉优化

1. **添加环境光变化**
   - 地震时可以让灯光闪烁
   - 增强紧张感

2. **添加后处理效果**
   - 可选：添加模糊效果
   - 可选：添加色彩失真

---

## 下一步工作

- [ ] 完成地震场景基础设置
- [ ] 添加音效和粒子效果
- [ ] 测试地震效果
- [ ] 整合CPR训练流程
- [ ] 在Quest设备上测试性能
- [ ] 优化用户体验

---

## 注意事项

1. **手部追踪**：确保XR Origin配置正确，HandOnHandInteraction的手部交互正常工作
2. **性能监控**：在Quest上测试时监控帧率，确保地震时保持60fps+
3. **安全提示**：在游戏开始时提示玩家注意VR安全，地震效果可能引起不适
4. **保存场景**：记得随时保存场景（Ctrl+S）

---

## 相关文件

- EarthquakeController.cs: `Assets/Scripts/Environment/EarthquakeController.cs`
- EarthquakeAutoSetup.cs: `Assets/Scripts/Editor/EarthquakeAutoSetup.cs`
- HandOnHandInteraction场景: `Assets/Samples/Meta XR Interaction SDK/78.0.0/Example Scenes/HandOnHandInteraction.unity`
- CPRManager.cs: `Assets/Scripts/CPR/CPRManager.cs`
