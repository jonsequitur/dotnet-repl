// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine.IO;
using System.CommandLine.Parsing;
using FluentAssertions;
using Xunit;

namespace dotnet_repl.Tests
{
    public class CommandLineParserTests
    {
        [Fact]
        public void Help_is_snazzy()
        {
            var parser = CommandLineParser.Create();

            var console = new TestConsole();

            parser.Invoke("-h", console);

            console.Out.ToString().Should().Be("snazzy!");

            // TODO-JOSEQU (Help_is_snazzy) write test
            Assert.True(false, "Test Help_is_snazzy is not written yet.");
        }
        


    }
}