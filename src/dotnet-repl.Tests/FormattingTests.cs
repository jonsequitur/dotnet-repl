// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Events;
using Xunit;

namespace dotnet_repl.Tests
{
    public class FormattingTests
    {
        [Fact]
        public async Task Null_is_formatted_as_null()
        {
            using var kernel = Repl.CreateKernel(new("csharp"));

            var result = await kernel.SubmitCodeAsync("null");

            var events = result.KernelEvents.ToSubscribedList();

            events
                .Should()
                .ContainSingle<ReturnValueProduced>()
                .Which
                .FormattedValues
                .Should()
                .ContainSingle(f => f.Value == "null");
        }
    }
}