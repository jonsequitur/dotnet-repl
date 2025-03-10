// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace dotnet_repl;

public class MarkdownKernel :
    Kernel,
    IKernelCommandHandler<SubmitCode>
{
    public MarkdownKernel() : base("markdown")
    {
        KernelInfo.LanguageName = "Markdown";
    }

    public Task HandleAsync(SubmitCode command, KernelInvocationContext context)
    {
        var @event = new DisplayedValueProduced(command.Code, command, [new FormattedValue("text/plain", command.Code)]);
        
        context.Publish(@event);
        
        return Task.CompletedTask;
    }
}