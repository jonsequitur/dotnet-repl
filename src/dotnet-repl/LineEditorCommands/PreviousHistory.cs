// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using RadLine;

namespace Microsoft.DotNet.Interactive.Repl.LineEditorCommands
{
    public class PreviousHistory : LineEditorCommand
    {
        private readonly LoopController _controller;

        public PreviousHistory(LoopController controller)
        {
            _controller = controller;
        }

        public override void Execute(LineEditorContext context)
        {
            if (_controller.History.Any() &&
                _controller.HistoryIndex > 0)
            {
                if (_controller.HistoryIndex == _controller.History.Count)
                {
                    _controller.StashedBufferContent = context.Buffer.Content;
                }

                context.Execute(new Clear());

                _controller.HistoryIndex--;

                context.Buffer.Insert(_controller.History[_controller.HistoryIndex].Code);
            }
        }
    }
}