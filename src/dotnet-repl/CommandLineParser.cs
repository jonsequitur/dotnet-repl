using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Notebook;
using Pocket;
using Spectre.Console;
using Formatter = Microsoft.DotNet.Interactive.Formatting.Formatter;

namespace dotnet_repl
{
    public static class CommandLineParser
    {
        public static Option<DirectoryInfo> LogPathOption { get; } = new(
            "--log-path",
            "Enable file logging to the specified directory");

        public static Option<string> DefaultKernelOption = new Option<string>(
            "--default-kernel",
            description: "The default language for the kernel",
            getDefaultValue: () => "csharp").FromAmong(
            "csharp", 
            "fsharp", 
            "pwsh",
            "sql");

        public static Option<FileInfo> NotebookOption = new Option<FileInfo>(
                "--notebook",
                description: "After starting the REPL, run all of the cells in the specified notebook file.")
            .ExistingOnly();

        public static Option<bool> ExitAfterRun = new(
            "--exit-after-run",
            "Exit the REPL when the specified notebook or script has run.");

        public static Option<DirectoryInfo> WorkingDirOption = new Option<DirectoryInfo>(
                "--working-dir",
                () => new DirectoryInfo(Environment.CurrentDirectory),
                "Working directory to which to change after launching the kernel.")
            .ExistingOnly();

        public static Parser Create(IAnsiConsole? ansiConsole = null)
        {
            var rootCommand = new RootCommand("dotnet-repl")
            {
                LogPathOption,
                DefaultKernelOption,
                NotebookOption,
                WorkingDirOption,
                ExitAfterRun
            };

            rootCommand.Handler = CommandHandler.Create<StartupOptions, CancellationToken>(
                (options, token) => StartRepl(options, token, ansiConsole ?? AnsiConsole.Console));

            return new CommandLineBuilder(rootCommand)
                .UseDefaults()
                .UseHelpBuilder(context => new SpectreHelpBuilder(context.Console))
                .Build();
        }

        private static async Task StartRepl(
            StartupOptions options,
            CancellationToken cancellationToken,
            IAnsiConsole ansiConsole)
        {
            new DefaultSpectreFormatterSet().Register();

            var theme = KernelSpecificTheme.GetTheme(options.DefaultKernelName);

            ansiConsole.RenderSplash(options, theme ?? new CSharpTheme());

            var kernel = Repl.CreateKernel(options);

            NotebookDocument? notebook = default;

            if (options.Notebook is { })
            {
                var content = await File.ReadAllTextAsync(options.Notebook.FullName, cancellationToken);
                var rawData = Encoding.UTF8.GetBytes(content);
                notebook = kernel.ParseNotebook(options.Notebook.FullName, rawData);

                if (notebook.Cells.Any())
                {
                    ansiConsole.Announce($"📓 Running notebook: {options.Notebook}");
                }
            }

            using var disposable = new CompositeDisposable();

            using var repl = new Repl(kernel, disposable.Dispose, ansiConsole);

            disposable.Add(repl);

            cancellationToken.Register(() => repl.Dispose());

            Formatter.DefaultMimeType = PlainTextFormatter.MimeType;

            await repl.RunAsync(notebook, options.ExitAfterRun);
        }
    }
}