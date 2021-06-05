using System.Linq;
using RadLine;

namespace dotnet_repl.LineEditorCommands
{
    public class PreviousHistory : LineEditorCommand
    {
        private readonly Repl _controller;

        public PreviousHistory(Repl controller)
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