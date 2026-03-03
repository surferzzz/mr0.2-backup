using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using CPRSimulation.Core;

namespace CPRSimulation.Editor
{
    public class OneClickFix
    {
        [MenuItem("Tools/CPR/!!! 一键修复当前场景 !!!", false, 1)]
        public static void FixEverything()
        {
            Debug.Log("===== 一键修复开始 =====");
            int fixCount = 0;

            // 1. 修复 XBot_Patient
            var patient = GameObject.Find("XBot_Patient");
            if (patient == null)
            {
                EditorUtility.DisplayDialog("错误", "场景中没有找到 XBot_Patient", "确定");
                return;
            }

            Undo.RecordObject(patient.transform, "Fix XBot");

            // 修复 Transform
            if (patient.transform.localScale.x < 0.5f)
            {
                patient.transform.localScale = Vector3.one;
                Debug.Log($"[修复] Scale: 从 {patient.transform.localScale} -> (1,1,1)");
                fixCount++;
            }

            if (patient.transform.position == Vector3.zero)
            {
                patient.transform.position = new Vector3(0, 0, 2.5f);
                Debug.Log("[修复] Position: 移到 (0, 0, 2.5)");
                fixCount++;
            }

            patient.transform.rotation = Quaternion.Euler(0, 180, 0);
            patient.SetActive(true);

            // 修复 Animator
            var animator = patient.GetComponent<Animator>();
            if (animator != null)
            {
                Undo.RecordObject(animator, "Fix Animator");
                animator.applyRootMotion = false;
                Debug.Log("[修复] Animator: applyRootMotion = false");
                fixCount++;
            }

            // 添加 PatientFallController
            var fallCtrl = patient.GetComponent<PatientFallController>();
            if (fallCtrl == null)
            {
                fallCtrl = Undo.AddComponent<PatientFallController>(patient);
                fallCtrl.fallDuration = 2f;
                fallCtrl.finalRotationX = -90f;
                Debug.Log("[修复] 添加 PatientFallController 组件");
                fixCount++;
            }

            // 2. 添加 CPR 检测（如果没有）
            var detector = patient.GetComponentInChildren<CPRPressureDetector>();
            if (detector == null)
            {
                var detectorObj = new GameObject("CPR_ChestDetector");
                Undo.RegisterCreatedObjectUndo(detectorObj, "Create Detector");
                detectorObj.transform.SetParent(patient.transform);
                detectorObj.transform.localPosition = new Vector3(0, 0.12f, 0.05f);

                detector = detectorObj.AddComponent<CPRPressureDetector>();
                detector.detectionSize = new Vector3(0.2f, 0.2f, 0.2f);
                detector.showDebugGizmos = true;
                detector.showDebugLog = true;

                var haptic = detectorObj.AddComponent<GloveHapticFeedback>();
                haptic.pressureDetector = detector;
                haptic.testModeWithoutDevice = true;

                var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = "ChestMarker";
                marker.transform.SetParent(detectorObj.transform);
                marker.transform.localPosition = Vector3.zero;
                marker.transform.localScale = new Vector3(0.12f, 0.04f, 0.12f);
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = new Color(1f, 0.2f, 0.2f, 0.5f);
                marker.GetComponent<Renderer>().material = mat;
                Object.DestroyImmediate(marker.GetComponent<Collider>());

                Debug.Log("[修复] 添加 CPR 检测组件");
                fixCount++;
            }

            // 3. 修复 CPRGameController
            var menuCanvas = GameObject.Find("MainMenuCanvas");
            if (menuCanvas != null)
            {
                // 移除旧的 SimpleMenuController
                var oldMenu = menuCanvas.GetComponent<Game.SimpleMenuController>();
                if (oldMenu != null)
                {
                    Undo.DestroyObjectImmediate(oldMenu);
                    Debug.Log("[修复] 移除旧的 SimpleMenuController");
                    fixCount++;
                }

                var controller = menuCanvas.GetComponent<Game.CPRGameController>();
                if (controller == null)
                {
                    controller = Undo.AddComponent<Game.CPRGameController>(menuCanvas);
                    Debug.Log("[修复] 添加 CPRGameController");
                    fixCount++;
                }

                Undo.RecordObject(controller, "Fix Controller");

                controller.patientModel = patient;
                controller.fallController = fallCtrl;
                controller.pressureDetector = detector;
                controller.delayBeforeCPR = 2.5f;
                controller.targetCompressions = 30;

                // 查找按钮 - 尝试多种路径
                var startBtn = FindInHierarchy<Button>(menuCanvas.transform, "StartButton");
                var quitBtn = FindInHierarchy<Button>(menuCanvas.transform, "QuitButton");

                if (startBtn != null) controller.startButton = startBtn;
                if (quitBtn != null) controller.quitButton = quitBtn;

                // 查找面板
                var bgPanel = FindInHierarchy<Transform>(menuCanvas.transform, "BackgroundPanel");
                if (bgPanel != null) controller.menuPanel = bgPanel.gameObject;

                // 查找训练面板
                var statusCanvas = GameObject.Find("CPR_StatusCanvas");
                if (statusCanvas != null)
                {
                    var trainingPanel = statusCanvas.transform.Find("TrainingPanel");
                    if (trainingPanel != null)
                        controller.trainingPanel = trainingPanel.gameObject;
                }

                Debug.Log("[修复] CPRGameController 引用已全部设置");
                fixCount++;
            }
            else
            {
                Debug.LogWarning("[警告] 未找到 MainMenuCanvas");
            }

            // 4. 确保地面存在
            var ground = GameObject.Find("Ground");
            if (ground == null)
            {
                ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Ground";
                ground.transform.position = Vector3.zero;
                ground.transform.localScale = new Vector3(3, 1, 3);
                Undo.RegisterCreatedObjectUndo(ground, "Create Ground");
                Debug.Log("[修复] 创建地面");
                fixCount++;
            }

            EditorUtility.SetDirty(patient);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            string report = $"修复完成！共修复 {fixCount} 个问题。\n\n" +
                "修复内容：\n" +
                "- XBot_Patient Transform (位置/缩放/旋转)\n" +
                "- PatientFallController 组件\n" +
                "- Animator 根运动设置\n" +
                "- CPRGameController 引用\n" +
                "- CPR 检测组件\n\n" +
                "现在点击 Play，然后点击「开始训练」。\n" +
                "X Bot 会倒地（代码动画 2 秒）。";

            Debug.Log($"===== 修复完成，共 {fixCount} 项 =====");
            EditorUtility.DisplayDialog("一键修复完成", report, "确定");

            Selection.activeGameObject = patient;
        }

        private static T FindInHierarchy<T>(Transform root, string name) where T : Component
        {
            // 直接子对象
            var direct = root.Find(name);
            if (direct != null)
            {
                var comp = direct.GetComponent<T>();
                if (comp != null) return comp;
            }

            // 递归搜索
            return FindRecursive<T>(root, name);
        }

        private static T FindRecursive<T>(Transform parent, string name) where T : Component
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                {
                    var comp = child.GetComponent<T>();
                    if (comp != null) return comp;
                }
                var result = FindRecursive<T>(child, name);
                if (result != null) return result;
            }
            return null;
        }
    }
}
