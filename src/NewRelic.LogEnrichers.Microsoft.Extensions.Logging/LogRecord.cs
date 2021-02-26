// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using Microsoft.Extensions.Logging;

namespace NewRelic.LogEnrichers.Microsoft.Extensions.Logging
{
    // Container for log message and properties, created in LogEnricherLogger.Log
    // incomplete properties defined here

    public sealed class LogRecord
    {
        internal LogRecord(DateTime timestamp, string categoryName, LogLevel logLevel, EventId eventId, object state, Exception exception)
        {
            Timestamp = timestamp;
            CategoryName = categoryName;
            LogLevel = logLevel;
            EventId = eventId;
            State = state;
            Exception = exception;
        }

        public DateTime Timestamp { get; }

        public string CategoryName { get; }

        public LogLevel LogLevel { get; }

        public EventId EventId { get; }

        public object State { get; }

        public Exception Exception { get; }
    }
}
