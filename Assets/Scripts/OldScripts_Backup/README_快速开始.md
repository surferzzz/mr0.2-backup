# 🚀 CPR训练系统 - 快速开始指南

## ✅ 已修复的问题

1. ✅ 修复了 `normalImpulse` 编译错误
2. ✅ 修复了字段未赋值警告
3. ✅ 创建了简化版UI系统 (`SimplePressUI`)
4. ✅ 所有脚本编译通过

## 🎯 三步快速设置

### 第一步：创建设置对象

1. 在Unity Hierarchy窗口中右键
2. 选择 `Create Empty`
3. 命名为 `CPRSetup`
4. 在Inspector中点击 `Add Component`
5. 搜索并添加 `QuickSetupCPR`

### 第二步：运行快速设置

1. 在Inspector中找到 `QuickSetupCPR` 组件
2. 右键点击组件标题
3. 选择 `🚀 快速设置CPR系统`
4. 等待Console显示 "✅ CPR训练系统设置完成！"

### 第三步：测试系统

1. 点击Unity顶部的播放按钮 ▶️
2. 在Scene视图中查看生成的内容：
   - Patient（患者模型）
   - ChestArea（绿色半透明的胸部区域）
   - SimplePressUI_Canvas（UI界面）
3. 尝试用鼠标点击ChestArea进行测试

## 🎨 UI功能说明

### SimplePressUI 特点

✅ **自动创建** - 无需手动设置UI元素  
✅ **实时更新** - 10fps流畅刷新  
✅ **清晰显示** - 大字体，易于阅读  
✅ **质量指示** - 颜色反馈按压质量  

### UI显示内容

```
CPR训练系统
━━━━━━━━━━━━━━━━━
状态: 正在按压...

总按压次数: 5
有效按压: 3
当前深度: 0.045m
最大深度: 0.052m
按压质量: 良好

[━━━━━━━━━] 深度滑块

● 按压质量: 良好
```

### 质量指示颜色

- 🔴 **红色** - 按压太浅 (< 2cm)
- 🟢 **绿色** - 按压良好 (2-6cm)
- 🟠 **橙色** - 按压过深 (> 6cm)
- ⚫ **灰色** - 未检测到按压

## 🎮 测试方法

### 方法1：Scene视图测试

1. 确保在Scene视图中
2. 用鼠标点击绿色的ChestArea
3. 观察UI数据变化
4. 查看Console日志

### 方法2：添加测试物体

```csharp
// 创建一个球体作为"手"
1. GameObject → 3D Object → Sphere
2. 命名为 "TestHand"
3. 添加 Rigidbody 组件
4. Position: (0, 2, 2) - 在患者上方
5. 运行场景，球体会掉落并触发按压
```

### 方法3：使用VR手柄

如果您有Meta Quest设备：

1. 确保场景中有 Camera Rig
2. 连接Quest设备
3. Build并运行到设备
4. 用手柄触碰ChestArea

## 📊 查看系统状态

在Inspector中右键点击 `QuickSetupCPR`，选择：

- **📊 显示系统状态** - 检查所有组件是否正确设置
- **🎮 开始训练** - 启动CPR训练流程
- **🧹 清理生成的对象** - 删除所有自动生成的对象

## 🔧 调整参数

### 胸部按压参数

在 `ChestArea` 的 `SimpleMetaXRChest` 组件中：

```
Max Press Depth: 0.06       // 最大按压深度 (6cm)
Recovery Speed: 5           // 恢复速度
Press Force Multiplier: 1   // 力度倍数
Min Press Force: 0.1        // 最小有效力度
```

### UI位置和大小

在 `SimplePressUI` 组件中：

```
UI Position: (0, 2, 3)      // UI在世界空间的位置
UI Scale: 0.005             // UI缩放大小
Auto Create UI: ✓           // 自动创建UI
```

### 数据管理参数

在 `SystemManager` 的 `PressDataManager` 组件中：

```
Min Valid Depth: 0.02       // 最小有效深度 (2cm)
Min Valid Duration: 0.5     // 最小有效时长 (0.5秒)
Max Valid Duration: 3.0     // 最大有效时长 (3秒)
```

## 🎯 常见问题

### Q: UI不显示？

**解决方案**：
1. 检查Scene视图中是否有 `SimplePressUI_Canvas`
2. 调整UI Position，确保在摄像机视野内
3. 运行 `📊 显示系统状态` 检查UI管理器

### Q: 按压没有反应？

**解决方案**：
1. 确保ChestArea有Collider和Rigidbody
2. 检查测试物体是否有Rigidbody
3. 查看Console是否有碰撞日志
4. 确保物体名称包含 "hand" 或 "controller"

### Q: 数据不更新？

**解决方案**：
1. 确保PressDataManager存在
2. 检查SimplePressUI是否正在运行
3. 查看Console是否有错误信息

### Q: 如何重置数据？

**方法1**：
```csharp
// 在Console中输入
PressDataManager.Instance.ResetSession();
```

**方法2**：
- 停止并重新运行场景

## 🎨 自定义UI

如果您想修改UI外观，编辑 `SimplePressUI.cs`：

```csharp
// 修改UI位置
uiPosition = new Vector3(0, 2f, 3f);

// 修改UI大小
uiScale = 0.005f;

// 修改面板颜色
image.color = new Color(0, 0, 0, 0.8f);

// 修改文字大小
titleText.fontSize = 32;
```

## 📱 部署到Quest 3

### 构建设置

1. `File → Build Settings`
2. 选择 `Android` 平台
3. 点击 `Switch Platform`
4. 添加当前场景
5. 点击 `Build And Run`

### 优化建议

- 降低UI分辨率
- 减少粒子效果
- 使用LOD系统
- 启用动态批处理

## 🎓 下一步学习

1. **添加场景** - 使用ScenarioManager创建车祸/地震场景
2. **语音指导** - 集成TTS系统
3. **多人模式** - 添加网络同步
4. **数据分析** - 导出训练报告

## 📞 获取帮助

如果遇到问题：

1. 检查Console窗口的错误信息
2. 运行 `📊 显示系统状态`
3. 查看完整文档：`README_VR心肺复苏系统完整指南.md`

---

**祝您使用愉快！** 🎉

如有问题，请查看Console日志或参考完整文档。

