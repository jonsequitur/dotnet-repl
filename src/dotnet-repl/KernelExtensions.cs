using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Process = System.Diagnostics.Process;

namespace Microsoft.DotNet.Interactive.Repl
{
    public static class KernelExtensions
    {
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