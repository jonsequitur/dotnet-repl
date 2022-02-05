using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Help;
using System.IO;
using Spectre.Console;

namespace dotnet_repl
{
    internal class SpectreHelpBuilder : HelpBuilder
    {
        public SpectreHelpBuilder(LocalizationResources localizationResources, int maxWidth = 2147483647) : base(localizationResources, maxWidth)
        {
            CustomizeLayout(GetLayout);
        }

        private IEnumerable<HelpSectionDelegate> GetLayout(HelpContext context)
        {
            using var writer = new StringWriter();

            var ansiConsole = new AnsiConsoleFactory().Create(new AnsiConsoleSettings
            {
                Ansi = AnsiSupport.Yes,
                Out = new AnsiConsoleOutput(context.Output)
            });

            yield return SynopsisSection(ansiConsole);
            yield return Default.CommandUsageSection();
            yield return Default.CommandArgumentsSection();
            yield return Default.OptionsSection();
            yield return Default.SubcommandsSection();
            yield return Default.AdditionalArgumentsSection();
        }

        private HelpSectionDelegate SynopsisSection(IAnsiConsole ansiConsole) =>
            _ =>
            {
                ansiConsole.Write(
                    new FigletText("dotnet repl")
                        .Color(Theme.Default.AnnouncementTextStyle.Foreground));
            };
     
    }
}