# VR急救游戏开发 - 脚本说明

## 项目结构

### Core（核心系统）
- **GameManager.cs**: 游戏管理器，管理场景切换和游戏状态
- **SceneLoader.cs**: 场景加载器，处理异步场景加载
- **ScoreSystem.cs**: 评分系统，评估CPR训练表现

### CPR（CPR训练系统）
- **CPRManager.cs**: CPR流程管理器，使用状态机管理CPR训练流程
- **CompressionDetector.cs**: 按压检测器，检测按压位置、深度和频率
- **BreathingDetector.cs**: 人工呼吸检测器，检测人工呼吸操作
- **CPRFeedback.cs**: CPR反馈系统，提供视觉和音频反馈

### Equipment（医疗设备）
- **EquipmentBase.cs**: 设备基类，所有医疗设备的基础类
- **AEDController.cs**: AED设备控制器，管理AED操作流程
- **FirstAidKit.cs**: 急救包，管理急救物品的存储和使用

### UI（用户界面）
- **MenuUI.cs**: 主菜单UI，管理主菜单界面
- **TutorialUI.cs**: 教程UI，管理教程引导系统
- **ScoreUI.cs**: 分数UI，显示CPR训练分数

### VR（VR交互）
- **VRHandController.cs**: VR手部控制器，管理手部追踪和交互
- **VRInteractionHelper.cs**: VR交互辅助类，提供常用的VR交互功能

## 设置步骤

### 1. 创建CPR训练假人

1. 在场景中创建一个GameObject命名为"CPR Dummy"
2. 添加子对象：
   - "Chest"（胸部区域）- 用于按压检测
   - "Head"（头部区域）- 用于人工呼吸检测
3. 为Chest添加Collider（Box Collider或Sphere Collider）
4. 创建空GameObject作为按压区域标记点，命名为"CompressionArea"
5. 创建空GameObject作为头部区域标记点，命名为"HeadArea"

### 2. 设置CPR检测系统

1. 在CPR Dummy上添加以下组件：
   - `CPRManager`
   - `CPRFeedback`
2. 在Chest上添加`CompressionDetector`组件
3. 在Head上添加`BreathingDetector`组件
4. 配置引用：
   - CPRManager的compressionDetector引用指向Chest上的CompressionDetector
   - CPRManager的breathingDetector引用指向Head上的BreathingDetector
   - CompressionDetector的compressionArea引用指向CompressionArea Transform
   - BreathingDetector的headArea引用指向HeadArea Transform

### 3. 设置VR手部控制器

1. 在XR Origin下找到左右手控制器
2. 创建空GameObject命名为"VR Hand Controller"
3. 添加`VRHandController`组件
4. 配置leftHandInteractor和rightHandInteractor引用

### 4. 设置UI

1. 创建Canvas（World Space）用于VR UI
2. 添加`MenuUI`、`TutorialUI`、`ScoreUI`组件
3. 配置UI引用（按钮、文本等）

### 5. 设置医疗设备

#### AED设备
1. 创建AED模型GameObject
2. 添加`AEDController`组件（继承自EquipmentBase）
3. 设置电极片位置（leftPadPosition和rightPadPosition）
4. 配置视觉反馈对象（电源按钮、电极片、状态灯等）

#### 急救包
1. 创建急救包模型GameObject
2. 添加`FirstAidKit`组件
3. 配置物品列表和预制体

### 6. 场景设置

1. 创建新场景"CPRTraining"
2. 添加XR Origin预制体（从Samples/XRI/Prefabs）
3. 添加XR Interaction Manager
4. 添加CPR Dummy
5. 添加UI Canvas
6. 添加GameManager GameObject（添加GameManager组件）
7. 添加ScoreSystem GameObject（添加ScoreSystem组件）

## 使用说明

### CPR训练流程

1. 启动场景后，CPRManager会自动进入CheckConsciousness状态
2. 按照UI提示完成各个步骤：
   - 检查意识和呼吸（10秒）
   - 呼叫急救
   - 30次胸外按压
   - 2次人工呼吸
   - 重复5个循环
3. 训练完成后会显示分数

### 评分标准

- **按压深度**: 目标5.5cm，容差±1cm
- **按压频率**: 目标110次/分钟，容差±10次/分钟
- **按压位置**: 必须在胸部中央区域
- **人工呼吸**: 必须在头部区域正确位置

## 注意事项

1. 确保XR设置已正确配置（OpenXR、Meta XR SDK）
2. 手部追踪需要在Quest设备上测试
3. 按压检测的精度取决于手部追踪的准确性
4. 建议在Quest设备上进行实际测试和调优
