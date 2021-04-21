using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.PowerShell;
using Pocket;
using Spectre.Console;
using static Pocket.Logger;
using Formatter = Microsoft.DotNet.Interactive.Formatting.Formatter;

namespace Microsoft.DotNet.Interactive.Repl
{
    public static class CommandLineParser
    {
        public static Option<DirectoryInfo> LogPathOption { get; } = new(
            "--log-path",
            "Enable file logging to the specified directory");

        public static Option<string> DefaultKernel = new Option<string>(
            "--default-kernel",
            description: "The default language for the kernel",
            getDefaultValue: () => "csharp").AddSuggestions("csharp", "fsharp");

        public static Parser Create()
        {
            var rootCommand = new RootCommand("dotnet-repl")
            {
                LogPathOption,
                DefaultKernel
            };

            rootCommand.Handler = CommandHandler.Create<StartupOptions, IConsole, TextReader, CancellationToken>(StartRepl);

            return new CommandLineBuilder(rootCommand)
                   .UseDefaults()
                   .Build();
        }

        private static async Task StartRepl(
            StartupOptions options,
            IConsole console,
            TextReader standardIn,
            CancellationToken cancellationToken)
        {
            var terminal = new Terminal(console);

            RenderSplash(terminal, options);

            using var loop = new LoopController(
                new TerminalHandler(terminal, promptOffset: 4), 
                CreateKernel(options));

            cancellationToken.Register(() => loop.Dispose());

            Formatter.DefaultMimeType = PlainTextFormatter.MimeType;

            await loop.RunAsync();
        }

        private static void RenderSplash(this ITerminal terminal, StartupOptions startupOptions)
        {
            var language = startupOptions.DefaultKernelName switch
            {
                "csharp" => "C#",
                "fsharp" => "F#",
                _ => throw new ArgumentOutOfRangeException()
            };

            terminal.Render(
                new FigletText($".NET / {language}")
                    .Centered()
                    .Color(Color.Aqua));
            terminal.Render(new Markup("[aqua]Built with .NET Interactive + Spectre.Console[/]").Centered());
            terminal.Out.WriteLine();
            terminal.Out.WriteLine();
        }

        public static Kernel CreateKernel(StartupOptions options)
        {
            using var _ = Log.OnEnterAndExit("Creating Kernels");

            var compositeKernel = new CompositeKernel()
                .UseDebugDirective();

            compositeKernel.Add(
                new CSharpKernel()
                    .UseNugetDirective()
                    .UseKernelHelpers()
                    .UseWho()
                    .UseDotNetVariableSharing(),
                new[] { "c#", "C#" });

            compositeKernel.Add(
                new FSharpKernel()
                    .UseDefaultFormatting()
                    .UseNugetDirective()
                    .UseKernelHelpers()
                    .UseWho()
                    .UseDefaultNamespaces()
                    .UseDotNetVariableSharing(),
                new[] { "f#", "F#" });

            compositeKernel.Add(
                new PowerShellKernel()
                    .UseProfiles()
                    .UseDotNetVariableSharing(),
                new[] { "powershell" });

            compositeKernel.Add(
                new KeyValueStoreKernel()
                    .UseWho());

            var kernel = compositeKernel
                         .UseKernelClientConnection(new ConnectNamedPipe())
                         .UseKernelClientConnection(new ConnectSignalR());

            compositeKernel.Add(new SQLKernel());
            compositeKernel.UseQuitCommand();

            if (options.Verbose)
            {
                kernel.LogEventsToPocketLogger();
            }

            kernel.DefaultKernelName = options.DefaultKernelName;

            if (kernel.DefaultKernelName == "fsharp")
            {
                kernel.FindKernel("fsharp").DeferCommand(new SubmitCode("Formatter.Register(fun(x: obj)(writer: TextWriter)->fprintfn writer \"%120A\" x)"));
            }

            return kernel;
        }
    }
}