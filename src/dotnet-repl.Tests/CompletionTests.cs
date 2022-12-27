using dotnet_repl.LineEditorCommands;
using FluentAssertions;
using RadLine;
using Xunit;
using Xunit.Abstractions;

namespace dotnet_repl.Tests;

public class CompletionTests : ReplInteractionTests
{
    public CompletionTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void it_completes_methods_after_dot()
    {
        var buffer = new LineBuffer("");
        buffer.Insert("Console.");
        buffer.MoveEnd();

        var context = new LineEditorContext(buffer, ServiceProvider);
        context.Execute(new CompletionCommand(AutoComplete.Next));

        buffer.Content.Should().Be("Console.BackgroundColor");
    }

    [Fact]
    public void When_there_are_no_results_it_doesnt_change_the_buffer()
    {
        var buffer = new LineBuffer("");
        buffer.Insert("Console.Zzz");
        buffer.MoveEnd();

        var context = new LineEditorContext(buffer, ServiceProvider);
        context.Execute(new CompletionCommand(AutoComplete.Next));

        buffer.Content.Should().Be("Console.Zzz");
    }

    [Fact(Skip = "WIP")]
    public void It_moves_the_cursor_to_the_insertion_point_if_applicable()
    {
        var buffer = new LineBuffer("");
        buffer.Insert("Console.WriteL");
        buffer.MoveEnd();

        var context = new LineEditorContext(buffer, ServiceProvider);
        context.Execute(new CompletionCommand(AutoComplete.Next));

        buffer.Content.Should().Be("Console.WriteLine()");
        buffer.CursorPosition.Should().Be("Console.WriteLine(".Length);
    }
}