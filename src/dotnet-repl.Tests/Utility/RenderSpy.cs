using System.Collections.Generic;
using Spectre.Console.Rendering;

namespace dotnet_repl.Tests.Utility
{
    public class RenderSpy : IRenderHook
    {
        public List<IRenderable> Renderables { get; } = new();

        public IEnumerable<IRenderable> Process(RenderOptions options, IEnumerable<IRenderable> renderables)
        {
            Renderables.AddRange(renderables);
            return renderables;
        }
    }
}