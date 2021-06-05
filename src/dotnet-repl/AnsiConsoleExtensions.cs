using System;
using System.Linq;
using System.Text;
using Microsoft.DotNet.Interactive.Events;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace dotnet_repl
{
    internal static class AnsiConsoleExtensions
    {
        public static void Announce(
            this IAnsiConsole ansiConsole,
            string text,
            Theme? theme = default)
        {
            theme ??= Theme.Default;

            ansiConsole.Write(
                new Panel(
                        new Paragraph(text, theme.AnnouncementText))
                    .BorderStyle(theme.AnnouncementBorder)
                    .HeavyBorder()
                    .Expand());
        } 
        
        public static void Announce(
            this IAnsiConsole ansiConsole,
            IRenderable content,
            Theme? theme = default)
        {
            theme ??= Theme.Default;

            ansiConsole.Write(
                new Panel(content)
                    .BorderStyle(theme.AnnouncementBorder)
                    .HeavyBorder()
                    .Expand());
        }

        public static void RenderSplash(
            this IAnsiConsole ansiConsole,
            StartupOptions startupOptions)
        {
            string language;

            switch (startupOptions.DefaultKernelName)
            {
                case "csharp":
                    language = "C#";
                    break;
                case "fsharp":
                    language = "F#";
                    Theme.Default = Theme.FSharp();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            ansiConsole.Write(
                new FigletText($".NET REPL: {language}")
                    .Centered()
                    .Color(Theme.Default.SplashColor));

            ansiConsole.Write(
                new Markup(".NET Interactive 💓 Spectre.Console\n\n", Theme.Default.Splash)
                    .Centered());
        }

        public static IRenderable GetErrorDisplay(
            DisplayEvent @event,
            string header = "❌",
            Theme? theme = default) =>
            new Panel(GetMarkup(@event))
                .Header(header)
                .Expand()
                .RoundedBorder()
                .BorderStyle((theme ?? Theme.Default).ErrorOutputBorder);

        public static Panel GetErrorDisplay(
            string message,
            string header = "❌",
            Theme? theme = default)
        {
            return new Panel(Markup.Escape(message))
                .Header(header)
                .Expand()
                .RoundedBorder()
                .BorderStyle((theme ?? Theme.Default).ErrorOutputBorder);
        }

        public static Panel GetSuccessDisplay(
            DisplayEvent @event,
            string header = "✔",
            Theme? theme = default)
        {
            return new Panel(GetMarkup(@event))
                .Header(header)
                .Expand()
                .RoundedBorder()
                .BorderStyle((theme ?? Theme.Default).SuccessOutputBorder);
        }

        public static Panel GetSuccessDisplay(
            string message,
            string header,
            Theme? theme = default)
        {
            return new Panel(Markup.Escape(message))
                .Header(header)
                .Expand()
                .RoundedBorder()
                .BorderStyle((theme ?? Theme.Default).SuccessOutputBorder);
        }

        public static void RenderErrorMessage(
            this IAnsiConsole ansiConsole,
            string message,
            string header = "❌",
            Theme? theme = default)
        {
            ansiConsole.Write(GetErrorDisplay(message, header, theme));
        }

        public static void RenderSuccessMessage(
            this IAnsiConsole ansiConsole,
            string message,
            string header = "✔",
            Theme? theme = default)
        {
            ansiConsole.Write(GetSuccessDisplay(message, header, theme));
        }

        public static void RenderBufferedStandardOutAndErr(
            this IAnsiConsole ansiConsole,
            StringBuilder? stdOut = null,
            StringBuilder? stdErr = null,
            Theme? theme = default)
        {
            if (stdOut is { })
            {
                ansiConsole.RenderSuccessMessage(stdOut.ToString(), "✒", theme);
            }

            if (stdErr is { })
            {
                ansiConsole.RenderErrorMessage(stdErr.ToString(), "✒", theme);
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
}