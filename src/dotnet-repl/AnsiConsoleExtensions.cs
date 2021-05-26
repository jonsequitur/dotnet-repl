using System;
using System.Linq;
using System.Text;
using Microsoft.DotNet.Interactive.Events;
using Spectre.Console;

namespace dotnet_repl
{
    internal static class AnsiConsoleExtensions
    {
        public static void Announce(this IAnsiConsole ansiConsole, string text)
        {
            ansiConsole.Write(
                new Panel(
                        new Paragraph(text, Theme.AnnouncementText))
                    .BorderStyle(Theme.AnnouncementBorder)
                    .HeavyBorder()
                    .Expand());
        }

        public static void RenderSplash(this IAnsiConsole ansiConsole, StartupOptions startupOptions)
        {
            var language = startupOptions.DefaultKernelName switch
            {
                "csharp" => "C#",
                "fsharp" => "F#",
                _ => throw new ArgumentOutOfRangeException()
            };

            ansiConsole.Write(
                new FigletText($".NET REPL: {language}")
                    .Centered()
                    .Color(Theme.SplashColor));

            ansiConsole.Write(
                new Markup("Built with .NET Interactive + Spectre.Console\n\n", Theme.Splash)
                    .Centered());
        }

        public static void RenderSuccessfulEvent(this IAnsiConsole ansiConsole, DisplayEvent @event)
        {
            ansiConsole.Write(
                new Panel(GetMarkup(@event))
                    .Header("✔")
                    .Expand()
                    .RoundedBorder()
                    .BorderStyle(Theme.SuccessOutputBorder));
        }

        public static void RenderErrorEvent(this IAnsiConsole ansiConsole, DisplayEvent @event, string header = "❌")
        {
            ansiConsole.Write(
                new Panel(GetMarkup(@event))
                    .Header(header)
                    .Expand()
                    .RoundedBorder()
                    .BorderStyle(Theme.ErrorOutputBorder));
        }

        public static void RenderSuccessMessage(this IAnsiConsole ansiConsole, string message, string header = "✔")
        {
            ansiConsole.Write(
                new Panel(Markup.Escape(message))
                    .Header(header)
                    .Expand()
                    .RoundedBorder()
                    .BorderStyle(Theme.SuccessOutputBorder));
        }

        public static void RenderErrorMessage(this IAnsiConsole ansiConsole, string message, string header = "❌")
        {
            ansiConsole.Write(
                new Panel(Markup.Escape(message))
                    .Header(header)
                    .Expand()
                    .RoundedBorder()
                    .BorderStyle(Theme.ErrorOutputBorder));
        }

        public static void RenderBufferedStandardOutAndErr(
            this IAnsiConsole ansiConsole,
            StringBuilder? stdOut = null,
            StringBuilder? stdErr = null)
        {
            if (stdOut is { })
            {
                ansiConsole.RenderSuccessMessage(stdOut.ToString(), "✒");
            }

            if (stdErr is { })
            {
                ansiConsole.RenderErrorMessage(stdErr.ToString(), "✒");
            }
        }

        private static Markup GetMarkup(DisplayEvent @event)
        {
            var formattedValue = @event.FormattedValues.First();

            var markup = formattedValue.MimeType switch
            {
                "text/plain+spectre" => new Markup(formattedValue.Value),
                _ => new Markup(Markup.Escape(formattedValue.Value))
            };

            return markup;
        }
    }

    internal static class KernelEventExtensions
    {
        public static string PlainTextValue(this DisplayEvent @event)
        {
            return @event.FormattedValues.FirstOrDefault()?.Value ?? string.Empty;
        }
    }
}