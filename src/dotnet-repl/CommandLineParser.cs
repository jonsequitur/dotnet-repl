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
            "html",
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

    public static Option<IDictionary<string, string>> InputsOption = new(
        "--input",
        description:
        "Specifies in a value for @input tokens in magic commands in the notebook, using the format --input <key>=<value>",
        parseArgument: result =>
        {
            var dict = new Dictionary<string, string>();

            foreach (var token in result.Tokens.Select(t => t.Value))
            {
                var keyAndValue = token.Split("=");
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
            "Run the file specified by --notebook and writes the output to the file specified by --output-path")
        .LegalFilePathsOnly();

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
            OutputPathOption,
            InputsOption,
            ConvertCommand(),
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
                NotebookOption,
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

        Command ConvertCommand()
        {
            // FIX: (ConvertCommand) 

            var notebookOption = new Option<FileInfo>("--notebook", "The notebook file to convert")
                .ExistingOnly();

            var outputPathOption = new Option<FileInfo>("--output-path")
                .LegalFilePathsOnly();

            var outputFormatOption = new Option<OutputFormat>(
                "--output-format",
                description: $"The output format to be used when running a notebook with the {NotebookOption.Aliases.First()} and {ExitAfterRunOption.Aliases.First()} options",
                getDefaultValue: () => OutputFormat.ipynb);

            var command = new Command("convert")
            {
                notebookOption,
                outputPathOption,
                outputFormatOption
            };

            return command;
        }

        Command DescribeCommand()
        {
            // FIX: (DescribeCommand) 

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

        if (options.Notebook is { } file)
        {
            notebook = await DocumentParser.LoadInteractiveDocumentAsync(file, kernel);
        }

        if (notebook is { Elements.Count: > 0 })
        {
            if (isTerminal)
            {
                ansiConsole.Announce($"📓 Running notebook: {options.Notebook}");
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
                ansiConsole.WriteLine($"Option {ExitAfterRunOption.Aliases.First()} option cannot be used without also specifying the {NotebookOption.Aliases.First()} option.");
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
                    var outputNotebook = resultNotebook.SerializeToJupyter();
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