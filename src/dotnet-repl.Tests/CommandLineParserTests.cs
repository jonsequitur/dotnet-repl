using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace dotnet_repl.Tests;

public class CommandLineParserTests
{
    private readonly RootCommand _rootCommand;

    public CommandLineParserTests()
    {
        _rootCommand = CommandLineParser.Create();
    }

    [Fact]
    public void Help_is_snazzy()
    {
        var parseResult = _rootCommand.Parse("-h");
        parseResult.Configuration.Output = new StringWriter();
        ((SynchronousCommandLineAction)parseResult.Action).Invoke(parseResult);

        var outputLines = parseResult.Configuration
                                     .Output
                                     .ToString()
                                     .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                                     .Select(l => l.Trim());

        string.Join('\n', outputLines)
              .Should().Contain(
                  """
                  [38;5;215m      _   _   _____   _____     ____    _____   ____    _     [0m
                  [38;5;215m     | \ | | | ____| |_   _|   |  _ \  | ____| |  _ \  | |    [0m
                  [38;5;215m     |  \| | |  _|     | |     | |_) | |  _|   | |_) | | |    [0m
                  [38;5;215m  _  | |\  | | |___    | |     |  _ <  | |___  |  __/  | |___ [0m
                  [38;5;215m (_) |_| \_| |_____|   |_|     |_| \_\ |_____| |_|     |_____|[0m
                  [38;5;215m                                                              [0m
                  """.Replace("\r", ""));
    }

    [Fact]
    public void Parser_configuration_is_valid()
    {
        _rootCommand.Parse("").Configuration.ThrowIfInvalid();
    }

    [Fact]
    public void Inputs_parse_key_value_pairs_into_a_dictionary()
    {
        var result = _rootCommand.Parse("--input one=1 --input two=2");

        result.GetValue(CommandLineParser.InputsOption)
              .Should()
              .ContainKey("one")
              .WhoseValue.Should().Be("1");

        result.GetValue(CommandLineParser.InputsOption)
              .Should()
              .ContainKey("two")
              .WhoseValue.Should().Be("2");
    }

    [Fact]
    public void Input_values_can_contain_spaces_if_quoted()
    {
        var result = _rootCommand.Parse("--input words=\"the quick brown fox\"");

        result.GetValue(CommandLineParser.InputsOption)
              .Should()
              .ContainKey("words")
              .WhoseValue.Should().Be("the quick brown fox");
    }
}