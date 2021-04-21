using System;
using System.CommandLine;
using System.CommandLine.IO;

namespace Microsoft.DotNet.Interactive.Repl
{
    public class Terminal : ITerminal
    {
        private readonly IConsole _console;

        public Terminal(IConsole? console = null)
        {
            _console = console ?? new SystemConsole();
        }

        public int BufferHeight => Console.BufferHeight;

        public int BufferWidth => Console.BufferWidth;

        public int CursorLeft => Console.CursorLeft;

        public int CursorTop => Console.CursorTop;

        public void SetCursorPosition(int left, int top) => Console.SetCursorPosition(left, top);

        public IStandardStreamWriter Out => _console.Out;

        public bool IsOutputRedirected => _console.IsOutputRedirected;

        public IStandardStreamWriter Error => _console.Error;

        public bool IsErrorRedirected => _console.IsErrorRedirected;

        public bool IsInputRedirected => _console.IsInputRedirected;
    }
}