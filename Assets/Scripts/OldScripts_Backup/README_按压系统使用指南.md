# 胸外按压训练系统 - 使用指南

## 📋 系统概述

这是一个完整的Unity VR胸外按压训练系统，支持Meta XR设备，可以实时检测和统计按压操作的次数、深度等数据，并提供直观的UI界面显示。

## 🎯 主要功能

- ✅ **实时按压检测** - 检测手部与胸部模型的交互
- ✅ **按压深度测量** - 精确测量按压深度
- ✅ **按压次数统计** - 自动统计总按压次数和有效按压次数
- ✅ **实时UI显示** - 显示训练数据和按压质量
- ✅ **数据保存** - 支持训练数据的保存和导出
- ✅ **XR手部跟踪** - 支持Meta Quest手部跟踪和控制器
- ✅ **按压质量评估** - 实时评估按压深度是否合适

## 📦 核心组件

### 1. PressDataManager (数据管理器)
- 负责统计和管理所有按压数据
- 实时计算按压次数、平均深度、最大深度等
- 支持数据保存到JSON文件

### 2. PressUIManager (UI管理器)
- 自动创建和管理训练界面
- 实时显示训练数据
- 提供按压质量的视觉反馈
- 包含重置、保存等控制按钮

### 3. SimpleMetaXRChest (胸部控制器)
- 检测手部与胸部的物理碰撞
- 计算按压深度和力度
- 提供胸部变形的视觉反馈

### 4. MetaXRGestureDetector (手势检测器)
- 增强的手部跟踪和手势识别
- 支持Meta XR手部跟踪
- 提供更精确的按压检测

### 5. PressSystemSetup (系统设置器)
- 一键自动设置整个系统
- 自动查找和连接所需组件
- 系统验证和测试功能

## 🚀 快速开始

### 方法一：自动设置（推荐）

1. **添加设置组件**
   ```
   在场景中创建空GameObject，命名为"PressSystemManager"
   添加 PressSystemSetup 脚本
   ```

2. **运行自动设置**
   ```
   在Inspector中点击 PressSystemSetup 组件
   确保 "Auto Setup On Start" 已勾选
   运行场景，系统会自动设置所有组件
   ```

3. **手动触发设置**（可选）
   ```
   在Inspector中右键点击 PressSystemSetup 组件
   选择 "设置按压系统"
   ```

### 方法二：手动设置

1. **创建数据管理器**
   ```
   创建空GameObject，命名为"PressDataManager"
   添加 PressDataManager 脚本
   ```

2. **创建UI管理器**
   ```
   创建空GameObject，命名为"PressUIManager"
   添加 PressUIManager 脚本
   ```

3. **设置胸部模型**
   ```
   创建或导入胸部3D模型
   添加 SimpleMetaXRChest 脚本
   确保有Collider组件用于碰撞检测
   ```

4. **添加手势检测**
   ```
   创建空GameObject，命名为"MetaXRGestureDetector"
   添加 MetaXRGestureDetector 脚本
   在Inspector中设置手部和胸部引用
   ```

## ⚙️ 配置说明

### PressDataManager 配置
```
Min Valid Depth: 0.02f        // 最小有效按压深度
Min Valid Duration: 0.5f       // 最小有效按压时长
Max Valid Duration: 3.0f       // 最大有效按压时长
```

### SimpleMetaXRChest 配置
```
Max Press Depth: 0.05f         // 最大按压深度
Recovery Speed: 5f             // 恢复速度
Press Force Multiplier: 1f     // 按压力度倍数
```

### MetaXRGestureDetector 配置
```
Press Detection Distance: 0.1f  // 按压检测距离
Press Force Threshold: 0.5f     // 按压力度阈值
Show Debug Info: true           // 显示调试信息
```

### PressUIManager 配置
```
Max Display Depth: 0.1f        // UI显示的最大深度
Auto Create UI: true           // 自动创建UI
UI World Position: (0, 2, 2)   // UI在世界空间的位置
UI Scale: (0.01, 0.01, 0.01)   // UI缩放
```

## 🎮 使用方法

