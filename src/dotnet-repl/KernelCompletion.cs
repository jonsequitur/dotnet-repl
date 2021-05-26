using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Pocket;
using RadLine;
using static Pocket.Logger<dotnet_repl.KernelCompletion>;

namespace dotnet_repl
{
    public class KernelCompletion : ITextCompletion
    {
        private readonly Kernel _kernel;

        public KernelCompletion(Kernel kernel)
        {
            _kernel = kernel;
        }

        public IEnumerable<string> GetCompletions(string prefix, string word, string suffix)
        {
            return GetCompletionsAsync(prefix, word, suffix).Result;
        }

        private async Task<IEnumerable<string>> GetCompletionsAsync(string prefix, string word, string suffix)
        {
            var code = prefix + word;

            var command = new RequestCompletions(
                code,
                new LinePosition(0, prefix.Length + word.Length));

            var result = await _kernel.SendAsync(command);

            var results = await result
                                .KernelEvents
                                .OfType<CompletionsProduced>()
                                .FirstOrDefaultAsync();

            var matches = results.Completions
                                 .Where(c => c.InsertText.Contains(code.Split('.', ' ').LastOrDefault() ?? code))
                                 .Select(c => c.InsertText);

            Log.Info(
                "prefix: {prefix}, code: {code}, suffix: {suffix}, matches: {matches}",
                prefix,
                code,
                suffix,
                string.Join(",", matches));

            return matches;
        }
    }
}