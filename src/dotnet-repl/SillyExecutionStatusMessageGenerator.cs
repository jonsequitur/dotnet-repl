// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Repl
{
    public class SillyExecutionStatusMessageGenerator
    {
        private readonly Random _random = new();

        private readonly string[] _verbs =
        {
            "🤞",
            "🦾",
            "🧠",
            "😎",
            "😱",
            "🙌",
            "🙌🏾",
            "Calculating",
            "Collating",
            "Compiling",
            "Computing",
            "Executing",
            "Invoking",
            "Running",
            "Tabulating",
            "Warming up",
        };

        private readonly string[] _conjunctions =
        {
            "all kinds of",
            "all the",
            "lots of",
            "much",
            "several",
            "so many",
            "so much",
            "the",
            "those",
        };

        private readonly string[] _nouns =
        {
            "💻",
            "📊",
            "🤖",
            "bits",
            "codes",
            "data transformations",
            "data",
            "exceptions",
            "functions",
            "genius",
            "internets",
            "implementation details",
            "monads",
            "pure functions",
            "side effects",
            "smart stuff",
            "software",
            "things",
        };

        public string GetStatusMessage() =>
            $"{_verbs[_random.Next(_verbs.Length)]} {_conjunctions[_random.Next(_conjunctions.Length)]} {_nouns[_random.Next(_nouns.Length)]}";
    }
}