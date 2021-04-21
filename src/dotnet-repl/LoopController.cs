using System;
using System.CommandLine.IO;
using System.Reactive.Linq;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Pocket;
using Spectre.Console;

namespace Microsoft.DotNet.Interactive.Repl
{
    public class LoopController : IDisposable
    {
        private readonly TerminalHandler _terminalHandler;
        private readonly Kernel _kernel;
        private readonly CompositeDisposable _disposables = new();
        private readonly ManualResetEvent _commandCompleted = new(false);
        private readonly CancellationTokenSource _disposalTokenSource = new();
        private readonly StringBuilder _submissionInProgress = new();

        public LoopController(
            TerminalHandler terminalHandler,
            Kernel kernel)
        {
            _terminalHandler = terminalHandler;
            _kernel = kernel;

            _disposables.Add(() => _disposalTokenSource.Cancel());
        }

        public Markup Prompt { get; private set; } = PromptMarkup.Ready;

        public void Start()
        {
            Task.Run(RunAsync);
        }

        public async Task RunAsync()
        {
            while (!_disposalTokenSource.IsCancellationRequested)
            {
                _commandCompleted.Reset();

                _terminalHandler.Terminal.Render(Prompt);

                var input = await _terminalHandler.GetInputAsync(_disposalTokenSource.Token);

                if (_disposalTokenSource.IsCancellationRequested)
                {
                    return;
                }

                _submissionInProgress.AppendLine(input);

                var command = new SubmitCode(_submissionInProgress.ToString());

                KernelCommandResult? result = default;

                await _terminalHandler
                      .Terminal
                      .Status()
                      .StartAsync("⏳", async ctx =>
                      {
                          ctx.Spinner(Spinner.Known.Aesthetic);
                          ctx.SpinnerStyle(Style.Parse("purple"));

                          result = await _kernel.SendAsync(command);
                      });

                if (result is { })
                {
                    HandleKernelEvents(command, result);
                }
            }
        }

        public async Task CommandCompleted()
        {
            await Task.Yield();

            _commandCompleted.WaitOne();
        }

        private void HandleKernelEvents(
            KernelCommand command,
            KernelCommandResult result)
        {
            var events = result.KernelEvents.ToEnumerable().ToList();

            var isIncomplete = false;

            foreach (var @event in events)
            {
                switch (@event)
                {
                    // events that tell us whether the submission was valid

                    case IncompleteCodeSubmissionReceived incomplete when incomplete.Command == command:
                        isIncomplete = true;
                        SetPrompt(PromptMarkup.More);
                        break;

                    case CompleteCodeSubmissionReceived complete when complete.Command == command:
                        _submissionInProgress.Clear();
                        SetPrompt(PromptMarkup.Ready);
                        break;

                    case CodeSubmissionReceived codeSubmissionReceived:
                        break;


                    // command completion events

                    case CommandFailed failed when failed.Command == command:
                        if (!isIncomplete)
                        {
                            RenderErrorResult(failed.Message);
                        }
                        _commandCompleted.Set();

                        break;

                    case CommandSucceeded succeeded when succeeded.Command == command:
                        _commandCompleted.Set();
                        break;


                    // output / display events

                    case DisplayedValueProduced displayedValueProduced:
                        RenderSuccessfulResult(displayedValueProduced);
                        break;

                    case DisplayedValueUpdated displayedValueUpdated:
                        RenderSuccessfulResult(displayedValueUpdated);
                        break;

                    case ErrorProduced errorProduced:
                        if (!isIncomplete)
                        {
                            RenderErrorResult(errorProduced);
                        }

                        break;

                    case ReturnValueProduced returnValueProduced:
                        RenderSuccessfulResult(returnValueProduced);
                        break;

                    case StandardErrorValueProduced standardErrorValueProduced:
                        RenderErrorResult(standardErrorValueProduced);
                        break;

                    case StandardOutputValueProduced standardOutputValueProduced:
                        RenderSuccessfulResult(standardOutputValueProduced);
                        break;
                }
            }
        }

        private void SetPrompt(Markup prompt)
        {
            Prompt = prompt;
        }

        private void RenderSuccessfulResult(DisplayEvent errorProduced)
        {
            var displayText = errorProduced.FormattedValues.SingleOrDefault()?.Value ?? "";
            _terminalHandler.Terminal.Out.WriteLine(displayText);
            _terminalHandler.Terminal.Out.WriteLine();
        }

        private void RenderErrorResult(DisplayEvent errorProduced)
        {
            var displayText = errorProduced.FormattedValues.SingleOrDefault()?.Value ?? "";
            RenderErrorResult(displayText);
        }

        private void RenderErrorResult(string displayText)
        {
            _terminalHandler.Terminal.Render(new Markup($"[red]{displayText}[/]"));
            _terminalHandler.Terminal.Out.WriteLine();
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}