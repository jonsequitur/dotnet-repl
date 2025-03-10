using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Pocket;
using RadLine;
using static Pocket.Logger<dotnet_repl.KernelCompletion>;

namespace dotnet_repl;

public class KernelCompletion
{
    private readonly Kernel _kernel;

    public KernelCompletion(Kernel kernel)
    {
        _kernel = kernel;
    }

    public IEnumerable<string> GetCompletions(LineBuffer buffer)
    {
        return GetCompletionsAsync(buffer).Result;
    }

    private async Task<IEnumerable<string>> GetCompletionsAsync(LineBuffer buffer)
    {
        var command = new RequestCompletions(
            buffer.Content,
            new LinePosition(0, buffer.CursorPosition));

        var result = await _kernel.SendAsync(command);

        var completionsProduced = result
                                  .Events
                                  .OfType<CompletionsProduced>()
                                  .FirstOrDefault();

        if (completionsProduced is null)
        {
            return Enumerable.Empty<string>();
        }

        var code = buffer.Content[..buffer.CursorPosition];

        var matches = completionsProduced
                      .Completions
                      .Select(c => c.InsertText)
                      .OrderBy(c => c)
                      .Where(text => text is not null &&
                                     text.StartsWith(code.Split('.', ' ').LastOrDefault() ?? code))
                      .ToArray();

        Log.Info(
            "buffer: {buffer}, code: {code}, matches: {matches}",
            buffer.Content,
            code,
            string.Join(",", matches));

        return matches;
    }
}