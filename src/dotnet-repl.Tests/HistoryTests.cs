// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Repl;
using Microsoft.DotNet.Interactive.Repl.LineEditorCommands;
using RadLine;
using Xunit;
using Xunit.Abstractions;

namespace dotnet_repl.Tests
{
    public class HistoryTests : ReplInteractionTests
    {
        public HistoryTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void previous_does_not_clear_buffer_when_there_is_no_history()
        {
            var buffer = new LineBuffer("hi");
            var context = new LineEditorContext(buffer, new KernelCompletion(Kernel));

            context.Execute(new PreviousHistory(LoopController));

            context.Buffer.Content.Should().Be("hi");
        }

        [Fact]
        public void previous_replaces_buffer_with_last_submission()
        {
            LoopController.TryAddToHistory(new SubmitCode("1"));

            var buffer = new LineBuffer("hi");
            var context = new LineEditorContext(buffer, new KernelCompletion(Kernel));

            context.Execute(new PreviousHistory(LoopController));

            context.Buffer.Content.Should().Be("1");
            LoopController.HistoryIndex.Should().Be(0);
        }

        [Fact]
        public void invoking_previous_twice_replaces_buffer_with_submission_before_last()
        {
            LoopController.TryAddToHistory(new SubmitCode("1"));
            LoopController.TryAddToHistory(new SubmitCode("2"));

            var buffer = new LineBuffer("hi");
            var context = new LineEditorContext(buffer, new KernelCompletion(Kernel));

            context.Execute(new PreviousHistory(LoopController));
            context.Execute(new PreviousHistory(LoopController));

            context.Buffer.Content.Should().Be("1");
            LoopController.HistoryIndex.Should().Be(0);
        }

        [Fact]
        public void invoking_previous_repeatedly_stops_at_last_submission()
        {
            LoopController.TryAddToHistory(new SubmitCode("1"));

            var buffer = new LineBuffer("hi");
            var context = new LineEditorContext(buffer, new KernelCompletion(Kernel));

            context.Execute(new PreviousHistory(LoopController));
            context.Execute(new PreviousHistory(LoopController));

            context.Buffer.Content.Should().Be("1");
            LoopController.HistoryIndex.Should().Be(0);
        }

        [Fact]
        public void next_does_not_clear_buffer_when_there_is_no_history()
        {
            var buffer = new LineBuffer("hi");
            var context = new LineEditorContext(buffer, new KernelCompletion(Kernel));

            context.Execute(new NextHistory(LoopController));

            context.Buffer.Content.Should().Be("hi");
            LoopController.HistoryIndex.Should().Be(-1);
        }

        [Fact]
        public void previous_then_next_returns_to_original_buffer()
        {
            LoopController.TryAddToHistory(new SubmitCode("1"));

            var buffer = new LineBuffer("hi");
            var context = new LineEditorContext(buffer, new KernelCompletion(Kernel));

            context.Execute(new PreviousHistory(LoopController));
            context.Execute(new NextHistory(LoopController));

            context.Buffer.Content.Should().Be("hi");
            LoopController.HistoryIndex.Should().Be(1);
        }

        [Fact]
        public void previous_twice_then_next_returns_to_original_buffer()
        {
            LoopController.TryAddToHistory(new SubmitCode("1"));
            LoopController.TryAddToHistory(new SubmitCode("2"));

            var buffer = new LineBuffer("hi");
            var context = new LineEditorContext(buffer, new KernelCompletion(Kernel));

            context.Execute(new PreviousHistory(LoopController));
            context.Execute(new PreviousHistory(LoopController));
            context.Execute(new NextHistory(LoopController));

            context.Buffer.Content.Should().Be("2");
            LoopController.HistoryIndex.Should().Be(1);
        }

        [Fact]
        public void Submitting_an_entry_resets_history_index()
        {
            LoopController.TryAddToHistory(new SubmitCode("1"));
            LoopController.TryAddToHistory(new SubmitCode("2"));
            LoopController.TryAddToHistory(new SubmitCode("3"));

            var buffer = new LineBuffer("hi");
            var context = new LineEditorContext(buffer, new KernelCompletion(Kernel));

            context.Execute(new PreviousHistory(LoopController));
            context.Execute(new PreviousHistory(LoopController));

            context.Execute(new SubmitCommand());

            context.Buffer.Content.Should().Be("");

            LoopController.HistoryIndex.Should().Be(1);

            // FIX: (Submitting_an_entry_resets_history_index) write test
            throw new NotImplementedException();
        }

        [Fact]
        public void Repeating_a_submission_resets_history_index()
        {   
            var buffer = new LineBuffer();
            var context = new LineEditorContext(buffer, new KernelCompletion(Kernel));

            buffer.Insert("1");
            context.Execute(new SubmitCommand());
            context.Execute(new PreviousHistory(LoopController));



            // FIX: (Repeating_a_submission_resets_history_index) write test
            throw new NotImplementedException();
        }
    }
}