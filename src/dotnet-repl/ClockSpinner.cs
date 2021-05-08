// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Spectre.Console;

namespace Microsoft.DotNet.Interactive.Repl
{
    internal class ClockSpinner : Spinner
    {
        public override TimeSpan Interval => TimeSpan.FromMilliseconds(.3);

        public override bool IsUnicode => true;

        public override IReadOnlyList<string> Frames { get; } = new[]
        {
            "🕛",
            "🕐",
            "🕑",
            "🕒",
            "🕓",
            "🕔",
            "🕕",
            "🕖",
            "🕗",
            "🕘",
            "🕙",
            "🕚",
        };
    }
}