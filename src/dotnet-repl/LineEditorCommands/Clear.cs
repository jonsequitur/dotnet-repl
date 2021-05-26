using RadLine;

namespace dotnet_repl.LineEditorCommands
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