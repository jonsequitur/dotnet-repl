using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Formatting.TabularData;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace dotnet_repl
{
    internal class DefaultSpectreFormatterSet
    {
        internal static readonly ITypeFormatter[] DefaultFormatters =
        {
            new SpectreFormatter<IRenderable>((value, context, ansiConsole) =>
            {
                ansiConsole.Write(value);
                return true;
            }),

            new SpectreFormatter<TabularDataResource>((value, context, ansiConsole) =>
            {
                var table = new Table();

                foreach (var field in value.Schema.Fields)
                {
                    table.AddColumn(field.Name);
                }

                foreach (var row in value.Data)
                {
                    var values = value
                        .Schema
                        .Fields
                        .Select(f => Markup.Escape(row[f.Name].ToDisplayString("text/plain")))
                        .ToArray();

                    table.AddRow(values);
                }

                ansiConsole.Write(table);

                return true;
            }),

            new SpectreFormatter(typeof(DataExplorer<>), (value, context, console) =>
            {
                if (((dynamic) value).Data is TabularDataResource tabular)
                {
                    tabular.FormatTo(context, PlainTextFormatter.MimeType);
                    return true;
                }

                return false;
            }),

            new SpectreFormatter<IDictionary<string, object>>((dict, context, console) =>
            {
                var table = new Table();

                foreach (var key in dict.Keys)
                {
                    table.AddColumn(key);
                }

                table.AddRow(dict.Keys.Select(k => Markup.Escape(dict[k]?.ToDisplayString() ?? string.Empty)).ToArray());

                console.Write(table);

                return true;
            }),

            new SpectreFormatter<string>((value, context, console) =>
            {
                console.Write(Markup.Escape(value));

                return true;
            }),
        };

        public void Register()
        {
            foreach (var formatter in DefaultFormatters)
            {
                Formatter.Register(formatter);
            }
        }
    }
}