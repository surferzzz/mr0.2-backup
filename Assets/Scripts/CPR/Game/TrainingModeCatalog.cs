using CPRSimulation;
using UnityEngine;

namespace CPRSimulation.Game
{
    public readonly struct TrainingModeCardDefinition
    {
        public TrainingModeCardDefinition(
            CPRTrainingController.TrainingMode mode,
            string icon,
            Color accentColor)
        {
            Mode = mode;
            Icon = icon;
            AccentColor = accentColor;
        }

        public CPRTrainingController.TrainingMode Mode { get; }
        public string Icon { get; }
        public Color AccentColor { get; }

        public string Title => TrainingModeCatalog.GetTitle(Mode);
        public string Description => TrainingModeCatalog.GetDescription(Mode);
        public string Difficulty => TrainingModeCatalog.GetDifficulty(Mode);
        public string Duration => TrainingModeCatalog.GetDuration(Mode);

        public string BuildRichLabel(int descriptionSize = 22, int metaSize = 20)
        {
            return $"<b>{Icon} {Title}</b>\n<size={descriptionSize}>{Description}</size>\n<size={metaSize}>{Difficulty}  |  {Duration}</size>";
        }
    }

    public static class TrainingModeCatalog
    {
        public static string SelectionTitle => LanguageManager.Get(
            "\u8bf7\u9009\u62e9\u8bad\u7ec3\u6a21\u5f0f",
            "\u30c8\u30ec\u30fc\u30cb\u30f3\u30b0\u30e2\u30fc\u30c9\u3092\u9078\u629e",
            "Choose a training mode"
        );

        public static string EntryLabel => LanguageManager.Get(
            "\u8bad\u7ec3\u6a21\u5f0f",
            "\u30c8\u30ec\u30fc\u30cb\u30f3\u30b0\u30e2\u30fc\u30c9",
            "Training Modes"
        );

        public static TrainingModeCardDefinition Get(CPRTrainingController.TrainingMode mode)
        {
            return mode switch
            {
                CPRTrainingController.TrainingMode.Story => new TrainingModeCardDefinition(mode, "[!]", UIColorScheme.Warning),
                CPRTrainingController.TrainingMode.CPR => new TrainingModeCardDefinition(mode, "[CPR]", UIColorScheme.Success),
                CPRTrainingController.TrainingMode.AED => new TrainingModeCardDefinition(mode, "[AED]", UIColorScheme.Danger),
                CPRTrainingController.TrainingMode.Quiz => new TrainingModeCardDefinition(mode, "[?]", UIColorScheme.Info),
                _ => new TrainingModeCardDefinition(CPRTrainingController.TrainingMode.None, "[ ]", UIColorScheme.PrimaryDark)
            };
        }

        public static string BuildCardLabel(CPRTrainingController.TrainingMode mode, int descriptionSize = 22, int metaSize = 20)
        {
            return Get(mode).BuildRichLabel(descriptionSize, metaSize);
        }

        public static string GetTitle(CPRTrainingController.TrainingMode mode)
        {
            return mode switch
            {
                CPRTrainingController.TrainingMode.Story => LanguageManager.Get(
                    "\u5267\u60c5\u6025\u6551\u6a21\u5f0f",
                    "\u30b7\u30ca\u30ea\u30aa\u6551\u6025",
                    "Story Rescue"
                ),
                CPRTrainingController.TrainingMode.CPR => LanguageManager.Get(
                    "CPR\u4e13\u9879\u8bad\u7ec3",
                    "CPR\u5c02\u9805\u30c8\u30ec\u30fc\u30cb\u30f3\u30b0",
                    "CPR Training"
                ),
                CPRTrainingController.TrainingMode.AED => LanguageManager.Get(
                    "AED\u4e13\u9879\u8bad\u7ec3",
                    "AED\u5c02\u9805\u30c8\u30ec\u30fc\u30cb\u30f3\u30b0",
                    "AED Training"
                ),
                CPRTrainingController.TrainingMode.Quiz => LanguageManager.Get(
                    "\u77e5\u8bc6\u6d4b\u9a8c",
                    "\u77e5\u8b58\u30c6\u30b9\u30c8",
                    "Knowledge Quiz"
                ),
                _ => string.Empty
            };
        }

