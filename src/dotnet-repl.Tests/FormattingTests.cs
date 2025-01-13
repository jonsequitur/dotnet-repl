using System.Collections.Generic;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Xunit;

namespace dotnet_repl.Tests;

public class FormattingTests
{
    public FormattingTests()
    {
        Repl.UseDefaultSpectreFormatting();
    }

    [Fact]
    public async Task Null_is_formatted_as_null()
    {
        using var kernel = KernelBuilder.CreateKernel(new("csharp"));

        var result = await kernel.SubmitCodeAsync("null");

        result.Events
              .Should()
              .ContainSingle<ReturnValueProduced>()
              .Which
              .FormattedValues
              .Single()
              .Value
              .Should()
              .Be("<null>");
    }

    [Fact]
    public void List_expansion_is_limited()
    {
        Enumerable.Range(1, 1000).ToDisplayString().Should().Contain("(980 more)");
    }
}