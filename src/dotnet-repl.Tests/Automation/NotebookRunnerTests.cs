using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Assent;
using Automation;
using dotnet_repl.Tests.Utility;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Documents;
using Microsoft.DotNet.Interactive.Documents.Jupyter;
using Microsoft.DotNet.Interactive.Events;
using Pocket;
using Spectre.Console;
using Xunit;

namespace dotnet_repl.Tests.Automation;

public class NotebookRunnerTests : IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly string _directory = Path.GetDirectoryName(PathUtility.PathToCurrentSourceFile());
    private readonly RootCommand _rootCommand;

    private readonly Configuration _assentConfiguration =
        new Configuration()
            .UsingExtension("json")
            .SetInteractive(Debugger.IsAttached);

    public NotebookRunnerTests()
    {
        StringWriter writer;
        var ansiConsole = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.Yes,
            Interactive = InteractionSupport.Yes,
            Out = new AnsiConsoleOutput(writer = new StringWriter())
        });

        _rootCommand = CommandLineParser.Create(ansiConsole, registerForDisposal: d => _disposables.Add(d));

        _disposables.Add(writer);
    }

    public void Dispose() => _disposables.Dispose();

    [Fact]
    public async Task When_an_ipynb_is_run_and_no_error_is_produced_then_the_exit_code_is_0()
    {
        var parseResult =_rootCommand.Parse($"--run \"{_directory}/succeed.ipynb\" --exit-after-run");
        parseResult.Configuration.Error = new StringWriter();
        var result = await ((AsynchronousCommandLineAction)_rootCommand.Action).InvokeAsync(parseResult);

        parseResult.Configuration.Error.ToString().Should().BeEmpty();
        result.Should().Be(0);
    }

    [Fact]
    public async Task When_an_ipynb_is_run_and_an_error_is_produced_from_a_cell_then_the_exit_code_is_2()
    {
        var parseResult = _rootCommand.Parse($"--run \"{_directory}/fail.ipynb\" --exit-after-run");
        parseResult.Configuration.Error = new StringWriter();
        var result = await ((AsynchronousCommandLineAction)_rootCommand.Action).InvokeAsync(parseResult);

        parseResult.Configuration.Error.ToString().Should().BeEmpty();
        result.Should().Be(2);
    }

    [Fact]
    public async Task Output_ipynb_metadata_reflects_default_kernel()
    {
        using var kernel = KernelBuilder.CreateKernel();

        kernel.DefaultKernelName = "fsharp";

        var document = new InteractiveDocument
        {
            new("123", "csharp")
        };

        var runner = new NotebookRunner(kernel);

        var outputDoc = await runner.RunNotebookAsync(document);

        outputDoc = Notebook.Parse(outputDoc.ToJupyterJson());

        outputDoc.GetDefaultKernelName().Should().Be("fsharp");
    }

    [Fact]
    public async Task Notebook_runner_produces_expected_output()
    {
        using var kernel = KernelBuilder.CreateKernel();

        var runner = new NotebookRunner(kernel);

        var notebookFile = Path.Combine(_directory, "VS Code outputs.ipynb");

        var expectedContent = await File.ReadAllTextAsync(notebookFile);

        var inputDoc = Notebook.Parse(expectedContent, kernel.CreateKernelInfos());

        var resultDoc = await runner.RunNotebookAsync(inputDoc);

        NormalizeMetadata(resultDoc);

        var resultContent = resultDoc.ToJupyterJson();

        this.Assent(resultContent, _assentConfiguration);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("ABC")]
    public async Task Parameters_can_be_passed_to_input_fields_declared_in_the_notebook(string passedParamName)
    {
        var dibContent = """
                         #!csharp
                         #!set --name abc --value @input:"abc"
                         abc.Display();
                         """;
        var inputs = new Dictionary<string, string>
        {
            [passedParamName] = "hello!"
        };

        using var kernel = KernelBuilder.CreateKernel(new StartupOptions
        {
            ExitAfterRun = true,
            Inputs = inputs
        });

        var inputDoc = CodeSubmission.Parse(dibContent, kernel.CreateKernelInfos());

        var runner = new NotebookRunner(kernel);

        var events = kernel.KernelEvents.ToSubscribedList();

        await runner.RunNotebookAsync(inputDoc, inputs);

        events.Should().NotContainErrors();

        events.Should()
              .ContainSingle<DisplayedValueProduced>()
              .Which
              .Value
              .Should()
              .Be("hello!");
    }

    private void NormalizeMetadata(InteractiveDocument document)
    {
        foreach (var element in document.Elements)
        {
            if (element.Metadata is not null)
            {
                if (element.Metadata.ContainsKey("dotnet_repl_cellExecutionStartTime"))
                {
                    element.Metadata["dotnet_repl_cellExecutionStartTime"] = DateTimeOffset.MinValue;
                }

                if (element.Metadata.ContainsKey("dotnet_repl_cellExecutionEndTime"))
                {
                    element.Metadata["dotnet_repl_cellExecutionEndTime"] = DateTimeOffset.MinValue;
                }
            }
        }
    }
}