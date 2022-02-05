// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.CommandLine.Binding;
using System.IO;

namespace dotnet_repl;

internal class StartupOptionsBinder : BinderBase<StartupOptions>
{
    private readonly Option<string> _defaultKernelOption;
    private readonly Option<DirectoryInfo> _workingDirOption;
    private readonly Option<FileInfo> _notebookOption;
    private readonly Option<DirectoryInfo> _logPathOption;
    private readonly Option<bool> _exitAfterRun;

    public StartupOptionsBinder(
        Option<string> defaultKernelOption,
        Option<DirectoryInfo> workingDirOption,
        Option<FileInfo> notebookOption,
        Option<DirectoryInfo> logPathOption,
        Option<bool> exitAfterRun)
    {
        _defaultKernelOption = defaultKernelOption;
        _workingDirOption = workingDirOption;
        _notebookOption = notebookOption;
        _logPathOption = logPathOption;
        _exitAfterRun = exitAfterRun;
    }

    protected override StartupOptions GetBoundValue(BindingContext bindingContext)
    {
        return new StartupOptions(
            bindingContext.ParseResult.GetValueForOption(_defaultKernelOption),
            bindingContext.ParseResult.GetValueForOption(_workingDirOption),
            bindingContext.ParseResult.GetValueForOption(_notebookOption),
            bindingContext.ParseResult.GetValueForOption(_logPathOption),
            bindingContext.ParseResult.GetValueForOption(_exitAfterRun)
        );
    }
}