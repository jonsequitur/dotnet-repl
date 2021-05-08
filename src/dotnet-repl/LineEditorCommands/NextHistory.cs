// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using RadLine;

namespace Microsoft.DotNet.Interactive.Repl.LineEditorCommands
{
    public class NextHistory : LineEditorCommand
    {
        private readonly LoopController _controller;

        public NextHistory(LoopController controller)
        {
            _controller = controller;
        }

        public override void Execute(LineEditorContext context)
        {
            if (_controller.History.Any() &&
                _controller.HistoryIndex < _controller.History.Count)
            {
                context.Execute(new Clear());

                _controller.HistoryIndex++;

                if (_controller.HistoryIndex > _controller.History.Count - 1 &&
                    _controller.StashedBufferContent is { } stashed)
                {
                    context.Buffer.Insert(stashed);
                }
                else
                {
                    context.Buffer.Insert(_controller.History[_controller.HistoryIndex].Code);
                }
            }
        }
    }
}