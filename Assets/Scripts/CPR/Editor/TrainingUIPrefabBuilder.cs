using CPRSimulation.Game;
using CPRSimulation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CPRSimulation.Editor
{
    public static class TrainingUIPrefabBuilder
    {
        private const string PrefabFolder = "Assets/Resources/VRTrainingUI/Prefabs";

        [MenuItem("CPR/UI/Rebuild Runtime UI Prefabs")]
        public static void RebuildRuntimeUiPrefabs()
        {
            EnsureFolder(PrefabFolder);
            SavePrefab(CreateModeCardPrefab(), PrefabFolder + "/TrainingModeCard.prefab");
            SavePrefab(CreateResultPanelPrefab(), PrefabFolder + "/TrainingResultPanel.prefab");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[TrainingUIPrefabBuilder] Runtime UI prefabs rebuilt.");
        }

        private static GameObject CreateModeCardPrefab()
        {
            GameObject root = new GameObject(
                "TrainingModeCard",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(Outline),
                typeof(TrainingModeCardView)
            );

            RectTransform rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400f, 116f);

            Text label = CreateText(root.transform, "Label", Vector2.zero, new Vector2(380f, 104f), 24, TextAnchor.MiddleCenter);
            label.supportRichText = true;

            TrainingModeCardView view = root.GetComponent<TrainingModeCardView>();
            view.Apply(TrainingModeCatalog.Get(CPRTrainingController.TrainingMode.CPR), 20, 18);
            return root;
        }

        private static GameObject CreateResultPanelPrefab()
        {
            GameObject root = new GameObject(
                "TrainingResultPanel",
                typeof(RectTransform),
                typeof(Image),
                typeof(Outline),
                typeof(TrainingResultPanelView)
            );

            RectTransform rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1120f, 620f);

            Text result = CreateText(root.transform, "CPR_ResultText", new Vector2(0f, 88f), new Vector2(1040f, 430f), 27, TextAnchor.UpperLeft);
            result.text = "Training Result";

            Button retry = CreateButton(root.transform, "CPR_ReplayButton", "Try Again", new Vector2(-220f, -235f), new Color(0.1f, 0.6f, 0.9f, 0.9f));
            Button exit = CreateButton(root.transform, "CPR_ExitButton", "Exit", new Vector2(220f, -235f), new Color(0.2f, 0.2f, 0.2f, 0.95f));

            TrainingResultPanelView view = root.GetComponent<TrainingResultPanelView>();
            view.Bind(result, retry, exit);
            view.ConfigureActions(null, null);
            return root;
        }

        private static Text CreateText(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = size;

            Text text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = UIColorScheme.TextPrimary;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(10, fontSize - 8);
            text.resizeTextMaxSize = fontSize;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            VRTrainingUIStyle.ApplyText(text, UIColorScheme.TextPrimary, fontSize >= 30);
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = new Vector2(320f, 90f);

            Button button = go.GetComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(new UnityAction(() => { }));

            Text text = CreateText(go.transform, "Label", Vector2.zero, rt.sizeDelta, 28, TextAnchor.MiddleCenter);
            text.text = label;
            VRTrainingUIStyle.ApplyButton(button, color);
            return button;
        }

        private static void SavePrefab(GameObject instance, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
