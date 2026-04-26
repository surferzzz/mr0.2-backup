using UnityEngine;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace CPRSimulation.Editor
{
    /// <summary>
    /// 一键配置手部操控与 UI 射线交互（捏合/扳机点按钮）
    /// </summary>
    public static class SetupHandUIInteraction
    {
        [MenuItem("Tools/CPR/配置手部与UI交互", false, 56)]
        public static void Setup()
        {
            int fixCount = 0;
            var report = "手部与UI交互 配置结果：\n\n";

            // 1. OVRCameraRig
            var cameraRig = Object.FindObjectOfType<OVRCameraRig>();
            if (cameraRig == null)
            {
                report += "✗ 未找到 OVRCameraRig，请先添加 VR 相机（如 Tools > CPR > 修复 Quest 3 VR 配置）\n";
                EditorUtility.DisplayDialog("配置手部与UI交互", report, "确定");
                return;
            }
            report += "✓ OVRCameraRig 已存在\n";

            // 2. VRUIPointer（手/柄射线 + 捏合点击）
            var uiPointer = cameraRig.GetComponentInChildren<VRUIPointer>(true);
            if (uiPointer == null)
            {
                Undo.RecordObject(cameraRig.gameObject, "Add VRUIPointer");
                uiPointer = cameraRig.gameObject.AddComponent<VRUIPointer>();
                uiPointer.preferHandTracking = true;
                uiPointer.showRayOnlyWhenPinching = false;
                uiPointer.clickButton = OVRInput.Button.SecondaryIndexTrigger;
                fixCount++;
                report += "✓ 已添加 VRUIPointer（手部/手柄射线，捏合或扳机点击）\n";
            }
            else
            {
                report += "✓ VRUIPointer 已存在\n";
            }

            // 3. EventSystem + OVRInputModule
            var eventSystem = Object.FindObjectOfType<EventSystem>();
            if (eventSystem == null)
            {
                var esObj = new GameObject("EventSystem");
                Undo.RegisterCreatedObjectUndo(esObj, "Create EventSystem");
                eventSystem = esObj.AddComponent<EventSystem>();
                esObj.AddComponent<StandaloneInputModule>();
                fixCount++;
                report += "✓ 已创建 EventSystem\n";
            }

            var ovrModule = eventSystem.GetComponent<OVRInputModule>();
            if (ovrModule == null)
            {
                Undo.RecordObject(eventSystem.gameObject, "Add OVRInputModule");
                ovrModule = eventSystem.gameObject.AddComponent<OVRInputModule>();
                var standalone = eventSystem.GetComponent<StandaloneInputModule>();
                if (standalone != null)
                    standalone.enabled = false;
                fixCount++;
                report += "✓ 已添加 OVRInputModule（VR 点击）\n";
            }
            else
            {
                report += "✓ OVRInputModule 已存在\n";
            }

            report += "\n运行后：\n";
            report += "• 射线从右手发出（手部追踪）或右手柄发出\n";
            report += "• 食指捏合或右手扳机可点击 UI 按钮\n";
            report += "• 在 VRUIPointer 上可勾选「仅捏合时显示射线」\n";

            if (fixCount > 0)
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("配置手部与UI交互", report, "确定");
            Debug.Log("[CPR] 手部与UI交互配置完成");
        }
    }
}
