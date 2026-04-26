using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using CPRSimulation;

namespace CPRSimulation.Editor
{
    /// <summary>
    /// 批量把所有 UI Button 改成按压式按钮（添加 VRPressButton 组件）
    /// 会遍历工程中所有场景并保存修改。
    /// </summary>
    public static class ConvertButtonsToPressStyle
    {
        [MenuItem("Tools/CPR/UI/将所有按钮改为按压式", false, 600)]
        public static void ApplyPressStyleToAllButtons()
        {
            if (!EditorUtility.DisplayDialog(
                    "将所有按钮改为按压式？",
                    "此操作会打开并修改工程中所有场景，将场景里的所有 UI Button 添加 VRPressButton 组件。\n\n" +
                    "建议在执行前先提交或备份工程。\n\n" +
                    "是否继续？",
                    "继续", "取消"))
            {
                return;
            }

            // 只扫描 Assets 下的场景，避免去打开 Package 里的只读示例场景
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
            if (sceneGuids == null || sceneGuids.Length == 0)
            {
                EditorUtility.DisplayDialog("未找到场景", "工程中没有任何 Scene 资源。", "确定");
                return;
            }

            string currentScenePath = EditorSceneManager.GetActiveScene().path;

            int totalButtons = 0;
            int addedCount = 0;

            try
            {
                for (int i = 0; i < sceneGuids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

                    var buttons = Object.FindObjectsOfType<Button>(true);
                    int sceneAdded = 0;

                    foreach (var btn in buttons)
                    {
                        if (btn == null) continue;

                        totalButtons++;

                        if (btn.gameObject.GetComponent<VRPressButton>() != null)
                            continue;

                        Undo.AddComponent<VRPressButton>(btn.gameObject);
                        sceneAdded++;
                        addedCount++;
                    }

                    if (sceneAdded > 0)
                    {
                        EditorSceneManager.MarkSceneDirty(scene);
                        EditorSceneManager.SaveScene(scene);
                    }
                }
            }
            finally
            {
                if (!string.IsNullOrEmpty(currentScenePath))
                {
                    EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
                }
            }

            EditorUtility.DisplayDialog(
                "完成",
                $"已扫描所有场景中的按钮。\n\n" +
                $"总按钮数: {totalButtons}\n" +
                $"新增 VRPressButton: {addedCount}",
                "确定");
        }
    }
}

