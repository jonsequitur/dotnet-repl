using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Pocket;
using RadLine;
using Spectre.Console;

namespace Microsoft.DotNet.Interactive.Repl
{
    public class LoopController : IDisposable
    {
        private readonly Kernel _kernel;
        private readonly Action _quit;
        private readonly CompositeDisposable _disposables = new();
        private readonly ManualResetEvent _commandCompleted = new(false);
        private readonly CancellationTokenSource _disposalTokenSource = new();
        private readonly SillyExecutionStatusMessageGenerator _executionStatusMessageGenerator = new();

        public LoopController(Kernel kernel, Action quit)
        {
            _kernel = kernel;
            _quit = quit;

            _disposables.Add(() => _disposalTokenSource.Cancel());
        }

        public LineEditorPrompt Prompt { get; set; } = new(
            "[bold aqua slowblink]  >[/]",
            "[bold aqua slowblink]...[/]");

        private LineEditor CreateLineEditor(Kernel kernel, IAnsiConsole? terminal = null, IInputSource? inputSource = null)
        {
            var editor = new LineEditor(terminal, inputSource)
            {
                MultiLine = true,
                Prompt = Prompt,
                Completion = new KernelCompletion(kernel),
                Highlighter = CreateWordHighlighter()
            };

            AddKeyBindings(editor);

            return editor;
        }

        public void Start()
        {
            Task.Run(RunAsync);
        }

        public async Task RunAsync()
        {
            var editor = CreateLineEditor(_kernel);

            while (!_disposalTokenSource.IsCancellationRequested)
            {
                _commandCompleted.Reset();

                var input = await editor.ReadLine(_disposalTokenSource.Token);

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
            }
        }

        private void HandleKernelEvents(
            KernelCommand command,
            KernelCommandResult result,
            StatusContext context)
        {
            var events = result.KernelEvents;

            var isIncomplete = false;

            using var _ = events.Subscribe(@event =>
            {
                switch (@event)
                {
                    // events that tell us whether the submission was valid

                    case IncompleteCodeSubmissionReceived incomplete when incomplete.Command == command:
                        isIncomplete = true;
                        break;

                    case CompleteCodeSubmissionReceived complete when complete.Command == command:
                        break;

                    case CodeSubmissionReceived codeSubmissionReceived:
                        break;

                    // output / display events

                    case ErrorProduced errorProduced:
                        if (!isIncomplete)
                        {
                            RenderErrorOutput(GetDisplayText(errorProduced));
                        }

                        break;

                    case StandardOutputValueProduced standardOutputValueProduced:
                        RenderSuccessfulOutput(GetDisplayText(standardOutputValueProduced));
                        break;

                    case DisplayedValueProduced displayedValueProduced:
                        RenderSuccessfulOutput(GetDisplayText(displayedValueProduced));
                        break;

                    case DisplayedValueUpdated displayedValueUpdated:
                        RenderSuccessfulOutput(GetDisplayText(displayedValueUpdated));
                        break;

                    case ReturnValueProduced returnValueProduced:
                        RenderSuccessfulOutput(GetDisplayText(returnValueProduced));
                        break;

                    case StandardErrorValueProduced standardErrorValueProduced:
                        RenderErrorOutput(GetDisplayText(standardErrorValueProduced));
                        break;

                    // command completion events

                    case CommandFailed failed when failed.Command == command:
                        if (!isIncomplete)
                        {
                            RenderErrorOutput(failed.Message);

                            // if (failed.Exception is { })
                            // {
                            //     AnsiConsole.WriteException(failed.Exception);
                            // }
                        }

                        _commandCompleted.Set();

                        break;

                    case CommandSucceeded succeeded when succeeded.Command == command:
                        // RenderSuccessfulOutput(displayText);
                        _commandCompleted.Set();
                        break;
                }
            });

            string GetDisplayText(DisplayEvent e) => e.FormattedValues.SingleOrDefault()?.Value ?? "";

            void RenderSuccessfulOutput(string message)
            {
                AnsiConsole.Render(
                    new Panel(Markup.Escape(message))
                        .Header("[green]✔[/]")
                        .Expand()
                        .RoundedBorder()
                        .BorderColor(Color.Green));
            }

            void RenderErrorOutput(string message)
            {
                AnsiConsole.Render(
                    new Panel(Markup.Escape(message))
                        .Header("[red]❌[/]")
                        .Expand()
                        .RoundedBorder()
                        .BorderColor(Color.Red));
            }
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        private void AddKeyBindings(LineEditor editor)
        {
            editor.KeyBindings.Add(
                ConsoleKey.C,
                ConsoleModifiers.Control,
                () => new Quit(_quit));

            editor.KeyBindings.Add<Clear>(
                ConsoleKey.C,
                ConsoleModifiers.Control | ConsoleModifiers.Alt);
        }

        private static WordHighlighter CreateWordHighlighter()
        {
            var wordHighlighter = new WordHighlighter();

            var keywordStyle = new Style(foreground: Color.LightSlateBlue);
            var operatorStyle = new Style(foreground: Color.SteelBlue1_1);

            var keywords = new[]
            {
                "async",
                "await",
                "bool",
                "break",
                "case",
                "catch",
                "class",
                "else",
                "finally",
                "for",
                "foreach",
                "if",
                "in",
                "int",
                "interface",
                "internal",
                "let",
                "match",
                "member",
                "mutable",
                "new",
                "not",
                "null",
                "open",
                "override",
                "private",
                "protected",
                "public",
                "record",
                "typeof",
                "return",
                "string",
                "struct",
                "switch",
                "then",
                "try",
                "type",
                "use",
                "using",
                "var",
                "void",
                "when",
                "while",
                "with",
            };

            var operatorsAndPunctuation = new[]
            {
                "_",
                "-",
                "->",
                ";",
                ":",
                "!",
                "?",
                ".",
                "'",
                "(",
                ")",
                "{",
                "}",
                "@",
                "*",
                "\"",
                "#",
                "%",
                "+",
                "<",
                "=",
                "=>",
                ">",
                "|",
                "|>",
                "$",
            };

            foreach (var keyword in keywords)
            {
                wordHighlighter.AddWord(keyword, keywordStyle);
            }

            foreach (var op in operatorsAndPunctuation)
            {
                wordHighlighter.AddWord(op, operatorStyle);
            }

            return wordHighlighter;
        }
    }
}