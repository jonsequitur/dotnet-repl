using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Completions;
using System.CommandLine.Help;
using System.IO;
using System.Linq;
using Spectre.Console;

namespace dotnet_repl;

internal class SpectreHelpBuilder : HelpBuilder
{
    private readonly int _maxWidth;

    public SpectreHelpBuilder(int maxWidth = int.MaxValue) : base(maxWidth)
    {
        _maxWidth = maxWidth;
        
        // Set up custom layout that uses Spectre sections instead of default HelpBuilder sections
        CustomizeLayout(GetSpectreLayout);
    }

    private IEnumerable<Func<HelpContext, bool>> GetSpectreLayout(HelpContext context)
    {
        yield return ctx => RenderSection(ctx, TitleSection);
        yield return ctx => RenderSection(ctx, CommandUsageSection);
        yield return ctx => RenderSection(ctx, OptionsSection);
        yield return ctx => RenderSection(ctx, ReplHelpSection);
    }

    private bool RenderSection(HelpContext context, Action<IAnsiConsole, Command> section)
    {
        var console = CreateSpectreConsole(context.Output);
        section(console, context.Command);
        return true;
    }

    private IAnsiConsole CreateSpectreConsole(TextWriter output) =>
        AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.Yes,
            Out = new AnsiConsoleOutput(output)
        });

    public void ShowHelp(Command command, TextWriter output)
    {
        var spectreConsole = CreateSpectreConsole(output);

        TitleSection(spectreConsole, command);
        CommandUsageSection(spectreConsole, command);
        OptionsSection(spectreConsole, command);
        ReplHelpSection(spectreConsole, command);
    }

    private void TitleSection(IAnsiConsole console)
    {
        var panel = new Grid();
        panel.AddColumn(new GridColumn());
        var figletText = new FigletText(".NET REPL").Color(Color.SandyBrown);
        figletText.Justification = Justify.Center;
        panel.AddRow(figletText);
        console.Write(panel);
    }

    private void TitleSection(IAnsiConsole console, Command command)
    {
        var panel = new Grid();
        panel.AddColumn(new GridColumn());
        var figletText = new FigletText(".NET REPL").Color(Color.SandyBrown);
        figletText.Justification = Justify.Center;
        panel.AddRow(figletText);
        console.Write(panel);
    }
    private void CommandUsageSection(IAnsiConsole console, Command command)
    {
        if (command is RootCommand)
        {
            console.Write(new Markup("🔵[sandybrown italic] Start the REPL like this:[/]\n\n"));
        }

        var panel = new Panel($"{command.Name} [[[Magenta1]options[/]]]")
                    .NoBorder()
                    .Expand();

        console.Write(panel);
    }

    private void OptionsSection(IAnsiConsole console, Command command)
    {
        if (!command.Options.Any())
        {
            return;
        }

        var table = new Table()
                    .AddColumn("[magenta1 italic]Option[/]")
                    .AddColumn("[magenta1 italic]Description[/]")
                    .BorderColor(Color.Magenta1);

        foreach (var option in command.Options)
        {
            var aliases = string.Join(", ", option.Aliases
                                                  .Concat(new[] { option.Name })
                                                  .Where(a => !a.StartsWith("/"))
                                                  .OrderBy(a => a.Length));

            table.AddRow(
                $"{aliases} {OptionArgumentHelpName(option)}",
                option.Description ?? "");
        }

        console.Write(table);

        string OptionArgumentHelpName(Option option)
        {
            if (option.HelpName is not null)
            {
                return InAngleBrackets($"{option.HelpName}");
            }

            if (option.ValueType == typeof(bool))
            {
                return "";
            }

            var completions = option.GetCompletions(CompletionContext.Empty).ToArray();
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
    }

    private void ReplHelpSection(IAnsiConsole console, Command command)
    {
        if (command is not RootCommand)
        {
            return;
        }

        console.Write(new Markup("🟢[sandybrown italic] Once it's running, here are some things you can do:[/]\n\n"));

        var grid = new Grid();
        grid.AddColumn();

        // Add your REPL shortcut keys or help rows here
        // grid.AddRow(...);

        console.Write(grid);
    }
}