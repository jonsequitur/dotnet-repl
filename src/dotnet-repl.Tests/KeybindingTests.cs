using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace dotnet_repl.Tests
{
    public class KeybindingTests
    {
        [Theory]
        [MemberData(nameof(KeyBindings))]
        public void ToKeybindingString_generates_VS_Code_style_keybinding_strings(ConsoleKeyInfo keyInfo, string expectedValue)
        {
            keyInfo.ToKeybindingString().Should().Be(expectedValue);
        }

        public static IEnumerable<object[]> KeyBindings()
        {
            foreach (var (writeOperation, expectedValue) in bindings())
            {
                yield return new object[] { writeOperation, expectedValue };
            }

            IEnumerable<(ConsoleKeyInfo keyInfo, string expectedValue)> bindings()
            {
                // The char is ignored so we'll just use space for all of these.
                yield return (new ConsoleKeyInfo(' ', ConsoleKey.Backspace, false, false, false), "backspace");
                yield return (new ConsoleKeyInfo(' ', ConsoleKey.Backspace, false, false, true), "ctrl+backspace");
                yield return (new ConsoleKeyInfo(' ', ConsoleKey.C, false, false, true), "ctrl+c");
                yield return (new ConsoleKeyInfo(' ', ConsoleKey.Delete, false, false, true), "ctrl+delete");
                yield return (new ConsoleKeyInfo(' ', ConsoleKey.Delete, false, false, false), "delete");
                yield return (new ConsoleKeyInfo(' ', ConsoleKey.Home, false, false, false), "home");
                yield return (new ConsoleKeyInfo(' ', ConsoleKey.End, false, false, false), "end");
                yield return (new ConsoleKeyInfo(' ', ConsoleKey.Enter, false, false, false), "enter");
                yield return (new ConsoleKeyInfo(' ', ConsoleKey.Escape, false, false, false), "escape");
                yield return (new ConsoleKeyInfo(' ', ConsoleKey.LeftArrow, false, false, false), "left");
                yield return (new ConsoleKeyInfo(' ', ConsoleKey.RightArrow, false, false, false), "right");
                yield return (new ConsoleKeyInfo(' ', ConsoleKey.UpArrow, false, false, false), "up");
                yield return (new ConsoleKeyInfo(' ', ConsoleKey.DownArrow, false, false, false), "down");
                yield return (new ConsoleKeyInfo(' ', ConsoleKey.UpArrow, true, true, true), "ctrl+shift+alt+up");
            }
        }
    }
}