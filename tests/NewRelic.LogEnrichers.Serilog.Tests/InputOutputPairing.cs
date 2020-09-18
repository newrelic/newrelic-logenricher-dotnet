// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using Serilog.Events;

namespace NewRelic.LogEnrichers.Serilog.Tests
{
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
