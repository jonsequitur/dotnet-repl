using System;
using System.Collections.Generic;
using Spectre.Console;

namespace dotnet_repl
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