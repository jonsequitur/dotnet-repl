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

namespace dotnet_repl.Tests.Notebooks;

public class NotebookAutomationTests : IDisposable
{
    private readonly IAnsiConsole _ansiConsole;

    private readonly StringWriter _writer;

    public NotebookAutomationTests()
    {
        _ansiConsole = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.Yes,
            Interactive = InteractionSupport.Yes,
            Out = new AnsiConsoleOutput(_writer = new StringWriter())
        });
    }

    public void Dispose() => _writer.Dispose();

    [Fact]
    public async Task When_an_ipynb_is_run_and_no_error_is_produced_then_the_exit_code_is_0()
    {
        using var disposables = new CompositeDisposable();
        var directory = Path.GetDirectoryName(PathUtility.PathToCurrentSourceFile());

        var parser = CommandLineParser.Create(_ansiConsole, registerForDisposal: d => disposables.Add(d));

        var console = new TestConsole();
        var result = await parser.InvokeAsync($"--notebook \"{directory}/succeed.ipynb\" --exit-after-run", console);

        var output = _writer.ToString();
        console.Error.ToString().Should().BeEmpty();
        result.Should().Be(0);
        output.Should().Contain("Success!");
    }

    [Fact]
    public async Task When_an_ipynb_is_run_and_an_error_is_produced_from_a_cell_then_the_exit_code_is_1()
    {
        using var disposables = new CompositeDisposable();
        var directory = Path.GetDirectoryName(PathUtility.PathToCurrentSourceFile());

        var parser = CommandLineParser.Create(_ansiConsole, registerForDisposal: d => disposables.Add(d));

        var console = new TestConsole();
        var result = await parser.InvokeAsync($"--notebook \"{directory}/fail.ipynb\" --exit-after-run", console);

        var output = _writer.ToString();
        console.Error.ToString().Should().BeEmpty();
        result.Should().Be(1);
        output.Should().Contain("Oops!");
    }
}