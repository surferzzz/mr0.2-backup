# 🚀 从这里开始 - CPR训练系统

## 📁 文档导航

根据您的情况选择对应文档：

### 🔥 快速开始（推荐新手）
- **`README_解决Quest3问题.md`** ⭐⭐⭐⭐⭐
  - **最新！针对您当前遇到的问题**
  - 3步解决Console错误
  - Quest 3 UI不显示问题
  - 手穿过胸部问题
  - **强烈推荐先看这个！**

### 🎮 设备特定指南
- **`README_Quest3使用指南.md`**
  - Quest 3完整使用流程
  - VR手部设置详解
  - Build到设备步骤

- **`README_Quest3错误修复.md`**
  - 详细的错误分析
  - 多种修复方案
  - 完整的FAQ

### 📖 基础使用
- **`README_超简单使用.md`**
  - 最简单的设置方法
  - EasySetup使用指南
  - 右键菜单命令

- **`README_快速开始.md`**
  - 5分钟快速上手
  - 基础概念介绍

### 📚 完整文档
- **`README_VR心肺复苏系统完整指南.md`**
  - 完整的系统架构
  - 所有组件详解
  - 高级自定义

- **`README_按压系统使用指南.md`**
  - 按压检测原理
  - 数据管理系统

---

## ⚡ 3秒决策树

```
你是Quest 3用户 + Console有错误？
    → README_解决Quest3问题.md ⭐

你是新手，想快速开始？
    → README_超简单使用.md

你用Quest 3，没有错误？
    → README_Quest3使用指南.md

你想深入了解系统？
    → README_VR心肺复苏系统完整指南.md
```

---

## 🎯 核心脚本说明

### 必须使用的脚本

1. **EasySetup.cs** ⭐⭐⭐⭐⭐
   - 一键设置整个CPR系统
   - 右键菜单操作，超级简单
   - **必须先运行这个！**

2. **QuickErrorFix.cs** ⭐⭐⭐⭐⭐（新！）
   - 修复Quest 3常见问题
   - 解决Console错误
   - 确保UI显示和手部碰撞
   - **有问题就用这个！**

### 核心功能脚本

3. **SimpleMetaXRChest.cs**
   - 胸部按压检测
   - 自动附加到患者模型

4. **SimplePressUI.cs**
   - UI显示系统
   - 自动创建和定位

5. **PressDataManager.cs**
   - 数据管理单例
   - 记录所有按压数据

6. **VRHandSetup.cs**
   - VR手部自动设置
   - 添加碰撞器

### 高级脚本（可选）

7. **CPRTrainingManager.cs**
   - 训练流程管理
   - 场景切换

8. **ScenarioManager.cs**
   - 多场景支持
   - 车祸/地震等场景

9. **MetaXRGestureDetector.cs**
   - 手势识别
   - 高级交互

---

## 🚀 5分钟快速启动

### 第1步：基础设置
```
1. Hierarchy → Create Empty → "CPRSetup"
2. Add Component → "EasySetup"
3. 右键 → "🚀 一键设置CPR系统"
```

### 第2步：修复Quest 3（如果需要）
```
1. Add Component → "QuickErrorFix"（添加到CPRSetup）
2. 右键 → "🔧 修复所有Quest 3问题"
```

### 第3步：测试
```
Unity编辑器：
  右键 EasySetup → "🎾 创建测试球体"
  点击 Play ▶️

Quest 3设备：
  Build And Run → 戴上头显 → 用手触碰胸部
```

---

## 🆘 遇到问题？

### Console有大量红色错误
→ **立即查看：`README_解决Quest3问题.md`**

### Quest 3看不到UI
→ **立即查看：`README_解决Quest3问题.md`**

### 手穿过胸部
→ **立即查看：`README_解决Quest3问题.md`**

### 不知道怎么开始
→ **查看：`README_超简单使用.md`**

### 编译错误
→ 安装 Meta XR SDK
→ Window → Package Manager

---

## 📊 系统组件结构

```
CPR训练系统
│
├── 患者模型 (Patient)
│   ├── 身体 (Body)
│   ├── 头部 (Head)
│   └── 胸部区域 (ChestArea) ← 按压检测
│
├── 系统管理器 (SystemManager)
│   ├── PressDataManager ← 数据管理
│   ├── CPRTrainingManager ← 训练流程
│   ├── ScenarioManager ← 场景管理
│   └── MetaXRGestureDetector ← 手势检测
│
├── UI系统 (SimplePressUI)
│   └── Canvas ← 自动跟随摄像机
│
└── VR手部 (VRHandSetup)
    ├── LeftHandCollider ← 左手碰撞
    └── RightHandCollider ← 右手碰撞
```

---

## ✅ 检查清单

在Build到Quest 3之前确认：

```
基础设置：
☐ 运行了 EasySetup → 一键设置
☐ Hierarchy中有 Patient 对象
☐ Hierarchy中有 SystemManager 对象
☐ Hierarchy中有 SimplePressUI 对象

Quest 3修复（如有错误）：
☐ 运行了 QuickErrorFix → 修复问题
☐ 运行了 QuickErrorFix → 诊断问题
☐ 诊断结果全是 ✅

Build设置：
☐ Platform = Android
☐ XR Plugin = Oculus
☐ Hand Tracking = Controllers and Hands
☐ Target Device = Quest 3
```

---

## 🎊 准备好了？

### 开发者模式
1. Quest 3开启开发者模式
2. USB连接到电脑
3. 允许调试

### Build测试
1. File → Build Settings
2. Build And Run
3. 戴上头显测试

### 享受训练！
- 看到UI ✅
- 触碰胸部 ✅
- 数据更新 ✅
- 开始CPR训练 🚑💚

---

## 📞 需要帮助？

1. **先看** `README_解决Quest3问题.md`
2. **然后看** 对应的详细文档
3. **使用** QuickErrorFix 诊断问题
4. **检查** Console的详细日志

---

## 🌟 推荐阅读顺序

```
第一次使用：
1️⃣ README_START_HERE.md (当前文档)
2️⃣ README_解决Quest3问题.md
3️⃣ README_超简单使用.md

有Quest 3设备：
1️⃣ README_解决Quest3问题.md
2️⃣ README_Quest3使用指南.md
3️⃣ README_Quest3错误修复.md (遇到问题时)

深入学习：
1️⃣ README_快速开始.md
2️⃣ README_VR心肺复苏系统完整指南.md
3️⃣ README_按压系统使用指南.md
```

---

## 💡 最后的提示

- ✅ **先运行 EasySetup，再用 QuickErrorFix**
- ✅ **Console错误大多可以忽略**
- ✅ **右键菜单是你的好朋友**
- ✅ **遇到问题先诊断，再修复**
- ✅ **保持耐心，系统很强大！**

**祝您CPR训练开发顺利！** 🚀🎮💚


