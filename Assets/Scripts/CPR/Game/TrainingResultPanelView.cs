using CPRSimulation;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CPRSimulation.Game
{
    [DisallowMultipleComponent]
    public sealed class TrainingResultPanelView : MonoBehaviour
    {
        [SerializeField] private Text resultText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button exitButton;

        public Text ResultText => resultText;
        public Button RetryButton => retryButton;
        public Button ExitButton => exitButton;

        public void Bind(Text result, Button retry, Button exit)
        {
            resultText = result;
            retryButton = retry;
            exitButton = exit;
            ApplyStyle();
        }

        public void ConfigureActions(UnityAction retryAction, UnityAction exitAction)
        {
            ConfigureButton(retryButton, retryAction, UIColorScheme.Primary);
            ConfigureButton(exitButton, exitAction, UIColorScheme.PrimaryDark);
        }

        public void SetResult(string text)
        {
            if (resultText != null)
                resultText.text = text;
        }

        public void RefreshLabels(string retryLabel, string exitLabel)
        {
            SetButtonLabel(retryButton, retryLabel);
            SetButtonLabel(exitButton, exitLabel);
        }

        private void ApplyStyle()
        {
            VRTrainingUIStyle.ApplyPanel(gameObject, true);

            if (resultText != null)
                VRTrainingUIStyle.ApplyText(resultText, UIColorScheme.TextPrimary);

            if (retryButton != null)
                VRTrainingUIStyle.ApplyButton(retryButton, UIColorScheme.Primary);

            if (exitButton != null)
                VRTrainingUIStyle.ApplyButton(exitButton, UIColorScheme.PrimaryDark);
        }

        private static void ConfigureButton(Button button, UnityAction action, Color color)
        {
            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            if (action != null)
                button.onClick.AddListener(action);
            VRTrainingUIStyle.ApplyButton(button, color);
        }

        private static void SetButtonLabel(Button button, string label)
        {
            if (button == null)
                return;

            Text text = button.GetComponentInChildren<Text>(true);
            if (text == null)
                return;

            text.text = label;
            VRTrainingUIStyle.ApplyText(text, UIColorScheme.TextPrimary);
        }
    }
}