        public static string GetDescription(CPRTrainingController.TrainingMode mode)
        {
            return mode switch
            {
                CPRTrainingController.TrainingMode.Story => LanguageManager.Get(
                    "\u5b8c\u6574\u6d41\u7a0b\uff1a\u53d1\u73b0\u3001\u547c\u6551\u3001AED\u3001CPR",
                    "\u767a\u898b\u30fb\u901a\u5831\u30fbAED\u30fbCPR\u306e\u5168\u4f53\u6d41\u308c",
                    "Full flow: discover, call, AED, CPR"
                ),
                CPRTrainingController.TrainingMode.CPR => LanguageManager.Get(
                    "\u805a\u7126\u624b\u4f4d\u3001\u6df1\u5ea6\u548c\u9891\u7387",
                    "\u624b\u306e\u4f4d\u7f6e\u30fb\u6df1\u3055\u30fb\u30c6\u30f3\u30dd\u3092\u96c6\u4e2d\u7df4\u7fd2",
                    "Focus on hand position, depth, and rate"
                ),
                CPRTrainingController.TrainingMode.AED => LanguageManager.Get(
                    "\u7ec3\u4e60\u5f00\u673a\u3001\u8d34\u7247\u3001\u5206\u6790\u548c\u7535\u51fb",
                    "\u96fb\u6e90\u30fb\u30d1\u30c3\u30c9\u8cbc\u4ed8\u30fb\u89e3\u6790\u30fb\u30b7\u30e7\u30c3\u30af",
                    "Practice power, pads, analysis, shock"
                ),
                CPRTrainingController.TrainingMode.Quiz => LanguageManager.Get(
                    "\u590d\u4e60 CPR/AED \u5173\u952e\u5224\u65ad\u9898",
                    "CPR/AED \u306e\u91cd\u8981\u5224\u65ad\u3092\u5fa9\u7fd2",
                    "Review CPR/AED decision points"
                ),
                _ => string.Empty
            };
        }

        public static string GetDifficulty(CPRTrainingController.TrainingMode mode)
        {
            return mode switch
            {
                CPRTrainingController.TrainingMode.Story => LanguageManager.Get("\u96be\u5ea6\uff1a\u4e2d\u7b49", "\u96e3\u6613\u5ea6\uff1a\u4e2d", "Difficulty: Medium"),
                CPRTrainingController.TrainingMode.CPR => LanguageManager.Get("\u96be\u5ea6\uff1a\u5165\u95e8", "\u96e3\u6613\u5ea6\uff1a\u521d\u7d1a", "Difficulty: Beginner"),
                CPRTrainingController.TrainingMode.AED => LanguageManager.Get("\u96be\u5ea6\uff1a\u5165\u95e8", "\u96e3\u6613\u5ea6\uff1a\u521d\u7d1a", "Difficulty: Beginner"),
                CPRTrainingController.TrainingMode.Quiz => LanguageManager.Get("\u96be\u5ea6\uff1a\u590d\u4e60", "\u96e3\u6613\u5ea6\uff1a\u5fa9\u7fd2", "Difficulty: Review"),
                _ => string.Empty
            };
        }

        public static string GetDuration(CPRTrainingController.TrainingMode mode)
        {
            return mode switch
            {
                CPRTrainingController.TrainingMode.Story => LanguageManager.Get("\u9884\u8ba1\uff1a6-8\u5206\u949f", "\u76ee\u5b89\uff1a6-8\u5206", "Time: 6-8 min"),
                CPRTrainingController.TrainingMode.CPR => LanguageManager.Get("\u9884\u8ba1\uff1a2-3\u5206\u949f", "\u76ee\u5b89\uff1a2-3\u5206", "Time: 2-3 min"),
                CPRTrainingController.TrainingMode.AED => LanguageManager.Get("\u9884\u8ba1\uff1a3-4\u5206\u949f", "\u76ee\u5b89\uff1a3-4\u5206", "Time: 3-4 min"),
                CPRTrainingController.TrainingMode.Quiz => LanguageManager.Get("\u9884\u8ba1\uff1a2\u5206\u949f", "\u76ee\u5b89\uff1a2\u5206", "Time: 2 min"),
                _ => string.Empty
            };
        }
    }
}
