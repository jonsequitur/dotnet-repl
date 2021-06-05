using System;

namespace dotnet_repl
{
    public sealed class LineEditorServiceProvider : IServiceProvider
    {
        private readonly KernelCompletion _completion;

        public LineEditorServiceProvider(KernelCompletion completion)
        {
            _completion = completion ?? throw new ArgumentNullException(nameof(completion));
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