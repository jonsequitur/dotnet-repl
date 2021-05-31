using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Formatting.TabularData;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace dotnet_repl
{
    public class DefaultSpectreFormatterSet
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

            new SpectreFormatter<IEnumerable>((enumerable, context, console) =>
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

                    table.AddRow(values.Select(v => v is null ? "" :Markup.Escape( v.ToDisplayString())).ToArray());
                }

                table.FormatTo(context);

                return true;
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