using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
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

        /// <summary>
        /// 使用「Standing React Death Backward」倒地动画创建控制器并分配给场景中的患者。
        /// 按下开始按钮后人物将播放此倒地动画。
        /// </summary>
        [MenuItem("Tools/CPR/使用 Standing React Death Backward 倒地动画", false, 31)]
        public static void SetupStandingReactDeathBackward()
        {
            const string fbxPath = "Assets/00/Standing React Death Backward.fbx";
            const string controllerPath = "Assets/00/StandingReactDeathBackward_Controller.controller";

            var allAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            if (allAssets == null || allAssets.Length == 0)
            {
                EditorUtility.DisplayDialog("未找到资源", "未找到: " + fbxPath + "\n请确认该 FBX 在项目中。", "确定");
                return;
            }

            var clip = allAssets.OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
            if (clip == null)
            {
                EditorUtility.DisplayDialog("未找到动画", "FBX 中未找到动画片段: " + fbxPath, "确定");
                return;
            }

            // 创建或覆盖控制器：状态名用 DeathFromRight，与 CPRTrainingController 默认一致
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                var root = controller.layers[0].stateMachine;
                var idle = root.AddState("Idle");
                root.defaultState = idle;
                var death = root.AddState("DeathFromRight");
                death.motion = clip;
                controller.AddParameter("StartFall", AnimatorControllerParameterType.Trigger);
                var trans = idle.AddTransition(death);
                trans.AddCondition(AnimatorConditionMode.If, 0, "StartFall");
                trans.hasExitTime = false;
                trans.duration = 0.1f;
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
                Debug.Log("[CPR] 已创建控制器: " + controllerPath + ", 状态: DeathFromRight, 片段: " + clip.name);
            }
            else
            {
                var root = controller.layers[0].stateMachine;
                foreach (var state in root.states)
                {
                    if (state.state.name == "DeathFromRight")
                    {
                        state.state.motion = clip;
                        EditorUtility.SetDirty(controller);
                        AssetDatabase.SaveAssets();
                        break;
                    }
                }
            }

            // 分配给学生：优先 Standing React Death Backward（你拖进来的预制体），再 Patient、XBot_Patient
            var patient = GameObject.Find("Standing React Death Backward") ?? GameObject.Find("Patient") ?? GameObject.Find("XBot_Patient") ?? GameObject.Find("X Bot");
            if (patient == null)
            {
                var animators = Object.FindObjectsOfType<Animator>();
                foreach (var a in animators)
                {
                    if (a.avatar != null && a.avatar.isHuman)
                    {
                        patient = a.gameObject;
                        break;
                    }
                }
            }

            if (patient != null)
            {
                var animator = patient.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = Undo.AddComponent<Animator>(patient);
                    // 从 Standing React Death Backward FBX 取 Avatar 并赋给新 Animator
                    var avatar = allAssets.OfType<Avatar>().FirstOrDefault();
                    if (avatar != null)
                    {
                        animator.avatar = avatar;
                        Debug.Log("[CPR] 已为 " + patient.name + " 添加 Animator 并设置 Avatar");
                    }
                    else
                    {
                        // 尝试从子节点（如模型实例）获取已有 Avatar
                        var childAnimator = patient.GetComponentInChildren<Animator>(true);
                        if (childAnimator != null && childAnimator.avatar != null)
                        {
                            animator.avatar = childAnimator.avatar;
                            Debug.Log("[CPR] 已从子节点复制 Avatar");
                        }
                    }
                }
                if (animator != null)
                {
                    Undo.RecordObject(animator, "Assign Standing React Death Backward");
                    animator.runtimeAnimatorController = controller;
                    Debug.Log("[CPR] 已为 " + patient.name + " 分配倒地动画控制器（Standing React Death Backward）");
                }
            }
            else
            {
                EditorUtility.DisplayDialog("未找到患者", "请在场景中放置 Patient、XBot_Patient 或带 Animator 的人形角色。", "确定");
                return;
            }

            EnsureCPRTrainingControllerInScene();
            EditorUtility.DisplayDialog("完成", "已使用「Standing React Death Backward」倒地动画。\n\n" +
                "• 控制器: " + controllerPath + "\n" +
                "• 状态名: DeathFromRight（与开始按钮流程一致）\n" +
                "• 运行后点击「开始」/「开始情景」按钮即可触发人物倒地。", "确定");
        }

        /// <summary>
        /// 向当前场景添加 CPR 训练控制器（负责：开始按钮 → 人物倒地流程）。若已存在则跳过。
        /// </summary>
        [MenuItem("Tools/CPR/向场景添加 CPR 训练控制器（开始按钮→倒地）", false, 20)]
        public static void EnsureCPRTrainingControllerInScene()
        {
            var existing = Object.FindObjectOfType<CPRSimulation.Game.CPRTrainingController>();
            if (existing != null)
            {
                EditorUtility.DisplayDialog("已存在", "场景中已有 CPR 训练控制器（" + existing.gameObject.name + "）。\n\n运行后点击 TrainingCanvas 上的「开始」/「开始情景」按钮即可触发人物倒地。", "确定");
                Selection.activeGameObject = existing.gameObject;
                return;
            }
            var go = new GameObject("CPRTrainingController");
            go.AddComponent<CPRSimulation.Game.CPRTrainingController>();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = go;
            EditorUtility.DisplayDialog("已添加", "已向场景添加「CPRTrainingController」。\n\n" +
                "• 运行游戏后，控制器会自动查找 Patient 和 TrainingCanvas 上的开始按钮。\n" +
                "• 点击开始/开始情景按钮后，人物将播放倒地动画。\n\n" +
                "请保存场景（Ctrl+S）。", "确定");
        }
    }
}
