using System;
using System.Reactive.Linq;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace dotnet_repl.Tests
{
    public class OutputTests : ReplInteractionTests
    {
        public OutputTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Standard_out_is_batched()
        {
            In.Push("Console.Write(\"hello\");Console.Write(\"repl\");");
            In.PushEnter();

            await Repl.ReadyForInput.FirstAsync();

            Out.ToString().Should().Contain("hellorepl");
        }

        [Fact]
        public async Task Standard_error_is_batched()
        {
            In.Push("Console.Error.Write(\"hello\");Console.Error.Write(\"repl\");");
            In.PushEnter();

            await Repl.ReadyForInput.FirstAsync();

            Out.ToString().Should().Contain("hellorepl");
        }
    }
}