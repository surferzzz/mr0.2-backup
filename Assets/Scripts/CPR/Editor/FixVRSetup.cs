using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine.XR.Management;

namespace CPRSimulation.Editor
{
    /// <summary>
    /// 一键修复 Quest 3 VR 场景配置
    /// 解决"只有静态画面、无法移动交互"的问题
    /// </summary>
    public class FixVRSetup
    {
        [MenuItem("Tools/CPR/修复 Quest 3 VR 配置", false, 1)]
        public static void FixVRConfiguration()
        {
            var report = "===== Quest 3 VR 修复报告 =====\n\n";
            int fixCount = 0;

            // 1. 添加 OVRCameraRig（VR 相机）
            report += "【步骤 1】检查 VR 相机...\n";
            GameObject cameraRig = GameObject.Find("OVRCameraRig");
            
            if (cameraRig == null)
            {
                // 查找 OVRCameraRig 预制体
                string[] guids = AssetDatabase.FindAssets("OVRCameraRig t:Prefab");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                    {
                        cameraRig = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                        cameraRig.name = "OVRCameraRig";
                        cameraRig.transform.position = new Vector3(0, 0, -2);
                        Undo.RegisterCreatedObjectUndo(cameraRig, "Create OVRCameraRig");
                        report += "✓ 已添加 OVRCameraRig（VR 相机）\n";
                        report += $"  位置: (0, 0, -2)\n";
                        fixCount++;
                    }
                }
                else
                {
                    report += "⚠ 未找到 OVRCameraRig 预制体\n";
                    report += "  请确认已安装 Meta XR Core SDK\n";
                }
            }
            else
            {
                report += $"✓ OVRCameraRig 已存在\n";
            }

            // 2. 禁用普通 Main Camera
            report += "\n【步骤 2】处理普通相机...\n";
            var mainCam = Camera.main;
            if (mainCam != null && mainCam.gameObject.name == "Main Camera")
            {
                Undo.RecordObject(mainCam.gameObject, "Disable Main Camera");
                mainCam.gameObject.SetActive(false);
                report += "✓ 已禁用 Main Camera（避免与 VR 相机冲突）\n";
                fixCount++;
            }
            else
            {
                report += "✓ 无需处理 Main Camera\n";
            }

            // 3. 修复 TrainingCanvas 为 World Space
            report += "\n【步骤 3】修复 UI Canvas...\n";
            var canvas = GameObject.Find("TrainingCanvas")?.GetComponent<Canvas>();
            if (canvas != null)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    Undo.RecordObject(canvas, "Fix Canvas Render Mode");
                    canvas.renderMode = RenderMode.WorldSpace;
                    
                    var rectTransform = canvas.GetComponent<RectTransform>();
                    Undo.RecordObject(rectTransform, "Fix Canvas Transform");
                    
                    // 设置为合适的 VR UI 尺寸和位置
                    rectTransform.sizeDelta = new Vector2(1920, 1080);
                    rectTransform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
                    rectTransform.position = new Vector3(0, 1.5f, 2);
                    rectTransform.rotation = Quaternion.identity;
                    
                    report += "✓ TrainingCanvas 已改为 World Space\n";
                    report += "  位置: (0, 1.5, 2) - 玩家前方\n";
                    report += "  尺寸: 1.92m × 1.08m\n";
                    fixCount++;
                }
                else
                {
                    report += $"✓ Canvas 已是 {canvas.renderMode} 模式\n";
                }
            }
            else
            {
                report += "⚠ 未找到 TrainingCanvas\n";
            }

            // 4. 检查 XR 插件配置
            report += "\n【步骤 4】检查 XR 插件...\n";
            try
            {
                var xrSettings = XRGeneralSettings.Instance;
                if (xrSettings != null && xrSettings.Manager != null && xrSettings.Manager.activeLoader != null)
                {
                    report += $"✓ XR 插件已启用: {xrSettings.Manager.activeLoader.name}\n";
                }
                else
                {
                    report += "⚠ XR 插件未启用\n";
                    report += "  请前往: Edit → Project Settings → XR Plug-in Management\n";
                    report += "  启用 Oculus 或 OpenXR\n";
                }
            }
            catch
            {
                report += "⚠ XR 插件管理器未初始化\n";
                report += "  请前往: Edit → Project Settings → XR Plug-in Management\n";
            }

            // 5. 检查 Meta XR 项目设置
            report += "\n【步骤 5】Meta XR 项目设置...\n";
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
                    }
                    else
                    {
                        report += "  提示: 前往 Edit → Project Settings → Meta XR 检查配置\n";
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

            // 6. 标记场景为已修改
            if (fixCount > 0)
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                report += "\n===== 修复完成 =====\n";
                report += $"共修复 {fixCount} 项配置\n\n";
            }
            else
            {
                report += "\n===== 检查完成 =====\n";
                report += "场景配置正常，无需修复\n\n";
            }

            report += "【下一步】\n";
            report += "1. 保存场景（Ctrl+S）\n";
            report += "2. File → Build Settings → Build And Run\n";
            report += "3. 部署到 Quest 3 后，应该能看到立体 VR 画面\n";
            report += "4. 使用手柄摇杆移动，扳机键交互\n\n";
            
            report += "【如果仍有问题】\n";
            report += "• 确认 Quest 3 已开启开发者模式\n";
            report += "• 确认 USB 调试已授权\n";
            report += "• 检查 Edit → Project Settings → XR Plug-in Management\n";
            report += "  确保 Android 平台下已勾选 Oculus\n";

            EditorUtility.DisplayDialog("Quest 3 VR 修复", report, "确定");
            Debug.Log(report);
        }

        [MenuItem("Tools/CPR/快速检查 VR 场景状态", false, 2)]
        public static void QuickCheckVRScene()
        {
            var report = "===== VR 场景状态检查 =====\n\n";

            // 检查 VR 相机
            var cameraRig = GameObject.Find("OVRCameraRig");
            if (cameraRig != null)
            {
                report += "✓ OVRCameraRig: 存在\n";
            }
            else
            {
                report += "✗ OVRCameraRig: 缺失（这会导致无 VR 效果）\n";
            }

            // 检查普通相机
            var mainCam = Camera.main;
            if (mainCam != null && mainCam.gameObject.name == "Main Camera" && mainCam.gameObject.activeInHierarchy)
            {
                report += "✗ Main Camera: 启用中（会与 VR 相机冲突）\n";
            }
            else
            {
                report += "✓ Main Camera: 已禁用或不存在\n";
            }

            // 检查 Canvas
            var canvas = GameObject.Find("TrainingCanvas")?.GetComponent<Canvas>();
            if (canvas != null)
            {
                report += $"✓ TrainingCanvas: {canvas.renderMode}\n";
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    report += "  ⚠ 建议改为 World Space（VR 中更稳定）\n";
                }
            }

            // 检查 XR 插件
            try
            {
                var xrSettings = XRGeneralSettings.Instance;
                if (xrSettings != null && xrSettings.Manager != null && xrSettings.Manager.activeLoader != null)
                {
                    report += $"✓ XR 插件: {xrSettings.Manager.activeLoader.name}\n";
                }
                else
                {
                    report += "✗ XR 插件: 未启用\n";
                }
            }
            catch
            {
                report += "✗ XR 插件: 未初始化\n";
            }

            report += "\n===== 检查完成 =====";
            
            EditorUtility.DisplayDialog("VR 场景状态", report, "确定");
            Debug.Log(report);
        }
    }
}
