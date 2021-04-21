using System.IO;

namespace Microsoft.DotNet.Interactive.Repl
{
    public class StartupOptions
    {
        public StartupOptions(
            string defaultKernel,
            DirectoryInfo? logPath = null,
            bool verbose = false)
        {
            DefaultKernelName = defaultKernel;
            LogPath = logPath;
            Verbose = verbose;
        }

        public DirectoryInfo? LogPath { get; }

        public bool Verbose { get; }

        public string DefaultKernelName { get; }
    }
}