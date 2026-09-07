using CPRSimulation;
using UnityEngine;
using UnityEngine.UI;

namespace CPRSimulation.Game
{
    [DisallowMultipleComponent]
    public sealed class TrainingModeCardView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Text label;

        public Button Button
        {
            get
            {
                EnsureReferences();
                return button;
            }
        }

        public Text Label
        {
            get
            {
                EnsureReferences();
                return label;
            }
        }

        public void Apply(TrainingModeCardDefinition definition, int descriptionSize = 22, int metaSize = 20)
        {
            EnsureReferences();

            if (background != null)
                background.color = definition.AccentColor;

            if (button != null)
                VRTrainingUIStyle.ApplyButton(button, definition.AccentColor);

            if (label != null)
            {
                label.text = definition.BuildRichLabel(descriptionSize, metaSize);
                label.supportRichText = true;
                label.alignment = TextAnchor.MiddleCenter;
                label.fontSize = 24;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 14;
                label.resizeTextMaxSize = 26;
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.verticalOverflow = VerticalWrapMode.Overflow;
                VRTrainingUIStyle.ApplyText(label, UIColorScheme.TextPrimary);
            }
        }

        private void EnsureReferences()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (background == null)
                background = GetComponent<Image>();

            if (label == null)
                label = GetComponentInChildren<Text>(true);
        }
    }
}
