using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Documents;
using Pocket;
using Spectre.Console;

namespace dotnet_repl
{
    public static partial class CommandLineParser
    {
        public static Option<DirectoryInfo> LogPathOption { get; } = new(
            "--log-path",
            "Enable file logging to the specified directory");

        public static Option<string> DefaultKernelOption = new Option<string>(
            "--default-kernel",
            description: "The default language for the kernel",
            getDefaultValue: () => Environment.GetEnvironmentVariable("DOTNET_REPL_DEFAULT_KERNEL") ?? "csharp")
                .FromAmong(
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

        public static Parser Create(
            IAnsiConsole? ansiConsole = null,
            Func<StartupOptions, IAnsiConsole, CancellationToken, Task<IDisposable>>? startRepl = null,
            Action<IDisposable>? registerForDisposal = null)
        {
            var rootCommand = new RootCommand("dotnet-repl")
            {
                LogPathOption,
                DefaultKernelOption,
                NotebookOption,
                WorkingDirOption,
                ExitAfterRun
            };

            startRepl ??= StartRepl;

            rootCommand.Handler = CommandHandler.Create<StartupOptions, CancellationToken>(
                (options, token) =>
                {
                    var repl = startRepl(options, ansiConsole ?? AnsiConsole.Console, token);
                    registerForDisposal?.Invoke(repl);
                    return repl;
                });

            return new CommandLineBuilder(rootCommand)
                .UseDefaults()
                .UseHelpBuilder(context => new SpectreHelpBuilder(context.Console))
                .Build();
        }

        public static async Task<IDisposable> StartRepl(
            StartupOptions options,
            IAnsiConsole ansiConsole,
            CancellationToken cancellationToken)
        {
            var theme = KernelSpecificTheme.GetTheme(options.DefaultKernelName);

            ansiConsole.RenderSplash(theme ?? new CSharpTheme());

            var kernel = Repl.CreateKernel(options);

            InteractiveDocument? notebook = default;

            if (options.Notebook is { } file)
            {
                notebook = await DocumentParser.ReadFileAsInteractiveDocument(file, kernel);

                if (notebook.Elements.Any())
                {
                    ansiConsole.Announce($"📓 Running notebook: {options.Notebook}");
                }
            }

            using var disposable = new CompositeDisposable();

            using var repl = new Repl(kernel, disposable.Dispose, ansiConsole);

            disposable.Add(repl);
            disposable.Add(kernel);

            cancellationToken.Register(() => disposable.Dispose());

            await repl.RunAsync(notebook, options.ExitAfterRun);

            return disposable;
        }
    }
}