### 基本操作
1. **开始训练** - 运行场景后系统自动开始
2. **进行按压** - 用手部（或控制器）按压胸部模型
3. **查看数据** - 实时查看UI界面上的统计数据
4. **重置数据** - 点击UI上的"重置数据"按钮
5. **保存数据** - 点击UI上的"保存数据"按钮

### UI界面说明
- **总按压次数** - 所有按压操作的总数
- **有效按压** - 符合深度和时长要求的按压次数
- **当前深度** - 实时显示当前按压深度
- **最大深度** - 本次训练中的最大按压深度
- **平均深度** - 有效按压的平均深度
- **训练时长** - 当前训练会话的总时长
- **深度可视化** - 实时显示按压深度的进度条
- **按压质量** - 实时评估当前按压质量（太浅/良好/太深）

### 按压质量标准
- 🔴 **太浅** - 深度 < 0.02m
- 🟢 **良好** - 深度 0.02m - 0.06m
- 🟠 **太深** - 深度 > 0.06m

## 📊 数据导出

系统会自动将训练数据保存为JSON格式：
```json
{
  "sessionStart": "2024-01-01T10:00:00",
  "totalDuration": 120.5,
  "validPressCount": 15,
  "averageDepth": 0.035,
  "maxDepth": 0.048,
  "presses": [
    {
      "depth": 0.035,
      "duration": 1.2,
      "timestamp": "2024-01-01T10:00:05",
      "isValidPress": true
    }
  ]
}
```

文件保存位置：`Application.persistentDataPath/PressSession_YYYYMMDD_HHMMSS.json`

## 🔧 故障排除

### 常见问题

1. **手部检测不工作**
   - 确保Meta XR SDK已正确导入
   - 检查OVRCameraRig是否存在于场景中
   - 确认手部跟踪权限已开启

2. **UI不显示**
   - 检查PressUIManager的"Auto Create UI"是否已勾选
   - 确认UI位置设置是否合理
   - 检查Canvas是否被正确创建

3. **按压检测不准确**
   - 调整SimpleMetaXRChest的碰撞器大小
   - 检查胸部模型是否有Rigidbody组件
   - 调整按压检测阈值参数

4. **数据不统计**
   - 确保PressDataManager存在于场景中
   - 检查SimpleMetaXRChest是否正确调用数据管理器
   - 查看Console是否有错误信息

### 调试技巧

1. **启用调试日志**
   ```csharp
   // 在各个组件中都有调试选项
   Show Debug Info = true
   Show Setup Log = true
   ```

2. **使用Scene视图调试**
   - 在Scene视图中可以看到Gizmos绘制的调试信息
   - 绿线表示正常状态，红线表示按压状态

3. **检查组件连接**
   ```
   使用PressSystemSetup的"验证系统设置"功能
   右键点击组件 -> "测试系统"
   ```

## 🎯 自定义扩展

### 添加新的按压质量标准
```csharp
// 在PressUIManager.UpdateQualityIndicator()中修改
if (currentDepth < 0.015f) // 自定义阈值
{
    pressQualityIndicator.color = Color.red;
    pressQualityText.text = "按压质量: 过浅";
}
```

### 添加音频反馈
```csharp
// 在PressDataManager.OnPressCompleted事件中添加
public AudioSource audioSource;
public AudioClip validPressSound;
public AudioClip invalidPressSound;

void OnPressCompleted(PressData pressData)
{
    if (pressData.isValidPress)
        audioSource.PlayOneShot(validPressSound);
    else
        audioSource.PlayOneShot(invalidPressSound);
}
```

### 添加触觉反馈
```csharp
// 在MetaXRGestureDetector.OnHandPressStart()中
OVRInput.SetControllerVibration(0.5f, 0.5f, OVRInput.Controller.Touch);
```

## 📝 版本信息

- **版本**: 1.0.0
- **Unity版本**: 2022.3 LTS或更高
- **Meta XR SDK**: 最新版本
- **支持平台**: Meta Quest 2/3/Pro

## 🤝 技术支持

如果遇到问题，请检查：
1. Unity Console中的错误信息
2. 组件Inspector中的配置
3. Meta XR SDK是否正确安装
4. 设备权限是否正确设置

---

**祝您训练愉快！** 🎉

