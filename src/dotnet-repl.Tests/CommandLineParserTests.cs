using System.CommandLine.IO;
using System.CommandLine.Parsing;
using FluentAssertions;
using Xunit;

namespace dotnet_repl.Tests;

public class CommandLineParserTests
{
    private readonly Parser _parser;

    public CommandLineParserTests()
    {
        _parser = CommandLineParser.Create();
    }

    [Fact(Skip = "Needs System.CommandLine improvements")]
    public void Help_is_snazzy()
    {
        var parser = CommandLineParser.Create();

        var console = new TestConsole();

        parser.Invoke("-h", console);

        console.Out.ToString().Should().Be("snazzy!");

        Assert.True(false, "Test Help_is_snazzy is not written yet.");
    }

    [Fact]
    public void Parser_configuration_is_valid()
    {
        _parser.Configuration.ThrowIfInvalid();
    }

    [Fact]
    public void Inputs_parse_key_value_pairs_into_a_dictionary()
    {
        var result = _parser.Parse("--input one=1 --input two=2");

        result.GetValueForOption(CommandLineParser.InputsOption)
              .Should()
              .ContainKey("one")
              .WhoseValue.Should().Be("1");

        result.GetValueForOption(CommandLineParser.InputsOption)
              .Should()
              .ContainKey("two")
              .WhoseValue.Should().Be("2");
    }

    [Fact]
    public void Input_values_can_contain_spaces_if_quoted()
    {
        var result = _parser.Parse("--input words=\"the quick brown fox\"");

        result.GetValueForOption(CommandLineParser.InputsOption)
              .Should()
              .ContainKey("words")
              .WhoseValue.Should().Be("the quick brown fox");
    }
}