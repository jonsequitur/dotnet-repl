using System.Collections.Generic;
using System.CommandLine;
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

    public FileInfo? FileToRun { get; }

    public bool ExitAfterRun { get; init; }

    public OutputFormat OutputFormat { get; }

    public FileInfo? OutputPath { get; }

    public IDictionary<string, string>? Inputs { get; init; }

    public bool IsAutomationMode => ExitAfterRun || OutputPath is not null;

    public static StartupOptions FromParseResult(ParseResult parseResult) =>
        new(
            parseResult.GetValue(CommandLineParser.DefaultKernelOption)!,
            parseResult.GetValue(CommandLineParser.WorkingDirOption),
            parseResult.GetValue(CommandLineParser.RunOption),
            parseResult.GetValue(CommandLineParser.LogPathOption),
            parseResult.GetValue(CommandLineParser.ExitAfterRunOption),
            parseResult.GetValue(CommandLineParser.OutputFormatOption),
            parseResult.GetValue(CommandLineParser.OutputPathOption),
            parseResult.GetValue(CommandLineParser.InputsOption));
}