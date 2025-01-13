using System.Collections;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.PackageManagement;
using Recipes;
using Spectre.Console;

namespace dotnet_repl;

internal static class KernelExtensions
{
    public static T UseAboutMagicCommand<T>(this T kernel)
        where T : Kernel
    {
        var about = new KernelActionDirective("#!about")
        {
            Description = "Show version and build information"
        };

        kernel.AddDirective(about, (_, context) =>
        {
            context.Display(VersionSensor.Version());
            return Task.CompletedTask;
        });

        Formatter.Register<VersionSensor.BuildInfo>((info, context) =>
        {
            var table = new Grid();
            table.AddColumn(new GridColumn());
            table.AddColumn(new GridColumn());
            table.AddRow("Version", info.AssemblyInformationalVersion);
            table.AddRow("Built", info.BuildDate);
            table.AddRow("Home", "https://github.com/jonsequitur/dotnet-repl");

            table.FormatTo(context, PlainTextFormatter.MimeType);

            return true;
        }, PlainTextFormatter.MimeType);

        return kernel;
    }

    public static TKernel UseDebugMagicCommand<TKernel>(this TKernel kernel)
        where TKernel : Kernel
    {
        var debug = new KernelActionDirective("#!debug");

        kernel.AddDirective(debug, async (_, context) =>
        {
            var process = Process.GetCurrentProcess();

            var processId = process.Id;

            context.Display($"Attach your debugger to process {processId} ({process.ProcessName}).");

            while (!Debugger.IsAttached)
            {
                await Task.Delay(500, context.CancellationToken);
            }
        });

        return kernel;
    }

    public static T UseHelpMagicCommand<T>(this T kernel)
        where T : Kernel
    {
        var help = new KernelActionDirective("#!help")
        {
            Description = "Show help for the REPL"
        };

        kernel.AddDirective(help, (_, _) =>
        {
            var console = AnsiConsole.Console;

            var grid = new Grid();
            grid.AddColumn();

            grid.ShowShortcutKeys();

            grid.AddRow(new Paragraph(""));

            grid.ShowMagics(kernel);

            console.Announce(grid);

            return Task.CompletedTask;
        });

        return kernel;
    }

    public static CSharpKernel UseNugetDirective(this CSharpKernel kernel, bool forceRestore = false)
    {
        kernel.UseNugetDirective((k, resolvedPackageReference) =>
        {
            k.AddAssemblyReferences(resolvedPackageReference
                                        .SelectMany(r => r.AssemblyPaths));
            return Task.CompletedTask;
        }, forceRestore);

        return kernel;
    }

    public static FSharpKernel UseNugetDirective(this FSharpKernel kernel, bool forceRestore = false)
    {
        kernel.UseNugetDirective((k, resolvedPackageReference) =>
        {
            var resolvedAssemblies = resolvedPackageReference
                .SelectMany(r => r.AssemblyPaths);

            var packageRoots = resolvedPackageReference
                .Select(r => r.PackageRoot);

            k.AddAssemblyReferencesAndPackageRoots(resolvedAssemblies, packageRoots);

            return Task.CompletedTask;
        }, forceRestore);

        return kernel;
    }

    public static T UseTableFormattingForEnumerables<T>(this T kernel)
        where T : Kernel
    {
        var formatter = new SpectreFormatter<IEnumerable>((enumerable, context, console) =>
        {
            var columnIndexByName = new Dictionary<string, int>();
            var columnCount = 0;

            var table = new Table();

            var destructuredObjects = new List<IDictionary<string, object>>();

            foreach (var item in enumerable)
            {
                var dictionary = Destructurer.GetOrCreate(item?.GetType()).Destructure(item);
                destructuredObjects.Add(dictionary);

                foreach (var key in dictionary.Keys)
                {
                    if (!columnIndexByName.ContainsKey(key))
                    {
                        columnIndexByName[key] = columnCount++;
                        table.AddColumn(Markup.Escape(key));
                    }
                }
            }

            // add a row to the table for each item
            foreach (var dict in destructuredObjects)
            {
                var values = new List<object>(new object[columnCount]);

                // add a row to the table for each item
                foreach (var pair in dict)
                {
                    if (columnIndexByName.TryGetValue(pair.Key, out var index))
                    {
                        values[index] = pair.Value;
                    }
                }

                table.AddRow(values.Select(v => v is null ? "" : Markup.Escape(v.ToDisplayString())).ToArray());
            }

            table.FormatTo(context, PlainTextFormatter.MimeType);

            return true;
        });

        Formatter.Register(formatter);

        return kernel;
    }
}