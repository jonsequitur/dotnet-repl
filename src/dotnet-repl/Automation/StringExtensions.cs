// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Automation;

internal static class StringExtensions
{
    public static string[] SplitIntoLines(this string s) =>
        s.Split(["\r\n", "\n"], StringSplitOptions.None);
}