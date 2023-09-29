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
    private readonly CompositeKernel _kernel;

    public NotebookRunner(CompositeKernel kernel)
    {
        _kernel = kernel;
    }

    public async Task<InteractiveDocument> RunNotebookAsync(
        InteractiveDocument notebook,
        IDictionary<string, string>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var resultDocument = new InteractiveDocument();

        if (parameters is not null)
        {
            // incoming parameters are treated case insensitively
            parameters = new Dictionary<string, string>(parameters, StringComparer.InvariantCultureIgnoreCase);

            var inputKernel = _kernel.ChildKernels.OfType<InputKernel>().FirstOrDefault();

            if (inputKernel is not null)
            {
                inputKernel.GetInputValueAsync = key =>
                {
                    if (parameters.TryGetValue(key, out var value))
                    {
                        return Task.FromResult<string?>(value);
                    }
                    else
                    {
                        return Task.FromResult<string?>(null);
                    }
                };
            }
        }

        foreach (var element in notebook.Elements)
        {
            var command = new SubmitCode(element.Contents, element.KernelName);

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

            var resultElement = new InteractiveDocumentElement(element.Contents, element.KernelName, outputs.ToArray());
            resultElement.Metadata ??= new Dictionary<string, object>();
            resultElement.Metadata.Add("dotnet_repl_cellExecutionStartTime", startTime);
            resultElement.Metadata.Add("dotnet_repl_cellExecutionEndTime", DateTimeOffset.Now);

            resultDocument.Add(resultElement);
        }

        var defaultKernelName = _kernel.DefaultKernelName;

        var defaultKernel = _kernel.ChildKernels.SingleOrDefault(k => k.Name == defaultKernelName);

        var languageName = defaultKernel?.KernelInfo.LanguageName ??
                           notebook.GetDefaultKernelName() ??
                           "C#";

        resultDocument.WithJupyterMetadata(languageName);

        return resultDocument;
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
        new(errorName: "Error",
            errorValue: errorProduced.Message);

    private ErrorElement CreateErrorOutputElement(CommandFailed failed) =>
        new(errorName: "Error",
            errorValue: failed.Message,
            stackTrace: failed.Exception switch
            {
                { } ex => (ex.StackTrace ?? "").SplitIntoLines(),
                _ => Array.Empty<string>()
            });
}