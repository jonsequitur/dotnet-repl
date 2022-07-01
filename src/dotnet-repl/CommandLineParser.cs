using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Documents;
using Microsoft.DotNet.Interactive.Documents.Jupyter;
using Pocket;
using Spectre.Console;

namespace dotnet_repl;

public static class CommandLineParser
{
    public static Option<DirectoryInfo> LogPathOption { get; } = new(
        "--log-path",
        "Enable file logging to the specified directory")
    {
        ArgumentHelpName = "PATH"
    };

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
            description: "After starting the REPL, run all of the cells in the specified notebook file")
        {
            ArgumentHelpName = "PATH"
        }
        .ExistingOnly();

    public static Option<bool> ExitAfterRun = new(
        "--exit-after-run",
        "Exit the REPL when the specified notebook or script has run");

    public static Option<DirectoryInfo> WorkingDirOption = new Option<DirectoryInfo>(
            "--working-dir",
            () => new DirectoryInfo(Environment.CurrentDirectory),
            "Working directory to which to change after launching the kernel")
        .ExistingOnly();

    public static Parser Create(
        IAnsiConsole? ansiConsole = null,
        Func<StartupOptions, IAnsiConsole, InvocationContext, Task<IDisposable>>? startRepl = null,
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

        startRepl ??= StartAsync;

        rootCommand.SetHandler(
            async (options, context) =>
            {
                var disposable = await startRepl(options, ansiConsole ?? AnsiConsole.Console, context);
                registerForDisposal?.Invoke(disposable);
            },
            new StartupOptionsBinder(
                DefaultKernelOption,
                WorkingDirOption,
                NotebookOption,
                LogPathOption,
                ExitAfterRun),
            Bind.FromServiceProvider<InvocationContext>());

        return new CommandLineBuilder(rootCommand)
               .UseDefaults()
               .UseHelpBuilder(_ => new SpectreHelpBuilder(LocalizationResources.Instance))
               .Build();
    }

    public static async Task<IDisposable> StartAsync(
        StartupOptions options,
        IAnsiConsole ansiConsole,
        InvocationContext context)
    {
        using var disposables = new CompositeDisposable();

        var isTerminal = ansiConsole.Profile.Out.IsTerminal;

        if (isTerminal)
        {
            var theme = KernelSpecificTheme.GetTheme(options.DefaultKernelName);
            ansiConsole.RenderSplash(theme ?? new CSharpTheme());
        }

        var kernel = KernelBuilder.CreateKernel(options);

        InteractiveDocument? notebook = default;

        if (options.Notebook is { } file)
        {
            notebook = await DocumentParser.ReadFileAsInteractiveDocument(file, kernel);
        }

        if (notebook is { } && notebook.Elements.Any())
        {
            if (isTerminal)
            {
                ansiConsole.Announce($"📓 Running notebook: {options.Notebook}");
            }
        }

        if (options.ExitAfterRun && !isTerminal)
        {
            if (notebook is null)
            {
                // TODO: (StartAsync) move this validation to the parser configuration
                ansiConsole.WriteLine($"Option {ExitAfterRun.Aliases.First()} option cannot be used without also specifying the {NotebookOption.Aliases.First()} option.");
                return disposables;
            }

            var resultDocument = await new NotebookRunner(kernel)
                                     .RunNotebookAsync(
                                         notebook,
                                         context.GetCancellationToken());

            await using var writer = new StringWriter();
            Notebook.Write(resultDocument, "\n", writer);
            ansiConsole.Write(writer.ToString());

            context.ExitCode = resultDocument.Elements.SelectMany(e => e.Outputs).OfType<ErrorElement>().Any()
                                   ? 1
                                   : 0;
        }
        else
        {
            using var repl = new Repl(kernel, disposables.Dispose, ansiConsole);

            disposables.Add(repl);

            disposables.Add(kernel);

            context.GetCancellationToken().Register(() => disposables.Dispose());

            await repl.RunAsync(
                i => context.ExitCode = i,
                notebook,
                options.ExitAfterRun);
        }

        return disposables;
    }
}