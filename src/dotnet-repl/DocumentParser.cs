using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Documents;
using Microsoft.DotNet.Interactive.Documents.Jupyter;
using KernelInfo = Microsoft.DotNet.Interactive.Documents.KernelInfo;

namespace dotnet_repl;

public static class DocumentParser
{
    public static async Task<InteractiveDocument> LoadInteractiveDocumentAsync(
        FileInfo file,
        CompositeKernel kernel)
    {
        var kernelInfos = CreateKernelInfos(kernel);
        return await LoadInteractiveDocumentAsync(file, kernelInfos);
    }

    public static async Task<InteractiveDocument> LoadInteractiveDocumentAsync(
        FileInfo file,
        KernelInfoCollection kernelInfos)
    {
        var fileContents = await File.ReadAllTextAsync(file.FullName);

        return file.Extension.ToLowerInvariant() switch
        {
            ".ipynb" => Notebook.Parse(fileContents, kernelInfos),
            ".dib" => CodeSubmission.Parse(fileContents, kernelInfos),

            ".cs" => new InteractiveDocument { new InteractiveDocumentElement(fileContents, "csharp") },
            ".csx" => new InteractiveDocument { new InteractiveDocumentElement(fileContents, "csharp") },
            ".fs" => new InteractiveDocument { new InteractiveDocumentElement(fileContents, "fsharp") },
            ".fsx" => new InteractiveDocument { new InteractiveDocumentElement(fileContents, "fsharp") },
            ".ps1" => new InteractiveDocument { new InteractiveDocumentElement(fileContents, "pwsh") },
            ".html" => new InteractiveDocument { new InteractiveDocumentElement(fileContents, "html") },
            ".js" => new InteractiveDocument { new InteractiveDocumentElement(fileContents, "javascript") },
            
            _ => throw new InvalidOperationException($"Unrecognized extension for a notebook: {file.Extension}"),
        };
    }

    public static KernelInfoCollection CreateKernelInfos(this CompositeKernel kernel)
    {
        KernelInfoCollection kernelInfos = new();

        var kernelChoosers = kernel.Directives.OfType<ChooseKernelDirective>();

        foreach (var kernelChooser in kernelChoosers)
        {
            List<string> kernelAliases = new();

            foreach (var alias in kernelChooser.Aliases.Where(a => a != kernelChooser.Name))
            {
                kernelAliases.Add(alias[2..]);
            }

            kernelInfos.Add(new KernelInfo(kernelChooser.Name[2..], kernelAliases));
        }

        return kernelInfos;
    }
}