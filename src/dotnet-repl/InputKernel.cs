using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Spectre.Console;

namespace dotnet_repl;

public class InputKernel :
    Kernel,
    IKernelCommandHandler<RequestInput>
{
    public InputKernel() : base("ask", null, null)
    {
    }

    public Func<string, Task<string?>>? GetInputValueAsync { get; set; }

    public async Task HandleAsync(RequestInput command, KernelInvocationContext context)
    {
        if (GetInputValueAsync is not null &&
            await GetInputValueAsync(command.ValueName) is { } value &&
            !string.IsNullOrWhiteSpace(value))
        {
            context.Publish(new InputProduced(value, command));
        }
        else
        {
            switch (command.InputTypeHint)
            {
                default:
                    value = AnsiConsole.Ask<string>($"Please provide a value for {command.ValueName}");

                    if (value is { })
                    {
                        context.Publish(new InputProduced(value, command));
                    }

                    break;
            }
        }
    }
}