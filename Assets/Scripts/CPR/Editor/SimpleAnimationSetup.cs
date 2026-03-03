using UnityEngine;
using UnityEditor;

namespace CPRSimulation.Editor
{
    public class SimpleAnimationSetup : EditorWindow
    {
        private GameObject targetModel;
        private AnimationClip animationClip;
        
        [MenuItem("Tools/CPR/简易动画配置窗口", false, 240)]
        public static void ShowWindow()
        {
            var window = GetWindow<SimpleAnimationSetup>("动画配置");
            window.minSize = new Vector2(350, 400);
            
            // 自动查找场景中的模型
            window.targetModel = GameObject.Find("Death From Right");
            if (window.targetModel == null)
                window.targetModel = GameObject.Find("Remy@Laying Seizure");
        }

        private void OnGUI()
        {
            GUILayout.Label("CPR患者动画简易配置", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            // 步骤1：选择模型
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("步骤1：选择场景中的患者模型", EditorStyles.boldLabel);
            
            targetModel = EditorGUILayout.ObjectField("患者模型", targetModel, typeof(GameObject), true) as GameObject;
            
            if (targetModel == null)
            {
                EditorGUILayout.HelpBox("请从Hierarchy拖拽患者模型到上面的框中", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox($"已选择: {targetModel.name}", MessageType.Info);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // 步骤2：选择动画（可选）
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("步骤2：选择动画片段（可选）", EditorStyles.boldLabel);
            
            animationClip = EditorGUILayout.ObjectField("动画片段", animationClip, typeof(AnimationClip), false) as AnimationClip;
            
            EditorGUILayout.HelpBox("如果不选择，将使用模型自带的动画", MessageType.Info);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // 步骤3：应用配置
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("步骤3：应用配置", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(targetModel == null))
            {
                if (GUILayout.Button("创建并应用动画控制器", GUILayout.Height(40)))
                {
                    CreateAndApplyAnimator();
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // 快捷操作
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("快捷操作", EditorStyles.boldLabel);

            if (GUILayout.Button("调整模型大小为30%"))
            {
                if (targetModel != null)
                {
                    Undo.RecordObject(targetModel.transform, "Scale Model");
                    targetModel.transform.localScale = Vector3.one * 0.3f;
                }
            }

            if (GUILayout.Button("重置模型位置"))
            {
                if (targetModel != null)
                {
                    Undo.RecordObject(targetModel.transform, "Reset Position");
                    targetModel.transform.position = Vector3.zero;
                    targetModel.transform.rotation = Quaternion.identity;
                }
            }

            if (GUILayout.Button("让模型平躺（旋转-90度）"))
            {
                if (targetModel != null)
                {
                    Undo.RecordObject(targetModel.transform, "Rotate Model");
                    targetModel.transform.rotation = Quaternion.Euler(-90, 0, 0);
                }
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // 状态检查
            if (targetModel != null)
            {
                EditorGUILayout.BeginVertical("box");
                GUILayout.Label("当前状态", EditorStyles.boldLabel);
                
                var animator = targetModel.GetComponent<Animator>();
                DrawStatus("Animator组件", animator != null);
                DrawStatus("动画控制器", animator != null && animator.runtimeAnimatorController != null);
                
                EditorGUILayout.EndVertical();
            }
        }

        private void CreateAndApplyAnimator()
        {
            if (targetModel == null) return;

            // 确保有Animator组件
            var animator = targetModel.GetComponent<Animator>();
            if (animator == null)
            {
                animator = targetModel.AddComponent<Animator>();
            }

            // 创建简单的动画控制器
            string folderPath = "Assets/Animations";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets", "Animations");
            }

            string controllerPath = $"{folderPath}/PatientController.controller";
            
            // 如果已存在，直接使用
            var existingController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
            if (existingController != null)
            {
                animator.runtimeAnimatorController = existingController;
                Debug.Log("[SimpleAnimationSetup] 使用已存在的动画控制器");
                return;
            }

            // 创建新控制器
            var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            
            // 添加参数
            controller.AddParameter("PlayDeath", AnimatorControllerParameterType.Trigger);
            
            var rootStateMachine = controller.layers[0].stateMachine;
            
            // 创建默认状态
            var idleState = rootStateMachine.AddState("Idle", new Vector3(200, 0, 0));
            
            // 如果有动画片段，添加动画状态
            if (animationClip != null)
            {
                var animState = rootStateMachine.AddState("DeathAnim", new Vector3(200, 100, 0));
                animState.motion = animationClip;
                
                var transition = idleState.AddTransition(animState);
                transition.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "PlayDeath");
                transition.hasExitTime = false;
                transition.duration = 0.1f;
            }
            
            rootStateMachine.defaultState = idleState;
            
            AssetDatabase.SaveAssets();
            
            // 应用控制器
            animator.runtimeAnimatorController = controller;
            
            Debug.Log($"[SimpleAnimationSetup] 动画控制器已创建并应用到 {targetModel.name}");
            
            EditorUtility.DisplayDialog("完成", 
                "动画控制器已创建并应用！\n\n" +
                "控制器位置: Assets/Animations/PatientController.controller", 
                "确定");
        }

        private void DrawStatus(string label, bool ok)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(ok ? "✓" : "✗", GUILayout.Width(20));
            GUILayout.Label(label);
            GUILayout.Label(ok ? "已配置" : "未配置");
            EditorGUILayout.EndHorizontal();
        }
    }
}
