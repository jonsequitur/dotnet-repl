using System;

namespace dotnet_repl
{
    public static class ConsoleKeyExtensions
    {
        public static string ToKeybindingString(this ConsoleKeyInfo keyInfo)
        {
            var value = "";

            if (keyInfo.Modifiers != 0)
            {
                if (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control))
                {
                    value += "ctrl+";
                }

                if (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift))
                {
                    value += "shift+";
                }

                if (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Alt))
                {
                    value += "alt+";
                }
            }

            return value + keyInfo.Key.ToString().ToLowerInvariant().Replace("arrow", "");
        }
    }
}