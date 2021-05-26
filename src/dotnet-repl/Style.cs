using Spectre.Console;

namespace dotnet_repl
{
    internal static class Theme
    {
        public static Color SplashColor { get; } = Color.Aqua;

        public static Style Splash { get; } = new(Color.Aqua);

        public static Style AnnouncementText { get; set; } = new(Color.SandyBrown);

        public static Style AnnouncementBorder { get; set; } = new(Color.Aqua);

        public static Style ErrorOutputBorder { get; set; } = new(Color.Red);

        public static Style SuccessOutputBorder { get; set; } = new(Color.Green);
    }
}