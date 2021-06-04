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
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Notebook;
using Microsoft.DotNet.Interactive.PowerShell;
using Pocket;
using Spectre.Console;
using static Pocket.Logger;
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
            getDefaultValue: () => "csharp").AddSuggestions("csharp", "fsharp");

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
                (options, token) => StartRepl(options, token, ansiConsole ?? new AnsiConsoleFactory().Create(new AnsiConsoleSettings())));

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

            ansiConsole.RenderSplash(options);

            var kernel = CreateKernel(options);

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

            using var loop = new LoopController(kernel, disposable.Dispose, ansiConsole);

            disposable.Add(loop);

            cancellationToken.Register(() => loop.Dispose());

            Formatter.DefaultMimeType = PlainTextFormatter.MimeType;

            await loop.RunAsync(notebook, options.ExitAfterRun);
        }

        public static CompositeKernel CreateKernel(StartupOptions options)
        {
            using var _ = Log.OnEnterAndExit("Creating Kernels");

            var compositeKernel = new CompositeKernel()
                .UseDebugDirective()
                .UseAboutMagicCommand();

            compositeKernel.Add(
                new CSharpKernel()
                    .UseNugetDirective()
                    .UseKernelHelpers()
                    .UseWho()
                    .UseDotNetVariableSharing(),
                new[] { "c#", "C#" });

            compositeKernel.Add(
                new FSharpKernel()
                    .UseDefaultFormatting()
                    .UseNugetDirective()
                    .UseKernelHelpers()
                    .UseWho()
                    .UseDefaultNamespaces()
                    .UseDotNetVariableSharing(),
                new[] { "f#", "F#" });

            compositeKernel.Add(
                new PowerShellKernel()
                    .UseProfiles()
                    .UseDotNetVariableSharing(),
                new[] { "powershell" });

            compositeKernel.Add(
                new KeyValueStoreKernel()
                    .UseWho());

            var kernel = compositeKernel
                .UseKernelClientConnection(new ConnectNamedPipe());

            compositeKernel.Add(new SQLKernel());
            compositeKernel.UseQuitCommand();

            if (options.Verbose)
            {
                kernel.LogEventsToPocketLogger();
            }

            kernel.DefaultKernelName = options.DefaultKernelName;

            if (kernel.DefaultKernelName == "fsharp")
            {
                kernel.FindKernel("fsharp").DeferCommand(new SubmitCode("Formatter.Register(fun(x: obj)(writer: TextWriter)->fprintfn writer \"%120A\" x)"));
            }

            return kernel;
        }
    }
}