using System.IO;

namespace dotnet_repl;

public class StartupOptions
{
    public StartupOptions(
        string defaultKernel = "csharp",
        DirectoryInfo? workingDir = null,
        FileInfo? notebook = null,
        DirectoryInfo? logPath = null,
        bool exitAfterRun = false,
        OutputFormat outputFormat = OutputFormat.ipynb,
        FileInfo? outputPath = null)
    {
        DefaultKernelName = defaultKernel;
        WorkingDir = workingDir ?? new DirectoryInfo(Directory.GetCurrentDirectory());
        Notebook = notebook;
        LogPath = logPath;
        ExitAfterRun = exitAfterRun;
        OutputFormat = outputFormat;
        OutputPath = outputPath;
    }

    public DirectoryInfo? LogPath { get; }

    public string DefaultKernelName { get; }

    public DirectoryInfo WorkingDir { get; }

    public FileInfo? Notebook { get; }

    public bool ExitAfterRun { get; }

    public OutputFormat OutputFormat { get; }

    public FileInfo? OutputPath { get; }
}