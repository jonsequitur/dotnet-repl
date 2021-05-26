using System;
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.IO;
using System.IO;
using Spectre.Console;

namespace dotnet_repl
{
    internal class SpectreHelpBuilder : HelpBuilder
    {
        private readonly IConsole _actualConsole;

        public SpectreHelpBuilder(IConsole console, int maxWidth = 2147483647) : base(new TestConsole(), maxWidth)
        {
            _actualConsole = console ?? throw new ArgumentNullException(nameof(console));
        }

        public override void Write(ICommand command)
        {
            using var writer = new StringWriter();

            var ansiConsole = new AnsiConsoleFactory().Create(new AnsiConsoleSettings
            {
                Ansi = AnsiSupport.Yes,
                Out = new AnsiConsoleOutput(writer)
            });

            base.Write(command);

            ansiConsole.Write(base.Console.Out.ToString() ?? string.Empty);

            _actualConsole.Out.Write(writer.ToString());
        }

        protected override void AddSynopsis(ICommand command)
        {
            base.AddSynopsis(command);
        }

        protected override void AddUsage(ICommand command)
        {
            base.AddUsage(command);
        }

        protected override void AddCommandArguments(ICommand command)
        {
            base.AddCommandArguments(command);
        }

        protected override void AddOptions(ICommand command)
        {
            base.AddOptions(command);
        }

        protected override void AddSubcommands(ICommand command)
        {
            base.AddSubcommands(command);
        }

        protected override void AddAdditionalArguments(ICommand command)
        {
            base.AddAdditionalArguments(command);
        }
    }
}