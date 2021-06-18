using System.Collections;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Formatting;
using Recipes;
using Spectre.Console;

namespace dotnet_repl
{
    internal static class KernelExtensions
    {
        public static T UseAboutMagicCommand<T>(this T kernel)
            where T : Kernel
        {
            var about = new Command("#!about", "Show version and build information")
            {
                Handler = CommandHandler.Create<KernelInvocationContext>(
                    context => context.Display(VersionSensor.Version()))
            };

            kernel.AddDirective(about);

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

        public static TKernel UseDebugDirective<TKernel>(this TKernel kernel)
            where TKernel : Kernel
        {
            kernel.AddDirective(new Command("#!debug")
            {
                Handler = CommandHandler.Create<KernelInvocationContext, IConsole, CancellationToken>(async (context, console, cancellationToken) =>
                {
                    await Attach();

                    async Task Attach()
                    {
                        var process = Process.GetCurrentProcess();

                        var processId = process.Id;

                        console.Out.WriteLine($"Attach your debugger to process {processId} ({process.ProcessName}).");

                        while (!Debugger.IsAttached)
                        {
                            await Task.Delay(500, cancellationToken);
                        }
                    }
                })
            });

            return kernel;
        }

        public static T UseHelpMagicCommand<T>(this T kernel)
            where T : Kernel
        {
            var help = new Command("#!help", "Show help for the REPL")
            {
                Handler = CommandHandler.Create<KernelInvocationContext>(
                    context =>
                    {
                        var console = AnsiConsole.Console;

                        var grid = new Grid();
                        grid.AddColumn();

                        grid.AddRow(new Paragraph("⌨ Shortcut keys:"));

                        var shortcutKeys = new Table();
                        shortcutKeys.AddColumn("Key");
                        shortcutKeys.AddColumn("What it does");
                        shortcutKeys.AddRow("Shift+Enter", "Inserts a newline without submitting the current code");
                        shortcutKeys.AddRow("Tab", "Show next completion");
                        shortcutKeys.AddRow("Shift-Tab", "Show previous completion");
                        shortcutKeys.AddRow("Ctrl-C", "Exit the REPL");
                        shortcutKeys.AddRow("Ctrl-Up", "Go back through your submission history (current session only)");
                        shortcutKeys.AddRow("Ctrl-Down", "Go forward through your submission history (current session only)");
                        grid.AddRow(shortcutKeys);

                        grid.AddRow(new Paragraph(""));
                        grid.AddRow(new Paragraph("🧙‍ Magic commands:"));

                        var magics = new Table();
                        magics.AddColumn("Kernel");
                        magics.AddColumn("Command");
                        magics.AddColumn("What it does");
                        kernel.VisitSubkernelsAndSelf(k =>
                        {
                            var kernelName = k.Name == ".NET"
                                                 ? "root"
                                                 : k.Name;

                            foreach (var magic in k.Directives.Where(d => !d.IsHidden))
                            {
                                magics.AddRow(kernelName, magic.Name, magic.Description ?? "");
                            }
                        });

                        grid.AddRow(magics);

                        console.Announce(grid);
                    })
            };

            help.AddAlias("#help");

            kernel.AddDirective(help);

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

                table.FormatTo(context);

                return true;
            });

            Formatter.Register(formatter);

            return kernel;
        }
    }
}