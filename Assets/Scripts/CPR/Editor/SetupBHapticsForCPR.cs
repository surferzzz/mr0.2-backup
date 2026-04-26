using UnityEngine;
using UnityEditor;

namespace CPRSimulation.Editor
{
    /// <summary>
    /// 一键配置 bHaptics TactGlove CPR 震动反馈
    /// </summary>
    public class SetupBHapticsForCPR
    {
        [MenuItem("Tools/CPR/配置 bHaptics 触觉反馈", false, 15)]
        public static void SetupBHaptics()
        {
            var report = "===== bHaptics CPR 震动配置 =====\n\n";
            int setupCount = 0;

            // 1. 查找或创建 BhapticsSDK2（bHaptics 运行时管理器）
            var bhapticsRuntime = Object.FindObjectOfType<Bhaptics.SDK2.BhapticsSDK2>();
            if (bhapticsRuntime == null)
            {
                var go = new GameObject("BhapticsSDK2");
                bhapticsRuntime = go.AddComponent<Bhaptics.SDK2.BhapticsSDK2>();
                Undo.RegisterCreatedObjectUndo(go, "Create BhapticsSDK2");
                report += "✓ 已创建 BhapticsSDK2\n";
                setupCount++;
            }
            else
            {
                report += $"✓ BhapticsSDK2 已存在: {bhapticsRuntime.gameObject.name}\n";
            }

            // 2. 查找 CPR 检测区
            var chestDetector = GameObject.Find("CPR_ChestDetector");
            var pressDetector = chestDetector != null ? chestDetector.GetComponent<CPRPressDetector>() : null;
            
            if (pressDetector == null)
            {
                pressDetector = Object.FindObjectOfType<CPRPressDetector>();
            }

            if (pressDetector == null)
            {
                report += "\n⚠ 未找到 CPRPressDetector\n";
                report += "请先运行游戏，点击开始按钮，让系统创建 CPR 检测区。\n";
                EditorUtility.DisplayDialog("配置 bHaptics", report, "确定");
                return;
            }

            report += $"✓ 找到 CPR 检测器: {pressDetector.gameObject.name}\n\n";

            // 3. 添加 GloveHapticController
            var hapticController = pressDetector.GetComponent<GloveHapticController>();
            if (hapticController == null)
            {
                hapticController = Undo.AddComponent<GloveHapticController>(pressDetector.gameObject);
                report += "✓ 已添加 GloveHapticController\n";
                setupCount++;
            }
            else
            {
                report += "✓ GloveHapticController 已存在\n";
            }

            // 4. 配置震动参数
            Undo.RecordObject(hapticController, "Configure Haptic Settings");
            hapticController.pressDetector = pressDetector;
            hapticController.shallowIntensity = 30;  // 浅：轻微震动
            hapticController.goodIntensity = 60;     // 适中：明显震动
            hapticController.deepIntensity = 95;     // 过深：强烈震动
            hapticController.enableRateFeedback = true;
            hapticController.rhythmIntensity = 50;
            hapticController.pulseDurationMs = 30;
            hapticController.logHapticEvents = true;

            report += "\n【震动反馈策略】\n";
            report += "• 深度 < 5cm：轻微震动（30%）- 提示加深\n";
            report += "• 深度 5-6cm：明显震动（60%）- 正确反馈\n";
            report += "• 深度 > 6cm：强烈震动（95%）- 警告过深\n";
            report += "• 速率节奏：50% 强度脉冲引导\n\n";

            // 5. 检查 Scripting Define Symbols
            report += "【重要】确认预处理符号：\n";
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android);
            if (!defines.Contains("BHAPTICS_SDK2"))
            {
                report += "⚠ 需要添加 BHAPTICS_SDK2 到 Scripting Define Symbols\n";
                report += "\n请执行：\n";
                report += "1. Edit → Project Settings → Player\n";
                report += "2. Other Settings → Scripting Define Symbols (Android)\n";
                report += "3. 添加：BHAPTICS_SDK2\n";
                report += "4. 点 Apply\n";
            }
            else
            {
                report += "✓ BHAPTICS_SDK2 已定义\n";
            }

            report += "\n===== 配置完成 =====\n\n";
            report += "【测试步骤】\n";
            report += "1. 连接 TactGlove 到同一 WiFi\n";
            report += "2. 运行游戏，点击开始按钮\n";
            report += "3. 按压红色检测区\n";
            report += "4. 手套会根据按压深度震动反馈\n\n";
            report += "【震动测试】\n";
            report += "可在 Inspector 右键 GloveHapticController → Context Menu 测试震动\n";

            EditorUtility.DisplayDialog("配置 bHaptics", report, "确定");
            Debug.Log(report);

            Selection.activeGameObject = hapticController.gameObject;
        }

        [MenuItem("Tools/CPR/添加 BHAPTICS_SDK2 预处理符号", false, 16)]
        public static void AddBHapticsDefine()
        {
            var target = BuildTargetGroup.Android;
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
            
            if (!defines.Contains("BHAPTICS_SDK2"))
            {
                if (!string.IsNullOrEmpty(defines))
                    defines += ";";
                defines += "BHAPTICS_SDK2";
                
                PlayerSettings.SetScriptingDefineSymbolsForGroup(target, defines);
                
                EditorUtility.DisplayDialog("完成", 
                    "已添加 BHAPTICS_SDK2 到 Android 平台的 Scripting Define Symbols。\n\n" +
                    "脚本将重新编译，请稍等片刻。", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("提示", "BHAPTICS_SDK2 已经存在于 Scripting Define Symbols 中。", "确定");
            }
        }
    }
}
