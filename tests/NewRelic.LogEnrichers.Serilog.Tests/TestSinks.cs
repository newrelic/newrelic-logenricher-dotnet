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
    /// This sink lets us examine the results of the enricher and formatter.
    /// </summary>
    public class TestSink : ILogEventSink
    {
        public readonly List<LogEvent> LogEvents = new List<LogEvent>();

        public void Emit(LogEvent logEvent)
        {
            LogEvents.Add(logEvent);
        }
    }

    /// <summary>
    /// Extends the TestSink to accept a formatter.  Captures formatted output
    /// </summary>
    public class TestSinkWithFormatter : ILogEventSink
    {
        private readonly ITextFormatter _formatter;
        public readonly List<InputOutputPairing> InputsAndOutputs = new List<InputOutputPairing>();

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

    public class InputOutputPairing
    {
        public LogEvent LogEvent { get; set; }
        public string FormattedOutput { get; set; }

        public InputOutputPairing(LogEvent logEvent)
        {
            LogEvent = logEvent;
        }
    }


}
