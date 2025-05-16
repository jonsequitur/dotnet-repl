using System;
using System.CommandLine.Parsing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
using Pocket;
using Serilog.Sinks.RollingFileAlternate;
using SerilogLoggerConfiguration = Serilog.LoggerConfiguration;
using static Pocket.Logger<dotnet_repl.Program>;

namespace dotnet_repl;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        var rootCommand = CommandLineParser.Create();

        var result = rootCommand.Parse(args);

        if (result.GetValue(CommandLineParser.LogPathOption) is { } path)
        {
            StartToolLogging(path);
        }

        return await rootCommand.InvokeAsync(args);
    }

    private static readonly Assembly[] _assembliesEmittingPocketLoggerLogs =
    {
        typeof(Program).Assembly,
        typeof(Kernel).Assembly, // Microsoft.DotNet.Interactive.dll
    };

    internal static IDisposable StartToolLogging(DirectoryInfo path)
    {
        var disposables = new CompositeDisposable();

        var log = new SerilogLoggerConfiguration()
                  .WriteTo
                  .RollingFileAlternate(path.FullName, outputTemplate: "{Message}{NewLine}")
                  .CreateLogger();

        var subscription = LogEvents.Subscribe(
            e => log.Information(e.ToLogString()),
            _assembliesEmittingPocketLoggerLogs);

        disposables.Add(subscription);
        disposables.Add(log);

        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            Log.Warning($"{nameof(TaskScheduler.UnobservedTaskException)}", args.Exception);
            args.SetObserved();
        };

        return disposables;
    }
}

