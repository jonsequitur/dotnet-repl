// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace dotnet_repl.Tests;

[CollectionDefinition("Do not parallelize", DisableParallelization = true)]
public class SerialTestCollectionDefinition 
{
}