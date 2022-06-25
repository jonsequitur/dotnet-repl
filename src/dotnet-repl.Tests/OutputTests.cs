using System;
using System.Reactive.Linq;
using FluentAssertions;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace dotnet_repl.Tests;

public class OutputTests : ReplInteractionTests
{
    private readonly ITestOutputHelper _output;

    public OutputTests(ITestOutputHelper output) : base(output)
    {
        _output = output;
    }

    [Fact(Skip = "later")]
    public async Task Standard_out_is_batched()
    {
        var events = Repl.ReadyForInput.Count();
        using var _ = Repl.ReadyForInput.Count().Subscribe(count =>
        {
            _output.WriteLine($"Count: {count}");
        });

        In.Push("Console.Write(\"hello\");Console.Write(\"repl\");");
        In.PushEnter();

        // await Task.Delay(3000);

        await Repl.ReadyForInput.FirstAsync();
        await Task.Delay(3000);

        // await events.FirstAsync(count => count >= 1);

        Out.ToString().Should().Contain("hellorepl");
    }

    [Fact(Skip = "later")]
    public async Task Standard_error_is_batched()
    {
        var events = Repl.ReadyForInput.Replay();

        In.Push("Console.Error.Write(\"hello\");Console.Error.Write(\"repl\");");
        In.PushEnter();

        events.Connect();
        await events.FirstAsync();
        await Task.Delay(3000);

        Out.ToString().Should().Contain("hellorepl");
    }
}