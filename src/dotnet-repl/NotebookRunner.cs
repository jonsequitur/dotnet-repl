using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Documents;
using Microsoft.DotNet.Interactive.Events;

namespace dotnet_repl;

public class NotebookRunner
{
    private readonly Kernel _kernel;

    public NotebookRunner(Kernel kernel)
    {
        _kernel = kernel;
    }

    public async Task<InteractiveDocument> RunNotebookAsync(
        InteractiveDocument notebook,
        CancellationToken cancellationToken)
    {
        var documentElements = new List<InteractiveDocumentElement>();

        foreach (var element in notebook.Elements)
        {
            var command = new SubmitCode(element.Contents, element.Language);

            var events = _kernel.KernelEvents.Replay();

            using var connect = events.Connect();

            var result = _kernel.SendAsync(command, cancellationToken);

            var tcs = new TaskCompletionSource();
            StringBuilder? stdOut = default;
            StringBuilder? stdErr = default;

            var outputs = new List<InteractiveDocumentOutputElement>();

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
                        outputs.Add(CreateErrorOutputElement(errorProduced));

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
                        outputs.Add(CreateDisplayOutputElement(displayedValueProduced));

                        break;

                    case DisplayedValueUpdated displayedValueUpdated:
                        outputs.Add(CreateDisplayOutputElement(displayedValueUpdated));
                        break;

                    case ReturnValueProduced returnValueProduced:

                        if (returnValueProduced.Value is DisplayedValue)
                        {
                            break;
                        }

                        outputs.Add(CreateDisplayOutputElement(returnValueProduced));
                        break;

                    // command completion events

                    case CommandFailed failed when failed.Command == command:
                        outputs.Add(CreateBufferedStandardOutAndErrElement(stdOut, stdErr));

                        outputs.Add(CreateErrorOutputElement(failed));
                        tcs.SetResult();

                        break;

                    case CommandSucceeded succeeded when succeeded.Command == command:
                        outputs.Add(CreateBufferedStandardOutAndErrElement(stdOut, stdErr));

                        tcs.SetResult();

                        break;
                }
            });

            await tcs.Task;

            var resultElement = new InteractiveDocumentElement(element.Language, element.Contents, outputs.ToArray());

            documentElements.Add(resultElement);
        }

        return new(documentElements);
    }

    private TextElement CreateBufferedStandardOutAndErrElement(
        StringBuilder? stdOut,
        StringBuilder? stdErr)
    {
        var sb = new StringBuilder();

        if (stdOut is { })
        {
            sb.Append(stdOut);
        }

        if (stdOut is { } && stdErr is { })
        {
            sb.Append("\n\n");
        }

        if (stdErr is { })
        {
            sb.Append(stdErr);
        }

        return new TextElement(sb.ToString());
    }

    private DisplayElement CreateDisplayOutputElement(DisplayEvent displayedValueProduced) =>
        new(displayedValueProduced
            .FormattedValues
            .ToDictionary(
                v => v.MimeType,
                v => (object)v.Value));

    private ErrorElement CreateErrorOutputElement(ErrorProduced errorProduced) =>
        new("Error",
            errorProduced.Message,
            Array.Empty<string>());

    private ErrorElement CreateErrorOutputElement(CommandFailed failed) =>
        new("Error",
            failed.Message,
            failed.Exception switch
            {
                { } ex => (ex.StackTrace ?? "").Split(new[] { "\r\n", "\n" }, StringSplitOptions.TrimEntries),
                _ => Array.Empty<string>()
            });
}