using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive;
using Xunit;

namespace dotnet_repl.Tests
{
    public class KernelChooserTests
    {
        [Theory]
        [InlineData("#!fsharp", "fsharp")]
        [InlineData("#!f#", "fsharp")]
        [InlineData("#!fsharp\n123\n#!csharp", "csharp")]
        [InlineData("#!fsharp\n123\n#!csharp\n123", "csharp")]
        [InlineData("#!fsharp\n123\n#!csharp\n123", "csharp")]
        [InlineData("#!csharp\n123\n#!fsharp\n123", "fsharp")]
        [InlineData("#!csharp\n123\n#!fsharp\n#!help", "fsharp")]
        public async Task Kernel_chooser_magic_commands_are_sticky(string submission, string expectedKernelName)
        {
            using var kernel = Repl.CreateKernel(new StartupOptions("csharp"));

            await kernel.SubmitCodeAsync(submission);

            kernel.DefaultKernelName.Should().Be(expectedKernelName);
        }
    }
}