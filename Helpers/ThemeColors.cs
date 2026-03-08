using System.Windows.Media;

namespace OpenMeido.Helpers
{
    public static class ThemeColors
    {
        public static readonly Color Primary = Color.FromRgb(0xE8, 0x74, 0x75);
        public static readonly Color PrimaryDark = Color.FromRgb(0xD6, 0x58, 0x59);
        public static readonly Color PrimaryLight = Color.FromRgb(0xF0, 0xA0, 0xA1);
        public static readonly Color Muted = Color.FromRgb(0xE8, 0xC4, 0xC5);
        public static readonly Color TextPrimary = Color.FromRgb(0x3A, 0x2E, 0x34);
        public static readonly Color TextSecondary = Color.FromRgb(0x7A, 0x67, 0x70);
        public static readonly Color BorderSubtle = Color.FromRgb(0xE7, 0xD8, 0xDC);
        public static readonly Color BorderStrong = Color.FromRgb(0xD9, 0xB7, 0xBC);
        public static readonly Color Surface = Color.FromRgb(0xFF, 0xFD, 0xFD);
        public static readonly Color SurfaceAlt = Color.FromRgb(0xFF, 0xF7, 0xF7);
        public static readonly Color SurfaceMuted = Color.FromRgb(0xFF, 0xF2, 0xF3);
        public static readonly Color Success = Color.FromRgb(0x4A, 0x8F, 0x6C);
        public static readonly Color Warning = Color.FromRgb(0xD9, 0x98, 0x52);
        public static readonly Color Info = Color.FromRgb(0x6A, 0x8B, 0xC6);
        public static readonly Color BackgroundSuccess = Color.FromRgb(0xEF, 0xF7, 0xF1);
        public static readonly Color BackgroundError = Color.FromRgb(0xFD, 0xF0, 0xF1);
        public static readonly Color BackgroundWarning = Color.FromRgb(0xFF, 0xF6, 0xEA);

        public static Color GetStatusColor(string statusType)
        {
            return statusType switch
            {
                ChatStatusTypes.Ready => Primary,
                ChatStatusTypes.Processing => PrimaryLight,
                ChatStatusTypes.Error => PrimaryDark,
                ChatStatusTypes.Warning => PrimaryLight,
                _ => Primary
            };
        }

        public static Color GetUiColor(string colorType)
        {
            return colorType switch
            {
                "success" => Success,
                "error" => PrimaryDark,
                "warning" => Warning,
                "info" => Info,
                "processing" => PrimaryLight,
                "muted" => Muted,
                "text_primary" => TextPrimary,
                "text_secondary" => TextSecondary,
                "surface" => Surface,
                "surface_alt" => SurfaceAlt,
                "surface_muted" => SurfaceMuted,
                "border_subtle" => BorderSubtle,
                "border_strong" => BorderStrong,
                "background_success" => BackgroundSuccess,
                "background_error" => BackgroundError,
                "background_warning" => BackgroundWarning,
                "border_success" => Success,
                "border_error" => PrimaryDark,
                "border_warning" => Warning,
                _ => Primary
            };
        }
    }
}
