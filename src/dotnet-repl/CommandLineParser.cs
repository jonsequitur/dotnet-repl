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
            "javascript",
            "html");

    public static Option<FileInfo> RunOption = new Option<FileInfo>(
            "--run",
            description: "Run all of the code in the specified notebook, source code, or script file. To exit when done, set the --exit-after-run option.")
        {
            ArgumentHelpName = "PATH"
        }
        .ExistingOnly();

    public static Option<bool> ExitAfterRunOption = new(
        "--exit-after-run",
        $"Exit after the file specified by {RunOption.Aliases.First()} has run");

    public static Option<DirectoryInfo> WorkingDirOption = new Option<DirectoryInfo>(
            "--working-dir",
            () => new DirectoryInfo(Environment.CurrentDirectory),
            "Working directory to which to change after launching the kernel")
        .ExistingOnly();

    public static Option<IDictionary<string, string>> InputsOption = new(
        "--input",
        description:
        "Specifies in a value for @input tokens in magic commands in the notebook, using the format --input <key>=<value>. Values containing spaces should be wrapped in quotes.",
        parseArgument: result =>
        {
            var dict = new Dictionary<string, string>();

            foreach (var token in result.Tokens.Select(t => t.Value))
            {
                var keyAndValue = token.Split("=", 2);

                if (keyAndValue.Length != 2)
                {
                    result.ErrorMessage = "The --input option requires an argument in the format <key>=<value>";
                    return null;
                }

                dict[keyAndValue[0]] = keyAndValue[1];
            }

            return dict;
        })
    {
        Arity = ArgumentArity.ZeroOrMore
    };

    public static Option<FileInfo> OutputPathOption = new Option<FileInfo>(
            "--output-path",
            description:
            $"Run the file specified by {RunOption.Aliases.First()} and writes the output to the file specified by --output-path")
        .LegalFilePathsOnly();

    public static Option<OutputFormat> OutputFormatOption = new(
        "--output-format",
        description: $"The output format to be used when running a notebook with the {RunOption.Aliases.First()} and {ExitAfterRunOption.Aliases.First()} options",
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
            RunOption,
            WorkingDirOption,
            ExitAfterRunOption,
            OutputFormatOption,
            OutputPathOption,
            InputsOption,
            DescribeCommand(),
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
                RunOption,
                LogPathOption,
                ExitAfterRunOption,
                OutputFormatOption,
                OutputPathOption,
                InputsOption),
            Bind.FromServiceProvider<InvocationContext>());

        return new CommandLineBuilder(rootCommand)
               .UseDefaults()
               .UseHelpBuilder(_ => new SpectreHelpBuilder(LocalizationResources.Instance))
               .Build();

        Command DescribeCommand()
        {
            var notebookArgument = new Argument<FileInfo>("notebook")
                .ExistingOnly();

            var command = new Command("describe")
            {
                notebookArgument
            };

            command.SetHandler(async context =>
            {
                var doc = await DocumentParser.LoadInteractiveDocumentAsync(
                              context.ParseResult.GetValueForArgument(notebookArgument),
                              KernelBuilder.CreateKernel());

                var console = ansiConsole ?? AnsiConsole.Console;

                var inputFields = doc.GetInputFields();

                if (inputFields.Any())
                {
                    console.WriteLine("Parameters", Theme.Default.AnnouncementTextStyle);

                    var table = new Table();
                    table.BorderStyle = Theme.Default.AnnouncementBorderStyle;
                    table.AddColumn(new TableColumn("Name"));
                    table.AddColumn(new TableColumn("Type"));
                    table.AddColumn(new TableColumn("Example"));

                    foreach (var inputField in inputFields)
                    {
                        table.AddRow(inputField.ValueName, inputField.TypeHint, $"--input {inputField.ValueName}=\"parameter value\"");
                    }

                    console.Write(table);
                }
            });

            return command;
        }
    }

    private static async Task<IDisposable> StartAsync(
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

        if (options.FileToRun is { } file)
        {
            notebook = await DocumentParser.LoadInteractiveDocumentAsync(file, kernel);
        }

        if (notebook is { Elements.Count: > 0 })
        {
            if (isTerminal)
            {
                if (options.FileToRun?.Extension is ".ipynb" or ".dib")
                {
                    ansiConsole.Announce($"📓 Running notebook: {options.FileToRun}");
                }
                else
                {
                    ansiConsole.Announce($"📄 Running file: {options.FileToRun}");
                }
            }
        }

        if (!options.IsAutomationMode)
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
            if (notebook is null)
            {
                // TODO: (StartAsync) move this validation to the parser configuration
                ansiConsole.WriteLine($"Option {ExitAfterRunOption.Aliases.First()} option cannot be used without also specifying the {RunOption.Aliases.First()} option.");
                return disposables;
            }

            var resultNotebook = await new NotebookRunner(kernel)
                                     .RunNotebookAsync(
                                         notebook,
                                         options.Inputs,
                                         cancellationToken: context.GetCancellationToken());

            switch (options.OutputFormat)
            {
                case OutputFormat.ipynb:
                {
                    var outputNotebook = resultNotebook.ToJupyterJson();
                    if (options.OutputPath is not null)
                    {
                        await File.WriteAllTextAsync(options.OutputPath.FullName, outputNotebook);
                    }

                    break;
                }

                case OutputFormat.trx:
                {
                    var output = resultNotebook.ToTestOutputDocumentXml();

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
                                   ? 2
                                   : 0;
        }

        return disposables;
    }
}