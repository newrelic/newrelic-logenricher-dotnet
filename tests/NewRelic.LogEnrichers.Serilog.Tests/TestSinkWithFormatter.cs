// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.IO;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;

namespace NewRelic.LogEnrichers.Serilog.Tests
{
    /// <summary>
    /// Extends the TestSink to accept a formatter.  Captures formatted output.
    /// </summary>
    public class TestSinkWithFormatter : ILogEventSink
    {
        public readonly List<InputOutputPairing> InputsAndOutputs = new List<InputOutputPairing>();

        private readonly ITextFormatter _formatter;

        public TestSinkWithFormatter(ITextFormatter formatter)
        {
            _formatter = formatter;
        }

        public void Emit(LogEvent logEvent)
        {
            var ioPairing = new InputOutputPairing(logEvent);
            InputsAndOutputs.Add(ioPairing);

            using (var writer = new StringWriter())
            {
                _formatter.Format(logEvent, writer);
                ioPairing.FormattedOutput = writer.ToString();
            }
        }
    }
}
