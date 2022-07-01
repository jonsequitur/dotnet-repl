using System;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading.Tasks;
using dotnet_repl.Tests.Utility;
using FluentAssertions;
using Pocket;
using Spectre.Console;
using Xunit;

namespace dotnet_repl.Tests.Automation;

public class NotebookAutomationTests : IDisposable
{
    private readonly IAnsiConsole _ansiConsole;
    private readonly CompositeDisposable _disposables = new();
    private readonly StringWriter _writer;
    private readonly string _directory = Path.GetDirectoryName(PathUtility.PathToCurrentSourceFile());
    private readonly Parser _parser;
    private readonly TestConsole console = new();

    public NotebookAutomationTests()
    {
        _ansiConsole = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.Yes,
            Interactive = InteractionSupport.Yes,
            Out = new AnsiConsoleOutput(_writer = new StringWriter())
        });

        _parser = CommandLineParser.Create(_ansiConsole, registerForDisposal: d => _disposables.Add(d));

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
}
