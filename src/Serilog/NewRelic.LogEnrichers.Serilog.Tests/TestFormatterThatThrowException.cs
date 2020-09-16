// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using Serilog.Events;
using Serilog.Formatting;

namespace NewRelic.LogEnrichers.Serilog.Tests
{
    public class TestFormatterThatThrowException : ITextFormatter
    {
        public void Format(LogEvent logEvent, TextWriter output)
        {
            output.WriteLine("Hello World");
            throw new Exception("Goodbye World");
        }
    }
}
