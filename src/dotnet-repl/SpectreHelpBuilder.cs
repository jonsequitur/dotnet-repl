using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Help;
using System.Linq;
using Spectre.Console;

namespace dotnet_repl;

internal class SpectreHelpBuilder : HelpBuilder
{
    public SpectreHelpBuilder(LocalizationResources localizationResources, int maxWidth = 2147483647) : base(localizationResources, maxWidth)
    {
        CustomizeLayout(GetLayout);
    }

    private IEnumerable<HelpSectionDelegate> GetLayout(HelpContext context)
    {
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.Yes,
            Out = new AnsiConsoleOutput(context.Output)
        });

        yield return TitleSection(console);

        yield return CommandUsageSection(console);

        yield return OptionsSection(console);

        yield return ReplHelpSection(console);
    }

    private HelpSectionDelegate TitleSection(IAnsiConsole console) =>
        _ =>
        {
            var panel = new Grid();
            panel.AddColumn(new GridColumn());
            panel.AddRow(new FigletText(".NET REPL").Color(Color.SandyBrown).Alignment(Justify.Center));
            console.Write(panel);
        };

    private HelpSectionDelegate CommandUsageSection(IAnsiConsole console) =>
        ctx =>
        {
            console.Write(new Markup("🔵[sandybrown italic] Start the REPL like this:[/]\n\n"));

            var panel = new Panel($"{ctx.Command.Name} [[[Magenta1]options[/]]]")
                        .NoBorder()
                        .Expand();

            console.Write(panel);
        };

    private HelpSectionDelegate OptionsSection(IAnsiConsole console) =>
        ctx =>
        {
            var table = new Table()
                        .AddColumn("[magenta1 italic]Option[/]")
                        .AddColumn("[magenta1 italic]Description[/]")
                        .BorderColor(Color.Magenta1);

            foreach (var option in ctx.Command.Options)
            {
                var aliases = string.Join(", ", option.Aliases
                                                      .Where(a => !a.StartsWith("/"))
                                                      .OrderBy(a => a.Length));

                table.AddRow(
                    $"{aliases} {OptionArgumentHelpName(option)}",
                    option.Description ?? "");
            }

            console.Write(table);

            string OptionArgumentHelpName(Option option)
            {
                if (option.ArgumentHelpName is not null)
                {
                    return InAngleBrackets($"{option.ArgumentHelpName}");
                }

                if (option.ValueType == typeof(bool))
                {
                    return "";
                }

                var completions = option.GetCompletions().ToArray();
                if (completions.Length > 0)
                {
                    return InAngleBrackets($"{string.Join("[gray]|[/]", completions.Select(c => c.Label)).Replace("csharp", "[bold aqua]csharp[/]")}");
                }

                return InAngleBrackets(option.Name.ToUpper());
            }

            string InAngleBrackets(string value)
            {
                return $"[gray]<[/]{value}[gray]>[/]";
            }
        };

    private static HelpSectionDelegate ReplHelpSection(IAnsiConsole console)
    {
        return _ =>
        {
            console.Write(new Markup("🟢[sandybrown italic] Once it's running, here are some things you can do:[/]\n\n"));

            var grid = new Grid();
            grid.AddColumn();

            grid.ShowShortcutKeys();

            console.Announce(grid);
        };
    }
}