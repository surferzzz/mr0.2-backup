# 🥽 Quest 3 VR使用指南

## 🔧 问题修复

### 已修复的Quest 3问题：
1. ✅ UI不显示 → UI现在会跟随摄像机
2. ✅ 手穿模（没有碰撞）→ 自动添加手部碰撞器

## 🚀 完整设置步骤

### 第一步：在Unity中设置系统

#### 方法1：使用EasySetup（推荐）

```
1. Hierarchy → 右键 → Create Empty
2. 命名为 "CPRSetup"
3. Add Component → "EasySetup"
4. 确保勾选：
   ✓ Create Patient
   ✓ Create Managers
   ✓ Create UI
   ✓ Setup VR Hands     ← 重要！Quest必须
5. 右键EasySetup → "🚀 一键设置CPR系统"
```

#### 方法2：手动设置VR手部

如果已有场景，只需添加手部碰撞：

```
1. 创建空对象 "VRHandSetup"
2. Add Component → "VRHandSetup"
3. 右键组件 → "🤚 设置VR手部碰撞"
```

### 第二步：构建到Quest 3

#### 1. 切换到Android平台
```
File → Build Settings
选择 Android
点击 Switch Platform
```

#### 2. 配置项目设置
```
Edit → Project Settings → Player

Company Name: 填写您的公司名
Product Name: CPR Training

Other Settings:
  - Package Name: com.yourcompany.cprtraining
  - Minimum API Level: Android 10.0 (API 29)
  - Target API Level: Android 12.0 (API 31)
```

#### 3. XR设置
```
Edit → Project Settings → XR Plug-in Management
勾选 Android 标签页下的:
  ✓ Oculus
```

#### 4. 质量设置（优化性能）
```
Edit → Project Settings → Quality
选择 Medium 或 Low
```

#### 5. 构建APK
```
File → Build Settings
点击 "Build And Run"
或 "Build" 然后手动安装到Quest
```

### 第三步：在Quest 3上运行

#### 1. 准备设备
```
- 确保Quest 3已开启开发者模式
- 使用USB-C线连接到电脑
- 在Quest中允许USB调试
```

#### 2. 运行应用
```
- 如果使用 Build And Run，应用会自动启动
- 或在Quest的"未知来源"中找到应用
```

#### 3. 开始训练
```
1. 戴上Quest 3头显
2. 确保手部跟踪正常
3. 寻找患者模型（应该在您前方）
4. 寻找UI面板（应该在视野中央偏上）
5. 用手按压绿色的胸部区域
6. 观察UI数据变化
```

## 🎯 Quest 3特别说明

### UI显示位置
- UI会跟随您的头部
- 始终显示在视野前方2米处
- 如果看不到UI，转动头部寻找

### 手部碰撞工作原理

系统会自动在您的手部添加：
1. **SphereCollider** - 球形碰撞体（半径8cm）
2. **Rigidbody** - 刚体组件
3. **VRHandCollider** - 碰撞检测脚本

这样您的手就能：
- ✅ 触碰并按压胸部
- ✅ 产生物理碰撞
- ✅ 触发按压检测
- ✅ 记录按压数据

### 手部模式设置

在`VRHandSetup`组件中：

#### 物理手部模式（默认）
```
Use Physical Hands: ✓
- 手会感受到阻力
- 可以推动物体
- 更真实的物理反馈
```

#### 穿透模式
```
Use Physical Hands: ✗
- 手会直接穿过物体
- 但仍然能检测按压
- 适合简化操作
```

## 🎮 Quest 3使用技巧

### 技巧1：找到UI
如果看不到UI：
```
1. 抬头向上看
2. UI应该在您视野中央偏上方
3. 或查看Console日志确认UI已创建
```

### 技巧2：正确的按压姿势
```
1. 找到绿色的胸部区域
2. 用手掌或手指触碰
3. 向下按压2-6cm
4. 保持每分钟100-120次的频率
```

### 技巧3：查看按压效果
```
观察UI上的数据：
- 当前深度：实时显示按压深度
- 质量指示器：颜色会变化
  🔴 红色 = 太浅
  🟢 绿色 = 良好
  🟠 橙色 = 太深
```

