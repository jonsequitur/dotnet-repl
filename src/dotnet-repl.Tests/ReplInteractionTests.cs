using System;
using System.IO;
using dotnet_repl.Tests.Utility;
using Microsoft.DotNet.Interactive;
using Microsoft.Extensions.DependencyInjection;
using Pocket;
using Pocket.For.Xunit;
using Spectre.Console;
using Xunit;
using Xunit.Abstractions;
using CompositeDisposable = System.Reactive.Disposables.CompositeDisposable;

namespace dotnet_repl.Tests;

[LogToPocketLogger(@"c:\temp\dotnet-repl.test.log")]
[Collection("Do not parallelize")]
public abstract class ReplInteractionTests : IDisposable
{
    private readonly CompositeDisposable _disposables;
    protected readonly Repl Repl;
    protected readonly ServiceProvider ServiceProvider;

    protected ReplInteractionTests(ITestOutputHelper output)
    {
        Kernel = KernelBuilder.CreateKernel(new StartupOptions("csharp"));

        AnsiConsole = Spectre.Console.AnsiConsole.Create(new AnsiConsoleSettings
        {
            Out = new AnsiConsoleOutput(Out),
            Ansi = AnsiSupport.Yes
        });

        ServiceProvider = new ServiceCollection()
                          .AddSingleton(Kernel)
                          .AddSingleton(new KernelCompletion(Kernel))
                          .BuildServiceProvider();

        Repl = new(
            Kernel,
            () => QuitWasSent = true,
            AnsiConsole,
            In);

        var  _ = Repl.StartAsync();

        _disposables = new CompositeDisposable
        {
            output.SubscribeToPocketLogger(),
            Repl,
            Kernel,
            ServiceProvider
        };
    }

    public IAnsiConsole AnsiConsole { get; }

    public CompositeKernel Kernel { get; }

    public TestInputSource In { get; } = new();

    public StringWriter Out { get; } = new();

    public bool QuitWasSent { get; private set; }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}