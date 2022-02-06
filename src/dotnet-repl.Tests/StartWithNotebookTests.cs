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

namespace dotnet_repl.Tests;

public class StartWithNotebookTests : IDisposable

{
    private readonly IAnsiConsole _ansiConsole;

    private readonly StringWriter _writer;

    public StartWithNotebookTests()
    {
        _ansiConsole = new AnsiConsoleFactory().Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.Yes,
            Interactive = InteractionSupport.Yes,
            Out = new AnsiConsoleOutput(_writer = new StringWriter())
        });
    }

    public void Dispose() => _writer.Dispose();

    [Fact(Skip = "Later")]
    public async Task when_an_ipynb_is_specified_it_runs_it()
    {
        using var disposables = new CompositeDisposable();
        var directory = Path.GetDirectoryName(PathUtility.PathToCurrentSourceFile());

        var parser = CommandLineParser.Create(_ansiConsole, registerForDisposal: d => disposables.Add(d));

        var console = new TestConsole();
        var result = await parser.InvokeAsync($"--notebook \"{directory}/test.ipynb\" --exit-after-run", console);

        console.Error.ToString().Should().BeEmpty();
        result.Should().Be(0);
        _writer.ToString().Should().Contain("Hello from C#");
        _writer.ToString().Should().Contain("Hello from F#");
    }

    [Fact(Skip = "Later")]
    public void When_an_ipynb_is_run_and_no_error_is_produced_then_the_exit_code_is_0()
    {
        // TODO-JOSEQU (When_an_ipynb_is_run_and_an_error_is_produced_from_a_cell_then_the_exit_code_is_1) write test
        Assert.True(false, "Test When_an_ipynb_is_run_and_an_error_is_produced_from_a_cell_then_the_exit_code_is_1 is not written yet.");
    }

    [Fact(Skip = "Later")]
    public void When_an_ipynb_is_run_and_an_error_is_produced_from_a_cell_then_the_exit_code_is_1()
    {
        // TODO-JOSEQU (When_an_ipynb_is_run_and_an_error_is_produced_from_a_cell_then_the_exit_code_is_1) write test
        Assert.True(false, "Test When_an_ipynb_is_run_and_an_error_is_produced_from_a_cell_then_the_exit_code_is_1 is not written yet.");
    }
}