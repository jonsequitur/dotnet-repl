using System.CommandLine.IO;
using System.CommandLine.Parsing;
using FluentAssertions;
using Xunit;

namespace dotnet_repl.Tests;

public class CommandLineParserTests
{
    [Fact(Skip = "Needs System.CommandLine improvements")]
    public void Help_is_snazzy()
    {
        var parser = CommandLineParser.Create();

        var console = new TestConsole();

        parser.Invoke("-h", console);

        console.Out.ToString().Should().Be("snazzy!");

        // TODO-JOSEQU (Help_is_snazzy) write test
        Assert.True(false, "Test Help_is_snazzy is not written yet.");
    }


    [Fact]
    public void Parser_configuration_is_valid()
    {
        CommandLineParser.Create().Configuration.ThrowIfInvalid();
    }
}