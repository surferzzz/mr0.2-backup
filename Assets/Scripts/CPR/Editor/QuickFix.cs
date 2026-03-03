using UnityEngine;
using UnityEditor;

namespace CPRSimulation.Editor
{
    public class QuickFix
    {
        [MenuItem("Tools/CPR/快速修复/修复 X Bot 位置和动画", false, 300)]
        public static void FixXBotPosition()
        {
            // 查找 X Bot
            var xbot = GameObject.Find("XBot_Patient");
            if (xbot == null)
            {
                Debug.LogError("未找到 XBot_Patient");
                return;
            }

            // 1. 修复位置
            xbot.transform.position = new Vector3(0, 0, 2.5f);
            xbot.transform.rotation = Quaternion.Euler(0, 180, 0);
            
            // 2. 修复 Animator
            var animator = xbot.GetComponent<Animator>();
            if (animator != null)
            {
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                
                // 重置动画状态
                animator.Rebind();
                animator.Update(0f);
            }

            // 3. 修复动画导入设置
            FixAnimationImport();

            Debug.Log("✓ X Bot 位置和动画已修复");
            EditorUtility.DisplayDialog("修复完成", 
                "X Bot 已修复！\n\n" +
                "• 位置已重置\n" +
                "• Root Motion 已禁用\n" +
                "• 动画已烘焙根运动", "确定");
        }

        private static void FixAnimationImport()
        {
            string modelPath = "Assets/00/X Bot@Death From Right.fbx";
            var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (importer == null) return;

            // 设置动画剪辑
            var clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
            {
                clips = importer.defaultClipAnimations;
            }

            bool needsReimport = false;
            foreach (var clip in clips)
            {
                // 烘焙所有根运动
                if (!clip.lockRootHeightY || !clip.lockRootPositionXZ || !clip.lockRootRotation)
                {
                    clip.lockRootRotation = true;
                    clip.lockRootHeightY = true;
                    clip.lockRootPositionXZ = true;
                    clip.keepOriginalOrientation = true;
                    clip.keepOriginalPositionY = true;
                    clip.keepOriginalPositionXZ = true;
                    needsReimport = true;
                }
            }

            if (needsReimport)
            {
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
                Debug.Log("  ✓ 动画导入设置已更新");
            }
        }

        [MenuItem("Tools/CPR/快速修复/重置 X Bot 到地面", false, 301)]
        public static void ResetXBotToGround()
        {
            var xbot = GameObject.Find("XBot_Patient");
            if (xbot == null)
            {
                Debug.LogError("未找到 XBot_Patient");
                return;
            }

            Undo.RecordObject(xbot.transform, "Reset XBot Position");
            xbot.transform.position = new Vector3(0, 0, 2.5f);
            
            var animator = xbot.GetComponent<Animator>();
            if (animator != null)
            {
                animator.applyRootMotion = false;
            }

            Debug.Log("✓ X Bot 位置已重置到地面");
        }
    }
}
