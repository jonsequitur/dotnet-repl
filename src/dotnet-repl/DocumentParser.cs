using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Documents;
using Microsoft.DotNet.Interactive.Documents.Jupyter;

namespace dotnet_repl
{
    public static class DocumentParser
    {
        public static async Task<InteractiveDocument> ReadFileAsInteractiveDocument(
            FileInfo file,
            CompositeKernel kernel)
        {
            using var stream = file.OpenRead();

            List<KernelName> kernelNames = new();

            var kernelChoosers = kernel.Directives.OfType<ChooseKernelDirective>();

            foreach (var kernelChooser in kernelChoosers)
            {
                List<string> kernelAliases = new();

                foreach (var alias in kernelChooser.Aliases)
                {
                    kernelAliases.Add(alias[2..]);
                }

                kernelNames.Add(new KernelName(kernelChooser.Name[2..], kernelAliases));
            }

            var notebook = file.Extension.ToLowerInvariant() switch
            {
                ".ipynb" => await Notebook.ReadAsync(stream, kernelNames),
                ".dib" => await CodeSubmission.ReadAsync(stream, "csharp", kernelNames),
                _ => throw new InvalidOperationException($"Unrecognized extension for a notebook: {file.Extension}"),
            };

            return notebook;
        }
    }
}
