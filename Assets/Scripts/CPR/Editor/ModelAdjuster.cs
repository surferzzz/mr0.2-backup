using UnityEngine;
using UnityEditor;

namespace CPRSimulation.Editor
{
    /// <summary>
    /// 快速调整Remy模型大小和位置
    /// </summary>
    public class ModelAdjuster
    {
        [MenuItem("Tools/CPR/调整Remy模型/缩小到50%", false, 220)]
        public static void ScaleDown50()
        {
            var remy = FindRemyModel();
            if (remy != null)
            {
                Undo.RecordObject(remy, "Scale Remy");
                remy.localScale = Vector3.one * 0.5f;
                Debug.Log("[ModelAdjuster] Remy模型缩放到50%");
            }
        }

        [MenuItem("Tools/CPR/调整Remy模型/缩小到30%", false, 221)]
        public static void ScaleDown30()
        {
            var remy = FindRemyModel();
            if (remy != null)
            {
                Undo.RecordObject(remy, "Scale Remy");
                remy.localScale = Vector3.one * 0.3f;
                Debug.Log("[ModelAdjuster] Remy模型缩放到30%");
            }
        }

        [MenuItem("Tools/CPR/调整Remy模型/缩小到20%", false, 222)]
        public static void ScaleDown20()
        {
            var remy = FindRemyModel();
            if (remy != null)
            {
                Undo.RecordObject(remy, "Scale Remy");
                remy.localScale = Vector3.one * 0.2f;
                Debug.Log("[ModelAdjuster] Remy模型缩放到20%");
            }
        }

        [MenuItem("Tools/CPR/调整Remy模型/重置位置到原点", false, 230)]
        public static void ResetPosition()
        {
            var remy = FindRemyModel();
            if (remy != null)
            {
                Undo.RecordObject(remy, "Reset Remy Position");
                remy.position = new Vector3(0, 0, 0);
                remy.rotation = Quaternion.Euler(0, 0, 0);
                Debug.Log("[ModelAdjuster] Remy模型位置已重置");
            }
        }

        [MenuItem("Tools/CPR/调整Remy模型/一键配置CPR（推荐）", false, 240)]
        public static void OneClickSetup()
        {
            var remy = FindRemyModel();
            if (remy == null)
            {
                Debug.LogError("[ModelAdjuster] 未找到Remy模型");
                return;
            }

            // 1. 缩放模型到合适大小
            Undo.RecordObject(remy, "Setup Remy for CPR");
            remy.localScale = Vector3.one * 0.35f;
            remy.position = new Vector3(0, 0, 0);
            remy.rotation = Quaternion.Euler(0, 90, 0); // 让模型朝向合适方向

            // 2. 查找或创建胸部检测点
            Transform chestTarget = FindChestInModel(remy);
            
            // 3. 配置CPR检测器
            var detector = Object.FindObjectOfType<CPRPressDetector>();
            if (detector == null)
            {
                var detectorObj = new GameObject("CPR_ChestDetector");
                detector = detectorObj.AddComponent<CPRPressDetector>();
                Undo.RegisterCreatedObjectUndo(detectorObj, "Create CPR Detector");
            }

            detector.useCollisionDetection = true;
            detector.detectionAreaSize = new Vector3(0.08f, 0.06f, 0.08f);
            
            if (chestTarget != null)
            {
                detector.chestSurface = chestTarget;
                detector.transform.position = chestTarget.position + Vector3.up * 0.03f;
            }
            else
            {
                // 估算胸部位置（模型中心偏上）
                detector.transform.position = remy.position + new Vector3(0, 0.15f, 0);
            }

            // 4. 添加触觉反馈
            var haptic = detector.GetComponent<GloveHapticController>();
            if (haptic == null)
            {
                haptic = detector.gameObject.AddComponent<GloveHapticController>();
                haptic.pressDetector = detector;
            }

            // 5. 添加手部追踪
            var adapter = detector.GetComponent<HandTrackingAdapter>();
            if (adapter == null)
            {
                adapter = detector.gameObject.AddComponent<HandTrackingAdapter>();
                adapter.pressDetector = detector;
                adapter.autoFindOVRHands = true;
            }

            // 6. 重新初始化
            detector.Initialize();

            Selection.activeGameObject = detector.gameObject;
            
            Debug.Log($"[ModelAdjuster] 一键配置完成！\n" +
                     $"- 模型缩放: 35%\n" +
                     $"- 检测器位置: {detector.transform.position}\n" +
                     $"- 请在Game视图中测试");
        }

        private static Transform FindRemyModel()
        {
            // 尝试多种名称
            string[] names = { 
                "Remy@Laying Seizure", 
                "Remy@Laying seizure",
                "Remy", 
                "remy" 
            };

            foreach (var name in names)
            {
                var obj = GameObject.Find(name);
                if (obj != null) return obj.transform;
            }

            // 查找包含Remy的对象
            var allObjects = Object.FindObjectsOfType<Transform>();
            foreach (var t in allObjects)
            {
                if (t.name.ToLower().Contains("remy"))
                    return t;
            }

            Debug.LogWarning("[ModelAdjuster] 未找到Remy模型，请确保场景中有Remy@Laying Seizure对象");
            return null;
        }

        private static Transform FindChestInModel(Transform model)
        {
            // 查找胸部相关的子对象
            string[] chestNames = { "ChestArea", "Chest", "Spine1", "Spine2" };
            
            foreach (var name in chestNames)
            {
                var chest = FindChildRecursive(model, name);
                if (chest != null) return chest;
            }

            // 查找mixamo骨骼中的Spine
            var spine = FindChildRecursive(model, "Spine");
            if (spine != null) return spine;

            return null;
        }

        private static Transform FindChildRecursive(Transform parent, string nameContains)
        {
            foreach (Transform child in parent)
            {
                if (child.name.ToLower().Contains(nameContains.ToLower()))
                    return child;

                var found = FindChildRecursive(child, nameContains);
                if (found != null) return found;
            }
            return null;
        }
    }
}
