using System;
using dotnet_repl.LineEditorCommands;
using RadLine;

namespace dotnet_repl
{
    internal static class KeyBindings
    {
        public static void AddKeyBindings(this Repl repl)
        {
            var editor = repl.LineEditor;

            // Remove old keybinding for autocomplete
            editor.KeyBindings.Remove(ConsoleKey.Tab);
            editor.KeyBindings.Remove(ConsoleKey.Tab, ConsoleModifiers.Control);

            editor.KeyBindings.Add(
                ConsoleKey.Tab,
                () => new CompletionCommand(AutoComplete.Next));

            editor.KeyBindings.Add(
                ConsoleKey.Tab,
                ConsoleModifiers.Shift,
                () => new CompletionCommand(AutoComplete.Previous));

            editor.KeyBindings.Add(
                ConsoleKey.C,
                ConsoleModifiers.Control,
                () => new Quit(repl.QuitAction));

            editor.KeyBindings.Add<Clear>(
                ConsoleKey.C,
                ConsoleModifiers.Control | ConsoleModifiers.Alt);
        }
    }
}