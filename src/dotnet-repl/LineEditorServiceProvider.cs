using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dotnet_repl
{
    public sealed class LineEditorServiceProvider : IServiceProvider
    {
        private readonly KernelCompletion _completion;

        public LineEditorServiceProvider(KernelCompletion completion)
        {
            if (completion is null)
            {
                throw new ArgumentNullException(nameof(completion));
            }

            _completion = completion;
        }

        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(KernelCompletion))
            {
                return _completion;
            }

            return null;
        }
    }
}
