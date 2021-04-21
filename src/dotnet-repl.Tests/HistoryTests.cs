using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace dotnet_repl.Tests
{
    public class HistoryTests : ReplInteractionTests
    {
        public HistoryTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Up_arrow_restores_last_submission()
        {
            Terminal.SendString("123");
            Terminal.SendEnter();
            Terminal.SendString("456");
            Terminal.SendEnter();

            Terminal.SendKey(ConsoleKey.UpArrow);

            await Terminal.InputConsumed();

            TerminalHandler.CurrentInput.Should().Be("456");

            Terminal.CursorLeft.Should().Be(3);
        }

    }
}