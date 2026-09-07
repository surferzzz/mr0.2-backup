using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CPRSimulation.EditorTools
{
    /// <summary>
    /// 在编辑器里预览患者“倒地姿势”（采样死亡动画），便于查看模型并调整 CPR 检测范围。
    /// 实际运行时倒地由 Animator 播放死亡动画完成，这里用 AnimationClip.SampleAnimation 还原同样的姿势。
    /// </summary>
    public static class PatientPosePreview
    {
        private const string MenuRoot = "CPR訓練システム/患者姿势/";

        private static GameObject FindPatient()
        {
            var ctrl = Object.FindObjectOfType<CPRSimulation.Game.CPRTrainingController>(true);
            if (ctrl != null && ctrl.patientModel != null)
                return ctrl.patientModel;

            string[] names = { "Standing React Death Backward", "XBot_Patient", "Patient", "X Bot" };
            foreach (var n in names)
            {
                var go = GameObject.Find(n);
                if (go != null) return go;
            }

            foreach (var a in Object.FindObjectsOfType<Animator>(true))
                if (a.runtimeAnimatorController != null) return a.gameObject;

            return null;
        }

        private static AnimationClip FindDeathClip(GameObject patient)
        {
            var animator = patient.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null)
                return null;

            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
            if (clips == null || clips.Length == 0)
                return null;

            string preferred = null;
            var ctrl = Object.FindObjectOfType<CPRSimulation.Game.CPRTrainingController>(true);
            if (ctrl != null) preferred = ctrl.fallDownStateName;

            if (!string.IsNullOrEmpty(preferred))
            {
                var match = clips.FirstOrDefault(c => c != null && c.name == preferred);
                if (match != null) return match;
            }

            var death = clips.FirstOrDefault(c => c != null && c.name.ToLower().Contains("death"));
            return death != null ? death : clips[0];
        }

        [MenuItem(MenuRoot + "预览倒地姿势 (采样动画末帧)")]
        private static void PreviewFallen()
        {
            var patient = FindPatient();
            if (patient == null) { Debug.LogWarning("[姿势预览] 未找到患者模型"); return; }

            var clip = FindDeathClip(patient);
            if (clip == null) { Debug.LogWarning("[姿势预览] 未找到倒地动画片段（请确认 Animator 已挂控制器）"); return; }

            Undo.RegisterFullObjectHierarchyUndo(patient, "Preview Fallen Pose");
            var animator = patient.GetComponent<Animator>();
            if (animator != null) animator.enabled = false;

            clip.SampleAnimation(patient, clip.length);
            Selection.activeGameObject = patient;
            SceneView.RepaintAll();
            Debug.Log($"[姿势预览] 已采样倒地末帧: {clip.name} @ {clip.length:F2}s。查看完成后请用菜单恢复站立。");
        }

        [MenuItem(MenuRoot + "恢复站立姿势 (采样动画首帧)")]
        private static void RestoreStanding()
        {
            var patient = FindPatient();
            if (patient == null) return;

            var clip = FindDeathClip(patient);
            if (clip == null) return;

            Undo.RegisterFullObjectHierarchyUndo(patient, "Restore Standing Pose");
            clip.SampleAnimation(patient, 0f);
            SceneView.RepaintAll();
            Debug.Log("[姿势预览] 已恢复站立（动画首帧）");
        }

        [MenuItem(MenuRoot + "选中训练控制器 (查看检测范围 Gizmo)")]
        private static void SelectController()
        {
            var ctrl = Object.FindObjectOfType<CPRSimulation.Game.CPRTrainingController>(true);
            if (ctrl != null)
            {
                Selection.activeGameObject = ctrl.gameObject;
                Debug.Log("[姿势预览] 已选中 CPRTrainingController：在 Scene 视图可见红/蓝/黄检测范围 Gizmo，参数在 Inspector“CPR 检测范围”分组。");
            }
        }
    }
}
