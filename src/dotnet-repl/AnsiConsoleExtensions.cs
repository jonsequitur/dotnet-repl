using System;
using System.Linq;
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
                    .SquareBorder()
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

        public static void RenderErrorEvent(this IAnsiConsole ansiConsole, DisplayEvent @event)
        {
            ansiConsole.Write(
                new Panel(GetMarkup(@event))
                    .Header("❌")
                    .Expand()
                    .RoundedBorder()
                    .BorderStyle(Theme.ErrorOutputBorder));
        }

        public static void RenderErrorMessage(this IAnsiConsole ansiConsole, string message)
        {
            ansiConsole.Write(
                new Panel(Markup.Escape(message))
                    .Header("❌")
                    .Expand()
                    .RoundedBorder()
                    .BorderStyle(Theme.ErrorOutputBorder));
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
}