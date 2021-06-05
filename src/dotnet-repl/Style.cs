using RadLine;
using Spectre.Console;

namespace dotnet_repl
{
    public class Theme
    {
        public static Theme Default { get; set; } = new();

        public Color SplashColor { get; set; } = Color.Aqua;

        public Style Splash { get; } = new(Color.Aqua);

        public Style AnnouncementText { get; set; } = new(Color.SandyBrown);

        public Style AnnouncementBorder { get; set; } = new(Color.Aqua);

        public Style ErrorOutputBorder { get; set; } = new(Color.Red);

        public Style SuccessOutputBorder { get; set; } = new(Color.Green);

        public ILineEditorPrompt Prompt = new LineEditorPrompt(
            $"[{Decoration.Bold} {Color.Aqua} {Decoration.SlowBlink}]  >[/]",
            $"[{Decoration.Bold} {Color.Aqua} {Decoration.SlowBlink}]...[/]");

        public IStatusMessageGenerator StatusMessageGenerator { get; set; } = new SillyExecutionStatusMessageGenerator();

        internal static Theme FSharp() => new()
        {
            SplashColor = Color.Magenta1,

            Prompt = new LineEditorPrompt(
            $"[{Color.White}]F#[/][{Decoration.Bold} {Color.Magenta1} {Decoration.SlowBlink}]>[/]",
            $"[{Decoration.Bold} {Color.Magenta1} {Decoration.SlowBlink}]...[/]")
        };

    }
}