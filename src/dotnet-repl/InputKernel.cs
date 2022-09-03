// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Spectre.Console;

namespace dotnet_repl;

public class InputKernel : Kernel, IKernelCommandHandler<RequestInput>
{
    public InputKernel() : base("ask", null, null)
    {
    }

    public Task HandleAsync(RequestInput command, KernelInvocationContext context)
    {
        switch (command.InputTypeHint)
        {
            default:
                var value = AnsiConsole.Ask<string>($"Please provide a value for {command.Prompt}");

                if (value is { })
                {
                    context.Publish(new InputProduced(value, command));
                }

                break;
        }

        return Task.CompletedTask;
    }
}