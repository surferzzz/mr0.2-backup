# CPR触觉反馈模拟系统

## 概述
使用Quest 3手部追踪 + bHaptics TactGlove实现CPR心肺复苏模拟训练。
基于Meta XR Interaction SDK的PokeExamples场景构建。

## 医学标准
- **按压深度**: 5-6 cm
- **按压速率**: 100-120 次/分钟

## 文件说明

| 文件 | 功能 |
|------|------|
| `CPRPressDetector.cs` | 核心检测器，支持碰撞检测和Transform追踪两种模式 |
| `GloveHapticController.cs` | bHaptics TactGlove震动反馈控制 |
| `HandTrackingAdapter.cs` | Meta XR SDK手部追踪适配器 |
| `CPRSimpleUI.cs` | 实时深度/速率UI显示 |
| `Editor/CPRSimpleSetup.cs` | 编辑器快速设置工具 |

## 快速开始

### 1. 打开场景
`Assets/Scenes/CPRSimulation.unity`

或使用菜单：`Tools > CPR > 打开CPR模拟场景`

### 2. 使用设置工具
打开菜单 `Tools > CPR > 快速设置场景`

按顺序点击：
1. **创建胸部检测点** - 会创建带碰撞检测的检测器
2. **添加触觉反馈控制器** - 连接bHaptics手套
3. **添加手部追踪适配器** - 自动查找OVR手部组件
4. **添加bHaptics SDK2** - 初始化触觉反馈

### 3. 配置预处理符号
`Edit > Project Settings > Player > Scripting Define Symbols`

添加：**BHAPTICS_SDK2**

### 4. 调整检测位置
将 `CPR_ChestDetector` 对象移动到虚拟病人的胸骨位置

## 检测模式

### 碰撞检测模式（推荐）
- `useCollisionDetection = true`
- 使用BoxCollider触发器检测手部进入
- 适合Meta XR SDK的Poke交互系统
- 自动识别手部碰撞体

### Transform追踪模式
- `useCollisionDetection = false`
- 直接追踪手掌Transform的Y坐标
- 需要通过HandTrackingAdapter提供手掌Transform

## 触觉反馈策略

| 按压状态 | 震动强度 | 效果 |
|---------|---------|------|
| 深度 < 5cm | 30% | 提示加深按压 |
| 深度 5-6cm | 60% | 正反馈，保持 |
| 深度 > 6cm | 95% | 警告力度过大 |
| 速率过慢 | 节奏脉冲 | 引导加快 |

## 场景层级结构

```
CPRSimulation/
├── OVRCameraRig/           # Meta XR相机
│   └── OVRHandPrefab_L/R   # 手部追踪
├── CPR_ChestDetector/      # 胸部检测器
│   ├── CPRPressDetector    # 按压检测
│   ├── GloveHapticController # 触觉反馈
│   └── HandTrackingAdapter # 手部适配
├── BhapticsSDK2/           # bHaptics初始化
└── Patient/                # 虚拟病人模型
```

## bHaptics配置

1. 访问 [bHaptics Developer Portal](https://developer.bhaptics.com)
2. 创建应用获取 AppId
3. 在Unity中: `Window > bHaptics > Settings`
4. 填入 AppId

## 测试

### 编辑器测试
- `GloveHapticController` 组件有右键菜单测试选项
- 无bHaptics设备时输出模拟日志

### 设备测试
1. 连接Quest 3和TactGlove
2. 运行场景
3. 手部接近胸部检测区域即可触发
