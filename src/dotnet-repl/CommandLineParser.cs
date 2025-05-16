using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Help;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automation;
using Microsoft.DotNet.Interactive.Documents;
using Microsoft.DotNet.Interactive.Documents.Jupyter;
using Pocket;
using Spectre.Console;

namespace dotnet_repl;

public static class CommandLineParser
{
    public static Option<DirectoryInfo> LogPathOption = new("--log-path")
    {
        Description = "Enable file logging to the specified directory",
        HelpName = "PATH"
    };

    public static Option<string> DefaultKernelOption = new Option<string>("--default-kernel")
        {
            Description = "The default language for the kernel",
            DefaultValueFactory = _ => Environment.GetEnvironmentVariable("DOTNET_REPL_DEFAULT_KERNEL") ?? "csharp"
        }
        .AcceptOnlyFromAmong(
            "csharp",
            "fsharp",
            "pwsh",
            "javascript",
            "http");

    public static Option<FileInfo> RunOption = new Option<FileInfo>("--run")
        {
            Description = "Run all of the code in the specified notebook, source code, or script file. To exit when done, set the --exit-after-run option.",
            HelpName = "PATH"
        }
        .AcceptExistingOnly();

    public static Option<bool> ExitAfterRunOption = new("--exit-after-run")
    {
        Description = $"Exit after the file specified by {RunOption.Name} has run"
    };

    public static Option<DirectoryInfo> WorkingDirOption = new Option<DirectoryInfo>("--working-dir")
        {
            DefaultValueFactory = _ => new DirectoryInfo(Environment.CurrentDirectory),
            Description = "Working directory to which to change after launching the kernel"
        }
        .AcceptExistingOnly();

    public static Option<IDictionary<string, string>> InputsOption = new("--input")
    {
        Description =
            "Specifies in a value for @input tokens in magic commands in the notebook, using the format --input <key>=<value>. Values containing spaces should be wrapped in quotes.",
        CustomParser = result =>
        {
            var dict = new Dictionary<string, string>();

            foreach (var token in result.Tokens.Select(t => t.Value))
            {
                var keyAndValue = token.Split("=", 2);

                if (keyAndValue.Length is not 2)
                {
                    result.AddError("The --input option requires an argument in the format <key>=<value>");
                    return new Dictionary<string, string>();
                }

                dict[keyAndValue[0]] = keyAndValue[1];
            }

            return dict;
        },
        Arity = ArgumentArity.ZeroOrMore
    };

    public static Option<FileInfo> OutputPathOption = new Option<FileInfo>("--output-path")
        {
            Description = $"Run the file specified by {RunOption.Name} and writes the output to the file specified by --output-path"
        }
        .AcceptLegalFilePathsOnly();

    public static Option<OutputFormat> OutputFormatOption = new("--output-format")
    {
        Description = $"The output format to be used when running a notebook with the {RunOption.Name} and {ExitAfterRunOption.Name} options",
        DefaultValueFactory = _ => OutputFormat.ipynb
    };

    public static RootCommand Create(
        IAnsiConsole? ansiConsole = null,
        Func<StartupOptions, IAnsiConsole, Action<IDisposable>?, CancellationToken, Task<int>>? startRepl = null,
        Action<IDisposable>? registerForDisposal = null)
    {
        var rootCommand = new RootCommand
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

        var helpOption = rootCommand.Options.OfType<HelpOption>().Single();
        ((HelpAction)helpOption.Action).Builder = new SpectreHelpBuilder();

        startRepl ??= StartAsync;

        rootCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            var options = StartupOptions.FromParseResult(parseResult);
            return await startRepl(options, ansiConsole ?? AnsiConsole.Console, registerForDisposal, cancellationToken);
        });

        return rootCommand;

        Command DescribeCommand()
        {
            var notebookArgument = new Argument<FileInfo>("notebook")
                .AcceptExistingOnly();

            var command = new Command("describe")
            {
                notebookArgument
            };

            command.SetAction(async parseResult =>
            {
                var doc = await DocumentParser.LoadInteractiveDocumentAsync(
                              parseResult.GetValue(notebookArgument)!,
                              KernelBuilder.CreateKernel());

                var console = ansiConsole ?? AnsiConsole.Console;

                var inputFields = doc.GetInputFields(_ => new DirectiveParseResult()).ToArray();

                if (inputFields.Length > 0)
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

    private static async Task<int> StartAsync(
        StartupOptions options,
        IAnsiConsole ansiConsole,
        Action<IDisposable>? registerForDisposal = null,
        CancellationToken cancellationToken = default)
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
            
            await repl.RunAsync(
                notebook,
                options.ExitAfterRun);
        }
        else
        {
            if (notebook is null)
            {
                // TODO: (StartAsync) move this validation to the parser configuration
                ansiConsole.WriteLine($"Option {ExitAfterRunOption.Name} option cannot be used without also specifying the {RunOption.Name} option.");
            }

            var resultNotebook = await new NotebookRunner(kernel)
                                     .RunNotebookAsync(
                                         notebook!,
                                         options.Inputs,
                                         cancellationToken: cancellationToken);

            switch (options.OutputFormat)
            {
                case OutputFormat.ipynb:
                {
                    var outputNotebook = resultNotebook.ToJupyterJson();
                    if (options.OutputPath is not null)
                    {
                        await File.WriteAllTextAsync(options.OutputPath.FullName, outputNotebook, cancellationToken);
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
                        await File.WriteAllTextAsync(options.OutputPath.FullName, output, cancellationToken);
                    }

                    break;
                }
            }

           return resultNotebook.Elements.SelectMany(e => e.Outputs).OfType<ErrorElement>().Any()
                                   ? 2
                                   : 0;
        }

        return 0;
    }
}