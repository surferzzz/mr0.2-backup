using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Linq;

namespace CPRSimulation.Editor
{
    /// <summary>
    /// 自动为 X Bot 创建 AnimatorController
    /// 在脚本编译后自动运行
    /// </summary>
    [InitializeOnLoad]
    public class AutoAnimatorSetup
    {
        private const string FBX_PATH = "Assets/00/X Bot@Death From Right.fbx";
        private const string CONTROLLER_PATH = "Assets/00/XBot_DeathController.controller";

        static AutoAnimatorSetup()
        {
            EditorApplication.delayCall += EnsureControllerExists;
        }

        [MenuItem("Tools/CPR/创建动画控制器", false, 30)]
        public static void CreateControllerManual()
        {
            EnsureControllerExists();
            EditorUtility.DisplayDialog("完成", "AnimatorController 已创建/更新。", "确定");
        }

        private static void EnsureControllerExists()
        {
            if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CONTROLLER_PATH) != null)
                return;

            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(FBX_PATH);
            if (modelAsset == null) return;

            // 从 FBX 中提取动画片段
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(FBX_PATH);
            var clip = allAssets.OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__"));

            if (clip == null)
            {
                Debug.LogWarning("[AutoAnimatorSetup] FBX 中未找到动画片段");
                return;
            }

            Debug.Log($"[AutoAnimatorSetup] 找到动画: {clip.name}, 时长: {clip.length}s");

            // 创建 AnimatorController
            var controller = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER_PATH);

            // 获取根状态机
            var rootStateMachine = controller.layers[0].stateMachine;

            // 添加状态
            var idleState = rootStateMachine.AddState("Idle");
            idleState.motion = null;

            var deathState = rootStateMachine.AddState("DeathFromRight");
            deathState.motion = clip;

            // 设置默认状态为 Idle
            rootStateMachine.defaultState = idleState;

            // 添加转换：Idle -> Death（通过 Trigger）
            controller.AddParameter("StartFall", AnimatorControllerParameterType.Trigger);
            var transition = idleState.AddTransition(deathState);
            transition.AddCondition(AnimatorConditionMode.If, 0, "StartFall");
            transition.hasExitTime = false;
            transition.duration = 0.1f;

            // Death 状态不循环，停在最后一帧
            // 动画本身的 loop 设置由 FBX 导入决定

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[AutoAnimatorSetup] AnimatorController 已创建: {CONTROLLER_PATH}");

            // 自动分配给场景中的 XBot_Patient
            AssignToSceneObject(controller);
        }

        private static void AssignToSceneObject(RuntimeAnimatorController controller)
        {
            var patient = GameObject.Find("XBot_Patient");
            if (patient == null) return;

            var animator = patient.GetComponent<Animator>();
            if (animator == null) return;

            if (animator.runtimeAnimatorController == null)
            {
                Undo.RecordObject(animator, "Assign Controller");
                animator.runtimeAnimatorController = controller;
                Debug.Log("[AutoAnimatorSetup] 已自动分配 Controller 到 XBot_Patient");
            }
        }
    }
}
