using System.CommandLine;

namespace Microsoft.DotNet.Interactive.Repl
{
    public interface ITerminal : IConsole
    {
        int BufferHeight { get; }
        
        int BufferWidth { get; }
        
        int CursorLeft { get; }
        
        int CursorTop { get; }

        void SetCursorPosition(int left, int top);
    }
}