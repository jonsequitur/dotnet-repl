using RadLine;
using Spectre.Console;

namespace dotnet_repl;

public class Theme
{
    public Style AnnouncementTextStyle { get; set; } = new(Color.SandyBrown);

    public Style AnnouncementBorderStyle { get; set; } = new(Color.Aqua);

    public static Theme Default { get; set; } = new();
}

public abstract class KernelSpecificTheme : Theme
{
    public abstract Style AccentStyle { get; }

    public Style ErrorOutputBorderStyle { get; set; } = new(Color.Red);

    public Style SuccessOutputBorderStyle { get; set; } = new(Color.Green);

    public abstract string PromptText { get; }

    public virtual string KernelDisplayName => PromptText;

    public virtual ILineEditorPrompt Prompt => new DelegatingPrompt(
        $"[{AnnouncementTextStyle.Foreground}]{PromptText} [/][{Decoration.Bold} {AccentStyle.Foreground} {Decoration.SlowBlink}]>[/]",
        $"[{Decoration.Bold} {AccentStyle.Foreground} {Decoration.SlowBlink}] ...[/]");

    public IStatusMessageGenerator StatusMessageGenerator { get; set; } = new SillyExecutionStatusMessageGenerator();

    public static KernelSpecificTheme? GetTheme(string kernelName) => kernelName switch
    {
        "csharp" => new CSharpTheme(),
        "fsharp" => new FSharpTheme(),
        "pwsh" => new PowerShellTheme(),
        "javascript" => new JavaScriptTheme(),
        "sql" => new SqlTheme(),
        "html" => new HtmlTheme(),
        "httpRequest" => new HttpRequestTheme(),
        _ => null
    };
}

public class CSharpTheme : KernelSpecificTheme
{
    public override Style AccentStyle => new(Color.Aqua);

    public override string PromptText => "C#";
}

public class FSharpTheme : KernelSpecificTheme
{
    public override Style AccentStyle => new(Color.Magenta1);

    public override string PromptText => "F#";
}

public class PowerShellTheme : KernelSpecificTheme
{
    public override Style AccentStyle => new(Color.BlueViolet);

    public override string KernelDisplayName => "PowerShell";

    public override string PromptText => "PS";
}

public class HttpRequestTheme : KernelSpecificTheme
{
    public override Style AccentStyle => new(Color.Aqua);

    public override string KernelDisplayName => "HTTP Request";

    public override string PromptText => "HTTP";
}

public class JavaScriptTheme : KernelSpecificTheme
{
    public override Style AccentStyle => new(Color.Yellow);

    public override string KernelDisplayName => "JavaScript";

    public override string PromptText => "JS";
}

public class HtmlTheme : KernelSpecificTheme
{
    public override Style AccentStyle => new(Color.Red3);

    public override string KernelDisplayName => "HTML";

    public override string PromptText => "HTML";
}

public class SqlTheme : KernelSpecificTheme
{
    public override Style AccentStyle => new(Color.Yellow3);

    public override string PromptText => "SQL";
}

internal class DelegatingPrompt : ILineEditorPrompt
{
    public DelegatingPrompt(string prompt, string? more = null)
    {
        InnerPrompt = new LineEditorPrompt(prompt, more);
    }

    public (Markup Markup, int Margin) GetPrompt(ILineEditorState state, int line)
    {
        return InnerPrompt.GetPrompt(state, line);
    }

    public ILineEditorPrompt InnerPrompt { get; set; }
}