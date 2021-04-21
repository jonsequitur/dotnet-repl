using System;
using System.CommandLine.Parsing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Pocket;
using Serilog.Sinks.RollingFileAlternate;
using SerilogLoggerConfiguration = Serilog.LoggerConfiguration;
using static Pocket.Logger<Microsoft.DotNet.Interactive.Repl.Program>;

namespace Microsoft.DotNet.Interactive.Repl
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            var parser = CommandLineParser.Create();

            var result = parser.Parse(args);

            if (result.ValueForOption(CommandLineParser.LogPathOption) is { } path)
            {
                StartToolLogging(path);
            }

            return await parser.InvokeAsync(args);
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
}