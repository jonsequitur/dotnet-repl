using Spectre.Console;

namespace Microsoft.DotNet.Interactive.Repl
{
    public class PromptMarkup
    {
        public static Markup Ready { get; } = new("[bold aqua]>  [/] ");
        public static Markup More { get; } = new("[bold aqua]...[/] ");
    }
}