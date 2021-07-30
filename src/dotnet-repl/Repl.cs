using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Notebook;
using Microsoft.DotNet.Interactive.PowerShell;
using Pocket;
using RadLine;
using Spectre.Console;
using static dotnet_repl.AnsiConsoleExtensions;
using Formatter = Microsoft.DotNet.Interactive.Formatting.Formatter;

namespace dotnet_repl
{
    public class Repl : IDisposable
    {
        private static readonly HashSet<string> _nonStickyKernelNames = new HashSet<string>
        {
            "value"
        };

        private readonly CompositeKernel _kernel;

        private readonly CompositeDisposable _disposables = new();

        private readonly CancellationTokenSource _disposalTokenSource = new();

        private readonly Subject<Unit> _readyForInput = new();

        public Repl(
            CompositeKernel kernel,
            Action quit,
            IAnsiConsole ansiConsole,
            IInputSource? inputSource = null)
        {
            _kernel = kernel;
            QuitAction = quit;
            AnsiConsole = ansiConsole;
            InputSource = inputSource;
            Theme = KernelSpecificTheme.GetTheme(kernel.DefaultKernelName) ?? new CSharpTheme();

            _disposables.Add(() => { _disposalTokenSource.Cancel(); });

            var provider = new LineEditorServiceProvider(new KernelCompletion(_kernel));
            LineEditor = new LineEditor(ansiConsole, inputSource, provider)
            {
                MultiLine = true,
                Prompt = Theme.Prompt,
                Highlighter = ReplWordHighlighter.Create(_kernel.DefaultKernelName)
            };

            _kernel.AddMiddleware(async (command, context, next) =>
            {
                await next(command, context);

                KernelCommand root = command;

                while (root.Parent is { } parent)
                {
                    root = parent;
                }
            });

            SetTheme();

            this.AddKeyBindings();
        }

        public IObservable<Unit> ReadyForInput => _readyForInput;

        public IAnsiConsole AnsiConsole { get; }

        public IInputSource? InputSource { get; }

        public LineEditor LineEditor { get; }

        internal Action QuitAction { get; }

        public KernelSpecificTheme Theme { get; set; }

        public void Start()
        {
            var ready = ReadyForInput.FirstAsync();
            Task.Run(() => RunAsync());
            ready.FirstAsync().Wait();
        }

        public async Task RunAsync(
            NotebookDocument? notebook = null,
            bool exitAfterRun = false)
        {
            var queuedSubmissions = new Queue<string>(notebook?.Cells.Select(c => $"#!{c.Language}\n{c.Contents}") ?? Array.Empty<string>());

            if (!queuedSubmissions.Any())
            {
                exitAfterRun = false;
            }

            while (!_disposalTokenSource.IsCancellationRequested)
            {
                await Task.Yield();

                if (!queuedSubmissions.TryDequeue(out var input))
                {
                    if (!exitAfterRun)
                    {
                        SetTheme();
                        _readyForInput.OnNext(Unit.Default);
                        input = await LineEditor.ReadLine(_disposalTokenSource.Token);
                    }
                }
                else
                {
                    LineEditor.History.Add(input);
                }

                if (_disposalTokenSource.IsCancellationRequested)
                {
                    return;
                }

                await RunKernelCommand(new SubmitCode(input));

                if (exitAfterRun && queuedSubmissions.Count == 0)
                {
                    break;
                }
            }
        }

        private void SetTheme()
        {
            if (KernelSpecificTheme.GetTheme(_kernel.DefaultKernelName) is { } theme)
            {
                Theme = theme;

                if (LineEditor.Prompt is DelegatingPrompt d)
                {
                    d.InnerPrompt = theme.Prompt;
                }
            }
        }

