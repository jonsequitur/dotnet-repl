using System;
using dotnet_repl.LineEditorCommands;
using RadLine;

namespace dotnet_repl
{
    internal static class KeyBindings
    {
        public static void AddKeyBindings(this LoopController controller)
        {
            var editor = controller.LineEditor;

            editor.KeyBindings.Add(
                ConsoleKey.Tab,
                () => new CompletionCommand(AutoComplete.Next));

            editor.KeyBindings.Add(
                ConsoleKey.Tab,
                ConsoleModifiers.Control,
                () => new CompletionCommand(AutoComplete.Previous));

            editor.KeyBindings.Add(
                ConsoleKey.C,
                ConsoleModifiers.Control,
                () => new Quit(controller.QuitAction));

            editor.KeyBindings.Add<Clear>(
                ConsoleKey.C,
                ConsoleModifiers.Control | ConsoleModifiers.Alt);

            editor.KeyBindings.Add(
                ConsoleKey.UpArrow,
                ConsoleModifiers.Control,
                () => new PreviousHistory(controller));

            editor.KeyBindings.Add(
                ConsoleKey.DownArrow,
                ConsoleModifiers.Control,
                () => new NextHistory(controller));
        }
    }
}