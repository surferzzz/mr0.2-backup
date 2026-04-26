# 场景诊断和错误排查指南

## 我已经完成的工作

1. ✅ 创建了 `SceneDiagnostics.cs` 诊断工具
2. ✅ 将诊断工具添加到场景中
3. ✅ 配置为启动时自动运行

## 请你在Unity中执行以下步骤

### 方法1：自动诊断（推荐）

1. **打开Unity编辑器**
2. **加载场景**: `Assets/Scenes/EarthquakeTraining.unity`
3. **按Play按钮**（或按F5）
4. **查看Console窗口**（Window → General → Console）
5. **复制所有错误信息**并告诉我

### 方法2：手动诊断

1. **在Hierarchy中找到** `SceneDiagnostics` GameObject
2. **在Inspector中右键点击** `SceneDiagnostics` 组件
3. **选择** `运行诊断` 或 `Run Diagnostics`
4. **查看Console输出**

### 方法3：查看具体错误

如果Console中有红色错误信息：

1. **点击错误消息**
2. **查看完整的错误堆栈**
3. **截图或复制错误文本**
4. **告诉我以下信息**：
   - 错误消息内容
   - 哪个脚本出错
   - 哪一行代码
   - 错误类型（编译错误/运行时错误）

## 常见问题和解决方案

### 如果是编译错误

**症状**: Unity无法进入Play模式，底部有红色错误提示

**可能原因**:
- 脚本语法错误
- 缺少using声明
- 命名空间问题
- 引用类型不存在

**查看方式**:
1. 打开Console窗口
2. 双击错误消息
3. 会自动打开出错的脚本文件
4. 告诉我具体的错误信息

### 如果是运行时错误

**症状**: 可以进入Play模式，但Console有错误

**可能原因**:
- GameObject未找到
- Component引用为null
- 方法调用失败

**查看方式**:
1. 进入Play模式
2. 查看Console错误
3. 点击错误查看堆栈跟踪
4. 告诉我错误详情

### 如果是引用缺失

**症状**: Console显示 "NullReferenceException" 或 "Object reference not set"

**可能原因**:
- Inspector中的引用未设置
- GameObject未创建
- 组件未添加

**解决方式**:
1. 运行诊断工具（会自动检测）
2. 根据诊断报告手动设置缺失的引用

## 我需要的信息

请复制并发送给我：

1. **错误类型**: 编译错误 / 运行时错误 / 警告
2. **错误数量**: 有多少个错误
3. **错误消息**: 完整的错误文本（前5个就够了）
4. **堆栈跟踪**: 如果有的话

例如：
```
错误类型: 编译错误
错误数量: 3个

错误1:
Assets/Scripts/Core/EarthquakeSceneController.cs(13,16): error CS0246:
The type or namespace name 'EarthquakeController' could not be found

错误2:
...
```

## 临时解决方案

如果错误太多导致无法运行，可以先：

### 选项1：禁用问题脚本
1. 在Hierarchy中找到有问题的GameObject
2. 取消勾选Component前的复选框
3. 尝试再次运行

### 选项2：使用简化场景
我可以帮你创建一个最小化的测试场景，只包含基本功能。

### 选项3：清理并重建
1. 关闭Unity
2. 删除 `Library` 文件夹
3. 重新打开Unity（会重新导入所有资源）

## 诊断报告格式

如果诊断工具成功运行，你会看到类似这样的输出：

```
===== 场景诊断开始 =====

--- 检查摄像机 ---
✓ 找到主摄像机: Main Camera
  位置: (0.0, 1.6, 0.0)
  旋转: (0.0, 0.0, 0.0)
场景中共有 1 个摄像机

--- 检查地震控制器 ---
✓ 找到EarthquakeController: EarthquakeController
✓ targetCamera已设置: Main Camera
  持续时间: 8秒
  受影响的刚体数量: 4

--- 检查UI系统 ---
场景中共有 1 个Canvas
  Canvas: EarthquakeUI, 渲染模式: WorldSpace
场景中共有 1 个Button
✓ EventSystem存在

--- 检查物理系统 ---
场景中共有 4 个Rigidbody
  激活的刚体: 4

--- 检查脚本组件 ---
✓ 找到EarthquakeSceneController: EarthquakeSceneManager
  earthquakeController引用未设置！  <-- 这种是需要修复的问题

===== 场景诊断完成 =====
```

## 下一步

根据你提供的错误信息，我会：
1. 识别问题根源
2. 修复相关脚本
3. 更新场景配置
4. 确保场景可以正常运行

---

**现在请在Unity中执行诊断，并把结果告诉我！**