        private async Task RunKernelCommand(KernelCommand command)
        {
            StringBuilder? stdOut = default;
            StringBuilder? stdErr = default;

            Task<KernelCommandResult>? result = default;

            var events = _kernel.KernelEvents.Replay();

            using var _ = events.Connect();

            await AnsiConsole.Status().StartAsync(Theme.StatusMessageGenerator.GetStatusMessage(), async ctx =>
            {
                ctx.Spinner(new ClockSpinner());

                var t = events.FirstOrDefaultAsync(
                    e => e is DisplayEvent or CommandFailed or CommandSucceeded);

                result = _kernel.SendAsync(command, _disposalTokenSource.Token);

                await t;
            });

            var waiting = new Panel("").NoBorder();

            var tcs = new TaskCompletionSource();

            await AnsiConsole
                .Live(waiting)
                .StartAsync(async ctx =>
                {
                    using var _ = events.Subscribe(@event =>
                    {
                        switch (@event)
                        {
                            // events that tell us whether the submission was valid
                            case IncompleteCodeSubmissionReceived incomplete when incomplete.Command == command:
                                break;

                            case CompleteCodeSubmissionReceived complete when complete.Command == command:
                                break;

                            case CodeSubmissionReceived codeSubmissionReceived:
                                break;

                            // output / display events

                            case ErrorProduced errorProduced:
                                ctx.UpdateTarget(GetErrorDisplay(errorProduced, Theme));

                                break;

                            case StandardOutputValueProduced standardOutputValueProduced:

                                stdOut ??= new StringBuilder();
                                stdOut.Append(standardOutputValueProduced.PlainTextValue());

                                break;

                            case StandardErrorValueProduced standardErrorValueProduced:

                                stdErr ??= new StringBuilder();
                                stdErr.Append(standardErrorValueProduced.PlainTextValue());

                                break;

                            case DisplayedValueProduced displayedValueProduced:
                                ctx.UpdateTarget(GetSuccessDisplay(displayedValueProduced, Theme));
                                ctx.Refresh();
                                break;

                            case DisplayedValueUpdated displayedValueUpdated:
                                ctx.UpdateTarget(GetSuccessDisplay(displayedValueUpdated, Theme));
                                break;

                            case ReturnValueProduced returnValueProduced:

                                if (returnValueProduced.Value is DisplayedValue)
                                {
                                    break;
                                }

                                ctx.UpdateTarget(GetSuccessDisplay(returnValueProduced, Theme));
                                break;

                            // command completion events

                            case CommandFailed failed when failed.Command == command:
                                AnsiConsole.RenderBufferedStandardOutAndErr(Theme, stdOut, stdErr);
                                ctx.UpdateTarget(GetErrorDisplay(failed.Message, Theme));
                                tcs.SetResult();

                                break;

                            case CommandSucceeded succeeded when succeeded.Command == command:
                                AnsiConsole.RenderBufferedStandardOutAndErr(Theme, stdOut, stdErr);
                                tcs.SetResult();

                                break;
                        }
                    });

                    await tcs.Task;
                });

            await result!;
        }

        public void Dispose() => _disposables.Dispose();

        public static CompositeKernel CreateKernel(StartupOptions options)
        {
            using var _ = Logger.Log.OnEnterAndExit("Creating Kernels");

            ResetFormattersToDefault();

            var compositeKernel = new CompositeKernel()
                .UseAboutMagicCommand()
                .UseDebugDirective()
                .UseHelpMagicCommand()
                .UseQuitCommand()
                .UseKernelClientConnection(new ConnectNamedPipe());

            compositeKernel.AddMiddleware(async (command, context, next) =>
            {
                var rootKernel = (CompositeKernel) context.HandlingKernel.RootKernel;

                await next(command, context);

                if (command.GetType().Name == "DirectiveCommand")
                {
                    var name = command.ToString()?.Replace("Directive: #!", "");

                    if (name is { } &&
                        !_nonStickyKernelNames.Contains(name) &&
                        rootKernel.FindKernel(name) is { } kernel)
                    {
                        rootKernel.DefaultKernelName = kernel.Name;
                    }
                }
            });
            
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

            compositeKernel.Add(new SQLKernel());

            if (options.Verbose)
            {
                compositeKernel.LogEventsToPocketLogger();
            }

            compositeKernel.DefaultKernelName = options.DefaultKernelName;

            if (compositeKernel.DefaultKernelName == "fsharp")
            {
                var fsharpKernel = compositeKernel.FindKernel("fsharp");

                fsharpKernel.DeferCommand(new SubmitCode("Formatter.Register(fun(x: obj)(writer: TextWriter)->fprintfn writer \"%120A\" x)"));
                fsharpKernel.DeferCommand(new SubmitCode("Formatter.Register(fun(x: System.Collections.IEnumerable)(writer: TextWriter)->fprintfn writer \"%120A\" x)"));
            }

            return compositeKernel;
        }

        public static void ResetFormattersToDefault()
        {
            Formatter.DefaultMimeType = PlainTextFormatter.MimeType;
            new DefaultSpectreFormatterSet().Register();
        }
    }
}