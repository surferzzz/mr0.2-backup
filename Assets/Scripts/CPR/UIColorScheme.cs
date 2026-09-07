using UnityEngine;

namespace CPRSimulation
{
    /// <summary>
    /// Shared visual language for the CPR training UI.
    /// Keep semantic colors here so runtime-generated panels stay consistent.
    /// </summary>
    public static class UIColorScheme
    {
        // Primary accent
        public static readonly Color Primary       = new Color(0.16f, 0.47f, 0.78f, 1f); // #2978C7
        public static readonly Color PrimaryDark   = new Color(0.05f, 0.20f, 0.31f, 1f); // #0D334F
        public static readonly Color PrimaryLight  = new Color(0.44f, 0.74f, 0.96f, 1f); // #70BDF5

        // Semantic feedback
        public static readonly Color Success       = new Color(0.20f, 0.72f, 0.38f, 1f); // #33B861
        public static readonly Color Warning       = new Color(0.94f, 0.62f, 0.22f, 1f); // #F09E38
        public static readonly Color Danger        = new Color(0.86f, 0.24f, 0.24f, 1f); // #DB3D3D
        public static readonly Color Info          = new Color(0.30f, 0.75f, 0.95f, 1f); // #4DBFF2

        // Depth quality
        public static Color DepthTooShallow => Warning;
        public static Color DepthGood       => Success;
        public static Color DepthTooDeep    => Danger;

        // Rate quality
        public static Color RateTooSlow  => Warning;
        public static Color RateGood     => Success;
        public static Color RateTooFast  => Danger;

        // Language buttons
        public static readonly Color LangChinese   = new Color(0.72f, 0.18f, 0.18f, 0.90f);
        public static readonly Color LangJapanese  = new Color(0.12f, 0.36f, 0.72f, 0.90f);
        public static readonly Color LangEnglish   = new Color(0.15f, 0.52f, 0.28f, 0.90f);
        public static readonly Color LangHighlight = new Color(1.00f, 0.86f, 0.26f, 1.00f);
        public static readonly Color LangDimmed    = new Color(1.00f, 1.00f, 1.00f, 0.42f);
        public static readonly Color LangHover     = new Color(0.44f, 0.74f, 0.96f, 1.00f);

        // Panels and backgrounds
        public static readonly Color PanelBg       = new Color(0.025f, 0.055f, 0.070f, 0.72f);
        public static readonly Color PanelBgLight  = new Color(0.025f, 0.055f, 0.070f, 0.26f);
        public static readonly Color PanelBgMedium = new Color(0.025f, 0.055f, 0.070f, 0.54f);

        // Text
        public static readonly Color TextPrimary   = new Color(0.94f, 0.98f, 1.00f, 1.00f);
        public static readonly Color TextSecondary = new Color(0.70f, 0.80f, 0.86f, 1.00f);

        // Button tint fallback
        public static readonly Color BtnNormal     = new Color(1.00f, 1.00f, 1.00f, 0.92f);
        public static readonly Color BtnHover      = new Color(0.76f, 0.92f, 1.00f, 1.00f);
        public static readonly Color BtnPressed    = new Color(0.62f, 0.78f, 0.90f, 1.00f);

        // Outline and shadow
        public static readonly Color OutlineDefault = new Color(0.65f, 0.86f, 1.00f, 0.18f);
        public static readonly Color OutlineActive  = new Color(1.00f, 0.86f, 0.26f, 1.00f);
        public static readonly Color ShadowDefault  = new Color(0.00f, 0.00f, 0.00f, 0.42f);
    }
}
