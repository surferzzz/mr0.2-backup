# Quest 加载卡住问题 - 已修复项与额外建议

## 已实施的修复

1. **Android 图形 API：Vulkan → OpenGLES3**
   - 部分 Quest 在 Vulkan 下会卡在启动画面，已改为 OpenGLES3
   - 若需改回：Edit → Project Settings → Player → Android → Other Settings → Graphics API

2. **PerformanceOptimizer.cs**
   - 为 `Camera.main` 增加空检查，避免 VR 下为 null 时异常
   - 移除 `AudioSettings.SetDSPBufferSize`（Android 上已弃用，可能导致启动卡住）
   - 将优化逻辑延迟一帧执行，等待 VR 相机就绪

2. **Unity 启动画面**
   - 关闭 Unity 默认启动画面（`m_ShowUnitySplashScreen: 0`），减少启动时间与潜在卡顿

## 若仍卡在加载，可尝试

1. **改用 OpenGLES3**
   - Edit → Project Settings → Player → Android → Other Settings
   - 在 Graphics API 中移除 Vulkan，仅保留 OpenGLES3（部分 Quest 在 Vulkan 下会卡住）

2. **关闭 Development Build**
   - File → Build Settings → 取消勾选 Development Build
   - Development Build + Vulkan 在某些 Quest 上会导致加载卡住

3. **查看 adb 日志**
   - 连接 Quest 后运行：`adb logcat -s Unity`
   - 查看是否有崩溃或初始化错误

4. **XR 插件冲突**
   - 若同时使用 Oculus XR 和 OpenXR，建议仅保留一种
   - Window → Package Manager 中检查并移除冗余 XR 包
