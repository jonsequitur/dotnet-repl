// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;

namespace dotnet_repl;

public class HtmlKernel : Kernel, IKernelCommandHandler<SubmitCode>
{
    private readonly Kernel _innerJsKernel;

    public HtmlKernel(Kernel innerJsKernel) : base("html", "HTML", "5.0")
    {
        _innerJsKernel = innerJsKernel;
    }

    public async Task HandleAsync(SubmitCode command, KernelInvocationContext context)
    {
        var jsCode = $"await dotnetInteractive.domHtmlFragmentProcessor('{command.Code}');";

        await _innerJsKernel.SendAsync(new SubmitCode(jsCode));
    }
}