using System.Collections.Generic;
using System.IO;

namespace dotnet_repl;

public class StartupOptions
{
    public StartupOptions(
        string defaultKernel = "csharp",
        DirectoryInfo? workingDir = null,
        FileInfo? fileToRun = null,
        DirectoryInfo? logPath = null,
        bool exitAfterRun = false,
        OutputFormat outputFormat = OutputFormat.ipynb,
        FileInfo? outputPath = null,
        IDictionary<string, string>? inputs = null)
    {
        DefaultKernelName = defaultKernel;
        WorkingDir = workingDir ?? new DirectoryInfo(Directory.GetCurrentDirectory());
        FileToRun = fileToRun;
        LogPath = logPath;
        ExitAfterRun = exitAfterRun;
        OutputFormat = outputFormat;
        OutputPath = outputPath;
        Inputs = inputs;
    }

    public DirectoryInfo? LogPath { get; }

    public string DefaultKernelName { get; }

    public DirectoryInfo WorkingDir { get; }

    public FileInfo? FileToRun { get; set; }

    public bool ExitAfterRun { get; set; }

    public OutputFormat OutputFormat { get; }

    public FileInfo? OutputPath { get; }

    public IDictionary<string, string>? Inputs { get; set; }

    public bool IsAutomationMode => ExitAfterRun || OutputPath is { };
}