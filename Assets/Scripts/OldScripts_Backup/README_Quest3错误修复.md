# Quest 3 错误修复指南

## 🚨 您看到的错误

```
NullReferenceException: Object reference not set to an instance of an object
Oculus.Interaction.UniqueIdentifier.Release
Oculus.Interaction.PoseDetection.HmdOffset.OnDisable
```

## ❓ 错误原因

这些错误来自 **Meta XR SDK 内部组件**，通常在以下情况发生：
1. ✅ Meta XR SDK已安装（**这是好事！**）
2. ⚠️ 场景中有Meta XR组件但未完全配置
3. ⚠️ 某些组件在运行时试图清理资源但引用丢失

**重要：这些错误不会影响您的CPR系统功能！**

---

## 🔧 快速修复方法

### 方法1：使用快速修复工具（推荐）⚡

```
1. Hierarchy → 找到任何GameObject（或创建新的）
2. Add Component → "QuickErrorFix"
3. 右键 QuickErrorFix → "🔧 修复所有Quest 3问题"
4. 等待完成 ✅
```

这会自动：
- ✅ 检测并处理有问题的组件
- ✅ 确保VR手部碰撞器正确
- ✅ 确保UI可见
- ✅ 确保胸部检测工作

### 方法2：诊断后针对性修复

```
1. 右键 QuickErrorFix → "📊 诊断当前问题"
2. 查看Console中的诊断报告
3. 根据提示修复每个问题
4. 右键 QuickErrorFix → "🔧 修复所有Quest 3问题"
```

### 方法3：完全重启系统

```
1. 右键 QuickErrorFix → "⚡ 快速重启系统"
2. 等待系统自动清理并重建
3. 检查Console确认成功
```

---

## 🎯 具体问题解决

### 问题1: UI不显示

**症状：** Quest 3中看不到UI界面

**解决方案：**
```
方式A - 自动修复：
1. 右键 QuickErrorFix → "🔧 修复所有Quest 3问题"

方式B - 手动修复：
1. Hierarchy → 找到 "SimplePressUI_Canvas"
2. Inspector → Canvas → Render Mode = "World Space"
3. Transform → Position = (0, 0, 2)
4. 将Canvas拖到Main Camera下作为子对象
5. Transform → Local Position = (0, 0, 2)
```

### 问题2: 手穿过胸部

**症状：** 手部无法触碰胸部，直接穿过

**解决方案：**
```
方式A - 自动修复：
1. 右键 QuickErrorFix → "🔧 修复所有Quest 3问题"

方式B - 手动修复：
1. Hierarchy → 找到 "LeftHandAnchor" 和 "RightHandAnchor"
2. 右键每个手部锚点 → Create Empty
3. 命名为 "LeftHandCollider" / "RightHandCollider"
4. Add Component → "Sphere Collider"
   - Radius = 0.08
5. Add Component → "Rigidbody"
   - Use Gravity = OFF
   - Is Kinematic = OFF
   - Collision Detection = Continuous Dynamic
6. Add Component → "VRHandCollider"
```

### 问题3: NullReferenceException 错误

**症状：** Console中大量红色错误

**解决方案：**
```
方式A - 忽略这些错误（推荐）：
这些错误来自Meta XR SDK内部，不影响CPR系统功能。
您可以安全地忽略它们。

方式B - 清理错误日志：
1. 右键 QuickErrorFix → "🧹 清理错误日志"
2. Console → Clear → ✅

方式C - 更新Meta XR SDK：
1. Window → Package Manager
2. 找到 "Meta XR SDK"
3. 点击 "Update to latest"
```

---

## 📋 完整设置流程（从零开始）

如果您是第一次设置，请按此顺序操作：

### 步骤1: 基础设置
```
1. 创建空GameObject → 命名 "CPRSetup"
2. Add Component → "EasySetup"
3. 右键 EasySetup → "🚀 一键设置CPR系统"
4. 等待完成 ✅
```

### 步骤2: 修复Quest 3问题
```
1. Add Component → "QuickErrorFix"（添加到CPRSetup或任何对象）
2. 右键 QuickErrorFix → "🔧 修复所有Quest 3问题"
3. 等待完成 ✅
```

### 步骤3: 验证设置
```
1. 右键 QuickErrorFix → "📊 诊断当前问题"
2. 查看Console，确保所有项都是 ✅
3. 如果有 ❌，按提示修复
```

### 步骤4: 测试
```
1. 点击 Play ▶️
2. 如果在Unity编辑器：
   - 右键 EasySetup → "🎾 创建测试球体"
   - 观察球体掉落触发按压
3. 如果在Quest 3：
   - Build and Run
   - 用手触碰绿色胸部区域
   - 查看UI更新
```

