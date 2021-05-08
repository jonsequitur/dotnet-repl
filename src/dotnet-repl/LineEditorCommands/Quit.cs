// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using RadLine;
using Spectre.Console;

namespace Microsoft.DotNet.Interactive.Repl.LineEditorCommands
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
}