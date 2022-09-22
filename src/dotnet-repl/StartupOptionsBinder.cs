using System.Collections.Generic;
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
    private readonly Option<OutputFormat> _outputFormat;
    private readonly Option<FileInfo> _outputPath;
    private readonly Option<IDictionary<string, string>> _inputs;

    public StartupOptionsBinder(
        Option<string> defaultKernelOption,
        Option<DirectoryInfo> workingDirOption,
        Option<FileInfo> notebookOption,
        Option<DirectoryInfo> logPathOption,
        Option<bool> exitAfterRun,
        Option<OutputFormat> outputFormat,
        Option<FileInfo> outputPath,
        Option<IDictionary<string, string>> inputs)
    {
        _defaultKernelOption = defaultKernelOption;
        _workingDirOption = workingDirOption;
        _notebookOption = notebookOption;
        _logPathOption = logPathOption;
        _exitAfterRun = exitAfterRun;
        _outputFormat = outputFormat;
        _outputPath = outputPath;
        _inputs = inputs;
    }

    protected override StartupOptions GetBoundValue(BindingContext bindingContext)
    {
        return new StartupOptions(
            bindingContext.ParseResult.GetValueForOption(_defaultKernelOption)!,
            bindingContext.ParseResult.GetValueForOption(_workingDirOption),
            bindingContext.ParseResult.GetValueForOption(_notebookOption),
            bindingContext.ParseResult.GetValueForOption(_logPathOption),
            bindingContext.ParseResult.GetValueForOption(_exitAfterRun),
            bindingContext.ParseResult.GetValueForOption(_outputFormat),
            bindingContext.ParseResult.GetValueForOption(_outputPath),
            bindingContext.ParseResult.GetValueForOption(_inputs));
    }
}