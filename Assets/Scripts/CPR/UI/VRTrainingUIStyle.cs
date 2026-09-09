using System.Collections.Generic;
using TMPro;
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
        // 面板用带边框的圆角矩形：边框由精灵自带，省掉 Outline 组件的顶点开销
        private const string PanelName = "button_rectangle_border";
        private const string ClickClipPath = Root + "Sounds/tap-a";

        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

        // 由 Tools ▸ VR Training ▸ Bake CJK TMP Fonts 烘出。
        // 主字体是 Noto Sans JP，其 fallbackFontAssetTable 里挂着
        // Noto Sans SC，用来补 JP 没有的 201 个简体字形。
        private const string FontAssetPath = "VRTrainingUI/Fonts/VRTrainingJP SDF";

        private static TMP_FontAsset _cjkFont;
        private static bool _cjkFontProbed;

        /// <summary>
        /// 烘好的 CJK 字体资源；没找到返回 null（调用方会退回 TMP 默认字体，
        /// 那种情况下 CJK 会显示成豆腐块，所以这里要报错报得明确）。
        /// </summary>
        public static TMP_FontAsset CjkFont
        {
            get
            {
                if (!_cjkFontProbed)
                {
                    _cjkFontProbed = true;
                    _cjkFont = Resources.Load<TMP_FontAsset>(FontAssetPath);
                    if (_cjkFont == null)
                    {
                        Debug.LogError(
                            $"[VRTrainingUIStyle] 找不到 TMP 字体资源 Resources/{FontAssetPath}。\n" +
                            "跑一次 Tools ▸ VR Training ▸ Bake CJK TMP Fonts 生成它。\n" +
                            "在那之前 TMP 会用默认字体（LiberationSans），日文和中文会显示成豆腐块。");
                    }
                }

                return _cjkFont;
            }
        }

        public static void ApplyPanel(GameObject panel, bool strong = false)
        {
            if (panel == null)
                return;

            Image image = panel.GetComponent<Image>();
            if (image != null)
            {
                // 之前这里是 sprite = null，面板因此是硬直角矩形 —— 这是整套 UI
                // 看起来「简陋」的主因。换成 Kenney 的九宫格圆角精灵，
                // 边框由精灵自带，不再需要 Outline 组件。
                // 精灵是 192x64，边框取 18px（与 ApplyDepthTrack 一致）：
                // 上下 18+18=36 < 64，中间还留 28px 可拉伸，不会重叠。
                image.sprite = GetSlicedSprite(Root + "PNG/Grey/Default/" + PanelName,
                                               new Vector4(18f, 18f, 18f, 18f));
                image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
                image.color = strong
                    ? new Color(0.025f, 0.055f, 0.070f, 0.88f)
                    : new Color(0.025f, 0.055f, 0.070f, 0.66f);
                image.raycastTarget = false;
            }

            // Outline / Shadow 都是 BaseMeshEffect，会把每个图形的顶点数翻几倍。
            // 圆角精灵已经自带边框和立体感，所以这里关掉而不是删除 ——
            // 删除会破坏可能存在的旧序列化引用，关掉则随时可以回退。
            Outline outline = panel.GetComponent<Outline>();
            if (outline != null)
                outline.enabled = false;

            Shadow shadow = GetDedicatedShadow(panel);
            if (image != null && image.sprite != null)
            {
                shadow.enabled = false;   // 精灵自带 depth，无需再叠投影
            }
            else
            {
                // 精灵加载失败时退回原来的观感，不至于变成一块纯色板
                shadow.enabled = true;
                shadow.effectColor = new Color(0f, 0f, 0f, strong ? 0.46f : 0.30f);
                shadow.effectDistance = strong ? new Vector2(0f, -6f) : new Vector2(0f, -3f);
                if (outline != null)
                {
                    outline.enabled = true;
                    outline.effectColor = strong
                        ? new Color(0.28f, 0.72f, 0.95f, 0.28f)
                        : new Color(0.65f, 0.86f, 1f, 0.12f);
                    outline.effectDistance = strong ? new Vector2(2f, -2f) : new Vector2(1f, -1f);
                }
            }
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

        /// <summary>
        /// TMP 版。不是替换上面那个 legacy 重载 —— 全项目还有 12 处传 legacy Text
        /// （MainMenuUI / TrainingResultPanelView / EarthquakeTrainingDirector 等），
        /// 两个重载在分阶段迁移期间共存，C# 按类型自动分流。
        /// </summary>
        public static void ApplyText(TMP_Text text, Color color, bool title = false)
        {
            if (text == null)
                return;

            if (CjkFont != null)
                text.font = CjkFont;

            text.color = color;
            text.richText = true;

            // 标题的强调改用 TMP 原生字重。legacy 那边是靠 Outline+Shadow 描边，
            // 但 Outline / Shadow 都是 BaseMeshEffect，对 TMP 不生效 ——
            // TMP 自己生成网格，不走 BaseMeshEffect.ModifyMesh。
            // TMP 真正的描边在材质上（outlineWidth），但那需要 fontMaterial
            // 实例化，会打断合批；深色面板配浅色文字本来就不需要描边。
            text.fontStyle = title ? FontStyles.Bold : FontStyles.Normal;

            DisableLegacyMeshEffects(text.gameObject);
        }

        /// <summary>
        /// 清掉迁移前留在同一个物体上的 Outline / Shadow。
        /// 它们对 TMP 不起作用，但留着 enabled 仍会被 UI 系统遍历，
        /// 而且会让人误以为描边是它们画的。
        /// </summary>
        private static void DisableLegacyMeshEffects(GameObject target)
        {
            if (target == null)
                return;

            foreach (BaseMeshEffect effect in target.GetComponents<BaseMeshEffect>())
            {
                if (effect != null)
                    effect.enabled = false;
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
