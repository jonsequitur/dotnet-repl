using System.Threading.Tasks;
using FluentAssertions;
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
            In.SendString("Console.Write(\"hello\");Console.Write(\"repl\");");
            In.SendEnter();

            await Repl.WaitingForInputAsync();

            Out.ToString().Should().Contain("hellorepl");
        }

        [Fact]
        public async Task Standard_error_is_batched()
        {
            In.SendString("Console.Error.Write(\"hello\");Console.Error.Write(\"repl\");");
            In.SendEnter();

            await Repl.WaitingForInputAsync();

            Out.ToString().Should().Contain("hellorepl");
        }
    }
}