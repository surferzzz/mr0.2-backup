# CPR训练场景设置指南

## 步骤1：打开场景
1. 在Unity编辑器中打开 `Assets/Scenes/CPRTraining.unity`

## 步骤2：添加XR Origin
1. 在Project窗口中，导航到 `Assets/Samples/XRI/Prefabs/`
2. 找到 `XR Origin (XR Rig).prefab`
3. 将预制体拖拽到场景Hierarchy中
4. 设置位置：Position (0, 0, 0)

## 步骤3：添加XR Interaction Manager
1. 在Hierarchy中右键点击，选择 `XR > XR Interaction Manager`
2. 或者从 `Assets/Samples/XRI/Prefabs/` 拖拽 `XR Interaction Setup.prefab`（如果存在）

## 步骤4：创建地面
1. 在Hierarchy中右键点击，选择 `3D Object > Plane`
2. 重命名为 "Floor"
3. 设置Transform：
   - Position: (0, 0, 0)
   - Scale: (10, 1, 10)
4. 添加材质（可选，创建灰色材质）

## 步骤5：创建CPR训练假人基础结构

### 5.1 创建假人根对象
1. 在Hierarchy中右键点击，选择 `Create Empty`
2. 重命名为 "CPR Dummy"
3. 设置Position: (0, 0.5, 2) - 距离玩家2米，高度0.5米

### 5.2 创建胸部区域
1. 在 "CPR Dummy" 下创建空对象，命名为 "Chest"
2. 设置Position: (0, 0.2, 0)
3. 添加 `Box Collider` 组件：
   - Size: (0.3, 0.2, 0.15) - 模拟胸部区域
   - Is Trigger: false
4. 添加 `CompressionDetector` 脚本（Assets/Scripts/CPR/CompressionDetector.cs）

### 5.3 创建头部区域
1. 在 "CPR Dummy" 下创建空对象，命名为 "Head"
2. 设置Position: (0, 0.6, 0.1)
3. 添加 `Sphere Collider` 组件：
   - Radius: 0.12
   - Is Trigger: false
4. 添加 `BreathingDetector` 脚本（Assets/Scripts/CPR/BreathingDetector.cs）

### 5.4 创建标记点
1. 在 "Chest" 下创建空对象，命名为 "CompressionArea"
   - Position: (0, 0, 0) - 相对于Chest
2. 在 "Head" 下创建空对象，命名为 "HeadArea"
   - Position: (0, 0, 0) - 相对于Head

### 5.5 添加假人模型（临时使用基本几何体）
1. 在 "CPR Dummy" 下创建 `3D Object > Capsule`，命名为 "Body"
   - Position: (0, 0, 0)
   - Scale: (0.4, 0.8, 0.3)
2. 在 "Head" 下创建 `3D Object > Sphere`，命名为 "HeadModel"
   - Position: (0, 0, 0)
   - Scale: (0.2, 0.2, 0.2)

## 步骤6：配置CPR管理器
1. 在 "CPR Dummy" 上添加 `CPRManager` 脚本（Assets/Scripts/CPR/CPRManager.cs）
2. 在Inspector中配置：
   - Compression Detector: 拖拽 "Chest" 对象
   - Breathing Detector: 拖拽 "Head" 对象
   - Feedback: 稍后配置

## 步骤7：配置检测器

### 7.1 配置CompressionDetector（在Chest上）
1. Compression Area: 拖拽 "CompressionArea" Transform
2. Hand Controller: 需要创建VRHandController（见步骤8）
3. Use Left Hand: false（使用右手）

### 7.2 配置BreathingDetector（在Head上）
1. Head Area: 拖拽 "HeadArea" Transform
2. Hand Controller: 需要创建VRHandController（见步骤8）
3. Use Left Hand: false（使用右手）

## 步骤8：创建VR手部控制器
1. 在Hierarchy中创建空对象，命名为 "VR Hand Controller"
2. 添加 `VRHandController` 脚本（Assets/Scripts/VR/VRHandController.cs）
3. 在XR Origin下找到左右手控制器：
   - 展开 XR Origin > Camera Offset > Left Controller
   - 展开 XR Origin > Camera Offset > Right Controller
4. 在VRHandController的Inspector中：
   - Left Hand Interactor: 拖拽 Left Controller 下的 XR Direct Interactor
   - Right Hand Interactor: 拖拽 Right Controller 下的 XR Direct Interactor

## 步骤9：创建UI Canvas
1. 在Hierarchy中右键点击，选择 `UI > Canvas`
2. 重命名为 "CPR Training UI"
3. 设置Canvas组件：
   - Render Mode: World Space
   - Position: (0, 1.8, 1.5) - 在玩家前方
   - Scale: (0.001, 0.001, 0.001) - 缩小以适应VR
4. 在Canvas下创建子对象：
   - 创建 `UI > Text - TextMeshPro`，命名为 "InstructionText"
   - 创建 `UI > Text - TextMeshPro`，命名为 "CompressionCountText"
   - 创建 `UI > Text - TextMeshPro`，命名为 "BreathCountText"
   - 创建 `UI > Text - TextMeshPro`，命名为 "CycleCountText"

## 步骤10：配置CPR反馈系统
1. 在 "CPR Dummy" 上添加 `CPRFeedback` 脚本（Assets/Scripts/CPR/CPRFeedback.cs）
2. 在Inspector中配置：
   - Instruction Text: 拖拽 "InstructionText"
   - Compression Count Text: 拖拽 "CompressionCountText"
   - Breath Count Text: 拖拽 "BreathCountText"
   - Cycle Count Text: 拖拽 "CycleCountText"

## 步骤11：创建游戏管理器
1. 在Hierarchy中创建空对象，命名为 "Game Manager"
2. 添加 `GameManager` 脚本（Assets/Scripts/Core/GameManager.cs）
3. 配置场景名称：
   - CPR Training Scene Name: "CPRTraining"

## 步骤12：创建评分系统
1. 在Hierarchy中创建空对象，命名为 "Score System"
2. 添加 `ScoreSystem` 脚本（Assets/Scripts/Core/ScoreSystem.cs）

## 步骤13：创建性能优化器（可选）
1. 在Hierarchy中创建空对象，命名为 "Performance Optimizer"
2. 添加 `PerformanceOptimizer` 脚本（Assets/Scripts/Core/PerformanceOptimizer.cs）

## 步骤14：测试场景
1. 保存场景（Ctrl+S）
2. 点击Play按钮
3. 在Game视图中测试（需要VR设备才能完整测试）

## 注意事项
- 所有脚本引用需要在Inspector中手动配置
- XR Origin的位置会根据VR设备自动调整
- 按压检测的精度取决于手部追踪的准确性
- 建议在Quest设备上进行实际测试

## 下一步
完成场景设置后，可以：
1. 添加更详细的假人模型
2. 优化UI布局和样式
3. 添加音效和视觉反馈
4. 在Quest设备上测试和调优
