// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using RadLine;
using Xunit;
using Xunit.Abstractions;

namespace dotnet_repl.Tests
{
    public class CompletionTests : ReplInteractionTests
    {
        public CompletionTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void it_completes_methods_after_dot()
        {
            var buffer = new LineBuffer("");
            buffer.Insert("Console.");
            buffer.MoveEnd();

            var context = new LineEditorContext(buffer);
            context.Execute(new AutoCompleteCommand(AutoComplete.Next));

            buffer.Content.Should().Be("Console.BackgroundColor");
        }

        [Fact]
        public void it_cycles_through_methods_after_dot()
        {
            var buffer = new LineBuffer("");
            buffer.Insert("Console.");
            buffer.MoveEnd();

            var context = new LineEditorContext(buffer, ServiceProvider);
            context.Execute(new AutoCompleteCommand(AutoComplete.Next));
            context.Execute(new AutoCompleteCommand(AutoComplete.Next));

            buffer.Content.Should().Be("Console.Beep");
        }
    }
}