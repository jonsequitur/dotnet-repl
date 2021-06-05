using System.Linq;
using RadLine;

namespace dotnet_repl.LineEditorCommands
{
    public class NextHistory : LineEditorCommand
    {
        private readonly Repl _controller;

        public NextHistory(Repl controller)
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