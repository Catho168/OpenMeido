using System.Windows.Media;

namespace OpenMeido.Helpers
{
    public static class ThemeColors
    {
        public static readonly Color Primary = Color.FromRgb(0xE8, 0x74, 0x75);
        public static readonly Color PrimaryDark = Color.FromRgb(0xD6, 0x58, 0x59);
        public static readonly Color PrimaryLight = Color.FromRgb(0xF0, 0xA0, 0xA1);
        public static readonly Color Muted = Color.FromRgb(0xE8, 0xC4, 0xC5);
        public static readonly Color BackgroundSuccess = Color.FromRgb(0xF8, 0xF0, 0xF0);
        public static readonly Color BackgroundError = Color.FromRgb(0xF5, 0xE8, 0xE8);

        public static Color GetStatusColor(string statusType)
        {
            return statusType switch
            {
                "ready" => Primary,
                "processing" => PrimaryLight,
                "error" => PrimaryDark,
                "warning" => PrimaryLight,
                _ => Primary
            };
        }

        public static Color GetUiColor(string colorType)
        {
            return colorType switch
            {
                "success" => Primary,
                "error" => PrimaryDark,
                "warning" => PrimaryLight,
                "processing" => PrimaryLight,
                "muted" => Muted,
                "background_success" => BackgroundSuccess,
                "background_error" => BackgroundError,
                "border_success" => PrimaryLight,
                "border_error" => PrimaryDark,
                _ => Primary
            };
        }
    }
}