### 技巧4：重新定位患者
如果患者位置不合适：
```
在Unity中修改 EasySetup 的:
Patient Position: (X, Y, Z)
例如：(0, 0, 1.5) 让患者更近
```

## 🐛 Quest 3常见问题

### Q1: UI不显示？

**原因**：UI位置不在视野内

**解决**：
```csharp
// 修改 SimplePressUI.cs 中的位置
canvasGO.transform.localPosition = new Vector3(0, 0, 2f);
// 调整第三个数字（2f）改变距离
```

### Q2: 手穿过胸部？

**原因**：手部没有碰撞器

**解决**：
```
1. 确保运行了 EasySetup 的 "一键设置"
2. 或右键 EasySetup → "🤚 单独设置VR手部"
3. 检查 Hierarchy 中是否有:
   - LeftHandAnchor/LeftHandCollider
   - RightHandAnchor/RightHandCollider
```

### Q3: 按压没有反应？

**检查清单**：
```
✓ ChestArea 有 Collider 和 Rigidbody
✓ 手部有 Collider 和 Rigidbody
✓ PressDataManager 存在
✓ SimplePressUI 存在
✓ 按压深度足够（>2cm）
```

### Q4: 性能问题/卡顿？

**优化方案**：
```
1. 降低质量设置 (Quality → Low)
2. 禁用不必要的特效
3. 减少场景中的物体数量
4. 使用简化的患者模型
```

### Q5: 手部跟踪不准？

**改善方法**：
```
1. 确保良好的光线环境
2. 保持手在Quest摄像头视野内
3. 避免快速移动手部
4. 检查Quest设置中的手部跟踪
```

## 🎯 性能优化建议

### Quest 3优化设置

```
1. 目标帧率: 72 FPS（Quest 3支持90/120fps）
2. 渲染分辨率: 1.2x
3. 抗锯齿: MSAA 2x（不要用4x）
4. 阴影质量: Medium
5. 光照: Baked（预烘焙）
```

### 代码优化

```csharp
// 在SimplePressUI中调整更新频率
yield return new WaitForSeconds(0.2f); // 从0.1改为0.2
// 降低UI更新频率可以提升性能
```

## 📊 调试Quest问题

### 使用Quest Link
```
1. 连接Quest到电脑
2. 启用Quest Link
3. 在Unity中按播放
4. 可以在Unity Console中看到日志
```

### 查看设备日志
```
使用 adb logcat 查看设备日志
adb logcat -s Unity
```

### 测试检查点
```
✓ 场景能启动
✓ 能看到患者模型
✓ 能看到UI面板
✓ 手能触碰物体
✓ 按压时UI数据更新
✓ Console有碰撞日志
```

## 🎨 自定义Quest体验

### 调整UI大小
```csharp
// 在 SimplePressUI 中
float uiScale = 0.008f; // 增大UI
// 或
float uiScale = 0.003f; // 缩小UI
```

### 调整手部碰撞大小
```csharp
// 在 VRHandSetup 中
float handColliderRadius = 0.1f; // 更大的碰撞范围
```

### 添加触觉反馈
```csharp
// 当按压时震动手柄
// OVRInput.SetControllerVibration(0.5f, 0.5f, OVRInput.Controller.Touch);
```

## 📝 部署检查清单

部署到Quest 3前确认：

- [ ] Android平台已切换
- [ ] Oculus XR插件已启用
- [ ] 开发者模式已开启
- [ ] USB调试已允许
- [ ] EasySetup已运行（包括VR手部设置）
- [ ] 场景中有患者模型
- [ ] 场景中有SystemManager
- [ ] 场景中有SimplePressUI
- [ ] 场景中有VRHandSetup
- [ ] 构建设置中场景已添加

## 🎉 成功标志

在Quest 3中运行成功的标志：

✅ 能看到VR环境  
✅ 能看到患者模型（躺在地上）  
✅ 能看到UI面板（显示数据）  
✅ 手部有白色/半透明的碰撞球  
✅ 手触碰胸部时有阻力  
✅ 按压时UI数字变化  
✅ 质量指示器颜色变化  

---

**祝您在Quest 3上体验愉快！** 🎮

如有问题，请在Unity Console中查看详细日志。

