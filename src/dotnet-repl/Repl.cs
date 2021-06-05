using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Notebook;
using Microsoft.DotNet.Interactive.PowerShell;
using Pocket;
using RadLine;
using Spectre.Console;
using static dotnet_repl.AnsiConsoleExtensions;

namespace dotnet_repl
{
    public class Repl : IDisposable
    {
        private readonly Kernel _kernel;
        private readonly CompositeDisposable _disposables = new();
        private readonly ManualResetEvent _commandCompleted = new(false);

        private readonly CancellationTokenSource _disposalTokenSource = new();

        private readonly List<SubmitCode> _history = new();

        private TaskCompletionSource _waitingForInput;

        public Repl(
            Kernel kernel,
            Action quit,
            IAnsiConsole ansiConsole,
            IInputSource? inputSource = null)
        {
            _kernel = kernel;
            QuitAction = quit;
            AnsiConsole = ansiConsole;

            _waitingForInput = new TaskCompletionSource();

            _disposables.Add(() =>
            {
                _disposalTokenSource.Cancel();
                _waitingForInput?.TrySetResult();
            });

            _kernel.AddMiddleware(async (command, context, next) =>
            {
                await next(command, context);

                KernelCommand root = command;

                while (root.Parent is { } parent)
                {
                    root = parent;
                }

                if (root is SubmitCode current)
                {
                    TryAddToHistory(current);
                }
            });

            var provider = new LineEditorServiceProvider(new KernelCompletion(_kernel));
            LineEditor = new LineEditor(ansiConsole, inputSource, provider)
            {
                MultiLine = true,
                Prompt = Theme.Prompt,
                Highlighter = ReplWordHighlighter.Create()
            };

            this.AddKeyBindings();
        }

        public IAnsiConsole AnsiConsole { get; }

        public IReadOnlyList<SubmitCode> History => _history;

        public int HistoryIndex { get; internal set; } = -1;

        public LineEditor LineEditor { get; }

        internal Action QuitAction { get; }

        internal string? StashedBufferContent { get; set; }

        public Theme Theme { get; set; } = Theme.Default;

        public void Start() => Task.Run(() => RunAsync());

        public bool TryAddToHistory(SubmitCode submitCode)
        {
            if (string.IsNullOrEmpty(submitCode.Code))
            {
                return false;
            }

            var added = false;

            if (History.LastOrDefault() is not { } previous || !previous.Code.Equals(submitCode.Code))
            {
                _history.Add(submitCode);
                added = true;
            }

            HistoryIndex = History.Count;

            return added;
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
                _commandCompleted.Reset();

                if (!queuedSubmissions.TryDequeue(out var input))
                {
                    if (!exitAfterRun)
                    {
                        input = await LineEditor.ReadLine(_disposalTokenSource.Token);
                    }
                }

                if (_disposalTokenSource.IsCancellationRequested)
                {
                    return;
                }

                var command = new SubmitCode(input);

                await RenderKernelEvents(command);

                if (exitAfterRun && queuedSubmissions.Count == 0)
                {
                    break;
                }

                ResetWaitingForInput();
            }
        }

        public Task WaitingForInputAsync() => _waitingForInput!.Task;

        private void ResetWaitingForInput()
        {
            var previous = _waitingForInput;

            _waitingForInput = new TaskCompletionSource();

            previous?.TrySetResult();
        }

        private async Task RenderKernelEvents(KernelCommand command)
        {
            StringBuilder? stdOut = default;
            StringBuilder? stdErr = default;

            Task<KernelCommandResult>? result = default;

            var events = _kernel.KernelEvents.Replay();

            using var _ = events.Connect();

            await AnsiConsole.Status().StartAsync(Theme.StatusMessageGenerator.GetStatusMessage(), async ctx =>
            {
                ctx.Spinner(new ClockSpinner());

                var t = events.FirstOrDefaultAsync(e => e is DisplayEvent or CommandFailed or CommandSucceeded);
                
                result = _kernel.SendAsync(command);

                await t;
            });

            var waiting = new Panel("").NoBorder();

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
                                ctx.UpdateTarget(GetErrorDisplay(errorProduced));

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
                                ctx.UpdateTarget(GetSuccessDisplay(displayedValueProduced));
                                ctx.Refresh();
                                break;

                            case DisplayedValueUpdated displayedValueUpdated:
                                ctx.UpdateTarget(GetSuccessDisplay(displayedValueUpdated));

                                break;

                            case ReturnValueProduced returnValueProduced:
                                ctx.UpdateTarget(GetSuccessDisplay(returnValueProduced));
                                break;

                            // command completion events

                            case CommandFailed failed when failed.Command == command:
                                AnsiConsole.RenderBufferedStandardOutAndErr(stdOut, stdErr);

                                ctx.UpdateTarget(GetErrorDisplay(failed.Message));

                                _commandCompleted.Set();

                                break;

                            case CommandSucceeded succeeded when succeeded.Command == command:
                                AnsiConsole.RenderBufferedStandardOutAndErr(stdOut, stdErr);
                                _commandCompleted.Set();
                                break;
                        }
                    });

                    await result!;
                });
        }

        public void Dispose() => _disposables.Dispose();

        public static CompositeKernel CreateKernel(StartupOptions options)
        {
            using var _ = Logger.Log.OnEnterAndExit("Creating Kernels");

            var compositeKernel = new CompositeKernel()
                .UseDebugDirective()
                .UseAboutMagicCommand();

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
                .UseKernelClientConnection(new ConnectNamedPipe());

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