using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Repl;
using Xunit;
using Xunit.Abstractions;

namespace dotnet_repl.Tests
{
    public class PromptTests : ReplInteractionTests
    {
        public PromptTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task prompt_indicates_ready()
        {
            Terminal.SendString("var x = 1;");
            Terminal.SendEnter();

            await Loop.CommandCompleted();

            Loop.Prompt.Should().Be(PromptMarkup.Ready);
        }

        [Fact]
        public async Task prompt_indicates_partial_submission()
        {
            Terminal.SendString("var x = 1");
            Terminal.SendEnter();

            await Loop.CommandCompleted();

            Loop.Prompt.Should().Be(PromptMarkup.More);
        }
    }
}