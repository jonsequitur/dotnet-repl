using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Notebook;
using Pocket;
using RadLine;
using Spectre.Console;

namespace dotnet_repl
{
    public class LoopController : IDisposable
    {
        private readonly Kernel _kernel;
        private readonly CompositeDisposable _disposables = new();
        private readonly ManualResetEvent _commandCompleted = new(false);

        private readonly CancellationTokenSource _disposalTokenSource = new();

        private readonly SillyExecutionStatusMessageGenerator _executionStatusMessageGenerator = new();

        private readonly List<SubmitCode> _history = new();

        private readonly LineEditorPrompt _prompt = new(
            "[bold aqua slowblink]  >[/]",
            "[bold aqua slowblink]...[/]");

        public LoopController(
            Kernel kernel,
            Action quit,
            IAnsiConsole ansiConsole,
            IInputSource? inputSource = null)
        {
            _kernel = kernel;
            QuitAction = quit;
            AnsiConsole = ansiConsole;

            _disposables.Add(() => _disposalTokenSource.Cancel());

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

            LineEditor = new LineEditor(ansiConsole, inputSource)
            {
                MultiLine = true,
                Prompt = _prompt,
                Completion = new KernelCompletion(kernel),
                Highlighter = ReplWordHighlighter.Create()
            };

            this.AddKeyBindings();
        }

        public IReadOnlyList<SubmitCode> History => _history;

        public int HistoryIndex { get; internal set; } = -1;

        public LineEditor LineEditor { get; }

        internal Action QuitAction { get; }
        public IAnsiConsole AnsiConsole { get; }

        internal string? StashedBufferContent { get; set; }

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

                KernelCommandResult? result = default;

                await AnsiConsole.Status().StartAsync(_executionStatusMessageGenerator.GetStatusMessage(), async ctx =>
                {
                    ctx.Spinner(new ClockSpinner());
                    ctx.SpinnerStyle(Style.Parse("green"));
                    
                    result = await _kernel.SendAsync(command);

                    if (result is { })
                    {
                        HandleKernelEvents(command, result, ctx);
                    }
                });

                if (exitAfterRun && queuedSubmissions.Count == 0)
                {
                    break;
                }
            }
        }

        private void HandleKernelEvents(
            KernelCommand command,
            KernelCommandResult result,
            StatusContext context)
        {
            var events = result.KernelEvents;

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
                        AnsiConsole.RenderErrorEvent(errorProduced);

                        break;

                    case StandardOutputValueProduced standardOutputValueProduced:
                        AnsiConsole.RenderSuccessfulEvent(standardOutputValueProduced);
                        break;

                    case DisplayedValueProduced displayedValueProduced:
                        AnsiConsole.RenderSuccessfulEvent(displayedValueProduced);
                        break;

                    case DisplayedValueUpdated displayedValueUpdated:
                        context.Status(displayedValueUpdated.FormattedValues.Single(v => v.MimeType == "text/plain").Value);
                        AnsiConsole.RenderSuccessfulEvent(displayedValueUpdated);
                        break;

                    case ReturnValueProduced returnValueProduced:
                        AnsiConsole.RenderSuccessfulEvent(returnValueProduced);
                        break;

                    case StandardErrorValueProduced standardErrorValueProduced:
                        AnsiConsole.RenderErrorEvent(standardErrorValueProduced);
                        break;

                    // command completion events

                    case CommandFailed failed when failed.Command == command:
                        AnsiConsole.RenderErrorMessage(failed.Message);

                        _commandCompleted.Set();

                        break;

                    case CommandSucceeded succeeded when succeeded.Command == command:
                        _commandCompleted.Set();
                        break;
                }
            });

        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}