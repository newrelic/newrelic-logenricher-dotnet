// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using Serilog.Core;
using Serilog.Events;

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
}
