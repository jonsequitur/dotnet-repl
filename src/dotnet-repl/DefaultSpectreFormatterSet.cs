// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Formatting;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Microsoft.DotNet.Interactive.Repl
{
    public class DefaultSpectreFormatterSet
    {
        internal static readonly ITypeFormatter[] DefaultFormatters =
        {
            new PlainTextFormatter<IRenderable>((value, context) =>
            {
                var ansiConsole = new AnsiConsoleFactory().Create(new AnsiConsoleSettings
                {
                    Ansi = AnsiSupport.Yes,
                    Out = new AnsiConsoleOutput(context.Writer)
                });

                ansiConsole.Write(value);

                return true;
            })
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