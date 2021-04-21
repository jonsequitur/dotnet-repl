using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Repl;
using Xunit;
using Xunit.Abstractions;

namespace dotnet_repl.Tests
{
    public class BufferTests : ReplInteractionTests
    {
        public BufferTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void Terminal_cursor_position_starts_at_0()
        {


            Terminal.CursorLeft.Should().Be(0);
        }

      

        [Fact]
        public async Task Terminal_cursor_resets_to_0_after_line_submission()
        {

            Terminal.SendString("123");
            Terminal.SendEnter();

            await Terminal.InputConsumed();

            Terminal.CursorLeft.Should().Be(0);
        }

        [Theory]
        [InlineData("a")]
        [InlineData("abc")]
        [InlineData("a b c ")]
        [InlineData("     a b c ")]
        public async Task Cursor_position_increments_as_input_is_typed(string input)
        {

            Terminal.SendString(input);

            await Terminal.InputConsumed();

            Terminal.CursorLeft.Should().Be(input.Length);
        }

        [Theory]
        [InlineData("a")]
        [InlineData("abc")]
        [InlineData("a b c ")]
        [InlineData("     a b c ")]
        public async Task Cursor_position_decrements_on_backspace(string input)
        {

            Terminal.SendString(input);
            Terminal.SendKey(ConsoleKey.Backspace);

            await Terminal.InputConsumed();

            Terminal.CursorLeft.Should().Be(input.Length - 1);
        }
    }
}