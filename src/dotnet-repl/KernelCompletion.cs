// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using RadLine;

namespace Microsoft.DotNet.Interactive.Repl
{
    public class KernelCompletion : ITextCompletion
    {
        private readonly Kernel _kernel;

        public KernelCompletion(Kernel kernel)
        {
            _kernel = kernel;
        }

        public IEnumerable<string>? GetCompletions(string prefix, string word, string suffix)
        {
            return GetCompletionsAsync(prefix, word, suffix).Result;
        }

        private async Task<IEnumerable<string>> GetCompletionsAsync(string prefix, string word, string suffix)
        {
            var code = prefix + word;

            var result = await _kernel.SendAsync(
                             new RequestCompletions(code, new LinePosition(0, prefix.Length + word.Length)));

            var results = await result
                                .KernelEvents
                                .OfType<CompletionsProduced>()
                                .FirstOrDefaultAsync();

            return results switch
            {
                { } => results.Completions.Select(c => prefix + word + c.InsertText),
                _ => Array.Empty<string>()
            };
        }
    }
}