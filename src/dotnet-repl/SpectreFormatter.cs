using System;
using Microsoft.DotNet.Interactive.Formatting;
using Spectre.Console;

namespace dotnet_repl
{
    internal class SpectreFormatter : ITypeFormatter
    {
        private readonly Func<object, FormatContext, IAnsiConsole, bool> _format = null!;

        private protected SpectreFormatter(Type type)
        {
            Type = type;
        }

        public SpectreFormatter(Type type, Func<object, FormatContext, IAnsiConsole, bool> format) : this(type)
        {
            Type = type;
            _format = format;
        }

        public virtual bool Format(object value, FormatContext context) => _format(value, context, CreateAnsiConsole(context));

        public string MimeType => PlainTextFormatter.MimeType;

        public Type Type { get; }

        protected IAnsiConsole CreateAnsiConsole(FormatContext context) =>
            new AnsiConsoleFactory().Create(new AnsiConsoleSettings
            {
                Ansi = AnsiSupport.Yes,
                Out = new AnsiConsoleOutput(context.Writer)
            });
    }

    internal class SpectreFormatter<T> : SpectreFormatter
    {
        private readonly Func<T, FormatContext, IAnsiConsole, bool> _format;

        public SpectreFormatter(Func<T, FormatContext, IAnsiConsole, bool> format) : base(typeof(T))
        {
            _format = format;
        }

        public bool Format(T value, FormatContext context) => _format(value, context, CreateAnsiConsole(context));

        public override bool Format(object value, FormatContext context)
        {
            if (value is T t)
            {
                return Format(t, context);
            }
            else
            {
                return false;
            }
        }
    }
}