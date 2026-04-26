using UnityEngine;
using UnityEditor;
using UnityEngine.XR.Management;

namespace CPRSimulation.Editor
{
    /// <summary>
    /// 修复 Meta XR 项目设置和预处理符号
    /// </summary>
    public class FixMetaXRSettings
    {
        [MenuItem("Tools/CPR/修复 Meta XR 项目设置", false, 3)]
        public static void FixMetaXRProjectSettings()
        {
            var report = "===== Meta XR 项目设置修复 =====\n\n";
            int fixCount = 0;

            // 1. 添加 BHAPTICS_SDK2 预处理符号
            report += "【步骤 1】添加预处理符号...\n";
            var target = BuildTargetGroup.Android;
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
            
            if (!defines.Contains("BHAPTICS_SDK2"))
            {
                if (!string.IsNullOrEmpty(defines))
                    defines += ";";
                defines += "BHAPTICS_SDK2";
                
                PlayerSettings.SetScriptingDefineSymbolsForGroup(target, defines);
                report += "✓ 已添加 BHAPTICS_SDK2\n";
                fixCount++;
            }
            else
            {
                report += "✓ BHAPTICS_SDK2 已存在\n";
            }

            // 2. 检查 XR 插件管理
            report += "\n【步骤 2】检查 XR 插件...\n";
            try
            {
                var xrSettings = XRGeneralSettings.Instance;
                if (xrSettings != null && xrSettings.Manager != null)
                {
                    if (xrSettings.Manager.activeLoader != null)
                    {
                        report += $"✓ XR 插件已启用: {xrSettings.Manager.activeLoader.name}\n";
                    }
                    else
                    {
                        report += "⚠ XR 插件未激活\n";
                        report += "  手动操作: Edit → Project Settings → XR Plug-in Management\n";
                        report += "  在 Android 标签下勾选 Oculus\n";
                    }
                }
                else
                {
                    report += "⚠ XR 插件管理器未初始化\n";
                    report += "  手动操作: Edit → Project Settings → XR Plug-in Management\n";
                    report += "  点击 Install XR Plugin Management\n";
                }
            }
            catch
            {
                report += "⚠ XR 插件管理器未安装\n";
                report += "  手动操作: Edit → Project Settings → XR Plug-in Management\n";
            }

            // 3. 配置 Android 构建设置
            report += "\n【步骤 3】检查 Android 构建设置...\n";
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {
                report += "✓ 当前平台: Android\n";
                
                // 检查最低 API 级别
                if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel29)
                {
                    PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
                    report += "✓ 已设置最低 API Level 29（Quest 要求）\n";
                    fixCount++;
                }
                else
                {
                    report += $"✓ 最低 API Level: {PlayerSettings.Android.minSdkVersion}\n";
                }
            }
            else
            {
                report += "⚠ 当前平台不是 Android\n";
                report += "  手动操作: File → Build Settings → Android → Switch Platform\n";
            }

            // 4. 检查 Meta XR 配置
            report += "\n【步骤 4】检查 Meta XR 配置...\n";
            try
            {
                var ovrConfigType = System.Type.GetType("OVRProjectConfig, Assembly-CSharp-Editor");
                if (ovrConfigType == null)
                {
                    ovrConfigType = System.Type.GetType("OVRProjectConfig, Meta.XR.Editor");
                }
                
                if (ovrConfigType != null)
                {
                    var ovrConfig = Resources.FindObjectsOfTypeAll(ovrConfigType);
                    if (ovrConfig.Length > 0)
                    {
                        report += "✓ Meta XR 配置已加载\n";
                        report += "  提示: 前往 Edit → Project Settings → Meta XR\n";
                        report += "  查看并应用 Project Setup Tool 的推荐修复\n";
                    }
                    else
                    {
                        report += "⚠ 未找到 Meta XR 配置\n";
                    }
                }
                else
                {
                    report += "  提示: 前往 Edit → Project Settings → Meta XR 检查配置\n";
                }
            }
            catch
            {
                report += "  提示: 前往 Edit → Project Settings → Meta XR 检查配置\n";
            }

            // 5. 总结
            report += "\n===== 修复完成 =====\n";
            if (fixCount > 0)
            {
                report += $"共修复 {fixCount} 项配置\n";
                report += "脚本将重新编译，请稍等...\n\n";
            }
            else
            {
                report += "所有设置正常\n\n";
            }

            report += "【部署到 Quest 3】\n";
            report += "1. 连接 Quest 3 到电脑（USB-C）\n";
            report += "2. Quest 中允许 USB 调试授权\n";
            report += "3. Unity: File → Build Settings → Build And Run\n";
            report += "4. 等待安装完成，自动启动应用\n\n";

            report += "【预期效果】\n";
            report += "✓ 双眼立体视觉（真正的 VR 效果）\n";
            report += "✓ 头部转动可环视四周\n";
            report += "✓ 手柄摇杆可移动\n";
            report += "✓ 扳机键可交互按钮\n";

            EditorUtility.DisplayDialog("Meta XR 设置修复", report, "确定");
            Debug.Log(report);
        }

        [MenuItem("Tools/CPR/检查 Meta XR 推荐修复", false, 4)]
        public static void CheckMetaXRRecommendations()
        {
            EditorUtility.DisplayDialog("Meta XR 推荐修复", 
                "请按以下步骤检查并应用推荐修复：\n\n" +
                "1. Edit → Project Settings → Meta XR\n" +
                "2. 查看 Project Setup Tool 部分\n" +
                "3. 点击 \"Apply All\" 应用所有推荐修复\n\n" +
                "常见推荐修复包括：\n" +
                "• 设置正确的 Color Space（Linear）\n" +
                "• 配置渲染管线\n" +
                "• 启用必要的 XR 功能\n" +
                "• 优化性能设置", 
                "确定");
        }
    }
}