---

## 🔍 诊断检查清单

使用以下命令检查每个部分：

### 检查VR手部
```
右键 QuickErrorFix → "📊 诊断当前问题"

应该看到：
✅ VR手部锚点存在
✅ 手部碰撞器存在
```

### 检查UI系统
```
Hierarchy → 搜索 "SimplePressUI_Canvas"

应该：
✅ Canvas存在
✅ Canvas → Render Mode = World Space
✅ Canvas是Main Camera的子对象
```

### 检查胸部检测
```
Hierarchy → Patient → ChestArea

应该有：
✅ Box Collider
✅ Rigidbody (Is Kinematic = OFF)
✅ SimpleMetaXRChest 脚本
```

### 检查数据管理
```
Hierarchy → 搜索 "SystemManager"

应该有：
✅ PressDataManager
✅ CPRTrainingManager
✅ ScenarioManager
✅ MetaXRGestureDetector
```

---

## 🎮 Quest 3 Build设置

### Android设置
```
File → Build Settings
1. Platform: Android ✅
2. Switch Platform（如果需要）
3. Add Open Scenes ✅
4. Player Settings...
   - Company Name: 填写您的名字
   - Product Name: CPR Training
   - Minimum API Level: Android 10.0 (API 29)
   - Target API Level: Android 13 (API 33)
5. XR Plug-in Management
   - Android → Oculus ✅
6. Oculus设置
   - Target Devices: Quest 3 ✅
   - Hand Tracking Support: Controllers and Hands ✅
```

### Build步骤
```
1. File → Build Settings
2. Build And Run（Quest 3通过USB连接）
3. 等待Build和安装
4. 戴上头显
5. 查看前方应该有UI
6. 用手触碰胸部测试
```

---

## 💡 常见问题FAQ

### Q1: 为什么有这么多红色错误？
**A:** 这些错误来自Meta XR SDK内部组件，通常是资源清理时的空引用。它们不会影响CPR系统功能，可以安全忽略。

### Q2: 我看不到UI怎么办？
**A:** 运行 `QuickErrorFix → 修复所有Quest 3问题`，它会自动将UI调整到正确位置。

### Q3: 手还是穿过胸部？
**A:** 确保：
1. ✅ 手部有碰撞器（LeftHandCollider / RightHandCollider）
2. ✅ 手部Rigidbody → Is Kinematic = OFF
3. ✅ 胸部有碰撞器和Rigidbody

### Q4: 如何清理并重新开始？
**A:** 
```
方式1: 右键 EasySetup → "🧹 清理所有CPR对象"
方式2: 右键 QuickErrorFix → "⚡ 快速重启系统"
```

### Q5: 可以在Unity编辑器中测试吗？
**A:** 可以！使用测试球体：
```
右键 EasySetup → "🎾 创建测试球体"
点击Play，观察球体掉落触发按压
```

---

## 📞 仍然有问题？

如果以上方法都不能解决您的问题：

1. **完全重置**
   ```
   1. 右键 EasySetup → "🧹 清理所有CPR对象"
   2. 删除 CPRSetup GameObject
   3. 重新创建 CPRSetup
   4. 重新运行 EasySetup
   5. 运行 QuickErrorFix
   ```

2. **检查依赖**
   ```
   Window → Package Manager
   
   确保已安装：
   - Meta XR SDK ✅
   - XR Interaction Toolkit（可选）
   - TextMesh Pro（可选，有替代方案）
   ```

3. **查看详细日志**
   ```
   Console → 右上角菜单 → Show Timestamp ✅
   找到第一个错误发生的时间
   查看那之前发生了什么
   ```

---

## ✅ 成功标志

当一切正常时，您应该看到：

### Unity编辑器中
```
Console:
✅ CPR训练系统设置完成
✅ VR手部碰撞器设置完成
✅ 简化UI创建完成
✅ 所有问题已修复

Hierarchy:
✅ Patient（患者模型）
✅ SystemManager（系统管理器）
✅ SimplePressUI（UI管理器）
✅ SimplePressUI_Canvas（UI画布）
✅ VRHandSetup（手部设置）
```

### Quest 3设备中
```
视野中：
✅ 看到黑色半透明UI面板
✅ UI显示 "CPR训练系统"
✅ 看到患者模型躺在前方

交互时：
✅ 手触碰胸部时有阻力
✅ UI实时更新按压数据
✅ 质量指示器变色（红/黄/绿）
```

---

## 🎊 祝您训练成功！

有任何问题请参考：
- `README_超简单使用.md` - 基础使用
- `README_Quest3使用指南.md` - Quest 3详细指南
- `README_VR心肺复苏系统完整指南.md` - 完整系统文档


