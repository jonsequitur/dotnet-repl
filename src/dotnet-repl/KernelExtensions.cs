using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Formatting;
using Recipes;
using Spectre.Console;

namespace dotnet_repl
{
    internal static class KernelExtensions
    {
        public static T UseAboutMagicCommand<T>(this T kernel)
            where T : Kernel
        {
            var about = new Command("#!about", "Show version and build information")
            {
                Handler = CommandHandler.Create<KernelInvocationContext>(
                    context => context.Display(VersionSensor.Version()))
            };

            kernel.AddDirective(about);

            Formatter.Register<VersionSensor.BuildInfo>((info, context) =>
            {
                var table = new Grid();
                table.AddColumn(new GridColumn());
                table.AddColumn(new GridColumn());
                table.AddRow("Version", info.AssemblyInformationalVersion);
                table.AddRow("Built", info.BuildDate);
                table.AddRow("Home", "https://github.com/jonsequitur/dotnet-repl");

                table.FormatTo(context, PlainTextFormatter.MimeType);

                return true;
            }, PlainTextFormatter.MimeType);

            return kernel;
        }

        public static TKernel UseDebugDirective<TKernel>(this TKernel kernel)
            where TKernel : Kernel
        {
            kernel.AddDirective(new Command("#!debug")
            {
                Handler = CommandHandler.Create<KernelInvocationContext, IConsole, CancellationToken>(async (context, console, cancellationToken) =>
                {
                    await Attach();

                    async Task Attach()
                    {
                        var process = Process.GetCurrentProcess();

                        var processId = process.Id;

                        console.Out.WriteLine($"Attach your debugger to process {processId} ({process.ProcessName}).");

                        while (!Debugger.IsAttached)
                        {
                            await Task.Delay(500, cancellationToken);
                        }
                    }
                })
            });

            return kernel;
        }
    }
}