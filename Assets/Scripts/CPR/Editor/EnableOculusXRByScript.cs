using UnityEngine;
using UnityEditor;

namespace CPRSimulation.Editor
{
    /// <summary>
    /// 当 XR Plug-in Management 里“勾不了”时，用此菜单通过代码为 Android 启用 Oculus 插件。
    /// 使用前请先等 “Installing packages...” 消失、且无编译错误。
    /// </summary>
    public static class EnableOculusXRByScript
    {
        const string OculusLoaderTypeName = "Unity.XR.Oculus.OculusLoader";

        [MenuItem("Tools/CPR/【勾不了时用】代码启用 Android Oculus 插件", false, 0)]
        public static void EnableOculusForAndroid()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("无法执行", "请先退出播放模式再执行此操作。", "确定");
                return;
            }

            // 通过反射调用 XR Management 的 API，避免对 Editor 程序集的强依赖
            var xrGeneralSettingsPerBuildTargetType = System.Type.GetType("UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget, Unity.XR.Management.Editor");
            var xrPackageMetadataStoreType = System.Type.GetType("UnityEditor.XR.Management.Metadata.XRPackageMetadataStore, Unity.XR.Management.Editor");

            if (xrGeneralSettingsPerBuildTargetType == null || xrPackageMetadataStoreType == null)
            {
                EditorUtility.DisplayDialog("启用 Oculus 插件",
                    "未找到 XR Management 编辑器 API。\n\n" +
                    "请确认：\n" +
                    "1. Window → Package Manager 中已安装 “XR Plugin Management”；\n" +
                    "2. 已安装 “Oculus XR Plugin”；\n" +
                    "3. 控制台无编译错误。\n\n" +
                    "然后关闭并重新打开 Unity 再试。", "确定");
                return;
            }

            var getSettingsMethod = xrGeneralSettingsPerBuildTargetType.GetMethod("XRGeneralSettingsForBuildTarget",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, null,
                new[] { typeof(BuildTargetGroup) }, null);

            var assignLoaderMethod = xrPackageMetadataStoreType.GetMethod("AssignLoader",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, null,
                new[] { typeof(UnityEngine.XR.Management.XRManagerSettings), typeof(string), typeof(BuildTargetGroup) }, null);

            var isAssignedMethod = xrPackageMetadataStoreType.GetMethod("IsLoaderAssigned",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, null,
                new[] { typeof(string), typeof(BuildTargetGroup) }, null);

            if (getSettingsMethod == null || assignLoaderMethod == null || isAssignedMethod == null)
            {
                EditorUtility.DisplayDialog("启用 Oculus 插件", "XR Management API 版本可能与当前脚本不匹配，请通过 Project Settings → XR Plug-in Management 手动勾选 Android 下的 Oculus。", "确定");
                return;
            }

            // 检查是否已启用
            var alreadyAssigned = (bool)isAssignedMethod.Invoke(null, new object[] { OculusLoaderTypeName, BuildTargetGroup.Android });
            if (alreadyAssigned)
            {
                EditorUtility.DisplayDialog("启用 Oculus 插件", "Android 平台下 Oculus 插件已启用，无需重复操作。\n\n可直接 Build And Run 到 Quest 3。", "确定");
                return;
            }

            // 获取 Android 的 XR 设置
            var androidSettings = getSettingsMethod.Invoke(null, new object[] { BuildTargetGroup.Android });
            if (androidSettings == null)
            {
                EditorUtility.DisplayDialog("启用 Oculus 插件",
                    "未找到 Android 的 XR 通用设置。\n\n" +
                    "请先：\n" +
                    "1. 打开 Edit → Project Settings → XR Plug-in Management；\n" +
                    "2. 点击上方的 Android 标签；\n" +
                    "3. 等待 “Installing packages...” 消失后，手动勾选 Oculus。\n\n" +
                    "若勾选仍为灰色，请重启 Unity 后再试。", "确定");
                return;
            }

            var assignedSettingsProp = androidSettings.GetType().GetProperty("AssignedSettings",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (assignedSettingsProp == null)
            {
                assignedSettingsProp = androidSettings.GetType().GetProperty("Manager",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            }

            var managerSettings = assignedSettingsProp?.GetValue(androidSettings) as UnityEngine.XR.Management.XRManagerSettings;
            if (managerSettings == null)
            {
                EditorUtility.DisplayDialog("启用 Oculus 插件",
                    "Android 的 XR Manager Settings 为空。\n\n" +
                    "请先打开 Edit → Project Settings → XR Plug-in Management，选择 Android，等待界面加载完成后再执行本菜单。", "确定");
                return;
            }

            bool success = (bool)assignLoaderMethod.Invoke(null, new object[] { managerSettings, OculusLoaderTypeName, BuildTargetGroup.Android });
            if (success)
            {
                EditorUtility.SetDirty(managerSettings);
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("启用 Oculus 插件", "已为 Android 平台启用 Oculus 插件。\n\n请保存项目后执行 File → Build Settings → Build And Run 部署到 Quest 3。", "确定");
                Debug.Log("[CPR] 已通过代码为 Android 启用 Oculus XR 插件。");
            }
            else
            {
                EditorUtility.DisplayDialog("启用 Oculus 插件",
                    "添加 Oculus 插件失败。\n\n" +
                    "请确认：\n" +
                    "1. Package Manager 中已安装 “Oculus XR Plugin”；\n" +
                    "2. 当前无编译错误；\n" +
                    "3. 未处于播放模式。\n\n" +
                    "然后重试，或手动在 XR Plug-in Management (Android) 中勾选 Oculus。", "确定");
            }
        }
    }
}
