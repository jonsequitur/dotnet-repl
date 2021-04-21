using System;
using System.Collections.Generic;
using System.CommandLine.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Pocket;
using static Pocket.Logger<Microsoft.DotNet.Interactive.Repl.TerminalHandler>;

namespace Microsoft.DotNet.Interactive.Repl
{
    public class TerminalHandler
    {
        private readonly int _promptOffset;
        private readonly Dictionary<string, KeyHandler> _keyHandlers;
        private readonly StringBuilder _currentInput = new();
        private int _cursorPosition;
        private int _cursorLimit;
        private readonly Func<CancellationToken, ConsoleKeyInfo> _readKey;
        public ITerminal Terminal { get; }

        private readonly List<string> _history = new();
        private int _historyIndex = 0;

        public TerminalHandler(
            ITerminal terminal,
            Func<CancellationToken, ConsoleKeyInfo>? readKey = null,
            int promptOffset = 0)
        {
            _promptOffset = promptOffset;
            Terminal = terminal;

            _cursorPosition = Terminal.CursorLeft;

            _readKey = readKey ?? DefaultReadKey;

            _keyHandlers = new Dictionary<string, KeyHandler>
            {
                ["ctrl+backspace"] = DeleteToStartOfLine,
                ["ctrl+c"] = Quit,
                ["ctrl+delete"] = DeleteToEndOfLine,
                ["backspace"] = Backspace,
                ["delete"] = Delete,
                ["home"] = MoveCursorHome,
                ["end"] = MoveCursorEnd,
                ["enter"] = Enter,
                ["escape"] = ClearLine,
                ["left"] = MoveCursorLeft,
                ["right"] = MoveCursorRight,
                ["ctrl+right"] = MoveCursorEnd,
                ["ctrl+left"] = MoveCursorHome,
                ["up"] = MoveToPreviousHistoryItem,
                ["down"] = MoveToNextHistoryItem,
            };
        }

        public string CurrentInput => _currentInput.ToString().TrimEnd('\n', '\r');

        public async Task<string> GetInputAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();

            while (!cancellationToken.IsCancellationRequested &&
                   await ReadKeyAsync(cancellationToken) is { } keyInfo &&
                   keyInfo.Key != ConsoleKey.Enter)
            {
            }

            if (_history.Count == 0)
            {
                return "";
            }

            return _history[^1];
        }

        private void DeleteToEndOfLine()
        {
            var cursorStartPosition = _cursorPosition;
            MoveCursorEnd();
            while (_cursorPosition > cursorStartPosition)
            {
                Backspace();
            }
        }

        private void DeleteToStartOfLine()
        {
            while (!IsCursorAtStartOfLine &&
                   _currentInput[_cursorPosition - 1] != ' ')
            {
                Backspace();
            }
        }

        private void Quit()
        {
            ClearLine();
            Terminal.Out.WriteLine("Quitting...");
        }

        private static ConsoleKeyInfo DefaultReadKey(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (Console.KeyAvailable)
                {
                    return Console.ReadKey(true);
                }

                Thread.Sleep(50);
            }

