using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CPRSimulation
{
    public enum VRTrainingUIAccent
    {
        Neutral,
        Primary,
        Success,
        Warning,
        Danger
    }

    public static class VRTrainingUIStyle
    {
        private const string Root = "VRTrainingUI/Kenney/";
        private const string ButtonName = "button_rectangle_depth_gradient";
        private const string ButtonFlatName = "button_rectangle_depth_flat";
        private const string ClickClipPath = Root + "Sounds/tap-a";

        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

        public static void ApplyPanel(GameObject panel, bool strong = false)
        {
            if (panel == null)
                return;

            Image image = panel.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = null;
                image.type = Image.Type.Simple;
                image.color = strong
                    ? new Color(0.025f, 0.055f, 0.070f, 0.82f)
                    : new Color(0.025f, 0.055f, 0.070f, 0.58f);
                image.raycastTarget = false;
            }

            Outline outline = panel.GetComponent<Outline>();
            if (outline == null)
                outline = panel.AddComponent<Outline>();
            outline.effectColor = strong
                ? new Color(0.28f, 0.72f, 0.95f, 0.28f)
                : new Color(0.65f, 0.86f, 1f, 0.12f);
            outline.effectDistance = strong ? new Vector2(2f, -2f) : new Vector2(1f, -1f);

            Shadow shadow = GetDedicatedShadow(panel);
            shadow.effectColor = new Color(0f, 0f, 0f, strong ? 0.46f : 0.30f);
            shadow.effectDistance = strong ? new Vector2(0f, -6f) : new Vector2(0f, -3f);
        }

        public static void ApplyButton(Button button, Color semanticColor)
        {
            if (button == null)
                return;

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = GetButtonSprite(GetAccent(semanticColor));
                image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
                image.color = image.sprite != null ? Color.white : semanticColor;
                image.raycastTarget = true;
            }

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.94f);
            colors.pressedColor = new Color(0.80f, 0.88f, 0.94f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.52f, 0.56f, 0.60f, 0.52f);
            colors.colorMultiplier = 1.0f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.transition = Selectable.Transition.ColorTint;

            ButtonMicroInteraction micro = button.GetComponent<ButtonMicroInteraction>();
            if (micro == null)
                micro = button.gameObject.AddComponent<ButtonMicroInteraction>();
            micro.hoverScale = 1.045f;
            micro.pressScale = 0.965f;
            micro.transitionSpeed = 12f;
            micro.enableGlow = false;

            VRUIButtonSfx sfx = button.GetComponent<VRUIButtonSfx>();
            if (sfx == null)
                sfx = button.gameObject.AddComponent<VRUIButtonSfx>();
            sfx.EnsureRegistered();
        }

        public static void ApplyText(Text text, Color color, bool title = false)
        {
            if (text == null)
                return;

            text.color = color;
            text.supportRichText = true;

            Shadow shadow = GetDedicatedShadow(text.gameObject);
            shadow.effectColor = new Color(0f, 0f, 0f, title ? 0.72f : 0.52f);
            shadow.effectDistance = title ? new Vector2(1.5f, -1.5f) : new Vector2(1f, -1f);

            if (title)
            {
                Outline outline = text.GetComponent<Outline>();
                if (outline == null)
                    outline = text.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.35f);
                outline.effectDistance = new Vector2(1f, -1f);
            }
        }

        public static void ApplyDepthTrack(Image image)
        {
            if (image == null)
                return;

            image.sprite = GetSlicedSprite(Root + "PNG/Grey/Default/" + ButtonFlatName, new Vector4(18f, 18f, 18f, 18f));
            image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = new Color(0.08f, 0.11f, 0.13f, 0.95f);
            image.raycastTarget = false;
        }

        public static void ApplyDepthFill(Image image)
        {
            if (image == null)
                return;

            image.sprite = GetSlicedSprite(Root + "PNG/Extra/Default/input_rectangle", new Vector4(18f, 18f, 18f, 18f));
            image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.raycastTarget = false;
        }

        private static Sprite GetButtonSprite(VRTrainingUIAccent accent)
        {
            string colorFolder = accent switch
            {
                VRTrainingUIAccent.Success => "Green",
                VRTrainingUIAccent.Warning => "Yellow",
                VRTrainingUIAccent.Danger => "Red",
                VRTrainingUIAccent.Primary => "Blue",
                _ => "Grey"
            };

            return GetSlicedSprite(Root + "PNG/" + colorFolder + "/Default/" + ButtonName, new Vector4(18f, 18f, 18f, 18f));
        }

        private static VRTrainingUIAccent GetAccent(Color color)
        {
            if (color.g > color.r && color.g > color.b)
                return VRTrainingUIAccent.Success;
            if (color.r > 0.75f && color.g > 0.55f)
                return VRTrainingUIAccent.Warning;
            if (color.r > 0.65f && color.g < 0.55f)
                return VRTrainingUIAccent.Danger;
            if (color.b >= color.r && color.b >= color.g)
                return VRTrainingUIAccent.Primary;
            return VRTrainingUIAccent.Neutral;
        }

        private static Sprite GetSlicedSprite(string resourcePath, Vector4 border)
        {
            if (SpriteCache.TryGetValue(resourcePath, out Sprite cached))
                return cached;

            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                SpriteCache[resourcePath] = null;
                return null;
            }

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                1,
                SpriteMeshType.FullRect,
                border
            );
            sprite.name = texture.name + "_RuntimeSliced";
            SpriteCache[resourcePath] = sprite;
            return sprite;
        }

        private static Shadow GetDedicatedShadow(GameObject target)
        {
            Shadow[] shadows = target.GetComponents<Shadow>();
            foreach (Shadow shadow in shadows)
            {
                if (shadow != null && shadow.GetType() == typeof(Shadow))
                    return shadow;
            }

            return target.AddComponent<Shadow>();
        }

        public static AudioClip LoadClickClip()
        {
            return Resources.Load<AudioClip>(ClickClipPath);
        }
    }

}
