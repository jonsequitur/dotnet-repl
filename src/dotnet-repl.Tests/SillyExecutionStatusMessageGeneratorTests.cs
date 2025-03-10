using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace dotnet_repl.Tests;

public class SillyExecutionStatusMessageGeneratorTests
{
    private readonly ITestOutputHelper _output;

    public SillyExecutionStatusMessageGeneratorTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void SillyExecutionStatusMessageGeneratorTests_is_funny()
    {
        var generator = new SillyExecutionStatusMessageGenerator();

        foreach (var _ in Enumerable.Range(1, 20))
        {
            _output.WriteLine(generator.GetStatusMessage());
        }
    }
}