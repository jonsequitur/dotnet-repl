// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using RadLine;
using Spectre.Console;

namespace Microsoft.DotNet.Interactive.Repl
{
    public class Quit : LineEditorCommand
    {
        private readonly Action _triggerQuit;

        public Quit(Action triggerQuit)
        {
            _triggerQuit = triggerQuit;
        }

        public override void Execute(LineEditorContext context)
        {
            // TODO: (Execute) this doesn't seem to reach the terminal
            AnsiConsole.Render(new Markup("Quitting..."));

            _triggerQuit();
        }
    }

    public class Clear : LineEditorCommand
    {
        public override void Execute(LineEditorContext context)
        {
            context.Buffer.Clear(0, context.Buffer.Content.Length);
            context.Buffer.Move(0);
        }
    }

    public class ShowPreviousHistorySubmission : LineEditorCommand
    {
        public override void Execute(LineEditorContext context)
        {
        }
    }

    public class ShowNextHistorySubmission : LineEditorCommand
    {
        public override void Execute(LineEditorContext context)
        {
        }
    }

    internal static class LineEditorContextExtensions
    {
    }
}