            return new ConsoleKeyInfo('c', ConsoleKey.C, false, false, true);
        }

        private async Task<ConsoleKeyInfo> ReadKeyAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();

            var info = _readKey(cancellationToken);

            var keyCode = info.ToKeybindingString();

            if (_keyHandlers.TryGetValue(keyCode, out var handler))
            {
                Log.Info("Received {key} ({keyChar}) for special handling", keyCode, info.KeyChar);

                handler();
            }
            else
            {
                Log.Info("Received {key} for write", keyCode);

                WriteChar(info.KeyChar);
            }

            return info;
        }

        private bool IsCursorAtStartOfLine => _cursorPosition == 0;

        private bool IsCursorAtEndOfLine => _cursorPosition == _cursorLimit;

        private bool IsCursorAtStartOfBuffer => Terminal.CursorLeft == 0;

        private void Enter()
        {
            var currentInput = CurrentInput;
            _currentInput.Clear();

            AddToHistory(currentInput);

            Terminal.Out.WriteLine();

            _currentInput.Clear();

            _cursorPosition = 0;
            AssertCursorPosition();
        }

        private void AddToHistory(string currentInput)
        {
            _history.Add(currentInput);
            _historyIndex++;
        }

        private bool IsCursorAtEndOfBuffer() => EffectiveCursorLeft == Terminal.BufferWidth - 1;

        private int EffectiveCursorLeft => Terminal.CursorLeft - _promptOffset;

        private void DecrementCursorPosition()
        {
            _cursorPosition--;
            AssertCursorPosition();
        }

        private void IncrementCursorPosition()
        {
            _cursorPosition++;
            AssertCursorPosition();
        }

        private void MoveCursorLeft()
        {
            if (IsCursorAtStartOfLine)
            {
                return;
            }

            if (IsCursorAtStartOfBuffer)
            {
                Terminal.SetCursorPosition(Terminal.BufferWidth - 1, Terminal.CursorTop - 1);
            }
            else
            {
                Terminal.SetCursorPosition(Terminal.CursorLeft - 1, Terminal.CursorTop);
            }

            DecrementCursorPosition();
        }

        private void MoveCursorRight()
        {
            if (IsCursorAtEndOfLine)
            {
                return;
            }

            using var _ = Cursor.Hide();

            if (IsCursorAtEndOfBuffer())
            {
                Terminal.SetCursorPosition(0, Terminal.CursorTop + 1);
            }
            else
            {
                Terminal.SetCursorPosition(Terminal.CursorLeft + 1, Terminal.CursorTop);
            }

            IncrementCursorPosition();
        }

        private void AssertCursorPosition()
        {
            var effectiveCursorLeft = EffectiveCursorLeft;

            if (_cursorPosition != effectiveCursorLeft)
            {
                Log.Error(
                    $"{nameof(_cursorPosition)} {{{nameof(_cursorPosition)}}} != {nameof(EffectiveCursorLeft)} {{{nameof(EffectiveCursorLeft)}}}",
                    args: new object[]
                    {
                        _cursorPosition,
                        effectiveCursorLeft
                    });
            }
        }

        private void MoveToPreviousHistoryItem()
        {
            if (_historyIndex > 0)
            {
                _historyIndex--;
                SetBufferText(_history[_historyIndex]);
                MoveCursorEnd();
            }
        }

        private void MoveToNextHistoryItem()
        {
            if (_historyIndex < _history.Count)
            {
                if (++_historyIndex == _history.Count)
                {
                    ClearLine();
                }
                else
                {
                    SetBufferText(_history[_historyIndex]);
                    MoveCursorEnd();
                }
            }
        }

        private void SetBufferText(string str)
        {
            ClearLine();

            foreach (var character in str)
            {
                WriteChar(character);
            }
        }

        private void MoveCursorHome()
        {
            while (!IsCursorAtStartOfLine)
            {
                MoveCursorLeft();
            }

            ReconcileCursorPosition();
        }

        private void MoveCursorEnd()
        {
            while (!IsCursorAtEndOfLine)
            {
                MoveCursorRight();
            }

            ReconcileCursorPosition();
        }

        private void ClearLine()
        {
            if (CurrentInput.Length == 0)
            {
                return;
            }

            MoveCursorEnd();
            while (!IsCursorAtStartOfLine)
            {
                Backspace();
            }
        }

        private void WriteChar(char c)
        {
            if (IsCursorAtEndOfLine)
            {
                _currentInput.Append(c);
                Terminal.Out.Write(c.ToString());
                IncrementCursorPosition();
            }
            else
            {
                var left = Terminal.CursorLeft;
                var top = Terminal.CursorTop;
                var str = _currentInput.ToString().Substring(_cursorPosition);
                _currentInput.Insert(_cursorPosition, c);
                Terminal.Out.Write(c + str);
                Terminal.SetCursorPosition(left, top);
                MoveCursorRight();
            }

            _cursorLimit++;
        }

        private void Backspace()
        {
            if (IsCursorAtStartOfLine)
            {
                return;
            }

            using var _ = Cursor.Hide();

            MoveCursorLeft();

            ReconcileCursorPosition();

            var index = _cursorPosition;

            _currentInput.Remove(index, 1);
            var replacement = _currentInput.ToString().Substring(index);
            var left = Terminal.CursorLeft;
            var top = Terminal.CursorTop;
            Terminal.Out.Write($"{replacement} ");
            Terminal.SetCursorPosition(left, top);
            _cursorLimit--;
        }

        private void ReconcileCursorPosition()
        {
            if (_cursorPosition > _currentInput.Length)
            {
                _cursorPosition = _currentInput.Length;
            }
        }

        private void Delete()
        {
            if (IsCursorAtEndOfLine)
            {
                return;
            }

            using var _ = Cursor.Hide();

            var index = _cursorPosition;
            _currentInput.Remove(index, 1);
            var replacement = _currentInput.ToString().Substring(index);
            var left = Terminal.CursorLeft;
            var top = Terminal.CursorTop;
            Terminal.Out.Write($"{replacement} ");
            Terminal.SetCursorPosition(left, top);
            _cursorLimit--;
        }
    }

    public delegate void KeyHandler();

    internal class Cursor : IDisposable
    {
        private Cursor()
        {
        }

        public static IDisposable Hide() => Disposable.Create(() => Console.CursorVisible = true);

        public void Dispose()
        {
            Console.CursorVisible = true;
        }
    }
}