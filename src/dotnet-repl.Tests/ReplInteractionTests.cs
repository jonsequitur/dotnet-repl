using System;
using System.IO;
using dotnet_repl.Tests.Utility;
using Microsoft.DotNet.Interactive;
using Microsoft.Extensions.DependencyInjection;
using Pocket;
using Spectre.Console;
using Xunit.Abstractions;
using CompositeDisposable = System.Reactive.Disposables.CompositeDisposable;

namespace dotnet_repl.Tests
{
    public abstract class ReplInteractionTests : IDisposable
    {
        private readonly CompositeDisposable _disposables;
        protected readonly Repl Repl;
        protected readonly ServiceProvider ServiceProvider;

        protected ReplInteractionTests(ITestOutputHelper output)
        {
            Kernel = Repl.CreateKernel(new StartupOptions("csharp"));
         
            AnsiConsole = new AnsiConsoleFactory().Create(new AnsiConsoleSettings
            {
                Out = new AnsiConsoleOutput(Out)
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

            Repl.Start();

            _disposables = new CompositeDisposable
            {
                output.SubscribeToPocketLogger(),
                Repl,
                Kernel
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
}