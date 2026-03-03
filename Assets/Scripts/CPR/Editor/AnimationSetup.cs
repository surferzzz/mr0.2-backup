using UnityEngine;
using UnityEditor;
using System.IO;

namespace CPRSimulation.Editor
{
    /// <summary>
    /// 自动配置Mixamo动画导入设置
    /// </summary>
    public class AnimationSetup
    {
        [MenuItem("Tools/CPR/动画配置/配置Death From Right动画", false, 250)]
        public static void SetupDeathFromRightAnimation()
        {
            string[] searchPaths = {
                "Assets/00/Death From Right.fbx",
                "Assets/Death From Right.fbx",
                "Assets/Animations/Death From Right.fbx"
            };

            string assetPath = null;
            foreach (var path in searchPaths)
            {
                if (File.Exists(Application.dataPath.Replace("Assets", "") + path))
                {
                    assetPath = path;
                    break;
                }
            }

            // 也尝试搜索
            if (assetPath == null)
            {
                var guids = AssetDatabase.FindAssets("Death From Right t:Model");
                if (guids.Length > 0)
                {
                    assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                }
            }

            if (assetPath == null)
            {
                Debug.LogError("[AnimationSetup] 未找到 'Death From Right' 模型文件");
                return;
            }

            SetupMixamoAnimation(assetPath);
        }

        [MenuItem("Tools/CPR/动画配置/配置选中的FBX动画", false, 251)]
        public static void SetupSelectedAnimation()
        {
            var selected = Selection.activeObject;
            if (selected == null)
            {
                Debug.LogError("[AnimationSetup] 请先选择一个FBX文件");
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(selected);
            if (!assetPath.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogError("[AnimationSetup] 请选择FBX文件");
                return;
            }

            SetupMixamoAnimation(assetPath);
        }

        private static void SetupMixamoAnimation(string assetPath)
        {
            Debug.Log($"[AnimationSetup] 开始配置: {assetPath}");

            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"[AnimationSetup] 无法获取ModelImporter: {assetPath}");
                return;
            }

            // === 配置Rig ===
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;

            // === 配置Animation ===
            importer.importAnimation = true;
            
            // 获取动画片段设置
            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            if (clips.Length == 0)
            {
                clips = importer.clipAnimations;
            }

            if (clips.Length > 0)
            {
                ModelImporterClipAnimation[] newClips = new ModelImporterClipAnimation[clips.Length];
                for (int i = 0; i < clips.Length; i++)
                {
                    newClips[i] = clips[i];
                    newClips[i].loopTime = false; // 倒地动画不循环
                    newClips[i].lockRootRotation = true;
                    newClips[i].lockRootHeightY = false;
                    newClips[i].keepOriginalPositionY = true;
                }
                importer.clipAnimations = newClips;
            }

            // === 配置Model ===
            importer.globalScale = 1f;
            importer.useFileScale = true;

            // 应用设置
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            Debug.Log($"[AnimationSetup] 配置完成！\n" +
                     $"- Animation Type: Humanoid\n" +
                     $"- 动画片段数: {clips.Length}\n" +
                     $"- 路径: {assetPath}");

            // 选中文件
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            
            // 提示下一步
            EditorUtility.DisplayDialog("动画配置完成", 
                "动画已配置为Humanoid类型。\n\n" +
                "下一步：\n" +
                "1. 展开FBX文件查看动画片段\n" +
                "2. 使用菜单 'Tools > CPR > 动画配置 > 创建患者动画控制器' 来应用动画",
                "确定");
        }

        [MenuItem("Tools/CPR/动画配置/创建患者动画控制器", false, 260)]
        public static void CreatePatientAnimatorController()
        {
            // 查找动画片段
            var guids = AssetDatabase.FindAssets("Death From Right t:AnimationClip");
            AnimationClip deathClip = null;
            
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                deathClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (deathClip != null) break;
            }

            // 也查找FBX内嵌的动画
            if (deathClip == null)
            {
                var modelGuids = AssetDatabase.FindAssets("Death From Right t:Model");
                foreach (var guid in modelGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var assets = AssetDatabase.LoadAllAssetsAtPath(path);
                    foreach (var asset in assets)
                    {
                        if (asset is AnimationClip clip && !clip.name.Contains("__preview__"))
                        {
                            deathClip = clip;
                            break;
                        }
                    }
                    if (deathClip != null) break;
                }
            }

            if (deathClip == null)
            {
                Debug.LogWarning("[AnimationSetup] 未找到动画片段，请先配置动画");
            }

            // 创建Animator Controller
            string controllerPath = "Assets/Animations/PatientAnimator.controller";
            
            // 确保目录存在
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
            {
                AssetDatabase.CreateFolder("Assets", "Animations");
            }

            var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            
            // 添加参数
            controller.AddParameter("TurnOver", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("IsLyingFlat", AnimatorControllerParameterType.Bool);

            // 获取根状态机
            var rootStateMachine = controller.layers[0].stateMachine;

            // 创建状态
            var idleState = rootStateMachine.AddState("Idle", new Vector3(250, 50, 0));
            var deathState = rootStateMachine.AddState("DeathFromRight", new Vector3(250, 150, 0));
            var lyingState = rootStateMachine.AddState("LyingFlat", new Vector3(500, 150, 0));

            // 设置动画
            if (deathClip != null)
            {
                deathState.motion = deathClip;
            }

            // 创建过渡
            var toDeathTransition = idleState.AddTransition(deathState);
            toDeathTransition.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "TurnOver");
            toDeathTransition.hasExitTime = false;
            toDeathTransition.duration = 0.1f;

            var toLyingTransition = deathState.AddTransition(lyingState);
            toLyingTransition.hasExitTime = true;
            toLyingTransition.exitTime = 1f;
            toLyingTransition.duration = 0.1f;

            // 设置默认状态
            rootStateMachine.defaultState = idleState;

            AssetDatabase.SaveAssets();

            Debug.Log($"[AnimationSetup] 动画控制器创建完成: {controllerPath}");

            // 尝试应用到场景中的Remy模型
            ApplyAnimatorToRemy(controller);

            Selection.activeObject = controller;
        }

        private static void ApplyAnimatorToRemy(RuntimeAnimatorController controller)
        {
            // 查找Remy模型
            string[] remyNames = { "Remy@Laying Seizure", "Remy", "Death From Right" };
            GameObject remy = null;

            foreach (var name in remyNames)
            {
                remy = GameObject.Find(name);
                if (remy != null) break;
            }

            if (remy == null)
            {
                Debug.Log("[AnimationSetup] 未在场景中找到患者模型，请手动将Animator Controller赋给模型");
                return;
            }

            var animator = remy.GetComponent<Animator>();
            if (animator == null)
            {
                animator = remy.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            
            Debug.Log($"[AnimationSetup] 已将动画控制器应用到: {remy.name}");
        }

        [MenuItem("Tools/CPR/动画配置/播放倒地动画测试", false, 270)]
        public static void TestPlayDeathAnimation()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[AnimationSetup] 请先进入Play模式再测试动画");
                EditorUtility.DisplayDialog("提示", "请先点击Play按钮进入播放模式，然后再使用此功能测试动画", "确定");
                return;
            }

            var animator = Object.FindObjectOfType<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetTrigger("TurnOver");
                Debug.Log("[AnimationSetup] 触发倒地动画");
            }
            else
            {
                Debug.LogWarning("[AnimationSetup] 未找到配置好的Animator");
            }
        }
    }
}
