// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using RadLine;

namespace Microsoft.DotNet.Interactive.Repl.LineEditorCommands
{
    public class Clear : LineEditorCommand
    {
        public override void Execute(LineEditorContext context)
        {
            context.Buffer.Clear(0, context.Buffer.Content.Length);
            context.Buffer.Move(0);
        }
    }
}