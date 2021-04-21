using System;
using System.Collections.Concurrent;
using System.CommandLine.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Repl;

namespace dotnet_repl.Tests
{
    public class TestTerminal : TestConsole, ITerminal
    {
        private readonly BlockingCollection<ConsoleKeyInfo> _input = new();

        private readonly ManualResetEvent _inputConsumed = new(false);

        public ConsoleKeyInfo ReadKey(CancellationToken cancellationToken = new())
        {
            var value = _input.Take(cancellationToken);

            if (_input.Count == 0)
            {
                _inputConsumed.Set();
            }

            return value;
        }

        public async Task InputConsumed()
        {
            await Task.Yield();

            _inputConsumed.WaitOne();
        }

        public void SendKeys(params ConsoleKeyInfo[] keys)
        {
            _inputConsumed.Reset();

            foreach (var keyInfo in keys)
            {
                UpdateCursorPositionOnReceiving(keyInfo);

                _input.Add(keyInfo);
            }
        }

        public void SendKey(ConsoleKey key, bool shift = false, bool alt = false, bool control = false) =>
            SendKeys(new ConsoleKeyInfo(' ', key, shift, alt, control));

        private void UpdateCursorPositionOnReceiving(ConsoleKeyInfo keyInfo)
        {
            switch (keyInfo)
            {
                case { Key: ConsoleKey.Enter }:

                    CursorLeft = 0;

                    break;

                case { Key: ConsoleKey.Backspace }:
                case { Key: ConsoleKey.LeftArrow }:
                    if (CursorLeft > 0)
                    {
                        CursorLeft--;
                    }
                    break;

                case { Key: ConsoleKey.RightArrow }:
                    CursorLeft++;
                    break;

                default:
                    CursorLeft++;
                    break;
            }
        }

        public void SendString(string value)
        {
            foreach (var c in value)
            {
                ConsoleKey consoleKey;

                var parsed = Enum.TryParse(typeof(ConsoleKey), c.ToString(), true, out var consoleKeyObj);

                if (!parsed)
                {
                    consoleKey = ConsoleKey.NoName;
                }
                else
                {
                    consoleKey = (ConsoleKey) consoleKeyObj;
                }

                SendKeys(new ConsoleKeyInfo(c, consoleKey, false, false, false));
            }
        }

        public void SendEnter() => SendKeys(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));

        public int BufferHeight { get; set; }
        public int BufferWidth { get; set; }
        public int CursorLeft { get; set; }
        public int CursorTop { get; set; }

        public void SetCursorPosition(int left, int top)
        {
            CursorLeft = left;
            CursorTop = top;
        }
    }
}