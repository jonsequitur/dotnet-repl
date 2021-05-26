// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using RadLine;

namespace dotnet_repl.Tests.Utility
{
    public class TestInputSource : IInputSource
    {
        private readonly BlockingCollection<ConsoleKeyInfo> _input = new();

        private readonly ManualResetEvent _inputConsumed = new(false);

        public async Task<ConsoleKeyInfo?> ReadKey(CancellationToken cancellationToken = new())
        {
            await Task.Yield();

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
                _input.Add(keyInfo);
            }
        }

        public void SendKey(ConsoleKey key, bool shift = false, bool alt = false, bool control = false) =>
            SendKeys(new ConsoleKeyInfo(' ', key, shift, alt, control));

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
    }
}