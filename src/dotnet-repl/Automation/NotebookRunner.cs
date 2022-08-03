using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using dotnet_repl;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Documents;
using Microsoft.DotNet.Interactive.Events;

namespace Automation;

public class NotebookRunner
{
    private readonly Kernel _kernel;

    public NotebookRunner(Kernel kernel)
    {
        _kernel = kernel;
    }

    public async Task<InteractiveDocument> RunNotebookAsync(
        InteractiveDocument notebook,
        CancellationToken cancellationToken = default)
    {
        var documentElements = new List<InteractiveDocumentElement>();

        foreach (var element in notebook.Elements)
        {
            var command = new SubmitCode(element.Contents, element.Language);

            var events = _kernel.KernelEvents.Replay();

            using var connect = events.Connect();

            var startTime = DateTimeOffset.Now;
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
                        if (CreateBufferedStandardOutAndErrElement(stdOut, stdErr) is { } te)
                        {
                            outputs.Add(te);
                        }

                        outputs.Add(CreateErrorOutputElement(failed));
                        tcs.SetResult();

                        break;

                    case CommandSucceeded succeeded when succeeded.Command == command:
                        if (CreateBufferedStandardOutAndErrElement(stdOut, stdErr) is { } textElement)
                        {
                            outputs.Add(textElement);
                        }

                        tcs.SetResult();

                        break;
                }
            });

            await tcs.Task;

            var resultElement = new InteractiveDocumentElement(element.Contents, element.Language, outputs.ToArray());
            resultElement.Metadata ??= new Dictionary<string, object>();
            resultElement.Metadata.Add("dotnet_repl_cellExecutionStartTime", startTime);
            resultElement.Metadata.Add("dotnet_repl_cellExecutionEndTime", DateTimeOffset.Now);

            documentElements.Add(resultElement);
        }

        return new(documentElements);
    }

    private TextElement? CreateBufferedStandardOutAndErrElement(
        StringBuilder? stdOut,
        StringBuilder? stdErr)
    {
        if (stdOut is null && stdErr is null)
        {
            return null;
        }

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

        return new TextElement(sb.ToString(), "stdout");
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
                { } ex => (ex.StackTrace ?? "").SplitIntoLines(),
                _ => Array.Empty<string>()
            });
}