using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Interactive.Documents;
using TRexLib;

namespace dotnet_repl;

internal static class Trx
{
    internal static string ToTrxString(this InteractiveDocument resultNotebook)
    {
        var testResults = new List<TestResult>();

        for (var i = 0; i < resultNotebook.Elements.Count; i++)
        {
            var element = resultNotebook.Elements[i];

            var content = element.Contents.Trim() ?? "";
            var indexOfNewLine = content.IndexOfAny(new[] { '\r', '\n' });

            if (indexOfNewLine != -1)
            {
                content = content[..indexOfNewLine];
            }

            var cell = $"Cell {i + 1,4}";

            DateTimeOffset startTime = default;
            if (element?.Metadata?.TryGetValue("dotnet_repl_cellExecutionStartTime", out var startTimeObj) == true &&
                startTimeObj is DateTimeOffset startTimeD)
            {
                startTime = startTimeD;
            }

            DateTimeOffset endTime = default;
            if (element?.Metadata?.TryGetValue("dotnet_repl_cellExecutionEndTime", out var endTimeObj) == true &&
                endTimeObj is DateTimeOffset endTimeD)
            {
                endTime = endTimeD;
            }

            var testResult = new TestResult(
                fullyQualifiedTestName: $"{cell}: {content}",
                outcome: element!.Outputs.OfType<ErrorElement>().Any()
                             ? TestOutcome.Failed
                             : TestOutcome.Passed,
                startTime: startTime,
                endTime: endTime,
                duration: endTime - startTime,
                output: element.Outputs.FirstOrDefault() switch
                {
                    DisplayElement displayElement => displayElement.Data.FirstOrDefault().Value.ToString(),
                    ErrorElement errorElement1 => errorElement1.ErrorValue,
                    ReturnValueElement returnValueElement => returnValueElement.Data.FirstOrDefault().Value.ToString(),
                    TextElement textElement => textElement.Text,
                    _ => null
                },
                stacktrace: element.Outputs.FirstOrDefault() switch
                {
                    ErrorElement errorElement => string.Join("\n", errorElement.StackTrace),
                    _ => null
                });

            testResults.Add(testResult);
        }

        using var writer = new StringWriter();

        var testOutputWriter = new TestOutputFileWriter(writer);

        testOutputWriter.Write(new TestResultSet(testResults));

        return writer.ToString();
    }
}