using System;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Assent;
using Automation;
using dotnet_repl.Tests.Utility;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Documents;
using Microsoft.DotNet.Interactive.Documents.Jupyter;
using Pocket;
using Spectre.Console;
using Xunit;

namespace dotnet_repl.Tests.Automation;

public class NotebookAutomationTests : IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly StringWriter _writer;
    private readonly string _directory = Path.GetDirectoryName(PathUtility.PathToCurrentSourceFile());
    private readonly Parser _parser;
    private readonly TestConsole console = new();

    private readonly Configuration _assentConfiguration =
        new Configuration()
            .UsingExtension("json")
            .SetInteractive(Debugger.IsAttached);

    public NotebookAutomationTests()
    {
        var ansiConsole = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.Yes,
            Interactive = InteractionSupport.Yes,
            Out = new AnsiConsoleOutput(_writer = new StringWriter())
        });

        _parser = CommandLineParser.Create(ansiConsole, registerForDisposal: d => _disposables.Add(d));

        _disposables.Add(_writer);
    }

    public void Dispose() => _disposables.Dispose();

    [Fact]
    public async Task When_an_ipynb_is_run_and_no_error_is_produced_then_the_exit_code_is_0()
    {
        var result = await _parser.InvokeAsync($"--notebook \"{_directory}/succeed.ipynb\" --exit-after-run", console);

        var output = _writer.ToString();
        console.Error.ToString().Should().BeEmpty();
        result.Should().Be(0);
        output.Should().Contain("Success!");
    }

    [Fact]
    public async Task When_an_ipynb_is_run_and_an_error_is_produced_from_a_cell_then_the_exit_code_is_1()
    {
        var result = await _parser.InvokeAsync($"--notebook \"{_directory}/fail.ipynb\" --exit-after-run", console);

        var output = _writer.ToString();
        console.Error.ToString().Should().BeEmpty();
        result.Should().Be(1);
        output.Should().Contain("Oops!");
    }

    [Fact]
    public async Task Notebook_runner_produces_expected_output()
    {
        using var kernel = KernelBuilder.CreateKernel();

        var runner = new NotebookRunner(kernel);

        var notebookFile = Path.Combine(_directory, "VS Code outputs.ipynb");

        var expectedContent = await File.ReadAllTextAsync(notebookFile);

        var inputDoc = Notebook.Parse(expectedContent, new(kernel.ChildKernels.Select(k => new KernelName(k.Name)).ToArray()));

        var resultDoc = await runner.RunNotebookAsync(inputDoc);

        NormalizeMetadata(resultDoc);

        var resultContent = resultDoc.Serialize();

        this.Assent(resultContent, _assentConfiguration);
    }

    private void NormalizeMetadata(InteractiveDocument document)
    {
        foreach (var element in document.Elements)
        {
            if (element.Metadata is { })
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