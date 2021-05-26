using System.Runtime.CompilerServices;

namespace dotnet_repl.Tests.Utility
{
    internal static class PathUtility
    {
        public static string PathToCurrentSourceFile([CallerFilePath] string path = null)
        {
            return path;
        }
    }
}