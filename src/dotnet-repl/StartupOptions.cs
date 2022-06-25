using System.IO;

namespace dotnet_repl;

public class StartupOptions
{
    public StartupOptions(
        string defaultKernel  = "csharp",
        DirectoryInfo? workingDir = null,
        FileInfo? notebook = null,
        DirectoryInfo? logPath = null,
        bool exitAfterRun = false)
    {
        DefaultKernelName = defaultKernel;
        WorkingDir = workingDir ?? new DirectoryInfo(Directory.GetCurrentDirectory());
        Notebook = notebook;
        LogPath = logPath;
        ExitAfterRun = exitAfterRun;
    }

    public DirectoryInfo? LogPath { get; }

    public string DefaultKernelName { get; }

    public DirectoryInfo WorkingDir { get; }

    public FileInfo? Notebook { get; }

    public bool ExitAfterRun { get; }
}