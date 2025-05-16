using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;

namespace dotnet_repl;

public static class RootCommandExtensions
{
    public static async Task<int> InvokeAsync(this RootCommand rootCommand, string[] args)
    {
        var parseResult = rootCommand.Parse(args);

        var action = parseResult.Action;

        switch (action)
        {
            case AsynchronousCommandLineAction asyncAction:
                return await asyncAction.InvokeAsync(parseResult, CancellationToken.None);

            case SynchronousCommandLineAction syncAction:
                return syncAction.Invoke(parseResult);

            default:
                throw new ArgumentOutOfRangeException(nameof(action));
        }
    }
}