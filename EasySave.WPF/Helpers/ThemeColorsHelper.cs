using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace EasySave.WPF.Helpers
{
    {
        public const string AccentPrimary = "AccentPrimary";
        public const string AccentLight = "AccentLight";
        public const string BgPrimary = "BgPrimary";
        public const string BgCard = "BgCard";
        public const string BgCardRunning = "BgCardRunning";
        public const string BgInput = "BgInput";
        public const string BorderLight = "BorderLight";
        public const string TextPrimary = "TextPrimary";
        public const string TextSecondary = "TextSecondary";
        public const string TextMuted = "TextMuted";
        public const string TextOnDark = "TextOnDark";
        public const string TextOnDarkMuted = "TextOnDarkMuted";
        public const string TextOnAccent = "TextOnAccent";
        public const string StatusSuccess = "StatusSuccess";
        public const string StatusDanger = "StatusDanger";
        public const string StatusWarning = "StatusWarning";
        public const string StatusSuccessBg = "StatusSuccessBg";
        public const string StatusDangerBg = "StatusDangerBg";
        public const string StatusWarningBg = "StatusWarningBg";

        private static readonly Dictionary<string, string> Fallbacks = new()
        {
            [AccentPrimary] = "#a67847",
            [AccentLight] = "#C99B6D",
            [BgPrimary] = "#DFC4A8",
            [BgCard] = "#F2E0CE",
            [BgCardRunning] = "#DCC4A8",
            [BgInput] = "#EBCFB8",
            [BorderLight] = "#DBBFA0",
            [TextPrimary] = "#553f2a",
            [TextSecondary] = "#7A6147",
            [TextMuted] = "#9C8468",
            [TextOnDark] = "#E7D3C1",
            [TextOnDarkMuted] = "#B8A08A",
            [TextOnAccent] = "#F5E6D3",
            [StatusSuccess] = "#5A7247",
            [StatusDanger] = "#9B4D4D",
            [StatusWarning] = "#B8860B",
            [StatusSuccessBg] = "#EFF5EB",
            [StatusDangerBg] = "#F5EDED",
            [StatusWarningBg] = "#F5F0E0",
        };

        // ===== API =====
        public static SolidColorBrush GetBrush(string key)
        {
            if (Application.Current?.Resources[key] is SolidColorBrush b)
                return b;
            var hex = Fallbacks.TryGetValue(key, out var fb) ? fb : "#000000";
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        }

        public static Color GetColorValue(string key) => GetBrush(key).Color;

        // Creates a brush from a hex string; used for theme swatches that are not yet in the active theme.
        public static SolidColorBrush GetBrushFromHex(string hex)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        }
    }
}
