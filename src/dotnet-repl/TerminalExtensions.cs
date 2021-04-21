using System;
using System.CommandLine.IO;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Microsoft.DotNet.Interactive.Repl
{
    public static class TerminalExtensions
    {
        public static void Render(this ITerminal terminal, IRenderable markup)
        {
            var _ansiConsole = ToAnsiConsole(terminal);

            _ansiConsole.Render(markup);
        }

        private static IAnsiConsole ToAnsiConsole(this ITerminal terminal)
        {
            return new AnsiConsoleFactory().Create(new AnsiConsoleSettings
            {
                Out = terminal.Out.ToTextWriter()
            });
        }

        public static Progress Progress(this ITerminal terminal)
        {
            return terminal.ToAnsiConsole().Progress();
        }

        public static Status Status(this ITerminal terminal)
        {
            return terminal.ToAnsiConsole().Status();
        }

        public static TextWriter ToTextWriter(this IStandardStreamWriter writer)
        {
            return new PassThroughWriter(writer.Write);
        }

        private class PassThroughWriter : TextWriter
        {
            private readonly Action<string> _write;

            public PassThroughWriter(Action<string> write)
            {
                _write = write;
            }

            public override Encoding Encoding => Console.Out.Encoding;

            public override void Write(string? value)
            {
                _write(value!);
            }

            public override void Write(char value)
            {
                base.Write(value);
            }

            public override void Write(char[]? buffer)
            {
                base.Write(buffer);
            }

            public override void Write(char[] buffer, int index, int count)
            {
                base.Write(buffer, index, count);
            }

            public override void Write(ReadOnlySpan<char> buffer)
            {
                base.Write(buffer);
            }

            public override void Write(string format, object? arg0)
            {
                base.Write(format, arg0);
            }

            public override void Write(string format, object? arg0, object? arg1)
            {
                base.Write(format, arg0, arg1);
            }

            public override void Write(string format, object? arg0, object? arg1, object? arg2)
            {
                base.Write(format, arg0, arg1, arg2);
            }

            public override void Write(string format, params object?[] arg)
            {
                base.Write(format, arg);
            }

            public override void Write(StringBuilder? value)
            {
                base.Write(value);
            }

            public override Task WriteAsync(char value)
            {
                return base.WriteAsync(value);
            }

            public override Task WriteAsync(char[] buffer, int index, int count)
            {
                return base.WriteAsync(buffer, index, count);
            }

            public override Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = new CancellationToken())
            {
                return base.WriteAsync(buffer, cancellationToken);
            }

            public override Task WriteAsync(string? value)
            {
                return base.WriteAsync(value);
            }

            public override Task WriteAsync(StringBuilder? value, CancellationToken cancellationToken = new CancellationToken())
            {
                return base.WriteAsync(value, cancellationToken);
            }

            public override void WriteLine()
            {
                base.WriteLine();
            }

            public override void WriteLine(char value)
            {
                base.WriteLine(value);
            }

            public override void WriteLine(char[]? buffer)
            {
                base.WriteLine(buffer);
            }

            public override void WriteLine(char[] buffer, int index, int count)
            {
                base.WriteLine(buffer, index, count);
            }

            public override void WriteLine(ReadOnlySpan<char> buffer)
            {
                base.WriteLine(buffer);
            }

            public override void WriteLine(string? value)
            {
                base.WriteLine(value);
            }

            public override void WriteLine(string format, object? arg0)
            {
                base.WriteLine(format, arg0);
            }

            public override void WriteLine(string format, object? arg0, object? arg1)
            {
                base.WriteLine(format, arg0, arg1);
            }

            public override void WriteLine(string format, object? arg0, object? arg1, object? arg2)
            {
                base.WriteLine(format, arg0, arg1, arg2);
            }

            public override void WriteLine(string format, params object?[] arg)
            {
                base.WriteLine(format, arg);
            }

            public override void WriteLine(StringBuilder? value)
            {
                base.WriteLine(value);
            }

            public override Task WriteLineAsync()
            {
                return base.WriteLineAsync();
            }

            public override Task WriteLineAsync(char value)
            {
                return base.WriteLineAsync(value);
            }

            public override Task WriteLineAsync(char[] buffer, int index, int count)
            {
                return base.WriteLineAsync(buffer, index, count);
            }

            public override Task WriteLineAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = new CancellationToken())
            {
                return base.WriteLineAsync(buffer, cancellationToken);
            }

            public override Task WriteLineAsync(string? value)
            {
                return base.WriteLineAsync(value);
            }

            public override Task WriteLineAsync(StringBuilder? value, CancellationToken cancellationToken = new CancellationToken())
            {
                return base.WriteLineAsync(value, cancellationToken);
            }
        }
    }
}