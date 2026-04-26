using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VRFirstAid.UI
{
    /// <summary>
    /// Lightweight runtime styling for the Earthquake world-space UI so it looks polished
    /// without manual scene tweaking. Safe to add multiple times; it will self-attach if missing.
    /// </summary>
    [DisallowMultipleComponent]
    public class EarthquakeUIStyler : MonoBehaviour
    {
        [Header("References")]
        public Button triggerButton;
        public TextMeshProUGUI instructionText;

        [Header("Palette")]
        public Color accent = new Color(0.18f, 0.52f, 0.96f);        // bright blue
        public Color accentDark = new Color(0.10f, 0.32f, 0.70f);
        public Color panelBg = new Color(0f, 0f, 0f, 0.55f);
        public Color textColor = new Color(0.95f, 0.98f, 1f, 1f);

        private RectTransform _panel;

        private void Awake()
        {
            // Attempt auto-binding if fields are empty
            if (triggerButton == null)
            {
                triggerButton = GetComponentInChildren<Button>(true);
            }
            if (instructionText == null)
            {
                instructionText = GetComponentInChildren<TextMeshProUGUI>(true);
            }

            BuildPanel();
            StyleButton();
            StyleText();
        }

        private void BuildPanel()
        {
            // Create a semi-transparent frame behind the UI if not present
            const string panelName = "UIFrame";
            var existing = transform.Find(panelName);
            if (existing != null)
            {
                _panel = existing as RectTransform;
            }
            else
            {
                var go = new GameObject(panelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                _panel = go.GetComponent<RectTransform>();
                _panel.SetParent(transform, false);
                _panel.SetAsFirstSibling(); // behind other elements
                _panel.anchorMin = new Vector2(0.5f, 0.5f);
                _panel.anchorMax = new Vector2(0.5f, 0.5f);
                _panel.pivot = new Vector2(0.5f, 0.5f);
                _panel.sizeDelta = new Vector2(520, 240);
            }

            var img = _panel.GetComponent<Image>();
            img.color = panelBg;
            img.raycastTarget = false;
        }

        private void StyleButton()
        {
            if (triggerButton == null) return;

            var colors = triggerButton.colors;
            colors.normalColor = accent;
            colors.highlightedColor = Color.Lerp(accent, Color.white, 0.25f);
            colors.pressedColor = accentDark;
            colors.selectedColor = colors.highlightedColor;
            triggerButton.colors = colors;

            var btnImage = triggerButton.GetComponent<Image>();
            if (btnImage != null)
            {
                btnImage.color = accent;
                AddShadow(btnImage.gameObject, new Color(0, 0, 0, 0.35f), new Vector2(4f, -4f));
                AddOutline(btnImage.gameObject, new Color(1f, 1f, 1f, 0.35f));
            }

            var label = triggerButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.enableVertexGradient = true;
                label.color = textColor;
                label.fontSize = 32f;
                label.fontStyle = FontStyles.Bold;
                label.alignment = TextAlignmentOptions.Center;
                label.enableAutoSizing = false;
            }
        }

        private void StyleText()
        {
            if (instructionText == null) return;
            instructionText.color = textColor;
            instructionText.fontSize = 28f;
            instructionText.fontStyle = FontStyles.Bold;
            instructionText.alignment = TextAlignmentOptions.Center;
            instructionText.enableWordWrapping = true;
            instructionText.margin = new Vector4(12, 12, 12, 12);
            AddShadow(instructionText.gameObject, new Color(0, 0, 0, 0.45f), new Vector2(2f, -2f));
        }

        private static void AddShadow(GameObject go, Color color, Vector2 distance)
        {
            var shadow = go.GetComponent<Shadow>();
            if (shadow == null) shadow = go.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
        }

        private static void AddOutline(GameObject go, Color color)
        {
            var outline = go.GetComponent<Outline>();
            if (outline == null) outline = go.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(2f, -2f);
        }
    }
}
