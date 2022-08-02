using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Automation;
using Microsoft.DotNet.Interactive.Documents;
using Microsoft.DotNet.Interactive.Documents.Jupyter;
using Microsoft.DotNet.Interactive.Formatting;
using Pocket;
using Spectre.Console;
using TRexLib;
using Formatter = Microsoft.DotNet.Interactive.Formatting.Formatter;

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
            description: "Run all of the cells in the specified notebook file")
        {
            ArgumentHelpName = "PATH"
        }
        .ExistingOnly();

    public static Option<bool> ExitAfterRunOption = new(
        "--exit-after-run",
        $"Exit after the file specified by {NotebookOption.Aliases.First()} has run");

    public static Option<DirectoryInfo> WorkingDirOption = new Option<DirectoryInfo>(
            "--working-dir",
            () => new DirectoryInfo(Environment.CurrentDirectory),
            "Working directory to which to change after launching the kernel")
        .ExistingOnly();

    public static Option<FileInfo> OutputPathOption = new(
        "--output-path",
        description:
        "Run the file specified by --notebook and writes the output to the file specified by --output-path");

    public static Option<OutputFormat> OutputFormatOption = new(
        "--output-format",
        description: $"The output format to be used when running a notebook with the {NotebookOption.Aliases.First()} and {ExitAfterRunOption.Aliases.First()} options",
        getDefaultValue: () => OutputFormat.ipynb);

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
            ExitAfterRunOption,
            OutputFormatOption,
            OutputPathOption
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
                ExitAfterRunOption,
                OutputFormatOption,
                OutputPathOption),
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
        var disposables = new CompositeDisposable();

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

        var isAutomationMode = options.ExitAfterRun || !isTerminal || options.OutputPath is { };

        if (!isAutomationMode)
        {
            Repl.UseDefaultSpectreFormatting();

            using var repl = new Repl(kernel, disposables.Dispose, ansiConsole);

            disposables.Add(repl);

            disposables.Add(kernel);

            context.GetCancellationToken().Register(() => disposables.Dispose());

            await repl.RunAsync(
                i => context.ExitCode = i,
                notebook,
                options.ExitAfterRun);
        }
        else
        {
            Formatter.ResetToDefault();
            Formatter.DefaultMimeType = HtmlFormatter.MimeType;

            if (notebook is null)
            {
                // TODO: (StartAsync) move this validation to the parser configuration
                ansiConsole.WriteLine($"Option {ExitAfterRunOption.Aliases.First()} option cannot be used without also specifying the {NotebookOption.Aliases.First()} option.");
                return disposables;
            }

            var resultNotebook = await new NotebookRunner(kernel)
                                     .RunNotebookAsync(
                                         notebook,
                                         context.GetCancellationToken());

            switch (options.OutputFormat)
            {
                case OutputFormat.ipynb:
                {
                    var outputNotebook = resultNotebook.Serialize();
                    if (options.OutputPath is null)
                    {
                        ansiConsole.Write(outputNotebook);
                    }
                    else
                    {
                        await File.WriteAllTextAsync(options.OutputPath.FullName, outputNotebook);
                    }

                    break;
                }

                case OutputFormat.trx:
                {
                    var output = WriteTrx(resultNotebook);

                    if (options.OutputPath is null)
                    {
                        ansiConsole.Write(output);
                    }
                    else
                    {
                        await File.WriteAllTextAsync(options.OutputPath.FullName, output);
                    }

                    break;
                }
            }

            context.ExitCode = resultNotebook.Elements.SelectMany(e => e.Outputs).OfType<ErrorElement>().Any()
                                   ? 1
                                   : 0;
        }

        return disposables;
    }

    private static string WriteTrx(InteractiveDocument resultNotebook)
    {
        var testResults = new List<TestResult>();

        for (var i = 0; i < resultNotebook.Elements.Count; i++)
        {
            var element = resultNotebook.Elements[i];

            var content = element.Contents.Trim().Replace("\r", "").Replace("\n", " ") ?? "";

            var testResult = new TestResult(
                fullyQualifiedTestName: $"Cell {i + 1,4}: {content.Substring(0, Math.Min(25, content.Length))}",
                outcome: element.Outputs.OfType<ErrorElement>().Any()
                             ? TestOutcome.Failed
                             : TestOutcome.Passed,
                output: element.Outputs.FirstOrDefault() switch
                {
                    DisplayElement displayElement => displayElement.Data.FirstOrDefault().Value.ToString(),
                    ErrorElement errorElement1 => errorElement1.ErrorValue,
                    ReturnValueElement returnValueElement => returnValueElement.Data.FirstOrDefault().Value.ToString(),
                    TextElement textElement => textElement.Text,
                    _ => null
                },
                stacktrace: element.Outputs.FirstOrDefault() switch
                {
                    ErrorElement errorElement => string.Join("\n", errorElement.StackTrace),
                    _ => null
                });

            testResults.Add(testResult);
        }

        using var writer = new StringWriter();

        var testOutputWriter = new TestOutputFileWriter(writer);

        testOutputWriter.Write(new TestResultSet(testResults));

        var output = writer.ToString();
        return output;
    }
}