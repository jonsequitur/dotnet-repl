using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Browser;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.PowerShell;
using Pocket;

namespace dotnet_repl;

public static class KernelBuilder
{
    private static readonly HashSet<string> _nonStickyKernelNames = new()
    {
        "value",
        "markdown"
    };

    public static CompositeKernel CreateKernel(StartupOptions? options = null)
    {
        options ??= new();

        using var _ = Logger.Log.OnEnterAndExit("Creating kernels");

        var compositeKernel = new CompositeKernel()
                              .UseAboutMagicCommand()
                              .UseDebugMagicCommand()
                              .UseHelpMagicCommand()
                              .UseImportMagicCommand()
                              .UseQuitCommand();

        compositeKernel.AddMiddleware(async (command, context, next) =>
        {
            var rootKernel = (CompositeKernel)context.HandlingKernel.RootKernel;

            await next(command, context);

            if (command.GetType().Name == "DirectiveCommand")
            {
                var name = command.ToString()?.Replace("Directive: #!", "");

                if (name is { } &&
                    !_nonStickyKernelNames.Contains(name) &&
                    rootKernel.FindKernelByName(name) is { } kernel)
                {
                    rootKernel.DefaultKernelName = kernel.Name;
                }
            }
        });

        compositeKernel.Add(
            new CSharpKernel()
                .UseNugetDirective()
                .UseKernelHelpers()
                .UseWho()
                .UseValueSharing(),
            new[] { "c#", "C#" });

        compositeKernel.Add(
            new FSharpKernel()
                .UseDefaultFormatting()
                .UseNugetDirective()
                .UseKernelHelpers()
                .UseWho()
                .UseValueSharing(),
            new[] { "f#", "F#" });

        compositeKernel.Add(
            new PowerShellKernel()
                .UseProfiles()
                .UseValueSharing(),
            new[] { "powershell" });

        compositeKernel.Add(
            new KeyValueStoreKernel()
                .UseWho());

        var playwrightConnector = new PlaywrightKernelConnector();

        var (htmlKernel, jsKernel) = Task.Run(async () =>
        {
            var htmlKernel = await playwrightConnector.CreateKernelAsync("html");
            var jsKernel = await playwrightConnector.CreateKernelAsync("javascript");
            return (htmlKernel, jsKernel);
        }).Result;
        
        compositeKernel.Add(jsKernel, new[] { "js" });
        compositeKernel.Add(htmlKernel);
        compositeKernel.Add(new MarkdownKernel());
        compositeKernel.Add(new SqlDiscoverabilityKernel());
        compositeKernel.Add(new KqlDiscoverabilityKernel());

        var inputKernel = new InputKernel();

        compositeKernel.Add(inputKernel);
        compositeKernel.SetDefaultTargetKernelNameForCommand(
            typeof(RequestInput),
            inputKernel.Name);

        compositeKernel.DefaultKernelName = options.DefaultKernelName;

        if (compositeKernel.DefaultKernelName == "fsharp")
        {
            var fsharpKernel = compositeKernel.FindKernelByName("fsharp");

            fsharpKernel.DeferCommand(new SubmitCode("Formatter.Register(fun(x: obj)(writer: TextWriter)->fprintfn writer \"%120A\" x)"));
            fsharpKernel.DeferCommand(new SubmitCode("Formatter.Register(fun(x: System.Collections.IEnumerable)(writer: TextWriter)->fprintfn writer \"%120A\" x)"));
        }

        return compositeKernel;
    }
}