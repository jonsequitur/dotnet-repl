using System.Linq;
using Microsoft.DotNet.Interactive.Events;

namespace dotnet_repl
{
    internal static class KernelEventExtensions
    {
        public static string PlainTextValue(this DisplayEvent @event)
        {
            return @event.FormattedValues.FirstOrDefault()?.Value ?? string.Empty;
        }
    }
}