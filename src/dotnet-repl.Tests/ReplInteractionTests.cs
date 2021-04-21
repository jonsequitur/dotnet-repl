using System;
using Microsoft.DotNet.Interactive.Repl;
using Pocket;
using Pocket.For.Xunit;
using Xunit.Abstractions;
using CompositeDisposable = System.Reactive.Disposables.CompositeDisposable;

namespace dotnet_repl.Tests
{
    [LogToPocketLogger(@"c:\temp\repltest.log")]
    public abstract class ReplInteractionTests : IDisposable
    {
        protected readonly TestTerminal Terminal;
        protected readonly TerminalHandler TerminalHandler;
        private readonly CompositeDisposable _disposables;
        protected readonly LoopController Loop;

        protected ReplInteractionTests(ITestOutputHelper output)
        {
            Terminal = new TestTerminal();
            TerminalHandler = new TerminalHandler(Terminal, Terminal.ReadKey);

            var kernel = CommandLineParser.CreateKernel(new StartupOptions("csharp"));

            Loop = new(TerminalHandler, kernel);

            Loop.Start();

            _disposables = new CompositeDisposable
            {
                output.SubscribeToPocketLogger(),
                Loop,
                kernel
            };
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}