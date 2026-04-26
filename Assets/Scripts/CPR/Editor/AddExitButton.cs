using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace CPRSimulation.Editor
{
    /// <summary>
    /// 在开始训练面板添加「終了」按钮的编辑器工具
    /// </summary>
    public static class AddExitButton
    {
        [MenuItem("Tools/CPR/添加終了按钮到开始面板", false, 55)]
        public static void AddExitTrainingButton()
        {
            // GameObject.Find 只查找激活对象；开始面板默认未激活，故先找 Canvas 再找子物体
            GameObject startPanel = GameObject.Find("StartTrainingPanel");
            if (startPanel == null)
            {
                var canvas = GameObject.Find("TrainingCanvas");
                if (canvas != null)
                {
                    var t = canvas.transform.Find("StartTrainingPanel");
                    if (t != null) startPanel = t.gameObject;
                }
            }
            if (startPanel == null)
            {
                EditorUtility.DisplayDialog("未找到面板", "场景中未找到 StartTrainingPanel。\n请确保已打开 CPRTraining 场景，且 TrainingCanvas 下存在该面板。", "确定");
                return;
            }

            // 检查是否已存在
            var existing = startPanel.transform.Find("ExitTrainingButton");
            if (existing != null)
            {
                EditorUtility.DisplayDialog("已存在", "終了按钮已存在。", "确定");
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            Undo.SetCurrentGroupName("Add Exit Training Button");
            int undoGroup = Undo.GetCurrentGroup();

            // 创建按钮
            var btnObj = CreateButton(startPanel.transform, "ExitTrainingButton", "終了", 
                new Vector2(0, -80), new Vector2(220, 55), new Color(0.5f, 0.5f, 0.5f));

            Undo.RegisterCreatedObjectUndo(btnObj, "Add ExitTrainingButton");

            // 绑定到 CPRTrainingController
            var controller = Object.FindObjectOfType<CPRSimulation.Game.CPRTrainingController>();
            if (controller != null)
            {
                Undo.RecordObject(controller, "Assign exitTrainingButton");
                var btn = btnObj.GetComponent<Button>();
                controller.exitTrainingButton = btn;
                EditorUtility.SetDirty(controller);
            }

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Selection.activeGameObject = btnObj;
            Debug.Log("[CPR] 已添加終了按钮到 StartTrainingPanel");
        }

        private static GameObject CreateButton(Transform parent, string name, string text,
            Vector2 anchoredPos, Vector2 sizeDelta, Color color)
        {
            var btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            var rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;
            rect.pivot = new Vector2(0.5f, 0.5f);

            var image = btnObj.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = true;

            btnObj.AddComponent<CanvasRenderer>();

            var button = btnObj.AddComponent<Button>();
            button.targetGraphic = image;

            // 创建 Text 子对象
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var textComp = textObj.AddComponent<Text>();
            textComp.text = text;
            textComp.fontSize = 28;
            textComp.alignment = TextAnchor.MiddleCenter;
            textComp.color = Color.white;
            textComp.raycastTarget = false;
            if (Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") != null)
                textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            textObj.AddComponent<CanvasRenderer>();

            return btnObj;
        }
    }
}
