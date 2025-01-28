using System.Linq;
using Microsoft.DotNet.Interactive;
using Spectre.Console;

namespace dotnet_repl;

internal static class RenderableExtensions
{
    public static void ShowMagics(this Grid grid, Kernel kernel)
    {
        grid.AddRow(new Paragraph("🧙‍ Magic commands:"));

        var magics = new Table().Expand();
        magics.AddColumn("[italic]Kernel[/]");
        magics.AddColumn("[italic]Command[/]");
        magics.AddColumn("[italic]What it does[/]");
        kernel.VisitSubkernelsAndSelf(k =>
        {
            var kernelName = k.Name == ".NET"
                                 ? "root"
                                 : k.Name;

            foreach (var magic in k.KernelInfo.SupportedDirectives.Where(d => !d.Hidden))
            {
                magics.AddRow(kernelName, magic.Name, Markup.Escape(magic.Description ?? ""));
            }
        });

        grid.AddRow(magics);
    }

    public static void ShowShortcutKeys(this Grid grid)
    {
        grid.AddRow(new Paragraph("⌨ Shortcut keys:"));

        var shortcutKeys = new Table();
        shortcutKeys.AddColumn("[italic]Key[/]");
        shortcutKeys.AddColumn("[italic]What it does[/]");
        shortcutKeys.AddRow("Shift+Enter", "Inserts a newline without submitting the current code");
        shortcutKeys.AddRow("Tab", "Show next completion");
        shortcutKeys.AddRow("Shift+Tab", "Show previous completion");
        shortcutKeys.AddRow("Ctrl+C", "Exit the REPL");
        shortcutKeys.AddRow("Ctrl+Up", "Go back through your submission history (current session only)");
        shortcutKeys.AddRow("Ctrl+Down", "Go forward through your submission history (current session only)");

        grid.AddRow(shortcutKeys);
    }
}